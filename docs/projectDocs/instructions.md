# Общие подходы к кодингу в проекте

Документ описывает фактические конвенции рабочего дерева. Источник истины — `.csproj`, `.slnx` и код, а не `readme.md` (последний описывает желаемую, но не существующую структуру).

## 1. Структура и зависимости

- Решение `Kriegsspiel.slnx` содержит три проекта: `Kriegsspiel.Core`, `Kriegsspiel.Console`, `Kriegsspiel.Core.Tests`. Именование единое: `Kriegsspiel.*` и в путях, и в namespace.
- `Kriegsspiel.Core` не имеет зависимостей; `Console` и `Tests` ссылаются только на Core. Не добавлять ссылки из Core в UI/тесты/внешние проекты.
- Контент лежит в `/Content/` (`scenarios/*.json`, `friction.json`, `reports.json`) и копируется в вывод Console через `Kriegsspiel.Console.csproj:21-23`.
- `readme.md:49-90` устарел: `Kriegsspiel.AI`, `Assessment/`, `Ai/`, `QueryCommand`, `TimeCommand`, `ReportFactory`, `reports.ru.json` и часть тестов отсутствуют.
- `Kriegsspiel.slnx:2-4` ссылается на несуществующий корневой `appsettings.json`; реальный файл — `Kriegsspiel.Console/appsettings.json`.

## 2. Сборка и проверки

- Общие настройки в `Directory.Build.props:3-7`: `net10.0`, `Nullable=enable`, `ImplicitUsings=enable`, `LangVersion=latest`, `TreatWarningsAsErrors=true`.
- Console — `OutputType=Exe`; Tests — `IsPackable=false`, xUnit 2.9.3, `Microsoft.NET.Test.Sdk 17.14.1`, coverlet, global using `Xunit` (`Kriegsspiel.Core.Tests.csproj:11-23`).
- Отдельного lint/typecheck/CI/`.editorconfig`/`global.json` нет; роль typecheck и warning-gate выполняет сборка.
- Команды: `dotnet build Kriegsspiel.slnx`, `dotnet test Kriegsspiel.Core.Tests/Kriegsspiel.Core.Tests.csproj`, `dotnet run --project Kriegsspiel.Console/Kriegsspiel.Console.csproj`. В текущем окружении `dotnet` может отсутствовать.
- Коммитить только по явной просьбе; перед коммитом проверять `git status`, `git diff`, `git log --oneline -10`.
- Технический долг вне текущей задачи фиксируется в `docs/projectDocs/tech-debt.md`; при декомпозиции его учитывают (см. `AGENTS.md`, шаблон декомпозиции).

## 3. Именование

- `file-scoped namespace` во всех файлах с типами; `Program.cs` — top-level без namespace.
- Шаблоны namespace: `Kriegsspiel.Core.<Domain|Arbitration|Orders|Content|Reporting>`, `Kriegsspiel.Console.<Startup|Commands|Rendering|Persistence>`, `Kriegsspiel.Core.Tests.Fixtures`.
- Типы — `PascalCase`; интерфейсы — `I` + `PascalCase`; приватные поля — `_camelCase`; статические readonly — `PascalCase` (`Opt`); локальные переменные и параметры — `camelCase`.
- Тесты: класс `<Subject>Tests`, метод `[Fact] Method_Scenario` (например, `AdvanceTo_YieldsInOrder`), стиль Arrange-Act-Assert, детерминированный `new Random(seed)`.
- Известные расхождения, не воспроизводить: `ITransmitionResolvers.cs`, `TransmitionResolver.cs`, `TransmitionResolverTests.cs` при правильных именах типов; `Assesment\` вместо `Assessment`; `Domain/Order.cs` объявляет `Kriegsspiel.Core.Orders`; рендеры и `JsonSaveStore.cs` физически лежат в `Commands/`, но имеют namespace `Rendering`/`Persistence`.

## 4. Моделирование предметной области

- Сервисы и изменяемое состояние — `public sealed class` (`Clock`, `GameState`, `Unit`, `Zone`, `Order`, `Arbiter`, resolver'ы, renderer'ы, commands).
- Неизменяемые значения — `record` (`CommanderTraits`, `TurnResult`, `TransmissionResult`); события — `abstract record GameEvent` + `sealed record`-наследники; закрытые множества — `enum` (`OrderIntent`, `UnitType`, `Posture`, `TerrainType`, `ReportKind`, `ObjectiveType`, `Misinterpretation`).
- Helpers — `static class` (`AmbiguityScorer`, `GameStateFactory`, `ServiceRegistration`, `TestContent`).
- Обязательные данные — `required ... { get; init; }`; меняющееся состояние симуляции — `{ get; set; }`. Примеры: `GameState.cs:7-15`, `Unit.cs:11-20`, `Order.cs:9-17`.
- Коллекции сейчас открыто предоставляются как `Dictionary<>`/`List<>` через `init`; на границах новых API предпочтительнее `IReadOnly*`.
- Время инкапсулировано в `Clock`: `PriorityQueue<GameEvent,int>`, `Now` с `private set`, `Schedule`, `AdvanceTo` (`Domain/Clock.cs:5-27`).
- Сторона сейчас определяется префиксом `ENEMY_` в ID (`CombatResolver.cs:12-13`, `MapRenderer.cs:15`, `OrderCommand.cs:15`) — для нового кода использовать явный `Faction`.
- ID генерируются как `Guid.NewGuid().ToString("N")[..8]` (`Order.cs:9`, `Report.cs:7`).
- Числа-параметры пока часто inline; новые значения выносить в `FrictionTable` или именованные константы.

## 5. Ошибки, валидация, диагностика

- Nullable + `TreatWarningsAsErrors` + `required` — основная защита; `string?` для необязательных слотов приказа/целей; `Order? Build(...)` с проверкой результата.
- Для отсутствующих сущностей в resolver-ах использовать `TryGetValue` + early `return`/`yield break` (`CombatResolver.cs:10,14`, `ExecutionResolver.cs:11-12,20,36,50,57`).
- Нарушения инвариантов — явные исключения с контекстом (как `InvalidOperationException("scenario null")` в `ContentLoader.cs:18`); не добавлять `try/catch` «на всякий случай».
- Валидацию контента выполнять один раз в `ContentLoader`/`GameStateFactory`, не разбрасывать по resolver-ам.
- Диагностика: `GameState.EventLog` как доменный журнал, вывод через Spectre.Console, compiler warnings. Кастомных exception-типов и logging-слоя нет.
- Известные слабые места: `Clock.Schedule` молча переносит прошлое время в `Now` (`Clock.cs:12`), `ReportTemplates.Pick` возвращает `"[нет шаблона]"`.

## 6. DI и оркестрация

- Точка композиции — `ServiceRegistration.Build(string[] args)` (`Console/Startup/ServiceRegistration.cs:13-53`): `IConfiguration` → `ContentLoader` → `ScenarioBundle` → `FrictionTable` + `Random` → resolver'ы и `Arbiter` → renderer'ы, store, commands, `GameLoop`. Всё регистрируется как `Singleton`.
- `Program.cs` только получает `GameLoop` и вызывает `Run()`.
- Сохранять архитектуру «один оркестратор хода + узкие resolver-интерфейсы + event clock». Resolver'ы сейчас получают `FrictionTable` и `Random` параметрами методов, а `Arbiter` хранит их как состояние; при рефакторинге ввести единый `IGameRules`/`IRandomSource`.
- `args` принимаются, но `AddCommandLine` не вызывается, поэтому `--Scenario` из launchSettings не работает; `UseLlm`/`Llm` из appsettings нигде не читаются.
- `IReportNarrator` и часть событий (`OrderIssued`, `OrderMisinterpreted`, `CombatTick`) сейчас «мёртвые»: подключить или удалить.
- `ICommand` реализуют не все команды (`OrderCommand`, `SaveCommand` не реализуют); новые команды делать через интерфейс.

## 7. Контент и данные

- JSON: camelCase-ключи, enum'ы как PascalCase-строки (`JsonStringEnumConverter`), абсолютные минуты, граф через `travelMinutes`, ID зон/юнитов заглавными, враг по префиксу `ENEMY_`.
- `ContentLoader.cs:8-14`: `PropertyNameCaseInsensitive=true`, `JsonStringEnumConverter`, `ReadCommentHandling.Skip`, `AllowTrailingCommas=true`.
- Шаблоны донесений сгруппированы по `ReportKind.ToString()`, плейсхолдеры `{unit}`, `{zone}`, `{strength}`; тексты пользовательские — русские.
- `GameStateFactory.FromBundle` (`ScenarioBundle.cs:12-31`) строит словари по `Id`; дубли ID приведут к исключению — добавлять явную проверку.
- `JsonSaveStore.cs:9-15` пишет `GameState` как есть в `saves/{yyyyMMdd_HHmmss}.json`; загрузки нет. Новые форматы сохранения требуют DTO, версии схемы и согласованных JsonSerializerOptions.

## 8. Стиль кода

- 4 пробела, K&R-скобки, expression-bodied и primary constructors для тривиальных членов.
- Using'и расположены неодинаково; для новых файлов — до `namespace` в одном стиле. Неиспользуемые using'и удалять (`Scenario.cs:4`).
- Комментарии в `.cs` не добавлять без явной просьбы; намерения выражать именами, типами и тестами.
- Язык документации и игровых текстов — русский; внутренние рассуждения — английские (согласно `AGENTS.md:9`, отвечать на русском).
- При чтении файлов использовать сабагентов для экономии контекста (`AGENTS.md:7`).

## 9. Приоритетные технические долги

Живой список находок ведётся в `docs/projectDocs/tech-debt.md`. Ниже краткий исторический срез (может частично пересекаться с `tech-debt.md` — при расхождениях ориентироваться на `tech-debt.md`):

1. Обновить `readme.md` и `Solution Items`, устранить опечатки `Transmition`/`Assesment` и разнести папки/namespace.
2. Подключить `AddCommandLine` или убрать `--Scenario`/`args`.
3. Решить судьбу `IReportNarrator` и шаблонов донесений.
4. Хранить донесения «в пути» вместо потери при `ArrivesAt > target`.
5. Заменить `ENEMY_` на тип стороны, вынести magic numbers в контент/константы.
6. Добавить `saves/` и исправить опечатки в `.gitignore` (`appsettings..json`, `!.vscodesettings.json`).
