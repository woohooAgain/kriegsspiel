namespace Kriegsspiel.Core.Domain;

public enum UnitType { Infantry, Artillery, Recon, HQ }
public enum Posture { March, Defend, Attack, Reserve, Withdraw }

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
}