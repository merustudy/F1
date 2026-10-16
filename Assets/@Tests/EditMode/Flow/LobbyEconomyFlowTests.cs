using System;
using System.Linq;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The gold, the stagecoach and the inn through the managers and the save file (Slice C stage 26): the settlement pays, hiring, dismissing and
    /// the treatment go through RunManager and are saved, and a restart brings them back. The strong party clears the two-floor cave: 2 x 2 + 20.
    /// </summary>
    public sealed class LobbyEconomyFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        [Test]
        public void TheSettlementPays_AndHiringDismissingAndTreating_AreSavedAndComeBackAfterARestart()
        {
            var kit = new FlowTestKit(FlowTestKit.StrongParty(("HirePrice", 20)));
            kit.InLobby();
            Assert.AreEqual(0, kit.Run.Run.Gold);
            Assert.AreEqual(3, kit.Run.Run.Recruits.Count, "Candidates from day one.");
            Assert.AreEqual(HireCheck.NotEnoughGold, kit.Run.CanHire(0));

            kit.Expedition.Depart("cave");
            Assert.AreEqual(HireCheck.NoCandidate, kit.Run.CanHire(0), "Not while the party is away.");
            Assert.Throws<InvalidOperationException>(() => kit.Run.Hire(0));
            Assert.Throws<InvalidOperationException>(() => kit.Run.Dismiss("dan"));
            Assert.Throws<InvalidOperationException>(() => kit.Run.Treat("dan"));
            kit.PlayExpeditionToTheEnd();
            SettlementReport report = kit.Expedition.Report;
            Assert.AreEqual(24, report.GoldEarned);
            Assert.AreEqual(2, report.FloorsReached);
            Assert.AreEqual(24, kit.Run.Run.Gold);
            kit.Expedition.AcknowledgeReport();

            // Hire the first candidate, dismiss dan, treat anna (she came back tired).
            string candidate = kit.Run.Run.Recruits[0].Id;
            string job = kit.Run.Run.Recruits[0].JobId;
            Assert.AreEqual(HireCheck.Ok, kit.Run.CanHire(0));
            kit.Run.Hire(0);
            Assert.AreEqual(4, kit.Run.Run.Gold);
            Assert.AreEqual(job, RunRules.FindMercenary(kit.Run.Run, candidate).JobId);
            Assert.IsTrue(kit.Run.CanDismiss("dan"));
            kit.Run.Dismiss("dan");
            MercenaryState anna = RunRules.FindMercenary(kit.Run.Run, "anna");
            Assert.Greater(anna.Fatigue, 0, "She fought.");
            kit.Run.Run.Gold = 20;
            Assert.AreEqual(TreatCheck.Ok, kit.Run.CanTreat("anna"));
            int before = anna.Fatigue;
            kit.Run.Treat("anna");
            Assert.AreEqual(Math.Max(0, before - 40), anna.Fatigue);
            Assert.AreEqual(0, kit.Run.Run.Gold);
            Assert.AreNotEqual(TreatCheck.Ok, kit.Run.CanTreat("anna"), "No gold left (or nothing left to take off).");

            // Everything is in the file.
            FlowTestKit again = kit.Restart();
            RunState run = again.Run.Run;
            Assert.AreEqual(0, run.Gold);
            Assert.IsTrue(run.Roster.Any(m => m.Id == candidate && m.JobId == job));
            Assert.IsFalse(run.Roster.Any(m => m.Id == "dan"));
            CollectionAssert.AreEqual(new[] { "dan" }, run.Dismissed);
            Assert.AreEqual(2, run.Recruits.Count);
            CollectionAssert.AreEqual(kit.Run.Run.Recruits.Select(r => r.Id), run.Recruits.Select(r => r.Id));
            Assert.AreEqual(anna.Fatigue, RunRules.FindMercenary(run, "anna").Fatigue);
            Assert.AreEqual(1, run.ExpeditionsByDungeon["cave"]);
            Assert.AreEqual(GamePhase.Lobby, again.Expedition.Phase);
        }

        [Test]
        public void WhileSavingIsBlocked_TheNewCommandsAreRefusedToo()
        {
            var kit = new FlowTestKit(FlowTestKit.StrongParty());
            kit.InLobby();
            kit.Run.Run.Gold = 100;
            System.IO.Directory.Delete(kit.SaveRoot, true);
            System.IO.File.WriteAllText(kit.SaveRoot, "blocked");
            kit.Run.Hire(0);
            Assert.IsTrue(kit.Run.IsSaveBlocked, "The hire happened but is not confirmed.");

            Assert.Throws<InvalidOperationException>(() => kit.Run.Hire(0));
            Assert.Throws<InvalidOperationException>(() => kit.Run.Dismiss("dan"));
            Assert.Throws<InvalidOperationException>(() => kit.Run.Treat("anna"));

            System.IO.File.Delete(kit.SaveRoot);
            System.IO.Directory.CreateDirectory(kit.SaveRoot);
            Assert.IsTrue(kit.Run.RetrySave());
            Assert.AreEqual(5, kit.Restart().Run.Run.Roster.Count);
        }
    }
}
