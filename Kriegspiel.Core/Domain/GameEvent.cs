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