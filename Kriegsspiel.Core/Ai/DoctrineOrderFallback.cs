using Kriegsspiel.Core.Domain;
using Kriegsspiel.Core.Orders;

namespace Kriegsspiel.Core.Ai;

public interface IDoctrineOrderFallback
{
    Order Build(AiWorldView view, int minute, int seed, string? difficultyProfileId = null);
}

public sealed class TemplateDoctrineOrderFallback : IDoctrineOrderFallback
{
    public Order Build(AiWorldView view, int minute, int seed, string? difficultyProfileId = null)
    {
        ArgumentNullException.ThrowIfNull(view);
        if (!view.IsValid)
            throw new ArgumentException("World view contains data outside allowed knowledge.", nameof(view));
        if (view.Units.Count == 0)
            throw new ArgumentException("Doctrine fallback needs at least one own unit.", nameof(view));

        var rng = new DeterministicRandomSource(AiStableId.Int("doctrine", seed, minute, view.ViewerFaction));
        var units = view.Units.OrderBy(u => u.Id, StringComparer.Ordinal).ToArray();
        var unit = units[rng.NextInt(units.Length)];
        var zones = view.Zones.OrderBy(z => z.Id, StringComparer.Ordinal).ToArray();
        var target = zones.Length == 0 ? null : zones[rng.NextInt(zones.Length)];
        var aggressive = difficultyProfileId is not null &&
            difficultyProfileId.Contains("aggressive", StringComparison.OrdinalIgnoreCase);
        var intent = aggressive ? OrderIntent.Seize : target is null ? OrderIntent.Hold : OrderIntent.Occupy;
        var deadline = minute + 60;

        return new Order
        {
            Id = AiStableId.Create("doctrine", seed, minute, unit.Id),
            ToUnitId = unit.Id,
            Intent = intent,
            TargetZoneId = target?.Id,
            NotLaterThan = deadline,
            ReportBy = deadline + 15,
            IssuedAt = minute,
            IssuerId = $"HQ_{view.ViewerFaction}",
            OrderVersion = 1,
            MovementMode = MovementMode.March,
            OriginalText = $"{intent} {target?.Id ?? unit.LocationZoneId} by {deadline}"
        };
    }
}
