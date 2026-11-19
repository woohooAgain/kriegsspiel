using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class MapRenderer
{
    public void Render(GameState state)
    {
        var t = new Table().Border(TableBorder.Rounded).Title("Известная обстановка");
        t.AddColumn("Зона"); t.AddColumn("Местность"); t.AddColumn("Свои"); t.AddColumn("Знание");
        foreach (var z in state.Zones.Values.OrderBy(z => z.Id))
        {
            var own = z.PresentUnitIds
                .Where(id => state.Units.ContainsKey(id) && !id.StartsWith("ENEMY_"))
                .Select(id => state.Units[id].Name);
            var k = state.Knowledge.Zones.GetValueOrDefault(z.Id);
            t.AddRow(z.Id, z.Terrain.ToString(), string.Join(", ", own),
                     k is null ? "?" : $"увер. {k.Confidence}");
        }
        AnsiConsole.Write(t);
    }
}