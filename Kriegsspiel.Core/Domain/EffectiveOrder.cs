namespace Kriegsspiel.Core.Domain;

using Kriegsspiel.Core.Orders;

public sealed class EffectiveOrder
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public string SourceOrderId { get; set; } = "";
    public int OrderVersion { get; set; } = 1;
    public string ToUnitId { get; set; } = "";
    public OrderIntent Intent { get; set; }
    public string? TargetZoneId { get; set; }
    public int? Deadline
    {
        get => NotLaterThan;
        set => NotLaterThan = value;
    }
    public string? ForbiddenZoneId { get; set; }
    public string Outcome { get; set; } = "";
    public int? CreatedMinute { get; set; }
    public int? SentMinute { get; set; }
    public int? DeliverAt { get; set; }
    public string? TargetObjectId { get; set; }
    public string? TargetUnitId { get; set; }
    public string? TargetEdgeId { get; set; }
    public int? NotLaterThan { get; set; }
    public int? ReportBy { get; set; }
    public MovementMode MovementMode { get; set; } = global::Kriegsspiel.Core.Orders.MovementMode.March;
    public List<OrderConstraint> Constraints { get; init; } = new();
    public List<OrderBranch> Branches { get; init; } = new();
    public OrderBranch? SelectedBranch { get; set; }
    public double AmbiguityScore { get; set; }
    public int EffectiveAtMinute { get; set; }
    public Misinterpretation Misinterpretation { get; set; } = global::Kriegsspiel.Core.Orders.Misinterpretation.None;
    public bool IsClarification { get; set; }
    public bool PoorControlInducing { get; set; }
    public string? OriginalText { get; init; }
    public string? Note { get; set; }

    public static EffectiveOrder From(Order order) => new()
    {
        SourceOrderId = order.Id,
        OrderVersion = order.OrderVersion,
        ToUnitId = order.ToUnitId,
        Intent = order.Intent,
        TargetZoneId = order.TargetZoneId,
        Deadline = order.Deadline,
        ForbiddenZoneId = order.ForbiddenZoneId,
        Outcome = order.Outcome,
        CreatedMinute = order.CreatedMinute,
        SentMinute = order.SentMinute,
        DeliverAt = order.DeliverAt,
        TargetObjectId = order.TargetObjectId,
        TargetUnitId = order.TargetUnitId,
        TargetEdgeId = order.TargetEdgeId,
        NotLaterThan = order.NotLaterThan,
        ReportBy = order.ReportBy,
        MovementMode = order.MovementMode,
        Constraints = new List<OrderConstraint>(order.Constraints),
        Branches = new List<OrderBranch>(order.Branches),
        AmbiguityScore = order.AmbiguityScore,
        EffectiveAtMinute = order.DeliveredMinute ?? order.IssuedAt,
        OriginalText = order.OriginalText
    };
}
