using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Save
{
    /// <summary>
    /// Save DTO of run.json: the run, the expedition in progress and the record of the battle in
    /// progress. The owner is RunManager. These types hold text and numbers only; enum values are
    /// stored by name and seeds as decimal strings. Rules: Docs/Architecture/07_SAVE.md.
    ///
    /// Version 26 (Slice D stage 47, 직업군 확대): the same shape; the jobs are the eight (warrior, viking, rogue, archer, mage, occultist, priest, minstrel) and
    /// the ranger's id is "archer" (RunSaveMigrator renames a version 25 file's job ids; the three new jobs appear in no older file).
    /// Version 25 (Slice D stage 42, 장비 요구 스탯): the same shape; a roster mercenary's or a candidate's stat its job's base weapon asks for stands at
    /// least at what it asks (RunSaveMigrator lifts a version 24 file's; the stagecoach has kept its candidates there since).
    /// Version 28 (Slice D stage 55, the story — Docs/Design/33 §12): the run's SeenDialogues (the dialogues heard in it, in the order heard).
    /// RunSaveMigrator gives a version 27 file every dialogue of the data as heard, so that no dialogue breaks into a run already going on.
    /// Version 24 (2026-10-20 스탯 개정, Slice D stage 41): a stats record's Spi is Wis (wisdom), and the stats stand on the new scale (a job's base values
    /// 1..8 around the power baseline 5): RunSaveMigrator rescales a version 23 file's roster and candidates ((old + 1) / 2) and gives a member of an
    /// expedition going on the maximum HP of its new constitution, its HP in proportion.
    /// Version 23 (Slice D stage 38): a member's DeathsDoor (it was at death's door on this expedition: the settlement may leave a trait). A version 22
    /// file's members were at none (false, as the field is missing).
    /// Version 22 (Slice D stage 37): a mercenary's and a candidate's Traits (TraitData ids). A version 21 file has none: RunSaveMigrator gives every
    /// one an empty list. Version 21 (Slice D stage 36): the same shape; the jobs are the five (warrior, viking, ranger, mage, priest). Older files are not read
    /// (RunSaveMigrator.OldestReadableVersion: the user's decision). What follows is the history.
    /// Version 20 (Slice D stage 35): the same shape; a mercenary's Exp may pass what its level takes (the barracks levels it up, one level a click).
    /// Version 19 (Slice D stage 34): the same shape; a mercenary's Stats hold what its levels grew (the level's HP bonus is gone).
    /// Version 18 (Slice D stage 32): a mercenary's and a candidate's Stats (the six stats). Version 17 (2026-10-18): no gate among the buildings.
    /// Version 16 (Slice C stage 30): the town's building levels (Buildings) and the constructions going on (Constructions).
    /// Version 15 (Slice C stage 27): a mercenary's Level and Exp. Version 14 (Slice C stage 26): the run's Gold, the candidates at the stagecoach (Recruits), the mercenaries Dismissed and the expeditions
    /// started per dungeon (Expeditions). Version 13 (stage 25): the run's Outcome and DefeatCause, and whether the expedition is the final stand.
    /// Version 11 (Slice B stage 22): HP is in hundredths at the new scale (a longsword deals 2-4) and an item has no grade any more. Version 10
    /// (Slice B stage 19): a member's board is a grid: its Bags (the start bag and those added, each where it lies and how it is
    /// turned) and its Items, each where it lies and how it is turned; an offer can be a Bag. Version 9 (Slice B stage 18): the expedition's Loot (the drops of the last won battle, null where one was taken) replaces
    /// PendingRewards (the reward choice, which is not read any more). Version 8 (stage 17): the expedition's region Coins and, at a
    /// shop, its Shop (the stock and the refreshes).
    /// Version 4 (Slice B): fatigue builds up from 0 (a mercenary's Fatigue was what was left of the
    /// maximum), a member carries its Fatigue on the expedition, and an item says whether it is a base
    /// weapon. Version 3 (boards without empty entries, an Inventory) and version 2 (one entry per slot,
    /// no inventory) are brought up by RunSaveMigrator. Version 1 stored "Front"/"Rear" rows and is not
    /// read any more.
    /// </summary>
    public sealed class RunSaveData
    {
        public const int CurrentSchemaVersion = 28;

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

        /// <summary>RunOutcome and DefeatCause by name (version 13). Version 12 had IsOver instead.</summary>
        public string Outcome;
        public string DefeatCause;

        /// <summary>The lobby's gold (version 14). Never negative.</summary>
        public int Gold;

        /// <summary>The candidates at the stagecoach (version 14): at most RecruitCount names of the recruit pool, each with its rolled job.</summary>
        public List<RecruitRecord> Recruits;

        /// <summary>The mercenaries dismissed from the roster (version 14): they never return.</summary>
        public List<string> Dismissed;

        /// <summary>Expeditions started per dungeon (version 14). A version 13 file knows none.</summary>
        public List<ExpeditionCountRecord> Expeditions;

        /// <summary>The town's buildings above level 1 (version 16; version 17 lists no gate): a building not listed is at level 1.</summary>
        public List<BuildingLevelRecord> Buildings;

        /// <summary>The constructions going on (version 16), at most one per building.</summary>
        public List<ConstructionRecord> Constructions;

        /// <summary>The story's dialogues heard in this run, in the order heard (version 28).</summary>
        public List<string> SeenDialogues;
    }

    public sealed class BuildingLevelRecord
    {
        public string BuildingId;
        public int Level;
    }

    public sealed class ConstructionRecord
    {
        public string BuildingId;
        public int ToLevel;
        public int FinishDay;
    }

    public sealed class RecruitRecord
    {
        public string Id;
        public string JobId;

        /// <summary>The stats the candidate would join with (version 18).</summary>
        public StatsRecord Stats;

        /// <summary>The innate traits the candidate was born with (version 22), in their order.</summary>
        public List<string> Traits;
    }

    /// <summary>The six stats (version 18, Slice D stage 32): strength, dexterity, constitution, intellect, spirit, endurance.</summary>
    public sealed class StatsRecord
    {
        public int Str;
        public int Dex;
        public int Con;
        public int Int;
        public int Wis;
        public int End;

        /// <summary>Spirit, written by version 23 and before: RunSaveMigrator reads it into <see cref="Wis"/>. Never written since.</summary>
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? Spi;
    }

    public sealed class ExpeditionCountRecord
    {
        public string DungeonId;
        public int Count;
    }

    public sealed class MercenaryRecord
    {
        public string Id;
        public string JobId;

        /// <summary>0 is fresh; it builds up on expeditions.</summary>
        public int Fatigue;

        /// <summary>The affliction the mercenary came home with (a FatigueState id), or null (version 6).</summary>
        public string Affliction;

        /// <summary>The level, from 1, and the experience toward the next level, 0 at the last (version 15).</summary>
        public int Level;
        public int Exp;

        /// <summary>The six stats (version 18). An expedition member has none of its own: it takes them from here.</summary>
        public StatsRecord Stats;

        /// <summary>Its traits (version 22, TraitData ids), in the order it got them. An expedition member takes them from here too.</summary>
        public List<string> Traits;
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

        /// <summary>The forced final stand (version 13): the map is the boss alone.</summary>
        public bool FinalStand;
        /// <summary>The drops of the last won battle still being picked up (version 9): one entry per drop, null where it was taken. Empty when none is.</summary>
        public List<OfferRecord> Loot;

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
        public List<OfferRecord> Stock;
        public int Refreshes;
    }

    public sealed class MemberRecord
    {
        public string MercenaryId;
        public string JobId;
        public int Row;

        /// <summary>In hundredths (version 11; whole numbers at the old scale before).</summary>
        public int MaxHp;
        public int Hp;
        public bool Alive;

        /// <summary>The items on the board, each with where it lies (version 10). No null entries. Version 9 kept them in board order, without places.</summary>
        public List<ItemRecord> Items;

        /// <summary>The bags in the board's frame (version 10): the start bag at the top-left first, then those added.</summary>
        public List<BagRecord> Bags;

        /// <summary>The fatigue the member carries on the expedition. It goes to the roster at the settlement.</summary>
        public int Fatigue;

        /// <summary>The affliction or virtue the member is in (a FatigueState id), or null (version 6).</summary>
        public string State;

        /// <summary>Whether the member was at death's door in a battle of this expedition (version 23).</summary>
        public bool DeathsDoor;
    }

    public sealed class ItemRecord
    {
        public string ItemId;

        /// <summary>True for a base weapon: the job's weapon given when the expedition left.</summary>
        public bool Base;

        /// <summary>The tier's name: Common, Tier1, Tier2 or Tier3 (version 12; version 7 named them Common, Bronze, Silver, Gold and version 5 Bronze, Silver, Gold, Diamond). An entry that does not name one is Common, as every item was before.</summary>
        public string Tier = "Common";

        /// <summary>The top-left square the item lies from and its quarter turns clockwise: on a board (version 10) and in the inventory's grid (round 49).</summary>
        public int X;
        public int Y;
        public int Turns;
    }

    /// <summary>A bag in a board's frame (version 10): the bag id, the top-left square it lies from and its quarter turns clockwise.</summary>
    public sealed class BagRecord
    {
        public string BagId;
        public int X;
        public int Y;
        public int Turns;
    }

    public sealed class OfferRecord
    {
        /// <summary>Item, Potion or Bag (version 10).</summary>
        public string Kind;
        public string Id;

        /// <summary>The tier's name of an item on offer; Common for a potion or a bag (version 12, as above). An entry that does not name one is Common.</summary>
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
