using System.Reflection;
using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Assessment;

public sealed record MetricOptions
{
    public int? LostOrderPenaltyMinutes { get; init; }
    public Func<int, Objective?, double>? ObjectiveProgressAt { get; init; }
    public int InitiativeWindowMinutes { get; init; } = 30;
}

public sealed record OrderDelayMetrics(
    double AverageMinutes,
    int DeliveredOrders,
    int LostOrders,
    bool LostOrdersUseInfiniteDelay,
    int? PenaltyMinutes);

public sealed record AmbiguityMetrics(int TotalOrders, int AmbiguousOrders, double Share);

public sealed record ReactionRecord(int ShownMinute, int? ReactionMinute, int? Segment, double DelayMinutes);

public sealed record ReactionMetrics(
    int Contradictions,
    int Reacted,
    double Share,
    double AverageDelayMinutes,
    IReadOnlyList<ReactionRecord> Details);

public sealed record PoorControlMetrics(
    int ImmediateLosses,
    int PendingLosses,
    int TotalLosses,
    IReadOnlyList<string> Sources);

public sealed record InitiativeRecord(
    string UnitId,
    int Minute,
    int EvaluationMinute,
    double ProgressAtStart,
    double ProgressAtEnd,
    bool UnitAlive,
    bool ToBetter);

public sealed record InitiativeMetrics(
    int Count,
    int BetterCount,
    double BetterShare,
    IReadOnlyList<InitiativeRecord> Details);

public sealed record LiteralismMetrics(int Count, IReadOnlyList<string> OrderIds);

public sealed record IntentDiscrepancy(
    string OrderId,
    string? OriginalIntent,
    string? EffectiveIntent,
    IReadOnlyList<string> Changes,
    int? Confidence,
    string? SourceId);

public sealed record IntentDiscrepancyMetrics(int Count, IReadOnlyList<IntentDiscrepancy> Details);

public sealed record CivilianMetrics(
    int Casualties,
    int TotalGroups,
    int EvacuatedGroups,
    int Tasks,
    int CompletedTasks,
    double TaskCompletionShare);

public sealed record MetricSnapshot(
    OrderDelayMetrics OrderDelay,
    AmbiguityMetrics Ambiguity,
    ReactionMetrics Reaction,
    PoorControlMetrics PoorControl,
    InitiativeMetrics Initiative,
    LiteralismMetrics Literalism,
    IntentDiscrepancyMetrics Discrepancies,
    CivilianMetrics Civilians);

public sealed class MetricCalculator
{
    private const int TurnMinutes = 30;

    private readonly Scenario _scenario;
    private readonly MetricOptions _options;
    private readonly ObjectiveEvaluator _evaluator;

    public MetricCalculator(
        Scenario scenario,
        MetricOptions? options = null,
        ObjectiveEvaluator? evaluator = null)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        _scenario = scenario;
        _options = options ?? new MetricOptions();
        _evaluator = evaluator ?? new ObjectiveEvaluator();
    }

    public MetricSnapshot Calculate(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var objectiveEvaluation = _evaluator.Evaluate(_scenario, state);

        return new MetricSnapshot(
            OrderDelay(state),
            Ambiguity(state),
            Reaction(state),
            PoorControl(state),
            Initiative(state, objectiveEvaluation),
            Literalism(state),
            Discrepancies(state),
            Civilians(state, objectiveEvaluation));
    }

    public OrderDelayMetrics OrderDelay(GameState state)
    {
        var delays = new List<double>();
        int delivered = 0;
        int lost = 0;

        foreach (var order in state.Orders)
        {
            if (order.Lost)
            {
                lost++;
                if (_options.LostOrderPenaltyMinutes is int penalty)
                    delays.Add(Math.Max(0, penalty));
                continue;
            }

            if (order.DeliveredMinute is int deliveredMinute)
            {
                delivered++;
                delays.Add(Math.Max(0, deliveredMinute - order.IssuedAt));
            }
        }

        return new OrderDelayMetrics(
            delays.Count == 0 ? 0 : delays.Average(),
            delivered,
            lost,
            _options.LostOrderPenaltyMinutes is null,
            _options.LostOrderPenaltyMinutes);
    }

    public AmbiguityMetrics Ambiguity(GameState state)
    {
        int total = state.Orders.Count;
        int ambiguous = state.Orders.Count(o => o.AmbiguityScore > 0);
        return new AmbiguityMetrics(total, ambiguous, total == 0 ? 0 : (double)ambiguous / total);
    }

    public ReactionMetrics Reaction(GameState state)
    {
        var shown = new SortedSet<int>();
        foreach (var e in state.EventLog)
        {
            if (IsContradictionEvent(e))
                shown.Add(e.Minute);
            if (e is ReportDelivered delivered && IsContradictionReport(delivered.Payload))
                shown.Add(delivered.Minute);
        }

        foreach (var report in state.DeliveredReports.Where(IsContradictionReport))
            shown.Add(report.ArrivesAt);

        foreach (var claims in state.Knowledge.Facts.Values)
        {
            if (claims.Select(c => c.Value).Distinct(StringComparer.Ordinal).Count() > 1)
                shown.Add(claims.Max(c => c.DeliveryMinute));
        }

        var details = new List<ReactionRecord>();
        foreach (var minute in shown)
        {
            var reaction = state.EventLog.FirstOrDefault(e => e.Minute > minute && IsReaction(e));
            details.Add(reaction is null
                ? new ReactionRecord(minute, null, null, Math.Max(0, EndMinute(state) - minute))
                : new ReactionRecord(
                    minute,
                    reaction.Minute,
                    (reaction.Minute - _scenario.StartMinute) / TurnMinutes + 1,
                    reaction.Minute - minute));
        }

        int reacted = details.Count(d => d.ReactionMinute.HasValue);
        return new ReactionMetrics(
            details.Count,
            reacted,
            details.Count == 0 ? 1 : (double)reacted / details.Count,
            details.Count == 0 ? 0 : details.Average(d => d.DelayMinutes),
            details);
    }

    public PoorControlMetrics PoorControl(GameState state)
    {
        var intervals = PoorControlIntervals(state);
        var poorUnits = intervals.Keys.ToHashSet(StringComparer.Ordinal);
        var poorZones = state.Units.Values
            .Where(u => poorUnits.Contains(u.Id))
            .Select(u => u.LocationZoneId)
            .ToHashSet(StringComparer.Ordinal);
        foreach (var zone in state.Zones.Values)
        {
            if (zone.PresentUnitIds.Any(poorUnits.Contains))
                poorZones.Add(zone.Id);
        }

        string? ZoneOf(string? unitId) =>
            unitId is not null && state.Units.TryGetValue(unitId, out var unit)
                ? unit.LocationZoneId
                : null;

        bool IsPoor(string? unitId, string? zoneId, int minute)
        {
            if (unitId is not null && IsPoorUnit(unitId, minute, intervals))
                return true;
            return zoneId is not null && poorZones.Contains(zoneId);
        }

        var sources = new List<string>();
        var pendingKeys = new HashSet<string>();
        int pendingLosses = 0;
        int immediateLosses = 0;

        foreach (var e in state.EventLog)
        {
            if (e is PendingCasualties pending)
            {
                if (!IsPoor(pending.UnitId, ZoneOf(pending.UnitId), pending.Minute))
                    continue;
                pendingKeys.Add(PendingKey(pending.Minute, pending.UnitId, pending.Amount));
                pendingLosses += pending.Amount;
                sources.Add(nameof(PendingCasualties));
                continue;
            }

            int amount = LossAmount(e);
            if (amount <= 0 || !IsPoorLoss(e, intervals, poorZones, ZoneOf))
                continue;
            immediateLosses += amount;
            sources.Add(e.GetType().Name);
        }

        foreach (var casualty in state.PendingCasualties)
        {
            if (!string.IsNullOrEmpty(casualty.UnitId) &&
                !IsPoor(casualty.UnitId, casualty.ZoneId ?? ZoneOf(casualty.UnitId), casualty.Minute))
                continue;
            var key = PendingKey(casualty.Minute, casualty.UnitId, casualty.Amount);
            if (!pendingKeys.Add(key))
                continue;
            pendingLosses += casualty.Amount;
            sources.Add(nameof(PendingCasualty));
        }

        return new PoorControlMetrics(
            immediateLosses,
            pendingLosses,
            immediateLosses + pendingLosses,
            sources.Distinct(StringComparer.Ordinal).ToList());
    }

    public InitiativeMetrics Initiative(GameState state, ScenarioObjectiveEvaluation objectives)
    {
        var records = new List<InitiativeRecord>();
        var controlMinutes = objectives.Objectives
            .SelectMany(o => o.Objective.ControlMinutes)
            .Distinct()
            .ToList();

        foreach (var e in state.EventLog.Where(IsInitiativeEvent))
        {
            int minute = e.Minute;
            int nextControl = controlMinutes.Where(m => m > minute).DefaultIfEmpty().Min();
            int end = EndMinute(state);
            int evaluationMinute = nextControl > 0
                ? Math.Min(nextControl, end)
                : Math.Min(end, minute + Math.Max(1, _options.InitiativeWindowMinutes));
            if (evaluationMinute <= minute)
                evaluationMinute = Math.Min(end, minute + Math.Max(1, _options.InitiativeWindowMinutes));

            string unitId = StringProperty(e, "UnitId", "SourceUnitId") ?? "";
            string? zoneId = StringProperty(e, "ZoneId", "TargetZoneId");
            var objective = objectives.Objectives.FirstOrDefault(o => zoneId is not null && o.Objective.Zone == zoneId)
                ?? objectives.Objectives.FirstOrDefault(o => o.Objective.ControlMinutes.Any(m => m > minute))
                ?? objectives.Objectives.FirstOrDefault();

            double progressEnd = _options.ObjectiveProgressAt?.Invoke(evaluationMinute, objective?.Objective)
                ?? objective?.Fraction
                ?? objectives.Score;
            double progressStart = DoubleProperty(e, "ProgressAtStart", "ProgressBefore", "ObjectiveProgressAtStart")
                ?? _options.ObjectiveProgressAt?.Invoke(minute, objective?.Objective)
                ?? progressEnd;
            bool alive = unitId.Length == 0 ||
                (state.Units.TryGetValue(unitId, out var unit) && !unit.Captured && unit.Strength > 0);

            records.Add(new InitiativeRecord(
                unitId,
                minute,
                evaluationMinute,
                progressStart,
                progressEnd,
                alive,
                alive && progressEnd >= progressStart));
        }

        int better = records.Count(r => r.ToBetter);
        return new InitiativeMetrics(
            records.Count,
            better,
            records.Count == 0 ? 0 : (double)better / records.Count,
            records);
    }

    public LiteralismMetrics Literalism(GameState state)
    {
        var orderIds = new List<string>();
        foreach (var e in state.EventLog)
        {
            var name = e.GetType().Name;
            var reason = StringProperty(e, "Reason", "Note");
            bool literal = name.Contains("Literal", StringComparison.OrdinalIgnoreCase)
                || BoolProperty(e, "LiteralExecution", "Literal", "ConstraintObeyedTaskFailed") == true
                || ContainsLiteralWord(reason);
            if (literal)
                orderIds.Add(StringProperty(e, "OrderId") ?? name);
        }

        foreach (var order in state.EffectiveOrders.Values)
        {
            if (ContainsLiteralWord(order.Note))
                orderIds.Add(order.SourceOrderId);
        }

        var distinct = orderIds.Distinct(StringComparer.Ordinal).ToList();
        return new LiteralismMetrics(distinct.Count, distinct);
    }

    public IntentDiscrepancyMetrics Discrepancies(GameState state)
    {
        var details = new List<IntentDiscrepancy>();
        foreach (var effective in state.EffectiveOrders.Values)
        {
            var original = state.Orders.FirstOrDefault(o => o.Id == effective.SourceOrderId);
            var changes = new List<string>();
            if (original is null)
            {
                changes.Add("OriginalMissing");
            }
            else
            {
                if (original.Intent != effective.Intent)
                    changes.Add("Intent");
                if (original.TargetZoneId != effective.TargetZoneId)
                    changes.Add("TargetZone");
                if (original.TargetObjectId != effective.TargetObjectId)
                    changes.Add("TargetObject");
                if (original.TargetUnitId != effective.TargetUnitId)
                    changes.Add("TargetUnit");
                if (original.TargetEdgeId != effective.TargetEdgeId)
                    changes.Add("TargetEdge");
                if (original.NotLaterThan != effective.NotLaterThan)
                    changes.Add("Deadline");
                if (original.MovementMode != effective.MovementMode)
                    changes.Add("MovementMode");
                if (!original.Constraints.Order().SequenceEqual(effective.Constraints.Order()))
                    changes.Add("Constraints");
            }

            if (effective.Misinterpretation != Misinterpretation.None)
                changes.Add("Misinterpretation:" + effective.Misinterpretation);

            if (changes.Count == 0)
                continue;

            var related = state.DeliveredReports
                .Where(r =>
                    r.Facts.TryGetValue("orderId", out var id) && id == effective.SourceOrderId ||
                    r.FactKey.Contains(effective.SourceOrderId, StringComparison.Ordinal))
                .ToList();

            details.Add(new IntentDiscrepancy(
                effective.SourceOrderId,
                original?.Intent.ToString(),
                effective.Intent.ToString(),
                changes,
                related.Count == 0 ? null : (int?)related.Max(r => r.Confidence),
                related.FirstOrDefault()?.SourceId));
        }

        return new IntentDiscrepancyMetrics(details.Count, details);
    }

    public CivilianMetrics Civilians(GameState state, ScenarioObjectiveEvaluation objectives)
    {
        var groups = state.Zones.Values.SelectMany(z => z.Civilians).ToList();
        var eventAmounts = new Dictionary<string, int>(StringComparer.Ordinal);
        int unattributed = 0;

        foreach (var e in state.EventLog)
        {
            int amount = IntProperty(e, "Casualties") ?? 0;
            if (amount <= 0)
                continue;
            var groupId = StringProperty(e, "CivilianGroupId", "GroupId");
            if (groupId is null)
                unattributed += amount;
            else
                eventAmounts[groupId] = eventAmounts.GetValueOrDefault(groupId) + amount;
        }

        int casualties = groups.Sum(g =>
                eventAmounts.TryGetValue(g.Id, out int amount) ? amount : g.Casualties)
            + eventAmounts.Where(kvp => groups.All(g => g.Id != kvp.Key)).Sum(kvp => kvp.Value)
            + unattributed;
        int evacuated = groups.Count(g => g.Evacuated);

        var tasks = objectives.Objectives
            .Where(o => o.Objective.Type is ObjectiveType.EvacuateCivilians or ObjectiveType.BlockEvacuation)
            .Select(o => o.Objective)
            .ToList();
        if (tasks.Count == 0)
            tasks = groups.Where(g => g.EvacuationTargetZoneId is not null)
                .Select(g => new Objective
                {
                    Type = ObjectiveType.EvacuateCivilians,
                    CivilianGroupId = g.Id,
                    SafeZone = g.EvacuationTargetZoneId,
                    UntilMinute = g.EvacuationDeadline
                })
                .ToList();

        int completed = 0;
        foreach (var task in tasks)
        {
            var group = task.CivilianGroupId is not null
                ? groups.FirstOrDefault(g => g.Id == task.CivilianGroupId)
                : groups.FirstOrDefault(g => task.Zone is null || g.LocationZoneId == task.Zone);
            if (group is null)
                continue;

            bool done = task.Type == ObjectiveType.EvacuateCivilians
                ? group.Evacuated &&
                  (task.SafeZone is null || group.LocationZoneId == task.SafeZone) &&
                  (task.UntilMinute is null || EndMinute(state) <= task.UntilMinute.Value)
                : !group.Evacuated;
            if (done)
                completed++;
        }

        return new CivilianMetrics(
            casualties,
            groups.Count,
            evacuated,
            tasks.Count,
            completed,
            tasks.Count == 0 ? 0 : (double)completed / tasks.Count);
    }

    private static Dictionary<string, List<(int Start, int End)>> PoorControlIntervals(GameState state)
    {
        var intervals = new Dictionary<string, List<(int Start, int End)>>(StringComparer.Ordinal);
        var open = new Dictionary<string, int>(StringComparer.Ordinal);
        int end = state.EndMinute ?? state.Clock.Now;

        foreach (var e in state.EventLog)
        {
            if (e is PoorControlEntered entered)
            {
                open[entered.UnitId] = entered.Minute;
                continue;
            }

            if (e is PoorControlExited exited)
            {
                if (open.Remove(exited.UnitId, out int start))
                    AddInterval(intervals, exited.UnitId, start, exited.Minute);
                continue;
            }

            var unitId = StringProperty(e, "UnitId");
            if (unitId is not null && BoolProperty(e, "PoorControl") == true)
                open.TryAdd(unitId, e.Minute);
        }

        foreach (var (unitId, start) in open)
            AddInterval(intervals, unitId, start, end);
        foreach (var unit in state.Units.Values.Where(u => u.PoorControl))
        {
            if (!intervals.ContainsKey(unit.Id))
                AddInterval(intervals, unit.Id, 0, end);
        }

        return intervals;
    }

    private static void AddInterval(
        Dictionary<string, List<(int Start, int End)>> intervals,
        string unitId,
        int start,
        int end)
    {
        if (!intervals.TryGetValue(unitId, out var list))
        {
            list = [];
            intervals[unitId] = list;
        }
        list.Add((start, Math.Max(start, end)));
    }

    private static bool IsPoorUnit(
        string unitId,
        int minute,
        Dictionary<string, List<(int Start, int End)>> intervals) =>
        intervals.TryGetValue(unitId, out var list) && list.Any(i => minute >= i.Start && minute <= i.End);

    private static bool IsPoorLoss(
        GameEvent e,
        Dictionary<string, List<(int Start, int End)>> intervals,
        HashSet<string> poorZones,
        Func<string?, string?> zoneOf)
    {
        var name = e.GetType().Name;
        if (name.Contains("PoorControl", StringComparison.OrdinalIgnoreCase))
            return true;
        if (BoolProperty(e, "PoorControl") == true)
            return true;
        if (StringProperty(e, "CivilianGroupId", "GroupId") is not null)
            return false;

        var unitId = StringProperty(e, "UnitId", "SourceUnitId");
        if (unitId is not null && IsPoorUnit(unitId, e.Minute, intervals))
            return true;

        var zoneId = StringProperty(e, "ZoneId") ?? zoneOf(unitId);
        return zoneId is not null && poorZones.Contains(zoneId);
    }

    private static string PendingKey(int minute, string unitId, int amount) => $"{minute}:{unitId}:{amount}";

    private static int LossAmount(GameEvent e) =>
        IntProperty(e, "Losses", "Loss", "OwnLosses", "FriendlyLosses", "LossSelf", "Amount") ?? 0;

    private static bool IsInitiativeEvent(GameEvent e)
    {
        var name = e.GetType().Name;
        return name.Contains("Initiative", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Improvis", StringComparison.OrdinalIgnoreCase)
            || BoolProperty(e, "InitiativeTaken", "IsImprovisation", "TookInitiative") == true;
    }

    private static bool IsReaction(GameEvent e)
    {
        var name = e.GetType().Name;
        if (name.Contains("Time", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Wait", StringComparison.OrdinalIgnoreCase) ||
            name.Contains("Skip", StringComparison.OrdinalIgnoreCase))
            return false;

        return e is OrderIssued or QuerySent
            || name.Contains("Reaction", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Order", StringComparison.OrdinalIgnoreCase)
                && e is not (OrderDelivered or OrderLost or OrderAccepted or OrderMisinterpreted or OrderSuperseded)
            || name.Contains("Query", StringComparison.OrdinalIgnoreCase)
                && e is not (QueryAnswered or QuerySilence);
    }

    private static bool IsContradictionEvent(GameEvent e) =>
        e.GetType().Name.Contains("Contradiction", StringComparison.OrdinalIgnoreCase);

    private static bool IsContradictionReport(Report? report) =>
        report is not null &&
        (report.Kind is ReportKind.Contradiction or ReportKind.Impossible ||
         report.Facts.ContainsKey("contradiction") ||
         report.FactKey.Contains("contradiction", StringComparison.OrdinalIgnoreCase));

    private static bool ContainsLiteralWord(string? text) =>
        text is not null &&
        (text.Contains("literal", StringComparison.OrdinalIgnoreCase) ||
         text.Contains("буквал", StringComparison.OrdinalIgnoreCase));

    private int EndMinute(GameState state)
    {
        if (_scenario.EndMinute > 0)
            return _scenario.EndMinute;
        if (state.EndMinute is int end)
            return end;
        return state.Clock.Now;
    }

    private static object? FirstProperty(object target, params string[] names)
    {
        var type = target.GetType();
        foreach (var name in names)
        {
            var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property is not null)
                return property.GetValue(target);
        }
        return null;
    }

    private static int? IntProperty(object target, params string[] names) =>
        FirstProperty(target, names) switch
        {
            int value => value,
            long value => (int)value,
            double value => (int)value,
            _ => null
        };

    private static double? DoubleProperty(object target, params string[] names) =>
        FirstProperty(target, names) switch
        {
            int value => value,
            long value => value,
            double value => value,
            _ => null
        };

    private static string? StringProperty(object target, params string[] names) =>
        FirstProperty(target, names) as string;

    private static bool? BoolProperty(object target, params string[] names) =>
        FirstProperty(target, names) as bool?;
}
