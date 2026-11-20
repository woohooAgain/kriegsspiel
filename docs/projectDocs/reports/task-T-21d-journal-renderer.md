# Отчёт задачи T-21d — Journal renderer

- **T-XX:** `T-21d`
- **Волна / ветка волны:** Wave 3 / `wave/3-cli-ui-cleanup`
- **Ветка задачи / MR:** `task/T-21d-journal-renderer` (`22de208`), merge `499d0fc` → ветка волны
- **Раунды:** 1 (`docs/projectDocs/reports/handoffs/implementer-T-21d-r1.md`)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент, 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- `JournalRenderer` переработан для семантических минут (выдача/отправка/доставка/возврат, ETA и факты) на основе `EventLog`, `InTransitReports`, состояний курьеров; API совместим с `JournalCommand`.
- Отображаются очередь/статусы/ETA курьеров и явная запись `QuerySilence` («нет ответа»).
- Потеря на обратном пути трактуется как «не вернулся / deliveryConfirmed=false» без утверждения «приказ потерян/не доставлен»; `OrderLost` не показывается как `OrderDelivered`; записи сортируются по времени/семантике.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Commands/JournalRenderer.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-21d-r1.md` (handoff)

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` → ok (0 warnings / 0 errors).
- `dotnet test Kriegsspiel.slnx --no-build` → ok (Passed 145, Failed 0).
- Ручной probe-сценарий (ETA/факты, очередь, `QuerySilence`, outbound-loss vs return-leg loss) описан в handoff; проверка ключевых строк (`QuerySilence`, «не вернулся», `deliveryConfirmed`, `OrderDelivered`) выполнена в коде.

## Верификация (verifier)
- Вердикт: **verified**
- Замечания/раунды (дельта): нет.
- Merge в ветку волны: выполнен (`499d0fc`).
- Post-merge в контексте ветки волны: сборка ok, тесты 145/145 ok.

## Процесс (трение)
- Неоднозначности: явный снимок «очереди до отправки» в core-состоянии отсутствует, видимость очереди выведена из доступных полей курьеров/событий (зафиксировано в handoff).
- Лишние поиски: отсутствовали.
- Оценка доп. затрат: низко.
- Предложения по улучшению: при необходимости расширить core/DTO явным состоянием очереди — отдельная задача (вне ownership).

## Заметки / tech-debt
- Нет (наблюдение по отсутствию явного snapshot очереди — в handoff).
