using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The expedition rules (Docs/Design/03_Dungeon_Structure.md): node choice, battles, rewards,
    /// item board and how an expedition ends. Pure functions over <see cref="ExpeditionState"/>.
    /// A command that is not allowed in the current state throws; callers check first.
    /// </summary>
    public static class ExpeditionRules
    {
        public static ExpeditionState Create(StaticData data, string dungeonId, ulong seed, IReadOnlyList<PartyMember> party)
        {
            DungeonData dungeon = data.Dungeons.Get(dungeonId);
            BalanceData balance = data.Balance;
            if (party == null || party.Count < balance.MinPartySize || party.Count > balance.PartySize)
            {
                throw new ArgumentException($"A party has {balance.MinPartySize}..{balance.PartySize} members.", nameof(party));
            }

            var state = new ExpeditionState
            {
                DungeonId = dungeon.Id,
                Seed = seed,
                Potions = new string[balance.PotionSlots],
                Phase = ExpeditionPhase.ChoosingNode,
                Result = ExpeditionResult.None,
            };

            var seen = new HashSet<string>();
            foreach (PartyMember member in party)
            {
                if (!seen.Add(member.MercenaryId))
                {
                    throw new ArgumentException($"Mercenary '{member.MercenaryId}' is in the party twice.", nameof(party));
                }

                JobData job = data.Jobs.Get(member.JobId);
                var items = new EquippedItem[job.ItemSlots];
                items[0] = new EquippedItem(data.Items.Get(job.WeaponItemId), job.WeaponGrade);
                state.Members.Add(new ExpeditionMember
                {
                    MercenaryId = member.MercenaryId,
                    JobId = job.Id,
                    Row = member.Row,
                    MaxHp = job.MaxHp,
                    Hp = job.MaxHp,
                    Alive = true,
                    Items = items,
                });
            }

            if (CountInRow(state, BattleRow.Front) > balance.RowCapacity || CountInRow(state, BattleRow.Rear) > balance.RowCapacity)
            {
                throw new ArgumentException($"A row holds at most {balance.RowCapacity} mercenaries.", nameof(party));
            }

            for (int i = 0; i < dungeon.StartingPotions.Count; i++)
            {
                state.Potions[i] = dungeon.StartingPotions[i];
            }

            state.Map = MapGenerator.Generate(data, dungeon, seed);
            return state;
        }

        /// <summary>Nodes that can be chosen now: the first floor at the start, then the nodes the current one leads to.</summary>
        public static List<MapNode> AvailableNodes(ExpeditionState state)
        {
            var nodes = new List<MapNode>();
            if (state.Phase != ExpeditionPhase.ChoosingNode)
            {
                return nodes;
            }

            if (state.CurrentNodeId < 0)
            {
                return state.Map.OnFloor(1);
            }

            foreach (int nodeId in state.Map.Get(state.CurrentNodeId).NextNodeIds)
            {
                nodes.Add(state.Map.Get(nodeId));
            }

            return nodes;
        }

        public static BattleSetup BeginBattle(StaticData data, ExpeditionState state, int nodeId)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingNode);
            if (!AvailableNodes(state).Exists(node => node.Id == nodeId))
            {
                throw new InvalidOperationException($"Node {nodeId} cannot be chosen now.");
            }

            state.CurrentNodeId = nodeId;
            state.Phase = ExpeditionPhase.InBattle;
            return BuildBattleSetup(data, state);
        }

        /// <summary>
        /// The setup of the battle in progress. It is a pure function of the state, so a resumed
        /// expedition rebuilds exactly the same battle.
        /// </summary>
        public static BattleSetup BuildBattleSetup(StaticData data, ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.InBattle);
            MapNode node = state.Map.Get(state.CurrentNodeId);
            return BuildBattleSetup(data, state, node.EnemyGroupId, SeedDeriver.Derive(state.Seed, "battle", node.Id));
        }

        /// <summary>The current party against a given enemy group. The simulator uses this to test one group directly.</summary>
        public static BattleSetup BuildBattleSetup(StaticData data, ExpeditionState state, string enemyGroupId, ulong battleSeed)
        {
            EnemyGroupData group = data.EnemyGroups.Get(enemyGroupId);
            DungeonData dungeon = data.Dungeons.Get(state.DungeonId);
            AffinityData affinity = data.Affinities.Get(dungeon.AffinityId);

            var party = new List<BattleUnitSetup>();
            AddPartyRow(data, state, BattleRow.Front, party);
            AddPartyRow(data, state, BattleRow.Rear, party);

            var enemies = new List<BattleUnitSetup>();
            AddEnemyRow(data, group.Front, BattleRow.Front, enemies);
            AddEnemyRow(data, group.Rear, BattleRow.Rear, enemies);

            var potions = new List<PotionData>();
            foreach (string potionId in state.Potions)
            {
                potions.Add(potionId == null ? null : data.Potions.Get(potionId));
            }

            return new BattleSetup
            {
                Seed = battleSeed,
                Balance = data.Balance,
                Party = party,
                Enemies = enemies,
                EnemyCooldownPermille = affinity.EnemyCooldownPermille,
                Potions = potions,
            };
        }

        /// <summary>Applies a finished battle: HP, deaths, potions, then the next phase or the end of the expedition.</summary>
        public static void CompleteBattle(StaticData data, ExpeditionState state, BattleEngine battle)
        {
            RequirePhase(state, ExpeditionPhase.InBattle);
            if (battle.Result == BattleResult.Ongoing)
            {
                throw new InvalidOperationException("The battle has not ended.");
            }

            foreach (BattleUnit unit in battle.Party)
            {
                ExpeditionMember member = FindMember(state, unit.Setup.SourceId);
                member.Alive = unit.Alive;
                member.Hp = unit.Alive ? unit.Hp : 0;
            }

            for (int i = 0; i < state.Potions.Length; i++)
            {
                state.Potions[i] = battle.Potions[i]?.Id;
            }

            switch (battle.Result)
            {
                case BattleResult.Defeat:
                    Finish(state, ExpeditionResult.Wiped);
                    return;
                case BattleResult.Retreated:
                    Finish(state, ExpeditionResult.Retreated);
                    return;
            }

            state.BattlesWon++;
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.Alive)
                {
                    int healed = member.Hp + member.MaxHp * data.Balance.PostBattleHealPercent / 100;
                    member.Hp = Math.Max(1, Math.Min(member.MaxHp, healed));
                }
            }

            MapNode node = state.Map.Get(state.CurrentNodeId);
            if (node.Kind == MapNodeKind.Boss)
            {
                Finish(state, ExpeditionResult.Cleared);
                return;
            }

            state.PendingRewards = GenerateRewards(data, state, node);
            state.Phase = state.PendingRewards.Count > 0 ? ExpeditionPhase.ChoosingReward : ExpeditionPhase.ChoosingNode;
        }

        public static void TakeItemReward(StaticData data, ExpeditionState state, int optionIndex, int memberIndex, int slotIndex)
        {
            RewardOption option = RequireReward(state, optionIndex, RewardKind.Item);
            ExpeditionMember member = RequireLivingMember(state, memberIndex);
            RequireSlot(member, slotIndex);

            member.Items[slotIndex] = new EquippedItem(data.Items.Get(option.Id), option.Grade);
            EndReward(state);
        }

        public static void TakePotionReward(ExpeditionState state, int optionIndex)
        {
            RewardOption option = RequireReward(state, optionIndex, RewardKind.Potion);
            int slot = FreePotionSlot(state);
            if (slot < 0)
            {
                throw new InvalidOperationException("There is no empty potion slot.");
            }

            state.Potions[slot] = option.Id;
            EndReward(state);
        }

        public static void SkipReward(ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingReward);
            EndReward(state);
        }

        /// <summary>Swaps the contents of two item slots (either may be empty). Allowed between battles.</summary>
        public static void SwapItems(ExpeditionState state, int memberA, int slotA, int memberB, int slotB)
        {
            RequireBetweenBattles(state);
            ExpeditionMember a = RequireLivingMember(state, memberA);
            ExpeditionMember b = RequireLivingMember(state, memberB);
            RequireSlot(a, slotA);
            RequireSlot(b, slotB);

            EquippedItem moved = a.Items[slotA];
            a.Items[slotA] = b.Items[slotB];
            b.Items[slotB] = moved;
        }

        public static bool CanSetRow(StaticData data, ExpeditionState state, int memberIndex, BattleRow row)
        {
            if (state.Phase != ExpeditionPhase.ChoosingNode && state.Phase != ExpeditionPhase.ChoosingReward)
            {
                return false;
            }

            if (memberIndex < 0 || memberIndex >= state.Members.Count || !state.Members[memberIndex].Alive)
            {
                return false;
            }

            return state.Members[memberIndex].Row == row || CountInRow(state, row) < data.Balance.RowCapacity;
        }

        public static void SetRow(StaticData data, ExpeditionState state, int memberIndex, BattleRow row)
        {
            if (!CanSetRow(data, state, memberIndex, row))
            {
                throw new InvalidOperationException($"Member {memberIndex} cannot move to the {row} row now.");
            }

            state.Members[memberIndex].Row = row;
        }

        /// <summary>Index of the first empty potion slot, or -1.</summary>
        public static int FreePotionSlot(ExpeditionState state)
        {
            return Array.IndexOf(state.Potions, null);
        }

        /// <summary>
        /// Draws the reward choices of a node without repetition, by weight, from the reward stream
        /// of that node. Potions are offered only while a potion slot is empty.
        /// </summary>
        static List<RewardOption> GenerateRewards(StaticData data, ExpeditionState state, MapNode node)
        {
            var rng = new Pcg32(SeedDeriver.Derive(state.Seed, "reward", node.Id), RngStream.Reward);
            int grade = data.Dungeons.Get(state.DungeonId).RewardGradeAt(node.Floor);

            var candidates = new List<RewardOption>();
            var weights = new List<int>();
            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.RewardWeight > 0)
                {
                    candidates.Add(new RewardOption(RewardKind.Item, item.Id, grade));
                    weights.Add(item.RewardWeight);
                }
            }

            if (FreePotionSlot(state) >= 0)
            {
                foreach (PotionData potion in data.Potions.Ordered)
                {
                    if (potion.RewardWeight > 0)
                    {
                        candidates.Add(new RewardOption(RewardKind.Potion, potion.Id, 0));
                        weights.Add(potion.RewardWeight);
                    }
                }
            }

            var options = new List<RewardOption>();
            while (options.Count < data.Balance.RewardChoices && candidates.Count > 0)
            {
                int total = 0;
                foreach (int weight in weights)
                {
                    total += weight;
                }

                int pick = rng.NextInt(total);
                int index = 0;
                while (pick >= weights[index])
                {
                    pick -= weights[index];
                    index++;
                }

                options.Add(candidates[index]);
                candidates.RemoveAt(index);
                weights.RemoveAt(index);
            }

            return options;
        }

        static void AddPartyRow(StaticData data, ExpeditionState state, BattleRow row, List<BattleUnitSetup> party)
        {
            foreach (ExpeditionMember member in state.Members)
            {
                if (!member.Alive || member.Row != row)
                {
                    continue;
                }

                party.Add(new BattleUnitSetup
                {
                    SourceId = member.MercenaryId,
                    Name = data.Mercenaries.Get(member.MercenaryId).Name,
                    Row = row,
                    MaxHp = member.MaxHp,
                    Hp = member.Hp,
                    Items = (EquippedItem[])member.Items.Clone(),
                    Passive = data.Jobs.Get(member.JobId).Passive,
                    HasDog = true,
                });
            }
        }

        static void AddEnemyRow(StaticData data, IReadOnlyList<string> enemyIds, BattleRow row, List<BattleUnitSetup> enemies)
        {
            foreach (string enemyId in enemyIds)
            {
                EnemyData enemy = data.Enemies.Get(enemyId);
                var items = new List<EquippedItem>();
                foreach (ItemGrant grant in enemy.Items)
                {
                    items.Add(new EquippedItem(data.Items.Get(grant.ItemId), grant.Grade));
                }

                enemies.Add(new BattleUnitSetup
                {
                    SourceId = enemy.Id,
                    Name = enemy.Name,
                    Row = row,
                    MaxHp = enemy.MaxHp,
                    Hp = enemy.MaxHp,
                    Items = items,
                    Passive = null,
                    HasDog = false,
                });
            }
        }

        static int CountInRow(ExpeditionState state, BattleRow row)
        {
            int count = 0;
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.Alive && member.Row == row)
                {
                    count++;
                }
            }

            return count;
        }

        static ExpeditionMember FindMember(ExpeditionState state, string mercenaryId)
        {
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.MercenaryId == mercenaryId)
                {
                    return member;
                }
            }

            throw new InvalidOperationException($"Mercenary '{mercenaryId}' is not on this expedition.");
        }

        static void Finish(ExpeditionState state, ExpeditionResult result)
        {
            state.Result = result;
            state.Phase = ExpeditionPhase.Finished;
            state.PendingRewards.Clear();
        }

        static void EndReward(ExpeditionState state)
        {
            state.PendingRewards.Clear();
            state.Phase = ExpeditionPhase.ChoosingNode;
        }

        static void RequirePhase(ExpeditionState state, ExpeditionPhase phase)
        {
            if (state.Phase != phase)
            {
                throw new InvalidOperationException($"Expedition is in phase {state.Phase}, not {phase}.");
            }
        }

        static void RequireBetweenBattles(ExpeditionState state)
        {
            if (state.Phase != ExpeditionPhase.ChoosingNode && state.Phase != ExpeditionPhase.ChoosingReward)
            {
                throw new InvalidOperationException($"Not allowed in phase {state.Phase}.");
            }
        }

        static RewardOption RequireReward(ExpeditionState state, int optionIndex, RewardKind kind)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingReward);
            if (optionIndex < 0 || optionIndex >= state.PendingRewards.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(optionIndex), optionIndex, "Unknown reward option.");
            }

            RewardOption option = state.PendingRewards[optionIndex];
            if (option.Kind != kind)
            {
                throw new InvalidOperationException($"Reward {optionIndex} is a {option.Kind}, not a {kind}.");
            }

            return option;
        }

        static ExpeditionMember RequireLivingMember(ExpeditionState state, int memberIndex)
        {
            if (memberIndex < 0 || memberIndex >= state.Members.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(memberIndex), memberIndex, "Unknown party member.");
            }

            ExpeditionMember member = state.Members[memberIndex];
            if (!member.Alive)
            {
                throw new InvalidOperationException($"Mercenary '{member.MercenaryId}' is dead.");
            }

            return member;
        }

        static void RequireSlot(ExpeditionMember member, int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= member.Items.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(slotIndex), slotIndex, "Unknown item slot.");
            }
        }
    }
}
