using Kriegsspiel.Core.Content;

namespace Kriegsspiel.Core.Orders;

public sealed record OrderScoringContext(
    string? CurrentZoneId = null,
    string? ReserveZoneId = null,
    string? GuardObjectId = null,
    string? ResolvedTargetId = null,
    IReadOnlyCollection<string>? KnownZoneIds = null);

public sealed record AmbiguityBreakdown(
    double MissingTarget,
    double Vague,
    double MissingDeadline,
    double MissingReportBy,
    double Branches,
    double Total);

public static class AmbiguityScorer
{
    private static readonly FrictionTable DefaultFriction = new();

    private static readonly string[] VagueMarkers =
    [
        "как-нибудь", "при возможности", "если получится", "на усмотрение",
        "по обстановке", "потом", "maybe", "asap", "somehow"
    ];

    public static double Score(Order order) => Breakdown(order, null, null).Total;

    public static double Score(Order order, FrictionTable friction) => Breakdown(order, null, friction).Total;

    public static double Score(Order order, OrderScoringContext? context) => Breakdown(order, context, null).Total;

    public static double Score(Order order, OrderScoringContext? context, FrictionTable? friction) =>
        Breakdown(order, context, friction).Total;

    public static AmbiguityBreakdown Breakdown(
        Order order,
        OrderScoringContext? context = null,
        FrictionTable? friction = null)
    {
        ArgumentNullException.ThrowIfNull(order);

        var settings = friction ?? DefaultFriction;
        var weights = settings.Orders.Ambiguity;
        var missingTarget = HasMissingTarget(order, context) ? weights.MissingTarget : 0.0;
        var vague = IsVague(order) ? weights.VagueFormulation : 0.0;
        var missingDeadline = order.NotLaterThan is null ? weights.MissingDeadline : 0.0;
        var missingReportBy = order.ReportBy is null ? weights.MissingReportTime : 0.0;
        var branches = Math.Min(order.Branches.Count, settings.Orders.MaxBranches) * weights.PerBranch;
        var total = Math.Clamp(missingTarget + vague + missingDeadline + missingReportBy + branches, 0.0, 1.0);

        return new AmbiguityBreakdown(missingTarget, vague, missingDeadline, missingReportBy, branches, total);
    }

    public static bool HasTarget(Order order)
    {
        ArgumentNullException.ThrowIfNull(order);
        return HasTargetSlots(order.TargetZoneId, order.TargetObjectId, order.TargetUnitId, order.TargetEdgeId);
    }

    private static bool HasTargetSlots(string? zone, string? obj, string? unit, string? edge) =>
        !string.IsNullOrWhiteSpace(zone) ||
        !string.IsNullOrWhiteSpace(obj) ||
        !string.IsNullOrWhiteSpace(unit) ||
        !string.IsNullOrWhiteSpace(edge);

    private static bool HasMissingTarget(Order order, OrderScoringContext? context)
    {
        if (HasTarget(order) || !string.IsNullOrWhiteSpace(context?.ResolvedTargetId)) return false;

        return order.Intent switch
        {
            OrderIntent.Hold => false,
            OrderIntent.Reserve => string.IsNullOrWhiteSpace(context?.ReserveZoneId),
            OrderIntent.Guard => string.IsNullOrWhiteSpace(context?.GuardObjectId) &&
                                 string.IsNullOrWhiteSpace(context?.CurrentZoneId),
            _ => true
        };
    }

    private static bool IsVague(Order order) =>
        order.VaguePatterns.Count > 0 ||
        (order.OriginalText is { } text &&
         VagueMarkers.Any(marker => text.Contains(marker, StringComparison.OrdinalIgnoreCase)));
}
