# TODO: переход на упрощённый канон `rules.md` v0.4 (in-place, без сохранения совместимости) — план r3

Источник требований: `docs/gameDocs/rules.md` (редакция «Ядро движка v0.4», упрощённый MVP-scope). `docs/gameDocs/rules_extended.md` — только архив/справка, **не** целевые требования.

## База плана и границы

- **База плана:** `master` @ `340829f` (содержит `rules.md` v0.4, архив `rules_extended.md` и актуальный `structure.md`). Канон-предок: `docs/rules-simplified-mvp` @ `adb8ba4`.
- **Ветка плана r3:** `docs/arch-mvp-transition-plan-r3` от `master` @ `340829f`. Работа — в отдельном linked worktree `/tmp/opencode/ks-arch-mvp-transition-r3`; коммиты только в эту ветку.
- **Предыдущий план r2** (`docs/arch-mvp-refactor-plan-r2` @ `13f197a`) **заменён полностью**: он сохранял совместимость, legacy-fallback и выносил удаление extended-кода за пределы плана. Эти требования отменены. Исторические отчёты не переписываются; `T-30`–`T-47` из r2 не выполняются.
- **FS-*:** `N/A` — это миграция действующего канона `rules.md` на упрощённый MVP, а не новая фича/механика. Трассировка — напрямую к `rules.md §1–§7`.
- **Техдолг:** не планируется; записи по переходу не создаются, задачи плана на них не ссылаются.
- **Окружение:** `dotnet 10.0.404`; `script` (util-linux) для TTY-прогонов Spectre; CI/gh-пайплайна нет. Отсутствие среды фиксировать доказательствами, не выдавать за успех.

## Приоритетные требования перехода

1. **Совместимость не сохраняется.** Разрешено ломать существующие API/поведение/тесты, если это требуется для v0.4. Никаких legacy-fallback, dual-engine, `Engine=Legacy|Mvp`, «старых методов ради AI/тестов» и временных обходов в итоговом состоянии.
2. **Тесты не являются контрактом.** Устаревшие тесты и сценарии переписываются или удаляются; добавляются проверки под v0.4. Новые тестовые классы не создаются — проверки добавляются в существующие тестовые классы (иначе — эскалация).
3. **Только необходимые изменения.** Планируется ровно то, что нужно для перехода на v0.4; расширенные механики не добавляются и не «доделываются».
4. **`docs/projectDocs/todo.md` очищен и содержит только этот план.** Старый план r2/r1 в нём не сохраняется.
5. **Без техдолга.** Переходные задачи закрываются в рамках плана; переходные записи не создаются.

## Сохраняющиеся жёсткие ограничения

1. **Не добавлять новые проекты в решение.** `Kriegsspiel.slnx` не получает новых проектов. Удаление LLM-проекта `Kriegsspiel.AI` (v0.4 исключает LLM) допустимо и планируется в `T-64`.
2. **Не создавать новые классы/типы** (`class`, `record`, `struct`, `interface`, `enum`, включая nested/private/тестовые). Разрешено переделывать/удалять существующие типы, менять поля/свойства/методы/конструкторы, добавлять/удалять члены существующих enum. Если без нового типа объективно невозможно — эскалация `orchestrator` с альтернативой, без молчаливого расширения scope.
3. **In-place рефакторинг.** Никаких `Kriegsspiel.Core/Mvp/*`, `Kriegsspiel.Console/Mvp/*`, второго движка или дублирования типов.
4. **Никаких новых механик сверх `rules.md`.** Extended-механики (morale/fatigue/contact geometry/ветви/ограничения/режимы движения/пленные/граждане/груз/отложенные потери/слияние `fact key`/расширенные метрики) не входят в v0.4 и в MVP-путь не попадают.
5. `AGENTS.md` и `docs/gameDocs/rules.md` не меняются. `docs/projectDocs/todo.md` правит только `architect`.
6. Код без комментариев, если явно не просили.

## Промежуточная сборка (не совместимость)

Чтобы каждая task-ветка и волна собирались до финального удаления extended-кода, в Wave 5–7 допускается **временное** сохранение старых членов/типов, которые ещё не используются v0.4-путём. Это **не** контракт совместимости и **не** dual-path:

- новый код не вызывает старые методы и не читает extended-поля;
- старые тесты, проверяющие удалённые механики, удаляются/переписываются в той задаче, которая меняет поведение;
- весь временный scaffolding удаляется в `T-65` до приёмки волны 8; после `T-65` в коде не остаётся extended-полей/типов, не входящих в v0.4.

## Формат и правила работы

- **Карточка:** `ID`, `Волна`, `Покрывает`, `Владеет файлами`, `Зависимости`, `Что сделать`, `Как проверить`, `Готово, если`.
- **File ownership:** исполнитель меняет только файлы из `Владеет файлами` (включая `(новый)`). Внутри волны ownership не пересекается; параллельные `implementer` — только по launch-плану. `Не трогать другие файлы` обязательно.
- **Типы:** в каждом PR проверять, что в diff нет новых объявлений `class|record|struct|interface|enum`; допускаются только изменения/удаление существующих. Добавление члена существующего типа допустимо и должно быть перечислено в задаче.
- **Волны:** следующая волна — после merge предыдущей и wave-gate. Валидация/merge — строго по одной `T-XX`; `implementer` не мержит; `verifier` мержит после `verified`; batch запрещён.
- **Gate:** у каждой задачи есть воспроизводимые `Как проверить` и `Готово, если`; у волны — сценарий проверок. При недостаточности ветка волны не создаётся.
- **Канонические команды:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; точечно — `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~<TestClass>"`.
- **Трассировка:** `Покрывает` ссылается на `docs/gameDocs/rules.md §1–§7`; трассы K/B/D — где применимо. Не придумывать несуществующие ID.

## Карта «понятие v0.4 → существующий тип»

| Понятие `rules.md` | Существующий тип/файл, который переделывается |
|---|---|
| Сторона player/enemy | `Faction` (`Domain/Unit.cs`): Blue=player, Red=enemy; `Neutral` не используется v0.4 |
| Поза march/attack/defense | `Posture` (`Domain/Unit.cs`): `March`, `Attack`, `Defend` (defense) |
| Локация node/edge | поля `Unit` (`IsOnEdge`, `EdgeFromZoneId`, `EdgeToZoneId`, `RemainingMinutes`, `EntryEdgeType`) |
| Приказ/эффективный приказ/статус | `Order`, `EffectiveOrder` (`Domain/Order.cs`, `Domain/EffectiveOrder.cs`); статус — строковый `Outcome` |
| Донесение, confidence, лог | `Report`, `GameEvent`, `GameState.DeliveredReports/InTransitReports`, `PlayerKnowledge` |
| Курьеры/ETA | `Courier`, `CourierPool`, `TransmissionResolver` |
| Граф/путь/плечо курьера | `Zone` (`TravelMinutes`, `EdgeTypes`, `Observability`, `DefenseBonus`), `PathPlanner` |
| Часы/фазы | `Clock`, `EventPhase` |
| PRNG | `IRandomSource`, `DeterministicRandomSource` |
| Контракты подсистем | существующие `ITransmissionResolver`, `IUnderstandingResolver`, `IExecutionResolver`, `ICombatResolver`, `IReportFactory`, `IGameRules`, `IKnowledgeUpdater` |
| Фабрика/доставка донесений | `ReportFactory`, `ReportDeliveryResolver` (`Reporting/IReportNarrator.cs`) |
| Интерпретация A/P | `AmbiguityScorer`, `ConflictResolver`, `UnderstandingResolver` |
| Движение/исполнение | `ExecutionResolver` |
| Бой | `CombatResolver` |
| Разведка/контакт | `ReconResolver` |
| Победа/AAR/метрики | `ObjectiveEvaluator`, `AfterActionReport`, `MetricCalculator` |
| Контент/сценарий | `ContentLoader`, `Scenario`, `ScenarioBundle`, `Objective` |
| Арбитр/тик | `Arbiter`, `TurnResult` |
| Противник | `EnemyDoctrine` (детерминированный скриптовый контроллер) |
| Консоль | существующие `OrderBuilder`, `OrderCommand`, `QueryCommand`, `MapCommand`, `MapRenderer`, `JournalCommand`, `JournalRenderer`, `ReportRenderer`, `SituationCommand`, `SaveCommand`, `LoadCommand`, `GameLoop`, `ServiceRegistration`, `Program` |
| Persistence | существующие `Persistence/*` DTO/mapper и `Console/Persistence/JsonSaveStore` |
| Тесты | существующие тестовые классы `Kriegsspiel.Core.Tests/*` |

## Карта волн

| Волна | Задачи | Зависимости | Launch-план (группы) | Сценарий проверок волны |
|---|---|---|---|---|
| Wave 5 — фундамент v0.4 | `T-50`–`T-54` | нет; `T-51`–`T-53` после `T-50`; `T-54` после `T-53` | G1=`T-50`; G2=`T-51`,`T-52`,`T-53`; G3=`T-54` | См. «Wave 5 — сценарий». Build + полный test + фильтры домена/часов/PRNG/контента/движения/донесений; нет новых проектов/типов. |
| Wave 6 — механики ядра | `T-55`–`T-60` | Wave 5; `T-60` после `T-56`/`T-59` | G1=`T-55`,`T-56`,`T-57`,`T-58`,`T-59`; G2=`T-60` | См. «Wave 6 — сценарий». Build + полный test + фильтры transmission/understanding/combat/recon/objective/doctrine; проверки формул §4–§7. |
| Wave 7 — интеграция, консоль, persistence, удаление LLM | `T-61`–`T-64` | Wave 6; `T-63` после `T-61`; `T-62` после `T-61`/`T-63`; `T-64` после `T-62` | G1=`T-61`; G2=`T-63`; G3=`T-62`; G4=`T-64` | См. «Wave 7 — сценарий». Build + полный test + TTY `test_01`/`crossing_01` + save/load + отсутствие LLM/новых типов. |
| Wave 8 — удаление scaffolding и документация | `T-65`, `T-66` | Wave 7; `T-66` после `T-65` | G1=`T-65`; G2=`T-66` (роль `docs-maintainer`) | См. «Wave 8 — сценарий». Build/test зелёные; extended-члены/типы удалены; docs соответствуют v0.4; `AGENTS.md`/`rules.md` не изменены. |

## Сценарии проверок волн (для verifier)

Общие правила:
- Выполнять **на ветке волны** после merge всех задач волны, до merge в `main`.
- `dotnet build Kriegsspiel.slnx` — успех, 0 ошибок; `dotnet test Kriegsspiel.slnx` — все тесты зелёные. В окружении без `dotnet` — ограничение с доказательствами, не успех.
- «Дублировать `Как проверить` `T-XX`» = выполнить команды/шаги задачи и подтвердить её утверждения на состоянии ветки волны.
- **Проверка ограничений:** `git diff --name-only <base>..<wave-branch>` не содержит новых `*.csproj`/проектов в `Kriegsspiel.slnx`; в diff нет новых объявлений `class|record|struct|interface|enum` (допустимы только изменения/удаление существующих); нет путей `*/Mvp/*`.
- Старые тесты не сохраняются «пакетом»: каждый затронутый тестовый класс переписан на v0.4-инвариант или удалён вместе с проверяемой extended-механикой.

### Wave 5 — сценарий проверок волны

- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~DomainInvariantsTests|FullyQualifiedName~FogOfWarTests|FullyQualifiedName~ClockTests|FullyQualifiedName~ContentLoaderTests|FullyQualifiedName~ExecutionResolverTests|FullyQualifiedName~ReportTests"`.
- **Проверки переделанных механик (дублировать из задач):**
  - `T-50` (домен/состояние/донесения): `DomainInvariantsTests`, knowledge-часть `FogOfWarTests`. Утверждения: `IsCombatEffective` = только `Strength >= 0.30 * InitialStrengthSnapshot`; edge-локация и один `PendingOrder`; `Order`/`EffectiveOrder` поддерживают move/hold/recon, target, deadline, forbidden, `Outcome`; `Report` — обязательные поля и confidence 1..3; knowledge append-only без слияния; `GameState` — append-only лог; новых типов событий/классов нет.
  - `T-51` (часы/PRNG): `ClockTests`. Утверждения: порядок `(minute, phase, sequence)`; фазы §2; запрет планирования в прошлое; `PendingCount`; одинаковый seed/state → одинаковая последовательность; `Restore`/`Reset` восстанавливают состояние.
  - `T-52` (контент): `ContentLoaderTests`. Утверждения: `test_01`/`crossing_01` загружаются в v0.4-схеме; негативные кейсы (defenseBonus вне 0..2, observability вне 1..3, travelTime/courierTime <= 0, K<1, вероятности вне [0,1], дубли id, несимметричное ребро, endMinute не кратен 30, seed отсутствует, initialStrength<=0, readiness вне 0..100, ammo<0, traits вне 0..3) дают контролируемую ошибку; legacy-fallback отсутствует.
  - `T-53` (движение): `ExecutionResolverTests`. Утверждения: вес ребра и фактическое время по §5; forbidden start запрещает возврат; `T_leg`/`T_leg_back` min 5; `RemainingMinutes` уменьшается по 1/мин; немедленный переход на следующее ребро без уменьшения нового в этом тике; offroad −5 readiness; posture; move→hold; pendingOrder; нет телепорта; hold/recon не двигаются.
  - `T-54` (донесения): `ReportTests`. Утверждения: confidence по §6; `ReportedOwnStrength = null` для ReconResult; honesty 0.12; потеря `rand() < p_report_loss`; `deliveryMinute = eventMinute + T_leg_back` (min 5); версии не сливаются; T_leg_back фиксируется на событие.
- **Критерий:** общие команды успешны, проверки `T-50`–`T-54` подтверждены, новых проектов/типов нет.

### Wave 6 — сценарий проверок волны

- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~TransmissionResolverTests|FullyQualifiedName~AmbiguityScorerTests|FullyQualifiedName~UnderstandingResolverTests|FullyQualifiedName~CombatResolverTests|FullyQualifiedName~FogOfWarTests|FullyQualifiedName~ObjectiveEvaluatorTests|FullyQualifiedName~EnemyDoctrineTests"`.
- **Проверки переделанных механик (дублировать из задач):**
  - `T-55` (курьеры/запросы/тишина): `TransmissionResolverTests`. Утверждения: приоритет Query>Order, далее FIFO; ETA от фактической отправки; `T_leg` min 5, `deliverAt=t+T_leg`, `returnAt=t+2*T_leg`; потеря на `sentMinute`; возврат `sentMinute+2*T_leg+60`; `QueryReply` в `deliverAt`; `deliveryMinute=deliverAt+T_leg_back`, `expectedReplyAt=deliverAt+T_leg_back+15`; `SilenceDetected`; потерянный курьер — `sentMinute+2*T_leg+30`.
  - `T-56` (понимание приказа): `AmbiguityScorerTests`, `UnderstandingResolverTests`. Утверждения: A=0; +0.70 при target==null и intent!=hold; +0.15 при deadline==null и intent==move; P по §5; forbidden-конфликт по pedantry/init; искажение при A>0 и pedantry<2 (target null → случайный смежный; recon — узел через ребро от смежного; target != null → swap move/hold); pedantry>=2 → ClarificationRequest; deadline expiry; pendingOrder перезаписывается и применяется на завершении ребра.
  - `T-57` (бой/отход): `CombatResolverTests`. Утверждения: бой на `t%30==0`; Power по §7 (posture march/attack/defense, terrain только defense, ammo 0→0.5); одновременные потери `0.25*Power_enemy/(sum)*strength`; ammo−1, readiness−10; strength 0 → удаление; отход при strength<0.30*initial в смежный узел без противника с min travelTime; иначе уничтожение; contact geometry/morale/capture не участвуют.
  - `T-58` (разведка/контакт): recon-часть `FogOfWarTests`. Утверждения: `p_detect=clamp(0.25+0.15*observability-edgePenalty,0.10,0.95)`; edgePenalty 0.15 при входе/offroad recon, 0 при периодической проверке в узле; каденция каждый 1-мин тик с deliverAt; успех → один ReconResult confidence 3 и остановка; 30-мин окно → один ReconResult confidence 1; смена приказа сбрасывает окно; Contact confidence 2; recon из соседнего узла не участвует в бою.
  - `T-59` (победа/разбор): `ObjectiveEvaluatorTests`. Утверждения: Score=Σ W_v*StateScore; Achieved/Contested/Failed по §7; победа при Score>=WinThreshold; для приказа фиксируются createdMinute/sentMinute/deliverAt/original/effective/status; ровно 3 метрики §7; расширенных метрик нет.
  - `T-60` (противник): `EnemyDoctrineTests`. Утверждения: скриптовый контроллер подаёт enemy-приказы в том же `Order`-формате на CommandPhase; полный прогон до endMinute детерминирован; победа/поражение считается; нет скрытой истины в MVP-событиях.
- **Критерий:** общие команды успешны, проверки `T-55`–`T-60` подтверждены, новых проектов/типов нет.

### Wave 7 — сценарий проверок волны

- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`; `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ArbiterIntegrationTests|FullyQualifiedName~PersistenceTests"`.
- **Проверки переделанных/интегрированных механик (дублировать из задач):**
  - `T-61` (арбитр/тик): `ArbiterIntegrationTests`. Утверждения: порядок шагов §2 (отправка → доставка/интерпретация → движение/обнаружение/deadline → бой+отдых каждые 30 → доставка донесений/возврат курьеров/SilenceDetected); лексикографический порядок id; CommandPhase каждые 30; in-transit донесение не теряется на границе; `TurnResult` содержит только доставленное к target; одинаковый seed → одинаковый EventLog; отдых +5 readiness тем, кто не двигался и не воевал 30 минут.
  - `T-62` (UI/DI): build + TTY-сценарий. Утверждения: меню ОБСТАНОВКА/КАРТА/ПРИКАЗ/ЗАПРОС/ЖУРНАЛ/ВРЕМЯ/СОХРАНИТЬ/ЗАГРУЗИТЬ/ВЫЙТИ; доступны только move/hold/recon, targetZone, deadline, forbiddenZone; предпросмотр A/P; карта/журнал/обстановка используют только доставленные донесения/лог и топологию; различают `QuerySilence`/`ReportLost`; LLM не вызывается.
  - `T-63` (persistence): `PersistenceTests`. Утверждения: round-trip сохраняет минуту/очередь часов, RNG state/seed, зоны/рёбра/edge-локации, приказы/effective/pending/outcome, курьеров/ETA, in-transit/delivered отчёты, knowledge/report log, EventLog; после load продолжение детерминировано.
  - `T-64` (удаление LLM): build/test. Утверждения: `Kriegsspiel.AI` удалён из решения и ссылок Console; LLM-классы/промпты/тесты удалены; `grep -R "Kriegsspiel.AI\|UseLlm\|Llm"` не находит живых ссылок (кроме истории git); `DeterministicRandomSource` остаётся.
- **TTY/детерминизм:** два прогона `test_01` с одинаковым seed дают одинаковый набор событий/донесений (сравнить хэш/refs); вход подавать через `script`/файл, не интерактивно.
- **Smoke-команды:**
  - `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Scenario test_01" /tmp/opencode/mvp-smoke-test_01.log`
  - `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Scenario crossing_01" /tmp/opencode/mvp-smoke-crossing_01.log`
- **Критерий:** общие команды, `T-61`–`T-64`, TTY-прогоны, save/load подтверждены; LLM/dual-engine отсутствуют; новых проектов/типов нет.

### Wave 8 — сценарий проверок волны (cleanup/docs)

- **Общие:** `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx`.
- **Проверки `T-65`:** в коде не осталось extended-полей/типов/enum-значений вне v0.4; удалены `Cargo`, `CivilianGroup`, `ContactGeometry`, `PendingCasualty`, `OrderBranch`, `MovementMode`, `OrderConstraint`, ветви/режимы движения/груз/граждане/отложенные потери/слияние `fact key`; старые тесты удалённых механик удалены; сборка/тесты зелёные.
- **Проверки `T-66`:** `structure.md` отражает фактические Core/Console/Test-файлы v0.4, `test_01`/`crossing_01`, отсутствие `Kriegsspiel.AI`; `readme.md` описывает запуск/проверки v0.4; `instructions.md` актуален; `git diff --name-only` по волне содержит только docs; `AGENTS.md` и `docs/gameDocs/rules.md` не изменены.
- **Критерий:** build/test зелёные, extended scaffolding удалён, docs консистентны, нет новых проектов/типов.

## Карточки задач

### T-50 — Домен v0.4: Unit/Order/Report/GameState/Knowledge
- **ID:** `T-50`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §1, §3, §4, §6, §7` (доменные поля, локация/поза, структура донесений, `isCombatEffective`, append-only лог); трассы K/B/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Domain/Unit.cs`
  - `Kriegsspiel.Core/Domain/Order.cs`
  - `Kriegsspiel.Core/Domain/EffectiveOrder.cs`
  - `Kriegsspiel.Core/Domain/Report.cs`
  - `Kriegsspiel.Core/Domain/GameEvent.cs`
  - `Kriegsspiel.Core/Domain/GameState.cs`
  - `Kriegsspiel.Core/Domain/Zone.cs`
  - `Kriegsspiel.Core/Domain/CommanderTraits.cs`
  - `Kriegsspiel.Core/Domain/Courier.cs`
  - `Kriegsspiel.Core/Domain/PlayerKnowledge.cs`
  - `Kriegsspiel.Core/Contracts/IGameRules.cs`
  - `Kriegsspiel.Core/Contracts/IKnowledgeUpdater.cs`
  - `Kriegsspiel.Core/Arbitration/KnowledgeUpdater.cs`
  - `Kriegsspiel.Core.Tests/DomainInvariantsTests.cs`
  - `Kriegsspiel.Core.Tests/FogOfWarTests.cs` (только knowledge-часть; recon-часть — `T-58`)
  - `Kriegsspiel.Core.Tests/Fixtures/TestContent.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** нет.
- **Что сделать:**
  1. Переделать `Unit`: edge-локация (`IsOnEdge`, `EdgeFromZoneId`, `EdgeToZoneId`, `RemainingMinutes`, `EntryEdgeType`), единичный `PendingOrder`, `LastMovedMinute`, `LastFoughtMinute`; `IsCombatEffective` = только `Strength >= 0.30 * (InitialStrengthSnapshot > 0 ? InitialStrengthSnapshot : Strength)`. `Posture.Defend` используется как defense. Extended-члены пока оставить только для промежуточной сборки, v0.4-путём не читать.
  2. Переделать `Order`/`EffectiveOrder`: move/hold/recon, targetZone, deadline, forbiddenZone, `Outcome` (Executed/Distorted/FailedRejected), CreatedMinute/SentMinute/DeliverAt, связь с `EffectiveOrder`; `PendingOrder` перезаписывается. Extended-поля (ветви/ограничения/режимы) не использовать.
  3. Переделать `Report`: `ObservedZoneId`, `ReportedOwnStrength` (`int?`), `ReportedEnemyStrength`, confidence 1..3; MVP-лог не сливает версии и не приоритизирует.
  4. Переделать `PlayerKnowledge`/`KnowledgeUpdater`: append-only все доставленные версии; не повышать/не понижать confidence, не сливать `fact key`; no-data/empty/occupied по факту донесения; скрытую истину не хранить.
  5. Переделать `GameState`: использовать существующие списки как append-only лог; добавить `HqZoneId`, `EndMinute`, `Seed`, `RngState`; новые типы/коллекции-классы не вводить.
  6. Переделать `GameEvent` без новых record-типов: зафиксировать MVP-семантику существующих событий; события отдыха/победы выражать существующими событиями/`TurnResult`/AAR, не добавляя типы.
  7. Переделать `Zone`/`CommanderTraits`/`IGameRules`: observability 1..3, defenseBonus 0..2, travel/courier times, edge type; traits Init/Pedantry/Honesty через существующие `Initiative`/`Literalism`/`ReportHonesty`; порог 0.30 и MVP-константы в `IGameRules`.
  8. Переписать `DomainInvariantsTests`, knowledge-часть `FogOfWarTests` и `TestContent` под v0.4; удалить проверки extended-инвариантов.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~DomainInvariantsTests|FullyQualifiedName~FogOfWarTests"`
  - Утверждения: `IsCombatEffective` false при `strength < 0.30*initial`, true при равенстве; edge-локация хранит `RemainingMinutes`; один `PendingOrder`; `Order`/`EffectiveOrder` поддерживают move/hold/recon + target/deadline/forbidden/outcome; `Report` confidence 1..3, own strength null для recon; knowledge хранит все версии без слияния; новых типов в diff нет.
- **Готово, если:**
  - [ ] Все перечисленные существующие типы переделаны; новых типов/проектов нет.
  - [ ] Домен содержит v0.4-поля/состояния; extended-поля не читаются MVP-путём.
  - [ ] `PlayerKnowledge`/`GameState` реализуют append-only лог без слияния версий.
  - [ ] `GameEvent` не расширен новыми record-типами.
  - [ ] `DomainInvariantsTests`/knowledge-часть `FogOfWarTests` зелёные; сборка зелёная.

### T-51 — Часы, фазы и детерминированный PRNG
- **ID:** `T-51`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §1 п.6, §2` (детерминизм, 1-минутный тик, порядок событий); трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Domain/Clock.cs`
  - `Kriegsspiel.Core/Contracts/IRandomSource.cs`
  - `Kriegsspiel.Core/Ai/DeterministicRandomSource.cs`
  - `Kriegsspiel.Core.Tests/ClockTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`.
- **Что сделать:**
  1. Переделать `Clock`/`EventPhase`: ключ `(minute, phase, sequence)`; фазы §2 (Dispatch → Delivery → Understanding → Movement → Combat → ReportCreate → ReportDeliver); `PhaseFor`/`PhaseForName`; запрет планирования в прошлое; `AdvanceTo` не теряет события; `PendingCount`/`PendingKeys`.
  2. Переделать `DeterministicRandomSource` в единый PRNG: `Seed`, `State`, `NextDouble`, `NextInt`, `Chance`, `Restore`, `Reset`; детерминизм при одинаковом seed/state. Если для компиляции нужен `Random`-совместимый вид — переделать существующий класс/адаптер, не создавая новый тип.
  3. Переписать `ClockTests` под v0.4-порядок, запрет прошлого, restore/reset и повторяемость seed; удалить проверки extended-порядка.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ClockTests"`
  - Утверждения: события с одинаковой минутой идут по фазам §2; sequence сохраняет порядок; `Schedule` в прошлое бросает исключение; `AdvanceTo` не теряет события; одинаковый seed/state → одинаковая последовательность; `Restore` продолжает ту же последовательность.
- **Готово, если:**
  - [ ] `Clock`/`EventPhase` переделаны без нового enum-типа; порядок фаз соответствует §2.
  - [ ] `DeterministicRandomSource` — единый воспроизводимый PRNG.
  - [ ] `ClockTests` покрывают порядок/прошлое/restore/seed.
  - [ ] Новых типов/проектов нет; сборка зелёная.

### T-52 — Контент и сценарии v0.4
- **ID:** `T-52`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §1, §2` (ScenarioConfig, граф, курьеры, юниты, цели, seed); трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Content/ContentLoader.cs`
  - `Kriegsspiel.Core/Content/Scenario.cs`
  - `Kriegsspiel.Core/Content/ScenarioBundle.cs`
  - `Kriegsspiel.Core/Content/Objective.cs`
  - `Content/scenarios/test_01.json`
  - `Content/scenarios/crossing_01.json`
  - `Content/scenarios/fs005_scale_demo.json` (удалить)
  - `Content/scenarios/river_line_10x10.json` (удалить)
  - `Kriegsspiel.Core.Tests/ContentLoaderTests.cs`
  - `Kriegsspiel.Core.Tests/Fs005ScaleContentTests.cs` (удалить)
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`.
- **Что сделать:**
  1. Переделать `Scenario`: `HqZoneId`, `CourierCount`, `CourierLossProbability`, `ReportLossProbability`, `WinThreshold`, `Seed`, `StartMinute`, `EndMinute` (кратен 30), цели с весами через существующий `Objective`; стартовая posture defense; `initialStrength`; новых типов-контейнеров нет.
  2. Переделать `ContentLoader`: v0.4-поля `hqZone`, `courierPool`, `targetZones`/objectives, `courierTimes`; маппинг `faction` Blue→player, Red→enemy; `type=HQ` задаёт `HqZoneId`, не создаёт боевой unit; `posture` → defense/march; traits Init/Pedantry/Honesty.
  3. Валидация §1–§2: связный граф, hq/targets существуют, endMinute кратен 30, K>=1, вероятности [0,1], defenseBonus 0..2, observability 1..3, travelTime/courierTime>0, симметричные рёбра, уникальные id, seed обязателен, readiness 0..100, ammo>=0, traits 0..3, initialStrength>0. Legacy-fallback отсутствует.
  4. Переделать `GameStateFactory` (существующий статический класс в `ScenarioBundle.cs`): строить `GameState` с node-локациями, defense, зонами/рёбрами/юнитами/целями/`EndMinute`/`HqZoneId`/`Seed`; новых типов нет.
  5. Мигрировать `test_01.json`/`crossing_01.json` на v0.4; удалить `fs005_scale_demo.json`, `river_line_10x10.json` и `Fs005ScaleContentTests` как устаревшие.
  6. Переписать `ContentLoaderTests` под v0.4-схему и негативные кейсы. Если для промежуточной сборки нужна перегрузка загрузчика — удалить её в `T-65`.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ContentLoaderTests"`
  - Утверждения: `test_01`/`crossing_01` загружаются v0.4-загрузчиком; hq/targets/курьеры/рёбра корректны; негативные кейсы бросают `ContentValidationException`; legacy-сценариев и fallback нет.
- **Готово, если:**
  - [ ] v0.4-схема §1–§2 покрыта существующими типами и валидируется.
  - [ ] `test_01`/`crossing_01` мигрированы; legacy-сценарии удалены.
  - [ ] `GameStateFactory` создаёт валидное начальное состояние без новых типов.
  - [ ] Позитивные/негативные тесты зелёные.
  - [ ] Новых типов/проектов нет.

### T-53 — Путь и движение: рефакторинг `PathPlanner`/`ExecutionResolver`
- **ID:** `T-53`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §2 шаг 3, §3, §4 (T_leg/T_leg_back), §5` (Дейкстра, forbidden, вес ребра, фактическое время, pendingOrder); трассы D/B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/PathPlanner.cs`
  - `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
  - `Kriegsspiel.Core/Arbitration/IExecutionResolver.cs`
  - `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`.
- **Что сделать:**
  1. Переделать `PathPlanner`: вес `travelTime` при `Deadline != null`, `travelTime*(EdgeType==Offroad?0.8:1.0)` при `Deadline == null`; фактическое время всегда `travelTime`; forbidden-правила (`forbiddenZone != start` исключается; `forbiddenZone == start` разрешает выход, запрещает возврат); `T_leg`/`T_leg_back` по `courierTime` с минимумом 5.
  2. Переделать `ExecutionResolver`: из узла в ребро (`RemainingMinutes=travelTime`), уменьшение по 1/мин; завершение ребра, offroad −5 readiness, прибытие; немедленный переход на следующее ребро в тот же тик без уменьшения нового; позы march/attack/defense; move в свободный target → defense + `EffectiveOrder.Intent=Hold`; hold/recon не двигаются; pendingOrder применяется на завершении ребра; deadline expiry; нет телепорта/fatigue/режимов движения/ветвей.
  3. Не создавать новый контракт обнаружения: после движения/прибытия вызывается существующий `ReconResolver` (переделывается в `T-58`, интегрируется в `T-61`).
  4. Переписать `ExecutionResolverTests` под v0.4: multi-hop, немедленный переход, no-decrement, offroad, posture, deadline, pendingOrder, no teleport, hold/recon stationary.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ExecutionResolverTests"`
  - Утверждения: remainingMinutes/переходы по §2; no-decrement; offroad −5; posture; move→hold; no teleport; hold/recon stationary; `T_leg`/`T_leg_back` min 5.
- **Готово, если:**
  - [ ] `PathPlanner`/`ExecutionResolver` переделаны без новых типов.
  - [ ] Движение/переходы/позы/pendingOrder соответствуют §2–§5.
  - [ ] Нет fatigue/movement modes/teleport в MVP-пути.
  - [ ] Тесты зелёные; новых типов/проектов нет.

### T-54 — Донесения: рефакторинг `ReportFactory`/`ReportDeliveryResolver`
- **ID:** `T-54`
- **Волна:** Wave 5
- **Покрывает:** `rules.md §4` (обратное плечо), `§6` (структура/confidence/потеря/доставка); трассы K/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Reporting/IReportNarrator.cs`
  - `Kriegsspiel.Core/Contracts/IReportFactory.cs`
  - `Kriegsspiel.Core.Tests/ReportTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-51`, `T-53`.
- **Что сделать:**
  1. Переделать `ReportFactory`: confidence по §6 (3 для Arrival/Impossible/Clarification; 2 для Contact/QueryReply по текущему узлу; 1 для Recon fail/QueryReply о соседнем); `ReportedOwnStrength = null` для ReconResult; для остальных — strength с honesty-искажением `1+(3-honesty)*0.12` при honesty<3; `ReportedEnemyStrength` = 0/сумма с обратным искажением.
  2. Переделать `ReportDeliveryResolver`: потеря `rand() < p_report_loss` → ReportLost; иначе `deliveryMinute = eventMinute + T_leg_back` (min 5; на ребре — от `ToZoneId`), значение фиксируется на событие; детерминированный PRNG.
  3. Не использовать пул курьеров штаба для Report; не сливать версии/fact key; шаблонный текст может остаться, но не влияет на семантику.
  4. Переписать `ReportTests` под §6: confidence/поля, honesty, recon own strength null, отсутствие слияния версий, потеря/доставка, T_leg_back min 5.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ReportTests"`
  - Утверждения: обязательные поля/границы confidence; `ReconResult.ReportedOwnStrength == null`; honesty<3 искажает strength; лог не сливает версии; ReportLost при срабатывании; deliveryMinute = eventMinute + T_leg_back (min 5).
- **Готово, если:**
  - [ ] `ReportFactory`/`ReportDeliveryResolver` переделаны под §6 без новых типов.
  - [ ] Пул курьеров не используется для Report.
  - [ ] `ReportTests` зелёные.
  - [ ] Новых типов/проектов нет.

### T-55 — Курьеры, отправка, запросы и тишина
- **ID:** `T-55`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 1, §4`; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/CourierPool.cs`
  - `Kriegsspiel.Core/Arbitration/TransmissionResolver.cs`
  - `Kriegsspiel.Core/Arbitration/ITransmissionResolver.cs`
  - `Kriegsspiel.Core.Tests/TransmissionResolverTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-51`, `T-53`, `T-54`.
- **Что сделать:**
  1. Переделать пул штаба: K курьеров; состояния free/outbound/return/lost через существующий `CourierState`; очередь Order/Query; приоритет Query выше Order, далее FIFO по `createdMinute`; отправка при свободном курьере в тик `t`, `sentMinute=t`.
  2. `T_leg`/`T_leg_back` — существующий `PathPlanner` по `courierTime` от hqZone до узла юнита (на ребре — до `ToZoneId`), минимум 5; `deliverAt=t+T_leg`, `returnAt=t+2*T_leg`; ETA от фактической отправки.
  3. Потеря курьера: в `sentMinute` `rand()<p_courier_loss` → `CourierLost`, `deliverAt=null`, возврат `sentMinute+2*T_leg+60`; приказ/запрос не доставляется; AAR: `SentMinute=t`, `DeliverAt=null`, `Outcome=FailedRejected`; неотправленный до `EndMinute` — `SentMinute=null`, `DeliverAt=null`.
  4. Query: в `deliverAt` живой юнит формирует `QueryReply`; `deliveryMinute=deliverAt+T_leg_back` (если не потеряно); `expectedReplyAt=deliverAt+T_leg_back+15`; иначе `SilenceDetected` (существующий `QuerySilence`); при потере курьера — `sentMinute+2*T_leg+30`.
  5. Переписать `TransmissionResolverTests` под §4: FIFO/приоритет, занятые курьеры, ETA, потеря/возврат, query reply/silence, min 5.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~TransmissionResolverTests"`
  - Утверждения: Query уходит раньше Order при равном времени; FIFO; `deliverAt`/`returnAt` по §4; потеря курьера не создаёт доставку; `SilenceDetected` в ожидаемом окне; T_leg >= 5.
- **Готово, если:**
  - [ ] Пул и резолвер переделаны под §4 без новых типов.
  - [ ] Query/Reply/Silence и потеря курьера покрыты тестами.
  - [ ] ETA от фактической отправки; Report-доставка не занимает пул штаба.
  - [ ] Тесты зелёные; новых типов/проектов нет.

### T-56 — Интерпретация приказа: `AmbiguityScorer`/`ConflictResolver`/`UnderstandingResolver`
- **ID:** `T-56`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §3` (pendingOrder), `§5`; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Orders/AmbiguityScorer.cs`
  - `Kriegsspiel.Core/Orders/ConflictResolver.cs`
  - `Kriegsspiel.Core/Orders/Misinterpretation.cs`
  - `Kriegsspiel.Core/Arbitration/UnderstandingResolver.cs`
  - `Kriegsspiel.Core/Arbitration/IUnderstandingResolver.cs`
  - `Kriegsspiel.Core.Tests/AmbiguityScorerTests.cs`
  - `Kriegsspiel.Core.Tests/UnderstandingResolverTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-51`, `T-54`.
- **Что сделать:**
  1. Переделать `AmbiguityScorer`: A=0; `+0.70` при target==null и intent!=hold; `+0.15` при deadline==null и intent==move; `P=clamp(0.03+A*(1+0.15*pedantry-0.10*init),0.03,0.90)`.
  2. Переделать `ConflictResolver`/`UnderstandingResolver`: Шаг 1 forbidden (target==forbidden или путь с фильтрацией не существует): `Pedantry>=Init` → `EffectiveOrder=null`, defense, ImpossibleOrder; иначе forbidden=null и Шаг 2. Шаг 2: один бросок `rand()<P`; `Pedantry>=2` → `EffectiveOrder=null`, defense, ClarificationRequest; иначе target==null → случайный смежный (recon — узел через ребро от смежного), target!=null → swap move/hold.
  3. Просрочка deadline: `t > Deadline` и цель не достигнута; `Pedantry>=2 && Init<=1` → `EffectiveOrder=null`, defense, ImpossibleOrder; иначе продолжать.
  4. `PendingOrder`: приказ на ребре не исполняется сразу, новый перезаписывает; применяется по завершении ребра; смена приказа сбрасывает recon-окно.
  5. Обновлять `Order.Outcome`: Distorted при искажении, FailedRejected при Impossible/Clarification/deadline; A сохранять в Order; использовать существующие `Misinterpretation`/`OrderMisinterpreted`, не добавлять новые event/record-типы; ветви/ограничения/режимы не использовать.
  6. Переписать `AmbiguityScorerTests`/`UnderstandingResolverTests` под v0.4; удалить проверки extended-ветвей/ограничений/режимов.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~AmbiguityScorerTests|FullyQualifiedName~UnderstandingResolverTests"`
  - Утверждения: A/P совпадают с §5; forbidden start; pedantry>=init; pedantry>=2 → Clarification; target null → смежный; target != null → swap; deadline-исход; pendingOrder применяется на ребре; ровно один roll.
- **Готово, если:**
  - [ ] A/P и Шаги 1–2 соответствуют §5; ровно один roll.
  - [ ] `EffectiveOrder` формируется/искажается отдельно от исходного `Order`; `Outcome` обновляется.
  - [ ] pendingOrder и deadline обработаны; recon-окно сбрасывается.
  - [ ] Нет новых типов событий/приказов; extended-поля не используются MVP-путём.
  - [ ] Тесты зелёные.

### T-57 — Бой и отступление
- **ID:** `T-57`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 4, §7` (бой/потери/отход); трасса B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/CombatResolver.cs`
  - `Kriegsspiel.Core/Arbitration/ICombatResolver.cs`
  - `Kriegsspiel.Core.Tests/CombatResolverTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-51`, `T-54`.
- **Что сделать:**
  1. Бой только на `t%30==0`, в каждом узле с юнитами обеих сторон `Strength>0`; recon из соседнего узла не участвует; юниты на рёбрах не участвуют.
  2. `Power = Strength * Readiness/100 * PostureMult * TerrainMult * AmmoMult`; Posture March 0.5/Attack 1.0/Defense 1.3; Terrain `1+0.1*DefenseBonus` только при Defense; Ammo 1.0 при >0, 0.5 при 0.
  3. Если `PowerPlayer+PowerEnemy>0`: одновременные потери для каждого юнита `0.25 * PowerEnemy/(sum) * Strength`; `Ammo=max(0,Ammo-1)`, `Readiness=max(0,Readiness-10)`; `Strength==0` → удалить; обновлять `LastFoughtMinute`.
  4. Отступление: выживший с `Strength < 0.30*InitialStrength` → смежный узел без противника с минимальным `travelTime`, `EffectiveOrder=null`; если свободных нет — уничтожить; удалить из `Zone.PresentUnitIds`/`GameState.Units`.
  5. Contact при начале боя (confidence 2) — через существующий `IReportFactory`/`Report`; не использовать contact geometry/morale/pending casualties/capture.
  6. Переписать `CombatResolverTests` под v0.4; удалить проверки extended-механик.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~CombatResolverTests"`
  - Утверждения: Power/потери по §7; terrain только defense; ammo 0→0.5; strength 0 удалён; отход/уничтожение; recon вне боя.
- **Готово, если:**
  - [ ] Бой реализован без morale/contact geometry/pending casualties/capture.
  - [ ] Потери/ammo/readiness/удаление по §7.
  - [ ] Отступление/уничтожение по §7.
  - [ ] Contact идёт через существующий контракт; новых типов нет.
  - [ ] Тесты зелёные.

### T-58 — Разведка и контакт
- **ID:** `T-58`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §2 шаг 3, §6` (p_detect, каденция, confidence, Contact); трасса K.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/ReconResolver.cs`
  - `Kriegsspiel.Core.Tests/FogOfWarTests.cs` (recon-часть; knowledge-часть уже переделана в `T-50`)
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-54`.
- **Что сделать:**
  1. `p_detect = clamp(0.25 + 0.15*Observability - edgePenalty, 0.10, 0.95)`; edgePenalty 0.15 при входе наблюдателя в v или recon через offroad; 0 при периодической проверке в узле.
  2. Recon-каденция: если `EffectiveOrder.Intent==Recon`, проверять каждый 1-мин тик с `DeliverAt`, пока юнит стоит в узле, из которого `TargetZone` достижим по одному ребру; в наблюдаемый узел не входит; `TargetZone==null` → проверок нет до смены приказа.
  3. Первый успех → один `ReconResult` confidence 3 и остановка; иначе по истечении 30-мин окна от `DeliverAt` → один `ReconResult` confidence 1; смена приказа/pendingOrder сбрасывает окно.
  4. Contact: при входе/прибытии в узел с enemy и при начале боя — Contact confidence 2; recon из соседнего узла в бою не участвует.
  5. Использовать существующий `ReportFactory`/`ReportDeliveryResolver`; не сливать донесения; новых типов нет.
  6. Переписать recon-часть `FogOfWarTests` под §6; knowledge-часть не откатывать.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~FogOfWarTests"`
  - Утверждения: p_detect по формуле; edgePenalty; успех→conf 3/стоп; провал→conf 1 через 30 мин; смена приказа сбрасывает; Contact conf 2; recon не в бою.
- **Готово, если:**
  - [ ] p_detect/каденция/окно соответствуют §6.
  - [ ] Confidence и Contact корректны.
  - [ ] Смена приказа сбрасывает наблюдение.
  - [ ] Нет fact-key слияния/скрытой истины; новых типов нет.
  - [ ] Тесты зелёные.

### T-59 — Победа, AAR и метрики
- **ID:** `T-59`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §7` (Score/StateScore/победа, разбор и 3 метрики); трассы B/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/ObjectiveEvaluator.cs`
  - `Kriegsspiel.Core/Assessment/AfterActionReport.cs`
  - `Kriegsspiel.Core/Assessment/MetricCalculator.cs`
  - `Kriegsspiel.Core.Tests/ObjectiveEvaluatorTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-50`, `T-51`, `T-52`.
- **Что сделать:**
  1. Переделать `ObjectiveEvaluator`: в `EndMinute` `Score=Σ W_v*StateScore(v)`; Achieved=1.0, если в v есть player-юниты с `IsCombatEffective==true` и нет enemy-боеспособных; Contested=0.5, если боеспособные есть у обеих; Failed=0.0, если player-боеспособных нет; победа при `Score>=WinThreshold`.
  2. Переделать `AfterActionReport`: для каждого приказа — CreatedMinute, SentMinute (null, если не отправлен до EndMinute), DeliverAt (null, если потерян/не отправлен), исходный Order, EffectiveOrder, Outcome (Executed/Distorted/FailedRejected).
  3. Переделать `MetricCalculator`: ровно 3 метрики §7 — avg(DeliverAt-CreatedMinute) по доставленным; доля приказов с `A>0`; доля Distorted или FailedRejected. Расширенные метрики не использовать; лишние record-типы удаляются в `T-65`.
  4. Переписать `ObjectiveEvaluatorTests` под v0.4-состояния/Score/порог/null-поля/3 метрики.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ObjectiveEvaluatorTests"`
  - Утверждения: Achieved/Contested/Failed по §7; Score/порог; null-поля для потерянных/неотправленных; ровно 3 метрики; расширенных метрик нет.
- **Готово, если:**
  - [ ] Score/StateScore/победа по §7.
  - [ ] AAR-поля приказа фиксируются.
  - [ ] Ровно 3 метрики §7 в MVP-пути.
  - [ ] Нет новых типов; extended-метрики не используются.
  - [ ] Тесты зелёные.

### T-60 — Скриптовый противник
- **ID:** `T-60`
- **Волна:** Wave 6
- **Покрывает:** `rules.md §1, §2` (внешний контроллер enemy), §7 (победа); трассы D/B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/EnemyDoctrine.cs`
  - `Kriegsspiel.Core.Tests/EnemyDoctrineTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-52`, `T-56`, `T-59`.
- **Что сделать:**
  1. Переделать `EnemyDoctrine` в детерминированный скриптовый контроллер enemy: на CommandPhase возвращает enemy-приказы в том же `Order`-формате (Move/Hold/Recon, targetZone, deadline, forbiddenZone); без LLM; детерминирован по seed/минуте; использует только знание/топологию, не скрытую истину.
  2. Удалить/упростить в существующем файле LLM-компилятор и extended-view-поля, не нужные v0.4; не создавать новые типы.
  3. Переписать `EnemyDoctrineTests` под v0.4-контроллер: валидные приказы, детерминизм, полный прогон на `test_01` воспроизводим, нет скрытой истины.
  4. Enemy подчиняется тем же резолверам; новых механик/типов нет.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~EnemyDoctrineTests"`
  - Утверждения: `test_01` загружается; enemy-приказы валидны; полный прогон детерминирован; победа/поражение считается.
- **Готово, если:**
  - [ ] `EnemyDoctrine` детерминирован и использует тот же `Order`.
  - [ ] Сквозной тест воспроизводим при одинаковом seed.
  - [ ] Новых механик/типов/проектов нет.
  - [ ] Тесты зелёные.

### T-61 — Арбитр и порядок тика
- **ID:** `T-61`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §1 п.6, §2` (игровой цикл, порядок шагов, детерминизм), интеграция §3–§7; трассы D/B/K.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/Arbiter.cs`
  - `Kriegsspiel.Core/Arbitration/TurnResult.cs`
  - `Kriegsspiel.Core.Tests/ArbiterIntegrationTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-55`–`T-60` (все механики Wave 6).
- **Что сделать:**
  1. Единая точка: принять Order/Query на CommandPhase (каждые 30 мин, `minute%30==0 && minute<EndMinute`), затем `advanceTime(30)`; внутри 1-мин тика порядок: 1) отправка депеш; 2) доставка/интерпретация; 3) движение/обнаружение/deadline; 4) при `t%30==0` бой + отдых (+5 readiness тем, кто не двигался/не воевал 30 мин); 5) доставка донесений/возврат курьеров/SilenceDetected.
  2. Лексикографический порядок `id` для юнитов и депеш; единый `DeterministicRandomSource`; одинаковый seed+вход → одинаковый EventLog/TurnResult.
  3. In-transit отчёты не теряются на границе `target`; `TurnResult` содержит только доставленное к `target`.
  4. Интегрировать существующие резолверы через контракты; не менять LLM (проект удаляется в `T-64`); не добавлять новых event-типов (использовать существующие `GameEvent`).
  5. Вызывать `ReconResolver` после движения/прибытия; бой — `CombatResolver`; победа в `EndMinute` — `ObjectiveEvaluator`.
  6. Переписать `ArbiterIntegrationTests` под v0.4-таймлайн, in-transit, Contact/CombatTick, Silence, OrderLost не как доставка, одинаковый seed → одинаковый лог.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~ArbiterIntegrationTests"`
  - Утверждения: порядок шагов §2; CommandPhase; in-transit; TurnResult; OrderLost; детерминизм; отдых +5.
- **Готово, если:**
  - [ ] Арбитр реализует цикл §2 и порядок шагов без новых типов.
  - [ ] Лексикографический порядок и seed-детерминизм подтверждены.
  - [ ] In-transit/граница target корректны.
  - [ ] OrderLost/Silence семантически корректны.
  - [ ] Тесты зелёные.

### T-62 — Консоль, DI и игровой цикл
- **ID:** `T-62`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §2` (цикл/CommandPhase), §4 (Query), §5 (move/hold/recon), §6 (карта/журнал/обстановка); трассы D/K.
- **Владеет файлами:**
  - `Kriegsspiel.Console/Program.cs`
  - `Kriegsspiel.Console/appsettings.json`
  - `Kriegsspiel.Console/Startup/ServiceRegistration.cs`
  - `Kriegsspiel.Console/Startup/GameLoop.cs`
  - `Kriegsspiel.Console/Commands/ICommand.cs`
  - `Kriegsspiel.Console/Commands/OrderBuilder.cs`
  - `Kriegsspiel.Console/Commands/OrderCommand.cs`
  - `Kriegsspiel.Console/Commands/QueryCommand.cs`
  - `Kriegsspiel.Console/Commands/MapCommand.cs`
  - `Kriegsspiel.Console/Commands/MapRenderer.cs`
  - `Kriegsspiel.Console/Commands/JournalCommand.cs`
  - `Kriegsspiel.Console/Commands/JournalRenderer.cs`
  - `Kriegsspiel.Console/Commands/ReportRenderer.cs`
  - `Kriegsspiel.Console/Commands/SituationCommand.cs`
  - `Kriegsspiel.Console/Commands/SaveCommand.cs`
  - `Kriegsspiel.Console/Commands/LoadCommand.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-61`, `T-63` (save/load-сценарий).
- **Что сделать:**
  1. Переделать `ServiceRegistration`: DI только на существующие v0.4-типы (`ContentLoader`, `ScenarioBundle`, `Arbiter`, `DeterministicRandomSource` как `IRandomSource`, `TransmissionResolver`/`CourierPool`, `UnderstandingResolver`, `ExecutionResolver`, `CombatResolver`, `ReconResolver`, `ReportFactory`, `KnowledgeUpdater`, `ObjectiveEvaluator`, `EnemyDoctrine`, команды/renderers/save-load). Не регистрировать LLM/AI-пайплайны; удалить `using Kriegsspiel.AI` и LLM-ключи из `appsettings.json`.
  2. Переделать `GameLoop`: меню ОБСТАНОВКА/КАРТА/ПРИКАЗ/ЗАПРОС/ЖУРНАЛ/ВРЕМЯ/СОХРАНИТЬ/ЗАГРУЗИТЬ/ВЫЙТИ; CommandPhase каждые 30; `ВРЕМЯ` вызывает `AdvanceTo(now+30)`; вывод доставленных донесений; enemy-приказы через `EnemyDoctrine`; никакого dual-engine/`Engine`-флага.
  3. Переделать `OrderBuilder`/`OrderCommand`: только Move/Hold/Recon, targetZone, deadline, forbiddenZone; предпросмотр A/P; без ветвей/ограничений/режимов движения/7 слотов; не раскрывать скрытую истину.
  4. Переделать `QueryCommand`: запросы §4, приоритет/ETA/тишина через `T-55`.
  5. Переделать renderers/commands: карта/журнал/обстановка/донесения только по доставленным донесениям/логу и топологии; различать `QuerySilence`/`ReportLost`/`ReportDelivered`; все версии без слияния; confidence/времена; не читать скрытую истину enemy.
  6. `Program.cs` — только сборка DI и запуск существующего `GameLoop`; новых классов нет.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx`
  - `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Scenario test_01" /tmp/opencode/mvp-smoke-test_01.log`
  - `script -qec "dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj -- --Scenario crossing_01" /tmp/opencode/mvp-smoke-crossing_01.log`
  - Утверждения: меню 9 действий; оба сценария запускаются; приказ/время/карта/журнал/обстановка работают; LLM/dual-engine отсутствуют.
- **Готово, если:**
  - [ ] DI поднимает v0.4-стек на существующих типах без LLM.
  - [ ] `GameLoop` реализует меню/CommandPhase/время/enemy-приказы.
  - [ ] UI показывает только v0.4-поля и доставленные донесения.
  - [ ] TTY-прогоны `test_01`/`crossing_01` успешны.
  - [ ] Новых типов/проектов/`Engine`-ветки нет.

### T-63 — Persistence v0.4
- **ID:** `T-63`
- **Волна:** Wave 7
- **Покрывает:** `rules.md §1 п.6, §2` (детерминизм/воспроизводимость), сохранение состояния v0.4; трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Persistence/ClockDto.cs`
  - `Kriegsspiel.Core/Persistence/CourierDtos.cs`
  - `Kriegsspiel.Core/Persistence/GameEventDto.cs`
  - `Kriegsspiel.Core/Persistence/GameStateDto.cs`
  - `Kriegsspiel.Core/Persistence/GameStateMapper.cs`
  - `Kriegsspiel.Core/Persistence/GameStatePersistence.cs`
  - `Kriegsspiel.Core/Persistence/KnowledgeDtos.cs`
  - `Kriegsspiel.Core/Persistence/OrderDtos.cs`
  - `Kriegsspiel.Core/Persistence/PersistenceJson.cs`
  - `Kriegsspiel.Core/Persistence/PersistenceSchema.cs`
  - `Kriegsspiel.Core/Persistence/ReportDto.cs`
  - `Kriegsspiel.Core/Persistence/UnitDtos.cs`
  - `Kriegsspiel.Core/Persistence/ZoneDtos.cs`
  - `Kriegsspiel.Console/Persistence/JsonSaveStore.cs`
  - `Kriegsspiel.Core.Tests/PersistenceTests.cs`
  - Не трогать другие файлы; не создавать новые типы/проекты.
- **Зависимости:** `T-61`, `T-50`, `T-55`.
- **Что сделать:**
  1. Переделать существующие DTO: Now/очередь `Clock`, seed/RNG state, zones/edges/courierTime, edge-локации юнитов (`IsOnEdge`, from/to/remaining/entry type), orders/effective/pending/outcome, couriers/ETA, in-transit/delivered reports, knowledge/report log, EventLog, `EndMinute`/`HqZoneId`.
  2. Переделать `GameStateMapper`/`GameStatePersistence`: маппинг домен ↔ DTO; не сериализовать живой `GameState` напрямую; схема/версия; без новых классов.
  3. Переделать `JsonSaveStore`: save/load в `saves/` (gitignored), стабильные JSON options; без новых классов.
  4. Переписать `PersistenceTests` под v0.4 round-trip: состояние после load эквивалентно; продолжение с тем же seed детерминировано.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FullyQualifiedName~PersistenceTests"`
  - Утверждения: round-trip сохраняет минуту/очередь/RNG/edge-локации/приказы/курьеров/отчёты/лог/EventLog; продолжение детерминировано.
- **Готово, если:**
  - [ ] Существующие DTO покрывают ключевое v0.4-состояние.
  - [ ] Round-trip проходит; save/load не ломает детерминизм.
  - [ ] Новых DTO-типов/классов/проектов нет.
  - [ ] Тесты зелёные.

### T-64 — Удаление LLM/AI-проекта
- **ID:** `T-64`
- **Волна:** Wave 7
- **Покрывает:** `rules.md` (v0.4 исключает LLM; внешний контроллер enemy — `T-60`); ограничение «не добавлять новые проекты».
- **Владеет файлами:**
  - `Kriegsspiel.slnx`
  - `Kriegsspiel.Console/Kriegsspiel.Console.csproj`
  - `Kriegsspiel.AI/Kriegsspiel.AI.csproj` (удалить)
  - `Kriegsspiel.AI/LlmAmbiguityInterpreter.cs` (удалить)
  - `Kriegsspiel.AI/LlmOrderCompiler.cs` (удалить)
  - `Kriegsspiel.AI/LlmReportNarrator.cs` (удалить)
  - `Kriegsspiel.AI/StateQueryTools.cs` (удалить)
  - `Kriegsspiel.AI/Prompts/` (удалить)
  - `Kriegsspiel.Core/Ai/AiPipelines.cs` (удалить)
  - `Kriegsspiel.Core/Ai/AiPromptBuilder.cs` (удалить)
  - `Kriegsspiel.Core/Ai/AiReplayLog.cs` (удалить)
  - `Kriegsspiel.Core/Ai/AiSchemas.cs` (удалить)
  - `Kriegsspiel.Core/Ai/AiStableId.cs` (удалить)
  - `Kriegsspiel.Core/Ai/AiWorldView.cs` (удалить)
  - `Kriegsspiel.Core/Ai/DoctrineOrderFallback.cs` (удалить)
  - `Kriegsspiel.Core/Ai/ILLmClient.cs` (удалить)
  - `Kriegsspiel.Core/Ai/NarrationFallback.cs` (удалить)
  - `Kriegsspiel.Core.Tests/AIAdapterTests.cs` (удалить)
  - Не трогать `Kriegsspiel.Core/Ai/DeterministicRandomSource.cs` (владелец `T-51`); не создавать новые проекты/типы.
- **Зависимости:** `T-62` (Console больше не использует AI).
- **Что сделать:**
  1. Удалить проект `Kriegsspiel.AI` из `Kriegsspiel.slnx` и ссылку на него из `Kriegsspiel.Console.csproj`.
  2. Удалить LLM-адаптеры, промпты/схемы, AI-пайплайны и `AIAdapterTests`; `DeterministicRandomSource` оставить.
  3. Убедиться, что в решении нет LLM/`UseLlm`/AI-регистраций; v0.4 работает без LLM.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx`
  - `grep -R "Kriegsspiel.AI\|UseLlm\|Llm" -n --include='*.cs' --include='*.csproj' --include='*.slnx' .` не находит живых ссылок (кроме истории git и docs).
  - Утверждения: проект удалён; Console собирается без AI; тесты зелёные.
- **Готово, если:**
  - [ ] `Kriegsspiel.AI` удалён из решения и ссылок.
  - [ ] LLM-классы/промпты/тесты удалены; `DeterministicRandomSource` сохранён.
  - [ ] Сборка/тесты зелёные.
  - [ ] Новых проектов/типов не добавлено.

### T-65 — Удаление временного extended-scaffolding
- **ID:** `T-65`
- **Волна:** Wave 8
- **Покрывает:** требование «без сохранения совместимости»; финальное приведение кода к `rules.md §1–§7`.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Domain/**`
  - `Kriegsspiel.Core/Orders/**`
  - `Kriegsspiel.Core/Arbitration/**`
  - `Kriegsspiel.Core/Assessment/**`
  - `Kriegsspiel.Core/Content/**`
  - `Kriegsspiel.Core/Contracts/**`
  - `Kriegsspiel.Core/Reporting/**`
  - `Kriegsspiel.Core/Persistence/**`
  - `Kriegsspiel.Console/Commands/**`
  - `Kriegsspiel.Console/Startup/**`
  - `Kriegsspiel.Console/Persistence/**`
  - `Kriegsspiel.Core.Tests/**`
  - `Content/scenarios/**`
  - Не трогать `AGENTS.md`, `docs/gameDocs/rules.md`, `docs/projectDocs/todo.md` и прочие docs; не создавать новые типы/проекты.
- **Зависимости:** `T-64` (AI удалён) и все задачи Wave 5–7.
- **Что сделать:**
  1. Удалить временные extended-члены/типы/enum-значения, не входящие в v0.4: `Cargo`, `CivilianGroup`, `ContactGeometry`, `PendingCasualty`, `OrderBranch`, `MovementMode`, `OrderConstraint`, ветви/ограничения/режимы движения, груз/гражданские/отложенные потери/слияние `fact key`/расширенные метрики.
  2. Упростить `Unit`/`Order`/`EffectiveOrder`/`Report`/`GameState`/`Zone`/`CommanderTraits` до v0.4-полей; удалить неиспользуемые extended-поля и event-типы; обновить оставшиеся ссылки.
  3. Удалить `ContactGeometryResolver` и другие extended-only файлы; обновить DI/резолверы/рендеры.
  4. Удалить устаревшие тесты удалённых механик; оставить только v0.4-проверки в существующих тестовых классах.
  5. Проверить, что в коде не осталось dual-path/legacy-fallback/`Engine`-ветки; `DeterministicRandomSource` остаётся.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx`
  - `grep -R "ContactGeometry\|OrderBranch\|MovementMode\|OrderConstraint\|PendingCasualt\|Cargo\|CivilianGroup\|Morale\|Fatigue\|PoorControl\|Captured" -n --include='*.cs' Kriegsspiel.Core Kriegsspiel.Console Kriegsspiel.Core.Tests` не находит живых ссылок.
  - Утверждения: extended-поля/типы удалены; сборка/тесты зелёные; новых типов нет.
- **Готово, если:**
  - [ ] В коде не осталось extended-механик вне v0.4.
  - [ ] Устаревшие тесты/сценарии удалены; v0.4-проверки зелёные.
  - [ ] Нет dual-path/legacy-fallback/`Engine`-ветки.
  - [ ] Новых типов/проектов нет.

### T-66 — Документация и структура v0.4
- **ID:** `T-66`
- **Волна:** Wave 8
- **Покрывает:** `AGENTS.md` (актуализация `structure.md`/доков), `docs/projectDocs/instructions.md` (конвенции запуска); механик не добавляет.
- **Владеет файлами:**
  - `docs/projectDocs/structure.md`
  - `readme.md`
  - `docs/projectDocs/instructions.md`
  - Не трогать другие файлы.
- **Зависимости:** `T-65`.
- **Роль:** `docs-maintainer` (не `implementer`).
- **Что сделать:**
  1. Обновить `structure.md`: отразить v0.4 Core/Console/Test-файлы, `test_01`/`crossing_01`, отсутствие `Kriegsspiel.AI`, отсутствие extended-механик; не выдумывать структуру.
  2. Обновить `readme.md`: запуск `--Scenario test_01`/`crossing_01`, команды проверки, ограничения v0.4; без LLM/dual-engine.
  3. Обновить `instructions.md`: конвенции существующих типов, тестовые фильтры, запрет новых типов/проектов; не менять `AGENTS.md`/`rules.md`.
  4. Проверить, что docs не расходятся с фактической структурой.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`; `dotnet test Kriegsspiel.slnx` (без изменений)
  - `grep -n "test_01\|crossing_01\|Kriegsspiel.AI" docs/projectDocs/structure.md readme.md docs/projectDocs/instructions.md`
  - `git diff --name-only` — только docs-файлы; `AGENTS.md`/`docs/gameDocs/rules.md` не изменены.
- **Готово, если:**
  - [ ] `structure.md` отражает фактические v0.4-файлы и сценарии.
  - [ ] `readme.md` описывает запуск/проверки/ограничения v0.4.
  - [ ] `instructions.md` актуален.
  - [ ] `AGENTS.md`/`rules.md` не изменены; код не менялся.

## Launch-план (машиночитаемая часть для orchestrator)

```yaml
base: master@340829f
plan_branch: docs/arch-mvp-transition-plan-r3
plan_worktree: /tmp/opencode/ks-arch-mvp-transition-r3
wave_branch_pattern: "wave/<N>-<slug>"
task_branch_pattern: "task/T-XX-<kebab-slug>"
merge_policy: per-task-after-verified (no batch)
constraints:
  new_projects: forbidden
  new_types: forbidden
  new_mvp_namespaces: forbidden
  dual_engine: forbidden
  legacy_fallback: forbidden
  tech_debt: none
waves:
  - id: 5
    slug: v04-foundation
    groups:
      - [T-50]
      - [T-51, T-52, T-53]
      - [T-54]
    merge: "T-50 first; then T-51/T-52/T-53 in any order; T-54 after T-53"
    gate_ref: "docs/projectDocs/todo.md#wave-5--сценарий-проверок-волны"
  - id: 6
    slug: v04-core-mechanics
    depends: [5]
    groups:
      - [T-55, T-56, T-57, T-58, T-59]
      - [T-60]
    merge: "G1 any order after all verified; T-60 after T-56/T-59; no new types"
    gate_ref: "docs/projectDocs/todo.md#wave-6--сценарий-проверок-волны"
  - id: 7
    slug: v04-integration-console-persistence
    depends: [6]
    groups:
      - [T-61]
      - [T-63]
      - [T-62]
      - [T-64]
    merge: "T-61 first; then T-63; then T-62; T-64 after T-62"
    gate_ref: "docs/projectDocs/todo.md#wave-7--сценарий-проверок-волны"
  - id: 8
    slug: cleanup-docs
    depends: [7]
    groups:
      - [T-65]
      - [T-66]
    merge: "T-65 first; T-66 after T-65"
    owner: "T-66=docs-maintainer"
    gate_ref: "docs/projectDocs/todo.md#wave-8--сценарий-проверок-волны-cleanupdocs"
```

## Принятые трактовки, риски и эскалации

- **Совместимость не сохраняется:** старые API/тесты/поведение могут ломаться. Промежуточное сохранение extended-членов нужно только для зелёной сборки до `T-65`; после `T-65` в коде не остаётся v0.4-несовместимого scaffolding.
- **Ограничение «без новых типов» vs канон:** все понятия v0.4 выражаются существующими типами (см. карту). Для новых полей/статусов добавляются члены существующих классов, а не новые типы. Если для механики объективно нужен новый тип — эскалация, не молчаливое расширение scope.
- **События:** новые record-типы `GameEvent` не добавляются. Для событий, которых нет в существующем наборе (`UnitDestroyed`, `UnitRetreated`, `RestApplied`, `VictoryEvaluated`, `UnitEnteredEdge`), MVP использует существующие события/`Report`/`TurnResult`/AAR и фиксирует семантику в `T-50`/`T-61`. Если `verifier`/`implementer` считает, что без нового события канон §2/§7 невыразим — эскалация `orchestrator` → `rules-auditor`/`analyst`, без молчаливого добавления типа.
- **Enum-члены:** новые enum-типы не создаются. Допустимо добавлять/удалять/переупорядочивать значения существующих enum (например, фазы `Clock`); переименование/добавление членов — только если нужно для компиляции/канона. Если без нового значения нельзя выразить канон — эскалация.
- **AI/LLM:** v0.4 исключает LLM; `Kriegsspiel.AI` удаляется из решения (`T-64`), `EnemyDoctrine` остаётся детерминированным скриптовым контроллером (`T-60`).
- **Legacy-сценарии:** целевыми остаются `test_01`/`crossing_01`; `fs005_scale_demo.json`/`river_line_10x10.json` удаляются как устаревшие (`T-52`), fallback отсутствует.
- **Тесты:** новые тестовые классы не создаются; существующие переписываются/удаляются. Если для проверки механики объективно нужен новый тестовый класс — эскалация (BLOCKERS) с альтернативой «расширить существующий класс», а не молчаливое добавление.
- **Блокеры канона:** не выявлены; аудит r6 `passed`. При обнаружении реальной неоднозначности — эскалация `orchestrator` → `rules-auditor`/`analyst`, без расширения механик.
