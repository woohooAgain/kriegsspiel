# Отчёт задачи T-10 — Couriers and transmission

- **T-XX:** T-10
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-10-couriers-and-transmission`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация/фиксация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Реализован `CourierPool` (состояния free→outbound→return→returned/lost, FIFO-очередь, приоритет запросов над приказами, отправка при освобождении/наличии свободного курьера, отдельные шансы потери по плечам, замена через 60 мин после расчётного возврата, ETA/фиксация deliveryTime/returnTime > 0, семантика подтверждения обратного плеча).
- `TransmitionResolver`/`ITransmitionResolvers` интегрированы с пулом (с сохранением совместимости вызова из `Arbiter`), поддержка обработки запроса как курьерного сообщения в рамках контрактов.
- Обновлены `TransmitionResolverTests`: пример §14 (06:15 → 06:45 при deliveryTime=30, возврат по ETA), FIFO/ETA от фактической отправки, приоритет запроса, потери туда/обратно, замена через 60 мин.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/CourierPool.cs` (новый)
- `Kriegsspiel.Core/Arbitration/ITransmitionResolvers.cs`
- `Kriegsspiel.Core/Arbitration/TransmitionResolver.cs`
- `Kriegsspiel.Core.Tests/TransmitionResolverTests.cs`

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-10`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "TransmissionResolverTests"` → Passed 7, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний нет; см. ограничения интеграции.
- Merge в ветку волны: выполнен (`add8857`, merge `task/T-10-couriers-and-transmission` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "TransmissionResolverTests"` → Passed 7, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке/worktree, затем верификация и merge в ветку волны в рамках восстановления полного цикла Wave 1.
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): семантика `deliveryConfirmed`/return-loss при фиксированном `TransmissionResult` (вне ownership), интеграция прогресса очереди/ETA в рантайме (`§3, §5 стадия 1, §14`, T-10).
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): потребовалось сверять ограничения ownership (`TurnResult/Arbiter/Domain` вне владения) и существующие контракты.
- Оценка доп. затрат: средне (+ согласование семантики потерь/подтверждения в рамках ownership).
- Предложения по улучшению: явно указывать в карточках задач допустимые точки интеграции/ожидаемые сигнатуры при ограниченном ownership.

## Заметки / tech-debt
- Ограничения зафиксированы implementer: `Arbiter` не вызывает `UpdateTo`/автодиспатч очереди (полная интеграция — T-20), упрощение конфигурации вероятности потери return-плеча относительно профиля, `QueryAnswered/QuerySilence` вне scope T-10. Отдельный `TD-*` не создавался в этой сессии; при необходимости оформить через `architect`/tech-debt intake.
