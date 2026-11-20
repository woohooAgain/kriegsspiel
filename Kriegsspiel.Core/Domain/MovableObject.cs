namespace Kriegsspiel.Core.Domain;

public enum MovableObjectState
{
    Stationary = 0,
    Moving = 1,
    Delayed = 2,
    Escorted = 3,
    Released = 4,
    Lost = 5
}

public enum PrisonerStatus
{
    None = 0,
    NotInterrogated = 1,
    Interrogated = 2,
    Testified = 3
}

public class MovableObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; init; } = "";
    public string LocationZoneId { get; set; } = "";
    public Faction Owner { get; set; } = Faction.Neutral;
    public int SpeedKmh { get; set; } = 2;
    public MovableObjectState State { get; set; } = MovableObjectState.Stationary;
    public string? EscortUnitId { get; set; }
    public string? CurrentEdgeId { get; set; }
    public int? EtaMinute { get; set; }
    public PrisonerStatus PrisonerStatus { get; set; } = global::Kriegsspiel.Core.Domain.PrisonerStatus.None;

    private int _quantity;

    public int Quantity
    {
        get => _quantity;
        set => _quantity = Math.Max(0, value);
    }

    public Faction OwnerFaction
    {
        get => Owner;
        set => Owner = value;
    }

    public bool IsInTransit => State is MovableObjectState.Moving or MovableObjectState.Escorted;
}
