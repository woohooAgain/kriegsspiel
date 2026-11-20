# Handoff: T-21e Situation and report renderer (round 1)

## Task
- ID: `T-21e`
- Role: `implementer`
- Branch: `task/T-21e-situation-report-renderer` (current; no branch switch/merge)
- Scope/ownership: `Kriegsspiel.Console/Commands/SituationCommand.cs`, `Kriegsspiel.Console/Commands/ReportRenderer.cs`, `docs/projectDocs/reports/handoffs/implementer-T-21e-r1.md`

## Implemented
- `ReportRenderer.Render(GameState)` signature preserved; now renders every delivered report with all four mandatory fields (event minute, delivery minute, source, confidence 0–3) plus kind, text/facts, fact key and age (`min с доставки` / `мин с события`) relative to `state.Clock.Now`. `ReportedBy` override shown as `через …` (rules §5 `ReportBy`). All user-provided strings escaped with `Markup.Escape`.
- Visual distinction in the kind cell: `Impossible` → bold red `НЕВОЗМОЖНО`, `QueryAnswer` → bold cyan `Ответ на запрос`, `Silence` → bold yellow `Молчание` (with explicit "нет ответа в срок — это не «факта нет»"), `Contradiction` → bold magenta `Противоречие`. Other kinds get Russian labels.
- Confidence rendered with color gradation (3 green, 2 yellow, 1 grey, 0 red); facts rendered as `key=value` lines; reports sorted by delivery then event minute.
- New public helpers: `RenderReports(...)`, `RenderClaims(...)`, `IsConflicting(...)`, `KindLabel`, `ConfidenceCell`, `AgeCell`, `FormatMinute`, `ClaimValueCell`. `SituationCommand(ReportRenderer)` primary constructor and `ICommand.Execute(GameState)` unchanged — DI (`ServiceRegistration`) and `GameLoop` need no changes.
- `SituationCommand.Execute` now renders: header with current time, knowledge summary table (zone presence `пусто`/`занято`/`нет данных`, reported owner/faction, confidence, all versions with both timestamps), contradiction section from `Knowledge.Facts` grouped by `fact key` (all conflicting versions, reduced confidence, note that truth is not revealed), the full report table, and a compact signal summary for `Impossible`/`QueryAnswer`/`Silence`/`Contradiction`.
- Situation reads only `state.Knowledge`, `state.DeliveredReports`, `state.Clock.Now` and static map metadata (`Zone.Id`/`Zone.Name`). No read of `Zone.PresentUnitIds`, `ControllingFaction` or hidden unit/zone state.

## Checks
- `dotnet build Kriegsspiel.slnx` — PASS (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.slnx --no-build` — PASS (145 passed, 0 failed, 0 skipped).

## Manual scenario (throwaway probe outside repo, `/tmp/opencode/situation-probe`)
Constructed `GameState` (`Clock.Now=180`, zones A/B/C) and applied reports through `KnowledgeUpdater`: B = empty recon claim, C = conflicting `Blue`/`Red` claims from two sources, plus delivered `Impossible` (with `ReportedBy=XO`), `QueryAnswer`, `Silence`, `Contradiction` reports. Hidden truth sentinel `Zone.C.PresentUnitIds=["ULTRA_SECRET_777"]`, `ControllingFaction=Neutral`. `SituationCommand.Execute(new ReportRenderer())` output verified:
- Report table shows event+delivery minutes (absolute + clock), source `S3 через XO`, confidence `2/3`, type, text/facts, and age (e.g. `65 мин с доставки / 80 мин с события`).
- Zone B shown as `пусто` (with source/timestamps), zone A shown as `нет данных` (`Наблюдений и донесений не было`).
- Zone C shown as `занято` / `разные данные` with both `Blue` and `Red` versions; contradiction table shows both versions with confidence reduced to `1/3`.
- `НЕВОЗМОЖНО`, `Ответ на запрос`, `Молчание`, `Противоречие` styled distinctly; signal summary lists all four kinds.
- Hidden sentinel `ULTRA_SECRET_777` and `нейтральные` absent from output — no hidden truth leak.

## Frictions / notes
- `IReportFactory`, `IReportNarrator` and `ReportTemplates` are creation-time concerns (delivered reports already carry narrated `Text`, all four fields and `Facts`) and are not registered in `ServiceRegistration` (T-22 owns DI). Adding them as required dependencies would break current DI and exceed ownership, so the renderer works from `state.DeliveredReports`/`state.Knowledge` as explicitly allowed by the task.
- `IReportNarrator.cs` still lives in `Kriegsspiel.Core/Reports/` with namespace `Kriegsspiel.Core.Reporting`; no using/reference added to avoid coupling to the pre-rename path (T-23 owns the rename).
- Console menu uses `Spectre.Console.SelectionPrompt`, which throws `NotSupportedException` with piped/non-TTY input. DI/startup itself succeeds (scenario loaded, `GameLoop` resolved); interactive CLI verification was replaced by the throwaway harness above.
- No code comments added; no files outside ownership modified; `todo.md` untouched.
