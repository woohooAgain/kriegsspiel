# Process-master — внедрение предложений (Round 1)

- **Роль:** `process-master`
- **Раунд:** 1
- **Дата:** 2026-11-26
- **Target:** `docs/projectDocs/reports/handoffs/process-master-proposals.md` (Тема 2, п. 3–4, PI-013…PI-017) + детализирующий handoff `process-master-implementer-permissions-r1.md` (PI-017…PI-021)
- **Base-ref:** `master` @ `284164a` (`Merge branch 'verifier/mvp-transition-gate-r3'`)
- **Собственная ветка:** `process/process-master-proposals-r1`
- **Worktree:** `/tmp/opencode/ks-process-master-proposals-r1`
- **Статус:** `actions_proposed` (часть PI — `applied`, часть — `proposed/blocked`)

## 1. Scope и решения по открытым вопросам

### Внедрено
- **PI-013** — единое дерево маршрутизации заметок.
- **PI-014** — структурированный `featureBacklog` (`BL-*`) и `analyst` как caretaker/groomer.
- **PI-015** — расширение `TD-*` полями `Тип`/`Severity` (без отдельного `BUG-*`).
- **PI-017** — исправлен порядок `permission.bash` во всех bash-ролях (`*: ask` первым).
- **PI-018** — командная гигиена (`workdir` вместо `cd`, без составных цепочек/`| head`).
- **PI-019** — ref-driven доступ к чужим артефактам (без discovery по веткам).
- **PI-020** — проверка permission-детерминизма на свежем процессе (gate-чек).

### Записано как `proposed`/отложено
- **PI-016** — отдельный `bugs.md`/`BUG-*`: отложено до подтверждённого объёма (анти-сложность).
- **PI-021** — политика read-only shell allowlist: требует решения пользователя/orchestrator (варианты A/B).
- **PI-022** — `todo.md`: индекс волн + отдельные файлы детализации. **Blocked:** правит только `architect`; `todo.md` не менялся.

### Q1–Q6 (из `process-master-proposals-r1.md`)
- **Q4 (notes):** реализовано — `analyst` закреплён caretaker/groomer `BL-*`, баги/доработки идут в расширенный `TD-*` (PI-014/PI-015).
- **Q5 (naming):** зафиксирован факт «default branch — `master`»; глобальная канонизация терминологии (`main` vs `master`) **не выполнялась** (потребовала бы правок `AGENTS.md`/сквозной терминологии; PI-010 зарезервирован).
- **Q1/Q2/Q3/Q6 (merge-контур, inventory веток, EOL-шум):** **не входят** в именованный handoff (`process-master-proposals.md`) и требуют решения человека; не внедрялись. Номера PI-006…PI-012 зарезервированы в журнале.

### Явно исключено
- **LSP для C#** — по прямому указанию пользователя («не подключай lsp, я сделаю сам»); `opencode.json` не менялся.

## 2. Что сделано (по файлам)

- `docs/projectDocs/process-improvement-log.md` — добавлены PI-013…PI-022, блок «Зарезервированные номера» (PI-006…PI-012).
- `docs/projectDocs/workflows/README.md` — раздел «Маршрутизация заметок» (таблица + дерево решений), обновлены принципы (BL/TD/PI, командная гигиена и refs).
- `docs/gameDocs/featureBacklog.md` — раздел «Владелец и формат новых записей (`BL-*`)»; старые пункты 1..31 не переписывались.
- `docs/gameDocs/featureSpecs/README.md` — источник `BL-*`, обратная ссылка `→ FS-*`, маршрутизация.
- `docs/projectDocs/tech-debt.md` — типы `bug|improvement|debt`, `Severity`, обновлённые владельцы/шаблон.
- `.opencode/skills/tech-debt-intake/SKILL.md` — типы/severity, маршрутизация, владельцы, шаблон.
- `.opencode/skills/feature-spec/SKILL.md` — связь `BL-* ↔ FS-*`, caretaker `analyst`.
- `.opencode/skills/process-contract/SKILL.md` — §5.1 ref-driven, §6 командная гигиена + порядок permission, §7 ref-driven и маршрутизация PI/TD/BL.
- `.opencode/skills/file-first-handoff/SKILL.md` — обязательный `worktree`+`branch@commit`+путь, запрет discovery.
- `.opencode/skills/gate-checklist/SKILL.md` — п.8 permission-детерминизм.
- `.opencode/agent/{analyst,architect,docs-maintainer,implementer,process-master,rules-auditor,scenario-tester,verifier}.md` — порядок `permission.bash` (`*: ask` первым) + краткая инструкция по командной гигиене/refs; `implementer` — запрет discovery чужих веток.
- `.opencode/agent/orchestrator.md` — требование передавать точные refs (`worktree`, `branch@commit`, путь), не ожидать discovery от субагентов.

## 3. Проверки (evidence refs)

- `git diff --check` — без whitespace-ошибок.
- `opencode debug agent <role>` (все 8 bash-ролей) — порядок разрешён: `* -> ask`, затем конкретные allow:
  - `analyst/architect/implementer`: `* -> ask; git * -> allow; dotnet * -> allow`
  - `docs-maintainer/process-master/rules-auditor`: `* -> ask; git * -> allow`
  - `scenario-tester`: `* -> ask; dotnet * -> allow; git * -> allow; script * -> allow`
  - `verifier`: `* -> ask; git * -> allow; dotnet * -> allow; script * -> allow`
- `dotnet build Kriegsspiel.slnx` — **успешно** (0 warnings, 0 errors).
- `dotnet test Kriegsspiel.slnx --no-build` — **148 passed, 0 failed, 0 skipped**. Изменения только в markdown/YAML-промптах, продуктовый код не менялся.

## 4. Пробелы данных / ограничения

- Эффект PI-017/PI-020 воспроизводим только на **свежем процессе** opencode (runtime-approvals текущей сессии могут маскировать результат); в этом запуске перезапуск не выполнялся.
- PI-006…PI-012 (merge-контур/EOL) не анализировались заново — это вход `process-master-proposals-r1.md` вне именованного scope; решения Q1–Q3/Q6 не принимались.
- `todo.md` не изменялся (PI-022 blocked: только `architect`).
- LSP исключён по решению пользователя.
- Сторонний строгий YAML-линтер отмечает pre-existing незакавыченное двоеточие в `description` у `.opencode/agent/rules-auditor.md`; файл не менялся по этому полю, `opencode debug agent rules-auditor` резолвит роль корректно (не блокер, наблюдение).
- Исходные handoff-файлы (`process-master-proposals.md`, `process-master-proposals-r1.md`, `process-master-implementer-permissions-r1.md`) на момент запуска были **untracked** в primary checkout и не включены в эту ветку (это вход, не ownership). Трассируемость до merge — по их путям в primary checkout; после merge refs в журнале указывают на входные handoff (пробел данных, как в PI-001).

## 5. Следующие шаги

- `verifier`/`orchestrator`: проверить ветку `process/process-master-proposals-r1`, при необходимости — merge (правила `process-contract` §5/§5.1).
- `architect`: рассмотреть PI-022 (структура `todo.md`) отдельным спецзапуском.
- Пользователь/orchestrator: решение по PI-021 (политика read-only allowlist) и Q1–Q3/Q6 (если потребуются).
