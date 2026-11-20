# Структура проекта

Актуальное состояние рабочего дерева. Формат дерева — по образцу `readme.md`, но сами пути и имена ниже соответствуют тому, что реально лежит в репозитории, а не плану в `readme.md`. Правила игры находятся в `docs/gameDocs/rules.md`; корневого `rules.md` больше нет.

```text
Kriegsspiel/                                      # корень репозитория; проекты и namespace — Kriegsspiel.*
├── .gitignore                                    # правила игнорирования Git (есть неточности в шаблонах)
├── AGENTS.md                                     # инструкции для агентов: роль, язык, чтение через сабагентов
├── Directory.Build.props                         # общие свойства сборки: net10.0, Nullable, ImplicitUsings,
│                                                 # LangVersion=latest, TreatWarningsAsErrors=true
├── Kriegsspiel.slnx                              # файл решения (.slnx); Solution Items ссылается на несуществующий
│                                                 # корневой appsettings.json
├── readme.md                                     # обзор проекта; описывает целевую, частично устаревшую структуру
├── Content/                                      # игровые данные; линкуются в вывод Kriegsspiel.Console
│   ├── friction.json                             # параметры трения/задержек/потерь/шумов
│   ├── reports.json                              # шаблоны текстов донесений по типам
│   └── scenarios/                                # сценарии
│       ├── crossing_01.json                      # сценарий «Переправа у Мельницы»
│       └── test_01.json                          # тестовый сценарий «один батальон, одна высота»
├── docs/                                         # документация проекта
│   ├── gameDocs/                                 # игровая документация
│   │   ├── featureBacklog.md                     # бэклог идей и доработок механик
│   │   └── rules.md                              # правила игры (канонический источник механик)
│   └── projectDocs/                              # проектная документация
│       ├── instructions.md                       # фактические конвенции кодинга и рабочие команды
│       ├── reports/                              # отчёты по задачам и волнам (см. reports/README.md)
│       │   ├── README.md                          # форматы отчётов по задачам и волнам
│       │   ├── task-T-01-domain-and-contracts.md  # отчёт Wave 0 / T-01
│       │   ├── task-T-02-clock-and-event-ordering.md # отчёт Wave 0 / T-02
│       │   ├── task-T-03-content-profile-and-validation.md # отчёт Wave 0 / T-03
│       │   └── wave-0-report.md                   # отчёт волны Wave 0
│       ├── structure.md                          # этот файл: актуальная структура репозитория
│       ├── tech-debt.md                          # оперативный список технического долга (TD-*)
│       ├── todo.md                               # изолированные задачи разработки по волнам
│       ├── templates/                            # шаблоны промптов и декомпозиции
│       │   └── taskDecompositionTemplate.md      # шаблон декомпозиции беклога в T-* задачи
│       └── workflows/                            # процессы и промпты оркестрации
│           ├── README.md
│           ├── task-lifecycle.md
│           └── prompts/
│               ├── implementer-take-task.md
│               ├── orchestrator-kickoff.md
│               ├── planner-decompose.md
│               ├── scenario-tester-run.md
│               ├── tech-debt-intake.md
│               └── verifier-review.md
├── Kriegsspiel.Console/                           # консольный UI и точка входа
│   ├── Kriegsspiel.Console.csproj                # проект Exe: net10.0, Spectre.Console, ссылка на Core, копирование Content
│   ├── Program.cs                                # точка входа, получение GameLoop и запуск
│   ├── appsettings.json                          # ContentRoot, Scenario, UseLlm, секция Llm
│   ├── Commands/                                 # команды UI и вспомогательные рендеры/сохранение
│   │   ├── ICommand.cs                           # интерфейс команды
│   │   ├── JournalCommand.cs                     # команда «ЖУРНАЛ»
│   │   ├── JournalRenderer.cs                    # рендер журнала (файл в Commands, namespace ...Rendering)
│   │   ├── JsonSaveStore.cs                      # сохранение GameState в saves/*.json (namespace ...Persistence)
│   │   ├── MapCommand.cs                         # команда «КАРТА»
│   │   ├── MapRenderer.cs                        # рендер карты по знанию игрока (namespace ...Rendering)
│   │   ├── OrderCommand.cs                       # интерактивный ввод приказа
│   │   ├── ReportRenderer.cs                     # рендер донесений (namespace ...Rendering)
│   │   ├── SaveCommand.cs                        # команда сохранения
│   │   └── SituationCommand.cs                   # команда «ОБСТАНОВКА»
│   ├── Properties/
│   │   └── launchSettings.json                   # профиль запуска с аргументом --Scenario test_01
│   └── Startup/
│       ├── GameLoop.cs                           # игровой цикл и меню
│       └── ServiceRegistration.cs                # ручная композиция сервисов и загрузка контента
├── Kriegsspiel.Core/                              # ядро симуляции и доменная логика
│   ├── Kriegsspiel.Core.csproj                    # проект библиотеки; содержит пустые Folder Include: Assesment, Ai
│   ├── Ai/                                       # пустая папка-заготовка под LLM-адаптеры
│   ├── Arbitration/                              # оркестрация хода и резолверы подсистем
│   │   ├── Arbiter.cs                            # продвижение времени, планирование и диспетчеризация событий
│   │   ├── CombatResolver.cs                     # разрешение боя
│   │   ├── ExecutionResolver.cs                  # исполнение приказов, движение, прибытие, отчёты
│   │   ├── ICombatResolver.cs                    # интерфейс боевого резолвера
│   │   ├── IExecutionResolver.cs                 # интерфейс резолвера исполнения
│   │   ├── ITransmitionResolvers.cs              # интерфейс передачи (опечатка в имени файла)
│   │   ├── IUnderstandingResolver.cs             # интерфейс резолвера понимания приказа
│   │   ├── TransmitionResolver.cs                # резолвер передачи/задержки/потери (опечатка в имени файла)
│   │   ├── TurnResult.cs                         # записи TurnResult и TransmissionResult
│   │   └── UnderstandingResolver.cs              # резолвер понимания и искажения приказа
│   ├── Assesment/                                # пустая папка-заготовка (опечатка; ожидаемо Assessment)
│   ├── Content/                                  # загрузка и модели игрового контента
│   │   ├── ContentLoader.cs                      # загрузка scenario/friction/reports из JSON
│   │   ├── FrictionTable.cs                      # таблица параметров трения
│   │   ├── Objective.cs                           # enum ObjectiveType и модель цели
│   │   ├── ReportTemplates.cs                    # шаблоны и подстановка текста донесений
│   │   ├── Scenario.cs                           # модель сценария
│   │   └── ScenarioBundle.cs                     # пакет контента и GameStateFactory
│   ├── Contracts/                                # контракты ядра для Wave 1
│   │   ├── IGameRules.cs                         # параметры/проверки правил, CombatEffective
│   │   ├── IKnowledgeUpdater.cs                  # обновление знания игрока
│   │   ├── IRandomSource.cs                      # детерминированный RNG
│   │   └── IReportFactory.cs                     # создание донесений
│   ├── Domain/                                   # доменные сущности и состояние игры
│   │   ├── Cargo.cs                              # enum CargoType и модель груза
│   │   ├── CivilianGroup.cs                      # модель группы гражданских
│   │   ├── Clock.cs                              # игровое время, фазы и очередь событий (PriorityQueue)
│   │   ├── CommanderTraits.cs                    # record черт командира (5 параметров 0–3)
│   │   ├── ContactGeometry.cs                    # enum ContactClass и геометрия контакта
│   │   ├── Courier.cs                            # enum CourierState и модель курьера
│   │   ├── EffectiveOrder.cs                     # эффективный приказ (версия/ветви/ReportBy)
│   │   ├── GameEvent.cs                          # базовый GameEvent и конкретные события
│   │   ├── GameState.cs                          # InTransitReports, курьеры, Seed/RNG, PendingCasualties
│   │   ├── MovableObject.cs                      # enum MovableObjectState/PrisonerStatus и базовый тип
│   │   ├── Order.cs                              # 7 слотов, версия, ветви (namespace Kriegsspiel.Core.Orders)
│   │   ├── PendingCasualty.cs                    # отложенные потери (minute, unitId, amount)
│   │   ├── PlayerKnowledge.cs                    # знание игрока о зонах (ZoneKnowledge, PlayerKnowledge)
│   │   ├── Report.cs                             # enum ReportKind и донесение (4 обязательных поля)
│   │   ├── Unit.cs                               # enums Faction/UnitType/Posture, IsCombatEffective
│   │   ├── Zone.cs                               # enums TerrainType/EdgeType и модель зоны
│   │   └── ZoneObject.cs                         # enum ZoneObjectKind и объекты зоны
│   ├── Orders/                                   # оценка и исходы понимания приказов
│   │   ├── AmbiguityScorer.cs                    # расчёт неоднозначности приказа
│   │   └── Misinterpretation.cs                  # enum типов искажения/неверного понимания
│   └── Reports/                                  # формирование текста донесений
│       └── IReportNarrator.cs                    # интерфейс, ReportContext и TemplateReportNarrator
│                                                 # (namespace Kriegsspiel.Core.Reporting)
└── Kriegsspiel.Core.Tests/                        # модульные тесты ядра
    ├── Kriegsspiel.Core.Tests.csproj              # xUnit, net10.0, ссылка только на Core
    ├── AmbiguityScorerTests.cs                   # тесты AmbiguityScorer
    ├── ClockTests.cs                             # тесты Clock
    ├── ContentLoaderTests.cs                     # тесты валидации загрузки контента
    ├── DomainInvariantsTests.cs                  # тесты инвариантов домена
    ├── TransmitionResolverTests.cs               # тесты TransmissionResolver (опечатка в имени файла)
    └── Fixtures/
        └── TestContent.cs                        # фабрика тестового контента/сценария
```


## Оговорки

- Именование единое: решение `Kriegsspiel.slnx`, каталоги, csproj и namespace — `Kriegsspiel.*`. Старые варианты написания в репозитории устранены.
- Опечатки в именах файлов/папок: `Transmition*`, `Assesment`, `TransmitionResolverTests.cs`. Исправление запланировано финальной задачей `T-23` в `todo.md`.
- Расположение файлов не всегда совпадает с namespace: `Domain/Order.cs` → `Kriegsspiel.Core.Orders`; `Reports/IReportNarrator.cs` → `Kriegsspiel.Core.Reporting`; рендеры и `JsonSaveStore` лежат в `Commands/`, но объявляют `Kriegsspiel.Console.Rendering` / `...Persistence`.
- `appsettings.json` в `Kriegsspiel.slnx` указан неверно; `launchSettings.json` передаёт `--Scenario test_01`, но командная строка в `ServiceRegistration` не подключена.
- Домен (Wave 0) вводит `Faction` как источник принадлежности, но резолверы (`CombatResolver`) всё ещё определяют противника префиксом `ENEMY_` в ID; переход на `Faction` — работа Wave 1 (см. `tech-debt.md`, TD-002).
- Каталоги `.git`, `.vs`, `bin`, `obj`, `saves` в дерево не включены как служебные/генерируемые; `saves/` создаётся только при сохранении.
- `readme.md` стоит считать целевым описанием, а этот файл — фактическим снимком структуры. Изменения инфраструктуры из `todo.md`, новые файлы в `docs/projectDocs/templates/`, `docs/projectDocs/workflows/`, `docs/projectDocs/tech-debt.md`, `docs/projectDocs/reports/` должны отражаться здесь при их появлении/изменении.
