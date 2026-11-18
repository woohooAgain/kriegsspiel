using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class TransmissionResolver : ITransmissionResolver
{
    public TransmissionResult Transmit(Order o, GameState s, FrictionTable f, Random rng)
    {
        var sender = s.Units.Values.FirstOrDefault(u => u.Type == UnitType.HQ)
                  ?? s.Units.Values.First();
        var receiver = s.Units[o.ToUnitId];

        int baseDelay = sender.Type == UnitType.HQ ? f.CourierBaseDelayMinutes : f.FieldCourierDelayMinutes;
        int distance = GraphDistance(s, sender.LocationZoneId, receiver.LocationZoneId);
        int delay = baseDelay + distance / 2 + rng.Next(-5, 6);

        double loss = f.CourierLostBase + f.CourierLostPerKm * distance;
        return new TransmissionResult(Math.Max(5, delay), rng.NextDouble() < loss);
    }

    private static int GraphDistance(GameState s, string from, string to)
    {
        if (from == to) return 0;
        var dist = new Dictionary<string, int> { [from] = 0 };
        var q = new Queue<string>(); q.Enqueue(from);
        while (q.Count > 0)
        {
            var cur = q.Dequeue();
            if (cur == to) return dist[cur];
            if (!s.Zones.TryGetValue(cur, out var z)) continue;
            foreach (var (n, m) in z.TravelMinutes)
                if (!dist.ContainsKey(n)) { dist[n] = dist[cur] + m; q.Enqueue(n); }
        }
        return 999;
    }
}