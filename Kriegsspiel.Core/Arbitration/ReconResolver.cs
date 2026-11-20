using System.Globalization;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;

namespace Kriegsspiel.Core.Arbitration;

public enum ReconOutcome
{
    Detected,
    UnconsciousFail,
    KnownFail
}

public sealed record ReconObservation(
    ReconOutcome Outcome,
    Report Report,
    double Probability,
    bool FirstContact = false);

public sealed class ReconResolver
{
    private const int ObservabilityMax = 3;

    private readonly FrictionTable _friction;
    private readonly IRandomSource _random;
    private readonly IReportFactory _factory;
    private readonly double _interpretationError;

    public ReconResolver(
        FrictionTable friction,
        IRandomSource random,
        IReportFactory? factory = null,
        double fullObservabilityInterpretationError = 0)
    {
        _friction = friction;
        _random = random;
        _factory = factory ?? new ReportFactory(friction, random);
        _interpretationError = Math.Clamp(fullObservabilityInterpretationError, 0, 1);
    }

    public ReconResolver(
        FrictionTable friction,
        Random random,
        IReportFactory? factory = null,
        double fullObservabilityInterpretationError = 0)
        : this(friction, new RandomSourceAdapter(random), factory, fullObservabilityInterpretationError)
    {
    }

    public double SuccessProbability(int observability, int observationMinutes, FrictionTable? friction = null)
    {
        var table = friction ?? _friction;
        var obs = Math.Clamp(observability, 0, ObservabilityMax);
        var window = Math.Max(1, table.Recon.FactKeyWindowMinutes);
        var minutes = Math.Max(0, observationMinutes);
        var probability = table.Recon.SuccessBase +
                          table.Recon.DetectionObservabilityWeight * obs -
                          table.Recon.DetectionBase * (minutes / (double)window);
        return Math.Clamp(
            probability,
            Math.Clamp(table.Recon.DetectionMin, 0, 1),
            Math.Clamp(table.Recon.DetectionMax, 0, 1));
    }

    public double UnconsciousFailProbability(int observability, FrictionTable? friction = null)
    {
        var table = friction ?? _friction;
        var obs = Math.Clamp(observability, 0, ObservabilityMax);
        var baseProbability = Math.Clamp(table.Recon.UnconsciousFailBase, 0, 1);
        var probability = baseProbability * (ObservabilityMax - obs) / (double)ObservabilityMax + _interpretationError;
        return Math.Clamp(probability, 0, Math.Clamp(table.Recon.UnconsciousFailCap, 0, 1));
    }

    public double DetectionProbability(int observability, bool concealed, double cover, FrictionTable? friction = null)
    {
        var table = friction ?? _friction;
        var obs = Math.Clamp(observability, 0, ObservabilityMax);
        var probability = table.Recon.DetectionBase +
                          table.Recon.DetectionObservabilityWeight * obs +
                          table.Recon.OpDetectionBonus -
                          (concealed ? table.Recon.CamouflageStealthBonus : 0) -
                          Math.Max(0, cover);
        return Math.Clamp(
            probability,
            Math.Clamp(table.Recon.DetectionMin, 0, 1),
            Math.Clamp(table.Recon.DetectionMax, 0, 1));
    }

    public ReconObservation Observe(
        Unit observer,
        Zone zone,
        Unit? target,
        int eventMinute,
        int deliveryMinute,
        FrictionTable? friction = null)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentNullException.ThrowIfNull(zone);

        var table = friction ?? _friction;
        var successProbability = SuccessProbability(zone.Observability, deliveryMinute - eventMinute, table);
        if (_random.Chance(successProbability))
        {
            var value = target is null
                ? KnowledgeClaim.EmptyValue
                : target.Concealed ? "Civilians" : target.Faction.ToString();
            var text = target is null
                ? "район пуст"
                : target.Concealed ? "гражданские в районе" : $"район занят: {value.ToLowerInvariant()}";
            return new ReconObservation(
                ReconOutcome.Detected,
                Build(observer, zone, eventMinute, deliveryMinute, ReportKind.Recon, 3, text, value),
                successProbability);
        }

        var unconsciousProbability = UnconsciousFailProbability(zone.Observability, table);
        if (_random.Chance(unconsciousProbability))
        {
            var alternatives = target is null
                ? new[] { Faction.Red.ToString(), Faction.Blue.ToString() }
                : new[] { KnowledgeClaim.EmptyValue, OtherFaction(target.Faction).ToString() };
            var value = alternatives[_random.NextInt(0, alternatives.Length)];
            var text = string.Equals(value, KnowledgeClaim.EmptyValue, StringComparison.OrdinalIgnoreCase)
                ? "район пуст"
                : $"опознано: {value.ToLowerInvariant()}";
            return new ReconObservation(
                ReconOutcome.UnconsciousFail,
                Build(observer, zone, eventMinute, deliveryMinute, ReportKind.Recon, 3, text, value),
                unconsciousProbability);
        }

        return new ReconObservation(
            ReconOutcome.KnownFail,
            Build(
                observer, zone, eventMinute, deliveryMinute, ReportKind.Recon, 1,
                "наблюдение невозможно, позиции не установлены", "UNKNOWN"),
            1 - unconsciousProbability);
    }

    public IReadOnlyList<ReconObservation> ObserveMutual(
        Unit first,
        Unit second,
        Zone zone,
        int eventMinute,
        int deliveryMinute,
        FrictionTable? friction = null)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);
        ArgumentNullException.ThrowIfNull(zone);

        if (!first.IsEnemyOf(second.Faction))
            return [];

        var table = friction ?? _friction;
        var firstProbability = DetectionProbability(zone.Observability, second.Concealed, zone.Cover, table);
        var secondProbability = DetectionProbability(zone.Observability, first.Concealed, zone.Cover, table);
        var firstSees = _random.Chance(firstProbability);
        var secondSees = _random.Chance(secondProbability);
        var confidence = zone.Observability >= 2 ? 2 : 1;

        var observations = new List<ReconObservation>(2);
        if (firstSees)
            observations.Add(Contact(first, second, zone, eventMinute, deliveryMinute, confidence, firstProbability, firstSees && !secondSees));
        if (secondSees)
            observations.Add(Contact(second, first, zone, eventMinute, deliveryMinute, confidence, secondProbability, secondSees && !firstSees));
        return observations;
    }

    private ReconObservation Contact(
        Unit observer,
        Unit observed,
        Zone zone,
        int eventMinute,
        int deliveryMinute,
        int confidence,
        double probability,
        bool firstContact)
    {
        var value = observed.Concealed ? "Civilians" : observed.Faction.ToString();
        var text = observed.Concealed ? "гражданские в районе" : "контакт в районе";
        return new ReconObservation(
            ReconOutcome.Detected,
            Build(observer, zone, eventMinute, deliveryMinute, ReportKind.Contact, confidence, text, value),
            probability,
            firstContact);
    }

    private Report Build(
        Unit observer,
        Zone zone,
        int eventMinute,
        int deliveryMinute,
        ReportKind kind,
        int confidence,
        string text,
        string value)
    {
        var factKey = "zone:" + zone.Id;
        var facts = new Dictionary<string, string>
        {
            ["zoneId"] = zone.Id,
            ["value"] = value,
            ["factKey"] = factKey,
            ["observability"] = zone.Observability.ToString(CultureInfo.InvariantCulture)
        };
        return _factory.Create(
            eventMinute, deliveryMinute, observer.Id, confidence, kind,
            text, factKey, observer.Faction, null, facts);
    }

    private static Faction OtherFaction(Faction faction) =>
        faction == Faction.Red ? Faction.Blue : Faction.Red;
}
