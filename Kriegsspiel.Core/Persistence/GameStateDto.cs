using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class EffectiveOrderEntryDto
{
    public required string Key { get; init; }
    public required EffectiveOrderDto Value { get; init; }
}

public sealed class GameStateDto
{
    public int SchemaVersion { get; init; }
    public required string ScenarioId { get; init; }
    public required ClockDto Clock { get; init; }
    public required PlayerKnowledgeDto Knowledge { get; init; }
    public List<ZoneDto> Zones { get; init; } = new();
    public List<UnitDto> Units { get; init; } = new();
    public List<OrderDto> Orders { get; init; } = new();
    public List<ReportDto> DeliveredReports { get; init; } = new();
    public List<ReportDto> InTransitReports { get; init; } = new();
    public List<ReportDto> LostReports { get; init; } = new();
    public List<CourierDto> Couriers { get; init; } = new();
    public List<PendingCasualtyDto> PendingCasualties { get; init; } = new();
    public List<GameEventDto> PendingEvents { get; init; } = new();
    public List<EffectiveOrderEntryDto> EffectiveOrders { get; init; } = new();
    public List<GameEventDto> EventLog { get; init; } = new();
    public int Seed { get; init; }
    public long RngState { get; init; }
    public int RngCallCount { get; init; }
    public int Turn { get; init; }
    public int? EndMinute { get; init; }
}
