using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Persistence;
using Spectre.Console;

namespace Kriegsspiel.Console.Persistence;

public sealed class JsonSaveStore
{
    public void Save(GameState state) => Save(state, $"saves/{DateTime.UtcNow:yyyyMMdd_HHmmss}.json");

    public void Save(GameState state, string path)
    {
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, GameStatePersistence.Serialize(state));
        AnsiConsole.MarkupLine($"[green]Сохранено: {path}[/]");
    }

    public GameState Load(string path) => GameStatePersistence.Deserialize(File.ReadAllText(path));

    public void LoadInto(string path, GameState state) => GameStatePersistence.ApplyTo(File.ReadAllText(path), state);

    public static string LatestSavePath()
    {
        if (!Directory.Exists("saves"))
            throw new FileNotFoundException("No save files found.", "saves");
        return Directory.GetFiles("saves", "*.json")
            .OrderByDescending(path => path)
            .FirstOrDefault()
            ?? throw new FileNotFoundException("No save files found.", "saves");
    }
}
