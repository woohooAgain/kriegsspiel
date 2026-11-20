using System.Text.Json;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class GameEventDto
{
    private static readonly Dictionary<string, Type> EventTypes = new(StringComparer.Ordinal)
    {
        [nameof(OrderIssued)] = typeof(OrderIssued),
        [nameof(OrderDelivered)] = typeof(OrderDelivered),
        [nameof(OrderLost)] = typeof(OrderLost),
        [nameof(OrderAccepted)] = typeof(OrderAccepted),
        [nameof(OrderMisinterpreted)] = typeof(OrderMisinterpreted),
        [nameof(UnitMoved)] = typeof(UnitMoved),
        [nameof(UnitArrived)] = typeof(UnitArrived),
        [nameof(CombatTick)] = typeof(CombatTick),
        [nameof(ReportDue)] = typeof(ReportDue),
        [nameof(QuerySent)] = typeof(QuerySent),
        [nameof(QueryAnswered)] = typeof(QueryAnswered),
        [nameof(QuerySilence)] = typeof(QuerySilence),
        [nameof(EdgeDestroyed)] = typeof(EdgeDestroyed),
        [nameof(CamouflageRevealed)] = typeof(CamouflageRevealed),
        [nameof(GuardRelieved)] = typeof(GuardRelieved),
        [nameof(PendingCasualties)] = typeof(PendingCasualties),
        [nameof(ReportDelivered)] = typeof(ReportDelivered),
        [nameof(OrderSuperseded)] = typeof(OrderSuperseded),
        [nameof(ReportLost)] = typeof(ReportLost),
        [nameof(CourierLost)] = typeof(CourierLost),
        [nameof(CourierReturned)] = typeof(CourierReturned),
        [nameof(ZoneControlChanged)] = typeof(ZoneControlChanged),
        [nameof(PoorControlEntered)] = typeof(PoorControlEntered),
        [nameof(PoorControlExited)] = typeof(PoorControlExited)
    };

    public required string Type { get; init; }
    public required int Minute { get; init; }
    public required JsonElement Payload { get; init; }

    public static GameEventDto From(GameEvent e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new GameEventDto
        {
            Type = e.GetType().Name,
            Minute = e.Minute,
            Payload = JsonSerializer.SerializeToElement((object)e, e.GetType(), PersistenceJson.Options)
        };
    }

    public GameEvent ToDomain()
    {
        if (!EventTypes.TryGetValue(Type, out var type))
            throw new NotSupportedException($"Game event type '{Type}' is not supported by the save schema.");

        var e = JsonSerializer.Deserialize(Payload.GetRawText(), type, PersistenceJson.Options) as GameEvent
            ?? throw new InvalidOperationException($"Game event '{Type}' could not be restored.");
        if (e.Minute != Minute)
            throw new InvalidOperationException($"Game event '{Type}' minute mismatch: {e.Minute} vs {Minute}.");
        return e;
    }
}
