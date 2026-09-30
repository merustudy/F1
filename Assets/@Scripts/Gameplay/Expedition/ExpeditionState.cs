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
        public RewardOption(RewardKind kind, string id, int grade)
        {
            Kind = kind;
            Id = id;
            Grade = grade;
        }

        public RewardKind Kind { get; }

        /// <summary>Item id or potion id.</summary>
        public string Id { get; }

        /// <summary>Item grade. 0 for a potion.</summary>
        public int Grade { get; }
    }

    /// <summary>A mercenary while on an expedition. HP and items exist only here.</summary>
    public sealed class ExpeditionMember
    {
        public string MercenaryId;
        public string JobId;
        public BattleRow Row;
        public int MaxHp;
        public int Hp;
        public bool Alive;

        /// <summary>Item slots. A null entry is empty.</summary>
        public EquippedItem[] Items;
    }

    /// <summary>Who goes on an expedition and where they stand.</summary>
    public readonly struct PartyMember
    {
        public PartyMember(string mercenaryId, string jobId, BattleRow row)
        {
            MercenaryId = mercenaryId;
            JobId = jobId;
            Row = row;
        }

        public string MercenaryId { get; }
        public string JobId { get; }
        public BattleRow Row { get; }
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

        public ExpeditionPhase Phase;
        public ExpeditionResult Result;

        /// <summary>The node being fought or last cleared. -1 before the first choice.</summary>
        public int CurrentNodeId = -1;

        public int BattlesWon;

        /// <summary>Offered while <see cref="Phase"/> is ChoosingReward; empty otherwise.</summary>
        public List<RewardOption> PendingRewards = new List<RewardOption>();
    }
}
