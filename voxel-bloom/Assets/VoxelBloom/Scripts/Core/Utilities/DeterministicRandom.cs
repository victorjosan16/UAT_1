namespace VoxelBloom.Core
{
    /// <summary>Mulberry32 PRNG, bit-identical to tools/voxel.mjs rng().</summary>
    public sealed class DeterministicRandom
    {
        uint state;
        public DeterministicRandom(uint seed) { state = seed; }
        public DeterministicRandom(int seed) { state = unchecked((uint)seed); }

        public double NextDouble()
        {
            unchecked
            {
                state += 0x6D2B79F5u;
                uint t = state;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                return (t ^ (t >> 14)) / 4294967296.0;
            }
        }

        /// <summary>Uniform integer in [0, maxExclusive).</summary>
        public int Next(int maxExclusive) => maxExclusive <= 0 ? 0 : (int)(NextDouble() * maxExclusive);
    }
}
