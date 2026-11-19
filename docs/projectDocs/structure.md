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
│       ├── structure.md                          # этот файл: актуальная структура репозитория
│       └── todo.md                               # изолированные задачи разработки по волнам
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
│   ├── Domain/                                   # доменные сущности и состояние игры
│   │   ├── Clock.cs                              # игровое время и очередь событий (PriorityQueue)
│   │   ├── CommanderTraits.cs                    # record черт командира
│   │   ├── GameEvent.cs                          # базовый GameEvent и конкретные события
│   │   ├── GameState.cs                          # агрегированное состояние игры
│   │   ├── Order.cs                              # enum OrderIntent и модель Order (namespace Kriegsspiel.Core.Orders)
│   │   ├── PlayerKnowledge.cs                    # знание игрока о зонах (ZoneKnowledge, PlayerKnowledge)
│   │   ├── Report.cs                             # enum ReportKind и модель донесения
│   │   ├── Unit.cs                               # enums UnitType/Posture и модель подразделения
│   │   └── Zone.cs                               # enum TerrainType и модель зоны
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
    ├── TransmitionResolverTests.cs               # тесты TransmissionResolver (опечатка в имени файла)
    └── Fixtures/
        └── TestContent.cs                        # фабрика тестового контента/сценария
```

## Отличия `readme.md` от фактического состояния

| `readme.md` | Факт |
|---|---|
| Проекты `Kriegsspiel.*` | Совпадает: каталоги, csproj, namespace и solution-ссылки используют единое написание `Kriegsspiel.*` |
| `Kriegsspiel.sln` | Фактически `Kriegsspiel.slnx` |
| `README.md` | Фактически `readme.md` |
| Корневой `rules.md` | Правила находятся в `docs/gameDocs/rules.md`; корневого файла нет |
| `Orders/OrderIntent.cs` | Отдельного файла нет; `OrderIntent` живёт в `Domain/Order.cs` |
| `Reporting/TemplateReportNarrator.cs`, `Reporting/ReportFactory.cs` | Папки/файлов нет; всё в `Reports/IReportNarrator.cs` |
| `Assessment/AfterActionReport.cs`, `Assessment/MetricCalculator.cs` | Отсутствуют; есть пустая `Assesment/` (опечатка) |
| `Ai/IOrderCompiler.cs`, `Ai/IAmbiguityInterpreter.cs` | Отсутствуют; папка `Ai/` пуста |
| Проект `Kriegsspiel.AI/` | Отсутствует |
| `Console/Commands/QueryCommand.cs`, `TimeCommand.cs` | Отсутствуют |
| `Console/Rendering/*`, `Console/Persistence/JsonSaveStore.cs` | Отдельных папок нет; файлы физически в `Commands/`, но объявляют другие namespace |
| `Content/reports.ru.json` | Отсутствует |
| `Core.Tests/UnderstandingResolverTests.cs`, `CombatResolverTests.cs` | Отсутствуют |
| `Solution Items` → корневой `appsettings.json` | Реальный конфиг — `Kriegsspiel.Console/appsettings.json` |

## Оговорки

- Именование единое: решение `Kriegsspiel.slnx`, каталоги, csproj и namespace — `Kriegsspiel.*`. Старые варианты написания в репозитории устранены.
- Опечатки в именах файлов/папок: `Transmition*`, `Assesment`, `TransmitionResolverTests.cs`. Исправление запланировано финальной задачей `T-23` в `todo.md`.
- Расположение файлов не всегда совпадает с namespace: `Domain/Order.cs` → `Kriegsspiel.Core.Orders`; `Reports/IReportNarrator.cs` → `Kriegsspiel.Core.Reporting`; рендеры и `JsonSaveStore` лежат в `Commands/`, но объявляют `Kriegsspiel.Console.Rendering` / `...Persistence`.
- `appsettings.json` в `Kriegsspiel.slnx` указан неверно; `launchSettings.json` передаёт `--Scenario test_01`, но командная строка в `ServiceRegistration` не подключена.
- Принадлежность юнита к противнику в текущем коде определяется префиксом `ENEMY_` в ID, а не отдельным полем фракции.
- Каталоги `.git`, `.vs`, `bin`, `obj`, `saves` в дерево не включены как служебные/генерируемые; `saves/` создаётся только при сохранении.
- `readme.md` стоит считать целевым описанием, а этот файл — фактическим снимком структуры. Изменения инфраструктуры из `todo.md` должны отражаться здесь по мере выполнения задач.
