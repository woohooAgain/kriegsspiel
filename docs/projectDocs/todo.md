# TODO: переход на упрощённый MVP-канон `rules.md` v0.4

Источник требований: `docs/gameDocs/rules.md` (редакция «Ядро движка v0.4», упрощённый MVP-scope). `docs/gameDocs/rules_extended.md` — только справка/архив, **не** целевые требования.

## База плана и границы

- **База плана:** ветка `docs/rules-simplified-mvp` @ `adb8ba4` (parent `master` @ `21357d4`). В базе уже лежит новый `rules.md`, архив `rules_extended.md` и соответствующая запись в `structure.md`; новых кодовых файлов база не содержит.
- **Не базироваться** на `process/commit-agent-attribution*` (посторонний concurrent commit).
- **Предыдущий план** (`todo.md` extended-правил, Wave 0–4) заменён: он описывал расширенные механики, которых нет в новом каноне. Исторические отчёты сохранены в `docs/projectDocs/reports/wave-0..4-report.md`; старые карточки доступны в git history до `adb8ba4`. Обратной силы нет: старые отчёты не переписываются.
- **FS-*:** `N/A` — это миграция действующего канона `rules.md` на упрощённый MVP, а не новая фича/механика; отдельная `FS-*` не требуется. Трассировка идёт напрямую к `rules.md §1–§7`.
- **Учтённый техдолг:** активных `open`/`planned` записей до этой декомпозиции не было. В рамках планирования зафиксированы `TD-001`–`TD-003` (legacy cleanup, AI/doctrine, миграция оставшихся сценариев) как **вне scope** этого MVP-плана; причины — в `docs/projectDocs/tech-debt.md`.
- **Окружение (факт на момент планирования):** `dotnet 10.0.404` доступен; `script` (util-linux 2.40.2) доступен для TTY-прогонов Spectre; remote `origin` есть, CI/gh-пайплайна нет. Ограничения фиксировать доказательствами, не выдавать за успех.

## Стратегия миграции (strangler, без «big bang»)

Новый упрощённый канон реализуется **рядом** с legacy extended-движком, в отдельном namespace:

- `Kriegsspiel.Core/Mvp/*` — новая доменная модель, контент-адаптер, резолверы, арбитр, assessment.
- `Kriegsspiel.Console/Mvp/*` — MVP-команды и игровой цикл; включаются конфигом `Engine=Mvp`.
- Legacy extended-код (`Kriegsspiel.Core/*`, `Kriegsspiel.AI/*`, старые команды/тесты) **замораживается**: не расширяется, не используется MVP-режимом, но остаётся в решении, чтобы каждая задача и каждая волна собирались и проходили тесты.
- Legacy-механики (morale/fatigue/contact geometry/ветви/ограничения/режимы движения/пленные/гражданские/груз/отложенные потери/слияние `fact key`/расширенные метрики) **не входят** в MVP и не добавляются. Их удаление — отдельный cleanup после приёмки (`TD-001`).
- Контент: MVP-поля добавляются в существующие сценарии **аддитивно**; legacy-загрузчик игнорирует неизвестные поля. Новый `mvp_01` задаёт все MVP-поля явно. Оставшиеся legacy-сценарии (`fs005_scale_demo`, `river_line_10x10`) не мигрируются в этом плане (`TD-003`).
- AI/LLM и `EnemyDoctrine` — внешние контроллеры extended-формата; в MVP-ядро не входят. Для MVP используется детерминированный скриптовый контроллер противника. Приведение AI к simplified `Order` — отдельный `TD-002`.

Почему так: смена доменной модели «на месте» ломает сборку в каждой task-ветке (Core/Console/AI/тесты связаны), а gate требует зелёные build/test на каждой задаче и волне. Strangler даёт непересекающийся ownership, параллельность и проверяемость.

## Формат и правила работы

- **Формат задачи:** `ID`, `Волна`, `Покрывает`, `Владеет файлами`, `Зависимости`, `Что сделать`, `Как проверить`, `Готово, если`. Задача выдаётся `implementer` без дополнительной декомпозиции.
- **File ownership:** исполнитель меняет только файлы из `Владеет файлами` (включая `(новый)`). Legacy-файлы вне ownership не трогать. Новые MVP-файлы не должны импортировать legacy-типы extended-механик.
- **Волны:** внутри волны задачи не пересекаются по файлам и запускаются группами launch-плана. Следующая волна — после merge предыдущей и wave-gate.
- **Валидация/merge:** строго по одной `T-XX`; `implementer` не мержит; `verifier` мержит после `verified`; batch запрещён.
- **Gate:** у каждой задачи есть воспроизводимые `Как проверить` и `Готово, если`; у волны — сценарий проверок. При недостаточности ветка волны не создаётся.
- **Канонические команды:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx` (полный набор: legacy + MVP); точечно — `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "<Mvp...>"`.
- **Ограничения:** код без комментариев, если не просили; `AGENTS.md` и `docs/gameDocs/rules.md` не менять; `docs/projectDocs/todo.md` правит только `architect`.
- **Трассировка:** `Покрывает` ссылается на `docs/gameDocs/rules.md §1–§7`; трассы K/B/D — где применимо. Не придумывать несуществующие ID.

## Карта волн

| Волна | Задачи | Зависимости | Launch-план (группы) | Сценарий проверок волны |
|---|---|---|---|---|
| Wave 5 — MVP-фундамент | `T-30`, `T-31`, `T-32`, `T-33` | нет; `T-31` после `T-30`; `T-32`/`T-33` после `T-30`+`T-31` | G1=`T-30`; G2=`T-31`; G3=`T-32`,`T-33` (параллельно, файлы не пересекаются) | См. «Wave 5 — сценарий». Кратко: build + полный test + фильтр Mvp-фундамента; проверки домена/графа/часов/RNG/контента/донесений. |
| Wave 6 — MVP-механики ядра | `T-34`–`T-39` | Wave 5; межзадачные вызовы только через контракты `T-30` | G1=`T-34`,`T-35`,`T-36`,`T-37`,`T-38`,`T-39` (параллельно) | См. «Wave 6 — сценарий». Кратко: build + полный test + фильтры transmission/order/execution/combat/recon/victory; проверки формул канона §4–§7. |
| Wave 7 — MVP-интеграция и режим запуска | `T-40`, `T-41`, `T-42a`, `T-42b`, `T-42c`, `T-43` | Wave 6; `T-41`/`T-42a`/`T-42b`/`T-43` после `T-40`; `T-42c` после `T-41`/`T-42a`/`T-42b`/`T-43` | G1=`T-40`; G2=`T-41`,`T-42a`,`T-42b`,`T-43`; G3=`T-42c` | См. «Wave 7 — сценарий». Кратко: build + полный test + Mvp integration/scenario + TTY-прогон `--Engine Mvp --Scenario mvp_01`; legacy-регрессия. |
| Wave 8 — документация/структура | `T-44` | Wave 7 | G1=`T-44` (роль `docs-maintainer`, не `implementer`) | См. «Wave 8 — сценарий». Кратко: build/test не затронуты; `structure.md`/`readme.md` отражают Mvp-файлы и режим; `AGENTS.md`/`rules.md` не изменены. |

## Сценарии проверок волн (детально, для verifier)

Общие правила:
- Выполнять **на ветке волны** после merge всех задач волны, до merge в `main`.
- `dotnet build Kriegsspiel.slnx` — успех, 0 ошибок; `dotnet test Kriegsspiel.slnx` — все тесты (legacy + MVP) зелёные. В окружении без `dotnet` — ограничение с доказательствами, не успех.
- «Дублировать `Как проверить` `T-XX`» = выполнить команды/шаги задачи и подтвердить её утверждения на состоянии ветки волны.
- MVP-тесты находятся в `Kriegsspiel.Core.Tests/Mvp/*`; legacy-тесты не удаляются и не переписываются в Wave 5–7.

### Wave 5 — сценарий проверок волны
- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpDomainTests|MvpGraphPathTests|MvpClockTests|MvpRandomTests|MvpContentLoaderTests|MvpReportTests"`.
- **Проверки добавленных механик (дублировать из задач):**
  - `T-30` (домен/контракты/граф): `MvpDomainTests`, `MvpGraphPathTests`. Утверждения: `IsCombatEffective` = только `strength >= 0.30 * initialStrength`; location node/edge; один `pendingOrder`; `Order` только move/hold/recon + targetZone/deadline/forbiddenZone; `Report` confidence 1..3 и обязательные поля; `GameState` содержит курьеров/очереди/отчёты/лог; Dijkstra по `travelTime` и `courierTime`, `offroad*0.8` только при `deadline==null`, `forbiddenZone==start` запрещает возврат, `T_leg >= 5`.
  - `T-31` (часы/RNG): `MvpClockTests`, `MvpRandomTests`. Утверждения: стабильный порядок `(minute, phase, sequence)`; фазы Dispatch→Delivery→Movement→Combat→ReportDelivery→Scheduled; запрет планирования в прошлое; `PendingCount`; одинаковый seed → одинаковая последовательность; `Restore` возвращает состояние.
  - `T-32` (контент): `MvpContentLoaderTests`. Утверждения: `test_01`/`crossing_01` загружаются MVP-загрузчиком (`mvp_01` — зона T-41); негативные кейсы (defenseBonus вне 0..2, observability вне 1..3, travelTime/courierTime <= 0, K<1, вероятности вне [0,1], дубли id, несимметричное ребро, endMinute не кратен 30, seed отсутствует, initialStrength<=0, readiness вне 0..100, ammo<0, traits вне 0..3) дают контролируемую ошибку; legacy-загрузчик и legacy-тесты не сломаны.
  - `T-33` (донесения/лог): `MvpReportTests`. Утверждения: обязательные поля; confidence по §6 (Arrival/Impossible/Clarification=3, Contact/QueryReply=2, Recon fail/adjacent QueryReply=1); `reportedOwnStrength=null` для ReconResult; искажение honesty 0.12; лог хранит все версии без слияния/снижения confidence; потеря донесения `rand()<p_report_loss`; доставка `eventMinute+T_leg_back`.
- **Критерий:** общие команды успешны, проверки `T-30`–`T-33` подтверждены, legacy-регрессий нет.

### Wave 6 — сценарий проверок волны
- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpTransmissionTests|MvpOrderInterpreterTests|MvpExecutionTests|MvpCombatTests|MvpReconTests|MvpObjectiveTests|MvpMetricsTests"`.
- **Проверки добавленных механик (дублировать из задач):**
  - `T-34` (курьеры/запросы/тишина): `MvpTransmissionTests`. Утверждения: приоритет Query>Order, далее FIFO; при занятых курьерах ETA от фактической отправки; `T_leg` min 5 и `deliverAt=t+T_leg`, `returnAt=t+2*T_leg`; потеря на `sentMinute`; возврат `sentMinute+2*T_leg+60`; `QueryReply` в `deliverAt`; `deliveryMinute=deliverAt+T_leg_back`, `expectedReplyAt=deliverAt+T_leg_back+15`; `SilenceDetected`; потерянный курьер — `sentMinute+2*T_leg+30`.
  - `T-35` (понимание приказа): `MvpOrderInterpreterTests`. Утверждения: A=0; +0.70 при targetZone==null и intent!=hold; +0.15 при deadline==null и intent==move; P=clamp(0.03+A*(1+0.15*pedantry-0.10*init),0.03,0.90); конфликт forbiddenZone по pedantry/init; искажение при A>0 и pedantry<2 (target null → случайный смежный; recon — узел через ребро от смежного; target != null → swap move/hold); pedantry>=2 → ClarificationRequest; deadline expiry; pendingOrder перезаписывается и применяется на завершении ребра.
  - `T-36` (движение): `MvpExecutionTests`. Утверждения: `remainingMinutes` уменьшается по 1/мин; завершение ребра и немедленный переход на следующее в тот же тик; `remainingMinutes` нового ребра не уменьшается в этом тике; offroad −5 readiness; фактическое время = `travelTime`; posture march/attack/defense; после move в свободный targetZone effectiveOrder.intent→hold; нет телепорта; hold/recon не двигаются.
  - `T-37` (бой/отход): `MvpCombatTests`. Утверждения: бой на `t%30==0`; Power по §7 (posture march/attack/defense, terrain только defense, ammo 0→0.5); одновременные потери `0.25*Power_enemy/(sum)*strength`; ammo−1, readiness−10; strength 0 → удаление; отход при strength<0.30*initial в смежный узел без противника с min travelTime; иначе уничтожение.
  - `T-38` (разведка/контакт): `MvpReconTests`. Утверждения: `p_detect=clamp(0.25+0.15*observability-edgePenalty,0.10,0.95)`; edgePenalty 0.15 при входе/offroad recon, 0 при периодической проверке в узле; каденция каждый 1-мин тик с deliverAt; успех → один ReconResult confidence 3 и остановка; 30-мин окно → один ReconResult confidence 1; смена приказа сбрасывает окно; Contact confidence 2; recon из соседнего узла не участвует в бою.
  - `T-39` (победа/разбор): `MvpObjectiveTests`, `MvpMetricsTests`. Утверждения: Score=Σ W_v*StateScore; Achieved/Contested/Failed по §7; победа при Score>=WinThreshold; для приказа фиксируются createdMinute/sentMinute/deliverAt/original/effective/status; метрики: avg(deliverAt-createdMinute) по доставленным, доля A>0, доля Distorted/Failed-Rejected; расширенных метрик нет.
- **Критерий:** общие команды успешны, проверки `T-34`–`T-39` подтверждены, legacy-регрессий нет.

### Wave 7 — сценарий проверок волны
- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpArbiterIntegrationTests|MvpScenarioTests|MvpOrderDraftBuilderTests|MvpPresentationTests|MvpPersistenceTests"`.
- **Проверки добавленных/изменённых механик (дублировать из задач):**
  - `T-40` (арбитр/тик): `MvpArbiterIntegrationTests`. Утверждения: порядок шагов §2 (отправка → доставка/интерпретация → движение/обнаружение/deadline → бой+отдых каждые 30 → доставка донесений/возврат курьеров/SilenceDetected); лексикографический порядок id; CommandPhase каждые 30; in-transit донесение не теряется на границе; `TurnResult` содержит только доставленное к target; одинаковый seed → одинаковый EventLog.
  - `T-41` (сценарий/противник): `MvpScenarioTests`. Утверждения: `mvp_01` загружается; скриптовый контроллер подаёт enemy-приказы в том же `Order`-формате на CommandPhase; полный прогон до endMinute детерминирован; победа/поражение считается; нет скрытой истины в MVP-событиях (ядро не читает лишнее).
  - `T-42a` (UI приказа): `MvpOrderDraftBuilderTests`; ручной сценарий. Утверждения: доступны только move/hold/recon, targetZone, deadline, forbiddenZone; нет ветвей/ограничений/режимов движения; предпросмотр A/P; заказ не раскрывает скрытую истину.
  - `T-42b` (карта/журнал/обстановка): `MvpPresentationTests`; ручной сценарий. Утверждения: карта/журнал используют только доставленные донесения/лог и топологию; показывают no-data/empty/occupied, confidence, event/delivery time, все версии без слияния; `QuerySilence` и `ReportLost` различимы; нет чтения скрытой истины.
  - `T-42c` (shell/DI): build + TTY-прогон. `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Engine Mvp --Scenario mvp_01" /tmp/opencode/mvp-smoke.log`; меню содержит ОБСТАНОВКА/КАРТА/ПРИКАЗ/ЗАПРОС/ЖУРНАЛ/ВРЕМЯ/СОХРАНИТЬ/ЗАГРУЗИТЬ/ВЫЙТИ; приказ/время/карта/журнал работают; `Engine=Legacy` не сломан.
  - `T-43` (persistence): `MvpPersistenceTests`. Утверждения: round-trip сохраняет минуту/очередь часов, RNG state/seed, зоны/рёбра/локации юнитов, приказы/effective/pending, курьеров/ETA, in-transit/delivered отчёты, report log, EventLog; после load продолжение детерминировано.
- **TTY/детерминизм:** два прогона `mvp_01` с одинаковым seed дают одинаковый набор событий/донесений (сравнить хэш/refs); вход подавать через `script`/файл, не интерактивно.
- **Legacy-регрессия:** `dotnet test Kriegsspiel.slnx` зелёный; `dotnet run -- --Scenario test_01` (legacy) работает как до волны; diff вне Mvp-файлов/аддитивного контента/`ServiceRegistration`/`appsettings` отсутствует.
- **Критерий:** общие команды, проверки `T-40`–`T-43`, TTY-прогон и legacy-регрессия подтверждены.

### Wave 8 — сценарий проверок волны (docs/structure)
- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx` — без изменений (docs не влияют).
- **Проверки:** `structure.md` содержит все новые `Kriegsspiel.Core/Mvp/*`, `Kriegsspiel.Console/Mvp/*`, `Kriegsspiel.Core.Tests/Mvp/*`, `Content/scenarios/mvp_01.json`; `readme.md` описывает MVP-режим `--Engine Mvp`, legacy-режим и `TD-001`–`TD-003`; `git diff --name-only` по волне содержит только docs; `AGENTS.md` и `docs/gameDocs/rules.md` не изменены.
- **Критерий:** docs полны и консистентны; код/тесты не менялись.

## Карточки задач

### T-30 — MVP: домен, контракты, граф
- **ID:** `T-30`
- **Волна:** Wave 5
- **Покрывает:** `docs/gameDocs/rules.md §1, §2, §3, §4, §5, §6, §7` (доменные типы, состояние, события, контракты, детерминированный граф); трассы K/B/D — в типах знания/донесений/курьеров.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Domain/Side.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Posture.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/CommanderTraits.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/UnitLocation.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Edge.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Zone.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Unit.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/OrderIntent.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/OrderStatus.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Order.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/EffectiveOrder.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/ReportKind.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Report.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/GameEvent.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/GameState.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/Courier.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/ReportLog.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/IMvpClock.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Domain/MvpRules.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Graph/GraphPath.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpRandomSource.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpReportFactory.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpTransmissionResolver.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpOrderInterpreter.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpExecutionResolver.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpCombatResolver.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpReconResolver.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Contracts/IMvpObjectiveEvaluator.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpDomainTests.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpGraphPathTests.cs` (новый)
  - Не трогать другие файлы (legacy `Kriegsspiel.Core/Domain/*`, `Contracts/*`, `Arbitration/*` не менять).
- **Зависимости:** нет.
- **Что сделать:**
  1. Описать домен строго по §3: `Side` (Player/Enemy), `Posture` (March/Attack/Defense), `CommanderTraits` (Init/Pedantry/Honesty 0..3), `Unit` (Id, Side, InitialStrength, Strength, Readiness 0..100, Ammo>=0, `UnitLocation`, EffectiveOrder, PendingOrder, LastMovedMinute/LastFoughtMinute, IsCombatEffective = Strength >= 0.30*InitialStrength). Никаких morale/fatigue/training/experience/contact geometry/capture.
  2. `UnitLocation` — node `{ ZoneId }` или edge `{ EdgeId, FromZoneId, ToZoneId, RemainingMinutes, EdgeType }`; edge-локация создаётся при движении; `PendingOrder` — единичный буфер, новый приказ перезаписывает.
  3. `Order` — Id, UnitId, Intent (Move/Hold/Recon), TargetZoneId?, Deadline?, ForbiddenZoneId?, CreatedMinute, SentMinute?, DeliverAt?, AmbiguityA, Status (Executed/Distorted/FailedRejected). `EffectiveOrder` — источник, intent, target, deadline, forbidden, статус/примечание. Никаких 7 слотов/ветвей/ограничений/режимов движения.
  4. `Report` — Kind (Arrival, Contact, ReconResult, ImpossibleOrder, ClarificationRequest, QueryReply), EventMinute, DeliveryMinute, SourceUnitId, Confidence 1..3, ObservedZoneId?, ReportedOwnStrength?, ReportedEnemyStrength?, Text?. Без `fact key`-слияния.
  5. `GameState` — ScenarioId, `IMvpClock` (интерфейс в T-30, реализация — T-31), Zones, Edges, Units, Orders, Couriers, InTransitReports, DeliveredReports, `ReportLog`, EventLog, Seed/RngState, EndMinute, HqZoneId. `IMvpClock` объявляет `Now`, `PendingCount`, `Schedule`, `AdvanceTo`; это разрывает циклическую зависимость T-30↔T-31.
  6. `ReportLog` — append-only индекс доставленных донесений по зоне/юниту; хранит все версии и не меняет confidence/не сливает.
  7. `GameEvent` — полный набор событий §2–§7: OrderIssued/Dispatched/Delivered/Lost/Interpreted/Impossible/ClarificationRequested, UnitEnteredEdge/Arrived, Contact, ReconResult, CombatTick, UnitDestroyed, UnitRetreated, ReportCreated/Lost/Delivered, CourierSent/Lost/Returned, QueryCreated/QueryReplyCreated, SilenceDetected, RestApplied, VictoryEvaluated. События — `record`-наследники.
  8. `MvpRules` — константы канона (0.30; 0.25; posture 0.5/1.0/1.3; terrain 0.1; ammo 0.5; p_detect 0.25/0.15/0.10/0.95; A 0.70/0.15; P 0.03/0.90; trait 0.15/0.10; T_leg min 5; reply +15; courier lost +30/+60; recon window 30; honesty 0.12 и т.д.).
  9. `GraphPath` — Дейкстра по `travelTime` (вес: travelTime при deadline != null; travelTime*(offroad?0.8:1.0) при deadline == null), обработка `forbiddenZone` (в т.ч. start-зона: выход разрешён, возврат запрещён), Дейкстра по `courierTime` с минимумом 5. Без механик.
  10. Контракты `IMvp*` — минимально достаточные сигнатуры для параллельных Wave 6 (transmission, order interpreter, execution, combat, recon, objective, report factory, random). Реализаций в T-30 нет.
  11. Тесты: инварианты домена, границы confidence, no-data vs empty не как механика слияния, граф/forbidden/T_leg.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpDomainTests|MvpGraphPathTests"`
  - Утверждения: `IsCombatEffective` false при `strength < 0.30*initial`, true при равенстве; edge-локация хранит remainingMinutes; один pendingOrder; Order/Report/GameEvent соответствуют §3–§7; Dijkstra даёт ожидаемые маршруты/времена; forbidden start-зона запрещает возврат; T_leg min 5.
- **Готово, если:**
  - [ ] Все перечисленные Mvp-файлы созданы; legacy-файлы не изменены.
  - [ ] Домен содержит только канонические поля/состояния, без extended-механик.
  - [ ] `GraphPath` реализует оба режима веса и forbidden-правила.
  - [ ] Контракты Wave 6 объявлены; реализаций нет.
  - [ ] `MvpDomainTests`/`MvpGraphPathTests` зелёные; полный legacy-build не сломан.

### T-31 — MVP: детерминированные часы и PRNG
- **ID:** `T-31`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §1 п.6, §2` (детерминизм, 1-минутный тик, порядок событий); трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Domain/MvpClock.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Random/MvpRandom.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpClockTests.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpRandomTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`; реализация `IMvpClock`/`IMvpRandomSource`.
- **Что сделать:**
  1. `MvpClock` реализует `IMvpClock` из T-30: `Now`, `PendingCount`, `Schedule(event, minute, phase)`; ключ `(minute, phase, sequence)`; фазы `Dispatch`, `Delivery`, `Movement`, `Combat`, `ReportDelivery`, `Scheduled`; стабильный порядок; запрет планирования в прошлое; `AdvanceTo(target)`; безопасный просмотр очереди.
  2. Не реализовывать 30-минутную командную паузу в часах — это уровень `MvpArbiter` (T-40); часы дают детерминированный порядок 1-минутных событий.
  3. `MvpRandom`: явный детерминированный PRNG (например, PCG/xorshift, не `System.Random`), `Seed`, `State`, `NextDouble`, `NextInt`, `Chance`, `Restore`; одинаковая последовательность при одинаковом seed/state.
  4. Тесты: порядок фаз/минут/sequence, запрет прошлого, `PendingCount`, повторный seed, restore, независимость от глобального состояния.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpClockTests|MvpRandomTests"`
  - Утверждения: события с одинаковой минутой идут по фазам; sequence сохраняет порядок; `Schedule` в прошлое бросает исключение; `AdvanceTo` не теряет события; `Restore` продолжает ту же последовательность.
- **Готово, если:**
  - [ ] Часы детерминированы и не имеют silent-clamp в прошлое.
  - [ ] PRNG воспроизводим и восстанавливаем.
  - [ ] Тесты часов/RNG покрывают порядок, прошлое, restore.
  - [ ] Legacy `Clock`/`Random` не изменены.

### T-32 — MVP: контент-адаптер и валидация
- **ID:** `T-32`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §1, §2` (ScenarioConfig, граф, курьеры, юниты, цели, seed), трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Content/MvpScenario.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Content/MvpContentLoader.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Content/MvpScenarioFactory.cs` (новый)
  - `Content/scenarios/test_01.json`
  - `Content/scenarios/crossing_01.json`
  - `Kriegsspiel.Core.Tests/Mvp/MvpContentLoaderTests.cs` (новый)
  - Не трогать другие файлы (legacy `ContentLoader.cs`, `Scenario.cs`, `FrictionTable.cs`, `Content/friction.json`, `Content/reports.json`, `fs005_scale_demo.json`, `river_line_10x10.json` не менять).
- **Зависимости:** `T-30`, `T-31` (для `MvpScenarioFactory`/`MvpClock`).
- **Что сделать:**
  1. `MvpScenario`: Id, Name, StartMinute, EndMinute (кратен 30), Seed, HqZoneId, CourierPool {Count>=1, CourierLoss [0,1], ReportLoss [0,1]}, Zones (Id, Name?, DefenseBonus 0..2, Observability 1..3, Edges), Units (Id, Side, InitialStrength>0, Readiness 0..100, Ammo>=0, LocationZoneId, Commander Init/Pedantry/Honesty 0..3), TargetZones (ZoneId, Weight>=0, хотя бы один), WinThreshold (0,1).
  2. `MvpContentLoader` читает те же JSON; MVP-поля `hqZone`, `courierPool`, `targetZones`, `courierTimes` (если нет — для legacy-сценариев допустим документированный fallback `courierTime=travelTime`; для `mvp_01` обязательны явно). Маппинг legacy: `faction` Blue→Player, Red→Enemy; `type=HQ` не создаёт боевой unit, а задаёт `HqZoneId`; `posture` Reserve/Defend→Defense, March→March; `initialStrength` = `initialStrengthSnapshot` ?? `strength`; traits: Initiative→Init, Literalism→Pedantry, ReportHonesty→Honesty.
  3. Валидация §1–§2: связный граф, hq/targets существуют, endMinute кратен 30, K>=1, вероятности [0,1], defenseBonus/observability, travelTime/courierTime>0, симметричные рёбра, уникальные id, seed обязателен, readiness/ammo/traits, initialStrength>0, нет параллельных рёбер (low-наблюдение r6).
  4. Аддитивно добавить MVP-поля в `test_01.json` и `crossing_01.json`: `hqZone` (START/B2), `courierPool` (2/0.05/0.03), `targetZones` (OBJ 1.0 / B3 1.0), `courierTimes` для рёбер. Legacy-загрузчик должен продолжать загружать эти файлы (неизвестные поля игнорируются).
  5. `MvpScenarioFactory` строит `MvpGameState`, валидирует стартовые локации (все в узлах; posture defense по умолчанию), заполняет Zones/Edges/Units/TargetZones/EndMinute/HqZoneId/Seed.
  6. Тесты: позитивная загрузка test_01/crossing_01; негативные кейсы на каждую валидацию; legacy `ContentLoaderTests` остаются зелёными.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpContentLoaderTests|ContentLoaderTests"`
  - Утверждения: test_01/crossing_01 загружаются MVP-загрузчиком; hq/targets/курьеры/рёбра корректны; негативные кейсы бросают `ContentValidationException`; legacy-тесты зелёные.
- **Готово, если:**
  - [ ] MVP-схема покрывает §1–§2 и валидируется.
  - [ ] test_01/crossing_01 дополнены аддитивно; legacy-загрузка не сломана.
  - [ ] `MvpScenarioFactory` создаёт валидное начальное состояние.
  - [ ] Позитивные/негативные Mvp-тесты зелёные.
  - [ ] Legacy content-файлы/загрузчик вне ownership не изменены.

### T-33 — MVP: донесения, append-only лог и доставка
- **ID:** `T-33`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §4` (обратное плечо), `§6` (структура/confidence/потеря/доставка донесений); трассы K/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Reporting/MvpReportFactory.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Reporting/MvpReportLog.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Reporting/MvpReportDelivery.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpReportTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`.
- **Что сделать:**
  1. `MvpReportFactory`: создать Report с обязательными полями; confidence по §6 (3 для Arrival/Impossible/Clarification, 2 для Contact/QueryReply по текущему узлу, 1 для Recon fail/QueryReply о соседнем узле); `ReportedOwnStrength = null` для ReconResult; для остальных — strength * (1+(3-honesty)*0.12) при honesty<3; `ReportedEnemyStrength` = 0 при отсутствии/провале, иначе сумма enemy strength в ObservedZoneId * (1-(3-honesty)*0.12).
  2. `MvpReportLog`: append-only; хранит все доставленные Report по зоне/юниту; версии не сливаются, confidence не пересчитывается; no-data vs empty — по наличию/значению донесения, без отдельной механики слияния.
  3. `MvpReportDelivery`: при триггере `rand()<p_report_loss` → ReportLost, донесение не доставляется; иначе DeliveryMinute = EventMinute + T_leg_back (`GraphPath` от текущего узла юнита; на ребре — от `ToZoneId`; min 5); фиксация T_leg_back на событие; планирование `ReportDelivered` в `MvpClock`.
  4. Не использовать пул курьеров штаба для Report (только собственные вестовые); пул — T-34.
  5. Тесты: confidence/поля, honesty-искажение, ReconResult ownStrength null, отсутствие слияния версий, потеря/доставка, T_leg_back min 5, in-transit.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpReportTests`
  - Утверждения: обязательные поля/границы confidence; `ReconResult.ReportedOwnStrength == null`; honesty<3 искажает strength вверх и enemy strength вниз; лог хранит обе версии без снижения confidence; ReportLost при срабатывании; deliveryMinute = eventMinute + T_leg_back.
- **Готово, если:**
  - [ ] Фабрика донесений соответствует §6.
  - [ ] Лог append-only, без слияния/приоритизации.
  - [ ] Доставка/потеря/T_leg_back реализованы и тестированы.
  - [ ] Пул курьеров не используется для Report.
  - [ ] Legacy `IReportNarrator`/`ReportFactory` не изменены.

### T-34 — MVP: курьеры, отправка, запросы, тишина
- **ID:** `T-34`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 1, §4`; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpCourierPool.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpTransmissionResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpTransmissionTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`, `T-33`; реализация `IMvpTransmissionResolver`.
- **Что сделать:**
  1. Пул штаба: K курьеров; состояния free/outbound/return/lost; очередь Order/Query; приоритет Query выше Order, далее FIFO по `createdMinute`; отправка при свободном курьере в тик `t`, `sentMinute=t`.
  2. `T_leg` — Дейкстра по `courierTime` от hqZone до узла юнита (на ребре — до `ToZoneId`), минимум 5; `deliverAt=t+T_leg`, `returnAt=t+2*T_leg`; ETA пересчитывается от фактической отправки.
  3. Потеря курьера: в `sentMinute` `rand()<p_courier_loss` → `CourierLost`, `deliverAt=null`, возврат `sentMinute+2*T_leg+60`; приказ/запрос не доставляется. Для AAR: `Order.SentMinute=t`, `Order.DeliverAt=null`, `Order.Status=FailedRejected`; неотправленный до `EndMinute` приказ — `SentMinute=null`, `DeliverAt=null`.
  4. Query: в `deliverAt` живой юнит формирует `QueryReply` (событие/Report через T-33); `deliveryMinute=deliverAt+T_leg_back` (если не потеряно донесение); `expectedReplyAt=deliverAt+T_leg_back+15`; если ответа нет — `SilenceDetected(unitId,minute)`; при потере курьера — `sentMinute+2*T_leg+30`.
  5. Тесты: FIFO/приоритет, занятые курьеры, ETA от фактической отправки, потеря/возврат, query reply/silence, min 5.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpTransmissionTests`
  - Утверждения: Query уходит раньше Order при равном времени; FIFO; `deliverAt`/`returnAt` по §4; потеря курьера не создаёт доставку; `SilenceDetected` в ожидаемом окне; T_leg >= 5.
- **Готово, если:**
  - [ ] Пул и резолвер реализуют §4.
  - [ ] Query/Reply/Silence и потеря курьера покрыты тестами.
  - [ ] ETA пересчитывается от фактической отправки.
  - [ ] Report-доставка не занимает пул штаба.

### T-35 — MVP: интерпретация приказа (A/P, forbidden, искажение, deadline)
- **ID:** `T-35`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §3` (pendingOrder), `§5`; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Orders/MvpAmbiguityScorer.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Orders/MvpOrderInterpreter.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpOrderInterpreterTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`; реализация `IMvpOrderInterpreter`.
- **Что сделать:**
  1. A: 0; `+0.70`, если `TargetZoneId==null` и `Intent!=Hold`; `+0.15`, если `Deadline==null` и `Intent==Move`.
  2. P = `clamp(0.03 + A*(1 + 0.15*Pedantry - 0.10*Init), 0.03, 0.90)`; один бросок на приказ после Шага 1.
  3. Шаг 1 (forbiddenZone): если target != null и (target == forbidden или путь с фильтрацией не существует): `Pedantry >= Init` → `EffectiveOrder=null`, юнит defense, `ImpossibleOrder`, конец; иначе `ForbiddenZone=null`, переход к Шагу 2.
  4. Шаг 2 (A>0): `rand()<P`; `Pedantry>=2` → `EffectiveOrder=null`, defense, `ClarificationRequest`; иначе target==null → случайный смежный узел (для Recon — узел через ребро от смежного); target!=null → intent swap Move↔Hold.
  5. Просрочка deadline: `t > Deadline` и цель не достигнута; `Pedantry>=2 && Init<=1` → `EffectiveOrder=null`, defense, `ImpossibleOrder`; иначе продолжать.
  6. `PendingOrder`: приказ на ребре не исполняется сразу, новый перезаписывает; применяется по завершении ребра; смена приказа сбрасывает recon-окно.
  7. Обновлять `Order.Status`: Distorted при искажении, FailedRejected при Impossible/Clarification/deadline; A сохранять в Order.
  8. Тесты: точные A/P, forbidden-конфликт, искажение, deadline, один roll, pendingOrder, recon null/target.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpOrderInterpreterTests`
  - Утверждения: A/P совпадают с §5; forbidden start-зона; pedantry>=init; pedantry>=2 → Clarification; target null → смежный; target != null → swap; deadline-исход; pendingOrder применяется на ребре; ровно один roll.
- **Готово, если:**
  - [ ] A/P и Шаги 1–2 соответствуют §5.
  - [ ] EffectiveOrder формируется/искажается отдельно от исходного Order.
  - [ ] pendingOrder и deadline обработаны.
  - [ ] Тесты покрывают точные формулы и исходы.
  - [ ] Нет ветвей/ограничений/режимов движения.

### T-36 — MVP: движение по рёбрам, позы, deadline
- **ID:** `T-36`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 3, §3 (location/posture), §5 (path/deadline/pendingOrder)`; трассы D/B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpExecutionResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpExecutionTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`; использует контракты `IMvpOrderInterpreter`/`IMvpReconResolver`/`IMvpReportFactory` (реализации T-35/T-38 не блокируют параллельный запуск; в тестах — стабы).
- **Что сделать:**
  1. Начало движения: из узла в ребро `EdgeLocation{RemainingMinutes=travelTime}`; каждый 1-мин тик уменьшать `RemainingMinutes`; при 0 — завершение, offroad `Readiness = max(0, Readiness-5)`, прибытие в `ToZoneId`; обновлять `Unit.LastMovedMinute` (для отдыха T-40).
  2. Немедленный переход на следующее ребро маршрута в тот же тик; `RemainingMinutes` нового ребра в этом тике **не** уменьшается; фактическое время всегда `travelTime` (не `*0.8`).
  3. Позы: на ребре — March; вход в узел с enemy → Attack; завершённый move в свободный target → Defense + `EffectiveOrder.Intent=Hold` с целью v; hold/recon не начинают движение; recon наблюдает из текущего узла. При достижении target/hold установить `Order.Status=Executed` (для AAR T-39).
  4. Проверка обнаружения после движения/прибытия — через `IMvpReconResolver` (каденция §6); deadline expiry — через `IMvpOrderInterpreter`/контракт; pendingOrder — применить на завершении ребра.
  5. Не телепортировать; не двигать hold/recon; не уменьшать readiness на road; offroad только -5.
  6. Тесты: multi-hop, немедленный переход, no-decrement нового ребра, offroad, posture, deadline, pendingOrder, no teleport.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpExecutionTests`
  - Утверждения: remainingMinutes/переходы по §2; no-decrement; offroad -5; posture; move→hold; no teleport; hold/recon stationary.
- **Готово, если:**
  - [ ] Движение/переходы/позы соответствуют §2–§3.
  - [ ] PendingOrder применяется на завершении ребра.
  - [ ] Deadline/обнаружение вызываются через контракты.
  - [ ] Тесты покрывают multi-hop/offroad/posture/no-teleport.
  - [ ] Нет fatigue/movement modes/teleport.

### T-37 — MVP: бой и отступление
- **ID:** `T-37`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 4, §7 (бой/потери/отход)`; трасса B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpCombatResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpCombatTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`; реализация `IMvpCombatResolver`.
- **Что сделать:**
  1. Бой только на `t%30==0`, в каждом узле с юнитами обеих сторон `Strength>0`; recon из соседнего узла не участвует; юниты на рёбрах не участвуют.
  2. `Power = Strength * Readiness/100 * PostureMult * TerrainMult * AmmoMult`; Posture March 0.5/Attack 1.0/Defense 1.3; Terrain `1+0.1*DefenseBonus` только при Defense; Ammo 1.0 при >0, 0.5 при 0.
  3. Если `PowerPlayer+PowerEnemy>0`: одновременные потери для каждого юнита `0.25 * PowerEnemy/(sum) * Strength`; `Ammo=max(0,Ammo-1)`, `Readiness=max(0,Readiness-10)`; `Strength==0` → удалить; обновлять `Unit.LastFoughtMinute` (для отдыха T-40).
  4. Отступление: выживший с `Strength < 0.30*InitialStrength` → смежный узел без противника с минимальным `travelTime`, `EffectiveOrder=null`; если свободных нет — уничтожить.
  5. Contact при начале боя (confidence 2) — через `IMvpReportFactory`/T-38; не создавать extended-отчёты.
  6. Тесты: формулы, terrain только defense, ammo 0, одновременность, удаление, отход/уничтожение, recon не в бою.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpCombatTests`
  - Утверждения: Power/потери по §7; terrain только defense; ammo 0→0.5; strength 0 удалён; отход/уничтожение; recon вне боя.
- **Готово, если:**
  - [ ] Бой реализован без morale/contact geometry/pending casualties.
  - [ ] Потери/ammo/readiness/удаление по §7.
  - [ ] Отступление/уничтожение по §7.
  - [ ] Тесты покрывают формулы и границы.
  - [ ] Contact-донесение идёт через контракт.

### T-38 — MVP: разведка, обнаружение, ReconResult
- **ID:** `T-38`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 3, §6` (p_detect, каденция, confidence, Contact); трасса K.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpReconResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpReconTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31`, `T-33`; реализация `IMvpReconResolver`.
- **Что сделать:**
  1. `p_detect = clamp(0.25 + 0.15*Observability - edgePenalty, 0.10, 0.95)`; edgePenalty 0.15 при входе наблюдателя в v или recon через offroad; 0 при периодической проверке в узле.
  2. Recon-каденция: если `EffectiveOrder.Intent==Recon`, проверять каждый 1-мин тик с `DeliverAt`, пока юнит стоит в узле, из которого `TargetZone` достижим по одному ребру; в наблюдаемый узел не входит; `TargetZone==null` → проверок нет до смены приказа.
  3. Первый успех → один `ReconResult` confidence 3 и остановка наблюдения; иначе по истечении 30-мин окна от `DeliverAt` → один `ReconResult` confidence 1; смена приказа/применение pendingOrder сбрасывает окно.
  4. Contact: при входе/прибытии в узел с enemy и при начале боя — Contact confidence 2; recon из соседнего узла в бою не участвует.
  5. Использовать `MvpReportFactory`/`MvpReportDelivery`; не сливать донесения.
  6. Тесты: p_detect/edgePenalty, каденция, success/fail/30-мин, reset, contact, recon вне боя.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpReconTests`
  - Утверждения: p_detect по формуле; edgePenalty; успех→conf 3/стоп; провал→conf 1 через 30 мин; смена приказа сбрасывает; Contact conf 2; recon не в бою.
- **Готово, если:**
  - [ ] p_detect/каденция/окно соответствуют §6.
  - [ ] Confidence и Contact корректны.
  - [ ] Смена приказа сбрасывает наблюдение.
  - [ ] Тесты покрывают success/fail/reset/contact.
  - [ ] Нет fact-key слияния/скрытой истины.

### T-39 — MVP: победа и AAR-метрики
- **ID:** `T-39`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §7` (Score/StateScore/победа, разбор и 3 метрики); трассы B/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Assessment/MvpObjectiveEvaluator.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Assessment/MvpAfterActionReport.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Assessment/MvpMetrics.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpObjectiveTests.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpMetricsTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-30`, `T-31` (GameState/IMvpClock); реализация `IMvpObjectiveEvaluator`.
- **Что сделать:**
  1. В `EndMinute`: `Score=Σ W_v*StateScore(v)`; Achieved=1.0, если в v есть player-юниты с `IsCombatEffective==true` и нет enemy-боеспособных; Contested=0.5, если боеспособные есть у обеих; Failed=0.0, если player-боеспособных нет; победа при `Score>=WinThreshold`.
  2. AAR: для каждого приказа — CreatedMinute, SentMinute (null, если не отправлен до EndMinute), DeliverAt (null, если потерян/не отправлен), исходный Order, EffectiveOrder, Status (Executed/Distorted/FailedRejected).
  3. Метрики: avg(DeliverAt-CreatedMinute) по доставленным; доля приказов с `A>0`; доля Distorted или FailedRejected. Только эти 3; без reaction/initiative/literalism/civilians.
  4. Тесты: состояния, Score/порог, потерянные/неотправленные, метрики.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "MvpObjectiveTests|MvpMetricsTests"`
  - Утверждения: Achieved/Contested/Failed по §7; Score/порог; null-поля для потерянных/неотправленных; 3 метрики; расширенных метрик нет.
- **Готово, если:**
  - [ ] Score/StateScore/победа по §7.
  - [ ] AAR-поля приказа фиксируются.
  - [ ] Ровно 3 метрики §7.
  - [ ] Тесты покрывают состояния/метрики.
  - [ ] Нет extended-метрик.

### T-40 — MVP: арбитр и порядок тика
- **ID:** `T-40`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §1 п.6, §2` (игровой цикл, порядок шагов, детерминизм), интеграция §3–§7; трассы D/B/K.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpArbiter.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Arbitration/MvpTurnResult.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpArbiterIntegrationTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-34`–`T-39` (все механики Wave 6).
- **Что сделать:**
  1. Единая точка: принять Order/Query на CommandPhase (каждые 30 мин, `minute%30==0 && minute<EndMinute`), затем `advanceTime(30)`; внутри 1-мин тика порядок: 1) отправка депеш; 2) доставка/интерпретация; 3) движение/обнаружение/deadline; 4) при `t%30==0` бой + отдых (+5 readiness тем, кто не двигался/не воевал 30 мин); 5) доставка донесений/возврат курьеров/SilenceDetected.
  2. Лексикографический порядок `id` для юнитов и депеш; единый `MvpRandom`; одинаковый seed+вход → одинаковый EventLog/TurnResult.
  3. In-transit отчёты не теряются на границе `target`; `TurnResult` содержит только доставленное к `target`.
  4. Интегрировать резолверы через контракты; не менять legacy `Arbiter`.
  5. Тесты: таймлайн 06:15–07:15, in-transit, Contact/CombatTick, Silence, OrderLost не как доставка, одинаковый seed → одинаковый лог.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpArbiterIntegrationTests`
  - Утверждения: порядок шагов §2; CommandPhase; in-transit; TurnResult; OrderLost; детерминизм.
- **Готово, если:**
  - [ ] Арбитр реализует цикл §2 и порядок шагов.
  - [ ] Лексикографический порядок и seed-детерминизм подтверждены.
  - [ ] In-transit/граница target корректны.
  - [ ] OrderLost/Silence семантически корректны.
  - [ ] Legacy `Arbiter` не изменён.

### T-41 — MVP: сценарий `mvp_01` и скриптовый противник
- **ID:** `T-41`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §1, §2` (входы сценария, внешний контроллер enemy), §7 (победа); трассы D/B.
- **Владеет файлами:**
  - `Content/scenarios/mvp_01.json` (новый)
  - `Kriegsspiel.Core/Mvp/Controllers/MvpScriptedEnemyController.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpScenarioTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-40`, `T-32`.
- **Что сделать:**
  1. Создать `mvp_01`: player/enemy, hqZone, courierPool, targetZones, seed, 1–2 часа, рёбра road/offroad с travelTime/courierTime, commander traits 0..3, стартовые posture defense.
  2. `MvpScriptedEnemyController`: на CommandPhase возвращает enemy-приказы в том же `Order`-формате (Move/Hold/Recon, targetZone, deadline, forbiddenZone); без LLM; детерминирован по seed/минуте.
  3. Сквозной тест: загрузка/фабрика, прогон до EndMinute, проверка доставки приказов, движения, recon/contact, боя, победы; одинаковый seed → одинаковый EventLog/отчёты.
  4. Не добавлять новых механик; enemy подчиняется тем же резолверам.
  5. Тесты: сценарий валиден, контроллер детерминирован, полный прогон воспроизводим.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpScenarioTests`
  - Утверждения: `mvp_01` загружается; enemy-приказы валидны; полный прогон детерминирован; победа/поражение считается.
- **Готово, если:**
  - [ ] `mvp_01` валиден и покрывает ключевые механики.
  - [ ] Скриптовый контроллер детерминирован и использует тот же Order.
  - [ ] Сквозной тест воспроизводим при одинаковом seed.
  - [ ] Новых механик нет.

### T-42a — MVP: UI приказа
- **ID:** `T-42a`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §5` (move/hold/recon, target, deadline, forbidden); трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Orders/MvpOrderDraftBuilder.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpOrderDraftBuilderTests.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpOrderCommand.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpOrderBuilder.cs` (новый)
  - Не трогать другие файлы (legacy `OrderBuilder.cs`/`OrderCommand.cs` не менять).
- **Зависимости:** `T-40`, `T-35`.
- **Что сделать:**
  1. `MvpOrderDraftBuilder` (Core, чистая логика): выбрать unit, intent Move/Hold/Recon, targetZone (или null для hold), deadline?, forbiddenZone?; закрытые списки; без ветвей/ограничений/режимов движения/7 слотов.
  2. Предпросмотр A и P через `MvpAmbiguityScorer`; не раскрывать скрытую истину.
  3. Console-команда/билдер — тонкая обёртка над Core-билдером; валидировать существование zone/unit.
  4. Тесты Core-билдера; build Console.
  5. Legacy UI не менять.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpOrderDraftBuilderTests`
  - Утверждения: доступны только move/hold/recon + target/deadline/forbidden; A/P предпросмотр; невалидные zone/unit отклоняются; нет extended-полей.
- **Готово, если:**
  - [ ] Core-билдер реализует §5-слоты.
  - [ ] Console-команда компилируется и вызывает Core-билдер.
  - [ ] Предпросмотр A/P есть.
  - [ ] Legacy UI не изменён.

### T-42b — MVP: карта, журнал, обстановка, донесения
- **ID:** `T-42b`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §3 (location/posture), §4 (курьеры/ETA/silence), §6 (донесения/confidence/версии)`; трассы K/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Reporting/MvpPresentationFormatter.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpPresentationTests.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpMapCommand.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpMapRenderer.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpJournalRenderer.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpReportRenderer.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Commands/MvpSituationCommand.cs` (новый)
  - Не трогать другие файлы (legacy renderers/commands не менять).
- **Зависимости:** `T-40`, `T-33`.
- **Что сделать:**
  1. `MvpPresentationFormatter` (Core, чистая логика): карта/топология, no-data/empty/occupied по логу донесений, confidence, event/delivery time, все версии без слияния; журнал приказов/курьеров/ETA/silence/report lost; обстановка/донесения с обязательными полями и возрастом.
  2. Console-рендеры используют только `MvpGameState`/`ReportLog`/EventLog; не читать скрытую истину enemy; различать `QuerySilence`/`ReportLost`/`ReportDelivered`.
  3. Показать свои силы в допустимом объёме; enemy — только по донесениям/логу.
  4. Тесты Core-форматтера; build Console.
  5. Legacy renderers не менять.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpPresentationTests`
  - Утверждения: no-data/empty/occupied; confidence/времена; все версии; silence/lost/delivered различимы; нет скрытой истины.
- **Готово, если:**
  - [ ] Core-форматтер покрывает карту/журнал/обстановку/донесения.
  - [ ] Console-рендеры компилируются и используют только знание/лог.
  - [ ] Версии не сливаются, silence/lost различимы.
  - [ ] Legacy UI не изменён.

### T-42c — MVP: shell/DI и режим `Engine=Mvp`
- **ID:** `T-42c`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §2 (цикл/CommandPhase)`, интеграция Mvp-команд/сохранения в консоль; трассы D/K.
- **Владеет файлами:**
  - `Kriegsspiel.Console/Mvp/MvpGameLoop.cs` (новый)
  - `Kriegsspiel.Console/Startup/ServiceRegistration.cs`
  - `Kriegsspiel.Console/appsettings.json`
  - Не трогать другие файлы.
- **Зависимости:** `T-40`, `T-41`, `T-42a`, `T-42b`, `T-43`.
- **Что сделать:**
  1. В `ServiceRegistration` ветвление по `config["Engine"]` (`Legacy` по умолчанию; `Mvp` — новый режим). В `Mvp` загрузить `MvpContentLoader`/сценарий, зарегистрировать `MvpArbiter`, `MvpRandom`, резолверы, команды, save/load; не инициализировать legacy DI/LLM.
  2. `MvpGameLoop`: меню ОБСТАНОВКА/КАРТА/ПРИКАЗ/ЗАПРОС/ЖУРНАЛ/ВРЕМЯ/СОХРАНИТЬ/ЗАГРУЗИТЬ/ВЫЙТИ; CommandPhase каждые 30; вывод доставленных донесений; `ВРЕМЯ` вызывает `AdvanceTo(now+30)`.
  3. `appsettings.json`: добавить `"Engine": "Legacy"` (по умолчанию) и пример `Mvp`; не менять legacy-настройки.
  4. Проверить TTY-прогон `--Engine Mvp --Scenario mvp_01`; legacy-режим не сломан.
  5. Не менять legacy команды/резолверы.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx`
  - `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Engine Mvp --Scenario mvp_01" /tmp/opencode/mvp-smoke.log`
  - Утверждения: меню 9 действий; MVP-сценарий запускается; приказ/время/карта/журнал работают; `Engine=Legacy` работает как раньше.
- **Готово, если:**
  - [ ] `Engine=Mvp` поднимает MVP-стек без legacy/LLM.
  - [ ] `MvpGameLoop` реализует меню/CommandPhase/время.
  - [ ] TTY-прогон успешен; legacy-режим не сломан.
  - [ ] Legacy `ServiceRegistration`-поведение по умолчанию сохранено.

### T-43 — MVP: persistence (save/load)
- **ID:** `T-43`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §1 п.6, §2` (детерминизм/воспроизводимость), сохранение состояния MVP; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Mvp/Persistence/MvpGameStateDto.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Persistence/MvpPersistenceMapper.cs` (новый)
  - `Kriegsspiel.Core/Mvp/Persistence/MvpPersistenceJson.cs` (новый)
  - `Kriegsspiel.Console/Mvp/Persistence/MvpJsonSaveStore.cs` (новый)
  - `Kriegsspiel.Core.Tests/Mvp/MvpPersistenceTests.cs` (новый)
  - Не трогать другие файлы (legacy `Core/Persistence/*`, `Console/Persistence/JsonSaveStore.cs` не менять).
- **Зависимости:** `T-40`.
- **Что сделать:**
  1. Версионированные DTO для `MvpGameState`: Now/очередь `MvpClock`, seed/RNG state, zones/edges, units/locations, orders/effective/pending, couriers/ETA, in-transit/delivered reports, report log, EventLog, EndMinute/HqZoneId.
  2. Маппинг домен ↔ DTO; не сериализовать живой `MvpGameState` напрямую; схема/версия.
  3. `MvpJsonSaveStore`: save/load в `saves/` (gitignored), стабильные JSON options.
  4. Round-trip тест: состояние после load эквивалентно; продолжение с тем же seed детерминировано.
  5. Legacy persistence не менять.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter MvpPersistenceTests`
  - Утверждения: round-trip сохраняет минуту/очередь/RNG/локации/приказы/курьеров/отчёты/лог/EventLog; продолжение детерминировано.
- **Готово, если:**
  - [ ] DTO покрывают ключевое состояние MVP.
  - [ ] Round-trip проходит.
  - [ ] Save/load не ломает детерминизм.
  - [ ] Legacy persistence не изменён.

### T-44 — MVP: документация и структура
- **ID:** `T-44`
- **Волна:** Wave 8
- **Покрывает:** `AGENTS.md` (актуализация `structure.md`/доков), `docs/projectDocs/instructions.md` (конвенции MVP-режима); механик не добавляет.
- **Владеет файлами:**
  - `docs/projectDocs/structure.md`
  - `readme.md`
  - `docs/projectDocs/instructions.md`
  - Не трогать другие файлы.
- **Зависимости:** Wave 7.
- **Роль:** `docs-maintainer` (не `implementer`).
- **Что сделать:**
  1. Обновить `structure.md`: добавить `Kriegsspiel.Core/Mvp/**`, `Kriegsspiel.Console/Mvp/**`, `Kriegsspiel.Core.Tests/Mvp/**`, `Content/scenarios/mvp_01.json`; отметить legacy как frozen/legacy (без удаления).
  2. Обновить `readme.md`: MVP-режим `--Engine Mvp`, legacy-режим, команды проверки, ограничения MVP, ссылки на `TD-001`–`TD-003`.
  3. Обновить `instructions.md` при необходимости: новые конвенции/Mvp-папки/тестовые фильтры; не менять `AGENTS.md`/`rules.md`.
  4. Проверить, что docs не расходятся с фактической структурой; ничего не выдумывать.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx` (без изменений)
  - `grep -n "Mvp" docs/projectDocs/structure.md readme.md`
  - `git diff --name-only` — только docs-файлы; `AGENTS.md`/`docs/gameDocs/rules.md` не изменены.
- **Готово, если:**
  - [ ] `structure.md` содержит все Mvp-файлы и `mvp_01`.
  - [ ] `readme.md` описывает `Engine=Mvp`/legacy/TD.
  - [ ] `instructions.md` актуален.
  - [ ] `AGENTS.md`/`rules.md` не изменены; код не менялся.

## Launch-план (машиночитаемая часть для orchestrator)

```yaml
base: docs/rules-simplified-mvp@adb8ba4
plan_branch: docs/arch-mvp-simplified-rules-plan
wave_branch_pattern: "wave/<N>-<slug>"
task_branch_pattern: "task/T-XX-<kebab-slug>"
merge_policy: per-task-after-verified (no batch)
waves:
  - id: 5
    slug: mvp-foundation
    groups:
      - [T-30]
      - [T-31]
      - [T-32, T-33]
    merge: "T-30 first; then T-31; then T-32/T-33 in any order"
    gate_ref: "docs/projectDocs/todo.md#wave-5--сценарий-проверок-волны"
  - id: 6
    slug: mvp-core-mechanics
    depends: [5]
    groups:
      - [T-34, T-35, T-36, T-37, T-38, T-39]
    merge: "any order after all verified"
    gate_ref: "docs/projectDocs/todo.md#wave-6--сценарий-проверок-волны"
  - id: 7
    slug: mvp-integration-and-mode
    depends: [6]
    groups:
      - [T-40]
      - [T-41, T-42a, T-42b, T-43]
      - [T-42c]
    merge: "T-40 first; then G2 any order; T-42c after T-41/T-42a/T-42b/T-43"
    gate_ref: "docs/projectDocs/todo.md#wave-7--сценарий-проверок-волны"
  - id: 8
    slug: mvp-docs-structure
    depends: [7]
    owner: docs-maintainer
    groups:
      - [T-44]
    gate_ref: "docs/projectDocs/todo.md#wave-8--сценарий-проверок-волны-docsstructure"
```

## Принятые трактовки и риски

- **r6 low-наблюдения:** recon не инициирует движение — реализуем буквально (юнит должен уже стоять в смежном узле); параллельные рёбра не поддерживаем (валидация T-32). Это не блокеры.
- **Legacy frozen:** в Wave 5–7 запрещено менять поведение legacy extended-движка, кроме аддитивного контента и ветвления `ServiceRegistration` по `Engine`. Legacy-тесты должны оставаться зелёными.
- **AI/LLM:** в MVP не используется; `TD-002` — отдельная задача после приёмки.
- **Удаление legacy:** `TD-001` — отдельный cleanup после приёмки MVP; в этот план не входит.
- **Сценарии:** MVP-режим целево поддерживает `test_01`, `crossing_01`, `mvp_01`; остальные legacy-сценарии — `TD-003`.
- **Блокеры канона:** не выявлены; r6 `passed`. При обнаружении реальной неоднозначности в реализации — эскалация `orchestrator` → `rules-auditor`/`analyst`, без расширения механик.
