using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Ai;

public sealed record AiUnitView(
    string Id,
    string Name,
    UnitType Type,
    string LocationZoneId,
    Faction Faction);

public sealed record AiZoneView(
    string Id,
    KnowledgePresence Presence,
    string? ReportedOwner,
    int Confidence,
    int? EventMinute,
    int? DeliveryMinute);

public sealed class AiWorldView
{
    public static IReadOnlySet<string> ForbiddenFields { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "strength",
        "morale",
        "readiness",
        "ammo",
        "fatigue",
        "experience",
        "concealed",
        "attentionEdgeId",
        "moveEdgeId",
        "presentUnitIds",
        "controllingFaction",
        "captured",
        "noPrisonersPolicy",
        "commandLossResilient",
        "commanderLost",
        "rngState",
        "initialStrengthSnapshot",
        "civilians",
        "cargo",
        "seed"
    };

    public Faction ViewerFaction { get; init; }

    public int Minute { get; init; }

    public IReadOnlyList<AiUnitView> Units { get; init; } = Array.Empty<AiUnitView>();

    public IReadOnlyList<AiZoneView> Zones { get; init; } = Array.Empty<AiZoneView>();

    public bool IsValid =>
        Units.All(u => u.Faction == ViewerFaction) &&
        Units.Select(u => u.Id).Distinct(StringComparer.Ordinal).Count() == Units.Count &&
        Zones.Select(z => z.Id).Distinct(StringComparer.Ordinal).Count() == Zones.Count;

    public static AiWorldView FromState(
        GameState state,
        Faction viewerFaction,
        IEnumerable<AiZoneView>? scenarioAllowedKnowledge = null)
    {
        ArgumentNullException.ThrowIfNull(state);

        var units = state.Units.Values
            .Where(u => MatchesViewer(u, viewerFaction))
            .Select(u => new AiUnitView(u.Id, u.Name, u.Type, u.LocationZoneId, viewerFaction))
            .OrderBy(u => u.Id, StringComparer.Ordinal)
            .ToArray();

        var zones = scenarioAllowedKnowledge is null
            ? state.Knowledge.Zones
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new AiZoneView(
                    pair.Key,
                    pair.Value.Presence,
                    pair.Value.ReportedOwner,
                    pair.Value.Confidence,
                    pair.Value.EventMinute,
                    pair.Value.DeliveryMinute))
                .ToArray()
            : scenarioAllowedKnowledge.OrderBy(z => z.Id, StringComparer.Ordinal).ToArray();

        return new AiWorldView
        {
            ViewerFaction = viewerFaction,
            Minute = state.Clock.Now,
            Units = units,
            Zones = zones
        };
    }

    private static bool MatchesViewer(Unit unit, Faction viewerFaction) =>
        unit.Faction == viewerFaction ||
        (viewerFaction == Faction.Red &&
         unit.Faction == Faction.Blue &&
         unit.Id.StartsWith("ENEMY_", StringComparison.Ordinal));
}
