# Отчёт задачи T-12 — Execution and routing

- **T-XX:** T-12
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-12-execution-and-routing`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Реализован `PathPlanner` (multi-hop маршруты по графу зон с учётом типов рёбер/проходимости и времён перехода).
- `ExecutionResolver` переведён на пошаговое движение/маршрутизацию, обработку контактов/событий и реакций на невыполнимость в рамках контрактов и ownership.
- Добавлены `ExecutionResolverTests` (маршруты, блокировки, контакты, реакции/дедлайны, отсутствие телепорта — по реализации/фильтру).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
- `Kriegsspiel.Core/Arbitration/PathPlanner.cs` (новый)
- `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-12`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ExecutionResolverTests` → Passed 13, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний нет; есть ограничения интеграции вне ownership.
- Merge в ветку волны: выполнен (merge `task/T-12-execution-and-routing` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ExecutionResolverTests` → Passed 13, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): диспетчеризация/планирование в `Arbiter`, хранение прогресса маршрута для persistence/полной интеграции (`§2–§5`, трассы D/B), вне ownership.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): уточнение границ с `Arbiter`/`CombatResolver`/T-20.
- Оценка доп. затрат: средне.
- Предложения по улучшению: фиксировать ожидания по интеграции событий/планирования с T-20 в карточках Wave 1.

## Заметки / tech-debt
- Ограничения implementer: диспетчеризация/`EdgeDestroyed` в `Arbiter` вне ownership; прогресс маршрута не сериализуется (связь с T-16/T-20); `docs/projectDocs/structure.md` не обновлялся (вне ownership).
