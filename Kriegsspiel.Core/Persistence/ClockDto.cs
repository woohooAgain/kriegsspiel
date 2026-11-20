using System.Reflection;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class ScheduledAtDto
{
    public int Minute { get; init; }
    public EventPhase Phase { get; init; }
    public long Sequence { get; init; }

    public static ScheduledAtDto From(ScheduledAt key) => new()
    {
        Minute = key.Minute,
        Phase = key.Phase,
        Sequence = key.Sequence
    };

    public ScheduledAt ToDomain() => new(Minute, Phase, Sequence);
}

public sealed class ClockEntryDto
{
    public required ScheduledAtDto Key { get; init; }
    public required GameEventDto Event { get; init; }
    public long Order { get; init; }
}

public sealed class ClockDto
{
    private static readonly FieldInfo QueueField = typeof(Clock).GetField("_queue", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo SequenceField = typeof(Clock).GetField("_sequence", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo OrderField = typeof(Clock).GetField("_order", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo LastProcessedField = typeof(Clock).GetField("_lastProcessedKey", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo NowField = typeof(Clock).GetField("<Now>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public int Now { get; init; }
    public long Sequence { get; init; }
    public long Order { get; init; }
    public ScheduledAtDto? LastProcessedKey { get; init; }
    public List<ClockEntryDto> Entries { get; init; } = new();

    public static ClockDto From(Clock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        var queue = QueueOf(clock);
        var entries = queue.UnorderedItems
            .Select(item => new ClockEntryDto
            {
                Key = ScheduledAtDto.From(item.Priority.Key),
                Event = GameEventDto.From(item.Element),
                Order = item.Priority.Order
            })
            .OrderBy(entry => entry.Order)
            .ToList();

        return new ClockDto
        {
            Now = clock.Now,
            Sequence = (long)SequenceField.GetValue(clock)!,
            Order = (long)OrderField.GetValue(clock)!,
            LastProcessedKey = (ScheduledAt?)LastProcessedField.GetValue(clock) is { } key
                ? ScheduledAtDto.From(key)
                : null,
            Entries = entries
        };
    }

    public Clock ToDomain()
    {
        var clock = new Clock(Now);
        ApplyTo(clock);
        return clock;
    }

    public void ApplyTo(Clock clock)
    {
        ArgumentNullException.ThrowIfNull(clock);
        QueueOf(clock).Clear();
        NowField.SetValue(clock, int.MinValue);
        LastProcessedField.SetValue(clock, null);
        foreach (var entry in Entries)
            clock.Schedule(entry.Event.ToDomain(), entry.Key.ToDomain());

        NowField.SetValue(clock, Now);
        SequenceField.SetValue(clock, Sequence);
        OrderField.SetValue(clock, Order);
        LastProcessedField.SetValue(clock, LastProcessedKey?.ToDomain());
    }

    private static PriorityQueue<GameEvent, (ScheduledAt Key, long Order)> QueueOf(Clock clock) =>
        (PriorityQueue<GameEvent, (ScheduledAt Key, long Order)>)QueueField.GetValue(clock)!;
}
