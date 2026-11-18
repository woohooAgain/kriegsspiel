using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class ReportRenderer
{
    public void Render(GameState state)
    {
        var t = new Table().Border(TableBorder.Simple);
        t.AddColumn("Время"); t.AddColumn("От"); t.AddColumn("Тип"); t.AddColumn("Текст");
        foreach (var r in state.DeliveredReports.OrderBy(r => r.ArrivesAt).TakeLast(30))
        {
            var time = $"{r.ArrivesAt / 60:00}:{r.ArrivesAt % 60:00}";
            t.AddRow(time, r.FromUnitId, r.Kind.ToString(), r.Text);
        }
        AnsiConsole.Write(t);
    }
}