# Handoff: structure sync (round 2)

- Роль: `docs-maintainer`, target `structure-sync`, раунд 2.
- Ветка: `docs/structure-sync`; коммит — см. `git log` ветки (subject: `docs(structure): sync tree with actual repository state`).
- Цель: устранить drift `docs/projectDocs/structure.md`, подтверждённый read-only аудитом раунда 1, без изменения продукта.

## Что сделано

Сверены все drift-пункты с фактическим состоянием рабочего дерева (`git ls-files`, дерево на диске) и обновлён только `docs/projectDocs/structure.md`.

Исправленные категории:
1. Добавлен проект `Kriegsspiel.AI/` (проект, LLM-адаптеры, `Prompts/`).
2. Раскрыт `Kriegsspiel.Core/Ai/` вместо пометки «заготовка» (10 файлов).
3. `Kriegsspiel.Core/Arbitration/`: добавлены 7 отсутствовавших резолверов/компонентов.
4. `Kriegsspiel.Core/Orders/`: добавлены `ConflictResolver.cs`, `OrderBranch.cs`.
5. Добавлен каталог `Kriegsspiel.Core/Persistence/` (13 DTO/маппинг/схема).
6. `Kriegsspiel.Console/Commands/`: добавлены `LoadCommand.cs`, `OrderBuilder.cs`, `QueryCommand.cs`.
7. Добавлен `Kriegsspiel.Console/Persistence/JsonSaveStore.cs` (перенесён из описания `Commands/`).
8. `Kriegsspiel.Core.Tests/`: добавлены 11 тестовых файлов.
9. `docs/gameDocs/featureSpecs/`: добавлены `FS-001`…`FS-011`.
10. `docs/projectDocs/reports/`: добавлены отчёты T-21a…T-21e, T-22…T-25, wave-3, wave-4.
11. `docs/projectDocs/reports/handoffs/`: добавлены 10 handoff-артефактов implementer.
12. Корень: добавлен `.gitignore`; описание `Kriegsspiel.slnx` уточнено (четыре проекта).

Исправленные неверные пути:
- `ITransmitionResolvers.cs` → `ITransmissionResolver.cs`.
- `TransmitionResolver.cs` → `TransmissionResolver.cs`.
- `Reports/IReportNarrator.cs` → `Reporting/IReportNarrator.cs`.
- `Commands/JsonSaveStore.cs` → `Persistence/JsonSaveStore.cs`.
- `TransmitionResolverTests.cs` → `TransmissionResolverTests.cs`.
- `Assesment/` → `Assessment/` (и убрана пометка «заготовка»).

## Изменённые файлы (ownership)

- `docs/projectDocs/structure.md` — актуализировано дерево.
- `docs/projectDocs/reports/handoffs/docs-maintainer-structure-sync-r2.md` — данный handoff.

## Проверки

- Сверка каждого документированного пути с диском: все записи существуют, отсутствующих путей нет.
- Сверка с `git ls-files`: все 227 отслеживаемых файлов присутствуют в дереве `structure.md`; недокументированных файлов нет.
- Продуктовый код, тесты, `AGENTS.md`, `docs/gameDocs/rules.md`, `docs/projectDocs/todo.md` не изменялись.

## Процесс (трение)

- Низко: исходный drift накопился из-за волн без синхронизации `structure.md`; предложение — включать правку `structure.md` в wave-задачу закрытия волны (вне scope данной задачи).
