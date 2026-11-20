using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed record RouteStep(
    string FromZoneId,
    string ToZoneId,
    string EdgeId,
    EdgeType EdgeType,
    int Minutes,
    int ReadinessDelta,
    int FatigueDelta);

public sealed record RoutePlan(
    string FromZoneId,
    string TargetZoneId,
    MovementMode MovementMode,
    IReadOnlyList<RouteStep> Steps,
    int TotalMinutes)
{
    public bool IsEmpty => Steps.Count == 0;
}

public static class PathPlanner
{
    private const int RoadReadinessDelta = -3;
    private const int OffroadReadinessDelta = -6;
    private const int CrossingReadinessDelta = -4;
    private const int OffroadExtraFatigue = 2;
    private const double MinimumMultiplier = 0.1;

    public static string EdgeId(string firstZoneId, string secondZoneId) =>
        string.CompareOrdinal(firstZoneId, secondZoneId) <= 0
            ? $"{firstZoneId}-{secondZoneId}"
            : $"{secondZoneId}-{firstZoneId}";

    public static bool IsEdgePassable(
        GameState state,
        string fromZoneId,
        string toZoneId,
        IReadOnlySet<string>? blockedEdges = null)
    {
        if (fromZoneId == toZoneId) return true;
        if (blockedEdges?.Contains(EdgeId(fromZoneId, toZoneId)) == true) return false;
        if (!state.Zones.TryGetValue(fromZoneId, out var from) ||
            !state.Zones.TryGetValue(toZoneId, out var to)) return false;
        if (!from.TravelMinutes.TryGetValue(toZoneId, out var travel) || travel <= 0 ||
            !to.TravelMinutes.ContainsKey(fromZoneId)) return false;

        var edgeId = EdgeId(fromZoneId, toZoneId);
        var edgeType = from.EdgeTypeFor(toZoneId);
        if (edgeType == EdgeType.Crossing && (!from.BridgeIntact || !to.BridgeIntact)) return false;

        foreach (var obj in from.Objects.Concat(to.Objects))
        {
            if (obj.IsIntact) continue;
            if (obj.EdgeId == edgeId) return false;
            if (obj.IsBridge && obj.EdgeId is null && (edgeType == EdgeType.Crossing || from.IsCrossing || to.IsCrossing))
                return false;
        }
        return true;
    }

    public static bool TryPlan(
        GameState state,
        string fromZoneId,
        string toZoneId,
        MovementMode movementMode,
        FrictionTable friction,
        out RoutePlan plan,
        IReadOnlySet<string>? blockedEdges = null)
    {
        plan = new RoutePlan(fromZoneId, toZoneId, movementMode, [], 0);
        if (!state.Zones.ContainsKey(fromZoneId) || !state.Zones.ContainsKey(toZoneId)) return false;
        if (fromZoneId == toZoneId) return true;

        var mode = ModeParameters(friction, movementMode);
        var distance = new Dictionary<string, int> { [fromZoneId] = 0 };
        var previous = new Dictionary<string, (string FromZoneId, RouteStep Step)>();
        var settled = new HashSet<string>();
        var frontier = new PriorityQueue<string, int>();
        frontier.Enqueue(fromZoneId, 0);

        while (frontier.TryDequeue(out var current, out var currentDistance))
        {
            if (!settled.Add(current)) continue;
            if (current == toZoneId) break;
            if (!state.Zones.TryGetValue(current, out var zone)) continue;

            foreach (var (neighbor, travel) in zone.TravelMinutes)
            {
                if (travel <= 0 || !IsEdgePassable(state, current, neighbor, blockedEdges)) continue;
                var step = CreateStep(zone, current, neighbor, travel, movementMode, mode);
                var candidate = currentDistance + step.Minutes;
                if (candidate >= distance.GetValueOrDefault(neighbor, int.MaxValue)) continue;
                distance[neighbor] = candidate;
                previous[neighbor] = (current, step);
                frontier.Enqueue(neighbor, candidate);
            }
        }

        if (!distance.TryGetValue(toZoneId, out var totalMinutes)) return false;

        var reversed = new List<RouteStep>();
        var cursor = toZoneId;
        while (cursor != fromZoneId)
        {
            if (!previous.TryGetValue(cursor, out var link)) return false;
            reversed.Add(link.Step);
            cursor = link.FromZoneId;
        }
        reversed.Reverse();
        plan = new RoutePlan(fromZoneId, toZoneId, movementMode, reversed, totalMinutes);
        return true;
    }

    public static int TravelMinutes(Zone zone, int baseTravel, MovementMode movementMode, FrictionTable friction)
    {
        var multiplier = Math.Max(MinimumMultiplier, zone.MoveMultiplier) *
                         Math.Max(MinimumMultiplier, ModeParameters(friction, movementMode).TimeMultiplier);
        return Math.Max(1, (int)Math.Ceiling(baseTravel * multiplier));
    }

    public static int ReadinessDelta(EdgeType edgeType, MovementMode movementMode, FrictionTable friction) =>
        EdgeReadinessDelta(edgeType) + ModeParameters(friction, movementMode).ReadinessDelta;

    public static int FatigueDelta(EdgeType edgeType, MovementMode movementMode, FrictionTable friction) =>
        ModeParameters(friction, movementMode).FatigueDelta + (edgeType == EdgeType.Offroad ? OffroadExtraFatigue : 0);

    private static RouteStep CreateStep(
        Zone zone,
        string fromZoneId,
        string toZoneId,
        int baseTravel,
        MovementMode movementMode,
        FrictionTable.MovementMode mode)
    {
        var edgeType = zone.EdgeTypeFor(toZoneId);
        return new RouteStep(
            fromZoneId,
            toZoneId,
            EdgeId(fromZoneId, toZoneId),
            edgeType,
            TravelMinutes(zone, baseTravel, movementMode, mode.TimeMultiplier),
            EdgeReadinessDelta(edgeType) + mode.ReadinessDelta,
            mode.FatigueDelta + (edgeType == EdgeType.Offroad ? OffroadExtraFatigue : 0));
    }

    private static int TravelMinutes(Zone zone, int baseTravel, MovementMode movementMode, double modeMultiplier)
    {
        var multiplier = Math.Max(MinimumMultiplier, zone.MoveMultiplier) * Math.Max(MinimumMultiplier, modeMultiplier);
        return Math.Max(1, (int)Math.Ceiling(baseTravel * multiplier));
    }

    private static int EdgeReadinessDelta(EdgeType edgeType) => edgeType switch
    {
        EdgeType.Road => RoadReadinessDelta,
        EdgeType.Offroad => OffroadReadinessDelta,
        EdgeType.Crossing => CrossingReadinessDelta,
        _ => RoadReadinessDelta
    };

    private static FrictionTable.MovementMode ModeParameters(FrictionTable friction, MovementMode mode) => mode switch
    {
        MovementMode.March => friction.Movement.March,
        MovementMode.Covert => friction.Movement.Stealth,
        MovementMode.Contact => friction.Movement.ContactReady,
        _ => throw new ArgumentOutOfRangeException(nameof(mode), mode, "Unknown movement mode.")
    };
}
