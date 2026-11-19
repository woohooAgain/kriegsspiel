using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class Arbiter
{
    private readonly FrictionTable _f;
    private readonly Random _rng;
    private readonly ITransmissionResolver _transmission;
    private readonly IUnderstandingResolver _understanding;
    private readonly IExecutionResolver _execution;
    private readonly ICombatResolver _combat;

    public Arbiter(FrictionTable f, Random rng,
                   ITransmissionResolver t, IUnderstandingResolver u,
                   IExecutionResolver ex, ICombatResolver c)
    { _f = f; _rng = rng; _transmission = t; _understanding = u; _execution = ex; _combat = c; }

    public TurnResult AdvanceTo(int target, GameState state, IReadOnlyList<Orders.Order> newOrders)
    {
        foreach (var o in newOrders)
        {
            state.Orders.Add(o);
            var r = _transmission.Transmit(o, state, _f, _rng);
            int at = state.Clock.Now + r.DelayMinutes;
            state.Clock.Schedule(r.Lost ? new OrderLost(at, o.Id) : new OrderDelivered(at, o.Id), at);
        }

        var produced = new List<Report>();
        foreach (var evt in state.Clock.AdvanceTo(target))
        {
            state.EventLog.Add(evt);
            produced.AddRange(Dispatch(evt, state));
        }

        var arrived = produced.Where(r => r.ArrivesAt <= target).ToList();
        state.DeliveredReports.AddRange(arrived);
        return new TurnResult(arrived, state.Clock.Now);
    }

    private IEnumerable<Report> Dispatch(GameEvent e, GameState s) => e switch
    {
        OrderDelivered d => _understanding.Resolve(d, s, _f, _rng),
        OrderAccepted a => _execution.Start(a, s, _f, _rng),
        UnitMoved m => _execution.OnMove(m, s, _f, _rng),
        UnitArrived ar => _execution.OnArrive(ar, s, _f, _rng),
        ReportDue r => _execution.GenerateReport(r, s, _f, _rng),
        CombatTick c => _combat.Resolve(c, s, _f, _rng),
        _ => []
    };
}