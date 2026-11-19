namespace Kriegsspiel.Core.Domain;

public sealed class ZoneKnowledge
{
    public bool OccupancyKnown { get; set; }
    public string? ReportedOwner { get; set; }
    public int Confidence { get; set; }
    public int LastReportMinute { get; set; }
}

public sealed class PlayerKnowledge
{
    public Dictionary<string, ZoneKnowledge> Zones { get; } = new();
}