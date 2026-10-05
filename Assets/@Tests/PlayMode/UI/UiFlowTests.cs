using System.Collections;
using System.Collections.Generic;
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
            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/StageFront/Field/PartyRow1"));
            Assert.IsFalse(units[0].Button.interactable, "Allies are not clickable until a potion is armed.");

            // The marks under the feet show what the engine says: the badge at the bar's end is the skull at death's
            // door, the shield with its number while the unit has one, and nothing else.
            foreach (BattleUnitView view in UiTestUtil.Views<BattleUnitView>(screen))
            {
                BattleUnit unit = view.Unit;
                string badge = unit.InDog ? "deaths_door" : unit.Shield > 0 ? "shield" : null;
                Assert.AreEqual(badge, view.BadgeShown == null ? null : view.BadgeShown.name, unit.Setup.SourceId);
                if (badge == "shield")
                {
                    Assert.AreEqual(unit.Shield.ToString(), UiTestUtil.TextAt(view, "UnitMarks/UnitBadge/UnitBadgeNumber"), unit.Setup.SourceId);
                }
            }

            // The rim around a unit's bar says that a potion can be used on it, and a light lies at its feet. Its board
            // in the panel takes the click too, but only while a potion is armed.
            BattleUnitView enemy = UiTestUtil.Views<BattleUnitView>(screen).First(u => u.Unit.Side == BattleSide.Enemy);
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(screen);
            BattleBoardView board = boards.Single(b => b.Unit == units[0].Unit);
            BattleBoardView enemyBoard = boards.Single(b => b.Unit == enemy.Unit);
            Assert.AreNotEqual(BattleUnitView.Rim.Target, units[0].RimShown);
            Assert.IsFalse(units[0].TargetLit);
            Assert.AreNotEqual(BattleUnitView.Rim.Target, enemy.RimShown);
            Assert.IsFalse(board.Button.interactable, "The board in the panel is not clickable either until a potion is armed.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            Assert.AreEqual(BattleUnitView.Rim.Target, units[0].RimShown, "The potion can be used on this ally.");
            Assert.IsTrue(units[0].TargetLit, "A light lies at its feet.");
            Assert.AreNotEqual(BattleUnitView.Rim.Target, enemy.RimShown, "Not on an enemy.");
            Assert.IsFalse(enemy.TargetLit, "Not on an enemy.");
            Assert.IsTrue(board.Button.interactable, "The board in the panel takes the click too.");
            Assert.IsFalse(enemyBoard.Button.interactable, "An enemy's board never does.");
            UiTestUtil.Click(units[0].Button);
            yield return null;
            Assert.AreNotEqual(BattleUnitView.Rim.Target, units[0].RimShown, "The potion is used: nothing waits for a target.");
            Assert.IsFalse(units[0].TargetLit, "The potion is used: nothing waits for a target.");

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
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(row1.Board.transform, "Party1BoardHead/Party1BoardName"));
            Assert.AreEqual("1", UiTestUtil.TextAt(row1.Board.transform, "Party1BoardHead/Party1BoardStud/Party1BoardRow"), "The head of a row's board says its row.");
            Assert.IsFalse(row1.Forward.interactable);
            Assert.IsTrue(row1.Back.interactable);
            PartyColumnView last = party.ColumnOfRow(partySize);
            Assert.IsTrue(last.Forward.interactable);
            Assert.IsFalse(last.Back.interactable);

            UiTestUtil.Click(row1.Back);
            yield return null;

            Assert.AreEqual(2, expedition.Members.Single(m => m.MercenaryId == front).Row);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == second).Row);
            Assert.AreEqual(UiText.Mercenary(second), UiTestUtil.TextAt(row1.Board.transform, "Party1BoardHead/Party1BoardName"), "A column shows whoever stands in its row now.");
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(party.ColumnOfRow(2).Board.transform, "Party2BoardHead/Party2BoardName"));

            UiTestUtil.Click(party.ColumnOfRow(2).Forward);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == front).Row, "Forward undoes Back.");
        }

        [UnityTest]
        public IEnumerator Battle_WhenARowEmpties_TheUnitsBehindMoveForward_AndTheDeadAreNamed()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachAnEnemyAdvanceInTheBossBattle();
            BattleScreen screen = UiTestUtil.Screen<BattleScreen>();
            yield return UiTestUtil.WaitForTheKillMoment(screen);
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            BattleEvent advanced = engine.Events.First(e => e.Kind == BattleEventKind.RowsAdvanced);
            BattleUnit[] dead = engine.Enemies.Where(u => !u.Alive).ToArray();
            BattleUnit[] living = engine.Enemies.Where(u => u.Alive).ToArray();
            Assert.IsNotEmpty(dead);
            Assert.IsNotEmpty(living);

            // Only the living stand on the stage, one per column, and each stands in the column of the row the engine says.
            var shown = new System.Collections.Generic.List<BattleUnitView>();
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                BattleUnitView[] inColumn = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/StageFront/Field/EnemyRow" + row));
                Assert.LessOrEqual(inColumn.Length, 1, $"Row {row} holds one unit.");
                foreach (BattleUnitView view in inColumn)
                {
                    Assert.AreEqual(row, view.Unit.Row, UiText.Name(view.Unit.Setup.Name));
                    Assert.IsTrue(view.Unit.Alive);
                    shown.Add(view);
                }
            }

            CollectionAssert.AreEquivalent(living, shown.Select(view => view.Unit));
            Assert.AreEqual(1, UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/StageFront/Field/EnemyRow1")).Length, "Someone advanced into row 1.");

            // The party's columns: one per row the party can stand in; the rest of the side's rows are not shown.
            int partyRows = engine.Setup.Balance.PartySize;
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                Assert.AreEqual(row <= partyRows, UiTestUtil.At(screen, "Frame/StageFront/Field/PartyRow" + row).gameObject.activeSelf, $"Party row {row}");
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
                    if (items[i].Icon != null)
                    {
                        Assert.AreEqual(active ? Color.white : UiPalette.IconDim, items[i].GetComponentsInChildren<Image>().Single(image => image.sprite == items[i].Icon).color, name);
                    }
                }
            }

            // While the battle runs there is no log on screen.
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/LogPanel").gameObject.activeSelf);

            // When the battle has ended, the result panel opens the whole log: the dead are named and the advance is a line of it.
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return UiTestUtil.WaitForResult(screen);
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

                // The head of the board says which row the unit stands in and its name; a monster's name is in its own red.
                Assert.AreEqual(unit.Unit.Row.ToString(), UiTestUtil.TextAt(board, "BoardHead/BoardStud/BoardRow"), id);
                Assert.AreEqual(UiText.Name(unit.Unit.Setup.Name), UiTestUtil.TextAt(board, "BoardHead/BoardName"), id);
                Assert.AreEqual(unit.Unit.Side == BattleSide.Enemy ? UiPalette.EnemyName : UiPalette.Text,
                    UiTestUtil.At(board, "BoardHead/BoardName").GetComponent<TMPro.TMP_Text>().color, id);

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
        public IEnumerator Battle_AUnitAtDeathsDoor_ShowsItUnderItsFeet()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachDeathsDoorInTheFirstBattle();

            // The red rim around the bar and the skull at its end, without a number.
            BattleUnitView unit = UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()).Single(u => u.Unit.InDog);
            Assert.AreEqual(BattleUnitView.Rim.Danger, unit.RimShown);
            Assert.AreEqual("deaths_door", unit.BadgeShown.name);
            Assert.IsFalse(UiTestUtil.At(unit, "UnitMarks/UnitBadge/UnitBadgeNumber").gameObject.activeSelf);

            // The state line holds the death's door state with its words, and nothing else.
            Assert.IsTrue(UiTestUtil.At(unit, "UnitMarks/UnitStates/UnitStatusChip").gameObject.activeSelf);
            Assert.IsNotEmpty(UiTestUtil.TextAt(unit, "UnitMarks/UnitStates/UnitStatusChip/UnitStatus"));
            Assert.IsFalse(UiTestUtil.At(unit, "UnitMarks/UnitStates/UnitBurnChip").gameObject.activeSelf);

            // Everyone else has neither the rim nor the state.
            foreach (BattleUnitView other in UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()).Where(u => !u.Unit.InDog))
            {
                Assert.AreNotEqual(BattleUnitView.Rim.Danger, other.RimShown);
                Assert.IsFalse(UiTestUtil.At(other, "UnitMarks/UnitStates/UnitStatusChip").gameObject.activeSelf);
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

            // The storm: the candle has burnt down and gone out (how the stage darkens: Battle_StageIsLitByTheCandle...).
            Managers.Expedition.AdvanceBattle(engine.Setup.Balance.StormStartMs - engine.TimeMs + engine.Setup.Balance.StormTickMs);
            yield return null;
            if (!Managers.Expedition.Battle.IsFinished)
            {
                Assert.AreEqual(1f, battle.StormProgress, 0.001f);
                Assert.IsFalse(battle.Candle.Lit, "The candle is out once the storm is here.");
            }
        }

        /// <summary>
        /// The stage is lit by the storm candle alone (2026-10-04 mockup B): the light stands on the flame and goes down
        /// with it as the candle burns, pulls in over the storm's last seconds, and once the storm has put the flame out the
        /// whole stage is as dark as where the light never reached. The battle is staged to last past the storm.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_StageIsLitByTheCandle_UntilTheStormPutsItOut()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterALongFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            BalanceData balance = engine.Setup.Balance;
            CandleView candle = battle.Candle;

            Managers.Expedition.AdvanceBattle(1000 - engine.TimeMs);
            yield return null;
            Assert.IsTrue(candle.Lit);
            Assert.AreEqual(0f, candle.DarknessAtFlame, 0.001f, "The stage is lit around the flame.");
            Assert.Greater(candle.Darkness, 0.5f, "Beyond the light the stage is dark.");
            Assert.AreEqual(1f, candle.LightReach, 0.001f, "The light has its full reach while the storm is far.");
            float early = candle.LightPosition.y;

            // Five seconds before the storm: the candle has burnt down, so the light stands lower, and it has pulled in.
            Managers.Expedition.AdvanceBattle(balance.StormStartMs - 5000 - engine.TimeMs);
            yield return null;
            Assert.AreEqual(BattleResult.Ongoing, engine.Result, "The staged battle lasts until the storm.");
            Assert.IsTrue(candle.Lit);
            Assert.Less(candle.LightPosition.y, early, "The light goes down with the flame.");
            Assert.Less(candle.LightReach, 1f, "The light pulls in as the storm nears.");
            Assert.AreEqual(0f, candle.DarknessAtFlame, 0.001f);

            // The storm puts the flame out; the light fades, and the stage is as dark everywhere as it was beyond the light.
            Managers.Expedition.AdvanceBattle(balance.StormStartMs + balance.StormTickMs - engine.TimeMs);
            yield return new WaitForSeconds(0.6f);
            Assert.AreEqual(BattleResult.Ongoing, engine.Result);
            Assert.IsFalse(candle.Lit, "The storm puts the candle out.");
            Assert.AreEqual(candle.Darkness, candle.DarknessAtFlame, 0.01f, "Once the light has faded the stage is dark everywhere.");
        }

        /// <summary>
        /// An item's cooldown is light (2026-10-04 round 18): a cell is dark until its item charges, the dark withdraws from
        /// the left as far as the item has charged (the candle's gold of the charged part reaches as far, and the front of the
        /// charge shows between them), and the flash of a firing lights the cell while the dark comes back. A cell whose
        /// item does not charge stays dark, and so do the empty cells of a battle board.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_ItemCells_StartDark_AndLightUpFromTheLeftAsTheyCharge()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            BattleEngine engine = Managers.Expedition.Battle.Engine;

            // 0.7 s in, before any item's first firing (every cooldown is longer): each working item is lit as far as it has charged.
            Managers.Expedition.AdvanceBattle(700 - engine.TimeMs);
            yield return null;
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(battle);
            int charging = 0;
            foreach (BattleBoardView board in boards)
            {
                for (int i = 0; i < board.Items.Count; i++)
                {
                    BattleItemState item = board.Unit.Items[i];
                    BattleItemView cell = board.Items[i];
                    string name = item.Equipped.Item.Id;
                    float charge = item.Active ? 1f - (float)(item.NextFireMs - engine.TimeMs) / item.CooldownMs : 0f;
                    Assert.AreEqual(charge, cell.Charge, 0.001f, name);
                    Assert.AreEqual(charge, cell.LitTo, 0.001f, $"The gold of {name} reaches as far as it has charged.");
                    Assert.AreEqual(charge, cell.DarkFrom, 0.001f, $"The dark of {name} begins where the gold ends.");
                    Assert.AreEqual(charge > 0f, cell.ShowsFront, name);
                    Assert.AreEqual(BattleItemView.ChargeShade, cell.Darkness, 0.001f, name);
                    if (charge > 0f)
                    {
                        charging++;
                    }
                }
            }

            Assert.Greater(charging, 0, "Items charge in the first seconds.");

            // Right after a firing the cell is dark all over; at the moment of the next it is fully lit; while its owner is down it stays dark.
            BattleBoardView partyBoard = boards.First(b => b.Unit.Side == BattleSide.Party && b.Items.Count > 0);
            BattleItemState first = partyBoard.Unit.Items[0];
            BattleItemView firstCell = partyBoard.Items[0];
            firstCell.Render(first.NextFireMs - first.CooldownMs, ownerAlive: true);
            Assert.AreEqual(0f, firstCell.DarkFrom, 0.001f, "Nothing charged: the dark covers the whole cell.");
            Assert.AreEqual(0f, firstCell.LitTo, 0.001f);
            firstCell.Render(first.NextFireMs, ownerAlive: true);
            Assert.AreEqual(1f, firstCell.DarkFrom, 0.001f, "Fully charged: the dark is gone.");
            Assert.IsFalse(firstCell.ShowsFront);
            firstCell.Render(engine.TimeMs, ownerAlive: false);
            Assert.AreEqual(0f, firstCell.DarkFrom, 0.001f, "An item that does not charge leaves its cell dark.");
            Assert.IsFalse(firstCell.ShowsFront);

            // A firing: the cell is lit under the flash, and the dark comes back as the flash fades.
            firstCell.Pulse();
            Assert.AreEqual(0f, firstCell.Darkness, 0.001f);
            yield return new WaitForSeconds(0.4f);
            Assert.AreEqual(BattleItemView.ChargeShade, firstCell.Darkness, 0.001f);

            // The empty cells of a battle board are as dark as a cell that has not charged.
            int empty = 0;
            foreach (BattleBoardView board in boards)
            {
                foreach (Transform child in UiTestUtil.At(board, "BoardCells"))
                {
                    if (child.gameObject.activeSelf && child.name.StartsWith("EmptyCellTemplate"))
                    {
                        Image dark = child.Find("EmptyCellDark").GetComponent<Image>();
                        Assert.AreEqual(BattleItemView.ChargeShade, dark.color.a, 0.001f);
                        Assert.AreEqual(UiPalette.ChargeDark.r, dark.color.r, 0.001f);
                        empty++;
                    }
                }
            }

            Assert.Greater(empty, 0, "A mercenary has more cells than items.");
        }

        /// <summary>
        /// A unit's marks stay in its column while the figure lunges or recoils, so its HP and states can be read
        /// while it acts (2026-10-04, round 19: "정보칸은 그대로"); a walk into a new column carries the marks too.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_TheMarksStayInTheColumn_WhileTheFigureLungesOrRecoils()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleUnitView unit = UiTestUtil.Views<BattleUnitView>(battle).First(view => view.Unit.Side == BattleSide.Party);
            yield return UntilStill(unit);
            Vector2 figure = unit.FigureRect.anchoredPosition;
            Vector2 marks = unit.MarksRect.anchoredPosition;

            unit.Lunge(1f);
            yield return UntilMoved(unit, figure);
            Assert.Greater(unit.FigureRect.anchoredPosition.x, figure.x, "The figure lunges at the enemy.");
            Assert.AreEqual(marks, unit.MarksRect.anchoredPosition, "The marks stay where they are during a lunge.");
            yield return UntilStill(unit);

            unit.Recoil(-1f);
            yield return UntilMoved(unit, figure);
            Assert.Less(unit.FigureRect.anchoredPosition.x, figure.x, "The figure is knocked back.");
            Assert.AreEqual(marks, unit.MarksRect.anchoredPosition, "The marks stay where they are during a recoil.");
            yield return UntilStill(unit);

            unit.Walk(-120f);
            yield return null;
            Assert.Less(unit.MarksRect.anchoredPosition.x, marks.x, "A walk into a new column carries the marks.");
            Assert.AreEqual(unit.MarksRect.anchoredPosition.x - marks.x, unit.FigureRect.anchoredPosition.x - figure.x, 0.01f,
                "The figure and the marks walk together.");
            yield return UntilStill(unit);
            Assert.AreEqual(marks, unit.MarksRect.anchoredPosition);
        }

        /// <summary>
        /// A mercenary shows its attack pose while its weapon's lunge plays and its hit pose while a blow's recoil plays, then
        /// its figure again (Docs/Design/10 §5). A pose stands on the figure's floor line: its place is the figure's, three times
        /// as wide and an eighth deeper, with the floor line 112 of 1008 up. A lunge that is not a weapon's shows no pose.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AMercenaryShowsItsAttackPoseWhileItsWeaponLunges_AndItsHitPoseWhileItRecoils()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleUnitView unit = UiTestUtil.Views<BattleUnitView>(battle).First(view => view.Unit.Side == BattleSide.Party);
            string job = Managers.Data.Data.Mercenaries.Get(unit.Unit.Setup.SourceId).JobId;
            Sprite figure = unit.Figure.Art;
            RectTransform art = unit.FigureArt.rectTransform;
            Vector2 size = art.sizeDelta;
            yield return UntilStill(unit);

            unit.Lunge(1f, withPose: true);
            yield return null;
            Assert.AreEqual(job + "_attack", unit.Figure.Art.name, "The weapon's lunge shows the attack pose.");
            Assert.AreEqual(size.x * 3f, art.sizeDelta.x, 0.01f, "The pose's place is three figure places wide.");
            Assert.AreEqual(size.y * 1008f / 896f, art.sizeDelta.y, 0.01f, "and an eighth deeper.");
            Assert.AreEqual(112f / 1008f, art.pivot.y, 0.0001f, "It stands on its floor line, where the figure's feet are.");
            yield return UntilStill(unit);
            yield return UntilFigure(unit);
            Assert.AreSame(figure, unit.Figure.Art, "The figure comes back when the lunge is over.");
            Assert.AreEqual(size, art.sizeDelta);
            Assert.AreEqual(0f, art.pivot.y);

            unit.Recoil(-1f);
            yield return null;
            Assert.AreEqual(job + "_hit", unit.Figure.Art.name, "A blow shows the hit pose.");
            Assert.IsTrue(unit.HasHitPose, "Its red flash is at half strength.");
            yield return UntilStill(unit);
            yield return UntilFigure(unit);
            Assert.AreSame(figure, unit.Figure.Art);

            unit.Lunge(1f);
            yield return null;
            Assert.AreSame(figure, unit.Figure.Art, "Defensive gear, an attack item or anything else lunges without the pose.");
            yield return UntilStill(unit);
        }

        /// <summary>
        /// A monster shows its attack pose while its weapon's lunge plays and its hit pose while a blow's recoil plays, then its
        /// figure again, as a mercenary does (Docs/Design/10 §5): the same pose place on the figure's floor line, the red flash
        /// at half strength.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AnEnemyShowsItsAttackPoseWhileItsWeaponLunges_AndItsHitPoseWhileItRecoils()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleUnitView enemy = UiTestUtil.Views<BattleUnitView>(battle).First(view => view.Unit.Side == BattleSide.Enemy);
            string id = enemy.Unit.Setup.SourceId;
            Sprite figure = enemy.Figure.Art;
            RectTransform art = enemy.FigureArt.rectTransform;
            Vector2 size = art.sizeDelta;
            yield return UntilStill(enemy);

            enemy.Lunge(-1f, withPose: true);
            yield return null;
            Assert.AreEqual(id + "_attack", enemy.Figure.Art.name, "The weapon's lunge shows the attack pose.");
            Assert.AreEqual(size.x * 3f, art.sizeDelta.x, 0.01f, "The pose's place is three figure places wide.");
            Assert.AreEqual(size.y * 1008f / 896f, art.sizeDelta.y, 0.01f, "and an eighth deeper.");
            Assert.AreEqual(112f / 1008f, art.pivot.y, 0.0001f, "It stands on its floor line, where the figure's feet are.");
            yield return UntilStill(enemy);
            yield return UntilFigure(enemy);
            Assert.AreSame(figure, enemy.Figure.Art, "The figure comes back when the lunge is over.");
            Assert.AreEqual(size, art.sizeDelta);

            enemy.Recoil(1f);
            yield return null;
            Assert.AreEqual(id + "_hit", enemy.Figure.Art.name, "A blow shows the hit pose.");
            Assert.IsTrue(enemy.HasHitPose, "Its red flash is at half strength.");
            yield return UntilStill(enemy);
            yield return UntilFigure(enemy);
            Assert.AreSame(figure, enemy.Figure.Art);
        }

        /// <summary>
        /// While a unit attacks, its column is drawn over every other, so that its weapon is not hidden behind the unit in front
        /// of it; when the attack is over the field's order comes back: a row over the rows behind it, the enemy's row 1 over the
        /// party's (Docs/Design/10 §5).
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AnAttackerIsDrawnInFrontOfEveryone_WhileItAttacks()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            RectTransform[] columns = Enumerable.Range(BattleRows.Front, BattleRows.Count)
                .SelectMany(row => new[] { UiTestUtil.At(battle, "Frame/StageFront/Field/PartyRow" + row), UiTestUtil.At(battle, "Frame/StageFront/Field/EnemyRow" + row) })
                .Select(column => (RectTransform)column).ToArray();
            int[] order = columns.Select(column => column.GetSiblingIndex()).ToArray();
            int last = order.Max();
            Assert.AreEqual(last, UiTestUtil.At(battle, "Frame/StageFront/Field/EnemyRow1").GetSiblingIndex(), "The enemy's row 1 is drawn over everyone.");
            Assert.Greater(UiTestUtil.At(battle, "Frame/StageFront/Field/PartyRow1").GetSiblingIndex(), UiTestUtil.At(battle, "Frame/StageFront/Field/PartyRow2").GetSiblingIndex(),
                "A row is drawn over the rows behind it.");

            BattleUnitView[] party = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Party)
                .OrderBy(view => view.Unit.Row).ToArray();
            Assert.Greater(party.Length, 1, "This needs a mercenary behind row 1.");
            BattleUnitView behind = party[1];
            yield return UntilStill(behind);
            behind.Lunge(1f, withPose: true);
            yield return null;
            Assert.IsTrue(behind.Attacking);
            Assert.AreEqual(last, behind.transform.parent.GetSiblingIndex(), "The attacker's column is drawn over everyone.");

            yield return UntilStill(behind);
            float until = Time.time + 1f;
            while (behind.Attacking && Time.time < until)
            {
                yield return null;
            }

            yield return null;
            CollectionAssert.AreEqual(order, columns.Select(column => column.GetSiblingIndex()).ToArray(), "The field's order comes back.");
        }

        /// <summary>A support item makes its owner swell and lights a warm light behind it for a moment (Docs/Design/10 §5).</summary>
        [UnityTest]
        public IEnumerator Battle_ASupportItemPulsesItsOwner_WithALightBehind()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleUnitView unit = UiTestUtil.Views<BattleUnitView>(battle).First(view => view.Unit.Side == BattleSide.Party);
            Image glow = UiTestUtil.At(unit.Figure, unit.Figure.name + "Glow").GetComponent<Image>();
            Assert.IsFalse(glow.enabled, "No light before.");

            unit.Pulse();
            yield return null;
            yield return null;
            Assert.IsTrue(unit.Figure.Pulsing);
            Assert.IsTrue(glow.enabled, "The light shows.");
            Assert.Greater(unit.FigureArt.rectTransform.localScale.x, 1f, "The figure swells.");
            Assert.IsFalse(unit.Figure.ShowsPose, "A support item shows no pose.");

            float until = Time.time + 2f;
            while (unit.Figure.Pulsing && Time.time < until)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(glow.enabled, "The light is gone.");
            Assert.AreEqual(1f, unit.FigureArt.rectTransform.localScale.x, 0.0001f);
        }

        /// <summary>Waits by the clock until the pose has gone back to the figure.</summary>
        static IEnumerator UntilFigure(BattleUnitView unit)
        {
            float until = Time.time + 2f;
            while (unit.Figure.ShowsPose && Time.time < until)
            {
                yield return null;
            }
        }

        static IEnumerator UntilMoved(BattleUnitView unit, Vector2 figure)
        {
            for (int frame = 0; frame < 60 && unit.FigureRect.anchoredPosition == figure; frame++)
            {
                yield return null;
            }
        }

        /// <summary>Waits out the unit's motion by the clock: a batch run's frames are far shorter than a game's.</summary>
        static IEnumerator UntilStill(BattleUnitView unit)
        {
            float until = Time.time + 2f;
            while (unit.Moving && Time.time < until)
            {
                yield return null;
            }

            yield return null;
        }

        /// <summary>
        /// A fallen mercenary turns into a grave at once, without a ghost, and those behind keep the places they are drawn
        /// in (with their badges, and their boards in the panel) until the grave has gone; then they walk into the rows the
        /// engine gave them at once (2026-10-04, round 21: 2안-B). Only the picture waits.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AFallenMercenary_TurnsIntoAGrave_AndThoseBehindWaitUntilItHasGone()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterAFirstBattleWhereRow1Falls();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            battle.Clock.SpeedPercent = 100;
            yield return null;
            BattleUnitView[] views = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Party).ToArray();
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(battle).Where(board => board.Unit.Side == BattleSide.Party).ToArray();
            BattleUnit front = engine.Party.Single(unit => unit.Row == BattleRows.Front);
            var drawnIn = engine.Party.ToDictionary(unit => unit, unit => unit.Row);
            int ghosts = battle.Fx.GhostsShown;

            UiTestUtil.AdvanceUntilFallen(front);
            BattleUnit[] behind = engine.Party.Where(unit => unit.Alive).ToArray();
            Assert.IsNotEmpty(behind);
            foreach (BattleUnit unit in behind)
            {
                Assert.AreEqual(drawnIn[unit] - 1, unit.Row, "The engine moves those behind forward at once.");
            }

            yield return null;
            float fell = Time.time;
            Assert.AreEqual(BattleScreen.GraveHold + BattleScreen.GraveVanish, battle.Fx.GravesLeft, 0.001f, "The grave stands at once, for its time at x1.");
            Assert.AreEqual(ghosts, battle.Fx.GhostsShown, "A mercenary leaves no ghost.");
            Assert.IsFalse(views.Single(view => view.Unit == front).gameObject.activeSelf, "The fallen leaves the stage at once.");
            Assert.IsFalse(boards.Single(board => board.Unit == front).gameObject.activeSelf, "The fallen leaves the panel at once.");

            while (battle.Fx.GravesLeft > 0f)
            {
                Assert.Less(Time.time - fell, 2f, "The grave did not go.");
                foreach (BattleUnit unit in behind)
                {
                    string name = UiText.Name(unit.Setup.Name);
                    BattleUnitView view = views.Single(v => v.Unit == unit);
                    Assert.AreEqual("PartyRow" + drawnIn[unit], view.transform.parent.name, $"{name} keeps its place while the grave stands.");
                    BattleBoardView waiting = boards.Single(b => b.Unit == unit);
                    Assert.AreEqual(drawnIn[unit].ToString(), UiTestUtil.TextAt(waiting, "BoardHead/BoardStud/BoardRow"), name);
                    Assert.AreEqual("PartyBoard" + drawnIn[unit], waiting.transform.parent.name, name);
                }

                yield return null;
            }

            Assert.GreaterOrEqual(Time.time - fell, BattleScreen.GraveHold + BattleScreen.GraveVanish - 0.001f, "Nobody walks before the grave has gone.");
            yield return null;
            foreach (BattleUnit unit in behind)
            {
                string name = UiText.Name(unit.Setup.Name);
                BattleUnitView view = views.Single(v => v.Unit == unit);
                Assert.AreEqual("PartyRow" + unit.Row, view.transform.parent.name, $"{name} walks into its new row when the grave has gone.");
                BattleBoardView moved = boards.Single(b => b.Unit == unit);
                Assert.AreEqual(unit.Row.ToString(), UiTestUtil.TextAt(moved, "BoardHead/BoardStud/BoardRow"), name);
                Assert.AreEqual("PartyBoard" + unit.Row, moved.transform.parent.name, name);
            }
        }

        /// <summary>
        /// The index in the log of the first enemy's death that a unit's item caused (the damage logged right before it came
        /// from a unit, not from burn, the storm or a passive), advancing the battle until there is one; -1 if the battle ends first.
        /// </summary>
        static int AdvanceUntilAnEnemyIsFelled(BattleEngine engine)
        {
            int guard = 0;
            while (true)
            {
                IReadOnlyList<BattleEvent> events = engine.Events;
                for (int i = 1; i < events.Count; i++)
                {
                    BattleEvent e = events[i];
                    BattleEvent before = events[i - 1];
                    if (e.Kind == BattleEventKind.Died && e.Target.Side == BattleSide.Enemy && before.Kind == BattleEventKind.Damaged
                        && before.Target.Equals(e.Target) && !before.Source.IsNone && before.Id != BattleEvent.CauseBurn
                        && before.Id != BattleEvent.CauseStorm && before.Id != BattleEvent.CausePassive)
                    {
                        return i;
                    }
                }

                if (engine.Result != BattleResult.Ongoing || ++guard > 2000)
                {
                    return -1;
                }

                Managers.Expedition.AdvanceBattle(100);
            }
        }

        /// <summary>
        /// An enemy felled by a unit's item is a kill moment (Docs/Design/10 §5): for half a second the battle and every motion
        /// run at a quarter of the speed, the fallen stays on the stage and the enemy keeps its places, the rest of the stage is
        /// in the dark under the columns of the two (the one who struck over everyone), the stage draws in and nothing shakes;
        /// then the fallen goes as a ghost, everything runs at its speed again and the enemy walks on, and the dark and the
        /// zoom go back. The moment runs on real time: here the battle is paused.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AnEnemyFelledByAUnitsItem_IsAKillMoment()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterTheBossBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            var field = (RectTransform)UiTestUtil.At(battle, "Frame/StageFront/Field");
            Vector2 still = field.anchoredPosition;
            int ghosts = battle.Fx.GhostsShown;

            int at = AdvanceUntilAnEnemyIsFelled(engine);
            Assert.GreaterOrEqual(at, 1, "The champion fells an enemy (UiTestUtil.StageChampion).");
            UnitRef fallenRef = engine.Events[at].Target;
            UnitRef strikerRef = engine.Events[at - 1].Source;
            yield return null;

            Assert.IsTrue(battle.KillMomentSlows, "A unit's item felled it: a kill moment.");
            yield return null;
            Assert.AreEqual(BattleScreen.KillSlowPercent, battle.Clock.SlowPercent, "The battle runs slow.");
            Assert.AreEqual(BattleScreen.KillSlowPercent / 100f, Time.timeScale, 1e-4f, "The motions run slow too.");
            Assert.AreEqual(ghosts, battle.Fx.GhostsShown, "The fallen stays on the stage meanwhile.");
            BattleUnitView[] views = UiTestUtil.Views<BattleUnitView>(battle);
            BattleUnitView fallen = views.Single(view => view.Unit.Ref.Equals(fallenRef));
            BattleUnitView striker = views.Single(view => view.Unit.Ref.Equals(strikerRef));
            Assert.IsFalse(fallen.Unit.Alive);
            foreach (BattleUnitView view in views.Where(view => view.Unit.Side == BattleSide.Enemy && view.Unit.Alive))
            {
                Assert.AreNotEqual("EnemyRow" + view.Unit.Row, view.transform.parent.name, "The enemy keeps its places while the fallen stays.");
            }

            int dark = battle.KillDarkLayer.GetSiblingIndex();
            Assert.Greater(fallen.transform.parent.GetSiblingIndex(), dark, "The fallen is lit over the dark.");
            Assert.Greater(striker.transform.parent.GetSiblingIndex(), fallen.transform.parent.GetSiblingIndex(), "The one who struck is over everyone.");
            foreach (BattleUnitView other in views.Where(view => view != fallen && view != striker))
            {
                Assert.Less(other.transform.parent.GetSiblingIndex(), dark, $"{UiText.Name(other.Unit.Setup.Name)} is in the dark.");
            }

            yield return new WaitForSecondsRealtime(0.25f);
            Assert.Greater(battle.KillDarkness, 0.5f, "The rest of the stage is dark.");
            Assert.Greater(battle.StageZoom, 1.05f, "The stage is drawn in.");
            Assert.AreEqual(still, field.anchoredPosition, "Nothing shakes.");
            var darkRect = (RectTransform)battle.KillDarkLayer;
            foreach (BattleUnitView view in views)
            {
                RectTransform figure = view.FigureRect;
                Vector3 middle = figure.TransformPoint(figure.rect.center);
                Assert.IsTrue(darkRect.rect.Contains(darkRect.InverseTransformPoint(middle)), $"The dark lies where {UiText.Name(view.Unit.Setup.Name)} stands.");
            }

            float until = Time.realtimeSinceStartup + 2f;
            while (battle.KillMomentSlows && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(battle.KillMomentSlows, "The slow time is over.");
            Assert.AreEqual(1f, Time.timeScale, "The motions run at their speed again.");
            Assert.AreEqual(100, battle.Clock.SlowPercent);
            Assert.AreEqual(ghosts + 1, battle.Fx.GhostsShown, "The fallen goes as a ghost.");
            Assert.IsFalse(fallen.gameObject.activeSelf);
            Assert.AreEqual(still, field.anchoredPosition, "Nothing shakes when it goes either.");

            yield return UiTestUtil.WaitForTheKillMoment(battle);
            Assert.AreEqual(1f, battle.StageZoom, 1e-4f, "The zoom is back.");
            Assert.AreEqual(0f, battle.KillDarkness, "The dark is gone.");
            foreach (BattleUnitView view in UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Enemy))
            {
                Assert.AreEqual("EnemyRow" + view.Unit.Row, view.transform.parent.name, "The enemy has walked on.");
            }
        }

        /// <summary>At x4 an enemy falls as before, without a kill moment holding the battle up (Docs/Design/10 §5).</summary>
        [UnityTest]
        public IEnumerator Battle_AtX4_AnEnemyFallsWithoutAKillMoment()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterTheBossBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.SpeedPercent = 400;
            int ghosts = battle.Fx.GhostsShown;

            Assert.GreaterOrEqual(AdvanceUntilAnEnemyIsFelled(Managers.Expedition.Battle.Engine), 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.KillMomentShown);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.Greater(battle.Fx.GhostsShown, ghosts, "It fades at once.");
        }

        /// <summary>An enemy the storm kills is no kill moment: nobody struck it (Docs/Design/10 §5).</summary>
        [UnityTest]
        public IEnumerator Battle_AnEnemyTheStormKills_IsNoKillMoment()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterALongFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            int ghosts = battle.Fx.GhostsShown;

            int guard = 0;
            while (engine.Enemies.All(unit => unit.Alive))
            {
                Assert.Less(++guard, 3000, "No enemy fell to the storm.");
                Assert.AreEqual(BattleResult.Ongoing, engine.Result);
                Managers.Expedition.AdvanceBattle(100);
            }

            Assert.IsTrue(engine.Events.Any(e => e.Kind == BattleEventKind.Damaged && e.Id == BattleEvent.CauseStorm && e.Target.Side == BattleSide.Enemy),
                "The storm is what killed it: the party carries nothing.");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.KillMomentShown);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.Greater(battle.Fx.GhostsShown, ghosts, "It fades at once.");
        }

        /// <summary>At a higher speed the grave goes sooner by as much, so that the battle does not run far ahead of what is shown.</summary>
        [UnityTest]
        public IEnumerator Battle_AGraveGoesSooner_AtAHigherSpeed()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterAFirstBattleWhereRow1Falls();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.SpeedPercent = 400;
            yield return null;

            UiTestUtil.AdvanceUntilFallen(Managers.Expedition.Battle.Engine.Party.Single(unit => unit.Row == BattleRows.Front));
            yield return null;
            Assert.AreEqual((BattleScreen.GraveHold + BattleScreen.GraveVanish) / 4f, battle.Fx.GravesLeft, 0.001f);
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
            Transform background = UiTestUtil.At(screen, "Frame/StageBack/Background");
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
