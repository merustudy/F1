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

        /// <summary>Takes the first node offered. The map is random, so this is an unbiased path.</summary>
        public static MapNode ChooseNode(List<MapNode> available)
        {
            return available[0];
        }

        /// <summary>
        /// Takes a potion while a slot is free and fewer than two are held, otherwise the first item
        /// that fits an empty slot of a mercenary standing in a row the item works in.
        /// </summary>
        public void ChooseReward(StaticData data, ExpeditionState state)
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
                    return;
                }
            }

            for (int i = 0; i < state.PendingRewards.Count; i++)
            {
                RewardOption option = state.PendingRewards[i];
                if (option.Kind != RewardKind.Item)
                {
                    continue;
                }

                ItemData item = data.Items.Get(option.Id);
                for (int m = 0; m < state.Members.Count; m++)
                {
                    ExpeditionMember member = state.Members[m];
                    if (!member.Alive || !item.UsableIn(member.Row))
                    {
                        continue;
                    }

                    int slot = Array.IndexOf(member.Items, null);
                    if (slot >= 0)
                    {
                        ExpeditionRules.TakeItemReward(data, state, i, m, slot);
                        return;
                    }
                }
            }

            ExpeditionRules.SkipReward(state);
        }
    }
}
