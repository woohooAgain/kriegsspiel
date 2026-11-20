using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

public class ClockTests
{
    [Fact]
    public void EventPhase_HasStableValues()
    {
        Assert.Equal(0, (int)EventPhase.Delivery);
        Assert.Equal(1, (int)EventPhase.Understanding);
        Assert.Equal(2, (int)EventPhase.Movement);
        Assert.Equal(3, (int)EventPhase.Combat);
        Assert.Equal(4, (int)EventPhase.ReportCreate);
        Assert.Equal(5, (int)EventPhase.ReportDeliver);
        Assert.Equal(6, (int)EventPhase.Scheduled);
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
    public void AdvanceTo_SameMinute_YieldsByPhase()
    {
        var clock = new Clock(100);
        var delivery = new OrderDelivered(100, "o1");
        var understanding = new OrderAccepted(100, "o1");
        var movement = new UnitMoved(100, "u1", "z1");
        var combat = new CombatTick(100, "z1");
        var reportCreate = new ReportDue(100, "u1", ReportKind.Status);
        var reportDeliver = new ReportDue(100, "u1", ReportKind.Arrival);
        var scheduled = new OrderIssued(100, "o2");
        clock.Schedule(scheduled, 100, EventPhase.Scheduled);
        clock.Schedule(reportDeliver, 100, EventPhase.ReportDeliver);
        clock.Schedule(reportCreate, 100, EventPhase.ReportCreate);
        clock.Schedule(combat, 100, EventPhase.Combat);
        clock.Schedule(movement, 100, EventPhase.Movement);
        clock.Schedule(understanding, 100, EventPhase.Understanding);
        clock.Schedule(delivery, 100, EventPhase.Delivery);

        var evts = clock.AdvanceTo(100).ToList();

        Assert.Equal(
            new GameEvent[] { delivery, understanding, movement, combat, reportCreate, reportDeliver, scheduled },
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
    public void Schedule_CompatibilityOverload_MapsEventTypesToPhases()
    {
        var clock = new Clock(0);
        clock.Schedule(new OrderLost(10, "o"), 10);
        clock.Schedule(new OrderMisinterpreted(10, "o", Misinterpretation.None), 10);
        clock.Schedule(new UnitArrived(10, "u", "z"), 10);
        clock.Schedule(new CombatTick(10, "z"), 10);
        clock.Schedule(new ReportDue(10, "u", ReportKind.Status), 10);
        clock.Schedule(new DeliveredReport(10), 10);
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

    private sealed record DeliveredReport(int Minute) : GameEvent(Minute);
}
