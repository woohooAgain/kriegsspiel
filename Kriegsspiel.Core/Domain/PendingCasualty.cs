namespace Kriegsspiel.Core.Domain;

public sealed class PendingCasualty
{
    public PendingCasualty()
    {
    }

    public PendingCasualty(int minute, string unitId, int amount)
    {
        Minute = minute;
        UnitId = unitId;
        Amount = amount;
    }

    public int Minute { get; set; }
    public string UnitId { get; set; } = "";
    public string? ZoneId { get; set; }
    public int? CombatTickMinute { get; set; }
    public bool Applied { get; set; }

    private int _amount;

    public int Amount
    {
        get => _amount;
        set => _amount = Math.Max(0, value);
    }

    public bool IsDueAt(int minute) => !Applied && minute >= Minute;
}
