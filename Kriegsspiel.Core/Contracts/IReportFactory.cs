namespace Kriegsspiel.Core.Contracts;

using Kriegsspiel.Core.Domain;

public interface IReportFactory
{
    Report Create(
        int eventMinute,
        int deliveryMinute,
        string sourceId,
        int confidence,
        ReportKind kind = ReportKind.Status,
        string text = "",
        string? factKey = null,
        Faction sourceFaction = Faction.Blue,
        string? reportedBy = null,
        Dictionary<string, string>? facts = null);

    Report Create(int eventMinute, int deliveryMinute, string sourceId, int confidence, string text) =>
        Create(eventMinute, deliveryMinute, sourceId, confidence, ReportKind.Status, text);

    Report CreateWithHonesty(
        int eventMinute,
        int deliveryMinute,
        Unit source,
        int confidence,
        ReportKind kind = ReportKind.Status,
        string text = "",
        string? factKey = null,
        Dictionary<string, string>? facts = null,
        string? reportedBy = null,
        int? honesty = null,
        string? observedZoneId = null,
        IReadOnlyCollection<Unit>? observedEnemies = null);
}
