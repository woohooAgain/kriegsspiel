using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class TransmissionResolver : ITransmissionResolver
{
    private readonly CourierPool _pool = new();

    public CourierPool Pool => _pool;

    public TransmissionResult Transmit(Order o, GameState s, FrictionTable f, Random rng)
        => _pool.TransmitOrder(o, s, f, rng);

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
        => _pool.TransmitQuery(queryId, requesterId, targetId, state, friction, rng, outboundTimeOverride, returnTimeOverride, outboundLossChance, returnLossChance);

    public void EnsureCouriers(GameState state, FrictionTable friction)
        => _pool.EnsureCouriers(state, friction);

    public void UpdateTo(GameState state, int minute)
        => _pool.UpdateTo(state, minute);

    public void UpdateTo(GameState state, int minute, Random rng)
        => _pool.UpdateTo(state, minute, rng);
}