using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed record RouteStep(
    string FromZoneId,
    string ToZoneId,
    string EdgeId,
    EdgeType EdgeType,
    int Minutes);

public sealed record RoutePlan(
    string FromZoneId,
    string TargetZoneId,
    IReadOnlyList<RouteStep> Steps,
    int TotalMinutes)
{
    public bool IsEmpty => Steps.Count == 0;
}

public static class PathPlanner
{
    public const int MinCourierLegMinutes = 5;
    private const double OffroadWeightMultiplier = 0.8;

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
        int? deadline,
        string? forbiddenZoneId,
        out RoutePlan plan,
        IReadOnlySet<string>? blockedEdges = null)
    {
        plan = new RoutePlan(fromZoneId, toZoneId, [], 0);
        if (!state.Zones.ContainsKey(fromZoneId) || !state.Zones.ContainsKey(toZoneId)) return false;
        if (fromZoneId == toZoneId) return true;
        if (forbiddenZoneId == toZoneId) return false;

        bool hasDeadline = deadline.HasValue;
        var cost = new Dictionary<string, double> { [fromZoneId] = 0 };
        var previous = new Dictionary<string, (string FromZoneId, RouteStep Step)>();
        var settled = new HashSet<string>();
        var frontier = new PriorityQueue<string, (double Cost, long Sequence)>();
        long sequence = 0;
        frontier.Enqueue(fromZoneId, (0, sequence++));

        while (frontier.TryDequeue(out var current, out var key))
        {
            if (!settled.Add(current)) continue;
            if (key.Cost > cost.GetValueOrDefault(current, double.PositiveInfinity)) continue;
            if (current == toZoneId) break;
            if (!state.Zones.TryGetValue(current, out var zone)) continue;

            foreach (var neighbor in zone.TravelMinutes.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                var travel = zone.TravelMinutes[neighbor];
                if (travel <= 0) continue;
                if (IsForbiddenNode(forbiddenZoneId, fromZoneId, neighbor)) continue;
                if (!IsEdgePassable(state, current, neighbor, blockedEdges)) continue;

                var edgeType = zone.EdgeTypeFor(neighbor);
                var step = new RouteStep(current, neighbor, EdgeId(current, neighbor), edgeType, travel);
                var weight = hasDeadline
                    ? travel
                    : travel * (edgeType == EdgeType.Offroad ? OffroadWeightMultiplier : 1.0);
                var candidate = key.Cost + weight;
                if (candidate >= cost.GetValueOrDefault(neighbor, double.PositiveInfinity)) continue;

                cost[neighbor] = candidate;
                previous[neighbor] = (current, step);
                frontier.Enqueue(neighbor, (candidate, sequence++));
            }
        }

        if (!cost.ContainsKey(toZoneId)) return false;

        var reversed = new List<RouteStep>();
        var cursor = toZoneId;
        while (cursor != fromZoneId)
        {
            if (!previous.TryGetValue(cursor, out var link)) return false;
            reversed.Add(link.Step);
            cursor = link.FromZoneId;
        }
        reversed.Reverse();

        var totalMinutes = reversed.Sum(step => step.Minutes);
        plan = new RoutePlan(fromZoneId, toZoneId, reversed, totalMinutes);
        return true;
    }

    public static int CourierLegMinutes(
        GameState state,
        string fromZoneId,
        string toZoneId,
        IReadOnlySet<string>? blockedEdges = null)
    {
        var shortest = ShortestCourierMinutes(state, fromZoneId, toZoneId, blockedEdges);
        return shortest == int.MaxValue ? int.MaxValue : Math.Max(MinCourierLegMinutes, shortest);
    }

    public static int CourierLegBackMinutes(
        GameState state,
        string fromZoneId,
        string toZoneId,
        IReadOnlySet<string>? blockedEdges = null) =>
        CourierLegMinutes(state, fromZoneId, toZoneId, blockedEdges);

    private static bool IsForbiddenNode(string? forbiddenZoneId, string startZoneId, string nodeId)
    {
        if (forbiddenZoneId is null) return false;
        if (forbiddenZoneId == startZoneId) return nodeId == startZoneId;
        return nodeId == forbiddenZoneId;
    }

    private static int ShortestCourierMinutes(
        GameState state,
        string fromZoneId,
        string toZoneId,
        IReadOnlySet<string>? blockedEdges)
    {
        if (fromZoneId == toZoneId) return 0;
        if (!state.Zones.ContainsKey(fromZoneId) || !state.Zones.ContainsKey(toZoneId)) return int.MaxValue;

        var distance = new Dictionary<string, int> { [fromZoneId] = 0 };
        var settled = new HashSet<string>();
        var frontier = new PriorityQueue<string, (int Minutes, long Sequence)>();
        long sequence = 0;
        frontier.Enqueue(fromZoneId, (0, sequence++));

        while (frontier.TryDequeue(out var current, out var key))
        {
            if (!settled.Add(current)) continue;
            if (key.Minutes > distance.GetValueOrDefault(current, int.MaxValue)) continue;
            if (current == toZoneId) return key.Minutes;
            if (!state.Zones.TryGetValue(current, out var zone)) continue;

            foreach (var neighbor in zone.CourierMinutes.Keys.OrderBy(id => id, StringComparer.Ordinal))
            {
                var minutes = zone.CourierMinutes[neighbor];
                if (minutes <= 0) continue;
                if (blockedEdges?.Contains(EdgeId(current, neighbor)) == true) continue;
                var candidate = key.Minutes + minutes;
                if (candidate >= distance.GetValueOrDefault(neighbor, int.MaxValue)) continue;
                distance[neighbor] = candidate;
                frontier.Enqueue(neighbor, (candidate, sequence++));
            }
        }

        return distance.GetValueOrDefault(toZoneId, int.MaxValue);
    }
}
