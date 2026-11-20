# Отчёт волны Wave 3 — CLI/UI, интеграция, уборка

- **Дата:** 2026-11-20
- **Ветка волны / target:** `wave/3-cli-ui-cleanup` → `master`
- **Сценарий проверок волны (architect):** `docs/projectDocs/todo.md` → «Сценарии проверок волн (детально, для verifier)» → Wave 3. Общие: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`. Механики: дублировать `Как проверить`/ручные сценарии `T-21a`–`T-21e`, `T-22`; `T-23` — сборка/тесты/smoke + проверки переименований/доков (без новой игровой механики).
- **Финализировал и закоммитил (verifier, после проверок):** агент, 2026-11-20

## Задачи волны
| T-* | Статус | Ветка/MR (refs) | Вердикт | Отчёт задачи | Примечание |
|-----|--------|------------------|---------|--------------|------------|
| `T-21a` | verified, merged | `task/T-21a-order-constructor-cli` (`0ca8599`), merge `ea1a4b4` | verified | `docs/projectDocs/reports/task-T-21a-order-constructor-cli.md` | конструктор 7 слотов, ветви ≤3, preview A |
| `T-21b` | verified, merged | `task/T-21b-query-cli` (`c2d3cff`), merge `3a50f6d` | verified | `docs/projectDocs/reports/task-T-21b-query-cli.md` | `QuerySent/Answered/Silence`, курьерный механизм |
| `T-21c` | verified, merged | `task/T-21c-map-renderer` (`90a7794`), merge `3674541` | verified | `docs/projectDocs/reports/task-T-21c-map-renderer.md` | только `PlayerKnowledge`, no-data/empty/конфликты |
| `T-21d` | verified, merged | `task/T-21d-journal-renderer` (`22de208`), merge `499d0fc` | verified | `docs/projectDocs/reports/task-T-21d-journal-renderer.md` | ETA/факты, очередь, семантика потерь/`QuerySilence` |
| `T-21e` | verified, merged | `task/T-21e-situation-report-renderer` (`f10f999`), merge `2ca6d77` | verified | `docs/projectDocs/reports/task-T-21e-situation-report-renderer.md` | 4 поля+возраст, противоречия, без hidden truth |
| `T-22` | verified, merged | `task/T-22-shell-di-integration` (`b81b313`), merge `ed9fb35` | verified | `docs/projectDocs/reports/task-T-22-shell-di-integration.md` | меню 9 действий, DI/AI/persistence, slnx/csproj/config |
| `T-23` | verified, merged | `task/T-23-final-cleanup-docs` (`135e3aa`), merge `f4158c8` | verified | `docs/projectDocs/reports/task-T-23-final-cleanup-docs.md` | ренеймы, `Core.csproj`, `readme.md`, `.gitignore` |

## Параллелизм и ownership
- `T-21a`–`T-21e`, `T-22` выполнены по непересекающимся наборам файлов (команды/рендеры, shell/DI/proj/slnx). `T-23` — финальная задача после завершения остальных (ренеймы/`Core.csproj`/docs), без пересечений с изменёнными в Wave 3 файлами вне её ownership. Пересечений ownership в пределах выполнения не зафиксировано (см. handoff-артефакты и task-отчёты).

## Post-merge проверки по задачам (в контексте ветки волны)
- `T-21a`–`T-22`: после каждого merge в `wave/3-cli-ui-cleanup` — `dotnet build Kriegsspiel.slnx` ok, `dotnet test Kriegsspiel.slnx --no-build` ok (145/145). Подробности/ручные сценарии — refs в task-отчётах и `docs/projectDocs/reports/handoffs/implementer-T-21*-r1.md`, `implementer-T-22-r1.md`.
- `T-23`: merge `f4158c8`, `dotnet build Kriegsspiel.slnx` ok, `dotnet test Kriegsspiel.slnx` ok (145/145). Эскалаций к `architect` не потребовалось.

## Wave-проверка (по сценарию architect, на ветке волны)
- **Общие проверки работоспособности:** `dotnet build Kriegsspiel.slnx` → ok (0 warnings/0 errors); `dotnet test Kriegsspiel.slnx` → ok (Passed 145, Failed 0).
- **Добавленные/изменённые механики (`T-21a`–`T-22`):** подтверждены сборкой/тестами и ручными сценариями в handoff/task-отчётах (7 слотов/ветви/preview A; запросы и курьерная семантика; карта/журнал/обстановка на знании и доставленных донесениях с обязательными полями и корректной семантикой потерь/молчания/противоречий; меню из 9 действий, save/load, DI/AI fallback, `UseLlm` не ломает старт). Smoke: `dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj --no-build` — успешный старт/загрузка сценария/баннер (`LLM: выкл`), далее останов на Spectre `SelectionPrompt` в non-TTY (`NotSupportedException`) — ограничение окружения, не дефект логики; интерактивная навигация меню требует TTY.
- **`T-23` (механики не добавлялись/не менялись):** сборка/тесты/smoke ok; старые имена/папки `Transmition*`, `Kriegsspiel.Core/Reports`, `Assesment` отсутствуют; `Kriegsspiel.Core.csproj` без устаревших записей; `readme.md`/`.gitignore` актуальны; `AGENTS.md` и правила не изменены.
- **Сборка / автотесты / запуск/сценарий:** build ok, tests ok, smoke/start ok (TTY-ограничение для интерактива зафиксировано).
- **Результат:** ok — корректирующие задачи не требуются.

## Merge ветки волны в основную
- Статус: выполнен (merge `wave/3-cli-ui-cleanup` → `master` после фиксации отчётов волны/задач). Пост-merge проверки на `master`: `dotnet build Kriegsspiel.slnx` ok, `dotnet test Kriegsspiel.slnx --no-build` ok (145/145).

## Ретро процесса (вклад process-master / факты участников)
- **Проблемы и фрикции:** ручные CLI-сценарии частично неавтоматизируемы и ограничены non-TTY (Spectre). Для `T-22` потребовались inline-реализация `IGameRules` и stub `ILlmClient` (нет публичной реализации/внешнего сервера) — осознанные компромиссы в рамках ownership. Исторические упоминания старых имён в документах/старых отчётах сохранены (обратной силы нет).
- **Принятые/предложенные меры:** file-first handoff + per-task отчёты, per-task merge/проверки в ветку волны, smoke/start как доказательство интеграции; предложить вынести публичный `IGameRules` и HTTP `ILlmClient` в отдельные задачи, добавить автоматизированные UI/snapshot-проверки или TTY-smoke в CI.
- **Эффект мер прошлых волн:** помогло — явный порядок task→verify→merge→report и финальная cleanup-задача `T-23` снизили риск дрейфа имён/структуры; пробел — отсутствие автоматического UI-тестового контура.
- **Циклы/регрессии:** не выявлено.
- **Ссылки на `PI-*`:** не создавались в этой волне (`docs/projectDocs/process-improvement-log.md` без новых записей в рамках сессии).

## Итоги
- Закрыто задач: **7/7** (`T-21a`, `T-21b`, `T-21c`, `T-21d`, `T-21e`, `T-22`, `T-23`) — verified, merged в ветку волны.
- Открытые блокеры / новые T-* / TD-* / PI-* : блокеров нет. Наблюдения (при подтверждении — отдельным intake): публичный `IGameRules` в Core, реальный HTTP `ILlmClient` (потребление `Llm.BaseUrl/Model/Timeout`), автоматизация CLI/TTY smoke-проверок.
