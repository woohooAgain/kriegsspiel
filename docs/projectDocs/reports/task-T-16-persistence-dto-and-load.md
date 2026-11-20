# Отчёт задачи T-16 — Persistence DTO and load

- **T-XX:** T-16
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-16-persistence-dto-and-load`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Добавлены версионированные DTO/маппинг/схема в `Kriegsspiel.Core/Persistence/*` для сериализации состояния (clock/очередь, RNG seed/state, in-transit reports, couriers/ETA, pending casualties, knowledge, orders/effective orders/orderVersion, полиморфные события) без прямой сериализации живого `GameState`.
- `JsonSaveStore` перенесён из `Kriegsspiel.Console/Commands/JsonSaveStore.cs` в `Kriegsspiel.Console/Persistence/JsonSaveStore.cs` и адаптирован под DTO/версионирование.
- Добавлен `Kriegsspiel.Console/Commands/LoadCommand.cs` (точка загрузки в пределах файла, без интеграции в меню/DI — T-22).
- Добавлены `PersistenceTests` (round-trip детерминированного состояния, контроль версии схемы).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Persistence/*` (новые DTO/маппинг/схема/утилиты)
- `Kriegsspiel.Console/Persistence/JsonSaveStore.cs` (новый путь)
- `Kriegsspiel.Console/Commands/JsonSaveStore.cs` (удалён/перенесён)
- `Kriegsspiel.Console/Commands/LoadCommand.cs` (новый)
- `Kriegsspiel.Core.Tests/PersistenceTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-16`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter PersistenceTests` → Passed 4, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний нет.
- Merge в ветку волны: выполнен (merge `task/T-16-persistence-dto-and-load` → `wave/1-core-mechanics`, `879f3c7`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter PersistenceTests` → Passed 4, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): объём DTO/маппинга для полного состояния (`§12`, D) при сохранении совместимости и запрете правок вне ownership.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): анализ сериализуемости графа/очереди `Clock` и полиморфных событий.
- Оценка доп. затрат: средне.
- Предложения по улучшению: —

## Заметки / tech-debt
- Интеграция в меню/DI — T-22 (вне ownership). Отдельный `TD-*` не оформлялся в этой сессии.
