using Kriegsspiel.Core.Contracts;

namespace Kriegsspiel.Core.Ai;

public sealed class DeterministicRandomSource : IRandomSource
{
    private long _state;

    public DeterministicRandomSource(int seed)
    {
        Seed = seed;
        _state = Mix(seed);
    }

    public int Seed { get; }

    public long State => _state;

    public int NextInt(int maxExclusive) => NextInt(0, maxExclusive);

    public double NextDouble()
    {
        _state = unchecked((long)((ulong)_state + 0x9E3779B97F4A7C15UL));
        ulong z = unchecked((ulong)_state);
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        z ^= z >> 31;
        return (z >> 11) * (1.0 / (1UL << 53));
    }

    public int NextInt(int minInclusive, int maxExclusive)
    {
        if (minInclusive >= maxExclusive)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return minInclusive + (int)(NextDouble() * (maxExclusive - minInclusive));
    }

    public void Restore(long state) => _state = state;

    public void Reset() => _state = Mix(Seed);

    private static long Mix(int seed) =>
        unchecked((long)(((ulong)(uint)seed + 0x9E3779B97F4A7C15UL) * 0xBF58476D1CE4E5B9UL));
}
