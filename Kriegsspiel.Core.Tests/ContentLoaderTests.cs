using System.Text.Json;
using System.Text.Json.Nodes;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Tests.Fixtures;

namespace Kriegsspiel.Core.Tests;

public sealed class ContentLoaderTests : IDisposable
{
    private readonly string _tempRoot;

    public ContentLoaderTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "kriegsspiel-content-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempRoot);
    }

    [Fact]
    public void Load_RepositoryScenarios_V04()
    {
        var test = LoadRepository("test_01");
        Assert.Equal("test_01", test.Scenario.Id);
        Assert.Equal("START", test.Scenario.HqZoneId);
        Assert.Equal(2, test.Scenario.CourierCount);
        Assert.Equal(0.05, test.Scenario.CourierLossProbability, 6);
        Assert.Equal(0.03, test.Scenario.ReportLossProbability, 6);
        Assert.Equal(0.7, test.Scenario.WinThreshold, 6);
        Assert.Equal(0.7, test.Scenario.VictoryThreshold, 6);
        Assert.False(string.IsNullOrWhiteSpace(test.Scenario.Name));
        Assert.DoesNotContain(test.Scenario.Units, u => u.Type == UnitType.HQ);
        Assert.All(test.Scenario.Units, u => Assert.True(u.Faction is Faction.Blue or Faction.Red));
        Assert.All(test.Scenario.Zones, z =>
        {
            Assert.InRange(z.DefenseBonus, 0, 2);
            Assert.InRange(z.Observation, 1, 3);
            Assert.All(z.CourierMinutes.Values, c => Assert.True(c > 0));
            Assert.All(z.EdgeTypes.Values, e => Assert.True(e is EdgeType.Road or EdgeType.Offroad));
        });
        Assert.Equal(1.0, test.Scenario.Objectives.Sum(o => o.Weight), 6);

        var crossing = LoadRepository("crossing_01");
        Assert.Equal("crossing_01", crossing.Scenario.Id);
        Assert.Equal("B2", crossing.Scenario.HqZoneId);
        Assert.DoesNotContain(crossing.Scenario.Units, u => u.Type == UnitType.HQ);
        var east = crossing.Scenario.Zones.Single(z => z.Id == "B4");
        Assert.Equal(2, east.DefenseBonus);
        Assert.Equal(1, east.Observation);
        Assert.Equal(EdgeType.Road, crossing.Scenario.Zones.Single(z => z.Id == "B3").EdgeTypes["B4"]);
        Assert.Equal(1.0, crossing.Scenario.Objectives.Sum(o => o.Weight), 6);
    }

    [Fact]
    public void Load_LegacyScenariosRemoved()
    {
        var root = FindRepoRoot();
        Assert.False(File.Exists(Path.Combine(root, "Content", "scenarios", "fs005_scale_demo.json")));
        Assert.False(File.Exists(Path.Combine(root, "Content", "scenarios", "river_line_10x10.json")));
    }

    [Fact]
    public void Load_V04Schema_MapsFields()
    {
        var bundle = LoadWith();
        Assert.Equal("valid_scenario", bundle.Scenario.Id);
        Assert.Equal("A", bundle.Scenario.HqZoneId);
        Assert.Equal(2, bundle.Scenario.CourierCount);
        Assert.Equal(0.05, bundle.Scenario.CourierLossProbability, 6);
        Assert.Equal(0.03, bundle.Scenario.ReportLossProbability, 6);
        Assert.Equal(0.7, bundle.Scenario.WinThreshold, 6);
        Assert.Single(bundle.Scenario.Units);
        var unit = bundle.Scenario.Units[0];
        Assert.Equal(Faction.Blue, unit.Faction);
        Assert.Equal(50, unit.InitialStrengthSnapshot);
        Assert.Equal(Posture.Defend, unit.Posture);
        Assert.Equal(1, unit.Traits.Init);
        Assert.Equal(1, unit.Traits.Pedantry);
        Assert.Equal(1, unit.Traits.Honesty);
        Assert.Equal(2, bundle.Scenario.Zones.Single(z => z.Id == "A").Observation);
        Assert.Equal(10, bundle.Scenario.Zones.Single(z => z.Id == "A").CourierMinutes["B"]);
        Assert.NotNull(bundle.ScenarioRaw);
    }

    [Fact]
    public void Load_HqUnitDefinesHqZoneAndIsNotCombatUnit()
    {
        var bundle = LoadWith(scenarioMutator: root =>
        {
            root.AsObject().Remove("hqZone");
            root["units"]!.AsArray().Add(new JsonObject
            {
                ["id"] = "HQ",
                ["name"] = "HQ",
                ["type"] = "HQ",
                ["faction"] = "Blue",
                ["strength"] = 5,
                ["initialStrength"] = 5,
                ["readiness"] = 100,
                ["ammo"] = 0,
                ["zoneId"] = "A",
                ["posture"] = "defense",
                ["traits"] = new JsonObject { ["init"] = 1, ["pedantry"] = 1, ["honesty"] = 3 }
            });
        });

        Assert.Equal("A", bundle.Scenario.HqZoneId);
        Assert.DoesNotContain(bundle.Scenario.Units, u => u.Type == UnitType.HQ);
        Assert.Equal("U1", Assert.Single(bundle.Scenario.Units).Id);
    }

    [Fact]
    public void Load_GameStateFactory_BuildsInitialState()
    {
        var bundle = LoadRepository("test_01");
        var state = GameStateFactory.FromBundle(bundle);

        Assert.Equal("START", state.HqZoneId);
        Assert.Equal(480, state.EndMinute);
        Assert.Equal(2001, state.Seed);
        Assert.Equal(2, state.Couriers.Count);
        Assert.All(state.Couriers, c => Assert.Equal(CourierState.Idle, c.State));
        Assert.Equal(2, state.Units.Count);
        Assert.Contains("1IR", state.Units.Keys);
        Assert.DoesNotContain("HQ", state.Units.Keys);
        Assert.Contains("1IR", state.Zones["START"].PresentUnitIds);
        Assert.Contains("ENEMY_1", state.Zones["OBJ"].PresentUnitIds);
    }

    [Fact]
    public void Load_DefenseBonusOutOfRange_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "A")["defenseBonus"] = 3));
        Assert.Contains("defenseBonus", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ObservabilityOutOfRange_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "B")["observability"] = 0));
        Assert.Contains("observability", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NonPositiveTravelTime_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "A")["travelMinutes"]!["B"] = 0));
        Assert.Contains("travelTime", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NonPositiveCourierTime_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "A")["courierTimes"]!["B"] = 0));
        Assert.Contains("courierTime", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_CourierPoolBelowOne_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Pool(root)["count"] = 0));
        Assert.Contains("courierPool.count", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ProbabilitiesOutOfRange_Throws()
    {
        var exCourier = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Pool(root)["courierLossProbability"] = 1.5));
        Assert.Contains("courierLossProbability", exCourier.Message, StringComparison.OrdinalIgnoreCase);

        var exReport = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Pool(root)["reportLossProbability"] = -0.1));
        Assert.Contains("reportLossProbability", exReport.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DuplicateIds_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                var zones = root["zones"]!.AsArray();
                zones.Add(zones[0]!.DeepClone());
            }));
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_AsymmetricEdge_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "B")["travelMinutes"]!.AsObject().Remove("A")));
        Assert.Contains("symmetric", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_InvalidEdgeType_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Zone(root, "A")["edgeTypes"]!["B"] = "Crossing"));
        Assert.Contains("edge type", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_EndMinuteNotMultipleOf30_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root["endMinute"] = 481));
        Assert.Contains("multiple of 30", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_MissingSeed_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root.AsObject().Remove("seed")));
        Assert.Contains("seed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_InitialStrengthNonPositive_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Unit(root, "U1")["initialStrength"] = 0));
        Assert.Contains("initialStrength", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ReadinessOutOfRange_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Unit(root, "U1")["readiness"] = 101));
        Assert.Contains("readiness", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NegativeAmmo_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Unit(root, "U1")["ammo"] = -1));
        Assert.Contains("ammo", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_TraitsOutOfRange_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => Unit(root, "U1")["traits"]!["pedantry"] = 4));
        Assert.Contains("pedantry", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_LegacyScenarioWithoutV04Fields_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root.AsObject().Remove("courierPool")));
        Assert.Contains("courierPool", ex.Message, StringComparison.OrdinalIgnoreCase);

        var exHq = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root.AsObject().Remove("hqZone")));
        Assert.Contains("hqZone", exHq.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DisconnectedGraph_Throws()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                root["zones"]!.AsArray().Add(new JsonObject
                {
                    ["id"] = "C",
                    ["name"] = "C",
                    ["terrain"] = "Open",
                    ["travelMinutes"] = new JsonObject(),
                    ["courierTimes"] = new JsonObject(),
                    ["edgeTypes"] = new JsonObject(),
                    ["defenseBonus"] = 0,
                    ["observability"] = 1
                });
            }));
        Assert.Contains("connected", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    private ScenarioBundle LoadRepository(string id)
    {
        var root = FindRepoRoot();
        return new ContentLoader().Load(
            Path.Combine(root, "Content", "scenarios", id + ".json"),
            Path.Combine(root, "Content", "friction.json"),
            Path.Combine(root, "Content", "reports.json"));
    }

    private ScenarioBundle LoadWith(Action<JsonNode>? scenarioMutator = null, Action<JsonNode>? frictionMutator = null, Action<JsonNode>? reportsMutator = null)
    {
        var scenario = CreateBaseScenarioNode();
        var friction = CreateBaseFrictionNode();
        var reports = CreateBaseReportsNode();
        scenarioMutator?.Invoke(scenario);
        frictionMutator?.Invoke(friction);
        reportsMutator?.Invoke(reports);

        var scenarioPath = WriteTemp("scenario.json", scenario.ToJsonString());
        var frictionPath = WriteTemp("friction.json", friction.ToJsonString());
        var reportsPath = WriteTemp("reports.json", reports.ToJsonString());
        return new ContentLoader().Load(scenarioPath, frictionPath, reportsPath);
    }

    private static JsonObject Zone(JsonNode root, string id) =>
        root["zones"]!.AsArray().OfType<JsonObject>().Single(z => (string?)z["id"] == id);

    private static JsonObject Unit(JsonNode root, string id) =>
        root["units"]!.AsArray().OfType<JsonObject>().Single(u => (string?)u["id"] == id);

    private static JsonObject Pool(JsonNode root) => root["courierPool"]!.AsObject();

    private static JsonNode CreateBaseScenarioNode()
    {
        return new JsonObject
        {
            ["id"] = "valid_scenario",
            ["name"] = "Valid",
            ["startMinute"] = 360,
            ["endMinute"] = 480,
            ["seed"] = 1,
            ["hqZone"] = "A",
            ["winThreshold"] = 0.7,
            ["courierPool"] = new JsonObject
            {
                ["count"] = 2,
                ["courierLossProbability"] = 0.05,
                ["reportLossProbability"] = 0.03
            },
            ["zones"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "A",
                    ["name"] = "A",
                    ["terrain"] = "Open",
                    ["travelMinutes"] = new JsonObject { ["B"] = 30 },
                    ["courierTimes"] = new JsonObject { ["B"] = 10 },
                    ["edgeTypes"] = new JsonObject { ["B"] = "Road" },
                    ["defenseBonus"] = 0,
                    ["observability"] = 2
                },
                new JsonObject
                {
                    ["id"] = "B",
                    ["name"] = "B",
                    ["terrain"] = "Forest",
                    ["travelMinutes"] = new JsonObject { ["A"] = 30 },
                    ["courierTimes"] = new JsonObject { ["A"] = 10 },
                    ["edgeTypes"] = new JsonObject { ["A"] = "Road" },
                    ["defenseBonus"] = 1,
                    ["observability"] = 1
                }
            },
            ["units"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "U1",
                    ["name"] = "U1",
                    ["type"] = "Infantry",
                    ["faction"] = "Blue",
                    ["strength"] = 50,
                    ["initialStrength"] = 50,
                    ["readiness"] = 80,
                    ["ammo"] = 100,
                    ["zoneId"] = "A",
                    ["posture"] = "defense",
                    ["traits"] = new JsonObject { ["init"] = 1, ["pedantry"] = 1, ["honesty"] = 1 }
                }
            },
            ["objectives"] = new JsonArray
            {
                new JsonObject { ["type"] = "HoldZone", ["zone"] = "B", ["weight"] = 1.0 }
            }
        };
    }

    private static JsonNode CreateBaseFrictionNode() => JsonNode.Parse(SerializeFriction(new FrictionTable()))!;
    private static JsonNode CreateBaseReportsNode() => JsonNode.Parse(SerializeReports(TestContent.Bundle().Reports))!;

    private string WriteTemp(string name, string content)
    {
        var path = Path.Combine(_tempRoot, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static string SerializeFriction(FrictionTable friction) => JsonSerializer.Serialize(friction, JsonOptions());
    private static string SerializeReports(ReportTemplates reports) => JsonSerializer.Serialize(reports, JsonOptions());

    private static JsonSerializerOptions JsonOptions() => new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Content", "scenarios", "crossing_01.json");
            if (File.Exists(candidate))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root containing Content/scenarios/crossing_01.json");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
        }
    }
}
