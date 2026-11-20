# Handoff: T-21b Query CLI (round 1)

## Task
- ID: `T-21b`
- Role: `implementer`
- Branch: `task/T-21b-query-cli` (current; no branch changes)
- Scope/ownership: `Kriegsspiel.Console/Commands/QueryCommand.cs` (new), `docs/projectDocs/reports/handoffs/implementer-T-21b-r1.md` (this file)

## Implemented
- Added `QueryCommand : ICommand` (Spectre.Console, existing command style) with ctor deps `(ITransmissionResolver transmission, FrictionTable friction, Random rng)` — all already registered/available in `ServiceRegistration`.
- Player-visible flow: selects requester/target from `Faction.Blue` units only (HQ preferred as requester) and query type from closed UI options; no hidden enemy truth or `Zone.PresentUnitIds` reads.
- Send path: generates deterministic-enough id `Q-{state.Clock.Now}-{seq}` and calls `ITransmissionResolver.TransmitQuery(...)` so dispatch uses the same courier mechanism/queue and priority/ETA rules as core (`CourierPool`, query-priority queue, outbound/return/loss timing). Displays `QuerySent` (minute, requester/target, type) and dispatch ETA/return/loss info when available, or queued/non-blocking message when not yet dispatched.
- Non-blocking: `Execute` returns promptly after creating/scheduling send and rendering status; answers are expected on later turns via normal turn processing/events/reports in state.
- Status/visibility (from `state.EventLog` and `state.DeliveredReports` only): renders `QuerySent`, `QueryAnswered` (event minute, delivery minute `Report.DeliveryMinute`, source, confidence 0–3 + text when present) and `QuerySilence` events; also lists delivered `ReportKind.QueryAnswer` reports with the 4 mandatory fields. Explicitly states `QuerySilence`/no-answer means «нет ответа», not «факта нет». If silence window (`friction.Reports.QuerySilenceMinutes`, default 60) elapsed without answer/silence event in log, UI shows an "ожидание истекло / нет ответа" status note (derived, non-fabricating) with the same semantics; actual `QuerySilence` event emission remains simulation/core responsibility.
- No comments added. `TreatWarningsAsErrors=true` respected.

## Checks
- `dotnet build Kriegsspiel.slnx` — **PASS** (0 Warning(s), 0 Error(s), net10.0; Core/Tests/Console built).
- `dotnet test Kriegsspiel.slnx --no-build` — **PASS** (145 passed, 0 failed, 0 skipped, `Kriegsspiel.Core.Tests`, ~345 ms).

## Manual CLI scenario (by implementation/contract inspection)
- Run game, choose `Query` action (once command is registered/invocable in T-22 integration): pick target and type -> `QuerySent` logged/scheduled via resolver and shown immediately with courier ETA/queue note; command returns (non-blocking).
- Advance time/turns through normal loop: when `QueryAnswered` event/report is delivered into `EventLog`/`DeliveredReports`, status shows answer with event minute, delivery minute, source, confidence (and text); when `QuerySilence` occurs, status shows it and explicitly "нет ответа, не «факта нет»". No hidden enemy truth is read/displayed.

## Frictions / notes
- `QueryCommand` is not registered in `ServiceRegistration` and no menu/dispatch wiring exists within owned scope; **T-22 registration/integration requirement**: add DI registration and wire command into `GameLoop`/UI command selection (and any constructor-arg plumbing) so it becomes invocable.
- Automatic `QueryAnswered`/`QuerySilence` generation/scheduling logic (answer content, who can answer, silence timing) is not present in core paths reviewed within ownership; command correctly surfaces whatever query events/reports the simulation produces. If scenarios expect full query resolution, core/arbiter/report-factory work may be needed outside T-21b scope.
- Silence-window expiry is surfaced as a UI status indicator when no event exists yet, to avoid implying "факта нет"; final authoritative silence remains `QuerySilence` event in log when produced by simulation.

## Files changed (owned)
- `Kriegsspiel.Console/Commands/QueryCommand.cs` (new)
- `docs/projectDocs/reports/handoffs/implementer-T-21b-r1.md` (this file)
