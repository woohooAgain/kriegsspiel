using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class ContactGeometryResolver
{
    public ContactGeometry Resolve(
        Unit attacker,
        Unit defender,
        Zone zone,
        IReadOnlyDictionary<string, Zone> zones,
        bool attackerSpottedFirst = false)
    {
        var defenderAttention = defender.AttentionEdgeId;
        var attackerMove = attacker.MoveEdgeId;

        if (attackerSpottedFirst)
            defenderAttention = attackerMove ?? defenderAttention;

        bool shares = SharesAdjacentZone(zone, zones, attackerMove, defenderAttention);
        var classification = Classify(attackerMove, defenderAttention, shares, zone, zones);

        return new ContactGeometry
        {
            Minute = 0,
            ZoneId = zone.Id,
            AttackerUnitId = attacker.Id,
            DefenderUnitId = defender.Id,
            MoveEdgeId = attackerMove,
            AttentionEdgeId = defenderAttention,
            Classification = classification,
            FirstContact = attackerSpottedFirst
        };
    }

    public static ContactClass Classify(
        string? moveEdgeId,
        string? attentionEdgeId,
        bool sharesAdjacentZone,
        Zone zone,
        IReadOnlyDictionary<string, Zone> zones)
    {
        if (string.IsNullOrWhiteSpace(moveEdgeId) || string.IsNullOrWhiteSpace(attentionEdgeId))
            return ContactClass.Front;

        if (string.Equals(moveEdgeId, attentionEdgeId, StringComparison.Ordinal))
            return ContactClass.Front;

        if (sharesAdjacentZone || SharesAdjacentZone(zone, zones, moveEdgeId, attentionEdgeId))
            return ContactClass.Flank;

        if (IsOppositeOrNoCommonNeighbor(zone, zones, moveEdgeId, attentionEdgeId))
            return ContactClass.Rear;

        return ContactClass.Rear;
    }

    public static bool SharesAdjacentZone(
        Zone zone,
        IReadOnlyDictionary<string, Zone> zones,
        string? moveEdgeId,
        string? attentionEdgeId)
    {
        if (string.IsNullOrWhiteSpace(moveEdgeId) || string.IsNullOrWhiteSpace(attentionEdgeId))
            return false;

        if (!zones.TryGetValue(moveEdgeId, out var moveNeighbor) ||
            !zones.TryGetValue(attentionEdgeId, out var attentionNeighbor))
            return false;

        if (moveNeighbor.TravelMinutes.ContainsKey(attentionEdgeId) ||
            attentionNeighbor.TravelMinutes.ContainsKey(moveEdgeId))
            return true;

        foreach (var candidate in moveNeighbor.TravelMinutes.Keys)
        {
            if (string.Equals(candidate, zone.Id, StringComparison.Ordinal))
                continue;
            if (attentionNeighbor.TravelMinutes.ContainsKey(candidate))
                return true;
        }

        return false;
    }

    public static bool IsOppositeOrNoCommonNeighbor(
        Zone zone,
        IReadOnlyDictionary<string, Zone> zones,
        string? moveEdgeId,
        string? attentionEdgeId)
    {
        if (string.IsNullOrWhiteSpace(moveEdgeId) || string.IsNullOrWhiteSpace(attentionEdgeId))
            return false;

        if (!zones.TryGetValue(moveEdgeId, out var moveNeighbor) ||
            !zones.TryGetValue(attentionEdgeId, out var attentionNeighbor))
            return true;

        bool hasCommonOtherThanCurrent = moveNeighbor.TravelMinutes.Keys
            .Where(id => !string.Equals(id, zone.Id, StringComparison.Ordinal))
            .Any(id => attentionNeighbor.TravelMinutes.ContainsKey(id));

        if (hasCommonOtherThanCurrent)
            return false;

        var neighbors = zone.TravelMinutes.Keys.OrderBy(id => id, StringComparer.Ordinal).ToList();
        int moveIndex = neighbors.IndexOf(moveEdgeId);
        int attentionIndex = neighbors.IndexOf(attentionEdgeId);
        if (neighbors.Count >= 2 && moveIndex >= 0 && attentionIndex >= 0)
        {
            int distance = Math.Abs(moveIndex - attentionIndex);
            int wrapped = neighbors.Count - distance;
            if (Math.Min(distance, wrapped) * 2 == neighbors.Count)
                return true;
        }

        return true;
    }
}
