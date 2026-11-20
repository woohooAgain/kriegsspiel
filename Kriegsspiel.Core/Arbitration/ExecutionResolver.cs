using System.Runtime.CompilerServices;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class ExecutionResolver : IExecutionResolver
{
    private const int MinReactionDelayMinutes = 30;
    private const int MaxReactionDelayMinutes = 60;
    private const int AttentionShiftReadinessDelta = -2;
    private const int MaxConditionValue = 100;
    private const string HeadquartersId = "HQ";

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

        var effective = s.EffectiveOrders.TryGetValue(e.OrderId, out var existing)
            ? existing
            : EffectiveOrder.From(order);
        s.EffectiveOrders[e.OrderId] = effective;
        unit.EffectiveOrder = effective;
        unit.CurrentOrderId = e.OrderId;
        unit.Posture = PostureFor(effective);

        var target = effective.TargetZoneId ?? order.TargetZoneId;
        if (effective.ReportBy is int reportAt)
            ScheduleReportDue(s, reportAt, unit.Id, e.Minute);

        if (!RequiresMovement(effective.Intent))
        {
            StateFor(s).Routes.Remove(unit.Id);
            yield return StatusReport(s, f, rng, unit, e.Minute, $"{unit.Name}: {unit.LocationZoneId}, задача {effective.Intent}.");
            yield break;
        }

        if (string.IsNullOrWhiteSpace(target) || !s.Zones.ContainsKey(target))
        {
            foreach (var report in Impossible(s, f, rng, unit, effective, e.Minute, "нет доступной цели", null))
                yield return report;
            yield break;
        }

        if (target == unit.LocationZoneId)
        {
            StateFor(s).Routes.Remove(unit.Id);
            unit.CurrentOrderId = null;
            yield return StatusReport(s, f, rng, unit, e.Minute, $"{unit.Name}: уже в {target}.", ReportKind.Arrival);
            yield break;
        }

        if (!PathPlanner.TryPlan(s, unit.LocationZoneId, target, effective.MovementMode, f, out var plan))
        {
            foreach (var report in Impossible(s, f, rng, unit, effective, e.Minute, $"нет маршрута в {target}", null))
                yield return report;
            yield break;
        }

        if (effective.NotLaterThan is int deadline && e.Minute + plan.TotalMinutes > deadline)
        {
            foreach (var report in Impossible(s, f, rng, unit, effective, e.Minute, "срок прибытия истёк", null))
                yield return report;
            yield break;
        }

        var state = StateFor(s);
        state.Routes[unit.Id] = new RouteState(effective, plan, 0, e.Minute);
        ScheduleStep(s, unit.Id, plan.Steps[0].ToZoneId, e.Minute);
        yield return StatusReport(s, f, rng, unit, e.Minute, $"{unit.Name}: выступает в {target} через {plan.Steps.Count} переход(ов).");
    }

    public IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        var state = StateFor(s);
        if (!state.Routes.TryGetValue(unit.Id, out var route))
        {
            var fallback = unit.EffectiveOrder ?? EffectiveOrder.From(new Order
            {
                ToUnitId = unit.Id,
                Intent = OrderIntent.Move,
                TargetZoneId = e.ToZoneId
            });
            if (!PathPlanner.TryPlan(s, unit.LocationZoneId, e.ToZoneId, fallback.MovementMode, f, out var direct))
                yield break;
            route = new RouteState(fallback, direct, 0, e.Minute);
            state.Routes[unit.Id] = route;
        }

        if (route.Index >= route.Plan.Steps.Count) yield break;
        var step = route.Plan.Steps[route.Index];
        if (step.ToZoneId != e.ToZoneId || unit.LocationZoneId != step.FromZoneId) yield break;

        if (!PathPlanner.IsEdgePassable(s, step.FromZoneId, step.ToZoneId, route.BlockedEdges))
        {
            foreach (var report in Impossible(s, f, rng, unit, route.Order, e.Minute, $"ребро {step.EdgeId} непроходимо", route))
                yield return report;
            yield break;
        }

        ApplyStep(s, unit, step);
        route.Index++;
        int minute = EffectiveMinute(s, e.Minute);
        bool atTarget = route.Index >= route.Plan.Steps.Count;
        bool contact = HasEnemyContact(s, unit, step.ToZoneId);
        if (atTarget || contact)
            s.Clock.Schedule(new UnitArrived(minute, unit.Id, step.ToZoneId), minute, EventPhase.Movement);

        if (contact)
        {
            state.Routes.Remove(unit.Id);
            s.Clock.Schedule(new CombatTick(minute, step.ToZoneId), minute, EventPhase.Combat);
            yield return StatusReport(
                s, f, rng, unit, minute,
                $"{unit.Name}: контакт в {step.ToZoneId}, до цели не дошёл.",
                ReportKind.Contact,
                $"contact:{step.ToZoneId}");
            yield break;
        }

        if (route.Index >= route.Plan.Steps.Count) yield break;

        var next = route.Plan.Steps[route.Index];
        int arrival = minute + next.Minutes + route.RemainingMinutesAfter(route.Index + 1);
        if (route.Order.NotLaterThan is int deadline && arrival > deadline)
        {
            foreach (var report in Impossible(s, f, rng, unit, route.Order, minute, "опоздание к сроку", route))
                yield return report;
            yield break;
        }
        ScheduleStep(s, unit.Id, next.ToZoneId, minute + next.Minutes);
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

    private static void ScheduleStep(GameState s, string unitId, string toZoneId, int minute)
    {
        int at = EffectiveMinute(s, minute);
        s.Clock.Schedule(new UnitMoved(at, unitId, toZoneId), at, EventPhase.Movement);
    }

    private static void ScheduleReportDue(GameState s, int minute, string unitId, int earliestMinute)
    {
        int at = Math.Max(minute, earliestMinute);
        s.Clock.Schedule(new ReportDue(at, unitId, ReportKind.Status), at, EventPhase.ReportCreate);
    }

    private static int EffectiveMinute(GameState s, int minute) => Math.Max(minute, s.Clock.Now);

    private static Posture PostureFor(EffectiveOrder order) => order.MovementMode switch
    {
        MovementMode.Contact => Posture.Attack,
        _ => order.Intent switch
        {
            OrderIntent.Hold or OrderIntent.Reserve or OrderIntent.Guard => Posture.Defend,
            OrderIntent.Withdraw => Posture.Withdraw,
            _ => Posture.March
        }
    };

    private static bool RequiresMovement(OrderIntent intent) =>
        intent is not (OrderIntent.Hold or OrderIntent.Reserve or OrderIntent.Guard);

    private static bool HasEnemyContact(GameState s, Unit unit, string zoneId) =>
        s.Zones.TryGetValue(zoneId, out var zone) &&
        zone.PresentUnitIds.Any(id => id != unit.Id &&
                                     s.Units.TryGetValue(id, out var other) &&
                                     !other.Captured &&
                                     unit.IsEnemyOf(other.Faction));

    private static void ApplyStep(GameState s, Unit unit, RouteStep step)
    {
        if (s.Zones.TryGetValue(unit.LocationZoneId, out var from))
            from.PresentUnitIds.Remove(unit.Id);
        if (s.Zones.TryGetValue(step.ToZoneId, out var to) && !to.PresentUnitIds.Contains(unit.Id))
            to.PresentUnitIds.Add(unit.Id);

        if (unit.AttentionEdgeId is not null && unit.AttentionEdgeId != step.EdgeId)
            unit.Readiness = ClampCondition(unit.Readiness + AttentionShiftReadinessDelta);
        unit.AttentionEdgeId = step.EdgeId;
        unit.MoveEdgeId = step.EdgeId;
        unit.Readiness = ClampCondition(unit.Readiness + step.ReadinessDelta);
        unit.Fatigue = ClampCondition(unit.Fatigue + step.FatigueDelta);
        unit.LocationZoneId = step.ToZoneId;
    }

    private static int ClampCondition(int value) => Math.Clamp(value, 0, MaxConditionValue);

    private IEnumerable<Report> Impossible(
        GameState s,
        FrictionTable f,
        Random rng,
        Unit unit,
        EffectiveOrder order,
        int minute,
        string reason,
        RouteState? route)
    {
        var state = StateFor(s);
        state.Routes.Remove(unit.Id);
        unit.Posture = order.MovementMode == MovementMode.Contact ? Posture.Attack : Posture.Defend;
        var reaction = ReactionFor(unit.Traits);

        if (reaction == ImpossibleReaction.Improvise &&
            TryImprovise(s, f, unit, order, minute, route, out var detour, out var note))
        {
            state.Routes[unit.Id] = detour;
            ScheduleStep(s, unit.Id, detour.Plan.Steps[0].ToZoneId, minute);
            yield return StatusReport(s, f, rng, unit, minute, note, factKey: $"route:{unit.Id}");
            yield break;
        }

        int delay = MinReactionDelayMinutes + rng.Next(0, MaxReactionDelayMinutes - MinReactionDelayMinutes + 1);
        if (reaction == ImpossibleReaction.Query)
        {
            s.Clock.Schedule(new QuerySent(minute, unit.Id, HeadquartersId), EffectiveMinute(s, minute), EventPhase.Scheduled);
            s.Clock.Schedule(new ReportDue(minute + delay, unit.Id, ReportKind.Impossible),
                EffectiveMinute(s, minute + delay), EventPhase.ReportCreate);
        }

        string action = reaction switch
        {
            ImpossibleReaction.Query => $"; запрос уточнения, пауза {delay} мин.",
            ImpossibleReaction.LiteralStop => "; буквальное исполнение: встал, ответа не ждёт.",
            _ => "; обход и запасная цель недоступны, запрос не отправлен."
        };
        yield return StatusReport(
            s, f, rng, unit, minute,
            $"{unit.Name}: не может выполнить приказ ({reason}){action}",
            ReportKind.Impossible,
            $"impossible:{unit.Id}");
    }

    private static bool TryImprovise(
        GameState s,
        FrictionTable f,
        Unit unit,
        EffectiveOrder order,
        int minute,
        RouteState? previous,
        out RouteState detour,
        out string note)
    {
        detour = null!;
        note = string.Empty;
        if (string.IsNullOrWhiteSpace(order.TargetZoneId)) return false;

        var blocked = new HashSet<string>(previous?.BlockedEdges ?? []);
        if (previous is not null && previous.Index < previous.Plan.Steps.Count)
            blocked.Add(previous.Plan.Steps[previous.Index].EdgeId);

        if (PathPlanner.TryPlan(s, unit.LocationZoneId, order.TargetZoneId, order.MovementMode, f, out var bypass, blocked) &&
            !bypass.IsEmpty && FitsDeadline(bypass, order, minute))
        {
            detour = new RouteState(order, bypass, 0, minute, blocked);
            note = $"{unit.Name}: обходит препятствие через {string.Join("→", bypass.Steps.Select(x => x.ToZoneId))}.";
            return true;
        }

        if (!s.Zones.TryGetValue(order.TargetZoneId, out var targetZone)) return false;
        RoutePlan? alternate = null;
        foreach (var neighbor in targetZone.TravelMinutes.Keys)
        {
            if (neighbor == unit.LocationZoneId) continue;
            if (!PathPlanner.TryPlan(s, unit.LocationZoneId, neighbor, order.MovementMode, f, out var candidate, blocked) ||
                candidate.IsEmpty || !FitsDeadline(candidate, order, minute)) continue;
            if (alternate is null || candidate.TotalMinutes < alternate.TotalMinutes)
                alternate = candidate;
        }
        if (alternate is null) return false;

        detour = new RouteState(order, alternate, 0, minute, blocked);
        note = $"{unit.Name}: выбирает запасную цель {alternate.TargetZoneId}.";
        return true;
    }

    private static bool FitsDeadline(RoutePlan plan, EffectiveOrder order, int minute) =>
        order.NotLaterThan is not int deadline || minute + plan.TotalMinutes <= deadline;

    private static ImpossibleReaction ReactionFor(CommanderTraits traits)
    {
        if (traits.Caution >= 2 && traits.Initiative <= 1) return ImpossibleReaction.Query;
        if (traits.Literalism >= 2 && traits.Caution <= 1) return ImpossibleReaction.LiteralStop;
        if (traits.Initiative >= 2 && traits.Literalism <= 1) return ImpossibleReaction.Improvise;
        if (traits.Caution >= 2) return ImpossibleReaction.Query;
        if (traits.Literalism >= 2) return ImpossibleReaction.LiteralStop;
        if (traits.Initiative >= 2) return ImpossibleReaction.Improvise;
        return ImpossibleReaction.Query;
    }

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
        int confidence = kind == ReportKind.Impossible && unit.Traits.ReportHonesty < 2 ? 1 : 2;
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

    private enum ImpossibleReaction
    {
        Query,
        LiteralStop,
        Improvise
    }

    private sealed class ExecutionState
    {
        public Dictionary<string, RouteState> Routes { get; } = new();
    }

    private sealed class RouteState
    {
        public RouteState(
            EffectiveOrder order,
            RoutePlan plan,
            int index,
            int startedAt,
            HashSet<string>? blockedEdges = null)
        {
            Order = order;
            Plan = plan;
            Index = index;
            StartedAt = startedAt;
            BlockedEdges = blockedEdges ?? [];
        }

        public EffectiveOrder Order { get; }
        public RoutePlan Plan { get; }
        public int Index { get; set; }
        public int StartedAt { get; }
        public HashSet<string> BlockedEdges { get; }

        public int RemainingMinutesAfter(int index) =>
            Plan.Steps.Skip(index).Sum(step => step.Minutes);
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
