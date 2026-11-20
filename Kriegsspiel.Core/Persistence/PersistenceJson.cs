using System.Text.Json;
using System.Text.Json.Serialization;

namespace Kriegsspiel.Core.Persistence;

public static class PersistenceJson
{
    public static JsonSerializerOptions Options { get; } = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
}
