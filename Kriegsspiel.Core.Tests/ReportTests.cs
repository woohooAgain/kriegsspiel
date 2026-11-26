using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;

namespace Kriegsspiel.Core.Tests;

public class ReportTests
{
    [Fact]
    public void Confidence_MapsToRulesV04()
    {
        var factory = new ReportFactory(new FrictionTable());

        Assert.Equal(3, factory.Create(100, 0, "U1", 1, ReportKind.Arrival).Confidence);
        Assert.Equal(3, factory.Create(100, 0, "U1", 1, ReportKind.Impossible).Confidence);
        Assert.Equal(3, factory.Create(100, 0, "U1", 1, ReportKind.Request).Confidence);
        Assert.Equal(2, factory.Create(100, 0, "U1", 3, ReportKind.Contact).Confidence);
        Assert.Equal(2, factory.Create(100, 0, "U1", 9, ReportKind.QueryAnswer).Confidence);
        Assert.Equal(1, factory.Create(100, 0, "U1", 1, ReportKind.QueryAnswer).Confidence);
        Assert.Equal(3, factory.Create(100, 0, "U1", 3, ReportKind.Recon).Confidence);
        Assert.Equal(1, factory.Create(100, 0, "U1", 2, ReportKind.Recon).Confidence);
        Assert.Equal(3, factory.Create(100, 0, "U1", 9, ReportKind.Status).Confidence);
        Assert.Equal(1, factory.Create(100, 0, "U1", 0, ReportKind.Status).Confidence);
    }

    [Fact]
    public void Factory_SetsRequiredFieldsAndRejectsOutOfRangeConfidence()
    {
        var factory = new ReportFactory(new FrictionTable());

        var report = factory.Create(
            100, 130, "U1", 2, ReportKind.Contact, "контакт", "zone:B", Faction.Red, "XO");

        Assert.True(report.HasRequiredFields);
        Assert.Equal(100, report.EventMinute);
        Assert.Equal(130, report.DeliveryMinute);
        Assert.Equal("U1", report.SourceId);
        Assert.Equal(2, report.Confidence);
        Assert.Equal("zone:B", report.FactKey);
        Assert.Equal(Faction.Red, report.SourceFaction);
        Assert.Equal("XO", report.ReportedBy);

        Assert.Throws<ArgumentOutOfRangeException>(() => Report.Create(100, 130, "U1", 0));
        Assert.Throws<ArgumentOutOfRangeException>(() => Report.Create(100, 130, "U1", 4));
    }

    [Fact]
    public void Factory_ReconResultHasNoReportedOwnStrength()
    {
        var factory = new ReportFactory(new FrictionTable());
        var source = NewUnit("U1", honesty: 3);

        var recon = factory.CreateWithHonesty(
            100, 0, source, 3, ReportKind.Recon, observedZoneId: "B");
        var contact = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact, observedZoneId: "B");

        Assert.Null(recon.ReportedOwnStrength);
        Assert.Equal(100, contact.ReportedOwnStrength);
    }

    [Theory]
    [InlineData(0, 136, 64)]
    [InlineData(1, 124, 76)]
    [InlineData(2, 112, 88)]
    [InlineData(3, 100, 100)]
    public void Factory_HonestyDistortsStrengthsByTwelvePercent(int honesty, int expectedOwn, int expectedEnemy)
    {
        var factory = new ReportFactory(new FrictionTable());
        var source = NewUnit("U1", honesty, 100);
        var enemies = new[] { NewUnit("E1", 3, 100, Faction.Red) };

        var report = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact,
            observedZoneId: "B", observedEnemies: enemies);

        Assert.Equal(expectedOwn, report.ReportedOwnStrength);
        Assert.Equal(expectedEnemy, report.ReportedEnemyStrength);
    }

    [Fact]
    public void Factory_EnemyStrengthIsZeroForReconFailAndNoContact()
    {
        var factory = new ReportFactory(new FrictionTable());
        var source = NewUnit("U1", honesty: 2);
        var enemies = new[] { NewUnit("E1", 3, 100, Faction.Red) };

        var reconFail = factory.CreateWithHonesty(
            100, 0, source, 1, ReportKind.Recon,
            observedZoneId: "B", observedEnemies: enemies);
        var noContact = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact,
            observedZoneId: "B", observedEnemies: Array.Empty<Unit>());

        Assert.Null(reconFail.ReportedOwnStrength);
        Assert.Equal(0, reconFail.ReportedEnemyStrength);
        Assert.Equal(0, noContact.ReportedEnemyStrength);
    }

    [Fact]
    public void Contract_CreateWithHonesty_ReturnsV04StrengthFields()
    {
        IReportFactory factory = new ReportFactory(new FrictionTable());
        var source = NewUnit("U1", honesty: 2);
        var enemies = new[] { NewUnit("E1", 3, 100, Faction.Red) };

        var arrival = factory.CreateWithHonesty(100, 0, source, 1, ReportKind.Arrival);
        var contact = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact,
            observedZoneId: "B", observedEnemies: enemies);

        Assert.Equal(112, arrival.ReportedOwnStrength);
        Assert.Equal(0, arrival.ReportedEnemyStrength);
        Assert.Equal(3, arrival.Confidence);

        Assert.Equal(112, contact.ReportedOwnStrength);
        Assert.Equal(88, contact.ReportedEnemyStrength);
        Assert.Equal("B", contact.ObservedZoneId);
        Assert.Equal(2, contact.Confidence);
    }

    [Fact]
    public void Delivery_RecordsLostReportWhenRandomBelowProbability()
    {
        var state = NewState();
        var unit = NewUnit("U1", 3);
        var report = Report.Create(100, 0, "U1", 2, ReportKind.Contact);
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new SequenceRandomSource(0.0));

        Assert.False(resolver.TryDeliver(report, state, unit));

        Assert.Single(state.LostReports);
        Assert.Empty(state.InTransitReports);
        Assert.Contains(state.EventLog, e => e is ReportLost lost && lost.ReportId == report.Id);
    }

    [Fact]
    public void Delivery_MovesReportIntoTransitWhenNotLost()
    {
        var state = NewState();
        var unit = NewUnit("U1", 3);
        var report = Report.Create(100, 0, "U1", 2, ReportKind.Contact);
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new SequenceRandomSource(0.99));

        Assert.True(resolver.TryDeliver(report, state, unit));

        Assert.Single(state.InTransitReports);
        Assert.Empty(state.LostReports);
        Assert.Empty(state.EventLog.OfType<ReportLost>());
    }

    [Fact]
    public void Delivery_UsesEventMinutePlusLegBackWithMinimumFive()
    {
        var state = NewState();
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new SequenceRandomSource());

        var nodeReport = Report.Create(100, 0, "U1", 2, ReportKind.Contact);
        Assert.True(resolver.TryDeliver(nodeReport, state, NewUnit("U1", 3, 100, Faction.Blue, "B")));
        Assert.Equal(130, nodeReport.DeliveryMinute);

        var shortReport = Report.Create(100, 0, "U2", 2, ReportKind.Contact);
        Assert.True(resolver.TryDeliver(shortReport, state, NewUnit("U2", 3, 100, Faction.Blue, "C")));
        Assert.Equal(105, shortReport.DeliveryMinute);

        var onEdge = NewUnit("U3", 3, 100, Faction.Blue, "HQ");
        onEdge.IsOnEdge = true;
        onEdge.EdgeFromZoneId = "HQ";
        onEdge.EdgeToZoneId = "B";
        var edgeReport = Report.Create(100, 0, "U3", 2, ReportKind.Contact);
        Assert.True(resolver.TryDeliver(edgeReport, state, onEdge));
        Assert.Equal(130, edgeReport.DeliveryMinute);
    }

    [Fact]
    public void Delivery_FixesLegBackAtEventAndDoesNotRecompute()
    {
        var state = NewState();
        var unit = NewUnit("U1", 3, 100, Faction.Blue, "B");
        var report = Report.Create(100, 0, "U1", 2, ReportKind.Contact);
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new SequenceRandomSource());

        Assert.True(resolver.TryDeliver(report, state, unit));
        Assert.Equal(130, report.DeliveryMinute);
        Assert.Equal("30", report.Facts["tLegBack"]);

        unit.LocationZoneId = "C";
        Assert.True(resolver.TryDeliver(report, state, unit));
        Assert.Equal(130, report.DeliveryMinute);
        Assert.Single(state.InTransitReports);
        Assert.Equal(5, ReportDeliveryResolver.ResolveLegBackMinutes(state, unit));
    }

    [Fact]
    public void Delivery_KeepsEveryVersionWithoutMerging()
    {
        var state = NewState();
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new SequenceRandomSource());
        var factory = new ReportFactory(new FrictionTable());
        var source = NewUnit("U1", 3);
        var facts = new Dictionary<string, string> { ["zoneId"] = "B", ["value"] = "Red" };

        var first = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact, "v1", "zone:B", facts,
            observedZoneId: "B", honesty: 3);
        var second = factory.CreateWithHonesty(
            100, 0, source, 2, ReportKind.Contact, "v2", "zone:B", facts,
            observedZoneId: "B", honesty: 0);

        Assert.True(resolver.TryDeliver(first, state, source));
        Assert.True(resolver.TryDeliver(second, state, source));

        Assert.Equal(2, state.InTransitReports.Count);
        Assert.Equal(2, state.InTransitReports.Select(report => report.Id).Distinct().Count());
        Assert.All(state.InTransitReports, report => Assert.Equal("zone:B", report.FactKey));
        Assert.NotEqual(first.ReportedOwnStrength, second.ReportedOwnStrength);
    }

    [Fact]
    public void Delivery_IsDeterministicForSameSeed()
    {
        Assert.Equal(RunDelivery(42), RunDelivery(42));
    }

    [Fact]
    public void Narrator_PreservesV04Structure()
    {
        var templates = new ReportTemplates
        {
            ByKind = new Dictionary<string, List<string>>
            {
                ["Contact"] = ["{sourceId}|{eventMinute}|{deliveryMinute}|{confidence}|{factKey}"]
            }
        };
        var fact = Report.Create(
            90, 130, "REAL_SOURCE", 2, ReportKind.Contact, "", "fact:real",
            Faction.Blue, null,
            new Dictionary<string, string>
            {
                ["sourceId"] = "FAKE_SOURCE",
                ["confidence"] = "9"
            },
            observedZoneId: "B",
            reportedOwnStrength: 77,
            reportedEnemyStrength: 33);
        var ctx = new ReportContext
        {
            Fact = fact,
            Unit = NewUnit("U1", 3),
            Templates = templates,
            Rng = new Random(1)
        };

        var narrated = new TemplateReportNarrator().Narrate(ctx);

        Assert.Equal("REAL_SOURCE|90|130|2|fact:real", narrated.Text);
        Assert.Equal(fact.Id, narrated.Id);
        Assert.Equal("B", narrated.ObservedZoneId);
        Assert.Equal(77, narrated.ReportedOwnStrength);
        Assert.Equal(33, narrated.ReportedEnemyStrength);
    }

    private static List<bool> RunDelivery(int seed)
    {
        var state = NewState();
        var unit = NewUnit("U1", 3);
        var resolver = new ReportDeliveryResolver(new FrictionTable(), new DeterministicRandomSource(seed));
        var delivered = new List<bool>();

        for (var minute = 100; minute < 120; minute++)
        {
            var report = Report.Create(minute, 0, unit.Id, 2, ReportKind.Contact);
            delivered.Add(resolver.TryDeliver(report, state, unit, reportLossProbability: 0.5));
        }

        return delivered;
    }

    private static Unit NewUnit(
        string id,
        int honesty,
        int strength = 100,
        Faction faction = Faction.Blue,
        string zone = "B") => new()
    {
        Id = id,
        Name = id,
        Faction = faction,
        LocationZoneId = zone,
        InitialStrengthSnapshot = strength,
        Strength = strength,
        Traits = new CommanderTraits(1, 1, 1, honesty)
    };

    private static GameState NewState() => new()
    {
        ScenarioId = "s",
        HqZoneId = "HQ",
        Clock = new Clock(0),
        Zones = new Dictionary<string, Zone>
        {
            ["HQ"] = new Zone
            {
                Id = "HQ",
                Name = "HQ",
                CourierMinutes = { ["B"] = 30, ["C"] = 2 }
            },
            ["B"] = new Zone
            {
                Id = "B",
                Name = "B",
                CourierMinutes = { ["HQ"] = 30 }
            },
            ["C"] = new Zone
            {
                Id = "C",
                Name = "C",
                CourierMinutes = { ["HQ"] = 2 }
            }
        },
        Units = new Dictionary<string, Unit>(),
        Knowledge = new PlayerKnowledge()
    };
}
