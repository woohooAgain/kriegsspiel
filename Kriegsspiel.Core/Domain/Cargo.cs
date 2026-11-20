namespace Kriegsspiel.Core.Domain;

public enum CargoType
{
    Ammo = 0,
    Supplies = 1
}

public sealed class Cargo : MovableObject
{
    public CargoType Type { get; set; } = CargoType.Ammo;

    public CargoType Kind
    {
        get => Type;
        set => Type = value;
    }

    public int Amount
    {
        get => Quantity;
        set => Quantity = value;
    }

    public bool IsAvailableTo(Faction faction, Zone? zone) =>
        Owner == faction &&
        (zone is null || zone.ControllingFaction is null || zone.ControllingFaction == faction);
}
