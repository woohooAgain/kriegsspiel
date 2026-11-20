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
    public bool Conflicting => Versions.Select(v => v.Value).Distinct().Count() > 1;

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

        var zoneId = FactValue(report, "zoneId") ?? FactValue(report, "zone");
        var factKey = string.IsNullOrWhiteSpace(report.FactKey)
            ? (zoneId is null ? "report:" + report.Id : "zone:" + zoneId)
            : report.FactKey;
        var value = FactValue(report, "value") ?? report.Text;

        if (zoneId is null && factKey.StartsWith("zone:", StringComparison.Ordinal))
            zoneId = factKey["zone:".Length..];

        if (!Facts.TryGetValue(factKey, out var claims))
        {
            claims = new List<KnowledgeClaim>();
            Facts[factKey] = claims;
        }

        var matching = claims.FirstOrDefault(c => string.Equals(c.Value, value, StringComparison.Ordinal));
        if (matching is not null)
        {
            if (!string.Equals(matching.SourceId, report.SourceId, StringComparison.Ordinal))
                matching.Confidence = Math.Min(3, matching.Confidence + 1);
            matching.DeliveryMinute = Math.Max(matching.DeliveryMinute, report.DeliveryMinute);
            matching.EventMinute = report.EventMinute;
        }
        else
        {
            foreach (var other in claims)
            {
                other.Confidence = Math.Max(1, other.Confidence - 1);
            }

            var isFirstClaim = claims.Count == 0;
            var newClaim = new KnowledgeClaim
            {
                FactKey = factKey,
                Value = value,
                Confidence = isFirstClaim ? Math.Clamp(report.Confidence, 0, 3) : Math.Max(1, report.Confidence - 1),
                DeliveryMinute = report.DeliveryMinute,
                EventMinute = report.EventMinute,
                SourceId = report.SourceId
            };
            claims.Add(newClaim);
        }

        if (zoneId is not null)
            SyncZone(zoneId, claims, report);

        LastUpdatedMinute = LastUpdatedMinute is null
            ? report.ArrivesAt
            : Math.Max(LastUpdatedMinute.Value, report.ArrivesAt);
    }

    public void Apply(IEnumerable<Report> reports)
    {
        ArgumentNullException.ThrowIfNull(reports);
        foreach (var report in reports)
            Apply(report);
    }

    public static bool IsConfidenceInRange(int value) => value is >= 0 and <= 3;

    private static string? FactValue(Report report, string key) =>
        report.Facts.TryGetValue(key, out var value) ? value : null;

    private void SyncZone(string zoneId, List<KnowledgeClaim> claims, Report report)
    {
        var zone = GetOrCreate(zoneId);
        zone.Versions.Clear();
        zone.Versions.AddRange(claims);
        zone.Confidence = claims.Count == 0 ? 0 : claims.Max(c => c.Confidence);
        zone.DeliveryMinute = report.ArrivesAt;
        zone.LastReportMinute = Math.Max(zone.LastReportMinute, report.ArrivesAt);
        if (zone.EventMinute is null || report.EventMinute >= zone.EventMinute.Value)
            zone.EventMinute = report.EventMinute;

        var hasOccupied = claims.Any(c => !c.IsEmptyClaim);
        zone.Presence = claims.Count == 0
            ? KnowledgePresence.NoData
            : hasOccupied ? KnowledgePresence.Occupied : KnowledgePresence.Empty;

        if (!hasOccupied)
        {
            zone.ReportedOwner = null;
            zone.ReportedFaction = null;
            return;
        }

        var primary = claims.First(c => !c.IsEmptyClaim);
        zone.ReportedOwner = primary.Value;
        zone.ReportedFaction = Enum.TryParse<Faction>(primary.Value, true, out var faction) ? faction : null;
    }
}
