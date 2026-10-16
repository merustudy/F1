using System;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// Levels and experience (Slice C stage 27, Docs/Design/05_Mercenary_Growth.md §2): the return settlement gives every surviving party
    /// member the same experience (the floors reached and a clear, BalanceData), a mercenary climbs the ladder of LevelData as its experience
    /// meets each level's ExpToNext, and a level adds its MaxHpBonusPercent to the job's maximum HP when the mercenary leaves on an
    /// expedition. A dead mercenary's level dies with it; a hired one starts at level 1.
    /// </summary>
    public static class LevelRules
    {
        public const int FirstLevel = 1;

        /// <summary>The maximum HP (hundredths) a mercenary of a job has at a level: the job's with the level's bonus.</summary>
        public static int MaxHpOf(StaticData data, string jobId, int level)
        {
            JobData job = data.Jobs.Get(jobId);
            return (int)((long)job.MaxHp * (100 + data.LevelAt(level).MaxHpBonusPercent) / 100);
        }

        /// <summary>What an ended expedition gives each survivor: ExpPerFloor for each map floor reached, ExpClear for a clear.</summary>
        public static int ExpFor(StaticData data, ExpeditionState expedition)
        {
            return data.Balance.ExpPerFloor * RunRules.FloorsReached(data, expedition)
                + (expedition.Result == ExpeditionResult.Cleared ? data.Balance.ExpClear : 0);
        }

        /// <summary>
        /// Adds experience to a mercenary and climbs as many levels as it meets. At the last level the experience stays 0: nothing is kept
        /// beyond it. Returns how many levels were gained.
        /// </summary>
        public static int Gain(StaticData data, MercenaryState mercenary, int exp)
        {
            if (exp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exp), exp, "Experience cannot be negative.");
            }

            if (mercenary.Level >= data.MaxLevel)
            {
                mercenary.Exp = 0;
                return 0;
            }

            int gained = 0;
            mercenary.Exp += exp;
            while (mercenary.Level < data.MaxLevel && mercenary.Exp >= data.LevelAt(mercenary.Level).ExpToNext)
            {
                mercenary.Exp -= data.LevelAt(mercenary.Level).ExpToNext;
                mercenary.Level++;
                gained++;
            }

            if (mercenary.Level >= data.MaxLevel)
            {
                mercenary.Exp = 0;
            }

            return gained;
        }

        /// <summary>The experience a mercenary needs to leave its level, or 0 at the last level.</summary>
        public static int ExpToNext(StaticData data, MercenaryState mercenary)
        {
            return data.LevelAt(mercenary.Level).ExpToNext;
        }
    }
}
