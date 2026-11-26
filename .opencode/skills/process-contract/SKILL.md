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

## 8. Атрибуция коммитов по роли агента
Цель — по истории git однозначно видеть, какая роль сделала коммит/merge. Правило действует для всех коммитящих ролей: `analyst`, `architect`, `implementer`, `verifier`, `scenario-tester`, `process-master`, `rules-auditor`, `docs-maintainer`. `orchestrator` и `repo-navigator` коммитов не делают.

**Основной способ — identity роли на конкретную команду** (меняет и author, и committer; глобальный/локальный `git config` не менять):
```sh
git -c user.name="<role>" -c user.email="<role>@kriegsspiel.local" commit -m "<message>"
git -c user.name="<role>" -c user.email="<role>@kriegsspiel.local" merge --no-ff <branch> -m "merge: <...>"
```
`<role>` — точное имя роли (`implementer`, `verifier`, ...). Override задаётся только на команду, чтобы роли не перетирали друг друга в общем worktree. Переменные `GIT_AUTHOR_*`/`GIT_COMMITTER_*` имеют приоритет над `-c` — в агентских запусках их не задавать (при наличии — снять/не использовать).

**Резервный способ — trailer `Made-by-Agent: <role>`** в сообщении коммита. Обязателен, когда identity может быть потеряна или переписана: squash, cherry-pick из внешней ветки, ручная правка/amend, коммиты инструментами. Для merge — вторым `-m`:
```sh
git -c user.name="<role>" -c user.email="<role>@kriegsspiel.local" commit -m "<message>" -m "Made-by-Agent: <role>"
git -c user.name="<role>" -c user.email="<role>@kriegsspiel.local" merge --no-ff <branch> -m "merge: <...>" -m "Made-by-Agent: <role>"
```

**Проверка (обязательна при per-task/wave верификации):**
```sh
git log -1 --format='%an <%ae> | %cn <%ce> | %(trailers:key=Made-by-Agent,valueonly)'
git log --format='%ae' <base>..<branch> | sort -u
```
Ожидаемо: email author/committer — `<role>@kriegsspiel.local`; trailer, если есть, называет ту же роль. При merge committer — роль, выполняющая merge, авторство исходных коммитов сохраняется.

**Ограничения:**
- Обратной силы нет: существующие коммиты не переписываются (никаких rebase/amend только ради атрибуции). Коммиты без атрибуции до введения правила — пробел данных, не нарушение.
- Собственный неопубликованный коммит можно исправить до merge; после merge/rebase-публикации историю не переписывать — фиксировать факт в отчёте/handoff.
- Если identity применить нельзя (внешний инструмент/человек), добавить trailer либо зафиксировать ограничение; не догадываться об авторстве.
