using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class ExecutionResolverTests
{
    [Fact]
    public void PathPlanner_MultiHopRoute_HasCorrectStepsAndTime()
    {
        var state = State(
            Zone("A", TerrainType.Open, ("B", 30)),
            Zone("B", TerrainType.Forest, ("A", 30), ("C", 20)),
            Zone("C", TerrainType.Village, ("B", 20)));

        var planned = PathPlanner.TryPlan(state, "A", "C", MovementMode.March, new FrictionTable(), out var route);

        Assert.True(planned);
        Assert.Equal(["B", "C"], route.Steps.Select(s => s.ToZoneId));
        Assert.Equal(50, route.TotalMinutes);
        Assert.Equal(30, route.Steps[0].Minutes);
        Assert.Equal(20, route.Steps[1].Minutes);
    }

    [Fact]
    public void PathPlanner_MovementMode_ChangesTravelTime()
    {
        var state = State(
            Zone("A", TerrainType.Open, ("B", 30)),
            Zone("B", TerrainType.Open, ("A", 30)));

        Assert.True(PathPlanner.TryPlan(state, "A", "B", MovementMode.March, new FrictionTable(), out var march));
        Assert.True(PathPlanner.TryPlan(state, "A", "B", MovementMode.Covert, new FrictionTable(), out var covert));
        Assert.True(PathPlanner.TryPlan(state, "A", "B", MovementMode.Contact, new FrictionTable(), out var contact));

        Assert.Equal(30, march.TotalMinutes);
        Assert.Equal(42, covert.TotalMinutes);
        Assert.Equal(36, contact.TotalMinutes);
    }

    [Fact]
    public void PathPlanner_DestroyedBridgeObject_ForcesDetour()
    {
        var state = State(
            Zone("A", TerrainType.Open, ("B", 30), ("D", 20)),
            Zone("B", TerrainType.Bridge, ("A", 30), ("D", 15)),
            Zone("D", TerrainType.Open, ("A", 20), ("B", 15)));
        state.Zones["A"].SetEdgeType("B", EdgeType.Crossing);
        state.Zones["B"].SetEdgeType("A", EdgeType.Crossing);
        state.Zones["A"].Objects.Add(new ZoneObject
        {
            Kind = ZoneObjectKind.Bridge,
            EdgeId = PathPlanner.EdgeId("A", "B"),
            State = ZoneObjectState.Destroyed
        });

        var planned = PathPlanner.TryPlan(state, "A", "B", MovementMode.March, new FrictionTable(), out var route);

        Assert.True(planned);
        Assert.Equal(["D", "B"], route.Steps.Select(s => s.ToZoneId));
        Assert.Equal(35, route.TotalMinutes);
        Assert.False(PathPlanner.IsEdgePassable(state, "A", "B"));
    }

    [Fact]
    public void PathPlanner_BridgeIntactFalse_BlocksCrossingOnly()
    {
        var state = State(
            Zone("A", TerrainType.Bridge, ("B", 30), ("C", 25)),
            Zone("B", TerrainType.Open, ("A", 30)),
            Zone("C", TerrainType.Open, ("A", 25)));
        state.Zones["A"].SetEdgeType("B", EdgeType.Crossing);
        state.Zones["B"].SetEdgeType("A", EdgeType.Crossing);
        state.Zones["A"].SetEdgeType("C", EdgeType.Road);
        state.Zones["C"].SetEdgeType("A", EdgeType.Road);
        state.Zones["A"].BridgeIntact = false;

        Assert.False(PathPlanner.IsEdgePassable(state, "A", "B"));
        Assert.True(PathPlanner.IsEdgePassable(state, "A", "C"));
        Assert.True(PathPlanner.TryPlan(state, "A", "C", MovementMode.March, new FrictionTable(), out _));
        Assert.False(PathPlanner.TryPlan(state, "A", "B", MovementMode.March, new FrictionTable(), out _));
    }

    [Fact]
    public void Start_SchedulesStepwiseMovement_WithoutTeleport()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(target: "C");
        state.Orders.Add(order);
        var resolver = new ExecutionResolver();

        var startReports = resolver.Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(7)).ToList();

        Assert.Single(startReports);
        Assert.Equal(ReportKind.Status, startReports[0].Kind);
        var firstMove = Assert.IsType<UnitMoved>(state.Clock.AdvanceTo(360).First());
        Assert.Equal("B", firstMove.ToZoneId);
        Assert.Equal("A", unit.LocationZoneId);

        resolver.OnMove(firstMove, state, new FrictionTable(), new Random(7)).ToList();
        Assert.Equal("B", unit.LocationZoneId);
        Assert.DoesNotContain(unit.Id, state.Zones["A"].PresentUnitIds);
        Assert.Contains(unit.Id, state.Zones["B"].PresentUnitIds);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Minute == 360 && key.Phase == EventPhase.Movement);

        var secondMove = state.Clock.AdvanceTo(380).OfType<UnitMoved>().Single();
        Assert.Equal("C", secondMove.ToZoneId);
        Assert.Equal("B", unit.LocationZoneId);

        resolver.OnMove(secondMove, state, new FrictionTable(), new Random(7)).ToList();
        Assert.Equal("C", unit.LocationZoneId);
        var arrival = state.Clock.AdvanceTo(380).OfType<UnitArrived>().Single();
        resolver.OnArrive(arrival, state, new FrictionTable(), new Random(7)).ToList();
        Assert.Null(unit.CurrentOrderId);
    }

    [Fact]
    public void OnMove_EnemyInIntermediateZone_SchedulesCombatTickBeforeGoal()
    {
        var state = ChainState(out var unit);
        var enemy = NewUnit("E1", "B", new CommanderTraits(1, 1, 1, 1), Faction.Red);
        state.Units[enemy.Id] = enemy;
        state.Zones["B"].PresentUnitIds.Add(enemy.Id);
        var order = NewOrder(target: "C");
        state.Orders.Add(order);
        var resolver = new ExecutionResolver();

        resolver.Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(11)).ToList();
        var move = state.Clock.AdvanceTo(360).OfType<UnitMoved>().Single();
        var reports = resolver.OnMove(move, state, new FrictionTable(), new Random(11)).ToList();

        Assert.Contains(reports, r => r.Kind == ReportKind.Contact && r.FactKey == "contact:B");
        Assert.Equal("B", unit.LocationZoneId);
        var eventsAtMinute = state.Clock.AdvanceTo(360).ToList();
        Assert.Contains(eventsAtMinute, e => e is CombatTick tick && tick.ZoneId == "B");
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute > 360);
        Assert.DoesNotContain(eventsAtMinute, e => e is UnitMoved moved && moved.ToZoneId == "C");
    }

    [Fact]
    public void Start_MissedDeadline_ProducesImpossibleAndQuery()
    {
        var state = ChainState(out var unit, new CommanderTraits(0, 3, 0, 2));
        var order = NewOrder(target: "C", deadline: 380);
        state.Orders.Add(order);

        var reports = new ExecutionResolver()
            .Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(3))
            .ToList();

        Assert.Contains(reports, r => r.Kind == ReportKind.Impossible);
        Assert.Equal("A", unit.LocationZoneId);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
        var events = state.Clock.AdvanceTo(360).ToList();
        Assert.Contains(events, e => e is QuerySent query && query.TargetId == "HQ");
        var dueKey = Assert.Single(state.Clock.PendingKeys, key => key.Phase == EventPhase.ReportCreate);
        Assert.InRange(dueKey.Minute, 390, 420);
    }

    [Theory]
    [InlineData(3, 0, 0, false, false)]
    [InlineData(0, 3, 0, true, false)]
    [InlineData(0, 0, 3, false, true)]
    public void UnreachableOrder_ReactionFollowsTraits(int initiative, int caution, int literalism, bool expectQuery, bool expectStop)
    {
        var state = State(
            Zone("A", TerrainType.Open, ("B", 30)),
            Zone("B", TerrainType.Open, ("A", 30), ("C", 30)),
            Zone("C", TerrainType.Bridge, ("B", 30)));
        state.Zones["B"].SetEdgeType("C", EdgeType.Crossing);
        state.Zones["C"].SetEdgeType("B", EdgeType.Crossing);
        state.Zones["B"].BridgeIntact = false;
        var unit = NewUnit("U1", "A", new CommanderTraits(initiative, caution, literalism, 1));
        state.Units[unit.Id] = unit;
        var order = NewOrder(target: "C");
        state.Orders.Add(order);

        var reports = new ExecutionResolver()
            .Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(5))
            .ToList();

        var events = state.Clock.AdvanceTo(360).ToList();
        Assert.Equal(expectQuery, events.Any(e => e is QuerySent));
        if (expectStop)
        {
            Assert.Contains(reports, r => r.Kind == ReportKind.Impossible);
            Assert.Equal("A", unit.LocationZoneId);
        }
        else if (expectQuery)
        {
            Assert.Contains(reports, r => r.Kind == ReportKind.Impossible);
        }
        else
        {
            Assert.DoesNotContain(reports, r => r.Kind == ReportKind.Impossible);
            var move = events.OfType<UnitMoved>().Single();
            Assert.Equal("B", move.ToZoneId);
        }
    }

    [Fact]
    public void OnMove_AppliesEdgeMovementModeAndAttentionDeltas()
    {
        var state = State(
            Zone("A", TerrainType.Forest, ("B", 30)),
            Zone("B", TerrainType.Forest, ("A", 30)));
        state.Zones["A"].SetEdgeType("B", EdgeType.Offroad);
        state.Zones["B"].SetEdgeType("A", EdgeType.Offroad);
        var unit = NewUnit("U1", "A", new CommanderTraits(1, 1, 1, 1));
        unit.AttentionEdgeId = "A-other";
        state.Units[unit.Id] = unit;
        var order = NewOrder(target: "B");
        state.Orders.Add(order);
        var resolver = new ExecutionResolver();

        resolver.Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(1)).ToList();
        var move = state.Clock.AdvanceTo(360).OfType<UnitMoved>().Single();
        resolver.OnMove(move, state, new FrictionTable(), new Random(1)).ToList();

        Assert.Equal(67, unit.Readiness);
        Assert.Equal(7, unit.Fatigue);
        Assert.Equal(PathPlanner.EdgeId("A", "B"), unit.MoveEdgeId);
        Assert.Equal(PathPlanner.EdgeId("A", "B"), unit.AttentionEdgeId);
    }

    [Fact]
    public void OnMove_EdgeDestroyedAfterPlanning_DoesNotTeleportAndQueries()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(target: "C");
        state.Orders.Add(order);
        var resolver = new ExecutionResolver();
        resolver.Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(9)).ToList();
        state.Zones["A"].TravelMinutes.Remove("B");
        state.Zones["B"].TravelMinutes.Remove("A");

        var move = state.Clock.AdvanceTo(360).OfType<UnitMoved>().Single();
        var reports = resolver.OnMove(move, state, new FrictionTable(), new Random(9)).ToList();

        Assert.Contains(reports, r => r.Kind == ReportKind.Impossible);
        Assert.Equal("A", unit.LocationZoneId);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
    }

    [Fact]
    public void Reports_UseInjectedContractsAndTrackDelivery()
    {
        var state = ChainState(out var unit);
        var factory = new CountingReportFactory();
        var rules = new LowConfidenceRules();
        var order = NewOrder(target: "B");
        state.Orders.Add(order);

        var reports = new ExecutionResolver(rules, factory)
            .Start(new OrderAccepted(360, order.Id), state, new FrictionTable(), new Random(2))
            .ToList();

        Assert.Equal(1, factory.Calls);
        Assert.Single(reports);
        Assert.Equal(1, reports[0].Confidence);
        Assert.True(reports[0].HasRequiredFields);
        Assert.Contains(reports[0], state.InTransitReports);
        Assert.True(reports[0].ArrivesAt > reports[0].Minute);
        Assert.Contains(state.Clock.PendingKeys,
            key => key.Minute == reports[0].ArrivesAt && key.Phase == EventPhase.ReportDeliver);
    }

    private static GameState State(params Zone[] zones) => new()
    {
        ScenarioId = "execution",
        Clock = new Clock(360),
        Zones = zones.ToDictionary(z => z.Id),
        Units = [],
        Knowledge = new PlayerKnowledge()
    };

    private static Zone Zone(string id, TerrainType terrain, params (string NeighborId, int Minutes)[] links)
    {
        var zone = new Zone { Id = id, Name = id, Terrain = terrain };
        foreach (var (neighborId, minutes) in links)
            zone.TravelMinutes[neighborId] = minutes;
        return zone;
    }

    private static GameState ChainState(out Unit unit, CommanderTraits? traits = null)
    {
        var state = State(
            Zone("A", TerrainType.Open, ("B", 30)),
            Zone("B", TerrainType.Open, ("A", 30), ("C", 20)),
            Zone("C", TerrainType.Village, ("B", 20)));
        unit = NewUnit("U1", "A", traits ?? new CommanderTraits(1, 1, 1, 2));
        state.Units[unit.Id] = unit;
        state.Zones["A"].PresentUnitIds.Add(unit.Id);
        return state;
    }

    private static Unit NewUnit(string id, string zoneId, CommanderTraits traits, Faction faction = Faction.Blue) => new()
    {
        Id = id,
        Name = id,
        Strength = 100,
        InitialStrengthSnapshot = 100,
        Readiness = 80,
        Morale = 80,
        Fatigue = 0,
        Ammo = 100,
        LocationZoneId = zoneId,
        Traits = traits,
        Faction = faction
    };

    private static Order NewOrder(string target, MovementMode mode = MovementMode.March, int? deadline = null) => new()
    {
        ToUnitId = "U1",
        Intent = OrderIntent.Move,
        TargetZoneId = target,
        MovementMode = mode,
        NotLaterThan = deadline,
        IssuedAt = 360
    };

    private sealed class CountingReportFactory : IReportFactory
    {
        public int Calls { get; private set; }

        public Report Create(
            int eventMinute,
            int deliveryMinute,
            string sourceId,
            int confidence,
            ReportKind kind = ReportKind.Status,
            string text = "",
            string? factKey = null,
            Faction sourceFaction = Faction.Blue,
            string? reportedBy = null,
            Dictionary<string, string>? facts = null)
        {
            Calls++;
            return Report.Create(eventMinute, deliveryMinute, sourceId, confidence, kind, text, factKey,
                sourceFaction, reportedBy, facts);
        }
    }

    private sealed class LowConfidenceRules : IGameRules
    {
        public int MaxConfidence => 1;

        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }
}
