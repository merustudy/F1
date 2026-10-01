namespace F1.Data
{
    /// <summary>
    /// The rows of a battle side. Each side has rows 1..<see cref="Count"/>; row 1 faces the other
    /// side, and a row holds one unit. A row is a plain number so that units can advance (row - 1).
    /// Where in a line something applies is a <see cref="RowSpan"/>, counted from an end of the line.
    /// The count is structure, not balance: a side is a line of at most this many units and the
    /// battle screen has one column per row.
    /// </summary>
    public static class BattleRows
    {
        public const int Front = 1;
        public const int Count = 4;

        public static bool IsValid(int row)
        {
            return row >= Front && row <= Count;
        }
    }

    public enum ItemCategory
    {
        /// <summary>"Weapon hit" and "weapon power" passives apply to these.</summary>
        Weapon,
        Support,
    }

    public enum EffectKind
    {
        Damage,
        Heal,
        Shield,
        Burn,
    }

    /// <summary>
    /// Who an effect is applied to. "Enemy" and "Ally" are relative to the owner of the item.
    /// Only living units are chosen. An attack range is counted from one end of the enemy line:
    /// the effect's reach says how many units it hits from that end.
    /// </summary>
    public enum TargetMode
    {
        /// <summary>The enemies counted from the front, as many as the reach: reach 1 is the front enemy.</summary>
        EnemyFront,
        /// <summary>The enemies counted from the back, as many as the reach: reach 1 is the rearmost enemy.</summary>
        EnemyBack,
        EnemyAll,
        Self,
        AllyLowestHp,
        AllyAll,
    }

    public enum PassiveTrigger
    {
        BattleStart,
        /// <summary>A damage effect of one of the unit's weapon items was applied to a target.</summary>
        WeaponHit,
        /// <summary>A heal effect of one of the unit's items was applied to a target.</summary>
        Heal,
        /// <summary>A standing modifier, checked whenever it is relevant.</summary>
        Always,
    }

    public enum PassiveCondition
    {
        None,
        /// <summary>The unit stands within the span of its line the passive names (front N or rear N).</summary>
        InRows,
        SelfInDog,
    }

    public enum PassiveEffect
    {
        Shield,
        Burn,
        WeaponPowerPercent,
    }

    public enum PassiveTarget
    {
        Self,
        AllyAll,
        /// <summary>The unit the triggering effect was applied to.</summary>
        EventTarget,
    }

    public enum PotionEffect
    {
        Heal,
        Shield,
    }
}
