# Промпт: приём находки в техдолг (любая роль)

Роль: любая (`analyst`, `implementer`, `verifier`, `scenario-tester`, `rules-auditor`, `architect`, `process-master`, `orchestrator` — через запуск соответствующего агента; сам `orchestrator` не читает файлы и не редактирует).

## Когда использовать
Найдена проблема/риск **вне текущей задачи/объёма**, которую нельзя или нецелесообразно править сейчас.

## Обязательные действия
1. Самостоятельно проверить дубликаты в `docs/projectDocs/tech-debt.md` (активные `open`/`planned`) и в `docs/projectDocs/tech-debt-archive.md`.
2. Подготовить запись `TD-###` по шаблону: статус `open`, дата, контекст (`T-*`/`FS-*`/wave/triage), источник (файлы/символы/команды/refs), описание, риск, предлагаемые действия, связи (`docs/gameDocs/rules.md §…`, `T-*`, `FS-*`).
3. При закрытии: `done` (или `wontfix` с причиной) + ссылки на `T-*`, MR/коммит, дата/волна. Активный список — в основном `open`/`planned`; `done`/`wontfix` — по политике архивации/очистки.
4. **Разграничение:** процессные фрикции/меры — в отчёты/`docs/projectDocs/process-improvement-log.md` (`PI-*`), не в `TD-*`. Продуктовые/кодовые/док-находки вне scope — в `TD-*`.
5. `todo.md` не править (кроме `architect` в отдельной ветке по специальному запросу).
6. Детали — file-first (правка `tech-debt.md`/артефакт). Оркестратору, если применимо, — только Status Envelope + refs.

## Выход
- Изменённый `docs/projectDocs/tech-debt.md` (и/или архив) или подготовленный блок/патч по refs.
- Status Envelope (кратко): `ROLE=<role> | TARGET=TD-### | STATUS=<recorded|updated|duplicate|blocked> | ARTIFACTS=<refs> | NEXT=<none|architect|orchestrator> | BLOCKERS=... | SUMMARY=...`
