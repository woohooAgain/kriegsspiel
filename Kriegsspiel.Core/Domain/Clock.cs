namespace Kriegsspiel.Core.Domain;

public sealed class Clock
{
    public int Now { get; private set; }
    private readonly PriorityQueue<GameEvent, int> _queue = new();

    public Clock(int startMinute) => Now = startMinute;

    public void Schedule(GameEvent e, int atMinute)
    {
        if (atMinute < Now) atMinute = Now;
        _queue.Enqueue(e, atMinute);
    }

    public IEnumerable<GameEvent> AdvanceTo(int targetMinute)
    {
        while (_queue.TryPeek(out var e, out var at) && at <= targetMinute)
        {
            _queue.Dequeue();
            Now = at;
            yield return e;
        }
        Now = targetMinute;
    }

    public bool HasPendingEvents => _queue.Count > 0;
}