using Kriegsspiel.Core.Domain;

namespace Kriegsspiel.Core.Persistence;

public sealed class KnowledgeClaimDto
{
    public string FactKey { get; init; } = "";
    public string Value { get; init; } = "";
    public int DeliveryMinute { get; init; }
    public int? EventMinute { get; init; }
    public string SourceId { get; init; } = "";
    public int Confidence { get; init; }

    public static KnowledgeClaimDto From(KnowledgeClaim claim) => new()
    {
        FactKey = claim.FactKey,
        Value = claim.Value,
        DeliveryMinute = claim.DeliveryMinute,
        EventMinute = claim.EventMinute,
        SourceId = claim.SourceId,
        Confidence = claim.Confidence
    };

    public KnowledgeClaim ToDomain() => new()
    {
        FactKey = FactKey,
        Value = Value,
        DeliveryMinute = DeliveryMinute,
        EventMinute = EventMinute,
        SourceId = SourceId,
        Confidence = Confidence
    };
}

public sealed class ZoneKnowledgeDto
{
    public KnowledgePresence Presence { get; init; }
    public string? ReportedOwner { get; init; }
    public Faction? ReportedFaction { get; init; }
    public int LastReportMinute { get; init; }
    public int? EventMinute { get; init; }
    public int Confidence { get; init; }
    public int? DeliveryMinute { get; init; }
    public List<KnowledgeClaimDto> Versions { get; init; } = new();

    public static ZoneKnowledgeDto From(ZoneKnowledge knowledge) => new()
    {
        Presence = knowledge.Presence,
        ReportedOwner = knowledge.ReportedOwner,
        ReportedFaction = knowledge.ReportedFaction,
        LastReportMinute = knowledge.LastReportMinute,
        EventMinute = knowledge.EventMinute,
        Confidence = knowledge.Confidence,
        DeliveryMinute = knowledge.DeliveryMinute,
        Versions = knowledge.Versions.Select(KnowledgeClaimDto.From).ToList()
    };

    public ZoneKnowledge ToDomain()
    {
        var knowledge = new ZoneKnowledge
        {
            Presence = Presence,
            ReportedOwner = ReportedOwner,
            ReportedFaction = ReportedFaction,
            EventMinute = EventMinute,
            Confidence = Confidence
        };
        foreach (var version in Versions)
            knowledge.Versions.Add(version.ToDomain());
        knowledge.DeliveryMinute = DeliveryMinute;
        knowledge.LastReportMinute = LastReportMinute;
        return knowledge;
    }
}

public sealed class PlayerKnowledgeDto
{
    public Dictionary<string, ZoneKnowledgeDto> Zones { get; init; } = new();
    public Dictionary<string, List<KnowledgeClaimDto>> Facts { get; init; } = new();
    public int? LastUpdatedMinute { get; init; }

    public static PlayerKnowledgeDto From(PlayerKnowledge knowledge) => new()
    {
        Zones = knowledge.Zones.ToDictionary(kv => kv.Key, kv => ZoneKnowledgeDto.From(kv.Value)),
        Facts = knowledge.Facts.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Select(KnowledgeClaimDto.From).ToList()),
        LastUpdatedMinute = knowledge.LastUpdatedMinute
    };

    public PlayerKnowledge ToDomain()
    {
        var knowledge = new PlayerKnowledge { LastUpdatedMinute = LastUpdatedMinute };
        foreach (var (key, value) in Zones)
            knowledge.Zones[key] = value.ToDomain();
        foreach (var (key, value) in Facts)
            knowledge.Facts[key] = value.Select(c => c.ToDomain()).ToList();
        return knowledge;
    }
}
