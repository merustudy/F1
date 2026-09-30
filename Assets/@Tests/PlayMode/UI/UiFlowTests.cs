using System.Collections;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace F1.Tests
{
    /// <summary>
    /// The screens over the shipped data, driven through their buttons
    /// (Docs/Architecture/09_VERTICAL_SLICE.md, Acceptance 1, 4, 8 and 11).
    /// </summary>
    public sealed class UiFlowTests
    {
        string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = BootTestUtil.UseTemporarySaveRoot();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return BootTestUtil.ShutdownApp();
        }

        [UnityTest]
        public IEnumerator Boot_ShowsTheTitle_WithoutContinueWhenThereIsNoRun()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();

            Assert.AreEqual("새 런", UiTestUtil.TextAt(title, "Frame/Buttons/NewRun/NewRunLabel"), "Fixed labels come from the string table.");
            Assert.AreEqual("언어: 한국어", UiTestUtil.TextAt(title, "Frame/Buttons/Language/LanguageLabel"));
            Assert.IsFalse(UiTestUtil.At(title, "Frame/Buttons/Continue").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Title_LanguageButton_SwitchesEveryLabelAndIsSaved()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();

            UiTestUtil.Click(title, "Frame/Buttons/Language");
            float deadline = Time.realtimeSinceStartup + UiTestUtil.DefaultTimeoutSeconds;
            while (Managers.Setting.LocaleCode != "en-US" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;

            Assert.AreEqual("en-US", Managers.Setting.LocaleCode);
            Assert.AreEqual("New Run", UiTestUtil.TextAt(title, "Frame/Buttons/NewRun/NewRunLabel"));
            Assert.AreEqual("Language: English", UiTestUtil.TextAt(title, "Frame/Buttons/Language/LanguageLabel"));
        }

        [UnityTest]
        public IEnumerator OneLap_ThroughEveryScreen_EndsInAChangedLobby_AndCanDepartAgain()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            StaticData data = Managers.Data.Data;
            string dungeonId = data.Dungeons.Ordered[0].Id;

            // Title -> lobby
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsTrue(Managers.Resource.IsScopeOpen(ResourceScope.Lobby));
            Assert.AreEqual(data.Mercenaries.Count, UiTestUtil.Views<RosterEntryView>(lobby).Length);
            Assert.IsFalse(UiTestUtil.ButtonAt(lobby, "Frame/Expedition/Depart").interactable, "An empty party cannot depart.");

            // Party: the first three mercenaries of the roster, each in the row its job recommends.
            RunState run = Managers.Run.Run;
            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            for (int i = 0; i < data.Balance.PartySize; i++)
            {
                BattleRow row = data.Jobs.Get(run.Roster[i].JobId).RecommendedRow;
                UiTestUtil.Click(row == BattleRow.Front ? entries[i].Front : entries[i].Rear);
            }

            Assert.AreEqual(data.Balance.PartySize, run.Party.Count);
            Assert.IsFalse(entries[data.Balance.PartySize].Front.interactable, "The party is full.");

            // Lobby -> node map
            UiTestUtil.Click(lobby, "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            Assert.IsTrue(Managers.Resource.IsScopeOpen(ResourceScope.Expedition));
            Assert.IsFalse(Managers.Resource.IsScopeOpen(ResourceScope.Lobby), "Only the scope of the shown screen stays open.");

            int battles = 0;
            while (Managers.Expedition.Phase != GamePhase.Settlement)
            {
                Assert.Less(battles, 20, "The expedition did not end.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                    {
                        NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
                        MapNodeView node = UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable);
                        UiTestUtil.Click(node.Button);
                        UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
                        yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
                        break;
                    }

                    case GamePhase.Battle:
                    {
                        battles++;
                        BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
                        battle.Clock.SpeedPercent = 100000;
                        float deadline = Time.realtimeSinceStartup + 60f;
                        while (!Managers.Expedition.Battle.IsFinished)
                        {
                            Assert.Less(Time.realtimeSinceStartup, deadline, "The battle did not end.");
                            yield return null;
                        }

                        yield return null;
                        Assert.IsTrue(UiTestUtil.At(battle, "Frame/ResultPanel").gameObject.activeSelf);
                        UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/Continue");
                        yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;
                    }

                    case GamePhase.Reward:
                    {
                        RewardScreen reward = UiTestUtil.Screen<RewardScreen>();
                        UiTestUtil.Click(reward, "Frame/Skip");
                        yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
                        break;
                    }
                }
            }

            // Settlement -> lobby
            SettlementScreen settlement = UiTestUtil.Screen<SettlementScreen>();
            SettlementReport report = Managers.Expedition.Report;
            UiTestUtil.Click(settlement, "Frame/Panel/Confirm");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);

            lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsFalse(Managers.Resource.IsScopeOpen(ResourceScope.Expedition), "The expedition's resources are released on return.");
            Assert.AreEqual(1 + data.Dungeons.Get(dungeonId).DurationDays, run.Day);
            Assert.AreEqual(run.Roster.Count, UiTestUtil.Views<RosterEntryView>(lobby).Length, "The dead are gone from the roster list.");
            Assert.AreEqual(data.Mercenaries.Count - report.FallenIds.Count, run.Roster.Count);

            // A second expedition can depart, or the lobby says why not.
            if (Managers.Run.CanDepart(dungeonId) == DepartCheck.Ok)
            {
                UiTestUtil.Click(lobby, "Frame/Expedition/Depart");
                yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
                Assert.AreEqual(2, run.ExpeditionCount);
            }
            else
            {
                Assert.IsFalse(UiTestUtil.ButtonAt(lobby, "Frame/Expedition/Depart").interactable);
                Assert.IsNotEmpty(UiTestUtil.TextAt(lobby, "Frame/Expedition/DepartStatus"));
            }
        }

        [UnityTest]
        public IEnumerator Battle_PotionAndRetreatButtons_SendTimedInputs()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen screen = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            screen.Clock.Paused = true;
            Managers.Expedition.AdvanceBattle(1200 - engine.TimeMs);
            yield return null;

            // A potion is armed by clicking it and used by clicking an ally.
            PotionSlotView[] potions = UiTestUtil.Views<PotionSlotView>(screen);
            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/PartyFront"));
            Assert.IsFalse(units[0].Button.interactable, "Allies are not clickable until a potion is armed.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            UiTestUtil.Click(units[0].Button);

            Assert.AreEqual(1, engine.Inputs.Count);
            Assert.AreEqual(BattleInputKind.UsePotion, engine.Inputs[0].Kind);
            Assert.AreEqual(1200, engine.Inputs[0].TimeMs, "The input carries the battle time it was made at, even while paused.");
            Assert.IsNull(engine.Potions[0]);

            // Retreat is an attempt; whatever the roll, it is recorded.
            UiTestUtil.Click(screen, "Frame/Controls/Retreat");
            Assert.AreEqual(2, engine.Inputs.Count);
            Assert.AreEqual(BattleInputKind.Retreat, engine.Inputs[1].Kind);
            Assert.AreEqual(1, engine.Events.Count(e => e.Kind == BattleEventKind.RetreatAttempted));
        }

        [UnityTest]
        public IEnumerator Lobby_WhenNoMercenaryIsLeft_ShowsTheEndAndOffersANewRun()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/RunOverPanel").gameObject.activeSelf);

            // Put the run into its end state directly; reaching it by play takes several lost expeditions.
            RunState run = Managers.Run.Run;
            foreach (MercenaryState mercenary in run.Roster.ToList())
            {
                run.Fallen.Add(mercenary.Id);
            }

            run.Roster.Clear();
            run.Party.Clear();
            run.IsOver = true;
            lobby.Refresh();
            yield return null;

            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/RunOverPanel").gameObject.activeSelf);
            Assert.AreEqual(0, UiTestUtil.Views<RosterEntryView>(lobby).Length);

            UiTestUtil.Click(lobby, "Frame/RunOverPanel/RunOverBox/RunOverNewRun");
            yield return null;
            yield return null;

            Assert.IsFalse(Managers.Run.Run.IsOver);
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/RunOverPanel").gameObject.activeSelf);
            Assert.AreEqual(Managers.Data.Data.Mercenaries.Count, UiTestUtil.Views<RosterEntryView>(lobby).Length);
        }

        [UnityTest]
        public IEnumerator Title_NewRunOverARunInProgress_AsksFirst()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            Managers.Run.Rest();
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Header/ToTitle");
            yield return UiTestUtil.WaitForScreen(ScreenId.Title);
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Assert.IsTrue(UiTestUtil.At(title, "Frame/Buttons/Continue").gameObject.activeSelf);

            UiTestUtil.Click(title, "Frame/Buttons/NewRun");
            yield return null;

            Assert.IsTrue(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);
            Assert.AreEqual(2, Managers.Run.Run.Day, "Nothing is lost until the player confirms.");

            UiTestUtil.Click(title, "Frame/ConfirmPanel/ConfirmBox/No");
            Assert.IsFalse(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);

            UiTestUtil.Click(title, "Frame/Buttons/NewRun");
            UiTestUtil.Click(title, "Frame/ConfirmPanel/ConfirmBox/Yes");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);

            Assert.AreEqual(1, Managers.Run.Run.Day);
        }

        [UnityTest]
        public IEnumerator ShowAsync_WhileAScreenIsBeingReplaced_Throws_AndTheScreenIgnoresClicks()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Managers.Run.StartNewRun();

            System.Threading.Tasks.Task first = Managers.UI.ShowAsync(ScreenId.Lobby);

            Assert.IsTrue(Managers.UI.IsBusy);
            Assert.IsFalse(title.GetComponent<CanvasGroup>().blocksRaycasts, "The screen being replaced takes no more clicks.");
            System.Threading.Tasks.Task second = Managers.UI.ShowAsync(ScreenId.Title);
            yield return TaskUtil.AwaitIgnoringFailure(second);
            Assert.IsInstanceOf<System.InvalidOperationException>(TaskUtil.FailureOf(second));

            yield return TaskUtil.Await(first);
            Assert.AreEqual(ScreenId.Lobby, Managers.UI.CurrentId);
            Assert.IsFalse(Managers.UI.IsBusy);
        }

        /// <summary>New run, the first three mercenaries in their recommended rows, depart and enter the first node.</summary>
        static IEnumerator EnterFirstBattle()
        {
            StaticData data = Managers.Data.Data;
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            for (int i = 0; i < data.Balance.PartySize; i++)
            {
                BattleRow row = data.Jobs.Get(Managers.Run.Run.Roster[i].JobId).RecommendedRow;
                UiTestUtil.Click(row == BattleRow.Front ? entries[i].Front : entries[i].Rear);
            }

            UiTestUtil.Click(lobby, "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
        }
    }
}
