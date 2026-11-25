---
name: process-contract
description: Use in every Kriegsspiel role run to load the canonical process invariants (sources of truth, branch/merge rules, own/merge discipline, no-backdating, file-first) before acting.
---

# Process contract (канонические инварианты)

Загружай этот скилл перед предметной работой. Он задаёт общие правила, которые раньше дублировались в `AGENTS.md` и `workflows/*`.

## 1. Приоритет источников (при расхождении)
1. `docs/gameDocs/rules.md` — канон игры.
2. `AGENTS.md` — глобальные правила работы агента.
3. Этот скилл (`process-contract`) — процессные инварианты.
4. `docs/projectDocs/instructions.md`, `structure.md`, `todo.md`, `reports/README.md`.
5. `docs/projectDocs/workflows/*` — человекочитаемый нарратив.
6. Определения ролей — `.opencode/agent/*`; шаблоны/чек-листы — `.opencode/skills/*`.

Если обнаружил противоречие — не выбирай молча: зафиксируй факт и эскалируй (через `orchestrator`) к ответственной роли (`analyst`/`rules-auditor` для правил, `architect` для плана, `process-master` для процесса).

## 2. Операционная модель
- Конкретные действия выполняет конкретная роль; `orchestrator` только дирижирует (не читает файлы, не синтезирует, выводит только Status Envelope).
- Детали — в артефактах (file-first); в ответ оркестратору — Status Envelope + refs, без логов/диффов/копипаста; на доработках — дельта.
- Каждая роль сама читает нужные файлы; для экономии контекста можно использовать поисковые субагенты, но не перекладывать анализ на `orchestrator`.

## 3. Обратной силы нет
Новые правила применяются только к новым задачам/волнам/спекам/отчётам. Старые артефакты не приводятся задним числом; при анализе извлекать доступную информацию из любого представления и явно фиксировать пробелы данных (без догадок).

## 4. План и ownership
- `docs/projectDocs/todo.md` правит только `architect` в отдельной ветке по спецзапросу. Остальные роли — никогда.
- Строгий file ownership: исполнитель меняет только файлы из `Владеет файлами` своей карточки. Чужие файлы — зависимость/следующая волна.
- Внутри волны задачи не пересекаются по файлам; параллельные `implementer` — только по launch-плану `architect`.

## 5. Ветвление и merge
- Ветка волны: `wave/<N>-<slug>` от указанной базы (создаёт `architect` после `gate_passed`).
- Ветка задачи: `task/T-XX-<kebab-slug>` от ветки волны; MR задачи → в ветку волны.
- `implementer` не мержит. `verifier` мержит per-task после `verified` и ветку волны в `main` при условиях.
- Прямые коммиты в `main` запрещены. Валидация/merge — строго по одной `T-XX`, batch запрещён.

## 6. Качество и команды
- Канонические проверки: `dotnet build Kriegsspiel.slnx`, `dotnet test Kriegsspiel.slnx` (или `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj`). Отсутствие среды — фиксировать как ограничение с доказательствами.
- Код без комментариев, если явно не просили. Следовать `docs/projectDocs/instructions.md`.
- `AGENTS.md` и `docs/gameDocs/rules.md` не менять без отдельного явного запроса с подтверждением.

## 7. Артефакты
- Канонические отчёты `docs/projectDocs/reports/**` — владелец `verifier`, коммит после проверок.
- Handoff — file-first (`docs/projectDocs/reports/handoffs/...`); `.tmp` — только локальный черновик, не транспорт.
- Процессные фрикции/меры → `PI-*`/отчёты (`process-master`); продуктовые/кодовые находки вне scope → `TD-*` (`tech-debt.md`).
