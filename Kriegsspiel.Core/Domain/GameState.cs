using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Domain;

public sealed class GameState
{
    public required string ScenarioId { get; init; }
    public required Clock Clock { get; init; }
    public required Dictionary<string, Zone> Zones { get; init; }
    public required Dictionary<string, Unit> Units { get; init; }
    public required PlayerKnowledge Knowledge { get; init; }
    public required List<Order> Orders { get; init; } = new();
    public required List<Report> DeliveredReports { get; init; } = new();
    public required List<GameEvent> EventLog { get; init; } = new();
    public int Turn { get; set; }
}