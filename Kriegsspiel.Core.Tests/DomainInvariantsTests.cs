using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class DomainInvariantsTests
{
    private static Unit NewUnit(
        int strength = 100,
        int morale = 70,
        int readiness = 60,
        int ammo = 10,
        int snapshot = 100) => new()
    {
        Id = "U1",
        Name = "U1",
        LocationZoneId = "A",
        Traits = new CommanderTraits(1, 1, 1, 1),
        Strength = strength,
        Morale = morale,
        Readiness = readiness,
        Ammo = ammo,
        InitialStrengthSnapshot = snapshot
    };

    [Theory]
    [InlineData(39, 70, 60, 10)]
    [InlineData(100, 29, 60, 10)]
    [InlineData(100, 70, 19, 10)]
    [InlineData(100, 70, 60, 0)]
    public void CombatEffective_FalseWhenAnyThresholdFails(
        int strength, int morale, int readiness, int ammo)
    {
        Assert.False(NewUnit(strength, morale, readiness, ammo).IsCombatEffective);
    }

    [Fact]
    public void CombatEffective_TrueAtExactThresholds()
    {
        Assert.True(NewUnit(40, 30, 20, 1).IsCombatEffective);
    }

    [Fact]
    public void CombatEffective_ZeroSnapshotFallsBackToCurrentStrength()
    {
        Assert.True(NewUnit(strength: 30, snapshot: 0).IsCombatEffective);
        Assert.False(NewUnit(strength: 30, morale: 20, snapshot: 0).IsCombatEffective);
    }

    [Fact]
    public void Report_HasFourMandatoryFields()
    {
        var report = Report.Create(
            100, 120, "U1", 2,
            ReportKind.Contact, "contact", "zone:A", Faction.Red, "XO");

        Assert.Equal(100, report.EventMinute);
        Assert.Equal(120, report.DeliveryMinute);
        Assert.Equal("U1", report.SourceId);
        Assert.Equal(2, report.Confidence);
        Assert.Equal(Faction.Red, report.SourceFaction);
        Assert.Equal("XO", report.ReportedBy);
        Assert.Equal("zone:A", report.FactKey);
        Assert.True(report.HasRequiredFields);
        Assert.Throws<ArgumentOutOfRangeException>(() => { report.Confidence = 4; });
        Assert.Throws<ArgumentOutOfRangeException>(() => { report.Confidence = -1; });
    }

    [Fact]
    public void Knowledge_DistinguishesNoDataFromEmpty()
    {
        var knowledge = new PlayerKnowledge();
        Assert.False(knowledge.HasZoneData("A"));

        var empty = Report.Create(
            100, 110, "SCOUT", 3,
            ReportKind.Recon, "пусто", "zone:A",
            facts: new Dictionary<string, string> { ["value"] = KnowledgeClaim.EmptyValue });
        knowledge.Apply(empty);

        Assert.True(knowledge.HasZoneData("A"));
        Assert.True(knowledge.Zones["A"].IsEmpty);
        Assert.Equal(100, knowledge.Zones["A"].EventMinute!.Value);
        Assert.Equal(110, knowledge.Zones["A"].DeliveryMinute!.Value);
    }

    [Fact]
    public void Knowledge_ContradictionKeepsBothVersionsAndLowersConfidence()
    {
        var knowledge = new PlayerKnowledge();
        var first = Report.Create(
            100, 110, "S1", 2, ReportKind.Contact, "Blue", "fact:bridge",
            facts: new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Blue" });
        var second = Report.Create(
            105, 120, "S2", 2, ReportKind.Contact, "Red", "fact:bridge",
            facts: new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Red" });

        knowledge.Apply(first);
        knowledge.Apply(second);

        var claims = knowledge.Facts["fact:bridge"];
        Assert.Equal(2, claims.Count);
        Assert.All(claims, claim => Assert.Equal(1, claim.Confidence));
        Assert.True(knowledge.Zones["B"].Conflicting);
        Assert.False(knowledge.Zones["B"].IsEmpty);
    }

    [Fact]
    public void Knowledge_SameFactFromDifferentSourcesRaisesConfidence()
    {
        var knowledge = new PlayerKnowledge();
        knowledge.Apply(Report.Create(100, 110, "S1", 2, ReportKind.Contact, "Blue", "f"));
        knowledge.Apply(Report.Create(101, 111, "S2", 2, ReportKind.Contact, "Blue", "f"));

        Assert.Single(knowledge.Facts["f"]);
        Assert.Equal(3, knowledge.Facts["f"][0].Confidence);
    }

    [Fact]
    public void Order_StoresVersionAndOriginalBranches()
    {
        var order = new Order
        {
            ToUnitId = "U1",
            Intent = OrderIntent.Occupy,
            TargetZoneId = "B3",
            OrderVersion = 4,
            MovementMode = MovementMode.Covert,
            OriginalText = "occupy B3",
            Branches =
            [
                new OrderBranch(BranchCondition.TargetHeldByEnemy, BranchAction.Attack, BranchAction.Hold)
            ]
        };

        Assert.Equal(4, order.OrderVersion);
        Assert.Single(order.Branches);
        Assert.Equal(BranchAction.Hold, order.Branches[0].Else);
        Assert.Equal(MovementMode.Covert, order.MovementMode);
        Assert.True(order.HasValidStructure);

        var branch = new OrderBranch(BranchCondition.BridgeDestroyed, BranchAction.Bypass);
        Assert.Throws<ArgumentException>(() =>
        {
            _ = new Order
            {
                ToUnitId = "U1",
                Branches = [branch, branch, branch, branch]
            };
        });
    }

    [Fact]
    public void GameState_ContainsDeliveryAndDeterminismQueues()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            Clock = new Clock(0),
            Zones = new(),
            Units = new(),
            Knowledge = new PlayerKnowledge(),
            Seed = 42
        };

        Assert.NotNull(state.InTransitReports);
        Assert.Same(state.Couriers, state.CourierPool);
        Assert.NotNull(state.PendingCasualties);
        Assert.NotNull(state.PendingEvents);
        Assert.Equal(42, state.Seed);
        Assert.Empty(state.Orders);
    }

    [Fact]
    public void CommanderTraits_HaveFiveTraitsWithDefaultProfessionalism()
    {
        var traits = new CommanderTraits(1, 2, 3, 0);

        Assert.Equal(1, traits.Professionalism);
        Assert.True(traits.InRange);
        Assert.False(CommanderTraits.IsInRange(4));
    }

    [Fact]
    public void Zone_ExposesObjectsCargoEdgesAndControl()
    {
        var zone = new Zone
        {
            Id = "B3",
            Name = "B3",
            Terrain = TerrainType.Bridge,
            ControllingFaction = Faction.Blue
        };
        zone.EdgeTypes["B4"] = EdgeType.Crossing;
        zone.Objects.Add(new ZoneObject { Kind = ZoneObjectKind.Bridge, EdgeId = "B3-B4" });
        zone.Cargo.Add(new Cargo { Type = CargoType.Ammo, Quantity = 20, Owner = Faction.Blue });

        Assert.Equal(EdgeType.Crossing, zone.EdgeTypeFor("B4"));
        Assert.True(zone.IsCrossing);
        Assert.True(zone.HasCargo(CargoType.Ammo));
        Assert.Equal(20, zone.CargoAmount(CargoType.Ammo));
        Assert.True(zone.IsControlledBy(Faction.Blue));
        Assert.False(zone.IsControlledBy(Faction.Red));
    }

    [Fact]
    public void Courier_LegsMustBePositive()
    {
        var courier = new Courier();
        Assert.False(courier.HasValidLegs);

        courier.OutboundDeliveryTime = 10;
        courier.ReturnTime = 15;

        Assert.True(courier.HasValidLegs);
        Assert.True(courier.IsAvailable);
    }

    [Fact]
    public void ContactGeometry_ClassifiesByEdges()
    {
        Assert.Equal(ContactClass.Front, ContactGeometry.Classify("e1", "e1"));
        Assert.Equal(ContactClass.Flank, ContactGeometry.Classify("e1", "e2", true));
        Assert.Equal(ContactClass.Rear, ContactGeometry.Classify("e1", "e2"));

        var rear = new ContactGeometry { Classification = ContactClass.Rear };
        Assert.Equal(1.8, rear.CollisionMultiplier);
        Assert.Equal(15, rear.MoralePenalty);
    }

    [Fact]
    public void FrozenEnums_HaveContractValues()
    {
        Assert.Equal(0, (int)Faction.Blue);
        Assert.Equal(1, (int)Faction.Red);
        Assert.Equal(2, (int)Faction.Neutral);
        Assert.Equal(0, (int)EdgeType.Road);
        Assert.Equal(1, (int)EdgeType.Offroad);
        Assert.Equal(2, (int)EdgeType.Crossing);
        Assert.Equal(0, (int)ZoneObjectKind.Bridge);
        Assert.Equal(3, (int)ZoneObjectKind.Depot);
        Assert.Equal(0, (int)CargoType.Ammo);
        Assert.Equal(1, (int)CargoType.Supplies);
        Assert.Equal(5, (int)ReportKind.Impossible);
        Assert.True(Enum.IsDefined(ReportKind.Impossible));
    }

    [Fact]
    public void Contracts_AreImplementableByWaveOne()
    {
        IGameRules rules = new RulesStub();
        IRandomSource rng = new RandomStub(7);
        IReportFactory factory = new ReportFactoryStub();
        IKnowledgeUpdater updater = new KnowledgeUpdaterStub();

        Assert.True(rules.IsCombatEffective(NewUnit()));
        Assert.Equal(30, rules.TurnMinutes);
        Assert.Equal(3, rules.MaxBranches);
        Assert.Equal(7, rng.Seed);
        rng.Restore(123);
        Assert.Equal(123, rng.State);

        var report = factory.Create(10, 20, "U1", 1);
        Assert.True(report.HasRequiredFields);

        var knowledge = new PlayerKnowledge();
        updater.Apply(knowledge, [report]);
        Assert.True(knowledge.Facts.ContainsKey("report:" + report.Id));
    }

    [Fact]
    public void NegativeQuantitiesAreClamped()
    {
        var cargo = new Cargo { Quantity = -5 };
        var casualty = new PendingCasualty(390, "U1", -3);
        var civilians = new CivilianGroup { Panic = 9, Population = -1 };

        Assert.Equal(0, cargo.Quantity);
        Assert.Equal(0, casualty.Amount);
        Assert.Equal(3, civilians.Panic);
        Assert.Equal(0, civilians.Population);
    }

    private sealed class RulesStub : IGameRules
    {
        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }

    private sealed class RandomStub : IRandomSource
    {
        private Random _random;

        public RandomStub(int seed)
        {
            Seed = seed;
            State = seed;
            _random = new Random(seed);
        }

        public int Seed { get; }
        public long State { get; private set; }

        public double NextDouble()
        {
            State++;
            return _random.NextDouble();
        }

        public int NextInt(int minInclusive, int maxExclusive)
        {
            State++;
            return _random.Next(minInclusive, maxExclusive);
        }

        public void Restore(long state)
        {
            State = state;
            _random = new Random((int)(state % int.MaxValue));
        }

        public void Reset() => Restore(Seed);
    }

    private sealed class ReportFactoryStub : IReportFactory
    {
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
            Dictionary<string, string>? facts = null) =>
            Report.Create(
                eventMinute, deliveryMinute, sourceId, confidence,
                kind, text, factKey, sourceFaction, reportedBy, facts);
    }

    private sealed class KnowledgeUpdaterStub : IKnowledgeUpdater
    {
        public void Apply(PlayerKnowledge knowledge, Report report) => knowledge.Apply(report);
    }
}
