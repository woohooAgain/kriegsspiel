using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed record TurnResult(IReadOnlyList<Report> NewReports, int Now);
public sealed record TransmissionResult(int DelayMinutes, bool Lost);
