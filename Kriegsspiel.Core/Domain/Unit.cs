namespace Kriegsspiel.Core.Domain;

public enum Faction
{
    Blue = 0,
    Red = 1,
    Neutral = 2
}

public enum UnitType
{
    Infantry,
    Artillery,
    Recon,
    HQ
}

public enum Posture
{
    March,
    Defend,
    Attack,
    Reserve,
    Withdraw
}

public sealed class Unit
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public UnitType Type { get; init; }
    public int Strength { get; set; }
    public int Readiness { get; set; }
    public int Morale { get; set; }
    public int Fatigue { get; set; }
    public int Ammo { get; set; }
    public required string LocationZoneId { get; set; }
    public Posture Posture { get; set; } = Posture.Reserve;
    public int SpeedKmh { get; init; } = 4;
    public required CommanderTraits Traits { get; init; }
    public string? CurrentOrderId { get; set; }

    public Faction Faction { get; init; } = global::Kriegsspiel.Core.Domain.Faction.Blue;
    public int InitialStrengthSnapshot { get; init; }
    public int Training { get; init; } = 1;
    public int Experience { get; set; }
    public bool CommandLossResilient { get; init; }
    public bool CommanderLost { get; set; }
    public bool Concealed { get; set; }
    public string? AttentionEdgeId { get; set; }
    public string? MoveEdgeId { get; set; }
    public string? GuardOfObjectId { get; set; }
    public bool Captured { get; set; }
    public bool NoPrisonersPolicy { get; set; }
    public string? EffectiveOrderId { get; set; }
    public bool PoorControl { get; set; }

    private EffectiveOrder? _effectiveOrder;

    public EffectiveOrder? EffectiveOrder
    {
        get => _effectiveOrder;
        set
        {
            _effectiveOrder = value;
            if (value is not null)
                EffectiveOrderId = value.SourceOrderId;
        }
    }

    public EffectiveOrder? CurrentEffectiveOrder
    {
        get => _effectiveOrder;
        set => EffectiveOrder = value;
    }

    public int StrengthSnapshot => InitialStrengthSnapshot > 0 ? InitialStrengthSnapshot : Strength;

    public bool IsCombatEffective =>
        Strength >= 0.4 * StrengthSnapshot &&
        Morale >= 30 &&
        Readiness >= 20 &&
        Ammo > 0;

    public bool CombatEffective => IsCombatEffective;

    public bool IsEnemyOf(Faction other) =>
        Faction != global::Kriegsspiel.Core.Domain.Faction.Neutral &&
        other != global::Kriegsspiel.Core.Domain.Faction.Neutral &&
        Faction != other;
}
