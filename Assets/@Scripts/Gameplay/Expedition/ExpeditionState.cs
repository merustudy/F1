using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    public enum ExpeditionPhase
    {
        ChoosingNode,
        InBattle,
        ChoosingReward,
        Finished,

        /// <summary>At a camp node: the party chooses what to do there before going on.</summary>
        AtCamp,

        /// <summary>At a shop node: the party buys what it wants for its region coins, then leaves (Slice B stage 17).</summary>
        AtShop,
    }

    public enum ExpeditionResult
    {
        None,
        /// <summary>The boss was defeated.</summary>
        Cleared,
        /// <summary>Every party member died.</summary>
        Wiped,
        Retreated,
    }

    public enum RewardKind
    {
        Item,
        Potion,
    }

    /// <summary>One of the choices offered after a won battle.</summary>
    public sealed class RewardOption
    {
        public RewardOption(RewardKind kind, string id, int grade, ItemTier tier = ItemTier.Common)
        {
            Kind = kind;
            Id = id;
            Grade = grade;
            Tier = tier;
        }

        public RewardKind Kind { get; }

        /// <summary>Item id or potion id.</summary>
        public string Id { get; }

        /// <summary>Item grade. 0 for a potion.</summary>
        public int Grade { get; }

        /// <summary>Item tier. Common for a potion.</summary>
        public ItemTier Tier { get; }
    }

    /// <summary>
    /// What a shop has for sale while the party is at it (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5): the offers
    /// in their slots (a sold slot is null) and how many times the stock was refreshed here, which sets the next refresh's cost.
    /// </summary>
    public sealed class ShopState
    {
        public List<RewardOption> Stock = new List<RewardOption>();
        public int Refreshes;
    }

    /// <summary>A mercenary while on an expedition. HP and items exist only here.</summary>
    public sealed class ExpeditionMember
    {
        public string MercenaryId;
        public string JobId;

        /// <summary>The row the mercenary stands in (1 = facing the enemy). A dead member keeps the row it died in.</summary>
        public int Row;
        public int MaxHp;
        public int Hp;
        public bool Alive;

        /// <summary>The item board: items in order, each taking its size in cells (see <see cref="ItemBoard"/>). No null entries.</summary>
        public List<EquippedItem> Items;

        /// <summary>Cells of the board, from the job.</summary>
        public int ItemSlots;

        /// <summary>Fatigue (0 = fresh, see <see cref="FatigueRules"/>). It came from the roster and goes back to it at the settlement.</summary>
        public int Fatigue;

        /// <summary>The affliction or virtue the member is in (a FatigueStateData id), or null. An affliction came from the roster or the breakdown; a virtue ends with the expedition.</summary>
        public string StateId;
    }

    /// <summary>Who goes on an expedition, where they stand and how tired they leave.</summary>
    public readonly struct PartyMember
    {
        public PartyMember(string mercenaryId, string jobId, int row, int fatigue = 0, string afflictionId = null)
        {
            MercenaryId = mercenaryId;
            JobId = jobId;
            Row = row;
            Fatigue = fatigue;
            AfflictionId = afflictionId;
        }

        public string MercenaryId { get; }
        public string JobId { get; }
        public int Row { get; }

        /// <summary>The mercenary's fatigue when the expedition leaves.</summary>
        public int Fatigue { get; }

        /// <summary>The affliction the mercenary leaves with, or null.</summary>
        public string AfflictionId { get; }
    }

    /// <summary>
    /// Plain runtime state of one expedition. Only <see cref="ExpeditionRules"/> changes it.
    /// Everything here disappears when the expedition ends.
    /// </summary>
    public sealed class ExpeditionState
    {
        public string DungeonId;
        public ulong Seed;
        public NodeMap Map;
        public List<ExpeditionMember> Members = new List<ExpeditionMember>();

        /// <summary>Potion ids per slot. A null entry is empty.</summary>
        public string[] Potions;

        /// <summary>Items kept outside the boards. They do nothing in battle and are gone with the expedition.</summary>
        public List<EquippedItem> Inventory = new List<EquippedItem>();

        public ExpeditionPhase Phase;
        public ExpeditionResult Result;

        /// <summary>The node being fought or last cleared. -1 before the first choice.</summary>
        public int CurrentNodeId = -1;

        public int BattlesWon;

        /// <summary>Offered while <see cref="Phase"/> is ChoosingReward; empty otherwise.</summary>
        public List<RewardOption> PendingRewards = new List<RewardOption>();

        /// <summary>
        /// The region coins the expedition has won and not spent (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5). They
        /// exist only here: the run has no money, and they go when the expedition ends.
        /// </summary>
        public int Coins;

        /// <summary>The shop the party is at while <see cref="Phase"/> is AtShop; null otherwise.</summary>
        public ShopState Shop;
    }
}
