using System.Runtime.CompilerServices;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class ExecutionResolver : IExecutionResolver
{
    private const int MaxConditionValue = 100;
    private const int CombatTurnMinutes = 30;

    private readonly IGameRules _rules;
    private readonly IReportFactory _reports;
    private readonly ConditionalWeakTable<GameState, ExecutionState> _states = new();

    public ExecutionResolver() : this(new DefaultGameRules(), new DefaultReportFactory())
    {
    }

    public ExecutionResolver(IGameRules rules, IReportFactory reports)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _reports = reports ?? throw new ArgumentNullException(nameof(reports));
    }

    public IEnumerable<Report> Start(OrderAccepted e, GameState s, FrictionTable f, Random rng)
    {
        var order = s.Orders.FirstOrDefault(o => o.Id == e.OrderId);
        if (order is null) yield break;
        if (!s.Units.TryGetValue(order.ToUnitId, out var unit)) yield break;

        if (unit.IsOnEdge)
        {
            unit.PendingOrder = order;
            yield break;
        }

        var effective = s.EffectiveOrders.TryGetValue(e.OrderId, out var existing)
            ? existing
            : EffectiveOrder.From(order);
        s.EffectiveOrders[e.OrderId] = effective;

        foreach (var report in BeginOrder(s, f, rng, unit, effective, e.Minute))
            yield return report;
    }

    public IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        var state = StateFor(s);
        if (!state.Routes.TryGetValue(unit.Id, out var route)) yield break;
        if (!unit.IsOnEdge) yield break;
        if (route.Index >= route.Plan.Steps.Count)
        {
            state.Routes.Remove(unit.Id);
            yield break;
        }

        var step = route.Plan.Steps[route.Index];
        if (unit.EdgeFromZoneId != step.FromZoneId || unit.EdgeToZoneId != step.ToZoneId)
        {
            state.Routes.Remove(unit.Id);
            yield break;
        }

        int minute = e.Minute;
        unit.LastMovedMinute = minute;
        if (unit.RemainingMinutes > 0)
            unit.RemainingMinutes--;

        if (unit.RemainingMinutes > 0)
        {
            ScheduleTick(s, unit, minute + 1);
            yield break;
        }

        CompleteEdge(s, unit, step);
        route.Index++;

        if (unit.PendingOrder is { } pending)
        {
            unit.PendingOrder = null;
            state.Routes.Remove(unit.Id);
            var effective = s.EffectiveOrders.TryGetValue(pending.Id, out var prepared)
                ? prepared
                : EffectiveOrder.From(pending);
            s.EffectiveOrders[pending.Id] = effective;
            foreach (var report in BeginOrder(s, f, rng, unit, effective, minute))
                yield return report;
            yield break;
        }

        if (HasEnemyContact(s, unit, unit.LocationZoneId))
        {
            state.Routes.Remove(unit.Id);
            unit.Posture = Posture.Attack;
            ScheduleCombat(s, unit.LocationZoneId, minute);
            yield return StatusReport(
                s, f, rng, unit, minute,
                $"{unit.Name}: контакт в {unit.LocationZoneId}, движение остановлено.",
                ReportKind.Contact,
                $"contact:{unit.LocationZoneId}");
            yield break;
        }

        bool atTarget = route.Index >= route.Plan.Steps.Count;
        if (!atTarget && AbortsOnDeadline(unit, route.Order, minute))
        {
            state.Routes.Remove(unit.Id);
            foreach (var report in FailOrder(s, f, rng, unit, route.Order, minute, "срок прибытия истёк"))
                yield return report;
            yield break;
        }

        if (atTarget)
        {
            state.Routes.Remove(unit.Id);
            unit.Posture = Posture.Defend;
            if (route.Order.Intent != OrderIntent.Hold)
                route.Order.Intent = OrderIntent.Hold;
            unit.CurrentOrderId = null;
            ScheduleArrival(s, unit.Id, unit.LocationZoneId, minute);
            yield break;
        }

        EnterEdge(s, unit, route.Plan.Steps[route.Index], minute);
    }

    public IEnumerable<Report> OnArrive(UnitArrived e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        var state = StateFor(s);
        if (state.Routes.TryGetValue(unit.Id, out var route) && route.Index >= route.Plan.Steps.Count)
            state.Routes.Remove(unit.Id);
        unit.CurrentOrderId = null;
        yield return StatusReport(s, f, rng, unit, e.Minute, $"{unit.Name}: прибыл в {e.ZoneId}.", ReportKind.Arrival);
    }

    public IEnumerable<Report> GenerateReport(ReportDue e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        yield return StatusReport(s, f, rng, unit, e.Minute, $"{unit.Name}: в {unit.LocationZoneId}, силы ~{unit.Strength}%.", e.Kind);
    }

    private List<Report> BeginOrder(
        GameState s,
        FrictionTable f,
        Random rng,
        Unit unit,
        EffectiveOrder order,
        int minute)
    {
        var reports = new List<Report>();
        var state = StateFor(s);
        state.Routes.Remove(unit.Id);

        s.EffectiveOrders[order.SourceOrderId] = order;
        unit.EffectiveOrder = order;
        unit.CurrentOrderId = order.SourceOrderId;

        if (order.ReportBy is int reportAt)
            ScheduleReportDue(s, reportAt, unit.Id, minute);

        if (order.Intent == OrderIntent.Recon)
        {
            unit.Posture = Posture.Defend;
            unit.CurrentOrderId = null;
            reports.Add(StatusReport(s, f, rng, unit, minute, $"{unit.Name}: ведёт наблюдение.", ReportKind.Status));
            return reports;
        }

        var target = order.TargetZoneId;
        bool holdIntent = order.Intent == OrderIntent.Hold;

        if (holdIntent && string.IsNullOrWhiteSpace(target))
        {
            unit.Posture = Posture.Defend;
            unit.CurrentOrderId = null;
            reports.Add(StatusReport(s, f, rng, unit, minute, $"{unit.Name}: удерживает {unit.LocationZoneId}."));
            return reports;
        }

        if (string.IsNullOrWhiteSpace(target) || !s.Zones.ContainsKey(target))
        {
            reports.AddRange(FailOrder(s, f, rng, unit, order, minute, "нет доступной цели"));
            return reports;
        }

        if (!unit.IsOnEdge && target == unit.LocationZoneId)
        {
            if (HasEnemyContact(s, unit, target))
            {
                unit.Posture = Posture.Attack;
                ScheduleCombat(s, target, minute);
                reports.Add(StatusReport(
                    s, f, rng, unit, minute,
                    $"{unit.Name}: цель {target} занята противником.",
                    ReportKind.Contact,
                    $"contact:{target}"));
            }
            else
            {
                unit.Posture = Posture.Defend;
                order.Intent = OrderIntent.Hold;
                reports.Add(StatusReport(
                    s, f, rng, unit, minute,
                    $"{unit.Name}: уже в {target}, удерживает.",
                    ReportKind.Arrival));
            }
            unit.CurrentOrderId = null;
            return reports;
        }

        if (AbortsOnDeadline(unit, order, minute))
        {
            reports.AddRange(FailOrder(s, f, rng, unit, order, minute, "срок прибытия истёк"));
            return reports;
        }

        var forbidden = order.ForbiddenZoneId;
        if (forbidden is not null && forbidden == target)
        {
            if (unit.Traits.Pedantry >= unit.Traits.Init)
            {
                reports.AddRange(FailOrder(s, f, rng, unit, order, minute, $"цель {target} запрещена"));
                return reports;
            }
            forbidden = null;
            order.ForbiddenZoneId = null;
        }

        if (!PathPlanner.TryPlan(s, unit.LocationZoneId, target, order.Deadline, forbidden, out var plan) || plan.IsEmpty)
        {
            if (forbidden is not null && unit.Traits.Init > unit.Traits.Pedantry)
            {
                order.ForbiddenZoneId = null;
                if (!PathPlanner.TryPlan(s, unit.LocationZoneId, target, order.Deadline, null, out plan) || plan.IsEmpty)
                {
                    reports.AddRange(FailOrder(s, f, rng, unit, order, minute, $"нет маршрута в {target}"));
                    return reports;
                }
            }
            else
            {
                reports.AddRange(FailOrder(s, f, rng, unit, order, minute, $"нет маршрута в {target}"));
                return reports;
            }
        }

        state.Routes[unit.Id] = new RouteState(order, plan, 0);
        EnterEdge(s, unit, plan.Steps[0], minute);
        reports.Add(StatusReport(
            s, f, rng, unit, minute,
            $"{unit.Name}: выступает в {target} через {plan.Steps.Count} переход(ов)."));
        return reports;
    }

    private void EnterEdge(GameState s, Unit unit, RouteStep step, int minute)
    {
        if (s.Zones.TryGetValue(step.FromZoneId, out var from))
            from.PresentUnitIds.Remove(unit.Id);

        unit.LocationZoneId = step.FromZoneId;
        unit.IsOnEdge = true;
        unit.EdgeFromZoneId = step.FromZoneId;
        unit.EdgeToZoneId = step.ToZoneId;
        unit.EntryEdgeType = step.EdgeType;
        unit.RemainingMinutes = step.Minutes;
        unit.Posture = Posture.March;
        unit.CurrentOrderId = unit.EffectiveOrder?.SourceOrderId;
        ScheduleTick(s, unit, minute + 1);
    }

    private void CompleteEdge(GameState s, Unit unit, RouteStep step)
    {
        if (s.Zones.TryGetValue(step.FromZoneId, out var from))
            from.PresentUnitIds.Remove(unit.Id);
        if (s.Zones.TryGetValue(step.ToZoneId, out var to) && !to.PresentUnitIds.Contains(unit.Id))
            to.PresentUnitIds.Add(unit.Id);

        unit.LocationZoneId = step.ToZoneId;
        unit.IsOnEdge = false;
        unit.EdgeFromZoneId = null;
        unit.EdgeToZoneId = null;
        unit.RemainingMinutes = 0;

        if (step.EdgeType == EdgeType.Offroad)
            unit.Readiness = ClampCondition(unit.Readiness - (int)_rules.OffroadReadinessPenalty);
    }

    private List<Report> FailOrder(
        GameState s,
        FrictionTable f,
        Random rng,
        Unit unit,
        EffectiveOrder order,
        int minute,
        string reason)
    {
        StateFor(s).Routes.Remove(unit.Id);
        order.Outcome = "Failed/Rejected";
        unit.EffectiveOrder = null;
        unit.EffectiveOrderId = null;
        unit.CurrentOrderId = null;
        unit.PendingOrder = null;
        unit.Posture = Posture.Defend;
        return
        [
            StatusReport(
                s, f, rng, unit, minute,
                $"{unit.Name}: не может выполнить приказ ({reason}).",
                ReportKind.Impossible,
                $"impossible:{unit.Id}")
        ];
    }

    private static bool AbortsOnDeadline(Unit unit, EffectiveOrder order, int minute) =>
        order.Deadline is int deadline &&
        minute > deadline &&
        unit.Traits.Pedantry >= 2 &&
        unit.Traits.Init <= 1;

    private static void ScheduleTick(GameState s, Unit unit, int minute)
    {
        int at = EffectiveMinute(s, minute);
        s.Clock.Schedule(new UnitMoved(at, unit.Id, unit.EdgeToZoneId ?? string.Empty), at, EventPhase.Movement);
    }

    private static void ScheduleArrival(GameState s, string unitId, string zoneId, int minute)
    {
        int at = EffectiveMinute(s, minute);
        s.Clock.Schedule(new UnitArrived(at, unitId, zoneId), at, EventPhase.Movement);
    }

    private static void ScheduleCombat(GameState s, string zoneId, int minute)
    {
        int tick = minute % CombatTurnMinutes == 0
            ? minute
            : ((minute / CombatTurnMinutes) + 1) * CombatTurnMinutes;
        if (s.Clock.PendingKeys.Any(key => key.Minute == tick && key.Phase == EventPhase.Combat)) return;
        s.Clock.Schedule(new CombatTick(tick, zoneId), tick, EventPhase.Combat);
    }

    private static void ScheduleReportDue(GameState s, int minute, string unitId, int earliestMinute)
    {
        int at = Math.Max(minute, earliestMinute);
        s.Clock.Schedule(new ReportDue(at, unitId, ReportKind.Status), at, EventPhase.ReportCreate);
    }

    private static int EffectiveMinute(GameState s, int minute) => Math.Max(minute, s.Clock.Now);

    private static bool HasEnemyContact(GameState s, Unit unit, string zoneId) =>
        s.Zones.TryGetValue(zoneId, out var zone) &&
        zone.PresentUnitIds.Any(id => id != unit.Id &&
                                     s.Units.TryGetValue(id, out var other) &&
                                     !other.Captured &&
                                     other.Strength > 0 &&
                                     unit.IsEnemyOf(other.Faction));

    private static int ClampCondition(int value) => Math.Clamp(value, 0, MaxConditionValue);

    private Report StatusReport(
        GameState s,
        FrictionTable f,
        Random rng,
        Unit unit,
        int minute,
        string text,
        ReportKind kind = ReportKind.Status,
        string? factKey = null)
    {
        int jitter = Math.Max(0, f.ReportJitterMinutes);
        int delivery = minute + Math.Max(0, f.ReportBaseDelayMinutes) + rng.Next(0, jitter + 1);
        int confidence = kind switch
        {
            ReportKind.Arrival or ReportKind.Impossible or ReportKind.Recon => 3,
            _ => 2
        };
        var report = _reports.Create(
            minute,
            delivery,
            unit.Id,
            _rules.ClampConfidence(confidence),
            kind,
            text,
            factKey,
            unit.Faction,
            unit.CommanderLost ? $"{unit.Id}-XO" : null);
        s.InTransitReports.Add(report);
        s.Clock.Schedule(new ReportDelivered(delivery, report.Id, unit.Id, report),
            EffectiveMinute(s, delivery), EventPhase.ReportDeliver);
        return report;
    }

    private ExecutionState StateFor(GameState s) => _states.GetValue(s, _ => new ExecutionState());

    private sealed class ExecutionState
    {
        public Dictionary<string, RouteState> Routes { get; } = new();
    }

    private sealed class RouteState
    {
        public RouteState(EffectiveOrder order, RoutePlan plan, int index)
        {
            Order = order;
            Plan = plan;
            Index = index;
        }

        public EffectiveOrder Order { get; }
        public RoutePlan Plan { get; }
        public int Index { get; set; }
    }

    private sealed class DefaultGameRules : IGameRules
    {
        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }

    private sealed class DefaultReportFactory : IReportFactory
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
            Report.Create(eventMinute, deliveryMinute, sourceId, confidence, kind, text, factKey,
                sourceFaction, reportedBy, facts);
    }
}
