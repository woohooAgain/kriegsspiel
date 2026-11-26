using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

public class ClockTests
{
    [Fact]
    public void EventPhase_FollowsRulesSectionTwoOrder()
    {
        Assert.Equal(0, (int)EventPhase.Dispatch);
        Assert.Equal(1, (int)EventPhase.Delivery);
        Assert.Equal(2, (int)EventPhase.Understanding);
        Assert.Equal(3, (int)EventPhase.Movement);
        Assert.Equal(4, (int)EventPhase.Combat);
        Assert.Equal(5, (int)EventPhase.ReportCreate);
        Assert.Equal(6, (int)EventPhase.ReportDeliver);
        Assert.True((int)EventPhase.Scheduled > (int)EventPhase.ReportDeliver);
    }

    [Fact]
    public void AdvanceTo_YieldsInOrder()
    {
        var clock = new Clock(360);
        clock.Schedule(new ReportDue(390, "u1", ReportKind.Status), 390);
        clock.Schedule(new ReportDue(375, "u2", ReportKind.Status), 375);

        var evts = clock.AdvanceTo(400).ToList();

        Assert.Equal(2, evts.Count);
        Assert.Equal(375, evts[0].Minute);
        Assert.Equal("u2", ((ReportDue)evts[0]).UnitId);
        Assert.Equal(400, clock.Now);
    }

    [Fact]
    public void AdvanceTo_SameMinute_YieldsByRulesPhaseOrder()
    {
        var clock = new Clock(100);
        var dispatch = new QuerySent(100, "hq", "u1");
        var delivery = new OrderDelivered(100, "o1");
        var understanding = new OrderAccepted(100, "o1");
        var movement = new UnitMoved(100, "u1", "z1");
        var combat = new CombatTick(100, "z1");
        var reportCreate = new ReportDue(100, "u1", ReportKind.Status);
        var reportDeliver = new ReportDelivered(100, "r1");
        clock.Schedule(reportDeliver, 100, EventPhase.ReportDeliver);
        clock.Schedule(reportCreate, 100, EventPhase.ReportCreate);
        clock.Schedule(combat, 100, EventPhase.Combat);
        clock.Schedule(movement, 100, EventPhase.Movement);
        clock.Schedule(understanding, 100, EventPhase.Understanding);
        clock.Schedule(delivery, 100, EventPhase.Delivery);
        clock.Schedule(dispatch, 100, EventPhase.Dispatch);

        var evts = clock.AdvanceTo(100).ToList();

        Assert.Equal(
            new GameEvent[] { dispatch, delivery, understanding, movement, combat, reportCreate, reportDeliver },
            evts);
        Assert.Equal(100, clock.Now);
    }

    [Fact]
    public void AdvanceTo_SamePhase_YieldsByExplicitSequence()
    {
        var clock = new Clock(50);
        var first = new OrderIssued(50, "first");
        var second = new OrderIssued(50, "second");
        var third = new OrderIssued(50, "third");
        clock.Schedule(first, new ScheduledAt(50, EventPhase.Scheduled, 3));
        clock.Schedule(second, new ScheduledAt(50, EventPhase.Scheduled, 1));
        clock.Schedule(third, new ScheduledAt(50, EventPhase.Scheduled, 2));

        var evts = clock.AdvanceTo(50).ToList();

        Assert.Equal(new GameEvent[] { second, third, first }, evts);
    }

    [Fact]
    public void AdvanceTo_SamePhase_AutoSequenceFollowsInsertion()
    {
        var clock = new Clock(50);
        var first = new UnitMoved(50, "u1", "z1");
        var second = new UnitMoved(50, "u2", "z2");
        clock.Schedule(first, 50, EventPhase.Movement);
        clock.Schedule(second, 50, EventPhase.Movement);

        var evts = clock.AdvanceTo(50).ToList();

        Assert.Equal(new GameEvent[] { first, second }, evts);
    }

    [Fact]
    public void Schedule_AutoSequenceFollowsExplicitReservation()
    {
        var clock = new Clock(0);
        clock.Schedule(new OrderIssued(10, "explicit"), new ScheduledAt(10, EventPhase.Scheduled, 5));
        clock.Schedule(new OrderIssued(10, "auto"), 10, EventPhase.Scheduled);

        var evts = clock.AdvanceTo(10).ToList();

        Assert.Equal("explicit", ((OrderIssued)evts[0]).OrderId);
        Assert.Equal("auto", ((OrderIssued)evts[1]).OrderId);
    }

    [Fact]
    public void Schedule_PastMinute_Throws()
    {
        var clock = new Clock(100);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.Schedule(new OrderIssued(90, "o"), 90));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            clock.Schedule(new OrderIssued(100, "o"), new ScheduledAt(99, EventPhase.Delivery)));
        Assert.Equal(0, clock.PendingCount);
    }

    [Fact]
    public void Schedule_AlreadyProcessedPhase_Throws()
    {
        var clock = new Clock(100);
        clock.Schedule(new UnitMoved(100, "u", "z"), 100, EventPhase.Movement);
        Assert.Single(clock.AdvanceTo(100));

        Assert.Throws<InvalidOperationException>(() =>
            clock.Schedule(new OrderDelivered(100, "o"), 100, EventPhase.Delivery));
        Assert.Throws<InvalidOperationException>(() =>
            clock.Schedule(new UnitMoved(100, "u2", "z"), new ScheduledAt(100, EventPhase.Movement, 0)));
        clock.Schedule(new CombatTick(100, "z"), 100, EventPhase.Combat);
        Assert.Equal(1, clock.PendingCount);
    }

    [Fact]
    public void AdvanceTo_PastTarget_ThrowsImmediately()
    {
        var clock = new Clock(100);

        Assert.Throws<ArgumentOutOfRangeException>(() => clock.AdvanceTo(99));
        Assert.Equal(100, clock.Now);
    }

    [Fact]
    public void AdvanceTo_CurrentMinute_ProcessesPendingEvents()
    {
        var clock = new Clock(100);
        clock.Schedule(new OrderDelivered(100, "o"), 100, EventPhase.Delivery);

        var evts = clock.AdvanceTo(100).ToList();

        Assert.Single(evts);
        Assert.IsType<OrderDelivered>(evts[0]);
        Assert.Equal(100, clock.Now);
        Assert.Equal(0, clock.PendingCount);
        Assert.False(clock.HasPendingEvents);
    }

    [Fact]
    public void AdvanceTo_DoesNotLoseEventsAcrossMinutes()
    {
        var clock = new Clock(0);
        clock.Schedule(new OrderDelivered(10, "o10"), 10, EventPhase.Delivery);
        clock.Schedule(new UnitMoved(20, "u20", "z"), 20, EventPhase.Movement);
        clock.Schedule(new CombatTick(30, "z"), 30, EventPhase.Combat);

        var first = clock.AdvanceTo(20).ToList();
        var second = clock.AdvanceTo(30).ToList();

        Assert.Equal(2, first.Count);
        Assert.Single(second);
        Assert.Equal(30, clock.Now);
        Assert.Equal(0, clock.PendingCount);
    }

    [Fact]
    public void PendingCount_DecreasesAndPendingKeysAreReadOnly()
    {
        var clock = new Clock(0);
        clock.Schedule(new OrderDelivered(10, "o"), 10, EventPhase.Delivery);
        clock.Schedule(new CombatTick(20, "z"), 20, EventPhase.Combat);
        clock.Schedule(new ReportDue(30, "u", ReportKind.Status), 30, EventPhase.ReportCreate);

        Assert.Equal(3, clock.PendingCount);
        Assert.True(clock.HasPendingEvents);
        Assert.Equal(new[] { 10, 20, 30 }, clock.PendingKeys.Select(key => key.Minute));

        var evts = clock.AdvanceTo(20).ToList();

        Assert.Equal(2, evts.Count);
        Assert.Equal(1, clock.PendingCount);
        Assert.Equal(20, clock.Now);
        Assert.Equal(new[] { 30 }, clock.PendingKeys.Select(key => key.Minute));
    }

    [Fact]
    public void Schedule_CompatibilityOverload_MapsEventTypesToRulesPhases()
    {
        var clock = new Clock(0);
        clock.Schedule(new OrderLost(10, "o"), 10);
        clock.Schedule(new OrderMisinterpreted(10, "o", Misinterpretation.None), 10);
        clock.Schedule(new UnitArrived(10, "u", "z"), 10);
        clock.Schedule(new CombatTick(10, "z"), 10);
        clock.Schedule(new ReportDue(10, "u", ReportKind.Status), 10);
        clock.Schedule(new ReportDelivered(10, "r"), 10);
        clock.Schedule(new OrderIssued(10, "o"), 10);

        Assert.Equal(
            new[]
            {
                EventPhase.Delivery,
                EventPhase.Understanding,
                EventPhase.Movement,
                EventPhase.Combat,
                EventPhase.ReportCreate,
                EventPhase.ReportDeliver,
                EventPhase.Scheduled
            },
            clock.PendingKeys.Select(key => key.Phase));
    }

    [Fact]
    public void PhaseFor_And_PhaseForName_RecognizeRulesPhases()
    {
        Assert.Equal(EventPhase.Dispatch, Clock.PhaseForName("OrderDispatch"));
        Assert.Equal(EventPhase.ReportCreate, Clock.PhaseForName("ReportDue"));
        Assert.Equal(EventPhase.ReportDeliver, Clock.PhaseForName("ReportDelivered"));
        Assert.Equal(EventPhase.Scheduled, Clock.PhaseForName("CourierReturned"));
        Assert.Equal(EventPhase.Delivery, Clock.PhaseFor(new OrderDelivered(1, "o")));
        Assert.Equal(EventPhase.Understanding, Clock.PhaseFor(new OrderAccepted(1, "o")));
        Assert.Equal(EventPhase.Movement, Clock.PhaseFor(new UnitMoved(1, "u", "z")));
        Assert.Equal(EventPhase.Combat, Clock.PhaseFor(new CombatTick(1, "z")));
        Assert.Equal(EventPhase.ReportCreate, Clock.PhaseFor(new ReportDue(1, "u", ReportKind.Status)));
    }

    [Fact]
    public void RandomSource_SameSeed_ProducesSameSequence()
    {
        var first = new DeterministicRandomSource(1234);
        var second = new DeterministicRandomSource(1234);

        for (int i = 0; i < 64; i++)
        {
            Assert.Equal(first.NextDouble(), second.NextDouble());
            Assert.Equal(first.NextInt(0, 100), second.NextInt(0, 100));
            Assert.Equal(first.Chance(0.5), second.Chance(0.5));
        }
    }

    [Fact]
    public void RandomSource_Restore_ContinuesSameSequence()
    {
        var source = new DeterministicRandomSource(7);
        for (int i = 0; i < 10; i++)
            source.NextDouble();
        var state = source.State;
        var expected = Enumerable.Range(0, 32).Select(_ => source.NextDouble()).ToArray();

        var replay = new DeterministicRandomSource(999);
        replay.Restore(state);
        var actual = Enumerable.Range(0, 32).Select(_ => replay.NextDouble()).ToArray();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void RandomSource_Reset_RestoresInitialSequence()
    {
        var source = new DeterministicRandomSource(42);
        var initialState = source.State;
        var first = Enumerable.Range(0, 32).Select(_ => source.NextDouble()).ToArray();

        source.Reset();
        Assert.Equal(initialState, source.State);
        var again = Enumerable.Range(0, 32).Select(_ => source.NextDouble()).ToArray();

        Assert.Equal(first, again);
    }

    [Fact]
    public void RandomSource_NextInt_StaysInRange()
    {
        var source = new DeterministicRandomSource(5);

        for (int i = 0; i < 200; i++)
        {
            var bounded = source.NextInt(-3, 7);
            Assert.InRange(bounded, -3, 6);
            var upper = source.NextInt(4);
            Assert.InRange(upper, 0, 3);
        }
    }

    [Fact]
    public void RandomSource_Chance_HandlesBoundaries()
    {
        var source = new DeterministicRandomSource(9);

        Assert.False(source.Chance(0));
        Assert.True(source.Chance(1));
        Assert.InRange(source.NextDouble(), 0.0, 1.0);
    }

    [Fact]
    public void RandomSource_DifferentSeeds_Diverge()
    {
        var first = new DeterministicRandomSource(1);
        var second = new DeterministicRandomSource(2);

        var firstSequence = Enumerable.Range(0, 8).Select(_ => first.NextDouble()).ToArray();
        var secondSequence = Enumerable.Range(0, 8).Select(_ => second.NextDouble()).ToArray();

        Assert.False(firstSequence.SequenceEqual(secondSequence));
    }
}
