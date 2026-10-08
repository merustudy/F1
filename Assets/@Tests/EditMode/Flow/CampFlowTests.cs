using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>A camp node through the application layer: entering it, resting there, and coming back to it after a restart.</summary>
    public sealed class CampFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        /// <summary>The strong party of the flow kit in a cave whose second floor is all camps: a battle, a camp, then the boss.</summary>
        static StaticData CaveWithACamp()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = new List<JobData>
            {
                new JobData("tank", TestData.Text("tank"), 100, 3, "blade", 200, 1, null),
                new JobData("healer", TestData.Text("healer"), 60, 3, "staff", 10, 2, null),
                new JobData("striker", TestData.Text("striker"), 80, 2, "blade", 200, 3, null),
            };
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }, null, campFloor: 2),
            };
            return new StaticData(parts);
        }

        /// <summary>Wins the first floor's battle, leaves its loot and makes camp on the second floor.</summary>
        static FlowTestKit AtTheCamp()
        {
            FlowTestKit kit = new FlowTestKit(CaveWithACamp()).OnNodeMap();
            kit.Expedition.EnterNode(kit.Expedition.AvailableNodes()[0].Id);
            kit.FightToTheEnd();
            kit.Expedition.CloseBattle();
            kit.Expedition.LeaveLoot();
            MapNode camp = kit.Expedition.AvailableNodes().First();
            Assert.AreEqual(MapNodeKind.Camp, camp.Kind, "The second floor is all camps.");
            kit.Expedition.EnterNode(camp.Id);
            return kit;
        }

        [Test]
        public void EnteringACampNode_StartsNoBattle_AndIsSaved()
        {
            FlowTestKit kit = AtTheCamp();

            Assert.AreEqual(GamePhase.Camp, kit.Expedition.Phase);
            Assert.IsNull(kit.Expedition.Battle);
            Assert.IsEmpty(kit.Expedition.AvailableNodes(), "No node is offered until the party has done something at the camp.");
            Assert.AreEqual("AtCamp", kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition.Phase);

            FlowTestKit restarted = kit.Restart();
            Assert.AreEqual(GamePhase.Camp, restarted.Expedition.Phase, "Closing the app at a camp comes back to the camp.");
        }

        [Test]
        public void RestingAtTheCamp_HealsAndGoesOnToTheNextFloor()
        {
            FlowTestKit kit = AtTheCamp();
            ExpeditionMember anna = kit.Expedition.Expedition.Members.Single(m => m.MercenaryId == "anna");
            anna.Hp = 1;
            int fatigue = anna.Fatigue;

            kit.Expedition.RestAtCamp();

            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.AreEqual(1 + anna.MaxHp * kit.Data.Balance.CampHealPercent / 100, anna.Hp);
            Assert.AreEqual(System.Math.Max(0, fatigue - kit.Data.Balance.CampFatigueRelief), anna.Fatigue);
            Assert.AreEqual(MapNodeKind.Boss, kit.Expedition.AvailableNodes().Single().Kind);
            Assert.AreEqual("ChoosingNode", kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition.Phase, "The rest is saved.");
        }

        [Test]
        public void TheCampsUpkeep_RaisesTheItemATier_AndGoesOnToTheNextFloor_AndIsSaved()
        {
            FlowTestKit kit = AtTheCamp();
            Assert.IsTrue(kit.Expedition.CanUpgradeAtCamp(0, 0));

            kit.Expedition.UpgradeAtCamp(0, 0);

            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.AreEqual(ItemTier.Bronze, kit.Expedition.Expedition.Members[0].Items[0].Tier);
            Assert.AreEqual(MapNodeKind.Boss, kit.Expedition.AvailableNodes().Single().Kind);
            ExpeditionRecord saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition;
            Assert.AreEqual("Bronze", saved.Members[0].Items[0].Tier, "The upkeep is saved.");
            Assert.IsFalse(kit.Expedition.CanUpgradeAtCamp(0, 0), "Not on the map.");
        }

        [Test]
        public void AtTheCamp_TheBoardsAndRowsCanBeRearranged_ButNoBattleCommand()
        {
            FlowTestKit kit = AtTheCamp();

            Assert.IsTrue(kit.Expedition.CanMoveToRow(0, 2));
            kit.Expedition.MoveToRow(0, 2);
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.AdvanceBattle(100));
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.LeaveLoot());
        }
    }
}
