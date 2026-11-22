using Kriegsspiel.Console.Rendering;
using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class MapCommand(MapRenderer r) : ICommand
{
    private const string DetailZone = "ДЕТАЛЬ ЗОНЫ";
    private const string Neighbors = "СОСЕДИ";
    private const string ToOverview = "К ОБЗОРУ";
    private const string FilterAction = "ФИЛЬТР";
    private const string SearchAction = "ПОИСК";
    private const string UnitsAction = "ПОДРАЗДЕЛЕНИЯ";
    private const string NextPage = "ДАЛЬШЕ";
    private const string PrevPage = "НАЗАД ПО СТРАНИЦАМ";
    private const string ResetAction = "СБРОС";
    private const string ExitAction = "ВЫХОД";
    private const string ManualZone = "ВВЕСТИ ID/НАЗВАНИЕ";

    private static readonly string[] OverviewActions =
    [
        DetailZone, FilterAction, SearchAction, UnitsAction, NextPage, PrevPage, ResetAction, ExitAction
    ];

    private static readonly string[] DetailActions = [Neighbors, ToOverview, UnitsAction, ExitAction];

    public void Execute(GameState state)
    {
        var filter = MapFilter.Default;
        var page = 0;
        string? focus = null;

        while (true)
        {
            if (focus is null)
                r.RenderOverview(state, filter, page);
            else
                r.RenderDetail(state, focus);

            var action = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title(focus is null ? "КАРТА (обзор) >" : $"КАРТА ({focus}) >")
                    .AddChoices(focus is null ? OverviewActions : DetailActions));

            switch (action)
            {
                case DetailZone:
                    focus = SelectZone(state, filter, page) ?? focus;
                    break;
                case Neighbors:
                    focus = SelectNeighbor(state, focus!) ?? focus;
                    break;
                case ToOverview:
                    focus = null;
                    break;
                case FilterAction:
                    filter = PromptFilter(filter);
                    page = 0;
                    break;
                case SearchAction:
                    filter = filter with
                    {
                        Text = AnsiConsole.Prompt(
                            new TextPrompt<string>("Фрагмент ID или названия зоны (пусто — все):").AllowEmpty()).Trim()
                    };
                    page = 0;
                    break;
                case UnitsAction:
                    var query = AnsiConsole.Prompt(
                        new TextPrompt<string>("Фильтр подразделений (ID/название/зона, пусто — все):").AllowEmpty()).Trim();
                    r.RenderUnitSummary(state, query);
                    break;
                case NextPage:
                    page++;
                    break;
                case PrevPage:
                    page = Math.Max(0, page - 1);
                    break;
                case ResetAction:
                    filter = MapFilter.Default;
                    page = 0;
                    focus = null;
                    break;
                case ExitAction:
                    return;
            }
        }
    }

    private string? SelectZone(GameState state, MapFilter filter, int page)
    {
        var choices = new List<string> { ManualZone };
        choices.AddRange(r.BuildPage(state, filter, page).Rows.Select(row => row.Id));
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Зона из текущей страницы или ручной ввод")
                .AddChoices(choices));

        if (selected != ManualZone)
            return selected;

        var text = AnsiConsole.Prompt(new TextPrompt<string>("ID или название зоны:").AllowEmpty()).Trim();
        if (text.Length == 0)
            return null;
        if (state.Zones.ContainsKey(text))
            return text;

        var matches = state.Zones.Values
            .Where(zone => zone.Id.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                           zone.Name.Contains(text, StringComparison.OrdinalIgnoreCase))
            .OrderBy(zone => zone.Id, StringComparer.Ordinal)
            .Select(zone => zone.Id)
            .ToList();

        if (matches.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Зона не найдена.[/]");
            return null;
        }
        if (matches.Count == 1)
            return matches[0];

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Найдено несколько зон")
                .AddChoices(matches));
    }

    private string? SelectNeighbor(GameState state, string zoneId)
    {
        if (!state.Zones.TryGetValue(zoneId, out var zone))
            return null;

        var neighbors = zone.TravelMinutes.Keys
            .OrderBy(id => id, StringComparer.Ordinal)
            .ToList();
        if (neighbors.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]У зоны нет связей.[/]");
            return null;
        }

        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Соседняя зона")
                .AddChoices(neighbors));
    }

    private MapFilter PromptFilter(MapFilter current) =>
        AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Сужение выборки зон")
                .AddChoices(
                    "Все", "Нет данных", "Пусто", "Занято", "Синие по донесениям",
                    "Красные по донесениям", "Нейтральные по донесениям",
                    "Сторона не опознана", "Только противоречия")) switch
        {
            "Нет данных" => current with { Presence = KnowledgePresence.NoData, Faction = null, UnknownFaction = false, OnlyConflicting = false },
            "Пусто" => current with { Presence = KnowledgePresence.Empty, Faction = null, UnknownFaction = false, OnlyConflicting = false },
            "Занято" => current with { Presence = KnowledgePresence.Occupied, Faction = null, UnknownFaction = false, OnlyConflicting = false },
            "Синие по донесениям" => current with { Faction = Faction.Blue, Presence = null, UnknownFaction = false, OnlyConflicting = false },
            "Красные по донесениям" => current with { Faction = Faction.Red, Presence = null, UnknownFaction = false, OnlyConflicting = false },
            "Нейтральные по донесениям" => current with { Faction = Faction.Neutral, Presence = null, UnknownFaction = false, OnlyConflicting = false },
            "Сторона не опознана" => current with { Faction = null, UnknownFaction = true, Presence = null, OnlyConflicting = false },
            "Только противоречия" => current with { Faction = null, UnknownFaction = false, Presence = null, OnlyConflicting = true },
            _ => current with { Faction = null, UnknownFaction = false, Presence = null, OnlyConflicting = false }
        };
}
