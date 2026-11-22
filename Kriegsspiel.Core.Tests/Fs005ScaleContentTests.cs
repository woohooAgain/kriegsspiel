using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Tests;

public sealed class Fs005ScaleContentTests
{
    [Fact]
    public void Load_DemoWithCanonicalContent_Succeeds()
    {
        var root = FindRepoRoot();
        var bundle = new ContentLoader().Load(
            Path.Combine(root, "Content", "scenarios", "fs005_scale_demo.json"),
            Path.Combine(root, "Content", "friction.json"),
            Path.Combine(root, "Content", "reports.json"));

        Assert.Equal("fs005_scale_demo", bundle.Scenario.Id);
        Assert.False(string.IsNullOrWhiteSpace(bundle.Scenario.Name));
        Assert.True(bundle.Friction.Hq.Couriers >= 1);
        Assert.Contains("Impossible", bundle.Reports.ByKind.Keys);
        Assert.NotNull(bundle.ScenarioRaw);
        Assert.Equal(bundle.Scenario.Zones.Count, GameStateFactory.FromBundle(bundle).Zones.Count);
    }

    [Fact]
    public void Load_DemoMeetsScaleAndGraphInvariants()
    {
        var scenario = LoadDemo().Scenario;

        Assert.True(scenario.Zones.Count >= 30);
        Assert.True(scenario.Units.Count >= 30);

        var zoneById = scenario.Zones.ToDictionary(z => z.Id, StringComparer.Ordinal);
        Assert.Equal(scenario.Zones.Count, zoneById.Count);
        Assert.Equal(scenario.Units.Count, scenario.Units.Select(u => u.Id).Distinct(StringComparer.Ordinal).Count());
        Assert.Contains(scenario.Units, u => u.Faction == Faction.Blue);
        Assert.Contains(scenario.Units, u => u.Faction == Faction.Red);

        var edgeTypes = new HashSet<EdgeType>();
        foreach (var zone in scenario.Zones)
        {
            Assert.Equal(zone.TravelMinutes.Count, zone.EdgeTypes.Count);
            foreach (var (neighbor, minutes) in zone.TravelMinutes)
            {
                Assert.NotEqual(zone.Id, neighbor);
                Assert.True(zoneById.ContainsKey(neighbor));
                Assert.True(minutes > 0);
                Assert.True(zone.EdgeTypes.ContainsKey(neighbor));

                var edgeType = zone.EdgeTypes[neighbor];
                edgeTypes.Add(edgeType);
                var back = zoneById[neighbor];
                Assert.Equal(minutes, back.TravelMinutes[zone.Id]);
                Assert.Equal(edgeType, back.EdgeTypes[zone.Id]);
            }
        }

        Assert.Contains(EdgeType.Road, edgeTypes);
        Assert.Contains(EdgeType.Offroad, edgeTypes);
        Assert.Contains(EdgeType.Crossing, edgeTypes);
        Assert.Contains(scenario.Zones, z => z.Objects.Any(o => o.Kind == ZoneObjectKind.Bridge));
        Assert.Contains(scenario.Zones, z => z.Objects.Any(o => o.Kind == ZoneObjectKind.ObservationPost));
        Assert.All(scenario.Units, u => Assert.True(zoneById.ContainsKey(u.LocationZoneId)));

        Assert.True(Math.Abs(scenario.Objectives.Sum(o => o.Weight) - 1.0) < 1e-6);
        Assert.All(scenario.Objectives, o => Assert.All(o.ControlMinutes, m => Assert.InRange(m, scenario.StartMinute, scenario.EndMinute)));
    }

    [Fact]
    public void Load_DemoTwice_IsReproducible()
    {
        var first = LoadDemo().Scenario;
        var second = LoadDemo().Scenario;

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(first.Zones.Select(z => z.Id), second.Zones.Select(z => z.Id));
        Assert.Equal(first.Units.Select(u => u.Id), second.Units.Select(u => u.Id));
    }

    private static ScenarioBundle LoadDemo()
    {
        var root = FindRepoRoot();
        return new ContentLoader().Load(
            Path.Combine(root, "Content", "scenarios", "fs005_scale_demo.json"),
            Path.Combine(root, "Content", "friction.json"),
            Path.Combine(root, "Content", "reports.json"));
    }

    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(dir.FullName, "Content", "scenarios", "fs005_scale_demo.json");
            if (File.Exists(candidate))
                return dir.FullName;
            dir = dir.Parent;
        }
        throw new DirectoryNotFoundException("Could not locate repository root containing Content/scenarios/fs005_scale_demo.json");
    }
}
