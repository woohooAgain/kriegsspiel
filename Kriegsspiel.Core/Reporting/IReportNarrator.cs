using System.Globalization;
using Kriegsspiel.Core.Arbitration;
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
            ObservedZoneId = ctx.Fact.ObservedZoneId,
            ReportedOwnStrength = ctx.Fact.ReportedOwnStrength,
            ReportedEnemyStrength = ctx.Fact.ReportedEnemyStrength,
            Text = text,
            Facts = new Dictionary<string, string>(ctx.Fact.Facts)
        };
    }
}

public sealed class ReportFactory : IReportFactory
{
    private const int DefaultHonesty = 3;
    private const double HonestyDistortionPerLevel = 0.12;

    public ReportFactory(FrictionTable friction)
    {
        ArgumentNullException.ThrowIfNull(friction);
    }

    public ReportFactory(FrictionTable friction, IRandomSource random)
        : this(friction)
    {
        ArgumentNullException.ThrowIfNull(random);
    }

    public ReportFactory(FrictionTable friction, Random random)
        : this(friction, new RandomSourceAdapter(random))
    {
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
        Report.Create(
            eventMinute, deliveryMinute, sourceId, NormalizeConfidence(kind, confidence),
            kind, text, factKey, sourceFaction, reportedBy, facts,
            observedZoneId: ZoneIdFromFacts(facts));

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
        int? honesty = null,
        string? observedZoneId = null,
        IReadOnlyCollection<Unit>? observedEnemies = null)
    {
        ArgumentNullException.ThrowIfNull(source);

        var level = Math.Clamp(honesty ?? source.Traits.ReportHonesty, 0, 3);
        var normalized = NormalizeConfidence(kind, confidence);
        int? ownStrength = kind == ReportKind.Recon ? null : DistortStrength(source.Strength, level);
        var enemyStrength = ResolveEnemyStrength(source, kind, normalized, observedEnemies, level);

        return Report.Create(
            eventMinute, deliveryMinute, source.Id, normalized, kind, text, factKey,
            source.Faction, reportedBy, facts,
            observedZoneId ?? ZoneIdFromFacts(facts), ownStrength, enemyStrength);
    }

    public static int NormalizeConfidence(ReportKind kind, int confidence) =>
        kind switch
        {
            ReportKind.Arrival or ReportKind.Impossible or ReportKind.Request => 3,
            ReportKind.Contact => 2,
            ReportKind.QueryAnswer => confidence <= 1 ? 1 : 2,
            ReportKind.Recon => confidence >= 3 ? 3 : 1,
            _ => Math.Clamp(confidence, 1, 3)
        };

    private static int DistortStrength(int strength, int honesty)
    {
        if (honesty >= DefaultHonesty)
            return Math.Max(0, strength);

        var factor = 1 + (DefaultHonesty - honesty) * HonestyDistortionPerLevel;
        return Math.Max(0, (int)Math.Round(strength * factor, MidpointRounding.AwayFromZero));
    }

    private static int DistortEnemyStrength(int strength, int honesty)
    {
        if (honesty >= DefaultHonesty)
            return Math.Max(0, strength);

        var factor = Math.Max(0, 1 - (DefaultHonesty - honesty) * HonestyDistortionPerLevel);
        return Math.Max(0, (int)Math.Round(strength * factor, MidpointRounding.AwayFromZero));
    }

    private static int ResolveEnemyStrength(
        Unit source,
        ReportKind kind,
        int normalizedConfidence,
        IReadOnlyCollection<Unit>? observedEnemies,
        int honesty)
    {
        if (observedEnemies is null || observedEnemies.Count == 0)
            return 0;
        if (kind == ReportKind.Recon && normalizedConfidence < 3)
            return 0;

        var total = observedEnemies
            .Where(unit => unit.Strength > 0 && !unit.Captured && source.IsEnemyOf(unit.Faction))
            .Sum(unit => unit.Strength);
        return DistortEnemyStrength(total, honesty);
    }

    private static string? ZoneIdFromFacts(Dictionary<string, string>? facts) =>
        facts?.GetValueOrDefault("zoneId") ?? facts?.GetValueOrDefault("observedZoneId");
}

public sealed class ReportDeliveryResolver
{
    private const string LegBackFactKey = "tLegBack";

    private readonly FrictionTable _friction;
    private readonly IRandomSource _random;

    public ReportDeliveryResolver(FrictionTable friction, IRandomSource random)
    {
        _friction = friction ?? throw new ArgumentNullException(nameof(friction));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public ReportDeliveryResolver(FrictionTable friction, Random random)
        : this(friction, new RandomSourceAdapter(random))
    {
    }

    public double ReportLossProbability =>
        Math.Clamp(_friction.Hq.ReportLossBase, 0, 1);

    public bool TryDeliver(
        Report report,
        GameState state,
        Unit source,
        double? reportLossProbability = null,
        FrictionTable? friction = null)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(source);

        var table = friction ?? _friction;
        var legBack = ResolveFixedLegBack(report, state, source);
        report.DeliveryMinute = report.EventMinute + legBack;

        var probability = Math.Clamp(reportLossProbability ?? table.Hq.ReportLossBase, 0, 1);
        if (_random.Chance(probability))
        {
            if (!state.LostReports.Any(lost => lost.Id == report.Id))
                state.LostReports.Add(report);
            state.EventLog.Add(new ReportLost(report.EventMinute, report.Id));
            return false;
        }

        if (!state.InTransitReports.Any(inTransit => inTransit.Id == report.Id))
            state.InTransitReports.Add(report);
        return true;
    }

    public static int ResolveLegBackMinutes(GameState state, Unit source)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(source);

        var zoneId = source.IsOnEdge && !string.IsNullOrWhiteSpace(source.EdgeToZoneId)
            ? source.EdgeToZoneId
            : source.LocationZoneId;
        var hqZoneId = state.HqZoneId;
        if (string.IsNullOrWhiteSpace(hqZoneId))
            return PathPlanner.MinCourierLegMinutes;

        var minutes = PathPlanner.CourierLegBackMinutes(state, zoneId, hqZoneId);
        return minutes == int.MaxValue
            ? PathPlanner.MinCourierLegMinutes
            : Math.Max(PathPlanner.MinCourierLegMinutes, minutes);
    }

    private static int ResolveFixedLegBack(Report report, GameState state, Unit source)
    {
        if (report.Facts.TryGetValue(LegBackFactKey, out var value) &&
            int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var fixedMinutes))
            return Math.Max(PathPlanner.MinCourierLegMinutes, fixedMinutes);

        var legBack = ResolveLegBackMinutes(state, source);
        report.Facts[LegBackFactKey] = legBack.ToString(CultureInfo.InvariantCulture);
        return legBack;
    }
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
