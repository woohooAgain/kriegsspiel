using System.Text.Json;
using System.Text.Json.Serialization;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Content;

public sealed class Scenario
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public int? Seed { get; init; }

    [JsonPropertyName("hqZone")]
    public string? HqZoneId { get; set; }

    [JsonPropertyName("winThreshold")]
    public double WinThreshold { get; set; } = 0.7;

    public int CourierCount { get; set; }
    public double CourierLossProbability { get; set; }
    public double ReportLossProbability { get; set; }

    public double VictoryThreshold { get; set; } = 0.7;

    public List<Zone> Zones { get; init; } = new();
    public List<Unit> Units { get; init; } = new();
    public List<Objective> Objectives { get; init; } = new();
    public Dictionary<string, JsonElement> Extensions { get; init; } = new();
}
