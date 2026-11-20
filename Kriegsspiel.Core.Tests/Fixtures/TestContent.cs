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
            Seed = 42,
            VictoryThreshold = 0.7,
            Zones =
            [
                new Zone { Id = "A", Name = "A", Terrain = TerrainType.Open, TravelMinutes = { ["B"] = 30 } },
                new Zone { Id = "B", Name = "B", Terrain = TerrainType.Forest, TravelMinutes = { ["A"] = 30 } }
            ],
            Units =
            [
                new Unit
                {
                    Id = "U1",
                    Name = "U1",
                    Type = UnitType.HQ,
                    Strength = 50,
                    Readiness = 80,
                    Morale = 80,
                    Fatigue = 0,
                    Ammo = 100,
                    LocationZoneId = "A",
                    Traits = new CommanderTraits(1, 1, 1, 1)
                },
                new Unit
                {
                    Id = "ENEMY_1",
                    Name = "E1",
                    Type = UnitType.Infantry,
                    Strength = 50,
                    Readiness = 80,
                    Morale = 80,
                    Fatigue = 0,
                    Ammo = 100,
                    LocationZoneId = "B",
                    Traits = new CommanderTraits(1, 1, 1, 0)
                }
            ],
            Objectives =
            [
                new Objective { Type = ObjectiveType.HoldZone, Zone = "B", UntilMinute = 480, ControlMinutes = [420, 480], Weight = 1.0 }
            ]
        },
        Friction = new FrictionTable(),
        Reports = new ReportTemplates
        {
            ByKind = new Dictionary<string, List<string>>
            {
                ["Status"] = ["[{eventTime}/{deliveryTime}] {source}: {unit} в {zone}, силы ~{strength}%, уверенность {confidence}."],
                ["Request"] = ["[{eventTime}/{deliveryTime}] {source}: {unit} запрашивает уточнение задачи (уверенность {confidence})."],
                ["Contact"] = ["[{eventTime}/{deliveryTime}] {source}: контакт в {zone}, факт {factKey}, уверенность {confidence}."],
                ["CombatResult"] = ["[{eventTime}/{deliveryTime}] {source}: {unit} сообщает о бое в {zone}, потери {losses}, уверенность {confidence}."],
                ["Arrival"] = ["[{eventTime}/{deliveryTime}] {source}: {unit} прибыл в {zone}, уверенность {confidence}."],
                ["Impossible"] = ["[{eventTime}/{deliveryTime}] {source}: {unit} не может выполнить приказ в {zone}, причина: {status}, уверенность {confidence}."]
            },
            Placeholders = new Dictionary<string, List<string>>
            {
                ["Status"] = ["eventMinute", "eventTime", "deliveryMinute", "deliveryTime", "sourceId", "source", "confidence", "kind", "zoneId", "zone", "unitId", "unit", "strength", "factKey"],
                ["Impossible"] = ["eventMinute", "eventTime", "deliveryMinute", "deliveryTime", "sourceId", "source", "confidence", "kind", "zoneId", "zone", "unitId", "unit", "status", "factKey"]
            }
        }
    };
}