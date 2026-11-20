# TODO: задачи для приведения движка в соответствие правилам

Источник правил: `docs/gameDocs/rules.md`. Задачи меняют только код и инфраструктуру, не правила.

## Формат и правила работы

- **Формат задачи:** каждая задача содержит `ID`, `Волна`, `Покрывает`, `Владеет файлами`, `Зависимости`, `Что сделать`, `Как проверить`, `Готово, если`. Задача предназначена для прямой выдачи агенту-разработчику без дополнительной декомпозиции.
- **File ownership:** агент меняет **только** файлы из раздела `Владеет файлами` своей задачи (включая помеченные `(новый)`). Другие файлы, включая тесты/контракты/конфиги соседних задач, не трогать. Если для компиляции/интеграции нужен чужой файл — зависимость выносится в следующую волну.
- **Волны:** внутри одной волны задачи выполняются параллельно и не имеют пересечений по файлам. Следующая волна стартует после завершения предыдущей и прохождения проверок интеграции.
- **Канонические команды проверки:** `dotnet build Kriegsspiel.slnx`, `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj` (при появлении отдельных test-проектов — `dotnet test Kriegsspiel.slnx`). В текущем окружении `dotnet` может отсутствовать; команды остаются каноническими и обязательными для фиксации в `Как проверить`.
- **Трассировка правил:** явные ID правок `K1..D8` в `docs/gameDocs/rules.md` отсутствуют; поле `Покрывает` использует разделы/пункты правил (`§1`–`§14`) и предметные механики. Трассы K/B/D сохранять в тексте задачи там, где применимо к доменной области (K — knowledge/fog, B — battle/combat, D — delivery/orders flow).
- **Ограничения:** код без добавления комментариев, если пользователь явно не попросил; `AGENTS.md` и файлы правил не менять.
- **Техдолг:** перед новой декомпозицией/волной сверяться с `docs/projectDocs/tech-debt.md`; находки из выполнения задач фиксировать там по шаблону.

## Карта волн

| Волна | Задачи | Зависимости | Правило |
|---|---|---|---|
| Wave 0 — фундамент | `T-01`, `T-02`, `T-03` | нет | Параллельно, файлы не пересекаются. Закладывает доменные типы, контракты, часы, контент и валидацию. |
| Wave 1 — механики ядра | `T-10`, `T-11`, `T-12`, `T-13`, `T-14`, `T-15`, `T-16`, `T-17`, `T-18` | Wave 0 | Параллельно, файлы не пересекаются. Реализации опираются на контракты/типы Wave 0, `Arbiter` не трогают (кроме задач-владельцев). |
| Wave 2 — координация | `T-20` | Wave 0, Wave 1 | Единственная задача интеграции конвейера в `Arbiter`. |
| Wave 3 — CLI/UI, интеграция, уборка | `T-21a`, `T-21b`, `T-21c`, `T-21d`, `T-21e`, `T-22`, затем `T-23` | Wave 2 (`T-23` — после всех задач Wave 3) | `T-21a`–`T-21e`, `T-22` параллельно по непересекающимся файлам; `T-23` финальный (ренеймы/чистка/документация) после завершения всех остальных. |

## Wave 0 — фундамент

### T-01 — Domain and contracts
- **ID:** `T-01`
- **Волна:** Wave 0
- **Покрывает:** §2–§12 (юниты/командиры/зоны/объекты/груз/маскировка, приказ и эффективный приказ, донесение и знание, события, состояние игры, боеспособность), трассы K/B/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Domain/Unit.cs`
  - `Kriegsspiel.Core/Domain/Zone.cs`
  - `Kriegsspiel.Core/Domain/CommanderTraits.cs`
  - `Kriegsspiel.Core/Domain/Order.cs`
  - `Kriegsspiel.Core/Domain/Report.cs`
  - `Kriegsspiel.Core/Domain/PlayerKnowledge.cs`
  - `Kriegsspiel.Core/Domain/GameEvent.cs`
  - `Kriegsspiel.Core/Domain/GameState.cs`
  - `Kriegsspiel.Core/Domain/ZoneObject.cs` (новый)
  - `Kriegsspiel.Core/Domain/MovableObject.cs` (новый)
  - `Kriegsspiel.Core/Domain/CivilianGroup.cs` (новый)
  - `Kriegsspiel.Core/Domain/EffectiveOrder.cs` (новый)
  - `Kriegsspiel.Core/Domain/Courier.cs` (новый)
  - `Kriegsspiel.Core/Domain/Cargo.cs` (новый)
  - `Kriegsspiel.Core/Domain/ContactGeometry.cs` (новый)
  - `Kriegsspiel.Core/Domain/PendingCasualty.cs` (новый)
  - `Kriegsspiel.Core/Contracts/IReportFactory.cs` (новый)
  - `Kriegsspiel.Core/Contracts/IKnowledgeUpdater.cs` (новый)
  - `Kriegsspiel.Core/Contracts/IRandomSource.cs` (новый)
  - `Kriegsspiel.Core/Contracts/IGameRules.cs` (новый)
  - `Kriegsspiel.Core.Tests/DomainInvariantsTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** нет.
- **Что сделать:**
  1. Ввести явную сторону `Faction` (замена convention `ENEMY_`) и использовать её в доменных моделях (`Unit`, знание/отчёты/события где применимо) как источник истины для принадлежности; не менять логику resolver/CLI вне этих файлов.
  2. Расширить `CommanderTraits` до 5 параметров: `Initiative`, `Caution`, `Literalism`, `ReportHonesty`, `Professionalism` (0–3).
  3. Расширить `Unit`: `Training`, `Experience`, `CommandLossResilient`, `Concealed`, `AttentionEdgeId`, `MoveEdgeId`, `GuardOfObjectId`, `Captured`, `NoPrisonersPolicy`, ссылка на эффективный приказ (`CurrentOrderId`/эквивалент указывает на `EffectiveOrder`), поля/флаги нужные для `CombatEffective` и состояния под плохим управлением; сохранить совместимость с текущими базовыми боевыми полями.
  4. Зафиксировать доменное определение `CombatEffective` (§7, §11): `Strength >= 0.4 * InitialStrengthSnapshot && Morale >= 30 && Readiness >= 20 && Ammo > 0` (метод/функция/свойство в домене, пригодное для переиспользования).
  5. Расширить `Zone`: типы рёбер (`EdgeType`: Road/Offroad/Crossing), поддержка объектов зоны/груза/контроля (`ZoneObject`, `Cargo`, `ControllingFaction`), укрытие/наблюдаемость и связанные с картой данные; выделить `ZoneObject` (bridge/fortification/observationPost/depot, состояние/целостность) в отдельный тип.
  6. Добавить новые доменные типы: `MovableObject`, `CivilianGroup`, `EffectiveOrder`, `Courier` (состояния и плечи/ETA-данные, необходимые для пула), `Cargo` (owner/количество/тип), `ContactGeometry` (front/flank/rear и данные векторов/рёбер), `PendingCasualty` (minute, unitId, amount/поля тика).
  7. Переработать `Order`: 7 слотов конструктора + `orderVersion`, ветви (`Branches`, ≤3), `MovementMode`, `Constraint` как enum (закрытый список), `TargetObjectId`, `TargetUnitId`, дедлайны/срок донесения, поля исходного текста/метаданных; сохранить исходный приказ как журналируемую сущность отдельно от `EffectiveOrder`.
  8. Переработать `Report`: 4 обязательных поля (минута события, минута доставки, источник, уверенность 0–3) + тип/факты/текст/`fact key` или эквивалентные поля, необходимые для K/B/D-потока; поддержать `ReportKind.Impossible` и совместимые виды донесений.
  9. Переработать `PlayerKnowledge`: только знание игрока (не истина), отсутствие данных vs пусто, два timestamp (событие/доставка-обновление), уверенность 0–3, хранение обеих версий при противоречии и правило снижения уверенности.
  10. Расширить `GameEvent` новыми типами событий: `QuerySent`, `QueryAnswered`, `QuerySilence`, `EdgeDestroyed`, `CamouflageRevealed`, `GuardRelieved`, `PendingCasualties`, `ReportDelivered`, `OrderSuperseded`, `OrderMisinterpreted`, `CombatTick` (сохранить/адаптировать существующие события без потери семантики).
  11. Расширить `GameState`: `InTransitReports`, `CourierPool`, `Seed`/состояние RNG, `PendingCasualties`, журнал/очереди и данные знаний, необходимые для детерминизма и сохранения; не реализовывать резолверы/сериализацию вне контрактов.
  12. Объявить контракты в `Kriegsspiel.Core/Contracts`: `IReportFactory`, `IKnowledgeUpdater`, `IRandomSource`, `IGameRules` (минимально достаточные сигнатуры для параллельной работы Wave 1: создание донесений/обновление знания/детерминированный RNG/правила-параметры и проверки).
  13. Добавить `DomainInvariantsTests.cs`: проверки инвариантов домена (например, `CombatEffective`, границы уверенности 0–3, наличие 4 полей донесения, запрет отрицательных ключевых значений, корректность enum/версий приказов, различие no-data/empty в знании).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter DomainInvariantsTests`
  - Утверждения: `CombatEffective` возвращает `false` при нарушении любого порога; `Report`/DTO-поля требуют event minute, delivery minute, source, confidence; `PlayerKnowledge` различает «нет данных» и «пусто»; `Order` хранит `orderVersion` и исходные ветви; `GameState` содержит `InTransitReports`, `CourierPool`, `Seed`/RNG state, `PendingCasualties`.
- **Готово, если:**
  - [x] Все перечисленные доменные/контрактные файлы созданы или обновлены в рамках ownership.
  - [x] Новые типы/поля/события из §2–§12 представлены в домене и компилируются.
  - [x] Контракты Wave 1 объявлены и не требуют правок `Arbiter`/CLI.
  - [x] `DomainInvariantsTests` добавлен и покрывает ключевые инварианты.

### T-02 — Clock and event ordering
- **ID:** `T-02`
- **Волна:** Wave 0
- **Покрывает:** §3 (порядок событий внутри отрезка, очередь/время), трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Domain/Clock.cs`
  - `Kriegsspiel.Core.Tests/ClockTests.cs`
  - Не трогать другие файлы.
- **Зависимости:** нет.
- **Что сделать:**
  1. Заменить ключ планирования с `int` на детерминированный ключ `(minute, phase, sequence)`; определить фазы минимум: `Delivery`, `Understanding`, `Movement`, `Combat`, `ReportCreate`, `ReportDeliver`, `Scheduled` (значения/порядок стабильны и документированы в коде типа/enum).
  2. Изменить `Schedule` так, чтобы попытка планирования в прошлое (`at < Now` по минуте/фазе) приводила к явной ошибке (исключение) либо требовала отдельного явного API; не выполнять тихий перенос в `Now`.
  3. Гарантировать `AdvanceTo(target)` работает при `target >= Now`, возвращает события в стабильном порядке `(minute, phase, sequence)` и корректно продвигает `Now`.
  4. Добавить `PendingCount` (и при необходимости безопасный просмотр/состояние очереди без мутации), чтобы тесты и интеграция могли проверять отложенные события.
  5. Обновить `ClockTests`: порядок при совпадении минут, запрет прошлого, `AdvanceTo` в будущее/на текущее время, учёт sequence, `PendingCount`.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ClockTests`
  - Утверждения: события с одинаковой минутой выдаются по фазам `Delivery→Understanding→Movement→Combat→ReportCreate→ReportDeliver→Scheduled`; `Schedule` в прошлое бросает ожидаемое исключение; `AdvanceTo(Now)` не теряет события `Now`; `PendingCount` уменьшается при извлечении.
- **Готово, если:**
  - [x] `Clock` использует `(minute, phase, sequence)` и стабильный порядок.
  - [x] Планирование в прошлое не маскируется silent-clamp.
  - [x] `ClockTests` покрывают порядок, прошлое и `PendingCount`.

### T-03 — Content profile and validation
- **ID:** `T-03`
- **Волна:** Wave 0
- **Покрывает:** §2, §4, §5, §6, §7, §9–§12 (параметры профиля/сценария, шаблоны донесений, цели и валидация).
- **Владеет файлами:**
  - `Kriegsspiel.Core/Content/ContentLoader.cs`
  - `Kriegsspiel.Core/Content/Scenario.cs`
  - `Kriegsspiel.Core/Content/ScenarioBundle.cs`
  - `Kriegsspiel.Core/Content/Objective.cs`
  - `Kriegsspiel.Core/Content/FrictionTable.cs`
  - `Kriegsspiel.Core/Content/ReportTemplates.cs`
  - `Content/friction.json`
  - `Content/reports.json`
  - `Content/scenarios/crossing_01.json`
  - `Content/scenarios/test_01.json`
  - `Kriegsspiel.Core.Tests/Fixtures/TestContent.cs`
  - `Kriegsspiel.Core.Tests/ContentLoaderTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** нет.
- **Что сделать:**
  1. Расширить `FrictionTable`/модели контента до параметров §12: вероятности/веса/времена, курьеры, `P0/Pmin/Pmax`, веса A, режимы передвижения, lethality/morale/capture/civilians/supply/recon, модификаторы и профили сложности (только данные/схема/валидация в рамках этих файлов).
  2. Расширить модели `Scenario`/`Objective`/`ScenarioBundle`: типы рёбер, объекты зон, цели всех поддерживаемых типов с контрольными минутами и весами (сумма весов = 1), пороги/состояния контроля и стартовые параметры, необходимые правилам.
  3. Обновить `ContentLoader` строгой валидацией: вероятности ∈ [0,1], суммы весов = 1 где требуется, `couriers >= 1`, уникальность id зон/юнитов/объектов, согласованность/симметричность рёбер, валидные перечисления, положительные времена (`deliveryTime`/`returnTime` > 0), неотрицательные коэффициенты.
  4. Обновить `ReportTemplates` и `Content/reports.json`: поддержка placeholder-полей для фактов донесений (время события/доставки, источник, уверенность, тип, зона/юнит/числа) без привязки к UI.
  5. Обновить JSON-контент (`friction.json`, `scenarios/*.json`, `reports.json`) под новую схему и валидные тестовые данные; обновить `TestContent` как каноничную fixture для тестов ядра.
  6. Добавить `ContentLoaderTests`: позитивная загрузка валидного контента и негативные кейсы на каждую ключевую валидацию (вероятность вне диапазона, сумма весов != 1, дубли id, битое ребро, 0 курьеров, невалидный enum/время).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ContentLoaderTests`
  - Утверждения: валидные `Content/*.json` и `TestContent` загружаются без ошибок; загрузка с вероятностью `> 1`, суммой весов целей `!= 1`, `couriers == 0`, дублирующим id или несимметричным/несуществующим ребром завершается контролируемой ошибкой валидации.
- **Готово, если:**
  - [x] Схема контента покрывает параметры §12 в пределах файлов владельца.
  - [x] `ContentLoader` валидирует обязательные инварианты профиля/сценария.
  - [x] Сценарии/шаблоны/fixture обновлены и проходят тесты загрузки.

> Проверки сборки и тестов выполнены после установки .NET 10.0.404: `dotnet build Kriegsspiel.slnx` — 0 предупреждений/0 ошибок; `dotnet test Kriegsspiel.slnx --no-build` — Passed 46, Failed 0. Итоги — `docs/projectDocs/reports/wave-0-report.md`.

## Wave 1 — механики ядра

### T-10 — Couriers and transmission
- **ID:** `T-10`
- **Волна:** Wave 1
- **Покрывает:** §3 (курьеры, очередь, подтверждение доставки), §5 стадия 1, трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/ITransmitionResolvers.cs`
  - `Kriegsspiel.Core/Arbitration/TransmitionResolver.cs`
  - `Kriegsspiel.Core/Arbitration/CourierPool.cs` (новый)
  - `Kriegsspiel.Core.Tests/TransmitionResolverTests.cs`
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать модель/логику пула курьеров в `CourierPool`: состояния free → outbound → return → returned/lost, отдельные шансы потери на плечо, замена через 60 минут после расчётного времени возврата, FIFO-очередь, немедленная отправка при освобождении, приоритет запросов над приказами.
  2. Интегрировать `CourierPool` в `TransmitionResolver` (и интерфейс при необходимости сигнатур): расчёт/фиксация `deliveryTime`/`returnTime` (>0), ETA отправки/доставки/возврата, `deliveryConfirmed=false` при потере на обратном пути, доставка приказа не отменяется потерей обратного плеча.
  3. Поддержать повтор/обработку запроса как сообщение, использующее курьера (совместимо с контрактами/событиями T-01), без изменения `Arbiter` и CLI.
  4. Обновить `TransmitionResolverTests`: пример из §14 (06:15 выдан → доставка 06:45 при deliveryTime=30, возврат по ETA), очередь FIFO, приоритет запроса, потеря туда/обратно, замена через 60 минут, ETA пересчитывается от фактической отправки.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter TransmitionResolverTests`
  - Утверждения: при занятых курьерах сообщение ставится в очередь и получает ETA от фактической отправки; запрос отправляется раньше приказа при одинаковом времени постановки в очередь; потеря на обратном пути не создаёт «приказ не доставлен», но оставляет `deliveryConfirmed=false`; замена курьера доступна через 60 мин после расчётного возврата.
- **Готово, если:**
  - [ ] `CourierPool` и resolver реализуют состояния, приоритеты, потери по плечам и замену.
  - [ ] Пример 06:15→06:45 воспроизводится в тестах.
  - [ ] Тесты transmission покрывают FIFO, приоритет, потери и ETA.

### T-11 — Orders ambiguity/understanding
- **ID:** `T-11`
- **Волна:** Wave 1
- **Покрывает:** §4 (конструктор, неоднозначность A, формула P, конфликты, ветви, искажения), §5 стадия 2, трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Orders/AmbiguityScorer.cs`
  - `Kriegsspiel.Core/Orders/Misinterpretation.cs`
  - `Kriegsspiel.Core/Orders/ConflictResolver.cs` (новый)
  - `Kriegsspiel.Core/Orders/OrderBranch.cs` (новый)
  - `Kriegsspiel.Core/Arbitration/IUnderstandingResolver.cs`
  - `Kriegsspiel.Core/Arbitration/UnderstandingResolver.cs`
  - `Kriegsspiel.Core.Tests/AmbiguityScorerTests.cs`
  - `Kriegsspiel.Core.Tests/UnderstandingResolverTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Переписать `AmbiguityScorer` по §4: нет цели (для намерений, где обязательна) +0.7, расплывчатость +0.25, нет дедлайна +0.1, нет срока донесения +0.1, каждая ветвь +0.05, `clamp(0,1)`; пустой `Куда` для `Hold` даёт 0, для `Reserve` — подстановка зоны резерва сценария (через контекст/правила), для прочих — вклад «нет цели».
  2. Реализовать `ConflictResolver` (детерминированно до roll P): доминирующая пара черт, лестница «запрос → буквальное исполнение → собственная оценка/инициатива», сокрытие нарушения при честности ≤1, формирование непротиворечивого эффективного приказа.
  3. Реализовать поддержку ветвей `OrderBranch` (закрытые условия/действия, ≤3), проверка условий по знанию командира; ветка `получен противоречащий приказ` срабатывает только если старый приказ уже исполняется.
  4. В `UnderstandingResolver` внедрить формулу `P = clamp(max(P0,Pmin)+A*(1+0.15*Literalism+0.10*Caution-0.10*Initiative), Pmin, Pmax)` с поддержкой модификатора `Pmin` профиля; выполнять **один** roll на приказ после разрешения конфликтов.
  5. Реализовать исходы по §4/§5: `no_target` → `correct`/`shift`/`clarify`; `shift` только в соседнюю зону по знанию командира; `clarify` — отдельное событие/запрос с курьером (не misinterpretation); применить 5 типов искажений к `EffectiveOrder` (сдвиг цели, подмена намерения, задержка, буквальное исполнение как исход разрешения, ошибка выбора ветви фиксируется отдельно), исходный приказ не мутировать.
  6. Добавить/обновить тесты: точные веса A, clamp, формула P, детерминизм конфликтов, один roll, `clarify` не считается misinterpretation, `shift` только к соседу из знания, ветви по знанию/ограничение на 3.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "AmbiguityScorerTests|UnderstandingResolverTests"`
  - Утверждения: приказ без цели/дедлайна/срока донесения даёт ожидаемое A (с учётом clamp); `Hold` с пустым `Куда` не добавляет 0.7; при A=0 P не опускается ниже `max(P0,Pmin)`; конфликт решается до P и даёт ожидаемый исход по чертам; при одном приказе выполняется один roll; `clarify` создаёт запрос/событие без `OrderMisinterpreted` типа misinterpretation.
- **Готово, если:**
  - [ ] A, P и разрешение конфликтов соответствуют §4.
  - [ ] `EffectiveOrder` формируется/искажается отдельно от исходного приказа.
  - [ ] Тесты покрывают веса, формулу, ветви, clarify/shift/correct и один roll.

### T-12 — Execution and routing
- **ID:** `T-12`
- **Волна:** Wave 1
- **Покрывает:** §2 (рёбра/объекты), §3–§5 (исполнение, маршрутизация, невыполнимость, дедлайны), трассы D/B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/IExecutionResolver.cs`
  - `Kriegsspiel.Core/Arbitration/ExecutionResolver.cs`
  - `Kriegsspiel.Core/Arbitration/PathPlanner.cs` (новый)
  - `Kriegsspiel.Core.Tests/ExecutionResolverTests.cs` (новый)
  - Не трогать другие файлы (в частности `CombatResolver.cs`).
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать `PathPlanner`: multi-hop маршруты по графу зон с учётом `EdgeType`, проходимости (`BridgeIntact`, разрушенные рёбра/объекты), режимов движения и времён перехода; запрет «телепорта» в целевую зону.
  2. В `ExecutionResolver` перейти на пошаговое движение по маршруту: планирование `UnitMoved`/`UnitArrived`/промежуточных шагов, контакт в промежуточной зоне порождает `CombatTick` (через события/планирование по контрактам), учёт дедлайнов и невозможности прибытия.
  3. Реализовать реакции на невыполнимый приказ по §5 (инициатива/осторожность/доминирующая пара): импровизация, stop+запрос 30–60 мин, выбор по чертам; не подменять логику понимания T-11.
  4. Применять дельты усталости/боеготовности/внимания по правилам и профилю (через `IGameRules`/параметры T-03), использовать `IReportFactory`/`IGameRules` из T-01 для планируемых событий/донесений в пределах ответственности execution.
  5. Добавить `ExecutionResolverTests`: multi-hop маршрут, блокировка разрушенным мостом, контакт в промежуточной зоне, реакция на невыполнимость по чертам, соблюдение дедлайна/режима, отсутствие телепорта.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ExecutionResolverTests`
  - Утверждения: путь строится через несколько рёбер с корректным временем; непроходимое ребро исключается/меняет маршрут; вход в занятую зону до цели создаёт боевой контакт/тик по контракту; при невыполнимом приказе исход соответствует чертам (импровизация/стоп+запрос/доминирующая пара).
- **Готово, если:**
  - [ ] Маршрутизация и движение работают по графу и типам рёбер.
  - [ ] Невыполнимые приказы и промежуточные контакты обрабатываются по правилам.
  - [ ] Тесты execution покрывают маршруты, блокировки, контакты и реакции.

### T-13 — Combat and contact geometry
- **ID:** `T-13`
- **Волна:** Wave 1
- **Покрывает:** §2, §7, §8 (геометрия контакта, формулы боя, потери/мораль/боезапас, `PoorControl`, пленные/отход), трасса B.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/ICombatResolver.cs`
  - `Kriegsspiel.Core/Arbitration/CombatResolver.cs`
  - `Kriegsspiel.Core/Arbitration/ContactGeometryResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/CombatResolverTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать `ContactGeometryResolver`: определение front/flank/rear по `AttentionEdgeId`/`MoveEdgeId` и рёбрам графа (включая профильный fallback/surprise-level при неоднозначной геометрии), право замечания первым влияет на posture/attention в допустимых пределах.
  2. Переписать `CombatResolver` по формулам §7: `Strength = N × Readiness/100 × Morale/100 × Position × Terrain × Collision`, terrain-бонус только при обороне, collision front/flank/rear, штрафы морали −10/−15 при фланге/тыле, убрать случайный множитель 1.0–1.1.
  3. Реализовать потери/тик: раздельный расчёт потерь сторон, немедленная половина + `PendingCasualties(minute+30)`, списание морали в том же тике, расход боезапаса/усталость, влияние `Ammo==0`, проверка `CombatEffective`.
  4. Учесть `PoorControl` (множители/качество исполнения в бою) и состояния отхода/паники/пленения по приоритетам §7–§8; события/метки для разбора в пределах доменных типов T-01.
  5. Добавить `CombatResolverTests`: детерминизм (без random 1.0–1.1), front/flank/rear, terrain только в обороне, штрафы морали, отложенные потери, ammo/fatigue/readiness эффекты, auto-withdraw/poor control.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter CombatResolverTests`
  - Утверждения: при одинаковом входе и seed/RNG-источнике результат боя детерминирован; flank даёт collision 1.4 и −10 морали, rear 1.8 и −15; ровно половина потерь планируется в `PendingCasualties`; `Ammo==0` снижает боевую эффективность согласно правилам/контрактам.
- **Готово, если:**
  - [ ] Геометрия и формулы боя реализованы без случайного 1.0–1.1.
  - [ ] `PendingCasualties`, мораль, боезапас и `PoorControl` обрабатываются по правилам.
  - [ ] Тесты боя покрывают ключевые формулы и состояния.

### T-14 — Reports, fog of war and recon
- **ID:** `T-14`
- **Волна:** Wave 1
- **Покрывает:** §5 стадия 4, §6 (туман войны, разведка, `fact key`, противоречия, наблюдение), трасса K/D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Reports/IReportNarrator.cs`
  - `Kriegsspiel.Core/Arbitration/KnowledgeUpdater.cs` (новый; реализация контракта из `T-01`, без создания второго интерфейса)
  - `Kriegsspiel.Core/Arbitration/ReconResolver.cs` (новый)
  - `Kriegsspiel.Core.Tests/FogOfWarTests.cs` (новый)
  - `Kriegsspiel.Core.Tests/ReportTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать контракт `IKnowledgeUpdater` из `Kriegsspiel.Core/Contracts/` (владелец `T-01`) классом `KnowledgeUpdater`; `IReportFactory` использовать как готовый контракт T-01. Не создавать дублирующий интерфейс в `Arbitration`. Логику формирования/применения донесений разместить в рамках `Core/Reports/*` и новых arbitration-файлов владельца, строго через `PlayerKnowledge`.
  2. Реализовать обновление знания: `fact key`, группировка, штраф противоречия до повышения уверенности, cross-confirmation только совпадающих версий от разных источников/наблюдений, хранение обеих версий, no-data vs empty, два timestamp, уверенность 0–3.
  3. Реализовать искажение/потерю донесений: честность (`ReportHonesty`) влияет на числа/текстовые факты, обычные источники confidence 1–2, разведка/подтверждение до 3, потеря донесения с вероятностью ниже приказов и зависимостью от враждебности маршрута (параметры профиля).
  4. Реализовать `ReconResolver`: двухшаговая модель `P_success = f(observability, observationTime, reconProfile)`, при провале `P_unconscious|fail = clamp(base×(3−obs)/3,0,cap)` и осознанный провал, взаимное обнаружение/наблюдение по формулам §6.
  5. Обеспечить, что UI/потребители получают только знание (через `PlayerKnowledge`/донесения), не истину; нарратор использует `ReportTemplates` и не выдумывает факты/время/источник/уверенность.
  6. Добавить `FogOfWarTests` и `ReportTests`: конфликт версий, cross-confirm, потеря/задержка, искажение честностью, no-data/empty, recon success/fail ветви, отсутствие утечки истины в знание.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter "FogOfWarTests|ReportTests"`
  - Утверждения: противоречащие донесения по одному `fact key` снижают уверенность и сохраняют обе версии; повышение уверенности не применяется к конфликтующим версиям; обычный отчёт не даёт confidence 3; recon при `obs=3` и ненулевом base может давать неосознанное заблуждение согласно профилю; знание не содержит скрытых истинных полей юнитов/зон.
- **Готово, если:**
  - [ ] `KnowledgeUpdater`/`ReconResolver` и report-логика реализуют §6.
  - [ ] `IReportNarrator` и шаблоны используются без выдумывания фактов.
  - [ ] Тесты fog-of-war/report покрывают ключевые K/D-сценарии.

### T-15 — Objectives and after-action report
- **ID:** `T-15`
- **Волна:** Wave 1
- **Покрывает:** §1, §10, §11, §12 (цели, контроль, частичный счёт, метрики разбора, истина только в AAR).
- **Владеет файлами:**
  - `Kriegsspiel.Core/Assessment/AfterActionReport.cs` (новый)
  - `Kriegsspiel.Core/Assessment/MetricCalculator.cs` (новый)
  - `Kriegsspiel.Core/Arbitration/ObjectiveEvaluator.cs` (новый)
  - `Kriegsspiel.Core.Tests/ObjectiveEvaluatorTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать `ObjectiveEvaluator`: поддержка 7+ типов целей (§11), состояния контроля `Achieved/Contested/Failed` с весами `1/0.5/0`, контрольные минуты, частичный счёт по доле удачных проверок, линейный счёт цели уничтожения до P, сумма весов целей = 1, порог победы (базово 0.7).
  2. Реализовать `MetricCalculator` и `AfterActionReport`: все 8 метрик §10 (задержка приказов с правилом для потерянных, доля неоднозначных A>0, реакция на противоречия, потери из-за `PoorControl` включая `PendingCasualties`, инициатива «к лучшему» по окну оценки, буквальное исполнение, расхождения намерение≠результат, потери/задачи по гражданским).
  3. Гарантировать, что истина и полная причинность доступны только в `AfterActionReport`/разборе, а не в игровых представлениях знания.
  4. Добавить `ObjectiveEvaluatorTests`: контрольные минуты, contested/partial score, уничтожение P%, порог 0.7, расчёт метрик (включая реакцию и потерянные приказы) на детерминированных фикстурах.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ObjectiveEvaluatorTests`
  - Утверждения: `Contested` даёт 0.5; итог сценария = Σ(вес×доля) и сравнивается с 0.7; метрика реакции не засчитывает `ВРЕМЯ +30`; потери `PoorControl` учитывают отложенные потери; потерянный приказ учитывается по правилу задержки/штрафа.
- **Готово, если:**
  - [ ] Оценка целей и AAR реализованы по §10–§11.
  - [ ] Все 8 метрик вычисляются и тестируются.
  - [ ] Истина отделена от игрового знания.

### T-16 — Persistence DTO and load
- **ID:** `T-16`
- **Волна:** Wave 1
- **Покрывает:** §12 (детерминизм, replay, сохранение/загрузка), трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Persistence/*` (новая папка, все новые файлы DTO/маппинга/версий схемы)
  - `Kriegsspiel.Console/Persistence/JsonSaveStore.cs` (новый путь после переноса)
  - `Kriegsspiel.Console/Commands/JsonSaveStore.cs` (существующий файл, перенос/удаление в рамках этой задачи)
  - `Kriegsspiel.Console/Commands/LoadCommand.cs` (новый)
  - `Kriegsspiel.Core.Tests/PersistenceTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Спроектировать версионированные DTO в `Core/Persistence` (`GameStateDto`, `ClockDto`, `GameEventDto` и сопутствующие DTO): очередь/состояние часов, RNG seed/state, in-transit reports, couriers/ETA, pending casualties, knowledge, orders/effective orders/orderVersion, полиморфные события без проблем абстрактной сериализации графа.
  2. Реализовать маппинг домен ↔ DTO с версией схемы и контролем совместимости; не сериализовать живой `GameState` напрямую.
  3. Перенести реализацию сохранения из `Console/Commands/JsonSaveStore.cs` в `Console/Persistence/JsonSaveStore.cs`, адаптировать под DTO/версионирование.
  4. Добавить `Console/Commands/LoadCommand.cs` (команда загрузки/точка вызова в пределах файла) для восстановления состояния из сохранения; интеграцию в меню/DI не выполнять (T-22).
  5. Добавить `PersistenceTests`: round-trip save/load для детерминированного состояния (clock queue, RNG, pending casualties, couriers, knowledge, polymorphic events) без потери данных.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter PersistenceTests`
  - Утверждения: после save/load совпадают минута, `PendingCount`/очередь `Clock`, seed/state RNG, in-transit reports, курьеры/ETA, pending casualties, knowledge (включая конфликтующие версии), `orderVersion` и типы событий.
- **Готово, если:**
  - [ ] DTO и маппинг версионированы и покрывают ключевое состояние симуляции.
  - [ ] `JsonSaveStore` перенесён в `Console/Persistence` и использует DTO.
  - [ ] `LoadCommand` и `PersistenceTests` добавлены, round-trip проходит.

### T-17 — AI/LLM adapter
- **ID:** `T-17`
- **Волна:** Wave 1
- **Покрывает:** §2, §12 (контракт LLM, валидация/seed/лог/fallback, ограничения на знания), раздел про активного противника как источник приказов.
- **Владеет файлами:**
  - `Kriegsspiel.AI/Kriegsspiel.AI.csproj` (новый)
  - `Kriegsspiel.AI/LlmOrderCompiler.cs` (новый)
  - `Kriegsspiel.AI/LlmReportNarrator.cs` (новый)
  - `Kriegsspiel.AI/LlmAmbiguityInterpreter.cs` (новый)
  - `Kriegsspiel.AI/StateQueryTools.cs` (новый)
  - `Kriegsspiel.AI/Prompts/*` (новая папка, новые файлы промптов/схем)
  - `Kriegsspiel.Core/Ai/*` (новые файлы только при необходимости интерфейсов/адаптеров; не менять существующие файлы вне папки)
  - `Kriegsspiel.Core.Tests/AIAdapterTests.cs` (новый; единственное место размещения тестов этой задачи)
  - Не трогать другие файлы (включая `.slnx`, `Program.cs`, `ServiceRegistration.cs`, `appsettings.json`).
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Создать проект `Kriegsspiel.AI` с адаптерами: `LlmOrderCompiler` (авторство приказов вражеского штаба в строгой схеме слотов/закрытых списков), `LlmAmbiguityInterpreter` (если требуется профилем/контрактом), `LlmReportNarrator` (только переформулировка уже вычисленных структурированных фактов), `StateQueryTools`, промпты/схемы в `Prompts`.
  2. Реализовать строгую валидацию схемы ответа, фиксацию seed/лога промпта-ответа-валидации (replay), детерминированный fallback к доктрине/шаблону профиля при невалидном ответе.
  3. Гарантировать, что LLM не получает истинное состояние игрока, кроме допустимого сценарием знания; понимание приказов/бой/формирование структурированных фактов остаются детерминированными (вне LLM).
  4. При необходимости добавить только новые интерфейсы/типы в `Kriegsspiel.Core/Ai/*` без правки чужих файлов; не добавлять `ProjectReference`/проекты в `.slnx` (делает T-22).
  5. Добавить тесты в `Kriegsspiel.Core.Tests/AIAdapterTests.cs`: валидация схемы, fallback на невалидный ответ, отсутствие утечки истинного состояния игрока в prompt/context, воспроизводимость при фиксированном seed/логе.
- **Как проверить:**
  - `dotnet build Kriegsspiel.AI/Kriegsspiel.AI.csproj` (если проект подключён локально) и/или `dotnet build Kriegsspiel.slnx` после интеграции T-22
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter AIAdapterTests`
  - Утверждения: невалидный LLM-ответ отклоняется и приводит к fallback; narrator не добавляет факты/время/источник/уверенность; контекст врага не содержит скрытых полей игрока; фиксированный seed/log даёт воспроизводимый результат.
- **Готово, если:**
  - [ ] AI-адаптеры и промпты созданы в рамках ownership.
  - [ ] Валидация, логирование/replay и fallback реализованы.
  - [ ] Тесты подтверждают отсутствие утечки истины и корректный fallback.

### T-18 — Enemy doctrine
- **ID:** `T-18`
- **Волна:** Wave 1
- **Покрывает:** §2 (активный противник), §3–§5 (те же стадии приказа для противника), §12 (детерминированный fallback).
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/EnemyDoctrine.cs` (новый)
  - `Kriegsspiel.Core.Tests/EnemyDoctrineTests.cs` (новый)
  - Не трогать другие файлы (включая `Arbiter.cs`).
- **Зависимости:** `T-01`, `T-02`, `T-03`.
- **Что сделать:**
  1. Реализовать `EnemyDoctrine` как детерминированный источник/политику формирования вражеских приказов при отключённом LLM (и fallback при ошибке LLM), используя контракты/типы T-01 и параметры T-03.
  2. Обеспечить, что вражеские приказы проходят те же 4 стадии (передача, понимание, исполнение, донесение) в рамках контрактов конвейера, без специальных исключений в логике доктрины.
  3. Ограничить входные данные доктрины допустимым знанием противника/сценария (без истинного состояния игрока вне правил наблюдения).
  4. Добавить `EnemyDoctrineTests`: детерминизм при фиксированном seed/входе, генерация валидных приказов по схеме слотов, корректный fallback-путь, отсутствие зависимости от скрытой истины игрока.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter EnemyDoctrineTests`
  - Утверждения: одинаковый seed+состояние дают одинаковые приказы; при отключённом/недоступном LLM используется doctrine/fallback; приказ соответствует закрытым спискам/слотам; вход не содержит истинных скрытых данных игрока.
- **Готово, если:**
  - [ ] `EnemyDoctrine` реализована и детерминирована.
  - [ ] Приказы противника совместимы с теми же стадиями и схемой.
  - [ ] Тесты доктрины проходят.

## Wave 2 — координация ядра

### T-20 — Arbiter integration
- **ID:** `T-20`
- **Волна:** Wave 2
- **Покрывает:** §3, §5–§7, §9–§11, §14 (единая точка планирования, фазы, in-transit, бой/донесения/запросы, регрессия примера), трассы D/B/K.
- **Владеет файлами:**
  - `Kriegsspiel.Core/Arbitration/Arbiter.cs`
  - `Kriegsspiel.Core/Arbitration/TurnResult.cs`
  - `Kriegsspiel.Core.Tests/ArbiterIntegrationTests.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-01`–`T-03`, `T-10`–`T-18`.
- **Что сделать:**
  1. Сделать `Arbiter` единственной точкой планирования событий: приём новых приказов/запросов, планирование через `Clock` по фазам, обработка `InTransitReports` и событий до `target`.
  2. Интегрировать резолверы Wave 1 (transmission/understanding/execution/combat/recon/reports/knowledge/objectives/couriers/enemy doctrine hooks) через контракты и текущие интерфейсы; после движения/прибытий проверять контакт и планировать `CombatTick` на ближайших 30-минутных границах.
  3. Реализовать семантику журнала/событий: `OrderLost` не логируется как доставка, `QuerySilence` логируется; потерянные/отложенные донесения не теряются на границе отрезка, `TurnResult` содержит только донесения, доставленные к `target`.
  4. Обеспечить корректный поток «приказ → доставка → понимание → исполнение/движение → бой → формирование донесения → доставка донесения» и обработку `PendingCasualties`/scheduled событий.
  5. Добавить `ArbiterIntegrationTests`: полный пример §14 (06:15–07:15), сохранение будущих репортов, запуск боя при контакте, отсутствие утечки недоставленных донесений, регрессия на семантику QuerySilence/OrderLost.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj --filter ArbiterIntegrationTests`
  - Утверждения: приказ 06:15 доставляется/понимается/исполняется по таймлайну §14; донесение с доставкой после конца отрезка остаётся в пути; `TurnResult` на 07:15 не включает недоставленные сообщения; контакт планирует `CombatTick`; событие `QuerySilence` присутствует при отсутствии ответа в окне, `OrderLost` не отображается как доставка.
- **Готово, если:**
  - [ ] `Arbiter` координирует все стадии и очередь через `Clock`.
  - [ ] In-transit/отложенные события и бой интегрированы корректно.
  - [ ] Интеграционный регресс на §14 проходит.

## Wave 3 — CLI/UI и уборка

### T-21a — Order constructor CLI
- **ID:** `T-21a`
- **Волна:** Wave 3
- **Покрывает:** §4, §13 (конструктор приказов, слоты, ветви, режимы, ограничения, дедлайны).
- **Владеет файлами:**
  - `Kriegsspiel.Console/Commands/OrderCommand.cs`
  - `Kriegsspiel.Console/Commands/OrderBuilder.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-20`.
- **Что сделать:**
  1. Реализовать в `OrderBuilder`/`OrderCommand` полный конструктор 7 слотов: кому/что/куда/к какому времени/ограничения/когда донести/режим передвижения, закрытые списки намерений/ограничений/режимов, цели и объекты/юниты где применимо.
  2. Добавить поддержку ветвей (предпросмотр «если X → сделаю Y», не более 3), шаблонов/повторного использования, дедлайна и срока донесения.
  3. Показывать расчёт/предпросмотр неоднозначности A (если доступно через контракты/данные) без раскрытия истины и скрытого состояния; выводить только данные, доступные игроку.
  4. Сформировать валидный `Order` для передачи в существующий поток (без изменения `ServiceRegistration`/`GameLoop`).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - Ручной сценарий CLI: команда приказа позволяет задать все 7 слотов, выбрать ограничение из закрытого списка, добавить до 3 ветвей, задать режим и оба дедлайна; попытка добавить 4-ю ветвь отклоняется.
  - Утверждения: создаваемый приказ содержит `TargetObjectId`/`TargetUnitId`/`MovementMode`/`Constraints`/`Branches`/сроки при заполнении; UI не показывает истинное расположение/состояние противника.
- **Готово, если:**
  - [ ] Конструктор покрывает все слоты и закрытые списки §4.
  - [ ] Ветви/режим/дедлайны/срок донесения поддержаны с предпросмотром.
  - [ ] Истина не раскрывается в интерфейсе приказа.

### T-21b — Query CLI
- **ID:** `T-21b`
- **Волна:** Wave 3
- **Покрывает:** §3, §5, §13 (запросы, приоритет, молчание 30–60/60 мин семантика событий).
- **Владеет файлами:**
  - `Kriegsspiel.Console/Commands/QueryCommand.cs` (новый)
  - Не трогать другие файлы.
- **Зависимости:** `T-20`.
- **Что сделать:**
  1. Реализовать `QueryCommand` с интерфейсом `ICommand` (сигнатура по существующему контракту): выбор адресата/типа запроса, формирование запроса как сообщения, занимающего курьера.
  2. Отображать/инициировать поток событий запроса: `QuerySent`, ожидание ответа `QueryAnswered` (донесение с 4 полями) или `QuerySilence` при отсутствии ответа в окне; учитывать приоритет запросов и ETA через доступные сервисы/состояние.
  3. Не раскрывать истину; показывать только отправку/статусы/полученные донесения из журнала/знания.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - Ручной сценарий CLI: создание запроса формирует отправку и не блокирует UI; при ответе отображается донесение с минутой события, минутой доставки, источником и уверенностью; при отсутствии ответа фиксируется/отображается `QuerySilence`.
  - Утверждения: запрос использует тот же курьерный механизм/очередь; `QuerySilence` не интерпретируется как «факта нет».
- **Готово, если:**
  - [ ] `QueryCommand` реализован как `ICommand`.
  - [ ] `QuerySent/QueryAnswered/QuerySilence` поддержаны в пользовательском потоке.
  - [ ] Истина не раскрывается.

### T-21c — Map renderer
- **ID:** `T-21c`
- **Волна:** Wave 3
- **Покрывает:** §6, §13 (карта знания, no-data vs empty, уверенность, противоречия), трасса K.
- **Владеет файлами:**
  - `Kriegsspiel.Console/Commands/MapRenderer.cs`
  - Не трогать другие файлы.
- **Зависимости:** `T-20`.
- **Что сделать:**
  1. Переписать `MapRenderer` на чтение **только** `PlayerKnowledge` (и производных данных знания), полностью убрать использование истинных полей `Zone.PresentUnitIds`/скрытого состояния юнитов.
  2. Отображать для зон: no-data vs пусто, оба timestamp (событие и доставка/обновление), уверенность 0–3, контролирующую/известную информацию только из знания.
  3. При противоречии по `fact key` показывать обе конфликтующие версии и сниженную уверенность (не последнюю истину).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - Ручной/тестовый сценарий: карта без наблюдений показывает «нет данных», после донесения о пустоте — «пусто»; при двух конфликтующих донесениях видны обе версии и confidence снижена.
  - Утверждения: в коде/выводе нет чтения `PresentUnitIds`; отображаются оба времени и уровень уверенности 0–3.
- **Готово, если:**
  - [ ] Рендер карты использует только знание игрока.
  - [ ] No-data/empty, timestamps, confidence и конфликты отображаются корректно.
  - [ ] Утечка истины через карту устранена.

### T-21d — Journal renderer
- **ID:** `T-21d`
- **Волна:** Wave 3
- **Покрывает:** §3, §5, §13 (журнал, ETA курьеров, семантика потерь/молчания), трасса D.
- **Владеет файлами:**
  - `Kriegsspiel.Console/Commands/JournalRenderer.cs`
  - Не трогать другие файлы.
- **Зависимости:** `T-20`.
- **Что сделать:**
  1. Переработать `JournalRenderer` для отображения семантических минут: отдача, отправка/доставка/возврат курьера (ETA и факт), получение донесений, статусы приказов/запросов.
  2. Показывать видимую очередь курьеров и ETA; `QuerySilence` отображать явно; потерю приказа не показывать как доставку.
  3. Отражать различие «курьер не вернулся»/`deliveryConfirmed=false` от факта потери приказа (без утверждения о недоставке при потере обратного плеча).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - Ручной сценарий CLI: журнал показывает ETA и фактические минуты, очередь и статусы курьеров; при `QuerySilence` есть запись; при потере на обратном пути нет записи «приказ потерян/не доставлен».
  - Утверждения: потерянные приказы не отображаются как `OrderDelivered`; события/записи сортированы по времени/семантике.
- **Готово, если:**
  - [ ] Журнал отражает ETA, очередь и семантические минуты.
  - [ ] `QuerySilence` видим, семантика потерь корректна.

### T-21e — Situation and report renderer
- **ID:** `T-21e`
- **Волна:** Wave 3
- **Покрывает:** §5, §6, §13 (обстановка, донесения, возраст/источник/уверенность, противоречия), трасса K/D.
- **Владеет файлами:**
  - `Kriegsspiel.Console/Commands/SituationCommand.cs`
  - `Kriegsspiel.Console/Commands/ReportRenderer.cs`
  - Не трогать другие файлы.
- **Зависимости:** `T-20`.
- **Что сделать:**
  1. Обновить `ReportRenderer` для вывода 4 обязательных полей донесения (минута события, минута доставки, источник, уверенность), типа/текста/фактов и возраста; использовать `IReportFactory`/нарратор/шаблоны через доступные контракты (без правки DI/файлов вне ownership).
  2. Обновить `SituationCommand` для показа обстановки на основе доставленных донесений и `PlayerKnowledge`: противоречивые версии, no-data vs empty где применимо, без раскрытия истины.
  3. Поддержать отображение `ReportKind.Impossible` и запросных ответов/молчания в пользовательском представлении (в рамках этих файлов).
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - Ручной сценарий CLI: ОБСТАНОВКА показывает донесения с источником, обеими минутами, уверенностью и возрастом; конфликтные донесения отображаются как противоречие, а не как единственная «истина».
  - Утверждения: нет прямого обращения к скрытому истинному состоянию; `Impossible` и ответы на запросы визуально различимы.
- **Готово, если:**
  - [ ] Рендер донесений показывает все обязательные поля и возраст.
  - [ ] ОБСТАНОВКА опирается на знание/доставленные донесения и отражает противоречия.

### T-22 — Shell/DI/integration
- **ID:** `T-22`
- **Волна:** Wave 3
- **Покрывает:** §3, §12, §13 (меню/цикл, DI, конфигурация, интеграция AI/пersistence/команд).
- **Владеет файлами:**
  - `Kriegsspiel.Console/Startup/ServiceRegistration.cs`
  - `Kriegsspiel.Console/Startup/GameLoop.cs`
  - `Kriegsspiel.Console/Program.cs`
  - `Kriegsspiel.Console/appsettings.json`
  - `Kriegsspiel.Console/Kriegsspiel.Console.csproj`
  - `Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj` (только при необходимости добавить package reference/настройки для новых тестов)
  - `Kriegsspiel.slnx`
  - Не трогать другие файлы (включая `Kriegsspiel.Core/Kriegsspiel.Core.csproj`, файлы команд/рендереров, кроме регистрации/вызова из перечисленных).
- **Зависимости:** `T-20`, `T-21a`–`T-21e`, `T-16`, `T-17`, `T-18`.
- **Что сделать:**
  1. Обновить DI в `ServiceRegistration.cs`: зарегистрировать новые сервисы/резолверы/контракты, persistence, AI (включая `Kriegsspiel.AI` при наличии), narrator/factory/knowledge/query компоненты; читать `UseLlm`/`Llm` настройки из конфигурации.
  2. Подключить команды в меню/цикле (`GameLoop`, `Program`): `ОБСТАНОВКА`, `КАРТА`, `ПРИКАЗ`, `ЗАПРОС`, `ЖУРНАЛ`, `ВРЕМЯ`, `СОХРАНИТЬ`, `ЗАГРУЗИТЬ`, `ВЫЙТИ`; использовать `AddCommandLine`/конфигурационный пайплайн как требуется текущей архитектуре.
  3. Обновить `appsettings.json` секциями `UseLlm`/`Llm` и актуальными путями контента/сценария (без изменения JSON-контента сценариев/таблиц, которыми владеет T-03).
   4. Обновить `Kriegsspiel.Console.csproj` и `Kriegsspiel.slnx`: добавить ссылку/проект `Kriegsspiel.AI`, исправить `Solution Items` только в пределах `slnx`; при необходимости добавить package reference в `Kriegsspiel.Core.Tests.csproj`; не редактировать `Kriegsspiel.Core.csproj`.
  5. Обеспечить сборку и запуск консольного shell с новыми командами и конфигурацией LLM on/off.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx` (или эквивалентный набор test-проектов после интеграции)
  - Ручной запуск: меню содержит все 9 действий; `ЗАГРУЗИТЬ/СОХРАНИТЬ` вызывают новые команды/store; переключение `UseLlm` не ломает запуск (fallback/отключение работает).
  - Утверждения: DI резолвит `Arbiter`, команды, persistence, narrator/factory; `slnx` ссылается на существующие проекты и актуальные solution items.
- **Готово, если:**
  - [ ] Все новые сервисы и команды подключены в shell/DI.
  - [ ] Конфигурация LLM и проекты/решение обновлены.
  - [ ] Приложение собирается и предоставляет полный набор действий меню.

### T-23 — Final cleanup/docs
- **ID:** `T-23`
- **Волна:** Wave 3 (финальная, после `T-21a`–`T-21e`, `T-22` и всех предыдущих волн)
- **Покрывает:** §12, поддерживаемость репозитория, приведение структуры/имён к актуальному состоянию.
- **Владеет файлами:**
  - `readme.md`
  - `.gitignore`
  - `Kriegsspiel.Core/Kriegsspiel.Core.csproj`
  - Ренеймы/перемещения (владеет исходными и целевыми путями операции):
    - `Kriegsspiel.Core/Arbitration/ITransmitionResolvers.cs` → `Kriegsspiel.Core/Arbitration/ITransmissionResolver.cs`
    - `Kriegsspiel.Core/Arbitration/TransmitionResolver.cs` → `Kriegsspiel.Core/Arbitration/TransmissionResolver.cs`
    - папка `Kriegsspiel.Core/Reports/` → `Kriegsspiel.Core/Reporting/` (все файлы внутри)
    - папка/элемент `Assesment` → `Assessment` в структуре проекта (включая удаление/замену устаревшей записи в `Kriegsspiel.Core.csproj`)
  - Не трогать другие файлы (`AGENTS.md` не менять).
- **Зависимости:** все задачи `T-01`–`T-22`.
- **Что сделать:**
  1. Выполнить финальные ренеймы: `Transmition*` → `Transmission*`, `Reports` → `Reporting`, `Assesment` → `Assessment`; обновить namespace/using/ссылки в затронутых владельцем путях так, чтобы сборка оставалась консистентной после завершения всех волн.
  2. Очистить `Kriegsspiel.Core.csproj` от устаревших пустых/неверных `Folder Include` (`Assesment\`, `Ai\` при необходимости) в соответствии с фактической структурой после интеграции.
  3. Обновить `readme.md` под реальную структуру решения/проектов, команд запуска, тестов, контента и конфигурации; исправить описание `Solution Items`/структуры, если оно расходится с `Kriegsspiel.slnx`.
  4. Обновить `.gitignore` при необходимости для артефактов сборки/сохранений/локальных файлов, не нарушая отслеживаемый контент.
  5. Не изменять `AGENTS.md` и правила.
- **Как проверить:**
  - `dotnet build Kriegsspiel.slnx`
  - `dotnet test Kriegsspiel.slnx`
  - Утверждения: в репозитории нет файлов/папок со старыми именами `Transmition*`, `Kriegsspiel.Core/Reports`, `Assesment`; `Kriegsspiel.Core.csproj` не содержит устаревших пустых записей; `readme.md` соответствует фактическим проектам (`Kriegsspiel.Core`, `Kriegsspiel.Console`, `Kriegsspiel.Core.Tests`, `Kriegsspiel.AI` если присутствует), командам и структуре.
- **Готово, если:**
  - [ ] Ренеймы и чистка проекта выполнены без нарушения сборки.
  - [ ] Документация и `.gitignore` актуализированы.
  - [ ] `AGENTS.md` и правила не изменены.

## Правила интеграции и разрешения конфликтов

1. **После каждой волны:** выполнить `dotnet build Kriegsspiel.slnx` и `dotnet test Kriegsspiel.slnx` (или канонический набор test-проектов волны). Волна считается интегрированной только при успешной сборке и тестах.
2. **Конфликты файлов:** владелец файла из таблицы задачи имеет приоритет при разрешении конфликта. Изменения вне ownership запрещены; при необходимости — новая зависимость/задача в следующей волне.
3. **Порядок рискованных изменений:** интеграция конвейера — только `T-20`; интеграция shell/DI/solution — только `T-22`; массовые ренеймы, удаление устаревших записей csproj и обновление `readme.md` — только финальная `T-23`.
4. **Пересечения, сознательно разведённые по волнам:**
   - Перенос `JsonSaveStore` из `Console/Commands` в `Console/Persistence` выполняет `T-16`; обновление регистраций/использования — `T-22`.
   - `Kriegsspiel.slnx`, `Kriegsspiel.Console.csproj` и `Kriegsspiel.Core.Tests.csproj` меняет только `T-22`; `Kriegsspiel.Core.csproj` — только `T-23`.
   - Ренеймы файлов/папок, созданных или активно меняемых в Wave 0–2 (`Transmition*`, `Reports`/reporting, assessment), выполняются только `T-23`.
5. **Проверка готовности:** задача закрывается только при выполнении всех пунктов `Готово, если` и прохождении её проверок; интеграционный релиз плана — после успешной финальной проверки `T-23`.
