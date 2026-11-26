using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class DomainInvariantsTests
{
    private static Unit NewUnit(
        int strength = 100,
        int snapshot = 100,
        int morale = 0,
        int readiness = 0,
        int ammo = 0) => new()
    {
        Id = "U1",
        Name = "U1",
        LocationZoneId = "A",
        Traits = new CommanderTraits(1, 1, 1, 1),
        Strength = strength,
        InitialStrengthSnapshot = snapshot,
        Morale = morale,
        Readiness = readiness,
        Ammo = ammo
    };

    [Theory]
    [InlineData(0)]
    [InlineData(29)]
    public void CombatEffective_FalseBelowThirtyPercentOfInitialStrength(int strength)
    {
        Assert.False(NewUnit(strength).IsCombatEffective);
    }

    [Fact]
    public void CombatEffective_TrueAtExactThirtyPercentAndIgnoresMoraleReadinessAmmo()
    {
        var unit = NewUnit(strength: 30, morale: 0, readiness: 0, ammo: 0);

        Assert.True(unit.IsCombatEffective);
    }

    [Fact]
    public void CombatEffective_ZeroSnapshotUsesCurrentStrengthAsInitial()
    {
        Assert.True(NewUnit(strength: 30, snapshot: 0).IsCombatEffective);
        Assert.True(NewUnit(strength: 29, snapshot: 0).IsCombatEffective);
    }

    [Fact]
    public void EdgeLocation_StoresRemainingMinutesAndEntryEdgeType()
    {
        var unit = NewUnit();
        unit.IsOnEdge = true;
        unit.EdgeFromZoneId = "A";
        unit.EdgeToZoneId = "B";
        unit.RemainingMinutes = 17;
        unit.EntryEdgeType = EdgeType.Offroad;

        Assert.True(unit.IsOnEdge);
        Assert.Equal("A", unit.EdgeFromZoneId);
        Assert.Equal("B", unit.EdgeToZoneId);
        Assert.Equal(17, unit.RemainingMinutes);
        Assert.Equal(EdgeType.Offroad, unit.EntryEdgeType);
    }

    [Fact]
    public void Unit_HasSinglePendingOrderBufferThatIsOverwritten()
    {
        var unit = NewUnit();
        var first = new Order { ToUnitId = unit.Id, Intent = OrderIntent.Move, TargetZoneId = "B" };
        var second = new Order { ToUnitId = unit.Id, Intent = OrderIntent.Hold, TargetZoneId = "A" };

        unit.PendingOrder = first;
        Assert.Same(first, unit.PendingOrder);

        unit.PendingOrder = second;
        Assert.Same(second, unit.PendingOrder);
    }

    [Fact]
    public void Unit_TracksLastMovedAndLastFoughtMinutes()
    {
        var unit = NewUnit();
        Assert.Null(unit.LastMovedMinute);
        Assert.Null(unit.LastFoughtMinute);

        unit.LastMovedMinute = 120;
        unit.LastFoughtMinute = 150;

        Assert.Equal(120, unit.LastMovedMinute);
        Assert.Equal(150, unit.LastFoughtMinute);
    }

    [Theory]
    [InlineData(OrderIntent.Move)]
    [InlineData(OrderIntent.Hold)]
    [InlineData(OrderIntent.Recon)]
    public void Order_SupportsV04IntentsTargetDeadlineForbiddenAndOutcome(OrderIntent intent)
    {
        var order = new Order
        {
            ToUnitId = "U1",
            Intent = intent,
            TargetZoneId = "B",
            Deadline = 240,
            ForbiddenZoneId = "C",
            Outcome = "Executed",
            CreatedMinute = 100,
            SentMinute = 105,
            DeliverAt = 125
        };

        Assert.Equal(intent, order.Intent);
        Assert.Equal("B", order.TargetZoneId);
        Assert.Equal(240, order.Deadline);
        Assert.Equal(240, order.NotLaterThan);
        Assert.Equal("C", order.ForbiddenZoneId);
        Assert.Equal("Executed", order.Outcome);
        Assert.Equal(100, order.CreatedMinute);
        Assert.Equal(105, order.SentMinute);
        Assert.Equal(125, order.DeliverAt);

        var effective = EffectiveOrder.From(order);

        Assert.Equal(order.Id, effective.SourceOrderId);
        Assert.Equal(intent, effective.Intent);
        Assert.Equal("B", effective.TargetZoneId);
        Assert.Equal(240, effective.Deadline);
        Assert.Equal("C", effective.ForbiddenZoneId);
        Assert.Equal("Executed", effective.Outcome);
        Assert.Equal(100, effective.CreatedMinute);
        Assert.Equal(105, effective.SentMinute);
        Assert.Equal(125, effective.DeliverAt);
    }

    [Fact]
    public void EffectiveOrder_CanBeDistortedWithOutcomeFailure()
    {
        var order = new Order { ToUnitId = "U1", Intent = OrderIntent.Move, TargetZoneId = "B" };
        var effective = EffectiveOrder.From(order);
        effective.Intent = OrderIntent.Hold;
        effective.Outcome = "FailedRejected";

        Assert.Equal(OrderIntent.Hold, effective.Intent);
        Assert.Equal("FailedRejected", effective.Outcome);
        Assert.Equal(OrderIntent.Move, order.Intent);
    }

    [Fact]
    public void Report_HasMandatoryFieldsAndConfidenceOneToThree()
    {
        var report = Report.Create(
            100, 120, "U1", 1,
            ReportKind.Contact, "contact", "zone:A", Faction.Red, "XO",
            observedZoneId: "A", reportedOwnStrength: 80, reportedEnemyStrength: 40);

        Assert.Equal(100, report.EventMinute);
        Assert.Equal(120, report.DeliveryMinute);
        Assert.Equal("U1", report.SourceId);
        Assert.Equal(1, report.Confidence);
        Assert.Equal("A", report.ObservedZoneId);
        Assert.Equal(80, report.ReportedOwnStrength);
        Assert.Equal(40, report.ReportedEnemyStrength);
        Assert.True(report.HasRequiredFields);
        Assert.Throws<ArgumentOutOfRangeException>(() => { report.Confidence = 4; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { report.Confidence = 0; });
    }

    [Fact]
    public void ReconReport_HasNullReportedOwnStrength()
    {
        var report = Report.Create(
            100, 110, "SCOUT", 3,
            ReportKind.Recon, "", "zone:B", Faction.Blue,
            observedZoneId: "B", reportedOwnStrength: null, reportedEnemyStrength: 0);

        Assert.Null(report.ReportedOwnStrength);
        Assert.Equal(0, report.ReportedEnemyStrength);
    }

    [Fact]
    public void Knowledge_DistinguishesNoDataFromEmpty()
    {
        var knowledge = new PlayerKnowledge();
        Assert.False(knowledge.HasZoneData("A"));

        var empty = Report.Create(
            100, 110, "SCOUT", 3,
            ReportKind.Recon, "", "zone:A",
            observedZoneId: "A",
            facts: new Dictionary<string, string> { ["value"] = KnowledgeClaim.EmptyValue });
        knowledge.Apply(empty);

        Assert.True(knowledge.HasZoneData("A"));
        Assert.True(knowledge.Zones["A"].IsEmpty);
        Assert.Equal(100, knowledge.Zones["A"].EventMinute!.Value);
        Assert.Equal(110, knowledge.Zones["A"].DeliveryMinute!.Value);
    }

    [Fact]
    public void Knowledge_KeepsEveryVersionWithoutMergingOrConfidenceChange()
    {
        var knowledge = new PlayerKnowledge();
        var first = Report.Create(
            100, 110, "S1", 2, ReportKind.Contact, "Blue", "fact:bridge",
            facts: new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Blue" });
        var second = Report.Create(
            105, 120, "S2", 3, ReportKind.Contact, "Red", "fact:bridge",
            facts: new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Red" });

        knowledge.Apply(first);
        knowledge.Apply(second);

        var claims = knowledge.Facts["fact:bridge"];
        Assert.Equal(2, claims.Count);
        Assert.Equal(2, claims[0].Confidence);
        Assert.Equal(3, claims[1].Confidence);
        Assert.Equal(2, knowledge.Zones["B"].Versions.Count);
        Assert.True(knowledge.Zones["B"].Conflicting);
        Assert.Equal(KnowledgePresence.Occupied, knowledge.Zones["B"].Presence);
        Assert.Equal(120, knowledge.LastUpdatedMinute);
    }

    [Fact]
    public void Knowledge_SameValueFromTwoSourcesIsStillTwoAppendedVersions()
    {
        var knowledge = new PlayerKnowledge();
        knowledge.Apply(Report.Create(100, 110, "S1", 2, ReportKind.Contact, "Blue", "f"));
        knowledge.Apply(Report.Create(101, 111, "S2", 2, ReportKind.Contact, "Blue", "f"));

        Assert.Equal(2, knowledge.Facts["f"].Count);
        Assert.All(knowledge.Facts["f"], claim => Assert.Equal(2, claim.Confidence));
    }

    [Fact]
    public void Knowledge_DoesNotStoreHiddenTruth()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            Clock = new Clock(0),
            Zones = new Dictionary<string, Zone>(),
            Units = new Dictionary<string, Unit>(),
            Knowledge = new PlayerKnowledge()
        };
        var zone = new Zone { Id = "B", Name = "B", Terrain = TerrainType.Forest, Observation = 1 };
        zone.PresentUnitIds.Add("SECRET_UNIT");
        state.Zones["B"] = zone;
        state.Units["SECRET_UNIT"] = new Unit
        {
            Id = "SECRET_UNIT",
            Name = "SECRET_UNIT",
            LocationZoneId = "B",
            Experience = 3,
            Concealed = true,
            Traits = new CommanderTraits(1, 1, 1, 0)
        };

        new KnowledgeUpdater().Apply(state.Knowledge, Report.Create(
            100, 110, "S1", 2, ReportKind.Contact, "Red", "zone:B",
            observedZoneId: "B", facts: new Dictionary<string, string> { ["value"] = "Red" }));

        var serialized = string.Join(
            "|",
            state.Knowledge.Facts.SelectMany(pair => pair.Value.Select(claim =>
                $"{claim.FactKey}:{claim.Value}:{claim.SourceId}")),
            state.Knowledge.Zones.SelectMany(pair => pair.Value.Versions.Select(claim =>
                $"{claim.FactKey}:{claim.Value}:{claim.SourceId}")));

        Assert.DoesNotContain("SECRET_UNIT", serialized);
        Assert.DoesNotContain("Concealed", serialized);
        Assert.DoesNotContain("Experience", serialized);
        Assert.DoesNotContain("PresentUnitIds", typeof(PlayerKnowledge).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void GameState_UsesAppendOnlyLogsAndStoresV04DeterminismFields()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            HqZoneId = "A",
            Clock = new Clock(0),
            Zones = new Dictionary<string, Zone>(),
            Units = new Dictionary<string, Unit>(),
            Knowledge = new PlayerKnowledge(),
            Seed = 42,
            RngState = 17,
            EndMinute = 480
        };
        state.EventLog.Add(new OrderIssued(10, "O1"));
        state.EventLog.Add(new OrderDelivered(20, "O1"));
        state.DeliveredReports.Add(Report.Create(10, 20, "U1", 1));
        state.DeliveredReports.Add(Report.Create(11, 21, "U1", 2));

        Assert.Equal("A", state.HqZoneId);
        Assert.Equal(42, state.Seed);
        Assert.Equal(17, state.RngState);
        Assert.Equal(480, state.EndMinute);
        var issued = Assert.IsType<OrderIssued>(state.EventLog[0]);
        var delivered = Assert.IsType<OrderDelivered>(state.EventLog[1]);
        Assert.Equal("O1", issued.OrderId);
        Assert.Equal("O1", delivered.OrderId);
        Assert.Equal(2, state.DeliveredReports.Count);
        Assert.Same(state.Couriers, state.CourierPool);
    }

    [Fact]
    public void CommanderTraits_MapV04InitPedantryHonesty()
    {
        var traits = new CommanderTraits(1, 2, 3, 0);

        Assert.Equal(1, traits.Init);
        Assert.Equal(3, traits.Pedantry);
        Assert.Equal(0, traits.Honesty);
        Assert.True(traits.InRange);
        Assert.False(CommanderTraits.IsInRange(4));
    }

    [Fact]
    public void Zone_ExposesV04ObservabilityDefenseAndCourierTimes()
    {
        var zone = new Zone
        {
            Id = "B3",
            Name = "B3",
            Terrain = TerrainType.Bridge,
            DefenseBonus = 2,
            Observation = 3
        };
        zone.TravelMinutes["B4"] = 20;
        zone.CourierMinutes["B4"] = 8;
        zone.EdgeTypes["B4"] = EdgeType.Offroad;

        Assert.Equal(3, zone.Observability);
        Assert.Equal(2, zone.DefenseBonus);
        Assert.Equal(20, zone.TravelMinutes["B4"]);
        Assert.Equal(8, zone.CourierMinutes["B4"]);
        Assert.Equal(EdgeType.Offroad, zone.EdgeTypeFor("B4"));
    }

    [Fact]
    public void Courier_LegsMustBePositive()
    {
        var courier = new Courier();

        Assert.False(courier.HasValidLegs);

        courier.OutboundDeliveryTime = 10;
        courier.ReturnTime = 15;
        courier.TLeg = 10;
        courier.TLegBack = 15;

        Assert.True(courier.HasValidLegs);
        Assert.True(courier.IsAvailable);
        Assert.Equal(10, courier.TLeg);
        Assert.Equal(15, courier.TLegBack);
    }

    [Fact]
    public void Contracts_ExposeV04RulesAndKnowledgeUpdater()
    {
        IGameRules rules = new RulesStub();
        IKnowledgeUpdater updater = new KnowledgeUpdater();

        Assert.True(rules.IsCombatEffective(NewUnit(strength: 30)));
        Assert.False(rules.IsCombatEffective(NewUnit(strength: 29)));
        Assert.Equal(0.30, rules.CombatStrengthThreshold, 5);
        Assert.True(rules.IsConfidenceInRange(1));
        Assert.True(rules.IsConfidenceInRange(3));
        Assert.False(rules.IsConfidenceInRange(0));
        Assert.False(rules.IsConfidenceInRange(4));
        Assert.Equal(1, rules.ClampConfidence(0));

        var knowledge = new PlayerKnowledge();
        updater.Apply(knowledge, Report.Create(10, 20, "U1", 1));
        Assert.Single(knowledge.Facts);
        Assert.StartsWith("report:", knowledge.Facts.Keys.Single());
    }

    private sealed class RulesStub : IGameRules
    {
        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }
}
