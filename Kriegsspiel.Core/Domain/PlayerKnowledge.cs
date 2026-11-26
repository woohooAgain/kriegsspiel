namespace Kriegsspiel.Core.Domain;

public enum KnowledgePresence
{
    NoData = 0,
    Empty = 1,
    Occupied = 2
}

public sealed class KnowledgeClaim
{
    public const string EmptyValue = "EMPTY";

    public string FactKey { get; init; } = "";
    public string Value { get; init; } = "";
    public int DeliveryMinute { get; set; }
    public int? EventMinute { get; set; }
    public string SourceId { get; init; } = "";

    private int _confidence;

    public int Confidence
    {
        get => _confidence;
        set => _confidence = Math.Clamp(value, 0, 3);
    }

    public bool IsEmptyClaim =>
        string.IsNullOrEmpty(Value) ||
        string.Equals(Value, EmptyValue, StringComparison.OrdinalIgnoreCase);
}

public sealed class ZoneKnowledge
{
    private int _confidence;
    private int? _deliveryMinute;

    public KnowledgePresence Presence { get; set; } = KnowledgePresence.NoData;
    public string? ReportedOwner { get; set; }
    public Faction? ReportedFaction { get; set; }
    public int LastReportMinute { get; set; }
    public int? EventMinute { get; set; }
    public List<KnowledgeClaim> Versions { get; } = new();

    public bool HasData => Presence != KnowledgePresence.NoData;
    public bool IsEmpty => Presence == KnowledgePresence.Empty;
    public bool Conflicting => Versions.Select(v => v.Value).Distinct(StringComparer.Ordinal).Count() > 1;

    public bool OccupancyKnown
    {
        get => HasData;
        set => Presence = value
            ? (string.IsNullOrEmpty(ReportedOwner) ? KnowledgePresence.Empty : KnowledgePresence.Occupied)
            : KnowledgePresence.NoData;
    }

    public int Confidence
    {
        get => _confidence;
        set => _confidence = Math.Clamp(value, 0, 3);
    }

    public int? DeliveryMinute
    {
        get => _deliveryMinute;
        set
        {
            _deliveryMinute = value;
            if (value.HasValue)
                LastReportMinute = value.Value;
        }
    }
}

public sealed class PlayerKnowledge
{
    public Dictionary<string, ZoneKnowledge> Zones { get; } = new();
    public Dictionary<string, List<KnowledgeClaim>> Facts { get; } = new();
    public int? LastUpdatedMinute { get; set; }

    public bool HasZoneData(string zoneId) =>
        Zones.TryGetValue(zoneId, out var knowledge) && knowledge.HasData;

    public ZoneKnowledge GetOrCreate(string zoneId)
    {
        if (!Zones.TryGetValue(zoneId, out var knowledge))
        {
            knowledge = new ZoneKnowledge();
            Zones[zoneId] = knowledge;
        }
        return knowledge;
    }

    public void Apply(Report report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var zoneId = ZoneId(report);
        var factKey = string.IsNullOrWhiteSpace(report.FactKey)
            ? zoneId is null ? "report:" + report.Id : "zone:" + zoneId
            : report.FactKey;
        var value = ClaimValue(report);

        var claim = new KnowledgeClaim
        {
            FactKey = factKey,
            Value = value,
            Confidence = report.Confidence,
            DeliveryMinute = report.DeliveryMinute,
            EventMinute = report.EventMinute,
            SourceId = report.SourceId
        };

        ClaimList(factKey).Add(claim);

        if (zoneId is not null)
            AppendZone(zoneId, claim, report);

        LastUpdatedMinute = LastUpdatedMinute is null
            ? report.DeliveryMinute
            : Math.Max(LastUpdatedMinute.Value, report.DeliveryMinute);
    }

    public void Apply(IEnumerable<Report> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        foreach (var report in reports)
            Apply(report);
    }

    public static bool IsConfidenceInRange(int value) => value is >= 1 and <= 3;

    private List<KnowledgeClaim> ClaimList(string factKey)
    {
        if (!Facts.TryGetValue(factKey, out var claims))
        {
            claims = new List<KnowledgeClaim>();
            Facts[factKey] = claims;
        }
        return claims;
    }

    private void AppendZone(string zoneId, KnowledgeClaim claim, Report report)
    {
        var zone = GetOrCreate(zoneId);
        zone.Versions.Add(claim);
        zone.Confidence = report.Confidence;
        zone.DeliveryMinute = report.DeliveryMinute;
        zone.LastReportMinute = Math.Max(zone.LastReportMinute, report.DeliveryMinute);
        zone.EventMinute = report.EventMinute;
        zone.Presence = PresenceFor(report, claim);

        if (claim.IsEmptyClaim)
        {
            zone.ReportedOwner = null;
            zone.ReportedFaction = null;
            return;
        }

        zone.ReportedOwner = claim.Value;
        zone.ReportedFaction = Enum.TryParse<Faction>(claim.Value, true, out var faction) ? faction : null;
    }

    private static KnowledgePresence PresenceFor(Report report, KnowledgeClaim claim)
    {
        if (claim.IsEmptyClaim)
            return KnowledgePresence.Empty;
        if (report.ReportedEnemyStrength is 0 && report.ReportedOwnStrength is null)
            return KnowledgePresence.Empty;
        return KnowledgePresence.Occupied;
    }

    private static string? ZoneId(Report report)
    {
        if (!string.IsNullOrWhiteSpace(report.ObservedZoneId))
            return report.ObservedZoneId;
        if (report.Facts.TryGetValue("zoneId", out var zoneId) && !string.IsNullOrWhiteSpace(zoneId))
            return zoneId;
        if (report.Facts.TryGetValue("zone", out var zone) && !string.IsNullOrWhiteSpace(zone))
            return zone;
        return report.FactKey.StartsWith("zone:", StringComparison.Ordinal)
            ? report.FactKey["zone:".Length..]
            : null;
    }

    private static string ClaimValue(Report report)
    {
        if (report.Facts.TryGetValue("value", out var value) && !string.IsNullOrEmpty(value))
            return value;
        return string.IsNullOrWhiteSpace(report.Text) ? KnowledgeClaim.EmptyValue : report.Text;
    }
}
