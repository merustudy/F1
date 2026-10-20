using System;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The stats' arithmetic (Slice D stage 32·33; 2026-10-20 스탯 개정: Docs/Design/05_Mercenary_Growth.md §2, 02_Combat_System.md §4·§5·§6, 04 §3): where a
    /// mercenary's stats start; how much of an item's power its owner's stat gives and how much it quickens the owner's equipment; the HP constitution
    /// gives; the rolls (hit, dodge, critical hits and their multiplier, the virtue at a breakdown, the death at death's door); the fatigue a mercenary gains.
    /// Power and cooldown count from BalanceData.PowerBaseline, the fatigue from FatigueBaseline, the rolls from StatBaseline, the HP from 0. The
    /// balance's checks keep the lowest stat above 0% power. The battle and the screens use these same functions.
    /// </summary>
    public static class StatRules
    {
        public const int Full = 100;

        /// <summary>The stats a mercenary of a job starts with: the job's base values, or the baseline for each when the job names none (test data).</summary>
        public static StatBlock BaseOf(StaticData data, string jobId)
        {
            return data.Jobs.Get(jobId).Stats ?? StatBlock.Uniform(data.Balance.StatBaseline);
        }

        // ---- Power and speed (Docs/Design/02 §4) ----

        /// <summary>The percent of its power an item has in its owner's hands: 100 for an owner without stats (an enemy) or an item that grows with none.</summary>
        public static int PowerPercent(BalanceData balance, StatBlock ownerStats, ItemData item)
        {
            return ownerStats == null || item.ScalesWith == null ? Full : PowerPercent(balance, ownerStats[item.ScalesWith.Value]);
        }

        /// <summary>The percent of power a stat's value gives: 100 at the power baseline, StatPowerPercentPerPoint more for each point above, less for each below.</summary>
        public static int PowerPercent(BalanceData balance, int value)
        {
            return Full + balance.StatPowerPercentPerPoint * (value - balance.PowerBaseline);
        }

        /// <summary>
        /// The percent of its cooldown an item cycles with in its owner's hands (2026-10-20 스탯 개정): 100 for an owner without stats or an item no stat
        /// quickens, else 100 less CooldownPercentPerPoint for each point of the quickening stat (dexterity for physical equipment, wisdom for magic) above the
        /// power baseline, more for each below; never under 1.
        /// </summary>
        public static int CooldownPercent(BalanceData balance, StatBlock ownerStats, ItemData item)
        {
            if (ownerStats == null || item.QuickenedBy == null)
            {
                return Full;
            }

            return Math.Max(1, Full - balance.CooldownPercentPerPoint * (ownerStats[item.QuickenedBy.Value] - balance.PowerBaseline));
        }

        /// <summary>A cooldown at a percent: rounded to the nearest millisecond, never below MinCooldownMs (the battle counts the same way).</summary>
        public static int CooldownMs(BalanceData balance, int baseMs, int percent)
        {
            long scaled = ((long)baseMs * percent + Full / 2) / Full;
            return scaled < balance.MinCooldownMs ? balance.MinCooldownMs : (int)scaled;
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

        // ---- HP (Docs/Design/05 §2) ----

        /// <summary>
        /// The maximum HP (hundredths) constitution gives before the traits (2026-10-20 스탯 개정): each point up to HpBandLow gives HpPerConPointLow, each
        /// up to HpBandMid HpPerConPointMid, each above HpPerConPointHigh. A job has no HP of its own any more.
        /// </summary>
        public static int MaxHpOf(BalanceData balance, int con)
        {
            long hp = 0;
            for (int point = 1; point <= con; point++)
            {
                hp += point <= balance.HpBandLow ? balance.HpPerConPointLow : point <= balance.HpBandMid ? balance.HpPerConPointMid : balance.HpPerConPointHigh;
            }

            return (int)hp;
        }

        // ---- The rolls (Slice D stage 33, Docs/Design/02_Combat_System.md §4·§5·§6, 04 §3) ----

        /// <summary>Hit and critical chances are counted in ten-thousandths (the data's points may be halves).</summary>
        public const int Certain = 10000;

        /// <summary>
        /// Whether an effect can miss (2026-10-20 스탯 개정): a physical weapon's or attack item's damage or burn. Magic always hits; heals, shields, passives,
        /// burn ticks and the storm never miss.
        /// </summary>
        public static bool CanMiss(ItemData item, ItemEffect effect)
        {
            return item.School == ItemSchool.Physical && (item.Category == ItemCategory.Weapon || item.Category == ItemCategory.Attack)
                && (effect.Kind == EffectKind.Damage || effect.Kind == EffectKind.Burn);
        }

        /// <summary>
        /// What the attacker adds to its hit chance, in ten-thousandths: a mercenary's dexterity above the baseline times HitPerDexPoint and its traits'
        /// points (Slice D stage 37; the traits' hit is physical only, 2026-10-20), an enemy's Accuracy.
        /// </summary>
        public static int Accuracy(BalanceData balance, StatBlock attackerStats, int enemyAccuracy, TraitBonus traits = null)
        {
            if (attackerStats == null)
            {
                return enemyAccuracy * 100;
            }

            return (attackerStats.Dex - balance.StatBaseline) * balance.HitPerDexPoint + (traits?.HitPercent ?? 0) * 100;
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
        /// A weapon's critical chance in ten-thousandths: the item's percent with the owner's points of the school's stat (dexterity for a physical weapon,
        /// wisdom for a magic one: 2026-10-20 스탯 개정), the owner's passive's points (Slice D stage 36, <see cref="CritBonusPercent"/>) and its traits'
        /// points (stage 37), less the target's helm (stage 39), within 0..100%. 0 for an item without a critical chance.
        /// </summary>
        /// <param name="takenPercent">The points the target's items take off (Slice D stage 39, the iron helm); 0 for a card, which has no target.</param>
        public static int CritChance(BalanceData balance, StatBlock ownerStats, ItemData item, int bonusPercent = 0, TraitBonus traits = null, int takenPercent = 0)
        {
            if (item.CritChancePercent <= 0)
            {
                return 0;
            }

            int stat = ownerStats == null ? 0
                : item.School == ItemSchool.Magic ? (ownerStats.Wis - balance.StatBaseline) * balance.CritPerWisPoint
                : (ownerStats.Dex - balance.StatBaseline) * balance.CritPerDexPoint;
            return Clamp((item.CritChancePercent + bonusPercent + (traits?.CritPercent ?? 0) - takenPercent) * 100 + stat, 0, Certain);
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

        /// <summary>
        /// The percent a critical hit's damage becomes (2026-10-20 스탯 개정, Docs/Design/02 §4): a physical weapon's PhysicalCritDamagePercent with what the
        /// weapon itself and the owner's traits add (percent points), a magic weapon's MagicCritDamagePercent as it is.
        /// </summary>
        public static int CritDamagePercent(BalanceData balance, ItemData weapon, TraitBonus traits = null)
        {
            return weapon.School == ItemSchool.Magic
                ? balance.MagicCritDamagePercent
                : balance.PhysicalCritDamagePercent + weapon.CritDamagePercent + (traits?.CritDamagePercent ?? 0);
        }

        /// <summary>The chance of a virtue at the breakdown, in percent: VirtueChancePercent with endurance's points (none without stats; 2026-10-20: endurance, before spirit) and the traits' (stage 37), within 0..100.</summary>
        public static int VirtueChancePercent(BalanceData balance, StatBlock stats, TraitBonus traits = null)
        {
            int endurance = stats == null ? 0 : (stats.End - balance.StatBaseline) * balance.VirtuePerEndPoint;
            return Clamp(balance.VirtueChancePercent + endurance + (traits?.VirtuePercent ?? 0), 0, Full);
        }

        /// <summary>What endurance takes off the death chance at death's door, in percent points (negative under the baseline; 2026-10-20: endurance, before constitution); 0 without stats.</summary>
        public static int DeathChanceShift(BalanceData balance, StatBlock stats)
        {
            return stats == null ? 0 : (stats.End - balance.StatBaseline) * balance.DogDeathPercentPerEndPoint;
        }

        /// <summary>
        /// The fatigue a mercenary gains from one event, after its endurance: a gain (a positive amount) times 100 less FatiguePercentPerEndPoint for each
        /// point above FatigueBaseline (more below it) with its traits' percent (stage 37), rounded half up, never under 0. What takes fatigue off stays as it is.
        /// </summary>
        public static int FatigueGain(BalanceData balance, StatBlock stats, int amount, TraitBonus traits = null)
        {
            int traitPercent = traits?.FatiguePercent ?? 0;
            if (amount <= 0 || (stats == null && traitPercent == 0))
            {
                return amount;
            }

            int endurance = stats == null ? 0 : balance.FatiguePercentPerEndPoint * (stats.End - balance.FatigueBaseline);
            int percent = Math.Max(0, Full - endurance + traitPercent);
            return (int)(((long)amount * percent + Full / 2) / Full);
        }

        static int Clamp(int value, int min, int max)
        {
            return value < min ? min : value > max ? max : value;
        }
    }
}
