# Handoff: T-21d Journal renderer (round 1)

## Task
- ID: `T-21d`
- Role: `implementer`
- Branch: `task/T-21d-journal-renderer` (current; no branch switch/merge)
- Scope/ownership: `Kriegsspiel.Console/Commands/JournalRenderer.cs`, `docs/projectDocs/reports/handoffs/implementer-T-21d-r1.md`

## Implemented
- `JournalRenderer.Render(GameState)` API preserved for `JournalCommand`; renders timeline table + separate "Курьеры и очередь" table (Spectre.Console).
- Timeline derived from `EventLog`, `InTransitReports`, `Couriers` and event payloads (player-visible only): semantic minutes for order issue (`OrderIssued`), query/order send, delivery facts (`OrderDelivered`), acceptance/misinterpretation/supersession, report receipt (`ReportDelivered` with event+delivery minutes and payload text when present), query answer (`QueryAnswered` with event/delivery/source/confidence), courier return/loss facts, and generic visible events. Pending/in-transit reports shown with ETA delivery minute. Entries sorted by `(Minute, Phase, Sequence)`.
- Shows planned ETA and actual/fact minutes where available (courier `ExpectedDeliveryMinute`/`ExpectedReturnMinute`, `OutboundDepartureMinute`, event minutes, `deliveryConfirmed`/state).
- Visible courier queue/status table: per-courier status, assignment (order/query), send fact, delivery ETA/fact, return ETA/fact, confirmation label.
- `QuerySilence` rendered explicitly as "Молчание по запросу" with "ответа нет в срок; это не означает отсутствие факта".
- Return-leg loss (`CourierLost` leg `Return`, `deliveryConfirmed=false`): rendered as "Курьер не вернулся (обратный путь)" stating confirmation not received and explicitly not asserting order/message undelivered; never "приказ потерян/не доставлен".
- Outbound loss / `OrderLost`: rendered as "Приказ потерян при доставке"/not delivered; lost orders are not rendered as `OrderDelivered` (only reflected from `EventLog` semantics).

## Checks
- `dotnet build Kriegsspiel.slnx` — PASS (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.slnx --no-build` — PASS (145 passed, 0 failed, 0 skipped).

## Manual scenario (throwaway probe outside repo, `/tmp/opencode/journaltest`)
Constructed `GameState` with `EventLog`/`Couriers`/`DeliveredReports` covering issue+send+delivery+accept, report receipt, `QueryAnswered` (event/delivery/source/confidence), `QuerySilence`, active courier with ETA, outbound-lost courier/order (`OrderLost` + `CourierLost Outbound`), and return-lost courier/order (`OrderDelivered` fact + `CourierLost Return`, `deliveryConfirmed=false`, `LostOnReturn=true`). `JournalRenderer.Render` output verified:
- Timeline shows send facts and ETA rows plus fact minutes (e.g. delivery/return ETAs and facts), report receipt with event+delivery minutes, query answer with 4 fields.
- Courier queue shows statuses/assignments and delivery/return ETA vs fact cells.
- `QuerySilence` present as explicit entry ("не означает отсутствие факта").
- Return-leg loss (C-RET/ORD-RETLOST): delivery fact `OrderDelivered` present, courier shown "не вернулся (обратный путь)… deliveryConfirmed=false; это не означает, что приказ или сообщение не было доставлено"; confirmation "не получено…, доставка могла состояться" — no "приказ потерян/не доставлен".
- Outbound loss (C-LOST/ORD-LOST): shown as "Приказ потерян при доставке"/"Сообщение/приказ не доставлен… потеря на пути туда"; no `OrderDelivered` for that order.

## Frictions / notes
- No persistent "queued, not yet dispatched" courier snapshot in core state; queue visibility is derived from current `state.Couriers` (active/returned/lost) and ETAs/statuses. Dispatch/ETA source of truth remains `Courier`/`CourierPool`; `GetQueue` is internal to pool and not exposed on `GameState`.
- `QueryAnswered`/`QuerySilence` emission depends on core/arbiter producing those events (per existing T-21b/core notes); renderer surfaces them explicitly when present in `EventLog`.
- Courier replacement can remove lost courier instances from `state.Couriers` after replacement time; return-lost context is best-effort from current snapshot plus `CourierLost`/delivery events in log.
- `JournalRenderer` wiring/DI/menu activation outside ownership (T-22). `JournalCommand` API unchanged.
