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

    /// <summary>
    /// What an item is (Docs/Design/02_Combat_System.md §4). The rules ask only whether it is a weapon; the battle screen
    /// moves the owner by it (Docs/Design/10_Art_Direction.md §5).
    /// </summary>
    public enum ItemCategory
    {
        /// <summary>A weapon held in the hand: a sword, a bow, a staff. "Weapon hit" and "weapon power" passives apply to its damage.</summary>
        Weapon,

        /// <summary>A healing or helping item: it heals or strengthens (a charm, a pouch of herbs).</summary>
        Support,

        /// <summary>Defensive gear: a shield, and later armour.</summary>
        Armor,

        /// <summary>An attack that is not a weapon: something thrown or spat.</summary>
        Attack,

        /// <summary>Anything else.</summary>
        Other,
    }

    /// <summary>
    /// The tier of an item (Docs/Design/02_Combat_System.md §4). Common is the tier an item is found at and the base weapon's;
    /// two of the same item at the same tier merge into one a tier up (The Bazaar's way), and Gold is the last. Renamed on
    /// 2026-10-07 (round 41) from Bronze·Silver·Gold·Diamond: the three tiers above the base read as copper, silver and gold.
    /// </summary>
    public enum ItemTier
    {
        Common,
        Bronze,
        Silver,
        Gold,
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

    /// <summary>
    /// What a fatigue state is (Docs/Design/04_Lobby_100Day_Economy.md §3): an affliction, which the breakdown at the
    /// threshold usually brings and which ends when fatigue comes back under it, or a virtue, which it brings by chance and
    /// which lasts until the expedition ends.
    /// </summary>
    public enum FatigueStateKind
    {
        Affliction,
        Virtue,
    }
}
