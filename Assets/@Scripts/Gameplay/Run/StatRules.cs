using System;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The stats' arithmetic (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2, 02_Combat_System.md §4): where a mercenary's stats
    /// start, how much of an item's power its owner's stat gives, and how much maximum HP constitution adds. Every effect is counted from one
    /// baseline (BalanceData.StatBaseline) in whole percents; the balance's checks keep the lowest stat above 0%, so there is no hidden floor.
    /// The battle and the screens use these same functions.
    /// </summary>
    public static class StatRules
    {
        public const int Full = 100;

        /// <summary>The stats a mercenary of a job starts with: the job's base values, or the baseline for each when the job names none (test data).</summary>
        public static StatBlock BaseOf(StaticData data, string jobId)
        {
            return data.Jobs.Get(jobId).Stats ?? StatBlock.Uniform(data.Balance.StatBaseline);
        }

        /// <summary>The percent of its power an item has in its owner's hands: 100 for an owner without stats (an enemy) or an item that grows with none.</summary>
        public static int PowerPercent(BalanceData balance, StatBlock ownerStats, ItemData item)
        {
            return ownerStats == null || item.ScalesWith == null ? Full : PowerPercent(balance, ownerStats[item.ScalesWith.Value]);
        }

        /// <summary>The percent of power a stat's value gives: 100 at the baseline, StatPowerPercentPerPoint more for each point above, less for each below.</summary>
        public static int PowerPercent(BalanceData balance, int value)
        {
            return Full + balance.StatPowerPercentPerPoint * (value - balance.StatBaseline);
        }

        /// <summary>What constitution adds to the job's maximum HP, in percent (negative under the baseline).</summary>
        public static int HpPercent(BalanceData balance, StatBlock stats)
        {
            return balance.HpPercentPerConPoint * (stats.Con - balance.StatBaseline);
        }

        /// <summary>A damage, heal or shield (hundredths) at a percent: rounded down, at least 1.</summary>
        public static int Scale(int value, int percent)
        {
            if (percent == Full)
            {
                return value;
            }

            long scaled = (long)value * percent / Full;
            return scaled < 1 ? 1 : (int)scaled;
        }

        // ---- The rolls (Slice D stage 33, Docs/Design/02_Combat_System.md §4·§5·§6, 04 §3) ----

        /// <summary>Hit and critical chances are counted in ten-thousandths: dexterity moves them by half points.</summary>
        public const int Certain = 10000;

        /// <summary>A weapon's or an attack item's damage or burn can miss; heals, shields, passives, burn ticks and the storm never do.</summary>
        public static bool CanMiss(ItemData item, ItemEffect effect)
        {
            return (item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Attack)
                && (effect.Kind == EffectKind.Damage || effect.Kind == EffectKind.Burn);
        }

        /// <summary>
        /// What the attacker adds to its hit chance, in ten-thousandths: a mercenary's stat of the item's axis above the baseline times HitPerStatPoint
        /// (nothing for an item without an axis) and its traits' points (Slice D stage 37), an enemy's Accuracy.
        /// </summary>
        public static int Accuracy(BalanceData balance, StatBlock attackerStats, int enemyAccuracy, ItemData item, TraitBonus traits = null)
        {
            if (attackerStats == null)
            {
                return enemyAccuracy * 100;
            }

            int axis = item.ScalesWith == null ? 0 : (attackerStats[item.ScalesWith.Value] - balance.StatBaseline) * balance.HitPerStatPoint;
            return axis + (traits?.HitPercent ?? 0) * 100;
        }

        /// <summary>
        /// What the target takes off a hit on it, in ten-thousandths: a mercenary's dexterity above the baseline times DodgePerDexPoint with its traits'
        /// points (stage 37), an enemy's Dodge.
        /// </summary>
        public static int Dodge(BalanceData balance, StatBlock targetStats, int enemyDodge, TraitBonus traits = null)
        {
            return targetStats == null ? enemyDodge * 100 : (targetStats.Dex - balance.StatBaseline) * balance.DodgePerDexPoint + (traits?.DodgePercent ?? 0) * 100;
        }

        /// <summary>The hit chance in ten-thousandths: HitBasePercent with the accuracy and less the dodge, within HitMinPercent..HitMaxPercent.</summary>
        public static int HitChance(BalanceData balance, int accuracy, int dodge)
        {
            return Clamp(balance.HitBasePercent * 100 + accuracy - dodge, balance.HitMinPercent * 100, balance.HitMaxPercent * 100);
        }

        /// <summary>
        /// A weapon's critical chance in ten-thousandths: the item's percent with the owner's dexterity's half points, the owner's passive's
        /// points (Slice D stage 36, <see cref="CritBonusPercent"/>) and its traits' points (stage 37), within 0..100%. 0 for an item without a
        /// critical chance.
        /// </summary>
        public static int CritChance(BalanceData balance, StatBlock ownerStats, ItemData item, int bonusPercent = 0, TraitBonus traits = null)
        {
            if (item.CritChancePercent <= 0)
            {
                return 0;
            }

            int dex = ownerStats == null ? 0 : (ownerStats.Dex - balance.StatBaseline) * balance.CritPerDexPoint;
            return Clamp((item.CritChancePercent + bonusPercent + (traits?.CritPercent ?? 0)) * 100 + dex, 0, Certain);
        }

        /// <summary>
        /// The percent points a passive adds to an item's critical chance (Slice D stage 36, the ranger's "무기의 치명타 확률 +10%"): its magnitude for
        /// a weapon item that has a critical chance, 0 otherwise. The passive holds everywhere (JobData allows it no condition).
        /// </summary>
        public static int CritBonusPercent(PassiveSpec passive, ItemData item)
        {
            return passive != null && passive.Trigger == PassiveTrigger.Always && passive.Effect == PassiveEffect.CritChancePercent
                && item.Category == ItemCategory.Weapon && item.CritChancePercent > 0
                ? passive.Magnitude
                : 0;
        }

        /// <summary>The chance of a virtue at the breakdown, in percent: VirtueChancePercent with spirit's points (none without stats) and the traits' (stage 37), within 0..100.</summary>
        public static int VirtueChancePercent(BalanceData balance, StatBlock stats, TraitBonus traits = null)
        {
            int spirit = stats == null ? 0 : (stats.Spi - balance.StatBaseline) * balance.VirtuePerSpiPoint;
            return Clamp(balance.VirtueChancePercent + spirit + (traits?.VirtuePercent ?? 0), 0, Full);
        }

        /// <summary>What constitution takes off the death chance at death's door, in percent points (negative under the baseline); 0 without stats.</summary>
        public static int DeathChanceShift(BalanceData balance, StatBlock stats)
        {
            return stats == null ? 0 : (stats.Con - balance.StatBaseline) * balance.DogDeathPercentPerConPoint;
        }

        /// <summary>
        /// The fatigue a mercenary gains from one event, after its endurance: a gain (a positive amount) times 100 less FatiguePercentPerEndPoint for each
        /// point above the baseline (more below it) with its traits' percent (stage 37), rounded half up, never under 0. What takes fatigue off stays as it is.
        /// </summary>
        public static int FatigueGain(BalanceData balance, StatBlock stats, int amount, TraitBonus traits = null)
        {
            int traitPercent = traits?.FatiguePercent ?? 0;
            if (amount <= 0 || (stats == null && traitPercent == 0))
            {
                return amount;
            }

            int endurance = stats == null ? 0 : balance.FatiguePercentPerEndPoint * (stats.End - balance.StatBaseline);
            int percent = Math.Max(0, Full - endurance + traitPercent);
            return (int)(((long)amount * percent + Full / 2) / Full);
        }

        static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }

        /// <summary>A burn's stacks at a percent: rounded half up (Docs/Design/02 §4: a burn counts stacks), at least 1.</summary>
        public static int ScaleStacks(int stacks, int percent)
        {
            if (percent == Full)
            {
                return stacks;
            }

            long scaled = ((long)stacks * percent + Full / 2) / Full;
            return scaled < 1 ? 1 : (int)scaled;
        }
    }
}
