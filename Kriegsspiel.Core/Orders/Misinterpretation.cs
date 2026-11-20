namespace Kriegsspiel.Core.Orders;

public enum Misinterpretation
{
    None,
    ShiftZone,
    ChangeIntent,
    DropConstraint,
    DelayExecution,
    LiteralExecution,
    BranchError
}
