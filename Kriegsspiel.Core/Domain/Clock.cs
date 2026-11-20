namespace Kriegsspiel.Core.Domain;

public enum EventPhase : byte
{
    Delivery = 0,
    Understanding = 1,
    Movement = 2,
    Combat = 3,
    ReportCreate = 4,
    ReportDeliver = 5,
    Scheduled = 6
}

public readonly record struct ScheduledAt(int Minute, EventPhase Phase, long Sequence = 0) : IComparable<ScheduledAt>
{
    public int CompareTo(ScheduledAt other)
    {
        int minuteComparison = Minute.CompareTo(other.Minute);
        if (minuteComparison != 0) return minuteComparison;
        int phaseComparison = ((int)Phase).CompareTo((int)other.Phase);
        return phaseComparison != 0 ? phaseComparison : Sequence.CompareTo(other.Sequence);
    }
}

public sealed class Clock
{
    private readonly PriorityQueue<GameEvent, (ScheduledAt Key, long Order)> _queue = new();
    private long _sequence;
    private long _order;
    private ScheduledAt? _lastProcessedKey;

    public Clock(int startMinute) => Now = startMinute;

    public int Now { get; private set; }

    public int PendingCount => _queue.Count;

    public bool HasPendingEvents => PendingCount > 0;

    public IReadOnlyList<ScheduledAt> PendingKeys =>
        _queue.UnorderedItems.Select(item => item.Item2.Key).OrderBy(key => key).ToArray();

    public void Schedule(GameEvent e, int atMinute) => Schedule(e, atMinute, PhaseFor(e));

    public void Schedule(GameEvent e, int minute, EventPhase phase) =>
        Schedule(e, new ScheduledAt(minute, phase, Interlocked.Increment(ref _sequence)));

    public void Schedule(GameEvent e, ScheduledAt key)
    {
        ArgumentNullException.ThrowIfNull(e);
        ValidateNotPast(key);
        ReserveSequence(key.Sequence);
        long order = Interlocked.Increment(ref _order);
        _queue.Enqueue(e, (key, order));
    }

    public IEnumerable<GameEvent> AdvanceTo(int targetMinute)
    {
        if (targetMinute < Now)
            throw new ArgumentOutOfRangeException(nameof(targetMinute), targetMinute, "Target minute is before Now.");
        return Enumerate(targetMinute);
    }

    private IEnumerable<GameEvent> Enumerate(int targetMinute)
    {
        int startMinute = Now;
        while (_queue.TryPeek(out var e, out var priority) && priority.Key.Minute <= targetMinute)
        {
            _queue.Dequeue();
            Now = priority.Key.Minute;
            _lastProcessedKey = priority.Key;
            yield return e;
        }
        if (targetMinute > startMinute)
        {
            Now = targetMinute;
            if (_lastProcessedKey is null || _lastProcessedKey.Value.Minute != targetMinute)
                _lastProcessedKey = null;
        }
    }

    private void ValidateNotPast(ScheduledAt key)
    {
        if (key.Minute < Now)
            throw new ArgumentOutOfRangeException(nameof(key), key, "Cannot schedule an event in the past.");
        if (key.Minute == Now && _lastProcessedKey is { } last && key.CompareTo(last) <= 0)
            throw new InvalidOperationException("Cannot schedule an event before an already processed key.");
    }

    private void ReserveSequence(long sequence)
    {
        long current = Volatile.Read(ref _sequence);
        while (sequence > current)
        {
            long observed = Interlocked.CompareExchange(ref _sequence, sequence, current);
            if (observed == current) return;
            current = observed;
        }
    }

    private static EventPhase PhaseFor(GameEvent e) => e switch
    {
        OrderDelivered or OrderLost => EventPhase.Delivery,
        OrderAccepted or OrderMisinterpreted => EventPhase.Understanding,
        UnitMoved or UnitArrived => EventPhase.Movement,
        CombatTick => EventPhase.Combat,
        ReportDue => EventPhase.ReportCreate,
        _ => PhaseForName(e.GetType().Name)
    };

    private static EventPhase PhaseForName(string name)
    {
        if (name.Contains("Report", StringComparison.Ordinal))
            return name.Contains("Deliver", StringComparison.Ordinal)
                ? EventPhase.ReportDeliver
                : EventPhase.ReportCreate;
        return EventPhase.Scheduled;
    }
}
