using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed record MapFilter
{
    public KnowledgePresence? Presence { get; init; }
    public Faction? Faction { get; init; }
    public bool UnknownFaction { get; init; }
    public bool OnlyConflicting { get; init; }
    public string Text { get; init; } = "";

    public static MapFilter Default { get; } = new();

    public string Description
    {
        get
        {
            var parts = new List<string>();
            if (Presence is { } presence)
                parts.Add("состояние=" + MapLabels.Presence(presence));
            if (Faction is { } faction)
                parts.Add("сторона=" + MapLabels.Side(faction));
            if (UnknownFaction)
                parts.Add("сторона=не опознана");
            if (OnlyConflicting)
                parts.Add("только противоречия");
            if (!string.IsNullOrWhiteSpace(Text))
                parts.Add("поиск=" + Text);
            return parts.Count == 0 ? "все зоны" : string.Join(", ", parts);
        }
    }
}

public sealed record MapZoneRow(
    string Id,
    string Zone,
    string Terrain,
    string State,
    string Reporter,
    string Confidence,
    string Minutes,
    string Versions);

public sealed record MapPage(IReadOnlyList<MapZoneRow> Rows, int Number, int TotalPages, int MatchedZones);

internal static class MapLabels
{
    public static string Presence(KnowledgePresence presence) => presence switch
    {
        KnowledgePresence.Empty => "пусто",
        KnowledgePresence.Occupied => "занято",
        _ => "нет данных"
    };

    public static string Side(Faction faction) => faction switch
    {
        Faction.Blue => "синие",
        Faction.Red => "красные",
        Faction.Neutral => "нейтральные",
        _ => "неизвестно"
    };

    public static string Terrain(TerrainType terrain) => terrain switch
    {
        TerrainType.Open => "открытая",
        TerrainType.Forest => "лес",
        TerrainType.Village => "деревня",
        TerrainType.River => "река",
        TerrainType.Bridge => "мост",
        _ => "неизвестно"
    };

    public static string Edge(EdgeType edge) => edge switch
    {
        EdgeType.Road => "дорога",
        EdgeType.Offroad => "бездорожье",
        EdgeType.Crossing => "переправа/мост",
        _ => "неизвестно"
    };

    public static string Unit(UnitType type) => type switch
    {
        UnitType.Infantry => "пехота",
        UnitType.Artillery => "артиллерия",
        UnitType.Recon => "разведка",
        UnitType.HQ => "штаб",
        _ => "неизвестно"
    };

    public static string Minute(int? minute) =>
        minute is null ? "—" : $"мин. {minute.Value} ({minute.Value / 60:00}:{minute.Value % 60:00})";

    public static string MinuteShort(int? minute) =>
        minute is null ? "—" : $"{minute.Value / 60:00}:{minute.Value % 60:00}";
}

public sealed class MapRenderer
{
    private const int PageSize = 12;
    private const int MaxUnitNamesPerZone = 5;
    private const int MaxZonesInUnitSummary = 12;

    public void Render(GameState state) => RenderOverview(state, MapFilter.Default, 0);

    public void RenderOverview(GameState state, MapFilter filter, int page)
    {
        var pageData = BuildPage(state, filter, page);
        AnsiConsole.MarkupLine("[bold]КАРТА ЗНАНИЯ — ОБЗОР[/]");
        AnsiConsole.MarkupLine($"[grey]{Markup.Escape(BuildSummary(state))}[/]");
        AnsiConsole.MarkupLine($"[grey]Фильтр: {Markup.Escape(filter.Description)} | страница {pageData.Number + 1}/{pageData.TotalPages}, зон: {pageData.MatchedZones}[/]");

        if (pageData.Rows.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]Нет зон по текущему фильтру. Сбросьте фильтр или измените поиск.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Rounded).Title("Зоны");
        table.AddColumn("Зона");
        table.AddColumn("Местность");
        table.AddColumn("Состояние");
        table.AddColumn("Кто по донесениям");
        table.AddColumn("Событие/доставка");
        table.AddColumn("Увер./версии");

        foreach (var row in pageData.Rows)
        {
            table.AddRow(
                Markup.Escape(row.Zone),
                Markup.Escape(row.Terrain),
                Markup.Escape(row.State),
                Markup.Escape(row.Reporter),
                Markup.Escape(row.Minutes),
                Markup.Escape(row.Confidence + "\n" + row.Versions));
        }

        AnsiConsole.Write(table);
    }

    public void RenderDetail(GameState state, string zoneId)
    {
        var detail = BuildDetail(state, zoneId);
        AnsiConsole.Write(new Panel(new Text(detail))
        {
            Header = new PanelHeader("КАРТА ЗНАНИЯ — ДЕТАЛЬ")
        });
    }

    public void RenderUnitSummary(GameState state, string text = "")
    {
        var summary = BuildUnitSummary(state, text);
        AnsiConsole.Write(new Panel(new Text(summary))
        {
            Header = new PanelHeader("ПОДРАЗДЕЛЕНИЯ В КОНТЕКСТЕ КАРТЫ")
        });
    }

    public string BuildSummary(GameState state)
    {
        var zones = state.Zones.Values.OrderBy(z => z.Id, StringComparer.Ordinal).ToList();
        var noData = 0;
        var empty = 0;
        var occupied = 0;
        var conflicts = 0;

        foreach (var zone in zones)
        {
            var knowledge = state.Knowledge.Zones.GetValueOrDefault(zone.Id);
            switch (knowledge?.Presence ?? KnowledgePresence.NoData)
            {
                case KnowledgePresence.Empty:
                    empty++;
                    break;
                case KnowledgePresence.Occupied:
                    occupied++;
                    break;
                default:
                    noData++;
                    break;
            }
            if (knowledge?.Conflicting == true)
                conflicts++;
        }

        var edgeTypes = new Dictionary<EdgeType, int>();
        var pairs = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in zones)
        {
            foreach (var neighborId in zone.TravelMinutes.Keys)
            {
                var pair = string.CompareOrdinal(zone.Id, neighborId) < 0
                    ? zone.Id + "|" + neighborId
                    : neighborId + "|" + zone.Id;
                if (!pairs.Add(pair))
                    continue;
                var type = zone.EdgeTypeFor(neighborId);
                edgeTypes[type] = edgeTypes.GetValueOrDefault(type) + 1;
            }
        }

        var edges = edgeTypes.Count == 0
            ? "нет"
            : string.Join(", ", edgeTypes.OrderBy(e => e.Key).Select(e => $"{MapLabels.Edge(e.Key)} {e.Value}"));
        var hubs = zones.Count == 0
            ? "нет"
            : string.Join(", ", zones
                .OrderByDescending(z => z.TravelMinutes.Count)
                .ThenBy(z => z.Id, StringComparer.Ordinal)
                .Take(3)
                .Select(z => $"{z.Id}({z.TravelMinutes.Count})"));

        return $"Зон: {zones.Count} | нет данных: {noData}, пусто: {empty}, занято: {occupied}, противоречия: {conflicts} | рёбра: {edges} | узлы: {hubs}";
    }

    public MapPage BuildPage(GameState state, MapFilter filter, int page)
    {
        var matching = state.Zones.Values
            .Where(zone => Matches(zone, state.Knowledge.Zones.GetValueOrDefault(zone.Id), filter))
            .OrderBy(zone => zone.Id, StringComparer.Ordinal)
            .ToList();

        var totalPages = Math.Max(1, (matching.Count + PageSize - 1) / PageSize);
        var number = Math.Clamp(page, 0, totalPages - 1);
        var rows = matching
            .Skip(number * PageSize)
            .Take(PageSize)
            .Select(zone =>
            {
                var knowledge = state.Knowledge.Zones.GetValueOrDefault(zone.Id);
                return new MapZoneRow(
                    zone.Id,
                    zone.Id + "\n" + zone.Name,
                    MapLabels.Terrain(zone.Terrain),
                    MapLabels.Presence(knowledge?.Presence ?? KnowledgePresence.NoData),
                    ReporterLabel(knowledge),
                    knowledge is null ? "0/3" : $"{knowledge.Confidence}/3",
                    $"с: {MapLabels.MinuteShort(knowledge?.EventMinute)}\nд: {MapLabels.MinuteShort(knowledge?.DeliveryMinute)}",
                    VersionSummary(knowledge));
            })
            .ToList();

        return new MapPage(rows, number, totalPages, matching.Count);
    }

    public string BuildDetail(GameState state, string zoneId)
    {
        if (!state.Zones.TryGetValue(zoneId, out var zone))
            return "Зона " + zoneId + " не найдена.";

        var knowledge = state.Knowledge.Zones.GetValueOrDefault(zoneId);
        var lines = new List<string>
        {
            $"ЗОНА: {zone.Id} — {zone.Name}",
            $"Местность: {MapLabels.Terrain(zone.Terrain)} | оборона: {zone.DefenseBonus} | наблюдение: {zone.Observation} | объектов: {zone.Objects.Count}",
            "",
            "ЗНАНИЕ (по донесениям, не истина):",
            $"  Состояние: {MapLabels.Presence(knowledge?.Presence ?? KnowledgePresence.NoData)}",
            $"  Кто по донесениям: {ReporterLabel(knowledge)}",
            $"  Уверенность: {(knowledge?.Confidence ?? 0)}/3",
            $"  Время события: {MapLabels.Minute(knowledge?.EventMinute)}",
            $"  Время доставки/обновления: {MapLabels.Minute(knowledge?.DeliveryMinute)}",
            "  Версии/противоречия:"
        };

        if (knowledge?.Versions.Count is > 0)
        {
            foreach (var claim in knowledge.Versions)
            {
                lines.Add($"    {claim.FactKey} = {ClaimValue(claim)} | увер. {claim.Confidence}/3 | источник {SourceLabel(claim)} | событие {MapLabels.Minute(claim.EventMinute)} | доставка {MapLabels.Minute(claim.DeliveryMinute)}");
            }
            if (knowledge.Conflicting)
                lines.Add("  Показаны все версии; уверенность снижена по правилу противоречия.");
        }
        else
        {
            lines.Add("    нет");
        }

        lines.Add("");
        lines.Add("ТОПОЛОГИЯ (статические данные карты):");
        var neighbors = zone.TravelMinutes.OrderBy(p => p.Key, StringComparer.Ordinal).ToList();
        if (neighbors.Count == 0)
        {
            lines.Add("  нет связей");
        }
        else
        {
            foreach (var neighbor in neighbors)
            {
                var name = state.Zones.TryGetValue(neighbor.Key, out var adjacent) ? adjacent.Name : "нет в сценарии";
                var neighborKnowledge = state.Knowledge.Zones.GetValueOrDefault(neighbor.Key);
                lines.Add($"  {neighbor.Key} — {name}: {MapLabels.Edge(zone.EdgeTypeFor(neighbor.Key))}, {neighbor.Value} мин, знание: {MapLabels.Presence(neighborKnowledge?.Presence ?? KnowledgePresence.NoData)}");
            }
        }

        var own = state.Units.Values
            .Where(unit => unit.Faction == Faction.Blue && unit.LocationZoneId == zoneId)
            .OrderBy(unit => unit.Id, StringComparer.Ordinal)
            .ToList();
        lines.Add("");
        lines.Add($"СВОИ В ЗОНЕ (данные управления): {own.Count}");
        foreach (var unit in own.Take(MaxUnitNamesPerZone))
            lines.Add($"  {unit.Id} — {unit.Name} ({MapLabels.Unit(unit.Type)}, сила {unit.Strength})");
        if (own.Count > MaxUnitNamesPerZone)
            lines.Add($"  … ещё {own.Count - MaxUnitNamesPerZone}");

        lines.Add("");
        lines.Add("ПРОТИВНИК (только знание/донесения):");
        if (knowledge?.ReportedFaction == Faction.Red)
        {
            lines.Add($"  занято, кто: {ReporterLabel(knowledge)}, ув. {knowledge.Confidence}/3, событие {MapLabels.Minute(knowledge.EventMinute)}, доставка {MapLabels.Minute(knowledge.DeliveryMinute)}");
        }
        else if (knowledge?.Presence == KnowledgePresence.Occupied && !string.IsNullOrEmpty(knowledge.ReportedOwner))
        {
            lines.Add($"  занято, кто по донесениям: {knowledge.ReportedOwner}, ув. {knowledge.Confidence}/3");
        }
        else
        {
            lines.Add("  нет донесений о занятии");
        }

        return string.Join("\n", lines);
    }

    public string BuildUnitSummary(GameState state, string text = "")
    {
        var groups = new Dictionary<string, List<Unit>>(StringComparer.Ordinal);
        var total = 0;
        foreach (var unit in state.Units.Values)
        {
            if (unit.Faction != Faction.Blue)
                continue;
            if (!string.IsNullOrWhiteSpace(text) && !MatchesUnit(unit, state, text))
                continue;
            if (!groups.TryGetValue(unit.LocationZoneId, out var list))
            {
                list = new List<Unit>();
                groups[unit.LocationZoneId] = list;
            }
            list.Add(unit);
            total++;
        }

        var lines = new List<string>
        {
            $"СВОИ ПОДРАЗДЕЛЕНИЯ (данные управления): {total} в {groups.Count} зонах",
        };

        var ordered = groups.OrderBy(g => g.Key, StringComparer.Ordinal).ToList();
        foreach (var group in ordered.Take(MaxZonesInUnitSummary))
        {
            var zoneName = state.Zones.TryGetValue(group.Key, out var zone) ? zone.Name : "нет в сценарии";
            lines.Add($"{group.Key} — {zoneName}: {group.Value.Count}");
            foreach (var unit in group.Value
                         .OrderBy(u => u.Id, StringComparer.Ordinal)
                         .Take(MaxUnitNamesPerZone))
            {
                lines.Add($"  {unit.Id} — {unit.Name} ({MapLabels.Unit(unit.Type)}, сила {unit.Strength})");
            }
            if (group.Value.Count > MaxUnitNamesPerZone)
                lines.Add($"  … ещё {group.Value.Count - MaxUnitNamesPerZone}");
        }
        if (ordered.Count > MaxZonesInUnitSummary)
            lines.Add($"… ещё {ordered.Count - MaxZonesInUnitSummary} зон; уточните поиск");

        lines.Add("");
        lines.Add("ЗАНЯТОСТЬ ПО ЗНАНИЮ (не истина):");
        var knownOccupied = state.Zones.Values
            .Where(zone => state.Knowledge.Zones.GetValueOrDefault(zone.Id)?.Presence == KnowledgePresence.Occupied)
            .OrderBy(zone => zone.Id, StringComparer.Ordinal)
            .Take(MaxZonesInUnitSummary)
            .ToList();
        if (knownOccupied.Count == 0)
        {
            lines.Add("  донесений о занятии нет");
        }
        else
        {
            foreach (var zone in knownOccupied)
            {
                var knowledge = state.Knowledge.Zones.GetValueOrDefault(zone.Id)!;
                lines.Add($"  {zone.Id} — занято, кто: {ReporterLabel(knowledge)}, ув. {knowledge.Confidence}/3, событие {MapLabels.Minute(knowledge.EventMinute)}, доставка {MapLabels.Minute(knowledge.DeliveryMinute)}");
            }
        }

        return string.Join("\n", lines);
    }

    private static bool Matches(Zone zone, ZoneKnowledge? knowledge, MapFilter filter)
    {
        if (filter.Presence is { } presence && (knowledge?.Presence ?? KnowledgePresence.NoData) != presence)
            return false;
        if (filter.Faction is { } faction && knowledge?.ReportedFaction != faction)
            return false;
        if (filter.UnknownFaction &&
            (knowledge?.ReportedFaction is not null || !string.IsNullOrEmpty(knowledge?.ReportedOwner)))
            return false;
        if (filter.OnlyConflicting && knowledge?.Conflicting != true)
            return false;
        if (!string.IsNullOrWhiteSpace(filter.Text) &&
            !zone.Id.Contains(filter.Text, StringComparison.OrdinalIgnoreCase) &&
            !zone.Name.Contains(filter.Text, StringComparison.OrdinalIgnoreCase))
            return false;
        return true;
    }

    private static bool MatchesUnit(Unit unit, GameState state, string text)
    {
        if (unit.Id.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            unit.Name.Contains(text, StringComparison.OrdinalIgnoreCase) ||
            unit.LocationZoneId.Contains(text, StringComparison.OrdinalIgnoreCase))
            return true;
        return state.Zones.TryGetValue(unit.LocationZoneId, out var zone) &&
               zone.Name.Contains(text, StringComparison.OrdinalIgnoreCase);
    }

    private static string ReporterLabel(ZoneKnowledge? knowledge)
    {
        if (knowledge is null || !knowledge.HasData)
            return "—";
        if (knowledge.Conflicting)
            return "разные данные";
        if (knowledge.ReportedFaction is { } faction)
            return MapLabels.Side(faction);
        return string.IsNullOrEmpty(knowledge.ReportedOwner) ? "—" : knowledge.ReportedOwner;
    }

    private static string ClaimValue(KnowledgeClaim claim) =>
        claim.IsEmptyClaim ? "пусто" : claim.Value;

    private static string SourceLabel(KnowledgeClaim claim) =>
        string.IsNullOrEmpty(claim.SourceId) ? "—" : claim.SourceId;

    private static string VersionSummary(ZoneKnowledge? knowledge)
    {
        if (knowledge?.Versions.Count is not > 0)
            return "—";

        var values = knowledge.Versions
            .Select(ClaimValue)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var text = string.Join(" | ", values.Take(2).Select(value =>
            value.Length <= 18 ? value : value[..18] + "…"));
        if (values.Count > 2)
            text += $" (+{values.Count - 2})";
        return (knowledge.Conflicting ? "! " : "") + text;
    }
}
