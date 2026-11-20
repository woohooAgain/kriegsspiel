namespace Kriegsspiel.Core.Domain;

public enum ContactClass
{
    Front = 0,
    Flank = 1,
    Rear = 2
}

public sealed class ContactGeometry
{
    public int Minute { get; init; }
    public string? ZoneId { get; init; }
    public string? AttackerUnitId { get; init; }
    public string? DefenderUnitId { get; init; }
    public string? MoveEdgeId { get; init; }
    public string? AttentionEdgeId { get; init; }
    public ContactClass Classification { get; init; } = ContactClass.Front;
    public bool FirstContact { get; init; }

    public double CollisionMultiplier => Classification switch
    {
        ContactClass.Flank => 1.4,
        ContactClass.Rear => 1.8,
        _ => 1.0
    };

    public int MoralePenalty => Classification switch
    {
        ContactClass.Flank => 10,
        ContactClass.Rear => 15,
        _ => 0
    };

    public static ContactClass Classify(
        string? moveEdgeId,
        string? attentionEdgeId,
        bool sharesAdjacentZone = false)
    {
        if (string.IsNullOrEmpty(moveEdgeId) || string.IsNullOrEmpty(attentionEdgeId))
            return ContactClass.Front;
        if (moveEdgeId == attentionEdgeId)
            return ContactClass.Front;
        return sharesAdjacentZone ? ContactClass.Flank : ContactClass.Rear;
    }
}
