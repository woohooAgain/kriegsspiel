# Handoff: T-21c Map renderer (round 1)

## Task
- ID: `T-21c`
- Role: `implementer`
- Branch: `task/T-21c-map-renderer` (current; branches/merge not touched)
- Scope/ownership: `Kriegsspiel.Console/Commands/MapRenderer.cs`, `docs/projectDocs/reports/handoffs/implementer-T-21c-r1.md` (this file)

## Implemented
- `MapRenderer.Render(GameState)` keeps its public API and renders a Spectre.Console table; no changes to `MapCommand` or any other file.
- Zone rows are driven by `state.Knowledge.Zones` only. From `state.Zones` the renderer takes only orientation metadata: `Id`, `Name`, `Terrain`. No `Zone.PresentUnitIds`, `state.Units`, `ControllingFaction`, `Concealed`, hidden unit state or other truth fields are read.
- Columns: зона; местность (Russian terrain label); состояние (`нет данных` / `пусто` / `занято` from `KnowledgePresence`); known controller from `ReportedFaction`/`ReportedOwner` (or `неизвестно`, `разные данные` on conflict); событие (`EventMinute`); доставка/обновление (`DeliveryMinute`); уверенность `0/3`–`3/3`; версии.
- Per-zone timestamps and confidence come from `ZoneKnowledge`; every version additionally shows its `FactKey`, value (`пусто` for `EMPTY`), per-claim confidence, event minute and delivery minute. `Conflicting` adds the `противоречие:` marker and the owner column stops claiming a single faction. Values are markup-escaped.
- No code comments added.

## Checks
- `dotnet build Kriegsspiel.slnx` — PASS (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.slnx --no-build` — PASS (145 passed, 0 failed, 0 skipped).
- `grep -n "PresentUnitIds" Kriegsspiel.Console/Commands/MapRenderer.cs` — no matches.

## Manual scenario (throwaway probe outside the repo)
- Constructed `GameState` with zones `A`, `B`, `C`; applied knowledge through the existing `KnowledgeUpdater`, then called `MapRenderer.Render`.
- No observations (`A`): `нет данных`, both timestamps `—`, confidence `0/3`, versions `—`.
- Empty recon report for `B`: `пусто`, event `мин. 100`, delivery `мин. 110`, confidence `3/3`, version `zone:B = пусто` with its own timestamps.
- Two conflicting owner reports for `C` (`Blue`/`Red`, sources `S1`/`S2`): `занято`, owner `разные данные`, zone confidence `1/3`, marker `противоречие:` and both versions with confidence `1/3` each and their individual event/delivery minutes.

## Frictions / notes
- Renderer remains dependent on `KnowledgeUpdater`/`PlayerKnowledge.Apply` having populated `ZoneKnowledge.Versions`, timestamps and confidence; this task did not change core knowledge synchronization.
- `MapRenderer` is not newly wired here: existing `MapCommand` and DI registration already call it; command-menu activation remains in T-22 scope.
- In an 80-column terminal Spectre.Console wraps the wider Russian captions/version cells; content remains complete. No hidden truth is exposed by the wrapping.
