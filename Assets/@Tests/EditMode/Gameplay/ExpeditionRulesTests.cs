using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The expedition rules of Docs/Design/03_Dungeon_Structure.md.</summary>
    public sealed class ExpeditionRulesTests
    {
        static readonly PartyMember[] DefaultParty =
        {
            new PartyMember("anna", "tank", 1),
            new PartyMember("ben", "healer", 2),
            new PartyMember("cora", "striker", 3),
        };

        /// <summary>Who stands where, as "anna:1 ben:2"; the dead are marked with "x".</summary>
        static string Rows(ExpeditionState state)
        {
            return string.Join(" ", state.Members.Select(m => m.Alive ? $"{m.MercenaryId}:{m.Row}" : $"{m.MercenaryId}:x"));
        }

        static ExpeditionState Create(StaticData data, ulong seed = 1)
        {
            return ExpeditionRules.Create(data, "cave", seed, DefaultParty);
        }

        /// <summary>Fights the current battle to its end with no player input.</summary>
        static BattleEngine Fight(StaticData data, ExpeditionState state, int nodeId)
        {
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, nodeId));
            battle.RunToEnd();
            ExpeditionRules.CompleteBattle(data, state, battle);
            return battle;
        }

        // ---- Map -----------------------------------------------------------------------------

        [Test]
        public void Map_HasBattleFloorsWithinWidth_ThenOneBoss_AndEveryNodeLeadsOn()
        {
            StaticData data = TestData.Data();
            DungeonData dungeon = data.Dungeons.Get("cave");

            for (ulong seed = 1; seed <= 200; seed++)
            {
                NodeMap map = MapGenerator.Generate(data, dungeon, seed);

                Assert.AreEqual(dungeon.Floors + 1, map.FloorCount);
                for (int floor = 1; floor <= dungeon.Floors; floor++)
                {
                    List<MapNode> nodes = map.OnFloor(floor);
                    Assert.That(nodes.Count, Is.InRange(dungeon.MapMinWidth, dungeon.MapMaxWidth));
                    foreach (MapNode node in nodes)
                    {
                        Assert.AreEqual(MapNodeKind.Battle, node.Kind);
                        Assert.That(node.NextNodeIds.Count, Is.InRange(1, 3));
                        Assert.IsTrue(node.NextNodeIds.All(id => map.Get(id).Floor == floor + 1), "Edges go to the next floor only.");
                        EnemyGroupData group = data.EnemyGroups.Get(node.EnemyGroupId);
                        Assert.IsFalse(group.IsBoss);
                        Assert.That(floor, Is.InRange(group.MinFloor, group.MaxFloor));
                    }

                    if (floor > 1)
                    {
                        var reachable = new HashSet<int>(map.OnFloor(floor - 1).SelectMany(n => n.NextNodeIds));
                        Assert.IsTrue(nodes.All(n => reachable.Contains(n.Id)), "Every node has a way in.");
                    }
                }

                MapNode boss = map.OnFloor(dungeon.Floors + 1).Single();
                Assert.AreEqual(MapNodeKind.Boss, boss.Kind);
                Assert.AreEqual("lair", boss.EnemyGroupId);
                Assert.IsEmpty(boss.NextNodeIds);
                Assert.IsTrue(map.OnFloor(dungeon.Floors).All(n => n.NextNodeIds.SequenceEqual(new[] { boss.Id })));
                Assert.IsTrue(map.Nodes.Select((node, index) => node.Id == index).All(same => same), "A node id is its index.");
            }
        }

        [Test]
        public void Map_IsTheSameForTheSameSeed_AndVariesAcrossSeeds()
        {
            StaticData data = TestData.Data();
            DungeonData dungeon = data.Dungeons.Get("cave");

            string Shape(ulong seed)
            {
                NodeMap map = MapGenerator.Generate(data, dungeon, seed);
                return string.Join("|", map.Nodes.Select(n => $"{n.Floor}.{n.Column}:{n.EnemyGroupId}>{string.Join(",", n.NextNodeIds)}"));
            }

            Assert.AreEqual(Shape(42), Shape(42));
            Assert.Greater(Enumerable.Range(1, 50).Select(i => Shape((ulong)i)).Distinct().Count(), 5);
        }

        // ---- Start ---------------------------------------------------------------------------

        [Test]
        public void Create_GivesEachMemberFullHpAndOnlyTheJobWeapon_AndTheDungeonPotions()
        {
            StaticData data = TestData.Data();

            ExpeditionState state = Create(data);

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.AreEqual(-1, state.CurrentNodeId);
            ExpeditionMember tank = state.Members[0];
            Assert.AreEqual(100, tank.Hp);
            Assert.AreEqual(100, tank.MaxHp);
            Assert.AreEqual(3, tank.ItemSlots, "Cells come from the job.");
            Assert.AreEqual(1, tank.Items.Count, "Only the weapon.");
            Assert.AreEqual("blade", tank.Items[0].Item.Id);
            Assert.AreEqual(10, tank.Items[0].Grade);
            Assert.AreEqual(2, state.Members[2].ItemSlots);
            Assert.IsEmpty(state.Inventory);
            CollectionAssert.AreEqual(new[] { "tonic", null, null }, state.Potions);
        }

        [Test]
        public void Create_RejectsInvalidParties()
        {
            StaticData data = TestData.Data(("PartySize", 3));

            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new PartyMember[0]));
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", 1),
                new PartyMember("anna", "tank", 2),
            }));
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", 1),
                new PartyMember("ben", "healer", 1),
            }), "Two in one row.");
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", 1),
                new PartyMember("ben", "healer", 3),
            }), "Row 2 is empty in front of row 3.");
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", 2),
            }), "Row 1 is empty.");
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", 0),
            }));
            Assert.Throws<KeyNotFoundException>(() => ExpeditionRules.Create(data, "nowhere", 1, DefaultParty));
        }

        // ---- Nodes and battles ---------------------------------------------------------------

        [Test]
        public void AvailableNodes_AreTheFirstFloorAtTheStart_ThenTheNodesTheCurrentOneLeadsTo()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);

            List<MapNode> first = ExpeditionRules.AvailableNodes(state);
            CollectionAssert.AreEqual(state.Map.OnFloor(1).Select(n => n.Id), first.Select(n => n.Id));

            MapNode chosen = first[0];
            Fight(data, state, chosen.Id);
            if (state.Phase == ExpeditionPhase.PickingLoot)
            {
                ExpeditionRules.LeaveLoot(state);
            }

            CollectionAssert.AreEqual(chosen.NextNodeIds, ExpeditionRules.AvailableNodes(state).Select(n => n.Id));
        }

        [Test]
        public void BeginBattle_WhenNodeIsNotAvailable_Throws()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            int bossId = state.Map.Nodes.Last().Id;

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BeginBattle(data, state, bossId));
        }

        [Test]
        public void BattleSetup_ListsUnitsRowByRow_AppliesAffinity_AndIsRebuiltIdentically()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            int nodeId = ExpeditionRules.AvailableNodes(state)[0].Id;

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, nodeId);

            Assert.AreEqual(ExpeditionPhase.InBattle, state.Phase);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, setup.Party.Select(u => u.SourceId));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, setup.Party.Select(u => u.Row));
            CollectionAssert.AreEqual(Enumerable.Range(1, setup.Enemies.Count), setup.Enemies.Select(u => u.Row), "Enemies stand one per row from row 1, in the order the group lists them.");
            Assert.IsTrue(setup.Party.All(u => u.HasDog));
            Assert.IsTrue(setup.Enemies.All(u => !u.HasDog && u.Hp == u.MaxHp));
            Assert.AreEqual(-80, setup.EnemyCooldownPermille);
            Assert.AreEqual("tonic", setup.Potions[0].Id);
            CollectionAssert.AreEqual(new[] { 3, 3, 2 }, setup.Party.Select(u => u.ItemSlots), "A member's board keeps its cells.");
            Assert.IsTrue(setup.Enemies.All(u => u.ItemSlots == ItemBoard.UsedCells(u.Items)), "An enemy's board is exactly what it carries.");

            BattleSetup rebuilt = ExpeditionRules.BuildBattleSetup(data, state);
            Assert.AreEqual(setup.Seed, rebuilt.Seed);
            CollectionAssert.AreEqual(setup.Enemies.Select(u => u.SourceId), rebuilt.Enemies.Select(u => u.SourceId));

            var first = new BattleEngine(setup);
            var second = new BattleEngine(rebuilt);
            first.RunToEnd();
            second.RunToEnd();
            Assert.AreEqual(BattleLog.Hash(first.Events), BattleLog.Hash(second.Events));
        }

        [Test]
        public void BattleSeed_DependsOnTheNode()
        {
            StaticData data = TestData.Data();
            ExpeditionState a = Create(data);
            ExpeditionState b = Create(data);
            List<MapNode> nodes = ExpeditionRules.AvailableNodes(a);

            ulong seedA = ExpeditionRules.BeginBattle(data, a, nodes[0].Id).Seed;
            ulong seedB = ExpeditionRules.BeginBattle(data, b, nodes[1].Id).Seed;

            Assert.AreNotEqual(seedA, seedB);
        }

        [Test]
        public void CompleteBattle_OnVictory_CarriesHpOver_HealsABit_AndDropsLoot()
        {
            StaticData data = TestData.Data(("PostBattleHealPercent", 10));
            ExpeditionState state = Create(data);

            BattleEngine battle = Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.AreEqual(1, state.BattlesWon);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase);
            BattleUnit tankUnit = battle.Party.Single(u => u.Setup.SourceId == "anna");
            Assert.Less(tankUnit.Hp, 100, "The tank was hit.");
            Assert.AreEqual(Math.Min(100, tankUnit.Hp + 10), state.Members[0].Hp, "HP carries over plus 10% of max HP.");
            Assert.AreEqual(data.Balance.DropCount, state.Loot.Count, "Two grunts carried two claws: as many as drop.");
            Assert.IsTrue(state.Loot.All(d => d != null && d.Kind == OfferKind.Item), "Every drop lies there, and only items drop.");
        }

        [Test]
        public void CompleteBattle_WhenBattleStillOngoing_Throws()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.CompleteBattle(data, state, battle));
        }

        [Test]
        public void CompleteBattle_OnDefeat_EndsTheExpeditionAsWiped()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            foreach (ExpeditionMember member in state.Members)
            {
                member.Items.Clear();
            }

            Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.AreEqual(ExpeditionPhase.Finished, state.Phase);
            Assert.AreEqual(ExpeditionResult.Wiped, state.Result);
            Assert.IsTrue(state.Members.All(m => !m.Alive && m.Hp == 0));
        }

        [Test]
        public void CompleteBattle_OnRetreat_EndsTheExpeditionAsRetreated_AndKeepsSurvivors()
        {
            StaticData data = TestData.Data(("RetreatChancePercent", 100));
            ExpeditionState state = Create(data);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            battle.AdvanceTo(500);
            Assert.IsTrue(battle.TryRetreat());

            ExpeditionRules.CompleteBattle(data, state, battle);

            Assert.AreEqual(ExpeditionResult.Retreated, state.Result);
            Assert.AreEqual(ExpeditionPhase.Finished, state.Phase);
            Assert.IsTrue(state.Members.All(m => m.Alive));
        }

        [Test]
        public void UsedPotions_AreGoneAfterTheBattle()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            Assert.IsTrue(battle.TryUsePotion(0, 0));
            battle.RunToEnd();

            ExpeditionRules.CompleteBattle(data, state, battle);

            CollectionAssert.AreEqual(new string[] { null, null, null }, state.Potions);
        }

        [Test]
        public void ClearingTheBoss_EndsTheExpeditionAsCleared()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            foreach (ExpeditionMember member in state.Members)
            {
                member.Items[0] = new EquippedItem(data.Items.Get("blade"), 200);
            }

            while (state.Phase != ExpeditionPhase.Finished)
            {
                if (state.Phase == ExpeditionPhase.PickingLoot)
                {
                    ExpeditionRules.LeaveLoot(state);
                }
                else
                {
                    Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
                }
            }

            Assert.AreEqual(ExpeditionResult.Cleared, state.Result);
            Assert.AreEqual(3, state.BattlesWon, "Two battle floors and the boss.");
            Assert.IsEmpty(state.Loot);
        }

        // ---- The loot and the item board ---------------------------------------------------

        /// <summary>Wins the first battle (two grunts, two claws) and stands at its loot: two drops lying there.</summary>
        ExpeditionState AtLoot(StaticData data, ulong seed = 1)
        {
            ExpeditionState state = Create(data, seed);
            Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase);
            return state;
        }

        [Test]
        public void Loot_IsWhatTheEnemiesCarried_AtTheFloorsGradeAndTier_AndTheSameForTheSameSeed()
        {
            StaticData data = TestData.Data();

            ExpeditionState first = AtLoot(data, 9);
            ExpeditionState second = AtLoot(data, 9);

            CollectionAssert.AreEqual(first.Loot.Select(d => (d.Id, d.Grade, d.Tier)), second.Loot.Select(d => (d.Id, d.Grade, d.Tier)));
            Assert.IsTrue(first.Loot.All(d => d.Id == "claw"), "The grunts carry claws and nothing else.");
            Assert.IsTrue(first.Loot.All(d => d.Grade == 8 && d.Tier == ItemTier.Common), "ItemGradeBase on floor 1, at Common: not the grade 5 the grunt carried it at.");
        }

        [Test]
        public void Loot_IsAsManyAsTheDropCount_OrAllOfFewerItems()
        {
            ExpeditionState one = AtLoot(TestData.Data(("DropCount", 1)));
            Assert.AreEqual(1, one.Loot.Count);

            ExpeditionState three = AtLoot(TestData.Data(("DropCount", 3)));
            Assert.AreEqual(2, three.Loot.Count, "Two grunts carried two claws: fewer than three.");
        }

        [Test]
        public void DropLoot_AnEliteDropsMoreATierUp_TheBossAndACampNothing()
        {
            StaticData data = TestData.Data(("DropCount", 1), ("EliteDropCount", 2));
            ExpeditionState state = Create(data);

            List<ItemOffer> elite = ExpeditionRules.DropLoot(data, state, new MapNode(99, 2, 0, MapNodeKind.Elite, "trio", new int[0]));
            Assert.AreEqual(2, elite.Count);
            Assert.IsTrue(elite.All(d => d.Id == "claw" && d.Tier == ItemTier.Bronze && d.Grade == 10), "A tier up for an elite, at the second floor's grade (8 + 2).");
            Assert.IsEmpty(ExpeditionRules.DropLoot(data, state, new MapNode(98, 3, 0, MapNodeKind.Boss, "lair", new int[0])), "The boss ends the expedition.");
            Assert.IsEmpty(ExpeditionRules.DropLoot(data, state, new MapNode(97, 2, 0, MapNodeKind.Camp, null, new int[0])), "Nobody is fought at a camp.");
        }

        static string Board(ExpeditionMember member)
        {
            return string.Join(" ", member.Items.Select(i => i.Item.Id));
        }

        static string Inventory(ExpeditionState state)
        {
            return string.Join(" ", state.Inventory.Select(i => i.Item.Id));
        }

        static EquippedItem Big(StaticData data, string id)
        {
            return new EquippedItem(data.Items.Get(id), 9);
        }

        [Test]
        public void TakeLoot_IntoAnEmptyCell_PutsTheItemAtTheEndOfTheBoard_AndTheLastDropTakenEndsTheLoot()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[0];
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, 2), "The last of three cells is empty.");

            ExpeditionRules.TakeLoot(data, state, 0, 0, 2);

            Assert.AreEqual("blade " + drop.Id, Board(state.Members[0]), "Behind the weapon, whichever empty cell was chosen.");
            Assert.AreEqual(drop.Grade, state.Members[0].Items[1].Grade);
            Assert.IsEmpty(state.Inventory);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "One drop still lies there.");
            Assert.IsNull(state.Loot[0], "The slot of the taken drop is empty.");
            Assert.IsNotNull(state.Loot[1]);

            ExpeditionRules.TakeLoot(data, state, 1, 1, 2);

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase, "The last drop taken ends the loot.");
            Assert.IsEmpty(state.Loot);
        }

        [Test]
        public void TakeLoot_OntoAnItem_SendsThatItemToTheInventory()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[0];

            ExpeditionRules.TakeLoot(data, state, 0, 0, 0);

            Assert.AreEqual(drop.Id, Board(state.Members[0]), "The newcomer stands where the weapon was.");
            Assert.AreEqual("blade", Inventory(state), "The weapon is not lost.");
        }

        [Test]
        public void TakeLoot_WhereItDoesNotFit_IsRefused()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ExpeditionMember striker = state.Members[2];
            striker.Items.Add(Big(data, "knife"));
            Assert.AreEqual(0, ItemBoard.FreeCells(striker.Items, striker.ItemSlots), "Two cells, two items.");

            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 2, 1), "A one-cell drop in place of the knife.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 2, 2), "Beyond the board.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 9, 0), "No such member.");
            Assert.IsFalse(ExpeditionRules.CanPlaceItem(data, state, 2, 2, 1), "A two-cell item in place of the knife: 1 + 2 is more than 2.");
            Assert.IsFalse(ExpeditionRules.CanPlaceItem(data, state, 3, 0, 1), "A three-cell item behind the tank's weapon: only 2 free.");
            Assert.IsTrue(ExpeditionRules.CanPlaceItem(data, state, 3, 0, 0), "A three-cell item in place of the tank's weapon.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, 0, 2, 2));
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "Nothing was taken.");
            Assert.IsNotNull(state.Loot[0]);
        }

        [Test]
        public void TakeLootToInventory_KeepsTheItemOffTheBoards()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[1];

            ExpeditionRules.TakeLootToInventory(data, state, 1);

            Assert.AreEqual(drop.Id, Inventory(state));
            Assert.AreEqual(drop.Grade, state.Inventory[0].Grade);
            Assert.IsTrue(state.Members.All(m => m.Items.Count == 1), "Every board still holds only its weapon.");
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "The other drop still lies there.");
            Assert.IsNull(state.Loot[1]);
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventory(data, state, 1), "A taken slot cannot be taken again.");
        }

        [Test]
        public void ABigItem_TakesSeveralCells_AndFitsOnlyWhereThatManyAreFree()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "ballista"));
            ExpeditionMember tank = state.Members[0];

            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state,0, 0, 1), "Three cells, one taken: two free.");
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state,0, 0, 0), "In place of the weapon: all three.");
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state,0, 2, 0), "The striker's board has two cells.");

            ExpeditionRules.PlaceFromInventory(data, state,0, 0, 0);

            Assert.AreEqual("ballista", Board(tank));
            Assert.AreEqual(3, ItemBoard.UsedCells(tank.Items));
            Assert.AreEqual("blade", Inventory(state));
        }

        [Test]
        public void LeaveLoot_ReturnsToNodeChoice_AndWhatLayThereIsGone()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ExpeditionRules.TakeLootToInventory(data, state, 0);

            ExpeditionRules.LeaveLoot(state);

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsEmpty(state.Loot);
            Assert.AreEqual(1, state.Inventory.Count, "Only what was taken is kept.");
        }

        [Test]
        public void LootCommands_RejectTakenSlotsWrongPhaseAndBadIndexes()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);

            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeLoot(data, state, 99, 0, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, 0, 0, 9), "No such cell.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeLoot(data, state, 0, 9, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeLootToInventory(data, state, 99));
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 99, 0, 0));
            Assert.IsNull(ExpeditionRules.DropAt(state, 99));

            ExpeditionRules.TakeLootToInventory(data, state, 0);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, 0), "The drop in slot 0 was taken already.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 0, 1));
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 0, 0));

            ExpeditionRules.LeaveLoot(state);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.LeaveLoot(state), "No loot lies there any more.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, 1), "No loot lies there any more.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 1, 0, 1), "No loot lies there any more.");
        }

        [Test]
        public void MoveItem_ToFreeCells_OfAnotherBoard_TakesItAlong()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            EquippedItem tankWeapon = state.Members[0].Items[0];

            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 0, 1, 2));
            ExpeditionRules.MoveItem(state, 0, 0, 1, 2);

            Assert.IsEmpty(state.Members[0].Items);
            Assert.AreEqual("staff blade", Board(state.Members[1]));
            Assert.AreSame(tankWeapon, state.Members[1].Items[1]);
        }

        [Test]
        public void MoveItem_OntoAnItem_TradesPlaces_WhenBothBoardsStillHoldTheirItems()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            ExpeditionMember striker = state.Members[2];
            tank.Items.Add(Big(data, "pike"));
            Assert.AreEqual("blade pike", Board(tank));

            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 0, 2, 0), "Blade for blade.");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 1, 2, 0), "The pike (2) for the striker's blade (1): the striker's two cells hold the pike alone.");
            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 2, 2, 1), "The striker's second cell is empty, but one free cell does not hold the pike.");
            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 1, 2, 2), "Beyond the striker's board.");

            ExpeditionRules.MoveItem(state, 0, 1, 1, 0);

            Assert.AreEqual("blade staff", Board(tank), "The healer's staff came in the pike's place.");
            Assert.AreEqual("pike", Board(state.Members[1]));
        }

        [Test]
        public void MoveItem_WithinOneBoard_ReordersIt()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            tank.Items.Add(Big(data, "knife"));
            tank.Items.Add(Big(data, "charm"));
            Assert.AreEqual("blade knife charm", Board(tank));
            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 0, 0, 0), "The same cell.");

            ExpeditionRules.MoveItem(state, 0, 0, 0, 2);
            Assert.AreEqual("charm knife blade", Board(tank), "Trading places with the item there.");

            ExpeditionRules.MoveToInventory(data, state,0, 1);
            Assert.AreEqual("charm blade", Board(tank));
            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 0, 0, 2), "An empty cell behind the items.");
            ExpeditionRules.MoveItem(state, 0, 0, 0, 2);
            Assert.AreEqual("blade charm", Board(tank), "To the end of the board.");

            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 1, 0, 2), "The last item is at the end already: nothing would change.");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 0, 0, 2), "The first item can still go to the end.");
        }

        [Test]
        public void MoveToInventory_TakesTheItemOff_AndTheItemsBehindCloseUp()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            tank.Items.Add(Big(data, "knife"));
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state,0, 2), "An empty cell.");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state,9, 0));

            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state,0, 0));
            ExpeditionRules.MoveToInventory(data, state,0, 0);

            Assert.AreEqual("knife", Board(tank));
            Assert.AreEqual(0, ItemBoard.FirstCellOf(tank.Items, 0), "The knife moved to the first cell.");
            Assert.AreEqual("blade", Inventory(state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state,0, 2));
        }

        [Test]
        public void PlaceFromInventory_OntoAnItem_SwapsThemThroughTheInventory()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"));
            state.Inventory.Add(Big(data, "charm"));

            ExpeditionRules.PlaceFromInventory(data, state,1, 0, 0);

            Assert.AreEqual("charm", Board(state.Members[0]));
            Assert.AreEqual("knife blade", Inventory(state), "The displaced weapon goes to the end of the inventory.");
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state,5, 0, 1), "No such inventory item.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.PlaceFromInventory(data, state,5, 0, 1));
        }

        [Test]
        public void TheInventory_HasCells_AndAnItemTakesItsSizeThere()
        {
            StaticData data = TestData.Data(("InventoryCells", 4));
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "ballista"));
            ExpeditionMember tank = state.Members[0];
            tank.Items.Add(Big(data, "pike"));
            Assert.AreEqual(1, ExpeditionRules.FreeInventoryCells(data, state), "Four cells, three taken.");

            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 1), "The pike can be picked up...");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 0, 1), "...but its two cells do not fit the one free cell.");
            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state, 0, 0), "The one-cell weapon fits.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state, 0, 1));

            ExpeditionRules.MoveToInventory(data, state, 0, 0);

            Assert.AreEqual(0, ExpeditionRules.FreeInventoryCells(data, state));
            Assert.AreEqual("pike", Board(tank));
            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 0), "A full inventory does not stop an item being picked up...");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 0, 1, 1), "...and moved to another board: boards trade directly.");
            Assert.IsFalse(ExpeditionRules.CanPickItem(state, 0, 2), "An empty cell.");
            Assert.IsFalse(ExpeditionRules.CanPickItem(state, 9, 0));
        }

        [Test]
        public void OntoAnItem_OnlyWhileWhatItDisplacesFitsTheInventory()
        {
            StaticData data = TestData.Data(("InventoryCells", 3));
            ExpeditionState state = AtLoot(data);
            int optionIndex = 0;
            state.Inventory.Add(Big(data, "ballista"));
            Assert.AreEqual(0, ExpeditionRules.FreeInventoryCells(data, state));

            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, optionIndex, 0, 1), "An empty cell displaces nothing.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, optionIndex, 0, 0), "The weapon there would have nowhere to go.");
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventory(data, state, optionIndex));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, optionIndex, 0, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, optionIndex));
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "Nothing was taken.");

            // The cells an inventory item leaves are free for whatever it displaces.
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, 0), "The ballista in place of the weapon: the weapon takes one of the three cells it leaves.");
            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, 0);
            Assert.AreEqual("ballista", Board(state.Members[0]));
            Assert.AreEqual("blade", Inventory(state));
            Assert.AreEqual(2, ExpeditionRules.FreeInventoryCells(data, state));
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, optionIndex, 0, 0), "The ballista would need three free cells; two are free.");
            Assert.IsTrue(ExpeditionRules.CanTakeLootToInventory(data, state, optionIndex), "A one-cell drop fits.");

            ExpeditionRules.TakeLootToInventory(data, state, optionIndex);

            Assert.AreEqual(1, ExpeditionRules.FreeInventoryCells(data, state));
        }

        [Test]
        public void BoardCommands_DuringBattle_AreRefused()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"));
            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 0, 1, 1));
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state,0, 0));
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state,0, 0, 1));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveItem(state, 0, 0, 1, 1));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state,0, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.PlaceFromInventory(data, state,0, 0, 1));
        }

        [Test]
        public void BoardCommands_OnlyAmongTheLiving()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Members[2].Alive = false;
            state.Members[2].Hp = 0;
            state.Inventory.Add(Big(data, "knife"));

            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 0, 0, 2, 1));
            Assert.IsFalse(ExpeditionRules.CanMoveItem(state, 2, 0, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state,2, 0));
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state,0, 2, 1));
        }

        [Test]
        public void TheInventory_StaysOutOfTheBattle()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"));

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.IsTrue(setup.Party.All(u => u.Items.Count == 1), "Only what is on the boards fights.");
        }

        [Test]
        public void BattleSetup_FollowsTheRows_NotTheOrderOfTheMembers()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionRules.MoveToRow(state, 2, 1);

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            CollectionAssert.AreEqual(new[] { "cora", "ben", "anna" }, setup.Party.Select(u => u.SourceId));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, setup.Party.Select(u => u.Row));
        }

        [Test]
        public void MoveToRow_TradesPlacesWithTheMemberStandingThere()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);

            Assert.IsTrue(ExpeditionRules.CanMoveToRow(state, 0, 2));
            Assert.IsTrue(ExpeditionRules.CanMoveToRow(state, 0, 3));
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 0, 1), "Already there.");
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 0, 0));
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 9, 1), "No such member.");

            ExpeditionRules.MoveToRow(state, 0, 2);
            Assert.AreEqual("anna:2 ben:1 cora:3", Rows(state));

            ExpeditionRules.MoveToRow(state, 2, 1);
            Assert.AreEqual("anna:2 ben:3 cora:1", Rows(state));

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToRow(state, 0, 2));
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 0, BattleRows.Count), "The party of three never reaches the last row.");
        }

        [Test]
        public void MoveToRow_OnlyBetweenBattles_AndOnlyAmongTheLiving()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Members[2].Alive = false;
            state.Members[2].Hp = 0;

            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 0, 3), "Only a dead member is in row 3.");
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 2, 1), "The dead do not move.");
            Assert.IsTrue(ExpeditionRules.CanMoveToRow(state, 0, 2));

            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            Assert.IsFalse(ExpeditionRules.CanMoveToRow(state, 0, 2), "Not during a battle.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToRow(state, 0, 2));
        }

        [Test]
        public void CompleteBattle_ThoseWhoAdvancedKeepTheirRow_ForTheNextBattle()
        {
            // Anna starts at death's door with no grace and dies to the first hit. Cora ends the battle alone.
            StaticData data = TestData.Data(("DogGraceMs", 0), ("DogDeathChancePercent", 100));
            ExpeditionState state = Create(data);
            state.Members[0].Hp = 0;
            state.Members[2].Items[0] = new EquippedItem(data.Items.Get("blade"), 200);

            BattleEngine battle = Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.AreEqual("anna:x ben:1 cora:2", Rows(state));
            Assert.AreEqual(2, ExpeditionRules.LivingCount(state));
            Assert.IsNull(Formation.Problem(ExpeditionRules.LivingRows(state, out _)));

            if (state.Phase == ExpeditionPhase.PickingLoot)
            {
                ExpeditionRules.LeaveLoot(state);
            }

            BattleSetup next = ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            CollectionAssert.AreEqual(new[] { "ben", "cora" }, next.Party.Select(u => u.SourceId));
            CollectionAssert.AreEqual(new[] { 1, 2 }, next.Party.Select(u => u.Row));
        }

        [Test]
        public void TheSameSeedAndChoices_GiveTheSameExpedition()
        {
            StaticData data = TestData.Data(("DogDeathChancePercent", 40));

            string Play(ulong seed)
            {
                ExpeditionState state = Create(data, seed);
                var trace = new List<string>();
                while (state.Phase != ExpeditionPhase.Finished)
                {
                    if (state.Phase == ExpeditionPhase.PickingLoot)
                    {
                        trace.Add(string.Join(",", state.Loot.Select(r => r.Id)));
                        ExpeditionRules.LeaveLoot(state);
                        continue;
                    }

                    BattleEngine battle = Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
                    trace.Add(BattleLog.Hash(battle.Events).ToString());
                }

                trace.Add(state.Result.ToString());
                return string.Join(";", trace);
            }

            Assert.AreEqual(Play(5), Play(5));
            Assert.AreNotEqual(Play(5), Play(6));
        }
    }
}
