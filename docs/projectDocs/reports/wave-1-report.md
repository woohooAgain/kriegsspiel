# Отчёт волны Wave 1 — механики ядра

- **Дата:** 2026-11-20
- **Ветка волны:** `wave/1-core-mechanics`
- **Основная ветка (merge target):** `master`
- **Сценарий проверок волны (architect):** `docs/projectDocs/todo.md` → «Сценарии проверок волн (детально, для verifier)» → Wave 1. Общие: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj` (при появлении отдельных test-проектов — `dotnet test Kriegsspiel.slnx`) без регрессий Wave 0. Механики (дублировать `Как проверить`): `T-10` `TransmitionResolverTests`, `T-11` `AmbiguityScorerTests|UnderstandingResolverTests`, `T-12` `ExecutionResolverTests`, `T-13` `CombatResolverTests`, `T-14` `FogOfWarTests|ReportTests`, `T-15` `ObjectiveEvaluatorTests`, `T-16` `PersistenceTests`, `T-17` `AIAdapterTests` (+ сборка `Kriegsspiel.AI/Kriegsspiel.AI.csproj` при доступности; ограничение при недоступности до интеграции), `T-18` `EnemyDoctrineTests` — с утверждениями из сценария/todo.
- **Финализировал и закоммитил (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Зафиксированное отклонение процесса
- **Факт:** после доработки/слияния плана в `master` и создания ветки волны был выполнен параллельный запуск `implementer` для `T-10`–`T-18` в отдельных task-ветках/worktrees (`/tmp/opencode/wave1/T-10..T-18`), реализации закоммичены в task-ветках и task-фильтры прогнаны в worktree’ах, но **не были сразу выполнены** обязательные шаги полного цикла: per-task верификация `verifier`, merge каждой задачи в ветку волны по одной (`verified`/post-merge сборка+старт в контексте ветки волны), финализация/коммит task-отчётов, wave-проверки по сценарию `architect`, финальный wave-report и merge ветки волны в `master`.
- **Причина:** преждевременное завершение шага сессии после этапа реализации/локальных прогонов в task-ветках.
- **Корректирующее действие (выполнено):** отклонение зафиксировано; далее выполнен полный цикл — по каждой задаче проведена верификация/merge в `wave/1-core-mechanics`, пост-merge проверки и task-report, затем wave-проверки по сценарию и финализация отчёта, подготовлен merge ветки волны в `master`.

## Задачи волны
| T-* | Статус | Ветка (impl commit) | Merge в ветку волны | Вердикт verifier | Отчёт задачи | Примечание |
|-----|--------|----------------------|---------------------|------------------|--------------|------------|
| `T-10` | verified, merged | `task/T-10-couriers-and-transmission` (`c9f8bc1`) | `add8857` | verified | `docs/projectDocs/reports/task-T-10-couriers-and-transmission.md` (`7563453`) | post-merge build+filter OK |
| `T-11` | verified, merged | `task/T-11-orders-ambiguity-understanding` (`5c2f4aa`) | `5b4155d` | verified | `docs/projectDocs/reports/task-T-11-orders-ambiguity-understanding.md` (`bc6d9e9`) | post-merge build+filter OK |
| `T-12` | verified, merged | `task/T-12-execution-and-routing` (`d65a83d`) | `3221d5b` | verified | `docs/projectDocs/reports/task-T-12-execution-and-routing.md` (`c928e9a`) | post-merge build+filter OK |
| `T-13` | verified, merged | `task/T-13-combat-and-contact-geometry` (`133b327`) | `c3c6a2d` | verified | `docs/projectDocs/reports/task-T-13-combat-and-contact-geometry.md` (`98328ba`) | post-merge build+filter OK |
| `T-14` | verified, merged | `task/T-14-reports-fog-and-recon` (`e6766df`) | `4cf0ff0` | verified | `docs/projectDocs/reports/task-T-14-reports-fog-and-recon.md` (`9d4726f`) | post-merge build+filter OK |
| `T-15` | verified, merged | `task/T-15-objectives-and-aar` (`7eeb961`) | `9c774bc` | verified | `docs/projectDocs/reports/task-T-15-objectives-and-aar.md` (`571cbd7`) | post-merge build+filter OK |
| `T-16` | verified, merged | `task/T-16-persistence-dto-and-load` (`474091c`) | `879f3c7` | verified | `docs/projectDocs/reports/task-T-16-persistence-dto-and-load.md` (`c816dc4`) | post-merge build+filter OK |
| `T-17` | verified, merged | `task/T-17-ai-llm-adapter` (`cc1a9fe`) | `906b24e` | verified | `docs/projectDocs/reports/task-T-17-ai-llm-adapter.md` (`7248914`) | post-merge build+AI build+filter OK; `Kriegsspiel.AI` не в `.slnx`/нет ref из test csproj (T-22) |
| `T-18` | verified, merged | `task/T-18-enemy-doctrine` (`8527984`) | `dcd3d68` | verified | `docs/projectDocs/reports/task-T-18-enemy-doctrine.md` (`8246d98`) | post-merge build+filter OK |

## Параллелизм и ownership
- `T-10`–`T-18` реализовывались параллельно в отдельных ветках/worktrees от `wave/1-core-mechanics` (`d66d427`). Владение файлами по карточкам не пересекалось; при per-task merge/верификации критичных нарушений ownership не выявлено (замечания/ограничения интеграции зафиксированы в task-отчётах, в т.ч. T-17: `.slnx`/DI вне ownership).

## Post-merge проверки по задачам (в контексте ветки волны)
- После каждого merge `T-10`→`T-18` в `wave/1-core-mechanics` выполнялись: `dotnet build Kriegsspiel.slnx` и `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "<task filter>"` по сценарию Wave 1 — **успешно** (см. task-отчёты). Дополнительно для `T-17`: `dotnet build Kriegsspiel.AI/Kriegsspiel.AI.csproj` — успешно.

## Wave-проверка (по сценарию architect, на ветке волны)
- **Общие:** `dotnet build Kriegsspiel.slnx` → **успешно** (0 предупреждений/0 ошибок; при одном из прогонов отмечен transient `MSB3026` file-lock retry при параллельном доступе к `Kriegsspiel.Core.dll`, повторная сборка успешна). `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj` → **Passed 142, Failed 0, Skipped 0** (без регрессий Wave 0).
- **T-10** `--filter "TransmissionResolverTests"` → Passed 7/7.
- **T-11** `--filter "AmbiguityScorerTests|UnderstandingResolverTests"` → Passed 23/23.
- **T-12** `--filter "ExecutionResolverTests"` → Passed 13/13.
- **T-13** `--filter "CombatResolverTests"` → Passed 7/7.
- **T-14** `--filter "FogOfWarTests|ReportTests"` → Passed 19/19.
- **T-15** `--filter "ObjectiveEvaluatorTests"` → Passed 8/8.
- **T-16** `--filter "PersistenceTests"` → Passed 4/4.
- **T-17** `--filter "AIAdapterTests"` → Passed 10/10; `dotnet build Kriegsspiel.AI/Kriegsspiel.AI.csproj` → **успешно**. Ограничение: проект `Kriegsspiel.AI` не подключён в `.slnx`/test csproj до T-22 (вне ownership T-17) — зафиксировано в task-отчёте.
- **T-18** `--filter "EnemyDoctrineTests"` → Passed 8/8.
- **Итог:** сценарий Wave 1 выполнен, проверки задач подтверждены на ветке волны.

## Merge ветки волны в основную
- Статус: готов к merge `wave/1-core-mechanics` → `master` после коммита данного отчёта; выполнить merge и пост-merge build/tests в контексте `master`.

## Ретро процесса
- Отклонение: неполный цикл после реализации (пропущены верификация/merge/checks/merge в main) из-за преждевременного завершения шага; исправлено в текущей сессии полным per-task циклом и wave-проверками.
- Наблюдение: при создании новых проектов task-владение `.slnx`/csproj-ссылок/DI следует явно разводить или планировать интеграционную задачу, иначе часть проверок (сборка solution) подтверждается косвенно.

## Итоги
- Закрыто задач: 9/9 (T-10–T-18 verified, merged в ветку волны, task-отчёты созданы)
- Открытые блокеры: нет по проверкам Wave 1. Замечания/ограничения интеграции (T-11/T-12/T-13/T-14/T-17/T-18, T-16→T-22, T-17→T-22) зафиксированы в task-отчётах; отдельные `TD-*` в `tech-debt.md` в этой сессии не заводились.
