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

        /// <summary>From the title: [새 런], then the intro (Slice D stage 55) skipped at once, on to the lobby.</summary>
        public static IEnumerator NewRunToLobby()
        {
            Click(Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return PassTheIntro();
        }

        /// <summary>The intro a new run plays (Slice D stage 55): skipped (Esc) once it shows, then the lobby.</summary>
        public static IEnumerator PassTheIntro()
        {
            yield return WaitForScreen(ScreenId.Intro);
            Screen<IntroScreen>().Skip();
            yield return WaitForScreen(ScreenId.Lobby);
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

        /// <summary>
        /// Clicks a button. Fails when a player could not click it: hidden, disabled or covered by something else. While something is
        /// held (round 54) the press lands on the layer that carries it, which lets a button that works on what is held through and
        /// otherwise lets go of what is held: the click goes that way, as a player's would.
        /// </summary>
        public static void Click(Button button)
        {
            Assert.IsTrue(button.gameObject.activeInHierarchy, $"Button '{button.name}' is not shown.");
            Assert.IsTrue(button.interactable, $"Button '{button.name}' is disabled.");
            PointerPress.Simulate();
            if (HeldLayerOver((RectTransform)button.transform, out HeldPointerView held, out Vector2 middle))
            {
                held.PressAt(middle);
                return;
            }

            AssertPointerReaches(button);
            button.onClick.Invoke();
            PointerAt((RectTransform)button.transform);
        }

        /// <summary>
        /// Where the pointer is after a press or a move over a rect (round 54): what is held rides it, so it is drawn there, as for a player
        /// whose pointer is there (no mouse moves in a test run).
        /// </summary>
        public static void PointerAt(RectTransform rect)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            foreach (HeldPointerView held in Object.FindObjectsByType<HeldPointerView>(FindObjectsSortMode.None))
            {
                if (held.Shown)
                {
                    held.PlaceAt((corners[0] + corners[2]) * 0.5f);
                }
            }
        }

        /// <summary>Whether the layer that carries what is held lies over the middle of a rect (round 54), and where that middle is on the screen.</summary>
        public static bool HeldLayerOver(RectTransform rect, out HeldPointerView held, out Vector2 middle)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            middle = (corners[0] + corners[2]) * 0.5f;
            Transform top = TopmostUnder(rect);
            held = top != null ? top.GetComponent<HeldPointerView>() : null;
            return held != null;
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
        public static Transform TopmostUnder(RectTransform rect, bool skipHeldLayer = false)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            var pointer = new PointerEventData(EventSystem.current) { position = (corners[0] + corners[2]) * 0.5f };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            if (skipHeldLayer)
            {
                hits.RemoveAll(hit => hit.gameObject.GetComponent<HeldPointerView>() != null);
            }

            return hits.Count == 0 ? null : hits[0].gameObject.transform;
        }

        public static void Click(Component root, string path)
        {
            Click(ButtonAt(root, path));
        }

        /// <summary>
        /// Clicks a square of a board's grid (Slice B stage 19) the way a player does: the pointer comes over it (what is held shows its
        /// ghost there), then the click. Fails when a click there would land on something other than the square (the pieces and the
        /// ghost take no pointer, so a click on an item is a click on one of its squares).
        /// </summary>
        public static void ClickSquare(PartyBoardView board, int x, int y)
        {
            GridSquareView square = SquareReached(board, x, y);
            PointerPress.Simulate();
            square.Hover();
            square.Click();
            PointerAt((RectTransform)square.transform);
        }

        /// <summary>A right click on a square of a board's grid: the card of the item there, or a turn of what is held.</summary>
        public static void RightClickSquare(PartyBoardView board, int x, int y)
        {
            GridSquareView square = SquareReached(board, x, y);
            PointerPress.Simulate();
            square.RightClickNow();
        }

        /// <summary>The pointer comes over a square of a board's grid: what is held shows its ghost there.</summary>
        public static void HoverSquare(PartyBoardView board, int x, int y)
        {
            GridSquareView square = SquareReached(board, x, y);
            square.Hover();
            PointerAt((RectTransform)square.transform);
        }

        /// <summary>
        /// A click on a good of the merchant (stage 21) at its top-left square, the pointer over it first. While something is held the
        /// press lands on the layer that carries it (a good is no place for it: it goes back), as a player's would.
        /// </summary>
        public static void ClickGood(MerchantGridView merchant, int slot)
        {
            GridSquareView square = merchant.SquareOf(slot);
            Assert.IsNotNull(square, $"Slot {slot} is not on the merchant's grid.");
            PointerPress.Simulate();
            if (HeldLayerOver((RectTransform)square.transform, out HeldPointerView held, out Vector2 middle))
            {
                held.PressAt(middle);
                return;
            }

            square.Hover();
            square.Click();
            PointerAt((RectTransform)square.transform);
        }

        /// <summary>The pointer comes over a good of the merchant at its top-left square (round 57: its tooltip shows over it).</summary>
        public static void HoverGood(MerchantGridView merchant, int slot)
        {
            GridSquareView square = merchant.SquareOf(slot);
            Assert.IsNotNull(square, $"Slot {slot} is not on the merchant's grid.");
            square.Hover();
            PointerAt((RectTransform)square.transform);
        }

        /// <summary>The pointer leaves a good of the merchant (its top-left square).</summary>
        public static void LeaveGood(MerchantGridView merchant, int slot)
        {
            merchant.SquareOf(slot).OnPointerExit(null);
        }

        /// <summary>The pointer comes over a square of the inventory's grid: what is held shows its ghost there and rides the pointer there.</summary>
        public static void HoverSquare(InventoryGridView grid, int x, int y)
        {
            GridSquareView square = grid.SquareAt(x, y);
            square.Hover();
            PointerAt((RectTransform)square.transform);
        }

        /// <summary>A click on a square of the inventory's grid, the pointer over it first.</summary>
        public static void ClickSquare(InventoryGridView grid, int x, int y)
        {
            GridSquareView square = grid.SquareAt(x, y);
            PointerPress.Simulate();
            square.Hover();
            square.Click();
            PointerAt((RectTransform)square.transform);
        }

        static GridSquareView SquareReached(PartyBoardView board, int x, int y)
        {
            GridSquareView square = board.Grid.SquareAt(x, y);
            Assert.IsTrue(square.gameObject.activeInHierarchy, $"Square ({x},{y}) of '{board.name}' is not shown.");
            // While something is held the layer that carries it takes the press and aims at what lies under it (round 54): look under it.
            Transform top = TopmostUnder((RectTransform)square.transform, skipHeldLayer: true);
            Assert.IsNotNull(top, $"A click on square ({x},{y}) of '{board.name}' hits nothing.");
            Assert.IsTrue(top == square.transform || top.IsChildOf(square.transform), $"'{top.name}' covers square ({x},{y}) of '{board.name}'.");
            return square;
        }

        /// <summary>
        /// The row of the nth living member, from the front (0 for the first), whose base weapon lies one row high at the top-left of the
        /// start bag: a board with two empty rows under its weapon to stage on. Since round 51 the valkyrie's battle axe is 3x2, and the
        /// party of these tests stands her in row 1.
        /// </summary>
        public static int FlatRow(int nth = 0, bool melee = false)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            int found = 0;
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                // melee: only a melee base weapon counts (a whetstone's star lights on one; Slice D stage 36 put the priest's staff in row 2).
                BoardItem weapon = expedition.Members.Find(m => m.Alive && m.Row == row)?.Board.ItemAt(0, 0);
                if (weapon != null && weapon.Height == 1 && (!melee || weapon.Item.Item.Melee) && found++ == nth)
                {
                    return row;
                }
            }

            Assert.Fail($"Fewer than {nth + 1} members with a base weapon one row high.");
            return -1;
        }

        /// <summary>Puts an item on a member's board as it is (no rule checked): staging.</summary>
        public static void Put(ExpeditionMember member, EquippedItem item, int x, int y, int turns = 0)
        {
            member.Board.Items.Add(new BoardItem(item, new Placement(x, y, turns)));
        }

        /// <summary>The item lying on a square of a member's board, or null.</summary>
        public static EquippedItem ItemAt(ExpeditionMember member, int x, int y)
        {
            return member.Board.ItemAt(x, y)?.Item;
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
        /// <summary>From the lobby (the town, stage 26) to the expedition screen (round 71 E2) and off to the dungeon it offers first.</summary>
        public static IEnumerator DepartFromLobby()
        {
            Click(Screen<LobbyScreen>(), "Frame/Strip/Depart");
            yield return WaitForScreen(ScreenId.Expedition);
            yield return DepartFromExpedition(Screen<ExpeditionScreen>());
        }

        /// <summary>Sets out from the expedition screen: [출발] opens the party popup (round 75 B), whose own [출발] departs with the party as formed.</summary>
        public static IEnumerator DepartFromExpedition(ExpeditionScreen screen)
        {
            Click(screen, "Frame/Details/Depart");
            yield return WaitForRedraw();
            Click(screen, "Frame/PartyWindow/PartyDepart");
            yield return WaitForScreen(ScreenId.NodeMap);
        }

        /// <summary>
        /// Opens a quarter's window of the town (stage 26) by its building, then waits for a redraw: the window and the shade were just
        /// turned on, and a panel takes clicks (and blocks them) only once the canvas has drawn it. Nothing if that window is open.
        /// </summary>
        public static IEnumerator OpenQuarter(LobbyScreen lobby, LobbyScreen.Quarter quarter)
        {
            if (lobby.OpenQuarter == quarter)
            {
                yield break;
            }

            string building = quarter == LobbyScreen.Quarter.Barracks ? "Frame/Barracks" : quarter == LobbyScreen.Quarter.Inn ? "Frame/Inn" : "Frame/Coach";
            Click(lobby, building);
            yield return WaitForRedraw();
        }

        /// <summary>Opens the barracks window, where the roster's lines and their row buttons are.</summary>
        public static IEnumerator OpenBarracks(LobbyScreen lobby)
        {
            return OpenQuarter(lobby, LobbyScreen.Quarter.Barracks);
        }

        /// <summary>Closes whatever window of the town is open, so the strip and the buildings take clicks again.</summary>
        public static void CloseQuarter(LobbyScreen lobby)
        {
            switch (lobby.OpenQuarter)
            {
                case LobbyScreen.Quarter.Barracks: Click(lobby, "Frame/BarracksWindow/BarracksClose"); break;
                case LobbyScreen.Quarter.Inn: Click(lobby, "Frame/InnWindow/InnClose"); break;
                case LobbyScreen.Quarter.Coach: Click(lobby, "Frame/CoachWindow/CoachClose"); break;
            }
        }

        /// <summary>The roster's lines in the barracks window, which must be open (<see cref="OpenBarracks"/>).</summary>
        public static RosterEntryView[] RosterEntries(LobbyScreen lobby)
        {
            Assert.AreEqual(LobbyScreen.Quarter.Barracks, lobby.OpenQuarter, "The barracks must be open to read its lines.");
            return Views<RosterEntryView>(lobby);
        }

        /// <summary>
        /// Fills the party from the top of the roster (the first mercenary takes row 1, the second row 2 and so on) with the command the party popup
        /// gives (Round 81 took the row buttons out of the barracks: Docs/Architecture/12_UI.md "원정 화면"). The popup itself is tested on its own.
        /// </summary>
        public static IEnumerator FillParty(LobbyScreen lobby)
        {
            int size = Managers.Data.Data.Balance.PartySize;
            for (int i = 0; i < size; i++)
            {
                PlaceInParty(lobby, Managers.Run.Run.Roster[i].Id, i + 1);
            }

            yield return null;
        }

        /// <summary>Puts a mercenary in a row of the lobby party (the party popup's command) and redraws the lobby.</summary>
        public static void PlaceInParty(LobbyScreen lobby, string mercenaryId, int row)
        {
            Assert.IsTrue(Managers.Run.CanPlaceInParty(mercenaryId, row), $"'{mercenaryId}' cannot take row {row}.");
            Managers.Run.PlaceInParty(mercenaryId, row);
            lobby.Refresh();
        }

        /// <summary>Rests a day at the inn of the town and closes its window.</summary>
        public static IEnumerator RestAtTheInn(LobbyScreen lobby)
        {
            yield return OpenQuarter(lobby, LobbyScreen.Quarter.Inn);
            Click(lobby, "Frame/InnWindow/Rest");
            CloseQuarter(lobby);
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
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());
            yield return DepartFromLobby();

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
        /// From the title: a new run with the final dungeon staged as unlocked (the mine's clears written into the run), the party
        /// filled, the abyss picked on the expedition screen, and its boss's battle entered and left paused at its start, as
        /// <see cref="EnterTheBossBattle"/> does for the mine (stage 28: the Abyss Lord and its court).
        /// </summary>
        public static IEnumerator EnterTheAbyssBossBattle(int bossPowerPercent = 999)
        {
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());

            StaticData data = Managers.Data.Data;
            DungeonData abyss = data.FinalDungeon;
            Managers.Run.Run.ClearedDungeons[abyss.UnlockDungeonId] = abyss.UnlockClears;
            Click(Screen<LobbyScreen>(), "Frame/Strip/Depart");
            yield return WaitForScreen(ScreenId.Expedition);
            ExpeditionScreen expedition = Screen<ExpeditionScreen>();
            Click(Views<DungeonMarkerView>(expedition).Single(marker => marker.DungeonId == abyss.Id).Button);
            Assert.AreEqual(abyss.Id, expedition.SelectedDungeonId);
            yield return DepartFromExpedition(expedition);
            Assert.AreEqual(abyss.Id, Managers.Expedition.Expedition.DungeonId);

            yield return FightThroughToTheBoss(bossPowerPercent);
        }

        /// <summary>
        /// Wins every battle on the way with a staged champion, leaves every loot, rests at every camp, and stops paused at the start of
        /// the boss's battle. Before the boss's node the champion's weapon is set to bossPowerPercent (999 keeps the one-blow champion; a
        /// screenshot that wants the boss to last a few blows passes less).
        /// </summary>
        public static IEnumerator FightThroughToTheBoss(int bossPowerPercent = 999)
        {
            StageChampion();

            int guard = 0;
            while (true)
            {
                Assert.Less(++guard, 100, "The boss was not reached.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        if (ExpeditionRules.AvailableNodes(Managers.Expedition.Expedition).Exists(node => node.Kind == MapNodeKind.Boss))
                        {
                            Regrade(Managers.Expedition.Expedition.Members.Single(m => m.Row == BattleRows.Front), bossPowerPercent);
                        }

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
            Regrade(champion, 999);
            champion.MaxHp = EndlessHp;
            champion.Hp = champion.MaxHp;
        }

        /// <summary>
        /// The member's first item (the job weapon) carried at another power, where it lies: 1 for a weapon too weak to end a battle, 999
        /// for one that ends every battle (Slice B stage 22: items have no grade; the power percent stands in for it in tests).
        /// </summary>
        public static void Regrade(ExpeditionMember member, int powerPercent)
        {
            BoardItem weapon = member.Board.Items[0];
            weapon.Item = new EquippedItem(weapon.Item.Item, weapon.Item.IsBase, weapon.Item.Tier, powerPercent);
        }

        /// <summary>HP nobody brings down in a battle, in hundredths (stage 22).</summary>
        public const int EndlessHp = 100000 * Centi.One;

        /// <summary>
        /// From the title: a new run, a full party, the first node, and its battle advanced (and
        /// left paused) to the moment a mercenary is at death's door. The party is staged so that
        /// this does not hang on the luck of the encounter: everyone has the least HP (0.01), and weapons too
        /// weak to end the battle first.
        /// </summary>
        public static IEnumerator ReachDeathsDoorInTheFirstBattle()
        {
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());
            yield return DepartFromLobby();

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Hp = 1;
                Regrade(member, 1);
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
        /// From the title: a new run, a full party, and the first node's battle entered (and left paused), staged so that the
        /// valkyrie breaks down a little way in. A plain hit tires nobody (2026-10-17: FatigueOnHit 0), so the breakdown comes from an
        /// ally's death's door: a decoy stands in row 1 with the least HP, the enemy's first blow puts it at death's door, which
        /// tires its allies by FatigueOnAllyDog; the valkyrie, in row 2 with endless HP, is staged exactly that far under the
        /// breakdown after the entry cost (far enough that nothing in the frames before the clock is paused can bring it).
        /// Her state poses are the ones drawn so far (round 38).
        /// </summary>
        public static IEnumerator ReachABreakdownInTheFirstBattle()
        {
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());
            yield return DepartFromLobby();

            BalanceData balance = Managers.Data.Data.Balance;
            Assume.That(balance.FatigueOnAllyDog, Is.GreaterThan(0), "An ally's death's door raises fatigue.");
            IReadOnlyList<ExpeditionMember> members = Managers.Expedition.Expedition.Members;
            foreach (ExpeditionMember member in members)
            {
                member.Fatigue = 0;
                member.MaxHp = EndlessHp;
                member.Hp = member.MaxHp;
                Regrade(member, 1);
            }

            ExpeditionMember valkyrie = members.Single(m => m.JobId == "viking");
            ExpeditionMember decoy = members.First(m => m != valkyrie);
            int row = BattleRows.Front;
            foreach (ExpeditionMember member in new[] { decoy, valkyrie }.Concat(members.Where(m => m != decoy && m != valkyrie)))
            {
                member.Row = row++;
            }

            decoy.MaxHp = 1;
            decoy.Hp = 1;
            valkyrie.Fatigue = balance.FatigueBreakdown - FatigueRules.BattleEntryCost(balance, valkyrie.Board.InReadingOrder()) - balance.FatigueOnAllyDog;

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
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());
            yield return DepartFromLobby();

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Board.Items.Clear();
                member.MaxHp = EndlessHp;
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
        /// mercenary in row 1 is the one who falls: the party carries no item, so it cannot end the battle, and row 1 has the least
        /// HP (0.01) while everyone else has so much that the enemy cannot bring them down first.
        /// </summary>
        public static IEnumerator EnterAFirstBattleWhereRow1Falls()
        {
            yield return NewRunToLobby();
            yield return FillParty(Screen<LobbyScreen>());
            yield return DepartFromLobby();

            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                member.Board.Items.Clear();
                if (member.Row != BattleRows.Front)
                {
                    member.MaxHp = EndlessHp;
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
