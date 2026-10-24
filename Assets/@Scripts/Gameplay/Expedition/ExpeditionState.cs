using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    public enum ExpeditionPhase
    {
        ChoosingNode,
        InBattle,

        /// <summary>Picking up the loot a won battle dropped (Slice B stage 18): a drop at a time, until all are taken or the rest is left.</summary>
        PickingLoot,
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

    /// <summary>What an <see cref="ItemOffer"/> is.</summary>
    public enum OfferKind
    {
        Item,
        Potion,

        /// <summary>A bag (Slice B stage 19): sold by a shop, or the last drop of an elite's loot.</summary>
        Bag,
    }

    /// <summary>An item, a potion or a bag not yet owned: a drop of a won battle's loot (Slice B stage 18; a potion never drops), or what a shop sells (stage 17).</summary>
    public sealed class ItemOffer
    {
        public ItemOffer(OfferKind kind, string id, ItemTier tier = ItemTier.Common)
        {
            Kind = kind;
            Id = id;
            Tier = tier;
        }

        public OfferKind Kind { get; }

        /// <summary>Item id, potion id or bag id.</summary>
        public string Id { get; }

        /// <summary>Item tier. Common for a potion or a bag.</summary>
        public ItemTier Tier { get; }
    }

    /// <summary>
    /// What a shop has for sale while the party is at it (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5): the offers
    /// in their slots (a sold slot is null) and how many times the stock was refreshed here, which sets the next refresh's cost.
    /// </summary>
    public sealed class ShopState
    {
        public List<ItemOffer> Stock = new List<ItemOffer>();
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

        /// <summary>The item board: bags in the frame and items on the bags (Slice B stage 19, see <see cref="ItemBoard"/>).</summary>
        public ItemBoard Board;

        /// <summary>Fatigue (0 = fresh, see <see cref="FatigueRules"/>). It came from the roster and goes back to it at the settlement.</summary>
        public int Fatigue;

        /// <summary>The affliction or virtue the member is in (a FatigueStateData id), or null. An affliction came from the roster or the breakdown; a virtue ends with the expedition.</summary>
        public string StateId;

        /// <summary>The mercenary's stats (Slice D stage 32): they do not change on an expedition, and the battle grows the member's items with them.</summary>
        public StatBlock Stats;

        /// <summary>The mercenary's traits (Slice D stage 37, TraitData ids): they do not change on an expedition; the battle and the fatigue count them.</summary>
        public IReadOnlyList<string> Traits = new List<string>();

        /// <summary>Whether the member was at death's door in a battle of this expedition (Slice D stage 38): coming home alive from it may leave a trait.</summary>
        public bool DeathsDoor;
    }

    /// <summary>
    /// A member's HP and maximum HP as the expedition's screens show them (Slice D stage 56, <see cref="ExpeditionRules.ShownHp"/>): with what the board's
    /// maximum HP items raise, the HP in proportion.
    /// </summary>
    public readonly struct MemberHp
    {
        public MemberHp(int hp, int maxHp)
        {
            Hp = hp;
            MaxHp = maxHp;
        }

        public int Hp { get; }
        public int MaxHp { get; }
    }

    /// <summary>Who goes on an expedition, where they stand and how tired they leave.</summary>
    public readonly struct PartyMember
    {
        /// <param name="stats">The mercenary's stats (Slice D stage 32); null for the job's base values.</param>
        /// <param name="traits">The mercenary's traits (Slice D stage 37); null for none.</param>
        public PartyMember(string mercenaryId, string jobId, int row, int fatigue = 0, string afflictionId = null, int level = LevelRules.FirstLevel, StatBlock stats = null,
            IReadOnlyList<string> traits = null)
        {
            Traits = traits ?? new List<string>();
            MercenaryId = mercenaryId;
            JobId = jobId;
            Row = row;
            Fatigue = fatigue;
            AfflictionId = afflictionId;
            Level = level;
            Stats = stats;
        }

        public string MercenaryId { get; }
        public string JobId { get; }
        public int Row { get; }

        /// <summary>The mercenary's fatigue when the expedition leaves.</summary>
        public int Fatigue { get; }

        /// <summary>The affliction the mercenary leaves with, or null.</summary>
        public string AfflictionId { get; }

        /// <summary>The mercenary's level (stage 27): it sets the maximum HP the member leaves with.</summary>
        public int Level { get; }

        /// <summary>The mercenary's stats (Slice D stage 32), or null for the job's base values.</summary>
        public StatBlock Stats { get; }

        /// <summary>The mercenary's traits (Slice D stage 37); empty for none.</summary>
        public IReadOnlyList<string> Traits { get; }
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

        /// <summary>
        /// Items kept outside the boards, on a grid of their own (round 49). They do nothing in battle and are gone with the expedition.
        /// Bags are never kept here (Slice B stage 19). Made with the expedition: its size is the balance data's.
        /// </summary>
        public InventoryGrid Inventory;

        public ExpeditionPhase Phase;
        public ExpeditionResult Result;

        /// <summary>The forced final stand (Slice C stage 25): the final dungeon's boss alone, fought past the last day. Losing it loses the run.</summary>
        public bool FinalStand;

        /// <summary>The node being fought or last cleared. -1 before the first choice.</summary>
        public int CurrentNodeId = -1;

        public int BattlesWon;

        /// <summary>
        /// The loot of the last won battle while <see cref="Phase"/> is PickingLoot: one entry per drop, null where it was taken; empty
        /// otherwise (Slice B stage 18, Docs/Design/03_Dungeon_Structure.md §5).
        /// </summary>
        public List<ItemOffer> Loot = new List<ItemOffer>();

        /// <summary>
        /// The unique items the last won battle made (Slice D stage 50, ForgeRules.ForgeAfterBattle), for the battle screen's merge. Not saved: an
        /// expedition resumed shows the uniques on the boards without the effect. Empty when the last battle made none.
        /// </summary>
        public List<UniqueForged> LastForged = new List<UniqueForged>();

        /// <summary>
        /// The region coins the expedition has won and not spent (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5). They
        /// exist only here: the run has no money, and they go when the expedition ends.
        /// </summary>
        public int Coins;

        /// <summary>The shop the party is at while <see cref="Phase"/> is AtShop; null otherwise.</summary>
        public ShopState Shop;
    }
}
