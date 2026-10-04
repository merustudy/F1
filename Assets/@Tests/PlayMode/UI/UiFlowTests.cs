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

            // Party: the first mercenaries of the roster, one per row from the front.
            RunState run = Managers.Run.Run;
            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            UiTestUtil.FillParty(lobby);

            Assert.AreEqual(data.Balance.PartySize, run.Party.Count);
            Assert.IsTrue(
                entries[data.Balance.PartySize].Rows.Where(button => button.gameObject.activeSelf).All(button => !button.interactable),
                "The party is full.");

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
                        UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
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

                        yield return UiTestUtil.WaitForRedraw();
                        Assert.IsTrue(UiTestUtil.At(battle, "Frame/ResultPanel").gameObject.activeSelf);
                        UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/Continue");
                        yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;
                    }

                    case GamePhase.Reward:
                    {
                        RewardScreen reward = UiTestUtil.Screen<RewardScreen>();
                        UiTestUtil.Click(reward, "Frame/BoardPanel/Skip");
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
            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/Field/PartyRow1"));
            Assert.IsFalse(units[0].Button.interactable, "Allies are not clickable until a potion is armed.");

            // A unit's plate says whose side it is on, and that a potion can be used on it. Its board
            // in the panel takes the click too, but only while a potion is armed.
            BattleUnitView enemy = UiTestUtil.Views<BattleUnitView>(screen).First(u => u.Unit.Side == BattleSide.Enemy);
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(screen);
            BattleBoardView board = boards.Single(b => b.Unit == units[0].Unit);
            BattleBoardView enemyBoard = boards.Single(b => b.Unit == enemy.Unit);
            Assert.AreEqual("plate_party", units[0].Plate.name);
            Assert.AreEqual("plate_enemy", enemy.Plate.name);
            Assert.IsFalse(board.Button.interactable, "The board in the panel is not clickable either until a potion is armed.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            Assert.AreEqual("plate_target", units[0].Plate.name, "The potion can be used on this ally.");
            Assert.AreEqual("plate_enemy", enemy.Plate.name, "Not on an enemy.");
            Assert.IsTrue(board.Button.interactable, "The board in the panel takes the click too.");
            Assert.IsFalse(enemyBoard.Button.interactable, "An enemy's board never does.");
            UiTestUtil.Click(units[0].Button);
            yield return null;
            Assert.AreEqual("plate_party", units[0].Plate.name, "The potion is used: nothing waits for a target.");

            Assert.AreEqual(1, engine.Inputs.Count);
            Assert.AreEqual(BattleInputKind.UsePotion, engine.Inputs[0].Kind);
            Assert.AreEqual(1200, engine.Inputs[0].TimeMs, "The input carries the battle time it was made at, even while paused.");
            Assert.IsNull(engine.Potions[0]);

            // Retreat is an attempt; whatever the roll, it is recorded.
            UiTestUtil.Click(screen, "Frame/Header/Retreat");
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
            yield return UiTestUtil.WaitForRedraw();

            Assert.IsTrue(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);
            Assert.AreEqual(2, Managers.Run.Run.Day, "Nothing is lost until the player confirms.");

            UiTestUtil.Click(title, "Frame/ConfirmPanel/ConfirmBox/No");
            Assert.IsFalse(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);
            yield return UiTestUtil.WaitForRedraw();

            UiTestUtil.Click(title, "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForRedraw();
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

        [UnityTest]
        public IEnumerator Lobby_RowButtons_JoinTheFirstEmptyRow_TradePlaces_AndRemove()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RunState run = Managers.Run.Run;
            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            string first = UiText.Mercenary(run.Roster[0].Id);
            string second = UiText.Mercenary(run.Roster[1].Id);
            string empty = UiStrings.Get(UiKeys.Lobby.EmptyRow);

            int size = Managers.Data.Data.Balance.PartySize;
            Assert.GreaterOrEqual(size, 2, "The cases below put two mercenaries in the party.");

            // The party lines, one per row the party can stand in.
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                Assert.AreEqual(row <= size, UiTestUtil.At(lobby, $"Frame/Expedition/PartyRow{row}").gameObject.activeSelf, $"Party row {row}");
            }

            string[] RowTexts()
            {
                return Enumerable.Range(1, size).Select(row => UiTestUtil.TextAt(lobby, $"Frame/Expedition/PartyRow{row}/PartyRowNames{row}")).ToArray();
            }

            // The names from row 1 back; the rows after them are empty.
            string[] Lined(params string[] fromFront)
            {
                return Enumerable.Range(0, size).Select(i => i < fromFront.Length ? fromFront[i] : empty).ToArray();
            }

            // The shown row buttons of an entry (one per row the party can stand in), by whether they can be clicked.
            bool[] RowButtons(RosterEntryView entry)
            {
                bool[] shown = entry.Rows.Where(button => button.gameObject.activeSelf).Select(button => button.interactable).ToArray();
                Assert.AreEqual(size, shown.Length, "One button per row the party can stand in.");
                return shown;
            }

            bool[] Lit(params int[] rows)
            {
                return Enumerable.Range(1, size).Select(rows.Contains).ToArray();
            }

            // Nobody is in the party: a newcomer can only take row 1, then the next one row 2.
            CollectionAssert.AreEqual(Lit(1), RowButtons(entries[0]));
            UiTestUtil.Click(entries[0].Rows[0]);
            CollectionAssert.AreEqual(Lit(2), RowButtons(entries[1]));
            UiTestUtil.Click(entries[1].Rows[1]);
            CollectionAssert.AreEqual(Lined(first, second), RowTexts());

            // A member can move only to a row where someone else stands: the two trade places.
            // The button of the row the member stands in stays lit, but clicking it changes nothing.
            CollectionAssert.AreEqual(Lit(1, 2), RowButtons(entries[1]));
            UiTestUtil.Click(entries[1].Rows[1]);
            CollectionAssert.AreEqual(Lined(first, second), RowTexts());
            UiTestUtil.Click(entries[1].Rows[0]);
            CollectionAssert.AreEqual(Lined(second, first), RowTexts());

            // Taking the one in row 1 out: the one behind advances.
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(entries[1].Remove);
            CollectionAssert.AreEqual(Lined(first), RowTexts());
            Assert.AreEqual(1, run.Party.Single().Row);
            Assert.IsFalse(entries[1].Remove.gameObject.activeSelf, "Only party members can be taken out.");
        }

        [UnityTest]
        public IEnumerator PartySide_ForwardAndBack_TradePlacesWithTheNextRow_AndTheColumnsFollowTheRows()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            ExpeditionState expedition = Managers.Expedition.Expedition;
            int partySize = Managers.Data.Data.Balance.PartySize;
            string front = expedition.Members.Single(m => m.Row == 1).MercenaryId;
            string second = expedition.Members.Single(m => m.Row == 2).MercenaryId;

            // One column per row the party can stand in, showing the member of that row.
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                Assert.AreEqual(row <= partySize, party.ColumnOfRow(row).gameObject.activeSelf, $"Row {row}");
            }

            // Row 1 cannot go further forward, the last row not further back.
            PartyColumnView row1 = party.ColumnOfRow(1);
            Assert.AreEqual(front, expedition.Members[row1.Member].MercenaryId);
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(row1, "Party1Info/Party1Plate/Party1Name"));
            Assert.IsFalse(row1.Forward.interactable);
            Assert.IsTrue(row1.Back.interactable);
            PartyColumnView last = party.ColumnOfRow(partySize);
            Assert.IsTrue(last.Forward.interactable);
            Assert.IsFalse(last.Back.interactable);

            UiTestUtil.Click(row1.Back);
            yield return null;

            Assert.AreEqual(2, expedition.Members.Single(m => m.MercenaryId == front).Row);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == second).Row);
            Assert.AreEqual(UiText.Mercenary(second), UiTestUtil.TextAt(row1, "Party1Info/Party1Plate/Party1Name"), "A column shows whoever stands in its row now.");
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(party.ColumnOfRow(2), "Party2Info/Party2Plate/Party2Name"));

            UiTestUtil.Click(party.ColumnOfRow(2).Forward);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == front).Row, "Forward undoes Back.");
        }

        [UnityTest]
        public IEnumerator Battle_WhenARowEmpties_TheCardsBehindMoveForward_AndTheDeadAreNamed()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachAnEnemyAdvanceInTheBossBattle();
            BattleScreen screen = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            BattleEvent advanced = engine.Events.First(e => e.Kind == BattleEventKind.RowsAdvanced);
            BattleUnit[] dead = engine.Enemies.Where(u => !u.Alive).ToArray();
            BattleUnit[] living = engine.Enemies.Where(u => u.Alive).ToArray();
            Assert.IsNotEmpty(dead);
            Assert.IsNotEmpty(living);

            // Only the living have a card, one per column, and each stands in the column of the row the engine says.
            var shown = new System.Collections.Generic.List<BattleUnitView>();
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                BattleUnitView[] inColumn = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/Field/EnemyRow" + row));
                Assert.LessOrEqual(inColumn.Length, 1, $"Row {row} holds one unit.");
                foreach (BattleUnitView view in inColumn)
                {
                    Assert.AreEqual(row, view.Unit.Row, UiText.Name(view.Unit.Setup.Name));
                    Assert.IsTrue(view.Unit.Alive);
                    shown.Add(view);
                }
            }

            CollectionAssert.AreEquivalent(living, shown.Select(view => view.Unit));
            Assert.AreEqual(1, UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/Field/EnemyRow1")).Length, "Someone advanced into row 1.");

            // The party's columns: one per row the party can stand in; the rest of the side's rows are not shown.
            int partyRows = engine.Setup.Balance.PartySize;
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                Assert.AreEqual(row <= partyRows, UiTestUtil.At(screen, "Frame/Field/PartyRow" + row).gameObject.activeSelf, $"Party row {row}");
            }

            // The board panel follows the stage: a living enemy's board is in the panel's column of its row, and the dead have none.
            var shownBoards = new System.Collections.Generic.List<BattleBoardView>();
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                BattleBoardView[] inColumn = UiTestUtil.Views<BattleBoardView>(UiTestUtil.At(screen, "Frame/BoardPanel/EnemyBoard" + row));
                Assert.LessOrEqual(inColumn.Length, 1, $"Column {row} holds one unit's board.");
                foreach (BattleBoardView board in inColumn)
                {
                    Assert.AreEqual(row, board.Unit.Row, UiText.Name(board.Unit.Setup.Name));
                    Assert.IsTrue(board.Unit.Alive);
                    shownBoards.Add(board);
                }
            }

            CollectionAssert.AreEquivalent(living, shownBoards.Select(board => board.Unit));

            // An item that cannot be used in the new row is dimmed (its icon, and the name that stands in for a missing icon) and its cooldown does not fill.
            foreach (BattleBoardView board in shownBoards)
            {
                BattleItemView[] items = UiTestUtil.Views<BattleItemView>(board);
                for (int i = 0; i < items.Length; i++)
                {
                    bool active = board.Unit.Items[i].Active;
                    string name = UiText.Name(board.Unit.Items[i].Equipped.Item.Name);
                    Assert.AreEqual(active ? UiPalette.Text : UiPalette.TextDim, items[i].GetComponentInChildren<TMPro.TMP_Text>().color, name);
                    Assert.AreEqual(active ? Color.white : UiPalette.IconDim, items[i].GetComponentsInChildren<Image>().Single(image => image.sprite == items[i].Icon).color, name);
                }
            }

            // While the battle runs there is no log on screen.
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/LogPanel").gameObject.activeSelf);

            // When the battle has ended, the result panel opens the whole log: the dead are named and the advance is a line of it.
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(BattleResult.Victory, engine.Result);
            UiTestUtil.Click(screen, "Frame/ResultPanel/ResultBox/ShowLog");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(screen, "Frame/LogPanel").gameObject.activeSelf);
            Assert.IsEmpty(UiTestUtil.TextAt(screen, "Frame/LogPanel/LogBox/LogPartyFallen"), "Nobody of the party died.");
            string fallen = UiTestUtil.TextAt(screen, "Frame/LogPanel/LogBox/LogEnemyFallen");
            foreach (BattleUnit unit in engine.Enemies)
            {
                StringAssert.Contains(UiText.Name(unit.Setup.Name), fallen);
            }

            string log = string.Join("\n", UiTestUtil.Views<TMPro.TMP_Text>(UiTestUtil.At(screen, "Frame/LogPanel/LogBox/LogScroll")).Select(t => t.text));
            StringAssert.Contains(UiStrings.Get(UiKeys.Log.EnemyAdvanced, advanced.B), log);
            StringAssert.Contains(UiStrings.Get(UiKeys.Log.Died, UiText.Name(dead[0].Setup.Name)), log);

            UiTestUtil.Click(screen, "Frame/LogPanel/LogBox/LogClose");
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/LogPanel").gameObject.activeSelf);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(screen, "Frame/ResultPanel/ResultBox/Continue");
            yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
        }

        [UnityTest]
        public IEnumerator PartySide_InventoryPopup_TakesARewardAndItemsMoveBetweenTheBoardsAndTheInventory()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            UiTestUtil.StageChampion();
            ExpeditionState expedition = Managers.Expedition.Expedition;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            map.Refresh();
            yield return null;

            // With nothing selected only a cell that holds an item can be picked up; the popup is closed.
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(1);
            Assert.IsTrue(row1.Slots[0].Button.interactable, "The weapon.");
            Assert.IsFalse(row1.Slots[1].Button.interactable, "An empty cell.");
            Assert.IsFalse(party.InventoryPanel.activeSelf);
            Assert.IsFalse(party.ToInventory.interactable);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryShow), UiTestUtil.TextAt(map, "Frame/BoardPanel/InventoryToggle/InventoryToggleLabel"));

            // Win the first battle; its reward goes straight into the inventory.
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            yield return UiTestUtil.FinishBattle();
            RewardScreen reward = UiTestUtil.Screen<RewardScreen>();
            Assert.IsFalse(UiTestUtil.ButtonAt(reward, "Frame/BoardPanel/RewardToInventory").interactable, "Nothing is picked yet.");
            int item = expedition.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            string rewardId = expedition.PendingRewards[item].Id;
            UiTestUtil.Click(UiTestUtil.Views<RewardOptionView>(reward)[item].Button);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(reward, "Frame/BoardPanel/RewardToInventory");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            map = UiTestUtil.Screen<NodeMapScreen>();
            party = map.GetComponentInChildren<PartySideView>();
            Assert.AreEqual(rewardId, expedition.Inventory.Single().Item.Id);

            // The popup opens from the node panel and lists the item.
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.InventoryPanel.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryHide), UiTestUtil.TextAt(map, "Frame/BoardPanel/InventoryToggle/InventoryToggleLabel"));
            InventoryEntryView entry = party.InventoryEntries[0];
            Assert.IsTrue(entry.gameObject.activeSelf);
            Assert.AreEqual(rewardId, entry.Item.Item.Id);
            StaticData data = Managers.Data.Data;
            Assert.AreEqual(
                UiStrings.Get(UiKeys.Board.InventoryTitle, ItemBoard.UsedCells(expedition.Inventory), data.Balance.InventoryCells),
                UiTestUtil.TextAt(map, "Frame/InventoryPanel/InventoryBox/InventoryTitle"),
                "The title counts cells in use out of all the cells.");

            // The entry, then the row-1 member's weapon cell: the item takes the weapon's place and the weapon goes to the inventory.
            row1 = party.ColumnOfRow(1);
            int frontIndex = row1.Member;
            string weapon = expedition.Members[frontIndex].Items[0].Item.Id;
            UiTestUtil.Click(entry.Button);
            yield return null;
            UiTestUtil.Click(row1.Slots[0].Button);
            yield return null;
            Assert.AreEqual(rewardId, expedition.Members[frontIndex].Items[0].Item.Id);
            Assert.AreEqual(weapon, expedition.Inventory.Single().Item.Id);
            Assert.IsTrue(party.InventoryPanel.activeSelf, "The popup stays open.");
            Assert.AreEqual(weapon, party.InventoryEntries[0].Item.Item.Id);

            // The board item, then "to inventory": the board is empty and the inventory holds both.
            UiTestUtil.Click(row1.Slots[0].Button);
            yield return null;
            Assert.IsTrue(party.ToInventory.interactable);
            UiTestUtil.Click(party.ToInventory);
            yield return null;
            Assert.IsEmpty(expedition.Members[frontIndex].Items);
            CollectionAssert.AreEqual(new[] { weapon, rewardId }, expedition.Inventory.Select(i => i.Item.Id));
            Assert.IsFalse(party.ToInventory.interactable);

            // Filled to its cells, the inventory takes nothing more: an empty board still takes an item out of it,
            // but a board item cannot go in ("to inventory" is off) while it can still go to another board.
            while (ExpeditionRules.FreeInventoryCells(data, expedition) > 0)
            {
                expedition.Inventory.Add(new EquippedItem(data.Items.Get(weapon), 1));
            }

            UiTestUtil.Click(party.InventoryEntries[0].Button);
            yield return null;
            UiTestUtil.Click(row1.Slots[0].Button);
            yield return null;
            Assert.AreEqual(weapon, expedition.Members[frontIndex].Items.Single().Item.Id);
            expedition.Inventory.Add(new EquippedItem(data.Items.Get(weapon), 1));
            Assert.AreEqual(0, ExpeditionRules.FreeInventoryCells(data, expedition));
            map.Refresh();
            yield return null;
            Assert.AreEqual(
                UiStrings.Get(UiKeys.Board.InventoryTitle, data.Balance.InventoryCells, data.Balance.InventoryCells),
                UiTestUtil.TextAt(map, "Frame/InventoryPanel/InventoryBox/InventoryTitle"));
            UiTestUtil.Click(row1.Slots[0].Button);
            yield return null;
            Assert.IsFalse(party.ToInventory.interactable, "No room for it in the inventory.");
            Assert.IsTrue(party.ColumnOfRow(2).Slots[1].Button.interactable, "Another board's empty cell still takes it.");
            UiTestUtil.Click(row1.Slots[0].Button);
            yield return null;

            // Closing the popup.
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return null;
            Assert.IsFalse(party.InventoryPanel.activeSelf);
        }

        [UnityTest]
        public IEnumerator Figures_ShowTheArtTheDataNames_OnThePartySideAndInBattle_AndThePlaceholderWithoutArt()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            // The party side: every member stands as the figure of its job, and its board shows
            // the icon of each item with the grade on its badge; an empty cell shows words only.
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            Assert.IsNotEmpty(Managers.Expedition.Expedition.Members);
            foreach (ExpeditionMember member in Managers.Expedition.Expedition.Members)
            {
                PartyColumnView column = party.ColumnOfRow(member.Row);
                string jobFigure = data.Jobs.Get(member.JobId).Figure;
                AssertShows(column.Figure, jobFigure, member.MercenaryId);

                // The row's board in the panel is shown with the column.
                Assert.IsTrue(column.Board.activeSelf, member.MercenaryId);
                Assert.IsNotNull(jobFigure, "The jobs of the game have figures.");

                Assert.IsNotEmpty(member.Items, member.MercenaryId);
                Assert.Greater(member.ItemSlots, member.Items.Count, member.MercenaryId);

                // The board is as high as the member's cells stacked, and the bag behind the cells is stretched over it.
                Assert.AreEqual(BattleItemView.BoardHeight(member.ItemSlots), column.BoardHeight, 0.01f, member.MercenaryId);
                Assert.IsTrue(UiTestUtil.At(column.Board.transform, "Party" + member.Row + "Cells/Party" + member.Row + "Bag").GetComponent<Image>().enabled, member.MercenaryId);
                for (int i = 0; i < member.Items.Count; i++)
                {
                    EquippedItem item = member.Items[i];
                    ItemSlotView slot = column.Slots[i];
                    Assert.AreSame(item, slot.Item, item.Item.Id);
                    Assert.IsNotNull(slot.Icon, item.Item.Id);
                    Assert.AreEqual(item.Item.Id, slot.Icon.name, item.Item.Id);
                    Assert.AreEqual(item.Grade.ToString(), slot.Grade, item.Item.Id);
                    Assert.IsFalse(slot.GetComponentsInChildren<TMPro.TMP_Text>().First(text => text.name.EndsWith("Text")).enabled, $"The words of {item.Item.Id} are hidden behind its icon.");
                }

                ItemSlotView empty = column.Slots[member.Items.Count];
                Assert.IsNull(empty.Item, member.MercenaryId);
                Assert.IsNull(empty.Icon, member.MercenaryId);
                Assert.IsEmpty(empty.Grade, member.MercenaryId);
                Assert.AreEqual(UiStrings.Get(UiKeys.Board.EmptySlot), empty.GetComponentsInChildren<TMPro.TMP_Text>().First(text => text.name.EndsWith("Text")).text, member.MercenaryId);
            }

            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);

            // The battle: a mercenary is its job's figure, an enemy its own.
            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>());
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(UiTestUtil.Screen<BattleScreen>());
            Assert.IsTrue(units.Any(u => u.Unit.Side == BattleSide.Party) && units.Any(u => u.Unit.Side == BattleSide.Enemy));
            Assert.AreEqual(units.Length, boards.Length, "Every unit on the stage has its line in the board panel.");
            foreach (BattleUnitView unit in units)
            {
                string id = unit.Unit.Setup.SourceId;
                string address = unit.Unit.Side == BattleSide.Party
                    ? data.Jobs.Get(data.Mercenaries.Get(id).JobId).Figure
                    : data.Enemies.Get(id).Figure;
                AssertShows(unit.Figure, address, id);

                // The badge on the plate says which row the unit stands in.
                Assert.AreEqual(unit.Unit.Row.ToString(), UiTestUtil.TextAt(unit, "UnitPlate/UnitBadge/UnitRow"), id);

                // A click on the figure's place counts for its unit.
                var place = (RectTransform)unit.Figure.transform;
                Transform hit = UiTestUtil.TopmostUnder(place);
                Assert.IsTrue(hit != null && hit.IsChildOf(unit.transform), $"A click on the figure of {id} lands on '{(hit == null ? "nothing" : hit.name)}'.");

                // The art keeps its own size on the canvas every figure shares, so it is wider than its column.
                if (address != null)
                {
                    RectTransform art = unit.Figure.GetComponentsInChildren<Image>(true).Single(i => i.sprite == unit.Figure.Art).rectTransform;
                    Assert.AreEqual(place.rect.height, art.rect.height, 0.01f, id);
                    Assert.AreEqual(art.rect.height * 3f / 4f, art.rect.width, 0.01f, id);
                    Assert.Greater(art.rect.width, place.rect.width, id);
                }

                // The unit's board of the panel is in the column of its row, under its stage column.
                BattleBoardView board = boards.Single(b => b.Unit == unit.Unit);
                string side = unit.Unit.Side == BattleSide.Party ? "Party" : "Enemy";
                Assert.AreEqual(side + "Board" + unit.Unit.Row, board.transform.parent.name, id);

                // The board is as high as the unit's cells stacked, with the bag behind them; each cell shows the icon
                // its item's data names instead of the name; an enemy's icons are mirrored.
                Assert.AreEqual(BattleItemView.BoardHeight(unit.Unit.Setup.ItemSlots), board.BoardHeight, 0.01f, id);
                Assert.IsTrue(UiTestUtil.At(board, "BoardCells/BoardBag").GetComponent<Image>().enabled, id);
                BattleItemView[] cells = UiTestUtil.Views<BattleItemView>(board);
                Assert.AreEqual(unit.Unit.Items.Count, cells.Length, id);
                for (int i = 0; i < cells.Length; i++)
                {
                    ItemData item = unit.Unit.Items[i].Equipped.Item;
                    Assert.IsNotNull(item.Icon, item.Id);
                    Assert.IsNotNull(cells[i].Icon, item.Id);
                    Assert.AreEqual(item.Id, cells[i].Icon.name, item.Id);
                    Assert.IsFalse(cells[i].GetComponentInChildren<TMPro.TMP_Text>().enabled, $"The name of {item.Id} is hidden behind its icon.");
                    Assert.AreEqual(unit.Unit.Side == BattleSide.Enemy, cells[i].Mirrored, item.Id);
                }
            }

            // A unit without art: the placeholder stands in.
            FigureView figure = units[0].Figure;
            figure.Show(null);
            Assert.IsNull(figure.Art);
            Assert.IsTrue(figure.ShowsPlaceholder);

            // An item without art: its name stands in.
            BattleItemView cell = UiTestUtil.Views<BattleItemView>(boards.Single(b => b.Unit == units[0].Unit))[0];
            cell.Bind(units[0].Unit.Items[0], units[0].Unit.Items[0].Equipped.Item.Size, null, mirrored: false);
            Assert.IsNull(cell.Icon);
            Assert.IsTrue(cell.GetComponentInChildren<TMPro.TMP_Text>().enabled);
            Assert.AreEqual(UiText.Name(units[0].Unit.Items[0].Equipped.Item.Name), cell.GetComponentInChildren<TMPro.TMP_Text>().text);
        }

        [UnityTest]
        public IEnumerator Battle_AUnitAtDeathsDoor_ShowsItOnItsPlate()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachDeathsDoorInTheFirstBattle();

            BattleUnitView unit = UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()).Single(u => u.Unit.InDog);
            Assert.AreEqual("plate_danger", unit.Plate.name);

            // The state line holds the death's door state with its words, and nothing else.
            Assert.IsTrue(UiTestUtil.At(unit, "UnitPlate/UnitStates/UnitStatusChip").gameObject.activeSelf);
            Assert.IsNotEmpty(UiTestUtil.TextAt(unit, "UnitPlate/UnitStates/UnitStatusChip/UnitStatus"));
            Assert.IsFalse(UiTestUtil.At(unit, "UnitPlate/UnitStates/UnitShieldChip").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(unit, "UnitPlate/UnitStates/UnitBurnChip").gameObject.activeSelf);

            // Everyone else keeps the plate of their side, without the state.
            foreach (BattleUnitView other in UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()).Where(u => !u.Unit.InDog))
            {
                Assert.AreEqual(other.Unit.Side == BattleSide.Party ? "plate_party" : "plate_enemy", other.Plate.name);
                Assert.IsFalse(UiTestUtil.At(other, "UnitPlate/UnitStates/UnitStatusChip").gameObject.activeSelf);
            }
        }

        /// <summary>
        /// What happens is played: once a hit has landed, a number has risen from the unit, the
        /// caption line says what happened, every event so far has been passed, and the storm's
        /// ring has filled as far as the time has gone.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_PlaysWhatHappens_AsRisingNumbersCaptionsAndTheStormCandle()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            BattleEngine engine = Managers.Expedition.Battle.Engine;

            // Small steps with a frame between them, so that what each step logs is still recent when it is drawn.
            int guard = 0;
            while (!engine.Events.Any(e => e.Kind == BattleEventKind.Damaged) && !Managers.Expedition.Battle.IsFinished && guard++ < 300)
            {
                Managers.Expedition.AdvanceBattle(100);
                yield return null;
            }

            Assert.IsTrue(engine.Events.Any(e => e.Kind == BattleEventKind.Damaged), "Somebody was hit within the first seconds.");
            yield return null;

            Assert.AreEqual(engine.Events.Count, battle.PlayedEvents, "Every event so far was passed.");
            Assert.Greater(battle.Fx.FloatingPlayed, 0, "A hit rose as a number.");
            Assert.IsNotEmpty(battle.Captions, "The hit reads as a caption.");
            Assert.AreEqual(Mathf.Clamp01((float)engine.TimeMs / engine.Setup.Balance.StormStartMs), battle.StormProgress, 0.001f);
            Assert.IsTrue(battle.Candle.Lit, "The candle burns while the storm is still to come.");
            Assert.AreEqual(engine.Events.Count(e => e.Kind == BattleEventKind.StormTicked), 0, "No storm yet.");
            Assert.IsFalse(battle.Fx.DangerShown);

            // The storm: the candle has burnt down and gone out, the stage is dark, and a tick flashes and names its damage.
            Managers.Expedition.AdvanceBattle(engine.Setup.Balance.StormStartMs - engine.TimeMs + engine.Setup.Balance.StormTickMs);
            yield return null;
            if (!Managers.Expedition.Battle.IsFinished)
            {
                Assert.AreEqual(1f, battle.StormProgress, 0.001f);
                Assert.IsFalse(battle.Candle.Lit, "The candle is out once the storm is here.");
                Assert.AreEqual(1f, battle.Fx.StormDarkness, 0.001f);
            }
        }

        [UnityTest]
        public IEnumerator Battle_IsFoughtInFrontOfTheBackgroundTheDungeonNames()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();

            string address = Managers.Data.Data.Dungeons.Get(Managers.Expedition.Expedition.DungeonId).Background;
            Assert.IsNotNull(address, "The dungeon of the game has a background.");
            BattleScreen screen = UiTestUtil.Screen<BattleScreen>();
            Assert.IsNotNull(screen.Background);
            Assert.AreEqual(address.Substring(address.LastIndexOf('/') + 1).Replace('-', '_'), screen.Background.name);

            // It lies behind everything else of the screen and takes no clicks.
            Transform background = UiTestUtil.At(screen, "Frame/Background");
            Assert.AreEqual(0, background.GetSiblingIndex());
            Assert.IsFalse(background.GetComponent<Image>().raycastTarget);
        }

        /// <summary>
        /// The figure shows the art of the address, or the placeholder when the data names none.
        /// A sprite is named after its file, and the file after the data id
        /// ("unit/enemy/goblin-raider" is "goblin_raider").
        /// </summary>
        static void AssertShows(FigureView figure, string address, string who)
        {
            if (address == null)
            {
                Assert.IsNull(figure.Art, who);
                Assert.IsTrue(figure.ShowsPlaceholder, who);
                return;
            }

            Assert.IsNotNull(figure.Art, who);
            Assert.IsFalse(figure.ShowsPlaceholder, who);
            Assert.AreEqual(address.Substring(address.LastIndexOf('/') + 1).Replace('-', '_'), figure.Art.name, who);
        }

        /// <summary>New run, the first mercenaries of the roster one per row, depart and enter the first node.</summary>
        static IEnumerator EnterFirstBattle()
        {
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            UiTestUtil.FillParty(lobby);

            UiTestUtil.Click(lobby, "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
        }
    }
}
