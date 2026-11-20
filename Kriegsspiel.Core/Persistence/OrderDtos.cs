using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Persistence;

public sealed class OrderBranchDto
{
    public BranchCondition Condition { get; init; }
    public BranchAction Then { get; init; }
    public BranchAction? Else { get; init; }
    public string? DroppedConstraintName { get; init; }

    public static OrderBranchDto From(OrderBranch branch) => new()
    {
        Condition = branch.Condition,
        Then = branch.Then,
        Else = branch.Else,
        DroppedConstraintName = branch.DroppedConstraintName
    };

    public OrderBranch ToDomain() => new(Condition, Then, Else, DroppedConstraintName);
}

public sealed class OrderDto
{
    public string Id { get; init; } = "";
    public string ToUnitId { get; init; } = "";
    public OrderIntent Intent { get; init; }
    public string? TargetZoneId { get; init; }
    public string? TargetObjectId { get; init; }
    public string? TargetUnitId { get; init; }
    public string? TargetEdgeId { get; init; }
    public int? NotLaterThan { get; init; }
    public List<OrderConstraint> Constraints { get; init; } = new();
    public int? ReportBy { get; init; }
    public int IssuedAt { get; init; }
    public double AmbiguityScore { get; init; }
    public int OrderVersion { get; init; }
    public MovementMode MovementMode { get; init; }
    public string? OriginalText { get; init; }
    public string? Explanation { get; init; }
    public string? IssuerId { get; init; }
    public List<string> VaguePatterns { get; init; } = new();
    public int? DeliveredMinute { get; init; }
    public bool Lost { get; init; }
    public List<OrderBranchDto> Branches { get; init; } = new();

    public static OrderDto From(Order order) => new()
    {
        Id = order.Id,
        ToUnitId = order.ToUnitId,
        Intent = order.Intent,
        TargetZoneId = order.TargetZoneId,
        TargetObjectId = order.TargetObjectId,
        TargetUnitId = order.TargetUnitId,
        TargetEdgeId = order.TargetEdgeId,
        NotLaterThan = order.NotLaterThan,
        Constraints = new List<OrderConstraint>(order.Constraints),
        ReportBy = order.ReportBy,
        IssuedAt = order.IssuedAt,
        AmbiguityScore = order.AmbiguityScore,
        OrderVersion = order.OrderVersion,
        MovementMode = order.MovementMode,
        OriginalText = order.OriginalText,
        Explanation = order.Explanation,
        IssuerId = order.IssuerId,
        VaguePatterns = new List<string>(order.VaguePatterns),
        DeliveredMinute = order.DeliveredMinute,
        Lost = order.Lost,
        Branches = order.Branches.Select(OrderBranchDto.From).ToList()
    };

    public Order ToDomain() => new()
    {
        Id = Id,
        ToUnitId = ToUnitId,
        Intent = Intent,
        TargetZoneId = TargetZoneId,
        TargetObjectId = TargetObjectId,
        TargetUnitId = TargetUnitId,
        TargetEdgeId = TargetEdgeId,
        NotLaterThan = NotLaterThan,
        Constraints = new List<OrderConstraint>(Constraints),
        ReportBy = ReportBy,
        IssuedAt = IssuedAt,
        AmbiguityScore = AmbiguityScore,
        OrderVersion = OrderVersion,
        MovementMode = MovementMode,
        OriginalText = OriginalText,
        Explanation = Explanation,
        IssuerId = IssuerId,
        VaguePatterns = new List<string>(VaguePatterns),
        DeliveredMinute = DeliveredMinute,
        Lost = Lost,
        Branches = Branches.Select(b => b.ToDomain()).ToList()
    };
}

public sealed class EffectiveOrderDto
{
    public string Id { get; init; } = "";
    public string SourceOrderId { get; init; } = "";
    public int OrderVersion { get; init; }
    public string ToUnitId { get; init; } = "";
    public OrderIntent Intent { get; init; }
    public string? TargetZoneId { get; init; }
    public string? TargetObjectId { get; init; }
    public string? TargetUnitId { get; init; }
    public string? TargetEdgeId { get; init; }
    public int? NotLaterThan { get; init; }
    public int? ReportBy { get; init; }
    public MovementMode MovementMode { get; init; }
    public List<OrderConstraint> Constraints { get; init; } = new();
    public List<OrderBranchDto> Branches { get; init; } = new();
    public OrderBranchDto? SelectedBranch { get; init; }
    public double AmbiguityScore { get; init; }
    public int EffectiveAtMinute { get; init; }
    public Misinterpretation Misinterpretation { get; init; }
    public bool IsClarification { get; init; }
    public bool PoorControlInducing { get; init; }
    public string? OriginalText { get; init; }
    public string? Note { get; init; }

    public static EffectiveOrderDto From(EffectiveOrder order) => new()
    {
        Id = order.Id,
        SourceOrderId = order.SourceOrderId,
        OrderVersion = order.OrderVersion,
        ToUnitId = order.ToUnitId,
        Intent = order.Intent,
        TargetZoneId = order.TargetZoneId,
        TargetObjectId = order.TargetObjectId,
        TargetUnitId = order.TargetUnitId,
        TargetEdgeId = order.TargetEdgeId,
        NotLaterThan = order.NotLaterThan,
        ReportBy = order.ReportBy,
        MovementMode = order.MovementMode,
        Constraints = new List<OrderConstraint>(order.Constraints),
        Branches = order.Branches.Select(OrderBranchDto.From).ToList(),
        SelectedBranch = order.SelectedBranch is { } selected ? OrderBranchDto.From(selected) : null,
        AmbiguityScore = order.AmbiguityScore,
        EffectiveAtMinute = order.EffectiveAtMinute,
        Misinterpretation = order.Misinterpretation,
        IsClarification = order.IsClarification,
        PoorControlInducing = order.PoorControlInducing,
        OriginalText = order.OriginalText,
        Note = order.Note
    };

    public EffectiveOrder ToDomain() => new()
    {
        Id = Id,
        SourceOrderId = SourceOrderId,
        OrderVersion = OrderVersion,
        ToUnitId = ToUnitId,
        Intent = Intent,
        TargetZoneId = TargetZoneId,
        TargetObjectId = TargetObjectId,
        TargetUnitId = TargetUnitId,
        TargetEdgeId = TargetEdgeId,
        NotLaterThan = NotLaterThan,
        ReportBy = ReportBy,
        MovementMode = MovementMode,
        Constraints = new List<OrderConstraint>(Constraints),
        Branches = Branches.Select(b => b.ToDomain()).ToList(),
        SelectedBranch = SelectedBranch?.ToDomain(),
        AmbiguityScore = AmbiguityScore,
        EffectiveAtMinute = EffectiveAtMinute,
        Misinterpretation = Misinterpretation,
        IsClarification = IsClarification,
        PoorControlInducing = PoorControlInducing,
        OriginalText = OriginalText,
        Note = Note
    };
}
