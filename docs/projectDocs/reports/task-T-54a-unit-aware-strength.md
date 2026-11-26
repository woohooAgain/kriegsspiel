# Отчёт задачи T-54a — unit-aware §6-strength на resolver-пути донесений

- **T-54a:** `ExecutionResolver.StatusReport` (Arrival/Impossible/Status/Contact) создаёт донесения через unit-aware `IReportFactory.CreateWithHonesty`; `ReportedOwnStrength`/`ReportedEnemyStrength` заполнены по `rules.md` v0.4 §6; removal legacy `DefaultReportFactory`; §6-тесты на resolver-пути — карточка `docs/projectDocs/todo.md` (`T-54a`, строки 308–336).
- **Волна / ветка волны:** Wave 5 (G4), `wave/5-v04-foundation` @ `2245237`.
- **Ветка задачи / MR:** `task/T-54a-unit-aware-strength` @ `474c3ac`, worktree implementer `/tmp/opencode/ks-implementer-T-54a-r1`; MR не создавался (передача по refs ветки/коммита).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-54a-r1.md` (`474c3ac`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/T-54a-r1`.
- **База задачи:** `2245237`; ветка задачи содержит ровно 1 коммит поверх базы.

## Выполнено (по пунктам «Что сделать»)
1. `IReportFactory`: добавлен unit-aware член `CreateWithHonesty(...)` (сигнатура как у `ReportFactory.CreateWithHonesty`) без нового типа; реализован существующим `ReportFactory`. Legacy `Create`/overload сохранены только как временный scaffolding (разрешено карточкой; production-путь `ExecutionResolver` их не вызывает).
2. `ExecutionResolver.StatusReport` переведён на `_reports.CreateWithHonesty(...)` с `unit`, `honesty = unit.Traits.ReportHonesty`; для `Contact` передаются enemy-юниты наблюдаемого узла (`unit.LocationZoneId`), для остальных — пустой набор. Confidence (`Arrival`/`Impossible`/`Recon` → 3, иначе 2) и `deliveryMinute`/`T_leg_back` не менялись.
3. Nested `DefaultReportFactory` удалён; default-конструктор использует существующий `ReportFactory(new FrictionTable())`. Вызовов `_reports.Create`/прямого `Report.Create` в `ExecutionResolver` не осталось.
4. `ExecutionResolverTests` дополнены §6-проверками resolver-пути: Arrival (own 112 / enemy 0 / confidence 3), Impossible (honesty 1 → own 124 / enemy 0 / confidence 3), Status (own 112 / enemy 0 / confidence 2), Contact (honesty 2 → own 112 / enemy 88 / `ObservedZoneId`), Contact-сумма двух enemy (honesty 3 → own 100 / enemy 200); существующий Contact-тест усилен.
5. `ReportTests` дополнены проверкой `IReportFactory.CreateWithHonesty` через контракт (§6-поля own/enemy/`ObservedZoneId`/confidence); поведение confidence/delivery не менялось.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
- `Kriegsspiel.Core/Contracts/IReportFactory.cs`
- `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`
- `Kriegsspiel.Core.Tests/ReportTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-54a-r1.md` (process-артефакт)
- `Kriegsspiel.Core/Reporting/IReportNarrator.cs` — не менялся (`ReportFactory.CreateWithHonesty` уже реализован).
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (`git diff --name-only 2245237..474c3ac` = только файлы выше).

## Проверки (evidence refs)
- Pre-merge (worktree `/tmp/opencode/ks-implementer-T-54a-r1`, `task/T-54a-unit-aware-strength` @ `474c3ac`; повторено verifier):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ExecutionResolverTests|FullyQualifiedName~ReportTests"` → 35/35 passed.
  - `dotnet test Kriegsspiel.slnx` → 181/181 passed.
  - `grep -n "_reports.Create(\|Report.Create(" Kriegsspiel.Core/Arbitration/ExecutionResolver.cs` → нет совпадений (exit 1).
  - `grep -rn "DefaultReportFactory" --include=*.cs` → нет ссылок (nested-класс удалён без внешних зависимостей).
  - `git diff --check 2245237..474c3ac` → чисто.
- Post-merge (ветка `verifier/T-54a-r1`, merge `3b1530d`, контекст волны после T-50–T-54):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `ExecutionResolverTests|ReportTests` → 35/35 passed.
  - `dotnet test Kriegsspiel.slnx` → 181/181 passed.

## Верификация (verifier)
- **Вердикт: verified.**
- **§6-разрыв закрыт:** `ExecutionResolver.StatusReport` использует unit-aware `CreateWithHonesty`; `ReportedOwnStrength` не `null` для Arrival/Impossible/Status/Contact (honesty-искажение 0.12 в обе стороны), `ReportedEnemyStrength == 0` без противника и сумма strength enemy для Contact; confidence/`deliveryMinute` соответствуют §4/§6. `ReportedOwnStrength == null` остаётся только для `ReportKind.Recon` (вне scope карточки, соответствует §6).
- **Нет новых типов/проектов/механик:** diff не содержит объявлений `class|record|struct|interface|enum`; `Kriegsspiel.slnx` не менялся; `DefaultReportFactory` — приватный nested-класс без внешних ссылок, его удаление не регрессия.
- **Ownership строгий;** `AGENTS.md`/`rules.md`/`todo.md`/чужие файлы не тронуты.
- **Тесты:** §6-проверки resolver-пути и контракта добавлены/усилены; старые проверки удалённых механик отсутствуют. `ExecutionResolverTests`/`ReportTests` зелёные.
- **Техдолг перехода:** новых `TD-*` нет; `tech-debt.md` не менялся.
- **Процесс/атрибуция:** коммит `474c3ac` — `implementer <implementer@kriegsspiel.local>`, trailer `Made-by-Agent: implementer`; ветка задачи от `2245237`, ровно 1 коммит.
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-54a-unit-aware-strength` → `verifier/T-54a-r1` (`--no-ff`, merge `3b1530d`); `wave/5-v04-foundation` переведена fast-forward на этот отчётный коммит. Атрибуция merge: `verifier <verifier@kriegsspiel.local>`, `Made-by-Agent: verifier`.
- **Post-merge в контексте волны:** сборка/`ExecutionResolverTests`+`ReportTests`/full suite → PASS (см. выше).

## Процесс (трение)
- Неоднозначность: handoff указывает строки карточки 308–343, фактический диапазон `T-54a` в `wave/todo.md` — 308–336 (до `### T-55`); расхождение навигационное, решения не меняет.
- Окружение: `wave/5-v04-foundation` checked-out в worktree `ks-architect-wave5-v04-foundation` с посторонним staged-изменением `todo.md` (r4→r3, вне scope T-54a). Чтобы не включать чужую правку в merge-commit, per-task merge и отчёт выполнены в собственном worktree `ks-verifier-T-54a-r1` (`verifier/T-54a-r1`), волна продвинута fast-forward без коммита в чужой ветке.
- Оценка доп. затрат: низко (малый diff, проверки воспроизведены полностью).

## Заметки / tech-debt
- TD нет; задача закрывается без техдолга. Legacy `IReportFactory.Create` остаётся временным scaffolding для ещё не переведённых потребителей (`ReconResolver` → `T-58`), как явно допускает карточка/план; удаление — `T-65`.
