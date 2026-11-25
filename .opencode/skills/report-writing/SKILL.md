---
name: report-writing
description: Use when finalizing or contributing Kriegsspiel task and wave reports in docs/projectDocs/reports; defines ownership, required sections, and task/wave templates.
---

# Report writing (task- и wave-отчёты)

Каталог: `docs/projectDocs/reports/`.

## Ownership и моменты фиксации
- **Канонический владелец отчётов — `verifier`.** Финализирует и коммитит после прохождения проверок (в ветке волны по процессу).
  - `task-T-XX-<slug>.md` — после per-task проверок (при `ready to merge` — статус/причина, актуализация после фактического merge/пост-merge).
  - `wave-<N>-report.md` — после wave-проверок по сценарию `architect`; включает вклад `process-master`.
- `implementer` предоставляет handoff и не финализирует канонические отчёты.
- `scenario-tester`, `analyst`, `process-master` и др. передают факты/ввод через артефакты и Status Envelope.
- Обратной силы нет: новые разделы обязательны для новых задач/волн; старые отчёты не переписываются.

## Анти-контекст-bloat
В ответах — Status Envelope + refs; подробности (логи, замечания, причины, фрикции, меры) — в отчётах/handoff/`process-improvement-log.md`. На доработках — дельта.

## Входы
- **Task-report:** handoff `implementer`, MR/diff refs, факты `verifier`, сценарий/DoD задачи.
- **Wave-report:** task-отчёты, сценарий проверок волны, ввод `scenario-tester`, итоги merge/post-merge, вклад `process-master`, refs на `PI-*`.

## Шаблон task-report
```md
# Отчёт задачи T-XX — <краткое имя>

- **T-XX:** …
- **Волна / ветка волны:** …
- **Ветка задачи / MR:** … (refs)
- **Раунды:** … (refs на handoff)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** …

## Выполнено (по пунктам «Что сделать»)
## Изменённые/созданные файлы (только ownership)
## Проверки (evidence refs)
## Верификация (verifier)
- Вердикт: verified / changes requested / blocked / ready to merge
- Замечания/раунды (дельта)
- Merge в ветку волны: выполнен / ready to merge
- Post-merge сборка/старт → результат
## Процесс (трение)
## Заметки / tech-debt
```

## Шаблон wave-report
```md
# Отчёт волны Wave N — <название>

- **Дата:** …
- **Ветка волны / target:** … (refs)
- **Сценарий проверок волны (architect):** ref/кратко
- **Финализировал и закоммитил (verifier, после проверок):** …

## Задачи волны
| T-* | Статус | Ветка/MR (refs) | Вердикт | Отчёт задачи | Примечание |

## Параллелизм и ownership
## Post-merge проверки по задачам (в контексте ветки волны)
## Wave-проверка (по сценарию architect, на ветке волны)
## Merge ветки волны в основную
## Ретро процесса (вклад process-master / факты участников)
## Итоги
```

## Процессное трение
Фиксировать: роль, раунд/этап, волна/`T-XX`, итог; неоднозначности (refs); лишние поиски и причину; оценку доп. затрат качественно (`низко/средне/высоко`); предложения/меры. Продуктовые находки вне scope — в `tech-debt.md`, не в процессный лог.
