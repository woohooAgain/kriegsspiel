using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Tests;

public class FogOfWarTests
{
    private static Report Claim(
        string sourceId,
        string value,
        int eventMinute = 100,
        int deliveryMinute = 120,
        int confidence = 2,
        string factKey = "fact:owner",
        string zoneId = "B") =>
        Report.Create(
            eventMinute, deliveryMinute, sourceId, confidence,
            ReportKind.Contact, value, factKey, Faction.Blue, null,
            new Dictionary<string, string> { ["zoneId"] = zoneId, ["value"] = value });

    [Fact]
    public void Apply_ContradictionLowersConfidenceAndKeepsBothVersions()
    {
        var knowledge = new PlayerKnowledge();
        var updater = new KnowledgeUpdater();

        updater.Apply(knowledge, Claim("S1", "Blue"));
        updater.Apply(knowledge, Claim("S2", "Red", eventMinute: 105, deliveryMinute: 125));

        var claims = knowledge.Facts["fact:owner"];
        Assert.Equal(2, claims.Count);
        Assert.All(claims, claim => Assert.Equal(1, claim.Confidence));
        Assert.True(knowledge.Zones["B"].Conflicting);
        Assert.Equal(2, knowledge.Zones["B"].Versions.Count);
        Assert.Equal(KnowledgePresence.Occupied, knowledge.Zones["B"].Presence);
    }

    [Fact]
    public void Apply_ThirdMatchingReportDoesNotRaiseConflictingVersion()
    {
        var knowledge = new PlayerKnowledge();
        var updater = new KnowledgeUpdater();

        updater.Apply(knowledge, Claim("S1", "Blue"));
        updater.Apply(knowledge, Claim("S2", "Red", eventMinute: 105, deliveryMinute: 125));
        updater.Apply(knowledge, Claim("S3", "Blue", eventMinute: 110, deliveryMinute: 130, confidence: 3));

        var claims = knowledge.Facts["fact:owner"];
        Assert.Equal(2, claims.Count);
        Assert.All(claims, claim => Assert.Equal(0, claim.Confidence));
        Assert.True(knowledge.Zones["B"].Conflicting);
    }

    [Fact]
    public void Apply_CrossConfirmationRequiresDifferentSourceOrObservation()
    {
        var knowledge = new PlayerKnowledge();
        var updater = new KnowledgeUpdater();

        updater.Apply(knowledge, Claim("S1", "Blue"));
        updater.Apply(knowledge, Claim("S1", "Blue", deliveryMinute: 121));
        Assert.Equal(2, knowledge.Facts["fact:owner"][0].Confidence);

        updater.Apply(knowledge, Claim("S2", "Blue", deliveryMinute: 122));
        Assert.Equal(3, knowledge.Facts["fact:owner"][0].Confidence);

        updater.Apply(knowledge, Claim("S1", "Blue", eventMinute: 101, deliveryMinute: 123));
        Assert.Equal(3, knowledge.Facts["fact:owner"][0].Confidence);
        Assert.Single(knowledge.Facts["fact:owner"]);
    }

    [Fact]
    public void Apply_EmptyReportIsDifferentFromNoData()
    {
        var knowledge = new PlayerKnowledge();
        Assert.False(knowledge.HasZoneData("A"));
        Assert.Equal(KnowledgePresence.NoData, knowledge.Zones.GetValueOrDefault("A")?.Presence ?? KnowledgePresence.NoData);

        var updater = new KnowledgeUpdater();
        var empty = Report.Create(
            100, 110, "SCOUT", 3, ReportKind.Recon, "", "zone:A",
            Faction.Blue, null, new Dictionary<string, string> { ["value"] = KnowledgeClaim.EmptyValue });
        updater.Apply(knowledge, empty);

        Assert.True(knowledge.HasZoneData("A"));
        Assert.True(knowledge.Zones["A"].IsEmpty);
        Assert.Single(knowledge.Zones["A"].Versions);
    }

    [Fact]
    public void Apply_KeepsEventAndDeliveryTimestamps()
    {
        var knowledge = new PlayerKnowledge();
        var updater = new KnowledgeUpdater();

        updater.Apply(knowledge, Claim("S1", "Red", eventMinute: 95, deliveryMinute: 130));

        var claim = Assert.Single(knowledge.Facts["fact:owner"]);
        Assert.Equal(95, claim.EventMinute);
        Assert.Equal(130, claim.DeliveryMinute);
        Assert.Equal(95, knowledge.Zones["B"].EventMinute);
        Assert.Equal(130, knowledge.Zones["B"].DeliveryMinute);
        Assert.Equal(130, knowledge.Zones["B"].LastReportMinute);
        Assert.Equal(130, knowledge.LastUpdatedMinute);
    }

    [Fact]
    public void Knowledge_DoesNotExposeHiddenUnitOrZoneTruth()
    {
        var state = new GameState
        {
            ScenarioId = "s",
            Clock = new Clock(0),
            Zones = new(),
            Units = new(),
            Knowledge = new PlayerKnowledge()
        };
        var zone = new Zone { Id = "B", Name = "B", Terrain = TerrainType.Forest, Observation = 1, Cover = 2 };
        zone.PresentUnitIds.Add("SECRET_UNIT");
        state.Zones["B"] = zone;
        state.Units["SECRET_UNIT"] = new Unit
        {
            Id = "SECRET_UNIT",
            Name = "SECRET_UNIT",
            LocationZoneId = "B",
            Experience = 3,
            Concealed = true,
            Traits = new CommanderTraits(1, 1, 1, 0)
        };

        new KnowledgeUpdater().Apply(state.Knowledge, Claim("S1", "Red"));
        var serialized = string.Join(
            "|",
            state.Knowledge.Facts.SelectMany(pair => pair.Value.Select(claim =>
                $"{claim.FactKey}:{claim.Value}:{claim.SourceId}")),
            state.Knowledge.Zones.SelectMany(pair => pair.Value.Versions.Select(claim =>
                $"{claim.FactKey}:{claim.Value}:{claim.SourceId}")));

        Assert.DoesNotContain("SECRET_UNIT", serialized);
        Assert.DoesNotContain("Concealed", serialized);
        Assert.DoesNotContain("Experience", serialized);
        Assert.DoesNotContain("Cover", serialized);
        Assert.DoesNotContain("PresentUnitIds", typeof(PlayerKnowledge).GetProperties().Select(p => p.Name));
        Assert.DoesNotContain("Experience", typeof(PlayerKnowledge).GetProperties().Select(p => p.Name));
    }

    [Fact]
    public void Recon_SuccessProducesAccurateConfidenceThreeReport()
    {
        var friction = new FrictionTable();
        var resolver = new ReconResolver(friction, new SequenceRandomSource(0.0));
        var observer = NewUnit("SCOUT", Faction.Blue, UnitType.Recon);
        var target = NewUnit("ENEMY", Faction.Red, UnitType.Infantry);

        var result = resolver.Observe(observer, new Zone { Id = "B", Name = "B", Observation = 3 }, target, 100, 115);

        Assert.Equal(ReconOutcome.Detected, result.Outcome);
        Assert.Equal(3, result.Report.Confidence);
        Assert.Equal("Red", result.Report.Facts["value"]);
        Assert.Equal(ReportKind.Recon, result.Report.Kind);
        Assert.Equal(0.85, result.Probability, 5);
    }

    [Fact]
    public void Recon_UnconsciousFailProducesPlausibleFalseConfidenceThreeReport()
    {
        var friction = new FrictionTable();
        var resolver = new ReconResolver(friction, new SequenceRandomSource(0.99, 0.0, 0.0));
        var observer = NewUnit("SCOUT", Faction.Blue, UnitType.Recon);
        var target = NewUnit("ENEMY", Faction.Red, UnitType.Infantry);

        var result = resolver.Observe(observer, new Zone { Id = "B", Name = "B", Observation = 0 }, target, 100, 130);

        Assert.Equal(ReconOutcome.UnconsciousFail, result.Outcome);
        Assert.Equal(3, result.Report.Confidence);
        Assert.Equal(KnowledgeClaim.EmptyValue, result.Report.Facts["value"]);
        Assert.Equal(0.3, result.Probability, 5);
    }

    [Fact]
    public void Recon_KnownFailProducesConfidenceOneReport()
    {
        var resolver = new ReconResolver(new FrictionTable(), new SequenceRandomSource(0.99, 0.99));

        var result = resolver.Observe(
            NewUnit("SCOUT", Faction.Blue, UnitType.Recon),
            new Zone { Id = "B", Name = "B", Observation = 2 },
            null, 100, 120);

        Assert.Equal(ReconOutcome.KnownFail, result.Outcome);
        Assert.Equal(1, result.Report.Confidence);
        Assert.Contains("наблюдение невозможно", result.Report.Text, StringComparison.Ordinal);
        Assert.Equal("UNKNOWN", result.Report.Facts["value"]);
    }

    [Fact]
    public void Recon_FullObservabilityErrorRequiresExplicitProfile()
    {
        var friction = new FrictionTable();
        var zone = new Zone { Id = "B", Name = "B", Observation = 3 };
        var observer = NewUnit("SCOUT", Faction.Blue, UnitType.Recon);
        var target = NewUnit("ENEMY", Faction.Red, UnitType.Infantry);

        var literal = new ReconResolver(friction, new SequenceRandomSource(0.99, 0.99));
        Assert.Equal(0, literal.UnconsciousFailProbability(3));

        var withError = new ReconResolver(
            friction, new SequenceRandomSource(0.99, 0.0, 0.0),
            fullObservabilityInterpretationError: 0.2);
        Assert.Equal(0.2, withError.UnconsciousFailProbability(3), 5);
        Assert.Equal(
            ReconOutcome.UnconsciousFail,
            withError.Observe(observer, zone, target, 100, 120).Outcome);
    }

    [Fact]
    public void Recon_MutualDetectionIsSymmetricAndMarksFirstContact()
    {
        var zone = new Zone { Id = "B", Name = "B", Observation = 2 };
        var first = NewUnit("BLUE", Faction.Blue, UnitType.Infantry);
        var enemy = NewUnit("RED", Faction.Red, UnitType.Infantry);
        var resolver = new ReconResolver(new FrictionTable(), new SequenceRandomSource(0.0, 0.99));

        var observations = resolver.ObserveMutual(first, enemy, zone, 100, 110);

        var observation = Assert.Single(observations);
        Assert.True(observation.FirstContact);
        Assert.Equal(Faction.Blue, observation.Report.SourceFaction);
        Assert.Equal("Red", observation.Report.Facts["value"]);
        Assert.Equal(2, observation.Report.Confidence);

        var friend = NewUnit("FRIEND", Faction.Blue, UnitType.Infantry);
        Assert.Empty(resolver.ObserveMutual(first, friend, zone, 100, 110));
    }

    [Fact]
    public void Recon_DetectionProbabilityUsesCoverAndCamouflage()
    {
        var resolver = new ReconResolver(new FrictionTable(), new SequenceRandomSource());

        Assert.Equal(0.35, resolver.DetectionProbability(1, false, 0), 5);
        Assert.Equal(0.05, resolver.DetectionProbability(3, true, 1), 5);
        Assert.Equal(0.65, resolver.DetectionProbability(3, false, 0), 5);
    }

    private static Unit NewUnit(string id, Faction faction, UnitType type) => new()
    {
        Id = id,
        Name = id,
        Faction = faction,
        Type = type,
        LocationZoneId = "B",
        Traits = new CommanderTraits(1, 1, 1, 3)
    };
}

internal sealed class SequenceRandomSource : IRandomSource
{
    private readonly Queue<double> _values;

    public SequenceRandomSource(params double[] values)
    {
        _values = new Queue<double>(values);
        Seed = 1;
        State = 0;
    }

    public int Seed { get; }
    public long State { get; private set; }

    public double NextDouble()
    {
        State++;
        return _values.Count > 0 ? _values.Dequeue() : 0.99;
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        var index = (int)(NextDouble() * (maxExclusive - minInclusive));
        return minInclusive + Math.Clamp(index, 0, maxExclusive - minInclusive - 1);
    }

    public void Restore(long state) => State = state;

    public void Reset() => State = 0;

    public bool Chance(double probability) => NextDouble() < probability;
}
