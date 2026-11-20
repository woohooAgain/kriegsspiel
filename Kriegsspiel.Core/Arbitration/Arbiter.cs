using System.Runtime.CompilerServices;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;

namespace Kriegsspiel.Core.Arbitration;

public sealed class Arbiter
{
    private sealed class ArbiterState
    {
        public readonly HashSet<string> DeliveredReports = new(StringComparer.Ordinal);
        public readonly HashSet<string> KnownInTransit = new(StringComparer.Ordinal);
        public readonly Dictionary<string, (int Minute, bool Lost)> ScheduledOrderDeliveries = new(StringComparer.Ordinal);
        public readonly HashSet<string> DispatchedOrders = new(StringComparer.Ordinal);
    }

    private readonly ConditionalWeakTable<GameState, ArbiterState> _states = new();
    private readonly FrictionTable _f;
    private readonly Random _rng;
    private readonly ITransmissionResolver _transmission;
    private readonly IUnderstandingResolver _understanding;
    private readonly IExecutionResolver _execution;
    private readonly ICombatResolver _combat;
    private readonly IKnowledgeUpdater _knowledge;
    private readonly IReportFactory _reports;

    public Arbiter(FrictionTable f, Random rng,
                   ITransmissionResolver t, IUnderstandingResolver u,
                   IExecutionResolver ex, ICombatResolver c)
        : this(f, rng, t, u, ex, c, new KnowledgeUpdater(), new ReportFactory(f))
    {
    }

    public Arbiter(FrictionTable f, Random rng,
                   ITransmissionResolver t, IUnderstandingResolver u,
                   IExecutionResolver ex, ICombatResolver c,
                   IKnowledgeUpdater knowledge, IReportFactory reports)
    {
        _f = f;
        _rng = rng;
        _transmission = t;
        _understanding = u;
        _execution = ex;
        _combat = c;
        _knowledge = knowledge;
        _reports = reports;
    }

    public TurnResult AdvanceTo(int target, GameState state, IReadOnlyList<Orders.Order> newOrders)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(newOrders);
        if (target < state.Clock.Now)
            throw new ArgumentOutOfRangeException(nameof(target), target, "Target minute is before Now.");

        var astate = _states.GetValue(state, _ => new ArbiterState());
        var deliveredNow = new List<Report>();

        _transmission.EnsureCouriers(state, _f);
        SyncCourierState(state, astate);

        foreach (var o in newOrders)
        {
            if (!state.Orders.Any(existing => existing.Id == o.Id))
                state.Orders.Add(o);
            ScheduleOrderTransmission(state, o, astate);
        }

        SyncCourierState(state, astate);

        foreach (var evt in state.Clock.AdvanceTo(target))
        {
            state.EventLog.Add(evt);
            SyncCourierState(state, astate);
            var produced = Dispatch(evt, state, target, astate, deliveredNow);
            HandleProducedReports(produced, state, target, astate, deliveredNow);
            ApplyEventSideEffects(evt, state, astate);
            SyncCourierState(state, astate);
        }

        SyncCourierState(state, astate);
        FinalizeBoundaryDeliveries(state, target, astate, deliveredNow);

        return new TurnResult(deliveredNow, state.Clock.Now);
    }

    private IEnumerable<Report> Dispatch(
        GameEvent e,
        GameState s,
        int target,
        ArbiterState astate,
        List<Report> deliveredNow)
    {
        switch (e)
        {
            case OrderDelivered d:
                return _understanding.Resolve(d, s, _f, _rng);
            case OrderAccepted a:
                return _execution.Start(a, s, _f, _rng);
            case UnitMoved m:
                var moved = _execution.OnMove(m, s, _f, _rng).ToList();
                AfterUnitStep(s, m.UnitId, m.Minute);
                return moved;
            case UnitArrived ar:
                var arrived = _execution.OnArrive(ar, s, _f, _rng).ToList();
                AfterUnitStep(s, ar.UnitId, ar.Minute);
                return arrived;
            case ReportDue r:
                return _execution.GenerateReport(r, s, _f, _rng);
            case CombatTick c:
                return _combat.Resolve(c, s, _f, _rng);
            case ReportDelivered rd:
                DeliverReportEvent(rd, s, target, astate, deliveredNow);
                return Array.Empty<Report>();
            case PendingCasualties pc:
                ApplyPendingCasualties(pc, s);
                return Array.Empty<Report>();
            case QueryAnswered qa:
                if (qa.Report is not null)
                    DeliverOrTrackReport(qa.Report, s, target, astate, deliveredNow, ensureScheduled: false);
                return Array.Empty<Report>();
            default:
                return Array.Empty<Report>();
        }
    }

    private void ApplyEventSideEffects(GameEvent evt, GameState state, ArbiterState astate)
    {
        switch (evt)
        {
            case OrderDelivered od:
                astate.DispatchedOrders.Add(od.OrderId);
                break;
            case OrderLost ol:
                astate.DispatchedOrders.Add(ol.OrderId);
                break;
        }
    }

    private void AfterUnitStep(GameState state, string unitId, int minute)
    {
        if (!state.Units.TryGetValue(unitId, out var unit)) return;
        if (!state.Zones.TryGetValue(unit.LocationZoneId, out var zone)) return;

        bool contact = zone.PresentUnitIds.Any(id =>
            id != unit.Id &&
            state.Units.TryGetValue(id, out var other) &&
            !other.Captured &&
            other.Strength > 0 &&
            unit.IsEnemyOf(other.Faction));

        if (!contact) return;

        int tickMinute = NextCombatTickMinute(minute);
        if (!state.Clock.PendingKeys.Any(k => k.Minute == tickMinute && k.Phase == EventPhase.Combat))
            state.Clock.Schedule(new CombatTick(tickMinute, zone.Id), tickMinute, EventPhase.Combat);
    }

    private static int NextCombatTickMinute(int minute)
    {
        if (minute % 30 == 0) return minute;
        return ((minute / 30) + 1) * 30;
    }

    private void ApplyPendingCasualties(PendingCasualties evt, GameState state)
    {
        var entries = state.PendingCasualties
            .Where(p => !p.Applied && p.UnitId == evt.UnitId && p.Minute == evt.Minute)
            .ToList();
        if (entries.Count == 0)
        {
            if (state.Units.TryGetValue(evt.UnitId, out var unitOnly))
                unitOnly.Strength = Math.Max(0, unitOnly.Strength - Math.Max(0, evt.Amount));
            return;
        }

        foreach (var entry in entries)
        {
            if (state.Units.TryGetValue(entry.UnitId, out var unit))
                unit.Strength = Math.Max(0, unit.Strength - entry.Amount);
            entry.Applied = true;
        }
    }

    private void ScheduleOrderTransmission(GameState state, Orders.Order order, ArbiterState astate)
    {
        var result = _transmission.Transmit(order, state, _f, _rng);
        SyncCourierState(state, astate);

        if (TryScheduleFromCourier(state, order.Id, astate))
            return;

        int at = Math.Max(state.Clock.Now, state.Clock.Now + Math.Max(1, result.DelayMinutes));
        if (astate.ScheduledOrderDeliveries.ContainsKey(order.Id)) return;
        astate.ScheduledOrderDeliveries[order.Id] = (at, result.Lost);
        state.Clock.Schedule(result.Lost ? new OrderLost(at, order.Id) : new OrderDelivered(at, order.Id), at, EventPhase.Delivery);
    }

    private bool TryScheduleFromCourier(GameState state, string orderId, ArbiterState astate)
    {
        var courier = state.Couriers.FirstOrDefault(c => c.AssignedOrderId == orderId);
        if (courier is null || courier.ExpectedDeliveryMinute is not int at)
            return false;
        if (astate.ScheduledOrderDeliveries.TryGetValue(orderId, out var existing))
        {
            if (existing.Minute == at && existing.Lost == courier.LostOnOutbound)
                return true;
            return true;
        }

        bool lost = courier.LostOnOutbound;
        astate.ScheduledOrderDeliveries[orderId] = (at, lost);
        state.Clock.Schedule(lost ? new OrderLost(at, orderId) : new OrderDelivered(at, orderId), at, EventPhase.Delivery);
        return true;
    }

    private void SyncCourierState(GameState state, ArbiterState astate)
    {
        _transmission.UpdateTo(state, state.Clock.Now, _rng);
        foreach (var courier in state.Couriers)
        {
            if (string.IsNullOrWhiteSpace(courier.AssignedOrderId)) continue;
            TryScheduleFromCourier(state, courier.AssignedOrderId, astate);
        }
    }

    private void HandleProducedReports(
        IEnumerable<Report> produced,
        GameState state,
        int target,
        ArbiterState astate,
        List<Report> deliveredNow)
    {
        foreach (var report in produced)
            DeliverOrTrackReport(report, state, target, astate, deliveredNow, ensureScheduled: true);
    }

    private void DeliverReportEvent(ReportDelivered evt, GameState state, int target, ArbiterState astate, List<Report> deliveredNow)
    {
        Report? report = evt.Payload;
        if (report is null)
            report = state.InTransitReports.FirstOrDefault(r => r.Id == evt.ReportId)
                  ?? state.DeliveredReports.FirstOrDefault(r => r.Id == evt.ReportId);
        if (report is null) return;
        DeliverOrTrackReport(report, state, target, astate, deliveredNow, ensureScheduled: false);
    }

    private void DeliverOrTrackReport(
        Report report,
        GameState state,
        int target,
        ArbiterState astate,
        List<Report> deliveredNow,
        bool ensureScheduled)
    {
        if (report is null) return;

        if (report.ArrivesAt <= state.Clock.Now || report.ArrivesAt <= target)
        {
            if (report.ArrivesAt > target)
            {
                TrackInTransit(report, state, astate, ensureScheduled);
                return;
            }

            if (astate.DeliveredReports.Add(report.Id))
            {
                state.InTransitReports.RemoveAll(r => r.Id == report.Id);
                if (!state.DeliveredReports.Any(r => r.Id == report.Id))
                    state.DeliveredReports.Add(report);
                try { _knowledge.Apply(state.Knowledge, report); } catch { }
                deliveredNow.Add(report);
            }
            return;
        }

        TrackInTransit(report, state, astate, ensureScheduled);
    }

    private void TrackInTransit(Report report, GameState state, ArbiterState astate, bool ensureScheduled)
    {
        if (!state.InTransitReports.Any(r => r.Id == report.Id) && !astate.DeliveredReports.Contains(report.Id))
            state.InTransitReports.Add(report);

        if (!ensureScheduled) return;
        int at = Math.Max(state.Clock.Now, report.ArrivesAt);
        if (astate.KnownInTransit.Add(report.Id))
            state.Clock.Schedule(new ReportDelivered(at, report.Id, report.FromUnitId, report), at, EventPhase.ReportDeliver);
    }

    private void FinalizeBoundaryDeliveries(GameState state, int target, ArbiterState astate, List<Report> deliveredNow)
    {
        foreach (var report in state.InTransitReports.Where(r => r.ArrivesAt <= target).ToList())
            DeliverOrTrackReport(report, state, target, astate, deliveredNow, ensureScheduled: false);
    }
}
