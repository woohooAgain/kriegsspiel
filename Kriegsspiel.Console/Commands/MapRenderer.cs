using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class MapRenderer
{
    public void Render(GameState state)
    {
        var table = new Table().Border(TableBorder.Rounded).Title("Карта знания");
        table.AddColumn("Зона");
        table.AddColumn("Местность");
        table.AddColumn("Состояние");
        table.AddColumn("Кто по донесениям");
        table.AddColumn("Событие");
        table.AddColumn("Доставка/обновление");
        table.AddColumn("Уверенность");
        table.AddColumn("Версии");

        foreach (var zone in state.Zones.Values.OrderBy(z => z.Id))
        {
            var knowledge = state.Knowledge.Zones.GetValueOrDefault(zone.Id);
            table.AddRow(
                Markup.Escape($"{zone.Id} — {zone.Name}"),
                TerrainLabel(zone.Terrain),
                PresenceLabel(knowledge),
                OwnerLabel(knowledge),
                MinuteLabel(knowledge?.EventMinute),
                MinuteLabel(knowledge?.DeliveryMinute),
                knowledge is null ? "0/3" : $"{knowledge.Confidence}/3",
                VersionsLabel(knowledge));
        }

        AnsiConsole.Write(table);
    }

    private static string TerrainLabel(TerrainType terrain) => terrain switch
    {
        TerrainType.Open => "открытая",
        TerrainType.Forest => "лес",
        TerrainType.Village => "деревня",
        TerrainType.River => "река",
        TerrainType.Bridge => "мост",
        _ => "неизвестно"
    };

    private static string PresenceLabel(ZoneKnowledge? knowledge) => knowledge?.Presence switch
    {
        KnowledgePresence.Empty => "пусто",
        KnowledgePresence.Occupied => "занято",
        _ => "нет данных"
    };

    private static string OwnerLabel(ZoneKnowledge? knowledge)
    {
        if (knowledge is null || !knowledge.HasData)
            return "неизвестно";
        if (knowledge.Conflicting)
            return "разные данные";
        if (knowledge.ReportedFaction is { } faction)
            return FactionLabel(faction);
        return string.IsNullOrEmpty(knowledge.ReportedOwner)
            ? "неизвестно"
            : Markup.Escape(knowledge.ReportedOwner);
    }

    private static string FactionLabel(Faction faction) => faction switch
    {
        Faction.Blue => "синие",
        Faction.Red => "красные",
        Faction.Neutral => "нейтральные",
        _ => "неизвестно"
    };

    private static string MinuteLabel(int? minute) =>
        minute is null ? "—" : $"мин. {minute.Value} ({minute.Value / 60:00}:{minute.Value % 60:00})";

    private static string VersionsLabel(ZoneKnowledge? knowledge)
    {
        if (knowledge is null || knowledge.Versions.Count == 0)
            return "—";

        var versions = knowledge.Versions
            .Select(VersionLabel)
            .Distinct(StringComparer.Ordinal);
        var prefix = knowledge.Conflicting ? "противоречие: " : "";
        return prefix + string.Join("\n", versions);
    }

    private static string VersionLabel(KnowledgeClaim claim)
    {
        var value = claim.IsEmptyClaim ? "пусто" : Markup.Escape(claim.Value);
        return $"{Markup.Escape(claim.FactKey)} = {value} (увер. {claim.Confidence}/3, событие {MinuteLabel(claim.EventMinute)}, доставка {MinuteLabel(claim.DeliveryMinute)})";
    }
}
