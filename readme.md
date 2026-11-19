Kriegsspiel/
├── Kriegsspiel.sln
├── Directory.Build.props              # общие настройки: LangVersion, Nullable, TreatWarningsAsErrors
├── README.md
│
├── Kriegsspiel.Core/
│   ├── Kriegsspiel.Core.csproj
│   ├── Domain/
│   │   ├── Clock.cs
│   │   ├── GameEvent.cs               # abstract record + наследники
│   │   ├── GameState.cs
│   │   ├── Zone.cs
│   │   ├── Unit.cs
│   │   ├── CommanderTraits.cs
│   │   ├── Order.cs
│   │   ├── Report.cs
│   │   └── PlayerKnowledge.cs
│   ├── Arbitration/
│   │   ├── Arbiter.cs                 # главный оркестратор хода
│   │   ├── TurnResult.cs
│   │   ├── ITransmissionResolver.cs
│   │   ├── IUnderstandingResolver.cs
│   │   ├── IExecutionResolver.cs
│   │   ├── ICombatResolver.cs
│   │   ├── TransmissionResolver.cs
│   │   ├── UnderstandingResolver.cs
│   │   ├── ExecutionResolver.cs
│   │   └── CombatResolver.cs
│   ├── Orders/
│   │   ├── OrderIntent.cs             # enum
│   │   ├── AmbiguityScorer.cs
│   │   └── Misinterpretation.cs
│   ├── Reporting/
│   │   ├── IReportNarrator.cs
│   │   ├── TemplateReportNarrator.cs
│   │   └── ReportFactory.cs
│   ├── Content/
│   │   ├── Scenario.cs
│   │   ├── FrictionTable.cs
│   │   ├── ContentLoader.cs
│   │   └── Objective.cs
│   ├── Assessment/
│   │   ├── AfterActionReport.cs
│   │   └── MetricCalculator.cs
│   └── Ai/
│       ├── IOrderCompiler.cs
│       └── IAmbiguityInterpreter.cs
│
├── Kriegsspiel.AI/
│   ├── Kriegsspiel.AI.csproj
│   ├── LlmReportNarrator.cs
│   ├── LlmOrderCompiler.cs
│   ├── LlmAmbiguityInterpreter.cs
│   ├── StateQueryTools.cs
│   └── Prompts/
│       ├── OrderCompilerPrompt.cs
│       └── NarratorPrompt.cs
│
├── Kriegsspiel.Console/
│   ├── Kriegsspiel.Console.csproj
│   ├── Program.cs
│   ├── appsettings.json
│   ├── Startup/
│   │   ├── ServiceRegistration.cs     # DI: Core + AI + UI
│   │   └── GameLoop.cs
│   ├── Commands/
│   │   ├── ICommand.cs
│   │   ├── SituationCommand.cs        # ОБСТАНОВКА
│   │   ├── MapCommand.cs              # КАРТА
│   │   ├── OrderCommand.cs            # ПРИКАЗ
│   │   ├── QueryCommand.cs            # ЗАПРОС
│   │   ├── TimeCommand.cs             # ВРЕМЯ
│   │   ├── JournalCommand.cs          # ЖУРНАЛ
│   │   └── SaveCommand.cs
│   ├── Rendering/
│   │   ├── ReportRenderer.cs
│   │   ├── MapRenderer.cs
│   │   ├── JournalRenderer.cs
│   │   └── AaRenderer.cs
│   └── Persistence/
│       └── JsonSaveStore.cs
│
├── Kriegsspiel.Core.Tests/
│   ├── Kriegsspiel.Core.Tests.csproj
│   ├── ClockTests.cs
│   ├── TransmissionResolverTests.cs
│   ├── UnderstandingResolverTests.cs
│   ├── CombatResolverTests.cs
│   └── Fixtures/
│       └── TestContent.cs
│
└── Content/
    ├── scenarios/
    │   └── crossing_01.json
    ├── friction.json
    ├── reports.json                   # шаблоны донесений
    └── reports.ru.json                # локализация (опционально)


