using System;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// Levels and experience (Slice C stage 27, Docs/Design/05_Mercenary_Growth.md §2): the return settlement gives every surviving party
    /// member the same experience (the floors reached and a clear, BalanceData), and a mercenary climbs the ladder of LevelData one level at a
    /// time when the player levels it up in the barracks (Slice D stage 35): its experience meets the level's ExpToNext, the level grows the
    /// stats by the job's grades (stage 34: the level's own HP bonus is gone). A dead mercenary's level dies with it; a hired one starts at level 1.
    /// </summary>
    public static class LevelRules
    {
        public const int FirstLevel = 1;

        /// <summary>The maximum HP (hundredths) a mercenary of a job has with the job's base stats.</summary>
        public static int MaxHpOf(StaticData data, string jobId)
        {
            return MaxHpOf(data, jobId, StatRules.BaseOf(data, jobId));
        }

        /// <summary>
        /// The maximum HP (hundredths) a mercenary has with its stats (2026-10-20 스탯 개정, Docs/Design/05 §2): what its constitution gives
        /// (<see cref="StatRules.MaxHpOf"/>; a job has no HP of its own any more) with what its traits add (stage 37), in percent, rounded down, never
        /// under 1. A level reaches it through the constitution it grows (stage 34 took the level's own bonus out). The job id names whose it is.
        /// </summary>
        public static int MaxHpOf(StaticData data, string jobId, StatBlock stats, TraitBonus traits = null)
        {
            data.Jobs.Get(jobId);
            int percent = Math.Max(1, 100 + (traits?.MaxHpPercent ?? 0));
            return Math.Max(1, (int)((long)StatRules.MaxHpOf(data.Balance, stats.Con) * percent / 100));
        }

        /// <summary>
        /// Grows a mercenary's stats for the levels it just gained (Slice D stage 34, Docs/Design/05_Mercenary_Growth.md §2; stage 35 grows one at a
        /// time, <see cref="LevelUp"/>): for each level, every
        /// stat gains its job's growth grade ÷ 100 points and, when the remainder is above 0, one more by that percent chance (one roll of the
        /// stream), up to StatMax. A job without grades grows nothing and rolls nothing. Returns what each stat gained.
        /// </summary>
        public static StatBlock Grow(StaticData data, MercenaryState mercenary, int levels, Pcg32 rng)
        {
            StatBlock grades = data.Jobs.Get(mercenary.JobId).Growth;
            StatBlock stats = mercenary.Stats ?? StatRules.BaseOf(data, mercenary.JobId);
            StatBlock before = stats;
            if (grades != null)
            {
                for (int level = 0; level < levels; level++)
                {
                    foreach (Stat stat in StatBlock.All)
                    {
                        int grade = grades[stat];
                        int points = grade / 100;
                        if (grade % 100 > 0 && rng.NextInt(100) < grade % 100)
                        {
                            points++;
                        }

                        stats = stats.With(stat, Math.Min(data.Balance.StatMax, stats[stat] + points));
                    }
                }
            }

            mercenary.Stats = stats;
            return new StatBlock(
                stats.Str - before.Str, stats.Dex - before.Dex, stats.Con - before.Con, stats.Int - before.Int, stats.Wis - before.Wis, stats.End - before.End);
        }

        /// <summary>The purpose a level-up's roll is derived for (stage 35): the mercenary's id after it, the level it rises to as the index.</summary>
        public const string GrowthPurpose = "growth:";

        /// <summary>Whether a mercenary's experience meets its level's ExpToNext (stage 35): the barracks may level it up. Never at the last level.</summary>
        public static bool CanLevelUp(StaticData data, MercenaryState mercenary)
        {
            return mercenary.Level < data.MaxLevel && mercenary.Exp >= data.LevelAt(mercenary.Level).ExpToNext;
        }

        /// <summary>
        /// Levels a mercenary up by one (Slice D stage 35, Docs/Design/05_Mercenary_Growth.md §2): the level's ExpToNext leaves the experience,
        /// the level rises, and the job's grades roll the growth. The roll's stream is the run seed's for this mercenary and this level
        /// (<see cref="GrowthPurpose"/> + id, the new level): the order of the clicks does not change it and a reload rolls the same. At the last
        /// level the experience is 0. Returns what each stat gained.
        /// </summary>
        public static StatBlock LevelUp(StaticData data, MercenaryState mercenary, ulong runSeed)
        {
            if (!CanLevelUp(data, mercenary))
            {
                throw new InvalidOperationException($"Mercenary '{mercenary.Id}' cannot level up now.");
            }

            mercenary.Exp -= data.LevelAt(mercenary.Level).ExpToNext;
            mercenary.Level++;
            if (mercenary.Level >= data.MaxLevel)
            {
                mercenary.Exp = 0;
            }

            var rng = new Pcg32(SeedDeriver.Derive(runSeed, GrowthPurpose + mercenary.Id, mercenary.Level), RngStream.Growth);
            return Grow(data, mercenary, 1, rng);
        }

        /// <summary>What an ended expedition gives each survivor: ExpPerFloor for each map floor reached, ExpClear for a clear.</summary>
        public static int ExpFor(StaticData data, ExpeditionState expedition)
        {
            return data.Balance.ExpPerFloor * RunRules.FloorsReached(data, expedition)
                + (expedition.Result == ExpeditionResult.Cleared ? data.Balance.ExpClear : 0);
        }

        /// <summary>
        /// Adds experience to a mercenary (stage 35: no level is climbed here; the barracks levels it up, <see cref="LevelUp"/>). What goes past
        /// the level's ExpToNext is kept for the levels after it. At the last level the experience stays 0: nothing is kept beyond it.
        /// Returns whether the mercenary can level up now.
        /// </summary>
        public static bool Gain(StaticData data, MercenaryState mercenary, int exp)
        {
            if (exp < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(exp), exp, "Experience cannot be negative.");
            }

            if (mercenary.Level >= data.MaxLevel)
            {
                mercenary.Exp = 0;
                return false;
            }

            mercenary.Exp += exp;
            return CanLevelUp(data, mercenary);
        }

        /// <summary>The experience a mercenary needs to leave its level, or 0 at the last level.</summary>
        public static int ExpToNext(StaticData data, MercenaryState mercenary)
        {
            return data.LevelAt(mercenary.Level).ExpToNext;
        }
    }
}
