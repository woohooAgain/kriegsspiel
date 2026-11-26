# Implementer handoff — T-52 (контент и сценарии v0.4)

- **Роль:** implementer
- **Раунд:** r1
- **Target:** `T-52` (`docs/projectDocs/todo.md`, Wave 5, G2)
- **База:** `wave/5-v04-foundation` @ `6f6adbc`
- **Worktree:** `/tmp/opencode/ks-implementer-T-52-r1`
- **Ветка:** `task/T-52-content-v04` @ `17c5432` (код; handoff-коммит добавляет только этот файл)
- **MR:** task-ветка → `wave/5-v04-foundation` (merge выполняет verifier)
- **Статус:** `ready_for_verifier`

## Дельта / закрытые пункты карточки

1. `Scenario`: добавлены v0.4-поля `HqZoneId` (`hqZone`), `CourierCount` (`courierPool.count`), `CourierLossProbability`, `ReportLossProbability`, `WinThreshold` (`winThreshold`); `Seed`/`StartMinute`/`EndMinute` сохранены.
2. `ContentLoader`: нормализация v0.4-схемы (`courierPool`, `courierTimes`, `observability`, `zoneId`, `initialStrength`, `posture=defense/march/attack`, traits `init/pedantry/honesty`) и валидация §1–§2: связность графа, hq/targets, `endMinute % 30`, `K>=1`, вероятности `[0,1]`, `defenseBonus 0..2`, `observability 1..3`, `travelTime/courierTime > 0`, симметрия рёбер/типов, уникальные id, обязательный `seed`, `initialStrength > 0`, `readiness 0..100`, `ammo >= 0`, traits `0..3`. Legacy-fallback отсутствует.
3. `type=HQ`: задаёт `HqZoneId`, удаляется из боевого списка; сценарии используют явный `hqZone`.
4. `GameStateFactory` (в `ScenarioBundle.cs`): начальное состояние с `HqZoneId`, `EndMinute`, `Seed`/`RngState`, node-локациями и детерминированным пулом `CourierCount` idle-курьеров.
5. `test_01`/`crossing_01` мигрированы на v0.4; `fs005_scale_demo.json`, `river_line_10x10.json`, `Fs005ScaleContentTests.cs` удалены.
6. `ContentLoaderTests` переписан на v0.4: позитивные проверки репозиторных сценариев/маппинга/фабрики и негативные кейсы из карточки (включая HQ-маппинг и отсутствие legacy-сценариев).

## Изменённые файлы (ownership T-52)

- `Kriegsspiel.Core/Content/ContentLoader.cs`
- `Kriegsspiel.Core/Content/Scenario.cs`
- `Kriegsspiel.Core/Content/ScenarioBundle.cs`
- `Content/scenarios/test_01.json`
- `Content/scenarios/crossing_01.json`
- `Content/scenarios/fs005_scale_demo.json` (удалён)
- `Content/scenarios/river_line_10x10.json` (удалён)
- `Kriegsspiel.Core.Tests/ContentLoaderTests.cs`
- `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs` (удалён)
- `docs/projectDocs/reports/handoffs/implementer-T-52-r1.md` (handoff)

`Kriegsspiel.Core/Content/Objective.cs` не менялся (в v0.4-схеме достаточно существующего `Objective`). Вне ownership изменений нет.

## Проверки (фактические)

| Команда | Результат | Лог |
|---|---|---|
| `dotnet build Kriegsspiel.slnx` | Build succeeded, 0 warnings, 0 errors | `/tmp/opencode/t52-logs/build.log` |
| `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ContentLoaderTests"` | 22 passed / 0 failed | `/tmp/opencode/t52-logs/test-content.log` |
| `dotnet test Kriegsspiel.slnx` | 158 passed / 0 failed / 0 skipped | `/tmp/opencode/t52-logs/test-all.log` |
| Wave 5 общий фильтр (`DomainInvariantsTests|FogOfWarTests|ClockTests|ContentLoaderTests|ExecutionResolverTests|ReportTests`) | 88 passed / 0 failed | `/tmp/opencode/t52-logs/test-wave5-filter.log` |
| `python3 -m json.tool` для `test_01.json`/`crossing_01.json` | JSON_OK | — |

Покрытые негативные кейсы `ContentLoaderTests`: defenseBonus вне 0..2, observability вне 1..3, travelTime/courierTime <= 0, K<1, вероятности вне [0,1], дубли id, несимметричное ребро, endMinute не кратен 30, отсутствие seed, initialStrength<=0, readiness вне 0..100, ammo<0, traits вне 0..3, disconnected graph, отсутствие v0.4-полей (hqZone/courierPool), invalid edge type.

## Отклонения / заметки

- `Scenario.VictoryThreshold` сохранён как временный alias для не входящего в ownership `ObjectiveEvaluator` (Wave 5 build зелёный); значение синхронизируется из `WinThreshold` при загрузке. Удаление/перевод на §7 — `T-59`/`T-65`, это не v0.4-путь.
- Валидация extended-секций `friction.json` (movement/capture/civilians/llm) сохранена, т.к. `FrictionTable` и её потребители удаляются в `T-65`; сценарная часть уже v0.4.
- `Zone` (T-50) хранит `Observation`/`TravelMinutes`/`CourierMinutes`; в JSON используются v0.4-имена `observability`/`courierTimes`, которые нормализуются загрузчиком.
- `fs005`/`river_line` всё ещё упомянуты в `docs/projectDocs/structure.md` (вне ownership); актуализация — `T-66` docs-maintainer.

## TD

Нет. Задача закрывается без TD (переходные заметки выше — в scope `T-59`/`T-65`/`T-66`).

## Процесс (трение)

- **Низко:** потребовалось свериться с T-50-полями `Zone`/`Unit`/`GameState` и не-owned потребителями (`ObjectiveEvaluator`, `Console`), чтобы сохранить зелёную сборку без выхода за ownership; отражено в отклонениях.
- **Низко:** из-за явного запрета новых типов v0.4-имена JSON нормализуются в существующие поля домена в `ContentLoader` (без DTO/конвертеров-классов).
