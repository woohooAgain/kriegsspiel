using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Console.Commands;
using Kriegsspiel.Console.Persistence;
using Kriegsspiel.Console.Rendering;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Kriegsspiel.Console.Startup;

public static class ServiceRegistration
{
    public static IServiceProvider Build(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(config);

        services.AddSingleton<ContentLoader>();
        services.AddSingleton(sp => {
            var root = config["ContentRoot"] ?? "Content";
            var scenarioId = config["Scenario"] ?? "test_01";
            return sp.GetRequiredService<ContentLoader>().Load(
                $"{root}/scenarios/{scenarioId}.json",
                $"{root}/friction.json",
                $"{root}/reports.json");
        });
        services.AddSingleton(sp => sp.GetRequiredService<ScenarioBundle>().Friction);
        services.AddSingleton(_ => new Random(config.GetValue<int?>("Seed") ?? Environment.TickCount));

        services.AddSingleton<ITransmissionResolver, TransmissionResolver>();
        services.AddSingleton<IUnderstandingResolver, UnderstandingResolver>();
        services.AddSingleton<IExecutionResolver, ExecutionResolver>();
        services.AddSingleton<ICombatResolver, CombatResolver>();
        services.AddSingleton<Arbiter>();

        services.AddSingleton<ReportRenderer>();
        services.AddSingleton<MapRenderer>();
        services.AddSingleton<JournalRenderer>();
        services.AddSingleton<JsonSaveStore>();

        services.AddSingleton<SituationCommand>();
        services.AddSingleton<MapCommand>();
        services.AddSingleton<OrderCommand>();
        services.AddSingleton<JournalCommand>();
        services.AddSingleton<SaveCommand>();

        services.AddSingleton<GameLoop>();
        return services.BuildServiceProvider();
    }
}