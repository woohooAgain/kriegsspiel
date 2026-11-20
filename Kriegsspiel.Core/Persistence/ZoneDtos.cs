using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public abstract class MovableObjectDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string LocationZoneId { get; init; } = "";
    public Faction Owner { get; init; }
    public int SpeedKmh { get; init; }
    public MovableObjectState State { get; init; }
    public string? EscortUnitId { get; init; }
    public string? CurrentEdgeId { get; init; }
    public int? EtaMinute { get; init; }
    public PrisonerStatus PrisonerStatus { get; init; }
    public int Quantity { get; init; }

    protected void CopyTo(MovableObject target)
    {
        target.LocationZoneId = LocationZoneId;
        target.Owner = Owner;
        target.SpeedKmh = SpeedKmh;
        target.State = State;
        target.EscortUnitId = EscortUnitId;
        target.CurrentEdgeId = CurrentEdgeId;
        target.EtaMinute = EtaMinute;
        target.PrisonerStatus = PrisonerStatus;
        target.Quantity = Quantity;
    }
}

public sealed class CargoDto : MovableObjectDto
{
    public CargoType Type { get; init; }

    public static CargoDto From(Cargo cargo) => new()
    {
        Id = cargo.Id,
        Name = cargo.Name,
        LocationZoneId = cargo.LocationZoneId,
        Owner = cargo.Owner,
        SpeedKmh = cargo.SpeedKmh,
        State = cargo.State,
        EscortUnitId = cargo.EscortUnitId,
        CurrentEdgeId = cargo.CurrentEdgeId,
        EtaMinute = cargo.EtaMinute,
        PrisonerStatus = cargo.PrisonerStatus,
        Quantity = cargo.Quantity,
        Type = cargo.Type
    };

    public Cargo ToDomain()
    {
        var cargo = new Cargo { Id = Id, Name = Name, Type = Type };
        CopyTo(cargo);
        return cargo;
    }
}

public sealed class CivilianGroupDto : MovableObjectDto
{
    public int Panic { get; init; }
    public Faction Loyalty { get; init; }
    public int Casualties { get; init; }
    public bool Evacuated { get; init; }
    public string? EvacuationTargetZoneId { get; init; }
    public int? EvacuationDeadline { get; init; }

    public static CivilianGroupDto From(CivilianGroup group) => new()
    {
        Id = group.Id,
        Name = group.Name,
        LocationZoneId = group.LocationZoneId,
        Owner = group.Owner,
        SpeedKmh = group.SpeedKmh,
        State = group.State,
        EscortUnitId = group.EscortUnitId,
        CurrentEdgeId = group.CurrentEdgeId,
        EtaMinute = group.EtaMinute,
        PrisonerStatus = group.PrisonerStatus,
        Quantity = group.Quantity,
        Panic = group.Panic,
        Loyalty = group.Loyalty,
        Casualties = group.Casualties,
        Evacuated = group.Evacuated,
        EvacuationTargetZoneId = group.EvacuationTargetZoneId,
        EvacuationDeadline = group.EvacuationDeadline
    };

    public CivilianGroup ToDomain()
    {
        var group = new CivilianGroup
        {
            Id = Id,
            Name = Name,
            Panic = Panic,
            Loyalty = Loyalty,
            Casualties = Casualties,
            Evacuated = Evacuated,
            EvacuationTargetZoneId = EvacuationTargetZoneId,
            EvacuationDeadline = EvacuationDeadline
        };
        CopyTo(group);
        return group;
    }
}

public sealed class ZoneObjectDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public ZoneObjectKind Kind { get; init; }
    public string? ZoneId { get; init; }
    public string? EdgeId { get; init; }
    public int Integrity { get; init; }
    public ZoneObjectState State { get; init; }
    public int DefenseBonus { get; init; }
    public int ObservationBonus { get; init; }
    public int AmmoCapacity { get; init; }
    public Faction? ControlledBy { get; init; }

    public static ZoneObjectDto From(ZoneObject obj) => new()
    {
        Id = obj.Id,
        Name = obj.Name,
        Kind = obj.Kind,
        ZoneId = obj.ZoneId,
        EdgeId = obj.EdgeId,
        Integrity = obj.Integrity,
        State = obj.State,
        DefenseBonus = obj.DefenseBonus,
        ObservationBonus = obj.ObservationBonus,
        AmmoCapacity = obj.AmmoCapacity,
        ControlledBy = obj.ControlledBy
    };

    public ZoneObject ToDomain() => new()
    {
        Id = Id,
        Name = Name,
        Kind = Kind,
        ZoneId = ZoneId,
        EdgeId = EdgeId,
        Integrity = Integrity,
        State = State,
        DefenseBonus = DefenseBonus,
        ObservationBonus = ObservationBonus,
        AmmoCapacity = AmmoCapacity,
        ControlledBy = ControlledBy
    };
}

public sealed class ZoneDto
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public TerrainType Terrain { get; init; }
    public Dictionary<string, int> TravelMinutes { get; init; } = new();
    public int DefenseBonus { get; init; }
    public int Observation { get; init; }
    public List<string> PresentUnitIds { get; init; } = new();
    public bool BridgeIntact { get; init; }
    public int FortificationLevel { get; init; }
    public Dictionary<string, EdgeType> EdgeTypes { get; init; } = new();
    public List<ZoneObjectDto> Objects { get; init; } = new();
    public List<CargoDto> Cargo { get; init; } = new();
    public List<CivilianGroupDto> Civilians { get; init; } = new();
    public Faction? ControllingFaction { get; init; }
    public double Cover { get; init; }
    public double MoveMultiplier { get; init; }
    public int TemporaryDefenseBonus { get; init; }

    public static ZoneDto From(Zone zone) => new()
    {
        Id = zone.Id,
        Name = zone.Name,
        Terrain = zone.Terrain,
        TravelMinutes = new Dictionary<string, int>(zone.TravelMinutes),
        DefenseBonus = zone.DefenseBonus,
        Observation = zone.Observation,
        PresentUnitIds = new List<string>(zone.PresentUnitIds),
        BridgeIntact = zone.BridgeIntact,
        FortificationLevel = zone.FortificationLevel,
        EdgeTypes = new Dictionary<string, EdgeType>(zone.EdgeTypes),
        Objects = zone.Objects.Select(ZoneObjectDto.From).ToList(),
        Cargo = zone.Cargo.Select(CargoDto.From).ToList(),
        Civilians = zone.Civilians.Select(CivilianGroupDto.From).ToList(),
        ControllingFaction = zone.ControllingFaction,
        Cover = zone.Cover,
        MoveMultiplier = zone.MoveMultiplier,
        TemporaryDefenseBonus = zone.TemporaryDefenseBonus
    };

    public Zone ToDomain() => new()
    {
        Id = Id,
        Name = Name,
        Terrain = Terrain,
        TravelMinutes = new Dictionary<string, int>(TravelMinutes),
        DefenseBonus = DefenseBonus,
        Observation = Observation,
        PresentUnitIds = new List<string>(PresentUnitIds),
        BridgeIntact = BridgeIntact,
        FortificationLevel = FortificationLevel,
        EdgeTypes = new Dictionary<string, EdgeType>(EdgeTypes),
        Objects = Objects.Select(o => o.ToDomain()).ToList(),
        Cargo = Cargo.Select(c => c.ToDomain()).ToList(),
        Civilians = Civilians.Select(c => c.ToDomain()).ToList(),
        ControllingFaction = ControllingFaction,
        Cover = Cover,
        MoveMultiplier = MoveMultiplier,
        TemporaryDefenseBonus = TemporaryDefenseBonus
    };
}
