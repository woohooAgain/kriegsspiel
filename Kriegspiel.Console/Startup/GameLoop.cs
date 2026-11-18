using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Kriegsspiel.Console.Commands;
using Spectre.Console;

namespace Kriegsspiel.Console.Startup;

public sealed class GameLoop
{
    private readonly Arbiter _arbiter;
    private readonly ScenarioBundle _bundle;
    private readonly SituationCommand _sit;
    private readonly MapCommand _map;
    private readonly OrderCommand _order;
    private readonly JournalCommand _journal;
    private readonly SaveCommand _save;
    private GameState _state;
    private readonly List<Order> _pending = new();

    public GameLoop(Arbiter arbiter, ScenarioBundle bundle,
                    SituationCommand sit, MapCommand map, OrderCommand order,
                    JournalCommand journal, SaveCommand save)
    {
        _arbiter = arbiter; _bundle = bundle;
        _sit = sit; _map = map; _order = order; _journal = journal; _save = save;
        _state = GameStateFactory.FromBundle(bundle);
    }

    public void Run()
    {
        AnsiConsole.MarkupLine($"[bold]{_bundle.Scenario.Name}[/] — старт {_state.Clock.Now / 60:00}:{_state.Clock.Now % 60:00}");

        while (_state.Clock.Now < _bundle.Scenario.EndMinute)
        {
            AnsiConsole.MarkupLine($"[bold]Текущее время[/]: {_state.Clock.Now / 60:00}:{_state.Clock.Now % 60:00}");
            var cmd = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[grey]Штаб >[/]")
                    .AddChoices("ОБСТАНОВКА", "КАРТА", "ПРИКАЗ", "ВРЕМЯ +30", "ЖУРНАЛ", "СОХРАНИТЬ", "ВЫЙТИ"));

            switch (cmd)
            {
                case "ОБСТАНОВКА": _sit.Execute(_state); break;
                case "КАРТА": _map.Execute(_state); break;
                case "ПРИКАЗ":
                    var o = _order.Build(_state);
                    if (o is not null) _pending.Add(o);
                    break;
                case "ЖУРНАЛ": _journal.Execute(_state); break;
                case "СОХРАНИТЬ": _save.Execute(_state); break;
                case "ВЫЙТИ": return;
                case "ВРЕМЯ +30":
                    var res = _arbiter.AdvanceTo(_state.Clock.Now + 30, _state, _pending);
                    _pending.Clear();
                    foreach (var r in res.NewReports)
                        AnsiConsole.MarkupLine($"[yellow]{r.ArrivesAt / 60:00}:{r.ArrivesAt % 60:00}[/] {r.Text}");
                    break;
            }
        }
        AnsiConsole.MarkupLine("[bold green]Сценарий завершён.[/]");
    }
}