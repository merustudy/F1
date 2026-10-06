using System;
using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class ExpeditionManagerTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        [Test]
        public void Phase_WithNothingInProgress_IsLobby()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();

            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.IsEmpty(kit.Expedition.AvailableNodes());
        }

        [Test]
        public void Depart_StartsAnExpeditionOnTheNodeMap()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();

            kit.Expedition.Depart("cave");

            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.AreEqual("cave", kit.Expedition.Expedition.DungeonId);
            Assert.IsTrue(kit.Run.IsAway);
            Assert.AreEqual(1, kit.Run.Run.ExpeditionCount);
            Assert.IsNotEmpty(kit.Expedition.AvailableNodes());
        }

        [Test]
        public void Depart_WhenThePartyCannotLeave_ThrowsAndStaysInTheLobby()
        {
            var kit = new FlowTestKit();
            kit.Run.StartNewRun();

            Assert.Throws<InvalidOperationException>(() => kit.Expedition.Depart("cave"));

            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.IsFalse(kit.Run.IsAway);
        }

        [Test]
        public void Depart_WhenAlreadyAway_Throws()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();

            Assert.Throws<InvalidOperationException>(() => kit.Expedition.Depart("cave"));
        }

        [Test]
        public void EnterNode_StartsABattleAtTimeZero()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            MapNode node = kit.Expedition.AvailableNodes()[0];

            kit.Expedition.EnterNode(node.Id);

            Assert.AreEqual(GamePhase.Battle, kit.Expedition.Phase);
            Assert.AreSame(node, kit.Expedition.Battle.Node);
            Assert.AreEqual(0, kit.Expedition.Battle.Engine.TimeMs);
            Assert.IsFalse(kit.Expedition.Battle.IsFinished);
            Assert.AreEqual(ExpeditionPhase.InBattle, kit.Expedition.Expedition.Phase);
        }

        [Test]
        public void EnterNode_WhenTheNodeCannotBeChosen_Throws()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            int bossId = kit.Expedition.Expedition.Map.Nodes.Single(n => n.Kind == MapNodeKind.Boss).Id;

            Assert.Throws<InvalidOperationException>(() => kit.Expedition.EnterNode(bossId));
            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
        }

        [Test]
        public void AdvanceBattle_MovesBattleTimeForward()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();

            kit.Expedition.AdvanceBattle(300);
            kit.Expedition.AdvanceBattle(0);
            kit.Expedition.AdvanceBattle(200);

            Assert.AreEqual(500, kit.Expedition.Battle.Engine.TimeMs);
            Assert.Throws<ArgumentOutOfRangeException>(() => kit.Expedition.AdvanceBattle(-1));
        }

        [Test]
        public void WhenABattleIsWon_TheResultIsAppliedAtOnce_AndTheSessionStaysUntilClosed()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();

            kit.FightToTheEnd();

            Assert.AreEqual(BattleResult.Victory, kit.Expedition.Battle.Engine.Result);
            Assert.AreEqual(GamePhase.Battle, kit.Expedition.Phase, "The screen still shows the ended battle.");
            Assert.AreEqual(ExpeditionPhase.ChoosingReward, kit.Expedition.Expedition.Phase, "The expedition already moved on.");
            Assert.AreEqual(1, kit.Expedition.Expedition.BattlesWon);

            int endTime = kit.Expedition.Battle.Engine.TimeMs;
            kit.Expedition.AdvanceBattle(1000);
            Assert.AreEqual(endTime, kit.Expedition.Battle.Engine.TimeMs, "An ended battle does not advance.");

            kit.Expedition.CloseBattle();

            Assert.IsNull(kit.Expedition.Battle);
            Assert.AreEqual(GamePhase.Reward, kit.Expedition.Phase);
        }

        [Test]
        public void CloseBattle_WhileTheBattleIsOngoing_Throws()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();

            Assert.Throws<InvalidOperationException>(() => kit.Expedition.CloseBattle());
        }

        [Test]
        public void TryUsePotion_IsRecordedWithItsBattleTime()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(700);

            Assert.IsTrue(kit.Expedition.TryUsePotion(0, 0));
            Assert.IsFalse(kit.Expedition.TryUsePotion(0, 0), "The slot is empty now.");

            BattleInput input = kit.Expedition.Battle.Engine.Inputs.Single();
            Assert.AreEqual(BattleInputKind.UsePotion, input.Kind);
            Assert.AreEqual(700, input.TimeMs);
        }

        [Test]
        public void TryRetreat_WhenItSucceeds_EndsTheExpeditionAndSettlesAtOnce()
        {
            FlowTestKit kit = new FlowTestKit(FlowTestKit.StrongParty(("RetreatChancePercent", 100))).InBattle();
            kit.Expedition.AdvanceBattle(100);

            Assert.IsTrue(kit.Expedition.TryRetreat());

            Assert.AreEqual(BattleResult.Retreated, kit.Expedition.Battle.Engine.Result);
            Assert.IsNull(kit.Expedition.Expedition, "The expedition state is gone once it is settled.");
            Assert.AreEqual(ExpeditionResult.Retreated, kit.Expedition.Report.Result);
            Assert.IsFalse(kit.Run.IsAway);
            Assert.AreEqual(3, kit.Run.Run.Day, "The dungeon takes two days even when the party retreats.");
            Assert.AreEqual(5, RunRules.FindMercenary(kit.Run.Run, "anna").Fatigue, "One battle was entered.");

            kit.Expedition.CloseBattle();
            Assert.AreEqual(GamePhase.Settlement, kit.Expedition.Phase);

            kit.Expedition.AcknowledgeReport();
            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.IsNull(kit.Expedition.Report);
        }

        [Test]
        public void TryRetreat_WhenItFails_KeepsFighting()
        {
            FlowTestKit kit = new FlowTestKit(FlowTestKit.StrongParty(("RetreatChancePercent", 0))).InBattle();

            Assert.IsTrue(kit.Expedition.TryRetreat(), "The attempt was made.");

            Assert.IsFalse(kit.Expedition.Battle.IsFinished);
            Assert.IsFalse(kit.Expedition.TryRetreat(), "Retreat is on cooldown.");
            Assert.AreEqual(GamePhase.Battle, kit.Expedition.Phase);
        }

        [Test]
        public void Rewards_CanBeTakenOrSkipped_ThenTheNodeMapReturns()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.FightToTheEnd();
            kit.Expedition.CloseBattle();
            ExpeditionState state = kit.Expedition.Expedition;
            int itemOption = state.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            RewardOption option = state.PendingRewards[itemOption];

            Assert.IsTrue(kit.Expedition.CanPlaceReward(itemOption, 0, 1), "An empty cell behind the weapon.");
            Assert.IsFalse(kit.Expedition.CanPlaceReward(itemOption, 0, 9));
            kit.Expedition.TakeItemReward(itemOption, 0, 1);

            Assert.AreEqual(option.Id, state.Members[0].Items[1].Item.Id);
            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.IsFalse(kit.Expedition.CanPlaceReward(itemOption, 0, 2), "No reward is pending any more.");
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.SkipReward(), "There is no reward to skip any more.");
        }

        [Test]
        public void AnItemReward_CanGoStraightToTheInventory()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.FightToTheEnd();
            kit.Expedition.CloseBattle();
            ExpeditionState state = kit.Expedition.Expedition;
            int itemOption = state.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            RewardOption option = state.PendingRewards[itemOption];
            Assert.IsTrue(kit.Expedition.CanTakeRewardToInventory(itemOption), "The inventory is empty.");

            kit.Expedition.TakeItemRewardToInventory(itemOption);

            Assert.AreEqual(option.Id, state.Inventory.Single().Item.Id);
            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.IsFalse(kit.Expedition.CanTakeRewardToInventory(itemOption), "No reward is pending any more.");
        }

        [Test]
        public void BoardCommands_WorkBetweenBattles_AndThrowDuringOne()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            ExpeditionState state = kit.Expedition.Expedition;
            string weapon = state.Members[0].Items[0].Item.Id;

            Assert.IsTrue(kit.Expedition.CanMoveItem(0, 0, 1, 1));
            kit.Expedition.MoveItem(0, 0, 1, 1);
            Assert.IsEmpty(state.Members[0].Items);
            Assert.AreEqual(weapon, state.Members[1].Items[1].Item.Id);

            Assert.IsTrue(kit.Expedition.CanPickItem(1, 1));
            Assert.IsFalse(kit.Expedition.CanPickItem(0, 0), "The board is empty now.");
            Assert.IsTrue(kit.Expedition.CanMoveToInventory(1, 1));
            kit.Expedition.MoveToInventory(1, 1);
            Assert.AreEqual(weapon, state.Inventory.Single().Item.Id);

            Assert.IsTrue(kit.Expedition.CanPlaceFromInventory(0, 0, 0));
            kit.Expedition.PlaceFromInventory(0, 0, 0);
            Assert.AreEqual(weapon, state.Members[0].Items.Single().Item.Id);
            Assert.IsEmpty(state.Inventory);

            Assert.IsTrue(kit.Expedition.CanMoveToRow(1, 1));
            Assert.IsFalse(kit.Expedition.CanMoveToRow(1, 2), "Already there.");
            kit.Expedition.MoveToRow(1, 1);
            CollectionAssert.AreEqual(new[] { 2, 1, 3 }, state.Members.Select(m => m.Row), "The two traded places.");

            kit.Expedition.EnterNode(kit.Expedition.AvailableNodes()[0].Id);

            Assert.IsFalse(kit.Expedition.CanMoveToRow(1, 2));
            Assert.IsFalse(kit.Expedition.CanMoveItem(0, 0, 1, 1));
            Assert.IsFalse(kit.Expedition.CanPickItem(0, 0));
            Assert.IsFalse(kit.Expedition.CanMoveToInventory(0, 0));
            Assert.IsFalse(kit.Expedition.CanPlaceFromInventory(0, 0, 1));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.MoveToRow(1, 2));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.MoveItem(0, 0, 1, 1));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.MoveToInventory(0, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.PlaceFromInventory(0, 0, 1));
        }

        [Test]
        public void WhenThePartyIsWiped_TheDeadLeaveTheRosterForGood()
        {
            FlowTestKit kit = new FlowTestKit(FlowTestKit.DeadlyEnemies()).InBattle();

            kit.FightToTheEnd();

            Assert.AreEqual(BattleResult.Defeat, kit.Expedition.Battle.Engine.Result);
            Assert.AreEqual(ExpeditionResult.Wiped, kit.Expedition.Report.Result);
            CollectionAssert.AreEquivalent(new[] { "anna", "ben", "cora" }, kit.Expedition.Report.FallenIds);
            CollectionAssert.AreEqual(new[] { "dan" }, kit.Run.Run.Roster.Select(m => m.Id));
            CollectionAssert.AreEquivalent(new[] { "anna", "ben", "cora" }, kit.Run.Run.Fallen);
            Assert.IsEmpty(kit.Run.Run.Party, "The dead are removed from the lobby party.");
            Assert.IsFalse(kit.Run.Run.IsOver, "One mercenary is still alive.");
        }

        [Test]
        public void WhenTheLastMercenaryDies_TheRunIsOver_AndTheLobbyCannotDepart()
        {
            var kit = new FlowTestKit(FlowTestKit.DeadlyEnemies());
            kit.Run.StartNewRun();
            kit.Run.SetParty(FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();
            kit.Expedition.AcknowledgeReport();

            kit.Run.SetParty(FlowTestKit.Party(("dan", 1)));
            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();

            Assert.IsTrue(kit.Expedition.Report.RunIsOver);
            kit.Expedition.AcknowledgeReport();

            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.IsTrue(kit.Run.Run.IsOver);
            Assert.IsEmpty(kit.Run.Run.Roster);
            Assert.AreEqual(DepartCheck.RunIsOver, kit.Run.CanDepart("cave"));
            Assert.Throws<InvalidOperationException>(() => kit.Run.Rest());

            kit.Run.StartNewRun();
            Assert.IsFalse(kit.Run.Run.IsOver, "A new run can always be started.");
        }

        [Test]
        public void Commands_InTheWrongPhase_Throw()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();

            Assert.Throws<InvalidOperationException>(() => kit.Expedition.EnterNode(0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.AdvanceBattle(100));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TryRetreat());
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TryUsePotion(0, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.CloseBattle());
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.SkipReward());
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TakePotionReward(0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TakeItemReward(0, 0, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.TakeItemRewardToInventory(0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.MoveItem(0, 0, 1, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.MoveToInventory(0, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.PlaceFromInventory(0, 0, 0));
            Assert.Throws<InvalidOperationException>(() => kit.Expedition.AcknowledgeReport());
            Assert.IsFalse(kit.Expedition.CanTakePotionReward);
            Assert.IsFalse(kit.Expedition.CanPlaceReward(0, 0, 0));
            Assert.IsFalse(kit.Expedition.CanMoveItem(0, 0, 1, 0));
            Assert.IsFalse(kit.Expedition.CanMoveToInventory(0, 0));
            Assert.IsFalse(kit.Expedition.CanPlaceFromInventory(0, 0, 0));
        }
    }
}
