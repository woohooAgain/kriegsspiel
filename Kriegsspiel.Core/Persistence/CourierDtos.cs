using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class CourierDto
{
    public string Id { get; init; } = "";
    public CourierState State { get; init; }
    public string? AssignedOrderId { get; init; }
    public string? AssignedQueryId { get; init; }
    public int OutboundDepartureMinute { get; init; }
    public int OutboundDeliveryTime { get; init; }
    public int ReturnTime { get; init; }
    public int? ExpectedDeliveryMinute { get; init; }
    public int? ExpectedReturnMinute { get; init; }
    public int? ReplacementMinute { get; init; }
    public bool DeliveryConfirmed { get; init; }
    public bool LostOnOutbound { get; init; }
    public bool LostOnReturn { get; init; }

    public static CourierDto From(Courier courier) => new()
    {
        Id = courier.Id,
        State = courier.State,
        AssignedOrderId = courier.AssignedOrderId,
        AssignedQueryId = courier.AssignedQueryId,
        OutboundDepartureMinute = courier.OutboundDepartureMinute,
        OutboundDeliveryTime = courier.OutboundDeliveryTime,
        ReturnTime = courier.ReturnTime,
        ExpectedDeliveryMinute = courier.ExpectedDeliveryMinute,
        ExpectedReturnMinute = courier.ExpectedReturnMinute,
        ReplacementMinute = courier.ReplacementMinute,
        DeliveryConfirmed = courier.DeliveryConfirmed,
        LostOnOutbound = courier.LostOnOutbound,
        LostOnReturn = courier.LostOnReturn
    };

    public Courier ToDomain() => new()
    {
        Id = Id,
        State = State,
        AssignedOrderId = AssignedOrderId,
        AssignedQueryId = AssignedQueryId,
        OutboundDepartureMinute = OutboundDepartureMinute,
        OutboundDeliveryTime = OutboundDeliveryTime,
        ReturnTime = ReturnTime,
        ExpectedDeliveryMinute = ExpectedDeliveryMinute,
        ExpectedReturnMinute = ExpectedReturnMinute,
        ReplacementMinute = ReplacementMinute,
        DeliveryConfirmed = DeliveryConfirmed,
        LostOnOutbound = LostOnOutbound,
        LostOnReturn = LostOnReturn
    };
}

public sealed class PendingCasualtyDto
{
    public int Minute { get; init; }
    public string UnitId { get; init; } = "";
    public string? ZoneId { get; init; }
    public int? CombatTickMinute { get; init; }
    public bool Applied { get; init; }
    public int Amount { get; init; }

    public static PendingCasualtyDto From(PendingCasualty casualty) => new()
    {
        Minute = casualty.Minute,
        UnitId = casualty.UnitId,
        ZoneId = casualty.ZoneId,
        CombatTickMinute = casualty.CombatTickMinute,
        Applied = casualty.Applied,
        Amount = casualty.Amount
    };

    public PendingCasualty ToDomain() => new()
    {
        Minute = Minute,
        UnitId = UnitId,
        ZoneId = ZoneId,
        CombatTickMinute = CombatTickMinute,
        Applied = Applied,
        Amount = Amount
    };
}
