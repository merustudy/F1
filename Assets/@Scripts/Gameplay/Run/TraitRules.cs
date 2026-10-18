using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// What a mercenary's traits add to the stats' formulas (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6), summed over its traits:
    /// percent points on the hit chance of its attacks, its dodge, the critical chance of its items that have one, its death chance at death's
    /// door and its chance of a virtue; percent on the fatigue it gains and on its maximum HP. A unit without traits (an enemy) adds nothing.
    /// </summary>
    public sealed class TraitBonus
    {
        public static readonly TraitBonus None = new TraitBonus(0, 0, 0, 0, 0, 0, 0);

        public TraitBonus(int hitPercent, int dodgePercent, int critPercent, int dogDeathPercent, int virtuePercent, int fatiguePercent, int maxHpPercent)
        {
            HitPercent = hitPercent;
            DodgePercent = dodgePercent;
            CritPercent = critPercent;
            DogDeathPercent = dogDeathPercent;
            VirtuePercent = virtuePercent;
            FatiguePercent = fatiguePercent;
            MaxHpPercent = maxHpPercent;
        }

        public int HitPercent { get; }
        public int DodgePercent { get; }
        public int CritPercent { get; }
        public int DogDeathPercent { get; }
        public int VirtuePercent { get; }
        public int FatiguePercent { get; }
        public int MaxHpPercent { get; }
    }

    /// <summary>
    /// The mercenaries' traits (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): what a list of them adds up to, and the innate ones a
    /// candidate of the stagecoach is born with. A mercenary never holds a trait twice nor two that clash (<see cref="TraitData.Clashes"/>).
    /// </summary>
    public static class TraitRules
    {
        /// <summary>The sum of what the traits do; <see cref="TraitBonus.None"/> for none.</summary>
        public static TraitBonus BonusOf(StaticData data, IReadOnlyList<string> traitIds)
        {
            if (traitIds == null || traitIds.Count == 0)
            {
                return TraitBonus.None;
            }

            int hit = 0, dodge = 0, crit = 0, dogDeath = 0, virtue = 0, fatigue = 0, maxHp = 0;
            foreach (string id in traitIds)
            {
                TraitData trait = data.Traits.Get(id);
                hit += trait.HitPercent;
                dodge += trait.DodgePercent;
                crit += trait.CritPercent;
                dogDeath += trait.DogDeathPercent;
                virtue += trait.VirtuePercent;
                fatigue += trait.FatiguePercent;
                maxHp += trait.MaxHpPercent;
            }

            return new TraitBonus(hit, dodge, crit, dogDeath, virtue, fatigue, maxHp);
        }

        /// <summary>Whether a mercenary holding these traits can take another: it does not hold it yet and none of them clashes with it.</summary>
        public static bool CanTake(StaticData data, IReadOnlyList<string> held, TraitData trait)
        {
            foreach (string id in held)
            {
                if (id == trait.Id || data.Traits.Get(id).Clashes(trait))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The innate traits a candidate is born with (05 §6, 04 §6), on the stagecoach's stream after its stats: how many by the weights
        /// RecruitTraitWeight0..2, then each by its Weight among the innate traits it can still take, in id order. Nothing is rolled when no trait
        /// can be drawn or no count above none has a weight, so data without traits draws as before. The order is the draw's.
        /// </summary>
        public static List<string> DrawInnate(StaticData data, Pcg32 rng)
        {
            var drawn = new List<string>();
            BalanceData balance = data.Balance;
            List<TraitData> pool = data.InnateTraits;
            if (pool.Count == 0 || balance.RecruitTraitWeight1 + balance.RecruitTraitWeight2 == 0)
            {
                return drawn;
            }

            int roll = rng.NextInt(balance.RecruitTraitWeight0 + balance.RecruitTraitWeight1 + balance.RecruitTraitWeight2);
            int count = roll < balance.RecruitTraitWeight0 ? 0 : roll < balance.RecruitTraitWeight0 + balance.RecruitTraitWeight1 ? 1 : 2;
            for (int i = 0; i < count; i++)
            {
                var open = new List<TraitData>();
                int total = 0;
                foreach (TraitData trait in pool)
                {
                    if (CanTake(data, drawn, trait))
                    {
                        open.Add(trait);
                        total += trait.Weight;
                    }
                }

                if (total == 0)
                {
                    break;
                }

                int pick = rng.NextInt(total);
                foreach (TraitData trait in open)
                {
                    if (pick < trait.Weight)
                    {
                        drawn.Add(trait.Id);
                        break;
                    }

                    pick -= trait.Weight;
                }
            }

            return drawn;
        }
    }
}
