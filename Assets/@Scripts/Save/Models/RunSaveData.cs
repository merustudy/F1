using System.Collections.Generic;

namespace F1.Save
{
    /// <summary>
    /// Save DTO of run.json: the run, the expedition in progress and the record of the battle in
    /// progress. The owner is RunManager. These types hold text and numbers only; enum values are
    /// stored by name and seeds as decimal strings. Rules: Docs/Architecture/07_SAVE.md.
    ///
    /// Version 8 (Slice B stage 17): the expedition's region Coins and, at a shop, its Shop (the stock and the refreshes).
    /// Version 4 (Slice B): fatigue builds up from 0 (a mercenary's Fatigue was what was left of the
    /// maximum), a member carries its Fatigue on the expedition, and an item says whether it is a base
    /// weapon. Version 3 (boards without empty entries, an Inventory) and version 2 (one entry per slot,
    /// no inventory) are brought up by RunSaveMigrator. Version 1 stored "Front"/"Rear" rows and is not
    /// read any more.
    /// </summary>
    public sealed class RunSaveData
    {
        public const int CurrentSchemaVersion = 8;

        public int SchemaVersion = CurrentSchemaVersion;
        public RunRecord Run;

        /// <summary>Null while the party is at home.</summary>
        public ExpeditionRecord Expedition;
    }

    public sealed class RunRecord
    {
        public string Seed;
        public int Day;
        public List<MercenaryRecord> Roster;
        public List<string> Fallen;
        public List<PartySlotRecord> Party;
        public int ExpeditionCount;
        public List<ClearRecord> ClearedDungeons;
        public bool IsOver;
    }

    public sealed class MercenaryRecord
    {
        public string Id;
        public string JobId;

        /// <summary>0 is fresh; it builds up on expeditions.</summary>
        public int Fatigue;

        /// <summary>The affliction the mercenary came home with (a FatigueState id), or null (version 6).</summary>
        public string Affliction;
    }

    public sealed class PartySlotRecord
    {
        public string MercenaryId;
        public int Row;
    }

    public sealed class ClearRecord
    {
        public string DungeonId;
        public int Count;
    }

    public sealed class ExpeditionRecord
    {
        public string DungeonId;
        public string Seed;
        public List<MemberRecord> Members;

        /// <summary>Items kept outside the boards, in order.</summary>
        public List<ItemRecord> Inventory;

        /// <summary>Potion ids per slot. A null entry is an empty slot.</summary>
        public List<string> Potions;

        public string Phase;
        public int CurrentNodeId;
        public int BattlesWon;
        public List<RewardRecord> PendingRewards;

        /// <summary>The region coins won and not spent (version 8). A file without them has none.</summary>
        public int Coins;

        /// <summary>Present only while the party is at a shop (version 8).</summary>
        public ShopRecord Shop;

        /// <summary>Present only while a battle is being fought.</summary>
        public BattleRecord Battle;
    }

    /// <summary>What a shop has for sale while the party is at it: the offers by slot (a sold slot is null), and how many refreshes were made there.</summary>
    public sealed class ShopRecord
    {
        public List<RewardRecord> Stock;
        public int Refreshes;
    }

    public sealed class MemberRecord
    {
        public string MercenaryId;
        public string JobId;
        public int Row;
        public int MaxHp;
        public int Hp;
        public bool Alive;

        /// <summary>The item board in order. No null entries; the cells come from the job.</summary>
        public List<ItemRecord> Items;

        /// <summary>The fatigue the member carries on the expedition. It goes to the roster at the settlement.</summary>
        public int Fatigue;

        /// <summary>The affliction or virtue the member is in (a FatigueState id), or null (version 6).</summary>
        public string State;
    }

    public sealed class ItemRecord
    {
        public string ItemId;
        public int Grade;

        /// <summary>True for a base weapon: the job's weapon given when the expedition left.</summary>
        public bool Base;

        /// <summary>The tier's name: Common, Bronze, Silver or Gold (version 7; version 5 named them Bronze, Silver, Gold, Diamond). An entry that does not name one is Common, as every item was before.</summary>
        public string Tier = "Common";
    }

    public sealed class RewardRecord
    {
        public string Kind;
        public string Id;
        public int Grade;

        /// <summary>The tier's name of an item reward; Common for a potion (version 7, as above). An entry that does not name one is Common.</summary>
        public string Tier = "Common";
    }

    /// <summary>
    /// Everything needed to rebuild a battle in progress: the accepted inputs and the battle time
    /// that is confirmed. The setup is rebuilt from the expedition.
    /// </summary>
    public sealed class BattleRecord
    {
        public int ConfirmedTimeMs;
        public List<BattleInputRecord> Inputs;

        /// <summary>Digest of the event log up to the confirmed time, as a decimal string.</summary>
        public string LogHash;
    }

    public sealed class BattleInputRecord
    {
        public int TimeMs;
        public string Kind;
        public int PotionSlot;
        public int PartyIndex;
    }
}
