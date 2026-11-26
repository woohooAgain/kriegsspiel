using Kriegsspiel.Core.Contracts;

namespace Kriegsspiel.Core.Ai;

public sealed class DeterministicRandomSource : IRandomSource
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;
    private const ulong Mix1 = 0xBF58476D1CE4E5B9UL;
    private const ulong Mix2 = 0x94D049BB133111EBUL;

    private long _state;

    public DeterministicRandomSource(int seed)
    {
        Seed = seed;
        _state = unchecked((long)(ulong)(uint)seed);
    }

    public int Seed { get; }

    public long State => _state;

    public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

    public double NextDouble() => (NextUInt64() >> 11) * (1.0 / (1UL << 53));

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        ulong range = (ulong)((long)maxExclusive - minInclusive);
        if (range == 1)
            return minInclusive;
        ulong threshold = unchecked(0UL - range) % range;
        ulong value;
        do
        {
            value = NextUInt64();
        }
        while (value < threshold);
        return minInclusive + (int)(value % range);
    }

    public bool Chance(double probability)
    {
        if (probability <= 0) return false;
        if (probability >= 1) return true;
        return NextDouble() < probability;
    }

    public void Restore(long state) => _state = state;

    public void Reset() => _state = unchecked((long)(ulong)(uint)Seed);

    private ulong NextUInt64()
    {
        ulong z = unchecked((ulong)_state + Gamma);
        _state = unchecked((long)z);
        z = (z ^ (z >> 30)) * Mix1;
        z = (z ^ (z >> 27)) * Mix2;
        return z ^ (z >> 31);
    }
}
