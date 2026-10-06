using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The expedition rules (Docs/Design/03_Dungeon_Structure.md): node choice, battles, rewards,
    /// item boards and the inventory, and how an expedition ends. Pure functions over
    /// <see cref="ExpeditionState"/>. A command that is not allowed in the current state throws;
    /// callers check first with the matching Can... query.
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

                if (member.Fatigue < 0 || member.Fatigue > balance.MaxFatigue)
                {
                    throw new ArgumentException($"Fatigue of '{member.MercenaryId}' is outside 0..{balance.MaxFatigue}.", nameof(party));
                }

                JobData job = data.Jobs.Get(member.JobId);
                state.Members.Add(new ExpeditionMember
                {
                    MercenaryId = member.MercenaryId,
                    JobId = job.Id,
                    Row = member.Row,
                    MaxHp = job.MaxHp,
                    Hp = job.MaxHp,
                    Alive = true,
                    Items = new List<EquippedItem> { new EquippedItem(data.Items.Get(job.WeaponItemId), job.WeaponGrade, isBase: true) },
                    ItemSlots = job.ItemSlots,
                    Fatigue = member.Fatigue,
                    StateId = member.AfflictionId,
                });
            }

            string problem = Formation.Problem(LivingRows(state, out _));
            if (problem != null)
            {
                throw new ArgumentException(problem, nameof(party));
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

        /// <summary>
        /// Enters a node that is fought (a battle, an elite, the boss) and starts its battle. Every living member pays the
        /// fatigue of going into a battle with the board it carries (<see cref="FatigueRules.BattleEntryCost"/>) before the
        /// battle is set up.
        /// </summary>
        public static BattleSetup BeginBattle(StaticData data, ExpeditionState state, int nodeId)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingNode);
            if (!AvailableNodes(state).Exists(node => node.Id == nodeId))
            {
                throw new InvalidOperationException($"Node {nodeId} cannot be chosen now.");
            }

            if (!state.Map.Get(nodeId).IsFought)
            {
                throw new InvalidOperationException($"Node {nodeId} is a camp: nobody is fought there.");
            }

            state.CurrentNodeId = nodeId;
            state.Phase = ExpeditionPhase.InBattle;
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.Alive)
                {
                    member.Fatigue = FatigueRules.Add(data.Balance, member.Fatigue, FatigueRules.BattleEntryCost(data.Balance, member.Items));
                }
            }

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
            return BuildBattleSetup(data, state, node.EnemyGroupId, SeedDeriver.Derive(state.Seed, "battle", node.Id), node.Floor);
        }

        /// <summary>
        /// The current party against a given enemy group on a floor. The simulator uses this to test one group directly.
        /// The enemies of a floor deeper than the first are stronger by the dungeon's per-floor share; the boss is as its data says.
        /// </summary>
        public static BattleSetup BuildBattleSetup(StaticData data, ExpeditionState state, string enemyGroupId, ulong battleSeed, int floor = 1)
        {
            EnemyGroupData group = data.EnemyGroups.Get(enemyGroupId);
            DungeonData dungeon = data.Dungeons.Get(state.DungeonId);
            AffinityData affinity = data.Affinities.Get(dungeon.AffinityId);

            // Both sides are listed from row 1 back: the unit order of the battle.
            var party = new List<BattleUnitSetup>();
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                AddPartyRow(data, state, row, party);
            }

            int deeper = group.IsBoss ? 0 : Math.Max(0, floor - 1);
            var enemies = new List<BattleUnitSetup>();
            for (int i = 0; i < group.Enemies.Count; i++)
            {
                enemies.Add(EnemySetup(data.Enemies.Get(group.Enemies[i]), BattleRows.Front + i, data, dungeon, deeper));
            }

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
                FatigueStates = data.FatigueStates.Ordered,
            };
        }

        /// <summary>Enters a camp node (Docs/Design/03_Dungeon_Structure.md §1). The party stays there until it chooses what to do.</summary>
        public static void EnterCamp(ExpeditionState state, int nodeId)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingNode);
            if (!AvailableNodes(state).Exists(node => node.Id == nodeId))
            {
                throw new InvalidOperationException($"Node {nodeId} cannot be chosen now.");
            }

            if (state.Map.Get(nodeId).Kind != MapNodeKind.Camp)
            {
                throw new InvalidOperationException($"Node {nodeId} is not a camp.");
            }

            state.CurrentNodeId = nodeId;
            state.Phase = ExpeditionPhase.AtCamp;
        }

        /// <summary>
        /// Rests at the camp: every living member gets back <c>CampHealPercent</c> of their maximum HP and their fatigue comes down by
        /// <c>CampFatigueRelief</c>. Then the party goes on to the next floor.
        /// </summary>
        public static void RestAtCamp(StaticData data, ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.AtCamp);
            BalanceData balance = data.Balance;
            foreach (ExpeditionMember member in state.Members)
            {
                if (!member.Alive)
                {
                    continue;
                }

                member.Hp = Math.Min(member.MaxHp, member.Hp + member.MaxHp * balance.CampHealPercent / 100);
                member.Fatigue = FatigueRules.Add(balance, member.Fatigue, -balance.CampFatigueRelief);
                member.StateId = FatigueRules.StateAfter(data, member.StateId, member.Fatigue);
            }

            state.Phase = ExpeditionPhase.ChoosingNode;
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

                // Those who advanced during the battle keep the row they ended in; what the battle did to their fatigue stays too.
                member.Row = unit.Row;
                member.Fatigue = unit.Fatigue;
                member.StateId = unit.State?.Id;
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

        /// <summary>
        /// Whether an item reward could be put at a cell of a member's board: into the free cells, or
        /// in place of the item there, which goes to the inventory and so must fit its free cells
        /// (Docs/Design/03_Dungeon_Structure.md §5).
        /// </summary>
        public static bool CanPlaceReward(StaticData data, ExpeditionState state, int optionIndex, int memberIndex, int cell)
        {
            if (state.Phase != ExpeditionPhase.ChoosingReward || optionIndex < 0 || optionIndex >= state.PendingRewards.Count)
            {
                return false;
            }

            RewardOption option = state.PendingRewards[optionIndex];
            return option.Kind == RewardKind.Item
                && (MergesInto(option.Id, option.Tier, LivingItemAt(state, memberIndex, cell))
                    || CanPlaceItem(data, state, data.Items.Get(option.Id).Size, memberIndex, cell));
        }

        /// <summary>
        /// Takes an item reward onto a cell of a member's board: into the free cells or in place of the item there (which goes to
        /// the inventory), or, onto the same item at the same tier, merged with it a tier up.
        /// </summary>
        public static void TakeItemReward(StaticData data, ExpeditionState state, int optionIndex, int memberIndex, int cell)
        {
            RewardOption option = RequireReward(state, optionIndex, RewardKind.Item);
            ExpeditionMember member = RequireLivingMember(state, memberIndex);
            var item = new EquippedItem(data.Items.Get(option.Id), option.Grade, tier: option.Tier);
            EquippedItem there = LivingItemAt(state, memberIndex, cell);
            if (CanMerge(item, there))
            {
                Merge(member.Items, item, there, null);
                EndReward(state);
                return;
            }

            if (!CanPlaceItem(data, state, item.Item.Size, memberIndex, cell))
            {
                throw new InvalidOperationException($"'{item.Item.Id}' cannot go at cell {cell} of '{member.MercenaryId}': it does not fit there, or what is there would not fit the inventory.");
            }

            PutOnBoard(state, member, cell, item);
            EndReward(state);
        }

        /// <summary>Whether an item reward could be taken straight into the inventory: it must fit the inventory's free cells.</summary>
        public static bool CanTakeRewardToInventory(StaticData data, ExpeditionState state, int optionIndex)
        {
            if (state.Phase != ExpeditionPhase.ChoosingReward || optionIndex < 0 || optionIndex >= state.PendingRewards.Count)
            {
                return false;
            }

            RewardOption option = state.PendingRewards[optionIndex];
            return option.Kind == RewardKind.Item && data.Items.Get(option.Id).Size <= FreeInventoryCells(data, state);
        }

        /// <summary>Takes an item reward straight into the inventory.</summary>
        public static void TakeItemRewardToInventory(StaticData data, ExpeditionState state, int optionIndex)
        {
            RewardOption option = RequireReward(state, optionIndex, RewardKind.Item);
            var item = new EquippedItem(data.Items.Get(option.Id), option.Grade, tier: option.Tier);
            if (item.Item.Size > FreeInventoryCells(data, state))
            {
                throw new InvalidOperationException($"'{item.Item.Id}' does not fit the inventory's free cells.");
            }

            state.Inventory.Add(item);
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

        /// <summary>
        /// Whether an item of a size could be put at a cell of a member's board now: between battles,
        /// a living member, into the free cells or in place of the item there, which must then fit
        /// the inventory's free cells.
        /// </summary>
        public static bool CanPlaceItem(StaticData data, ExpeditionState state, int size, int memberIndex, int cell)
        {
            return CanPlaceItem(state, size, memberIndex, cell, FreeInventoryCells(data, state));
        }

        /// <param name="inventoryRoom">The inventory cells free for whatever the item displaces.</param>
        static bool CanPlaceItem(ExpeditionState state, int size, int memberIndex, int cell, int inventoryRoom)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return false;
            }

            ExpeditionMember member = state.Members[memberIndex];
            return ItemBoard.CanPut(member.Items, member.ItemSlots, cell, size) && SizeAt(member, cell) <= inventoryRoom;
        }

        /// <summary>
        /// Whether the item at a cell of one board can go to a cell of a board (the same or another
        /// member's): into the free cells, or trading places with the item there. Both boards must
        /// hold what they end up with. Allowed between battles.
        /// </summary>
        public static bool CanMoveItem(ExpeditionState state, int fromMember, int fromCell, int toMember, int toCell)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, fromMember) || !IsLivingMember(state, toMember))
            {
                return false;
            }

            ExpeditionMember from = state.Members[fromMember];
            ExpeditionMember to = state.Members[toMember];
            int fromIndex = ItemBoard.IndexAtCell(from.Items, fromCell);
            if (fromIndex < 0 || toCell < 0 || toCell >= to.ItemSlots)
            {
                return false;
            }

            int toIndex = ItemBoard.IndexAtCell(to.Items, toCell);
            if (toIndex >= 0 && CanMerge(from.Items[fromIndex], to.Items[toIndex]))
            {
                return true;
            }

            if (from == to)
            {
                // Within one board the item only changes place: to the end (unless it is there
                // already), or trading with another.
                return toIndex < 0 ? fromIndex != from.Items.Count - 1 : toIndex != fromIndex;
            }

            int size = from.Items[fromIndex].Item.Size;
            if (toIndex < 0)
            {
                return ItemBoard.FreeCells(to.Items, to.ItemSlots) >= size;
            }

            int other = to.Items[toIndex].Item.Size;
            return ItemBoard.UsedCells(to.Items) - other + size <= to.ItemSlots
                && ItemBoard.UsedCells(from.Items) - size + other <= from.ItemSlots;
        }

        public static void MoveItem(ExpeditionState state, int fromMember, int fromCell, int toMember, int toCell)
        {
            if (!CanMoveItem(state, fromMember, fromCell, toMember, toCell))
            {
                throw new InvalidOperationException($"The item at cell {fromCell} of member {fromMember} cannot go to cell {toCell} of member {toMember}.");
            }

            ExpeditionMember from = state.Members[fromMember];
            ExpeditionMember to = state.Members[toMember];
            int fromIndex = ItemBoard.IndexAtCell(from.Items, fromCell);
            int toIndex = ItemBoard.IndexAtCell(to.Items, toCell);
            EquippedItem moved = from.Items[fromIndex];
            if (toIndex >= 0 && CanMerge(moved, to.Items[toIndex]))
            {
                Merge(to.Items, moved, to.Items[toIndex], from.Items);
            }
            else if (toIndex < 0)
            {
                from.Items.RemoveAt(fromIndex);
                to.Items.Add(moved);
            }
            else
            {
                from.Items[fromIndex] = to.Items[toIndex];
                to.Items[toIndex] = moved;
            }
        }

        /// <summary>
        /// Whether the item at a cell of a member's board can be picked up now: between battles, a
        /// living member, a cell that holds an item. Where it may go is asked separately
        /// (<see cref="CanMoveItem"/>, <see cref="CanMoveToInventory"/>).
        /// </summary>
        public static bool CanPickItem(ExpeditionState state, int memberIndex, int cell)
        {
            return IsBetweenBattles(state)
                && IsLivingMember(state, memberIndex)
                && ItemBoard.IndexAtCell(state.Members[memberIndex].Items, cell) >= 0;
        }

        /// <summary>Whether a cell of a member's board holds an item that can go to the inventory now: it must fit the inventory's free cells.</summary>
        public static bool CanMoveToInventory(StaticData data, ExpeditionState state, int memberIndex, int cell)
        {
            return CanPickItem(state, memberIndex, cell)
                && SizeAt(state.Members[memberIndex], cell) <= FreeInventoryCells(data, state);
        }

        /// <summary>Takes the item at a cell off the board into the inventory. The items behind it close up.</summary>
        public static void MoveToInventory(StaticData data, ExpeditionState state, int memberIndex, int cell)
        {
            if (!CanMoveToInventory(data, state, memberIndex, cell))
            {
                throw new InvalidOperationException($"Cell {cell} of member {memberIndex} holds nothing that can go to the inventory now.");
            }

            ExpeditionMember member = state.Members[memberIndex];
            int index = ItemBoard.IndexAtCell(member.Items, cell);
            state.Inventory.Add(member.Items[index]);
            member.Items.RemoveAt(index);
        }

        /// <summary>
        /// Whether an item of the inventory could go at a cell of a member's board now. The cells it
        /// leaves in the inventory are free for whatever it displaces there.
        /// </summary>
        public static bool CanPlaceFromInventory(StaticData data, ExpeditionState state, int inventoryIndex, int memberIndex, int cell)
        {
            if (inventoryIndex < 0 || inventoryIndex >= state.Inventory.Count)
            {
                return false;
            }

            EquippedItem item = state.Inventory[inventoryIndex];
            if (CanMerge(item, LivingItemAt(state, memberIndex, cell)))
            {
                return true;
            }

            int size = item.Item.Size;
            return CanPlaceItem(state, size, memberIndex, cell, FreeInventoryCells(data, state) + size);
        }

        /// <summary>Puts an item of the inventory at a cell of a member's board. An item displaced there goes to the inventory.</summary>
        public static void PlaceFromInventory(StaticData data, ExpeditionState state, int inventoryIndex, int memberIndex, int cell)
        {
            if (!CanPlaceFromInventory(data, state, inventoryIndex, memberIndex, cell))
            {
                throw new InvalidOperationException($"Inventory item {inventoryIndex} cannot go to cell {cell} of member {memberIndex} now.");
            }

            EquippedItem item = state.Inventory[inventoryIndex];
            ExpeditionMember member = state.Members[memberIndex];
            EquippedItem there = LivingItemAt(state, memberIndex, cell);
            if (CanMerge(item, there))
            {
                Merge(member.Items, item, there, state.Inventory);
                return;
            }

            state.Inventory.RemoveAt(inventoryIndex);
            PutOnBoard(state, member, cell, item);
        }

        // ---- Tiers: merging and the camp's upkeep --------------------------------------------

        /// <summary>
        /// Whether an item merges into another (Docs/Design/02_Combat_System.md §4 "합치기"): the same item at the same tier, below
        /// Gold, two different items and neither a base weapon. They become one a tier up where the second one is.
        /// </summary>
        public static bool CanMerge(EquippedItem item, EquippedItem into)
        {
            return item != null && !ReferenceEquals(item, into) && !item.IsBase && MergesInto(item.Item.Id, item.Tier, into);
        }

        /// <summary>Whether putting an item (of a board or the inventory) on a cell of a living member's board would merge it into the item there.</summary>
        public static bool MergesAt(ExpeditionState state, EquippedItem item, int memberIndex, int cell)
        {
            return CanMerge(item, LivingItemAt(state, memberIndex, cell));
        }

        /// <summary>Whether taking an item reward onto a cell of a living member's board would merge it into the item there.</summary>
        public static bool RewardMergesAt(ExpeditionState state, int optionIndex, int memberIndex, int cell)
        {
            if (state.Phase != ExpeditionPhase.ChoosingReward || optionIndex < 0 || optionIndex >= state.PendingRewards.Count)
            {
                return false;
            }

            RewardOption option = state.PendingRewards[optionIndex];
            return option.Kind == RewardKind.Item && MergesInto(option.Id, option.Tier, LivingItemAt(state, memberIndex, cell));
        }

        /// <summary>Whether some cell of a living member's board holds what the item would merge into.</summary>
        public static bool HasMergeTarget(ExpeditionState state, EquippedItem item)
        {
            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (!member.Alive)
                {
                    continue;
                }

                foreach (EquippedItem there in member.Items)
                {
                    if (CanMerge(item, there))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>Whether an item of this id and tier merges into the item given (null for none).</summary>
        static bool MergesInto(string itemId, ItemTier tier, EquippedItem into)
        {
            return into != null && !into.IsBase && into.Item.Id == itemId && into.Tier == tier && tier < ItemTier.Gold;
        }

        /// <summary>
        /// Merges an item into another on a board: the board's item becomes the merge, a tier up at the better grade of the two,
        /// and the item merged in leaves the list it came from (null for a reward, which comes from nowhere).
        /// </summary>
        static void Merge(List<EquippedItem> board, EquippedItem item, EquippedItem into, List<EquippedItem> source)
        {
            int index = board.IndexOf(into);
            board[index] = new EquippedItem(into.Item, Math.Max(item.Grade, into.Grade), tier: into.Tier + 1);
            source?.Remove(item);
        }

        /// <summary>The item at a cell of a living member's board between battles, or null.</summary>
        static EquippedItem LivingItemAt(ExpeditionState state, int memberIndex, int cell)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return null;
            }

            List<EquippedItem> items = state.Members[memberIndex].Items;
            int index = ItemBoard.IndexAtCell(items, cell);
            return index < 0 ? null : items[index];
        }

        /// <summary>Whether the item at a cell of a member's board can go a tier up at the camp (the camp's upkeep): any item below Gold, a base weapon too.</summary>
        public static bool CanUpgradeAtCamp(ExpeditionState state, int memberIndex, int cell)
        {
            EquippedItem item = LivingItemAt(state, memberIndex, cell);
            return state.Phase == ExpeditionPhase.AtCamp && item != null && item.Tier < ItemTier.Gold;
        }

        /// <summary>The camp's upkeep: the item at a cell goes a tier up (a base weapon stays one). Then the party goes on to the next floor.</summary>
        public static void UpgradeAtCamp(ExpeditionState state, int memberIndex, int cell)
        {
            if (!CanUpgradeAtCamp(state, memberIndex, cell))
            {
                throw new InvalidOperationException($"Nothing at cell {cell} of member {memberIndex} can go a tier up now.");
            }

            List<EquippedItem> items = state.Members[memberIndex].Items;
            int index = ItemBoard.IndexAtCell(items, cell);
            items[index] = items[index].TierUp();
            state.Phase = ExpeditionPhase.ChoosingNode;
        }

        /// <summary>
        /// The inventory's free cells. It has <c>BalanceData.InventoryCells</c> and an item takes its
        /// size there as on a board (Docs/Design/03_Dungeon_Structure.md §5).
        /// </summary>
        public static int FreeInventoryCells(StaticData data, ExpeditionState state)
        {
            return data.Balance.InventoryCells - ItemBoard.UsedCells(state.Inventory);
        }

        /// <summary>How many members are alive. They stand in rows 1..n, which is what a span of the line is counted on.</summary>
        public static int LivingCount(ExpeditionState state)
        {
            int alive = 0;
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.Alive)
                {
                    alive++;
                }
            }

            return alive;
        }

        /// <summary>
        /// Between battles a living member can move to another row where a living member stands.
        /// An empty row cannot be chosen: the party always stands without gaps.
        /// </summary>
        public static bool CanMoveToRow(ExpeditionState state, int memberIndex, int row)
        {
            if (!IsBetweenBattles(state))
            {
                return false;
            }

            if (memberIndex < 0 || memberIndex >= state.Members.Count || !state.Members[memberIndex].Alive)
            {
                return false;
            }

            int[] rows = LivingRows(state, out List<int> members);
            return Formation.CanMove(rows, members.IndexOf(memberIndex), row);
        }

        /// <summary>Moves a member to a row: it trades places with the member standing there.</summary>
        public static void MoveToRow(ExpeditionState state, int memberIndex, int row)
        {
            if (!CanMoveToRow(state, memberIndex, row))
            {
                throw new InvalidOperationException($"Member {memberIndex} cannot move to row {row} now.");
            }

            int[] rows = LivingRows(state, out List<int> members);
            Formation.Move(rows, members.IndexOf(memberIndex), row);
            for (int i = 0; i < rows.Length; i++)
            {
                state.Members[members[i]].Row = rows[i];
            }
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
            DungeonData dungeon = data.Dungeons.Get(state.DungeonId);
            int grade = dungeon.RewardGradeAt(node.Floor);
            ItemTier tier = dungeon.RewardTierAt(node.Floor, node.Kind == MapNodeKind.Elite);

            var candidates = new List<RewardOption>();
            var weights = new List<int>();
            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.RewardWeight > 0)
                {
                    candidates.Add(new RewardOption(RewardKind.Item, item.Id, grade, tier));
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

        static void AddPartyRow(StaticData data, ExpeditionState state, int row, List<BattleUnitSetup> party)
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
                    Items = new List<EquippedItem>(member.Items),
                    ItemSlots = member.ItemSlots,
                    Passive = data.Jobs.Get(member.JobId).Passive,
                    HasDog = true,
                    Fatigue = member.Fatigue,
                    FatigueState = member.StateId == null ? null : data.FatigueStates.Get(member.StateId),
                });
            }
        }

        /// <param name="deeper">How many floors below the first the enemy stands: its HP and item grades grow by the dungeon's share for each.</param>
        static BattleUnitSetup EnemySetup(EnemyData enemy, int row, StaticData data, DungeonData dungeon, int deeper)
        {
            var items = new List<EquippedItem>();
            foreach (ItemGrant grant in enemy.Items)
            {
                items.Add(new EquippedItem(data.Items.Get(grant.ItemId), grant.Grade + deeper * dungeon.EnemyGradePerFloor));
            }

            // An enemy's board is exactly what it carries: there are no empty cells to show.
            int maxHp = (int)((long)enemy.MaxHp * (100 + deeper * dungeon.EnemyHpPerFloorPercent) / 100);
            return new BattleUnitSetup
            {
                SourceId = enemy.Id,
                Name = enemy.Name,
                Row = row,
                MaxHp = maxHp,
                Hp = maxHp,
                Items = items,
                ItemSlots = ItemBoard.UsedCells(items),
                Passive = null,
                HasDog = false,
            };
        }

        /// <summary>The rows of the living members, and which member each one belongs to.</summary>
        public static int[] LivingRows(ExpeditionState state, out List<int> memberIndices)
        {
            memberIndices = new List<int>();
            for (int i = 0; i < state.Members.Count; i++)
            {
                if (state.Members[i].Alive)
                {
                    memberIndices.Add(i);
                }
            }

            var rows = new int[memberIndices.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = state.Members[memberIndices[i]].Row;
            }

            return rows;
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

        /// <summary>The cells the item at a cell of a board takes; 0 for an empty cell.</summary>
        static int SizeAt(ExpeditionMember member, int cell)
        {
            int index = ItemBoard.IndexAtCell(member.Items, cell);
            return index < 0 ? 0 : member.Items[index].Item.Size;
        }

        /// <summary>Puts an item at a cell of a member's board; whatever it displaces goes to the inventory, which the caller has checked has room.</summary>
        static void PutOnBoard(ExpeditionState state, ExpeditionMember member, int cell, EquippedItem item)
        {
            EquippedItem left = ItemBoard.Put(member.Items, member.ItemSlots, cell, item);
            if (left != null)
            {
                state.Inventory.Add(left);
            }
        }

        /// <summary>Choosing a node, a reward, or what to do at a camp: the boards and the rows can be rearranged.</summary>
        static bool IsBetweenBattles(ExpeditionState state)
        {
            return state.Phase == ExpeditionPhase.ChoosingNode || state.Phase == ExpeditionPhase.ChoosingReward || state.Phase == ExpeditionPhase.AtCamp;
        }

        static bool IsLivingMember(ExpeditionState state, int memberIndex)
        {
            return memberIndex >= 0 && memberIndex < state.Members.Count && state.Members[memberIndex].Alive;
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
    }
}
