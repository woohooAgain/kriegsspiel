# Отчёт задачи T-21b — Query CLI

- **T-XX:** `T-21b`
- **Волна / ветка волны:** Wave 3 / `wave/3-cli-ui-cleanup`
- **Ветка задачи / MR:** `task/T-21b-query-cli` (`c2d3cff`), merge `3a50f6d` → ветка волны
- **Раунды:** 1 (`docs/projectDocs/reports/handoffs/implementer-T-21b-r1.md`)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент, 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Добавлен `QueryCommand : ICommand` с выбором адресата/типа запроса из player-visible данных.
- Отправка запроса идёт через `ITransmissionResolver.TransmitQuery(...)` — тот же курьерный механизм/очередь, приоритет и ETA; команда неблокирующая.
- Отображение `QuerySent`, `QueryAnswered` (4 поля донесения: минута события, минута доставки, источник, уверенность) и `QuerySilence` из журнала/состояния; явно фиксируется, что молчание — «нет ответа», а не «факта нет».
- Истина противника не раскрывается; новые сервисы не требуются (регистрация в DI и пункт меню — ответственность `T-22`, зафиксировано в handoff).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Commands/QueryCommand.cs` (новый)
- `docs/projectDocs/reports/handoffs/implementer-T-21b-r1.md` (handoff)

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` → ok (0 warnings / 0 errors).
- `dotnet test Kriegsspiel.slnx --no-build` → ok (Passed 145, Failed 0).
- Ручной сценарий (выбор адресата → `QuerySent` с ETA → ответ/молчание в журнале) описан в handoff; автоматизированных UI-тестов в задаче нет.

## Верификация (verifier)
- Вердикт: **verified**
- Замечания/раунды (дельта): нет.
- Merge в ветку волны: выполнен (`3a50f6d`).
- Post-merge в контексте ветки волны: сборка ok, тесты 145/145 ok.

## Процесс (трение)
- Неоднозначности: команда создана, но не подключена к меню/DI — по ownership это `T-22`; передано как явное требование интеграции.
- Лишние поиски: отсутствовали.
- Оценка доп. затрат: низко.
- Предложения по улучшению: в `T-22` проверить резолвинг `QueryCommand` и видимость `QuerySilence` в smoke-прогоне.

## Заметки / tech-debt
- Нет.
