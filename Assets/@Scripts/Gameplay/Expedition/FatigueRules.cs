using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// Fatigue (Docs/Design/04_Lobby_100Day_Economy.md §3): it starts at 0, builds up on an expedition and comes
    /// down with rest, between 0 and <c>BalanceData.MaxFatigue</c>. When a battle starts, every living member
    /// pays <c>FatigueBattleEntry</c> and <c>FatigueEquipment</c> for each piece of equipment on their board that
    /// is not a base weapon. Pure functions; the screens ask them too, to show the cost.
    /// </summary>
    public static class FatigueRules
    {
        /// <summary>Equipment (a weapon or an armor) that is not a base weapon: the items that cost fatigue when a battle starts.</summary>
        public static bool CostsFatigue(EquippedItem item)
        {
            return !item.IsBase && (item.Item.Category == ItemCategory.Weapon || item.Item.Category == ItemCategory.Armor);
        }

        /// <summary>What one item adds to its owner's fatigue when a battle starts.</summary>
        public static int ItemCost(BalanceData balance, EquippedItem item)
        {
            return CostsFatigue(item) ? balance.FatigueEquipment : 0;
        }

        /// <summary>What the equipment on a board adds when a battle starts: one cost per item, whatever its size.</summary>
        public static int EquipmentCost(BalanceData balance, IReadOnlyList<EquippedItem> board)
        {
            int cost = 0;
            foreach (EquippedItem item in board)
            {
                cost += ItemCost(balance, item);
            }

            return cost;
        }

        /// <summary>Everything a member pays when a battle starts.</summary>
        public static int BattleEntryCost(BalanceData balance, IReadOnlyList<EquippedItem> board)
        {
            return balance.FatigueBattleEntry + EquipmentCost(balance, board);
        }

        /// <summary>Fatigue after a change, kept within 0..MaxFatigue.</summary>
        public static int Add(BalanceData balance, int fatigue, int amount)
        {
            return Math.Max(0, Math.Min(balance.MaxFatigue, fatigue + amount));
        }

        /// <summary>Whether a state is an affliction that has ended: fatigue came back under the threshold (a virtue lasts until the expedition ends).</summary>
        public static bool AfflictionEnds(BalanceData balance, FatigueStateData state, int fatigue)
        {
            return state != null && state.Kind == FatigueStateKind.Affliction && fatigue < balance.FatigueBreakdown;
        }

        /// <summary>The state id a mercenary keeps after its fatigue came down: the same, or null when its affliction ended.</summary>
        public static string StateAfter(StaticData data, string stateId, int fatigue)
        {
            return stateId != null && AfflictionEnds(data.Balance, data.FatigueStates.Get(stateId), fatigue) ? null : stateId;
        }

        /// <summary>A value changed by a state's percent: value x (100 + percent) / 100, rounded down, never below 0.</summary>
        public static int Scaled(int value, int percent)
        {
            return Math.Max(0, (int)((long)value * (100 + percent) / 100));
        }
    }
}
