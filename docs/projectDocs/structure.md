# Структура проекта

Актуальное состояние рабочего дерева: пути и краткое назначение файлов/каталогов. Правила игры — в `docs/gameDocs/rules.md`.

```text
Kriegsspiel/
├── .gitignore                                  # Правила игнорирования служебных и генерируемых путей.
├── AGENTS.md                                   # Глобальные правила работы агентов и указатели на харнес.
├── opencode.json                               # Конфиг opencode ($schema, instructions, default_agent=orchestrator).
├── .opencode/                                  # Стандартные артефакты opencode: роли, скиллы, команды.
│   ├── agent/                                  # Субагенты и primary-роль orchestrator.
│   │   ├── orchestrator.md                     # Primary-агент: дирижирование циклом по статусам/refs.
│   │   ├── analyst.md                          # Роль analyst: FS-*, разрешение неоднозначностей.
│   │   ├── architect.md                        # Роль architect: декомпозиция, todo.md, сценарии, triage.
│   │   ├── implementer.md                      # Роль implementer: одна T-*, проверки, handoff.
│   │   ├── verifier.md                         # Роль verifier: gate, per-task/wave, merge, отчёты.
│   │   ├── scenario-tester.md                  # Роль scenario-tester: воспроизводимые прогоны.
│   │   ├── process-master.md                   # Роль process-master: ретро волны и PI-*.
│   │   ├── rules-auditor.md                    # Роль rules-auditor: аудит правил и трактовок.
│   │   ├── docs-maintainer.md                  # Роль docs-maintainer: актуализация structure.md.
│   │   └── repo-navigator.md                   # Роль repo-navigator: read-only поиск контекста.
│   ├── command/                                # Лончеры ролей/процедур (замена прежних prompts/).
│   │   ├── orchestrator-kickoff.md             # Запуск/ведение цикла.
│   │   ├── analyst-spec.md                     # Лончер analyst.
│   │   ├── architect-decompose.md              # Лончер architect.
│   │   ├── implementer-take-task.md            # Лончер implementer.
│   │   ├── verifier-review.md                  # Лончер verifier.
│   │   ├── scenario-tester-run.md              # Лончер scenario-tester.
│   │   ├── process-master-retro.md             # Лончер process-master.
│   │   ├── rules-auditor-audit.md              # Лончер rules-auditor.
│   │   └── tech-debt-intake.md                 # Лончер приёма находки в техдолг.
│   └── skills/                                 # Переиспользуемые стандарты/шаблоны/чек-листы.
│       ├── process-contract/SKILL.md           # Канонические процессные инварианты и приоритет источников.
│       ├── status-envelope/SKILL.md            # Формат/статусы Status Envelope.
│       ├── file-first-handoff/SKILL.md         # Правила file-first handoff.
│       ├── report-writing/SKILL.md             # Шаблоны task/wave-отчётов.
│       ├── task-decomposition/SKILL.md         # Декомпозиция в волны/T-* и launch-план.
│       ├── feature-spec/SKILL.md               # Шаблон/статусы FS-*.
│       ├── tech-debt-intake/SKILL.md           # TD-* и разграничение с PI-*.
│       ├── process-retro/SKILL.md              # PI-* и замыкание петли мер.
│       ├── gate-checklist/SKILL.md             # Формальный gate проверяемости.
│       ├── scenario-evidence/SKILL.md          # Воспроизводимые прогоны и evidence.
│       └── code-navigation-lsp/SKILL.md        # LSP-first навигация по коду/символам; fallback для docs/config.
├── Kriegsspiel.AI/                             # Адаптеры LLM: компиляция приказов, трактовка неоднозначностей, нарратив.
│   ├── Kriegsspiel.AI.csproj                   # Проект адаптеров LLM.
│   ├── LlmAmbiguityInterpreter.cs              # LLM-трактовка неоднозначных приказов.
│   ├── LlmOrderCompiler.cs                     # Компиляция текстовых приказов через LLM.
│   ├── LlmReportNarrator.cs                    # LLM-нарратив донесений.
│   ├── StateQueryTools.cs                      # Загрузка промптов/схем и проекция состояния для LLM.
│   └── Prompts/                                # Промпты и JSON-схемы для LLM.
│       ├── AmbiguityPrompt.txt                 # Промпт трактовки неоднозначностей.
│       ├── NarrationPrompt.txt                 # Промпт нарратива донесений.
│       ├── NarrationSchema.json                # JSON-схема нарратива.
│       ├── OrderPrompt.txt                     # Промпт компиляции приказов.
│       └── OrderSchema.json                    # JSON-схема приказа.
├── Directory.Build.props                       # Общие свойства сборки для проектов решения.
├── Kriegsspiel.slnx                            # Файл решения (Core, Console, Core.Tests, AI).
├── readme.md                                   # Обзор проекта (может отставать от фактической структуры).
├── Content/                                    # Игровые данные и сценарии.
│   ├── friction.json                           # Параметры трения и связанные игровые коэффициенты.
│   ├── reports.json                            # Шаблоны текстов донесений.
│   └── scenarios/                              # Сценарии.
│       ├── crossing_01.json                    # Игровой сценарий.
│       ├── fs005_scale_demo.json                # Демо-сценарий увеличенного масштаба для проверок FS-005.
│       ├── river_line_10x10.json               # Большой игровой сценарий (поле 10×10).
│       └── test_01.json                        # Тестовый сценарий.
├── docs/                                       # Документация проекта.
│   ├── gameDocs/                               # Игровая документация.
│   │   ├── featureBacklog.md                   # Бэклог идей и предложений по механикам.
│   │   ├── featureSpecs/                       # Бизнес-спецификации фич (FS-*), готовит analyst.
│   │   │   ├── README.md                       # Правила и шаблон оформления FS-*.
│   │   │   ├── FS-001-commander-casualty-and-replacement.md # Выбывание и замена командиров.
│   │   │   ├── FS-002-squad-splitting.md       # Разделение отряда.
│   │   │   ├── FS-003-rally-routed-troops.md   # Сбор панически отступивших войск.
│   │   │   ├── FS-004-campaign-unit-carryover.md # Длинные кампании и перенос юнитов.
│   │   │   ├── FS-005-large-maps-console-display.md # Большие карты и отображение в консоли.
│   │   │   ├── FS-006-audio-accompaniment.md   # Звуковое сопровождение.
│   │   │   ├── FS-007-composite-units.md       # Составные подразделения.
│   │   │   ├── FS-008-complex-equipment-model.md # Более сложная модель снаряжения.
│   │   │   ├── FS-009-localization.md          # Локализация.
│   │   │   ├── FS-010-friction-table-comments.md # Понятные комментарии для таблицы трения.
│   │   │   └── FS-011-enemy-encirclement.md    # Окружение противника.
│   │   ├── rules.md                            # Канонические правила игры.
│   │   ├── rules_candidat.md                   # Кандидатная версия упрощённых правил (источник миграции в rules.md).
│   │   └── rules_extended.md                   # Прежняя расширенная версия правил, сохранённая как архив.
│   └── projectDocs/                            # Проектная и процессная документация.
│       ├── instructions.md                     # Фактические конвенции разработки и проверки.
│       ├── process-improvement-log.md          # Сквозной журнал процессных мер (PI-*), ведёт process-master.
│       ├── reports/                            # Канонические отчёты, file-first handoff и локальные черновики.
│       │   ├── README.md                       # Шаблоны, ownership и правила отчётов/handoff.
│       │   ├── handoffs/                       # Структурированные handoff-артефакты (file-first).
│       │   │   ├── README.md                   # Правила именования и использования handoff-файлов.
│       │   │   ├── docs-maintainer-structure-sync-r2.md # Handoff docs-maintainer: синхронизация structure.md, раунд 2.
│       │   │   ├── implementer-T-21a-r1.md     # Handoff T-21a, раунд 1.
│       │   │   ├── implementer-T-21b-r1.md     # Handoff T-21b, раунд 1.
│       │   │   ├── implementer-T-21c-r1.md     # Handoff T-21c, раунд 1.
│       │   │   ├── implementer-T-21d-r1.md     # Handoff T-21d, раунд 1.
│       │   │   ├── implementer-T-21e-r1.md     # Handoff T-21e, раунд 1.
│       │   │   ├── implementer-T-22-r1.md      # Handoff T-22, раунд 1.
│       │   │   ├── implementer-T-23-r1.md      # Handoff T-23, раунд 1.
│       │   │   ├── implementer-T-24-r1.md      # Handoff T-24, раунд 1.
│       │   │   ├── implementer-T-24-r2.md      # Handoff T-24, раунд 2.
│       │   │   └── implementer-T-25-r1.md      # Handoff T-25, раунд 1.
│       │   ├── .tmp/                           # Локальные gitignored черновики (не транспорт процесса).
│       │   │   └── README.md                   # Назначение каталога локальных черновиков.
│       │   ├── task-T-01-domain-and-contracts.md # Отчёт по задаче T-01.
│       │   ├── task-T-02-clock-and-event-ordering.md # Отчёт по задаче T-02.
│       │   ├── task-T-03-content-profile-and-validation.md # Отчёт по задаче T-03.
│       │   ├── task-T-10-couriers-and-transmission.md # Отчёт по задаче T-10.
│       │   ├── task-T-11-orders-ambiguity-understanding.md # Отчёт по задаче T-11.
│       │   ├── task-T-12-execution-and-routing.md # Отчёт по задаче T-12.
│       │   ├── task-T-13-combat-and-contact-geometry.md # Отчёт по задаче T-13.
│       │   ├── task-T-14-reports-fog-and-recon.md # Отчёт по задаче T-14.
│       │   ├── task-T-15-objectives-and-aar.md # Отчёт по задаче T-15.
│       │   ├── task-T-16-persistence-dto-and-load.md # Отчёт по задаче T-16.
│       │   ├── task-T-17-ai-llm-adapter.md    # Отчёт по задаче T-17.
│       │   ├── task-T-18-enemy-doctrine.md     # Отчёт по задаче T-18.
│       │   ├── task-T-20-arbiter-integration.md # Отчёт по задаче T-20.
│       │   ├── task-T-21a-order-constructor-cli.md # Отчёт по задаче T-21a.
│       │   ├── task-T-21b-query-cli.md         # Отчёт по задаче T-21b.
│       │   ├── task-T-21c-map-renderer.md      # Отчёт по задаче T-21c.
│       │   ├── task-T-21d-journal-renderer.md  # Отчёт по задаче T-21d.
│       │   ├── task-T-21e-situation-report-renderer.md # Отчёт по задаче T-21e.
│       │   ├── task-T-22-shell-di-integration.md # Отчёт по задаче T-22.
│       │   ├── task-T-23-final-cleanup-docs.md # Отчёт по задаче T-23.
│       │   ├── task-T-24-scalable-knowledge-map.md # Отчёт по задаче T-24.
│       │   ├── task-T-25-fs005-scale-demo.md   # Отчёт по задаче T-25.
│       │   ├── wave-0-report.md                # Отчёт по волне 0.
│       │   ├── wave-1-report.md                # Отчёт по волне 1.
│       │   ├── wave-2-report.md                # Отчёт по волне 2.
│       │   ├── wave-3-report.md                # Отчёт по волне 3.
│       │   └── wave-4-report.md                # Отчёт по волне 4.
│       ├── structure.md                        # Описание структуры репозитория и назначения файлов.
│       ├── tech-debt.md                        # Активный список технического долга.
│       ├── tech-debt-archive.md                # Архив закрытых записей технического долга.
│       ├── todo.md                             # План исполняемых задач и карточек.
│       ├── templates/                          # Шаблоны документов и декомпозиции.
│       │   └── taskDecompositionTemplate.md    # Шаблон декомпозиции работ на задачи и волны.
│       └── workflows/                          # Человекочитаемый обзор процесса (source of truth — .opencode).
│           ├── README.md                       # Обзор процесса оркестрации и правил выполнения.
│           └── task-lifecycle.md               # Жизненный цикл фичи, задачи, волны и роли.
├── Kriegsspiel.Console/                        # Консольное приложение и точка входа.
│   ├── Kriegsspiel.Console.csproj              # Проект консольного приложения.
│   ├── Program.cs                              # Точка входа приложения.
│   ├── appsettings.json                        # Конфигурация запуска консольного приложения.
│   ├── Commands/                               # Команды интерфейса, рендеринг и вспомогательные операции ввода-вывода.
│   │   ├── ICommand.cs                         # Контракт команды пользовательского интерфейса.
│   │   ├── JournalCommand.cs                   # Команда отображения журнала событий.
│   │   ├── JournalRenderer.cs                  # Формирование вывода журнала.
│   │   ├── LoadCommand.cs                      # Команда загрузки сохранённой игры.
│   │   ├── MapCommand.cs                       # Команда отображения карты.
│   │   ├── MapRenderer.cs                      # Формирование текстового представления карты.
│   │   ├── OrderBuilder.cs                     # Пошаговый конструктор приказа.
│   │   ├── OrderCommand.cs                     # Команда ввода приказа.
│   │   ├── QueryCommand.cs                     # Команда запроса оценки приказа/передачи.
│   │   ├── ReportRenderer.cs                   # Формирование вывода донесений.
│   │   ├── SaveCommand.cs                      # Команда сохранения игры.
│   │   └── SituationCommand.cs                 # Команда отображения текущей обстановки.
│   ├── Persistence/                            # Сохранение и загрузка состояния игры.
│   │   └── JsonSaveStore.cs                    # Сохранение/загрузка состояния игры в JSON-файл.
│   ├── Properties/
│   │   └── launchSettings.json                 # Профили запуска для локальной разработки.
│   └── Startup/
│       ├── GameLoop.cs                         # Основной цикл взаимодействия с пользователем.
│       └── ServiceRegistration.cs              # Композиция и регистрация зависимостей приложения.
├── Kriegsspiel.Core/                           # Ядро симуляции, доменная модель и правила.
│   ├── Kriegsspiel.Core.csproj                 # Проект библиотеки ядра.
│   ├── Ai/                                     # AI-пайплайны: промпты, схемы, worldview и fallback-и.
│   │   ├── AiPipelines.cs                      # Валидация AI-ответов и интерфейсы интерпретации.
│   │   ├── AiPromptBuilder.cs                  # Сборка промптов из игрового контекста.
│   │   ├── AiReplayLog.cs                      # Журнал AI-вызовов для воспроизводимости.
│   │   ├── AiSchemas.cs                        # Схемы AI-приказов/нарратива и проверки.
│   │   ├── AiStableId.cs                       # Стабильные идентификаторы для AI-контекста.
│   │   ├── AiWorldView.cs                      # Представление мира для AI.
│   │   ├── DeterministicRandomSource.cs        # Детерминированный источник случайности.
│   │   ├── DoctrineOrderFallback.cs            # Fallback-приказы по доктрине при сбое AI.
│   │   ├── ILLmClient.cs                       # Контракт LLM-клиента и делегирующая реализация.
│   │   └── NarrationFallback.cs                # Fallback-нарратив при сбое AI.
│   ├── Arbitration/                            # Оркестрация хода и разрешение игровых подсистем.
│   │   ├── Arbiter.cs                          # Координация игрового шага и диспетчеризация событий.
│   │   ├── CombatResolver.cs                   # Разрешение боевых столкновений.
│   │   ├── ContactGeometryResolver.cs          # Разрешение геометрии контакта сторон.
│   │   ├── CourierPool.cs                      # Пул курьеров и диспетчеризация сообщений.
│   │   ├── EnemyDoctrine.cs                    # Доктрина противника и его решения.
│   │   ├── ExecutionResolver.cs                # Исполнение приказов и связанные игровые действия.
│   │   ├── ICombatResolver.cs                  # Контракт разрешения боя.
│   │   ├── IExecutionResolver.cs               # Контракт исполнения приказов.
│   │   ├── ITransmissionResolver.cs            # Контракт передачи сообщений/приказов.
│   │   ├── IUnderstandingResolver.cs           # Контракт интерпретации и понимания приказов.
│   │   ├── KnowledgeUpdater.cs                 # Обновление знаний стороны о противнике и местности.
│   │   ├── ObjectiveEvaluator.cs               # Оценка выполнения целей сценария.
│   │   ├── PathPlanner.cs                      # Построение маршрутов движения по зонам.
│   │   ├── ReconResolver.cs                    # Разведка и обнаружение противника.
│   │   ├── TransmissionResolver.cs             # Реализация передачи и задержек сообщений.
│   │   ├── TurnResult.cs                       # Результаты шага и передачи.
│   │   └── UnderstandingResolver.cs            # Реализация понимания и искажения приказов.
│   ├── Assessment/                             # Оценка результатов боя и after-action отчёты.
│   │   ├── AfterActionReport.cs                # Формирование after-action отчёта.
│   │   └── MetricCalculator.cs                 # Расчёт метрик задержек приказов и боя.
│   ├── Content/                                # Загрузка и модели игрового контента.
│   │   ├── ContentLoader.cs                    # Загрузка сценариев, параметров и шаблонов из данных.
│   │   ├── FrictionTable.cs                    # Модель параметров трения.
│   │   ├── Objective.cs                        # Модель целей сценария.
│   │   ├── ReportTemplates.cs                  # Набор и выбор шаблонов донесений.
│   │   ├── Scenario.cs                         # Модель сценария.
│   │   └── ScenarioBundle.cs                   # Пакет загруженного контента и создание начального состояния.
│   ├── Contracts/                              # Контракты ядра для взаимодействия подсистем.
│   │   ├── IGameRules.cs                       # Контракт доступа к правилам и игровым параметрам.
│   │   ├── IKnowledgeUpdater.cs                # Контракт обновления знаний стороны.
│   │   ├── IRandomSource.cs                    # Контракт источника случайности.
│   │   └── IReportFactory.cs                   # Контракт создания донесений.
│   ├── Domain/                                 # Доменные сущности и состояние игры.
│   │   ├── Cargo.cs                            # Модель груза.
│   │   ├── CivilianGroup.cs                    # Модель группы гражданских.
│   │   ├── Clock.cs                            # Игровое время и очередь событий.
│   │   ├── CommanderTraits.cs                  # Характеристики командиров.
│   │   ├── ContactGeometry.cs                  # Модель геометрии контакта сторон.
│   │   ├── Courier.cs                          # Модель курьера и состояний доставки.
│   │   ├── EffectiveOrder.cs                   # Модель фактически исполняемого приказа.
│   │   ├── GameEvent.cs                        # Базовые и конкретные игровые события.
│   │   ├── GameState.cs                        # Состояние игры и его ключевые коллекции.
│   │   ├── MovableObject.cs                    # Модель перемещаемого объекта.
│   │   ├── Order.cs                            # Модель исходного приказа.
│   │   ├── PendingCasualty.cs                  # Модель отложенных потерь.
│   │   ├── PlayerKnowledge.cs                  # Модель знаний стороны об обстановке.
│   │   ├── Report.cs                           # Модель донесения.
│   │   ├── Unit.cs                             # Модель подразделения.
│   │   ├── Zone.cs                             # Модель зоны местности.
│   │   └── ZoneObject.cs                       # Модель объекта, расположенного в зоне.
│   ├── Orders/                                 # Логика оценки, конфликтов и вариантов понимания приказов.
│   │   ├── AmbiguityScorer.cs                  # Оценка неоднозначности приказа.
│   │   ├── ConflictResolver.cs                 # Разрешение конфликтов приказов.
│   │   ├── Misinterpretation.cs                # Типы и варианты неверного понимания приказа.
│   │   └── OrderBranch.cs                      # Ветвление приказов и правила вычисления ветвей.
│   ├── Persistence/                            # DTO, маппинг и сериализация состояния игры.
│   │   ├── ClockDto.cs                         # DTO игрового времени и очереди событий.
│   │   ├── CourierDtos.cs                      # DTO курьеров и отложенных потерь.
│   │   ├── GameEventDto.cs                     # DTO игровых событий.
│   │   ├── GameStateDto.cs                     # DTO полного состояния игры.
│   │   ├── GameStateMapper.cs                  # Маппинг состояния в DTO и обратно.
│   │   ├── GameStatePersistence.cs             # Загрузка/сохранение состояния игры.
│   │   ├── KnowledgeDtos.cs                    # DTO знаний стороны.
│   │   ├── OrderDtos.cs                        # DTO приказов и их ветвей.
│   │   ├── PersistenceJson.cs                  # Общие JsonSerializerOptions сохранений.
│   │   ├── PersistenceSchema.cs                # Версия и проверка схемы сохранения.
│   │   ├── ReportDto.cs                        # DTO донесений.
│   │   ├── UnitDtos.cs                         # DTO подразделений и командиров.
│   │   └── ZoneDtos.cs                         # DTO зон, объектов и грузов.
│   └── Reporting/                              # Формирование и представление текста донесений.
│       └── IReportNarrator.cs                  # Контракт и базовая реализация нарратива донесений.
└── Kriegsspiel.Core.Tests/                     # Модульные тесты ядра.
    ├── Kriegsspiel.Core.Tests.csproj           # Проект тестов ядра.
    ├── AIAdapterTests.cs                       # Тесты AI-адаптеров и fallback-ов.
    ├── AmbiguityScorerTests.cs                 # Тесты оценки неоднозначности приказов.
    ├── ArbiterIntegrationTests.cs              # Интеграционные тесты шага арбитра.
    ├── ClockTests.cs                           # Тесты игрового времени и очереди событий.
    ├── CombatResolverTests.cs                  # Тесты разрешения боя.
    ├── ContentLoaderTests.cs                   # Тесты загрузки и валидации контента.
    ├── DomainInvariantsTests.cs                # Тесты доменных инвариантов.
    ├── EnemyDoctrineTests.cs                   # Тесты доктрины противника.
    ├── ExecutionResolverTests.cs               # Тесты исполнения приказов.
    ├── FogOfWarTests.cs                        # Тесты тумана войны и знаний стороны.
    ├── Fs005ScaleContentTests.cs               # Тесты масштабированного контента FS-005.
    ├── ObjectiveEvaluatorTests.cs              # Тесты оценки целей.
    ├── PersistenceTests.cs                     # Тесты сохранения/загрузки состояния.
    ├── ReportTests.cs                          # Тесты формирования донесений.
    ├── TransmissionResolverTests.cs            # Тесты передачи и доставки сообщений.
    ├── UnderstandingResolverTests.cs           # Тесты понимания и искажения приказов.
    └── Fixtures/
        └── TestContent.cs                      # Вспомогательные тестовые данные и сценарии.
```

## Примечания
- В описаниях указано только краткое назначение файлов и каталогов.
- Служебные и генерируемые каталоги (`.git`, `.vs`, `bin`, `obj`, `saves`) в дерево не включены.
- При добавлении, удалении или существенном изменении назначения файлов структуру необходимо актуализировать.
