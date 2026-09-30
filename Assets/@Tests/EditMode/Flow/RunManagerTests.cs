using System;
using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class RunManagerTests
    {
        [Test]
        public void Run_WhenNoRunWasStarted_Throws()
        {
            var kit = new FlowTestKit();

            Assert.IsFalse(kit.Run.HasRun);
            Assert.Throws<InvalidOperationException>(() => { RunState unused = kit.Run.Run; });
            Assert.Throws<InvalidOperationException>(() => kit.Run.Rest());
            Assert.Throws<InvalidOperationException>(() => kit.Run.SetParty(FlowTestKit.DefaultParty()));
        }

        [Test]
        public void StartNewRun_CreatesDayOneFromTheSeedSource_AndRaisesRunStarted()
        {
            var kit = new FlowTestKit(seed: 1234);
            int raised = 0;
            kit.Run.RunStarted += () => raised++;

            kit.Run.StartNewRun();

            Assert.IsTrue(kit.Run.HasRun);
            Assert.AreEqual(1234UL, kit.Run.Run.Seed);
            Assert.AreEqual(1, kit.Run.Run.Day);
            Assert.AreEqual(4, kit.Run.Run.Roster.Count);
            Assert.IsFalse(kit.Run.IsAway);
            Assert.AreEqual(1, raised);
        }

        [Test]
        public void SetParty_WhenValid_ChangesTheParty_AndPartyProblemExplainsInvalidOnes()
        {
            FlowTestKit kit = new FlowTestKit();
            kit.Run.StartNewRun();

            Assert.IsNull(kit.Run.PartyProblem(FlowTestKit.DefaultParty()));
            kit.Run.SetParty(FlowTestKit.DefaultParty());

            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, kit.Run.Run.Party.Select(p => p.MercenaryId));
            Assert.IsNotNull(kit.Run.PartyProblem(FlowTestKit.Party(("nobody", BattleRow.Front))));
            Assert.Throws<InvalidOperationException>(() => kit.Run.SetParty(FlowTestKit.Party(("nobody", BattleRow.Front))));
        }

        [Test]
        public void Rest_PassesADay()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();

            kit.Run.Rest();

            Assert.AreEqual(2, kit.Run.Run.Day);
        }

        [Test]
        public void CanDepart_ReportsWhyThePartyCannotLeave()
        {
            FlowTestKit kit = new FlowTestKit();
            kit.Run.StartNewRun();

            Assert.AreEqual(DepartCheck.PartyTooSmall, kit.Run.CanDepart("cave"));

            kit.Run.SetParty(FlowTestKit.DefaultParty());
            Assert.AreEqual(DepartCheck.Ok, kit.Run.CanDepart("cave"));
        }

        [Test]
        public void LobbyCommands_WhileThePartyIsAway_Throw()
        {
            FlowTestKit kit = new FlowTestKit().OnNodeMap();

            Assert.IsTrue(kit.Run.IsAway);
            Assert.Throws<InvalidOperationException>(() => kit.Run.Rest());
            Assert.Throws<InvalidOperationException>(() => kit.Run.SetParty(FlowTestKit.DefaultParty()));
            Assert.AreEqual(1, kit.Run.Run.Day, "A rejected command changes nothing.");
        }

        [Test]
        public void StartNewRun_WhileAway_DropsTheExpeditionAndStartsOver()
        {
            FlowTestKit kit = new FlowTestKit().InBattle();
            kit.Expedition.AdvanceBattle(500);

            kit.Run.StartNewRun();

            Assert.IsFalse(kit.Run.IsAway);
            Assert.AreEqual(GamePhase.Lobby, kit.Expedition.Phase);
            Assert.IsNull(kit.Expedition.Expedition);
            Assert.IsNull(kit.Expedition.Battle);
            Assert.IsNull(kit.Expedition.Report);
            Assert.AreEqual(0, kit.Run.Run.ExpeditionCount);
            Assert.IsEmpty(kit.Run.Run.Party);
        }
    }
}
