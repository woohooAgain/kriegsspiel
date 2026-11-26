namespace Kriegsspiel.Core.Domain;

public enum CourierState
{
    Idle = 0,
    Delivering = 1,
    Returning = 2,
    Returned = 3,
    Lost = 4
}

public sealed class Courier
{
    public string Id { get; init; } = Guid.NewGuid().ToString("N")[..8];
    public CourierState State { get; set; } = CourierState.Idle;
    public string? AssignedOrderId { get; set; }
    public string? AssignedQueryId { get; set; }
    public int OutboundDepartureMinute { get; set; }
    public int OutboundDeliveryTime { get; set; }
    public int ReturnTime { get; set; }
    public int? ExpectedDeliveryMinute { get; set; }
    public int? ExpectedReturnMinute { get; set; }
    public int? ReplacementMinute { get; set; }
    public bool DeliveryConfirmed { get; set; }
    public bool LostOnOutbound { get; set; }
    public bool LostOnReturn { get; set; }

    public int? SentMinute { get; set; }
    public int? DeliverAt { get; set; }
    public int? ReturnAt { get; set; }
    public int? LossReturnAt { get; set; }
    public int TLeg { get; set; }
    public int TLegBack { get; set; }

    public bool IsQuery => AssignedQueryId is not null;
    public bool IsAvailable => State == CourierState.Idle;
    public bool HasValidLegs => OutboundDeliveryTime > 0 && ReturnTime > 0;
}
