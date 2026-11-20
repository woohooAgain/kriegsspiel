using System.Globalization;
using Kriegsspiel.Core.Content;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Ai;

public static class NarrationFallback
{
    public static Report Build(Report fact, ReportTemplates templates, int seed)
    {
        ArgumentNullException.ThrowIfNull(fact);
        ArgumentNullException.ThrowIfNull(templates);

        var vars = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["eventMinute"] = fact.EventMinute.ToString(CultureInfo.InvariantCulture),
            ["deliveryMinute"] = fact.DeliveryMinute.ToString(CultureInfo.InvariantCulture),
            ["sourceId"] = fact.SourceId,
            ["confidence"] = fact.Confidence.ToString(CultureInfo.InvariantCulture),
            ["factKey"] = fact.FactKey
        };
        foreach (var (key, value) in fact.Facts)
            vars[key] = value;

        return WithText(fact, templates.Pick(fact.Kind.ToString(), vars, new Random(seed)));
    }

    public static Report WithText(Report fact, string text)
    {
        ArgumentNullException.ThrowIfNull(fact);

        var report = Report.Create(
            fact.EventMinute,
            fact.DeliveryMinute,
            fact.SourceId,
            fact.Confidence,
            fact.Kind,
            text,
            fact.FactKey,
            fact.SourceFaction,
            fact.ReportedBy,
            new Dictionary<string, string>(fact.Facts));
        report.Id = fact.Id;
        return report;
    }
}
