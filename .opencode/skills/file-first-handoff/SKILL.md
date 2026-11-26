---
name: file-first-handoff
description: Use when passing work between roles or writing implementer/scenario-tester handoffs in Kriegsspiel; defines file-first artifact naming, structure, delta rule and log restrictions.
---

# File-first handoff

Передача между ролями опирается на артефакты в ветке/репозитории, а не на inline-текст. В коммуникации с `orchestrator` — только Status Envelope + ref.

## Где хранить
- **Handoff `implementer`:** `docs/projectDocs/reports/handoffs/implementer-T-XX-r<N>.md` (раунд `N`).
- **Другие роли:** `docs/projectDocs/reports/handoffs/<role>-<target>-r<N>.md` по договорённости.
- **Локальный scratch:** `docs/projectDocs/reports/.tmp/<...>.md` — gitignored, не транспорт; не единственный источник для `verifier`.
- Handoff коммитится в собственной ветке запуска (worktree) при необходимости доступности и трассируемости; он не заменяет канонический `task-*`/`wave-*` отчёт.
- Ref handoff всегда называет `worktree` и `branch@commit` запуска (см. `process-contract` §5.1); потребитель читает артефакт по этому ref, не переключая HEAD общего каталога.

## Минимальная структура handoff (implementer)
- Раунд `N`, ветка/коммит/MR, target.
- Закрытые замечания / дельта относительно прошлого раунда.
- Что сделано (по пунктам «Что сделать»).
- Изменённые/созданные файлы (только ownership).
- Проверки: команды/шаги → результат, refs на логи/артефакты (без копипаста логов).
- Статус, `TD-*` предложения.
- `Процесс (трение)`: неоднозначности (refs `FS-*`/`rules §…`/карточка), лишние поиски и причина, оценка затрат `низко/средне/высоко`, предложения.

## Правила
- **Ref-driven (PI-019):** ref handoff обязан содержать `worktree`, `branch@commit` и путь к артефакту; потребитель читает строго по переданному ref. Не разыскивать артефакты перебором чужих веток (`git ls-files`, перебор `git show`); если ref не передан — `blocked`/эскалация через `orchestrator`.
- Полные логи, диффы, длинные разборы и копипасты в ответ оркестратору запрещены.
- На доработках — только дельта (что изменилось, новые результаты, refs).
- Устаревшие handoff можно заменять/удалять по решению в рамках ветки, только если канонический отчёт и refs сохраняют трассируемость.
