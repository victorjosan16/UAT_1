namespace TollRoadTycoon.Core
{
    /// <summary>Small deterministic PRNG (xorshift64*) so simulations and tests are reproducible.</summary>
    public sealed class SimRandom
    {
        private ulong _state;

        public SimRandom(int seed)
        {
            _state = (ulong)(uint)seed * 0x9E3779B97F4A7C15UL + 0x2545F4914F6CDD1DUL;
            if (_state == 0) _state = 0x2545F4914F6CDD1DUL;
        }

        public ulong NextULong()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * 0x2545F4914F6CDD1DUL;
        }

        /// <summary>Uniform double in [0, 1).</summary>
        public double NextDouble() => (NextULong() >> 11) * (1.0 / (1UL << 53));

        /// <summary>Uniform int in [minInclusive, maxExclusive).</summary>
        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) return minInclusive;
            return minInclusive + (int)(NextULong() % (ulong)(maxExclusive - minInclusive));
        }
    }
}
