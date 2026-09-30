using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The core loop without any screen: lobby -> expedition -> settlement -> lobby, and again
    /// (Docs/Architecture/09_VERTICAL_SLICE.md, Acceptance 1, 2, 3 and 5).
    /// </summary>
    public sealed class GameLoopTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        [Test]
        public void OneLap_ChangesTheLobby_AndASecondExpeditionCanDepart()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();

            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();

            SettlementReport report = kit.Expedition.Report;
            Assert.AreEqual(ExpeditionResult.Cleared, report.Result);
            Assert.AreEqual(3, report.DayAfter);
            kit.Expedition.AcknowledgeReport();

            RunState run = kit.Run.Run;
            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.AreEqual(3, run.Day, "The dungeon takes two days.");
            Assert.AreEqual(70, RunRules.FindMercenary(run, "anna").Fatigue, "The dungeon costs 30 fatigue.");
            Assert.AreEqual(100, RunRules.FindMercenary(run, "dan").Fatigue, "Dan stayed home.");
            Assert.AreEqual(1, run.ClearedDungeons["cave"]);
            Assert.AreEqual(4, run.Roster.Count);

            Assert.AreEqual(DepartCheck.Ok, kit.Run.CanDepart("cave"));
            kit.Expedition.Depart("cave");

            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.AreEqual(2, run.ExpeditionCount);
            Assert.IsTrue(
                kit.Expedition.Expedition.Members.All(m => m.Hp == m.MaxHp && m.Items.Count(i => i != null) == 1),
                "Every expedition starts at full HP with only the job weapon: items do not survive the return.");
        }

        [Test]
        public void WhenFatigueRunsOut_RestingMakesThePartyAbleToDepartAgain()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            for (int i = 0; i < 3; i++)
            {
                kit.Expedition.Depart("cave");
                kit.PlayExpeditionToTheEnd();
                kit.Expedition.AcknowledgeReport();
            }

            Assert.AreEqual(10, RunRules.FindMercenary(kit.Run.Run, "anna").Fatigue);
            Assert.AreEqual(DepartCheck.NotEnoughFatigue, kit.Run.CanDepart("cave"));

            kit.Run.Rest();
            kit.Run.Rest();

            Assert.AreEqual(DepartCheck.Ok, kit.Run.CanDepart("cave"), "Two days of rest restore 20 fatigue.");
        }

        [TestCase(1)]
        [TestCase(16)]
        [TestCase(33)]
        [TestCase(250)]
        [TestCase(5000)]
        public void FrameLength_DoesNotChangeTheOutcome(int stepMs)
        {
            ulong reference = PlayAndHash(100);

            Assert.AreEqual(reference, PlayAndHash(stepMs));
        }

        [Test]
        public void TheSameSeedAndInputs_GiveTheSameExpedition()
        {
            Assert.AreEqual(PlayWithPotionAndHash(), PlayWithPotionAndHash());
        }

        /// <summary>Plays one expedition with the given step and digests every battle log and the final run state.</summary>
        static ulong PlayAndHash(int stepMs)
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();
            ulong hash = 17;
            while (kit.Expedition.Phase != GamePhase.Settlement)
            {
                switch (kit.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        kit.Expedition.EnterNode(kit.Expedition.AvailableNodes()[0].Id);
                        break;
                    case GamePhase.Battle:
                        kit.FightToTheEnd(stepMs);
                        hash = hash * 31 + BattleLog.Hash(kit.Expedition.Battle.Engine.Events);
                        kit.Expedition.CloseBattle();
                        break;
                    default:
                        kit.Expedition.SkipReward();
                        break;
                }
            }

            return hash * 31 + (ulong)kit.Run.Run.Day;
        }

        static ulong PlayWithPotionAndHash()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(2500);
            kit.Expedition.TryUsePotion(0, 0);
            kit.FightToTheEnd(40);
            return BattleLog.Hash(kit.Expedition.Battle.Engine.Events);
        }
    }
}
