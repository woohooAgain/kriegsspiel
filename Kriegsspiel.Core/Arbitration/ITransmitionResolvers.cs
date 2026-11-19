using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public interface ITransmissionResolver
{
    TransmissionResult Transmit(Order o, GameState s, FrictionTable f, Random rng);
}
