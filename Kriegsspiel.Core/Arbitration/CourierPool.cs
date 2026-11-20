using System.Runtime.CompilerServices;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public enum CourierMessageKind
{
    Order = 0,
    Query = 1
}

public sealed record CourierDispatchResult(
    int DispatchMinute,
    int DeliveryMinute,
    int ReturnMinute,
    bool OutboundLost,
    bool ReturnLost,
    bool DeliveryConfirmed,
    string CourierId);

public sealed class CourierPool
{
    private sealed class QueuedMessage
    {
        public required CourierMessageKind Kind { get; init; }
        public required string MessageId { get; init; }
        public string? FromUnitId { get; init; }
        public string? ToUnitId { get; init; }
        public required int EnqueuedMinute { get; init; }
        public required long Sequence { get; init; }
        public required int OutboundTime { get; init; }
        public required int ReturnTime { get; init; }
        public required double OutboundLossChance { get; init; }
        public required double ReturnLossChance { get; init; }
        public bool ReturnKnownOnDispatch { get; init; }
        public bool ForcedReturnLost { get; init; }
    }

    private sealed class PoolState
    {
        public long Sequence;
        public readonly List<QueuedMessage> Queue = new();
        public readonly HashSet<string> ReturnedOrLostApplied = new(StringComparer.Ordinal);
    }

    private readonly ConditionalWeakTable<GameState, PoolState> _states = new();

    public TransmissionResult TransmitOrder(Order order, GameState state, FrictionTable friction, Random rng)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(friction);
        ArgumentNullException.ThrowIfNull(rng);

        int now = state.Clock.Now;
        EnsureCouriers(state, friction);
        UpdateTo(state, now);

        var (outboundTime, returnTime, outboundLoss, returnLoss) = ComputeParameters(order, state, friction);
        var queued = new QueuedMessage
        {
            Kind = CourierMessageKind.Order,
            MessageId = order.Id,
            FromUnitId = ResolveSenderId(state),
            ToUnitId = order.ToUnitId,
            EnqueuedMinute = now,
            Sequence = NextSequence(state),
            OutboundTime = outboundTime,
            ReturnTime = returnTime,
            OutboundLossChance = outboundLoss,
            ReturnLossChance = returnLoss
        };

        Enqueue(state, queued);
        var dispatch = TryDispatchAvailable(state, rng, now);
        var result = dispatch.FirstOrDefault(d => d.MessageId == order.Id);

        if (result is null)
        {
            int etaDispatch = EstimateDispatchMinute(state, queued, now);
            int delay = Math.Max(1, (etaDispatch + outboundTime) - now);
            return new TransmissionResult(delay, false);
        }

        if (result.OutboundLost)
            return new TransmissionResult(Math.Max(1, result.DeliveryMinute - now), true);

        return new TransmissionResult(Math.Max(1, result.DeliveryMinute - now), false);
    }

    public CourierDispatchResult? TransmitQuery(
        string queryId,
        string requesterId,
        string targetId,
        GameState state,
        FrictionTable friction,
        Random rng,
        int? outboundTimeOverride = null,
        int? returnTimeOverride = null,
        double? outboundLossChance = null,
        double? returnLossChance = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(queryId);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(friction);
        ArgumentNullException.ThrowIfNull(rng);

        int now = state.Clock.Now;
        EnsureCouriers(state, friction);
        UpdateTo(state, now);

        int distance = GraphDistance(state, ResolveZoneForUnit(state, requesterId), ResolveZoneForUnit(state, targetId));
        int outboundTime = Math.Max(1, outboundTimeOverride ?? Math.Max(1, friction.Hq.DefaultDeliveryTimeMinutes));
        int returnTime = Math.Max(1, returnTimeOverride ?? Math.Max(1, friction.Hq.DefaultReturnTimeMinutes));
        double pOut = Clamp01(outboundLossChance ?? (friction.Hq.CourierLossBase + friction.Hq.CourierLossPerKm * distance));
        double pRet = Clamp01(returnLossChance ?? friction.Hq.CourierLossBase);

        var queued = new QueuedMessage
        {
            Kind = CourierMessageKind.Query,
            MessageId = queryId,
            FromUnitId = requesterId,
            ToUnitId = targetId,
            EnqueuedMinute = now,
            Sequence = NextSequence(state),
            OutboundTime = outboundTime,
            ReturnTime = returnTime,
            OutboundLossChance = pOut,
            ReturnLossChance = pRet
        };

        Enqueue(state, queued);
        state.Clock.Schedule(new QuerySent(now, requesterId, targetId), now);
        var dispatches = TryDispatchAvailable(state, rng, now);
        var sent = dispatches.FirstOrDefault(d => d.MessageId == queryId);
        if (sent is null) return null;
        return ToPublicResult(sent);
    }

    public void UpdateTo(GameState state, int minute) => UpdateTo(state, minute, null);

    public void UpdateTo(GameState state, int minute, Random? rng)
    {
        ArgumentNullException.ThrowIfNull(state);
        EnsureState(state);
        var poolState = _states.GetValue(state, _ => new PoolState());
        foreach (var courier in state.Couriers.ToList())
        {
            if (courier.State == CourierState.Delivering && courier.ExpectedDeliveryMinute is int deliveryAt && deliveryAt <= minute)
            {
                if (courier.LostOnOutbound)
                {
                    courier.State = CourierState.Lost;
                    courier.DeliveryConfirmed = false;
                }
                else
                {
                    courier.State = CourierState.Returning;
                }
            }

            if (courier.State == CourierState.Returning && courier.ExpectedReturnMinute is int returnAt && returnAt <= minute)
            {
                if (courier.LostOnReturn)
                {
                    courier.State = CourierState.Lost;
                    courier.DeliveryConfirmed = false;
                }
                else
                {
                    courier.State = CourierState.Returned;
                    courier.DeliveryConfirmed = true;
                }
            }

            if (courier.State == CourierState.Lost)
            {
                if (courier.ReplacementMinute is int replacementAt && replacementAt <= minute)
                {
                    if (poolState.ReturnedOrLostApplied.Add(courier.Id + ":replaced@" + replacementAt))
                    {
                        state.Couriers.Remove(courier);
                        state.Couriers.Add(CreateIdleCourier());
                    }
                }
            }
        }

        if (rng is not null)
            TryDispatchAvailable(state, rng, minute);
    }

    private void EnsureState(GameState state)
    {
        _ = _states.GetValue(state, _ => new PoolState());
    }

    public IReadOnlyList<QueuedMessageSnapshot> GetQueue(GameState state)
    {
        if (!_states.TryGetValue(state, out var poolState))
            return Array.Empty<QueuedMessageSnapshot>();
        return poolState.Queue
            .OrderBy(m => m.Kind == CourierMessageKind.Query ? 0 : 1)
            .ThenBy(m => m.EnqueuedMinute)
            .ThenBy(m => m.Sequence)
            .Select(m => new QueuedMessageSnapshot(m.Kind, m.MessageId, m.EnqueuedMinute, m.OutboundTime, m.ReturnTime))
            .ToList();
    }

    public void EnsureCouriers(GameState state, FrictionTable friction)
    {
        if (state.Couriers.Count > 0) return;
        int count = Math.Max(1, friction.Hq.Couriers);
        for (int i = 0; i < count; i++)
            state.Couriers.Add(CreateIdleCourier());
    }

    private (int outboundTime, int returnTime, double outboundLoss, double returnLoss) ComputeParameters(Order order, GameState state, FrictionTable friction)
    {
        string fromZone = ResolveZoneForUnit(state, ResolveSenderId(state));
        string toZone = ResolveZoneForUnit(state, order.ToUnitId);
        int distance = GraphDistance(state, fromZone, toZone);
        int outboundTime = Math.Max(1, friction.Hq.DefaultDeliveryTimeMinutes);
        int returnTime = Math.Max(1, friction.Hq.DefaultReturnTimeMinutes);
        double outboundLoss = Clamp01(friction.Hq.CourierLossBase + friction.Hq.CourierLossPerKm * distance);
        double returnLoss = Clamp01(friction.Hq.CourierLossBase);
        return (outboundTime, returnTime, outboundLoss, returnLoss);
    }

    private void Enqueue(GameState state, QueuedMessage message)
    {
        var poolState = _states.GetValue(state, _ => new PoolState());
        poolState.Queue.Add(message);
    }

    private long NextSequence(GameState state)
    {
        var poolState = _states.GetValue(state, _ => new PoolState());
        return ++poolState.Sequence;
    }

    private List<InternalDispatch> TryDispatchAvailable(GameState state, Random rng, int now)
    {
        var poolState = _states.GetValue(state, _ => new PoolState());
        var dispatched = new List<InternalDispatch>();
        while (true)
        {
            var courier = state.Couriers.FirstOrDefault(c => c.IsAvailable || c.State == CourierState.Returned);
            if (courier is null) break;
            if (courier.State == CourierState.Returned)
                MakeIdle(courier);
            var next = poolState.Queue
                .OrderBy(m => m.Kind == CourierMessageKind.Query ? 0 : 1)
                .ThenBy(m => m.EnqueuedMinute)
                .ThenBy(m => m.Sequence)
                .FirstOrDefault();
            if (next is null) break;
            poolState.Queue.Remove(next);
            dispatched.Add(DispatchMessage(state, courier, next, rng, now));
        }
        return dispatched;
    }

    private InternalDispatch DispatchMessage(GameState state, Courier courier, QueuedMessage message, Random rng, int dispatchMinute)
    {
        bool outboundLost = rng.NextDouble() < message.OutboundLossChance;
        bool returnLost = !outboundLost && rng.NextDouble() < message.ReturnLossChance;
        int deliveryMinute = dispatchMinute + message.OutboundTime;
        int calculatedReturnMinute = deliveryMinute + message.ReturnTime;

        courier.State = outboundLost ? CourierState.Lost : CourierState.Delivering;
        courier.AssignedOrderId = message.Kind == CourierMessageKind.Order ? message.MessageId : null;
        courier.AssignedQueryId = message.Kind == CourierMessageKind.Query ? message.MessageId : null;
        courier.OutboundDepartureMinute = dispatchMinute;
        courier.OutboundDeliveryTime = message.OutboundTime;
        courier.ReturnTime = message.ReturnTime;
        courier.ExpectedDeliveryMinute = deliveryMinute;
        courier.ExpectedReturnMinute = calculatedReturnMinute;
        courier.ReplacementMinute = calculatedReturnMinute + 60;
        courier.LostOnOutbound = outboundLost;
        courier.LostOnReturn = returnLost;
        courier.DeliveryConfirmed = false;

        if (outboundLost)
        {
            state.Clock.Schedule(new CourierLost(deliveryMinute, courier.Id, "Outbound"), deliveryMinute);
        }
        else if (returnLost)
        {
            state.Clock.Schedule(new CourierLost(calculatedReturnMinute, courier.Id, "Return"), calculatedReturnMinute);
        }
        else
        {
            state.Clock.Schedule(new CourierReturned(calculatedReturnMinute, courier.Id), calculatedReturnMinute);
        }

        return new InternalDispatch(
            message.MessageId,
            message.Kind,
            courier.Id,
            dispatchMinute,
            deliveryMinute,
            calculatedReturnMinute,
            outboundLost,
            returnLost);
    }

    private int EstimateDispatchMinute(GameState state, QueuedMessage candidate, int now)
    {
        var poolState = _states.GetValue(state, _ => new PoolState());
        var ordered = poolState.Queue
            .OrderBy(m => m.Kind == CourierMessageKind.Query ? 0 : 1)
            .ThenBy(m => m.EnqueuedMinute)
            .ThenBy(m => m.Sequence)
            .ToList();
        int index = ordered.FindIndex(m => ReferenceEquals(m, candidate) || (m.MessageId == candidate.MessageId && m.Sequence == candidate.Sequence));
        if (index < 0) index = ordered.Count - 1;

        var slots = state.Couriers
            .Select(c => NextFreeMinute(c, now))
            .OrderBy(t => t)
            .ToList();
        if (slots.Count == 0)
            slots.Add(now);

        int dispatchMinute = now;
        for (int i = 0; i <= index && i < ordered.Count; i++)
        {
            slots.Sort();
            dispatchMinute = Math.Max(slots[0], ordered[i].EnqueuedMinute);
            int completion = dispatchMinute + ordered[i].OutboundTime + ordered[i].ReturnTime;
            slots[0] = completion;
        }
        return dispatchMinute;
    }

    private static int NextFreeMinute(Courier courier, int now)
    {
        if (courier.IsAvailable || courier.State == CourierState.Returned)
            return now;
        if (courier.State == CourierState.Lost)
            return Math.Max(now, courier.ReplacementMinute ?? now);
        if (courier.LostOnOutbound)
            return Math.Max(now, courier.ExpectedDeliveryMinute ?? now);
        return Math.Max(now, courier.ExpectedReturnMinute ?? (courier.OutboundDepartureMinute + courier.OutboundDeliveryTime + courier.ReturnTime));
    }

    private static Courier CreateIdleCourier() => new()
    {
        State = CourierState.Idle,
        DeliveryConfirmed = false
    };

    private static void MakeIdle(Courier courier)
    {
        courier.State = CourierState.Idle;
        courier.AssignedOrderId = null;
        courier.AssignedQueryId = null;
        courier.OutboundDepartureMinute = 0;
        courier.OutboundDeliveryTime = 0;
        courier.ReturnTime = 0;
        courier.ExpectedDeliveryMinute = null;
        courier.ExpectedReturnMinute = null;
        courier.ReplacementMinute = null;
        courier.LostOnOutbound = false;
        courier.LostOnReturn = false;
    }

    private static string ResolveSenderId(GameState state)
    {
        var hq = state.Units.Values.FirstOrDefault(u => u.Type == UnitType.HQ);
        if (hq is not null) return hq.Id;
        return state.Units.Keys.FirstOrDefault() ?? "HQ";
    }

    private static string ResolveZoneForUnit(GameState state, string? unitId)
    {
        if (!string.IsNullOrWhiteSpace(unitId) && state.Units.TryGetValue(unitId, out var unit))
            return unit.LocationZoneId;
        var hq = state.Units.Values.FirstOrDefault(u => u.Type == UnitType.HQ);
        return hq?.LocationZoneId ?? state.Zones.Keys.FirstOrDefault() ?? "";
    }

    private static int GraphDistance(GameState state, string from, string to)
    {
        if (string.IsNullOrEmpty(from) || string.IsNullOrEmpty(to)) return 0;
        if (from == to) return 0;
        var dist = new Dictionary<string, int>(StringComparer.Ordinal) { [from] = 0 };
        var q = new Queue<string>();
        q.Enqueue(from);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == to) return dist[cur];
            if (!state.Zones.TryGetValue(cur, out var zone)) continue;
            foreach (var (next, minutes) in zone.TravelMinutes)
            {
                if (dist.ContainsKey(next)) continue;
                dist[next] = dist[cur] + Math.Max(1, minutes);
                q.Enqueue(next);
            }
        }
        return 999;
    }

    private static double Clamp01(double value) => value < 0 ? 0 : (value > 1 ? 1 : value);

    private static CourierDispatchResult ToPublicResult(InternalDispatch d) => new(
        d.DispatchMinute,
        d.DeliveryMinute,
        d.ReturnMinute,
        d.OutboundLost,
        d.ReturnLost,
        !d.OutboundLost && !d.ReturnLost,
        d.CourierId);

    private sealed record InternalDispatch(
        string MessageId,
        CourierMessageKind Kind,
        string CourierId,
        int DispatchMinute,
        int DeliveryMinute,
        int ReturnMinute,
        bool OutboundLost,
        bool ReturnLost);

    public sealed record QueuedMessageSnapshot(
        CourierMessageKind Kind,
        string MessageId,
        int EnqueuedMinute,
        int OutboundTime,
        int ReturnTime);
}
