using System;

// Deterministic 64-bit RNG with a snapshot-friendly ulong state. Inherits from
// System.Random so it can be passed anywhere the codebase already takes a
// Random — IntentResolver, DeckFlowResolver, CardUpgradeRules, etc.
//
// All Random virtuals that the codebase actually calls (Next / Next(max) /
// Next(min,max) / NextDouble / Sample) are overridden to draw from the
// xorshift64* stream. The base-class internal state goes unused.
public sealed class GameRng : Random
{
    private ulong _state;

    public GameRng()
        : this(DefaultSeed())
    {
    }

    public GameRng(ulong state)
        : base(0)
    {
        _state = state == 0UL ? 1UL : state;
    }

    public ulong State
    {
        get => _state;
        set => _state = value == 0UL ? 1UL : value;
    }

    private static ulong DefaultSeed()
    {
        var t = (ulong)Environment.TickCount64;
        var mix = t * 6364136223846793005UL + 1442695040888963407UL;
        return mix == 0UL ? 1UL : mix;
    }

    private ulong NextRaw()
    {
        var x = _state;
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        _state = x;
        return x * 2685821657736338717UL;
    }

    protected override double Sample()
    {
        // 53-bit mantissa for an unbiased [0,1).
        return (NextRaw() >> 11) / (double)(1UL << 53);
    }

    public override int Next()
    {
        return (int)(NextRaw() & 0x7FFFFFFFUL);
    }

    public override int Next(int maxValue)
    {
        if (maxValue <= 0)
        {
            return 0;
        }

        // 32-bit Lemire-style unbiased range reduction.
        return (int)((NextRaw() >> 32) * (uint)maxValue >> 32);
    }

    public override int Next(int minValue, int maxValue)
    {
        if (maxValue <= minValue)
        {
            return minValue;
        }

        return minValue + Next(maxValue - minValue);
    }

    public override double NextDouble()
    {
        return Sample();
    }
}
