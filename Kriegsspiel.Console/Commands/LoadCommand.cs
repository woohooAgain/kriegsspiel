using Kriegsspiel.Console.Persistence;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Console.Commands;

public sealed class LoadCommand : ICommand
{
    private readonly JsonSaveStore _store;
    private readonly string? _path;

    public LoadCommand(JsonSaveStore store, string? path = null)
    {
        _store = store;
        _path = path;
    }

    public GameState Load() => _store.Load(_path ?? JsonSaveStore.LatestSavePath());

    public void Execute(GameState state) => _store.LoadInto(_path ?? JsonSaveStore.LatestSavePath(), state);
}
