using System.Collections.Generic;

namespace F1.Gameplay
{
    public static class BattleLog
    {
        /// <summary>
        /// A 64-bit digest of an event log (FNV-1a over every field). Two battles with the same digest
        /// played out identically; determinism tests and replay checks compare it.
        /// </summary>
        public static ulong Hash(IReadOnlyList<BattleEvent> events)
        {
            ulong hash = 14695981039346656037UL;
            foreach (BattleEvent e in events)
            {
                hash = Add(hash, e.TimeMs);
                hash = Add(hash, (int)e.Kind);
                hash = Add(hash, (int)e.Source.Side);
                hash = Add(hash, e.Source.Index);
                hash = Add(hash, (int)e.Target.Side);
                hash = Add(hash, e.Target.Index);
                hash = Add(hash, e.A);
                hash = Add(hash, e.B);
                hash = Add(hash, e.C);
                if (e.Id != null)
                {
                    foreach (char c in e.Id)
                    {
                        hash = Add(hash, c);
                    }
                }

                hash = Add(hash, -1);
            }

            return hash;
        }

        static ulong Add(ulong hash, int value)
        {
            unchecked
            {
                for (int shift = 0; shift < 32; shift += 8)
                {
                    hash ^= (byte)(value >> shift);
                    hash *= 1099511628211UL;
                }

                return hash;
            }
        }
    }
}
