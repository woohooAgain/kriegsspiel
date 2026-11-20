using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class ReportRenderer
{
    public void Render(GameState state)
    {
        ArgumentNullException.ThrowIfNull(state);
        RenderReports(state.DeliveredReports, state.Clock.Now);
    }

    public void RenderReports(IEnumerable<Report> reports, int now, string title = "Доставленные донесения")
    {
        ArgumentNullException.ThrowIfNull(reports);
        var table = new Table().Border(TableBorder.Rounded).Title(title);
        table.AddColumn("Событие");
        table.AddColumn("Доставка");
        table.AddColumn("Источник");
        table.AddColumn("Уверен.");
        table.AddColumn("Тип");
        table.AddColumn("Возраст");
        table.AddColumn("Текст / факты");

        var ordered = reports
            .OrderBy(r => r.DeliveryMinute)
            .ThenBy(r => r.EventMinute)
            .ToList();

        if (ordered.Count == 0)
        {
            table.AddRow("—", "—", "—", "—", "—", "—", "Донесений нет.");
            AnsiConsole.Write(table);
            return;
        }

        foreach (var report in ordered)
            table.AddRow(
                FormatMinute(report.EventMinute),
                FormatMinute(report.DeliveryMinute),
                SourceCell(report),
                ConfidenceCell(report.Confidence),
                KindCell(report.Kind),
                AgeCell(report.EventMinute, report.DeliveryMinute, now),
                BodyCell(report));

        AnsiConsole.Write(table);
    }

    public void RenderClaims(IReadOnlyList<KnowledgeClaim> claims, int now, string title = "Противоречивые версии")
    {
        ArgumentNullException.ThrowIfNull(claims);
        var table = new Table().Border(TableBorder.Simple).Title(title);
        table.AddColumn("Ключ факта");
        table.AddColumn("Версия");
        table.AddColumn("Источник");
        table.AddColumn("Уверен.");
        table.AddColumn("Событие");
        table.AddColumn("Доставка");
        table.AddColumn("Возраст");

        foreach (var claim in claims
                     .OrderBy(c => c.FactKey, StringComparer.Ordinal)
                     .ThenBy(c => c.DeliveryMinute))
        {
            table.AddRow(
                $"[bold magenta]противоречие[/]\n{Markup.Escape(claim.FactKey)}",
                ClaimValueCell(claim),
                Markup.Escape(string.IsNullOrWhiteSpace(claim.SourceId) ? "—" : claim.SourceId),
                ConfidenceCell(claim.Confidence),
                FormatMinute(claim.EventMinute),
                FormatMinute(claim.DeliveryMinute),
                AgeCell(claim.EventMinute ?? claim.DeliveryMinute, claim.DeliveryMinute, now));
        }

        AnsiConsole.Write(table);
    }

    public static bool IsConflicting(IReadOnlyList<KnowledgeClaim> claims)
    {
        ArgumentNullException.ThrowIfNull(claims);
        return claims.Select(ClaimValueKey).Distinct(StringComparer.Ordinal).Count() > 1;
    }

    public static string KindLabel(ReportKind kind) => kind switch
    {
        ReportKind.Impossible => "НЕВОЗМОЖНО",
        ReportKind.QueryAnswer => "Ответ на запрос",
        ReportKind.Silence => "Молчание",
        ReportKind.Contradiction => "Противоречие",
        ReportKind.Recon => "Разведка",
        ReportKind.CivilianRumor => "Слух",
        ReportKind.CombatResult => "Результат боя",
        ReportKind.Loss => "Потери",
        ReportKind.Arrival => "Прибытие",
        ReportKind.Contact => "Контакт",
        ReportKind.Request => "Запрос",
        ReportKind.Status => "Статус",
        ReportKind.Interrogation => "Допрос",
        _ => kind.ToString()
    };

    public static string ConfidenceCell(int confidence) => confidence switch
    {
        3 => "[bold green]3/3[/]",
        2 => "[yellow]2/3[/]",
        1 => "[grey]1/3[/]",
        _ => "[red]0/3[/]"
    };

    public static string AgeCell(int eventMinute, int deliveryMinute, int now)
    {
        var deliveryAge = Math.Max(0, now - deliveryMinute);
        var eventAge = Math.Max(0, now - eventMinute);
        return $"{deliveryAge} мин с доставки\n{eventAge} мин с события";
    }

    public static string FormatMinute(int minute) =>
        $"{minute} ({minute / 60:00}:{minute % 60:00})";

    public static string FormatMinute(int? minute) =>
        minute is null ? "—" : FormatMinute(minute.Value);

    public static string ClaimValueCell(KnowledgeClaim claim) =>
        claim.IsEmptyClaim ? "[grey]пусто[/]" : Markup.Escape(claim.Value);

    private static string ClaimValueKey(KnowledgeClaim claim) =>
        claim.IsEmptyClaim ? KnowledgeClaim.EmptyValue : claim.Value;

    private static string SourceCell(Report report)
    {
        var source = string.IsNullOrWhiteSpace(report.SourceId) ? "—" : report.SourceId;
        var cell = Markup.Escape(source);
        if (!string.IsNullOrWhiteSpace(report.ReportedBy) &&
            !string.Equals(report.ReportedBy, report.SourceId, StringComparison.Ordinal))
            cell += $"\nчерез {Markup.Escape(report.ReportedBy)}";
        return cell;
    }

    private static string KindCell(ReportKind kind) => kind switch
    {
        ReportKind.Impossible => "[bold red]НЕВОЗМОЖНО[/]",
        ReportKind.QueryAnswer => "[bold cyan]Ответ на запрос[/]",
        ReportKind.Silence => "[bold yellow]Молчание[/]",
        ReportKind.Contradiction => "[bold magenta]Противоречие[/]",
        _ => Markup.Escape(KindLabel(kind))
    };

    private static string BodyCell(Report report)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(report.Text))
            parts.Add(Markup.Escape(report.Text.Trim()));
        if (!string.IsNullOrWhiteSpace(report.FactKey))
            parts.Add($"ключ: {Markup.Escape(report.FactKey)}");
        foreach (var (key, value) in report.Facts.OrderBy(kv => kv.Key, StringComparer.Ordinal))
            parts.Add($"{Markup.Escape(key)}={Markup.Escape(value)}");
        if (report.Kind == ReportKind.Silence)
            parts.Add("[yellow]Нет ответа в срок — это не «факта нет»[/]");
        if (parts.Count == 0)
            parts.Add("—");
        return string.Join("\n", parts);
    }
}
