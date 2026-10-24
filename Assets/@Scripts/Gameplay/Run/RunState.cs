using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>A living mercenary of the roster.</summary>
    public sealed class MercenaryState
    {
        /// <summary>Id of the MercenaryData this mercenary came from.</summary>
        public string Id;
        public string JobId;

        /// <summary>Fatigue: 0 is fresh. It builds up on expeditions and comes down on days off (see <see cref="FatigueRules"/>).</summary>
        public int Fatigue;

        /// <summary>The affliction the mercenary came home with (a FatigueStateData id), or null. It ends when fatigue comes back under the threshold.</summary>
        public string AfflictionId;

        /// <summary>The level (Slice C stage 27, LevelData), from 1, and the experience toward the next one (0 at the last level).</summary>
        public int Level = LevelRules.FirstLevel;
        public int Exp;

        /// <summary>The six stats (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2): the job's base values for a new run's roster, the candidate's for a hired one.</summary>
        public StatBlock Stats;

        /// <summary>Its traits (Slice D stage 37, 05 §6), TraitData ids in the order it got them: the data's for the starting roster, the candidate's for a hired one.</summary>
        public List<string> Traits = new List<string>();

        /// <summary>The figure it is shown in (stage 57): one of its job's, drawn by its name's gender; null for the job's own (a run saved before).</summary>
        public string Figure;
    }

    /// <summary>A candidate at the stagecoach (Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §6): a name of the recruit pool and the job it was drawn with.</summary>
    public sealed class RecruitState
    {
        /// <summary>Id of the MercenaryData row (of the recruit pool) that names the candidate. It is the mercenary's id once hired.</summary>
        public string Id;
        public string JobId;

        /// <summary>The stats the candidate would join with (Slice D stage 32): the job's base values with the stagecoach's spread (stage 34).</summary>
        public StatBlock Stats;

        /// <summary>The innate traits it was born with (Slice D stage 37), drawn with it; they join with it.</summary>
        public List<string> Traits = new List<string>();

        /// <summary>The figure it is shown in (stage 57): one of its job's, drawn by its name's gender; null for the job's own (a run saved before).</summary>
        public string Figure;
    }

    /// <summary>A construction going on at a building of the town (Slice C stage 30): the level it builds and the day it is done.</summary>
    public sealed class ConstructionState
    {
        public string BuildingId;
        public int ToLevel;

        /// <summary>The run day the level arrives: the construction ends the moment the day counter reaches it.</summary>
        public int FinishDay;
    }

    public sealed class PartySlot
    {
        public string MercenaryId;

        /// <summary>The row the mercenary stands in (1 = facing the enemy).</summary>
        public int Row;
    }

    /// <summary>
    /// Plain runtime state of one 100-day run. Only <see cref="RunRules"/> changes it.
    /// The roster holds living mercenaries only; the dead are listed in <see cref="Fallen"/> and never return.
    /// </summary>
    public sealed class RunState
    {
        public ulong Seed;

        /// <summary>Starts at 1.</summary>
        public int Day;

        public List<MercenaryState> Roster = new List<MercenaryState>();
        public List<string> Fallen = new List<string>();

        /// <summary>The party chosen in the lobby, in the order the mercenaries joined. It never has an empty row in front of an occupied one.</summary>
        public List<PartySlot> Party = new List<PartySlot>();

        /// <summary>Number of expeditions started. Used to derive each expedition's seed.</summary>
        public int ExpeditionCount;

        /// <summary>Dungeon id -> times cleared.</summary>
        public SortedDictionary<string, int> ClearedDungeons = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>Dungeon id -> expeditions started there (Slice C stage 26, for the result window). The sum is <see cref="ExpeditionCount"/>.</summary>
        public SortedDictionary<string, int> ExpeditionsByDungeon = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>The lobby's gold (Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §5): paid by the return settlement, spent on hiring and treatment. A run starts with none.</summary>
        public int Gold;

        /// <summary>The candidates at the stagecoach, drawn anew at each settlement (§6). Hiring takes one out.</summary>
        public List<RecruitState> Recruits = new List<RecruitState>();

        /// <summary>Mercenaries dismissed from the roster: they never come back, not even as candidates (§6).</summary>
        public List<string> Dismissed = new List<string>();

        /// <summary>The town's buildings and their levels (Slice C stage 30, BuildingRules): building id -> level. A building not listed is at level 1.</summary>
        public SortedDictionary<string, int> BuildingLevels = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>The constructions going on, at most one per building (stage 30).</summary>
        public List<ConstructionState> Constructions = new List<ConstructionState>();

        /// <summary>The story's dialogues heard in this run, in the order they were heard (Slice D stage 55). Only StoryRules changes it.</summary>
        public List<string> SeenDialogues = new List<string>();

        /// <summary>How the run stands (Slice C stage 25, Docs/Design/04_Lobby_100Day_Economy.md §8): ongoing, won at the final boss, or lost.</summary>
        public RunOutcome Outcome;

        /// <summary>Why a lost run was lost; None while it is ongoing or won.</summary>
        public DefeatCause DefeatCause;

        /// <summary>True once the run is won or lost.</summary>
        public bool IsOver => Outcome != RunOutcome.Ongoing;
    }

    public enum RunOutcome
    {
        Ongoing,
        Victory,
        Defeat,
    }

    /// <summary>What ended a lost run: the last mercenary died, the deaths reached the limit, or the final stand after the last day was lost.</summary>
    public enum DefeatCause
    {
        None,
        Wipeout,
        DeathLimit,
        FinalStandLost,
    }

    /// <summary>
    /// Whether the lobby party can leave. Fatigue does not stop anyone: sending a tired mercenary is the player's call.
    /// Past the last day only the final stand is left (FinalStandDue), and a dungeon stays closed until the clears that open it (Locked).
    /// </summary>
    public enum DepartCheck
    {
        Ok,
        RunIsOver,
        PartyTooSmall,
        FinalStandDue,
        Locked,
    }

    /// <summary>Whether a candidate can be hired now (Slice C stage 26).</summary>
    public enum HireCheck
    {
        Ok,
        RunIsOver,
        NoCandidate,
        RosterFull,
        NotEnoughGold,

        /// <summary>The stagecoach is under construction (stage 30).</summary>
        CoachLocked,
    }

    /// <summary>Whether a mercenary can be treated at the inn now (Slice C stage 26).</summary>
    public enum TreatCheck
    {
        Ok,
        RunIsOver,
        NotInRoster,
        NotTired,
        NotEnoughGold,

        /// <summary>The inn is under construction (stage 30): no treatment, resting goes on.</summary>
        InnLocked,
    }

    /// <summary>The traits a survivor acquired at a settlement (Slice D stage 38), in the order they were rolled.</summary>
    public sealed class AcquiredTraits
    {
        public string MercenaryId;
        public List<string> TraitIds = new List<string>();
    }

    /// <summary>What the return settlement changed, for the result screen.</summary>
    public sealed class SettlementReport
    {
        public string DungeonId;
        public ExpeditionResult Result;

        /// <summary>The gold the settlement paid (Slice C stage 26): for the map floors the party reached and for a clear, and what the company holds after it.</summary>
        public int GoldEarned;
        public int FloorsReached;
        public int GoldAfter;

        /// <summary>
        /// A cleared expedition's region coins turned into gold (2026-10-17): what the shop would have paid for the items brought home,
        /// the coins converted (those left plus the items'), and the gold they made (counted in GoldEarned). All 0 unless cleared.
        /// </summary>
        public int CoinsFromItems;
        public int CoinsConverted;
        public int GoldFromCoins;

        /// <summary>The buildings whose construction the passing days finished (stage 30).</summary>
        public List<string> BuildingsCompleted = new List<string>();

        /// <summary>
        /// The experience every survivor got (stage 27), and the survivors whose experience now meets their level's (Slice D stage 35: the barracks
        /// levels them up), in the order of <see cref="SurvivorIds"/>.
        /// </summary>
        public int ExpEarned;
        public List<string> ReadyToLevelUp = new List<string>();
        public List<string> FallenIds = new List<string>();
        public List<string> SurvivorIds = new List<string>();

        /// <summary>The fatigue each survivor came back with, in the order of <see cref="SurvivorIds"/>.</summary>
        public List<int> SurvivorFatigue = new List<int>();

        /// <summary>The affliction each survivor came home in (a FatigueStateData id), or null, in the order of <see cref="SurvivorIds"/>. A virtue ended with the expedition.</summary>
        public List<string> SurvivorStates = new List<string>();

        /// <summary>The survivors the expedition left new traits on (Slice D stage 38), in the order of <see cref="SurvivorIds"/>; none without any.</summary>
        public List<AcquiredTraits> NewTraits = new List<AcquiredTraits>();
        public int DaysPassed;
        public int DayAfter;
        public bool RunIsOver;

        /// <summary>How the run stands after this settlement, and why if it was lost.</summary>
        public RunOutcome Outcome;
        public DefeatCause DefeatCause;
    }
}
