using System;
using System.Collections;
using System.IO;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace F1.Tests
{
    /// <summary>
    /// Renders every screen to PNG files for a visual check. Not part of the chain: it needs a
    /// graphics device, so it runs only when selected by name (Tools/screenshots.sh).
    /// The files go to F1_SCREENSHOT_DIR, outside the repository.
    /// </summary>
    [Explicit("Needs a graphics device. Run Tools/screenshots.sh.")]
    [Category("Screenshot")]
    public sealed class UiScreenshotTests
    {
        const int Width = 1920;
        const int Height = 1080;

        string _saveRoot;
        string _outputDirectory;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = BootTestUtil.UseTemporarySaveRoot();
            _outputDirectory = Environment.GetEnvironmentVariable("F1_SCREENSHOT_DIR");
            if (string.IsNullOrEmpty(_outputDirectory))
            {
                _outputDirectory = Path.Combine(Application.temporaryCachePath, "F1Screenshots");
            }

            Directory.CreateDirectory(_outputDirectory);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return BootTestUtil.ShutdownApp();
        }

        [UnityTest]
        public IEnumerator EveryScreen_Korean()
        {
            yield return CaptureLap("ko-KR", "ko");
        }

        [UnityTest]
        public IEnumerator EveryScreen_English()
        {
            yield return CaptureLap("en-US", "en");
        }

        /// <summary>The boss battle right after an enemy row emptied: who advanced, who fell and which items stopped.</summary>
        [UnityTest]
        public IEnumerator AfterAnAdvance_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachAnEnemyAdvanceInTheBossBattle();
            yield return Capture("ko_15_battle_after_advance");
        }

        IEnumerator CaptureLap(string localeCode, string prefix)
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, localeCode);
            StaticData data = Managers.Data.Data;
            yield return Capture(prefix + "_01_title");

            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            yield return Capture(prefix + "_02_lobby_empty");

            // knight, spellblade, bishop, archmage from row 1 back: the party the balance was tuned with.
            string[] jobs = { "knight", "spellblade", "bishop", "archmage" };
            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            for (int i = 0; i < Mathf.Min(jobs.Length, data.Balance.PartySize); i++)
            {
                int index = Managers.Run.Run.Roster.FindIndex(m => m.JobId == jobs[i]);
                UiTestUtil.Click(entries[index].Rows[i]);
            }

            yield return Capture(prefix + "_03_lobby_party");

            UiTestUtil.Click(lobby, "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            yield return Capture(prefix + "_04_map");

            UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            Managers.Expedition.AdvanceBattle(7300 - Managers.Expedition.Battle.Engine.TimeMs);
            UiTestUtil.Click(UiTestUtil.Views<PotionSlotView>(battle)[0].Button);
            yield return Capture(prefix + "_05_battle");

            bool capturedReward = false;
            bool capturedBoard = false;
            bool capturedBoss = false;
            int guard = 0;
            while (Managers.Expedition.Phase != GamePhase.Settlement)
            {
                Assert.Less(++guard, 100, "The expedition did not end.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        map = UiTestUtil.Screen<NodeMapScreen>();
                        if (!capturedBoard)
                        {
                            capturedBoard = true;
                            UiTestUtil.Click(UiTestUtil.Views<ItemSlotView>(map)[1].Button);
                            yield return Capture(prefix + "_08_map_item_selected");
                            UiTestUtil.Click(UiTestUtil.Views<ItemSlotView>(map)[1].Button);
                        }

                        UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
                        UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
                        yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
                        break;

                    case GamePhase.Battle:
                        battle = UiTestUtil.Screen<BattleScreen>();
                        battle.Clock.Paused = true;
                        bool boss = Managers.Expedition.Battle.Node.Kind == MapNodeKind.Boss;
                        if (boss && !capturedBoss)
                        {
                            capturedBoss = true;
                            Managers.Expedition.AdvanceBattle(30000);
                            yield return Capture(prefix + "_09_boss_battle");
                        }

                        while (!Managers.Expedition.Battle.IsFinished)
                        {
                            Managers.Expedition.AdvanceBattle(250);
                        }

                        yield return UiTestUtil.WaitForRedraw();
                        if (boss || !capturedReward)
                        {
                            yield return Capture(prefix + (boss ? "_10_boss_result" : "_06_battle_result"));
                        }

                        if (boss)
                        {
                            UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/ShowLog");
                            yield return Capture(prefix + "_16_battle_log");
                            UiTestUtil.Click(battle, "Frame/LogPanel/LogBox/LogClose");
                            yield return UiTestUtil.WaitForRedraw();
                        }

                        UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/Continue");
                        yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;

                    case GamePhase.Reward:
                        RewardScreen reward = UiTestUtil.Screen<RewardScreen>();
                        RewardOptionView[] options = UiTestUtil.Views<RewardOptionView>(reward);
                        int item = Managers.Expedition.Expedition.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
                        UiTestUtil.Click(options[item].Button);
                        if (!capturedReward)
                        {
                            capturedReward = true;
                            yield return Capture(prefix + "_07_reward");
                        }

                        // Put the item in the second slot of the first member.
                        UiTestUtil.Click(UiTestUtil.Views<ItemSlotView>(reward)[1].Button);
                        yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
                        break;
                }
            }

            yield return UiTestUtil.WaitForScreen(ScreenId.Settlement);
            yield return Capture(prefix + "_11_settlement");

            UiTestUtil.Click(UiTestUtil.Screen<SettlementScreen>(), "Frame/Panel/Confirm");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return Capture(prefix + "_12_lobby_after");

            // A failed save: the save directory is made unwritable for one command.
            Directory.Delete(_saveRoot, true);
            File.WriteAllText(_saveRoot, "blocked");
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Rest");
            yield return Capture(prefix + "_14_save_failed");
            File.Delete(_saveRoot);
            Directory.CreateDirectory(_saveRoot);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(Managers.UI.SaveError, "Blocker/Box/Retry");
            Assert.IsFalse(Managers.Run.IsSaveBlocked);

            // The end of a run, staged directly.
            RunState run = Managers.Run.Run;
            run.Fallen.AddRange(run.Roster.Select(m => m.Id));
            run.Roster.Clear();
            run.Party.Clear();
            run.IsOver = true;
            UiTestUtil.Screen<LobbyScreen>().Refresh();
            yield return null;
            yield return Capture(prefix + "_13_run_over");
        }

        /// <summary>Draws the UI through the main camera into a texture and writes it as a PNG.</summary>
        IEnumerator Capture(string name)
        {
            yield return null;

            var camera = Camera.main;
            Assert.IsNotNull(camera, "The Main scene has a camera.");
            Canvas canvas = Managers.UI.Current.GetComponentInParent<Canvas>().rootCanvas;

            var target = new RenderTexture(Width, Height, 24, RenderTextureFormat.ARGB32);
            RenderMode oldMode = canvas.renderMode;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases();
            yield return null;
            Canvas.ForceUpdateCanvases();

            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            image.Apply();
            RenderTexture.active = previous;

            camera.targetTexture = null;
            canvas.renderMode = oldMode;
            canvas.worldCamera = null;

            File.WriteAllBytes(Path.Combine(_outputDirectory, name + ".png"), image.EncodeToPNG());
            UnityEngine.Object.Destroy(image);
            target.Release();
            UnityEngine.Object.Destroy(target);
            yield return null;
        }
    }
}
