using System.Text.Json;

namespace Kriegsspiel.Core.Content;

using Kriegsspiel.Core.Domain;

public sealed class ScenarioBundle
{
    public required Scenario Scenario { get; init; }
    public required FrictionTable Friction { get; init; }
    public required ReportTemplates Reports { get; init; }
    public JsonElement? ScenarioRaw { get; init; }
    public JsonElement? FrictionRaw { get; init; }
    public JsonElement? ReportsRaw { get; init; }
}

public static class GameStateFactory
{
    public static GameState FromBundle(ScenarioBundle b)
    {
        var state = new GameState
        {
            ScenarioId = b.Scenario.Id,
            Clock = new Clock(b.Scenario.StartMinute),
            Zones = b.Scenario.Zones.ToDictionary(z => z.Id),
            Units = b.Scenario.Units.ToDictionary(u => u.Id),
            Knowledge = new PlayerKnowledge(),
            Orders = new(),
            DeliveredReports = new(),
            EventLog = new()
        };
        foreach (var u in state.Units.Values)
            if (state.Zones.TryGetValue(u.LocationZoneId, out var z) && !z.PresentUnitIds.Contains(u.Id))
                z.PresentUnitIds.Add(u.Id);
        return state;
    }
}