namespace Kriegsspiel.Core.Domain;

public enum ReportKind { Status, Contact, Request, CombatResult, Arrival }

public sealed class Report
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public int Minute { get; init; }
    public int ArrivesAt { get; init; }
    public required string FromUnitId { get; init; }
    public required string Text { get; init; }
    public int Confidence { get; init; }
    public ReportKind Kind { get; init; }
}