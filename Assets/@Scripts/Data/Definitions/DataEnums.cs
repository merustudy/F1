using System;
using System.Collections.Generic;

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
    /// The frame of a mercenary's item board (Slice B stage 19, Docs/Design/02_Combat_System.md §4): squares in columns and rows,
    /// where bags lie and items lie on the bags. It is structure, not balance: the board panel's column holds this many squares
    /// (user 2026-10-08 "틀은 3x8"). How many of them are usable is what the bags make.
    /// </summary>
    public static class BoardFrame
    {
        public const int Width = 3;
        public const int Height = 8;

        /// <summary>The longest side an item or a bag may have: it must fit the frame's width when turned.</summary>
        public const int MaxSide = Width;

        public static bool Contains(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>
        /// Lays items of these sizes (unturned, in this order) in a bag <paramref name="width"/> wide (2026-10-19 Round 86: an enemy's board is
        /// the start bag): each at the first place it fits in reading order (top to bottom, left to right), the bag a row taller whenever one
        /// fits nowhere. Returns the rows the bag needs (at least <paramref name="minHeight"/>) and where each item lies, or -1 when an item is
        /// wider than the bag.
        /// </summary>
        public static int Pack(IReadOnlyList<(int Width, int Height)> sizes, int width, int minHeight, out List<(int X, int Y)> places)
        {
            places = new List<(int X, int Y)>();
            foreach ((int w, int _) in sizes)
            {
                if (w > width)
                {
                    return -1;
                }
            }

            var taken = new HashSet<(int, int)>();
            int height = minHeight;
            foreach ((int w, int h) in sizes)
            {
                for (int y = 0; ; y++)
                {
                    int x = 0;
                    for (; x + w <= width; x++)
                    {
                        if (Free(taken, x, y, w, h))
                        {
                            break;
                        }
                    }

                    if (x + w > width)
                    {
                        continue;
                    }

                    places.Add((x, y));
                    for (int dy = 0; dy < h; dy++)
                    {
                        for (int dx = 0; dx < w; dx++)
                        {
                            taken.Add((x + dx, y + dy));
                        }
                    }

                    height = Math.Max(height, y + h);
                    break;
                }
            }

            return height;
        }

        static bool Free(HashSet<(int, int)> taken, int x, int y, int w, int h)
        {
            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    if (taken.Contains((x + dx, y + dy)))
                    {
                        return false;
                    }
                }
            }

            return true;
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

        /// <summary>Defensive gear: a shield, and armour (Slice D stage 39: the iron armour and the iron helm).</summary>
        Armor,

        /// <summary>An attack that is not a weapon: something thrown or spat.</summary>
        Attack,

        /// <summary>Anything else.</summary>
        Other,
    }

    /// <summary>
    /// The tier of an item (Docs/Design/02_Combat_System.md §4). Common is the tier an item is found at and the base weapon's;
    /// two of the same item at the same tier merge into one a tier up (The Bazaar's way), and Tier3 is the last. Named by stars
    /// on screen (일반·1성·2성·3성; 2026-10-15 round 69, the way TFT counts its merges). Before that Common·Bronze·Silver·Gold
    /// (round 41) and Bronze·Silver·Gold·Diamond (The Bazaar's); the order of the values never changed.
    /// </summary>
    public enum ItemTier
    {
        Common,
        Tier1,
        Tier2,
        Tier3,
    }

    /// <summary>
    /// When an item activates (Docs/Design/02_Combat_System.md §3·§4). Cooldown: from the start of a battle it charges and fires each time its
    /// cooldown is full. BattleStart (Slice D stage 39, the armour): once at 0 ms, after the job passives, and never again; it has no cooldown.
    /// </summary>
    public enum ItemTrigger
    {
        Cooldown,
        BattleStart,
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
        /// <summary>Slice D stage 36 (the ranger): percent points added to the critical chance of the unit's weapon items that have one.</summary>
        CritChancePercent,
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

    /// <summary>
    /// A mercenary's stat (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2): strength, dexterity, constitution, intellect, spirit and
    /// endurance. Strength, dexterity, intellect and spirit are what an item's power can grow with (<see cref="ItemData.ScalesWith"/>).
    /// </summary>
    public enum Stat
    {
        Str,
        Dex,
        Con,
        Int,
        Spi,
        End,
    }

    /// <summary>
    /// Where a mercenary's trait comes from (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): it was born with it (a candidate of the
    /// stagecoach draws it, the starting roster's are in the data) or an expedition left it (stage 38). The job's trait is the job's passive.
    /// </summary>
    public enum TraitKind
    {
        Innate,
        Acquired,
    }

    /// <summary>
    /// What leaves an acquired trait on a mercenary at the return settlement (Slice D stage 38, Docs/Design/05_Mercenary_Growth.md §6): the
    /// expedition was cleared or retreated from, the mercenary came back from death's door, or came home in an affliction or had a virtue.
    /// The order is the order the settlement rolls them in.
    /// </summary>
    public enum TraitSource
    {
        Clear,
        Retreat,
        DeathsDoor,
        Affliction,
        Virtue,
    }

    /// <summary>Whether a trait is a strength or a weakness (05 §6). A strength may carry a small drawback; the data says which it is.</summary>
    public enum TraitPolarity
    {
        Positive,
        Negative,
    }
}
