# Журнал улучшений процесса (process-improvement-log)

Сквозной журнал процессных мер. Владелец: `process-master`. Назначение: фиксировать проблемы процесса, меры, критерии проверки эффекта и итог по волнам, чтобы не полагаться только на память/перечитывание старых отчётов и вовремя выявлять регрессии и циклы.

## Правила
- **Обратной силы нет:** журнал ведётся для новых волн/мер после введения процесса; прошлые волны не пересобираются. При анализе истории использовать доступные `wave-*`/`task-*` отчёты и явно отмечать пробелы в данных (что не фиксировалось ранее).
- **Не путать с техдолгом:** продуктовые/кодовые находки — в `docs/projectDocs/tech-debt.md` (`TD-*`), процессные проблемы/меры — здесь (`PI-*`) и в отчётах.
- **Детерминизм vs сложность:** каждая мера должна иметь проверяемый критерий эффекта и оценку влияния на сложность процесса. Меры, повышающие бюрократию без эффекта, упрощаются или откатываются.
- **Циклы:** если новая мера вернула проблему, устранённую ранее, фиксировать как `regressed/cycle` со ссылкой на исходную запись и волну проявления.

## Формат записи (`PI-*`)
```md
## PI-### — <краткое имя>
- **Статус:** proposed | applied | verified | regressed | rolled-back | wontfix
- **Волны/контекст:** Wave N, <T-* / refs>
- **Проблема (факты):** …
- **Причина/гипотеза:** …
- **Мера:** … (что изменено/предложено; ссылки на файлы процесса/шаблонов/промптов)
- **Критерий проверки эффекта:** … (что наблюдаем в следующих волнах)
- **Влияние на сложность:** низко/средне/высоко (+ обоснование)
- **Результат/наблюдение:** … (волна, факт; «недостаточно данных» — допустимо с пояснением)
- **Ссылки:** `wave-*-report.md`, task-отчёты, коммиты/MR/ветки, связанные PI-*
```

## Записи

<!-- Новые записи добавляются сверху или по порядку PI-### (не переиспользовать номера). -->

## PI-001 — Gate по актуальности базы плана и ownership перед стартом волны
- **Статус:** applied
- **Волны/контекст:** Wave 4 / FS-005, T-24, T-25; refs: Status Envelope (gate C r1 `gate_failed`, r2 `gate_passed` после rebase), commit `1156fba`, `docs/projectDocs/reports/wave-3-report.md` (контекст процесса).
- **Проблема (факты):** Первоначальная декомпозиция Wave 4 была в старой arch-ветке от базы `501262a`; к моменту gate в `master` уже были `3546794`, `8168b08` и `Content/scenarios/river_line_10x10.json`. Gate C вернул `gate_failed` из-за рассинхрона базы; потребовался rebase плана на `master@8168b08`, добавление `river_line_10x10.json` в масштабную проверку и `structure.md` в ownership T-25.
- **Причина/гипотеза:** Gate проверял формальную проверяемость плана, но не фиксировал явный precondition «база плана == актуальная целевая база волны» и полноту ownership относительно файлов, появившихся в базе после декомпозиции.
- **Мера:** В gate/checklist волны добавить обязательный шаг: сверить базовый commit плана с целевой базой, перечислить новые файлы/артефакты базы, затрагивающие сценарий/ownership, и подтвердить, что они отражены в плане/ownership/проверках. При расхождении — `gate_failed` до создания ветки волны.
- **Критерий проверки эффекта:** В следующих волнах нет повторного `gate_failed`/rebase плана по причине устаревшей базы или пропущенного файла в ownership; при наличии дрейфа базы он ловится на первом gate.
- **Влияние на сложность:** низко (короткая checklist-проверка с refs на base commit и diff имён файлов).
- **Результат/наблюдение:** Wave 4 — мера предложена после факта; прямые gate-артефакты r1/r2 в репозитории не найдены, факт подтверждён Status Envelope (пробел данных). Мера встроена в `gate-checklist` п.3; эффект в следующей волне — наблюдение (после Wave 4 данных нет).
- **Ссылки:** `1156fba`, `8168b08`, `docs/projectDocs/todo.md` (Wave 4/FS-005), `/tmp/opencode/process-master-wave4-input.md`.

## PI-002 — Проверка UI-пути ввода/меню, а не только модели
- **Статус:** applied
- **Волны/контекст:** Wave 4 / T-24; refs: `docs/projectDocs/reports/task-T-24-scalable-knowledge-map.md`, `docs/projectDocs/reports/handoffs/implementer-T-24-r1.md`, `implementer-T-24-r2.md`.
- **Проблема (факты):** В r1 фильтр «Синие по донесениям» сбрасывался к «все зоны» из-за опечатки строки в `MapCommand` (`Синые/Синие`, `MapCommand.cs:162` по отчёту verifier r2). Probe/model-level проверки проходили, дефект проявился только в реальном CLI-пути выбора меню; verifier вернул `changes_requested`, implementer исправил в r2.
- **Причина/гипотеза:** Проверки концентрировались на модели/рендеринге и не включали воспроизводимый прогон конкретного пункта меню/ввода пользователя.
- **Мера:** Для UI/CLI-задач в `Как проверить`/handoff требовать минимум один воспроизводимый прогон каждого нового/изменённого интерактивного пути (конкретный пункт меню/ввод → наблюдаемый результат) отдельным evidence ref; model/probe-проверки не заменяют input-path.
- **Критерий проверки эффекта:** В следующих волнах дефекты класса «опечатка/расхождение строки меню и ветки обработки» ловятся implementer/verifier в r1, без отдельного `changes_requested` по тому же пути.
- **Влияние на сложность:** низко/средне (1-несколько коротких CLI/TTY-прогонов на изменённый путь; в non-TTY среде нужен script/pseudotty).
- **Результат/наблюдение:** Wave 4 — исправлено в r2, post-merge CLI-фильтр подтверждён. Мера встроена в `scenario-evidence` (раздел TTY/Spectre); эффект в следующей волне — наблюдение.
- **Ссылки:** `docs/projectDocs/reports/task-T-24-scalable-knowledge-map.md`, `docs/projectDocs/reports/handoffs/implementer-T-24-r2.md`, `9f6c82a`, `e369af2`.

## PI-003 — Трассируемость ручного merge при отсутствии remote/gh
- **Статус:** observed
- **Волны/контекст:** Wave 4 / T-24, T-25; refs: task-отчёты T-24/T-25, Status Envelope (нет remote/gh, merge verifier вручную).
- **Проблема (факты):** MR не создавались из-за отсутствия remote/gh; merge выполнял verifier вручную (`--no-ff`) в ветку волны. Трассируемость сохранена через commit refs, handoff и task-отчёты, но отсутствует внешний MR/CI-артефакт.
- **Причина/гипотеза:** Ограничение окружения/репозитория (локальный worktree без remote/gh), а не процессный выбор.
- **Мера:** При отсутствии remote/gh считать обязательным evidence-пакет: ref ветки/commit, handoff, `verifier` task-report с вердиктом, команда/факт merge, post-merge build/test refs. Не маркировать как «MR merged», если MR не существует.
- **Критерий проверки эффекта:** В средах без remote/gh каждый merge имеет полный локальный evidence-пакет; при появлении remote/gh — возврат к обычному MR-потоку без потери дисциплины.
- **Влияние на сложность:** низко (фиксация уже существующих refs в отчёте).
- **Результат/наблюдение:** Wave 4 — наблюдалось; дисциплина evidence в T-24/T-25 выдержана. Данных CI/MR нет по причине окружения (пробел данных, не регрессия).
- **Ссылки:** `docs/projectDocs/reports/task-T-24-scalable-knowledge-map.md`, `docs/projectDocs/reports/task-T-25-fs005-scale-demo.md`, `e369af2`, `3e1ba9e`.

## PI-004 — Атрибуция коммитов по роли агента
- **Статус:** applied
- **Волны/контекст:** вне волны; процессная правка по запросу orchestrator (ветка `process/commit-agent-attribution-r1`).
- **Проблема (факты):** история git не фиксировала, какая роль агента сделала конкретный коммит/merge; identity бралась из общего `git config`, роли смешивались. Аудит «кто что менял» требовал догадок по путям/отчётам (пробел трассируемости процесса).
- **Причина/гипотеза:** у процесса не было обязательного механизма атрибуции; роли использовали локальную/глобальную identity без role-разметки.
- **Мера:** `process-contract` §8: основной способ — per-command identity `git -c user.name=<role> -c user.email=<role>@kriegsspiel.local` для commit/merge; резерв — trailer `Made-by-Agent: <role>` при риске потери identity (squash/cherry-pick/amend/инструменты). Роли-коммиттеры ссылаются на §8; `verifier` сверяет атрибуцию при per-task/wave проверке. Обновлены `.opencode/skills/process-contract`, агенты `analyst/architect/implementer/verifier/scenario-tester/process-master/rules-auditor/docs-maintainer`, `workflows/*`, `instructions.md`.
- **Критерий проверки эффекта:** в новых коммитах `git log` даёт email роли (`<role>@kriegsspiel.local`), для риск-коммитов — совпадающий `Made-by-Agent`; `verifier` не находит неатрибутированных коммитов в ветках задач/волн; не появляется ручных переписываний истории ради атрибуции.
- **Влияние на сложность:** низко (одна опция в команде коммита/merge + опциональный trailer; проверка одной командой `git log`).
- **Результат/наблюдение:** мера внедрена в процессные документы; эффект в следующей волне — наблюдение (данных применения ещё нет). Обратной силы нет: старые коммиты не переписываются.
- **Ссылки:** `.opencode/skills/process-contract/SKILL.md` §8, `docs/projectDocs/workflows/README.md`, `docs/projectDocs/workflows/task-lifecycle.md`, `docs/projectDocs/instructions.md`, ветка `process/commit-agent-attribution-r1`.

## PI-005 — Изоляция запусков агентов через отдельный git worktree
- **Статус:** applied
- **Волны/контекст:** вне волны; процессная правка по запросу (ветка `process/agent-worktrees-r1`).
- **Проблема (факты):** внешний `git checkout` в общем каталоге переключал HEAD во время работы; коммит `adb8ba4` (docs/rules) оказался в истории чужой ветки `process/commit-agent-attribution` (reflog: HEAD переключён `docs/rules-simplified-mvp` → `process/commit-agent-attribution` перед этим коммитом). `git branch --contains adb8ba4` подтверждает присутствие в `process/commit-agent-attribution` и `docs/rules-simplified-mvp`. Коллизия повторяющаяся; атрибуция §8 её не предотвращает (она про авторство, не про ветку).
- **Причина/гипотеза:** агенты работали/коммитили в общем primary checkout, у запуска не было собственной ветки/worktree; HEAD менялся внешними checkout'ами, и коммит попадал в текущую (чужую) ветку.
- **Мера:** `process-contract` §5.1 — обязательный linked worktree на запуск + собственная ветка, запрет checkout/commit в общем каталоге, проверяемая идентификация «своей» ветки, жизненный цикл worktree, handoff refs `worktree`/`branch@commit`. Ссылки на §5.1 в `.opencode/agent/*`, `file-first-handoff`, `workflows/*`, `instructions.md`; `orchestrator` требует эти refs. Имена веток и §8 сохранены; `.gitignore` игнорирует in-repo `.worktrees/`.
- **Критерий проверки эффекта:** ни один коммит нового запуска не попадает в ветку, которую запуск не создавал/не получил ref-ом; в Status Envelope есть `worktree` + `branch@commit`; `git worktree list` показывает одну ветку на worktree; в primary checkout нет checkout/commit агентов; `verifier` не находит кросс-веточных коммитов.
- **Влияние на сложность:** средне (одна команда `git worktree add/remove` на запуск + 2 refs в отчёте; убирает ручные переключения и разбор коллизий).
- **Результат/наблюдение:** мера внедрена в процессные документы; эффект в следующей волне — наблюдение (данных применения ещё нет). Обратной силы нет: `adb8ba4`/история не переписываются, старые кросс-веточные коммиты — исторический факт.
- **Ссылки:** `.opencode/skills/process-contract/SKILL.md` §5.1, `.opencode/skills/file-first-handoff/SKILL.md`, `.opencode/agent/*`, `docs/projectDocs/workflows/README.md`, `docs/projectDocs/workflows/task-lifecycle.md`, `docs/projectDocs/instructions.md`, `adb8ba4`, ветка `process/agent-worktrees-r1`, PI-004.

## PI-013 — Единое дерево маршрутизации заметок (feature vs TD-bug/improvement vs PI)
- **Статус:** applied
- **Волны/контекст:** вне волны; источник — handoff `docs/projectDocs/reports/handoffs/process-master-proposals.md` (Тема 2); refs: `featureBacklog.md`, `tech-debt.md`, `process-improvement-log.md`, `feature-spec`, `tech-debt-intake`, `process-retro`.
- **Проблема (факты):** нет явного правила/intake, куда писать быстрые заметки; риск дублирования и смешения `PI-*`/`TD-*`/`BL-*`.
- **Причина/гипотеза:** разграничение описано по артефактам, но нет единого decision tree/intake.
- **Мера:** дерево решений и таблица маршрутизации зафиксированы в `docs/projectDocs/workflows/README.md` (раздел «Маршрутизация заметок»); ссылки/уточнения — в `feature-spec`, `tech-debt-intake`, `featureBacklog.md`, `featureSpecs/README.md`.
- **Критерий проверки эффекта:** новые заметки попадают в один «дом»; нет дублей PI↔TD↔BL; роли не спрашивают «куда писать».
- **Влияние на сложность:** низко (дерево + ссылки).
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект в следующих волнах — наблюдение (данных применения ещё нет).
- **Ссылки:** `docs/projectDocs/workflows/README.md` (intake), `docs/gameDocs/featureBacklog.md`, `docs/gameDocs/featureSpecs/README.md`, `.opencode/skills/feature-spec/SKILL.md`, `.opencode/skills/tech-debt-intake/SKILL.md`, PI-014, PI-015.

## PI-014 — Структурировать featureBacklog (`BL-*`) и закрепить analyst как groomer
- **Статус:** applied
- **Волны/контекст:** вне волны; источник — handoff `process-master-proposals.md` (2.4.1/2.5); refs: `featureBacklog.md` (плоский список 1..31), `featureSpecs/README.md`.
- **Проблема (факты):** backlog без ID/статуса/типа/источника/связей → дубли и потерянные связи с `FS-*`.
- **Причина/гипотеза:** исторический формат «просто список».
- **Мера:** новые записи — `BL-###` по шаблону (тип/статус/дата-источник/ценность/заметки/связи); старые пункты 1..31 не переписываются (обратной силы нет); явно указан `analyst` как caretaker/groomer, `architect` потребляет фичи через `FS-*`; связь `BL-* ↔ FS-*` зафиксирована в `featureSpecs/README.md` и `feature-spec`.
- **Критерий проверки эффекта:** новые идеи имеют `BL-*` и статус; `FS-*`, выросшие из backlog, имеют обратную ссылку на `BL-*`; нет дублирования тел.
- **Влияние на сложность:** низко/средне (шаблон + дисциплина ссылок).
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект — наблюдение.
- **Ссылки:** `docs/gameDocs/featureBacklog.md`, `docs/gameDocs/featureSpecs/README.md`, `.opencode/skills/feature-spec/SKILL.md`, PI-013.

## PI-015 — Расширить `TD-*` полями Тип/Severity вместо нового bug-трекера
- **Статус:** applied
- **Волны/контекст:** вне волны; источник — handoff `process-master-proposals.md` (2.4.2); refs: `tech-debt.md` (активных записей нет), `tech-debt-intake`.
- **Проблема (факты):** баги/доработки существующей реализации не имеют явного дома; `TD-*` семантически «долг/риск».
- **Причина/гипотеза:** `TD-*` создавался как «находки вне задачи», без подтипов.
- **Мера:** добавлены поля `Тип: bug|improvement|debt` и `Severity: low|medium|high` (для `bug`); шаблон и владельцы обновлены в `docs/projectDocs/tech-debt.md` и `.opencode/skills/tech-debt-intake/SKILL.md`; отдельный `BUG-*`-трекер не вводится, пока объём мал.
- **Критерий проверки эффекта:** замечания по текущей реализации фиксируются как `TD-*` с типом; `architect` конвертирует в `T-*`; нет параллельного трекера.
- **Влияние на сложность:** низко (2 поля).
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект — наблюдение.
- **Ссылки:** `docs/projectDocs/tech-debt.md`, `.opencode/skills/tech-debt-intake/SKILL.md`, PI-013, PI-016.

## PI-016 — Выделить `bugs.md`/`BUG-*` при росте объёма
- **Статус:** proposed (отложено)
- **Волны/контекст:** вне волны; источник — handoff `process-master-proposals.md` (опциональный пункт).
- **Проблема (факты):** потенциальный рост числа `TD-*` с `Тип: bug` может потребовать отдельного triage/severity-борда.
- **Причина/гипотеза:** преждевременное выделение трекера повышает сложность и дублирование.
- **Мера:** не вводить `docs/projectDocs/bugs.md`/`BUG-*` сейчас; вернуться к вопросу при подтверждённом объёме багов (анти-сложность).
- **Критерий проверки эффекта:** при росте объёма `bug` появляется отдельное решение/артефакт; до этого нет параллельного трекера.
- **Влияние на сложность:** средне/высоко — поэтому отложено.
- **Результат/наблюдение:** отложено до подтверждённой потребности; активных `TD-*`-багов нет (пробел данных о будущем объёме).
- **Ссылки:** PI-015, `docs/projectDocs/tech-debt.md`.

## PI-017 — Исправить порядок `permission.bash` во всех агентах (last-match-wins)
- **Статус:** applied
- **Волны/контекст:** Wave 5 / T-53; фактический permission-запрос `implementer`; источник — handoff `process-master-proposals.md` и `process-master-implementer-permissions-r1.md`; общая конфигурация ролей.
- **Проблема (факты):** `"*": ask` стоял после `"git *": allow`/`"dotnet *": allow`; opencode выбирает последнее совпавшее правило, поэтому allow-правила не работали. `git ls-files` → ask, `git -C ... log` → ask, `git log`/`git branch`/`git show` → ask; разрешённые `head *`, `echo *`, `git show *`, `git worktree *` — runtime-approvals, не конфиг.
- **Причина/гипотеза:** неверный порядок правил относительно семантики opencode «last matching rule wins»; широкое правило должно идти первым.
- **Мера:** во всех `.opencode/agent/*` с `bash`-правилами `"*": ask` поставлен первым, затем `git *`/`dotnet *`/`script *`; правило и проверка зафиксированы в `process-contract` §6 и `gate-checklist` п.8.
- **Критерий проверки эффекта:** на свежем процессе (без approvals) `implementer` выполняет `git status`, `git show <ref>:<path>`, `dotnet build` без `message=asking`; в логе `action.pattern="git *"`/`"dotnet *"` + `action=allow`; `opencode debug agent <role>` показывает `*: ask` до конкретных allow.
- **Влияние на сложность:** низко (конфиг, 8 файлов, без кода). Детерминизм: высокий положительный эффект; убирает зависимость от накопленных approvals.
- **Результат/наблюдение:** конфиг исправлен в ветке `process/process-master-proposals-r1`; эффект требует проверки на свежем процессе/после сброса approvals (в текущем процессе approvals могут маскировать результат). Обратной силы нет — старые сессии/approvals не переписываются.
- **Ссылки:** `.opencode/agent/{analyst,architect,docs-maintainer,implementer,process-master,rules-auditor,scenario-tester,verifier}.md`; `.opencode/skills/process-contract/SKILL.md` §6; `.opencode/skills/gate-checklist/SKILL.md`; PI-018, PI-020.

## PI-018 — Командная гигиена: `workdir` вместо `cd`, без составных цепочек
- **Статус:** applied
- **Волны/контекст:** Wave 5 / T-53; команда с `cd ... && ... | head ...`; источник — `process-master-implementer-permissions-r1.md`.
- **Проблема (факты):** `implementer` использовал `cd`, `&&`, пайпы и shell-чтение (`git ls-files | head`, `git show | head`, `echo`), хотя bash-инструмент поддерживает `workdir`, а read/grep/glob уже разрешены; один непокрытый префикс вызывает ask на всю команду.
- **Причина/гипотеза:** привычка к shell-однострочникам; в ролевых промптах не было явного правила.
- **Мера:** в `process-contract` §6 добавлено правило командной гигиены; краткая инструкция добавлена в `.opencode/agent/*` (bash-роли), `workflows/README.md`; `implementer` отдельно запрещено искать артефакты по чужим веткам.
- **Критерий проверки эффекта:** в handoff/логах нет `cd`; составные команды — только моноцелевые; permission-запросы по `cd`/`head`/`echo` не появляются на свежем процессе (при выбранной политике allowlist).
- **Влияние на сложность:** низко (текст роли/процесса); детерминизм: средне-высокий, зависит от соблюдения модели.
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект — наблюдение.
- **Ссылки:** `.opencode/skills/process-contract/SKILL.md` §6, `.opencode/agent/*`, `docs/projectDocs/workflows/README.md`, PI-017.

## PI-019 — Ref-driven доступ к чужим артефактам (без поиска по веткам)
- **Статус:** applied
- **Волны/контекст:** Wave 5 / T-53; `implementer` читал `verifier/...` через `git show`, искал файлы через `git ls-files`/`ls`; источник — `process-master-implementer-permissions-r1.md`.
- **Проблема (факты):** `implementer` самостоятельно разыскивал handoff `verifier` в чужой ветке. Чтение read-only легально по §5.1, но discovery-подход недетерминирован (можно прочитать устаревший/не тот ref), тратит контекст и добавляет permission-запросы.
- **Причина/гипотеза:** `orchestrator` не передал точный ref (или артефакт не был доступен в ветке волны/базе); в ролевом промпте не было правила «чужие refs — только по переданному ref».
- **Мера:** `process-contract` §5.1/§7 и `file-first-handoff` закрепляют ref-driven (`worktree`, `branch@commit`, путь; нет ref ⇒ `blocked`); `orchestrator.md` требует передавать точные refs; `implementer.md` и другие роли получили явную инструкцию не заниматься discovery.
- **Критерий проверки эффекта:** implementer-запуски не содержат `git show` по веткам, не переданным в refs; все чтения чужих артефактов идут по `branch@commit`; при отсутствии ref — `blocked`, а не перебор веток.
- **Влияние на сложность:** низко/средне (дисциплина orchestrator + текст ролей); детерминизм: высокий положительный эффект; риск — чуть больше `blocked` при неполных refs.
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект — наблюдение.
- **Ссылки:** `.opencode/skills/process-contract/SKILL.md` §5.1/§7, `.opencode/skills/file-first-handoff/SKILL.md`, `.opencode/agent/orchestrator.md`, `.opencode/agent/implementer.md`, PI-017.

## PI-020 — Проверка permission-детерминизма на свежем процессе
- **Статус:** applied
- **Волны/контекст:** общая harness-конфигурация; источник — `process-master-implementer-permissions-r1.md`; обнаружено, что «работает» часто означает «кто-то ранее нажал Allow always».
- **Проблема (факты):** часть команд разрешена runtime-approvals (`head *`, `echo *`, `git show *`, `git worktree *`), а не конфигом; в БД `permission` 0 строк; после перезапуска/на другой машине поведение меняется. Проверки «команда прошла» не доказывают корректность конфига.
- **Причина/гипотеза:** approvals маскируют ошибку порядка правил; нет шага проверки resolved permissions и свежего процесса.
- **Мера:** в `gate-checklist` добавлен п.8 (permission-детерминизм, ref на `opencode debug agent <role>`, smoke на свежем процессе); правило отражено в `process-contract` §6.
- **Критерий проверки эффекта:** в gate/ревью есть ref на вывод `opencode debug agent`; permission-изменения не принимаются по принципу «у меня прошло»; регрессия порядка ловится до merge.
- **Влияние на сложность:** низко (один чек + ref); детерминизм: высокий положительный эффект.
- **Результат/наблюдение:** применено в ветке `process/process-master-proposals-r1`; эффект — наблюдение на следующем gate/wave.
- **Ссылки:** `.opencode/skills/gate-checklist/SKILL.md`, `.opencode/skills/process-contract/SKILL.md` §6, PI-017.

## PI-021 — Политика read-only shell allowlist vs специализированные инструменты
- **Статус:** proposed (требует решения)
- **Волны/контекст:** после исправления PI-017 остаются ask на `cat`/`head`/`echo`/`ls`/`cd`; источник — `process-master-implementer-permissions-r1.md`.
- **Проблема (факты):** harness уже имеет allow для `read`/`grep`/`glob`/`list`, но роли продолжают использовать shell-чтение; каждый новый префикс требует approval.
- **Причина/гипотеза:** нет явной политики, что считать допустимым shell-чтением; нет единого allowlist.
- **Мера (не внедрена):** выбрать и зафиксировать один вариант: (A) только специализированные инструменты, shell — для git/dotnet/script; (B) узкий read-only allowlist (`ls`, `cat`, `head`, `tail`, `sed -n`, `grep`, `rg`, `wc`, `pwd`, `cd`) после `*: ask`. Запретить `find -exec`, `xargs`, `bash -c`, `eval`, `python -c` без отдельного анализа.
- **Критерий проверки эффекта:** одинаковый набор разрешённых read-only команд во всех ролях; отсутствие «случайных» approvals; новые роли не требуют ручных подтверждений для чтения.
- **Влияние на сложность:** средне (политика в нескольких файлах); детерминизм: высокий при варианте B, зависит от дисциплины при A.
- **Результат/наблюдение:** не внедрено — требуется решение пользователя/orchestrator по политике безопасности; PI-018 временно закрывает риск дисциплиной.
- **Ссылки:** PI-017, PI-018; `.opencode/agent/*.md`; документация opencode permissions.

## PI-022 — `todo.md`: индекс волн + отдельные файлы детализации
- **Статус:** proposed (blocked: только `architect` правит `todo.md`)
- **Волны/контекст:** вне волны; источник — дополнительное уточнение в handoff `process-master-proposals.md`.
- **Проблема (факты):** `todo.md` не очищается от выполненных задач и накапливает слишком объёмные карточки; план трудно читать.
- **Причина/гипотеза:** детализация задач/волн хранится в том же файле, что и индекс плана.
- **Мера (предложение):** держать в `todo.md` список задач по волнам (короткое имя, короткий summary волны, статус), а детализацию волны — в отдельном файле (путь/формат согласовать с `architect`). Правки `todo.md` — только `architect` в отдельной ветке по спецзапросу.
- **Критерий проверки эффекта:** `todo.md` не накапливает завершённые задачи; волна имеет короткий summary и ссылку на файл детализации; трассируемость `T-*`/волн сохранена.
- **Влияние на сложность:** средне (новая структура плана + дисциплина ссылок).
- **Результат/наблюдение:** не внедрено — вне полномочий `process-master` (нужен отдельный запуск `architect`); блокер зафиксирован.
- **Ссылки:** `docs/projectDocs/todo.md` (read-only для process-master), `.opencode/agent/architect.md`, `process-contract` §4.

## PI-023 — LSP-first навигация по коду в инструкциях ролей
- **Статус:** applied
- **Волны/контекст:** вне волны; harness-инструкции для субагентов, работающих с кодом; ветка `process/agent-lsp-instructions-r1`.
- **Проблема (факты):** LSP в проекте подключён и работает, но ролевые карточки/скиллы не задавали порядок навигации; для поиска C#-символов оставались `grep`/`glob`/`read`, что даёт перебор файлов, лишний контекст и недетерминированный результат вместо точных `file:line`/diagnostics.
- **Причина/гипотеза:** правило отсутствовало в каноническом процессе; роли не имели короткой ссылки на LSP-first, а каждая карточка отдельно описывала только гигиену `read`/`grep`/`glob`.
- **Мера:** новый скилл `.opencode/skills/code-navigation-lsp/SKILL.md` (LSP-first порядок, fallback для docs/config, достаточный результат, запрет угадывания/перебора); короткая ссылка в `.opencode/skills/process-contract/SKILL.md` §6 и карточках `implementer`/`verifier`/`scenario-tester`/`repo-navigator`; лончеры `implementer-take-task`/`verifier-review`/`scenario-tester-run` загружают скилл и фиксируют LSP-first в промпте.
- **Критерий проверки эффекта:** в новых запусках поиск C#-символов начинается с LSP (definition/references/symbols) и даёт `file:line` (+ diagnostics при правках); `grep`/`glob` используются для docs/config или как fallback; нет перебора файлов/`git ls-files` при доступном LSP; при недоступном LSP ограничение фиксируется явно.
- **Влияние на сложность:** низко (один короткий скилл + ссылки; без новых статусов/permission-правил; формат Status Envelope не меняется).
- **Результат/наблюдение:** применено в ветке `process/agent-lsp-instructions-r1`; эффект в следующих запусках — наблюдение (данных применения ещё нет). **Обратной силы нет:** старые артефакты/инструкции не переписываются, правило действует с момента изменения.
- **Ссылки:** `.opencode/skills/code-navigation-lsp/SKILL.md`, `.opencode/skills/process-contract/SKILL.md` §6, `.opencode/agent/{implementer,verifier,scenario-tester,repo-navigator}.md`, `.opencode/command/{implementer-take-task,verifier-review,scenario-tester-run}.md`.

## Зарезервированные номера (не переиспользовать)
- **PI-006 … PI-012** — кандидаты из `docs/projectDocs/reports/handoffs/process-master-proposals-r1.md` (merge-контур: владелец merge/no-orphan-branch, tighten `ready_to_merge`, inventory веток, gate-остатки, `main`/`master`, merge vs push; EOL-шум). В этом запуске **не внедрялись**: требуют решений Q1–Q3/Q6 (человек/orchestrator) и/или отдельного scope. Номера зарезервированы, чтобы не переиспользовать; обратной силы нет.
