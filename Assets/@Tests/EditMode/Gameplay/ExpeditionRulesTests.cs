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
            new PartyMember("anna", "tank", BattleRow.Front),
            new PartyMember("ben", "healer", BattleRow.Rear),
            new PartyMember("cora", "striker", BattleRow.Rear),
        };

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
            Assert.AreEqual(3, tank.Items.Length);
            Assert.AreEqual("blade", tank.Items[0].Item.Id);
            Assert.AreEqual(10, tank.Items[0].Grade);
            Assert.IsNull(tank.Items[1]);
            Assert.AreEqual(2, state.Members[2].Items.Length, "Slots come from the job.");
            CollectionAssert.AreEqual(new[] { "tonic", null, null }, state.Potions);
        }

        [Test]
        public void Create_RejectsInvalidParties()
        {
            StaticData data = TestData.Data(("RowCapacity", 2), ("PartySize", 3));

            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new PartyMember[0]));
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", BattleRow.Front),
                new PartyMember("anna", "tank", BattleRow.Front),
            }));
            Assert.Throws<ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[]
            {
                new PartyMember("anna", "tank", BattleRow.Front),
                new PartyMember("ben", "healer", BattleRow.Front),
                new PartyMember("cora", "striker", BattleRow.Front),
            }), "Three in a row of two.");
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
            if (state.Phase == ExpeditionPhase.ChoosingReward)
            {
                ExpeditionRules.SkipReward(state);
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
        public void BattleSetup_ListsFrontBeforeRear_AppliesAffinity_AndIsRebuiltIdentically()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            int nodeId = ExpeditionRules.AvailableNodes(state)[0].Id;

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, nodeId);

            Assert.AreEqual(ExpeditionPhase.InBattle, state.Phase);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, setup.Party.Select(u => u.SourceId));
            Assert.AreEqual(BattleRow.Front, setup.Party[0].Row);
            Assert.IsTrue(setup.Party.All(u => u.HasDog));
            Assert.IsTrue(setup.Enemies.All(u => !u.HasDog && u.Hp == u.MaxHp));
            Assert.AreEqual(-80, setup.EnemyCooldownPermille);
            Assert.AreEqual("tonic", setup.Potions[0].Id);

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
        public void CompleteBattle_OnVictory_CarriesHpOver_HealsABit_AndOffersRewards()
        {
            StaticData data = TestData.Data(("PostBattleHealPercent", 10));
            ExpeditionState state = Create(data);

            BattleEngine battle = Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.AreEqual(1, state.BattlesWon);
            Assert.AreEqual(ExpeditionPhase.ChoosingReward, state.Phase);
            BattleUnit tankUnit = battle.Party.Single(u => u.Setup.SourceId == "anna");
            Assert.Less(tankUnit.Hp, 100, "The tank was hit.");
            Assert.AreEqual(Math.Min(100, tankUnit.Hp + 10), state.Members[0].Hp, "HP carries over plus 10% of max HP.");
            Assert.AreEqual(data.Balance.RewardChoices, state.PendingRewards.Count);
            Assert.AreEqual(state.PendingRewards.Count, state.PendingRewards.Select(r => r.Id).Distinct().Count(), "No repeated reward.");
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
                member.Items[0] = null;
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
                if (state.Phase == ExpeditionPhase.ChoosingReward)
                {
                    ExpeditionRules.SkipReward(state);
                }
                else
                {
                    Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
                }
            }

            Assert.AreEqual(ExpeditionResult.Cleared, state.Result);
            Assert.AreEqual(3, state.BattlesWon, "Two battle floors and the boss.");
            Assert.IsEmpty(state.PendingRewards);
        }

        // ---- Rewards and the item board ------------------------------------------------------

        ExpeditionState AtReward(StaticData data, ulong seed = 1)
        {
            ExpeditionState state = Create(data, seed);
            Fight(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);
            Assert.AreEqual(ExpeditionPhase.ChoosingReward, state.Phase);
            return state;
        }

        [Test]
        public void Rewards_AreTheSameForTheSameSeed_AndItemGradeFollowsTheFloor()
        {
            StaticData data = TestData.Data();

            ExpeditionState first = AtReward(data, 9);
            ExpeditionState second = AtReward(data, 9);

            CollectionAssert.AreEqual(first.PendingRewards.Select(r => (r.Kind, r.Id, r.Grade)), second.PendingRewards.Select(r => (r.Kind, r.Id, r.Grade)));
            foreach (RewardOption option in first.PendingRewards.Where(r => r.Kind == RewardKind.Item))
            {
                Assert.AreEqual(8, option.Grade, "ItemGradeBase on floor 1.");
                Assert.Greater(data.Items.Get(option.Id).RewardWeight, 0);
            }
        }

        [Test]
        public void Rewards_DoNotOfferPotions_WhenEveryPotionSlotIsFull()
        {
            StaticData data = TestData.Data(("PotionSlots", 1), ("RewardChoices", 4));

            for (ulong seed = 1; seed <= 20; seed++)
            {
                ExpeditionState state = AtReward(data, seed);
                Assert.IsTrue(state.PendingRewards.All(r => r.Kind == RewardKind.Item), "The only potion slot holds the starting potion.");
                Assert.AreEqual(3, state.PendingRewards.Count, "Only three items can be offered.");
            }
        }

        [Test]
        public void TakeItemReward_PutsTheItemInTheSlot_ReplacingWhatWasThere()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtReward(data);
            int optionIndex = state.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            RewardOption option = state.PendingRewards[optionIndex];

            ExpeditionRules.TakeItemReward(data, state, optionIndex, 0, 0);

            Assert.AreEqual(option.Id, state.Members[0].Items[0].Item.Id, "The weapon in slot 0 was replaced.");
            Assert.AreEqual(option.Grade, state.Members[0].Items[0].Grade);
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsEmpty(state.PendingRewards);
        }

        [Test]
        public void TakePotionReward_FillsTheFirstEmptySlot()
        {
            StaticData data = TestData.Data(("RewardChoices", 4));
            ExpeditionState state = null;
            int optionIndex = -1;
            for (ulong seed = 1; optionIndex < 0; seed++)
            {
                state = AtReward(data, seed);
                optionIndex = state.PendingRewards.FindIndex(r => r.Kind == RewardKind.Potion);
            }

            ExpeditionRules.TakePotionReward(state, optionIndex);

            CollectionAssert.AreEqual(new[] { "tonic", "tonic", null }, state.Potions);
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
        }

        [Test]
        public void SkipReward_ReturnsToNodeChoice()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtReward(data);

            ExpeditionRules.SkipReward(state);

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsEmpty(state.PendingRewards);
        }

        [Test]
        public void RewardCommands_RejectWrongKindWrongPhaseAndBadIndexes()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = AtReward(data);
            int itemIndex = state.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakePotionReward(state, itemIndex), "An item is not a potion.");
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeItemReward(data, state, 99, 0, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeItemReward(data, state, itemIndex, 0, 9));
            Assert.Throws<ArgumentOutOfRangeException>(() => ExpeditionRules.TakeItemReward(data, state, itemIndex, 9, 0));

            ExpeditionRules.SkipReward(state);
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.SkipReward(state), "No reward is pending any more.");
        }

        [Test]
        public void SwapItems_ExchangesTwoSlots_IncludingEmptyOnes()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            EquippedItem tankWeapon = state.Members[0].Items[0];
            EquippedItem healerWeapon = state.Members[1].Items[0];

            ExpeditionRules.SwapItems(state, 0, 0, 1, 0);
            Assert.AreSame(healerWeapon, state.Members[0].Items[0]);
            Assert.AreSame(tankWeapon, state.Members[1].Items[0]);

            ExpeditionRules.SwapItems(state, 0, 0, 0, 2);
            Assert.IsNull(state.Members[0].Items[0]);
            Assert.AreSame(healerWeapon, state.Members[0].Items[2]);
        }

        [Test]
        public void SwapItems_DuringBattle_Throws()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Create(data);
            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.SwapItems(state, 0, 0, 1, 0));
        }

        [Test]
        public void SetRow_MovesAMember_UnlessTheRowIsFull()
        {
            StaticData data = TestData.Data(("RowCapacity", 2));
            ExpeditionState state = Create(data);

            Assert.IsFalse(ExpeditionRules.CanSetRow(data, state, 0, BattleRow.Rear), "The rear row already holds two.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.SetRow(data, state, 0, BattleRow.Rear));

            ExpeditionRules.SetRow(data, state, 1, BattleRow.Front);
            Assert.AreEqual(BattleRow.Front, state.Members[1].Row);
            Assert.IsTrue(ExpeditionRules.CanSetRow(data, state, 0, BattleRow.Rear));
            Assert.IsTrue(ExpeditionRules.CanSetRow(data, state, 0, BattleRow.Front), "Staying in the same row is always allowed.");
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
                    if (state.Phase == ExpeditionPhase.ChoosingReward)
                    {
                        trace.Add(string.Join(",", state.PendingRewards.Select(r => r.Id)));
                        ExpeditionRules.SkipReward(state);
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
