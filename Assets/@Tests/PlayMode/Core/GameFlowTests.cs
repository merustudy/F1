using System.Collections;
using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    /// <summary>The application layer after a real boot, over the shipped data.</summary>
    public sealed class GameFlowTests
    {
        [SetUp]
        public void SetUp()
        {
            string saveRoot = BootTestUtil.UseTemporarySaveRoot();
            BootTestUtil.WriteSettings(saveRoot, "ko-KR");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return BootTestUtil.ShutdownApp();
        }

        [UnityTest]
        public IEnumerator AfterBoot_ThereIsNoRunYet()
        {
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();

            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.IsFalse(Managers.Run.HasRun);
            Assert.AreEqual(GamePhase.Lobby, Managers.Expedition.Phase);
        }

        [UnityTest]
        public IEnumerator AfterBoot_AnExpeditionOverTheShippedDataRunsToItsSettlement()
        {
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();
            RunManager run = Managers.Run;
            ExpeditionManager expedition = Managers.Expedition;
            StaticData data = Managers.Data.Data;

            run.StartNewRun();
            List<PartySlot> party = run.Run.Roster
                .Take(data.Balance.PartySize)
                .Select((m, index) => new PartySlot { MercenaryId = m.Id, Row = BattleRows.Front + index })
                .ToList();
            Assert.IsNull(run.PartyProblem(party));
            run.SetParty(party);
            string dungeonId = data.Dungeons.Ordered[0].Id;
            Assert.AreEqual(DepartCheck.Ok, run.CanDepart(dungeonId));

            expedition.Depart(dungeonId);
            int guard = 0;
            while (expedition.Phase != GamePhase.Settlement)
            {
                Assert.Less(++guard, 100000, "The expedition did not end.");
                switch (expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        expedition.EnterNode(expedition.AvailableNodes()[0].Id);
                        break;
                    case GamePhase.Battle:
                        if (expedition.Battle.IsFinished)
                        {
                            expedition.CloseBattle();
                        }
                        else
                        {
                            expedition.AdvanceBattle(250);
                        }

                        break;
                    case GamePhase.Loot:
                        expedition.LeaveLoot();
                        break;
                    case GamePhase.Camp:
                        // The camp floor of the long map (stage 13): the party rests and goes on.
                        expedition.RestAtCamp();
                        break;
                    case GamePhase.Shop:
                        // A shop node (stage 17): the party buys nothing and goes on.
                        expedition.LeaveShop();
                        break;
                }
            }

            Assert.AreNotEqual(ExpeditionResult.None, expedition.Report.Result);
            Assert.Greater(run.Run.Day, 1);
            Assert.IsFalse(run.IsAway);
        }
    }
}
