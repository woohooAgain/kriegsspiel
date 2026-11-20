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

    public List<Report> InTransitReports { get; init; } = new();
    public List<Report> LostReports { get; init; } = new();
    public List<Courier> Couriers { get; init; } = new();
    public List<PendingCasualty> PendingCasualties { get; init; } = new();
    public List<GameEvent> PendingEvents { get; init; } = new();
    public Dictionary<string, EffectiveOrder> EffectiveOrders { get; init; } = new();
    public int Seed { get; set; }
    public long RngState { get; set; }
    public int RngCallCount { get; set; }
    public int? EndMinute { get; set; }

    public List<Courier> CourierPool => Couriers;

    public bool HasPendingDeliveryWork =>
        InTransitReports.Count > 0 ||
        PendingEvents.Count > 0 ||
        PendingCasualties.Count > 0;
}
