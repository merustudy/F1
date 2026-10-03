using System.Collections;
using System.IO;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace F1.Tests
{
    /// <summary>
    /// Continuing after the app was closed, and what the player sees when saving fails
    /// (Docs/Architecture/07_SAVE.md; Docs/Architecture/09_VERTICAL_SLICE.md, Acceptance 7).
    /// </summary>
    public sealed class UiSaveTests
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

        /// <summary>New run and a party of the first mercenaries of the roster, one per row.</summary>
        static IEnumerator NewRunWithParty()
        {
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
        }

        [UnityTest]
        public IEnumerator Continue_AfterTheAppWasClosedInTheLobby_ComesBackToTheSameLobby()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return NewRunWithParty();
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Rest");
            string[] party = Managers.Run.Run.Party.Select(p => p.MercenaryId).ToArray();

            yield return BootTestUtil.RestartApp();

            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Assert.IsTrue(UiTestUtil.At(title, "Frame/Buttons/Continue").gameObject.activeSelf);
            Assert.IsEmpty(UiTestUtil.TextAt(title, "Frame/Notice"));
            UiTestUtil.Click(title, "Frame/Buttons/Continue");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);

            Assert.AreEqual(2, Managers.Run.Run.Day);
            CollectionAssert.AreEqual(party, Managers.Run.Run.Party.Select(p => p.MercenaryId));
        }

        [UnityTest]
        public IEnumerator Continue_AfterTheAppWasClosedMidBattle_ShowsThatBattlePausedAtTheConfirmedTime()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return NewRunWithParty();
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);

            // A potion at 1.2 s is the last thing confirmed; 0.3 s more pass before the app closes.
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            Managers.Expedition.AdvanceBattle(1200 - Managers.Expedition.Battle.Engine.TimeMs);
            Assert.IsTrue(Managers.Expedition.TryUsePotion(0, 0));
            Managers.Expedition.AdvanceBattle(300);
            int nodeId = Managers.Expedition.Battle.Node.Id;

            yield return BootTestUtil.RestartApp();
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/Continue");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);

            battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            Assert.AreEqual(BattleResume.Exact, Managers.Expedition.LastResume);
            Assert.AreEqual(nodeId, Managers.Expedition.Battle.Node.Id);
            Assert.AreEqual(1200, engine.TimeMs);
            Assert.IsNull(engine.Potions[0], "The potion stays used.");
            Assert.IsTrue(battle.Clock.Paused, "A continued battle waits for the player.");

            yield return null;
            yield return null;
            Assert.AreEqual(1200, engine.TimeMs, "It does not move while paused.");

            UiTestUtil.Click(battle, "Frame/Header/Pause");
            float deadline = Time.realtimeSinceStartup + UiTestUtil.DefaultTimeoutSeconds;
            while (engine.TimeMs == 1200 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.Greater(engine.TimeMs, 1200, "It moves on once unpaused.");
        }

        [UnityTest]
        public IEnumerator WhenSavingFails_AnOverlayBlocksTheGame_UntilTheRetrySucceeds()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return NewRunWithParty();
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            Button rest = UiTestUtil.ButtonAt(lobby, "Frame/Expedition/Rest");
            SaveErrorOverlay overlay = Managers.UI.SaveError;
            Assert.IsFalse(overlay.IsShown);
            Assert.IsTrue(UiTestUtil.PointerReaches(rest));

            // The save directory becomes a file: every write fails from now on.
            Directory.Delete(_saveRoot, true);
            File.WriteAllText(_saveRoot, "blocked");
            UiTestUtil.Click(rest);
            yield return UiTestUtil.WaitForRedraw();

            Assert.IsTrue(Managers.Run.IsSaveBlocked);
            Assert.IsTrue(overlay.IsShown);
            Assert.AreEqual(2, Managers.Run.Run.Day, "The rest happened but is not confirmed.");
            Assert.IsFalse(UiTestUtil.PointerReaches(rest), "The overlay takes every click.");

            UiTestUtil.Click(overlay, "Blocker/Box/Retry");
            yield return null;
            Assert.IsTrue(overlay.IsShown, "Still failing.");

            File.Delete(_saveRoot);
            Directory.CreateDirectory(_saveRoot);
            UiTestUtil.Click(overlay, "Blocker/Box/Retry");
            yield return UiTestUtil.WaitForRedraw();

            Assert.IsFalse(Managers.Run.IsSaveBlocked);
            Assert.IsFalse(overlay.IsShown);
            Assert.IsTrue(UiTestUtil.PointerReaches(rest));
            var saved = new SaveManager(_saveRoot).Load<RunSaveData>(RunManager.FileName);
            Assert.AreEqual(2, saved.Value.Run.Day, "The retried save holds the rest.");
        }

        [UnityTest]
        public IEnumerator Title_WhenTheLanguageCannotBeSaved_KeepsTheLanguage_AndSaysSo()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Directory.Delete(_saveRoot, true);
            File.WriteAllText(_saveRoot, "blocked");

            // The language is applied first and put back when the save fails; the notice comes after that.
            UiTestUtil.Click(title, "Frame/Buttons/Language");
            float deadline = Time.realtimeSinceStartup + UiTestUtil.DefaultTimeoutSeconds;
            while (UiTestUtil.TextAt(title, "Frame/Notice").Length == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Title.SettingsNotSaved), UiTestUtil.TextAt(title, "Frame/Notice"));
            Assert.AreEqual("ko-KR", Managers.Setting.LocaleCode);
            Assert.AreEqual("새 런", UiTestUtil.TextAt(title, "Frame/Buttons/NewRun/NewRunLabel"), "The screen is still in the old language.");
        }

        [UnityTest]
        public IEnumerator Title_WhenTheSavedRunCannotBeRead_SaysSo_AndOffersOnlyANewRun()
        {
            Directory.CreateDirectory(_saveRoot);
            File.WriteAllText(Path.Combine(_saveRoot, RunManager.FileName), "{{{ not json");

            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();

            Assert.AreEqual(SaveLoadStatus.Corrupt, Managers.Run.LoadStatus);
            Assert.IsFalse(UiTestUtil.At(title, "Frame/Buttons/Continue").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Title.SaveUnreadable), UiTestUtil.TextAt(title, "Frame/Notice"));

            UiTestUtil.Click(title, "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            Assert.AreEqual(1, Managers.Run.Run.Day);
        }
    }
}
