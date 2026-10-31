using System;

namespace Veliqora.Core.Random
{
    /// <summary>
    /// Deterministic, platform-independent PRNG (SplitMix64 seeding + xoshiro128** style output on 64-bit state).
    /// Never use System.Random for content: its sequence is not guaranteed stable across runtimes.
    /// </summary>
    public sealed class DeterministicRandom
    {
        private ulong _s0;
        private ulong _s1;

        public DeterministicRandom(ulong seed)
        {
            ulong x = seed;
            _s0 = SplitMix(ref x);
            _s1 = SplitMix(ref x);
            if (_s0 == 0 && _s1 == 0) _s1 = 0x9E3779B97F4A7C15UL;
        }

        private static ulong SplitMix(ref ulong x)
        {
            x += 0x9E3779B97F4A7C15UL;
            ulong z = x;
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }

        /// <summary>xoroshiro128+ step.</summary>
        public ulong NextULong()
        {
            ulong s0 = _s0;
            ulong s1 = _s1;
            ulong result = s0 + s1;
            s1 ^= s0;
            _s0 = RotL(s0, 24) ^ s1 ^ (s1 << 16);
            _s1 = RotL(s1, 37);
            return result;
        }

        private static ulong RotL(ulong x, int k) => (x << k) | (x >> (64 - k));

        /// <summary>Uniform integer in [minInclusive, maxExclusive) without modulo bias.</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentException("Empty range");
            ulong span = (ulong)(maxExclusive - minInclusive);
            ulong limit = ulong.MaxValue - (ulong.MaxValue % span);
            ulong r;
            do { r = NextULong(); } while (r >= limit);
            return minInclusive + (int)(r % span);
        }

        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        public void Shuffle<T>(T[] array)
        {
            for (int i = array.Length - 1; i > 0; i--)
            {
                int j = Range(0, i + 1);
                T tmp = array[i];
                array[i] = array[j];
                array[j] = tmp;
            }
        }

        /// <summary>Stable 64-bit mixing of a base seed with extra integers (level id, attempt, etc.).</summary>
        public static ulong Combine(ulong seed, long a, long b = 0)
        {
            ulong x = seed ^ ((ulong)a * 0xD6E8FEB86659FD93UL) ^ ((ulong)b * 0xA0761D6478BD642FUL);
            return SplitMix(ref x);
        }
    }
}
