using System.Text.Json;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Content;

public sealed class Scenario
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public int? Seed { get; init; }
    public double VictoryThreshold { get; init; } = 0.7;
    public List<Zone> Zones { get; init; } = new();
    public List<Unit> Units { get; init; } = new();
    public List<Objective> Objectives { get; init; } = new();
    public Dictionary<string, JsonElement> Extensions { get; init; } = new();
}