using Kriegsspiel.Console.Persistence;
using Kriegsspiel.Core.Domain;
namespace Kriegsspiel.Console.Commands;

public sealed class SaveCommand(JsonSaveStore store) : ICommand
{
    public void Execute(GameState state) => store.Save(state);
}