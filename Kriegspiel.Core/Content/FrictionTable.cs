namespace Kriegsspiel.Core.Content;

public sealed class FrictionTable
{
    public int CourierBaseDelayMinutes { get; init; } = 25;
    public int FieldCourierDelayMinutes { get; init; } = 15;
    public double CourierLostBase { get; init; } = 0.05;
    public double CourierLostPerKm { get; init; } = 0.01;
    public double CourierLostHostileBonus { get; init; } = 0.10;
    public double AmbiguityWeight { get; init; } = 0.8;
    public double LiteralismWeight { get; init; } = 0.15;
    public double CautionWeight { get; init; } = 0.10;
    public double InitiativeWeight { get; init; } = 0.25;
    public double CombatLethality { get; init; } = 0.35;
    public int ReportBaseDelayMinutes { get; init; } = 20;
    public int ReportJitterMinutes { get; init; } = 20;
    public double StrengthReportNoise { get; init; } = 0.25;
}