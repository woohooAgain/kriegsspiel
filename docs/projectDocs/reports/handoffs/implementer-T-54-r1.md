# Handoff: T-54 — донесения (`ReportFactory`/`ReportDeliveryResolver`), round 1

## Задача и refs
- **T-54**, Wave 5, покрывает `docs/gameDocs/rules.md` §4 (обратное плечо) и §6 (структура/confidence/потеря/доставка); трассы K/D.
- Карточка: `docs/projectDocs/todo.md` (`T-54`, строки 269–292); сценарий Wave 5: `docs/projectDocs/todo.md` (строки 94–103).
- Gate: `docs/projectDocs/reports/handoffs/verifier-gate-mvp-transition-r3.md`.
- **База (ветка волны):** `wave/5-v04-foundation` @ `85e1dd3`.
- **Ветка задачи:** `task/T-54-report-factory-delivery`.
- **Worktree:** `/tmp/opencode/ks-implementer-T-54-r1`.
- **MR:** не создан; передача по ref ветки/коммитов, per-task merge выполняет `verifier`. Merge не выполнялся.
- **Ownership:** изменены только `Kriegsspiel.Core/Reporting/IReportNarrator.cs`, `Kriegsspiel.Core.Tests/ReportTests.cs` (плюс этот handoff). `Kriegsspiel.Core/Contracts/IReportFactory.cs` оставлен без изменений.

## Что сделано (v0.4, in-place, без новых типов)
- **`ReportFactory`**:
  - `NormalizeConfidence` приведён к §6: `Arrival`/`Impossible`/`Request` (Clarification) → 3; `Contact` → 2; `QueryAnswer` → 2 для текущего узла и 1 для соседнего (по confidence вызывающего); `Recon` → 3 при успехе и 1 при провале; прочие — clamp 1..3.
  - `CreateWithHonesty` вычисляет `ReportedOwnStrength`: `null` для `ReportKind.Recon`; иначе `strength × (1 + (3 − honesty) × 0.12)` при honesty < 3.
  - `ReportedEnemyStrength`: 0 при отсутствии наблюдённых противников / провале recon; иначе сумма strength противников в наблюдённой зоне с обратным искажением `× (1 − (3 − honesty) × 0.12)`.
  - Удалено legacy-искажение `facts`/текста и «сокрытие потерь»; honesty влияет только на strength-поля по §6.
  - `Create` (контракт `IReportFactory`) нормализует confidence и заполняет `ObservedZoneId` из facts; signature сохранена, т.к. её реализует/вызывает non-owned код (`ExecutionResolver.DefaultReportFactory`).
  - `TemplateReportNarrator` сохраняет v0.4-поля (`ObservedZoneId`, `ReportedOwnStrength`, `ReportedEnemyStrength`).
- **`ReportDeliveryResolver`**:
  - Потеря: `rand() < p_report_loss` → `ReportLost` + `LostReports`, без доставки. `p_report_loss` берётся из `FrictionTable.Hq.ReportLossBase` (опционально переопределяется параметром).
  - Иначе `DeliveryMinute = EventMinute + T_leg_back`; `T_leg_back` через `PathPlanner.CourierLegBackMinutes`, для edge-локации — от `ToZoneId`, минимум 5.
  - `T_leg_back` фиксируется на событие в `Report.Facts["tLegBack"]` и повторно не пересчитывается.
  - Удалена legacy-связка с hostility/потерей приказов; пул курьеров штаба не используется.
  - Версии не сливаются: разные `Report.Id` с одинаковым `FactKey` добавляются независимо; повтор того же `Report.Id` не дублируется.
  - PRNG — существующий `IRandomSource` (в т.ч. `DeterministicRandomSource`); `Random` оборачивается в существующий `RandomSourceAdapter`.
- **`ReportTests`** полностью переписан под §6: confidence-матрица, обязательные поля/границы, recon own strength null, honesty 0.12, enemy strength 0/reverse-distortion, потеря/доставка, `eventMinute + T_leg_back` и min 5, фиксация T_leg_back, отсутствие слияния версий, детерминизм по seed, сохранение v0.4-полей narrator'ом.

## Отклонения / решения
- **`IReportFactory` не менялся:** его signature используется non-owned `ExecutionResolver.DefaultReportFactory` и другими потребителями; unit-aware расчёт strength сделан через существующий `CreateWithHonesty` (добавлены optional `observedZoneId`/`observedEnemies` в конец). Это сохраняет сборку всего решения без правок чужих файлов.
- **Confidence для `QueryAnswer`:** §6 различает текущий/соседний узел, но в `ReportKind` нет отдельного признака; используем confidence вызывающего (2/1) как v0.4-контракт.
- **`ReportKind.Request` = ClarificationRequest** (отдельного kind в домене нет; новый тип запрещён).
- **`p_report_loss`:** `GameState` не хранит scenario `reportLossProbability`; резолвер по умолчанию использует `FrictionTable.Hq.ReportLossBase` и принимает явное значение параметром для будущей интеграции (T-55+).
- Новых `class/record/struct/interface/enum` не добавлено; проектов/путей `*/Mvp/*` нет.

## Изменённые файлы (ownership)
- `Kriegsspiel.Core/Reporting/IReportNarrator.cs`
- `Kriegsspiel.Core.Tests/ReportTests.cs`
- `docs/projectDocs/reports/handoffs/implementer-T-54-r1.md` (этот handoff)

`AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md`, `IReportFactory.cs`, чужие ветки/задачи не тронуты.

## Проверки (команда → результат)
- `dotnet build Kriegsspiel.slnx` → **PASS** (0 Warning(s), 0 Error(s)).
- `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ReportTests"` → **PASS** (15/15 passed, 0 failed, 0 skipped).
- `dotnet test Kriegsspiel.slnx` → **PASS** (176/176 passed, 0 failed, 0 skipped).
- `git diff --check` → чисто.
- Проверка типов: diff по коду не содержит добавленных объявлений `class|record|struct|interface|enum`; новых `*.csproj`/проектов и путей `*/Mvp/*` нет.

## Утверждения T-54 (закрытие)
- Confidence соответствует §6; обязательные поля и границы 1..3 соблюдены.
- `ReportKind.Recon` даёт `ReportedOwnStrength == null`; для остальных own strength искажается на 0.12 за уровень honesty.
- `ReportedEnemyStrength` = 0 для провала recon/отсутствия контакта, иначе сумма с обратным искажением.
- Потеря срабатывает при `rand() < p_report_loss`; создаётся `ReportLost`, доставки нет.
- `DeliveryMinute = EventMinute + T_leg_back`, min 5, на ребре — от `ToZoneId`; значение фиксируется на событие.
- Версии/fact key не сливаются; пул курьеров штаба для Report не используется.

## TD / процесс
- Новых `TD-*` нет; задача закрывается без техдолга.
- Процессное трение: среднее. Основное ограничение — переписать семантику донесений, не меняя домен/интерфейс и не ломая non-owned потребителей `IReportFactory`; решено расширением существующего `CreateWithHonesty` optional-параметрами.

## Для verifier
- Проверить diff `85e1dd3..<task-head>` по ownership и отсутствию новых объявлений типов/проектов.
- Повторить: `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ReportTests"`; `dotnet test Kriegsspiel.slnx`.
- Merge — per-task в `wave/5-v04-foundation`, затем сценарий Wave 5 (включая `ReportTests` и общие команды).
