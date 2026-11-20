using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class ReportDto
{
    public string Id { get; init; } = "";
    public int Minute { get; init; }
    public int ArrivesAt { get; init; }
    public string FromUnitId { get; init; } = "";
    public string Text { get; init; } = "";
    public int Confidence { get; init; }
    public ReportKind Kind { get; init; }
    public Faction SourceFaction { get; init; }
    public string FactKey { get; init; } = "";
    public Dictionary<string, string> Facts { get; init; } = new();
    public string? ReportedBy { get; init; }

    public static ReportDto From(Report report) => new()
    {
        Id = report.Id,
        Minute = report.Minute,
        ArrivesAt = report.ArrivesAt,
        FromUnitId = report.FromUnitId,
        Text = report.Text,
        Confidence = report.Confidence,
        Kind = report.Kind,
        SourceFaction = report.SourceFaction,
        FactKey = report.FactKey,
        Facts = new Dictionary<string, string>(report.Facts),
        ReportedBy = report.ReportedBy
    };

    public Report ToDomain() => new()
    {
        Id = Id,
        Minute = Minute,
        ArrivesAt = ArrivesAt,
        FromUnitId = FromUnitId,
        Text = Text,
        Confidence = Confidence,
        Kind = Kind,
        SourceFaction = SourceFaction,
        FactKey = FactKey,
        Facts = new Dictionary<string, string>(Facts),
        ReportedBy = ReportedBy
    };
}
