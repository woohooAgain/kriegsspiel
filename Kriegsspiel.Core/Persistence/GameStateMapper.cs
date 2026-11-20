using System.Reflection;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public static class GameStateMapper
{
    private static readonly FieldInfo ScenarioIdField = typeof(GameState).GetField(
        "<ScenarioId>k__BackingField",
        BindingFlags.Instance | BindingFlags.NonPublic)!;

    public static GameStateDto ToDto(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);

        var effectiveKeys = new Dictionary<EffectiveOrder, string>();
        foreach (var (key, value) in state.EffectiveOrders)
            effectiveKeys[value] = key;

        return new GameStateDto
        {
            SchemaVersion = PersistenceSchema.CurrentVersion,
            ScenarioId = state.ScenarioId,
            Clock = ClockDto.From(state.Clock),
            Knowledge = PlayerKnowledgeDto.From(state.Knowledge),
            Zones = state.Zones.Values.Select(ZoneDto.From).ToList(),
            Units = state.Units.Values.Select(u => UnitDto.From(u, KeyFor(u))).ToList(),
            Orders = state.Orders.Select(OrderDto.From).ToList(),
            DeliveredReports = state.DeliveredReports.Select(ReportDto.From).ToList(),
            InTransitReports = state.InTransitReports.Select(ReportDto.From).ToList(),
            LostReports = state.LostReports.Select(ReportDto.From).ToList(),
            Couriers = state.Couriers.Select(CourierDto.From).ToList(),
            PendingCasualties = state.PendingCasualties.Select(PendingCasualtyDto.From).ToList(),
            PendingEvents = state.PendingEvents.Select(GameEventDto.From).ToList(),
            EffectiveOrders = state.EffectiveOrders.Select(kv => new EffectiveOrderEntryDto
            {
                Key = kv.Key,
                Value = EffectiveOrderDto.From(kv.Value)
            }).ToList(),
            EventLog = state.EventLog.Select(GameEventDto.From).ToList(),
            Seed = state.Seed,
            RngState = state.RngState,
            RngCallCount = state.RngCallCount,
            Turn = state.Turn,
            EndMinute = state.EndMinute
        };

        string? KeyFor(Unit unit) =>
            unit.EffectiveOrder is not null && effectiveKeys.TryGetValue(unit.EffectiveOrder, out var key)
                ? key
                : null;
    }

    public static GameState FromDto(GameStateDto dto)
    {
        Validate(dto);

        var state = new GameState
        {
            ScenarioId = dto.ScenarioId,
            Clock = dto.Clock.ToDomain(),
            Zones = new(),
            Units = new(),
            Knowledge = dto.Knowledge.ToDomain(),
            Seed = dto.Seed,
            RngState = dto.RngState,
            RngCallCount = dto.RngCallCount,
            Turn = dto.Turn,
            EndMinute = dto.EndMinute
        };

        foreach (var zone in dto.Zones)
        {
            var value = zone.ToDomain();
            state.Zones[value.Id] = value;
        }

        foreach (var entry in dto.EffectiveOrders)
            state.EffectiveOrders[entry.Key] = entry.Value.ToDomain();

        foreach (var unit in dto.Units)
        {
            var value = unit.ToDomain();
            if (unit.EffectiveOrderKey is { } key && state.EffectiveOrders.TryGetValue(key, out var effectiveOrder))
                value.EffectiveOrder = effectiveOrder;
            value.EffectiveOrderId = unit.EffectiveOrderId;
            state.Units[value.Id] = value;
        }

        state.Orders.AddRange(dto.Orders.Select(o => o.ToDomain()));
        state.DeliveredReports.AddRange(dto.DeliveredReports.Select(r => r.ToDomain()));
        state.InTransitReports.AddRange(dto.InTransitReports.Select(r => r.ToDomain()));
        state.LostReports.AddRange(dto.LostReports.Select(r => r.ToDomain()));
        state.Couriers.AddRange(dto.Couriers.Select(c => c.ToDomain()));
        state.PendingCasualties.AddRange(dto.PendingCasualties.Select(c => c.ToDomain()));
        state.PendingEvents.AddRange(dto.PendingEvents.Select(e => e.ToDomain()));
        state.EventLog.AddRange(dto.EventLog.Select(e => e.ToDomain()));
        return state;
    }

    public static void ApplyTo(GameStateDto dto, GameState target)
    {
        Validate(dto);
        ArgumentNullException.ThrowIfNull(target);
        var loaded = FromDto(dto);

        ScenarioIdField.SetValue(target, loaded.ScenarioId);
        dto.Clock.ApplyTo(target.Clock);
        Replace(target.Zones, loaded.Zones);
        Replace(target.Units, loaded.Units);
        Replace(target.EffectiveOrders, loaded.EffectiveOrders);

        target.Orders.Clear();
        target.Orders.AddRange(loaded.Orders);
        target.DeliveredReports.Clear();
        target.DeliveredReports.AddRange(loaded.DeliveredReports);
        target.InTransitReports.Clear();
        target.InTransitReports.AddRange(loaded.InTransitReports);
        target.LostReports.Clear();
        target.LostReports.AddRange(loaded.LostReports);
        target.Couriers.Clear();
        target.Couriers.AddRange(loaded.Couriers);
        target.PendingCasualties.Clear();
        target.PendingCasualties.AddRange(loaded.PendingCasualties);
        target.PendingEvents.Clear();
        target.PendingEvents.AddRange(loaded.PendingEvents);
        target.EventLog.Clear();
        target.EventLog.AddRange(loaded.EventLog);

        target.Knowledge.Zones.Clear();
        foreach (var (key, value) in loaded.Knowledge.Zones)
            target.Knowledge.Zones[key] = value;
        target.Knowledge.Facts.Clear();
        foreach (var (key, value) in loaded.Knowledge.Facts)
            target.Knowledge.Facts[key] = value;
        target.Knowledge.LastUpdatedMinute = loaded.Knowledge.LastUpdatedMinute;

        target.Seed = loaded.Seed;
        target.RngState = loaded.RngState;
        target.RngCallCount = loaded.RngCallCount;
        target.Turn = loaded.Turn;
        target.EndMinute = loaded.EndMinute;
    }

    private static void Validate(GameStateDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        if (dto.SchemaVersion != PersistenceSchema.CurrentVersion)
            throw new NotSupportedException(
                $"Save schema version {dto.SchemaVersion} is not supported; expected {PersistenceSchema.CurrentVersion}.");
        ArgumentNullException.ThrowIfNull(dto.Clock, "Save has no clock state.");
        ArgumentNullException.ThrowIfNull(dto.Knowledge, "Save has no player knowledge.");
        if (string.IsNullOrWhiteSpace(dto.ScenarioId))
            throw new InvalidOperationException("Save has no scenario id.");
    }

    private static void Replace<T>(Dictionary<string, T> target, Dictionary<string, T> source)
    {
        target.Clear();
        foreach (var (key, value) in source)
            target[key] = value;
    }
}
