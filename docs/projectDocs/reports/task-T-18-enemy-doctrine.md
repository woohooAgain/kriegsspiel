# Отчёт задачи T-18 — Enemy doctrine

- **T-XX:** T-18
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-18-enemy-doctrine`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Реализован `EnemyDoctrine` (детерминированное правило + расширяемая точка для doctrine AI) для генерации решений врага в рамках ownership.
- Добавлены `EnemyDoctrineTests` (детерминизм, реакция на события/контекст, отсутствие прямого доступа к истине вне допустимого контекста — по фильтру/реализации).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/EnemyDoctrine.cs` (новый)
- `Kriegsspiel.Core.Tests/EnemyDoctrineTests.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-18`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter EnemyDoctrineTests` → Passed 8, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний нет.
- Merge в ветку волны: выполнен (merge `task/T-18-enemy-doctrine` → `wave/1-core-mechanics`, `dcd3d68`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter EnemyDoctrineTests` → Passed 8, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): объём контекста/интеграция doctrine-AI с пайплайном T-17/T-20.
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): сверка границ с AI-контрактами.
- Оценка доп. затрат: низко/средне.
- Предложения по улучшению: —

## Заметки / tech-debt
- Интеграция doctrine в общий цикл/AI-пайплайн — T-20/T-22. Отдельный `TD-*` не оформлялся в этой сессии.
