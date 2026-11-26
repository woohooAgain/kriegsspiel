# Отчёт задачи T-52 — контент и сценарии v0.4

- **T-52:** `Scenario`/`ContentLoader`/`ScenarioBundle` (`GameStateFactory`) под v0.4-схему §1–§2; миграция `test_01`/`crossing_01`; удаление legacy-сценариев и `Fs005ScaleContentTests`; `ContentLoaderTests` под v0.4.
- **Волна / ветка волны:** Wave 5, `wave/5-v04-foundation` @ `6f6adbc` (база — `master@90acbe6`).
- **Ветка задачи / MR:** `task/T-52-content-v04` @ `a835d7f` (код-коммит `17c5432`), worktree implementer `/tmp/opencode/ks-implementer-T-52-r1`; MR не создавался (передача по refs ветки).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-52-r1.md` (`a835d7f`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/wave5-g2`.

## Выполнено (по пунктам «Что сделать»)
1. `Scenario`: v0.4-поля `HqZoneId` (`hqZone`), `CourierCount` (`courierPool.count`), `CourierLossProbability`, `ReportLossProbability`, `WinThreshold` (`winThreshold`); `Seed`/`StartMinute`/`EndMinute` сохранены; новых типов-контейнеров нет.
2. `ContentLoader`: нормализация v0.4-имён (`courierPool`, `courierTimes`, `observability`, `zoneId`, `initialStrength`, `posture`, traits `init/pedantry/honesty`) в существующие доменные поля; маппинг `faction` Blue→player, Red→enemy; `type=HQ` задаёт `HqZoneId` и исключается из боевого списка. Legacy-fallback отсутствует.
3. Валидация §1–§2: связный граф, hq/targets существуют, `endMinute % 30 == 0`, `K >= 1`, вероятности `[0,1]`, `defenseBonus 0..2`, `observability 1..3`, `travelTime/courierTime > 0`, симметричные рёбра (travel/courier/type), уникальные id, обязательный `seed`, `initialStrength > 0`, `readiness 0..100`, `ammo >= 0`, traits `0..3`; ошибки — `ContentValidationException`.
4. `GameStateFactory` (существующий статический класс в `ScenarioBundle.cs`): начальное состояние с `HqZoneId`/`EndMinute`/`Seed`/`RngState`, node-локациями и детерминированным пулом `CourierCount` idle-курьеров; новых типов нет.
5. `test_01.json`/`crossing_01.json` мигрированы на v0.4 (`hqZone`, `courierPool`, `winThreshold`, `observability`, `courierTimes`, `edgeTypes` road/offroad, traits); `fs005_scale_demo.json`, `river_line_10x10.json`, `Fs005ScaleContentTests.cs` удалены.
6. `ContentLoaderTests` переписан: позитивные проверки репозиторных сценариев/маппинга/фабрики и негативные кейсы карточки (defenseBonus/observability, travel/courier time, K, вероятности, дубли id, несимметричное ребро, endMinute, seed, initialStrength, readiness, ammo, traits, disconnected graph, отсутствие v0.4-полей, invalid edge type).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Content/ContentLoader.cs`
- `Kriegsspiel.Core/Content/Scenario.cs`
- `Kriegsspiel.Core/Content/ScenarioBundle.cs`
- `Content/scenarios/test_01.json`
- `Content/scenarios/crossing_01.json`
- `Content/scenarios/fs005_scale_demo.json` (удалён)
- `Content/scenarios/river_line_10x10.json` (удалён)
- `Kriegsspiel.Core.Tests/ContentLoaderTests.cs`
- `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs` (удалён)
- `docs/projectDocs/reports/handoffs/implementer-T-52-r1.md` (process-артефакт)
- `Kriegsspiel.Core/Content/Objective.cs` — без изменений (существующего типа достаточно).
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (проверено `git diff --name-only 6f6adbc..a835d7f`).

## Проверки (evidence refs)
- Pre-merge (worktree `/tmp/opencode/ks-implementer-T-52-r1`, `task/T-52-content-v04` @ `a835d7f`; повторено verifier):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test ... --filter "FullyQualifiedName~ContentLoaderTests"` → 22/22 passed.
  - `dotnet test Kriegsspiel.slnx` → 158/158 passed.
  - Wave 5 общий фильтр → 88/88 passed.
  - `python3 -m json.tool` для `test_01.json`/`crossing_01.json` → OK (репозиторные сценарии загружаются позитивными тестами).
- Post-merge (ветка `verifier/wave5-g2`, контекст волны после T-51, merge `f31e55e`):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `ContentLoaderTests` → 22/22 passed.
  - `dotnet test Kriegsspiel.slnx` → 166/166 passed (рост относительно pre-merge T-52 за счёт уже смерженных v0.4 `ClockTests` из T-51).
  - Wave 5 общий фильтр → 96/96 passed.

## Верификация (verifier)
- **Вердикт: verified.**
- Соответствие `rules.md` v0.4 (§1–§2): поля сценария, граф (road/offroad), курьеры, юниты/цели/seed, валидация и отсутствие legacy-fallback подтверждены кодом и тестами; новых механик сверх v0.4 нет.
- Ownership строгий; `AGENTS.md`/`rules.md`/чужие файлы не тронуты. Удаления (`fs005_scale_demo.json`, `river_line_10x10.json`, `Fs005ScaleContentTests`) входят в ownership карточки.
- Новых проектов/объявлений `class|record|struct|interface|enum` в diff нет; путей `*/Mvp/*` нет; `Kriegsspiel.slnx` не менялся.
- Тесты: legacy-пакет `Fs005ScaleContentTests` удалён вместе с механикой; `ContentLoaderTests` переписан под v0.4; новых тестовых классов не создано.
- Техдолга перехода нет (новых `TD-*`; `tech-debt.md` не менялся). `Scenario.VictoryThreshold` — временный alias для не-owned `ObjectiveEvaluator` (cleanup `T-59`/`T-65`), не v0.4-путь.
- Процесс: работа в task-ветке, коммиты `17c5432`/`a835d7f` с атрибуцией `implementer`; в чужие ветки/`master` коммитов нет.
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-52-content-v04` → `verifier/wave5-g2` (`--no-ff`, merge `f31e55e`); `wave/5-v04-foundation` переведена fast-forward.
- **Post-merge в контексте волны:** сборка/`ContentLoaderTests`/full suite/Wave 5 фильтр → PASS (см. выше).

## Процесс (трение)
- Неоднозначности: карточка запрещает новые DTO-контейнеры, но требует v0.4-JSON-имена; реализовано нормализацией JSON в существующие доменные поля `ContentLoader` (без новых типов) — согласуется с ограничением.
- Лишние поиски: `fs005`/`river_line` остаются в `docs/projectDocs/structure.md` (вне ownership T-52); актуализация — `T-66` docs-maintainer, на проверку не влияет.
- Оценка доп. затрат: средне (крупный diff валидатора/тестов и миграция сценариев), проверки воспроизводимы.
- Предложения: при cleanup `T-65` синхронно удалить временный alias `VictoryThreshold` и extended-валидацию `friction.json`, чтобы исключить двойные источники истины.

## Заметки / tech-debt
- TD нет.
- Наблюдение (не блокирует): `friction.json`-валидация extended-секций сохранена для промежуточной зелёной сборки; `FrictionTable` и потребители удаляются в `T-65`.
