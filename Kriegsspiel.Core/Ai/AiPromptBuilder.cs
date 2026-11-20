using System.Text.Json;
using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Ai;

public static class AiPromptBuilder
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public static string BuildOrderPrompt(AiWorldView view, string instructionText, string schemaText)
    {
        ArgumentNullException.ThrowIfNull(view);
        ArgumentNullException.ThrowIfNull(instructionText);
        ArgumentNullException.ThrowIfNull(schemaText);
        if (!view.IsValid)
            throw new ArgumentException("World view contains data outside allowed knowledge.", nameof(view));

        var context = JsonSerializer.Serialize(new
        {
            minute = view.Minute,
            viewerFaction = view.ViewerFaction.ToString(),
            units = view.Units.Select(u => new
            {
                id = u.Id,
                name = u.Name,
                type = u.Type.ToString(),
                locationZoneId = u.LocationZoneId,
                faction = u.Faction.ToString()
            }),
            zones = view.Zones.Select(z => new
            {
                id = z.Id,
                presence = z.Presence.ToString(),
                reportedOwner = z.ReportedOwner,
                confidence = z.Confidence,
                eventMinute = z.EventMinute,
                deliveryMinute = z.DeliveryMinute
            })
        }, Options);

        return instructionText + "\nCONTEXT:\n" + context + "\nSCHEMA:\n" + schemaText;
    }

    public static string BuildNarrationPrompt(Report fact, string instructionText, string schemaText)
    {
        ArgumentNullException.ThrowIfNull(fact);
        ArgumentNullException.ThrowIfNull(instructionText);
        ArgumentNullException.ThrowIfNull(schemaText);

        var context = JsonSerializer.Serialize(new
        {
            schemaVersion = AiSchemas.Version,
            report = new
            {
                id = fact.Id,
                kind = fact.Kind.ToString(),
                eventMinute = fact.EventMinute,
                deliveryMinute = fact.DeliveryMinute,
                sourceId = fact.SourceId,
                confidence = fact.Confidence,
                factKey = fact.FactKey,
                facts = fact.Facts.OrderBy(pair => pair.Key, StringComparer.Ordinal)
                    .Select(pair => new { key = pair.Key, value = pair.Value })
            }
        }, Options);

        return instructionText + "\nFACTS:\n" + context + "\nSCHEMA:\n" + schemaText;
    }
}
