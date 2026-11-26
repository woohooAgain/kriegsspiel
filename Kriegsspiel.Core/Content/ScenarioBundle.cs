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
        var scenario = b.Scenario;
        var state = new GameState
        {
            ScenarioId = scenario.Id,
            HqZoneId = scenario.HqZoneId,
            Clock = new Clock(scenario.StartMinute),
            Zones = scenario.Zones.ToDictionary(z => z.Id),
            Units = scenario.Units.ToDictionary(u => u.Id),
            Knowledge = new PlayerKnowledge(),
            Orders = new(),
            DeliveredReports = new(),
            EventLog = new(),
            EndMinute = scenario.EndMinute,
            Seed = scenario.Seed ?? 0,
            RngState = scenario.Seed ?? 0
        };

        foreach (var unit in state.Units.Values)
        {
            if (state.Zones.TryGetValue(unit.LocationZoneId, out var zone) && !zone.PresentUnitIds.Contains(unit.Id))
                zone.PresentUnitIds.Add(unit.Id);
        }

        for (var i = 0; i < scenario.CourierCount; i++)
            state.Couriers.Add(new Courier { Id = $"C{i + 1:00}", State = CourierState.Idle });

        return state;
    }
}
