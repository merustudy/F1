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
    /// The mercenaries' traits (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): what a list of them adds up to, the innate ones a
    /// candidate of the stagecoach is born with, and the acquired ones an expedition leaves on a survivor at the settlement (stage 38). A mercenary
    /// never holds a trait twice nor two that clash (<see cref="TraitData.Clashes"/>).
    /// </summary>
    public static class TraitRules
    {
        /// <summary>The purpose a survivor's acquired traits are derived for (stage 38): the mercenary's id after it, the run's expedition count as the index.</summary>
        public const string AcquiredPurpose = "trait:";

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

        /// <summary>
        /// The sources of acquired traits a survivor of an ended expedition meets at the settlement (stage 38, 05 §6), in the order they are rolled:
        /// the clear or the retreat, death's door if it went there, then the affliction it came home in or the virtue it had (a virtue lasts to the
        /// end of the expedition, so one state at most). The dead meet none.
        /// </summary>
        public static List<TraitSource> SourcesOf(StaticData data, ExpeditionResult result, ExpeditionMember member)
        {
            var sources = new List<TraitSource>();
            if (!member.Alive)
            {
                return sources;
            }

            if (result == ExpeditionResult.Cleared)
            {
                sources.Add(TraitSource.Clear);
            }
            else if (result == ExpeditionResult.Retreated)
            {
                sources.Add(TraitSource.Retreat);
            }

            if (member.DeathsDoor)
            {
                sources.Add(TraitSource.DeathsDoor);
            }

            if (member.StateId != null)
            {
                sources.Add(data.FatigueStates.Get(member.StateId).Kind == FatigueStateKind.Affliction ? TraitSource.Affliction : TraitSource.Virtue);
            }

            return sources;
        }

        /// <summary>The chance, in percent, that a source leaves a trait (BalanceData TraitChance*Percent).</summary>
        public static int ChancePercent(BalanceData balance, TraitSource source)
        {
            switch (source)
            {
                case TraitSource.Clear: return balance.TraitChanceClearPercent;
                case TraitSource.Retreat: return balance.TraitChanceRetreatPercent;
                case TraitSource.DeathsDoor: return balance.TraitChanceDeathsDoorPercent;
                case TraitSource.Affliction: return balance.TraitChanceAfflictionPercent;
                default: return balance.TraitChanceVirtuePercent;
            }
        }

        /// <summary>
        /// The stream a survivor's acquired traits are rolled on (stage 38): derived from the run seed by the mercenary and the run's expedition
        /// count, so the order of the settlement and of the level-ups does not change it.
        /// </summary>
        public static Pcg32 AcquiredStream(ulong runSeed, string mercenaryId, int expeditionCount)
        {
            return new Pcg32(SeedDeriver.Derive(runSeed, AcquiredPurpose + mercenaryId, expeditionCount), RngStream.Trait);
        }

        /// <summary>
        /// The acquired traits a survivor takes at the settlement (stage 38, 05 §6): for each source in turn, among the acquired traits of that
        /// source it can still take (beside those it holds and those this draw gave it), one roll of the source's chance and, when it hits, one
        /// of them by Weight in id order. A source with none to take or a chance of 0 rolls nothing. Each source gives one at most.
        /// </summary>
        public static List<string> DrawAcquired(StaticData data, IReadOnlyList<string> held, IReadOnlyList<TraitSource> sources, Pcg32 rng)
        {
            var drawn = new List<string>();
            var holding = new List<string>(held);
            foreach (TraitSource source in sources)
            {
                var open = new List<TraitData>();
                int total = 0;
                foreach (TraitData trait in data.Traits.Ordered)
                {
                    if (trait.Kind == TraitKind.Acquired && trait.Source == source && trait.Weight > 0 && CanTake(data, holding, trait))
                    {
                        open.Add(trait);
                        total += trait.Weight;
                    }
                }

                int chance = ChancePercent(data.Balance, source);
                if (total == 0 || chance <= 0 || rng.NextInt(100) >= chance)
                {
                    continue;
                }

                int pick = rng.NextInt(total);
                foreach (TraitData trait in open)
                {
                    if (pick < trait.Weight)
                    {
                        drawn.Add(trait.Id);
                        holding.Add(trait.Id);
                        break;
                    }

                    pick -= trait.Weight;
                }
            }

            return drawn;
        }
    }
}
