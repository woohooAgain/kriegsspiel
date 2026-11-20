# Отчёт волны Wave 0 — фундамент

- **Дата:** 2026-11-20
- **Целевая интеграционная ветка / main:** `master` (после финальных runtime-фиксов)

## Задачи волны
| T-* | Статус | Ветка | MR | Вердикт verifier | Примечание |
|-----|--------|-------|----|------------------|------------|
| `T-01` | done | `task/T-01-domain-and-contracts` | не создан (нет remote) | `verified` (финальный раунд) | `cecd2da`, `2e9b274`, `db7a076`, `b226f81` |
| `T-02` | done | `task/T-02-clock-and-event-ordering` | не создан (нет remote) | `verified` | `894054a` |
| `T-03` | done | `task/T-03-content-profile-and-validation` | не создан (нет remote) | `verified` (финальный раунд) | `fde7313`, `89f0558`, `ff8090a`, `7e72d01`, `1aae871` |

## Параллелизм и ownership
- `T-01`, `T-02`, `T-03` запускались параллельно в отдельных git worktree; пересечений `Владеет файлами` нет (проверено до старта).
- Каждый task-коммит изменял только файлы своего ownership; `AGENTS.md`, `docs/gameDocs/rules.md`, csproj и чужой код не тронуты.
- До старта зафиксирован «замороженный контракт» `T-01 → T-03` (Faction/EdgeType/ZoneObject/Cargo/CommanderTraits/Zone/GameState), что позволило избежать type-drift при параллельной работе.
- Финальный verifier отдельно проверил новые фиксы: ownership соблюдён, `git diff --check` чистый.

## Post-merge проверки (после каждого merge в целевую ветку)
- Интеграция: `wave0-integration` (`2305abe`, `49d234b`, `6879aab`, `594150a`, `23cc87d`) → fast-forward `master` (`23cc87d`).
- После установки .NET SDK `10.0.404` (`/home/ikulikov/.dotnet`) выявленные compile/test-дефекты исправлены и слиты в `master`.
- `dotnet build Kriegsspiel.slnx` → **Build succeeded**, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --no-build` → **Passed 46, Failed 0, Skipped 0**.
- `dotnet test Kriegsspiel.slnx --no-build` → **Passed 46, Failed 0, Skipped 0**.
- `git diff --check` → чисто.
- Ранее, до установки .NET, выполнялись `python3 -m json.tool` (4 JSON OK), проверка баланса скобок по всем `.cs` и статическая проверка кросс-сигнатур verifier'ом.

## Wave-level интеграционное тестирование («запуск и игра»)
- Кто: оркестратор (роль `scenario-tester` не запускалась отдельно).
- Сценарий: `test_01` (по `appsettings.json`), запуск `Kriegsspiel.Console` через pty.
- Шаги: `ОБСТАНОВКА` → `ВРЕМЯ +30` → `ВЫЙТИ`.
- Результат: консоль стартовала (`Тест: один батальон, одна высота — старт 06:00`), меню отработало, процесс завершился с `exit 0`.
- Ограничение: не выполнялся полный сценарный прогон `crossing_01` до конца; проверен smoke-путь запуска и базовых команд.

## Итоги
- Закрыто задач: 3/3 (код слит в `master`, финальный verifier `verified` после runtime-прогона).
- Блокеров нет: .NET установлен, сборка и тесты зелёные, консоль запускается.
- DoD-чекбоксы в `todo.md` для `T-01`/`T-03` отмечены.
- Открытые `TD-*`: `TD-001` AmbiguityScorer, `TD-002` CombatResolver/Faction, `TD-003` Impossible ⇒ ReportBy, `TD-004` `deliveryMinute = ∞`, `TD-005` Seed/EndMinute в `GameStateFactory`. Compile/test-фиксы новых TD не создали.
- Wave 0 формально подтверждена сборкой, тестами и запуском; Wave 1 может стартовать.
