using Kriegsspiel.Console.Rendering;
using Kriegsspiel.Core.Domain;
namespace Kriegsspiel.Console.Commands;

public sealed class MapCommand(MapRenderer r) : ICommand
{
    public void Execute(GameState state) => r.Render(state);
}