using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed record UnderstandingOptions(
    double PMinModifier = 0,
    string? ReserveZoneId = null,
    string? ResolvedTargetId = null,
    string? ProfileId = null);

public sealed class UnderstandingResolver : IUnderstandingResolver
{
    public IEnumerable<Report> Resolve(OrderDelivered e, GameState s, FrictionTable f, Random rng) =>
        Resolve(e, s, f, rng, new UnderstandingOptions());

    public IEnumerable<Report> Resolve(
        OrderDelivered e,
        GameState s,
        FrictionTable f,
        Random rng,
        UnderstandingOptions options)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(s);
        ArgumentNullException.ThrowIfNull(f);
        ArgumentNullException.ThrowIfNull(rng);

        options ??= new UnderstandingOptions();
        if (s.Orders.FirstOrDefault(order => order.Id == e.OrderId) is not { } order) yield break;
        if (!s.Units.TryGetValue(order.ToUnitId, out var unit)) yield break;

        var scoringContext = BuildScoringContext(s, order, unit, options);
        var ambiguity = AmbiguityScorer.Breakdown(order, scoringContext, f);
        var conflict = ConflictResolver.Resolve(order, unit.Traits, OrderConflictContext.From(s, order, unit));

        var effective = EffectiveOrder.From(order);
        effective.AmbiguityScore = ambiguity.Total;
        effective.EffectiveAtMinute = e.Minute;
        effective.Note = string.Empty;
        ConflictResolver.Apply(effective, conflict);
        CorrectTarget(effective, scoringContext);

        var branches = OrderBranchRules.Select(order.Branches, BranchEvaluationContext.From(s, order, unit, e.Minute), f.Orders.MaxBranches);
        effective.SelectedBranch = branches;
        var shiftCandidates = ShiftCandidates(s, unit, order);
        var needsTarget = !HasAnyTarget(effective);

        var probability = Probability(ambiguity.Total, unit.Traits, f, ResolvePMinModifier(s, f, options));
        var triggered = rng.NextDouble() < probability;

        if (conflict.Outcome == ConflictOutcome.Query)
        {
            yield return EmitQuery(s, unit, order, effective, e.Minute, f,
                "остановка и запрос по конфликту цели и ограничения");
            yield break;
        }

        var outcome = triggered
            ? ChooseDistortion(ambiguity, unit.Traits, needsTarget, shiftCandidates.Count > 0,
                conflict.Outcome == ConflictOutcome.Literal)
            : Misinterpretation.None;

        if (needsTarget)
        {
            if (outcome == Misinterpretation.ShiftZone && shiftCandidates.Count > 0)
            {
                effective.TargetZoneId = shiftCandidates[0];
                effective.Misinterpretation = Misinterpretation.ShiftZone;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "сдвиг цели к соседней известной зоне");
                Commit(s, unit, order, effective, e.Minute);
                yield break;
            }

            yield return EmitQuery(s, unit, order, effective, e.Minute, f,
                "нет однозначной цели и известной соседней зоны — запрос уточнения");
            yield break;
        }

        switch (outcome)
        {
            case Misinterpretation.ShiftZone when shiftCandidates.Count > 0:
                effective.TargetZoneId = shiftCandidates[0];
                effective.Misinterpretation = Misinterpretation.ShiftZone;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "сдвиг цели к соседней известной зоне");
                break;
            case Misinterpretation.ChangeIntent:
                effective.Intent = DistortIntent(effective.Intent);
                effective.Misinterpretation = Misinterpretation.ChangeIntent;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "подмена намерения");
                if (order.Branches.Count > 0)
                {
                    effective.SelectedBranch = OrderBranchRules.SelectWrong(
                        order.Branches, BranchEvaluationContext.From(s, order, unit, e.Minute), f.Orders.MaxBranches);
                    effective.Note = ConflictResolver.AppendNote(effective.Note, "ошибка выбора ветви по знанию");
                }
                break;
            case Misinterpretation.DelayExecution:
                effective.Misinterpretation = Misinterpretation.DelayExecution;
                effective.Note = ConflictResolver.AppendNote(effective.Note,
                    $"задержка {ConflictResolver.QueryDelayMinutes(unit.Traits)} мин");
                break;
        }

        Commit(s, unit, order, effective, e.Minute);

        if (effective.Misinterpretation == Misinterpretation.DelayExecution)
        {
            yield return Report.Create(
                e.Minute,
                e.Minute + Math.Max(1, f.Hq.DefaultDeliveryTimeMinutes),
                unit.Id,
                1,
                ReportKind.Request,
                $"{unit.Name}: уточните задачу, без этого действовать не могу.",
                "delay:" + order.Id,
                unit.Faction);
        }

        if (order.Branches.Count > 0 && effective.Note.Contains("ошибка выбора ветви", StringComparison.Ordinal))
            s.Clock.Schedule(new OrderMisinterpreted(e.Minute, order.Id, Misinterpretation.BranchError), e.Minute);

        if (conflict.Outcome == ConflictOutcome.OwnAssessment && conflict.ConcealViolation)
        {
            yield return Report.Create(
                e.Minute,
                e.Minute + Math.Max(1, f.Hq.DefaultDeliveryTimeMinutes),
                unit.Id,
                1,
                ReportKind.Status,
                $"{unit.Name}: действую по обстановке, приказ {order.Id}.",
                "status:" + order.Id,
                unit.Faction);
        }
    }

    public static double Probability(
        double ambiguity,
        CommanderTraits traits,
        FrictionTable friction,
        double pMinModifier = 0)
    {
        ArgumentNullException.ThrowIfNull(traits);
        ArgumentNullException.ThrowIfNull(friction);

        var settings = friction.Orders;
        var pMin = EffectivePMin(settings, pMinModifier);
        var baseProbability = Math.Max(settings.P0, pMin);
        var factor = 1.0
            + settings.Traits.Literalism * traits.Literalism
            + settings.Traits.Caution * traits.Caution
            - settings.Traits.Initiative * traits.Initiative;
        return Math.Clamp(baseProbability + ambiguity * factor, pMin, settings.PMax);
    }

    public static double EffectivePMin(FrictionTable.OrderParameters settings, double modifier)
    {
        ArgumentNullException.ThrowIfNull(settings);
        var modified = settings.PMinModifierType.Equals("Multiplicative", StringComparison.OrdinalIgnoreCase)
            ? settings.PMin * (1.0 + modifier)
            : settings.PMin + modifier;
        return Math.Clamp(modified, 0.0, settings.PMax);
    }

    private static double ResolvePMinModifier(GameState state, FrictionTable friction, UnderstandingOptions options)
    {
        if (options.PMinModifier != 0) return options.PMinModifier;
        var profile = options.ProfileId is null
            ? friction.DifficultyProfiles.FirstOrDefault(p => p.Id == state.ScenarioId) ?? friction.DifficultyProfiles.FirstOrDefault()
            : friction.DifficultyProfiles.FirstOrDefault(p => p.Id == options.ProfileId);
        if (profile is null) return 0;

        foreach (var key in new[] { "pMinModifier", "ordersPMinModifier", "orders.pMinModifier" })
            if (profile.Modifiers.TryGetValue(key, out var modifier)) return modifier;
        return 0;
    }

    private static OrderScoringContext BuildScoringContext(
        GameState state,
        Order order,
        Unit unit,
        UnderstandingOptions options)
    {
        var reserveZone = options.ReserveZoneId ?? KnownSetting(state, "reserveZone", "reserveZoneId");
        return new OrderScoringContext(
            unit.LocationZoneId,
            reserveZone,
            unit.GuardOfObjectId,
            options.ResolvedTargetId,
            state.Knowledge.Zones.Where(entry => entry.Value.HasData).Select(entry => entry.Key).ToArray());
    }

    private static string? KnownSetting(GameState state, params string[] keys)
    {
        foreach (var key in keys)
        {
            if (!state.Knowledge.Facts.TryGetValue(key, out var claims)) continue;
            var claim = claims.OrderByDescending(item => item.DeliveryMinute).FirstOrDefault();
            if (claim is not null && !string.IsNullOrWhiteSpace(claim.Value)) return claim.Value;
        }
        return null;
    }

    private static void CorrectTarget(EffectiveOrder effective, OrderScoringContext context)
    {
        if (HasAnyTarget(effective)) return;

        switch (effective.Intent)
        {
            case OrderIntent.Hold when !string.IsNullOrWhiteSpace(context.CurrentZoneId):
                effective.TargetZoneId = context.CurrentZoneId;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "correct: цель — текущий район");
                break;
            case OrderIntent.Reserve when !string.IsNullOrWhiteSpace(context.ReserveZoneId):
                effective.TargetZoneId = context.ReserveZoneId;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "correct: цель — зона резерва");
                break;
            case OrderIntent.Guard when !string.IsNullOrWhiteSpace(context.GuardObjectId):
                effective.TargetObjectId = context.GuardObjectId;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "correct: цель — объект охранения");
                break;
            case OrderIntent.Guard when !string.IsNullOrWhiteSpace(context.CurrentZoneId):
                effective.TargetZoneId = context.CurrentZoneId;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "correct: цель — текущая позиция");
                break;
            case not OrderIntent.Hold when !string.IsNullOrWhiteSpace(context.ResolvedTargetId):
                effective.TargetZoneId = context.ResolvedTargetId;
                effective.Note = ConflictResolver.AppendNote(effective.Note, "correct: цель восстановлена по контексту");
                break;
        }
    }

    private static bool HasAnyTarget(EffectiveOrder effective) =>
        !string.IsNullOrWhiteSpace(effective.TargetZoneId) ||
        !string.IsNullOrWhiteSpace(effective.TargetObjectId) ||
        !string.IsNullOrWhiteSpace(effective.TargetUnitId) ||
        !string.IsNullOrWhiteSpace(effective.TargetEdgeId);

    private static Misinterpretation ChooseDistortion(
        AmbiguityBreakdown ambiguity,
        CommanderTraits traits,
        bool needsTarget,
        bool hasShift,
        bool conflictLiteral)
    {
        if (conflictLiteral) return Misinterpretation.LiteralExecution;
        if (needsTarget) return hasShift ? Misinterpretation.ShiftZone : Misinterpretation.None;
        if (ambiguity.Vague > 0 && traits.Literalism >= 2) return Misinterpretation.ChangeIntent;
        if (traits.Caution >= 2 && ambiguity.Total > 0) return Misinterpretation.DelayExecution;
        if (ambiguity.MissingTarget > 0 && hasShift) return Misinterpretation.ShiftZone;
        return ambiguity.Vague > 0 ? Misinterpretation.ChangeIntent : Misinterpretation.DelayExecution;
    }

    private static OrderIntent DistortIntent(OrderIntent intent) => intent switch
    {
        OrderIntent.Seize or OrderIntent.Occupy or OrderIntent.Advance or OrderIntent.Move => OrderIntent.Hold,
        OrderIntent.Hold or OrderIntent.Reserve or OrderIntent.Support or OrderIntent.Delay => OrderIntent.Occupy,
        OrderIntent.Recon or OrderIntent.Check => OrderIntent.Recon == intent ? OrderIntent.Check : OrderIntent.Recon,
        OrderIntent.Withdraw => OrderIntent.Reserve,
        _ => OrderIntent.Hold
    };

    private static List<string> ShiftCandidates(GameState state, Unit unit, Order order)
    {
        if (!state.Zones.TryGetValue(unit.LocationZoneId, out var currentZone)) return [];
        var objectiveZone = KnownZoneForObject(state, order.TargetObjectId);
        var known = currentZone.TravelMinutes.Keys
            .Where(zoneId => zoneId != unit.LocationZoneId && state.Knowledge.HasZoneData(zoneId))
            .ToList();

        if (objectiveZone is null)
            return known.OrderBy(zoneId => zoneId, StringComparer.Ordinal).ToList();

        return known
            .OrderBy(zoneId => zoneId == objectiveZone ? 0 : 1)
            .ThenBy(zoneId => Distance(state, unit.LocationZoneId, zoneId, objectiveZone))
            .ThenBy(zoneId => zoneId, StringComparer.Ordinal)
            .ToList();
    }

    private static string? KnownZoneForObject(GameState state, string? objectId)
    {
        if (string.IsNullOrWhiteSpace(objectId)) return null;
        foreach (var (key, claims) in state.Knowledge.Facts)
        {
            if (!key.Contains(objectId, StringComparison.OrdinalIgnoreCase)) continue;
            var claim = claims.OrderByDescending(item => item.DeliveryMinute).FirstOrDefault();
            if (claim is null) continue;
            if (state.Knowledge.HasZoneData(claim.Value)) return claim.Value;
            var separator = key.IndexOf(':');
            if (separator < 0) continue;
            var zoneId = key[(separator + 1)..];
            if (state.Knowledge.HasZoneData(zoneId)) return zoneId;
        }
        return null;
    }

    private static int Distance(GameState state, string from, string to, string target)
    {
        if (to == target) return 0;
        var visited = new HashSet<string> { from, to };
        var frontier = new List<(string ZoneId, int Steps)> { (to, 1) };
        while (frontier.Count > 0)
        {
            var (zoneId, steps) = frontier[0];
            frontier.RemoveAt(0);
            if (!state.Zones.TryGetValue(zoneId, out var zone)) continue;
            foreach (var neighbor in zone.TravelMinutes.Keys)
            {
                if (!visited.Add(neighbor)) continue;
                if (neighbor == target) return steps + 1;
                frontier.Add((neighbor, steps + 1));
            }
        }
        return int.MaxValue / 4;
    }

    private static Report EmitQuery(
        GameState state,
        Unit unit,
        Order order,
        EffectiveOrder effective,
        int minute,
        FrictionTable friction,
        string reason)
    {
        effective.IsClarification = true;
        effective.Misinterpretation = Misinterpretation.None;
        effective.Note = ConflictResolver.AppendNote(effective.Note, reason);
        state.EffectiveOrders[order.Id] = effective;
        unit.EffectiveOrder = effective;

        state.Clock.Schedule(new QuerySent(minute, "HQ", unit.Id), minute);
        return Report.Create(
            minute,
            minute + Math.Max(1, friction.Hq.DefaultDeliveryTimeMinutes),
            unit.Id,
            1,
            ReportKind.Request,
            $"{unit.Name}: {reason}. Приказ {order.Id}.",
            "clarify:" + order.Id,
            unit.Faction);
    }

    private static void Commit(GameState state, Unit unit, Order order, EffectiveOrder effective, int minute)
    {
        state.EffectiveOrders[order.Id] = effective;
        unit.EffectiveOrder = effective;
        if (effective.IsClarification) return;

        var executionOrder = CreateExecutionOrder(order, effective);
        state.Orders.Add(executionOrder);
        unit.CurrentOrderId = executionOrder.Id;

        var acceptanceMinute = effective.Misinterpretation == Misinterpretation.DelayExecution
            ? minute + ConflictResolver.QueryDelayMinutes(unit.Traits)
            : minute;
        state.Clock.Schedule(new OrderAccepted(acceptanceMinute, executionOrder.Id), acceptanceMinute);
        if (effective.Misinterpretation != Misinterpretation.None)
            state.Clock.Schedule(new OrderMisinterpreted(minute, order.Id, effective.Misinterpretation), minute);
    }

    private static Order CreateExecutionOrder(Order source, EffectiveOrder effective)
    {
        var literal = effective.Misinterpretation == Misinterpretation.LiteralExecution;
        return new Order
        {
            Id = effective.Id,
            ToUnitId = effective.ToUnitId,
            Intent = literal ? OrderIntent.Hold : effective.Intent,
            TargetZoneId = literal ? null : effective.TargetZoneId,
            TargetObjectId = literal ? null : effective.TargetObjectId,
            TargetUnitId = literal ? null : effective.TargetUnitId,
            TargetEdgeId = literal ? null : effective.TargetEdgeId,
            NotLaterThan = effective.NotLaterThan,
            ReportBy = effective.ReportBy,
            MovementMode = effective.MovementMode,
            AmbiguityScore = effective.AmbiguityScore,
            OrderVersion = effective.OrderVersion,
            IssuedAt = source.IssuedAt,
            DeliveredMinute = effective.EffectiveAtMinute,
            OriginalText = source.OriginalText,
            Explanation = source.Explanation,
            IssuerId = source.IssuerId,
            Constraints = new List<OrderConstraint>(effective.Constraints),
            Branches = new List<OrderBranch>(effective.Branches),
            VaguePatterns = new List<string>(source.VaguePatterns)
        };
    }
}
