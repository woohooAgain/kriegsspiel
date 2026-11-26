namespace Kriegsspiel.Core.Contracts;

using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

public interface IGameRules
{
    int TurnMinutes => 30;
    int CommandPhaseMinutes => 30;
    int MinCourierLegMinutes => 5;
    int CourierLossReturnExtraMinutes => 60;
    int SilenceGraceMinutes => 15;
    int ReconWindowMinutes => 30;
    int MinTravelTimeMinutes => 5;
    double CombatStrengthThreshold => 0.30;
    int CombatMoraleThreshold => 30;
    int CombatReadinessThreshold => 20;
    int MaxBranches => 3;
    int MaxConfidence => 3;
    int CourierReplacementMinutes => 60;
    int QuerySilenceMinutes => 60;
    double BaseMisinterpretationProbability => 0.03;
    double MinMisinterpretationProbability => 0.03;
    double MaxMisinterpretationProbability => 0.90;
    double ReconBaseProbability => 0.25;
    double ReconObservabilityStep => 0.15;
    double ReconEdgePenalty => 0.15;
    double ReconMinProbability => 0.10;
    double ReconMaxProbability => 0.95;
    double ReportHonestyStep => 0.12;
    double OffroadReadinessPenalty => 5.0;
    double RestReadinessGain => 5.0;

    bool IsCombatEffective(Unit unit);

    bool CombatEffective(Unit unit) => IsCombatEffective(unit);

    bool IsCombatEffective(int strength, int initialStrengthSnapshot)
    {
        var snapshot = initialStrengthSnapshot > 0 ? initialStrengthSnapshot : strength;
        return strength >= CombatStrengthThreshold * snapshot;
    }

    bool IsTraitInRange(int value) => value is >= 0 and <= 3;

    bool IsConfidenceInRange(int value) => value is >= 1 and <= 3;

    int ClampConfidence(int value) => Math.Clamp(value, 1, MaxConfidence);

    bool IsProbabilityInRange(double value) => value >= 0 && value <= 1;

    bool HasValidCourierLegs(int deliveryMinutes, int returnMinutes) =>
        deliveryMinutes > 0 && returnMinutes > 0;

    bool IsValidOrder(Order order) =>
        order.Branches.Count <= MaxBranches && !string.IsNullOrWhiteSpace(order.ToUnitId);
}
