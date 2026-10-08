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

        /// <summary>
        /// Waits until the battle's result is shown. The last enemy's kill moment is seen first (Docs/Design/10 §5), so the
        /// result comes a moment after the battle has ended.
        /// </summary>
        public static IEnumerator WaitForResult(BattleScreen battle, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (!battle.ResultShown)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("The battle's result was not shown.");
                }

                yield return null;
            }

            yield return WaitForRedraw();
        }

        /// <summary>Waits until no moment is shown on the stage: a kill moment's fallen have gone and the dark and the zoom are back.</summary>
        public static IEnumerator WaitForTheKillMoment(BattleScreen battle, float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (battle.MomentShown)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("The kill moment did not end.");
                }

                yield return null;
            }

            yield return WaitForRedraw();
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
            PointerPress.Simulate();
            button.onClick.Invoke();
        }

        /// <summary>A right click on a thing that shows an item (round 47): its card opens. The press that it is closes a card already up.</summary>
        public static void RightClick(Component thing)
        {
            Assert.IsTrue(thing.gameObject.activeInHierarchy, $"'{thing.name}' is not shown.");
            var rightClick = thing.GetComponent<RightClick>();
            Assert.IsNotNull(rightClick, $"'{thing.name}' takes no right click.");
            PointerPress.Simulate();
            rightClick.Simulate();
        }

        /// <summary>A press on nothing in particular, the way a click on the background is: what closes an item's card (round 42).</summary>
        public static void PressTheBackground()
        {
            PointerPress.Simulate();
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

            yield return EnterTheBossBattle();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            while (!engine.Events.Any(e => e.Kind == BattleEventKind.RowsAdvanced && e.A == (int)BattleSide.Enemy))
            {
                Assert.AreEqual(BattleResult.Ongoing, engine.Result, "The boss battle ended before an enemy row emptied.");
                Managers.Expedition.AdvanceBattle(100);
            }

            yield return WaitForRedraw();
        }

        /// <summary>The camp window on the node map, and its rest and mend cards.</summary>
        public const string CampBox = "Frame/Map/Camp/CampWindow/CampBox";
        public const string CampRest = CampBox + "/CampChoices/Rest";
        public const string CampMend = CampBox + "/CampChoices/Mend";

        /// <summary>The shop's window on the node map (round 44), its refresh and leave buttons, and the cost on the refresh.</summary>
        public const string ShopBox = "Frame/Map/Shop/ShopWindow/ShopBox";
        public const string ShopRefresh = ShopBox + "/ShopRefresh";
        public const string ShopRefreshCost = ShopRefresh + "/ShopRefreshRow/ShopRefreshCost";
        public const string ShopLeave = ShopBox + "/ShopLeave";

        /// <summary>
        /// On the node map, goes into the first node that can be chosen: a battle opens the battle screen; a camp opens its
        /// window over the map, where the party rests, and a shop its window, which the party leaves at once; the map is shown again.
        /// </summary>
        public static IEnumerator GoIntoTheFirstNode()
        {
            NodeMapScreen map = Screen<NodeMapScreen>();
            Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            Click(map, "Frame/BoardPanel/Enter");
            if (Managers.Expedition.Phase != GamePhase.Camp && Managers.Expedition.Phase != GamePhase.Shop)
            {
                yield return WaitForScreen(ScreenId.Battle);
                yield break;
            }

            yield return WaitForRedraw();
            Click(map, Managers.Expedition.Phase == GamePhase.Camp ? CampRest : ShopLeave);
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase);
            yield return WaitForRedraw();
        }

        /// <summary>
        /// From the title: a new run, a full party with a champion in row 1 (<see cref="StageChampion"/>), every battle on the
        /// way won and every loot left, a rest at every camp, and the boss's battle entered and left paused at its start.
        /// </summary>
        public static IEnumerator EnterTheBossBattle()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            StageChampion();

            int guard = 0;
            while (true)
            {
                Assert.Less(++guard, 100, "The boss was not reached.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        yield return GoIntoTheFirstNode();
                        break;

                    case GamePhase.Battle:
                        BattleScreen battle = Screen<BattleScreen>();
                        BattleEngine engine = Managers.Expedition.Battle.Engine;
                        battle.Clock.Paused = true;
                        if (Managers.Expedition.Battle.Node.Kind == MapNodeKind.Boss)
                        {
                            yield return WaitForRedraw();
                            yield break;
                        }

                        while (!Managers.Expedition.Battle.IsFinished)
                        {
                            Managers.Expedition.AdvanceBattle(250);
                        }

                        Assert.AreEqual(BattleResult.Victory, engine.Result);
                        yield return WaitForResult(battle);
                        ContinueAfterBattle(battle);
                        yield return WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;

                    case GamePhase.Loot:
                        // Only after an app closed over the loot: the battle screen in its after-the-win look (round 47).
                        Click(Screen<BattleScreen>(), "Frame/BoardPanel/LootContinue");
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

        /// <summary>
        /// From the title: a new run, a full party, the first node, and its battle advanced (and left paused) to the moment a
        /// mercenary (the valkyrie, in row 1) broke down at the fatigue threshold (round 36). Staged: everyone leaves a few hits short of the threshold
        /// after the entry cost (far enough that the breakdown cannot come in the frames before the clock is paused), with so
        /// much HP and so weak a weapon that the battle lasts until somebody has been hit that often (a hit raises the fatigue;
        /// a kill would bring it down).
        /// </summary>
        public static IEnumerator ReachABreakdownInTheFirstBattle()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            BalanceData balance = Managers.Data.Data.Balance;
            Assume.That(balance.FatigueOnHit, Is.GreaterThan(0), "A hit raises fatigue.");
            IReadOnlyList<ExpeditionMember> members = Managers.Expedition.Expedition.Members;
            foreach (ExpeditionMember member in members)
            {
                member.Fatigue = balance.FatigueBreakdown - FatigueRules.BattleEntryCost(balance, member.Items) - 6 * balance.FatigueOnHit;
                member.MaxHp = 100000;
                member.Hp = member.MaxHp;
                member.Items[0] = new EquippedItem(member.Items[0].Item, 1);
            }

            // The valkyrie stands in row 1, where the hits land: hers are the state poses drawn so far (round 38).
            ExpeditionMember valkyrie = members.Single(m => m.JobId == "valkyrie");
            ExpeditionMember front = members.Single(m => m.Row == BattleRows.Front);
            if (valkyrie != front)
            {
                front.Row = valkyrie.Row;
                valkyrie.Row = BattleRows.Front;
            }

            NodeMapScreen map = Screen<NodeMapScreen>();
            Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            Click(map, "Frame/BoardPanel/Enter");
            yield return WaitForScreen(ScreenId.Battle);
            Screen<BattleScreen>().Clock.Paused = true;
            yield return WaitForRedraw();

            // Small steps with a frame between them, so that the breakdown is still recent when it is drawn, and is drawn in the
            // frame it is reached (the caller sees its banner at its start).
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            Assert.IsFalse(engine.Events.Any(e => e.Kind == BattleEventKind.BrokeDown), "The breakdown came before the clock was paused: stage more hits.");
            int guard = 0;
            while (!engine.Events.Any(e => e.Kind == BattleEventKind.BrokeDown))
            {
                Assert.AreEqual(BattleResult.Ongoing, engine.Result, "The battle ended before anyone broke down.");
                Assert.Less(guard++, 1200, "Nobody broke down within two minutes of battle.");
                Managers.Expedition.AdvanceBattle(100);
                yield return null;
            }

            yield return WaitForRedraw();
        }

        /// <summary>
        /// From the title: a new run, a full party, and the first node's battle entered (and left paused), staged to last
        /// past the storm: the party carries no item, so it cannot end the battle, and has so much HP that the enemy cannot
        /// end it either before the storm has come.
        /// </summary>
        public static IEnumerator EnterALongFirstBattle()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Items.Clear();
                member.MaxHp = 100000;
                member.Hp = member.MaxHp;
            }

            NodeMapScreen map = Screen<NodeMapScreen>();
            Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            Click(map, "Frame/BoardPanel/Enter");
            yield return WaitForScreen(ScreenId.Battle);
            Screen<BattleScreen>().Clock.Paused = true;
        }

        /// <summary>
        /// From the title: a new run, a full party, and the first node's battle entered (and left paused), staged so that the
        /// mercenary in row 1 is the one who falls: the party carries no item, so it cannot end the battle, and row 1 has one
        /// HP while everyone else has so much that the enemy cannot bring them down first.
        /// </summary>
        public static IEnumerator EnterAFirstBattleWhereRow1Falls()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return WaitForScreen(ScreenId.Lobby);
            FillParty(Screen<LobbyScreen>());
            Click(Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return WaitForScreen(ScreenId.NodeMap);

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Items.Clear();
                if (member.Row != BattleRows.Front)
                {
                    member.MaxHp = 100000;
                }

                member.Hp = member.Row == BattleRows.Front ? 1 : member.MaxHp;
            }

            NodeMapScreen map = Screen<NodeMapScreen>();
            Click(Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            Click(map, "Frame/BoardPanel/Enter");
            yield return WaitForScreen(ScreenId.Battle);
            Screen<BattleScreen>().Clock.Paused = true;
        }

        /// <summary>Moves the battle on by 100 ms at a time until the unit has fallen, with no frame between: the screen has not drawn it yet.</summary>
        public static void AdvanceUntilFallen(BattleUnit unit)
        {
            while (unit.Alive)
            {
                Assert.AreEqual(BattleResult.Ongoing, Managers.Expedition.Battle.Engine.Result, "The battle ended before the unit fell.");
                Managers.Expedition.AdvanceBattle(100);
            }
        }

        /// <summary>Runs the shown battle to its end without input and returns to the phase after it.</summary>
        public static IEnumerator FinishBattle()
        {
            BattleScreen battle = Screen<BattleScreen>();
            yield return EndBattle(battle);
            ContinueAfterBattle(battle);
            yield return WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
        }

        /// <summary>Runs the battle to its end and waits for its result: the band after a win (the loot lies there), the window otherwise.</summary>
        public static IEnumerator EndBattle(BattleScreen battle)
        {
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return WaitForResult(battle);
        }

        /// <summary>Continue from the result: after a win from the panel (the loot still lying there is left, round 47), otherwise from the result window.</summary>
        public static void ContinueAfterBattle(BattleScreen battle)
        {
            Click(battle, battle.AfterWin ? "Frame/BoardPanel/LootContinue" : "Frame/ResultPanel/ResultBox/Continue");
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
