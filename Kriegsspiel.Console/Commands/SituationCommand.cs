using Kriegsspiel.Console.Rendering;
using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class SituationCommand(ReportRenderer renderer) : ICommand
{
    public void Execute(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        var now = state.Clock.Now;
        AnsiConsole.MarkupLine($"[bold]ОБСТАНОВКА[/] на {ReportRenderer.FormatMinute(now)}");
        RenderKnowledge(state);
        AnsiConsole.WriteLine();
        RenderContradictions(state, now);
        AnsiConsole.WriteLine();
        renderer.Render(state);
        RenderSignals(state);
    }

    private static void RenderKnowledge(GameState state)
    {
        var table = new Table().Border(TableBorder.Simple).Title("Обстановка по донесениям");
        table.AddColumn("Зона");
        table.AddColumn("Состояние");
        table.AddColumn("Кто по донесениям");
        table.AddColumn("Уверенность");
        table.AddColumn("Версии");

        var zones = state.Knowledge.Zones
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .ToList();

        foreach (var (id, knowledge) in zones)
        {
            var name = state.Zones.TryGetValue(id, out var zone) ? zone.Name : id;
            var versions = knowledge.Versions.Count == 0
                ? "—"
                : (knowledge.Conflicting ? "[bold magenta]противоречие: [/]" : "") +
                  string.Join("\n", knowledge.Versions.Select(VersionLine));
            table.AddRow(
                Markup.Escape(name),
                PresenceCell(knowledge),
                OwnerCell(knowledge),
                ReportRenderer.ConfidenceCell(knowledge.Confidence),
                versions);
        }

        var noData = state.Zones.Keys.Count(id => !state.Knowledge.HasZoneData(id));
        if (noData > 0)
            table.AddRow(
                $"[grey]прочие зоны ({noData})[/]",
                "[grey]нет данных[/]",
                "—",
                ReportRenderer.ConfidenceCell(0),
                "Наблюдений и донесений не было.");
        if (zones.Count == 0 && noData == 0)
            table.AddRow("—", "[grey]нет данных[/]", "—", ReportRenderer.ConfidenceCell(0), "Наблюдений и донесений не было.");

        AnsiConsole.Write(table);
    }

    private void RenderContradictions(GameState state, int now)
    {
        var claims = state.Knowledge.Facts
            .Where(kv => ReportRenderer.IsConflicting(kv.Value))
            .SelectMany(kv => kv.Value)
            .ToList();

        if (claims.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Противоречий по fact key не зафиксировано.[/]");
            return;
        }

        renderer.RenderClaims(claims, now);
        AnsiConsole.MarkupLine("[yellow]Показаны все версии; уверенность снижена по правилу противоречия. Истина не раскрывается.[/]");
    }

    private static void RenderSignals(GameState state)
    {
        var impossible = state.DeliveredReports.Count(r => r.Kind == ReportKind.Impossible);
        var answers = state.DeliveredReports.Count(r => r.Kind == ReportKind.QueryAnswer);
        var silences = state.DeliveredReports.Count(r => r.Kind == ReportKind.Silence);
        var contradictions = state.DeliveredReports.Count(r => r.Kind == ReportKind.Contradiction);

        if (impossible + answers + silences + contradictions == 0)
            return;

        var parts = new List<string>();
        if (impossible > 0) parts.Add($"[bold red]НЕВОЗМОЖНО: {impossible}[/]");
        if (answers > 0) parts.Add($"[bold cyan]Ответы на запросы: {answers}[/]");
        if (silences > 0) parts.Add($"[bold yellow]Молчание: {silences} (не «факта нет»)[/]");
        if (contradictions > 0) parts.Add($"[bold magenta]Противоречия: {contradictions}[/]");
        AnsiConsole.MarkupLine($"[bold]Сигналы:[/] {string.Join("; ", parts)}");
    }

    private static string VersionLine(KnowledgeClaim claim)
    {
        var value = ReportRenderer.ClaimValueCell(claim);
        var source = string.IsNullOrWhiteSpace(claim.SourceId) ? "—" : Markup.Escape(claim.SourceId);
        return $"{value} — источник {source}, увер. {ReportRenderer.ConfidenceCell(claim.Confidence)}, " +
               $"событие {ReportRenderer.FormatMinute(claim.EventMinute)}, доставка {ReportRenderer.FormatMinute(claim.DeliveryMinute)}";
    }

    private static string PresenceCell(ZoneKnowledge knowledge) => knowledge.Presence switch
    {
        KnowledgePresence.Empty => "[green]пусто[/]",
        KnowledgePresence.Occupied => "[yellow]занято[/]",
        _ => "[grey]нет данных[/]"
    };

    private static string OwnerCell(ZoneKnowledge knowledge)
    {
        if (!knowledge.HasData)
            return "неизвестно";
        if (knowledge.Conflicting)
            return "[bold magenta]разные данные[/]";
        if (knowledge.ReportedFaction is { } faction)
            return Markup.Escape(FactionLabel(faction));
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
}
