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

        /// <summary>True once no mercenary is left alive.</summary>
        public bool IsOver;
    }

    /// <summary>Whether the lobby party can leave. Fatigue does not stop anyone: sending a tired mercenary is the player's call.</summary>
    public enum DepartCheck
    {
        Ok,
        RunIsOver,
        PartyTooSmall,
    }

    /// <summary>What the return settlement changed, for the result screen.</summary>
    public sealed class SettlementReport
    {
        public string DungeonId;
        public ExpeditionResult Result;
        public List<string> FallenIds = new List<string>();
        public List<string> SurvivorIds = new List<string>();

        /// <summary>The fatigue each survivor came back with, in the order of <see cref="SurvivorIds"/>.</summary>
        public List<int> SurvivorFatigue = new List<int>();

        /// <summary>The affliction each survivor came home in (a FatigueStateData id), or null, in the order of <see cref="SurvivorIds"/>. A virtue ended with the expedition.</summary>
        public List<string> SurvivorStates = new List<string>();
        public int DaysPassed;
        public int DayAfter;
        public bool RunIsOver;
    }
}
