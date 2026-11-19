namespace Kriegsspiel.Core.Orders;

using Kriegsspiel.Core.Domain;

public enum OrderIntent { Seize, Hold, Move, Recon, Support, Withdraw, Reserve }

public sealed class Order
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public required string ToUnitId { get; init; }
    public OrderIntent Intent { get; init; }
    public string? TargetZoneId { get; init; }
    public int? NotLaterThan { get; init; }
    public List<string> Constraints { get; init; } = new();
    public int? ReportBy { get; init; }
    public int IssuedAt { get; init; }
    public double AmbiguityScore { get; init; }
}