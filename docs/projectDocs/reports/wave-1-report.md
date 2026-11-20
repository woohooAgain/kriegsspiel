# Отчёт волны Wave 1 — механики ядра

- **Дата:** 2026-11-20
- **Ветка волны:** `wave/1-core-mechanics`
- **Основная ветка (merge target):** `master`
- **Сценарий проверок волны (architect):** `docs/projectDocs/todo.md` → «Сценарии проверок волн (детально, для verifier)» → Wave 1. Общие: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj` (при появлении отдельных test-проектов — `dotnet test Kriegsspiel.slnx`) без регрессий Wave 0. Механики (дублировать `Как проверить`): `T-10` `TransmitionResolverTests`, `T-11` `AmbiguityScorerTests|UnderstandingResolverTests`, `T-12` `ExecutionResolverTests`, `T-13` `CombatResolverTests`, `T-14` `FogOfWarTests|ReportTests`, `T-15` `ObjectiveEvaluatorTests`, `T-16` `PersistenceTests`, `T-17` `AIAdapterTests` (+ сборка `Kriegsspiel.AI/Kriegsspiel.AI.csproj` при доступности; ограничение при недоступности до интеграции), `T-18` `EnemyDoctrineTests` — с утверждениями из сценария/todo.
- **Финализировал и закоммитил (verifier, после проверок):** в процессе (черновик/промежуточная фиксация)

## Зафиксированное отклонение процесса
- **Факт:** после доработки/слияния плана в `master` и создания ветки волны был выполнен параллельный запуск `implementer` для `T-10`–`T-18` в отдельных task-ветках/worktrees (`/tmp/opencode/wave1/T-10..T-18`), реализации закоммичены в task-ветках и task-фильтры прогнаны в worktree’ах, но **не выполнены** обязательные шаги полного цикла: per-task верификация `verifier`, merge каждой задачи в ветку волны по одной (`verified`/post-merge сборка+старт в контексте ветки волны), финализация/коммит task-отчётов, wave-проверки по сценарию `architect`, формирование финального wave-report и merge ветки волны в `master`.
- **Причина:** преждевременное завершение шага сессии после этапа реализации/локальных прогонов в task-ветках; полный цикл (проверки → merge в волну → wave-check → merge в main) не доведён без явного указания остановиться.
- **Корректирующее действие:** зафиксировать отклонение (настоящий отчёт), продолжить работу по процессу: по каждой задаче — верификация/merge в `wave/1-core-mechanics`, пост-merge проверки и task-report, затем wave-проверка, финализация отчёта и merge ветки волны в `master`.

## Задачи волны
| T-* | Статус | Ветка | MR (→ ветка волны) | Вердикт verifier | Отчёт задачи | Примечание |
|-----|--------|-------|---------------------|------------------|--------------|------------|
| `T-10` | in_review (реализация в task-ветке) | `task/T-10-couriers-and-transmission` | — | pending | — | commit `c9f8bc1`, worktree `/tmp/opencode/wave1/T-10` |
| `T-11` | in_review (реализация в task-ветке) | `task/T-11-orders-ambiguity-understanding` | — | pending | — | commit `5c2f4aa` |
| `T-12` | in_review (реализация в task-ветке) | `task/T-12-execution-and-routing` | — | pending | — | commit `d65a83d` |
| `T-13` | in_review (реализация в task-ветке) | `task/T-13-combat-and-contact-geometry` | — | pending | — | commit `133b327` |
| `T-14` | in_review (реализация в task-ветке) | `task/T-14-reports-fog-and-recon` | — | pending | — | commit `e6766df` |
| `T-15` | in_review (реализация в task-ветке) | `task/T-15-objectives-and-aar` | — | pending | — | commit `7eeb961` |
| `T-16` | in_review (реализация в task-ветке) | `task/T-16-persistence-dto-and-load` | — | pending | — | commit `474091c` |
| `T-17` | in_review (реализация в task-ветке) | `task/T-17-ai-llm-adapter` | — | pending | — | commit `cc1a9fe`, ограничения см. task-report/заметки |
| `T-18` | in_review (реализация в task-ветке) | `task/T-18-enemy-doctrine` | — | pending | — | commit `8527984` |

## Параллелизм и ownership
- `T-10`–`T-18` реализовывались параллельно в отдельных ветках/worktrees от `wave/1-core-mechanics` (`d66d427`). Ownership по карточкам задач не пересекается (проверка по спискам файлов веток — требует подтверждения при per-task верификации/merge).

## Post-merge проверки по задачам (в контексте ветки волны)
- pending

## Wave-проверка (по сценарию architect, на ветке волны)
- pending

## Merge ветки волны в основную
- Статус: не выполнен (работа в процессе)

## Ретро процесса
- Отклонение: неполный цикл после реализации (пропущены верификация/merge/checks/merge в main). Причина — преждевременное завершение шага; исправляется в рамках текущей сессии. Требует дисциплины полного цикла по умолчанию.

## Итоги
- Закрыто задач: 0/9 (pending верификация и merge в ветку волны)
- Открытые блокеры / новые T-* / TD-*: TBD по результатам проверок
