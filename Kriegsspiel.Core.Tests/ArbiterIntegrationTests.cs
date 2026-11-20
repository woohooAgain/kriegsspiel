using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Tests;

public class ArbiterIntegrationTests
{
    [Fact]
    public void AdvanceTo_Timeline_06_15_To_07_15_DeliversAndExecutesOrder_AndLateReportStaysInTransit()
    {
        var friction = TimelineFriction();
        var state = TimelineState(friction);
        var unit12 = state.Units["U12"];
        var rng = new ZeroRandom();
        var arbiter = new Arbiter(friction, rng,
            new TransmissionResolver(), new ImmediateUnderstandingResolver(),
            new TimelineExecutionResolver(), new NoContactCombatResolver());

        var order = new Order
        {
            Id = "ord-0615",
            ToUnitId = "U12",
            Intent = OrderIntent.Occupy,
            TargetZoneId = "B3",
            NotLaterThan = 450,
            ReportBy = 465,
            MovementMode = MovementMode.Covert,
            AmbiguityScore = 0,
            IssuedAt = 375
        };

        var first = arbiter.AdvanceTo(405, state, [order]);

        Assert.Equal(405, first.Now);
        Assert.Equal(405, state.Clock.Now);
        Assert.Contains(state.EventLog, e => e is OrderDelivered d && d.OrderId == "ord-0615" && d.Minute == 405);
        Assert.Contains(state.EventLog, e => e is OrderAccepted a && a.Minute == 405);
        Assert.Equal("B3", unit12.LocationZoneId);
        Assert.Contains(state.Couriers, c => c.ExpectedDeliveryMinute == 405 || c.ExpectedReturnMinute == 435 || c.OutboundDepartureMinute == 375);

        var second = arbiter.AdvanceTo(435, state, Array.Empty<Order>());

        Assert.Equal(435, second.Now);
        Assert.Contains(second.NewReports, r => r.FromUnitId == "U9" && r.ArrivesAt == 425 && r.ArrivesAt <= 435);
        Assert.DoesNotContain(second.NewReports, r => r.Id == "r12-late" || r.ArrivesAt > 435);

        var late = state.InTransitReports.FirstOrDefault(r => r.Id == "r12-late")
                ?? state.DeliveredReports.FirstOrDefault(r => r.Id == "r12-late");
        Assert.NotNull(late);
        Assert.Equal(440, late!.ArrivesAt);
        Assert.DoesNotContain(state.DeliveredReports, r => r.Id == "r12-late");
        Assert.Contains(state.Clock.PendingKeys, k => k.Minute >= 440);

        var third = arbiter.AdvanceTo(450, state, Array.Empty<Order>());
        Assert.Contains(third.NewReports, r => r.Id == "r12-late");
        Assert.Contains(state.DeliveredReports, r => r.Id == "r12-late");
    }

    [Fact]
    public void AdvanceTo_QuerySilence_IsLogged_And_OrderLost_IsNotDelivery()
    {
        var friction = TimelineFriction();
        var state = TimelineState(friction);
        var rng = new ZeroRandom();
        var arbiter = new Arbiter(friction, rng,
            new AlwaysLoseTransmissionResolver(), new ImmediateUnderstandingResolver(),
            new TimelineExecutionResolver(), new NoContactCombatResolver());

        var lostOrder = new Order { Id = "ord-lost", ToUnitId = "U12", Intent = OrderIntent.Hold, TargetZoneId = "A", IssuedAt = 375 };
        arbiter.AdvanceTo(420, state, [lostOrder]);

        Assert.Contains(state.EventLog, e => e is OrderLost ol && ol.OrderId == "ord-lost");
        Assert.DoesNotContain(state.EventLog, e => e is OrderDelivered od && od.OrderId == "ord-lost");
        Assert.DoesNotContain(state.DeliveredReports, r => r.FactKey == "delivered:ord-lost");

        state.Clock.Schedule(new QuerySilence(430, "U9", 1), 430, EventPhase.Scheduled);
        arbiter.AdvanceTo(450, state, Array.Empty<Order>());

        Assert.Contains(state.EventLog, e => e is QuerySilence qs && qs.TargetId == "U9" && qs.Minute == 430);
        Assert.DoesNotContain(state.EventLog, e => e is OrderDelivered od && od.OrderId == "ord-lost");
    }

    [Fact]
    public void AdvanceTo_ContactDuringMovement_PlansCombatTick()
    {
        var friction = TimelineFriction();
        var state = TimelineState(friction);
        state.Units["E1"].LocationZoneId = "B3";
        state.Zones["A"].PresentUnitIds.Remove("E1");
        state.Zones["B3"].PresentUnitIds.Add("E1");

        var rng = new ZeroRandom();
        var arbiter = new Arbiter(friction, rng,
            new TransmissionResolver(), new ImmediateUnderstandingResolver(),
            new ContactingExecutionResolver(), new NoContactCombatResolver());

        var order = new Order
        {
            Id = "ord-contact",
            ToUnitId = "U12",
            Intent = OrderIntent.Occupy,
            TargetZoneId = "B3",
            MovementMode = MovementMode.March,
            IssuedAt = 375
        };

        arbiter.AdvanceTo(435, state, [order]);

        Assert.Contains(state.EventLog, e => e is CombatTick ct && ct.ZoneId == "B3");
        Assert.Contains(state.EventLog, e => e is CombatTick ct && (ct.Minute % 30 == 0));
    }

    private static FrictionTable TimelineFriction() => new()
    {
        Hq = new FrictionTable.HqParameters
        {
            Couriers = 2,
            CourierReplacementMinutes = 60,
            DefaultDeliveryTimeMinutes = 30,
            DefaultReturnTimeMinutes = 30,
            CourierLossBase = 0.0,
            CourierLossPerKm = 0.0,
            CourierLossHostileBonus = 0.0,
            ReportLossBase = 0.0
        },
        Orders = new FrictionTable.OrderParameters
        {
            P0 = 0.0,
            PMin = 0.0,
            PMax = 1.0,
            MaxBranches = 3
        },
        Movement = new FrictionTable.MovementParameters(),
        Combat = new FrictionTable.CombatParameters(),
        Recon = new FrictionTable.ReconParameters(),
        Reports = new FrictionTable.ReportParameters { HonestyDistortionPerLevel = 0.1, QuerySilenceMinutes = 60 },
        Supply = new FrictionTable.SupplyParameters(),
        Capture = new FrictionTable.CaptureParameters(),
        Civilians = new FrictionTable.CivilianParameters(),
        Llm = new FrictionTable.LlmParameters { Enabled = false },
        DifficultyProfiles = []
    };

    private static GameState TimelineState(FrictionTable friction)
    {
        var state = new GameState
        {
            ScenarioId = "s14",
            Clock = new Clock(375),
            Zones = new Dictionary<string, Zone>
            {
                ["HQ"] = Zone("HQ", "HQ", TerrainType.Village, ("A", 30)),
                ["A"] = Zone("A", "A", TerrainType.Open, ("HQ", 30), ("B3", 20), ("B4", 20)),
                ["B3"] = Zone("B3", "Мост B3", TerrainType.Bridge, ("A", 20)),
                ["B4"] = Zone("B4", "Лес", TerrainType.Forest, ("A", 20))
            },
            Units = new Dictionary<string, Unit>
            {
                ["U12"] = Unit("U12", "12-й полк", "A", Faction.Blue, 2, 0, 0, 2),
                ["U9"] = Unit("U9", "9-й полк", "A", Faction.Blue, 1, 1, 1, 2),
                ["E1"] = Unit("E1", "Противник", "B4", Faction.Red, 1, 1, 1, 1)
            },
            Knowledge = new PlayerKnowledge(),
            Seed = 1
        };
        foreach (var unit in state.Units.Values)
            if (state.Zones.TryGetValue(unit.LocationZoneId, out var zone) && !zone.PresentUnitIds.Contains(unit.Id))
                zone.PresentUnitIds.Add(unit.Id);
        return state;
    }

    private static Zone Zone(string id, string name, TerrainType terrain, params (string to, int minutes)[] links)
    {
        var z = new Zone { Id = id, Name = name, Terrain = terrain };
        foreach (var (to, minutes) in links)
            z.TravelMinutes[to] = minutes;
        return z;
    }

    private static Unit Unit(string id, string name, string zone, Faction faction, int initiative, int caution, int literalism, int honesty) => new()
    {
        Id = id,
        Name = name,
        Strength = 100,
        InitialStrengthSnapshot = 100,
        Readiness = 80,
        Morale = 80,
        Fatigue = 0,
        Ammo = 100,
        LocationZoneId = zone,
        Faction = faction,
        Traits = new CommanderTraits(initiative, caution, literalism, honesty)
    };

    private sealed class ZeroRandom : Random
    {
        public override int Next(int minValue, int maxValue) => minValue;
        public override int Next() => 0;
        protected override double Sample() => 0.0;
    }

    private sealed class NoContactCombatResolver : ICombatResolver
    {
        public IEnumerable<Report> Resolve(CombatTick e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
    }

    private sealed class AlwaysLoseTransmissionResolver : ITransmissionResolver
    {
        public TransmissionResult Transmit(Order o, GameState s, FrictionTable f, Random rng) => new(Math.Max(1, f.Hq.DefaultDeliveryTimeMinutes), true);
        public void EnsureCouriers(GameState state, FrictionTable friction) { }
        public void UpdateTo(GameState state, int minute) { }
        public void UpdateTo(GameState state, int minute, Random rng) { }
    }

    private sealed class TimelineExecutionResolver : IExecutionResolver
    {
        public IEnumerable<Report> Start(OrderAccepted e, GameState s, FrictionTable f, Random rng)
        {
            if (e.OrderId != "ord-0615") return Array.Empty<Report>();
            if (!s.Units.TryGetValue("U12", out var unit)) return Array.Empty<Report>();
            s.Zones[unit.LocationZoneId].PresentUnitIds.Remove(unit.Id);
            unit.LocationZoneId = "B3";
            if (!s.Zones["B3"].PresentUnitIds.Contains(unit.Id)) s.Zones["B3"].PresentUnitIds.Add(unit.Id);
            unit.CurrentOrderId = e.OrderId;

            var u9 = s.Units["U9"];
            var report9 = new Report
            {
                Id = "r9-1",
                Minute = 425,
                ArrivesAt = 425,
                FromUnitId = u9.Id,
                Text = "Слышим бой северо-восточнее моста. Своих не видим.",
                Kind = ReportKind.Contact,
                Confidence = 2,
                FactKey = "zone:B3:noise",
                SourceFaction = Faction.Blue
            };
            s.InTransitReports.Add(report9);
            s.Clock.Schedule(new ReportDelivered(425, report9.Id, u9.Id, report9), 425, EventPhase.ReportDeliver);

            var late = new Report
            {
                Id = "r12-late",
                Minute = 430,
                ArrivesAt = 440,
                FromUnitId = unit.Id,
                Text = "Заняли мост. Противника не обнаружили.",
                Kind = ReportKind.Status,
                Confidence = 2,
                FactKey = "zone:B3:control",
                SourceFaction = Faction.Blue
            };
            s.InTransitReports.Add(late);
            s.Clock.Schedule(new ReportDelivered(440, late.Id, unit.Id, late), 440, EventPhase.ReportDeliver);
            return Array.Empty<Report>();
        }

        public IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
        public IEnumerable<Report> OnArrive(UnitArrived e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
        public IEnumerable<Report> GenerateReport(ReportDue e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
    }

    private sealed class ContactingExecutionResolver : IExecutionResolver
    {
        public IEnumerable<Report> Start(OrderAccepted e, GameState s, FrictionTable f, Random rng)
        {
            if (!s.Units.TryGetValue("U12", out var unit))
                return Array.Empty<Report>();
            int at = Math.Max(e.Minute, s.Clock.Now);
            s.Clock.Schedule(new UnitMoved(at, unit.Id, "B3"), at, EventPhase.Movement);
            return Array.Empty<Report>();
        }

        public IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng)
        {
            if (!s.Units.TryGetValue(e.UnitId, out var unit)) return Array.Empty<Report>();
            if (s.Zones.TryGetValue(unit.LocationZoneId, out var from)) from.PresentUnitIds.Remove(unit.Id);
            unit.LocationZoneId = e.ToZoneId;
            if (s.Zones.TryGetValue(e.ToZoneId, out var to) && !to.PresentUnitIds.Contains(unit.Id)) to.PresentUnitIds.Add(unit.Id);
            return Array.Empty<Report>();
        }

        public IEnumerable<Report> OnArrive(UnitArrived e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
        public IEnumerable<Report> GenerateReport(ReportDue e, GameState s, FrictionTable f, Random rng) => Array.Empty<Report>();
    }

    private sealed class ImmediateUnderstandingResolver : IUnderstandingResolver
    {
        public IEnumerable<Report> Resolve(OrderDelivered e, GameState s, FrictionTable f, Random rng)
        {
            s.Clock.Schedule(new OrderAccepted(e.Minute, e.OrderId), e.Minute, EventPhase.Understanding);
            return Array.Empty<Report>();
        }
    }
}
