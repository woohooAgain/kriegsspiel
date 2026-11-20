namespace Kriegsspiel.Core.Content;

public enum ObjectiveType
{
    HoldZone,
    PreserveStrength,
    DelayEnemy,
    DisableEnemyStrength,
    NoEnemyCombatEffectiveInZone,
    EvacuateCivilians,
    BlockEvacuation
}

public enum ControlState
{
    Achieved,
    Contested,
    Failed
}

public sealed class Objective
{
    public ObjectiveType Type { get; init; }
    public string? Zone { get; init; }
    public string? Unit { get; init; }
    public string? TargetFaction { get; init; }
    public string? SafeZone { get; init; }
    public string? CivilianGroupId { get; init; }
    public int? UntilMinute { get; init; }
    public int? MinPercent { get; init; }
    public int? Minutes { get; init; }
    public List<int> ControlMinutes { get; init; } = new();
    public double Weight { get; init; } = 1.0;
    public double VictoryThreshold { get; init; }
    public Dictionary<string, double> ControlStateWeights { get; init; } = new()
    {
        [nameof(ControlState.Achieved)] = 1.0,
        [nameof(ControlState.Contested)] = 0.5,
        [nameof(ControlState.Failed)] = 0.0
    };
}