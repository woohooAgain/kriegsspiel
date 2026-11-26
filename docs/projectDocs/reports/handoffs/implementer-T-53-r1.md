# Handoff: T-53 — путь и движение (`PathPlanner`/`ExecutionResolver`), round 1

## Задача и refs
- **T-53**, Wave 5, покрывает `docs/gameDocs/rules.md` §2 шаг 3, §3, §4 (`T_leg`/`T_leg_back`), §5.
- Карточка: `docs/projectDocs/todo.md` (`T-53`, строки 243–269); сценарий Wave 5: `docs/projectDocs/todo.md` (строки 94–103).
- Gate: `docs/projectDocs/reports/handoffs/verifier-gate-mvp-transition-r3.md` (в ветке `verifier/mvp-transition-gate-r3`).
- **База (ветка волны):** `wave/5-v04-foundation` @ `6f6adbc`.
- **Ветка задачи:** `task/T-53-path-planner-movement` @ `5c5a9a8` (код-коммит).
- **Worktree:** `/tmp/opencode/ks-implementer-T-53-r1`.
- **MR:** не создан; передача по ref ветки/коммита, per-task merge выполняет `verifier`. Merge не выполнялся.

## Что сделано (v0.4, in-place, без новых типов)
- **`PathPlanner`**:
  - `TryPlan` переписан на вес ребра: `travelTime` при `deadline != null`, `travelTime * (EdgeType == Offroad ? 0.8 : 1.0)` при `deadline == null`; Дейкстра с детерминированным порядком соседей (лексикографически).
  - Фактическое время шага и `TotalMinutes` всегда `travelTime` (взвешенная стоимость используется только для выбора маршрута).
  - forbidden-правила: `forbiddenZone != start` исключает узел; `forbiddenZone == start` разрешает выход, но запрещает возврат; путь в `forbiddenZone` невозможен.
  - Добавлены `CourierLegMinutes`/`CourierLegBackMinutes` по `Zone.CourierMinutes` (Дейкстра) с минимумом 5.
  - `RouteStep`/`RoutePlan` очищены от extended-полей (`MovementMode`, readiness/fatigue-дельты); новых типов нет.
- **`ExecutionResolver`**:
  - Переход «узел → ребро»: `IsOnEdge`, `EdgeFromZoneId`, `EdgeToZoneId`, `EntryEdgeType`, `RemainingMinutes = travelTime`; уменьшение `RemainingMinutes` на 1 за минутный тик через существующий `UnitMoved` (без новых событий/типов).
  - Завершение ребра: перенос присутствия в `ToZoneId`, сброс edge-локации; offroad — `readiness -= 5` в момент завершения.
  - Немедленный переход на следующее ребро в тот же тик: `RemainingMinutes` нового ребра в этом тике не уменьшается (следующий тик — `+1`).
  - Позы: `March` на ребре, `Attack` при контакте в узле, `Defend` в узле без движения; `move` в свободный target → `Defend` + `EffectiveOrder.Intent = Hold`.
  - `pendingOrder`: при приходе приказа на ребре перезаписывается (единичный буфер) и применяется при завершении ребра; старый маршрут снимается.
  - `deadline expiry`: при `t > deadline` и недостигнутой цели педантичный командир (`Pedantry >= 2`, `Init <= 1`) прерывает приказ (Impossible, defense); иначе продолжает.
  - Forbidden-конфликт на входе приказа: `Pedantry >= Init` → Impossible; `Init > Pedantry` → forbidden сбрасывается и маршрут пересчитывается.
  - `hold` без target и `recon` не двигаются; `hold` с target идёт к цели и удерживает её.
  - Контакт в промежуточном/целевом узле останавливает движение, ставит `Attack`, формирует Contact-донесение и планирует CombatTick на ближайший `t % 30 == 0`.
  - Удалены extended-механики движения из MVP-пути: fatigue, movement modes (Covert/Contact/March-множители), attention-shift readiness, improvise/query-реакции, ветви/обходимые цели. `OnArrive`/`GenerateReport` сохранены как точки контракта.
- **Тесты**: `ExecutionResolverTests` полностью переписан под v0.4 (15 тестов): вес/фактическое время, forbidden (узел/start/target), courier legs min 5, вход в ребро без телепорта, декремент по 1/мин, немедленный переход без декремента, offroad −5, posture, move→hold, contact, pendingOrder (перезапись + применение), deadline expiry, hold/recon stationary, hold с target.

## Отклонения / решения
- **Первый тик ребра:** при входе в ребро на минуте `t` первый декремент выполняется на `t+1`, поэтому прибытие = `t + travelTime` (фактическое время = `travelTime`). Правило «новое ребро не уменьшается в тике завершения предыдущего» выполнено.
- **Проверка deadline:** выполняется при завершении ребра/на входе приказа (не посреди ребра), чтобы не останавливать юнит в edge-локации; при `t > deadline`, недостигнутой цели и `Pedantry >= 2`, `Init <= 1` — Impossible/defense.
- **Contact:** используется простая проверка присутствия противника (существующее поведение) для выбора позы/остановки; вероятностная разведка/контакт остаются за `T-58`/`T-61` (новый контракт обнаружения не вводился).
- **Отчёты:** `StatusReport` оставлен в текущем контракте `IReportFactory` (без новых полей); delivery-формулы §6 не менялись — это scope `T-54`.
- **`IGameRules.MinTravelTimeMinutes=5`** (замечание verifier по T-50): не входит в ownership T-53 (`IGameRules.cs` не принадлежит задаче), поэтому оставлен без изменений; TD по условию задачи не заводится.
- `IExecutionResolver.cs` не менялся: сигнатуры `Start/OnMove/OnArrive/GenerateReport` сохранены для `Arbiter` и существующих стабов интеграционных тестов.

## Изменённые файлы (ownership)
- `Kriegsspiel.Core/Arbitration/PathPlanner.cs`
- `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
- `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-53-r1.md` (этот handoff)

Другие файлы не менялись; `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие ветки/задачи не тронуты.

## Проверки (команда → результат)
- `dotnet build Kriegsspiel.slnx` → **PASS** (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ExecutionResolverTests"` → **PASS** (15/15 passed, 0 failed, 0 skipped).
- `dotnet test Kriegsspiel.slnx` → **PASS** (153/153 passed, 0 failed, 0 skipped).
- `git diff --check` → чисто.
- Проверка типов: diff по коду не содержит добавленных объявлений `class|record|struct|interface|enum`; новых проектов/путей `*/Mvp/*` нет.
- Полный diff базы `6f6adbc..5c5a9a8` — только 3 owned `.cs` файла (плюс handoff отдельным коммитом).

## Утверждения T-53 (закрытие)
- Вес ребра и фактическое время соответствуют §5; `TotalMinutes` считается по фактическому `travelTime`.
- forbidden start запрещает возврат, forbidden != start исключает узел; `T_leg`/`T_leg_back` по courierTime с min 5.
- `RemainingMinutes` уменьшается по 1/мин; при завершении ребра следующее ребро начинается в тот же тик без декремента.
- offroad −5 readiness; posture march/attack/defense; move→hold; pendingOrder перезаписывается и применяется на завершении ребра.
- Нет телепорта; hold без target и recon не двигаются.
- Новых типов/проектов/расширенных механик движения в MVP-пути нет.

## TD / процесс
- Новых `TD-*` нет; задача закрывается без техдолга.
- Процессное трение: низкое. Основная сложность — выразить per-minute `RemainingMinutes` на существующем `UnitMoved`/`Clock` без новых событий; решение — минутные `UnitMoved`-тики, совместимые с `Arbiter`.

## Для verifier
- Проверить diff `6f6adbc..5c5a9a8` по ownership и отсутствию новых объявлений типов/проектов.
- Повторить: `dotnet build Kriegsspiel.slnx`; `dotnet test ... --filter "FullyQualifiedName~ExecutionResolverTests"`; `dotnet test Kriegsspiel.slnx`.
- Merge — per-task в `wave/5-v04-foundation`, затем сценарий Wave 5 (включая `ExecutionResolverTests` и общие команды).
