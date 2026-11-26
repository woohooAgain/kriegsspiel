# Handoff: T-51 — часы, фазы и детерминированный PRNG, round 1

## Задача и refs
- **T-51**, Wave 5, покрывает `docs/gameDocs/rules.md` §1 п.6, §2; карточка `docs/projectDocs/todo.md:183-216`; сценарий Wave 5 `docs/projectDocs/todo.md:94-103`.
- **Ветка волны (база):** `wave/5-v04-foundation` @ `6f6adbc`.
- **Ветка задачи:** `task/T-51-clock-phase-prng` (от `wave/5-v04-foundation@6f6adbc`).
- **Worktree:** `/tmp/opencode/ks-implementer-T-51-r1`.
- **MR:** не создан; передача по ref ветки/коммита, per-task merge выполняет `verifier`. Merge не выполнялся.
- **Ownership:** изменены только `Kriegsspiel.Core/Domain/Clock.cs`, `Kriegsspiel.Core/Ai/DeterministicRandomSource.cs`, `Kriegsspiel.Core.Tests/ClockTests.cs` + этот handoff. `Kriegsspiel.Core/Contracts/IRandomSource.cs` не менялся (контракт уже содержит требуемый набор членов). `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/ветки не тронуты.

## Что сделано (v0.4)
- **EventPhase:** в существующий enum добавлен `Dispatch`, порядок значений приведён к §2: `Dispatch(0) → Delivery(1) → Understanding(2) → Movement(3) → Combat(4) → ReportCreate(5) → ReportDeliver(6)`; `Scheduled(7)` сохранён как служебная фаза для событий вне шагов тика. Новый enum-тип не создавался.
- **Clock:**
  - ключ `ScheduledAt(minute, phase, sequence)` и лексикографический `CompareTo` (minute → phase → sequence) сохранены;
  - `PhaseFor(GameEvent)` и `PhaseForName(string)` сделаны публичными для проверки соответствия §2; маппинг существующих событий не менял поведение;
  - запрет планирования в прошлое (`minute < Now` → `ArgumentOutOfRangeException`, недопустимый ключ не попадает в очередь) и запрет вставки раньше уже обработанного ключа той же минуты сохранены;
  - `AdvanceTo` отдаёт все события с `minute <= target`, обновляет `Now`, не теряет события между вызовами;
  - `PendingCount`/`HasPendingEvents`/`PendingKeys` сохранены.
- **DeterministicRandomSource:** единый воспроизводимый PRNG на SplitMix64. Публичные `Seed`, `State`, `NextDouble`, `NextInt(min,max)`, `NextInt(max)`, `Chance`, `Restore`, `Reset`; `NextInt` — без modulo-bias (rejection); одинаковый seed/state даёт одинаковую последовательность; `Restore` продолжает ту же последовательность; `Reset` возвращает начальное состояние.
- **ClockTests:** переписаны под v0.4: порядок фаз §2, sequence внутри фазы, запрет прошлого, отсутствие потерь при `AdvanceTo`, `PendingCount`/`PendingKeys`, публичный маппинг фаз, детерминизм seed/state, `Restore`/`Reset`, границы `NextInt`, `Chance`. Проверки старого extended-порядка удалены.

## Изменённые файлы (ownership)
- `Kriegsspiel.Core/Domain/Clock.cs` — `EventPhase.Dispatch`, публичные `PhaseFor`/`PhaseForName`, порядок §2.
- `Kriegsspiel.Core/Ai/DeterministicRandomSource.cs` — единый SplitMix64 PRNG, публичный `Chance`, детерминированные `NextInt`.
- `Kriegsspiel.Core.Tests/ClockTests.cs` — v0.4-проверки часов и PRNG.
- `docs/projectDocs/reports/handoffs/implementer-T-51-r1.md` — этот handoff.
- `Kriegsspiel.Core/Contracts/IRandomSource.cs` — **без изменений** (уже объявляет `Seed`, `State`, `NextDouble`, `NextInt`, `Restore`, `Reset`, default `Chance`).

## Проверки (команда → результат)
- `dotnet build Kriegsspiel.slnx` → **PASS** (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ClockTests"` → **PASS** (20/20 passed, 0 failed, 0 skipped).
- `dotnet test Kriegsspiel.slnx` → **PASS** (159/159 passed, 0 failed, 0 skipped).
- Типы: `git diff -U0 -- '*.cs' | grep -E "^\+.*\b(class|record|struct|interface|enum)\s"` → пусто (новых объявлений нет); новых проектов/путей `*/Mvp/*` нет; в `Kriegsspiel.slnx` изменений нет.

## Утверждения T-51 (закрытие)
- События одной минуты выдаются в порядке фаз §2; `sequence` сохраняет порядок внутри фазы, авто-sequence следует вставке.
- `Schedule` в прошлое бросает исключение и не добавляет событие; уже обработанный ключ минуты не переоткрывается.
- `AdvanceTo` не теряет события на нескольких вызовах.
- `PendingCount`/`PendingKeys` корректны и read-only.
- Одинаковый seed/state → одинаковая последовательность; `Restore` продолжает ту же последовательность; `Reset` восстанавливает начальную.
- Новых типов/проектов нет.

## Отклонения/решения
- `Scheduled` оставлен в enum как служебная фаза: на неё маппятся события вне шагов тика (`OrderIssued`, `QuerySent`, `CourierLost`/`CourierReturned` и т.п.), которые используются другими (ещё не переделанными) задачами/тестами Wave 5. Удаление/переклассификация — вне scope T-51.
- `IRandomSource.cs` не менялся: существующий контракт уже покрывает `Seed/State/NextDouble/NextInt/Chance/Restore/Reset`; правка была бы косметической.
- Числовые значения `EventPhase` сдвинуты из-за `Dispatch`; совместимость со старыми save-файлами не сохраняется (принцип v0.4), round-trip в текущем `PersistenceTests` зелёный.

## TD / процесс
- Новых `TD-*` нет; задача закрывается без техдолга.
- Процессное трение: низкое; правки локальны, полный `dotnet test` зелёный на момент handoff.

## Для verifier
- Проверить diff `6f6adbc..<task-head>` по ownership и отсутствию новых объявлений типов.
- Повторить: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ClockTests"`; `dotnet test Kriegsspiel.slnx`.
- Merge задачи — в `wave/5-v04-foundation` (per-task), затем запуск сценария Wave 5.
