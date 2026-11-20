namespace Kriegsspiel.Core.Domain;

public sealed class CivilianGroup : MovableObject
{
    private int _panic;

    public int Panic
    {
        get => _panic;
        set => _panic = Math.Clamp(value, 0, 3);
    }

    public Faction Loyalty { get; set; } = Faction.Neutral;
    public bool LoyaltyHidden => true;
    public int Casualties { get; set; }
    public bool Evacuated { get; set; }
    public string? EvacuationTargetZoneId { get; set; }
    public int? EvacuationDeadline { get; set; }

    public int Population
    {
        get => Quantity;
        set => Quantity = value;
    }
}
