using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;
using Spectre.Console;

namespace Kriegsspiel.Console.Commands;

public sealed class OrderCommand
{
    public Order? Build(GameState state)
    {
        var unit = AnsiConsole.Prompt(
            new SelectionPrompt<Unit>()
                .Title("Кому приказ?")
                .UseConverter(u => $"{u.Name} ({u.LocationZoneId})")
                .AddChoices(state.Units.Values.Where(u => !u.Id.StartsWith("ENEMY_"))));

        var intent = AnsiConsole.Prompt(
            new SelectionPrompt<OrderIntent>()
                .Title("Что?")
                .AddChoices(Enum.GetValues<OrderIntent>()));

        string? target = null;
        if (intent is OrderIntent.Seize or OrderIntent.Move or OrderIntent.Recon)
            target = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("Куда?")
                    .AddChoices(state.Zones.Keys));

        var reportBy = AnsiConsole.Prompt(
            new TextPrompt<int?>("Донести к минуте (Enter — пропустить):")
                .AllowEmpty());

        var draft = new Order
        {
            ToUnitId = unit.Id,
            Intent = intent,
            TargetZoneId = target,
            ReportBy = reportBy,
            IssuedAt = state.Clock.Now
        };
        var order = new Order
        {
            Id = draft.Id,
            ToUnitId = draft.ToUnitId,
            Intent = draft.Intent,
            TargetZoneId = draft.TargetZoneId,
            NotLaterThan = draft.NotLaterThan,
            Constraints = draft.Constraints,
            ReportBy = draft.ReportBy,
            IssuedAt = draft.IssuedAt,
            AmbiguityScore = AmbiguityScorer.Score(draft)
        };
        AnsiConsole.MarkupLine(
            $"[green]Приказ {order.Id} принят. Неоднозначность: {order.AmbiguityScore:0.00}[/]");
        return order;
    }
}