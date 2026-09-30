using System.Collections.Generic;

namespace F1.Save
{
    /// <summary>
    /// Save DTO of run.json: the run, the expedition in progress and the record of the battle in
    /// progress. The owner is RunManager. These types hold text and numbers only; enum values are
    /// stored by name and seeds as decimal strings. Rules: Docs/Architecture/07_SAVE.md.
    /// </summary>
    public sealed class RunSaveData
    {
        public const int CurrentSchemaVersion = 1;

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
        public int Fatigue;
    }

    public sealed class PartySlotRecord
    {
        public string MercenaryId;
        public string Row;
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

        /// <summary>Potion ids per slot. A null entry is an empty slot.</summary>
        public List<string> Potions;

        public string Phase;
        public int CurrentNodeId;
        public int BattlesWon;
        public List<RewardRecord> PendingRewards;

        /// <summary>Present only while a battle is being fought.</summary>
        public BattleRecord Battle;
    }

    public sealed class MemberRecord
    {
        public string MercenaryId;
        public string JobId;
        public string Row;
        public int MaxHp;
        public int Hp;
        public bool Alive;

        /// <summary>Item slots in order. A null entry is an empty slot.</summary>
        public List<ItemRecord> Items;
    }

    public sealed class ItemRecord
    {
        public string ItemId;
        public int Grade;
    }

    public sealed class RewardRecord
    {
        public string Kind;
        public string Id;
        public int Grade;
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
