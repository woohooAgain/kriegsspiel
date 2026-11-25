# AGENTS.md

Думай на английском, отвечай на русском. Действуй строго в рамках роли из `.opencode/agent/*`; не смешивай роли.

## Источники истины (при расхождении)
1. `docs/gameDocs/rules.md` — канон игры.
2. Этот файл.
3. `.opencode/skills/process-contract` и остальные скиллы.
4. `docs/projectDocs/*` (`instructions.md`, `structure.md`, `todo.md`, `reports/README.md`).
5. `docs/projectDocs/workflows/*`.

Противоречие не разрешай молча — эскалируй через `orchestrator`.

## Где что лежит
- Роли — `.opencode/agent/*`; скиллы — `.opencode/skills/*`; лончеры — `.opencode/command/*`.
- Процедурные детали (ветвление, gate, handoff, отчёты, PI/TD) — в скиллах; не дублируй их здесь.

## Жёсткие правила (safety)
- Не менять `AGENTS.md` и `docs/gameDocs/rules.md` без отдельного запроса с подтверждением.
- `docs/projectDocs/todo.md` правит только `architect` в отдельной ветке по спецзапросу.
- File ownership строгое: только файлы из `Владеет файлами` своей карточки.
- Прямые коммиты в `main` запрещены; merge per-task (`verifier`), batch запрещён.
- Передача между ролями — file-first + Status Envelope, без inline-логов/диффов.
- Новые правила не имеют обратной силы.

## Проверки
- `dotnet build Kriegsspiel.slnx`, `dotnet test Kriegsspiel.slnx` (или `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj`). Нет среды — фиксируй как ограничение, не как успех.
- Код без комментариев, если не просили.
