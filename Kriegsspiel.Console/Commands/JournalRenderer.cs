using System.Globalization;
using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Spectre.Console;

namespace Kriegsspiel.Console.Rendering;

public sealed class JournalRenderer
{
    public void Render(GameState state)
    {
        RenderTimeline(state);
        AnsiConsole.WriteLine();
        RenderCouriers(state);
    }

    private static void RenderTimeline(GameState state)
    {
        var entries = BuildEntries(state)
            .OrderBy(e => e.Minute)
            .ThenBy(e => e.Phase)
            .ThenBy(e => e.Sequence)
            .ToList();

        var table = new Table().Border(TableBorder.Simple).Title("Журнал");
        table.AddColumn("Время");
        table.AddColumn("Мин");
        table.AddColumn("Событие");
        table.AddColumn("Детали");

        if (entries.Count == 0)
        {
            table.AddRow("—", "—", "Нет записей", "Событий и активных курьерских операций пока нет.");
        }
        else
        {
            foreach (var entry in entries)
                table.AddRow(FormatClock(entry.Minute), entry.Minute.ToString(CultureInfo.InvariantCulture), Markup.Escape(entry.Kind), Markup.Escape(entry.Details));
        }

        AnsiConsole.Write(table);
    }

    private static List<JournalEntry> BuildEntries(GameState state)
    {
        var entries = new List<JournalEntry>();
        int seq = 0;

        foreach (var evt in state.EventLog)
        {
            switch (evt)
            {
                case OrderIssued oi:
                    entries.Add(new JournalEntry(oi.Minute, 10, seq++, "Отдача приказа", DescribeOrder(state, oi.OrderId, "отдан")));
                    break;
                case QuerySent qs:
                    entries.Add(new JournalEntry(qs.Minute, 11, seq++, "Отправка запроса", $"Запрос отправлен: от {qs.RequesterId} к {qs.TargetId}."));
                    break;
                case OrderDelivered od:
                    entries.Add(new JournalEntry(od.Minute, 20, seq++, "Доставка приказа (факт)", $"{DescribeOrder(state, od.OrderId, "доставлен")} Факт доставки зафиксирован."));
                    break;
                case OrderLost ol:
                    entries.Add(new JournalEntry(ol.Minute, 20, seq++, "Приказ потерян при доставке", $"{DescribeOrder(state, ol.OrderId, "не доставлен")} Курьер не дошёл (потеря на пути туда)."));
                    break;
                case OrderAccepted oa:
                    entries.Add(new JournalEntry(oa.Minute, 30, seq++, "Приказ принят", $"{DescribeOrder(state, oa.OrderId, "принят/понят")}"));
                    break;
                case OrderMisinterpreted om:
                    entries.Add(new JournalEntry(om.Minute, 31, seq++, "Приказ понят с искажением", $"Приказ {om.OrderId}: {om.Type}."));
                    break;
                case OrderSuperseded os:
                    entries.Add(new JournalEntry(os.Minute, 32, seq++, "Приказ заменён", $"Приказ {os.OldOrderId} заменён приказом {os.NewOrderId} (суперсессия)."));
                    break;
                case ReportDelivered rd:
                    entries.Add(new JournalEntry(rd.Minute, 50, seq++, "Получение донесения (факт)", DescribeReportDelivered(state, rd)));
                    break;
                case QueryAnswered qa:
                    entries.Add(new JournalEntry(qa.Minute, 51, seq++, "Ответ на запрос (факт)", DescribeQueryAnswered(qa)));
                    break;
                case QuerySilence qsil:
                    entries.Add(new JournalEntry(qsil.Minute, 52, seq++, "Молчание по запросу", $"Адресат {qsil.TargetId}, уверенность {qsil.Confidence}. Ответа нет в срок; это не означает отсутствие факта."));
                    break;
                case CourierReturned cr:
                    entries.Add(new JournalEntry(cr.Minute, 60, seq++, "Возврат курьера (факт)", DescribeCourierReturned(state, cr)));
                    break;
                case CourierLost cl:
                    entries.Add(new JournalEntry(cl.Minute, 60, seq++, DescribeCourierLostKind(state, cl), DescribeCourierLost(state, cl)));
                    break;
                case ReportDue rdue:
                    entries.Add(new JournalEntry(rdue.Minute, 40, seq++, "Срок донесения", $"Подразделение {rdue.UnitId}, тип {rdue.Kind}."));
                    break;
                case ReportLost rl:
                    entries.Add(new JournalEntry(rl.Minute, 53, seq++, "Донесение потеряно", $"Донесение {rl.ReportId} потеряно при доставке."));
                    break;
                case UnitMoved um:
                    entries.Add(new JournalEntry(um.Minute, 35, seq++, "Перемещение", $"Подразделение {um.UnitId} движется в зону {um.ToZoneId}."));
                    break;
                case UnitArrived ua:
                    entries.Add(new JournalEntry(ua.Minute, 36, seq++, "Прибытие", $"Подразделение {ua.UnitId} прибыло в зону {ua.ZoneId}."));
                    break;
                case CombatTick ct:
                    entries.Add(new JournalEntry(ct.Minute, 45, seq++, "Бой", $"Боевой тик в зоне {ct.ZoneId}."));
                    break;
                default:
                    entries.Add(new JournalEntry(evt.Minute, 70, seq++, evt.GetType().Name, DescribeGeneric(evt)));
                    break;
            }
        }

        foreach (var courier in state.Couriers.OrderBy(c => c.Id, StringComparer.Ordinal))
            AppendCourierDerivedEntries(state, courier, entries, ref seq);

        foreach (var report in state.InTransitReports)
        {
            if (entries.Any(e => e.Kind.Contains("донес", StringComparison.OrdinalIgnoreCase) && e.Details.Contains(report.Id, StringComparison.Ordinal)))
                continue;
            entries.Add(new JournalEntry(report.ArrivesAt, 49, seq++, "Получение донесения (ETA)", DescribeInTransitReport(state, report)));
        }

        return entries;
    }

    private static void AppendCourierDerivedEntries(GameState state, Courier courier, List<JournalEntry> entries, ref int seq)
    {
        if (courier.OutboundDepartureMinute <= 0 && courier.State == CourierState.Idle)
            return;

        var label = CourierAssignmentLabel(courier);
        if (courier.OutboundDepartureMinute > 0)
        {
            var dispatchDetails = $"{label}. Отправка: {FormatClock(courier.OutboundDepartureMinute)} (факт). " +
                                  $"Доставка: ETA {FormatMinute(courier.ExpectedDeliveryMinute)}. " +
                                  $"Возврат: ETA {FormatMinute(courier.ExpectedReturnMinute)}.";
            if (!entries.Any(e => e.Minute == courier.OutboundDepartureMinute && e.Kind == "Отправка курьера" && e.Details.Contains(label, StringComparison.Ordinal)))
                entries.Add(new JournalEntry(courier.OutboundDepartureMinute, 12, seq++, "Отправка курьера", dispatchDetails));
        }

        if (courier.State is CourierState.Delivering or CourierState.Returning)
        {
            var etaKind = courier.State == CourierState.Delivering ? "Доставка (ETA)" : "Возврат (ETA)";
            int etaMinute = courier.State == CourierState.Delivering
                ? courier.ExpectedDeliveryMinute ?? state.Clock.Now
                : courier.ExpectedReturnMinute ?? state.Clock.Now;
            var etaDetails = $"{label}. Статус: {CourierStateLabel(courier)}. ETA {FormatMinute(etaMinute)}; отправка {FormatMinute(courier.OutboundDepartureMinute)} (факт), доставка ETA {FormatMinute(courier.ExpectedDeliveryMinute)}, возврат ETA {FormatMinute(courier.ExpectedReturnMinute)}.";
            if (!entries.Any(e => e.Kind == etaKind && e.Details.Contains(label, StringComparison.Ordinal)))
                entries.Add(new JournalEntry(etaMinute, 15, seq++, etaKind, etaDetails));
        }
    }

    private static void RenderCouriers(GameState state)
    {
        var table = new Table().Border(TableBorder.Simple).Title("Курьеры и очередь");
        table.AddColumn("Курьер");
        table.AddColumn("Статус");
        table.AddColumn("Назначение");
        table.AddColumn("Отправка (факт)");
        table.AddColumn("Доставка ETA/факт");
        table.AddColumn("Возврат ETA/факт");
        table.AddColumn("Подтверждение");

        if (state.Couriers.Count == 0)
        {
            table.AddRow("—", "нет данных", "—", "—", "—", "—", "—");
            AnsiConsole.Write(table);
            return;
        }

        foreach (var courier in state.Couriers.OrderBy(CourierSortKey).ThenBy(c => c.Id, StringComparer.Ordinal))
        {
            table.AddRow(
                Markup.Escape(courier.Id),
                Markup.Escape(CourierStateLabel(courier)),
                Markup.Escape(CourierAssignmentLabel(courier)),
                Markup.Escape(FormatMinute(courier.OutboundDepartureMinute > 0 ? courier.OutboundDepartureMinute : null)),
                Markup.Escape(CourierDeliveryCell(state, courier)),
                Markup.Escape(CourierReturnCell(state, courier)),
                Markup.Escape(CourierConfirmationLabel(courier)));
        }

        AnsiConsole.Write(table);
    }

    private static string DescribeOrder(GameState state, string orderId, string status)
    {
        var order = state.Orders.FirstOrDefault(o => o.Id == orderId);
        if (order is null)
            return $"Приказ {orderId}: {status}.";
        var parts = new List<string>
        {
            $"Приказ {orderId}",
            $"кому {order.ToUnitId}",
            $"намерение {order.Intent}",
            status
        };
        if (!string.IsNullOrWhiteSpace(order.TargetZoneId)) parts.Add($"цель {order.TargetZoneId}");
        if (order.IssuedAt > 0) parts.Add($"отдан {FormatClock(order.IssuedAt)}");
        return string.Join(", ", parts) + ".";
    }

    private static string DescribeReportDelivered(GameState state, ReportDelivered delivered)
    {
        var report = delivered.Payload
            ?? state.DeliveredReports.FirstOrDefault(r => r.Id == delivered.ReportId)
            ?? state.InTransitReports.FirstOrDefault(r => r.Id == delivered.ReportId);
        if (report is null)
            return $"Донесение {delivered.ReportId} доставлено получателю {delivered.UnitId ?? "—"} (факт {FormatClock(delivered.Minute)}).";

        var details = $"Донесение {report.Id} от {report.SourceId}, тип {report.Kind}. " +
                      $"Минута события {FormatMinute(report.EventMinute)}; минута доставки {FormatMinute(report.DeliveryMinute)} (факт).";
        if (!string.IsNullOrWhiteSpace(report.Text))
            details += $" {Trim(report.Text, 120)}";
        return details;
    }

    private static string DescribeInTransitReport(GameState state, Report report)
    {
        var text = $"Донесение {report.Id} от {report.SourceId}, тип {report.Kind}. " +
                   $"Минута события {FormatMinute(report.EventMinute)}; ожидаемая доставка {FormatMinute(report.DeliveryMinute)} (ETA). Ещё не получено.";
        if (!string.IsNullOrWhiteSpace(report.Text))
            text += $" {Trim(report.Text, 100)}";
        return text;
    }

    private static string DescribeQueryAnswered(QueryAnswered qa)
    {
        if (qa.Report is null)
            return $"Источник {qa.SourceId}: ответ получен (содержимое недоступно).";

        var r = qa.Report;
        var text = $"Источник {r.SourceId}, тип {r.Kind}. Минута события {FormatMinute(r.EventMinute)}; минута доставки {FormatMinute(r.DeliveryMinute)} (факт); уверенность {r.Confidence}.";
        if (!string.IsNullOrWhiteSpace(r.Text))
            text += $" {Trim(r.Text, 120)}";
        return text;
    }

    private static string DescribeCourierReturned(GameState state, CourierReturned returned)
    {
        var courier = state.Couriers.FirstOrDefault(c => c.Id == returned.CourierId);
        if (courier is null)
            return $"Курьер {returned.CourierId} вернулся (факт {FormatClock(returned.Minute)}). Подтверждение доставки получено.";
        return $"Курьер {returned.CourierId} вернулся (факт {FormatClock(returned.Minute)}), задание: {CourierAssignmentLabel(courier)}. Подтверждение доставки получено.";
    }

    private static string DescribeCourierLostKind(GameState state, CourierLost lost)
    {
        return string.Equals(lost.Leg, "Outbound", StringComparison.OrdinalIgnoreCase)
            ? "Потеря курьера (путь туда)"
            : "Курьер не вернулся (обратный путь)";
    }

    private static string DescribeCourierLost(GameState state, CourierLost lost)
    {
        var courier = state.Couriers.FirstOrDefault(c => c.Id == lost.CourierId);
        if (string.Equals(lost.Leg, "Outbound", StringComparison.OrdinalIgnoreCase))
        {
            var assignment = courier is null ? "задание неизвестно" : CourierAssignmentLabel(courier);
            var prefix = courier is not null && courier.IsQuery ? "Запрос не доставлен" : "Сообщение/приказ не доставлен";
            return $"Курьер {lost.CourierId}, {assignment}. {prefix}: курьер потерян на пути туда, факт {FormatClock(lost.Minute)}.";
        }

        var retAssignment = courier is null ? "задание неизвестно (курьер заменён/недоступен в снимке)" : CourierAssignmentLabel(courier);
        return $"Курьер {lost.CourierId}, {retAssignment}. Курьер потерян на обратном пути, факт {FormatClock(lost.Minute)}. Подтверждение доставки не получено (deliveryConfirmed=false); это не означает, что приказ или сообщение не было доставлено.";
    }

    private static string CourierAssignmentLabel(Courier courier)
    {
        if (!string.IsNullOrWhiteSpace(courier.AssignedOrderId))
            return $"приказ {courier.AssignedOrderId}";
        if (!string.IsNullOrWhiteSpace(courier.AssignedQueryId))
            return $"запрос {courier.AssignedQueryId}";
        return "нет задания";
    }

    private static string CourierStateLabel(Courier courier)
    {
        if (courier.State == CourierState.Lost && courier.LostOnOutbound)
            return "потерян на доставке (приказ/запрос не доставлен)";
        if (courier.State == CourierState.Lost && courier.LostOnReturn)
            return "не вернулся (обратный путь)";
        return courier.State switch
        {
            CourierState.Idle => "свободен",
            CourierState.Delivering => "в пути (доставка)",
            CourierState.Returning => "в пути обратно",
            CourierState.Returned => "вернулся",
            CourierState.Lost => "потерян",
            _ => courier.State.ToString()
        };
    }

    private static string CourierDeliveryCell(GameState state, Courier courier)
    {
        if (courier.ExpectedDeliveryMinute is null)
            return "—";
        if (courier.LostOnOutbound)
            return $"ETA {FormatMinute(courier.ExpectedDeliveryMinute)}, факт: потеря на пути туда";
        if (courier.State is CourierState.Returning or CourierState.Returned || courier.LostOnReturn ||
            state.EventLog.Any(e => e is OrderDelivered od && od.OrderId == courier.AssignedOrderId && od.Minute == courier.ExpectedDeliveryMinute))
            return $"ETA {FormatMinute(courier.ExpectedDeliveryMinute)}, факт {FormatMinute(courier.ExpectedDeliveryMinute)}";
        return $"ETA {FormatMinute(courier.ExpectedDeliveryMinute)}";
    }

    private static string CourierReturnCell(GameState state, Courier courier)
    {
        if (courier.ExpectedReturnMinute is null)
            return "—";
        if (courier.LostOnReturn || (courier.State == CourierState.Lost && courier.LostOnReturn))
            return $"ETA {FormatMinute(courier.ExpectedReturnMinute)}, факт: не вернулся (обратный путь)";
        if (courier.State == CourierState.Returned || state.EventLog.Any(e => e is CourierReturned cr && cr.CourierId == courier.Id))
            return $"ETA {FormatMinute(courier.ExpectedReturnMinute)}, факт {FormatMinute(courier.ExpectedReturnMinute)}";
        return $"ETA {FormatMinute(courier.ExpectedReturnMinute)}";
    }

    private static string CourierConfirmationLabel(Courier courier)
    {
        if (courier.LostOnOutbound)
            return "нет (доставка не состоялась)";
        if (courier.LostOnReturn)
            return "не получено (deliveryConfirmed=false), доставка могла состояться";
        if (courier.DeliveryConfirmed || courier.State == CourierState.Returned)
            return "получено (deliveryConfirmed=true)";
        if (courier.State == CourierState.Idle && courier.AssignedOrderId is null && courier.AssignedQueryId is null)
            return "—";
        return "ожидается";
    }

    private static int CourierSortKey(Courier courier) => courier.State switch
    {
        CourierState.Delivering => 0,
        CourierState.Returning => 1,
        CourierState.Lost => 2,
        CourierState.Returned => 3,
        CourierState.Idle => 4,
        _ => 5
    };

    private static string DescribeGeneric(GameEvent evt) => evt switch
    {
        CamouflageRevealed cr => $"Подразделение {cr.UnitId}, причина: {cr.Reason}.",
        EdgeDestroyed ed => $"Ребро {ed.EdgeId}" + (ed.ZoneId is null ? "" : $", зона {ed.ZoneId}") + ".",
        GuardRelieved gr => $"Смена охраны: {gr.OldUnitId} -> {gr.NewUnitId}.",
        ZoneControlChanged zc => $"Зона {zc.ZoneId}, контроль: {zc.ControllingFaction?.ToString() ?? "нет"}.",
        PoorControlEntered pc => $"Подразделение {pc.UnitId}, причина: {pc.Reason}.",
        PoorControlExited pc => $"Подразделение {pc.UnitId}: плохое управление снято.",
        PendingCasualties p => $"Подразделение {p.UnitId}, потери {p.Amount} (ожидают применения).",
        _ => evt.ToString() ?? evt.GetType().Name
    };

    private static string FormatClock(int minute) => $"{minute / 60:00}:{minute % 60:00}";

    private static string FormatMinute(int? minute) => minute is null ? "—" : $"{minute.Value} ({FormatClock(minute.Value)})";

    private static string FormatMinute(int minute) => $"{minute} ({FormatClock(minute)})";

    private static string Trim(string value, int max) => value.Length <= max ? value : value[..max] + "…";

    private sealed record JournalEntry(int Minute, int Phase, int Sequence, string Kind, string Details);
}