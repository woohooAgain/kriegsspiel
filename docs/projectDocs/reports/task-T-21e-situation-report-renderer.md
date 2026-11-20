# Отчёт задачи T-21e — Situation and report renderer

- **T-XX:** `T-21e`
- **Волна / ветка волны:** Wave 3 / `wave/3-cli-ui-cleanup`
- **Ветка задачи / MR:** `task/T-21e-situation-report-renderer` (`f10f999`), merge `2ca6d77` → ветка волны
- **Раунды:** 1 (`docs/projectDocs/reports/handoffs/implementer-T-21e-r1.md`)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент, 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- `ReportRenderer` обновлён: вывод всех 4 обязательных полей донесения (минута события, минута доставки, источник, уверенность 0–3), тип/текст/факты/fact key и возраст относительно текущего времени; визуально различимы `Impossible`, ответ на запрос, молчание и противоречие.
- `SituationCommand` показывает обстановку на основе `DeliveredReports` и `PlayerKnowledge`: no-data vs empty, конфликтующие версии, сводка по противоречиям/сигналам, без раскрытия скрытой истины.
- Публичные API/сигнатуры совместимы с `GameLoop`/DI; чтения `Zone.PresentUnitIds` и скрытого состояния нет.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Commands/SituationCommand.cs`
- `Kriegsspiel.Console/Commands/ReportRenderer.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-21e-r1.md` (handoff)

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` → ok (0 warnings / 0 errors).
- `dotnet test Kriegsspiel.slnx --no-build` → ok (Passed 145, Failed 0).
- `grep -n "PresentUnitIds"` по обоим файлам → пусто.
- Ручной probe-сценарий (4 поля+возраст, no-data/empty, конфликт как обе версии, `Impossible`/query/silence/contradiction различимы, без утечки hidden sentinel) описан в handoff.

## Верификация (verifier)
- Вердикт: **verified**
- Замечания/раунды (дельта): нет.
- Merge в ветку волны: выполнен (`2ca6d77`).
- Post-merge в контексте ветки волны: сборка ok, тесты 145/145 ok.

## Процесс (трение)
- Неоднозначности: `IReportFactory`/нарратор не подключены в DI (T-22) — рендер опирается на уже сформированные `DeliveredReports`/`Knowledge`, что допустимо по задаче.
- Лишние поиски: отсутствовали.
- Оценка доп. затрат: низко.
- Предложения по улучшению: после `T-22` прогнать smoke ОБСТАНОВКА/ЗАПРОС в интегрированном меню.

## Заметки / tech-debt
- Нет.
