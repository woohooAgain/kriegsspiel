using Kriegsspiel.Core.Arbitration;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class QueryCommand(ITransmissionResolver transmission, FrictionTable friction, Random rng) : ICommand
{
    public void Execute(GameState state)
    {
        AnsiConsole.MarkupLine("[bold]Запрос[/]");
        RenderStatus(state);

        var blueUnits = state.Units.Values
            .Where(u => u.Faction == Faction.Blue)
            .OrderBy(u => u.Type == UnitType.HQ ? 0 : 1)
            .ThenBy(u => u.Id)
            .ToList();

        if (blueUnits.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Нет доступных подразделений для запроса.[/]");
            return;
        }

        var action = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Действие")
                .AddChoices("Новый запрос", "Назад"));

        if (action == "Назад")
            return;

        var requester = blueUnits.FirstOrDefault(u => u.Type == UnitType.HQ) ?? blueUnits[0];
        var targets = blueUnits.Where(u => u.Id != requester.Id).ToList();
        if (targets.Count == 0)
            targets = blueUnits;

        var target = AnsiConsole.Prompt(
            new SelectionPrompt<Unit>()
                .Title("Адресат запроса")
                .UseConverter(u => $"{u.Name} ({u.Id}, {u.Type}, {u.LocationZoneId})")
                .AddChoices(targets));

        var queryType = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Тип запроса")
                .AddChoices("Обстановка/контакт", "Состояние и потери", "Положение", "Подтверждение получения"));

        int seq = state.EventLog.OfType<QuerySent>().Count() + 1;
        var queryId = $"Q-{state.Clock.Now}-{seq}";

        var dispatch = transmission.TransmitQuery(queryId, requester.Id, target.Id, state, friction, rng);

        AnsiConsole.MarkupLine($"[green]QuerySent[/]: [bold]{Markup.Escape(queryId)}[/] от {Markup.Escape(requester.Name)} к {Markup.Escape(target.Name)} ({Markup.Escape(queryType)}), мин {state.Clock.Now}.");
        if (dispatch is not null)
        {
            if (dispatch.OutboundLost)
                AnsiConsole.MarkupLine($"  Курьер: [red]потерян на доставке[/] (ожид. доставка {dispatch.DeliveryMinute}, возврат {dispatch.ReturnMinute}, id {Markup.Escape(dispatch.CourierId)}).");
            else
                AnsiConsole.MarkupLine($"  Курьер: отправлен (доставка ~{dispatch.DeliveryMinute}, возврат ~{dispatch.ReturnMinute}, подтверждение только по возврату, id {Markup.Escape(dispatch.CourierId)}). Приоритет запросов учтён в очереди.");
        }
        else
        {
            AnsiConsole.MarkupLine("  Курьер: запрос поставлен в очередь (ETA будет определён при отправке), не блокирует действия.");
        }

        AnsiConsole.MarkupLine("[grey]Ответ появится в последующие ходы после обычной обработки (QueryAnswered/QuerySilence).[/]");
        RenderStatus(state);
    }

    private void RenderStatus(GameState state)
    {
        var queryEvents = state.EventLog
            .Where(e => e is QuerySent or QueryAnswered or QuerySilence)
            .OrderBy(e => e.Minute)
            .ToList();

        int silenceWindow = Math.Max(1, friction.Reports.QuerySilenceMinutes);

        if (queryEvents.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Запросов в журнале нет.[/]");
            return;
        }

        var table = new Table().Border(TableBorder.Simple).Title("Статус запросов");
        table.AddColumn("Мин");
        table.AddColumn("Событие");
        table.AddColumn("Детали");

        foreach (var e in queryEvents)
        {
            switch (e)
            {
                case QuerySent qs:
                    table.AddRow(
                        $"{qs.Minute}",
                        "QuerySent",
                        $"от {Markup.Escape(qs.RequesterId)} к {Markup.Escape(qs.TargetId)}");
                    break;
                case QueryAnswered qa when qa.Report is not null:
                    var r = qa.Report;
                    var details = $"событие {r.EventMinute}, доставка {r.DeliveryMinute}, источник {Markup.Escape(r.SourceId)}, уверенность {r.Confidence}";
                    if (!string.IsNullOrWhiteSpace(r.Text))
                        details += $"; {Markup.Escape(Trim(r.Text, 80))}";
                    table.AddRow($"{qa.Minute}", "QueryAnswered", details);
                    break;
                case QueryAnswered qa:
                    table.AddRow($"{qa.Minute}", "QueryAnswered", $"источник {Markup.Escape(qa.SourceId)} (донесение недоступно)");
                    break;
                case QuerySilence qsil:
                    table.AddRow(
                        $"{qsil.Minute}",
                        "QuerySilence",
                        $"адресат {Markup.Escape(qsil.TargetId)}, уверенность {qsil.Confidence} — нет ответа, не «факта нет»");
                    break;
            }
        }

        foreach (var sent in queryEvents.OfType<QuerySent>())
        {
            bool answered = queryEvents.OfType<QueryAnswered>().Any(a => a.Minute >= sent.Minute);
            bool silenced = queryEvents.OfType<QuerySilence>().Any(s => s.Minute >= sent.Minute);
            if (!answered && !silenced && state.Clock.Now >= sent.Minute + silenceWindow)
            {
                table.AddRow(
                    $"{state.Clock.Now}",
                    "Ожидание истекло",
                    $"адресат {Markup.Escape(sent.TargetId)} — окно {silenceWindow} мин истекло, нет ответа (не «факта нет»); QuerySilence появится/фиксируется по правилам симуляции");
            }
        }

        AnsiConsole.Write(table);

        var answers = state.DeliveredReports
            .Where(r => r.Kind == ReportKind.QueryAnswer)
            .OrderBy(r => r.DeliveryMinute)
            .ToList();
        if (answers.Count > 0)
        {
            var rt = new Table().Border(TableBorder.Simple).Title("Полученные ответы (донесения)");
            rt.AddColumn("Событие");
            rt.AddColumn("Доставка");
            rt.AddColumn("Источник");
            rt.AddColumn("Уверенность");
            rt.AddColumn("Текст");
            foreach (var r in answers.TakeLast(10))
                rt.AddRow($"{r.EventMinute}", $"{r.DeliveryMinute}", Markup.Escape(r.SourceId), $"{r.Confidence}", Markup.Escape(Trim(r.Text, 60)));
            AnsiConsole.Write(rt);
        }
    }

    private static string Trim(string value, int max) =>
        value.Length <= max ? value : value[..max] + "…";
}
