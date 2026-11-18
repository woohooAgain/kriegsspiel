namespace Kriegsspiel.Core.Content;

using Kriegsspiel.Core.Domain;
using System.Security.AccessControl;

public sealed class Scenario
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int StartMinute { get; init; }
    public int EndMinute { get; init; }
    public List<Zone> Zones { get; init; } = new();
    public List<Unit> Units { get; init; } = new();
    public List<Objective> Objectives { get; init; } = new();
}