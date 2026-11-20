using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Orders;

public sealed record BranchEvaluationContext(
    bool TargetHeldByEnemy,
    bool BattleEnRoute,
    bool DeadlineExpired,
    bool BridgeDestroyed,
    bool ContradictoryOrderReceived,
    bool CommsLost,
    int Minute)
{
    public bool Matches(BranchCondition condition) => condition switch
    {
        BranchCondition.TargetHeldByEnemy => TargetHeldByEnemy,
        BranchCondition.BattleEnRoute => BattleEnRoute,
        BranchCondition.DeadlineExpired => DeadlineExpired,
        BranchCondition.BridgeDestroyed => BridgeDestroyed,
        BranchCondition.ContradictoryOrderReceived => ContradictoryOrderReceived,
        BranchCondition.CommsLost => CommsLost,
        _ => false
    };

    public static BranchEvaluationContext From(GameState state, Order order, Unit unit, int minute)
    {
        ArgumentNullException.ThrowIfNull(state);
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(unit);

        var targetZoneId = order.TargetZoneId ?? unit.LocationZoneId;
        var heldByEnemy = targetZoneId is not null &&
            state.Knowledge.Zones.TryGetValue(targetZoneId, out var knownZone) &&
            knownZone.ReportedFaction is { } reportedFaction &&
            unit.IsEnemyOf(reportedFaction);

        var battle = HasFact(state, "combat:" + targetZoneId, "true", "engaged", "battle") ||
                     HasFact(state, "battle:" + targetZoneId, "true", "engaged", "battle") ||
                     state.EventLog.Any(evt => evt is CombatTick tick &&
                         tick.Minute <= minute && tick.Minute >= Math.Max(0, (order.DeliveredMinute ?? minute) - 30));

        var bridgeDestroyed = HasFactPrefixed(state, "bridge:" + targetZoneId, "destroyed", "true") ||
                              HasFact(state, "bridgeDestroyed:" + targetZoneId, "true");

        var contradictory = !string.IsNullOrWhiteSpace(unit.CurrentOrderId) &&
                            unit.CurrentOrderId != order.Id &&
                            state.Orders.Any(existing => existing.Id == unit.CurrentOrderId &&
                                (existing.DeliveredMinute ?? existing.IssuedAt) <= minute);

        var commsLost = HasFact(state, "comms", "lost") ||
                        HasFact(state, "commsLost", "true") ||
                        HasFactPrefixed(state, "comms:", "lost");

        return new BranchEvaluationContext(
            heldByEnemy,
            battle,
            order.NotLaterThan is { } deadline && deadline <= minute,
            bridgeDestroyed,
            contradictory,
            commsLost,
            minute);
    }

    private static bool HasFact(GameState state, string key, params string[] values) =>
        state.Knowledge.Facts.TryGetValue(key, out var claims) &&
        claims.Any(claim => values.Any(value =>
            string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase)));

    private static bool HasFactPrefixed(GameState state, string prefix, params string[] values) =>
        state.Knowledge.Facts.Any(entry =>
            entry.Key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
            entry.Value.Any(claim => values.Any(value =>
                string.Equals(claim.Value, value, StringComparison.OrdinalIgnoreCase))));
}

public static class OrderBranchRules
{
    public static bool IsValidStructure(IReadOnlyList<OrderBranch> branches, int maxBranches = 3) =>
        branches.Count <= maxBranches && branches.All(IsValidBranch);

    public static OrderBranch? Select(
        IReadOnlyList<OrderBranch> branches,
        BranchEvaluationContext context,
        int maxBranches = 3)
    {
        if (!IsValidStructure(branches, maxBranches)) return null;

        foreach (var branch in branches)
        {
            if (context.Matches(branch.Condition)) return branch;
            if (branch.Else is { } alternative)
                return new OrderBranch(branch.Condition, alternative, null, branch.DroppedConstraintName);
        }
        return null;
    }

    public static OrderBranch? SelectWrong(
        IReadOnlyList<OrderBranch> branches,
        BranchEvaluationContext context,
        int maxBranches = 3)
    {
        if (!IsValidStructure(branches, maxBranches) || branches.Count == 0) return null;
        if (branches.Count == 1)
        {
            var only = branches[0];
            return only.Else is { } alternative
                ? new OrderBranch(only.Condition, alternative, null, only.DroppedConstraintName)
                : null;
        }

        var first = branches[0];
        var opposite = branches.FirstOrDefault(branch => context.Matches(branch.Condition) != context.Matches(first.Condition));
        return opposite ?? branches[^1];
    }

    private static bool IsValidBranch(OrderBranch branch) =>
        branch.Then != BranchAction.DropConstraint || !string.IsNullOrWhiteSpace(branch.DroppedConstraintName);
}
