using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;
using Kriegsspiel.Core.Tests.Fixtures;

namespace Kriegsspiel.Core.Tests;

public class ReportTests
{
    [Fact]
    public void Factory_NormalSourcesNeverGetConfidenceThree()
    {
        var factory = new ReportFactory(new FrictionTable(), new Random(7));

        Assert.Equal(2, factory.Create(100, 120, "S1", 3).Confidence);
        Assert.Equal(1, factory.Create(100, 120, "S1", 0, ReportKind.Silence).Confidence);
        Assert.Equal(3, factory.Create(100, 120, "SCOUT", 3, ReportKind.Recon).Confidence);
    }

    [Fact]
    public void Factory_HonestyInflatesStrengthAndHidesLosses()
    {
        var source = NewUnit("U1", 0);
        var factory = new ReportFactory(new FrictionTable(), new Random(7));
        var facts = new Dictionary<string, string>
        {
            ["strength"] = "100",
            ["losses"] = "40",
            ["initialStrength"] = "100"
        };

        var report = factory.CreateWithHonesty(
            100, 120, source, 2, ReportKind.Status, "потери 40", "zone:B", facts);

        Assert.Equal("145", report.Facts["strength"]);
        Assert.Equal("22", report.Facts["losses"]);
        Assert.Equal("держимся", report.Facts["status"]);
        Assert.Equal("держимся", report.Text);
    }

    [Fact]
    public void Factory_HonestyThreeKeepsNumbersExact()
    {
        var source = NewUnit("U1", 3);
        var factory = new ReportFactory(new FrictionTable(), new Random(7));
        var facts = new Dictionary<string, string> { ["strength"] = "100", ["losses"] = "40" };

        var report = factory.CreateWithHonesty(
            100, 120, source, 2, ReportKind.Status, "потери 40", "zone:B", facts);

        Assert.Equal("100", report.Facts["strength"]);
        Assert.Equal("40", report.Facts["losses"]);
        Assert.Equal("потери 40", report.Text);
        Assert.False(report.Facts.ContainsKey("status"));
    }

    [Fact]
    public void LossProbability_IsBelowOrderLossAndGrowsWithHostility()
    {
        var friction = new FrictionTable();
        var neutralReport = ReportDeliveryResolver.ReportLossProbability(friction, 0);
        var hostileReport = ReportDeliveryResolver.ReportLossProbability(friction, 1);
        var hostileOrder = ReportDeliveryResolver.OrderLossProbability(friction, 1);

        Assert.Equal(0.03, neutralReport, 5);
        Assert.True(hostileReport > neutralReport);
        Assert.True(hostileReport < hostileOrder);
        Assert.Equal(0.03, ReportDeliveryResolver.ReportLossProbability(friction, -1), 5);
    }

    [Fact]
    public void TryDeliver_UsesRouteHostilityAndRecordsLoss()
    {
        var friction = new FrictionTable();
        var state = NewState();
        state.Zones["B"].ControllingFaction = Faction.Red;
        var report = Report.Create(100, 120, "S1", 2, ReportKind.Status, "contact", "zone:B");
        var resolver = new ReportDeliveryResolver(friction, new SequenceRandomSource(0.0));

        Assert.False(resolver.TryDeliver(report, state, "A", "B"));
        Assert.Single(state.LostReports);
        Assert.Empty(state.InTransitReports);
        Assert.Contains(state.EventLog, e => e is ReportLost lost && lost.ReportId == report.Id);

        var deliveredState = NewState();
        var delivered = new ReportDeliveryResolver(friction, new SequenceRandomSource(0.99));
        Assert.True(delivered.TryDeliver(report, deliveredState, "A", "B"));
        Assert.Single(deliveredState.InTransitReports);
        Assert.Empty(deliveredState.LostReports);
    }

    [Fact]
    public void Narrator_PreservesStructuredFieldsAndUsesOnlyReportedFacts()
    {
        var templates = TestContent.Bundle().Reports;
        var fact = Report.Create(
            100, 120, "S1", 2, ReportKind.Status, "исходный текст", "fact:owner",
            Faction.Red, "XO",
            new Dictionary<string, string>
            {
                ["zoneId"] = "B",
                ["unit"] = "SCOUT_REPORTED",
                ["strength"] = "77",
                ["value"] = "Red"
            });
        var truthUnit = NewUnit("TRUE_UNIT", 3);
        truthUnit.LocationZoneId = "A";
        truthUnit.Strength = 999;
        var ctx = new ReportContext
        {
            Fact = fact,
            Unit = truthUnit,
            Templates = templates,
            Rng = new Random(1)
        };

        var narrated = new TemplateReportNarrator().Narrate(ctx);

        Assert.Equal(fact.Id, narrated.Id);
        Assert.Equal(100, narrated.EventMinute);
        Assert.Equal(120, narrated.DeliveryMinute);
        Assert.Equal("S1", narrated.SourceId);
        Assert.Equal(2, narrated.Confidence);
        Assert.Equal("fact:owner", narrated.FactKey);
        Assert.Equal(Faction.Red, narrated.SourceFaction);
        Assert.Equal("XO", narrated.ReportedBy);
        Assert.Equal(fact.Facts, narrated.Facts);
        Assert.Contains("SCOUT_REPORTED", narrated.Text, StringComparison.Ordinal);
        Assert.Contains("77%", narrated.Text, StringComparison.Ordinal);
        Assert.Contains("уверенность 2", narrated.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("TRUE_UNIT", narrated.Text, StringComparison.Ordinal);
        Assert.DoesNotContain("999", narrated.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void Narrator_MetadataOverridesMisleadingFactValues()
    {
        var templates = new ReportTemplates
        {
            ByKind = new Dictionary<string, List<string>>
            {
                ["Status"] = ["{sourceId}|{eventMinute}|{deliveryMinute}|{confidence}|{factKey}"]
            }
        };
        var fact = Report.Create(
            90, 130, "REAL_SOURCE", 2, ReportKind.Status, "", "fact:real",
            Faction.Blue, null,
            new Dictionary<string, string>
            {
                ["sourceId"] = "FAKE_SOURCE",
                ["confidence"] = "9",
                ["eventMinute"] = "1",
                ["deliveryMinute"] = "2"
            });
        var ctx = new ReportContext
        {
            Fact = fact,
            Unit = NewUnit("U1", 3),
            Templates = templates,
            Rng = new Random(1)
        };

        var narrated = new TemplateReportNarrator().Narrate(ctx);

        Assert.Equal("REAL_SOURCE|90|130|2|fact:real", narrated.Text);
        Assert.Equal(2, narrated.Confidence);
    }

    private static Unit NewUnit(string id, int honesty) => new()
    {
        Id = id,
        Name = id,
        LocationZoneId = "A",
        InitialStrengthSnapshot = 100,
        Strength = 100,
        Traits = new CommanderTraits(1, 1, 1, honesty)
    };

    private static GameState NewState() => new()
    {
        ScenarioId = "s",
        Clock = new Clock(0),
        Zones = new()
        {
            ["A"] = new Zone { Id = "A", Name = "A", Terrain = TerrainType.Open },
            ["B"] = new Zone { Id = "B", Name = "B", Terrain = TerrainType.Forest }
        },
        Units = new(),
        Knowledge = new PlayerKnowledge()
    };
}
