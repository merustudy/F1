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
            Assert.AreEqual(1, tank.Board.Bags.Count, "Only the start bag (Slice B stage 19)...");
            Assert.AreEqual(new Placement(0, 0), tank.Board.Bags[0].At, "...at the top-left of the frame.");
            Assert.AreEqual("pack", tank.Board.Bags[0].Bag.Id);
            Assert.AreEqual(1, tank.Board.Items.Count, "Only the weapon.");
            Assert.AreEqual("blade", tank.Board.Items[0].Item.Item.Id);
            Assert.AreEqual(new Placement(0, 0), tank.Board.Items[0].At, "Unturned at the bag's top-left.");
            Assert.AreEqual(10, tank.Board.Items[0].Item.Grade);
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
            Assert.IsTrue(setup.Party.All(u => u.Layout != null && u.Layout.Bags.Count == 1 && u.Layout.Items.Count == u.Items.Count), "A member's board goes in with its bags and where its items lie.");
            Assert.IsTrue(setup.Enemies.All(u => u.Layout != null && u.Layout.Bags.Count == 0), "An enemy's items stand one under another, without bags.");

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
                member.Board.Items.Clear();
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
                member.Board.Items[0].Item = new EquippedItem(data.Items.Get("blade"), 200);
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

        [Test]
        public void DropLoot_AnEliteMayDropABag_InPlaceOfItsLastItem_AndNothingElseDoes()
        {
            StaticData data = TestData.Data(("DropCount", 1), ("EliteDropCount", 2), ("EliteBagPercent", 100));
            ExpeditionState state = Create(data);

            List<ItemOffer> elite = ExpeditionRules.DropLoot(data, state, new MapNode(99, 2, 0, MapNodeKind.Elite, "trio", new int[0]));
            Assert.AreEqual(2, elite.Count, "The number of drops stays.");
            Assert.AreEqual(OfferKind.Item, elite[0].Kind);
            Assert.AreEqual(OfferKind.Bag, elite[1].Kind, "Its last drop is a bag (Slice B stage 19).");
            Assert.IsTrue(elite[1].Id == "pouch" || elite[1].Id == "strap", "A bag with a loot weight: never the start bag.");
            Assert.AreEqual(0, elite[1].Grade);

            List<ItemOffer> battle = ExpeditionRules.DropLoot(data, state, new MapNode(98, 2, 0, MapNodeKind.Battle, "trio", new int[0]));
            Assert.IsTrue(battle.All(d => d.Kind == OfferKind.Item), "Only an elite drops a bag.");

            StaticData never = TestData.Data(("DropCount", 1), ("EliteDropCount", 2), ("EliteBagPercent", 0));
            Assert.IsTrue(ExpeditionRules.DropLoot(never, Create(never), new MapNode(99, 2, 0, MapNodeKind.Elite, "trio", new int[0])).All(d => d.Kind == OfferKind.Item));
        }

        static string Board(ExpeditionMember member)
        {
            return string.Join(" ", TestBoards.Ids(member));
        }

        static string Inventory(ExpeditionState state)
        {
            return string.Join(" ", state.Inventory.Select(i => i.Item.Id));
        }

        static EquippedItem Big(StaticData data, string id)
        {
            return new EquippedItem(data.Items.Get(id), 9);
        }

        static Placement At(int x, int y, int turns = 0)
        {
            return new Placement(x, y, turns);
        }

        // ---- The grid boards (Slice B stage 19) ----------------------------------------------
        // The test members start with the pack (3x2) at the top-left and their weapon unturned at its top-left: the tank's and the
        // striker's blade (2x1), the healer's staff (3x1). The grunts drop claws (1x1).

        [Test]
        public void TakeLoot_OntoEmptySquares_LaysItThere_AndTheLastDropTakenEndsTheLoot()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[0];
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, At(2, 0)), "The pack's square beside the blade is empty.");

            ExpeditionRules.TakeLoot(data, state, 0, 0, At(2, 0));

            Assert.AreEqual("blade " + drop.Id, Board(state.Members[0]));
            Assert.AreEqual(drop.Grade, TestBoards.ItemAt(state.Members[0], 2, 0).Grade);
            Assert.IsEmpty(state.Inventory);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "One drop still lies there.");
            Assert.IsNull(state.Loot[0], "The slot of the taken drop is empty.");
            Assert.IsNotNull(state.Loot[1]);

            ExpeditionRules.TakeLoot(data, state, 1, 1, At(1, 1));

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase, "The last drop taken ends the loot.");
            Assert.IsEmpty(state.Loot);
        }

        [Test]
        public void TakeLoot_OverOneItem_SendsThatItemToTheInventory()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[0];

            ExpeditionRules.TakeLoot(data, state, 0, 0, At(1, 0));

            Assert.AreEqual(drop.Id, Board(state.Members[0]), "The claw lies over the blade's second square: the blade leaves the board.");
            Assert.AreEqual("blade", Inventory(state), "The weapon is not lost.");
            Assert.IsNull(TestBoards.ItemAt(state.Members[0], 0, 0), "The blade's other square is left empty.");
        }

        [Test]
        public void AnItem_GoesOnlyOnBags_InsideTheFrame_AndNeverOverTwoItems()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.Put(tank, Big(data, "knife"), 2, 1);
            ItemData pike = data.Items.Get("pike");

            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 0, At(0, 2)), "The third row has no bag.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 9, At(2, 0)), "No such member.");
            Assert.IsFalse(ExpeditionRules.CanPlaceItem(data, state, pike, 0, At(0, 0)), "The pike (3x2) would lie over the blade and the knife.");
            Assert.IsFalse(ExpeditionRules.CanPlaceItem(data, state, pike, 0, At(1, 0)), "Past the frame's right edge.");
            Assert.IsTrue(ExpeditionRules.CanPlaceItem(data, state, pike, 1, At(0, 0)), "Over the healer's staff alone: the staff would go to the inventory.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, 0, 0, At(0, 2)));
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "Nothing was taken.");
            Assert.IsNotNull(state.Loot[0]);
        }

        [Test]
        public void AnItem_TurnsAQuarterAtATime_AndLiesTurned()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "bow"));

            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(2, 0)), "Unturned (2x1), the bow would pass the frame's edge.");
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(2, 0, 1)), "Turned a quarter (1x2), it stands in the pack's last column.");
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(2, 0, 3)), "Three quarters lies the same way.");

            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(2, 0, 1));

            BoardItem bow = state.Members[0].Board.ItemAt(2, 1);
            Assert.AreEqual("bow", bow.Item.Item.Id);
            Assert.AreEqual(At(2, 0, 1), bow.At);
            Assert.AreEqual(1, bow.Width);
            Assert.AreEqual(2, bow.Height);
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
            Assert.IsTrue(state.Members.All(m => m.Board.Items.Count == 1), "Every board still holds only its weapon.");
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "The other drop still lies there.");
            Assert.IsNull(state.Loot[1]);
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventory(data, state, 1), "A taken slot cannot be taken again.");
        }

        [Test]
        public void ADrop_CanBeLaidOnFreeSquaresOfTheInventory_NotOverAnItem()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtLoot(data);
            ItemOffer drop = state.Loot[1];
            ItemData item = data.Items.Get(drop.Id);
            state.Inventory.Add(new EquippedItem(data.Items.Get("blade"), 8), At(0, 0));

            // Round 55: the drop held over the inventory's grid lands where it is laid, over no item and inside the grid.
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 1, At(0, 0)), "Over the blade: the inventory swaps nothing.");
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 1, At(state.Inventory.Width - item.Width + 1, 0)), "Out of the grid.");
            Placement free = At(state.Inventory.Width - item.Width, state.Inventory.Height - item.Height);
            Assert.IsTrue(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 1, free));

            ExpeditionRules.TakeLootToInventoryAt(data, state, 1, free);

            Assert.AreEqual(free, state.Inventory.Items.Single(i => i.Item.Item.Id == drop.Id).At);
            Assert.IsNull(state.Loot[1]);
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 1, At(0, 1)), "Taken already.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventoryAt(data, state, 1, At(0, 1)));
        }

        [Test]
        public void ABigItem_NeedsBagsUnderEverySquare_ABagAddedMakesTheRoom()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "ballista"));
            ExpeditionMember tank = state.Members[0];

            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(0, 0)), "The ballista (3x3) needs three rows; the pack has two.");
            TestBoards.AddBag(data, tank, "pouch", 0, 2);
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(0, 0)), "Across the pack and the pouch, over the blade alone.");
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 2, At(0, 0)), "The striker has only the pack.");

            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(0, 0));

            Assert.AreEqual("ballista", Board(tank));
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

            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeLoot(data, state, 99, 0, At(2, 0)));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, 0, 0, At(0, 7)), "No bag there.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeLoot(data, state, 0, 9, At(2, 0)));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, 99));
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 99, 0, At(2, 0)));
            Assert.IsNull(ExpeditionRules.DropAt(state, 99));

            ExpeditionRules.TakeLootToInventory(data, state, 0);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, 0), "The drop in slot 0 was taken already.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 0, At(2, 0)));
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 0, 0, 0));

            ExpeditionRules.LeaveLoot(state);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.LeaveLoot(state), "No loot lies there any more.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, 1), "No loot lies there any more.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 1, 0, At(2, 0)), "No loot lies there any more.");
        }

        [Test]
        public void MoveItem_ToEmptySquaresOfAnotherBoard_TakesItAlong()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            EquippedItem tankWeapon = TestBoards.ItemAt(state.Members[0], 0, 0);

            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 1, 0, 1, At(0, 1)), "Picked by its second square, to the healer's empty row.");
            ExpeditionRules.MoveItem(data, state, 0, 1, 0, 1, At(0, 1));

            Assert.IsEmpty(state.Members[0].Board.Items);
            Assert.AreEqual("staff blade", Board(state.Members[1]));
            Assert.AreSame(tankWeapon, TestBoards.ItemAt(state.Members[1], 1, 1));
            Assert.IsEmpty(state.Inventory);
        }

        [Test]
        public void MoveItem_OverOneItem_SendsItToTheInventory_AndOverTwoIsRefused()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember healer = state.Members[1];
            TestBoards.Put(healer, Big(data, "knife"), 0, 1);
            TestBoards.Put(healer, Big(data, "charm"), 1, 1);

            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 1, At(0, 1)), "The blade (2x1) would lie over the knife and the charm.");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 1, At(1, 1)), "Over the charm alone (and the empty square beside it).");

            ExpeditionRules.MoveItem(data, state, 0, 0, 0, 1, At(1, 1));

            Assert.AreEqual("staff knife blade", Board(healer));
            Assert.AreEqual("charm", Inventory(state), "What it lay over is not lost.");
            Assert.IsEmpty(state.Members[0].Board.Items);
        }

        [Test]
        public void MoveItem_WithinOneBoard_ToAnotherSpotOrTurned_NotWhereItLies()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];

            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 0, At(0, 0)), "Where it lies already.");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 0, At(1, 0)), "One square to the right: it is not in its own way.");

            ExpeditionRules.MoveItem(data, state, 0, 0, 0, 0, At(0, 0, 1));

            BoardItem blade = tank.Board.ItemAt(0, 1);
            Assert.AreEqual(At(0, 0, 1), blade.At, "Turned in place: it stands in the first column.");
            Assert.IsNull(tank.Board.ItemAt(1, 0));
            Assert.IsEmpty(state.Inventory);
        }

        [Test]
        public void MoveToInventory_TakesTheItemOff_AndLeavesItsSquaresEmpty()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.Put(tank, Big(data, "knife"), 2, 0);
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 0, 0, 1), "An empty square.");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 9, 0, 0));

            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state, 0, 1, 0), "By any of its squares.");
            ExpeditionRules.MoveToInventory(data, state, 0, 1, 0);

            Assert.AreEqual("knife", Board(tank));
            Assert.AreEqual(At(2, 0), tank.Board.ItemAt(2, 0).At, "Nothing moves up: the knife stays where it lies.");
            Assert.AreEqual("blade", Inventory(state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state, 0, 0, 0));
        }

        [Test]
        public void PlaceFromInventory_OverAnItem_SwapsThemThroughTheInventory()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"));
            state.Inventory.Add(Big(data, "charm"));

            ExpeditionRules.PlaceFromInventory(data, state, 1, 0, At(0, 0));

            Assert.AreEqual("charm", Board(state.Members[0]));
            Assert.AreEqual("knife blade", Inventory(state), "The displaced weapon goes to the end of the inventory.");
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 5, 0, At(2, 0)), "No such inventory item.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.PlaceFromInventory(data, state, 5, 0, At(2, 0)));
        }

        [Test]
        public void TheInventory_IsAGrid_AnItemNeedsARoomOfItsShape_NotJustSquares()
        {
            StaticData data = TestData.Data(("InventoryWidth", 4), ("InventoryHeight", 3));
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "ballista"), At(0, 0));
            state.Inventory.Add(Big(data, "knife"), At(3, 1));
            ExpeditionMember tank = state.Members[0];
            TestBoards.Put(tank, Big(data, "knife"), 2, 0);
            Assert.AreEqual(10, state.Inventory.UsedSquares, "Twelve squares, two free: (3,0) and (3,2), apart.");

            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 0, 0), "The blade can be picked up...");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 0, 0, 0), "...but two squares apart are no room for it, turned or not.");
            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state, 0, 2, 0), "The one-square knife has a room.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state, 0, 0, 0));

            ExpeditionRules.MoveToInventory(data, state, 0, 2, 0);

            Assert.AreEqual(new Placement(3, 0), state.Inventory.Items[2].At, "The first room in reading order.");
            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 1, At(0, 1)), "A full inventory does not stop a move to empty squares.");
            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 1, At(0, 0)), "Over the staff, which would have nowhere to go.");
            Assert.IsFalse(ExpeditionRules.CanPickItem(state, 0, 2, 1), "An empty square.");
            Assert.IsFalse(ExpeditionRules.CanPickItem(state, 9, 0, 0));
        }

        [Test]
        public void OverAnItem_OnlyWhileWhatItDisplacesHasARoomInTheInventory()
        {
            StaticData data = TestData.Data(("InventoryWidth", 3), ("InventoryHeight", 3));
            ExpeditionState state = AtLoot(data);
            int drop = 0;
            state.Inventory.Add(Big(data, "ballista"));
            Assert.IsFalse(state.Inventory.HasRoomFor(data.Items.Get("knife")), "The ballista fills the three by three.");

            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, drop, 0, At(2, 0)), "An empty square displaces nothing.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, drop, 0, At(0, 0)), "The blade there would have nowhere to go.");
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventory(data, state, drop));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, drop, 0, At(0, 0)));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLootToInventory(data, state, drop));
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "Nothing was taken.");

            // The squares an inventory item leaves are free for whatever it displaces.
            TestBoards.AddBag(data, state.Members[0], "pouch", 0, 2);
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(0, 0)), "The ballista over the blade: the blade takes two of the nine squares it leaves.");
            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(0, 0));
            Assert.AreEqual("ballista", Board(state.Members[0]));
            Assert.AreEqual("blade", Inventory(state));
            Assert.AreEqual(new Placement(0, 0), state.Inventory.Items[0].At, "The blade lies where the ballista lay.");
            Assert.IsTrue(ExpeditionRules.CanTakeLootToInventory(data, state, drop), "A one-square drop has a room.");

            ExpeditionRules.TakeLootToInventory(data, state, drop);

            Assert.AreEqual(new Placement(2, 0), state.Inventory.Items[1].At, "After the blade in reading order.");
        }

        [Test]
        public void TheInventory_TakesAShapeAtItsFirstRoom_InReadingOrder_UnturnedBeforeTurned()
        {
            var grid = new InventoryGrid(4, 3);
            StaticData data = TestData.Data();
            grid.Add(Big(data, "staff"));
            grid.Add(Big(data, "staff"));
            grid.Add(Big(data, "staff"));

            Assert.AreEqual(new[] { new Placement(0, 0), new Placement(0, 1), new Placement(0, 2) }, grid.Items.Select(i => i.At).ToArray());
            Assert.IsTrue(grid.FindRoom(3, 1, null, out Placement turned), "Only turned does the staff fit the last column.");
            Assert.AreEqual(new Placement(3, 0, 1), turned);
            Assert.IsFalse(grid.FindRoom(3, 3, grid.Items[0], out Placement _), "Leaving one staff out frees one row, not three.");
            Assert.Throws<InvalidOperationException>(() => grid.Add(Big(data, "staff"), new Placement(1, 1)), "Over another staff.");
            Assert.Throws<InvalidOperationException>(() => grid.Add(Big(data, "knife"), new Placement(4, 0)), "Outside the grid.");
        }

        [Test]
        public void AnInventoryItem_MovesOnTheGrid_ToFreeSquaresOnly_TurnedAsHeld()
        {
            StaticData data = TestData.Data(("InventoryWidth", 4), ("InventoryHeight", 3));
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "staff"), At(0, 0));
            state.Inventory.Add(Big(data, "knife"), At(0, 1));

            Assert.IsTrue(ExpeditionRules.CanMoveInInventory(state, 0, At(3, 0, 1)), "Turned, down the last column.");
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 0, At(0, 1)), "Over the knife: no swap.");
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 0, At(2, 0)), "Hanging out of the grid.");
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 0, At(0, 0)), "Where and as it lies.");
            Assert.IsTrue(ExpeditionRules.CanMoveInInventory(state, 0, At(1, 0)), "Over its own squares is fine.");
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 5, At(1, 0)), "No such item.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveInInventory(state, 0, At(0, 1)));

            ExpeditionRules.MoveInInventory(state, 0, At(3, 0, 1));

            Assert.AreEqual(At(3, 0, 1), state.Inventory.Items[0].At);
            Assert.AreEqual(1, state.Inventory.Items[0].Width);
            Assert.AreEqual(3, state.Inventory.Items[0].Height);
        }

        [Test]
        public void ABoardItem_GoesToAChosenPlaceOfTheInventory_IfItIsFree()
        {
            StaticData data = TestData.Data(("InventoryWidth", 4), ("InventoryHeight", 3));
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"), At(0, 0));
            ExpeditionMember tank = state.Members[0];

            Assert.IsFalse(ExpeditionRules.CanMoveToInventoryAt(state, 0, 0, 0, At(0, 0)), "The blade over the knife.");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventoryAt(state, 0, 0, 0, At(3, 0)), "Hanging out of the grid unturned.");
            Assert.IsTrue(ExpeditionRules.CanMoveToInventoryAt(state, 0, 0, 0, At(3, 0, 1)), "Turned, down the last column.");
            Assert.IsFalse(ExpeditionRules.CanMoveToInventoryAt(state, 0, 2, 0, At(1, 1)), "An empty square of the board.");

            ExpeditionRules.MoveToInventoryAt(state, 0, 0, 0, At(3, 0, 1));

            Assert.IsEmpty(tank.Board.Items);
            Assert.AreEqual("knife blade", Inventory(state));
            Assert.AreEqual(At(3, 0, 1), state.Inventory.Items[1].At);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventoryAt(state, 0, 0, 0, At(1, 1)));

            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 0, At(1, 1)), "Not during a battle.");
        }

        [Test]
        public void BoardCommands_DuringBattle_AreRefused()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Inventory.Add(Big(data, "knife"));
            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 1, At(0, 1)));
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 0, 0, 0));
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanPlaceBag(state, 0, data.Bags.Get("pouch"), At(0, 2)));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveItem(data, state, 0, 0, 0, 1, At(0, 1)));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.MoveToInventory(data, state, 0, 0, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(2, 0)));
        }

        [Test]
        public void BoardCommands_OnlyAmongTheLiving()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            state.Members[2].Alive = false;
            state.Members[2].Hp = 0;
            state.Inventory.Add(Big(data, "knife"));

            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 0, 2, At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 2, 0, 0, 0, At(0, 1)));
            Assert.IsFalse(ExpeditionRules.CanMoveToInventory(data, state, 2, 0, 0));
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 2, At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanPlaceBag(state, 2, data.Bags.Get("pouch"), At(0, 2)));
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
        public void TheBattle_ReadsABoardInReadingOrder_AndCarriesWhereEachItemLies()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.Put(tank, Big(data, "charm"), 0, 1);
            TestBoards.Put(tank, Big(data, "knife"), 2, 0, 1);

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            BattleUnitSetup unit = setup.Party.Single(u => u.SourceId == tank.MercenaryId);

            CollectionAssert.AreEqual(new[] { "blade", "knife", "charm" }, unit.Items.Select(i => i.Item.Id), "Row by row, left to right: the battle's order (Docs/Design/02 §4).");
            CollectionAssert.AreEqual(new[] { At(0, 0), At(2, 0, 1), At(0, 1) }, unit.Layout.Items, "Where each lies, for the screen.");
            Assert.AreEqual(1, unit.Layout.Bags.Count);
            BattleUnitSetup enemy = setup.Enemies[0];
            CollectionAssert.IsEmpty(enemy.Layout.Bags, "An enemy has no bags.");
            Assert.AreEqual(At(0, 0), enemy.Layout.Items[0]);
        }

        // ---- Bags (Slice B stage 19) ---------------------------------------------------------

        [Test]
        public void ABag_LiesInTheFrameOverNoBag_TurnedToo()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            BagData pouch = data.Bags.Get("pouch");

            Assert.IsTrue(ExpeditionRules.CanPlaceBag(state, 0, pouch, At(0, 2)));
            Assert.IsFalse(ExpeditionRules.CanPlaceBag(state, 0, pouch, At(0, 1)), "Over the pack.");
            Assert.IsFalse(ExpeditionRules.CanPlaceBag(state, 0, pouch, At(1, 7)), "Past the frame's right edge.");
            Assert.IsTrue(ExpeditionRules.CanPlaceBag(state, 0, pouch, At(2, 5, 1)), "Turned, it stands at the frame's right edge: rows 5 to 7.");
            Assert.IsFalse(ExpeditionRules.CanPlaceBag(state, 0, pouch, At(2, 6, 1)), "Past the frame's bottom.");
        }

        [Test]
        public void TheStartBag_StaysPut_AndABagIsPickedByAnEmptySquare_WithNothingAcrossIt()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.AddBag(data, tank, "pouch", 0, 2);

            Assert.IsFalse(ExpeditionRules.CanPickBag(state, 0, 2, 0), "The start bag never moves.");
            Assert.IsTrue(ExpeditionRules.CanPickBag(state, 0, 1, 2), "An empty square of the pouch.");
            Assert.IsFalse(ExpeditionRules.CanPickBag(state, 0, 1, 4), "No bag there.");

            TestBoards.Put(tank, Big(data, "knife"), 0, 2);
            Assert.IsFalse(ExpeditionRules.CanPickBag(state, 0, 0, 2), "A square holding an item picks the item.");
            Assert.IsTrue(ExpeditionRules.CanPickBag(state, 0, 2, 2));

            tank.Board.Items.Add(new BoardItem(Big(data, "bow"), At(2, 1, 1)));
            Assert.IsFalse(ExpeditionRules.CanPickBag(state, 0, 2, 2), "The bow lies across the pack and the pouch: move it first.");
        }

        [Test]
        public void ABagPickedUp_MovesByAnyOfItsSquares_EvenOneUnderAnItem()
        {
            // The screen holds a picked bag by its top-left square, which may hold an item: the bag still moves (a bug found in stage 19).
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.AddBag(data, tank, "pouch", 0, 2);
            TestBoards.Put(tank, Big(data, "knife"), 0, 2);
            Assert.IsTrue(ExpeditionRules.CanPickBag(state, 0, 2, 2), "Picked by its empty square.");

            Assert.IsTrue(ExpeditionRules.CanMoveBag(state, 0, 0, 2, 0, At(0, 4)), "Then held by its top-left square, under the knife.");
            ExpeditionRules.MoveBag(state, 0, 0, 2, 0, At(0, 4));
            Assert.AreEqual("knife", TestBoards.ItemAt(tank, 0, 4).Item.Id);
            Assert.IsFalse(ExpeditionRules.CanMoveBag(state, 0, 0, 0, 0, At(0, 5)), "The start bag never moves, by any square.");
        }

        [Test]
        public void MoveBag_CarriesItsItems_TurnsThemWithIt_AndGoesToAnotherBoard()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionMember tank = state.Members[0];
            TestBoards.AddBag(data, tank, "pouch", 0, 2);
            TestBoards.Put(tank, Big(data, "knife"), 2, 2);

            ExpeditionRules.MoveBag(state, 0, 0, 2, 0, At(0, 4));
            Assert.AreEqual(At(0, 4), tank.Board.BagAt(0, 4).At);
            Assert.AreEqual("knife", TestBoards.ItemAt(tank, 2, 4).Item.Id, "The knife kept its place in the pouch.");

            Assert.IsFalse(ExpeditionRules.CanMoveBag(state, 0, 0, 4, 0, At(0, 4)), "Where it lies already.");
            Assert.IsFalse(ExpeditionRules.CanMoveBag(state, 0, 0, 4, 0, At(0, 1)), "Over the pack.");

            ExpeditionRules.MoveBag(state, 0, 0, 4, 0, At(2, 3, 1));
            BoardItem knife = tank.Board.ItemAt(2, 5);
            Assert.IsNotNull(knife, "Turned a quarter clockwise, the pouch's last square is its bottom one.");
            Assert.AreEqual(1, knife.At.Turns, "The knife turned with it.");

            ExpeditionRules.MoveBag(state, 0, 2, 3, 1, At(0, 2));
            Assert.AreEqual(1, tank.Board.Bags.Count, "The pouch left the tank...");
            Assert.AreEqual("staff knife", Board(state.Members[1]), "...and the knife went with it to the healer.");
        }

        [Test]
        public void ABagDrop_LiesInTheFrame_AndNeverGoesToTheInventory()
        {
            StaticData data = TestData.Data(("EliteBagPercent", 100));
            ExpeditionState state = AtLoot(data);
            state.Loot[0] = new ItemOffer(OfferKind.Bag, "pouch", 0);

            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventory(data, state, 0), "Bags are never kept in the inventory.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 0, At(0, 1)), "Over the pack.");
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 0, 0, 0), "A bag merges into nothing.");
            ExpeditionRules.TakeLoot(data, state, 0, 0, At(0, 2));

            Assert.AreEqual(2, state.Members[0].Board.Bags.Count);
            Assert.AreEqual("pouch", state.Members[0].Board.BagAt(1, 2).Bag.Id);
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
            state.Members[2].Board.Items[0].Item = new EquippedItem(data.Items.Get("blade"), 200);

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
