using System.Diagnostics.CodeAnalysis;

namespace Kriegsspiel.Core.Domain;

public enum ReportKind
{
    Status = 0,
    Contact = 1,
    Request = 2,
    CombatResult = 3,
    Arrival = 4,
    Impossible = 5,
    QueryAnswer = 6,
    Silence = 7,
    Recon = 8,
    CivilianRumor = 9,
    Interrogation = 10,
    Loss = 11,
    Contradiction = 12
}

public sealed class Report
{
    private int _minute;
    private int _arrivesAt;
    private int _confidence;

    public Report()
    {
    }

    [SetsRequiredMembers]
    public Report(int eventMinute, int deliveryMinute, string sourceId, int confidence)
    {
        Minute = eventMinute;
        ArrivesAt = deliveryMinute;
        FromUnitId = sourceId;
        Text = "";
        Confidence = confidence;
    }

    public string Id { get; set; } = Guid.NewGuid().ToString("N")[..8];

    public int Minute
    {
        get => _minute;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _minute = value;
        }
    }

    public int ArrivesAt
    {
        get => _arrivesAt;
        set
        {
            if (value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _arrivesAt = value;
        }
    }

    public required string FromUnitId { get; set; }
    public required string Text { get; set; }

    public int Confidence
    {
        get => _confidence;
        set
        {
            if (value is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(value));
            _confidence = value;
        }
    }

    public ReportKind Kind { get; set; }
    public string? ObservedZoneId { get; set; }
    public int? ReportedOwnStrength { get; set; }
    public int? ReportedEnemyStrength { get; set; }

    public int EventMinute
    {
        get => Minute;
        set => Minute = value;
    }

    public int DeliveryMinute
    {
        get => ArrivesAt;
        set => ArrivesAt = value;
    }

    public string SourceId
    {
        get => FromUnitId;
        set => FromUnitId = value;
    }

    public Faction SourceFaction { get; set; } = Faction.Blue;
    public string FactKey { get; set; } = "";
    public Dictionary<string, string> Facts { get; init; } = new();
    public string? ReportedBy { get; set; }

    public string? ReportByUnitId
    {
        get => ReportedBy;
        set => ReportedBy = value;
    }

    public bool HasRequiredFields =>
        !string.IsNullOrWhiteSpace(FromUnitId) &&
        Minute >= 0 &&
        ArrivesAt >= 0 &&
        Confidence is >= 1 and <= 3;

    public static Report Create(
        int eventMinute,
        int deliveryMinute,
        string sourceId,
        int confidence,
        ReportKind kind = ReportKind.Status,
        string text = "",
        string? factKey = null,
        Faction sourceFaction = Faction.Blue,
        string? reportedBy = null,
        Dictionary<string, string>? facts = null,
        string? observedZoneId = null,
        int? reportedOwnStrength = null,
        int? reportedEnemyStrength = null)
    {
        if (string.IsNullOrWhiteSpace(sourceId))
            throw new ArgumentException("A report needs a source.", nameof(sourceId));

        var report = new Report
        {
            Minute = eventMinute,
            ArrivesAt = deliveryMinute,
            FromUnitId = sourceId,
            Text = text,
            Kind = kind,
            Confidence = confidence,
            FactKey = factKey ?? "",
            SourceFaction = sourceFaction,
            ReportedBy = reportedBy,
            ObservedZoneId = observedZoneId,
            ReportedOwnStrength = reportedOwnStrength,
            ReportedEnemyStrength = reportedEnemyStrength
        };

        if (facts is null) return report;
        foreach (var (key, value) in facts)
            report.Facts[key] = value;
        return report;
    }
}
