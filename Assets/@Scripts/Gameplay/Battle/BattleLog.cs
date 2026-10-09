using System.Collections.Generic;

namespace F1.Gameplay
{
    /// <summary>Why one mercenary died, as read from the event log.</summary>
    public sealed class DeathCause
    {
        public UnitRef Unit;

        /// <summary>Battle time the mercenary last dropped to 0 HP. 0 when it entered the battle at 0 HP.</summary>
        public int DogEnteredMs;

        public int DiedMs;

        /// <summary>Who dealt the last hit. None for burn and storm.</summary>
        public UnitRef LastHitSource;

        /// <summary>Item id, or BattleEvent.CauseBurn / CauseStorm.</summary>
        public string LastHitCause;

        public int DeathChancePercent;
        public int DeathRoll;

        /// <summary>True when hits during the grace period broke it; false when the grace period ran out.</summary>
        public bool GraceWasBroken;

        /// <summary>Hits that broke the grace. 0 when it was not broken.</summary>
        public int GraceHits;

        /// <summary>
        /// True when the fatigue collapse at death's door killed it (the Collapsed event right before Died, C = 1): no hit and no
        /// death roll did. A mercenary who entered the battle at 0 HP may never have been hit, so LastHitCause can be null.
        /// </summary>
        public bool Collapsed;
    }

    public static class BattleLog
    {
        /// <summary>The deaths of party members, in the order they happened.</summary>
        public static List<DeathCause> PartyDeaths(IReadOnlyList<BattleEvent> events)
        {
            var deaths = new List<DeathCause>();
            var open = new Dictionary<int, DeathCause>();

            foreach (BattleEvent e in events)
            {
                if (e.Target.IsNone || e.Target.Side != BattleSide.Party)
                {
                    continue;
                }

                if (!open.TryGetValue(e.Target.Index, out DeathCause cause))
                {
                    cause = new DeathCause { Unit = e.Target, LastHitSource = UnitRef.None };
                    open.Add(e.Target.Index, cause);
                }

                switch (e.Kind)
                {
                    case BattleEventKind.Damaged:
                        cause.LastHitSource = e.Source;
                        cause.LastHitCause = e.Id;
                        break;
                    case BattleEventKind.DogEntered:
                        cause.DogEnteredMs = e.TimeMs;
                        cause.GraceWasBroken = false;
                        cause.GraceHits = 0;
                        break;
                    case BattleEventKind.GraceBroken:
                        cause.GraceWasBroken = true;
                        cause.GraceHits = e.A;
                        break;
                    case BattleEventKind.DeathRolled:
                        cause.DeathChancePercent = e.A;
                        cause.DeathRoll = e.B;
                        break;
                    case BattleEventKind.Collapsed:
                        // The collapse sends the unit to death's door (C = 0, DogEntered follows) or kills it there (C = 1, Died follows).
                        cause.Collapsed = e.C == 1;
                        break;
                    case BattleEventKind.Died:
                        cause.DiedMs = e.TimeMs;
                        deaths.Add(cause);
                        open.Remove(e.Target.Index);
                        break;
                }
            }

            return deaths;
        }

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
