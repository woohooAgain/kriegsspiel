using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public interface IExecutionResolver
{
    IEnumerable<Report> Start(OrderAccepted e, GameState s, FrictionTable f, Random rng);
    IEnumerable<Report> OnMove(UnitMoved e, GameState s, FrictionTable f, Random rng);
    IEnumerable<Report> OnArrive(UnitArrived e, GameState s, FrictionTable f, Random rng);
    IEnumerable<Report> GenerateReport(ReportDue e, GameState s, FrictionTable f, Random rng);
}
