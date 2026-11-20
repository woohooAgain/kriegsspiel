# Handoff: T-22 Shell/DI/integration (round 1)

## Task
- ID: `T-22`
- Role: `implementer`
- Branch: `task/T-22-shell-di-integration` (current; no branch switch, no merge)
- Scope/ownership: `Kriegsspiel.Console/Startup/ServiceRegistration.cs`, `Kriegsspiel.Console/Startup/GameLoop.cs`, `Kriegsspiel.Console/Program.cs`, `Kriegsspiel.Console/appsettings.json`, `Kriegsspiel.Console/Kriegsspiel.Console.csproj`, `Kriegsspiel.slnx`, `docs/projectDocs/reports/handoffs/implementer-T-22-r1.md`
- `Kriegsspiel.Core.Tests.csproj` not changed (no package/test change required). `todo.md` untouched.

## Implemented
- **Config pipeline** (`ServiceRegistration.Build`): `SetBasePath(AppContext.BaseDirectory)` → `AddJsonFile("appsettings.json", optional: true)` → `AddCommandLine(args)` (so `--Scenario`, `--Seed`, `--UseLlm`, `--ContentRoot` work). Added `Microsoft.Extensions.Configuration.CommandLine` 11.0.0 and `appsettings.json` copy-to-output in `Kriegsspiel.Console.csproj`.
- **Content/seed**: scenario/friction/reports loaded from configured `ContentRoot`/`Scenario`; `Seed` from config overrides scenario seed (fallback `Scenario.Seed`, then `1`). One seed feeds both `DeterministicRandomSource` (`IRandomSource`) and `System.Random` used by `Arbiter`/commands; `GameState.Seed`/`RngState` initialised from the same source.
- **LLM config**: `UseLlm` + `Llm:SchemaVersion`/`FallbackMode`/`LogPrompts` override the friction profile's `LlmParameters` via a shallow `FrictionTable` copy; the effective friction is what all AI components resolve. `Provider`/`BaseUrl`/`Model`/`TimeoutMs` remain in `appsettings.json` for future wiring (see Frictions).
- **DI registrations**: `IConfiguration`, `ScenarioBundle`, `FrictionTable`, `ReportTemplates`, `ContentLoader`, `IRandomSource`, `Random`, `IGameRules` (inline `ShellGameRules`, `IsCombatEffective(unit) => unit.IsCombatEffective`), `IKnowledgeUpdater`, `IReportFactory`/`ReportFactory`, `ReportDeliveryResolver`, `IAmbiguityInterpreter`, `AiReplayLog`, `ILlmClient`, `IDoctrineOrderFallback`, `TemplateReportNarrator`, `LlmReportNarrator`, `IReportNarrator` (template when off, LLM when on), `LlmOrderCompiler`, all four resolver contracts, `ContactGeometryResolver`, `Arbiter` (8-arg ctor injecting knowledge/report factory), renderers, `JsonSaveStore`, all commands (including `QueryCommand`/`LoadCommand`), `GameLoop`. Factory lambdas used where multiple ctors would be ambiguous (`Arbiter`, `ReportFactory`, `ReportDeliveryResolver`, `LoadCommand`).
- **AI wiring**: `Kriegsspiel.AI` project referenced from Console and added to `Kriegsspiel.slnx`; `Solution Items` now points to `Kriegsspiel.Console/appsettings.json` (pre-existing edit kept and verified). `IReportNarrator` resolved from `Kriegsspiel.Core.Reporting`, `LlmReportNarrator` from `Kriegsspiel.AI` — no files in `Kriegsspiel.Core/Reports/` touched.
- **Menu/loop**: exactly 9 choices — `ОБСТАНОВКА`, `КАРТА`, `ПРИКАЗ`, `ЗАПРОС`, `ЖУРНАЛ`, `ВРЕМЯ`, `СОХРАНИТЬ`, `ЗАГРУЗИТЬ`, `ВЫЙТИ`. `ЗАПРОС` calls `QueryCommand`; `ВРЕМЯ` advances 30 minutes; `ЗАГРУЗИТЬ` uses `LoadCommand.Load()` (latest save) to replace `_state` and clears pending orders; missing save is reported to the user without crashing. Startup line shows the effective LLM state. `Program.cs` unchanged (already resolves `GameLoop` and calls `Run()`).

## Checks
- `dotnet build Kriegsspiel.slnx` — PASS, 0 Warning(s), 0 Error(s).
- `dotnet test Kriegsspiel.slnx --no-build` — PASS (145 passed, 0 failed, 0 skipped).
- `dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj --no-build` (default config) — startup OK: scenario loaded, banner `LLM: выкл`, then Spectre prompt fails in non-TTY (`NotSupportedException: Cannot show selection prompt since the current terminal isn't interactive`).
- Same run with `-- --UseLlm true` — startup OK, banner `LLM: вкл`, identical non-TTY prompt limitation. Both prove the toggle does not break startup.

## Manual smoke (throwaway probe outside repo, `/tmp/opencode/t22-smoke`)
- Resolved every contract listed above from `ServiceRegistration.Build(args)` with real content (`--ContentRoot /mnt/d/Development/Kriegsspiel/Content`); all 26 resolutions succeeded.
- `--UseLlm false` → `IReportNarrator` = `TemplateReportNarrator`, `Llm.Enabled=False`; `--UseLlm true` → `IReportNarrator` = `LlmReportNarrator`, `Llm.Enabled=True`, `LlmOrderCompiler`/`AiReplayLog` resolvable. No startup failure in either mode.
- `SaveCommand.Execute(state)` → `saves/<timestamp>.json`, then `LoadCommand.Load()` → `scenario=test_01 seed=2001 minute=360` (save/load call path working).
- Menu's 9 labels verified by source inspection; interactive selection could not be driven because Spectre requires a TTY (documented above), so per-item command execution was not driven through the live prompt. `QueryCommand`/`LoadCommand` construction and `GameLoop` construction were verified through the DI probe.

## Frictions / notes
- No public concrete `IGameRules` exists in Core (only private nested `DefaultGameRules` in resolvers and test stubs), so a minimal private `ShellGameRules` was added inside `ServiceRegistration` (allowed inline/ad-hoc). If Core later exposes a public rules implementation, replace the inline class in this file.
- `ILlmClient` is registered as `DelegateLlmClient(_ => null)`: no external Ollama server is available in this environment, so the core AI pipelines take their built-in `Fallback`/doctrine-template path (empty response ⇒ fallback). `Provider`/`BaseUrl`/`Model`/`TimeoutMs` are read from appsettings only as future wiring points; a real HTTP client is not introduced in T-22.
- `IReportNarrator` is registered but `Arbiter`'s constructors do not accept a narrator, so narration is not invoked by the shell beyond DI resolution; using it in report generation is outside T-22 ownership.
- `Llm.Enabled` is driven by `UseLlm` (appsettings/command line); friction.json's `llm.enabled` remains unchanged (T-03 content ownership).
- T-23 must still perform the planned renames (`Transmition*` → `Transmission*`, `Kriegsspiel.Core/Reports/` → `Reporting/`, `Assesment` → `Assessment`), clean `Kriegsspiel.Core.csproj` folders, update `readme.md`/`.gitignore`. T-22 deliberately uses the future-proof namespace `Kriegsspiel.Core.Reporting`, so no T-22 file should need edits after those renames.
- No code comments added; no files outside ownership modified.
