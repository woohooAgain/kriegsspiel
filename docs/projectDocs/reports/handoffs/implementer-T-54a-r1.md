# Handoff: T-54a — Unit-aware §6-strength на resolver-пути, round 1

## Задача и refs
- **T-54a**, Wave 5 (G4), покрывает `docs/gameDocs/rules.md` §6 (`ReportedOwnStrength`/`ReportedEnemyStrength` для Arrival/Impossible/Status/Contact), §2 шаг 3 (Contact), §4 (доставка); трассы K/N-01/N-04.
- Карточка: `docs/projectDocs/todo.md` (`T-54a`, строки 308–343).
- **База (ветка волны):** `wave/5-v04-foundation` @ `2245237`.
- **Ветка задачи:** `task/T-54a-unit-aware-strength`.
- **Worktree:** `/tmp/opencode/ks-implementer-T-54a-r1`.
- **MR:** не создан; передача по ref ветки/коммитов, per-task merge выполняет `verifier`. Merge не выполнялся.
- **Ownership:** изменены `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`, `Kriegsspiel.Core/Contracts/IReportFactory.cs`, `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`, `Kriegsspiel.Core.Tests/ReportTests.cs` (+ этот handoff). `Kriegsspiel.Core/Reporting/IReportNarrator.cs` не менялся: `ReportFactory.CreateWithHonesty` там уже реализован, добавление члена в интерфейс проверяет соответствие на компиляции.

## Что сделано (in-place, без новых типов/проектов)
- **`IReportFactory`**: добавлен unit-aware член `CreateWithHonesty(...)` с сигнатурой как у `ReportFactory.CreateWithHonesty` (source `Unit`, `honesty`, `observedZoneId`, `observedEnemies`). Legacy `Create`/overload сохранены как временный scaffolding для ещё не переведённых потребителей (`ReconResolver` → T-58); production-путь `ExecutionResolver` их больше не вызывает.
- **`ExecutionResolver`**:
  - default-конструктор теперь использует существующий `ReportFactory` (`new ReportFactory(new FrictionTable())`); nested `DefaultReportFactory` удалён.
  - `StatusReport` (Arrival/Impossible/Status/Contact) создаёт донесение через `_reports.CreateWithHonesty(...)` с `unit`, `honesty = unit.Traits.ReportHonesty`; для Contact передаются enemy-юниты наблюдаемого узла (`unit.LocationZoneId`), для остальных — `Array.Empty<Unit>()`.
  - confidence (`Arrival`/`Impossible`→3, `Contact`/`Status`→2) и `deliveryMinute = minute + ReportBaseDelayMinutes + jitter` сохранены без изменений; `T_leg_back` по-прежнему применяется `ReportDeliveryResolver` вне резолвера.
  - добавлен private static helper `EnemiesInZone`, тип `Unit`-коллекции без новых типов.
- **Тесты**:
  - `ExecutionResolverTests`: якорные §6-проверки на resolver-пути — Arrival (`ReportedOwnStrength=112`, `ReportedEnemyStrength=0`, confidence 3), Impossible (honesty 1 → 124, enemy 0, confidence 3), Status (112/0, confidence 2), Contact (honesty 2 → own 112, enemy 88, `ObservedZoneId`), Contact-сумма двух enemy (honesty 3 → own 100, enemy 200). Дополнительно усилен существующий Contact-тест.
  - `ReportTests`: `IReportFactory.CreateWithHonesty` доступен через контракт и возвращает §6-поля (own/enemy/`ObservedZoneId`/confidence) для Arrival и Contact.

## Отклонения / решения
- `IReportNarrator.cs` оставлен без правок: `ReportFactory.CreateWithHonesty` уже был реализован, добавление члена в интерфейс достаточно для unit-aware вызова через контракт.
- Для Contact `observedEnemyStrength` считается `ReportFactory.ResolveEnemyStrength` (фильтр стороны/strength/captured + honesty-искажение); передаются все юниты узла, кроме источника.
- Новые `class/record/struct/interface/enum`/проекты не добавлялись; TD не создаётся.

## Изменённые файлы (ownership)
- `Kriegsspiel.Core/Contracts/IReportFactory.cs`
- `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
- `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`
- `Kriegsspiel.Core.Tests/ReportTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-54a-r1.md` (этот handoff)

`AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие ветки/задачи не тронуты.

## Проверки (команда → результат)
- `dotnet build Kriegsspiel.slnx` → **PASS** (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ExecutionResolverTests|FullyQualifiedName~ReportTests"` → **PASS** (35/35 passed, 0 failed, 0 skipped).
- `dotnet test Kriegsspiel.slnx` → **PASS** (181/181 passed, 0 failed, 0 skipped).
- `grep -n "_reports.Create(\|Report.Create(" Kriegsspiel.Core/Arbitration/ExecutionResolver.cs` → **нет совпадений** (exit 1).
- `git diff --check` → чисто.

## Утверждения T-54a (закрытие)
- `ExecutionResolver` создаёт все production-донесения через unit-aware `IReportFactory.CreateWithHonesty`.
- `ReportedOwnStrength`/`ReportedEnemyStrength` заполнены по §6 для Arrival/Impossible/Status/Contact (own не `null`; enemy 0 без противника; сумма strength enemy для Contact с honesty-искажением).
- Legacy `_reports.Create`/прямой `Report.Create` не используются production-путём `ExecutionResolver`.
- `ExecutionResolverTests`/`ReportTests` зелёные; новых типов/проектов нет.

## TD / процесс
- Новых `TD-*` нет; задача закрывается без техдолга.
- Процессное трение: низкое. Ограничение — default-конструктор резолвера не имел `FrictionTable`; использован существующий `ReportFactory` с дефолтной таблицей, DI-путь уже инжектит `IReportFactory`.

## Для verifier
- Проверить diff `2245237..<task-head>` по ownership и отсутствию новых объявлений типов/проектов.
- Повторить: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ExecutionResolverTests|FullyQualifiedName~ReportTests"`; `dotnet test Kriegsspiel.slnx`; grep-проверку выше.
- Merge — per-task в `wave/5-v04-foundation`, затем сценарий Wave 5 (G4).
