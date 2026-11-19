using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Arbitration;

public sealed class UnderstandingResolver : IUnderstandingResolver
{
    public IEnumerable<Report> Resolve(OrderDelivered e, GameState s, FrictionTable f, Random rng)
    {
        var order = s.Orders.FirstOrDefault(o => o.Id == e.OrderId);
        if (order is null) yield break;
        var unit = s.Units[order.ToUnitId];

        double fail = order.AmbiguityScore * f.AmbiguityWeight
                    + unit.Traits.Literalism * f.LiteralismWeight
                    + unit.Traits.Caution * f.CautionWeight * 0.5;

        if (rng.NextDouble() >= fail)
        {
            unit.CurrentOrderId = order.Id;
            s.Clock.Schedule(new OrderAccepted(e.Minute, order.Id), e.Minute);
            yield break;
        }

        var type = rng.Next(0, 100) switch
        {
            < 40 => Misinterpretation.ShiftZone,
            < 70 => Misinterpretation.ChangeIntent,
            < 90 => Misinterpretation.DropConstraint,
            _ => Misinterpretation.DelayExecution
        };

        if (type == Misinterpretation.DelayExecution)
        {
            s.Clock.Schedule(new OrderAccepted(e.Minute + 45, order.Id), e.Minute + 45);
            yield return new Report
            {
                Minute = e.Minute,
                ArrivesAt = e.Minute + 30,
                FromUnitId = unit.Id,
                Kind = ReportKind.Request,
                Confidence = 1,
                Text = $"{unit.Name}: уточните задачу, без этого действовать не могу."
            };
        }
        else
        {
            unit.CurrentOrderId = order.Id;
            s.Clock.Schedule(new OrderAccepted(e.Minute, order.Id), e.Minute);
        }
    }
}