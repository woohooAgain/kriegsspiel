# Handoff: T-21a Order constructor CLI (round 1)

## Task
- ID: `T-21a`
- Role: `implementer`
- Branch: `task/T-21a-order-constructor-cli`
- Scope/ownership: `Kriegsspiel.Console/Commands/OrderCommand.cs`, `Kriegsspiel.Console/Commands/OrderBuilder.cs` (new), this handoff

## Implemented
- Replaced `OrderCommand.Build(GameState)` with delegation to new interactive `OrderBuilder` (public signature preserved for `GameLoop`).
- Added `OrderBuilder` with full 7-slot constructor flow:
  - Recipient: player units only (`Faction.Blue`) selected from scenario state.
  - Intent: closed list (`OrderIntent` enum).
  - Target: selectable by intent-appropriate kind (zone/object/unit/edge or none) with player-visible sources only: zones from map metadata, objects from scenario map objects, units limited to own units plus enemy units revealed in `PlayerKnowledge` (reported owners/versions/fact claim values matching known unit ids). No `PresentUnitIds`/hidden truth reads for selection logic.
  - Deadline (`NotLaterThan`), constraints (multi-select, closed `OrderConstraint` enum), report-by (`ReportBy`), movement mode (closed `MovementMode` enum).
- Branches: add/edit flow with preview `ЕСЛИ X → ТО Y [ИНАЧЕ Z]`, copy-from-template support, explicit rejection when attempting to add a 4th branch (`max 3`, loop-guard message). `DropConstraint` action requires naming the dropped constraint.
- Template/reuse: can start from a previous player order in `state.Orders` and reuse/edit slots (recipient/intent/target/constraints/deadlines/mode/branches) before building.
- Ambiguity preview: computes and displays `AmbiguityScorer.Breakdown` (with scoring context from recipient/current zone and known zones) before finalizing; output contains no enemy truth/hidden state.
- Produces valid `Order` for existing flow, populating target fields, constraints, branches, `NotLaterThan`, `ReportBy`, `MovementMode`, and `AmbiguityScore`; `IssuedAt` set from `state.Clock.Now`.

## Checks
- `dotnet build Kriegsspiel.slnx` — **PASS** (0 Warning(s), 0 Error(s)) after fixing missing `using Kriegsspiel.Core.Orders;` in `OrderCommand.cs`.
- `dotnet test Kriegsspiel.slnx --no-build` — **PASS** (145 passed, 0 failed, 0 skipped; Core.Tests).

## Manual scenario (CLI behavior)
- Run console app, choose `ПРИКАЗ`:
  - Optionally reuse previous order as template.
  - Select recipient (own unit), intent, target kind/value, NotLaterThan, constraints from closed list, ReportBy, movement mode.
  - Add up to 3 branches with condition/action/optional else and preview; on attempt to add branch when already 3 present (or after reaching limit) shows explicit rejection/limit message.
  - Ambiguity A breakdown/total shown; order accepted with id and score.
- Verified by code inspection/interactive prompt structure: all 7 slots covered, closed enum lists for intents/constraints/modes/branch conditions/actions, no UI output from `Zone.PresentUnitIds` or hidden enemy state.

## Frictions / ambiguities
- "Player-visible" target objects/edges: map objects and edges are treated as scenario/map metadata (available for targeting) while unit targets are restricted to own + knowledge-revealed enemy units; this avoids hidden-truth leakage but may differ if some objects were intended to be undiscovered (no separate visibility model available within ownership).
- Reserve-zone and some guard-context nuances for ambiguity scoring are limited to available in-scope data (`OrderScoringContext` filled with current/known zones and target object when set); scoring itself remains domain-owned (`AmbiguityScorer`).
- Interactive Spectre.Console flow is not covered by automated UI tests in this task (no test ownership); correctness relies on domain enums/scorer and preserved `OrderCommand.Build` contract.

## Files changed (owned)
- `Kriegsspiel.Console/Commands/OrderCommand.cs`
- `Kriegsspiel.Console/Commands/OrderBuilder.cs` (new)
- `docs/projectDocs/reports/handoffs/implementer-T-21a-r1.md` (this file)
