using System.Collections;
using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace F1.Tests
{
    /// <summary>Drives the screens the way a player does: by clicking buttons and reading texts.</summary>
    internal static class UiTestUtil
    {
        public const float DefaultTimeoutSeconds = 20f;

        /// <summary>Boots the app in a locale and waits for the title screen.</summary>
        public static IEnumerator BootToTitle(string saveRoot, string localeCode)
        {
            BootTestUtil.WriteSettings(saveRoot, localeCode);
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();
            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.AreEqual(ScreenId.Title, Managers.UI.CurrentId);
        }

        public static IEnumerator WaitForScreen(ScreenId id, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (Managers.UI.CurrentId != id || Managers.UI.IsBusy)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail($"Screen {id} was not shown; the screen is {Managers.UI.CurrentId}.");
                }

                yield return null;
            }

            // One more frame so layout groups and localized labels have settled.
            yield return null;
        }

        /// <summary>
        /// Waits until what a screen just showed or hid can be clicked. A panel that was switched on
        /// takes part in pointer raycasts only after the canvas has drawn it once.
        /// </summary>
        public static IEnumerator WaitForRedraw()
        {
            yield return null;
            yield return null;
        }

        public static T Screen<T>()
            where T : UIScreen
        {
            Assert.IsInstanceOf<T>(Managers.UI.Current);
            return (T)Managers.UI.Current;
        }

        public static Transform At(Component root, string path)
        {
            Transform found = root.transform.Find(path);
            Assert.IsNotNull(found, $"'{path}' was not found under {root.name}.");
            return found;
        }

        public static string TextAt(Component root, string path)
        {
            return At(root, path).GetComponent<TMP_Text>().text;
        }

        public static Button ButtonAt(Component root, string path)
        {
            var button = At(root, path).GetComponent<Button>();
            Assert.IsNotNull(button, $"'{path}' is not a button.");
            return button;
        }

        /// <summary>Clicks a button. Fails when a player could not click it: hidden, disabled or covered by something else.</summary>
        public static void Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Button '{button.name}' is not shown.");
            Assert.IsTrue(button.interactable, $"Button '{button.name}' is disabled.");
            AssertPointerReaches(button);
            button.onClick.Invoke();
        }

        static void AssertPointerReaches(Button button)
        {
            Transform top = TopmostUnderPointer(button);
            Assert.IsNotNull(top, $"A click in the middle of button '{button.name}' hits nothing.");
            Assert.IsTrue(top == button.transform || top.IsChildOf(button.transform), $"'{top.name}' covers button '{button.name}'.");
        }

        /// <summary>True when a mouse click in the middle of the button would land on it and not on something above it.</summary>
        public static bool PointerReaches(Button button)
        {
            Transform top = TopmostUnderPointer(button);
            return top != null && (top == button.transform || top.IsChildOf(button.transform));
        }

        static Transform TopmostUnderPointer(Button button)
        {
            return TopmostUnder((RectTransform)button.transform);
        }

        /// <summary>
        /// Casts a pointer ray at the middle of the rect, the way the event system does for a mouse
        /// click, and returns what it lands on first, or null when it hits nothing.
        /// </summary>
        public static Transform TopmostUnder(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var pointer = new PointerEventData(EventSystem.current) { position = (corners[0] + corners[2]) * 0.5f };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            return hits.Count == 0 ? null : hits[0].gameObject.transform;
        }

        public static void Click(Component root, string path)
        {
            Click(ButtonAt(root, path));
        }

        /// <summary>The shown clones of a template, in display order.</summary>
        public static T[] Views<T>(Component root)
            where T : Component
        {
            return root.GetComponentsInChildren<T>(false);
        }

        /// <summary>
        /// Fills the party from the top of the roster list: the first mercenary takes row 1, the
        /// second row 2 and so on. Each click is on the only row a newcomer can take.
        /// </summary>
        public static void FillParty(LobbyScreen lobby)
        {
            RosterEntryView[] entries = Views<RosterEntryView>(lobby);
            int size = Managers.Data.Data.Balance.PartySize;
            for (int i = 0; i < size; i++)
            {
                Click(entries[i].Rows[i]);
            }
        }

        /// <summary>
        /// From the title: a new run, a full party, and through the first dungeon by its screens up
        /// to the boss. The mercenary in row 1 is staged so strong that every battle is won and
        /// nobody falls. The boss battle is left paused at the moment an enemy has died and those
        /// behind it have advanced.
        /// </summary>
        public static IEnumerator ReachAnEnemyAdvanceInTheBossBattle()
        {
            StaticData data = Managers.Data.Data;
            string dungeonId = data.Dungeons.Ordered[0].Id;
            Assert.Greater(data.BossGroupOf(dungeonId).Enemies.Count, 1, "This needs a boss group with someone behind row 1.");

            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            StageChampion();

            int guard = 0;
            while (true)
            {
                Assert.Less(++guard, 50, "The boss was not reached.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        NodeMapScreen map = Screen<NodeMapScreen>();
                        Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
                        Click(map, "Frame/BoardPanel/Enter");
                        yield return WaitForScreen(ScreenId.Battle);
                        break;

                    case GamePhase.Battle:
                        BattleScreen battle = Screen<BattleScreen>();
                        BattleEngine engine = Managers.Expedition.Battle.Engine;
                        battle.Clock.Paused = true;
                        if (Managers.Expedition.Battle.Node.Kind == MapNodeKind.Boss)
                        {
                            while (!engine.Events.Any(e => e.Kind == BattleEventKind.RowsAdvanced && e.A == (int)BattleSide.Enemy))
                            {
                                Assert.AreEqual(BattleResult.Ongoing, engine.Result, "The boss battle ended before an enemy row emptied.");
                                Managers.Expedition.AdvanceBattle(100);
                            }

                            yield return WaitForRedraw();
                            yield break;
                        }

                        while (!Managers.Expedition.Battle.IsFinished)
                        {
                            Managers.Expedition.AdvanceBattle(250);
                        }

                        Assert.AreEqual(BattleResult.Victory, engine.Result);
                        yield return WaitForRedraw();
                        Click(battle, "Frame/ResultPanel/ResultBox/Continue");
                        yield return WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;

                    case GamePhase.Reward:
                        Click(Screen<RewardScreen>(), "Frame/BoardPanel/Skip");
                        yield return WaitForScreen(ScreenId.NodeMap);
                        break;

                    default:
                        Assert.Fail($"Unexpected phase {Managers.Expedition.Phase}.");
                        break;
                }
            }
        }

        /// <summary>
        /// Makes the mercenary in row 1 of the expedition so strong that every battle is won and
        /// nobody falls. Staged directly: winning by play alone is a matter of luck.
        /// </summary>
        public static void StageChampion()
        {
            ExpeditionMember champion = Managers.Expedition.Expedition.Members.Single(m => m.Row == BattleRows.Front);
            champion.Items[0] = new EquippedItem(champion.Items[0].Item, 999);
            champion.MaxHp = 100000;
            champion.Hp = champion.MaxHp;
        }

        /// <summary>
        /// From the title: a new run, a full party, the first node, and its battle advanced (and
        /// left paused) to the moment a mercenary is at death's door. The party is staged so that
        /// this does not hang on the luck of the encounter: everyone has one HP, and weapons too
        /// weak to end the battle first.
        /// </summary>
        public static IEnumerator ReachDeathsDoorInTheFirstBattle()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Hp = 1;
                member.Items[0] = new EquippedItem(member.Items[0].Item, 1);
            }

            NodeMapScreen map = Screen<NodeMapScreen>();
            Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            Click(map, "Frame/BoardPanel/Enter");
            yield return WaitForScreen(ScreenId.Battle);

            Screen<BattleScreen>().Clock.Paused = true;
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            while (!engine.Party.Any(u => u.InDog))
            {
                Assert.AreEqual(BattleResult.Ongoing, engine.Result, "The battle ended before anyone was at death's door.");
                Managers.Expedition.AdvanceBattle(100);
            }

            yield return WaitForRedraw();
        }

        /// <summary>Runs the shown battle to its end without input and returns to the phase after it.</summary>
        public static IEnumerator FinishBattle()
        {
            BattleScreen battle = Screen<BattleScreen>();
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return WaitForRedraw();
            Click(battle, "Frame/ResultPanel/ResultBox/Continue");
            yield return WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
        }

        public static T ViewNamed<T>(Component root, string text)
            where T : Component
        {
            T view = Views<T>(root).FirstOrDefault(v => v.GetComponentsInChildren<TMP_Text>().Any(t => t.text == text));
            Assert.IsNotNull(view, $"No {typeof(T).Name} shows '{text}'.");
            return view;
        }
    }
}
