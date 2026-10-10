using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The expedition rules (Docs/Design/03_Dungeon_Structure.md): node choice, battles, the loot they drop,
    /// the shop and its coins, item boards and the inventory, and how an expedition ends. Pure functions over
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
                Inventory = new InventoryGrid(balance.InventoryWidth, balance.InventoryHeight),
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
                    Board = ItemBoard.Starting(data.StartBag, new EquippedItem(data.Items.Get(job.WeaponItemId), job.WeaponGrade, isBase: true)),
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
                throw new InvalidOperationException($"Node {nodeId} is a camp or a shop: nobody is fought there.");
            }

            state.CurrentNodeId = nodeId;
            state.Phase = ExpeditionPhase.InBattle;
            foreach (ExpeditionMember member in state.Members)
            {
                if (member.Alive)
                {
                    member.Fatigue = FatigueRules.Add(data.Balance, member.Fatigue, FatigueRules.BattleEntryCost(data.Balance, member.Board.InReadingOrder()));
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

            int deeper = FloorsBelowTheFirst(group, floor);
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
            state.Coins += CoinsFor(data, node);
            if (node.Kind == MapNodeKind.Boss)
            {
                Finish(state, ExpeditionResult.Cleared);
                return;
            }

            state.Loot = DropLoot(data, state, node);
            state.Phase = state.Loot.Count > 0 ? ExpeditionPhase.PickingLoot : ExpeditionPhase.ChoosingNode;
        }

        // ---- Loot (Slice B stage 18) -----------------------------------------------------------

        /// <summary>
        /// What a won battle at a node drops (Docs/Design/03_Dungeon_Structure.md §5): of every item the enemies of its group carried,
        /// <c>DropCount</c> are drawn without repetition from the loot stream of the node (<c>EliteDropCount</c> for an elite); all of them
        /// when they carried fewer. A drop is the item at the dungeon's grade and the floor's tier (an elite's a tier up), as the shop's stock is:
        /// not at the grade the enemy carried it (that is the enemy's strength, far above a found item's). An elite's loot holds a bag with the
        /// chance <c>EliteBagPercent</c> (Slice B stage 19): its last drop is a bag drawn by loot weight instead. The boss drops nothing: the
        /// expedition ends there. Nothing for a node that is not fought.
        /// </summary>
        public static List<ItemOffer> DropLoot(StaticData data, ExpeditionState state, MapNode node)
        {
            var drops = new List<ItemOffer>();
            if (!node.IsFought || node.Kind == MapNodeKind.Boss)
            {
                return drops;
            }

            DungeonData dungeon = data.Dungeons.Get(state.DungeonId);
            int grade = dungeon.ItemGradeAt(node.Floor);
            ItemTier tier = dungeon.ItemTierAt(node.Floor, node.Kind == MapNodeKind.Elite);
            var carried = new List<ItemOffer>();
            foreach (string enemyId in data.EnemyGroups.Get(node.EnemyGroupId).Enemies)
            {
                foreach (ItemGrant grant in data.Enemies.Get(enemyId).Items)
                {
                    carried.Add(new ItemOffer(OfferKind.Item, grant.ItemId, grade, tier));
                }
            }

            var rng = new Pcg32(SeedDeriver.Derive(state.Seed, "loot", node.Id), RngStream.Loot);
            int count = node.Kind == MapNodeKind.Elite ? data.Balance.EliteDropCount : data.Balance.DropCount;
            while (drops.Count < count && carried.Count > 0)
            {
                int index = rng.NextInt(carried.Count);
                drops.Add(carried[index]);
                carried.RemoveAt(index);
            }

            if (node.Kind == MapNodeKind.Elite && rng.NextInt(100) < data.Balance.EliteBagPercent)
            {
                BagData bag = DrawBag(data, rng);
                if (bag != null)
                {
                    var offer = new ItemOffer(OfferKind.Bag, bag.Id, 0);
                    if (drops.Count > 0)
                    {
                        drops[drops.Count - 1] = offer;
                    }
                    else
                    {
                        drops.Add(offer);
                    }
                }
            }

            return drops;
        }

        /// <summary>A bag drawn by loot weight, or null when no bag drops.</summary>
        static BagData DrawBag(StaticData data, Pcg32 rng)
        {
            int total = 0;
            foreach (BagData bag in data.Bags.Ordered)
            {
                total += bag.LootWeight;
            }

            if (total <= 0)
            {
                return null;
            }

            int pick = rng.NextInt(total);
            foreach (BagData bag in data.Bags.Ordered)
            {
                if (pick < bag.LootWeight)
                {
                    return bag;
                }

                pick -= bag.LootWeight;
            }

            return null;
        }

        /// <summary>The drop lying in a slot of the loot, or null: for a slot already taken, no such slot, or no loot to pick.</summary>
        public static ItemOffer DropAt(ExpeditionState state, int slot)
        {
            return state.Phase == ExpeditionPhase.PickingLoot && slot >= 0 && slot < state.Loot.Count ? state.Loot[slot] : null;
        }

        /// <summary>
        /// Whether a drop could be put on a member's board at a placement: an item as <see cref="CanPlaceItem"/> says (over nothing, or over
        /// one item, which goes to the inventory and so must fit its free squares), or onto the same item at the same tier at the
        /// placement's top-left square, to merge with it; a bag in the frame over no bag (<see cref="CanPlaceBag"/>).
        /// </summary>
        public static bool CanTakeLoot(StaticData data, ExpeditionState state, int slot, int memberIndex, Placement at)
        {
            ItemOffer drop = DropAt(state, slot);
            if (drop == null)
            {
                return false;
            }

            if (drop.Kind == OfferKind.Bag)
            {
                return CanPlaceBag(state, memberIndex, data.Bags.Get(drop.Id), at);
            }

            return MergesInto(drop.Id, drop.Tier, LivingItemAt(state, memberIndex, at.X, at.Y)?.Item)
                || CanPlaceItem(data, state, data.Items.Get(drop.Id), memberIndex, at);
        }

        /// <summary>
        /// Takes a drop onto a member's board at a placement: an item over nothing or over one item (which goes to the inventory), or,
        /// onto the same item at the same tier, merged with it a tier up; a bag into the frame. Its slot is left empty; the last drop
        /// taken ends the loot.
        /// </summary>
        public static void TakeLoot(StaticData data, ExpeditionState state, int slot, int memberIndex, Placement at)
        {
            ItemOffer drop = RequireDrop(state, slot);
            ExpeditionMember member = RequireLivingMember(state, memberIndex);
            if (!CanTakeLoot(data, state, slot, memberIndex, at))
            {
                throw new InvalidOperationException($"The drop in slot {slot} cannot go at {at} of '{member.MercenaryId}' now.");
            }

            if (drop.Kind == OfferKind.Bag)
            {
                member.Board.Bags.Add(new BoardBag(data.Bags.Get(drop.Id), at));
                Pick(state, slot);
                return;
            }

            var item = new EquippedItem(data.Items.Get(drop.Id), drop.Grade, tier: drop.Tier);
            BoardItem there = LivingItemAt(state, memberIndex, at.X, at.Y);
            if (CanMerge(item, there?.Item))
            {
                MergeInto(item, there);
            }
            else
            {
                Put(state, member.Board, item, at, null);
            }

            Pick(state, slot);
        }

        /// <summary>Whether a drop could be taken straight into the inventory: an item (bags are never kept there) that has room on its grid.</summary>
        public static bool CanTakeLootToInventory(StaticData data, ExpeditionState state, int slot)
        {
            ItemOffer drop = DropAt(state, slot);
            return drop != null && drop.Kind == OfferKind.Item && state.Inventory.HasRoomFor(data.Items.Get(drop.Id));
        }

        /// <summary>Takes a drop straight into the inventory, at its first room (<see cref="InventoryGrid.FindRoom"/>).</summary>
        public static void TakeLootToInventory(StaticData data, ExpeditionState state, int slot)
        {
            if (!CanTakeLootToInventory(data, state, slot))
            {
                throw new InvalidOperationException($"The drop in slot {slot} cannot go to the inventory now.");
            }

            ItemOffer drop = state.Loot[slot];
            state.Inventory.Add(new EquippedItem(data.Items.Get(drop.Id), drop.Grade, tier: drop.Tier));
            Pick(state, slot);
        }

        /// <summary>Whether a drop could be laid on a placement of the inventory's grid (round 55): an item (a bag never goes there) over no item there.</summary>
        public static bool CanTakeLootToInventoryAt(StaticData data, ExpeditionState state, int slot, Placement at)
        {
            ItemOffer drop = DropAt(state, slot);
            if (drop == null || drop.Kind != OfferKind.Item)
            {
                return false;
            }

            ItemData item = data.Items.Get(drop.Id);
            return state.Inventory.IsFree(at.X, at.Y, at.WidthOf(item.Width, item.Height), at.HeightOf(item.Width, item.Height), null);
        }

        /// <summary>Takes a drop onto a placement of the inventory's grid (round 55).</summary>
        public static void TakeLootToInventoryAt(StaticData data, ExpeditionState state, int slot, Placement at)
        {
            if (!CanTakeLootToInventoryAt(data, state, slot, at))
            {
                throw new InvalidOperationException($"The drop in slot {slot} cannot go to {at} of the inventory now.");
            }

            ItemOffer drop = state.Loot[slot];
            state.Inventory.Add(new EquippedItem(data.Items.Get(drop.Id), drop.Grade, tier: drop.Tier), at);
            Pick(state, slot);
        }

        /// <summary>Leaves whatever loot still lies there and goes on to choosing the next node.</summary>
        public static void LeaveLoot(ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.PickingLoot);
            EndLoot(state);
        }

        /// <summary>Whether taking a drop with its top-left on a square of a living member's board would merge it into the item there.</summary>
        public static bool LootMergesAt(ExpeditionState state, int slot, int memberIndex, int x, int y)
        {
            ItemOffer drop = DropAt(state, slot);
            return drop != null && drop.Kind == OfferKind.Item && MergesInto(drop.Id, drop.Tier, LivingItemAt(state, memberIndex, x, y)?.Item);
        }

        // ---- The boards (Slice B stage 19: a grid of squares, bags, turning) ---------------------

        /// <summary>
        /// Whether an item could be put on a member's board now at a placement (Docs/Design/03_Dungeon_Structure.md §5): between battles,
        /// a living member, every square inside the frame and on a bag, over no item or over exactly one, which goes to the inventory and
        /// so must have room on its grid. Over two or more it cannot go.
        /// </summary>
        public static bool CanPlaceItem(StaticData data, ExpeditionState state, ItemData item, int memberIndex, Placement at)
        {
            return CanPut(state, memberIndex, item, at, null, null);
        }

        /// <param name="moving">The item being moved itself, which is not in its own way (null for an item coming from elsewhere).</param>
        /// <param name="leavingInventory">The inventory's item being placed, whose squares count as free for what it displaces (or null).</param>
        static bool CanPut(ExpeditionState state, int memberIndex, ItemData item, Placement at, BoardItem moving, BoardItem leavingInventory)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return false;
            }

            ItemBoard board = state.Members[memberIndex].Board;
            int width = at.WidthOf(item.Width, item.Height);
            int height = at.HeightOf(item.Width, item.Height);
            if (!board.OnBags(at.X, at.Y, width, height))
            {
                return false;
            }

            List<BoardItem> under = board.ItemsUnder(at.X, at.Y, width, height, moving);
            return under.Count == 0
                || (under.Count == 1 && state.Inventory.FindRoom(under[0].Item.Item.Width, under[0].Item.Item.Height, leavingInventory, out _));
        }

        /// <summary>
        /// Puts an item on a board at a placement. The one item it lies over, if any, goes to the inventory at its first room: the caller
        /// checked it has one (and took out of the inventory whatever left it).
        /// </summary>
        static void Put(ExpeditionState state, ItemBoard board, EquippedItem item, Placement at, BoardItem moving)
        {
            int width = at.WidthOf(item.Item.Width, item.Item.Height);
            int height = at.HeightOf(item.Item.Width, item.Item.Height);
            foreach (BoardItem under in board.ItemsUnder(at.X, at.Y, width, height, moving))
            {
                board.Items.Remove(under);
                state.Inventory.Add(under.Item);
            }

            board.Items.Add(new BoardItem(item, at));
        }

        /// <summary>
        /// Whether the item covering a square of one board can go to a placement on a board (the same or another member's): onto the same
        /// item at the same tier at the placement's top-left square, to merge; or there as <see cref="CanPlaceItem"/> says, the one item it
        /// lies over going to the inventory. Not to where it lies already. Allowed between battles.
        /// </summary>
        public static bool CanMoveItem(StaticData data, ExpeditionState state, int fromMember, int fromX, int fromY, int toMember, Placement to)
        {
            if (!CanPickItem(state, fromMember, fromX, fromY) || !IsLivingMember(state, toMember))
            {
                return false;
            }

            BoardItem moving = state.Members[fromMember].Board.ItemAt(fromX, fromY);
            if (fromMember == toMember && moving.At.Equals(to))
            {
                return false;
            }

            if (CanMerge(moving.Item, LivingItemAt(state, toMember, to.X, to.Y)?.Item))
            {
                return true;
            }

            return CanPut(state, toMember, moving.Item.Item, to, fromMember == toMember ? moving : null, null);
        }

        public static void MoveItem(StaticData data, ExpeditionState state, int fromMember, int fromX, int fromY, int toMember, Placement to)
        {
            if (!CanMoveItem(data, state, fromMember, fromX, fromY, toMember, to))
            {
                throw new InvalidOperationException($"The item at ({fromX},{fromY}) of member {fromMember} cannot go to {to} of member {toMember}.");
            }

            ItemBoard from = state.Members[fromMember].Board;
            ItemBoard target = state.Members[toMember].Board;
            BoardItem moving = from.ItemAt(fromX, fromY);
            BoardItem there = LivingItemAt(state, toMember, to.X, to.Y);
            from.Items.Remove(moving);
            if (CanMerge(moving.Item, there?.Item))
            {
                MergeInto(moving.Item, there);
                return;
            }

            Put(state, target, moving.Item, to, null);
        }

        /// <summary>
        /// Whether the item covering a square of a member's board can be picked up now: between battles, a living member, a square that
        /// holds an item. Where it may go is asked separately (<see cref="CanMoveItem"/>, <see cref="CanMoveToInventory"/>).
        /// </summary>
        public static bool CanPickItem(ExpeditionState state, int memberIndex, int x, int y)
        {
            return LivingItemAt(state, memberIndex, x, y) != null;
        }

        /// <summary>Whether a square of a member's board holds an item that can go to the inventory now: it must have room on the inventory's grid.</summary>
        public static bool CanMoveToInventory(StaticData data, ExpeditionState state, int memberIndex, int x, int y)
        {
            BoardItem item = LivingItemAt(state, memberIndex, x, y);
            return item != null && state.Inventory.HasRoomFor(item.Item.Item);
        }

        /// <summary>
        /// Takes the item covering a square off the board into the inventory, at its first room (<see cref="InventoryGrid.FindRoom"/>).
        /// Its squares on the board are left empty (nothing moves up).
        /// </summary>
        public static void MoveToInventory(StaticData data, ExpeditionState state, int memberIndex, int x, int y)
        {
            if (!CanMoveToInventory(data, state, memberIndex, x, y))
            {
                throw new InvalidOperationException($"Square ({x},{y}) of member {memberIndex} holds nothing that can go to the inventory now.");
            }

            ItemBoard board = state.Members[memberIndex].Board;
            BoardItem item = board.ItemAt(x, y);
            board.Items.Remove(item);
            state.Inventory.Add(item.Item);
        }

        /// <summary>
        /// Whether an item of the inventory could go to a placement on a member's board now: onto the same item at the same tier at its
        /// top-left square, to merge; or there as <see cref="CanPlaceItem"/> says, the squares it leaves in the inventory free for whatever
        /// it displaces.
        /// </summary>
        public static bool CanPlaceFromInventory(StaticData data, ExpeditionState state, int inventoryIndex, int memberIndex, Placement at)
        {
            if (inventoryIndex < 0 || inventoryIndex >= state.Inventory.Count)
            {
                return false;
            }

            EquippedItem item = state.Inventory[inventoryIndex];
            if (CanMerge(item, LivingItemAt(state, memberIndex, at.X, at.Y)?.Item))
            {
                return true;
            }

            return CanPut(state, memberIndex, item.Item, at, null, state.Inventory.Items[inventoryIndex]);
        }

        /// <summary>Puts an item of the inventory on a member's board at a placement. An item displaced there goes to the inventory.</summary>
        public static void PlaceFromInventory(StaticData data, ExpeditionState state, int inventoryIndex, int memberIndex, Placement at)
        {
            if (!CanPlaceFromInventory(data, state, inventoryIndex, memberIndex, at))
            {
                throw new InvalidOperationException($"Inventory item {inventoryIndex} cannot go to {at} of member {memberIndex} now.");
            }

            EquippedItem item = state.Inventory[inventoryIndex];
            ExpeditionMember member = state.Members[memberIndex];
            BoardItem there = LivingItemAt(state, memberIndex, at.X, at.Y);
            state.Inventory.RemoveAt(inventoryIndex);
            if (CanMerge(item, there?.Item))
            {
                MergeInto(item, there);
                return;
            }

            Put(state, member.Board, item, at, null);
        }

        /// <summary>
        /// Whether an item of the inventory could be laid elsewhere on the inventory's grid now (round 49, Docs/Design/03_Dungeon_Structure.md
        /// §5): between battles, inside the grid over no other item (Diablo II's swap is not made: the hand holds one thing), and not
        /// where and as it lies already.
        /// </summary>
        public static bool CanMoveInInventory(ExpeditionState state, int inventoryIndex, Placement to)
        {
            if (!IsBetweenBattles(state) || inventoryIndex < 0 || inventoryIndex >= state.Inventory.Count)
            {
                return false;
            }

            BoardItem moving = state.Inventory.Items[inventoryIndex];
            ItemData item = moving.Item.Item;
            return !moving.At.Equals(to) && state.Inventory.IsFree(to.X, to.Y, to.WidthOf(item.Width, item.Height), to.HeightOf(item.Width, item.Height), moving);
        }

        public static void MoveInInventory(ExpeditionState state, int inventoryIndex, Placement to)
        {
            if (!CanMoveInInventory(state, inventoryIndex, to))
            {
                throw new InvalidOperationException($"Inventory item {inventoryIndex} cannot go to {to} of the inventory now.");
            }

            state.Inventory.Items[inventoryIndex].At = to;
        }

        /// <summary>
        /// Whether the item covering a square of a member's board could be laid at a placement of the inventory's grid now (round 49): it
        /// can be picked, and the placement is inside the grid over no item.
        /// </summary>
        public static bool CanMoveToInventoryAt(ExpeditionState state, int memberIndex, int x, int y, Placement to)
        {
            BoardItem item = LivingItemAt(state, memberIndex, x, y);
            if (item == null)
            {
                return false;
            }

            ItemData data = item.Item.Item;
            return state.Inventory.IsFree(to.X, to.Y, to.WidthOf(data.Width, data.Height), to.HeightOf(data.Width, data.Height), null);
        }

        /// <summary>Takes the item covering a square off the board onto a placement of the inventory. Its squares on the board are left empty.</summary>
        public static void MoveToInventoryAt(ExpeditionState state, int memberIndex, int x, int y, Placement to)
        {
            if (!CanMoveToInventoryAt(state, memberIndex, x, y, to))
            {
                throw new InvalidOperationException($"Square ({x},{y}) of member {memberIndex} holds nothing that can go to {to} of the inventory now.");
            }

            ItemBoard board = state.Members[memberIndex].Board;
            BoardItem item = board.ItemAt(x, y);
            board.Items.Remove(item);
            state.Inventory.Add(item.Item, to);
        }

        // ---- Bags (Slice B stage 19) ----------------------------------------------------------

        /// <summary>
        /// Whether a bag could be put in a member's frame now at a placement: between battles, a living member, inside the frame over no
        /// bag (a bag comes from a shop or a loot and is laid where nothing is).
        /// </summary>
        public static bool CanPlaceBag(ExpeditionState state, int memberIndex, BagData bag, Placement at)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return false;
            }

            return state.Members[memberIndex].Board.FreeOfBags(at.X, at.Y, at.WidthOf(bag.Width, bag.Height), at.HeightOf(bag.Width, bag.Height), null);
        }

        /// <summary>
        /// Whether the bag at a square of a member's board can be picked up now: between battles, a living member, a square of a bag that
        /// is not the start bag and holds no item, and no item lying across that bag and another (the player moves that item first).
        /// </summary>
        public static bool CanPickBag(ExpeditionState state, int memberIndex, int x, int y)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return false;
            }

            return state.Members[memberIndex].Board.ItemAt(x, y) == null && MovableBagAt(state, memberIndex, x, y) != null;
        }

        /// <summary>The bag covering a square of a living member's board between battles if it can move (not the start bag, no item across it and another), or null.</summary>
        static BoardBag MovableBagAt(ExpeditionState state, int memberIndex, int x, int y)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return null;
            }

            ItemBoard board = state.Members[memberIndex].Board;
            BoardBag bag = board.BagAt(x, y);
            return bag != null && !bag.Bag.Start && !board.HasItemAcross(bag) ? bag : null;
        }

        /// <summary>
        /// Whether the bag covering a square (any square of it: the one it was picked up by need not be empty any more) can go to a placement
        /// in a member's frame (the same or another member's): inside the frame over no other bag, not where it lies already. The items in it
        /// go with it, turned with it (Backpack Battles' turning).
        /// </summary>
        public static bool CanMoveBag(ExpeditionState state, int fromMember, int fromX, int fromY, int toMember, Placement to)
        {
            BoardBag bag = MovableBagAt(state, fromMember, fromX, fromY);
            if (bag == null || !IsLivingMember(state, toMember))
            {
                return false;
            }

            if (fromMember == toMember && bag.At.Equals(to))
            {
                return false;
            }

            return state.Members[toMember].Board.FreeOfBags(
                to.X, to.Y, to.WidthOf(bag.Bag.Width, bag.Bag.Height), to.HeightOf(bag.Bag.Width, bag.Bag.Height), fromMember == toMember ? bag : null);
        }

        /// <summary>Moves a bag and the items lying in it to a placement: the items keep their place in the bag, turned as the bag turns.</summary>
        public static void MoveBag(ExpeditionState state, int fromMember, int fromX, int fromY, int toMember, Placement to)
        {
            if (!CanMoveBag(state, fromMember, fromX, fromY, toMember, to))
            {
                throw new InvalidOperationException($"The bag at ({fromX},{fromY}) of member {fromMember} cannot go to {to} of member {toMember}.");
            }

            ItemBoard from = state.Members[fromMember].Board;
            ItemBoard target = state.Members[toMember].Board;
            BoardBag bag = from.BagAt(fromX, fromY);
            List<BoardItem> carried = from.ItemsIn(bag);
            int turns = ((to.Turns - bag.At.Turns) % 4 + 4) % 4;
            foreach (BoardItem item in carried)
            {
                // The item's place in the bag, turned a quarter clockwise at a time inside the bag's box.
                int x = item.At.X - bag.At.X;
                int y = item.At.Y - bag.At.Y;
                int width = item.Width;
                int height = item.Height;
                int boxWidth = bag.Width;
                int boxHeight = bag.Height;
                for (int turn = 0; turn < turns; turn++)
                {
                    int turnedX = boxHeight - y - height;
                    y = x;
                    x = turnedX;
                    (width, height) = (height, width);
                    (boxWidth, boxHeight) = (boxHeight, boxWidth);
                }

                from.Items.Remove(item);
                item.At = new Placement(to.X + x, to.Y + y, item.At.Turns + turns);
                target.Items.Add(item);
            }

            from.Bags.Remove(bag);
            bag.At = to;
            target.Bags.Add(bag);
        }

        // ---- The shop and the region coins (Slice B stage 17) ----------------------------------

        /// <summary>
        /// The region coins a won battle at a node brings (Docs/Design/03_Dungeon_Structure.md §5): <c>CoinsPerEnemy</c> for each
        /// enemy of its group and <c>CoinsPerFloor</c> for each floor below the first; an elite's are <c>EliteCoinPercent</c> of
        /// that. The boss brings none: the expedition ends there. Nothing for a node that is not fought.
        /// </summary>
        public static int CoinsFor(StaticData data, MapNode node)
        {
            if (!node.IsFought || node.Kind == MapNodeKind.Boss)
            {
                return 0;
            }

            BalanceData balance = data.Balance;
            long coins = (long)balance.CoinsPerEnemy * data.EnemyGroups.Get(node.EnemyGroupId).Enemies.Count + (long)balance.CoinsPerFloor * (node.Floor - 1);
            if (node.Kind == MapNodeKind.Elite)
            {
                coins = coins * balance.EliteCoinPercent / 100;
            }

            return (int)Math.Min(int.MaxValue, coins);
        }

        /// <summary>
        /// Enters a shop node (Docs/Design/03_Dungeon_Structure.md §1, §5): the stock is drawn and the party stays there, buying what
        /// it wants, until it leaves. No fatigue is paid.
        /// </summary>
        public static void EnterShop(StaticData data, ExpeditionState state, int nodeId)
        {
            RequirePhase(state, ExpeditionPhase.ChoosingNode);
            if (!AvailableNodes(state).Exists(node => node.Id == nodeId))
            {
                throw new InvalidOperationException($"Node {nodeId} cannot be chosen now.");
            }

            MapNode shop = state.Map.Get(nodeId);
            if (shop.Kind != MapNodeKind.Shop)
            {
                throw new InvalidOperationException($"Node {nodeId} is not a shop.");
            }

            state.CurrentNodeId = nodeId;
            state.Phase = ExpeditionPhase.AtShop;
            state.Shop = new ShopState { Refreshes = 0 };
            state.Shop.Stock = DrawStock(data, state, shop, 0);
        }

        /// <summary>
        /// The stock of a shop after so many refreshes (stage 21, round 56): first its potions, drawn once from the shop stream of the node
        /// (<see cref="DrawPotions"/>), then its goods, drawn that many times past the first, the last draw being the goods. The goods are
        /// the items and bags a shop stocks (those with a weight and a price), at the floor's grade and tier, without repetition; the
        /// stock is the goods followed by the potions.
        /// </summary>
        static List<ItemOffer> DrawStock(StaticData data, ExpeditionState state, MapNode shop, int refreshes)
        {
            var rng = new Pcg32(SeedDeriver.Derive(state.Seed, "shop", shop.Id), RngStream.Shop);
            DungeonData dungeon = data.Dungeons.Get(state.DungeonId);
            int grade = dungeon.ItemGradeAt(shop.Floor);
            ItemTier tier = dungeon.ItemTierAt(shop.Floor, false);
            List<ItemOffer> potions = DrawPotions(data, rng);
            List<ItemOffer> stock = null;
            for (int draw = 0; draw <= refreshes; draw++)
            {
                stock = DrawOffers(data, rng, data.Balance.ShopSlots, grade, tier);
            }

            stock.AddRange(potions);
            return stock;
        }

        /// <summary>
        /// A shop's potions (round 56, the merchant's last column): each potion a shop stocks comes with `ShopPotionChancePercent`, and when
        /// none does, one of them by its weight, so that a shop has at least one. They come whether or not a potion slot is empty (one
        /// cannot be bought without an empty slot).
        /// </summary>
        static List<ItemOffer> DrawPotions(StaticData data, Pcg32 rng)
        {
            var kinds = new List<PotionData>();
            foreach (PotionData potion in data.Potions.Ordered)
            {
                if (potion.ShopWeight > 0 && potion.Price > 0)
                {
                    kinds.Add(potion);
                }
            }

            var potions = new List<ItemOffer>();
            foreach (PotionData potion in kinds)
            {
                if (rng.NextInt(100) < data.Balance.ShopPotionChancePercent)
                {
                    potions.Add(new ItemOffer(OfferKind.Potion, potion.Id, 0));
                }
            }

            if (potions.Count == 0 && kinds.Count > 0)
            {
                int total = 0;
                foreach (PotionData potion in kinds)
                {
                    total += potion.ShopWeight;
                }

                int pick = rng.NextInt(total);
                foreach (PotionData potion in kinds)
                {
                    if (pick < potion.ShopWeight)
                    {
                        potions.Add(new ItemOffer(OfferKind.Potion, potion.Id, 0));
                        break;
                    }

                    pick -= potion.ShopWeight;
                }
            }

            return potions;
        }

        /// <summary>What an offer costs: an item's price times its tier's percent (as its effects grow), a potion's or a bag's price as it is.</summary>
        public static int PriceOf(StaticData data, ItemOffer offer)
        {
            if (offer.Kind == OfferKind.Potion)
            {
                return data.Potions.Get(offer.Id).Price;
            }

            if (offer.Kind == OfferKind.Bag)
            {
                return data.Bags.Get(offer.Id).Price;
            }

            return (int)Math.Min(int.MaxValue, (long)data.Items.Get(offer.Id).Price * data.Balance.TierPercent(offer.Tier) / 100);
        }

        /// <summary>What the next refresh costs at the shop the party is at: <c>ShopRefreshBase</c>, and <c>ShopRefreshStep</c> more for every refresh made here.</summary>
        public static int RefreshCost(StaticData data, ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.AtShop);
            return (int)Math.Min(int.MaxValue, data.Balance.ShopRefreshBase + (long)data.Balance.ShopRefreshStep * state.Shop.Refreshes);
        }

        /// <summary>The offer in a slot of the shop the party is at, or null: not at a shop, no such slot, or sold.</summary>
        public static ItemOffer OfferAt(ExpeditionState state, int slot)
        {
            if (state.Phase != ExpeditionPhase.AtShop || state.Shop == null || slot < 0 || slot >= state.Shop.Stock.Count)
            {
                return null;
            }

            return state.Shop.Stock[slot];
        }

        /// <summary>Whether the coins cover the offer in a slot.</summary>
        public static bool CanAfford(StaticData data, ExpeditionState state, int slot)
        {
            ItemOffer offer = OfferAt(state, slot);
            return offer != null && state.Coins >= PriceOf(data, offer);
        }

        /// <summary>
        /// Whether the item or bag in a slot could be bought onto a member's board at a placement: the coins cover it, and it goes there as
        /// a drop of loot would (an item over nothing, over one item that goes to the inventory, or merged into the same item at the same
        /// tier at the placement's top-left square; a bag into the frame over no bag).
        /// </summary>
        public static bool CanBuyToBoard(StaticData data, ExpeditionState state, int slot, int memberIndex, Placement at)
        {
            ItemOffer offer = OfferAt(state, slot);
            if (offer == null || offer.Kind == OfferKind.Potion || state.Coins < PriceOf(data, offer))
            {
                return false;
            }

            if (offer.Kind == OfferKind.Bag)
            {
                return CanPlaceBag(state, memberIndex, data.Bags.Get(offer.Id), at);
            }

            return MergesInto(offer.Id, offer.Tier, LivingItemAt(state, memberIndex, at.X, at.Y)?.Item)
                || CanPlaceItem(data, state, data.Items.Get(offer.Id), memberIndex, at);
        }

        /// <summary>Buys the item or bag in a slot onto a member's board at a placement: the coins are paid as it is put there, and the slot is sold.</summary>
        public static void BuyToBoard(StaticData data, ExpeditionState state, int slot, int memberIndex, Placement at)
        {
            if (!CanBuyToBoard(data, state, slot, memberIndex, at))
            {
                throw new InvalidOperationException($"The offer in slot {slot} cannot be bought onto {at} of member {memberIndex} now.");
            }

            ItemOffer offer = state.Shop.Stock[slot];
            ExpeditionMember member = state.Members[memberIndex];
            if (offer.Kind == OfferKind.Bag)
            {
                member.Board.Bags.Add(new BoardBag(data.Bags.Get(offer.Id), at));
                Pay(data, state, slot);
                return;
            }

            var item = new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier);
            BoardItem there = LivingItemAt(state, memberIndex, at.X, at.Y);
            if (CanMerge(item, there?.Item))
            {
                MergeInto(item, there);
            }
            else
            {
                Put(state, member.Board, item, at, null);
            }

            Pay(data, state, slot);
        }

        /// <summary>Whether the item in a slot could be bought straight into the inventory: the coins cover it and it has room on the grid (a bag never goes there).</summary>
        public static bool CanBuyToInventory(StaticData data, ExpeditionState state, int slot)
        {
            ItemOffer offer = OfferAt(state, slot);
            return offer != null && offer.Kind == OfferKind.Item && state.Coins >= PriceOf(data, offer)
                && state.Inventory.HasRoomFor(data.Items.Get(offer.Id));
        }

        public static void BuyToInventory(StaticData data, ExpeditionState state, int slot)
        {
            if (!CanBuyToInventory(data, state, slot))
            {
                throw new InvalidOperationException($"The offer in slot {slot} cannot be bought into the inventory now.");
            }

            ItemOffer offer = state.Shop.Stock[slot];
            state.Inventory.Add(new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier));
            Pay(data, state, slot);
        }

        /// <summary>
        /// Whether the item in a slot could be bought onto a placement of the inventory's grid (round 55): the coins cover it and there it
        /// lies over no item (a bag never goes there).
        /// </summary>
        public static bool CanBuyToInventoryAt(StaticData data, ExpeditionState state, int slot, Placement at)
        {
            ItemOffer offer = OfferAt(state, slot);
            if (offer == null || offer.Kind != OfferKind.Item || state.Coins < PriceOf(data, offer))
            {
                return false;
            }

            ItemData item = data.Items.Get(offer.Id);
            return state.Inventory.IsFree(at.X, at.Y, at.WidthOf(item.Width, item.Height), at.HeightOf(item.Width, item.Height), null);
        }

        public static void BuyToInventoryAt(StaticData data, ExpeditionState state, int slot, Placement at)
        {
            if (!CanBuyToInventoryAt(data, state, slot, at))
            {
                throw new InvalidOperationException($"The offer in slot {slot} cannot be bought onto {at} of the inventory now.");
            }

            ItemOffer offer = state.Shop.Stock[slot];
            state.Inventory.Add(new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier), at);
            Pay(data, state, slot);
        }

        /// <summary>Whether the potion in a slot could be bought: the coins cover it and a potion slot is empty.</summary>
        public static bool CanBuyPotion(StaticData data, ExpeditionState state, int slot)
        {
            ItemOffer offer = OfferAt(state, slot);
            return offer != null && offer.Kind == OfferKind.Potion && state.Coins >= PriceOf(data, offer) && FreePotionSlot(state) >= 0;
        }

        public static void BuyPotion(StaticData data, ExpeditionState state, int slot)
        {
            if (!CanBuyPotion(data, state, slot))
            {
                throw new InvalidOperationException($"The offer in slot {slot} cannot be bought as a potion now.");
            }

            state.Potions[FreePotionSlot(state)] = state.Shop.Stock[slot].Id;
            Pay(data, state, slot);
        }

        /// <summary>Whether buying the item in a slot with its top-left on a square of a living member's board would merge it into the item there.</summary>
        public static bool ShopMergesAt(ExpeditionState state, int slot, int memberIndex, int x, int y)
        {
            ItemOffer offer = OfferAt(state, slot);
            return offer != null && offer.Kind == OfferKind.Item && MergesInto(offer.Id, offer.Tier, LivingItemAt(state, memberIndex, x, y)?.Item);
        }

        /// <summary>Whether the stock can be refreshed now: at a shop, with the coins to pay the next refresh.</summary>
        public static bool CanRefreshShop(StaticData data, ExpeditionState state)
        {
            return state.Phase == ExpeditionPhase.AtShop && state.Shop != null && state.Coins >= RefreshCost(data, state);
        }

        /// <summary>Pays the next refresh and draws every slot anew. The cost climbs for the next one; it starts over at the next shop.</summary>
        public static void RefreshShop(StaticData data, ExpeditionState state)
        {
            if (!CanRefreshShop(data, state))
            {
                throw new InvalidOperationException("The stock cannot be refreshed now.");
            }

            state.Coins -= RefreshCost(data, state);
            state.Shop.Refreshes++;

            // Round 56 ("포션은 그대로"): the goods are drawn anew; the potions stay as they are (one bought stays gone).
            List<ItemOffer> fresh = DrawStock(data, state, state.Map.Get(state.CurrentNodeId), state.Shop.Refreshes);
            int goods = ShopGoodsCount(data);
            var stock = new List<ItemOffer>();
            for (int i = 0; i < fresh.Count; i++)
            {
                stock.Add(i < goods || i >= state.Shop.Stock.Count ? fresh[i] : state.Shop.Stock[i]);
            }

            state.Shop.Stock = stock;
        }

        /// <summary>
        /// How many goods (items and bags) a shop's stock begins with, before its potions (Slice B stage 21, round 56): `ShopSlots`, or fewer
        /// when the data has fewer priced items and bags. It depends on the data alone.
        /// </summary>
        public static int ShopGoodsCount(StaticData data)
        {
            int candidates = 0;
            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.ShopWeight > 0 && item.Price > 0)
                {
                    candidates++;
                }
            }

            foreach (BagData bag in data.Bags.Ordered)
            {
                if (bag.ShopWeight > 0 && bag.Price > 0)
                {
                    candidates++;
                }
            }

            return Math.Min(data.Balance.ShopSlots, candidates);
        }

        /// <summary>Leaves the shop: what was not bought is gone, and the party goes on to the next floor.</summary>
        public static void LeaveShop(ExpeditionState state)
        {
            RequirePhase(state, ExpeditionPhase.AtShop);
            state.Shop = null;
            state.Phase = ExpeditionPhase.ChoosingNode;
        }

        static void Pay(StaticData data, ExpeditionState state, int slot)
        {
            state.Coins -= PriceOf(data, state.Shop.Stock[slot]);
            state.Shop.Stock[slot] = null;
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

        /// <summary>Whether putting an item (of a board or the inventory) with its top-left on a square of a living member's board would merge it into the item there.</summary>
        public static bool MergesAt(ExpeditionState state, EquippedItem item, int memberIndex, int x, int y)
        {
            return CanMerge(item, LivingItemAt(state, memberIndex, x, y)?.Item);
        }

        /// <summary>Whether some living member's board holds what the item would merge into.</summary>
        public static bool HasMergeTarget(ExpeditionState state, EquippedItem item)
        {
            foreach (ExpeditionMember member in state.Members)
            {
                if (!member.Alive)
                {
                    continue;
                }

                foreach (BoardItem there in member.Board.Items)
                {
                    if (CanMerge(item, there.Item))
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
        /// Merges an item into another lying on a board: the board's item becomes the merge, a tier up at the better grade of the two, where
        /// it lies. The item merged in is gone; the caller took it from where it was (a board, the inventory) or it came from nowhere (a drop, an offer).
        /// </summary>
        static void MergeInto(EquippedItem item, BoardItem into)
        {
            into.Item = new EquippedItem(into.Item.Item, Math.Max(item.Grade, into.Item.Grade), tier: into.Item.Tier + 1);
        }

        /// <summary>The item covering a square of a living member's board between battles, or null.</summary>
        static BoardItem LivingItemAt(ExpeditionState state, int memberIndex, int x, int y)
        {
            if (!IsBetweenBattles(state) || !IsLivingMember(state, memberIndex))
            {
                return null;
            }

            return state.Members[memberIndex].Board.ItemAt(x, y);
        }

        /// <summary>Whether the item covering a square of a member's board can go a tier up at the camp (the camp's upkeep): any item below Gold, a base weapon too.</summary>
        public static bool CanUpgradeAtCamp(ExpeditionState state, int memberIndex, int x, int y)
        {
            BoardItem item = LivingItemAt(state, memberIndex, x, y);
            return state.Phase == ExpeditionPhase.AtCamp && item != null && item.Item.Tier < ItemTier.Gold;
        }

        /// <summary>The camp's upkeep: the item covering a square goes a tier up (a base weapon stays one). Then the party goes on to the next floor.</summary>
        public static void UpgradeAtCamp(ExpeditionState state, int memberIndex, int x, int y)
        {
            if (!CanUpgradeAtCamp(state, memberIndex, x, y))
            {
                throw new InvalidOperationException($"Nothing at ({x},{y}) of member {memberIndex} can go a tier up now.");
            }

            BoardItem item = state.Members[memberIndex].Board.ItemAt(x, y);
            item.Item = item.Item.TierUp();
            state.Phase = ExpeditionPhase.ChoosingNode;
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
        /// Draws a shop's goods without repetition, by shop weight: the items with a weight and a price at the grade and tier given, and
        /// the bags with a weight and a price (Slice B stage 19). The potions are drawn apart (round 56: <see cref="DrawPotions"/>).
        /// </summary>
        static List<ItemOffer> DrawOffers(StaticData data, Pcg32 rng, int count, int grade, ItemTier tier)
        {
            var candidates = new List<ItemOffer>();
            var weights = new List<int>();
            foreach (ItemData item in data.Items.Ordered)
            {
                if (item.ShopWeight > 0 && item.Price > 0)
                {
                    candidates.Add(new ItemOffer(OfferKind.Item, item.Id, grade, tier));
                    weights.Add(item.ShopWeight);
                }
            }

            foreach (BagData bag in data.Bags.Ordered)
            {
                if (bag.ShopWeight > 0 && bag.Price > 0)
                {
                    candidates.Add(new ItemOffer(OfferKind.Bag, bag.Id, 0));
                    weights.Add(bag.ShopWeight);
                }
            }

            var offers = new List<ItemOffer>();
            while (offers.Count < count && candidates.Count > 0)
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

                offers.Add(candidates[index]);
                candidates.RemoveAt(index);
                weights.RemoveAt(index);
            }

            return offers;
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
                    Items = member.Board.InReadingOrder(),
                    Layout = BoardLayout.Of(member.Board),
                    StarDamage = StarRules.DamageInReadingOrder(member.Board),
                    Passive = data.Jobs.Get(member.JobId).Passive,
                    HasDog = true,
                    Fatigue = member.Fatigue,
                    FatigueState = member.StateId == null ? null : data.FatigueStates.Get(member.StateId),
                });
            }
        }

        /// <summary>How many floors below the first a group stands on a floor: 0 for a boss group, which is as the data says wherever it is.</summary>
        static int FloorsBelowTheFirst(EnemyGroupData group, int floor)
        {
            return group.IsBoss ? 0 : Math.Max(0, floor - 1);
        }

        /// <param name="deeper">How many floors below the first the enemy stands: its HP and item grades grow by the dungeon's share for each.</param>
        static BattleUnitSetup EnemySetup(EnemyData enemy, int row, StaticData data, DungeonData dungeon, int deeper)
        {
            var items = new List<EquippedItem>();
            foreach (ItemGrant grant in enemy.Items)
            {
                items.Add(new EquippedItem(data.Items.Get(grant.ItemId), grant.Grade + deeper * dungeon.EnemyGradePerFloor));
            }

            // An enemy's board is exactly what it carries, one item under another: it has no bags and no empty squares to show.
            int maxHp = (int)((long)enemy.MaxHp * (100 + deeper * dungeon.EnemyHpPerFloorPercent) / 100);
            return new BattleUnitSetup
            {
                SourceId = enemy.Id,
                Name = enemy.Name,
                Row = row,
                MaxHp = maxHp,
                Hp = maxHp,
                Items = items,
                Layout = BoardLayout.Stacked(items),
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

        /// <summary>Choosing a node, picking loot, or what to do at a camp or a shop: the boards and the rows can be rearranged.</summary>
        static bool IsBetweenBattles(ExpeditionState state)
        {
            return state.Phase == ExpeditionPhase.ChoosingNode || state.Phase == ExpeditionPhase.PickingLoot
                || state.Phase == ExpeditionPhase.AtCamp || state.Phase == ExpeditionPhase.AtShop;
        }

        static bool IsLivingMember(ExpeditionState state, int memberIndex)
        {
            return memberIndex >= 0 && memberIndex < state.Members.Count && state.Members[memberIndex].Alive;
        }

        static void Finish(ExpeditionState state, ExpeditionResult result)
        {
            state.Result = result;
            state.Phase = ExpeditionPhase.Finished;
            state.Loot.Clear();
        }

        /// <summary>Empties the slot of a drop just taken; the last one taken ends the loot.</summary>
        static void Pick(ExpeditionState state, int slot)
        {
            state.Loot[slot] = null;
            if (state.Loot.TrueForAll(drop => drop == null))
            {
                EndLoot(state);
            }
        }

        static void EndLoot(ExpeditionState state)
        {
            state.Loot.Clear();
            state.Phase = ExpeditionPhase.ChoosingNode;
        }

        static void RequirePhase(ExpeditionState state, ExpeditionPhase phase)
        {
            if (state.Phase != phase)
            {
                throw new InvalidOperationException($"Expedition is in phase {state.Phase}, not {phase}.");
            }
        }

        static ItemOffer RequireDrop(ExpeditionState state, int slot)
        {
            RequirePhase(state, ExpeditionPhase.PickingLoot);
            if (slot < 0 || slot >= state.Loot.Count)
            {
                throw new ArgumentOutOfRangeException(nameof(slot), slot, "No such slot of the loot.");
            }

            ItemOffer drop = state.Loot[slot];
            if (drop == null)
            {
                throw new InvalidOperationException($"The drop in slot {slot} was taken already.");
            }

            return drop;
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
