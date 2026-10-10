using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    public enum BattleSide
    {
        Party,
        Enemy,
    }

    public enum BattleResult
    {
        Ongoing,
        Victory,
        /// <summary>Every mercenary of the party died.</summary>
        Defeat,
        Retreated,
    }

    /// <summary>An item at a grade and a tier, in a unit's item slot.</summary>
    public sealed class EquippedItem
    {
        /// <param name="isBase">True for a base weapon: the job's weapon a mercenary leaves on the expedition with.</param>
        public EquippedItem(ItemData item, int grade, bool isBase = false, ItemTier tier = ItemTier.Common)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            if (grade < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(grade), grade, "Grade must be at least 1.");
            }

            if (tier < ItemTier.Common || tier > ItemTier.Gold)
            {
                throw new ArgumentOutOfRangeException(nameof(tier), tier, "Not a tier.");
            }

            Grade = grade;
            IsBase = isBase;
            Tier = tier;
        }

        public ItemData Item { get; }
        public int Grade { get; }

        /// <summary>Common, Bronze, Silver or Gold: the effects grow with it (Docs/Design/02_Combat_System.md §4).</summary>
        public ItemTier Tier { get; }

        /// <summary>The size of one of the item's effects at its grade and tier.</summary>
        public int Magnitude(BalanceData balance, ItemEffect effect)
        {
            return effect.MagnitudeAt(Grade, balance.TierPercent(Tier));
        }

        /// <summary>
        /// What a melee weapon on one of the item's stars deals more (Slice B stage 20): the data's StarDamage at Common, as much again per
        /// tier step (Bronze twice, Silver three times, Gold four times). 0 for an item without stars.
        /// </summary>
        public int StarDamage => Item.StarDamage * ((int)Tier - (int)ItemTier.Common + 1);

        /// <summary>The same item a tier up: what two of it merge into, or what a camp's upkeep makes of it.</summary>
        public EquippedItem TierUp()
        {
            if (Tier == ItemTier.Gold)
            {
                throw new InvalidOperationException($"'{Item.Id}' is Gold already.");
            }

            return new EquippedItem(Item, Grade, IsBase, Tier + 1);
        }

        /// <summary>
        /// A base weapon: given when the expedition left, not found in it. It stays one when it moves to another
        /// board, and it costs no fatigue (Docs/Design/04_Lobby_100Day_Economy.md §3 "장비 피로").
        /// </summary>
        public bool IsBase { get; }
    }

    /// <summary>Everything the battle needs to know about one unit at the start.</summary>
    public sealed class BattleUnitSetup
    {
        /// <summary>Mercenary id or enemy id.</summary>
        public string SourceId;
        public LocalizedText Name;
        /// <summary>The row the unit starts in (1 = facing the enemy).</summary>
        public int Row;
        public int MaxHp;
        public int Hp;
        /// <summary>The items of the board in reading order (Slice B stage 19): no empty entries. The order is the activation order (see ItemBoard).</summary>
        public IReadOnlyList<EquippedItem> Items;

        /// <summary>Where the items and bags lie, for the screen only (the battle ignores it); null when nobody draws the board (tests).</summary>
        public BoardLayout Layout;

        /// <summary>
        /// What each item of <see cref="Items"/> (same order) deals more by the stars on it (Slice B stage 20, <see cref="StarRules"/>): added to a
        /// weapon's damage before the weapon-power passive. Null when nothing does (an enemy, tests).
        /// </summary>
        public IReadOnlyList<int> StarDamage;
        /// <summary>Null when the unit has no passive.</summary>
        public PassiveSpec Passive;
        /// <summary>Mercenaries enter Death-or-Glory at 0 HP. Enemies die.</summary>
        public bool HasDog;

        /// <summary>The fatigue the unit starts with (a mercenary's, after the cost of entering; 0 for an enemy) and the state it is in, or null.</summary>
        public int Fatigue;
        public FatigueStateData FatigueState;
    }

    /// <summary>
    /// The complete input of a battle besides player inputs. The same setup and the same inputs
    /// always produce the same result.
    /// </summary>
    public sealed class BattleSetup
    {
        public ulong Seed;
        public BalanceData Balance;
        /// <summary>Units in unit order: row 1 first. No row may be empty in front of an occupied one.</summary>
        public IReadOnlyList<BattleUnitSetup> Party;
        /// <summary>The same order and rule as <see cref="Party"/>.</summary>
        public IReadOnlyList<BattleUnitSetup> Enemies;
        /// <summary>Affinity modifier on enemy item cooldowns, in thousandths.</summary>
        public int EnemyCooldownPermille;
        /// <summary>Potion slots in order. A null entry is an empty slot.</summary>
        public IReadOnlyList<PotionData> Potions;

        /// <summary>The states the breakdown at the fatigue threshold picks from, in id order. Needed only when a party unit can reach the threshold.</summary>
        public IReadOnlyList<FatigueStateData> FatigueStates;
    }

    /// <summary>Identifies a unit inside a battle. <see cref="None"/> when there is no unit (burn, storm).</summary>
    public readonly struct UnitRef : IEquatable<UnitRef>
    {
        public static readonly UnitRef None = new UnitRef(BattleSide.Party, -1);

        public UnitRef(BattleSide side, int index)
        {
            Side = side;
            Index = index;
        }

        public BattleSide Side { get; }
        public int Index { get; }
        public bool IsNone => Index < 0;

        public bool Equals(UnitRef other)
        {
            return Side == other.Side && Index == other.Index;
        }

        public override bool Equals(object obj)
        {
            return obj is UnitRef other && Equals(other);
        }

        public override int GetHashCode()
        {
            return ((int)Side * 397) ^ Index;
        }

        public override string ToString()
        {
            return IsNone ? "-" : $"{Side}#{Index}";
        }
    }

    public enum BattleInputKind
    {
        UsePotion,
        Retreat,
    }

    /// <summary>A player input with the battle time it was made at.</summary>
    public readonly struct BattleInput
    {
        public BattleInput(int timeMs, BattleInputKind kind, int potionSlot, int partyIndex)
        {
            TimeMs = timeMs;
            Kind = kind;
            PotionSlot = potionSlot;
            PartyIndex = partyIndex;
        }

        public int TimeMs { get; }
        public BattleInputKind Kind { get; }
        public int PotionSlot { get; }
        public int PartyIndex { get; }
    }

    public enum BattleEventKind
    {
        BattleStarted,
        /// <summary>Source = owner, Id = item id, A = item slot.</summary>
        ItemActivated,
        /// <summary>Source (None for burn and storm), Target, Id = cause, A = amount, B = absorbed by shield, C = target HP after.</summary>
        Damaged,
        /// <summary>Source, Target, Id = cause, A = amount, B = HP actually restored, C = target HP after.</summary>
        Healed,
        /// <summary>Source, Target, Id = cause, A = amount, C = target shield after.</summary>
        ShieldGained,
        /// <summary>Source, Target, Id = cause, A = stacks added, C = target burn after.</summary>
        BurnApplied,
        /// <summary>Target, A = battle time the grace period ends.</summary>
        DogEntered,
        /// <summary>Target.</summary>
        DogExited,
        /// <summary>Target, A = hits taken during grace.</summary>
        GraceBroken,
        /// <summary>Target, A = death chance percent, B = roll 0..99, C = 1 if the unit died.</summary>
        DeathRolled,
        /// <summary>Target.</summary>
        Died,
        /// <summary>
        /// A row of one side emptied and the rows behind it moved one step forward.
        /// A = (int)BattleSide, B = the row that emptied.
        /// </summary>
        RowsAdvanced,
        /// <summary>Target, Id = potion id, A = potion slot.</summary>
        PotionUsed,
        /// <summary>A = success chance percent, B = roll 0..99, C = 1 on success.</summary>
        RetreatAttempted,
        /// <summary>A = damage of this storm tick.</summary>
        StormTicked,
        /// <summary>A = (int)BattleResult.</summary>
        BattleEnded,

        /// <summary>A party unit's fatigue changed. Target, Id = cause ("hit", "dog", "ally_dog", "ally_death", "kill", "virtue"), A = change, C = fatigue after.</summary>
        FatigueChanged,
        /// <summary>The breakdown at the threshold. Target, Id = the state's id, A = roll 0..99, B = virtue chance percent, C = 1 for a virtue.</summary>
        BrokeDown,
        /// <summary>The unit's fatigue reached its maximum. Target, C = 1 if it died (it was at death's door already), else it went to death's door.</summary>
        Collapsed,
        /// <summary>An affliction ended: fatigue came back under the threshold. Target, Id = the state's id.</summary>
        FatigueStateEnded,
    }

    /// <summary>
    /// One entry of the battle log. Presentation replays these; it never changes the outcome.
    /// The meaning of Id, A, B and C depends on <see cref="Kind"/>.
    /// </summary>
    public sealed class BattleEvent
    {
        public const string CauseBurn = "burn";
        public const string CauseStorm = "storm";
        public const string CausePassive = "passive";

        /// <summary>The causes of a <see cref="BattleEventKind.FatigueChanged"/> event, in its Id.</summary>
        public const string FatigueHit = "hit";
        public const string FatigueDog = "dog";
        public const string FatigueAllyDog = "ally_dog";
        public const string FatigueAllyDeath = "ally_death";
        public const string FatigueKill = "kill";
        public const string FatigueVirtue = "virtue";

        public BattleEvent(int timeMs, BattleEventKind kind, UnitRef source, UnitRef target, string id, int a, int b, int c)
        {
            TimeMs = timeMs;
            Kind = kind;
            Source = source;
            Target = target;
            Id = id;
            A = a;
            B = b;
            C = c;
        }

        public int TimeMs { get; }
        public BattleEventKind Kind { get; }
        public UnitRef Source { get; }
        public UnitRef Target { get; }
        public string Id { get; }
        public int A { get; }
        public int B { get; }
        public int C { get; }
    }
}
