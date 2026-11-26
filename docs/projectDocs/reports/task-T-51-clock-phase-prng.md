# Отчёт задачи T-51 — часы, фазы и детерминированный PRNG

- **T-51:** `Clock`/`EventPhase` (ключ `(minute, phase, sequence)`, фазы §2), запрет планирования в прошлое, `PendingCount`/`PendingKeys`; единый детерминированный PRNG `DeterministicRandomSource`; `ClockTests` под v0.4.
- **Волна / ветка волны:** Wave 5, `wave/5-v04-foundation` @ `6f6adbc` (база — `master@90acbe6`).
- **Ветка задачи / MR:** `task/T-51-clock-phase-prng` @ `d34e204`, worktree implementer `/tmp/opencode/ks-implementer-T-51-r1`; MR не создавался (передача по refs ветки).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-51-r1.md` (`d34e204`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/wave5-g2`.

## Выполнено (по пунктам «Что сделать»)
1. `EventPhase`: добавлен `Dispatch`, значения приведены к порядку §2 (`Dispatch → Delivery → Understanding → Movement → Combat → ReportCreate → ReportDeliver`); `Scheduled` сохранён как служебная фаза для не переделанных потребителей (временное сохранение до cleanup, MVP-путём не используется). `ScheduledAt`/`CompareTo` — порядок `(minute, phase, sequence)`.
2. `Clock`: `Schedule` в прошлое (`minute < Now`) и вставка раньше уже обработанного ключа той же минуты запрещены; `AdvanceTo` не теряет события; `PendingCount`/`PendingKeys`/`HasPendingEvents`; публичные `PhaseFor`/`PhaseForName`.
3. `DeterministicRandomSource`: единый SplitMix64 PRNG с `Seed`, `State`, `NextDouble`, `NextInt(min,max)`, `NextInt(max)`, `Chance`, `Restore`, `Reset`; одинаковый seed/state → одинаковая последовательность; `Restore` продолжает последовательность. Новый тип/адаптер не создавался.
4. `ClockTests` переписаны на v0.4: порядок фаз §2, sequence внутри фазы, запрет прошлого, отсутствие потерь при `AdvanceTo`, `PendingCount`/`PendingKeys`, детерминизм seed/state, `Restore`/`Reset`; проверки extended-порядка удалены.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Domain/Clock.cs`
- `Kriegsspiel.Core/Ai/DeterministicRandomSource.cs`
- `Kriegsspiel.Core.Tests/ClockTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-51-r1.md` (process-артефакт)
- `Kriegsspiel.Core/Contracts/IRandomSource.cs` — без изменений (контракт уже содержит требуемые члены).
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (проверено `git diff --name-only 6f6adbc..d34e204`).

## Проверки (evidence refs)
- Pre-merge (worktree `/tmp/opencode/ks-implementer-T-51-r1`, `task/T-51-clock-phase-prng` @ `d34e204`; повторено verifier):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test ... --filter "FullyQualifiedName~ClockTests"` → 20/20 passed.
  - `dotnet test Kriegsspiel.slnx` → 159/159 passed.
- Post-merge (ветка `verifier/wave5-g2`, контекст волны, merge `1559df5`):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `ClockTests` → 20/20 passed.
  - `dotnet test Kriegsspiel.slnx` → 159/159 passed.

## Верификация (verifier)
- **Вердикт: verified.**
- Соответствие `rules.md` v0.4 (§1 п.6, §2): порядок фаз/ключей, запрет прошлого и детерминизм подтверждены кодом и тестами; новых механик сверх v0.4 нет.
- Ownership строгий; `AGENTS.md`/`rules.md`/чужие файлы не тронуты.
- Новых проектов/объявлений `class|record|struct|interface|enum` в diff нет; путей `*/Mvp/*` нет; `Kriegsspiel.slnx` не менялся.
- Расширение существующего `EventPhase` (не новый enum-тип) допущено карточкой; `Scheduled` — предсуществующая служебная фаза, не участвует в v0.4-порядке шагов, удаление/переклассификация — вне scope T-51.
- Тесты переписаны под v0.4; старые extended-проверки удалены; новых тестовых классов нет.
- Техдолга перехода нет (новых `TD-*`; `tech-debt.md` не менялся).
- Процесс: работа в task-ветке, коммит `d34e204` с атрибуцией `implementer`; в чужие ветки/`master` коммитов нет.
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-51-clock-phase-prng` → `verifier/wave5-g2` (`--no-ff`, merge `1559df5`); `wave/5-v04-foundation` переведена fast-forward.
- **Post-merge в контексте волны:** сборка/`ClockTests`/full suite → PASS (см. выше).

## Процесс (трение)
- Неоднозначности: карточка требует «фазы §2» и одновременно «без нового enum-типа»; реализация расширяет существующий `EventPhase` и сохраняет `Scheduled` для промежуточной зелёной сборки — согласуется с временным сохранением extended-членов до cleanup-волны.
- Лишние поиски: handoff задачи изначально отсутствует в базе волны (лежит в task-ветке) — читался из worktree implementer; на проверку не влияет.
- Оценка доп. затрат: низко (локальный diff, проверки воспроизводимы).
- Предложения: при переклассификации `Scheduled` в cleanup учитывать события `OrderIssued`/`QuerySent`/`CourierLost`/`CourierReturned`, чтобы не потерять их упорядочивание.

## Заметки / tech-debt
- TD нет.
- Наблюдение (не блокирует): числовые значения `EventPhase` сдвинуты из-за `Dispatch`; совместимость со старыми save-файлами не сохраняется (принцип v0.4), текущий round-trip `PersistenceTests` зелёный.
