namespace Kriegsspiel.Core.Orders;

public enum OrderIntent
{
    Seize = 0,
    Hold = 1,
    Move = 2,
    Recon = 3,
    Support = 4,
    Withdraw = 5,
    Reserve = 6,
    Occupy = 7,
    Advance = 8,
    Delay = 9,
    Escort = 10,
    Destroy = 11,
    Block = 12,
    Check = 13,
    Guard = 14
}

public enum MovementMode
{
    March = 0,
    Covert = 1,
    Contact = 2
}

public enum OrderConstraint
{
    NoZoneEntry = 0,
    NoEngagement = 1,
    NoRetreatBeyondZone = 2,
    NoAmmoUse = 3,
    NoWeaponDisplay = 4
}

public enum BranchCondition
{
    TargetHeldByEnemy = 0,
    BattleEnRoute = 1,
    DeadlineExpired = 2,
    BridgeDestroyed = 3,
    ContradictoryOrderReceived = 4,
    CommsLost = 5
}

public enum BranchAction
{
    Attack = 0,
    Bypass = 1,
    Hold = 2,
    Withdraw = 3,
    AlternateTarget = 4,
    RequestClarification = 5,
    ReportImmediately = 6,
    DropConstraint = 7
}

public sealed record OrderBranch(
    BranchCondition Condition,
    BranchAction Then,
    BranchAction? Else = null,
    string? DroppedConstraintName = null);

public sealed class Order
{
    private List<OrderBranch> _branches = new();

    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public required string ToUnitId { get; init; }
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
    public int OrderVersion { get; init; } = 1;
    public MovementMode MovementMode { get; init; } = global::Kriegsspiel.Core.Orders.MovementMode.March;
    public string? OriginalText { get; init; }
    public string? Explanation { get; init; }
    public string? IssuerId { get; init; }
    public List<string> VaguePatterns { get; init; } = new();
    public int? DeliveredMinute { get; set; }
    public bool Lost { get; set; }

    public List<OrderBranch> Branches
    {
        get => _branches;
        init
        {
            if (value.Count > 3)
                throw new ArgumentException("An order cannot have more than three branches.", nameof(value));
            _branches = value;
        }
    }

    public bool AddBranch(OrderBranch branch)
    {
        if (_branches.Count >= 3) return false;
        _branches.Add(branch);
        return true;
    }

    public bool HasValidStructure => !string.IsNullOrWhiteSpace(ToUnitId) && _branches.Count <= 3;
}
