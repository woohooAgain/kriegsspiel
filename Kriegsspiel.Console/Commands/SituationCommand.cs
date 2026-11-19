using Kriegsspiel.Console.Rendering;
using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class SituationCommand(ReportRenderer renderer) : ICommand
{
    public void Execute(GameState state)
    {
        if (state.DeliveredReports.Count == 0)
        { AnsiConsole.MarkupLine("[grey]Донесений нет.[/]"); return; }
        renderer.Render(state);
    }
}