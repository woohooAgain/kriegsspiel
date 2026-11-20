using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public interface ICombatResolver
{
    IEnumerable<Report> Resolve(CombatTick e, GameState s, FrictionTable f, Random rng);
}
