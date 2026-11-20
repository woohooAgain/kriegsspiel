namespace Kriegsspiel.Core.Domain;

public enum ZoneObjectKind
{
    Bridge = 0,
    Fortification = 1,
    ObservationPost = 2,
    Depot = 3
}

public enum ZoneObjectState
{
    Intact = 0,
    Damaged = 1,
    Destroyed = 2
}

public sealed class ZoneObject
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string Name { get; init; } = "";
    public ZoneObjectKind Kind { get; init; } = ZoneObjectKind.Bridge;
    public string? ZoneId { get; set; }
    public string? EdgeId { get; set; }
    public int Integrity { get; set; } = 100;
    public ZoneObjectState State { get; set; } = ZoneObjectState.Intact;
    public int DefenseBonus { get; set; }
    public int ObservationBonus { get; set; }
    public int AmmoCapacity { get; set; }
    public Faction? ControlledBy { get; set; }

    public bool Destroyed
    {
        get => State == ZoneObjectState.Destroyed;
        set => State = value ? ZoneObjectState.Destroyed : ZoneObjectState.Intact;
    }

    public bool IsIntact => State != ZoneObjectState.Destroyed && Integrity > 0;
    public bool Functional => IsIntact;
    public bool Passable => IsIntact;
    public bool IsBridge => Kind == ZoneObjectKind.Bridge;
}
