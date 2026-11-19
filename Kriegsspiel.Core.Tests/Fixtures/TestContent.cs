using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Tests.Fixtures;

public static class TestContent
{
    public static ScenarioBundle Bundle() => new()
    {
        Scenario = new Scenario
        {
            Id = "t",
            Name = "Test",
            StartMinute = 360,
            EndMinute = 480,
            Zones = [
                new Zone { Id = "A", Name = "A", Terrain = TerrainType.Open, TravelMinutes = { ["B"] = 30 } },
                new Zone { Id = "B", Name = "B", Terrain = TerrainType.Forest, TravelMinutes = { ["A"] = 30 } }
            ],
            Units = [
                new Unit { Id = "U1", Name = "U1", Type = UnitType.HQ, Strength = 50,
                    LocationZoneId = "A", Traits = new CommanderTraits(1,1,1,1) },
                new Unit { Id = "ENEMY_1", Name = "E1", Type = UnitType.Infantry, Strength = 50,
                    LocationZoneId = "B", Traits = new CommanderTraits(1,1,1,0) }
            ]
        },
        Friction = new FrictionTable(),
        Reports = new ReportTemplates()
    };
}