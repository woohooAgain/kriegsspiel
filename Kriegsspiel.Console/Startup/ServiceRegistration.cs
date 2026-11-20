using Kriegsspiel.AI;
using Kriegsspiel.Console.Commands;
using Kriegsspiel.Console.Persistence;
using Kriegsspiel.Console.Rendering;
using Kriegsspiel.Core.Ai;
using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Reporting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kriegsspiel.Console.Startup;

public static class ServiceRegistration
{
    public static IServiceProvider Build(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddCommandLine(args)
            .Build();

        var contentRoot = config["ContentRoot"] ?? "Content";
        var scenarioId = config["Scenario"] ?? "test_01";
        var bundle = new ContentLoader().Load(
            $"{contentRoot}/scenarios/{scenarioId}.json",
            $"{contentRoot}/friction.json",
            $"{contentRoot}/reports.json");

        var useLlm = config.GetValue<bool?>("UseLlm") ?? false;
        var llm = new FrictionTable.LlmParameters
        {
            Enabled = useLlm,
            SchemaVersion = config["Llm:SchemaVersion"] ?? bundle.Friction.Llm.SchemaVersion,
            FallbackMode = config["Llm:FallbackMode"] ?? bundle.Friction.Llm.FallbackMode,
            LogPrompts = config.GetValue<bool?>("Llm:LogPrompts") ?? bundle.Friction.Llm.LogPrompts
        };
        var friction = new FrictionTable
        {
            Hq = bundle.Friction.Hq,
            Orders = bundle.Friction.Orders,
            Movement = bundle.Friction.Movement,
            Combat = bundle.Friction.Combat,
            Recon = bundle.Friction.Recon,
            Reports = bundle.Friction.Reports,
            Supply = bundle.Friction.Supply,
            Capture = bundle.Friction.Capture,
            Civilians = bundle.Friction.Civilians,
            Llm = llm,
            DifficultyProfiles = bundle.Friction.DifficultyProfiles
        };
        var effectiveBundle = new ScenarioBundle
        {
            Scenario = bundle.Scenario,
            Friction = friction,
            Reports = bundle.Reports,
            ScenarioRaw = bundle.ScenarioRaw,
            FrictionRaw = bundle.FrictionRaw,
            ReportsRaw = bundle.ReportsRaw
        };
        var seed = config.GetValue<int?>("Seed") ?? bundle.Scenario.Seed ?? 1;

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);
        services.AddSingleton(effectiveBundle);
        services.AddSingleton(friction);
        services.AddSingleton(bundle.Reports);
        services.AddSingleton<ContentLoader>();
        services.AddSingleton<IRandomSource>(new DeterministicRandomSource(seed));
        services.AddSingleton(new Random(seed));
        services.AddSingleton<IGameRules, ShellGameRules>();
        services.AddSingleton<IKnowledgeUpdater, KnowledgeUpdater>();
        services.AddSingleton<IReportFactory>(sp => new ReportFactory(
            sp.GetRequiredService<FrictionTable>(),
            sp.GetRequiredService<IRandomSource>()));
        services.AddSingleton(sp => new ReportDeliveryResolver(
            sp.GetRequiredService<FrictionTable>(),
            sp.GetRequiredService<IRandomSource>()));
        services.AddSingleton<IAmbiguityInterpreter, DeterministicAmbiguityInterpreter>();
        services.AddSingleton<AiReplayLog>();
        services.AddSingleton<ILlmClient>(new DelegateLlmClient(_ => null));
        services.AddSingleton<IDoctrineOrderFallback, TemplateDoctrineOrderFallback>();
        services.AddSingleton<TemplateReportNarrator>();
        services.AddSingleton<LlmReportNarrator>();
        services.AddSingleton<IReportNarrator>(sp => useLlm
            ? sp.GetRequiredService<LlmReportNarrator>()
            : sp.GetRequiredService<TemplateReportNarrator>());
        services.AddSingleton<LlmOrderCompiler>();

        services.AddSingleton<ITransmissionResolver, TransmissionResolver>();
        services.AddSingleton<IUnderstandingResolver, UnderstandingResolver>();
        services.AddSingleton<IExecutionResolver, ExecutionResolver>();
        services.AddSingleton<ContactGeometryResolver>();
        services.AddSingleton<ICombatResolver, CombatResolver>();
        services.AddSingleton<Arbiter>(sp => new Arbiter(
            sp.GetRequiredService<FrictionTable>(),
            sp.GetRequiredService<Random>(),
            sp.GetRequiredService<ITransmissionResolver>(),
            sp.GetRequiredService<IUnderstandingResolver>(),
            sp.GetRequiredService<IExecutionResolver>(),
            sp.GetRequiredService<ICombatResolver>(),
            sp.GetRequiredService<IKnowledgeUpdater>(),
            sp.GetRequiredService<IReportFactory>()));

        services.AddSingleton<ReportRenderer>();
        services.AddSingleton<MapRenderer>();
        services.AddSingleton<JournalRenderer>();
        services.AddSingleton<JsonSaveStore>();

        services.AddSingleton<SituationCommand>();
        services.AddSingleton<MapCommand>();
        services.AddSingleton<OrderCommand>();
        services.AddSingleton<QueryCommand>();
        services.AddSingleton<JournalCommand>();
        services.AddSingleton<SaveCommand>();
        services.AddSingleton(sp => new LoadCommand(sp.GetRequiredService<JsonSaveStore>()));

        services.AddSingleton<GameLoop>();
        return services.BuildServiceProvider();
    }

    private sealed class ShellGameRules : IGameRules
    {
        public bool IsCombatEffective(Unit unit) => unit.IsCombatEffective;
    }
}
