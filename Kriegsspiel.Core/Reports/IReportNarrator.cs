using System.Globalization;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Reporting;

public sealed class ReportContext
{
    public required Report Fact { get; init; }
    public required Unit Unit { get; init; }
    public required ReportTemplates Templates { get; init; }
    public required Random Rng { get; init; }
}

public interface IReportNarrator
{
    Report Narrate(ReportContext ctx);
}

public sealed class TemplateReportNarrator : IReportNarrator
{
    public Report Narrate(ReportContext ctx)
    {
        ArgumentNullException.ThrowIfNull(ctx);

        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in ctx.Fact.Facts)
            vars[key] = value;

        var kind = ctx.Fact.Kind.ToString();
        vars["eventMinute"] = ctx.Fact.EventMinute.ToString(CultureInfo.InvariantCulture);
        vars["deliveryMinute"] = ctx.Fact.DeliveryMinute.ToString(CultureInfo.InvariantCulture);
        vars["sourceId"] = ctx.Fact.SourceId;
        vars["source"] = string.IsNullOrWhiteSpace(ctx.Fact.ReportedBy) ? ctx.Fact.SourceId : ctx.Fact.ReportedBy;
        vars["confidence"] = ctx.Fact.Confidence.ToString(CultureInfo.InvariantCulture);
        vars["kind"] = kind;
        vars["factKey"] = ctx.Fact.FactKey;

        var text = ctx.Templates.Pick(kind, vars, ctx.Rng);
        return new Report
        {
            Id = ctx.Fact.Id,
            Minute = ctx.Fact.Minute,
            ArrivesAt = ctx.Fact.ArrivesAt,
            FromUnitId = ctx.Fact.FromUnitId,
            Kind = ctx.Fact.Kind,
            Confidence = ctx.Fact.Confidence,
            FactKey = ctx.Fact.FactKey,
            SourceFaction = ctx.Fact.SourceFaction,
            ReportedBy = ctx.Fact.ReportedBy,
            Text = text,
            Facts = new Dictionary<string, string>(ctx.Fact.Facts)
        };
    }
}

public sealed class ReportFactory : IReportFactory
{
    private const int DefaultHonesty = 3;

    private readonly FrictionTable _friction;

    public ReportFactory(FrictionTable friction, IRandomSource random)
        : this(friction)
    {
    }

    public ReportFactory(FrictionTable friction, Random random)
        : this(friction, new RandomSourceAdapter(random))
    {
    }

    public ReportFactory(FrictionTable friction)
    {
        _friction = friction;
    }

    public Report Create(
        int eventMinute,
        int deliveryMinute,
        string sourceId,
        int confidence,
        ReportKind kind = ReportKind.Status,
        string text = "",
        string? factKey = null,
        Faction sourceFaction = Faction.Blue,
        string? reportedBy = null,
        Dictionary<string, string>? facts = null) =>
        CreateCore(
            eventMinute, deliveryMinute, sourceId, NormalizeConfidence(kind, confidence),
            DefaultHonesty, null, sourceFaction, kind, text, factKey, reportedBy, facts);

    public Report CreateWithHonesty(
        int eventMinute,
        int deliveryMinute,
        Unit source,
        int confidence,
        ReportKind kind = ReportKind.Status,
        string text = "",
        string? factKey = null,
        Dictionary<string, string>? facts = null,
        string? reportedBy = null,
        int? honesty = null) =>
        CreateCore(
            eventMinute, deliveryMinute, source.Id, NormalizeConfidence(kind, confidence),
            Math.Clamp(honesty ?? source.Traits.ReportHonesty, 0, 3), source, source.Faction,
            kind, text, factKey, reportedBy, facts);

    public static int NormalizeConfidence(ReportKind kind, int confidence) =>
        kind == ReportKind.Recon
            ? Math.Clamp(confidence, 1, 3)
            : Math.Clamp(confidence, 1, 2);

    private Report CreateCore(
        int eventMinute,
        int deliveryMinute,
        string sourceId,
        int confidence,
        int honesty,
        Unit? source,
        Faction sourceFaction,
        ReportKind kind,
        string text,
        string? factKey,
        string? reportedBy,
        Dictionary<string, string>? facts)
    {
        var (distortedText, distortedFacts) = Distort(text, facts, honesty, source);
        return Report.Create(
            eventMinute, deliveryMinute, sourceId, confidence, kind,
            distortedText, factKey, sourceFaction, reportedBy, distortedFacts);
    }

    private (string Text, Dictionary<string, string> Facts) Distort(
        string text,
        Dictionary<string, string>? facts,
        int honesty,
        Unit? source)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (facts is not null)
            foreach (var (key, value) in facts)
                result[key] = value;

        if (honesty >= DefaultHonesty)
            return (text, result);

        var deficit = DefaultHonesty - honesty;
        var strengthFactor = 1 + deficit * _friction.Reports.HonestyDistortionPerLevel;
        var lossFactor = Math.Max(0, 1 - deficit * _friction.Reports.HonestyDistortionPerLevel);
        var actualLosses = LossValue(result);

        foreach (var key in result.Keys.ToList())
        {
            if (IsStrengthKey(key) &&
                int.TryParse(result[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var strength))
            {
                result[key] = Math.Max(0, (int)Math.Round(strength * strengthFactor, MidpointRounding.AwayFromZero))
                    .ToString(CultureInfo.InvariantCulture);
            }
            else if (IsLossKey(key) &&
                     int.TryParse(result[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var losses))
            {
                result[key] = Math.Max(0, (int)Math.Round(losses * lossFactor, MidpointRounding.AwayFromZero))
                    .ToString(CultureInfo.InvariantCulture);
            }
        }

        var initialStrength = InitialStrength(result, source);
        if (actualLosses > 0 && initialStrength > 0 && actualLosses >= initialStrength / 3.0)
        {
            result["status"] = "держимся";
            if (text.Contains("потер", StringComparison.OrdinalIgnoreCase))
                text = "держимся";
        }

        return (text, result);
    }

    private static bool IsStrengthKey(string key) =>
        key.Equals("strength", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("currentStrength", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("troops", StringComparison.OrdinalIgnoreCase);

    private static bool IsLossKey(string key) =>
        key.Equals("losses", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("loss", StringComparison.OrdinalIgnoreCase) ||
        key.Equals("casualties", StringComparison.OrdinalIgnoreCase);

    private static int LossValue(IReadOnlyDictionary<string, string> facts)
    {
        foreach (var key in facts.Keys)
            if (IsLossKey(key) && int.TryParse(facts[key], NumberStyles.Integer, CultureInfo.InvariantCulture, out var losses))
                return losses;
        return 0;
    }

    private static int InitialStrength(IReadOnlyDictionary<string, string> facts, Unit? source)
    {
        if (facts.TryGetValue("initialStrength", out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var initial))
            return initial;
        if (source is null)
            return 0;
        return source.InitialStrengthSnapshot > 0 ? source.InitialStrengthSnapshot : source.Strength;
    }
}

public sealed class ReportDeliveryResolver
{
    private const double ReportHostilityWeight = 0.5;
    private const double OrderLossCeilingRatio = 0.9;
    private const int HostileZoneReferenceCount = 3;

    private readonly FrictionTable _friction;
    private readonly IRandomSource _random;

    public ReportDeliveryResolver(FrictionTable friction, IRandomSource random)
    {
        _friction = friction;
        _random = random;
    }

    public ReportDeliveryResolver(FrictionTable friction, Random random)
        : this(friction, new RandomSourceAdapter(random))
    {
    }

    public static double OrderLossProbability(FrictionTable friction, double hostility) =>
        Math.Clamp(
            friction.Hq.CourierLossBase + Math.Clamp(hostility, 0, 1) * friction.Hq.CourierLossHostileBonus,
            0, 1);

    public static double ReportLossProbability(FrictionTable friction, double hostility)
    {
        var normalized = Math.Clamp(hostility, 0, 1);
        var orderLoss = OrderLossProbability(friction, normalized);
        var reportLoss = friction.Hq.ReportLossBase + normalized * friction.Hq.CourierLossHostileBonus * ReportHostilityWeight;
        return Math.Clamp(Math.Min(reportLoss, orderLoss * OrderLossCeilingRatio), 0, 1);
    }

    public static double RouteHostility(GameState state, Faction reporter, string? fromZoneId, string? toZoneId)
    {
        var zones = new List<Zone>();
        if (fromZoneId is not null && state.Zones.TryGetValue(fromZoneId, out var from))
            zones.Add(from);
        if (toZoneId is not null && state.Zones.TryGetValue(toZoneId, out var to) && !zones.Contains(to))
            zones.Add(to);
        if (zones.Count == 0)
            zones.AddRange(state.Zones.Values);

        var hostile = zones.Count(zone =>
            (zone.ControllingFaction is { } faction && IsEnemy(faction, reporter)) ||
            zone.PresentUnitIds.Any(id =>
                state.Units.TryGetValue(id, out var unit) && unit.IsEnemyOf(reporter) && !unit.Captured));

        return Math.Min(1, hostile / (double)Math.Max(HostileZoneReferenceCount, zones.Count));
    }

    public bool TryDeliver(
        Report report,
        GameState state,
        string? fromZoneId = null,
        string? toZoneId = null,
        FrictionTable? friction = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(state);

        var table = friction ?? _friction;
        fromZoneId ??= report.Facts.GetValueOrDefault("fromZoneId");
        toZoneId ??= report.Facts.GetValueOrDefault("toZoneId") ?? report.Facts.GetValueOrDefault("zoneId");
        var hostility = RouteHostility(state, report.SourceFaction, fromZoneId, toZoneId);
        if (_random.Chance(ReportLossProbability(table, hostility)))
        {
            state.LostReports.Add(report);
            state.EventLog.Add(new ReportLost(report.DeliveryMinute, report.Id));
            return false;
        }

        state.InTransitReports.Add(report);
        return true;
    }

    private static bool IsEnemy(Faction left, Faction right) =>
        left != Faction.Neutral && right != Faction.Neutral && left != right;
}

internal sealed class RandomSourceAdapter : IRandomSource
{
    private Random _random;

    public RandomSourceAdapter(int seed)
    {
        Seed = seed;
        State = seed;
        _random = new Random(seed);
    }

    public RandomSourceAdapter(Random random)
    {
        Seed = 0;
        State = 0;
        _random = random;
    }

    public int Seed { get; }
    public long State { get; private set; }

    public double NextDouble()
    {
        State++;
        return _random.NextDouble();
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        State++;
        return _random.Next(minInclusive, maxExclusive);
    }

    public void Restore(long state)
    {
        State = state;
        var seed = (int)(Math.Abs(state) % int.MaxValue);
        _random = new Random(seed == 0 ? 1 : seed);
    }

    public void Reset() => Restore(Seed);
}
