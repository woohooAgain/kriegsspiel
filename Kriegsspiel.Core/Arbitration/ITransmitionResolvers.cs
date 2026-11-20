using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public interface ITransmissionResolver
{
    TransmissionResult Transmit(Order o, GameState s, FrictionTable f, Random rng);

    CourierDispatchResult? TransmitQuery(
        string queryId,
        string requesterId,
        string targetId,
        GameState state,
        FrictionTable friction,
        Random rng,
        int? outboundTimeOverride = null,
        int? returnTimeOverride = null,
        double? outboundLossChance = null,
        double? returnLossChance = null) => null;

    void EnsureCouriers(GameState state, FrictionTable friction) { }

    void UpdateTo(GameState state, int minute) { }

    void UpdateTo(GameState state, int minute, Random rng) => UpdateTo(state, minute);
}
