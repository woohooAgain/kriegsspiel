using System.Text.Json;
using System.Text.Json.Nodes;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Kriegsspiel.Core.Persistence;

namespace Kriegsspiel.Core.Tests;

public class PersistenceTests
{
    [Fact]
    public void RoundTrip_PreservesClockRngAndPolymorphicEvents()
    {
        var state = CreateState();
        var json = GameStatePersistence.Serialize(state);

        var loaded = GameStatePersistence.Deserialize(json);

        Assert.Equal(state.Clock.Now, loaded.Clock.Now);
        Assert.Equal(state.Clock.PendingCount, loaded.Clock.PendingCount);
        Assert.Equal(state.Clock.PendingKeys, loaded.Clock.PendingKeys);
        Assert.Equal(state.Seed, loaded.Seed);
        Assert.Equal(state.RngState, loaded.RngState);
        Assert.Equal(state.RngCallCount, loaded.RngCallCount);
        Assert.Equal(state.Turn, loaded.Turn);
        Assert.Equal(state.EndMinute, loaded.EndMinute);
        Assert.Equal(
            state.EventLog.Select(e => e.GetType().Name),
            loaded.EventLog.Select(e => e.GetType().Name));
        Assert.Equal(
            state.PendingEvents.Select(e => e.GetType().Name),
            loaded.PendingEvents.Select(e => e.GetType().Name));

        var answered = Assert.IsType<QueryAnswered>(loaded.EventLog[1]);
        Assert.Equal("U1", answered.SourceId);
        Assert.Equal("R3", answered.Report.Id);
        Assert.Equal(ReportKind.Contact, answered.Report.Kind);
        var reportDelivered = Assert.IsType<ReportDelivered>(loaded.EventLog[3]);
        Assert.Equal("R1", reportDelivered.ReportId);
        Assert.Equal("R1", reportDelivered.Payload?.Id);
        Assert.Equal("zone:B", reportDelivered.Payload?.FactKey);
        Assert.IsType<ZoneControlChanged>(loaded.EventLog[2]);
        Assert.IsType<CourierLost>(loaded.EventLog[4]);
    }

    [Fact]
    public void RoundTrip_PreservesReportsCouriersCasualtiesKnowledgeAndOrders()
    {
        var state = CreateState();
        var loaded = GameStatePersistence.Deserialize(GameStatePersistence.Serialize(state));

        AssertReports(state.InTransitReports, loaded.InTransitReports);
        AssertReports(state.DeliveredReports, loaded.DeliveredReports);
        AssertReports(state.LostReports, loaded.LostReports);

        Assert.Equal(state.Couriers.Count, loaded.Couriers.Count);
        for (int i = 0; i < state.Couriers.Count; i++)
        {
            var source = state.Couriers[i];
            var result = loaded.Couriers[i];
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.State, result.State);
            Assert.Equal(source.AssignedOrderId, result.AssignedOrderId);
            Assert.Equal(source.AssignedQueryId, result.AssignedQueryId);
            Assert.Equal(source.OutboundDepartureMinute, result.OutboundDepartureMinute);
            Assert.Equal(source.OutboundDeliveryTime, result.OutboundDeliveryTime);
            Assert.Equal(source.ReturnTime, result.ReturnTime);
            Assert.Equal(source.ExpectedDeliveryMinute, result.ExpectedDeliveryMinute);
            Assert.Equal(source.ExpectedReturnMinute, result.ExpectedReturnMinute);
            Assert.Equal(source.ReplacementMinute, result.ReplacementMinute);
            Assert.Equal(source.DeliveryConfirmed, result.DeliveryConfirmed);
            Assert.Equal(source.LostOnOutbound, result.LostOnOutbound);
            Assert.Equal(source.LostOnReturn, result.LostOnReturn);
        }

        Assert.Equal(state.PendingCasualties.Count, loaded.PendingCasualties.Count);
        for (int i = 0; i < state.PendingCasualties.Count; i++)
        {
            var source = state.PendingCasualties[i];
            var result = loaded.PendingCasualties[i];
            Assert.Equal(source.Minute, result.Minute);
            Assert.Equal(source.UnitId, result.UnitId);
            Assert.Equal(source.ZoneId, result.ZoneId);
            Assert.Equal(source.CombatTickMinute, result.CombatTickMinute);
            Assert.Equal(source.Applied, result.Applied);
            Assert.Equal(source.Amount, result.Amount);
        }

        Assert.True(loaded.Knowledge.Zones["B"].Conflicting);
        Assert.Equal(state.Knowledge.Zones["B"].Versions.Count, loaded.Knowledge.Zones["B"].Versions.Count);
        Assert.Equal(
            state.Knowledge.Zones["B"].Versions.Select(v => (v.Value, v.Confidence, v.SourceId, v.DeliveryMinute)),
            loaded.Knowledge.Zones["B"].Versions.Select(v => (v.Value, v.Confidence, v.SourceId, v.DeliveryMinute)));
        Assert.Equal(state.Knowledge.Zones["B"].Confidence, loaded.Knowledge.Zones["B"].Confidence);
        Assert.Equal(state.Knowledge.Zones["B"].DeliveryMinute, loaded.Knowledge.Zones["B"].DeliveryMinute);
        Assert.Equal(state.Knowledge.LastUpdatedMinute, loaded.Knowledge.LastUpdatedMinute);
        Assert.Equal(state.Knowledge.Facts["zone:B"].Count, loaded.Knowledge.Facts["zone:B"].Count);

        var order = Assert.Single(loaded.Orders);
        Assert.Equal("O1", order.Id);
        Assert.Equal(3, order.OrderVersion);
        Assert.Equal(375, order.DeliveredMinute);
        Assert.Equal(0.25, order.AmbiguityScore, 3);
        Assert.Equal(MovementMode.Covert, order.MovementMode);
        Assert.Equal([OrderConstraint.NoZoneEntry], order.Constraints);
        var branch = Assert.Single(order.Branches);
        Assert.Equal(BranchCondition.BattleEnRoute, branch.Condition);
        Assert.Equal(BranchAction.Bypass, branch.Then);
        Assert.Equal(BranchAction.Hold, branch.Else);

        var effectiveOrder = loaded.EffectiveOrders["O1"];
        Assert.Equal(3, effectiveOrder.OrderVersion);
        Assert.Equal(375, effectiveOrder.EffectiveAtMinute);
        Assert.Equal(order.Id, effectiveOrder.SourceOrderId);
        Assert.Same(effectiveOrder, loaded.Units["U1"].EffectiveOrder);
        Assert.Equal("custom-ref", loaded.Units["U1"].EffectiveOrderId);

        Assert.Equal(state.Zones["A"].TravelMinutes, loaded.Zones["A"].TravelMinutes);
        Assert.Equal(state.Zones["B"].EdgeTypes, loaded.Zones["B"].EdgeTypes);
        Assert.Single(loaded.Zones["B"].Objects);
        Assert.Single(loaded.Zones["B"].Cargo);
        Assert.Single(loaded.Zones["B"].Civilians);
        Assert.Equal(2, loaded.Zones["B"].Civilians[0].Panic);
        Assert.Equal(Faction.Red, loaded.Zones["B"].Civilians[0].Loyalty);
        Assert.Equal(state.Units["U1"].Traits, loaded.Units["U1"].Traits);
        Assert.Equal(3, loaded.Units["U1"].Experience);
    }

    [Fact]
    public void Deserialize_RejectsUnsupportedSchemaVersion()
    {
        var json = GameStatePersistence.Serialize(CreateState());
        var node = JsonNode.Parse(json)!;
        node["schemaVersion"] = PersistenceSchema.CurrentVersion + 1;

        Assert.Throws<NotSupportedException>(() => GameStatePersistence.Deserialize(node.ToJsonString()));
    }

    [Fact]
    public void ApplyTo_RestoresExistingState()
    {
        var state = CreateState();
        var json = GameStatePersistence.Serialize(state);
        var target = new GameState
        {
            ScenarioId = "old",
            Clock = new Clock(0),
            Zones = new(),
            Units = new(),
            Knowledge = new PlayerKnowledge()
        };

        GameStatePersistence.ApplyTo(json, target);

        Assert.Equal(state.ScenarioId, target.ScenarioId);
        Assert.Equal(state.Clock.Now, target.Clock.Now);
        Assert.Equal(state.Clock.PendingKeys, target.Clock.PendingKeys);
        Assert.Equal(state.RngState, target.RngState);
        Assert.Equal(state.EffectiveOrders.Count, target.EffectiveOrders.Count);
        Assert.Equal(state.InTransitReports.Count, target.InTransitReports.Count);
        Assert.Equal(state.Knowledge.Zones["B"].Versions.Count, target.Knowledge.Zones["B"].Versions.Count);
    }

    private static void AssertReports(List<Report> expected, List<Report> actual)
    {
        Assert.Equal(expected.Count, actual.Count);
        for (int i = 0; i < expected.Count; i++)
        {
            var source = expected[i];
            var result = actual[i];
            Assert.Equal(source.Id, result.Id);
            Assert.Equal(source.Minute, result.Minute);
            Assert.Equal(source.ArrivesAt, result.ArrivesAt);
            Assert.Equal(source.FromUnitId, result.FromUnitId);
            Assert.Equal(source.Text, result.Text);
            Assert.Equal(source.Confidence, result.Confidence);
            Assert.Equal(source.Kind, result.Kind);
            Assert.Equal(source.SourceFaction, result.SourceFaction);
            Assert.Equal(source.FactKey, result.FactKey);
            Assert.Equal(source.Facts, result.Facts);
            Assert.Equal(source.ReportedBy, result.ReportedBy);
        }
    }

    private static GameState CreateState()
    {
        var clock = new Clock(360);
        clock.Schedule(new OrderIssued(360, "O1"), new ScheduledAt(360, EventPhase.Delivery, 1));
        clock.Schedule(new CombatTick(375, "B"), new ScheduledAt(375, EventPhase.Combat, 2));
        clock.Schedule(new QuerySent(380, "U1", "U2"), new ScheduledAt(380, EventPhase.Delivery, 3));
        clock.AdvanceTo(365).ToList();
        clock.Schedule(new ReportDue(365, "U1", ReportKind.Status), 365, EventPhase.ReportCreate);

        var zoneA = new Zone
        {
            Id = "A",
            Name = "A",
            Terrain = TerrainType.Open,
            TravelMinutes = { ["B"] = 30 },
            EdgeTypes = { ["B"] = EdgeType.Road },
            DefenseBonus = 1,
            Observation = 2,
            PresentUnitIds = ["U1"]
        };
        var zoneB = new Zone
        {
            Id = "B",
            Name = "B",
            Terrain = TerrainType.Bridge,
            TravelMinutes = { ["A"] = 30 },
            EdgeTypes = { ["A"] = EdgeType.Crossing },
            BridgeIntact = false,
            FortificationLevel = 2,
            ControllingFaction = Faction.Red,
            Cover = 0.25,
            MoveMultiplier = 0.8,
            TemporaryDefenseBonus = 1,
            Cargo =
            [
                new Cargo
                {
                    Id = "C1",
                    Name = "ammo",
                    LocationZoneId = "B",
                    Owner = Faction.Red,
                    State = MovableObjectState.Moving,
                    EtaMinute = 420,
                    PrisonerStatus = PrisonerStatus.NotInterrogated,
                    Quantity = 12,
                    Type = CargoType.Ammo
                }
            ],
            Civilians =
            [
                new CivilianGroup
                {
                    Id = "CV1",
                    Name = "civilians",
                    LocationZoneId = "B",
                    Owner = Faction.Neutral,
                    Quantity = 30,
                    Panic = 2,
                    Loyalty = Faction.Red,
                    Casualties = 1,
                    EvacuationTargetZoneId = "A",
                    EvacuationDeadline = 450
                }
            ],
            Objects =
            [
                new ZoneObject
                {
                    Id = "O1",
                    Name = "bridge",
                    Kind = ZoneObjectKind.Bridge,
                    ZoneId = "B",
                    EdgeId = "A-B",
                    Integrity = 40,
                    State = ZoneObjectState.Damaged,
                    DefenseBonus = 1,
                    ObservationBonus = 1,
                    AmmoCapacity = 10,
                    ControlledBy = Faction.Red
                }
            ]
        };

        var unit = new Unit
        {
            Id = "U1",
            Name = "U1",
            Type = UnitType.Infantry,
            Strength = 40,
            Readiness = 70,
            Morale = 65,
            Fatigue = 3,
            Ammo = 80,
            LocationZoneId = "A",
            Posture = Posture.Defend,
            Traits = new CommanderTraits(2, 1, 3, 0, 2),
            CurrentOrderId = "O1",
            Faction = Faction.Blue,
            InitialStrengthSnapshot = 50,
            Training = 2,
            Experience = 3,
            CommandLossResilient = true,
            Concealed = true,
            AttentionEdgeId = "A-B",
            MoveEdgeId = "A-B",
            GuardOfObjectId = "O1",
            PoorControl = true
        };
        var enemy = new Unit
        {
            Id = "ENEMY_1",
            Name = "E1",
            Type = UnitType.Artillery,
            Strength = 25,
            Readiness = 90,
            Morale = 80,
            Ammo = 40,
            LocationZoneId = "B",
            Traits = new CommanderTraits(1, 2, 1, 1),
            Faction = Faction.Red,
            CommanderLost = true,
            Captured = false,
            NoPrisonersPolicy = true
        };

        var order = new Order
        {
            Id = "O1",
            ToUnitId = "U1",
            Intent = OrderIntent.Occupy,
            TargetZoneId = "B",
            NotLaterThan = 450,
            ReportBy = 465,
            IssuedAt = 360,
            AmbiguityScore = 0.25,
            OrderVersion = 3,
            MovementMode = MovementMode.Covert,
            OriginalText = "occupy B",
            Explanation = "bridge",
            IssuerId = "HQ",
            DeliveredMinute = 375,
            Constraints = [OrderConstraint.NoZoneEntry],
            VaguePatterns = ["很快"],
            Branches =
            [
                new OrderBranch(BranchCondition.BattleEnRoute, BranchAction.Bypass, BranchAction.Hold, "NoEngagement")
            ]
        };
        var effectiveOrder = new EffectiveOrder
        {
            Id = "E1",
            SourceOrderId = "O1",
            OrderVersion = 3,
            ToUnitId = "U1",
            Intent = OrderIntent.Occupy,
            TargetZoneId = "B",
            EffectiveAtMinute = 375,
            Constraints = [OrderConstraint.NoZoneEntry],
            Branches = [new OrderBranch(BranchCondition.BattleEnRoute, BranchAction.Bypass)],
            SelectedBranch = new OrderBranch(BranchCondition.BattleEnRoute, BranchAction.Bypass),
            AmbiguityScore = 0.25,
            Note = "accepted",
            OriginalText = "occupy B"
        };
        unit.EffectiveOrder = effectiveOrder;
        unit.EffectiveOrderId = "custom-ref";

        var transit = Report.Create(360, 400, "U1", 2, ReportKind.Status, "status", "zone:B", Faction.Blue, "U1",
            new Dictionary<string, string> { ["zoneId"] = "B", ["strength"] = "80" });
        transit.Id = "R1";
        var delivered = Report.Create(370, 375, "U1", 3, ReportKind.Contact, "contact", "zone:B", Faction.Blue);
        delivered.Id = "R3";
        var lost = Report.Create(365, 395, "ENEMY_1", 1, ReportKind.Silence, "silence", "zone:A", Faction.Red);
        lost.Id = "R2";

        var state = new GameState
        {
            ScenarioId = "SCN",
            Clock = clock,
            Zones = new Dictionary<string, Zone> { ["A"] = zoneA, ["B"] = zoneB },
            Units = new Dictionary<string, Unit> { ["U1"] = unit, ["ENEMY_1"] = enemy },
            Knowledge = new PlayerKnowledge(),
            Seed = 42,
            RngState = 987654321,
            RngCallCount = 17,
            Turn = 2,
            EndMinute = 480
        };
        state.Orders.Add(order);
        state.EffectiveOrders["O1"] = effectiveOrder;
        state.InTransitReports.Add(transit);
        state.DeliveredReports.Add(delivered);
        state.LostReports.Add(lost);
        state.Couriers.Add(new Courier
        {
            Id = "C1",
            State = CourierState.Delivering,
            AssignedOrderId = "O1",
            OutboundDepartureMinute = 360,
            OutboundDeliveryTime = 30,
            ReturnTime = 30,
            ExpectedDeliveryMinute = 390,
            ExpectedReturnMinute = 420,
            ReplacementMinute = 480
        });
        state.Couriers.Add(new Courier
        {
            Id = "C2",
            State = CourierState.Returning,
            AssignedQueryId = "Q1",
            OutboundDepartureMinute = 365,
            OutboundDeliveryTime = 15,
            ReturnTime = 20,
            ExpectedDeliveryMinute = 380,
            ExpectedReturnMinute = 400,
            DeliveryConfirmed = true,
            LostOnReturn = false
        });
        state.PendingCasualties.Add(new PendingCasualty(390, "U1", 7) { ZoneId = "B", CombatTickMinute = 390 });
        state.PendingCasualties.Add(new PendingCasualty(400, "ENEMY_1", 3) { ZoneId = "B", Applied = true });
        state.PendingEvents.Add(new PoorControlEntered(365, "ENEMY_1", "低控制"));
        state.Knowledge.Apply(new Report
        {
            Id = "K1",
            Minute = 360,
            ArrivesAt = 370,
            FromUnitId = "U1",
            Text = "Blue",
            Confidence = 2,
            FactKey = "zone:B",
            Facts = new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Blue" }
        });
        state.Knowledge.Apply(new Report
        {
            Id = "K2",
            Minute = 365,
            ArrivesAt = 375,
            FromUnitId = "ENEMY_1",
            Text = "Red",
            Confidence = 2,
            FactKey = "zone:B",
            Facts = new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Red" }
        });
        state.EventLog.Add(new OrderIssued(360, "O1"));
        state.EventLog.Add(new QueryAnswered(370, "U1", delivered));
        state.EventLog.Add(new ZoneControlChanged(375, "B", Faction.Blue));
        state.EventLog.Add(new ReportDelivered(380, "R1", "U1", transit));
        state.EventLog.Add(new CourierLost(385, "C1", "return"));
        return state;
    }
}
