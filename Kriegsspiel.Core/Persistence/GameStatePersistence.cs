using System.Text.Json;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public static class GameStatePersistence
{
    public static string Serialize(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        return JsonSerializer.Serialize(GameStateMapper.ToDto(state), PersistenceJson.Options);
    }

    public static GameStateDto DeserializeDto(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        return JsonSerializer.Deserialize<GameStateDto>(json, PersistenceJson.Options)
            ?? throw new InvalidOperationException("Save file does not contain a game state.");
    }

    public static GameState Deserialize(string json) => GameStateMapper.FromDto(DeserializeDto(json));

    public static void ApplyTo(string json, GameState target) => GameStateMapper.ApplyTo(DeserializeDto(json), target);
}
