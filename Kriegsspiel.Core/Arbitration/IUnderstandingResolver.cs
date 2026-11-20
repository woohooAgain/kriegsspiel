using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public interface IUnderstandingResolver
{
    IEnumerable<Report> Resolve(OrderDelivered e, GameState s, FrictionTable f, Random rng);

    IEnumerable<Report> Resolve(
        OrderDelivered e,
        GameState s,
        FrictionTable f,
        Random rng,
        UnderstandingOptions options) => Resolve(e, s, f, rng);
}
