using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class JournalRenderer
{
    public void Render(GameState state)
    {
        var t = new Table().Border(TableBorder.Simple).Title("Журнал");
        t.AddColumn("Время"); t.AddColumn("Событие");
        foreach (var e in state.EventLog.TakeLast(40))
            t.AddRow($"{e.Minute / 60:00}:{e.Minute % 60:00}", e.GetType().Name);
        AnsiConsole.Write(t);
    }
}