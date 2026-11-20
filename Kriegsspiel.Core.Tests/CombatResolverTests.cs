using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Tests;

public class CombatResolverTests
{
    [Fact]
    public void Resolve_IsDeterministic_WithoutRandomMultiplier()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = new FrictionTable();

        var stateA = CreateState();
        var stateB = CreateState();

        var reportsA = resolver.Resolve(new CombatTick(100, "C"), stateA, friction, new Random(1)).ToList();
        var reportsB = resolver.Resolve(new CombatTick(100, "C"), stateB, friction, new Random(999)).ToList();

        Assert.Equal(stateA.Units["B1"].Strength, stateB.Units["B1"].Strength);
        Assert.Equal(stateA.Units["B1"].Morale, stateB.Units["B1"].Morale);
        Assert.Equal(stateA.Units["B1"].Readiness, stateB.Units["B1"].Readiness);
        Assert.Equal(stateA.Units["B1"].Ammo, stateB.Units["B1"].Ammo);
        Assert.Equal(stateA.Units["R1"].Strength, stateB.Units["R1"].Strength);
        Assert.Equal(stateA.Units["R1"].Morale, stateB.Units["R1"].Morale);
        Assert.Equal(
            stateA.PendingCasualties.OrderBy(p => p.UnitId).Select(p => (p.UnitId, p.Amount, p.Minute)),
            stateB.PendingCasualties.OrderBy(p => p.UnitId).Select(p => (p.UnitId, p.Amount, p.Minute)));
        Assert.Equal(reportsA.Count, reportsB.Count);
    }

    [Fact]
    public void ContactGeometryResolver_ClassifiesFrontFlankRear_AndSpottingFirst()
    {
        var resolver = new ContactGeometryResolver();
        var zones = new Dictionary<string, Zone>
        {
            ["C"] = ZoneWith("C", "A", "B", "N", "S"),
            ["A"] = ZoneWith("A", "C", "B"),
            ["B"] = ZoneWith("B", "C", "A"),
            ["N"] = ZoneWith("N", "C"),
            ["S"] = ZoneWith("S", "C")
        };

        var front = resolver.Resolve(Unit("AT", Faction.Blue, "C", move: "A"), Unit("DF", Faction.Red, "C", attention: "A"), zones["C"], zones);
        var flank = resolver.Resolve(Unit("AT", Faction.Blue, "C", move: "A"), Unit("DF", Faction.Red, "C", attention: "B"), zones["C"], zones);
        var rear = resolver.Resolve(Unit("AT", Faction.Blue, "C", move: "N"), Unit("DF", Faction.Red, "C", attention: "S"), zones["C"], zones);
        var spotted = resolver.Resolve(Unit("AT", Faction.Blue, "C", move: "A"), Unit("DF", Faction.Red, "C", attention: "S"), zones["C"], zones, attackerSpottedFirst: true);

        Assert.Equal(ContactClass.Front, front.Classification);
        Assert.Equal(ContactClass.Flank, flank.Classification);
        Assert.Equal(ContactClass.Rear, rear.Classification);
        Assert.Equal(1.0, front.CollisionMultiplier);
        Assert.Equal(1.4, flank.CollisionMultiplier);
        Assert.Equal(1.8, rear.CollisionMultiplier);
        Assert.Equal(0, front.MoralePenalty);
        Assert.Equal(10, flank.MoralePenalty);
        Assert.Equal(15, rear.MoralePenalty);
        Assert.Equal(ContactClass.Front, spotted.Classification);
        Assert.Equal("A", spotted.AttentionEdgeId);
        Assert.True(spotted.FirstContact);
    }

    [Fact]
    public void Resolve_AppliesContactMoralePenalties_FlankAndRear()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = ZeroLethalityFriction();

        var front = CreateState(attention: "A", move: "A");
        var flank = CreateState(attention: "B", move: "A");
        var rear = CreateState(attention: "S", move: "N");

        resolver.Resolve(new CombatTick(100, "C"), front, friction, new Random(1)).ToList();
        resolver.Resolve(new CombatTick(100, "C"), flank, friction, new Random(1)).ToList();
        resolver.Resolve(new CombatTick(100, "C"), rear, friction, new Random(1)).ToList();

        Assert.Equal(80, front.Units["R1"].Morale);
        Assert.Equal(70, flank.Units["R1"].Morale);
        Assert.Equal(65, rear.Units["R1"].Morale);
    }

    [Fact]
    public void Resolve_TerrainBonus_AppliesOnlyToDefense()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = new FrictionTable();

        var defended = CreateState(attention: "A", move: "A", defenderPosture: Posture.Defend, defenseBonus: 5);
        var notDefending = CreateState(attention: "A", move: "A", defenderPosture: Posture.Attack, defenseBonus: 5);

        resolver.Resolve(new CombatTick(100, "C"), defended, friction, new Random(1)).ToList();
        resolver.Resolve(new CombatTick(100, "C"), notDefending, friction, new Random(1)).ToList();

        int attackerLossVsDefender = TotalLoss(defended, "B1");
        int attackerLossVsNonDefense = TotalLoss(notDefending, "B1");
        int defenderLossDefending = TotalLoss(defended, "R1");
        int defenderLossNotDefending = TotalLoss(notDefending, "R1");

        Assert.True(attackerLossVsDefender > attackerLossVsNonDefense);
        Assert.True(defenderLossDefending < defenderLossNotDefending);
    }

    [Fact]
    public void Resolve_HalfLossImmediate_HalfPending_AndConsumesResources()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = new FrictionTable();
        var state = CreateState();

        resolver.Resolve(new CombatTick(120, "C"), state, friction, new Random(1)).ToList();

        foreach (var unitId in new[] { "B1", "R1" })
        {
            var unit = state.Units[unitId];
            var pending = Assert.Single(state.PendingCasualties, p => p.UnitId == unitId);
            int total = (100 - unit.Strength) + pending.Amount;
            Assert.Equal(total / 2, pending.Amount);
            Assert.Equal(150, pending.Minute);
            Assert.Equal("C", pending.ZoneId);
            Assert.Equal(120, pending.CombatTickMinute);
            Assert.Equal(95, unit.Readiness);
            Assert.Equal(5, unit.Fatigue);
            Assert.Equal(95, unit.Ammo);
            Assert.True(unit.Morale < 80);
        }

        Assert.Contains(state.Clock.PendingKeys, k => k.Minute == 150 && k.Phase == EventPhase.Combat);
    }

    [Fact]
    public void Resolve_AmmoZero_ReducesCombatEffectiveness()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = new FrictionTable();

        var withAmmo = CreateState(attackerAmmo: 100);
        var noAmmo = CreateState(attackerAmmo: 0);

        resolver.Resolve(new CombatTick(100, "C"), withAmmo, friction, new Random(1)).ToList();
        resolver.Resolve(new CombatTick(100, "C"), noAmmo, friction, new Random(1)).ToList();

        Assert.True(TotalLoss(noAmmo, "B1") > TotalLoss(withAmmo, "B1"));
        Assert.True(TotalLoss(noAmmo, "R1") < TotalLoss(withAmmo, "R1"));
        Assert.Equal(0, noAmmo.Units["B1"].Ammo);
    }

    [Fact]
    public void Resolve_PoorControl_PanicWithdrawal_And_Capture()
    {
        var resolver = new CombatResolver(new ContactGeometryResolver());
        var friction = new FrictionTable();

        var panicZones = Zones("C", "A");
        var poorAttacker = Unit("B1", Faction.Blue, "C", move: "A", posture: Posture.Attack);
        poorAttacker.PoorControl = true;
        poorAttacker.EffectiveOrder = new EffectiveOrder { PoorControlInducing = true, SourceOrderId = "o1", ToUnitId = "B1" };
        var panickedDefender = Unit("R1", Faction.Red, "C", attention: "A", posture: Posture.Defend, morale: 20);
        var panicState = State(panicZones, poorAttacker, panickedDefender);
        resolver.Resolve(new CombatTick(100, "C"), panicState, friction, new Random(1)).ToList();

        Assert.True(panicState.Units["B1"].PoorControl);
        Assert.Equal(Posture.Withdraw, panicState.Units["R1"].Posture);
        Assert.Equal("A", panicState.Units["R1"].LocationZoneId);

        var captureZones = Zones("C");
        var attacker = Unit("B2", Faction.Blue, "C", move: "C", posture: Posture.Attack, strength: 100);
        var defender = Unit("R2", Faction.Red, "C", attention: "C", posture: Posture.Defend, morale: 10, strength: 10, ammo: 0);
        var captureState = State(captureZones, attacker, defender);
        resolver.Resolve(new CombatTick(100, "C"), captureState, friction, new Random(1)).ToList();

        Assert.True(captureState.Units["R2"].Captured);
        Assert.False(captureState.Units["R2"].IsCombatEffective);
    }

    private static int TotalLoss(GameState state, string unitId)
    {
        var unit = state.Units[unitId];
        int pending = state.PendingCasualties.Where(p => p.UnitId == unitId).Sum(p => p.Amount);
        return (100 - unit.Strength) + pending;
    }

    private static FrictionTable ZeroLethalityFriction() => new()
    {
        Combat = new FrictionTable.CombatParameters
        {
            LethalitySelf = 0,
            LethalityEnemy = 0
        }
    };

    private static GameState CreateState(
        string attention = "A",
        string move = "A",
        Posture defenderPosture = Posture.Defend,
        int defenseBonus = 0,
        int attackerAmmo = 100)
    {
        var zones = new Dictionary<string, Zone>
        {
            ["C"] = ZoneWith("C", defenseBonus, "A", "B", "N", "S"),
            ["A"] = ZoneWith("A", "C", "B"),
            ["B"] = ZoneWith("B", "C", "A"),
            ["N"] = ZoneWith("N", "C"),
            ["S"] = ZoneWith("S", "C")
        };

        var blue = Unit("B1", Faction.Blue, "C", move: move, posture: Posture.Attack, ammo: attackerAmmo);
        var red = Unit("R1", Faction.Red, "C", attention: attention, posture: defenderPosture);
        return State(zones, blue, red);
    }

    private static GameState State(Dictionary<string, Zone> zones, params Unit[] units)
    {
        var map = units.ToDictionary(u => u.Id);
        foreach (var unit in units)
        {
            if (!zones.TryGetValue(unit.LocationZoneId, out var zone)) continue;
            if (!zone.PresentUnitIds.Contains(unit.Id))
                zone.PresentUnitIds.Add(unit.Id);
        }

        return new GameState
        {
            ScenarioId = "t",
            Clock = new Clock(100),
            Zones = zones,
            Units = map,
            Knowledge = new PlayerKnowledge(),
            Seed = 1
        };
    }

    private static Dictionary<string, Zone> Zones(string center, params string[] neighbors)
    {
        var dict = new Dictionary<string, Zone> { [center] = ZoneWith(center, neighbors) };
        foreach (var n in neighbors)
            dict[n] = ZoneWith(n, center);
        return dict;
    }

    private static Zone ZoneWith(string id, params string[] neighbors) => ZoneWith(id, 0, neighbors);

    private static Zone ZoneWith(string id, int defenseBonus, params string[] neighbors)
    {
        var z = new Zone
        {
            Id = id,
            Name = id,
            Terrain = TerrainType.Open,
            DefenseBonus = defenseBonus
        };
        foreach (var n in neighbors)
        {
            z.TravelMinutes[n] = 30;
            z.EdgeTypes[n] = EdgeType.Road;
        }
        return z;
    }

    private static Unit Unit(
        string id,
        Faction faction,
        string zone,
        string? move = null,
        string? attention = null,
        Posture posture = Posture.Attack,
        int morale = 80,
        int strength = 100,
        int ammo = 100)
    {
        return new Unit
        {
            Id = id,
            Name = id,
            Faction = faction,
            LocationZoneId = zone,
            MoveEdgeId = move,
            AttentionEdgeId = attention,
            Posture = posture,
            Strength = strength,
            InitialStrengthSnapshot = 100,
            Readiness = 100,
            Morale = morale,
            Fatigue = 0,
            Ammo = ammo,
            Traits = new CommanderTraits(1, 1, 1, 1)
        };
    }
}
