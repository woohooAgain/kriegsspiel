using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class ExecutionResolverTests
{
    [Fact]
    public void PathPlanner_NoDeadline_UsesOffroadWeightButKeepsTravelTime()
    {
        var state = WeightedState();

        Assert.True(PathPlanner.TryPlan(state, "A", "B", deadline: null, forbiddenZoneId: null, out var route));

        Assert.Equal(["B"], route.Steps.Select(step => step.ToZoneId));
        Assert.Equal(10, route.TotalMinutes);
        Assert.Equal(10, route.Steps[0].Minutes);
        Assert.Equal(EdgeType.Offroad, route.Steps[0].EdgeType);
    }

    [Fact]
    public void PathPlanner_WithDeadline_ChoosesByTravelTimeEvenIfActualTimeShorter()
    {
        var state = WeightedState();

        Assert.True(PathPlanner.TryPlan(state, "A", "B", deadline: 500, forbiddenZoneId: null, out var route));

        Assert.Equal(["C", "B"], route.Steps.Select(step => step.ToZoneId));
        Assert.Equal(9, route.TotalMinutes);
        Assert.Equal([8, 1], route.Steps.Select(step => step.Minutes));
    }

    [Fact]
    public void PathPlanner_ForbiddenZone_ExcludesNodesAndTarget()
    {
        var state = ChainState(out _);

        Assert.True(PathPlanner.TryPlan(state, "A", "C", deadline: null, forbiddenZoneId: null, out var open));
        Assert.Equal(["B", "C"], open.Steps.Select(step => step.ToZoneId));

        Assert.False(PathPlanner.TryPlan(state, "A", "C", deadline: null, forbiddenZoneId: "B", out _));
        Assert.False(PathPlanner.TryPlan(state, "A", "B", deadline: null, forbiddenZoneId: "B", out _));
    }

    [Fact]
    public void PathPlanner_ForbiddenStart_AllowsExitWithoutReturn()
    {
        var state = ChainState(out _);

        Assert.True(PathPlanner.TryPlan(state, "A", "C", deadline: null, forbiddenZoneId: "A", out var route));

        Assert.Equal(["B", "C"], route.Steps.Select(step => step.ToZoneId));
        Assert.DoesNotContain(route.Steps, step => step.FromZoneId == "A" && step.ToZoneId == "A");
        Assert.Single(route.Steps, step => step.FromZoneId == "A");
    }

    [Fact]
    public void PathPlanner_CourierLegs_UseCourierTimeWithMinimumFive()
    {
        var state = State(
            Zone("A", ("B", 5)),
            Zone("B", ("A", 5), ("C", 5)),
            Zone("C", ("B", 5)));
        state.Zones["A"].CourierMinutes["B"] = 3;
        state.Zones["B"].CourierMinutes["A"] = 3;
        state.Zones["B"].CourierMinutes["C"] = 3;
        state.Zones["C"].CourierMinutes["B"] = 3;

        Assert.Equal(5, PathPlanner.CourierLegMinutes(state, "A", "B"));
        Assert.Equal(6, PathPlanner.CourierLegMinutes(state, "A", "C"));
        Assert.Equal(6, PathPlanner.CourierLegBackMinutes(state, "C", "A"));
    }

    [Fact]
    public void Start_EntersEdgeWithoutTeleport()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(unit.Id, "C");
        var resolver = new ExecutionResolver();

        Start(state, resolver, order);

        Assert.True(unit.IsOnEdge);
        Assert.Equal("A", unit.EdgeFromZoneId);
        Assert.Equal("B", unit.EdgeToZoneId);
        Assert.Equal(30, unit.RemainingMinutes);
        Assert.Equal("A", unit.LocationZoneId);
        Assert.Equal(Posture.March, unit.Posture);
        Assert.Equal(order.Id, unit.CurrentOrderId);
        Assert.DoesNotContain(unit.Id, state.Zones["A"].PresentUnitIds);
        Assert.DoesNotContain(unit.Id, state.Zones["B"].PresentUnitIds);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute == 360);
        Assert.Contains(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute == 361);
    }

    [Fact]
    public void OnMove_DecrementsRemainingEachMinuteAndTransitionsImmediately()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(unit.Id, "C");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        Advance(state, resolver, 389);

        Assert.True(unit.IsOnEdge);
        Assert.Equal("A", unit.EdgeFromZoneId);
        Assert.Equal("B", unit.EdgeToZoneId);
        Assert.Equal(1, unit.RemainingMinutes);
        Assert.Equal("A", unit.LocationZoneId);

        Advance(state, resolver, 390);

        Assert.True(unit.IsOnEdge);
        Assert.Equal("B", unit.EdgeFromZoneId);
        Assert.Equal("C", unit.EdgeToZoneId);
        Assert.Equal(20, unit.RemainingMinutes);
        Assert.Equal("B", unit.LocationZoneId);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute == 390);
        Assert.Contains(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute == 391);
    }

    [Fact]
    public void OnMove_OffroadEdge_AppliesReadinessPenaltyOnCompletion()
    {
        var state = State(
            Zone("A", ("B", 2)),
            Zone("B", ("A", 2)));
        state.Zones["A"].SetEdgeType("B", EdgeType.Offroad);
        state.Zones["B"].SetEdgeType("A", EdgeType.Offroad);
        var unit = NewUnit("U1", "A");
        state.Units[unit.Id] = unit;
        state.Zones["A"].PresentUnitIds.Add(unit.Id);
        var order = NewOrder(unit.Id, "B");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        Advance(state, resolver, 361);
        Assert.Equal(80, unit.Readiness);
        Assert.Equal(1, unit.RemainingMinutes);

        var reports = Advance(state, resolver, 362);

        Assert.False(unit.IsOnEdge);
        Assert.Equal("B", unit.LocationZoneId);
        Assert.Equal(75, unit.Readiness);
        Assert.Contains(reports, report => report.Kind == ReportKind.Arrival);
    }

    [Fact]
    public void OnMove_ReachingFreeTarget_SetsDefenseAndHoldIntent()
    {
        var state = ChainState(out var unit, ab: 2, bc: 5);
        var order = NewOrder(unit.Id, "B");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        Advance(state, resolver, 362);

        Assert.False(unit.IsOnEdge);
        Assert.Equal("B", unit.LocationZoneId);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.NotNull(unit.EffectiveOrder);
        Assert.Equal(OrderIntent.Hold, unit.EffectiveOrder!.Intent);
        Assert.Null(unit.CurrentOrderId);
        Assert.Contains(unit.Id, state.Zones["B"].PresentUnitIds);
    }

    [Fact]
    public void OnMove_EnemyContact_StopsMovementAndSetsAttackPosture()
    {
        var state = ChainState(out var unit, ab: 2, bc: 5);
        var enemy = NewUnit("E1", "B", new CommanderTraits(1, 1, 1, 1), Faction.Red);
        state.Units[enemy.Id] = enemy;
        state.Zones["B"].PresentUnitIds.Add(enemy.Id);
        var order = NewOrder(unit.Id, "C");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        var reports = Advance(state, resolver, 362);

        Assert.Equal("B", unit.LocationZoneId);
        Assert.Equal(Posture.Attack, unit.Posture);
        Assert.False(unit.IsOnEdge);
        Assert.Null(unit.PendingOrder);
        Assert.Contains(reports, report => report.Kind == ReportKind.Contact && report.FactKey == "contact:B");
        var contactReport = reports.Single(report => report.Kind == ReportKind.Contact);
        Assert.Equal(112, contactReport.ReportedOwnStrength);
        Assert.Equal(88, contactReport.ReportedEnemyStrength);
        Assert.Equal("B", contactReport.ObservedZoneId);
        Assert.Equal(2, contactReport.Confidence);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
        Assert.Contains(state.Clock.PendingKeys, key => key.Phase == EventPhase.Combat && key.Minute == 390);
    }

    [Fact]
    public void OnArrive_ReportCarriesHonestyDistortedOwnStrengthAndZeroEnemy()
    {
        var state = ChainState(out var unit, ab: 2);
        var order = NewOrder(unit.Id, "B");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        Advance(state, resolver, 361);
        var reports = Advance(state, resolver, 362);

        var arrival = reports.Single(report => report.Kind == ReportKind.Arrival);
        Assert.Equal(112, arrival.ReportedOwnStrength);
        Assert.Equal(0, arrival.ReportedEnemyStrength);
        Assert.Equal(3, arrival.Confidence);
    }

    [Fact]
    public void ImpossibleReport_CarriesHonestyDistortedOwnStrengthAndZeroEnemy()
    {
        var traits = new CommanderTraits(Initiative: 0, Caution: 3, Literalism: 2, ReportHonesty: 1);
        var state = ChainState(out var unit, traits, ab: 3, bc: 3);
        var order = NewOrder(unit.Id, "C", deadline: 361);
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        var reports = Advance(state, resolver, 363);

        var impossible = reports.Single(report => report.Kind == ReportKind.Impossible);
        Assert.Equal(124, impossible.ReportedOwnStrength);
        Assert.Equal(0, impossible.ReportedEnemyStrength);
        Assert.Equal(3, impossible.Confidence);
    }

    [Fact]
    public void StatusReport_CarriesOwnStrengthAndZeroEnemy()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(unit.Id, target: null, intent: OrderIntent.Hold);
        var resolver = new ExecutionResolver();

        var reports = Start(state, resolver, order);

        var status = Assert.Single(reports);
        Assert.Equal(ReportKind.Status, status.Kind);
        Assert.Equal(112, status.ReportedOwnStrength);
        Assert.Equal(0, status.ReportedEnemyStrength);
        Assert.Equal(2, status.Confidence);
    }

    [Fact]
    public void OnMove_EnemyContact_ReportSumsEnemyStrengthInObservedZone()
    {
        var traits = new CommanderTraits(Initiative: 1, Caution: 1, Literalism: 1, ReportHonesty: 3);
        var state = ChainState(out var unit, traits, ab: 2, bc: 5);
        var first = NewUnit("E1", "B", new CommanderTraits(1, 1, 1, 1), Faction.Red);
        var second = NewUnit("E2", "B", new CommanderTraits(1, 1, 1, 1), Faction.Red);
        state.Units[first.Id] = first;
        state.Units[second.Id] = second;
        state.Zones["B"].PresentUnitIds.Add(first.Id);
        state.Zones["B"].PresentUnitIds.Add(second.Id);
        var order = NewOrder(unit.Id, "C");
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        var reports = Advance(state, resolver, 362);

        var contact = reports.Single(report => report.Kind == ReportKind.Contact);
        Assert.Equal(100, contact.ReportedOwnStrength);
        Assert.Equal(200, contact.ReportedEnemyStrength);
        Assert.Equal("B", contact.ObservedZoneId);
        Assert.Equal(2, contact.Confidence);
    }

    [Fact]
    public void Start_PendingOrder_OverwritesAndAppliesOnEdgeCompletion()
    {
        var state = State(
            Zone("A", ("B", 10)),
            Zone("B", ("A", 10), ("C", 10), ("D", 10)),
            Zone("C", ("B", 10)),
            Zone("D", ("B", 10)));
        var unit = NewUnit("U1", "A");
        state.Units[unit.Id] = unit;
        state.Zones["A"].PresentUnitIds.Add(unit.Id);
        var first = NewOrder(unit.Id, "C");
        var second = NewOrder(unit.Id, "D");
        var third = NewOrder(unit.Id, "C");
        var resolver = new ExecutionResolver();
        Start(state, resolver, first);

        Advance(state, resolver, 365);
        Start(state, resolver, second);
        Assert.Equal(second.Id, unit.PendingOrder!.Id);

        Advance(state, resolver, 366);
        Start(state, resolver, third);
        Assert.Equal(third.Id, unit.PendingOrder!.Id);

        Advance(state, resolver, 370);

        Assert.Null(unit.PendingOrder);
        Assert.NotNull(unit.EffectiveOrder);
        Assert.Equal(third.Id, unit.EffectiveOrder!.SourceOrderId);
        Assert.True(unit.IsOnEdge);
        Assert.Equal("B", unit.EdgeFromZoneId);
        Assert.Equal("C", unit.EdgeToZoneId);
        Assert.Equal(10, unit.RemainingMinutes);
        Assert.Contains(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement && key.Minute == 371);
    }

    [Fact]
    public void OnMove_DeadlineExpired_PedanticCommanderAborts()
    {
        var traits = new CommanderTraits(Initiative: 0, Caution: 3, Literalism: 2, ReportHonesty: 1);
        var state = ChainState(out var unit, traits, ab: 3, bc: 3);
        var order = NewOrder(unit.Id, "C", deadline: 361);
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        var reports = Advance(state, resolver, 363);

        Assert.Equal("B", unit.LocationZoneId);
        Assert.False(unit.IsOnEdge);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.Null(unit.EffectiveOrder);
        Assert.Null(unit.CurrentOrderId);
        Assert.Contains(reports, report => report.Kind == ReportKind.Impossible);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
    }

    [Fact]
    public void Start_HoldWithoutTarget_StaysStationary()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(unit.Id, target: null, intent: OrderIntent.Hold);
        var resolver = new ExecutionResolver();

        var reports = Start(state, resolver, order);

        Assert.Equal("A", unit.LocationZoneId);
        Assert.False(unit.IsOnEdge);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.Equal(OrderIntent.Hold, unit.EffectiveOrder!.Intent);
        Assert.Null(unit.CurrentOrderId);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
        Assert.Single(reports);
    }

    [Fact]
    public void Start_Recon_StaysStationary()
    {
        var state = ChainState(out var unit);
        var order = NewOrder(unit.Id, "C", intent: OrderIntent.Recon);
        var resolver = new ExecutionResolver();

        var reports = Start(state, resolver, order);

        Assert.Equal("A", unit.LocationZoneId);
        Assert.False(unit.IsOnEdge);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.Equal(OrderIntent.Recon, unit.EffectiveOrder!.Intent);
        Assert.DoesNotContain(state.Clock.PendingKeys, key => key.Phase == EventPhase.Movement);
        Assert.Single(reports);
    }

    [Fact]
    public void Start_HoldWithTarget_MovesThenHolds()
    {
        var state = ChainState(out var unit, ab: 2, bc: 5);
        var order = NewOrder(unit.Id, "B", intent: OrderIntent.Hold);
        var resolver = new ExecutionResolver();
        Start(state, resolver, order);

        Advance(state, resolver, 362);

        Assert.False(unit.IsOnEdge);
        Assert.Equal("B", unit.LocationZoneId);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.Equal(OrderIntent.Hold, unit.EffectiveOrder!.Intent);
        Assert.Null(unit.CurrentOrderId);
    }

    private static List<Report> Start(GameState state, ExecutionResolver resolver, Order order, int minute = 360)
    {
        state.Orders.Add(order);
        return resolver.Start(new OrderAccepted(minute, order.Id), state, new FrictionTable(), new Random(1)).ToList();
    }

    private static List<Report> Advance(GameState state, ExecutionResolver resolver, int target)
    {
        var friction = new FrictionTable();
        var rng = new Random(1);
        var reports = new List<Report>();
        foreach (var gameEvent in state.Clock.AdvanceTo(target))
        {
            switch (gameEvent)
            {
                case UnitMoved move:
                    reports.AddRange(resolver.OnMove(move, state, friction, rng));
                    break;
                case UnitArrived arrive:
                    reports.AddRange(resolver.OnArrive(arrive, state, friction, rng));
                    break;
            }
        }
        return reports;
    }

    private static GameState WeightedState()
    {
        var state = State(
            Zone("A", ("B", 10), ("C", 8)),
            Zone("B", ("A", 10), ("C", 1)),
            Zone("C", ("A", 8), ("B", 1)));
        state.Zones["A"].SetEdgeType("B", EdgeType.Offroad);
        state.Zones["B"].SetEdgeType("A", EdgeType.Offroad);
        return state;
    }

    private static GameState ChainState(out Unit unit, CommanderTraits? traits = null, int ab = 30, int bc = 20)
    {
        var state = State(
            Zone("A", ("B", ab)),
            Zone("B", ("A", ab), ("C", bc)),
            Zone("C", ("B", bc)));
        unit = NewUnit("U1", "A", traits);
        state.Units[unit.Id] = unit;
        state.Zones["A"].PresentUnitIds.Add(unit.Id);
        return state;
    }

    private static GameState State(params Zone[] zones) => new()
    {
        ScenarioId = "execution",
        Clock = new Clock(360),
        Zones = zones.ToDictionary(zone => zone.Id),
        Units = new Dictionary<string, Unit>(),
        Knowledge = new PlayerKnowledge()
    };

    private static Zone Zone(string id, params (string NeighborId, int Minutes)[] links)
    {
        var zone = new Zone { Id = id, Name = id, Terrain = TerrainType.Open };
        foreach (var (neighborId, minutes) in links)
            zone.TravelMinutes[neighborId] = minutes;
        return zone;
    }

    private static Unit NewUnit(string id, string zoneId, CommanderTraits? traits = null, Faction faction = Faction.Blue) => new()
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
        Traits = traits ?? new CommanderTraits(1, 1, 1, 2),
        Faction = faction
    };

    private static Order NewOrder(
        string unitId,
        string? target,
        OrderIntent intent = OrderIntent.Move,
        int? deadline = null,
        string? forbiddenZoneId = null) => new()
    {
        ToUnitId = unitId,
        Intent = intent,
        TargetZoneId = target,
        Deadline = deadline,
        ForbiddenZoneId = forbiddenZoneId,
        IssuedAt = 360,
        CreatedMinute = 360
    };
}
