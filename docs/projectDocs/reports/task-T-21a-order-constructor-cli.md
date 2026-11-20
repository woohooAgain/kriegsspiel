# Отчёт задачи T-21a — Order constructor CLI

- **T-XX:** `T-21a`
- **Волна / ветка волны:** Wave 3 / `wave/3-cli-ui-cleanup`
- **Ветка задачи / MR:** `task/T-21a-order-constructor-cli` (`0ca8599`), merge `ea1a4b4` → ветка волны
- **Раунды:** 1 (`docs/projectDocs/reports/handoffs/implementer-T-21a-r1.md`)
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент, 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Полный конструктор 7 слотов в новом `OrderBuilder` (адресат, намерение, цель, дедлайн, ограничения, срок донесения, режим передвижения); `OrderCommand.Build(GameState)` сохранил сигнатуру и делегирует билдеру.
- Закрытые списки намерений/ограничений/режимов через доменные enum; цели берутся из player-visible данных (свои юниты, известные враги из `PlayerKnowledge`, объекты/рёбра/зоны сценария) без чтения `PresentUnitIds`.
- Ветви с предпросмотром «если X → то Y», максимум 3 (4-я явно отклоняется); шаблон/повторное использование предыдущего приказа; предпросмотр `AmbiguityScorer.Breakdown`.
- Сформированный `Order` содержит `TargetObjectId`/`TargetUnitId`/`MovementMode`/`Constraints`/`Branches`/`NotLaterThan`/`ReportBy` при заполнении; истина противника в UI не раскрывается.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Console/Commands/OrderCommand.cs`
- `Kriegsspiel.Console/Commands/OrderBuilder.cs` (новый)
- `docs/projectDocs/reports/handoffs/implementer-T-21a-r1.md` (handoff)

## Проверки (evidence refs)
- `dotnet build Kriegsspiel.slnx` → ok (0 warnings / 0 errors) в ветке задачи и повторно verifier.
- `dotnet test Kriegsspiel.slnx --no-build` → ok (Passed 145, Failed 0).
- Ручной сценарий описан в handoff (7 слотов, 4-я ветвь отклоняется, A-предпросмотр, без утечки истины).

## Верификация (verifier)
- Вердикт: **verified**
- Замечания/раунды (дельта): нет; ownership соблюдён (только 2 файла команды + handoff).
- Merge в ветку волны: выполнен (`ea1a4b4` в `wave/3-cli-ui-cleanup`).
- Post-merge в контексте ветки волны: сборка ok, тесты 145/145 ok.

## Процесс (трение)
- Неоднозначности: видимость объектов/рёбер карты трактована как метаданные сценария; цели-юниты ограничены своим знанием (handoff, п. «Frictions»).
- Лишние поиски: отсутствовали.
- Оценка доп. затрат: низко.
- Предложения по улучшению: автоматизировать проверки рендеров/CLI отдельным test-проектом в следующей волне (вне ownership текущей задачи).

## Заметки / tech-debt
- Возможная будущая запись TD: нет отдельного UI/CLI test-проекта (покрытие ручными сценариями) — не оформлялось, так как находится вне текущей волны.
