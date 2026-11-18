namespace Kriegsspiel.Core.Domain;

public enum TerrainType { Open, Forest, Village, River, Bridge }

public sealed class Zone
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public TerrainType Terrain { get; init; }
    public Dictionary<string, int> TravelMinutes { get; init; } = new();
    public int DefenseBonus { get; init; }
    public int Observation { get; init; }

    public List<string> PresentUnitIds { get; init; } = new();
    public bool BridgeIntact { get; set; } = true;
    public int FortificationLevel { get; set; }
}