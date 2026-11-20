using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Persistence;

public sealed class CommanderTraitsDto
{
    public int Initiative { get; init; }
    public int Caution { get; init; }
    public int Literalism { get; init; }
    public int ReportHonesty { get; init; }
    public int Professionalism { get; init; }

    public static CommanderTraitsDto From(CommanderTraits traits) => new()
    {
        Initiative = traits.Initiative,
        Caution = traits.Caution,
        Literalism = traits.Literalism,
        ReportHonesty = traits.ReportHonesty,
        Professionalism = traits.Professionalism
    };

    public CommanderTraits ToDomain() => new(Initiative, Caution, Literalism, ReportHonesty, Professionalism);
}

public sealed class UnitDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public UnitType Type { get; init; }
    public int Strength { get; init; }
    public int Readiness { get; init; }
    public int Morale { get; init; }
    public int Fatigue { get; init; }
    public int Ammo { get; init; }
    public string LocationZoneId { get; init; } = "";
    public Posture Posture { get; init; }
    public int SpeedKmh { get; init; }
    public CommanderTraitsDto Traits { get; init; } = new();
    public string? CurrentOrderId { get; init; }
    public Faction Faction { get; init; }
    public int InitialStrengthSnapshot { get; init; }
    public int Training { get; init; }
    public int Experience { get; init; }
    public bool CommandLossResilient { get; init; }
    public bool CommanderLost { get; init; }
    public bool Concealed { get; init; }
    public string? AttentionEdgeId { get; init; }
    public string? MoveEdgeId { get; init; }
    public string? GuardOfObjectId { get; init; }
    public bool Captured { get; init; }
    public bool NoPrisonersPolicy { get; init; }
    public string? EffectiveOrderId { get; init; }
    public string? EffectiveOrderKey { get; init; }
    public bool PoorControl { get; init; }

    public static UnitDto From(Unit unit, string? effectiveOrderKey) => new()
    {
        Id = unit.Id,
        Name = unit.Name,
        Type = unit.Type,
        Strength = unit.Strength,
        Readiness = unit.Readiness,
        Morale = unit.Morale,
        Fatigue = unit.Fatigue,
        Ammo = unit.Ammo,
        LocationZoneId = unit.LocationZoneId,
        Posture = unit.Posture,
        SpeedKmh = unit.SpeedKmh,
        Traits = CommanderTraitsDto.From(unit.Traits),
        CurrentOrderId = unit.CurrentOrderId,
        Faction = unit.Faction,
        InitialStrengthSnapshot = unit.InitialStrengthSnapshot,
        Training = unit.Training,
        Experience = unit.Experience,
        CommandLossResilient = unit.CommandLossResilient,
        CommanderLost = unit.CommanderLost,
        Concealed = unit.Concealed,
        AttentionEdgeId = unit.AttentionEdgeId,
        MoveEdgeId = unit.MoveEdgeId,
        GuardOfObjectId = unit.GuardOfObjectId,
        Captured = unit.Captured,
        NoPrisonersPolicy = unit.NoPrisonersPolicy,
        EffectiveOrderId = unit.EffectiveOrderId,
        EffectiveOrderKey = effectiveOrderKey,
        PoorControl = unit.PoorControl
    };

    public Unit ToDomain() => new()
    {
        Id = Id,
        Name = Name,
        Type = Type,
        Strength = Strength,
        Readiness = Readiness,
        Morale = Morale,
        Fatigue = Fatigue,
        Ammo = Ammo,
        LocationZoneId = LocationZoneId,
        Posture = Posture,
        SpeedKmh = SpeedKmh,
        Traits = Traits.ToDomain(),
        CurrentOrderId = CurrentOrderId,
        Faction = Faction,
        InitialStrengthSnapshot = InitialStrengthSnapshot,
        Training = Training,
        Experience = Experience,
        CommandLossResilient = CommandLossResilient,
        CommanderLost = CommanderLost,
        Concealed = Concealed,
        AttentionEdgeId = AttentionEdgeId,
        MoveEdgeId = MoveEdgeId,
        GuardOfObjectId = GuardOfObjectId,
        Captured = Captured,
        NoPrisonersPolicy = NoPrisonersPolicy,
        EffectiveOrderId = EffectiveOrderId,
        PoorControl = PoorControl
    };
}
