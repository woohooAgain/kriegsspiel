# Отчёт задачи T-53 — путь и движение (`PathPlanner`/`ExecutionResolver`)

- **T-53:** `PathPlanner` (Дейкстра, вес ребра, forbidden, `T_leg`/`T_leg_back`) и `ExecutionResolver` (движение по рёбрам, позы, pendingOrder, deadline) под v0.4; `ExecutionResolverTests`.
- **Волна / ветка волны:** Wave 5, `wave/5-v04-foundation` @ `6f6adbc` (база — `master@90acbe6`).
- **Ветка задачи / MR:** `task/T-53-path-planner-movement` @ `b764c4f` (код-коммит `5c5a9a8`), worktree implementer `/tmp/opencode/ks-implementer-T-53-r1`; MR не создавался (передача по refs ветки).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-53-r1.md` (`b764c4f`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/wave5-g2`.

## Выполнено (по пунктам «Что сделать»)
1. `PathPlanner`: вес `travelTime` при `deadline != null`, `travelTime * (EdgeType == Offroad ? 0.8 : 1.0)` при `deadline == null`; фактическое время и `TotalMinutes` всегда `travelTime`; детерминированный порядок соседей. Forbidden: `forbiddenZone != start` исключает узел, `forbiddenZone == start` разрешает выход и запрещает возврат, путь в `forbiddenZone` невозможен. `CourierLegMinutes`/`CourierLegBackMinutes` по `courierTime` с минимумом 5. `RouteStep`/`RoutePlan` очищены от extended-полей.
2. `ExecutionResolver`: вход «узел → ребро» (`RemainingMinutes = travelTime`, без телепорта); уменьшение на 1 за минутный тик; завершение ребра с offroad −5 readiness; немедленный переход на следующее ребро в тот же тик без декремента нового; позы march/attack/defense; `move` в свободный target → defense + `Intent=Hold`; `hold` без target и `recon` не двигаются; `pendingOrder` перезаписывается и применяется на завершении ребра; deadline expiry по §5; contact останавливает движение. Extended-механики движения (fatigue/movement modes/attention-shift/ветви) удалены из MVP-пути; новый контракт обнаружения не вводился.
3. `ExecutionResolverTests` переписаны под v0.4 (15 тестов): вес/фактическое время, forbidden (узел/start/target), courier legs min 5, вход в ребро, декремент по 1/мин и немедленный переход, offroad −5, posture, move→hold, contact, pendingOrder (перезапись + применение), deadline, hold/recon stationary, hold с target.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/PathPlanner.cs`
- `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
- `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-53-r1.md` (process-артефакт)
- `Kriegsspiel.Core/Arbitration/IExecutionResolver.cs` — без изменений (сигнатуры `Start/OnMove/OnArrive/GenerateReport` сохранены).
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (проверено `git diff --name-only 6f6adbc..b764c4f`).

## Проверки (evidence refs)
- Pre-merge (worktree `/tmp/opencode/ks-implementer-T-53-r1`, `task/T-53-path-planner-movement` @ `b764c4f`; повторено verifier):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test ... --filter "FullyQualifiedName~ExecutionResolverTests"` → 15/15 passed.
  - `dotnet test Kriegsspiel.slnx` → 153/153 passed.
- Post-merge (ветка `verifier/wave5-g2`, контекст волны после T-51/T-52, merge `e9526fb`):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `ExecutionResolverTests` → 15/15 passed.
  - `dotnet test Kriegsspiel.slnx` → 168/168 passed.
  - Wave 5 общий фильтр → 98/98 passed (G2 partial: `T-54` ещё не влита).

## Верификация (verifier)
- **Вердикт: verified.**
- Соответствие `rules.md` v0.4 (§2 шаг 3, §3, §4 `T_leg`/`T_leg_back`, §5): вес/фактическое время, forbidden-правила, движение по 1/мин, no-decrement нового ребра, offroad −5, позы, move→hold, pendingOrder, deadline — подтверждены кодом и тестами; новых механик сверх v0.4 нет.
- Ownership строгий; `AGENTS.md`/`rules.md`/чужие файлы не тронуты.
- Новых проектов/объявлений `class|record|struct|interface|enum` в diff нет; путей `*/Mvp/*` нет; `Kriegsspiel.slnx` не менялся.
- Тесты переписаны под v0.4; extended-проверки движения удалены; новых тестовых классов нет.
- Техдолга перехода нет (новых `TD-*`; `tech-debt.md` не менялся).
- Процесс: работа в task-ветке, коммиты `5c5a9a8`/`b764c4f` с атрибуцией `implementer`; в чужие ветки/`master` коммитов нет.
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-53-path-planner-movement` → `verifier/wave5-g2` (`--no-ff`, merge `e9526fb`); `wave/5-v04-foundation` переведена fast-forward.
- **Post-merge в контексте волны:** сборка/`ExecutionResolverTests`/full suite/Wave 5 фильтр → PASS (см. выше).

## Процесс (трение)
- Неоднозначности: карточка требует «per-minute `RemainingMinutes`» без новых событий; реализовано минутными `UnitMoved`-тиками на существующем `Clock`/`Arbiter` — согласуется с ограничением «новых типов нет».
- Лишние поиски: проверка `IGameRules` — handoff отмечает вне ownership; `MinTravelTimeMinutes` из T-50 не используется (см. заметки).
- Оценка доп. затрат: средне (крупный diff резолвера/тестов), проверки воспроизводимы.
- Предложения: на этапе cleanup удалить неиспользуемый `IGameRules.MinTravelTimeMinutes`, чтобы не путать его с `T_leg`-минимумом.

## Заметки / tech-debt
- TD нет.
- Наблюдение (не блокирует, вне изменения T-53): `IGameRules.MinTravelTimeMinutes = 5` (добавлен в T-50) остаётся неиспользуемым; к фактическому времени ребра минимум не применяется — `T-53` использует минимум 5 только для `T_leg`/`T_leg_back` (`MinCourierLegMinutes`). Учесть при cleanup.
