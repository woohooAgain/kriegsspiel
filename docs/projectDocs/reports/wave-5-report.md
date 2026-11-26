# Отчёт волны Wave 5 — фундамент `rules.md` v0.4 (`T-50`–`T-54`, `T-54a`)

- **Дата:** 2026-11-26
- **Ветка волны / target:** `wave/5-v04-foundation` @ `8ec2f960e9451dddc0d610a1ad3acf4c39494a5a` (база `master` @ `90acbe6`) → `master` (merge `79b5fa7`)
- **Сценарий проверок волны (architect):** `docs/projectDocs/todo.md` — «Wave 5 — сценарий проверок волны» (`:107-117`, редакция r4 с блоком `T-54a`); карточки `T-50`–`T-54a` (`:154-336`); launch plan `:718-748` (G1=`T-50`; G2=`T-51`,`T-52`,`T-53`; G3=`T-54`; G4=`T-54a`).
- **Канон:** `docs/gameDocs/rules.md` v0.4 (§1–§7).
- **Runtime evidence (r2):** `scenario/wave5-r2` @ `fb9faf93d2204de42aefc7add78012d1d7cc16f7` (создана от `8ec2f96`; меняет только handoff/evidence); `docs/projectDocs/reports/handoffs/scenario-tester-wave5-r2.md`; каталог `docs/projectDocs/reports/handoffs/wave5-r2-evidence/`. Вердикт scenario-tester: `passed`.
- **Worktree/ветка verifier (process-contract §5.1):** `/tmp/opencode/ks-verifier-wave5-r2`, `verifier/wave5-r2` (от `8ec2f96`); mainline-merge — `/tmp/opencode/ks-verifier-wave5-merge-r2`, `verifier/wave5-mainline-merge-r2` (от `master@cfe3d40`). Коммиты только в эти ветки; в `master` — только штатный merge.
- **Предыдущий раунд:** `verifier/wave5-r1` @ `bb0ce7a` — `wave_failed` (латентный §6-разрыв на legacy-пути `ExecutionResolver.StatusReport`); в волну/`master` не вливался. Настоящий отчёт — re-gate после triage `architect` (r4) и корректирующей `T-54a`.
- **Итог:** **`wave_passed`** — build/test/сценарий зелёные; §6-разрыв на resolver-пути закрыт `T-54a` (статическая проверка + runtime-пробник + автотесты); волна влита в `master` (merge `79b5fa7`), отчёт финализирован отдельным merge-коммитом.

## Задачи волны

| T-* | Статус | Ветка/MR (refs) | Вердикт | Отчёт задачи | Примечание |
|---|---|---|---|---|---|
| `T-50` | verified, merged | `task/T-50-domain-v04` @ `55bf554`; merge `2c0ee5e` | verified | `docs/projectDocs/reports/task-T-50-domain-v04.md` | Домен/состояние/донесения/knowledge; `DomainInvariantsTests`+`FogOfWarTests` 34/34 |
| `T-51` | verified, merged | `task/T-51-clock-phase-prng` @ `d34e204`; merge `1559df5` | verified | `docs/projectDocs/reports/task-T-51-clock-phase-prng.md` | Часы/фазы/PRNG; `ClockTests` 20/20 |
| `T-52` | verified, merged | `task/T-52-content-v04` @ `a835d7f`; merge `f31e55e` | verified | `docs/projectDocs/reports/task-T-52-content-v04.md` | Контент v0.4, legacy-сценарии удалены; `ContentLoaderTests` 22/22 |
| `T-53` | verified, merged | `task/T-53-path-planner-movement` @ `b764c4f`; merge `e9526fb` | verified | `docs/projectDocs/reports/task-T-53-path-planner-movement.md` | Путь/движение; `ExecutionResolverTests` 19/19 (включая §6-тесты `T-54a` в том же классе) |
| `T-54` | verified, merged | `task/T-54-report-factory-delivery` @ `6391e2a`; merge `b938a5a` | verified | `docs/projectDocs/reports/task-T-54-report-factory-delivery.md` | Донесения/доставка; `ReportTests` 16/16 |
| `T-54a` | verified, merged | `task/T-54a-unit-aware-strength` @ `474c3ac`; merge `3b1530d` | verified | `docs/projectDocs/reports/task-T-54a-unit-aware-strength.md` | Unit-aware §6 на resolver-пути; `ExecutionResolverTests`+`ReportTests` 35/35 |

## Параллелизм и ownership

- Launch-план r4 соблюдён: G1=`T-50`; G2=`T-51`,`T-52`,`T-53`; G3=`T-54`; G4=`T-54a`. Per-task merge, batch не применялся.
- Внутри параллельных групп пересечений ownership нет. `T-54a` выполнен после `T-53`/`T-54` без параллелизма, как требует карточка (`todo.md:319`).
- Diff волны (`90acbe6..8ec2f96`, 45 путей) соответствует ownership задач: код/тесты `Kriegsspiel.Core`/`Kriegsspiel.Core.Tests`, сценарии `Content/scenarios`, task-handoff/отчёты, `todo.md` (план r4 — коммит `architect`). Новых файлов вне ownership не обнаружено.
- Ветки задач влиты в ветку волны per-task и являются её предками (`task/T-50…T-54a` — все `merge-base --is-ancestor` = true).
- Коммитов в `master`/чужие ветки за время волны не обнаружено (`master` до merge оставался `cfe3d40`); scenario evidence — в собственной ветке `scenario/wave5-r2`.

## Post-merge проверки по задачам (в контексте ветки волны @ `8ec2f96`)

Повторено verifier в `/tmp/opencode/ks-verifier-wave5-r2` (`dotnet 10.0.404`):

- `dotnet build Kriegsspiel.slnx` → **Build succeeded**, `0 Warning(s)`, `0 Error(s)`.
- `dotnet test Kriegsspiel.slnx` → **Failed: 0, Passed: 181, Skipped: 0, Total: 181**.
- Wave 5 filter (`DomainInvariantsTests|FogOfWarTests|ClockTests|ContentLoaderTests|ExecutionResolverTests|ReportTests`) → **Failed: 0, Passed: 111, Skipped: 0, Total: 111**.
- Per-task (отдельные фильтры):
  - `DomainInvariantsTests|FogOfWarTests` → 34/34;
  - `ClockTests` → 20/20;
  - `ContentLoaderTests` → 22/22;
  - `ExecutionResolverTests` → 19/19;
  - `ReportTests` → 16/16;
  - `ExecutionResolverTests|ReportTests` (T-54a) → 35/35.
- Результаты совпадают с runtime-evidence r2 (refs: `wave5-r2-evidence/build.log`, `test-full.log`, `test-wave5-filter.log`, `test-<T-XX>.log`).

## Wave-проверка (re-gate по сценарию architect, на `8ec2f96`)

### Общие проверки
- **Сборка / автотесты:** ok / ok (181/181; Wave 5-фильтр 111/111) — повторено verifier независимо от scenario-tester.
- **Проверки переделанных механик (`T-50`–`T-54a`):** per-task фильтры зелёные (см. выше); утверждения сценария подтверждены именами/содержанием тестов (ref: `wave5-r2-evidence/test-names.txt`, `scenario-tester-wave5-r2.md` §2).
- **Воспроизводимость:** тесты детерминированы (фиксированные сценарии/`new Random(1)`), внешних seed/времени нет; TTY-шаги в сценарий Wave 5 не входят (TTY — Wave 7), `script` не требовался.
- **Окружение:** `dotnet 10.0.404`; `gh`/CI отсутствуют — ограничение, не выдаётся за успех.

### Ограничения волны (diff `90acbe6..8ec2f96`, 45 путей)
- **Новые проекты/`.csproj`/`.slnx`/`.sln`:** нет (`csproj-count=0`); `Kriegsspiel.slnx` не менялся.
- **Новые объявления `class|record|struct|interface|enum` в коде (`*.cs`):** нет (`cs-decl-count=0`; проверка с фильтром `-- '*.cs'` — совпадения в handoff-прозе не учитываются).
- **Пути `*/Mvp/*`:** нет (`mvp-count=0`).
- **`AGENTS.md` / `docs/gameDocs/rules.md`:** не изменены; blob-hashes совпадают с базой (`AGENTS.md` `e690ec7…`, `rules.md` `d9b89f8…`).
- **Legacy-сценарии/тесты:** `fs005_scale_demo.json`, `river_line_10x10.json`, `Fs005ScaleContentTests.cs` удалены; в `Content/scenarios/` остались `test_01.json`, `crossing_01.json` (`Load_LegacyScenariosRemoved` PASS).
- Refs: `wave5-r2-evidence/constraints-check.log`, `diff-name-status.txt`; воспроизведено verifier.

### Закрытие §6-разрыва (`T-54a`) — основание для re-gate
- **Было (r1, `85573dc`):** `ExecutionResolver.StatusReport` создавал Arrival/Impossible/Status/Contact через legacy `IReportFactory.Create`, оставляя `ReportedOwnStrength`/`ReportedEnemyStrength = null` (нарушение `rules.md §6`).
- **Стало (`8ec2f96`):** `StatusReport` вызывает unit-aware `_reports.CreateWithHonesty(...)` с `unit`/`honesty = unit.Traits.ReportHonesty`; для Contact передаются enemy-юниты наблюдаемого узла (`ExecutionResolver.cs:398`). `CreateWithHonesty` заполняет own-strength с honesty-искажением и enemy-strength (`0` без enemy либо сумма по узлу); `null` own-strength остаётся только для `ReportKind.Recon` (соответствует §6).
- **Статическая проверка:** `grep -n "_reports.Create(\|Report.Create(" Kriegsspiel.Core/Arbitration/ExecutionResolver.cs` → совпадений нет (`grep-exit=1`); найден только `_reports.CreateWithHonesty(`. Ref: `wave5-r2-evidence/t54a-legacy-call-grep.log`.
- **Runtime-пробник (r2):** на реальном resolver-пути Arrival/Status/Impossible → own≠`null`, enemy=0, confidence 3/2/3; Contact → own=100, enemy=200, confidence 2. Ref: `wave5-r2-evidence/t54a-resolver-path-probe.log`.
- **Автотесты:** `OnArrive_ReportCarriesHonestyDistortedOwnStrengthAndZeroEnemy`, `ImpossibleReport_CarriesHonestyDistortedOwnStrengthAndZeroEnemy`, `StatusReport_CarriesOwnStrengthAndZeroEnemy`, `OnMove_EnemyContact_ReportSumsEnemyStrengthInObservedZone`, `OnMove_EnemyContact_StopsMovementAndSetsAttackPosture`, `Contract_CreateWithHonesty_ReturnsV04StrengthFields` — PASS (35/35).
- **Вывод:** причина `wave_failed` r1 устранена; явного покрытия в последующих карточках больше не требуется для Wave 5. Legacy `Create` остаётся только временным scaffolding для ещё не переведённых потребителей (`Understanding/Combat/Recon` — `T-56`/`T-57`/`T-58`, удаление — `T-65`), продуктовый путь `ExecutionResolver` его не вызывает.

### Результат re-gate
- Все пункты сценария `todo.md:107-117` подтверждены; корректирующих `T-*`/эскалаций не требуется. **`wave_passed`**.

## Merge ветки волны в основную

- **Статус: выполнен.** `wave/5-v04-foundation` @ `8ec2f96` → `master` (база `cfe3d40`), merge commit **`79b5fa7455a2bc6e1de61c308162882996a3164c`** (`--no-ff`, без конфликтов; parents `cfe3d40` + `8ec2f96`, атрибуция `verifier <verifier@kriegsspiel.local>`, trailer `Made-by-Agent: verifier`).
- Merge выполнен в изолированном worktree `/tmp/opencode/ks-verifier-wave5-merge-r2` (ветка `verifier/wave5-mainline-merge-r2` от `master@cfe3d40`); primary checkout не переключался и не изменялся (в нём остаются несвязанные EOL-правки). `master` переведён fast-forward на merge-коммит.
- **Post-merge на `79b5fa7`:** `dotnet build Kriegsspiel.slnx` → 0 Warning / 0 Error; `dotnet test Kriegsspiel.slnx --no-build` → **181/181**; Wave 5 filter → **111/111**. Ошибок/регрессий нет.
- **Настоящий отчёт** закоммичен в `verifier/wave5-r2` и влит в `master` отдельным merge-коммитом (ref — в Status Envelope, чтобы не вписывать хеш коммита в собственное содержимое); изменений продуктового кода/тестов нет.
- История r1 (`verifier/wave5-r1` @ `bb0ce7a`, `wave_failed`) в `master` не вливалась и не переписывалась.

## Ретро процесса (вклад process-master / факты участников)

- **Вклад `process-master`:** ретро Wave 5 выполнено по артефактам волны (task/wave-отчёты, handoffs r1/r2, история gate `wave_failed`→`T-54a`→`wave_passed`, scenario r1→r2). Обновлён `docs/projectDocs/process-improvement-log.md`: новые `PI-023`–`PI-025`, статусы прошлых мер и явные отсрочки `PI-016`/`PI-021`/`PI-022`. Ref ветки/коммита — в Status Envelope `process-master`; канонический коммит отчёта делает `verifier`.
- **Что сработало:**
  - file-first handoff + refs, per-task verify/merge без batch, независимое повторение build/test verifier: 6/6 задач verified, §6-разрыв найден до `master`; post-merge 181/181.
  - `PI-004` (атрибуция): `git log 90acbe6..8ec2f96` — все 24 коммита с email роли и `Made-by-Agent`; кросс-веточных нет → **verified**.
  - `PI-005` (worktree): все роли в собственных linked worktree; `master` до штатного merge оставался `cfe3d40` → **verified** (наблюдение: worktree ветки волны архитектора нёс постороннее staged-изменение `todo.md`; потребители обошли отдельным worktree).
  - `PI-001`/`PI-003`: gate r3 сверил базу плана без `gate_failed`; manual merge с полным локальным evidence-пакетом → **verified**.
- **Трение и утечка (refs/оценка):**
  - **Среднее (r1):** §6-разрыв на `ExecutionResolver.StatusReport` не имел владельца на «шве» `T-53` (ExecutionResolver) ↔ `T-54` (ReportFactory); известный дефект записан как неблокирующая заметка с неверным перенаправлением на `T-55`/`T-58`; scenario-tester r1 вернул `passed` с латентной находкой, а verifier — `wave_failed`. Утечка в `master` остановлена на wave-gate, но ценой дополнительного цикла и `T-54a`. → новые `PI-023` (integration seam в сценарии волны) и `PI-024` (эскалация известного нарушения канона + единый критерий вердикта).
  - **Низкое (r2):** constraint-check «нет новых объявлений типов» без `-- '*.cs'` дал 19 ложных совпадений в handoff-прозе → `PI-025`.
  - **Низкое:** дрейф линейных ref (номера строк карточки `T-54a`: 308–343 vs 308–336; gate-handoff вне базы волны `90acbe6`) — наблюдение в `PI-019`.
- **Циклы/регрессии:** не выявлено. `wave_failed` r1 — не регрессия ранее закрытого дефекта, а впервые обнаруженный латентный разрыв; история r1 (`bb0ce7a`) в `master` не вливалась, трассировка «причина → `T-54a`» зафиксирована (`todo.md`, `86cf112`).
- **Эффект прошлых мер / сложность:** `PI-017`–`PI-020` появились по факту трения `T-53` и в базе Wave 5 ещё не действовали — эффект ожидается в Wave 6; `PI-002` без данных (в Wave 5 нет интерактивных CLI/TTY). `PI-016`/`PI-021`/`PI-022` явно отложены с причинами (анти-сложность/вне полномочий). `PI-023`/`PI-024` не добавляют артефактов (уточняют сценарий и критерий вердикта), `PI-025` — один pathspec; рост бюрократии минимальный, детерминизм повышается.
- **Ссылки на `PI-*`:** `docs/projectDocs/process-improvement-log.md` — `PI-001`–`PI-005`, `PI-016`, `PI-017`–`PI-025`; ref работы — в Status Envelope `process-master`.
- **Маршрут ретро:** принять `PI-023`/`PI-024`/`PI-025` через `architect`/`verifier`/`orchestrator` (правки сценария волны/gate/ролей) — вне scope этого ретро; затем `architect` — планирование Wave 6.

## Итоги

- **Закрыто задач: 6/6** (`T-50`–`T-54`, `T-54a`) — verified, per-task merged в ветку волны; post-merge проверки зелёные.
- **Build:** 0 errors / 0 warnings; **full suite:** 181/181; **Wave 5 filter:** 111/111; ограничения волны (нет новых проектов/типов/`Mvp`, канон не изменён) соблюдены.
- **§6:** разрыв на resolver-пути закрыт `T-54a` (static + runtime + tests); `wave_failed` r1 снят.
- **Вердикт:** `wave_passed`; волна влита в `master` (`79b5fa7`), отчёт — отдельным merge-коммитом.
- **Блокеры:** нет. **Новые `T-*`/`TD-*`:** нет (переход без техдолга). `AGENTS.md`, `docs/gameDocs/rules.md`, `todo.md` verifier не изменял; продуктовый код/тесты не правились.
- **Маршрут:** ретро Wave 5 выполнено `process-master` (`process/wave5-retro-r1@5f62193`; новые `PI-023`–`PI-025` — `proposed`, `PI-001`/`PI-003`/`PI-004`/`PI-005` — `verified`, `PI-016`/`PI-021`/`PI-022` отложены) и влито `verifier`; далее `architect`/`orchestrator` — принятие `PI-023`–`PI-025` (сценарий волны/gate/роли) и планирование Wave 6.
