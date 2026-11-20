using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class OrderBuilder
{
    private const int MaxBranches = 3;

    public Order? Build(GameState state)
    {
        var template = SelectTemplate(state);
        var recipient = SelectRecipient(state, template);
        if (recipient is null) return null;

        var intent = SelectIntent(template);
        var target = SelectTarget(state, recipient, intent, template);
        var notLaterThan = PromptMinute("К какому времени выполнить (минута, Enter — пропустить):", template?.NotLaterThan);
        var constraints = SelectConstraints(template);
        var reportBy = PromptMinute("Когда донести (минута, Enter — пропустить):", template?.ReportBy);
        var movementMode = SelectMovementMode(template);
        var branches = BuildBranches(template);

        var draft = new Order
        {
            ToUnitId = recipient.Id,
            Intent = intent,
            TargetZoneId = target.ZoneId,
            TargetObjectId = target.ObjectId,
            TargetUnitId = target.UnitId,
            TargetEdgeId = target.EdgeId,
            NotLaterThan = notLaterThan,
            Constraints = constraints,
            ReportBy = reportBy,
            IssuedAt = state.Clock.Now,
            MovementMode = movementMode,
            Branches = branches,
            OrderVersion = 1
        };

        var context = BuildScoringContext(state, recipient, draft);
        var breakdown = AmbiguityScorer.Breakdown(draft, context);
        AnsiConsole.MarkupLine($"[yellow]Предпросмотр неоднозначности A:[/] {breakdown.Total:0.00} (цель {breakdown.MissingTarget:0.00}, формулировка {breakdown.Vague:0.00}, дедлайн {breakdown.MissingDeadline:0.00}, срок донесения {breakdown.MissingReportBy:0.00}, ветви {breakdown.Branches:0.00})");
        RenderBranchPreview(branches);

        var order = new Order
        {
            Id = draft.Id,
            ToUnitId = draft.ToUnitId,
            Intent = draft.Intent,
            TargetZoneId = draft.TargetZoneId,
            TargetObjectId = draft.TargetObjectId,
            TargetUnitId = draft.TargetUnitId,
            TargetEdgeId = draft.TargetEdgeId,
            NotLaterThan = draft.NotLaterThan,
            Constraints = new List<OrderConstraint>(draft.Constraints),
            ReportBy = draft.ReportBy,
            IssuedAt = draft.IssuedAt,
            AmbiguityScore = breakdown.Total,
            MovementMode = draft.MovementMode,
            Branches = new List<OrderBranch>(draft.Branches),
            OrderVersion = draft.OrderVersion
        };
        AnsiConsole.MarkupLine($"[green]Приказ {order.Id} принят. Неоднозначность: {order.AmbiguityScore:0.00}[/]");
        return order;
    }

    private static Order? SelectTemplate(GameState state)
    {
        var reusable = state.Orders
            .Where(o => state.Units.TryGetValue(o.ToUnitId, out var u) && u.Faction == Faction.Blue)
            .OrderByDescending(o => o.IssuedAt)
            .Take(20)
            .ToList();
        if (reusable.Count == 0) return null;

        var choices = new List<string> { "Новый приказ" };
        choices.AddRange(reusable.Select(o => FormatTemplateOption(state, o)));
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Создать новый приказ или использовать шаблон?")
                .AddChoices(choices));
        if (selected == "Новый приказ") return null;
        var index = choices.IndexOf(selected) - 1;
        return index >= 0 && index < reusable.Count ? reusable[index] : null;
    }

    private static string FormatTemplateOption(GameState state, Order order)
    {
        var unitName = state.Units.TryGetValue(order.ToUnitId, out var u) ? u.Name : order.ToUnitId;
        var target = order.TargetZoneId ?? order.TargetObjectId ?? order.TargetUnitId ?? order.TargetEdgeId ?? "-";
        return $"{order.Id} | {unitName} | {order.Intent} -> {target} | t={order.IssuedAt}";
    }

    private static Unit? SelectRecipient(GameState state, Order? template)
    {
        var units = state.Units.Values
            .Where(u => u.Faction == Faction.Blue)
            .OrderBy(u => u.Name)
            .ToList();
        if (units.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Нет доступных подразделений для приказа.[/]");
            return null;
        }

        var prompt = new SelectionPrompt<Unit>()
            .Title("Кому приказ?")
            .UseConverter(u => $"{u.Name} ({u.Id}, зона {u.LocationZoneId})")
            .AddChoices(units);
        if (template is not null)
        {
            var preselected = units.FirstOrDefault(u => u.Id == template.ToUnitId);
            if (preselected is not null)
            {
                AnsiConsole.MarkupLine($"[grey]Шаблон: адресат {preselected.Name}[/]");
            }
        }
        var selected = AnsiConsole.Prompt(prompt);
        if (template is not null)
        {
            var preferred = units.FirstOrDefault(u => u.Id == template.ToUnitId);
            if (preferred is not null && AnsiConsole.Confirm($"Использовать адресата из шаблона: {preferred.Name}?", true))
                return preferred;
        }
        return selected;
    }

    private static OrderIntent SelectIntent(Order? template)
    {
        var prompt = new SelectionPrompt<OrderIntent>()
            .Title("Что? (намерение)")
            .AddChoices(Enum.GetValues<OrderIntent>());
        if (template is not null)
            AnsiConsole.MarkupLine($"[grey]Шаблон: намерение {template.Intent}[/]");
        var selected = AnsiConsole.Prompt(prompt);
        if (template is not null && AnsiConsole.Confirm($"Оставить намерение из шаблона: {template.Intent}?", false))
            return template.Intent;
        return selected;
    }

    private sealed record TargetSelection(string? ZoneId, string? ObjectId, string? UnitId, string? EdgeId);

    private static TargetSelection SelectTarget(GameState state, Unit recipient, OrderIntent intent, Order? template)
    {
        var kinds = TargetKindsFor(intent);
        if (kinds.Count == 0)
            return new TargetSelection(template?.TargetZoneId, template?.TargetObjectId, template?.TargetUnitId, template?.TargetEdgeId);

        var options = new List<string> { "Без цели" };
        options.AddRange(kinds);
        var kind = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"Куда? (цель для {intent})")
                .AddChoices(options));

        var selection = kind switch
        {
            "Зона" => new TargetSelection(SelectZone(state, template), null, null, null),
            "Объект" => new TargetSelection(null, SelectObject(state, template), null, null),
            "Подразделение" => new TargetSelection(null, null, SelectKnownUnit(state, recipient, template), null),
            "Ребро" => new TargetSelection(null, null, null, SelectEdge(state, template)),
            _ => new TargetSelection(null, null, null, null)
        };

        if (template is not null && HasAnyTarget(template) && !HasAnyTarget(selection) &&
            AnsiConsole.Confirm("Взять цель из шаблона?", true))
        {
            return new TargetSelection(template.TargetZoneId, template.TargetObjectId, template.TargetUnitId, template.TargetEdgeId);
        }

        if (template is not null && HasAnyTarget(template) && HasAnyTarget(selection) == false)
        {
        }

        return selection;
    }

    private static bool HasAnyTarget(Order order) =>
        !string.IsNullOrWhiteSpace(order.TargetZoneId) ||
        !string.IsNullOrWhiteSpace(order.TargetObjectId) ||
        !string.IsNullOrWhiteSpace(order.TargetUnitId) ||
        !string.IsNullOrWhiteSpace(order.TargetEdgeId);

    private static bool HasAnyTarget(TargetSelection selection) =>
        !string.IsNullOrWhiteSpace(selection.ZoneId) ||
        !string.IsNullOrWhiteSpace(selection.ObjectId) ||
        !string.IsNullOrWhiteSpace(selection.UnitId) ||
        !string.IsNullOrWhiteSpace(selection.EdgeId);

    private static List<string> TargetKindsFor(OrderIntent intent) => intent switch
    {
        OrderIntent.Seize or OrderIntent.Occupy or OrderIntent.Move or OrderIntent.Advance or OrderIntent.Recon or OrderIntent.Destroy or OrderIntent.Block or OrderIntent.Check
            => new List<string> { "Зона", "Объект", "Подразделение", "Ребро" },
        OrderIntent.Hold or OrderIntent.Reserve or OrderIntent.Guard
            => new List<string> { "Зона", "Объект" },
        OrderIntent.Support
            => new List<string> { "Зона", "Подразделение" },
        OrderIntent.Withdraw
            => new List<string> { "Зона", "Ребро" },
        OrderIntent.Delay or OrderIntent.Escort
            => new List<string> { "Объект", "Подразделение", "Зона" },
        _ => new List<string> { "Зона", "Объект", "Подразделение", "Ребро" }
    };

    private static string? SelectZone(GameState state, Order? template)
    {
        var zones = state.Zones.Values.OrderBy(z => z.Id).ToList();
        var choices = zones.Select(z => $"{z.Id} — {z.Name}").ToList();
        if (template?.TargetZoneId is { } tz && zones.Any(z => z.Id == tz))
            AnsiConsole.MarkupLine($"[grey]Шаблон: зона {Markup.Escape(tz)}[/]");
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Выберите зону")
                .AddChoices(choices));
        var id = selected.Split('—', 2)[0].Trim();
        return id;
    }

    private static string? SelectObject(GameState state, Order? template)
    {
        var objects = state.Zones.Values
            .SelectMany(z => z.Objects.Select(o => new { Zone = z, Object = o }))
            .OrderBy(x => x.Zone.Id).ThenBy(x => x.Object.Id)
            .ToList();
        if (objects.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Известных объектов на карте нет.[/]");
            return template?.TargetObjectId;
        }
        if (template?.TargetObjectId is { } to)
            AnsiConsole.MarkupLine($"[grey]Шаблон: объект {Markup.Escape(to)}[/]");
        var choices = objects.Select(x => $"{x.Object.Id} ({x.Object.Kind}, зона {x.Zone.Id})").ToList();
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Выберите объект (сценарные данные карты)")
                .AddChoices(choices));
        return selected.Split(' ', 2)[0].Trim();
    }

    private static string? SelectKnownUnit(GameState state, Unit recipient, Order? template)
    {
        var known = GetPlayerKnownTargetUnits(state, recipient);
        if (known.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Нет подразделений, доступных как цель (свои или известные из донесений).[/]");
            return template?.TargetUnitId;
        }
        if (template?.TargetUnitId is { } tu)
            AnsiConsole.MarkupLine($"[grey]Шаблон: подразделение {Markup.Escape(tu)}[/]");
        var selected = AnsiConsole.Prompt(
            new SelectionPrompt<Unit>()
                .Title("Выберите подразделение (только известные игроку)")
                .UseConverter(u => $"{u.Name} ({u.Id}, {u.Faction}, зона {u.LocationZoneId})")
                .AddChoices(known));
        return selected.Id;
    }

    private static List<Unit> GetPlayerKnownTargetUnits(GameState state, Unit recipient)
    {
        var result = new Dictionary<string, Unit>(StringComparer.Ordinal);
        foreach (var unit in state.Units.Values.Where(u => u.Faction == Faction.Blue))
            result[unit.Id] = unit;

        var knownIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var zone in state.Knowledge.Zones.Values)
        {
            if (!string.IsNullOrWhiteSpace(zone.ReportedOwner))
                knownIds.Add(zone.ReportedOwner);
            foreach (var version in zone.Versions)
                if (!string.IsNullOrWhiteSpace(version.Value))
                    knownIds.Add(version.Value);
        }
        foreach (var claims in state.Knowledge.Facts.Values)
        foreach (var claim in claims)
            if (!string.IsNullOrWhiteSpace(claim.Value))
                knownIds.Add(claim.Value);

        foreach (var id in knownIds)
        {
            if (!state.Units.TryGetValue(id, out var unit)) continue;
            if (unit.Faction == Faction.Blue) { result[id] = unit; continue; }
            if (unit.IsEnemyOf(Faction.Blue))
                result[id] = unit;
        }

        if (result.ContainsKey(recipient.Id))
            result.Remove(recipient.Id);

        return result.Values.OrderBy(u => u.Faction).ThenBy(u => u.Name).ToList();
    }

    private static string? SelectEdge(GameState state, Order? template)
    {
        var edges = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var zone in state.Zones.Values)
        {
            foreach (var neighbor in zone.TravelMinutes.Keys)
            {
                if (!state.Zones.ContainsKey(neighbor)) continue;
                var a = string.CompareOrdinal(zone.Id, neighbor) <= 0 ? zone.Id : neighbor;
                var b = string.CompareOrdinal(zone.Id, neighbor) <= 0 ? neighbor : zone.Id;
                edges.Add($"{a}<->{b}");
            }
        }
        if (edges.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]Рёбра на карте отсутствуют.[/]");
            return template?.TargetEdgeId;
        }
        if (template?.TargetEdgeId is { } te)
            AnsiConsole.MarkupLine($"[grey]Шаблон: ребро {Markup.Escape(te)}[/]");
        return AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("Выберите ребро (направление/соединение)")
                .AddChoices(edges));
    }

    private static int? PromptMinute(string title, int? templateValue)
    {
        var prompt = new TextPrompt<string>(title).AllowEmpty();
        var input = AnsiConsole.Prompt(prompt);
        if (string.IsNullOrWhiteSpace(input))
        {
            if (templateValue is not null && AnsiConsole.Confirm($"Оставить значение из шаблона: {templateValue}?", true))
                return templateValue;
            return null;
        }
        if (int.TryParse(input.Trim(), out var minute) && minute >= 0)
            return minute;
        AnsiConsole.MarkupLine("[red]Некорректное значение минуты, поле оставлено пустым.[/]");
        if (templateValue is not null && AnsiConsole.Confirm($"Оставить значение из шаблона: {templateValue}?", true))
            return templateValue;
        return null;
    }

    private static List<OrderConstraint> SelectConstraints(Order? template)
    {
        var prompt = new MultiSelectionPrompt<OrderConstraint>()
            .Title("Ограничения (закрытый список, пробел — выбрать, Enter — подтвердить)")
            .NotRequired()
            .InstructionsText("[grey](Пробел — отметить, Enter — готово, можно без ограничений)[/]")
            .AddChoices(Enum.GetValues<OrderConstraint>());
        var selected = AnsiConsole.Prompt(prompt).ToList();
        if (template is not null && template.Constraints.Count > 0 && selected.Count == 0 &&
            AnsiConsole.Confirm($"Взять ограничения из шаблона ({template.Constraints.Count})?", false))
        {
            return new List<OrderConstraint>(template.Constraints);
        }
        return selected;
    }

    private static MovementMode SelectMovementMode(Order? template)
    {
        var prompt = new SelectionPrompt<MovementMode>()
            .Title("Режим передвижения")
            .AddChoices(Enum.GetValues<MovementMode>());
        var selected = AnsiConsole.Prompt(prompt);
        if (template is not null && AnsiConsole.Confirm($"Оставить режим из шаблона: {template.MovementMode}?", false))
            return template.MovementMode;
        return selected;
    }

    private static List<OrderBranch> BuildBranches(Order? template)
    {
        var branches = new List<OrderBranch>();
        if (template is not null && template.Branches.Count > 0 &&
            AnsiConsole.Confirm($"Скопировать ветви из шаблона ({template.Branches.Count}) как основу?", true))
        {
            foreach (var b in template.Branches.Take(MaxBranches))
                branches.Add(b);
            RenderBranchPreview(branches);
        }

        while (true)
        {
            if (!AnsiConsole.Confirm(branches.Count == 0 ? "Добавить ветвь приказа?" : "Добавить ещё ветвь?", false))
                break;
            if (branches.Count >= MaxBranches)
            {
                AnsiConsole.MarkupLine("[red]Нельзя добавить 4-ю ветвь: максимум 3 ветви на приказ.[/]");
                break;
            }

            var condition = AnsiConsole.Prompt(
                new SelectionPrompt<BranchCondition>()
                    .Title("ЕСЛИ (условие)")
                    .AddChoices(Enum.GetValues<BranchCondition>()));
            var thenAction = AnsiConsole.Prompt(
                new SelectionPrompt<BranchAction>()
                    .Title("ТО (действие)")
                    .AddChoices(Enum.GetValues<BranchAction>()));
            BranchAction? elseAction = null;
            if (AnsiConsole.Confirm("Добавить ИНАЧЕ?", false))
            {
                elseAction = AnsiConsole.Prompt(
                    new SelectionPrompt<BranchAction>()
                        .Title("ИНАЧЕ (действие)")
                        .AddChoices(Enum.GetValues<BranchAction>()));
            }
            string? dropped = null;
            if (thenAction == BranchAction.DropConstraint || elseAction == BranchAction.DropConstraint)
            {
                var name = AnsiConsole.Prompt(new TextPrompt<string>("Название снимаемого ограничения:").AllowEmpty());
                dropped = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
                if (string.IsNullOrWhiteSpace(dropped))
                {
                    AnsiConsole.MarkupLine("[red]Для действия 'снять ограничение' нужно указать название. Ветвь не добавлена.[/]");
                    continue;
                }
            }

            var branch = new OrderBranch(condition, thenAction, elseAction, dropped);
            branches.Add(branch);
            AnsiConsole.MarkupLine($"[green]Ветвь добавлена ({branches.Count}/{MaxBranches}).[/]");
            AnsiConsole.MarkupLine($"[grey]Предпросмотр:[/] {FormatBranch(branch)}");

            if (branches.Count >= MaxBranches)
            {
                AnsiConsole.MarkupLine("[yellow]Достигнут лимит: 3 ветви. Добавление 4-й ветви недоступно.[/]");
                break;
            }
        }

        return branches;
    }

    private static void RenderBranchPreview(IReadOnlyList<OrderBranch> branches)
    {
        if (branches.Count == 0) return;
        AnsiConsole.MarkupLine("[yellow]Ветви (предпросмотр если X → сделать Y):[/]");
        foreach (var branch in branches)
            AnsiConsole.MarkupLine($"  - {Markup.Escape(FormatBranch(branch))}");
    }

    private static string FormatBranch(OrderBranch branch)
    {
        var text = $"ЕСЛИ {branch.Condition} → ТО {branch.Then}";
        if (branch.Else is { } elseAction)
            text += $" ИНАЧЕ {elseAction}";
        if (!string.IsNullOrWhiteSpace(branch.DroppedConstraintName))
            text += $" (снять: {branch.DroppedConstraintName})";
        return text;
    }

    private static OrderScoringContext BuildScoringContext(GameState state, Unit recipient, Order draft)
    {
        var knownZoneIds = state.Knowledge.Zones
            .Where(kv => kv.Value.HasData)
            .Select(kv => kv.Key)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        return new OrderScoringContext(
            CurrentZoneId: recipient.LocationZoneId,
            ReserveZoneId: null,
            GuardObjectId: draft.TargetObjectId,
            ResolvedTargetId: null,
            KnownZoneIds: knownZoneIds);
    }
}
