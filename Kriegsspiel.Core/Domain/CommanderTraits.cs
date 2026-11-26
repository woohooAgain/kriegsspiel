namespace Kriegsspiel.Core.Domain;

public sealed record CommanderTraits(
    int Initiative,
    int Caution,
    int Literalism,
    int ReportHonesty,
    int Professionalism = 1)
{
    public int Init => Initiative;
    public int Pedantry => Literalism;
    public int Honesty => ReportHonesty;

    public bool InRange =>
        IsInRange(Initiative) &&
        IsInRange(Caution) &&
        IsInRange(Literalism) &&
        IsInRange(ReportHonesty) &&
        IsInRange(Professionalism);

    public static bool IsInRange(int value) => value is >= 0 and <= 3;
}
