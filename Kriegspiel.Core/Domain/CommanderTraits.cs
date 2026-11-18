namespace Kriegsspiel.Core.Domain;

public sealed record CommanderTraits(
    int Initiative,
    int Caution,
    int Literalism,
    int ReportHonesty);