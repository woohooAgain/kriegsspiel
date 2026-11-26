namespace Kriegsspiel.Core.Domain;

public enum TerrainType
{
    Open,
    Forest,
    Village,
    River,
    Bridge
}

public enum EdgeType
{
    Road = 0,
    Offroad = 1,
    Crossing = 2
}

public sealed class Zone
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public TerrainType Terrain { get; init; }
    public Dictionary<string, int> TravelMinutes { get; init; } = new();
    public Dictionary<string, int> CourierMinutes { get; init; } = new();
    public int DefenseBonus { get; init; }
    public int Observation { get; init; }

    public List<string> PresentUnitIds { get; init; } = new();
    public bool BridgeIntact { get; set; } = true;
    public int FortificationLevel { get; set; }

    public Dictionary<string, EdgeType> EdgeTypes { get; init; } = new();
    public List<ZoneObject> Objects { get; init; } = new();
    public List<Cargo> Cargo { get; init; } = new();
    public List<CivilianGroup> Civilians { get; init; } = new();
    public Faction? ControllingFaction { get; set; }
    public double Cover { get; set; }
    public double MoveMultiplier { get; init; } = 1.0;
    public int TemporaryDefenseBonus { get; set; }

    public int Observability => Observation;

    public bool IsCrossing =>
        Terrain == TerrainType.Bridge ||
        Objects.Any(o => o.Kind == ZoneObjectKind.Bridge && o.IsIntact);

    public EdgeType EdgeTypeFor(string neighborZoneId) =>
        EdgeTypes.GetValueOrDefault(
            neighborZoneId,
            Terrain == TerrainType.Bridge ? EdgeType.Crossing : EdgeType.Road);

    public bool IsControlledBy(Faction faction) => ControllingFaction == faction;

    public bool HasCargo(CargoType type) => Cargo.Any(c => c.Type == type && c.Quantity > 0);

    public int CargoAmount(CargoType type) =>
        Cargo.Where(c => c.Type == type).Sum(c => c.Quantity);

    public void SetEdgeType(string neighborZoneId, EdgeType type) =>
        EdgeTypes[neighborZoneId] = type;
}
