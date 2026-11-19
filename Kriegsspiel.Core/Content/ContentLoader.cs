namespace Kriegsspiel.Core.Content;

using System.Text.Json;
using System.Text.Json.Serialization;

public sealed class ContentLoader
{
    private static readonly JsonSerializerOptions Opt = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    public ScenarioBundle Load(string scenarioPath, string frictionPath, string reportsPath)
    {
        var scenario = JsonSerializer.Deserialize<Scenario>(File.ReadAllText(scenarioPath), Opt)
                       ?? throw new InvalidOperationException("scenario null");
        var friction = JsonSerializer.Deserialize<FrictionTable>(File.ReadAllText(frictionPath), Opt)
                       ?? new FrictionTable();
        var reports = JsonSerializer.Deserialize<ReportTemplates>(File.ReadAllText(reportsPath), Opt)
                      ?? new ReportTemplates();
        return new ScenarioBundle { Scenario = scenario, Friction = friction, Reports = reports };
    }
}