using System.Collections;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace F1.Tests
{
    /// <summary>
    /// The story over the shipped data (Slice D stage 55, Docs/Design/33 §9 and §12, Round 101): a new run plays the intro (the full cut the first
    /// time, the short cut after), it goes on by a click and skips by Esc, the lobby shows the intro's last scene as a banner and then Linette's first
    /// dialogue with her faces, the dialogue is saved as heard, Linette then stands in the town and repeats it, and the title's [인트로 보기] plays the
    /// full cut and comes back.
    /// </summary>
    public sealed class UiStoryTests
    {
        string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = BootTestUtil.UseTemporarySaveRoot();
            LobbyScreen.StoryShown = true;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            LobbyScreen.StoryShown = true;
            yield return BootTestUtil.ShutdownApp();
        }

        [UnityTest]
        public IEnumerator NewRun_PlaysTheIntro_ThenTheBanner_ThenLinettesFirstDialogue_AndSavesItAsHeard()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            Assert.IsFalse(Managers.Setting.IntroSeen);
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Intro);

            IntroScreen intro = UiTestUtil.Screen<IntroScreen>();
            Assert.IsFalse(intro.Timeline.ShortCut, "The first time is the full cut.");
            Assert.AreEqual(14, intro.Timeline.Shots.Count, "Fourteen pictures; the fifteenth scene is the lobby's banner.");
            Assert.AreEqual(128000f, intro.Timeline.Total);
            Assert.AreEqual("intro_mural", intro.CurrentScene);

            // A click brings every line of the scene in, the next click the next scene.
            intro.Next();
            Assert.AreEqual("intro_mural", intro.CurrentScene);
            Assert.AreEqual("젊은 왕자가 괴물을 막았다. 모두가 그를 영웅이라 불렀다.", intro.Lines[1].text);
            intro.Next();
            Assert.AreEqual("intro_brooch", intro.CurrentScene);

            // The king's words inside the captain's line are gold (Round 98: only what is in quotation marks).
            while (intro.CurrentScene != "intro_throne")
            {
                intro.Next();
            }

            StringAssert.Contains("<color=" + UiPalette.StoryGoldHex + ">“구덩이를 쳐라.", intro.Lines[0].text);
            intro.Advance(100f);
            yield return null;

            intro.Skip();
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            Assert.IsTrue(Managers.Setting.IntroSeen);

            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsTrue(lobby.StoryBanner.IsOpen, "The intro's last scene over the lobby.");
            Assert.AreEqual("일식까지 99주", lobby.StoryBanner.Countdown);
            Assert.AreEqual("첫 마을 — 광산 마을", lobby.StoryBanner.Guide);
            Assert.IsFalse(lobby.Dialogue.IsOpen, "The dialogue waits for the banner.");

            lobby.StoryBanner.Advance(5000f);
            Assert.IsFalse(lobby.StoryBanner.IsOpen);
            Assert.IsTrue(lobby.Dialogue.IsOpen, "Linette's first dialogue follows.");
            Assert.AreEqual("리넷", lobby.Dialogue.Speaker);
            Assert.AreEqual("장부의 첫 줄은 광산 마을입니다. 무너진 갱의 광부들이 구덩이에 떨어졌습니다.", lobby.Dialogue.Text);
            Sprite neutral = lobby.Dialogue.Face;
            Assert.IsNotNull(neutral);
            Assert.IsFalse(lobby.TownNpc.gameObject.activeSelf, "Nobody stands in the town before a dialogue is heard.");

            lobby.Dialogue.Next();
            Assert.AreEqual("당신은 갈 수 없지요. 그 손목으로는.", lobby.Dialogue.Text);
            Assert.AreNotEqual(neutral, lobby.Dialogue.Face, "The cold face.");
            lobby.Dialogue.Next();
            Assert.AreEqual(neutral, lobby.Dialogue.Face);
            lobby.Dialogue.Next();
            yield return null;

            Assert.IsFalse(lobby.Dialogue.IsOpen);
            CollectionAssert.AreEqual(new[] { "first_lobby" }, Managers.Run.Run.SeenDialogues);
            Assert.IsFalse(Managers.Run.IsSaveBlocked);
            Assert.IsTrue(System.IO.File.ReadAllText(System.IO.Path.Combine(_saveRoot, F1.Flow.RunManager.FileName)).Contains("first_lobby"));

            // Linette now stands by the gate; pointing at her shows her bubble, a click repeats the dialogue (nothing more to save).
            Assert.IsTrue(lobby.TownNpc.gameObject.activeSelf);
            Assert.AreEqual("리넷", lobby.TownNpc.Name);
            lobby.TownNpc.Hover();
            Assert.IsTrue(lobby.BubbleShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Story.NpcHint), lobby.BubbleDetail);
            lobby.TownNpc.Leave();
            UiTestUtil.Click(lobby.TownNpc.Button);
            Assert.IsTrue(lobby.Dialogue.IsOpen);
            Assert.AreEqual(0, lobby.Dialogue.Index);
            lobby.Dialogue.Close();
            CollectionAssert.AreEqual(new[] { "first_lobby" }, Managers.Run.Run.SeenDialogues);

            // The second new run plays the short cut (74.5 s; #3 and #9 left out).
            UiTestUtil.Click(lobby, "Frame/Header/ToTitle");
            yield return UiTestUtil.WaitForScreen(ScreenId.Title);
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/ConfirmPanel/ConfirmBox/Yes");
            yield return UiTestUtil.WaitForScreen(ScreenId.Intro);
            intro = UiTestUtil.Screen<IntroScreen>();
            Assert.IsTrue(intro.Timeline.ShortCut);
            Assert.AreEqual(74500f, intro.Timeline.Total);
            Assert.IsFalse(intro.Timeline.Shots.Any(s => s.Scene.Id == "intro_throne" || s.Scene.Id == "intro_ledger"));
            intro.Skip();
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsTrue(lobby.StoryBanner.IsOpen);
            lobby.StoryBanner.Close();
            Assert.IsTrue(lobby.Dialogue.IsOpen, "A new run hears Linette again.");
            lobby.Dialogue.Close();
            CollectionAssert.AreEqual(new[] { "first_lobby" }, Managers.Run.Run.SeenDialogues, "Esc closes it as heard.");
        }

        [UnityTest]
        public IEnumerator Title_WatchIntro_PlaysTheFullCut_AndComesBack()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Assert.AreEqual("인트로 보기", UiTestUtil.TextAt(title, "Frame/Settings/Intro/IntroLabel"));

            UiTestUtil.Click(title, "Frame/Settings/Intro");
            yield return UiTestUtil.WaitForScreen(ScreenId.Intro);
            IntroScreen intro = UiTestUtil.Screen<IntroScreen>();
            Assert.IsFalse(intro.Timeline.ShortCut);
            Assert.AreEqual(UiStrings.Get(UiKeys.Story.IntroHint), UiTestUtil.TextAt(intro, "Frame/Hint"));

            intro.Advance(intro.Timeline.Total);
            yield return UiTestUtil.WaitForScreen(ScreenId.Title);
            Assert.IsFalse(Managers.Run.HasRun, "Watching the intro starts no run.");
            Assert.IsTrue(Managers.Setting.IntroSeen);
        }

        [UnityTest]
        public IEnumerator Lobby_WhenTheAbyssOpens_LinetteSaysTheNextPage()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.NewRunToLobby();
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            lobby.StoryBanner.Close();
            lobby.Dialogue.Close();

            DungeonData abyss = Managers.Data.Data.FinalDungeon;
            Managers.Run.Run.ClearedDungeons[abyss.UnlockDungeonId] = abyss.UnlockClears;
            UiTestUtil.Click(lobby, "Frame/Header/ToTitle");
            yield return UiTestUtil.WaitForScreen(ScreenId.Title);
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/Continue");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            lobby = UiTestUtil.Screen<LobbyScreen>();

            Assert.IsFalse(lobby.StoryBanner.IsOpen, "Only after the intro.");
            Assert.IsTrue(lobby.Dialogue.IsOpen);
            Assert.AreEqual("광산 마을의 종이 돌아왔습니다. 장부의 한 줄을 지웁니다.", lobby.Dialogue.Text);
            lobby.Dialogue.Next();
            lobby.Dialogue.Next();
            Assert.AreEqual("겁이 많은 사람이니까요.", lobby.Dialogue.Text);
            lobby.Dialogue.Next();
            CollectionAssert.AreEqual(new[] { "first_lobby", "abyss_unlocked" }, Managers.Run.Run.SeenDialogues);
        }
    }
}
