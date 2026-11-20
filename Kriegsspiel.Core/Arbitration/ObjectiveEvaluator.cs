using System.Reflection;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed record ObjectiveCheck(int Minute, ControlState Control, double Score);

public sealed record ObjectiveEvaluation(
    int Index,
    Objective Objective,
    double Weight,
    double Fraction,
    bool Achieved,
    double Threshold,
    IReadOnlyList<ObjectiveCheck> Checks);

public sealed record ScenarioObjectiveEvaluation(
    IReadOnlyList<ObjectiveEvaluation> Objectives,
    double WeightSum,
    bool WeightsValid,
    double Score,
    double VictoryThreshold,
    bool IsVictory);

public sealed class ObjectiveEvaluator
{
    public const double DefaultVictoryThreshold = 0.7;

    private const double Tolerance = 1e-9;

    private readonly Faction _friendlyFaction;

    public ObjectiveEvaluator(Faction? friendlyFaction = null)
    {
        _friendlyFaction = friendlyFaction ?? Faction.Blue;
    }

    public Faction FriendlyFaction => _friendlyFaction;

    public ScenarioObjectiveEvaluation Evaluate(Scenario scenario, GameState state)
    {
        ArgumentNullException.ThrowIfNull(scenario);
        ArgumentNullException.ThrowIfNull(state);

        var objectives = new List<ObjectiveEvaluation>(scenario.Objectives.Count);
        for (var index = 0; index < scenario.Objectives.Count; index++)
            objectives.Add(EvaluateObjective(scenario.Objectives[index], state, scenario, index));

        double weightSum = objectives.Sum(o => o.Weight);
        double score = weightSum <= Tolerance
            ? 0
            : objectives.Sum(o => o.Weight / weightSum * o.Fraction);
        double threshold = scenario.VictoryThreshold > 0
            ? scenario.VictoryThreshold
            : DefaultVictoryThreshold;

        return new ScenarioObjectiveEvaluation(
            objectives,
            weightSum,
            Math.Abs(weightSum - 1.0) <= Tolerance,
            score,
            threshold,
            score >= threshold);
    }

    public ObjectiveEvaluation EvaluateObjective(
        Objective objective,
        GameState state,
        Scenario? scenario = null,
        int index = 0,
        Faction? enemyFaction = null)
    {
        ArgumentNullException.ThrowIfNull(objective);
        ArgumentNullException.ThrowIfNull(state);

        var enemy = enemyFaction ?? ParseFaction(objective.TargetFaction)
            ?? (_friendlyFaction == Faction.Red ? Faction.Blue : Faction.Red);
        var minutes = CheckMinutes(objective, scenario, state);
        var checks = new List<ObjectiveCheck>(minutes.Count);

        foreach (var minute in minutes)
            checks.Add(Check(objective, state, enemy, minute, scenario));

        double fraction = checks.Count == 0
            ? 0
            : checks.Average(c => c.Score);
        double threshold = objective.VictoryThreshold > 0
            ? objective.VictoryThreshold
            : DefaultVictoryThreshold;

        return new ObjectiveEvaluation(
            index,
            objective,
            objective.Weight,
            fraction,
            fraction >= threshold,
            threshold,
            checks);
    }

    private ObjectiveCheck Check(
        Objective objective,
        GameState state,
        Faction enemy,
        int minute,
        Scenario? scenario)
    {
        var zone = objective.Zone is not null && state.Zones.TryGetValue(objective.Zone, out var found)
            ? found
            : null;

        return objective.Type switch
        {
            ObjectiveType.HoldZone => ControlCheck(objective, state, enemy, zone, minute, requireFriendly: true),
            ObjectiveType.NoEnemyCombatEffectiveInZone => ControlCheck(objective, state, enemy, zone, minute, requireFriendly: false),
            ObjectiveType.BlockEvacuation => BlockCheck(objective, state, enemy, zone, minute),
            ObjectiveType.EvacuateCivilians => EvacuationCheck(objective, state, minute),
            ObjectiveType.PreserveStrength => StrengthCheck(objective, state, minute),
            ObjectiveType.DelayEnemy => DelayCheck(objective, state, enemy, minute, scenario),
            ObjectiveType.DisableEnemyStrength => DisableCheck(objective, state, enemy, minute),
            _ => new ObjectiveCheck(minute, ControlState.Failed, 0)
        };
    }

    private ObjectiveCheck ControlCheck(
        Objective objective,
        GameState state,
        Faction enemy,
        Zone? zone,
        int minute,
        bool requireFriendly)
    {
        var control = ClassifyControl(zone, state, enemy, requireFriendly);
        return new ObjectiveCheck(minute, control, StateWeight(objective, control));
    }

    private ObjectiveCheck BlockCheck(
        Objective objective,
        GameState state,
        Faction enemy,
        Zone? zone,
        int minute)
    {
        var group = FindCivilianGroup(objective, state, zone);
        var control = ClassifyControl(zone, state, enemy, requireFriendly: true);
        if (group is not null && group.Evacuated)
            control = ControlState.Failed;

        return new ObjectiveCheck(minute, control, StateWeight(objective, control));
    }

    private ObjectiveCheck EvacuationCheck(Objective objective, GameState state, int minute)
    {
        var group = FindCivilianGroup(objective, state, null);
        var deadline = objective.UntilMinute ?? group?.EvacuationDeadline;
        bool complete = group is not null
            && group.Evacuated
            && (objective.SafeZone is null || group.LocationZoneId == objective.SafeZone)
            && (deadline is null || minute <= deadline.Value);

        var control = complete ? ControlState.Achieved : ControlState.Failed;
        return new ObjectiveCheck(minute, control, StateWeight(objective, control));
    }

    private ObjectiveCheck StrengthCheck(Objective objective, GameState state, int minute)
    {
        var unit = objective.Unit is not null && state.Units.TryGetValue(objective.Unit, out var found)
            ? found
            : null;
        int required = objective.MinPercent ?? 50;
        bool complete = unit is not null
            && !unit.Captured
            && unit.StrengthSnapshot > 0
            && 100.0 * unit.Strength / unit.StrengthSnapshot >= required;

        var control = complete ? ControlState.Achieved : ControlState.Failed;
        return new ObjectiveCheck(minute, control, complete ? 1 : 0);
    }

    private ObjectiveCheck DelayCheck(
        Objective objective,
        GameState state,
        Faction enemy,
        int minute,
        Scenario? scenario)
    {
        int required = Math.Max(1, objective.Minutes ?? 30);
        int start = scenario?.StartMinute ?? 0;
        int arrival = FirstEnemyArrival(objective, state, enemy) ?? ResolveEndMinute(state, scenario);
        int elapsed = Math.Clamp(Math.Min(minute, arrival) - start, 0, required);
        double fraction = (double)elapsed / required;
        var control = fraction >= 1 - Tolerance
            ? ControlState.Achieved
            : fraction > 0 ? ControlState.Contested : ControlState.Failed;

        return new ObjectiveCheck(minute, control, fraction);
    }

    private ObjectiveCheck DisableCheck(Objective objective, GameState state, Faction enemy, int minute)
    {
        var enemyUnits = state.Units.Values
            .Where(u => u.Faction == enemy || (enemy == Faction.Neutral && u.IsEnemyOf(_friendlyFaction)))
            .ToList();
        int total = enemyUnits.Sum(u => u.StrengthSnapshot);
        int disabled = enemyUnits
            .Where(u => !u.CombatEffective || u.Captured)
            .Sum(u => u.StrengthSnapshot);
        double actual = total > 0 ? (double)disabled / total : 0;
        int requiredPercent = Math.Max(1, objective.MinPercent ?? 40);
        double fraction = Math.Clamp(actual / (requiredPercent / 100.0), 0, 1);
        var control = fraction >= 1 - Tolerance
            ? ControlState.Achieved
            : fraction > 0 ? ControlState.Contested : ControlState.Failed;

        return new ObjectiveCheck(minute, control, fraction);
    }

    private ControlState ClassifyControl(Zone? zone, GameState state, Faction enemy, bool requireFriendly)
    {
        if (zone is null)
            return ControlState.Failed;

        var present = state.Units.Values
            .Where(u => u.LocationZoneId == zone.Id || zone.PresentUnitIds.Contains(u.Id))
            .ToList();
        bool friendly = present.Any(u => u.CombatEffective && u.Faction == _friendlyFaction);
        bool hostile = present.Any(u => u.CombatEffective && IsEnemy(u, enemy));

        if (!requireFriendly && !hostile)
            return ControlState.Achieved;
        if (friendly && !hostile)
            return ControlState.Achieved;
        if (friendly && hostile)
            return ControlState.Contested;
        return ControlState.Failed;
    }

    private bool IsEnemy(Unit unit, Faction enemy) =>
        enemy == Faction.Neutral ? unit.IsEnemyOf(_friendlyFaction) : unit.Faction == enemy;

    private static double StateWeight(Objective objective, ControlState state) =>
        objective.ControlStateWeights.GetValueOrDefault(state.ToString(), state switch
        {
            ControlState.Achieved => 1.0,
            ControlState.Contested => 0.5,
            _ => 0.0
        });

    private static List<int> CheckMinutes(Objective objective, Scenario? scenario, GameState state)
    {
        if (objective.ControlMinutes.Count > 0)
            return objective.ControlMinutes.Distinct().OrderBy(m => m).ToList();
        if (objective.UntilMinute is int until)
            return [until];
        if (scenario is not null && scenario.EndMinute > 0)
            return [scenario.EndMinute];
        return [state.Clock.Now];
    }

    private static int ResolveEndMinute(GameState state, Scenario? scenario)
    {
        if (scenario is not null && scenario.EndMinute > 0)
            return scenario.EndMinute;
        if (state.EndMinute is int end)
            return end;
        return state.Clock.Now;
    }

    private int? FirstEnemyArrival(Objective objective, GameState state, Faction enemy)
    {
        if (objective.Zone is null)
            return null;

        var minutes = new List<int>();
        foreach (var e in state.EventLog)
        {
            var unitId = e switch
            {
                UnitArrived a when a.ZoneId == objective.Zone => a.UnitId,
                UnitMoved m when m.ToZoneId == objective.Zone => m.UnitId,
                _ => GenericArrivalUnit(e, objective.Zone)
            };
            if (unitId is not null && state.Units.TryGetValue(unitId, out var unit) &&
                (unit.Faction == enemy || enemy == Faction.Neutral && unit.IsEnemyOf(_friendlyFaction)))
                minutes.Add(e.Minute);
        }

        return minutes.Count == 0 ? null : minutes.Min();
    }

    private static string? GenericArrivalUnit(GameEvent e, string zoneId)
    {
        var name = e.GetType().Name;
        if (!name.Contains("Arriv", StringComparison.OrdinalIgnoreCase) &&
            !name.Contains("Enter", StringComparison.OrdinalIgnoreCase))
            return null;

        var type = e.GetType();
        string? eventZone = type.GetProperty("ZoneId", BindingFlags.Public | BindingFlags.Instance)?.GetValue(e) as string
            ?? type.GetProperty("ToZoneId", BindingFlags.Public | BindingFlags.Instance)?.GetValue(e) as string;
        if (eventZone != zoneId)
            return null;

        return type.GetProperty("UnitId", BindingFlags.Public | BindingFlags.Instance)?.GetValue(e) as string;
    }

    private static CivilianGroup? FindCivilianGroup(Objective objective, GameState state, Zone? zone)
    {
        if (objective.CivilianGroupId is not null)
        {
            foreach (var group in state.Zones.Values.SelectMany(z => z.Civilians))
            {
                if (group.Id == objective.CivilianGroupId)
                    return group;
            }
        }

        if (zone is not null && zone.Civilians.Count > 0)
            return zone.Civilians[0];

        return state.Zones.Values.SelectMany(z => z.Civilians).FirstOrDefault();
    }

    private static Faction? ParseFaction(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;
        if (value.Equals("ENEMY", StringComparison.OrdinalIgnoreCase))
            return Faction.Red;
        return Enum.TryParse<Faction>(value, true, out var faction) ? faction : null;
    }
}
