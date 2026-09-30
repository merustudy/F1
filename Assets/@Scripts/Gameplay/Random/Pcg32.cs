using System;

namespace F1.Gameplay
{
    /// <summary>
    /// PCG32 (XSH-RR 64/32). The only source of randomness in gameplay results.
    /// The generator is fully described by <see cref="State"/> and <see cref="Increment"/>,
    /// so it can be saved and restored exactly.
    /// </summary>
    public sealed class Pcg32
    {
        const ulong Multiplier = 6364136223846793005UL;

        ulong _state;
        readonly ulong _increment;

        public Pcg32(ulong seed, ulong stream)
        {
            _state = 0UL;
            _increment = (stream << 1) | 1UL;
            NextUInt();
            _state = unchecked(_state + seed);
            NextUInt();
        }

        Pcg32(ulong state, ulong increment, bool restored)
        {
            _state = state;
            _increment = increment;
        }

        public ulong State => _state;
        public ulong Increment => _increment;

        public static Pcg32 Restore(ulong state, ulong increment)
        {
            if ((increment & 1UL) == 0UL)
            {
                throw new ArgumentException("A PCG32 increment is always odd.", nameof(increment));
            }

            return new Pcg32(state, increment, true);
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _increment);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rotation = (int)(old >> 59);
            return (xorShifted >> rotation) | (xorShifted << ((-rotation) & 31));
        }

        /// <summary>Uniform integer in [0, bound). Rejection sampling removes modulo bias.</summary>
        public int NextInt(int bound)
        {
            if (bound <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(bound), bound, "Bound must be positive.");
            }

            uint range = (uint)bound;
            uint threshold = (uint)((0x100000000UL - range) % range);
            while (true)
            {
                uint value = NextUInt();
                if (value >= threshold)
                {
                    return (int)(value % range);
                }
            }
        }

        /// <summary>Uniform integer in [minInclusive, maxInclusive].</summary>
        public int NextInt(int minInclusive, int maxInclusive)
        {
            if (maxInclusive < minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxInclusive), "Max is below min.");
            }

            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }
    }

    /// <summary>Stream ids keep independent uses of randomness from disturbing each other.</summary>
    public static class RngStream
    {
        public const ulong Map = 1UL;
        public const ulong Reward = 2UL;
        /// <summary>Death rolls.</summary>
        public const ulong Battle = 3UL;
        /// <summary>Retreat rolls. Separate so that retreat attempts never change death rolls.</summary>
        public const ulong Input = 4UL;
    }

    /// <summary>Derives child seeds: run seed -> expedition seed -> battle seed.</summary>
    public static class SeedDeriver
    {
        public static ulong Derive(ulong seed, string purpose, int index)
        {
            if (purpose == null)
            {
                throw new ArgumentNullException(nameof(purpose));
            }

            ulong tag = unchecked(Fnv1a(purpose) + (ulong)(uint)index * 0x9E3779B97F4A7C15UL);
            return Mix(seed ^ Mix(tag));
        }

        static ulong Fnv1a(string text)
        {
            ulong hash = 14695981039346656037UL;
            foreach (char c in text)
            {
                hash ^= c;
                hash = unchecked(hash * 1099511628211UL);
            }

            return hash;
        }

        /// <summary>SplitMix64 finalizer.</summary>
        static ulong Mix(ulong value)
        {
            unchecked
            {
                value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
                value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
                return value ^ (value >> 31);
            }
        }
    }
}
