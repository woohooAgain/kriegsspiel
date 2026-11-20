using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed record EnemyZoneObjectView(string Id, ZoneObjectKind Kind, bool Functional);

public sealed record EnemyUnitView(
    string Id,
    string Name,
    UnitType Type,
    string LocationZoneId,
    Faction Faction,
    CommanderTraits Traits,
    int Training,
    int Experience,
    bool CombatEffective,
    bool Concealed)
{
    public static EnemyUnitView From(Unit unit, IGameRules rules)
    {
        ArgumentNullException.ThrowIfNull(unit);
        ArgumentNullException.ThrowIfNull(rules);
        return new EnemyUnitView(
            unit.Id,
            unit.Name,
            unit.Type,
            unit.LocationZoneId,
            unit.Faction,
            unit.Traits,
            unit.Training,
            unit.Experience,
            rules.IsCombatEffective(unit),
            unit.Concealed);
    }
}

public sealed record EnemyZoneView(
    string Id,
    string Name,
    TerrainType Terrain,
    int DefenseBonus,
    int Observability,
    IReadOnlyDictionary<string, int> TravelMinutes,
    IReadOnlyDictionary<string, EdgeType> EdgeTypes,
    IReadOnlyList<EnemyZoneObjectView> Objects)
{
    public static EnemyZoneView From(Zone zone)
    {
        ArgumentNullException.ThrowIfNull(zone);
        return new EnemyZoneView(
            zone.Id,
            zone.Name,
            zone.Terrain,
            zone.DefenseBonus,
            zone.Observability,
            new Dictionary<string, int>(zone.TravelMinutes),
            new Dictionary<string, EdgeType>(zone.EdgeTypes),
            zone.Objects.Select(o => new EnemyZoneObjectView(o.Id, o.Kind, o.IsIntact)).ToArray());
    }
}

public sealed record EnemyObservation(
    string ZoneId,
    Faction ObservedFaction,
    string? ObservedUnitId,
    int EstimatedStrength,
    int Minute,
    int Confidence,
    string SourceId)
{
    public static EnemyObservation? FromReport(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);
        if (!report.Facts.TryGetValue("zoneId", out var zoneId) || string.IsNullOrWhiteSpace(zoneId))
            return null;

        var factionValue = report.Facts.GetValueOrDefault("observedFaction")
                           ?? report.Facts.GetValueOrDefault("faction");
        if (factionValue is null || !Enum.TryParse<Faction>(factionValue, true, out var faction))
            return null;

        var observedUnitId = report.Facts.GetValueOrDefault("unitId");
        if (string.IsNullOrWhiteSpace(observedUnitId)) observedUnitId = null;

        var strengthValue = report.Facts.GetValueOrDefault("estimatedStrength")
                            ?? report.Facts.GetValueOrDefault("strength");
        int.TryParse(strengthValue?.TrimEnd('%'), out var strength);

        return new EnemyObservation(
            zoneId,
            faction,
            observedUnitId,
            strength,
            report.Minute,
            report.Confidence,
            report.SourceId);
    }
}

public sealed record EnemyObjectiveView(
    ObjectiveType Type,
    string? Zone,
    string? Unit,
    string? TargetFaction,
    int? UntilMinute,
    double Weight)
{
    public static EnemyObjectiveView From(Objective objective)
    {
        ArgumentNullException.ThrowIfNull(objective);
        return new EnemyObjectiveView(
            objective.Type,
            objective.Zone,
            objective.Unit,
            objective.TargetFaction,
            objective.UntilMinute,
            objective.Weight);
    }
}

public sealed class EnemyDoctrineInput
{
    public required Faction Faction { get; init; }
    public required int Minute { get; init; }
    public int? EndMinute { get; init; }
    public required FrictionTable Friction { get; init; }
    public required IReadOnlyList<EnemyUnitView> Units { get; init; }
    public required IReadOnlyList<EnemyZoneView> Zones { get; init; }
    public IReadOnlyList<EnemyObservation> Observations { get; init; } = [];
    public IReadOnlyList<EnemyObjectiveView> Objectives { get; init; } = [];
}

public interface IEnemyOrderCompiler
{
    IReadOnlyList<Order>? TryCompile(EnemyDoctrineInput input);
}

public sealed class EnemyDoctrine
{
    private readonly IGameRules _rules;
    private readonly IRandomSource _random;

    public EnemyDoctrine(IGameRules rules, IRandomSource random)
    {
        _rules = rules ?? throw new ArgumentNullException(nameof(rules));
        _random = random ?? throw new ArgumentNullException(nameof(random));
    }

    public bool UsesDoctrine(FrictionTable friction, bool llmAvailable)
    {
        ArgumentNullException.ThrowIfNull(friction);
        return !friction.Llm.Enabled || !llmAvailable;
    }

    public IReadOnlyList<Order> Author(EnemyDoctrineInput input) => Plan(input);

    public IReadOnlyList<Order> Fallback(EnemyDoctrineInput input) => Plan(input);

    public IReadOnlyList<Order> Resolve(
        EnemyDoctrineInput input,
        bool llmAvailable,
        IEnemyOrderCompiler? compiler = null)
    {
        ValidateInput(input);
        var checkpoint = _random.State;

        if (!UsesDoctrine(input.Friction, llmAvailable) && compiler is not null)
        {
            IReadOnlyList<Order>? compiled = null;
            try
            {
                compiled = compiler.TryCompile(input);
            }
            catch (Exception)
            {
                compiled = null;
            }

            if (compiled is { Count: > 0 } && compiled.All(o => IsValidOrder(o, input)))
                return compiled;
        }

        _random.Restore(checkpoint);
        return Plan(input);
    }

    public bool IsValidOrder(Order order, EnemyDoctrineInput input)
    {
        ArgumentNullException.ThrowIfNull(order);
        ValidateInput(input);

        var maxBranches = Math.Min(_rules.MaxBranches, input.Friction.Orders.MaxBranches);
        if (!_rules.IsValidOrder(order) || order.Branches.Count > maxBranches)
            return false;
        if (!Enum.IsDefined(typeof(OrderIntent), order.Intent) ||
            !Enum.IsDefined(typeof(MovementMode), order.MovementMode))
            return false;
        if (order.Constraints.Any(c => !Enum.IsDefined(typeof(OrderConstraint), c)))
            return false;
        if (order.Branches.Any(b =>
                !Enum.IsDefined(typeof(BranchCondition), b.Condition) ||
                !Enum.IsDefined(typeof(BranchAction), b.Then) ||
                (b.Else.HasValue && !Enum.IsDefined(typeof(BranchAction), b.Else.Value))))
            return false;
        if (order.AmbiguityScore < 0 || order.AmbiguityScore > 1 || order.IssuedAt < 0)
            return false;
        if (!input.Units.Any(u => u.Id == order.ToUnitId))
            return false;
        if (order.TargetZoneId is not null && !input.Zones.Any(z => z.Id == order.TargetZoneId))
            return false;
        if (order.TargetUnitId is not null &&
            !input.Units.Any(u => u.Id == order.TargetUnitId) &&
            !input.Observations.Any(o => o.ObservedUnitId == order.TargetUnitId))
            return false;
        return true;
    }

    private IReadOnlyList<Order> Plan(EnemyDoctrineInput input)
    {
        ValidateInput(input);
        var orders = new List<Order>();

        foreach (var unit in input.Units.OrderBy(u => u.Id, StringComparer.Ordinal))
        {
            var intent = ChooseIntent(unit, input);
            var target = ChooseTarget(unit, intent, input);
            var stance = _random.NextDouble();
            var deadline = ClampMinute(
                input.Minute + _rules.TurnMinutes * 2,
                input.Minute,
                input.EndMinute);
            var reportBy = ClampMinute(input.Minute + _rules.TurnMinutes, input.Minute, deadline);
            var constraints = ChooseConstraints(unit, intent);
            var branches = ChooseBranches(unit, target, input);

            var order = new Order
            {
                ToUnitId = unit.Id,
                Intent = intent,
                TargetZoneId = target.Zone?.Id,
                TargetObjectId = target.ObjectId,
                TargetUnitId = target.UnitId,
                TargetEdgeId = target.EdgeId,
                NotLaterThan = deadline,
                ReportBy = reportBy,
                IssuedAt = input.Minute,
                MovementMode = ChooseMovementMode(unit, stance),
                Constraints = constraints,
                Branches = branches,
                AmbiguityScore = ComputeAmbiguity(target.Zone?.Id, deadline, reportBy, branches.Count, input)
            };

            if (!IsValidOrder(order, input))
                throw new InvalidOperationException($"Generated order for {unit.Id} is not pipeline-compatible.");

            orders.Add(order);
        }

        return orders;
    }

    private static void ValidateInput(EnemyDoctrineInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (input.Faction == Faction.Neutral)
            throw new ArgumentException("Enemy doctrine requires a non-neutral faction.", nameof(input));
        if (input.Minute < 0)
            throw new ArgumentOutOfRangeException(nameof(input));
        if (input.Friction is null)
            throw new ArgumentException("Enemy doctrine requires a friction profile.", nameof(input));
        if (input.Units is null || input.Zones is null)
            throw new ArgumentException("Enemy doctrine requires unit and zone views.", nameof(input));
        if (input.Units.Any(u => u.Faction != input.Faction))
            throw new ArgumentException("Enemy doctrine accepts only own-faction units.", nameof(input));
        if (input.Units.Select(u => u.Id).Distinct(StringComparer.Ordinal).Count() != input.Units.Count ||
            input.Zones.Select(z => z.Id).Distinct(StringComparer.Ordinal).Count() != input.Zones.Count)
            throw new ArgumentException("Enemy doctrine requires unique unit and zone ids.", nameof(input));
    }

    private static Faction Opponent(Faction faction) =>
        faction == Faction.Red ? Faction.Blue : Faction.Red;

    private static IEnumerable<EnemyObjectiveView> RelevantObjectives(
        EnemyDoctrineInput input,
        Faction opponent) =>
        input.Objectives.Where(o => o.TargetFaction is null ||
            (Enum.TryParse<Faction>(o.TargetFaction, true, out var target) && target == opponent));

    private static OrderIntent ChooseIntent(EnemyUnitView unit, EnemyDoctrineInput input)
    {
        if (unit.Type == UnitType.HQ) return OrderIntent.Reserve;
        if (!unit.CombatEffective) return OrderIntent.Withdraw;
        if (unit.Type == UnitType.Recon) return OrderIntent.Recon;

        var opponent = Opponent(input.Faction);
        var local = input.Zones.FirstOrDefault(z => z.Id == unit.LocationZoneId);
        var primary = RelevantObjectives(input, opponent)
            .OrderByDescending(o => o.Weight)
            .ThenBy(o => o.Zone ?? string.Empty, StringComparer.Ordinal)
            .FirstOrDefault();

        if (primary is not null)
        {
            return primary.Type switch
            {
                ObjectiveType.HoldZone or ObjectiveType.NoEnemyCombatEffectiveInZone or ObjectiveType.BlockEvacuation
                    => OrderIntent.Block,
                ObjectiveType.DelayEnemy or ObjectiveType.EvacuateCivilians => OrderIntent.Delay,
                ObjectiveType.DisableEnemyStrength => OrderIntent.Destroy,
                _ => OrderIntent.Hold
            };
        }

        var contact = input.Observations.Any(o =>
            o.ZoneId == unit.LocationZoneId && o.ObservedFaction == opponent);
        if (contact) return unit.Traits.Caution >= 2 ? OrderIntent.Hold : OrderIntent.Seize;

        var defensible = local is not null &&
            (local.DefenseBonus >= 2 || local.Objects.Any(o => o.Functional && o.Kind == ZoneObjectKind.Bridge));
        return defensible ? OrderIntent.Hold : OrderIntent.Advance;
    }

    private TargetChoice ChooseTarget(EnemyUnitView unit, OrderIntent intent, EnemyDoctrineInput input)
    {
        var local = input.Zones.FirstOrDefault(z => z.Id == unit.LocationZoneId);
        var opponent = Opponent(input.Faction);
        var objectives = RelevantObjectives(input, opponent).ToArray();
        var candidates = new List<WeightedZone>();

        foreach (var zone in input.Zones)
        {
            double score = 0;
            if (zone.Id == unit.LocationZoneId) score += 1;
            foreach (var objective in objectives.Where(o => o.Zone == zone.Id))
                score += 10 * Math.Max(0.01, objective.Weight);

            var contacts = input.Observations
                .Where(o => o.ZoneId == zone.Id && o.ObservedFaction == opponent)
                .ToArray();
            if (contacts.Length > 0)
                score += 2 + contacts.Max(o => Math.Max(0, o.Confidence));
            if (local is not null && local.TravelMinutes.ContainsKey(zone.Id)) score += 0.5;
            if (zone.Objects.Any(o => o.Functional && o.Kind == ZoneObjectKind.Bridge)) score += 0.5;

            if (score > 0) candidates.Add(new WeightedZone(zone, score));
        }

        if (candidates.Count == 0)
            return new TargetChoice(local ?? input.Zones.FirstOrDefault(), null, null, null);

        var bestScore = candidates.Max(c => c.Score);
        var best = candidates
            .Where(c => c.Score == bestScore)
            .OrderBy(c => c.Zone.Id, StringComparer.Ordinal)
            .ToArray();
        var chosen = best[_random.NextInt(best.Length)].Zone;

        string? objectId = null;
        if (intent is OrderIntent.Block or OrderIntent.Check or OrderIntent.Guard)
            objectId = chosen.Objects.FirstOrDefault(o => o.Functional)?.Id;

        string? observedUnitId = null;
        if (intent == OrderIntent.Destroy)
        {
            observedUnitId = input.Observations
                .Where(o => o.ZoneId == chosen.Id && o.ObservedFaction == opponent && o.ObservedUnitId is not null)
                .OrderByDescending(o => o.Confidence)
                .ThenBy(o => o.ObservedUnitId, StringComparer.Ordinal)
                .Select(o => o.ObservedUnitId)
                .FirstOrDefault();
        }

        string? edgeId = null;
        if (intent == OrderIntent.Withdraw && local is not null && local.Id != chosen.Id)
            edgeId = EdgeId(local.Id, chosen.Id);

        return new TargetChoice(chosen, objectId, observedUnitId, edgeId);
    }

    private static MovementMode ChooseMovementMode(EnemyUnitView unit, double stance)
    {
        if (unit.Traits.Caution >= 2) return MovementMode.Contact;
        if (unit.Type == UnitType.Recon || unit.Concealed) return MovementMode.Covert;
        return stance < 0.5 ? MovementMode.March : MovementMode.Contact;
    }

    private static List<OrderConstraint> ChooseConstraints(EnemyUnitView unit, OrderIntent intent)
    {
        var constraints = new List<OrderConstraint>();
        if (intent == OrderIntent.Recon && unit.Traits.Caution >= 1)
            constraints.Add(OrderConstraint.NoEngagement);
        else if (unit.Concealed)
            constraints.Add(OrderConstraint.NoWeaponDisplay);
        if (!unit.CombatEffective)
            constraints.Add(OrderConstraint.NoAmmoUse);
        return constraints;
    }

    private static List<OrderBranch> ChooseBranches(
        EnemyUnitView unit,
        TargetChoice target,
        EnemyDoctrineInput input)
    {
        var max = Math.Min(3, input.Friction.Orders.MaxBranches);
        var branches = new List<OrderBranch>();
        if (max <= 0) return branches;

        var enemyAtTarget = input.Observations.Any(o =>
            o.ObservedFaction != input.Faction &&
            (target.Zone is null || o.ZoneId == target.Zone.Id));
        branches.Add(new OrderBranch(
            BranchCondition.TargetHeldByEnemy,
            enemyAtTarget ? BranchAction.Attack : BranchAction.Hold));

        if (max > 1 && HasBridgeNearby(unit, target, input))
            branches.Add(new OrderBranch(BranchCondition.BridgeDestroyed, BranchAction.AlternateTarget));
        if (max > 2)
            branches.Add(new OrderBranch(BranchCondition.DeadlineExpired, BranchAction.ReportImmediately));
        return branches;
    }

    private static bool HasBridgeNearby(
        EnemyUnitView unit,
        TargetChoice target,
        EnemyDoctrineInput input)
    {
        var zoneIds = new HashSet<string>(StringComparer.Ordinal);
        if (target.Zone is not null) zoneIds.Add(target.Zone.Id);
        if (input.Zones.FirstOrDefault(z => z.Id == unit.LocationZoneId) is { } local)
        {
            zoneIds.Add(local.Id);
            foreach (var neighbor in local.TravelMinutes.Keys) zoneIds.Add(neighbor);
        }
        return input.Zones.Any(z =>
            zoneIds.Contains(z.Id) && z.Objects.Any(o => o.Functional && o.Kind == ZoneObjectKind.Bridge));
    }

    private static double ComputeAmbiguity(
        string? targetZoneId,
        int? deadline,
        int? reportBy,
        int branchCount,
        EnemyDoctrineInput input)
    {
        var weights = input.Friction.Orders.Ambiguity;
        double ambiguity = 0;
        if (string.IsNullOrWhiteSpace(targetZoneId)) ambiguity += weights.MissingTarget;
        if (deadline is null) ambiguity += weights.MissingDeadline;
        if (reportBy is null) ambiguity += weights.MissingReportTime;
        ambiguity += branchCount * weights.PerBranch;
        return Math.Clamp(ambiguity, 0, 1);
    }

    private static int ClampMinute(int value, int floor, int? ceiling) =>
        ceiling.HasValue && ceiling.Value >= floor
            ? Math.Clamp(value, floor, ceiling.Value)
            : Math.Max(floor, value);

    private static string EdgeId(string first, string second) =>
        string.CompareOrdinal(first, second) <= 0 ? $"{first}:{second}" : $"{second}:{first}";

    private sealed record WeightedZone(EnemyZoneView Zone, double Score);

    private sealed record TargetChoice(
        EnemyZoneView? Zone,
        string? ObjectId,
        string? UnitId,
        string? EdgeId);
}
