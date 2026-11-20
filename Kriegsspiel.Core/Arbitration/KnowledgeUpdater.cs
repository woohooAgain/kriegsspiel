using Kriegsspiel.Core.Contracts;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Arbitration;

public sealed class KnowledgeUpdater : IKnowledgeUpdater
{
    private const int MaxConfidence = 3;

    public void Apply(PlayerKnowledge knowledge, Report report)
    {
        ArgumentNullException.ThrowIfNull(knowledge);
        ArgumentNullException.ThrowIfNull(report);

        if (!report.HasRequiredFields)
            throw new ArgumentException("A report needs event minute, delivery minute, source and confidence.", nameof(report));

        var factKey = FactKey(report);
        var value = ClaimValue(report);
        var claims = Claims(knowledge, factKey);
        var matching = claims.FirstOrDefault(c => SameValue(c.Value, value));
        var conflicting = claims.Where(c => !SameValue(c.Value, value)).ToList();

        if (conflicting.Count > 0)
        {
            foreach (var claim in claims)
                claim.Confidence = Clamp(claim.Confidence - 1);
        }

        if (matching is not null)
        {
            UpdateTimestamps(matching, report);
            if (conflicting.Count == 0 && IsIndependent(matching, report))
                matching.Confidence = Clamp(matching.Confidence + 1);
        }
        else
        {
            claims.Add(new KnowledgeClaim
            {
                FactKey = factKey,
                Value = value,
                Confidence = Clamp(report.Confidence - (claims.Count > 0 ? 1 : 0)),
                DeliveryMinute = report.DeliveryMinute,
                EventMinute = report.EventMinute,
                SourceId = report.SourceId
            });
        }

        var zoneId = ZoneId(report, factKey);
        if (zoneId is not null)
            SyncZone(knowledge, zoneId, factKey, claims);

        knowledge.LastUpdatedMinute = knowledge.LastUpdatedMinute is null
            ? report.DeliveryMinute
            : Math.Max(knowledge.LastUpdatedMinute.Value, report.DeliveryMinute);
    }

    private static List<KnowledgeClaim> Claims(PlayerKnowledge knowledge, string factKey)
    {
        if (!knowledge.Facts.TryGetValue(factKey, out var claims))
        {
            claims = new List<KnowledgeClaim>();
            knowledge.Facts[factKey] = claims;
        }
        return claims;
    }

    private static string FactKey(Report report)
    {
        if (!string.IsNullOrWhiteSpace(report.FactKey))
            return report.FactKey;
        if (report.Facts.TryGetValue("factKey", out var key) && !string.IsNullOrWhiteSpace(key))
            return key;
        var zoneId = ZoneFromFacts(report);
        return zoneId is null ? "report:" + report.Id : "zone:" + zoneId;
    }

    private static string? ZoneFromFacts(Report report)
    {
        if (report.Facts.TryGetValue("zoneId", out var zoneId) && !string.IsNullOrWhiteSpace(zoneId))
            return zoneId;
        if (report.Facts.TryGetValue("zone", out var zone) && !string.IsNullOrWhiteSpace(zone))
            return zone;
        return null;
    }

    private static string? ZoneId(Report report, string factKey)
    {
        var zoneId = ZoneFromFacts(report);
        if (zoneId is not null)
            return zoneId;
        return factKey.StartsWith("zone:", StringComparison.Ordinal) ? factKey["zone:".Length..] : null;
    }

    private static string ClaimValue(Report report)
    {
        if (report.Facts.TryGetValue("value", out var value) && !string.IsNullOrEmpty(value))
            return value;
        return string.IsNullOrWhiteSpace(report.Text) ? KnowledgeClaim.EmptyValue : report.Text;
    }

    private static bool SameValue(string left, string right) =>
        string.Equals(left, right, StringComparison.OrdinalIgnoreCase);

    private static bool IsIndependent(KnowledgeClaim claim, Report report) =>
        !string.Equals(claim.SourceId, report.SourceId, StringComparison.Ordinal) ||
        (claim.EventMinute.HasValue && report.EventMinute != claim.EventMinute.Value);

    private static void UpdateTimestamps(KnowledgeClaim claim, Report report)
    {
        claim.DeliveryMinute = Math.Max(claim.DeliveryMinute, report.DeliveryMinute);
        if (claim.EventMinute is null || report.EventMinute > claim.EventMinute.Value)
            claim.EventMinute = report.EventMinute;
    }

    private static int Clamp(int confidence) => Math.Clamp(confidence, 0, MaxConfidence);

    private static void SyncZone(
        PlayerKnowledge knowledge,
        string zoneId,
        string factKey,
        IReadOnlyList<KnowledgeClaim> factClaims)
    {
        var zone = knowledge.GetOrCreate(zoneId);
        var claims = new List<KnowledgeClaim>(zone.Versions);

        foreach (var claim in factClaims)
            Merge(claims, claim);

        foreach (var (key, list) in knowledge.Facts)
        {
            if (!string.Equals(key, factKey, StringComparison.Ordinal) &&
                !string.Equals(key, "zone:" + zoneId, StringComparison.Ordinal))
                continue;
            foreach (var claim in list)
                Merge(claims, claim);
        }

        zone.Versions.Clear();
        zone.Versions.AddRange(claims);
        zone.Confidence = claims.Count == 0 ? 0 : claims.Max(c => c.Confidence);
        zone.DeliveryMinute = claims.Count == 0 ? null : claims.Max(c => c.DeliveryMinute);
        zone.LastReportMinute = Math.Max(zone.LastReportMinute, zone.DeliveryMinute ?? 0);

        var events = claims.Where(c => c.EventMinute.HasValue).Select(c => c.EventMinute!.Value).ToList();
        zone.EventMinute = events.Count == 0 ? null : events.Min();

        var hasOccupied = claims.Any(c => !c.IsEmptyClaim);
        zone.Presence = claims.Count == 0
            ? KnowledgePresence.NoData
            : hasOccupied ? KnowledgePresence.Occupied : KnowledgePresence.Empty;

        var primary = claims.FirstOrDefault(c => !c.IsEmptyClaim);
        zone.ReportedOwner = primary?.Value;
        zone.ReportedFaction = primary is not null &&
                               Enum.TryParse<Faction>(primary.Value, true, out var faction)
            ? faction
            : null;
    }

    private static void Merge(List<KnowledgeClaim> claims, KnowledgeClaim claim)
    {
        var index = claims.FindIndex(c =>
            ReferenceEquals(c, claim) ||
            (string.Equals(c.FactKey, claim.FactKey, StringComparison.Ordinal) && SameValue(c.Value, claim.Value)));
        if (index < 0)
            claims.Add(claim);
        else
            claims[index] = claim;
    }
}
