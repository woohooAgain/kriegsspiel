namespace Kriegsspiel.Core.Contracts;

using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

public interface IGameRules
{
    int TurnMinutes => 30;
    double CombatStrengthThreshold => 0.4;
    int CombatMoraleThreshold => 30;
    int CombatReadinessThreshold => 20;
    int MaxBranches => 3;
    int MaxConfidence => 3;
    int CourierReplacementMinutes => 60;
    int QuerySilenceMinutes => 60;
    double BaseMisinterpretationProbability => 0.03;
    double MinMisinterpretationProbability => 0.03;
    double MaxMisinterpretationProbability => 0.90;

    bool IsCombatEffective(Unit unit);

    bool CombatEffective(Unit unit) => IsCombatEffective(unit);

    bool IsCombatEffective(int strength, int initialStrengthSnapshot, int morale, int readiness, int ammo)
    {
        var snapshot = initialStrengthSnapshot > 0 ? initialStrengthSnapshot : strength;
        return strength >= CombatStrengthThreshold * snapshot &&
               morale >= CombatMoraleThreshold &&
               readiness >= CombatReadinessThreshold &&
               ammo > 0;
    }

    bool IsTraitInRange(int value) => value is >= 0 and <= 3;

    bool IsConfidenceInRange(int value) => value is >= 0 and <= 3;

    int ClampConfidence(int value) => Math.Clamp(value, 0, MaxConfidence);

    bool IsProbabilityInRange(double value) => value >= 0 && value <= 1;

    bool HasValidCourierLegs(int deliveryMinutes, int returnMinutes) =>
        deliveryMinutes > 0 && returnMinutes > 0;

    bool IsValidOrder(Order order) =>
        order.Branches.Count <= MaxBranches && !string.IsNullOrWhiteSpace(order.ToUnitId);
}
