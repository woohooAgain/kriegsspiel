using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Orders;

public enum ConflictOutcome
{
    None,
    Literal,
    Query,
    OwnAssessment
}

public sealed record ConflictResolution(
    ConflictOutcome Outcome,
    OrderConstraint? ConflictingConstraint,
    bool ConcealViolation,
    string? Note)
{
    public bool HasConflict => Outcome != ConflictOutcome.None;
}

public sealed record OrderConflictContext(
    string? ForbiddenZoneId = null,
    bool HasExplicitConflict = false)
{
    public static OrderConflictContext From(GameState state, Order order, Unit unit)
    {
        var forbidden = state.Knowledge.Facts
            .Where(entry => entry.Key.StartsWith("constraint:noZoneEntry", StringComparison.OrdinalIgnoreCase))
            .SelectMany(entry => entry.Value)
            .OrderByDescending(claim => claim.DeliveryMinute)
            .Select(claim => claim.Value)
            .FirstOrDefault();
        return new OrderConflictContext(forbidden);
    }
}

public static class ConflictResolver
{
    public static ConflictResolution Resolve(
        Order order,
        CommanderTraits traits,
        OrderConflictContext? context = null)
    {
        ArgumentNullException.ThrowIfNull(order);
        ArgumentNullException.ThrowIfNull(traits);

        var constraint = FindConflictingConstraint(order, context);
        var unnamedDrop = order.Branches.Any(branch =>
            branch.Then == BranchAction.DropConstraint && string.IsNullOrWhiteSpace(branch.DroppedConstraintName));
        if (constraint is null && !unnamedDrop && !(context?.HasExplicitConflict ?? false))
            return new ConflictResolution(ConflictOutcome.None, null, false, null);

        var outcome = ChooseOutcome(traits);
        var conceal = traits.ReportHonesty <= 1;
        var note = outcome switch
        {
            ConflictOutcome.Literal => "буквальное исполнение: stop at constraint boundary, донесение о противоречии",
            ConflictOutcome.Query => $"остановка и запрос на {QueryDelayMinutes(traits)} мин",
            ConflictOutcome.OwnAssessment => conceal
                ? "конфликт разрешён собственной оценкой; нарушение скрыто в донесении"
                : "конфликт разрешён собственной оценкой; ограничение отброшено",
            _ => null
        };
        return new ConflictResolution(outcome, constraint, conceal, note);
    }

    public static EffectiveOrder Apply(EffectiveOrder effective, ConflictResolution resolution)
    {
        ArgumentNullException.ThrowIfNull(effective);
        ArgumentNullException.ThrowIfNull(resolution);
        if (!resolution.HasConflict) return effective;

        switch (resolution.Outcome)
        {
            case ConflictOutcome.OwnAssessment:
                if (resolution.ConflictingConstraint is { } dropped)
                    effective.Constraints.Remove(dropped);
                effective.Misinterpretation = Misinterpretation.DropConstraint;
                break;
            case ConflictOutcome.Literal:
                effective.Misinterpretation = Misinterpretation.LiteralExecution;
                break;
            case ConflictOutcome.Query:
                effective.IsClarification = true;
                break;
        }

        effective.Note = AppendNote(effective.Note, resolution.Note);
        return effective;
    }

    public static int QueryDelayMinutes(CommanderTraits traits) => 30 + 15 * Math.Clamp(traits.Caution, 0, 2);

    public static string AppendNote(string? current, string? addition) =>
        string.IsNullOrWhiteSpace(addition)
            ? current ?? string.Empty
            : string.IsNullOrWhiteSpace(current) ? addition : current + "; " + addition;

    private static ConflictOutcome ChooseOutcome(CommanderTraits traits)
    {
        if (traits.Literalism >= 2 && traits.Caution <= 1) return ConflictOutcome.Literal;
        if (traits.Caution >= 2 && traits.Initiative <= 1) return ConflictOutcome.Query;
        if (traits.Initiative >= 2 && traits.Literalism <= 1) return ConflictOutcome.OwnAssessment;

        if (traits.Caution >= 1 && traits.Caution >= traits.Literalism && traits.Caution >= traits.Initiative)
            return ConflictOutcome.Query;
        if (traits.Literalism >= 1 && traits.Literalism >= traits.Initiative) return ConflictOutcome.Literal;
        if (traits.Initiative >= 1) return ConflictOutcome.OwnAssessment;
        return ConflictOutcome.Query;
    }

    private static OrderConstraint? FindConflictingConstraint(Order order, OrderConflictContext? context)
    {
        foreach (var constraint in order.Constraints.OrderBy(value => (int)value))
        {
            switch (constraint)
            {
                case OrderConstraint.NoZoneEntry when !string.IsNullOrWhiteSpace(order.TargetZoneId) &&
                    (string.IsNullOrWhiteSpace(context?.ForbiddenZoneId) ||
                     string.Equals(context.ForbiddenZoneId, order.TargetZoneId, StringComparison.OrdinalIgnoreCase)):
                case OrderConstraint.NoEngagement when IsAssaultIntent(order.Intent) ||
                    order.Branches.Any(branch => branch.Then == BranchAction.Attack):
                case OrderConstraint.NoAmmoUse when order.Intent is OrderIntent.Destroy or OrderIntent.Block or OrderIntent.Check:
                case OrderConstraint.NoRetreatBeyondZone when order.Intent == OrderIntent.Withdraw:
                    return constraint;
            }
        }
        return null;
    }

    private static bool IsAssaultIntent(OrderIntent intent) => intent is
        OrderIntent.Seize or OrderIntent.Occupy or OrderIntent.Advance or
        OrderIntent.Destroy or OrderIntent.Block or OrderIntent.Check;
}
