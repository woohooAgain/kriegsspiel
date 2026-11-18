namespace Kriegsspiel.Core.Content;

public enum ObjectiveType { HoldZone, PreserveStrength, DelayEnemy }

public sealed class Objective
{
    public ObjectiveType Type { get; init; }
    public string? Zone { get; init; }
    public string? Unit { get; init; }
    public int? UntilMinute { get; init; }
    public int? MinPercent { get; init; }
    public int? Minutes { get; init; }
    public double Weight { get; init; } = 1.0;
}