using System.Text.Json;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Console.Persistence;

public sealed class JsonSaveStore
{
    private static readonly JsonSerializerOptions Opt = new() { WriteIndented = true };
    public void Save(GameState state)
    {
        Directory.CreateDirectory("saves");
        var path = $"saves/{DateTime.UtcNow:yyyyMMdd_HHmmss}.json";
        File.WriteAllText(path, JsonSerializer.Serialize(state, Opt));
        Spectre.Console.AnsiConsole.MarkupLine($"[green]Сохранено: {path}[/]");
    }
}