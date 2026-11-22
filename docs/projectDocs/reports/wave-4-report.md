# Отчёт волны Wave 4 — FS-005: большие карты и отображение в консоли

- **Дата:** 2026-11-22
- **Ветка волны / target:** `wave/4-large-maps-console-display` @ `84daf08` (HEAD перед wave-gate) → `master`
- **Сценарий проверок волны (architect):** `docs/projectDocs/todo.md:76-85`, карточки `T-24` (`todo.md:648-674`) и `T-25` (`todo.md:676-700`), `docs/gameDocs/featureSpecs/FS-005-large-maps-console-display.md`. Общие: `dotnet build Kriegsspiel.slnx`, `dotnet test Kriegsspiel.slnx`. Добавленные/изменённые механики: `T-24` — масштабируемая карта-знание (обзор↔деталь, топология, §6, фильтры/поиск, сводка юнитов, туман войны); `T-25` — демо-контент и проверки масштаба; обязательный прогон на `fs005_scale_demo` и/или `river_line_10x10` плюс регрессия на `test_01`/`crossing_01`.
- **Финализировал и закоммитил (verifier, после проверок):** verifier, 2026-11-22, после wave-проверок на HEAD `84daf08`; продуктовый код/тесты и `todo.md` не изменялись.

## Задачи волны
| T-* | Статус | Ветка/MR (refs) | Вердикт | Отчёт задачи | Примечание |
|-----|--------|------------------|---------|--------------|------------|
| `T-24` | verified, merged | `task/T-24-scalable-knowledge-map` @ `9f6c82a`; merge `e369af2` | verified (r2) | `docs/projectDocs/reports/task-T-24-scalable-knowledge-map.md` | обзор↔деталь, топология, §6, масштабируемые юниты; r2 — исправление фильтра «Синие» |
| `T-25` | verified, merged | `task/T-25-fs005-scale-demo` @ `6544cbc`; merge `3e1ba9e` | verified (r1) | `docs/projectDocs/reports/task-T-25-fs005-scale-demo.md` | `fs005_scale_demo`: 36 зон / 36 юнитов, 3 контент-теста, запись в `structure.md:16` |

## Параллелизм и ownership
- Launch-план: G1=`T-24` (`Kriegsspiel.Console/Commands/MapCommand.cs`, `MapRenderer.cs`), G2=`T-25` (`Content/scenarios/fs005_scale_demo.json`, `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs`, `docs/projectDocs/structure.md`) — пересечений ownership нет (`docs/projectDocs/todo.md:79`, `:646`).
- Per-task merge выполнены строго по одной задаче: `e369af2` (T-24), затем `3e1ba9e` (T-25). Коммит `84daf08` добавил только `PI-001..PI-003` в `docs/projectDocs/process-improvement-log.md`. Diff `1156fba..84daf08` содержит только файлы ownership, handoff/task-отчёты и process-log; `Content/scenarios/river_line_10x10.json` не изменён.

## Post-merge проверки по задачам (в контексте ветки волны)
- `T-24` @ `e369af2`: `dotnet build Kriegsspiel.slnx` — 0 warning/0 error; `dotnet test Kriegsspiel.slnx --no-build` — 145/145; CLI-прогон фильтра `Синие по донесениям` без сброса (`Фильтр: сторона=синие`, 0 зон на старте). Эскалаций нет (refs в task-отчёте).
- `T-25` @ `3e1ba9e`: build — 0/0; `Fs005ScaleContentTests` — 3/3; `dotnet test Kriegsspiel.slnx` — 148/148; `structure.md:16` содержит запись о сценарии; регрессий после T-24 нет.
- `84daf08` (process-log) — документальное изменение, сборку/тесты не затрагивает.

## Wave-проверка (по сценарию architect, на ветке волны)
- **Общие проверки работоспособности:** на HEAD `84daf08` (`dotnet 10.0.404`) самостоятельно повторены `dotnet build Kriegsspiel.slnx` → Build succeeded, 0 Warning / 0 Error (`/tmp/opencode/wave4-logs/verify-build.log`); `dotnet test Kriegsspiel.slnx --no-build` → 148 passed / 0 failed / 0 skipped (`verify-test-all.log`); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --no-build --filter Fs005ScaleContentTests` → 3 passed (`verify-test-fs005.log`). Первичные прогоны scenario-tester: `build.log`, `test-all.log`, `test-fs005.log` — те же результаты.
- **Добавленные/изменённые механики (`T-24`):** `git grep -n "PresentUnitIds" -- Kriegsspiel.Console/Commands/MapCommand.cs Kriegsspiel.Console/Commands/MapRenderer.cs` — совпадений нет (exit 1). CLI `fs005_scale_demo` (36 зон): компактный обзор 1/3→2/3→3/3→2/3; деталь `C2 — Северный мост` с §6-полями, топологией `B2/C1/C3` (бездорожье, 7 мин) и `D2` (переправа/мост, 5 мин), `СВОИ В ЗОНЕ: 1 (BLU-13)`, `ПРОТИВНИК: нет донесений о занятии`; сетка соседей и переход в `D2`, возврат в обзор; `ПОДРАЗДЕЛЕНИЯ` — `1 в 1 зонах`; фильтры (`состояние=нет данных`, только противоречия → 0 зон, сброс → все), поиск `мост` → 3 зоны. Регресс r2: `test_01` → `Синие по донесениям` даёт `Фильтр: сторона=синие | зон: 0` без сброса. Refs: `verify-cli-fs005-scale.txt`, `verify-cli-blue-filter.txt`, ввод `/tmp/opencode/scenario-wave4-input.md`, канонические `final-*.txt`.
- **Добавленные/изменённые механики (`T-25`):** 36 зон / 36 юнитов (18/18), валидные рёбра (Road/Offroad/Crossing), канонические `friction.json`/`reports.json`, `structure.md:16`, 3/3 целевых теста; `river_line_10x10.json` не изменён относительно `1156fba`.
- **Проверка на масштабе:** `river_line_10x10` (100 зон) — обзор 1/9→2/9→3/9, типы рёбер, деталь `C1 — Северная застава` с топологией и разделами знания/своих/противника, сводка `7 в 7 зонах`; `fs005_scale_demo` — см. выше. Запуск через `script` (TTY для Spectre) с генератором `/tmp/opencode/wave4-run/gen_keys.py`, EXIT=0 для всех 6 прогонов: `verify-cli-{fs005-scale,river-scale,test01-regression,crossing-regression,knowledge-probe,blue-filter}.log|.txt`.
- **§6 NoData/Empty/Occupied/Conflicting:** через загруженный сейв `/tmp/opencode/wave4-run/saves/zz-fs005-knowledge-probe.json` воспроизведено `Зон: 36 | нет данных: 32, пусто: 1, занято: 3, противоречия: 1`; `B5` — пусто, SCOUT-1, 2/3, событие 400/доставка 415; `C2` — занято, красные, RCN-BLU, 2/3, 405/420; `D2` — разные данные, обе версии RCN-A и PATROL-B, 1/3, «уверенность снижена по правилу противоречия»; сводка `18 в 16 зонах`; скрытая истина (RED-12 в D2) в выводе отсутствует. Ref: `verify-cli-knowledge-probe.txt`.
- **Регрессия:** `test_01` — деталь `OBJ — Высота 4.2`, сосед `START`, поиск `Высота` → 1 зона, сводка `2 в 1 зонах`; `crossing_01` — детали `B3`/`B4` с `ЗНАНИЕ`, `Версии/противоречия`, `ТОПОЛОГИЯ`, `СВОИ В ЗОНЕ`, `ПРОТИВНИК`, без ошибок. Refs: `verify-cli-test01-regression.txt`, `verify-cli-crossing-regression.txt`.
- **Сборка / автотесты / запуск/сценарий:** ok / ok / ok (6 CLI-прогонов EXIT=0).
- **Результат:** ok — все пункты `todo.md:76-85` подтверждены; корректирующие `T-*`, triage `architect` и эскалации не требуются.

## Merge ветки волны в основную
- Статус: после фиксации этого отчёта выполняется merge `wave/4-large-maps-console-display` → `master` в отдельном worktree `/tmp/opencode/mainline-merge`; основной worktree `/mnt/d/Development/Kriegsspiel` (49 uncommitted EOL-правок на `docs/fs-026-external-ai-narrative`) не затрагивается. Фактический merge commit и пост-merge проверки фиксируются в финальной версии отчёта.

## Ретро процесса (вклад process-master / факты участников)
- **Проблемы и фрикции (с refs):** gate C r1 провалился из-за рассинхрона базы плана (старая arch-ветка от `501262a`, тогда как в `master` уже были `3546794`, `8168b08` и `Content/scenarios/river_line_10x10.json`); восстановление rebase плана на `master@8168b08` → `1156fba` стоило дополнительного цикла. T-24 потребовал r2 из-за опечатки в строке фильтра меню (`Синые`→`Синие`): проверка модели проходила, но реальный выбор пункта меню сбрасывал фильтр; дефект найден verifier CLI-прогоном. Отсутствие remote/gh оставило merge ручным, без внешнего MR/CI-артефакта. Источник фактов — Status Envelope субагентов; прямых gate/triage/MR-артефактов в репозитории нет (пробел данных, без догадок). Вклад: `/tmp/opencode/process-master-wave4-input.md`.
- **Принятые/предложенные меры и влияние на сложность:** эффективны file-first handoff + refs, строгий per-task verify/merge, ownership-параллелизм T-24/T-25, post-merge build/test (145→148 тестов). Предложены PI-001 (pre-gate base-sync/ownership check, сложность низко), PI-002 (обязательный воспроизводимый прогон input-path/меню для UI-фич, низко/средне), PI-003 (evidence-пакет при ручном merge без remote/gh, низко).
- **Эффект мер прошлых волн:** подход Wave 3 (file-first, per-task отчёты, post-merge проверки) помог — параллельные T-24/T-25 прошли без конфликтов ownership, регрессий нет; утечка в Wave 4 — проверка UI-пути только на уровне модели дала лишний раунд (PI-002). Пробелы данных: нет сохранённых gate C r1/r2 артефактов, triage architect и MR/CI-логов; состав/форма старых отчётов не нормализовались (обратной силы нет).
- **Циклы/регрессии:** не выявлено.
- **Ссылки на `PI-*`:** `docs/projectDocs/process-improvement-log.md` @ `84daf08` — PI-001 (proposed), PI-002 (proposed), PI-003 (observed).

## Итоги
- Закрыто задач: **2/2** (`T-24` r2, `T-25` r1) — verified, merged в ветку волны, post-merge проверки зелёные; wave-gate по сценарию `todo.md:76-85` пройден.
- Открытые блокеры / новые `T-*` / `TD-*`: нет. Новые процессные меры: PI-001..PI-003. `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md` не изменялись; продуктовый код и тесты verifier не правились.
