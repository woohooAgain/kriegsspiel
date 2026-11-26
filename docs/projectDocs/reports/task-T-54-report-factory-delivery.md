# Отчёт задачи T-54 — донесения (`ReportFactory`/`ReportDeliveryResolver`)

- **T-54:** `ReportFactory` (confidence, `ReportedOwnStrength`/`ReportedEnemyStrength`, honesty-искажение) и `ReportDeliveryResolver` (потеря, `deliveryMinute = eventMinute + T_leg_back`, фиксация `T_leg_back`, отсутствие слияния версий) под `rules.md` v0.4 §4/§6; `ReportTests`.
- **Волна / ветка волны:** Wave 5, `wave/5-v04-foundation` @ `85e1dd3` (база — `master@90acbe6`).
- **Ветка задачи / MR:** `task/T-54-report-factory-delivery` @ `6391e2a` (код-коммит `ceeb485`), worktree implementer `/tmp/opencode/ks-implementer-T-54-r1`; MR не создавался (передача по refs ветки).
- **Раунды:** r1 — `docs/projectDocs/reports/handoffs/implementer-T-54-r1.md` (`6391e2a`).
- **Финализировал и закоммитил отчёт (verifier, после проверок):** этот коммит в `verifier/T-54-r1`.

## Выполнено (по пунктам «Что сделать»)
1. `ReportFactory`:
   - `NormalizeConfidence` по §6: `Arrival`/`Impossible`/`Request` (Clarification) → 3; `Contact` → 2; `QueryAnswer` → 2 (текущий узел) / 1 (соседний); `Recon` → 3 при успехе / 1 при провале; прочие — clamp 1..3.
   - `CreateWithHonesty`: `ReportedOwnStrength == null` для `ReportKind.Recon`; для остальных — `strength × (1 + (3 − honesty) × 0.12)` при honesty < 3. `ReportedEnemyStrength` = 0 при отсутствии наблюдённых противников / провале recon, иначе сумма strength противников с обратным искажением `× (1 − (3 − honesty) × 0.12)`.
   - Legacy-искажение `facts`/текста и «сокрытие потерь» удалены; honesty влияет только на strength-поля §6.
   - `Create` (контракт `IReportFactory`) нормализует confidence и заполняет `ObservedZoneId` из facts; сигнатура сохранена (non-owned `ExecutionResolver.DefaultReportFactory`/`StatusReport`).
   - `TemplateReportNarrator` сохраняет v0.4-поля (`ObservedZoneId`, `ReportedOwnStrength`, `ReportedEnemyStrength`).
2. `ReportDeliveryResolver`:
   - Потеря `rand() < p_report_loss` (по умолчанию `FrictionTable.Hq.ReportLossBase`, опциональный override) → `ReportLost` + `LostReports`, без доставки.
   - Иначе `DeliveryMinute = EventMinute + T_leg_back`; `T_leg_back` через `PathPlanner.CourierLegBackMinutes`, для edge-локации — от `ToZoneId`, минимум 5.
   - `T_leg_back` фиксируется на событие в `Report.Facts["tLegBack"]` и не пересчитывается; `DeliveryMinute` идемпотентен.
   - Удалена legacy-связка `ReportLossProbability`/`RouteHostility`/`OrderLossProbability`; пул курьеров штаба для Report не используется.
   - Версии не сливаются: разные `Report.Id` с одинаковым `FactKey` добавляются независимо; повтор того же `Id` не дублируется.
   - PRNG — существующий `IRandomSource` (`DeterministicRandomSource`/`RandomSourceAdapter`).
3. `ReportTests` полностью переписан под §6 (15 тестов): confidence-матрица, обязательные поля/границы 1..3, recon own strength null, honesty 0.12, enemy strength 0/обратное искажение, потеря/доставка, `eventMinute + T_leg_back` с минимумом 5 и edge `ToZoneId`, фиксация `T_leg_back`, отсутствие слияния версий, детерминизм по seed, сохранение v0.4-полей narrator'ом.

## Изменённые/созданные файлы (только ownership)
- `Kriegsspiel.Core/Reporting/IReportNarrator.cs`
- `Kriegsspiel.Core.Tests/ReportTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-54-r1.md` (process-артефакт)
- `Kriegsspiel.Core/Contracts/IReportFactory.cs` — без изменений (сигнатура контракта сохранена; unit-aware расчёт вынесен в `CreateWithHonesty`).
- Вне ownership: `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, чужие файлы/проекты — не менялись (`git diff --name-only 85e1dd3..6391e2a` = только 3 файла выше).

## Проверки (evidence refs)
- Pre-merge (worktree `/tmp/opencode/ks-verifier-T-54-r1`, detached `task/T-54-report-factory-delivery` @ `6391e2a`; повторено verifier):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ReportTests"` → 15/15 passed.
  - `dotnet test Kriegsspiel.slnx` → 176/176 passed.
  - `git diff --check` → чисто.
- Post-merge (ветка `verifier/T-54-r1`, merge `b938a5a`, контекст волны после T-50–T-53):
  - `dotnet build Kriegsspiel.slnx` → 0 Warning(s), 0 Error(s).
  - `ReportTests` → 15/15 passed.
  - `dotnet test Kriegsspiel.slnx` → 176/176 passed.

## Верификация (verifier)
- **Вердикт: verified.**
- Соответствие `rules.md` v0.4 §4/§6: confidence-матрица, `ReportedOwnStrength = null` для recon, honesty-искажение 0.12 (в обе стороны), потеря `rand() < p_report_loss`, `deliveryMinute = eventMinute + T_leg_back` (min 5, edge от `ToZoneId`), фиксация `T_leg_back`, отсутствие слияния версий, непричастность пула курьеров штаба — подтверждены кодом и тестами. Новых механик сверх v0.4 нет.
- Ownership строгий; `AGENTS.md`/`rules.md`/`todo.md`/чужие файлы не тронуты.
- Новых проектов/объявлений `class|record|struct|interface|enum` в diff нет; путей `*/Mvp/*` нет; `Kriegsspiel.slnx` не менялся.
- Тесты переписаны под v0.4, старые extended-проверки донесений удалены; новых тестовых классов нет.
- Техдолга перехода нет (новых `TD-*`; `tech-debt.md` не менялся).
- Процесс/атрибуция: коммиты `ceeb485`/`6391e2a` с `Made-by-Agent: implementer`; в чужие ветки/`master` коммитов нет.
- **Merge в ветку волны:** выполнен per-task (без batch): `task/T-54-report-factory-delivery` → `verifier/T-54-r1` (`--no-ff`, merge `b938a5a`); `wave/5-v04-foundation` переведена fast-forward. Атрибуция merge: `verifier <verifier@kriegsspiel.local>`, `Made-by-Agent: verifier`.
- **Post-merge в контексте волны:** сборка/`ReportTests`/full suite → PASS (см. выше).

## Процесс (трение)
- Неоднозначность: §6 различает `QueryAnswer` по текущему/соседнему узлу, но в домене нет признака узла; использован confidence вызывающего (2/1) без нового типа — согласуется с ограничением «новых типов нет» (документировано в handoff).
- Ограничение ownership: unit-aware расчёт strength реализован в `CreateWithHonesty`, т.к. изменение `IReportFactory` сломало бы non-owned `ExecutionResolver.DefaultReportFactory`/`StatusReport`. Сигнатура `Create` сохранена.
- Оценка доп. затрат: средне (полный переписанный тест-класс, семантика §6 без изменения домена).

## Заметки / tech-debt
- TD нет.
- Наблюдение (не блокирует, вне ownership T-54): реальный legacy-путь `ExecutionResolver.StatusReport` создаёт `Arrival`/`Impossible`/`Status` через `IReportFactory.Create` (без `Unit`), поэтому `ReportedOwnStrength`/`ReportedEnemyStrength` в этих донесениях остаются `null`. Unit-aware §6-поля гарантированы только через `CreateWithHonesty`. Ожидаемо закрывается при интеграции `ReportDeliveryResolver`/`ReportFactory` в `T-55`/`T-58` (карточки уже ссылаются на существующие фабрику/резолвер). Рекомендация: учесть при wave-проверке Wave 6 / решение о `TD-*` — `architect`.
