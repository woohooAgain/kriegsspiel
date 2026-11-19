using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class ExecutionResolver : IExecutionResolver
{
    public IEnumerable<Report> Start(OrderAccepted e, GameState s, FrictionTable f, Random rng)
    {
        var order = s.Orders.FirstOrDefault(o => o.Id == e.OrderId);
        if (order is null) yield break;
        var unit = s.Units[order.ToUnitId];

        if (string.IsNullOrWhiteSpace(order.TargetZoneId) || order.TargetZoneId == unit.LocationZoneId)
        {
            yield return Status(unit, e.Minute, rng, f);
            yield break;
        }
        if (!s.Zones.TryGetValue(unit.LocationZoneId, out var from)) yield break;
        if (!from.TravelMinutes.TryGetValue(order.TargetZoneId, out var travel)) travel = 30;

        unit.Posture = order.Intent switch
        {
            OrderIntent.Seize or OrderIntent.Move => Posture.March,
            OrderIntent.Hold => Posture.Defend,
            OrderIntent.Withdraw => Posture.Withdraw,
            _ => unit.Posture
        };

        s.Clock.Schedule(new UnitMoved(e.Minute + travel, unit.Id, order.TargetZoneId), e.Minute + travel);
    }

    public IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        if (s.Zones.TryGetValue(unit.LocationZoneId, out var oldZ))
            oldZ.PresentUnitIds.Remove(unit.Id);
        unit.LocationZoneId = e.ToZoneId;
        unit.Fatigue = Math.Min(100, unit.Fatigue + 10);
        if (s.Zones.TryGetValue(e.ToZoneId, out var newZ))
            newZ.PresentUnitIds.Add(unit.Id);

        s.Clock.Schedule(new UnitArrived(e.Minute, unit.Id, e.ToZoneId), e.Minute);
        yield return Status(unit, e.Minute, rng, f);
    }

    public IEnumerable<Report> OnArrive(UnitArrived e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        unit.CurrentOrderId = null;
        yield return Status(unit, e.Minute, rng, f);
    }

    public IEnumerable<Report> GenerateReport(ReportDue e, GameState s, FrictionTable f, Random rng)
    {
        if (!s.Units.TryGetValue(e.UnitId, out var unit)) yield break;
        yield return Status(unit, e.Minute, rng, f);
    }

    private static Report Status(Unit u, int minute, Random rng, FrictionTable f)
    {
        int delay = f.ReportBaseDelayMinutes + rng.Next(0, f.ReportJitterMinutes + 1);
        return new Report
        {
            Minute = minute,
            ArrivesAt = minute + delay,
            FromUnitId = u.Id,
            Kind = ReportKind.Status,
            Confidence = 2,
            Text = $"{u.Name}: в {u.LocationZoneId}, силы ~{u.Strength}%."
        };
    }
}