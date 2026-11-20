using Kriegsspiel.Console.Commands;
using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Spectre.Console;

namespace Kriegsspiel.Console.Startup;

public sealed class GameLoop
{
    private const int TurnMinutes = 30;

    private readonly Arbiter _arbiter;
    private readonly ScenarioBundle _bundle;
    private readonly FrictionTable _friction;
    private readonly SituationCommand _sit;
    private readonly MapCommand _map;
    private readonly OrderCommand _order;
    private readonly QueryCommand _query;
    private readonly JournalCommand _journal;
    private readonly SaveCommand _save;
    private readonly LoadCommand _load;
    private GameState _state;
    private readonly List<Order> _pending = new();

    public GameLoop(Arbiter arbiter, ScenarioBundle bundle, FrictionTable friction,
                    IRandomSource random,
                    SituationCommand sit, MapCommand map, OrderCommand order,
                    QueryCommand query, JournalCommand journal, SaveCommand save, LoadCommand load)
    {
        _arbiter = arbiter;
        _bundle = bundle;
        _friction = friction;
        _sit = sit;
        _map = map;
        _order = order;
        _query = query;
        _journal = journal;
        _save = save;
        _load = load;
        _state = GameStateFactory.FromBundle(bundle);
        _state.Seed = random.Seed;
        _state.RngState = random.State;
    }

    public void Run()
    {
        AnsiConsole.MarkupLine($"[bold]{_bundle.Scenario.Name}[/] — старт {Format(_state.Clock.Now)}; LLM: {(_friction.Llm.Enabled ? "вкл" : "выкл")}");

        while (_state.Clock.Now < _bundle.Scenario.EndMinute)
        {
            AnsiConsole.MarkupLine($"[bold]Текущее время[/]: {Format(_state.Clock.Now)}");
            var cmd = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Штаб >[/]")
                    .AddChoices("ОБСТАНОВКА", "КАРТА", "ПРИКАЗ", "ЗАПРОС", "ЖУРНАЛ",
                                "ВРЕМЯ", "СОХРАНИТЬ", "ЗАГРУЗИТЬ", "ВЫЙТИ"));

            switch (cmd)
            {
                case "ОБСТАНОВКА":
                    _sit.Execute(_state);
                    break;
                case "КАРТА":
                    _map.Execute(_state);
                    break;
                case "ПРИКАЗ":
                    var order = _order.Build(_state);
                    if (order is not null)
                        _pending.Add(order);
                    break;
                case "ЗАПРОС":
                    _query.Execute(_state);
                    break;
                case "ЖУРНАЛ":
                    _journal.Execute(_state);
                    break;
                case "ВРЕМЯ":
                    var result = _arbiter.AdvanceTo(_state.Clock.Now + TurnMinutes, _state, _pending);
                    _pending.Clear();
                    foreach (var report in result.NewReports)
                        AnsiConsole.MarkupLine($"[yellow]{Format(report.ArrivesAt)}[/] {Markup.Escape(report.Text)}");
                    break;
                case "СОХРАНИТЬ":
                    _save.Execute(_state);
                    break;
                case "ЗАГРУЗИТЬ":
                    try
                    {
                        _state = _load.Load();
                        _pending.Clear();
                        AnsiConsole.MarkupLine("[green]Сохранение загружено.[/]");
                    }
                    catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                    {
                        AnsiConsole.MarkupLine($"[red]Сохранение не найдено: {Markup.Escape(ex.Message)}[/]");
                    }
                    break;
                case "ВЫЙТИ":
                    return;
            }
        }
        AnsiConsole.MarkupLine("[bold green]Сценарий завершён.[/]");
    }

    private static string Format(int minute) => $"{minute / 60:00}:{minute % 60:00}";
}
