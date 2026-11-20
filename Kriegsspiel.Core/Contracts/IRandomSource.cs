namespace Kriegsspiel.Core.Contracts;

public interface IRandomSource
{
    int Seed { get; }
    long State { get; }

    double NextDouble();
    int NextInt(int minInclusive, int maxExclusive);
    void Restore(long state);
    void Reset();

    int Next(int minInclusive, int maxExclusive) => NextInt(minInclusive, maxExclusive);

    int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

    void SetState(long state) => Restore(state);

    bool Chance(double probability)
    {
        if (probability <= 0) return false;
        if (probability >= 1) return true;
        return NextDouble() < probability;
    }
}
