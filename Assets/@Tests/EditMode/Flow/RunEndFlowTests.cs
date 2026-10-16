using System;
using System.Linq;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The end of a run through the managers and the save file (Slice C stage 25): the forced final stand past the last day, the victory it
    /// can bring, and what the file keeps of it.
    /// </summary>
    public sealed class RunEndFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        [Test]
        public void PastTheLastDay_TheLobbyOffersOnlyTheFinalStand_AndWinningItEndsTheRun()
        {
            // A strong party, and a last day the first expedition (2 days) already passes.
            var kit = new FlowTestKit(FlowTestKit.StrongParty(("TotalDays", 1)));
            kit.InLobby();
            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();
            kit.Expedition.AcknowledgeReport();

            Assert.AreEqual(3, kit.Run.Run.Day);
            Assert.IsTrue(kit.Run.IsFinalStandDue);
            Assert.IsFalse(kit.Run.CanRest);
            Assert.Throws<InvalidOperationException>(() => kit.Run.Rest());
            Assert.AreEqual(DepartCheck.FinalStandDue, kit.Run.CanDepart("cave"));
            Assert.AreEqual(DepartCheck.Ok, kit.Run.CanDepart("pit"));

            kit.Expedition.Depart("pit");
            Assert.IsTrue(kit.Expedition.Expedition.FinalStand);
            Assert.AreEqual(1, kit.Expedition.AvailableNodes().Count, "The boss alone.");

            // The file keeps the final stand: a restart comes back to the same single boss node.
            FlowTestKit restarted = kit.Restart();
            Assert.IsTrue(restarted.Expedition.Expedition.FinalStand);
            Assert.AreEqual(1, restarted.Expedition.Expedition.Map.Nodes.Count);
            Assert.AreEqual(GamePhase.NodeMap, restarted.Expedition.Phase);

            restarted.PlayExpeditionToTheEnd();
            Assert.AreEqual(RunOutcome.Victory, restarted.Expedition.Report.Outcome);
            Assert.IsTrue(restarted.Expedition.Report.RunIsOver);
            restarted.Expedition.AcknowledgeReport();

            Assert.AreEqual(GamePhase.Lobby, restarted.Expedition.Phase);
            Assert.IsTrue(restarted.Run.Run.IsOver);
            Assert.AreEqual(RunOutcome.Victory, restarted.Run.Run.Outcome);
            Assert.AreEqual(DepartCheck.RunIsOver, restarted.Run.CanDepart("pit"));

            // The victory is in the file.
            FlowTestKit again = restarted.Restart();
            Assert.AreEqual(RunOutcome.Victory, again.Run.Run.Outcome);
            Assert.AreEqual(DefeatCause.None, again.Run.Run.DefeatCause);
            Assert.IsTrue(again.Run.Run.IsOver);

            again.Run.StartNewRun();
            Assert.AreEqual(RunOutcome.Ongoing, again.Run.Run.Outcome, "A new run can always be started.");
        }

        [Test]
        public void TheFinalDungeon_OpensAfterTheClearsItNames_AndClearingItWins()
        {
            var kit = new FlowTestKit(FlowTestKit.StrongParty());
            kit.InLobby();
            Assert.AreEqual(DepartCheck.Locked, kit.Run.CanDepart("pit"));
            Assert.IsFalse(kit.Run.IsUnlocked("pit"));

            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();
            kit.Expedition.AcknowledgeReport();
            Assert.IsTrue(kit.Run.IsUnlocked("pit"));
            Assert.AreEqual(DepartCheck.Ok, kit.Run.CanDepart("pit"));

            kit.Expedition.Depart("pit");
            Assert.IsFalse(kit.Expedition.Expedition.FinalStand, "Chosen before the last day: a whole expedition, not the stand.");
            Assert.AreEqual(2, kit.Expedition.Expedition.Map.FloorCount, "One floor and the boss.");
            kit.PlayExpeditionToTheEnd();

            Assert.AreEqual(RunOutcome.Victory, kit.Expedition.Report.Outcome);
            kit.Expedition.AcknowledgeReport();
            Assert.IsTrue(kit.Run.Run.IsOver);
        }

        [Test]
        public void ADeadlyFinalStand_LosesTheRun_AndTheFileSaysWhy()
        {
            var kit = new FlowTestKit(FlowTestKit.DeadlyEnemies(("TotalDays", 1), ("MinPartySize", 1)));
            kit.Run.StartNewRun();
            kit.Run.SetParty(FlowTestKit.Party(("anna", 1)));
            kit.Expedition.Depart("cave");
            kit.PlayExpeditionToTheEnd();
            kit.Expedition.AcknowledgeReport();
            Assert.IsTrue(kit.Run.IsFinalStandDue);

            kit.Run.SetParty(FlowTestKit.Party(("ben", 1)));
            kit.Expedition.Depart("pit");
            kit.PlayExpeditionToTheEnd();

            Assert.AreEqual(RunOutcome.Defeat, kit.Expedition.Report.Outcome);
            Assert.AreEqual(DefeatCause.FinalStandLost, kit.Expedition.Report.DefeatCause);
            kit.Expedition.AcknowledgeReport();

            FlowTestKit restarted = kit.Restart();
            Assert.AreEqual(DefeatCause.FinalStandLost, restarted.Run.Run.DefeatCause);
            Assert.IsTrue(restarted.Run.Run.IsOver);
            Assert.IsTrue(restarted.Run.Run.Roster.Any(), "Someone stayed home: lost, not wiped out.");
        }
    }
}
