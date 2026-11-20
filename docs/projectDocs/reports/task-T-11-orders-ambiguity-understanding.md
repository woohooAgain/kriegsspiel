# Отчёт задачи T-11 — Orders ambiguity/understanding

- **T-XX:** T-11
- **Волна / ветка волны:** Wave 1 / `wave/1-core-mechanics`
- **Ветка задачи:** `task/T-11-orders-ambiguity-understanding`
- **Целевая ветка MR:** `wave/1-core-mechanics`
- **MR:** локальный merge (remote/MR отсутствует)
- **Раунды:** 1 (реализация в task-ветке) + верификация/merge в ветку волны
- **Финализировал и закоммитил отчёт (verifier, после проверок):** агент (полный цикл), 2026-11-20

## Выполнено (по пунктам «Что сделать»)
- Обновлён `AmbiguityScorer`: веса/правила A по §4 (в т.ч. пустой `Куда` для `Hold`/`Reserve`/прочих по контексту), clamp и использование параметров/контекста в пределах ownership.
- Добавлены `ConflictResolver` и логика ветвей (`OrderBranch` как правила/оценка в owned-файле без дублирования доменных типов из `Domain/Order.cs`), детерминированное разрешение конфликтов до roll P, выбор ветви по знанию/ограничения.
- `UnderstandingResolver`/`IUnderstandingResolver` переработаны под формулу P с модификатором `Pmin`, один roll, формирование `EffectiveOrder` отдельно от исходного приказа, исходы `correct/shift/clarify` и искажения по правилам (в рамках доступных контрактов).
- Тесты: расширены `AmbiguityScorerTests`, добавлены `UnderstandingResolverTests` (веса/формула/конфликты/ветви/roll/clarify-shift-correct).

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core.Tests/AmbiguityScorerTests.cs`
- `Kriegsspiel.Core.Tests/UnderstandingResolverTests.cs` (новый)
- `Kriegsspiel.Core/Arbitration/IUnderstandingResolver.cs`
- `Kriegsspiel.Core/Arbitration/UnderstandingResolver.cs`
- `Kriegsspiel.Core/Orders/AmbiguityScorer.cs`
- `Kriegsspiel.Core/Orders/ConflictResolver.cs` (новый)
- `Kriegsspiel.Core/Orders/Misinterpretation.cs`
- `Kriegsspiel.Core/Orders/OrderBranch.cs` (новый)

## Проверки (из «Как проверить», выполнены implementer в ветке)
- `dotnet build Kriegsspiel.slnx` (worktree `/tmp/opencode/wave1/T-11`) → успех (0/0).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "AmbiguityScorerTests|UnderstandingResolverTests"` → Passed 23, Failed 0.

## Верификация (verifier)
- Вердикт: verified
- Замечания/раунды: критичных замечаний по фильтру/сборке нет; есть интеграционные ограничения (см. заметки).
- Merge в ветку волны: выполнен (merge `task/T-11-orders-ambiguity-understanding` → `wave/1-core-mechanics`).
- Post-merge в контексте ветки волны (сборка+старт): `dotnet build Kriegsspiel.slnx` → успех (0/0); `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "AmbiguityScorerTests|UnderstandingResolverTests"` → Passed 23, Failed 0.

## Процесс (трение)
- Старт/конец или отметки раундов: реализация в task-ветке, затем верификация/merge в ветку волны (восстановление полного цикла).
- Неоднозначности (ссылки на карточку/`docs/gameDocs/rules.md §…`/док/файл): интеграция `clarify` (курьер/`QuerySent`) и применение `EffectiveOrder` в execution/Arbiter вне ownership T-11 (`§4, §5 стадия 2`, трасса D).
- Лишние поиски и причина (чего не хватило в `structure.md`/ownership/шаблоне/требованиях): анализ пересечений (типы ветвей в `Domain/Order.cs`, вызовы из `Arbiter`/`OrderCommand`) при жёстком ownership.
- Оценка доп. затрат: средне/высоко (+ согласование контрактов и обходы при ограничениях ownership).
- Предложения по улучшению: заранее фиксировать в карточках контрактные точки интеграции между T-10/T-11/T-12/T-20.

## Заметки / tech-debt
- Ограничения implementer: реальное занятие курьера при `clarify`/полная диспетчеризация требует T-20; влияние искажений на исполнение зависит от использования `EffectiveOrder` в execution; эвристики при отсутствии параметризованных ограничений/данных сценария в резолвере; `docs/projectDocs/structure.md` и `OrderCommand` вне ownership. Отдельный `TD-*` не оформлялся в этой сессии.
