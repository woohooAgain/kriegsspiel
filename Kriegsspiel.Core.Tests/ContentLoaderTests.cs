using System.Text.Json;
using System.Text.Json.Nodes;
using Kriegsspiel.Core.Content;
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
    public void Load_RepositoryContentAndFixtures_Succeeds()
    {
        var loader = new ContentLoader();
        var root = FindRepoRoot();

        var crossing = loader.Load(
            Path.Combine(root, "Content", "scenarios", "crossing_01.json"),
            Path.Combine(root, "Content", "friction.json"),
            Path.Combine(root, "Content", "reports.json"));
        Assert.NotNull(crossing.Scenario);
        Assert.Equal("crossing_01", crossing.Scenario.Id);
        Assert.True(crossing.Friction.Hq.Couriers >= 1);
        Assert.Contains("Impossible", crossing.Reports.ByKind.Keys);

        var test = loader.Load(
            Path.Combine(root, "Content", "scenarios", "test_01.json"),
            Path.Combine(root, "Content", "friction.json"),
            Path.Combine(root, "Content", "reports.json"));
        Assert.Equal("test_01", test.Scenario.Id);

        var bundle = TestContent.Bundle();
        Assert.NotNull(bundle.Scenario);
        Assert.NotEmpty(bundle.Scenario.Zones);
        Assert.NotEmpty(bundle.Scenario.Units);
        Assert.NotEmpty(bundle.Scenario.Objectives);
        Assert.True(Math.Abs(bundle.Scenario.Objectives.Sum(o => o.Weight) - 1.0) < 1e-6);
        Assert.Contains("Impossible", bundle.Reports.ByKind.Keys);

        var tempScenario = WriteTemp("scenario_fixture.json", SerializeScenario(bundle.Scenario));
        var tempFriction = WriteTemp("friction_fixture.json", SerializeFriction(bundle.Friction));
        var tempReports = WriteTemp("reports_fixture.json", SerializeReports(bundle.Reports));
        var loadedFixture = loader.Load(tempScenario, tempFriction, tempReports);
        Assert.Equal(bundle.Scenario.Id, loadedFixture.Scenario.Id);
        Assert.Equal(bundle.Scenario.Zones.Count, loadedFixture.Scenario.Zones.Count);
    }

    [Fact]
    public void Load_DefaultControlStateWeights_Succeeds()
    {
        var bundle = LoadWith();
        var weights = bundle.Scenario.Objectives[0].ControlStateWeights;
        Assert.Equal(3, weights.Count);
        Assert.True(Math.Abs(weights.Values.Sum() - 1.5) < 1e-6);
    }

    [Fact]
    public void Load_ProbabilityAboveOne_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(frictionMutator: root => root["hq"]!["courierLossBase"] = 1.2));
        Assert.Contains("courierLossBase", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ObjectiveWeightsSumNotOne_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                root["objectives"] = new JsonArray
                {
                    new JsonObject { ["type"] = "HoldZone", ["zone"] = "A", ["untilMinute"] = 420, ["controlMinutes"] = new JsonArray(420), ["weight"] = 0.6 },
                    new JsonObject { ["type"] = "PreserveStrength", ["unit"] = "U1", ["minPercent"] = 50, ["weight"] = 0.6 }
                };
            }));
        Assert.Contains("objectives weight sum", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_ZeroCouriers_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(frictionMutator: root => root["hq"]!["couriers"] = 0));
        Assert.Contains("couriers", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_DuplicateIds_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                var zones = root["zones"]!.AsArray();
                zones.Add(JsonNode.Parse(zones[0]!.ToJsonString()));
            }));
        Assert.Contains("duplicate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_BrokenOrAsymmetricEdge_ThrowsValidation()
    {
        var exMissing = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                root["zones"]![0]!["travelMinutes"]!["C"] = 10;
                root["zones"]![0]!["edgeTypes"]!["C"] = "Road";
            }));
        Assert.Contains("unknown", exMissing.Message, StringComparison.OrdinalIgnoreCase);

        var exAsymmetric = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root =>
            {
                root["zones"]![1]!["travelMinutes"]!.AsObject().Remove("A");
                root["zones"]![1]!["edgeTypes"]!.AsObject().Remove("A");
            }));
        Assert.Contains("symmetric", exAsymmetric.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_InvalidEnum_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root["zones"]![0]!["edgeTypes"]!["B"] = "Hyperloop"));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NonPositiveDeliveryOrReturnTime_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(frictionMutator: root => root["hq"]!["defaultDeliveryTimeMinutes"] = 0));
        Assert.Contains("defaultDeliveryTimeMinutes", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NegativeCoefficient_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(frictionMutator: root => root["combat"]!["lethalitySelf"] = -0.1));
        Assert.Contains("lethalitySelf", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_OrdersP0BelowPMin_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(frictionMutator: root =>
            {
                root["orders"]!["pMin"] = 0.2;
                root["orders"]!["p0"] = 0.1;
                root["orders"]!["pMax"] = 0.9;
            }));
        Assert.Contains("p0", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Load_NegativeSeed_ThrowsValidation()
    {
        var ex = Assert.Throws<ContentValidationException>(() =>
            LoadWith(scenarioMutator: root => root["seed"] = -1));
        Assert.Contains("seed", ex.Message, StringComparison.OrdinalIgnoreCase);
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

    private static JsonNode CreateBaseScenarioNode()
    {
        return new JsonObject
        {
            ["id"] = "valid_scenario",
            ["name"] = "Valid",
            ["startMinute"] = 360,
            ["endMinute"] = 480,
            ["seed"] = 1,
            ["victoryThreshold"] = 0.7,
            ["zones"] = new JsonArray
            {
                new JsonObject
                {
                    ["id"] = "A",
                    ["name"] = "A",
                    ["terrain"] = "Open",
                    ["travelMinutes"] = new JsonObject { ["B"] = 30 },
                    ["edgeTypes"] = new JsonObject { ["B"] = "Road" },
                    ["defenseBonus"] = 0,
                    ["observation"] = 1,
                    ["controllingFaction"] = "Blue",
                    ["cover"] = 0.0,
                    ["objects"] = new JsonArray(),
                    ["cargo"] = new JsonArray()
                },
                new JsonObject
                {
                    ["id"] = "B",
                    ["name"] = "B",
                    ["terrain"] = "Forest",
                    ["travelMinutes"] = new JsonObject { ["A"] = 30 },
                    ["edgeTypes"] = new JsonObject { ["A"] = "Road" },
                    ["defenseBonus"] = 1,
                    ["observation"] = 1,
                    ["controllingFaction"] = "Neutral",
                    ["cover"] = 0.2,
                    ["objects"] = new JsonArray(),
                    ["cargo"] = new JsonArray()
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
                    ["initialStrengthSnapshot"] = 50,
                    ["readiness"] = 80,
                    ["morale"] = 80,
                    ["fatigue"] = 0,
                    ["ammo"] = 100,
                    ["training"] = 2,
                    ["experience"] = 1,
                    ["locationZoneId"] = "A",
                    ["posture"] = "Reserve",
                    ["speedKmh"] = 4,
                    ["traits"] = new JsonObject { ["initiative"] = 1, ["caution"] = 1, ["literalism"] = 1, ["reportHonesty"] = 1, ["professionalism"] = 1 }
                }
            },
            ["objectives"] = new JsonArray
            {
                new JsonObject { ["type"] = "HoldZone", ["zone"] = "B", ["untilMinute"] = 480, ["controlMinutes"] = new JsonArray(420, 480), ["weight"] = 1.0 }
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

    private static string SerializeScenario(Scenario scenario) => JsonSerializer.Serialize(scenario, JsonOptions());
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
