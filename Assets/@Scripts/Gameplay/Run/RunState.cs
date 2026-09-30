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

        /// <summary>Remaining fatigue points. Expeditions spend them; days off restore them.</summary>
        public int Fatigue;
    }

    public sealed class PartySlot
    {
        public string MercenaryId;
        public BattleRow Row;
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

        /// <summary>The party chosen in the lobby, in party order.</summary>
        public List<PartySlot> Party = new List<PartySlot>();

        /// <summary>Number of expeditions started. Used to derive each expedition's seed.</summary>
        public int ExpeditionCount;

        /// <summary>Dungeon id -> times cleared.</summary>
        public SortedDictionary<string, int> ClearedDungeons = new SortedDictionary<string, int>(System.StringComparer.Ordinal);

        /// <summary>True once no mercenary is left alive.</summary>
        public bool IsOver;
    }

    public enum DepartCheck
    {
        Ok,
        RunIsOver,
        PartyTooSmall,
        NotEnoughFatigue,
    }

    /// <summary>What the return settlement changed, for the result screen.</summary>
    public sealed class SettlementReport
    {
        public string DungeonId;
        public ExpeditionResult Result;
        public List<string> FallenIds = new List<string>();
        public List<string> SurvivorIds = new List<string>();
        public int FatigueCost;
        public int DaysPassed;
        public int DayAfter;
        public bool RunIsOver;
    }
}
