# Отчёт задачи T-22 — Shell/DI/integration

- **T-XX:** `T-22`
- **Волна / ветка волны:** Wave 3 / `wave/3-cli-ui-cleanup`
- **Ветка задачи / MR:** `task/T-22-shell-di-integration` (`b81b313`), merge `ed9fb35` → ветка волны
- **Раунды:** 1 (`docs/projectDocs/reports/handoffs/implementer-T-22-r1.md`)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент, 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- DI: зарегистрированы сервисы/контракты/резолверы, persistence, AI-компоненты, narrator/factory/knowledge/query; конфиг `SetBasePath`+`AddJsonFile`+`AddCommandLine`, чтение `UseLlm`/`Llm`/`Seed`/`ContentRoot`/`Scenario`. `IGameRules` реализован inline (`ShellGameRules`), `ILlmClient` — stub с fallback (core/AI fallback-путь сохраняется), `IReportNarrator` выбирается по `UseLlm`, `Arbiter` резолвится с knowledge/report factory.
- Меню/цикл: 9 действий — `ОБСТАНОВКА, КАРТА, ПРИКАЗ, ЗАПРОС, ЖУРНАЛ, ВРЕМЯ, СОХРАНИТЬ, ЗАГРУЗИТЬ, ВЫЙТИ`; подключены `QueryCommand`/`LoadCommand`; `ВРЕМЯ` +30 мин, загрузка заменяет состояние и чистит pending; LLM on/off не ломает запуск.
- `appsettings.json`: секции `UseLlm`/`Llm` и пути контента/сценария/Seed актуализированы (JSON контента T-03 не менялся).
- Проекты/решение: `Kriegsspiel.Console.csproj` — ref на `Kriegsspiel.AI`, `Configuration.CommandLine`, copy `appsettings.json`; `Kriegsspiel.slnx` — добавлен `Kriegsspiel.AI`, Solution Item → `Kriegsspiel.Console/appsettings.json`. `Kriegsspiel.Core.Tests.csproj` не менялся (не потребовалось).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Startup/ServiceRegistration.cs`
- `Kriegsspiel.Console/Startup/GameLoop.cs`
- `Kriegsspiel.Console/appsettings.json`
- `Kriegsspiel.Console/Kriegsspiel.Console.csproj`
- `Kriegsspiel.slnx`
- `docs/projectDocs/reports/handoffs/implementer-T-22-r1.md` (handoff)

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` → ok (0 warnings / 0 errors).
- `dotnet test Kriegsspiel.slnx --no-build` → ok (Passed 145, Failed 0).
- Smoke (non-TTY): `dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj --no-build` стартует, показывает сценарий и `LLM: выкл`, затем останавливается на Spectre `SelectionPrompt` (`NotSupportedException: current terminal isn't interactive`) — подтверждает успешный старт/DI/загрузку контента и баннер до интерактивного меню. Меню содержит 9 пунктов (проверено по коду). Toggle `UseLlm` не ломает старт (handoff).
- Отдельный DI probe вне репо (handoff): резолв 26+ контрактов, save/load путь, выбор narrator по `UseLlm`.

## Верификация (verifier)
- Вердикт: **verified**
- Замечания/раунды (дельта): нет.
- Merge в ветку волны: выполнен (`ed9fb35`).
- Post-merge в контексте ветки волны: сборка ok, тесты 145/145 ok.

## Процесс (трение)
- Неоднозначности: публичной реализации `IGameRules` в Core нет — добавлена минимальная inline-реализация в DI; реальный HTTP LLM-клиент не подключён (stub+fallback), внешнего сервера в окружении нет.
- Лишние поиски: отсутствовали.
- Оценка доп. затрат: низко/средне (разбор DI/ctor-ов AI и narrator).
- Предложения по улучшению: вынести публичный `IGameRules` в Core и реальный `ILlmClient` (HTTP) отдельными задачами; автоматизировать smoke shell в CI/TTY.

## Заметки / tech-debt
- T-23 продолжает: ренеймы `Transmition*`→`Transmission*`, `Core/Reports/`→`Reporting/`, `Assesment`→`Assessment`, чистка `Core.csproj`, `readme.md`/`.gitignore`. Namespace `Kriegsspiel.Core.Reporting` уже используется — правки T-22 файлов после ренейма папки не требуются (handoff).
- Наблюдение: `Llm.BaseUrl/Model` пока не потребляются stub-клиентом (ожидаемо до появления HTTP-клиента).
