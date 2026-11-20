namespace Kriegsspiel.Core.Domain;

using Kriegsspiel.Core.Orders;

public abstract record GameEvent(int Minute);
public sealed record OrderIssued(int Minute, string OrderId) : GameEvent(Minute);
public sealed record OrderDelivered(int Minute, string OrderId) : GameEvent(Minute);
public sealed record OrderLost(int Minute, string OrderId) : GameEvent(Minute);
public sealed record OrderAccepted(int Minute, string OrderId) : GameEvent(Minute);
public sealed record OrderMisinterpreted(int Minute, string OrderId, Misinterpretation Type) : GameEvent(Minute);
public sealed record UnitMoved(int Minute, string UnitId, string ToZoneId) : GameEvent(Minute);
public sealed record UnitArrived(int Minute, string UnitId, string ZoneId) : GameEvent(Minute);
public sealed record CombatTick(int Minute, string ZoneId) : GameEvent(Minute);
public sealed record ReportDue(int Minute, string UnitId, ReportKind Kind) : GameEvent(Minute);

public sealed record QuerySent(int Minute, string RequesterId, string TargetId) : GameEvent(Minute);
public sealed record QueryAnswered(int Minute, string SourceId, Report Report) : GameEvent(Minute);
public sealed record QuerySilence(int Minute, string TargetId, int Confidence = 1) : GameEvent(Minute);
public sealed record EdgeDestroyed(int Minute, string EdgeId, string? ZoneId = null) : GameEvent(Minute);
public sealed record CamouflageRevealed(int Minute, string UnitId, string Reason) : GameEvent(Minute);
public sealed record GuardRelieved(int Minute, string OldUnitId, string NewUnitId) : GameEvent(Minute);
public sealed record PendingCasualties(int Minute, string UnitId, int Amount) : GameEvent(Minute);
public sealed record ReportDelivered(int Minute, string ReportId, string? UnitId = null, Report? Payload = null) : GameEvent(Minute);
public sealed record OrderSuperseded(int Minute, string OldOrderId, string NewOrderId) : GameEvent(Minute);
public sealed record ReportLost(int Minute, string ReportId) : GameEvent(Minute);
public sealed record CourierLost(int Minute, string CourierId, string Leg) : GameEvent(Minute);
public sealed record CourierReturned(int Minute, string CourierId) : GameEvent(Minute);
public sealed record ZoneControlChanged(int Minute, string ZoneId, Faction? ControllingFaction) : GameEvent(Minute);
public sealed record PoorControlEntered(int Minute, string UnitId, string Reason) : GameEvent(Minute);
public sealed record PoorControlExited(int Minute, string UnitId) : GameEvent(Minute);
