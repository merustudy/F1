using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;

namespace F1.Sim
{
    /// <summary>
    /// Stands in for the player. Policies are simulator tools, not game rules: they only make the
    /// inputs a player could make, through the same engine API the game uses.
    /// </summary>
    public sealed class SimPolicy
    {
        /// <summary>No player input at all.</summary>
        public static readonly SimPolicy None = new SimPolicy("none", 0, 0, 0, false, false);

        /// <summary>
        /// Drinks when someone is at death's door or badly hurt. Leaves after a death, or when two are
        /// at death's door with no potion left.
        /// </summary>
        public static readonly SimPolicy Balanced = new SimPolicy("balanced", 30, 2, 1, false, true);

        /// <summary>
        /// Drinks earlier. Leaves after a death, or as soon as one mercenary is past their grace at
        /// death's door with no potion left.
        /// </summary>
        public static readonly SimPolicy Safe = new SimPolicy("safe", 50, 1, 1, true, true);

        SimPolicy(string name, int potionHpPercent, int retreatDogCount, int retreatDeathCount, bool countOnlyPastGrace, bool active)
        {
            Name = name;
            PotionHpPercent = potionHpPercent;
            RetreatDogCount = retreatDogCount;
            RetreatDeathCount = retreatDeathCount;
            CountOnlyPastGrace = countOnlyPastGrace;
            Active = active;
        }

        public string Name { get; }

        /// <summary>Use a heal potion on a mercenary at or below this HP percent.</summary>
        public int PotionHpPercent { get; }

        /// <summary>Retreat when this many mercenaries are at death's door and no potion can help.</summary>
        public int RetreatDogCount { get; }

        /// <summary>Retreat when this many mercenaries have died in this battle.</summary>
        public int RetreatDeathCount { get; }

        /// <summary>Count a mercenary at death's door only once their grace is over, when hits start to roll.</summary>
        public bool CountOnlyPastGrace { get; }

        public bool Active { get; }

        public static SimPolicy Parse(string name)
        {
            switch (name)
            {
                case "none": return None;
                case "balanced": return Balanced;
                case "safe": return Safe;
                default: throw new ArgumentException($"Unknown policy '{name}'. Use none, balanced or safe.");
            }
        }

        /// <summary>Makes the inputs this policy wants at the current battle time.</summary>
        public void Act(BattleEngine battle)
        {
            if (!Active || battle.Result != BattleResult.Ongoing)
            {
                return;
            }

            UsePotionIfNeeded(battle);
            if (battle.Result == BattleResult.Ongoing && ShouldRetreat(battle))
            {
                battle.TryRetreat();
            }
        }

        void UsePotionIfNeeded(BattleEngine battle)
        {
            BattleUnit target = null;
            foreach (BattleUnit unit in battle.Party)
            {
                if (unit.Alive && unit.InDog)
                {
                    target = unit;
                    break;
                }
            }

            bool targetInDog = target != null;
            if (target == null)
            {
                foreach (BattleUnit unit in battle.Party)
                {
                    if (unit.Alive && unit.Hp * 100 <= unit.MaxHp * PotionHpPercent && (target == null || unit.Hp * target.MaxHp < target.Hp * unit.MaxHp))
                    {
                        target = unit;
                    }
                }
            }

            if (target == null)
            {
                return;
            }

            // A heal answers low HP and death's door; a shield is only worth it at death's door.
            int slot = FindPotion(battle, PotionEffect.Heal);
            if (slot < 0 && targetInDog)
            {
                slot = FindPotion(battle, PotionEffect.Shield);
            }

            if (slot >= 0)
            {
                battle.TryUsePotion(slot, target.Index);
            }
        }

        bool ShouldRetreat(BattleEngine battle)
        {
            if (!battle.CanRetreat)
            {
                return false;
            }

            int dead = 0;
            int inDog = 0;
            foreach (BattleUnit unit in battle.Party)
            {
                if (!unit.Alive)
                {
                    dead++;
                }
                else if (unit.InDog && (!CountOnlyPastGrace || battle.TimeMs >= unit.GraceEndMs))
                {
                    inDog++;
                }
            }

            if (dead >= RetreatDeathCount)
            {
                return true;
            }

            return inDog >= RetreatDogCount && !HasAnyPotion(battle);
        }

        static int FindPotion(BattleEngine battle, PotionEffect effect)
        {
            for (int i = 0; i < battle.Potions.Count; i++)
            {
                if (battle.Potions[i] != null && battle.Potions[i].Effect == effect)
                {
                    return i;
                }
            }

            return -1;
        }

        static bool HasAnyPotion(BattleEngine battle)
        {
            for (int i = 0; i < battle.Potions.Count; i++)
            {
                if (battle.Potions[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Runs a battle to its end, letting the policy act after every automatic event.</summary>
        public void RunBattle(BattleEngine battle)
        {
            Act(battle);
            while (battle.Result == BattleResult.Ongoing)
            {
                battle.AdvanceTo(battle.NextAutomaticEventMs());
                Act(battle);
            }
        }

        /// <summary>A living mercenary under this share of their maximum HP makes the party rest at a camp rather than mend an item.</summary>
        const int RestBelowHpPercent = 50;

        /// <summary>
        /// At a camp: rests when someone is badly hurt or at or over the fatigue threshold (or without player input); otherwise
        /// mends (a tier up) the first item below Gold that works where its owner stands, from the front member back. Returns
        /// true for mending.
        /// </summary>
        public bool ChooseAtCamp(StaticData data, ExpeditionState state)
        {
            bool hurt = false;
            foreach (ExpeditionMember member in state.Members)
            {
                hurt |= member.Alive && (member.Hp * 100 < member.MaxHp * RestBelowHpPercent || member.Fatigue >= data.Balance.FatigueBreakdown);
            }

            if (Active && !hurt)
            {
                int living = ExpeditionRules.LivingCount(state);
                var order = new List<int>();
                for (int m = 0; m < state.Members.Count; m++)
                {
                    order.Add(m);
                }

                order.Sort((a, b) => state.Members[a].Row.CompareTo(state.Members[b].Row));
                foreach (int m in order)
                {
                    ExpeditionMember member = state.Members[m];
                    int cell = 0;
                    foreach (EquippedItem item in member.Items)
                    {
                        if (item.Item.UsableIn(member.Row, living) && ExpeditionRules.CanUpgradeAtCamp(state, m, cell))
                        {
                            ExpeditionRules.UpgradeAtCamp(state, m, cell);
                            return true;
                        }

                        cell += item.Item.Size;
                    }
                }
            }

            ExpeditionRules.RestAtCamp(data, state);
            return false;
        }

        /// <summary>Takes the first node offered. The map is random, so this is an unbiased path.</summary>
        public static MapNode ChooseNode(List<MapNode> available)
        {
            return available[0];
        }

        /// <summary>
        /// Takes a potion while a slot is free and fewer than two are held, otherwise an item that merges into one on a board
        /// (a tier up), otherwise the first item that fits the free cells of a mercenary standing where the item works,
        /// otherwise the first item that fits the inventory, otherwise skips. Then merges what can merge and equips from the
        /// inventory whatever fits someone. Returns how many merges it made.
        /// </summary>
        public int ChooseReward(StaticData data, ExpeditionState state)
        {
            int potionsHeld = 0;
            foreach (string potion in state.Potions)
            {
                if (potion != null)
                {
                    potionsHeld++;
                }
            }

            for (int i = 0; i < state.PendingRewards.Count; i++)
            {
                if (state.PendingRewards[i].Kind == RewardKind.Potion && Active && potionsHeld < 2 && ExpeditionRules.FreePotionSlot(state) >= 0)
                {
                    ExpeditionRules.TakePotionReward(state, i);
                    return 0;
                }
            }

            for (int i = 0; i < state.PendingRewards.Count; i++)
            {
                RewardOption option = state.PendingRewards[i];
                if (option.Kind == RewardKind.Item && Active)
                {
                    var offered = new EquippedItem(data.Items.Get(option.Id), option.Grade, tier: option.Tier);
                    if (FindMergeTarget(state, offered, out int targetMember, out int targetCell))
                    {
                        ExpeditionRules.TakeItemReward(data, state, i, targetMember, targetCell);
                        return 1 + Tidy(data, state);
                    }
                }
            }

            for (int i = 0; i < state.PendingRewards.Count; i++)
            {
                RewardOption option = state.PendingRewards[i];
                if (option.Kind != RewardKind.Item)
                {
                    continue;
                }

                int member = MemberWithRoomFor(state, data.Items.Get(option.Id));
                if (member >= 0)
                {
                    ExpeditionRules.TakeItemReward(data, state, i, member, ItemBoard.UsedCells(state.Members[member].Items));
                    return Tidy(data, state);
                }
            }

            for (int i = 0; i < state.PendingRewards.Count; i++)
            {
                if (ExpeditionRules.CanTakeRewardToInventory(data, state, i))
                {
                    ExpeditionRules.TakeItemRewardToInventory(data, state, i);
                    return Tidy(data, state);
                }
            }

            ExpeditionRules.SkipReward(state);
            return 0;
        }

        /// <summary>
        /// After a reward: merges what can merge (an inventory item into the same one on a board, then an item on a board into
        /// the next same one found on the boards), then equips from the inventory. Without player input only the equipping.
        /// Returns how many merges it made.
        /// </summary>
        int Tidy(StaticData data, ExpeditionState state)
        {
            int merges = 0;
            bool merged = Active;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < state.Inventory.Count && !merged; i++)
                {
                    if (FindMergeTarget(state, state.Inventory[i], out int member, out int cell))
                    {
                        ExpeditionRules.PlaceFromInventory(data, state, i, member, cell);
                        merged = true;
                    }
                }

                for (int m = 0; m < state.Members.Count && !merged; m++)
                {
                    if (!state.Members[m].Alive)
                    {
                        continue;
                    }

                    int cell = 0;
                    foreach (EquippedItem item in state.Members[m].Items)
                    {
                        if (FindMergeTarget(state, item, out int member, out int targetCell) && (member != m || targetCell != cell))
                        {
                            ExpeditionRules.MoveItem(state, m, cell, member, targetCell);
                            merged = true;
                            break;
                        }

                        cell += item.Item.Size;
                    }
                }

                merges += merged ? 1 : 0;
            }

            EquipFromInventory(data, state);
            return merges;
        }

        /// <summary>The first item on a living member's board (from the first member, in board order) that the given item merges into.</summary>
        static bool FindMergeTarget(ExpeditionState state, EquippedItem item, out int member, out int cell)
        {
            for (member = 0; member < state.Members.Count; member++)
            {
                if (!state.Members[member].Alive)
                {
                    continue;
                }

                cell = 0;
                foreach (EquippedItem there in state.Members[member].Items)
                {
                    if (ExpeditionRules.CanMerge(item, there))
                    {
                        return true;
                    }

                    cell += there.Item.Size;
                }
            }

            cell = -1;
            return false;
        }

        /// <summary>The first living member standing where the item works with free cells for it, or -1.</summary>
        static int MemberWithRoomFor(ExpeditionState state, ItemData item)
        {
            int living = ExpeditionRules.LivingCount(state);
            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (member.Alive && item.UsableIn(member.Row, living) && ItemBoard.FreeCells(member.Items, member.ItemSlots) >= item.Size)
                {
                    return m;
                }
            }

            return -1;
        }

        /// <summary>Puts every inventory item that fits someone's free cells on that board, in inventory order.</summary>
        static void EquipFromInventory(StaticData data, ExpeditionState state)
        {
            for (int i = 0; i < state.Inventory.Count;)
            {
                int member = MemberWithRoomFor(state, state.Inventory[i].Item);
                if (member < 0)
                {
                    i++;
                    continue;
                }

                ExpeditionRules.PlaceFromInventory(data, state, i, member, ItemBoard.UsedCells(state.Members[member].Items));
            }
        }
    }
}
