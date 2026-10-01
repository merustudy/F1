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

                        yield return UiTestUtil.WaitForRedraw();
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
            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(UiTestUtil.At(screen, "Frame/Field/PartyRow1"));
            Assert.IsFalse(units[0].Button.interactable, "Allies are not clickable until a potion is armed.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            UiTestUtil.Click(units[0].Button);

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
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(row1, "Party1Card/Party1Name"));
            Assert.IsFalse(row1.Forward.interactable);
            Assert.IsTrue(row1.Back.interactable);
            PartyColumnView last = party.ColumnOfRow(partySize);
            Assert.IsTrue(last.Forward.interactable);
            Assert.IsFalse(last.Back.interactable);

            UiTestUtil.Click(row1.Back);
            yield return null;

            Assert.AreEqual(2, expedition.Members.Single(m => m.MercenaryId == front).Row);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == second).Row);
            Assert.AreEqual(UiText.Mercenary(second), UiTestUtil.TextAt(row1, "Party1Card/Party1Name"), "A column shows whoever stands in its row now.");
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(party.ColumnOfRow(2), "Party2Card/Party2Name"));

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

            // An item that cannot be used in the new row is dimmed and its cooldown does not fill.
            foreach (BattleUnitView view in shown)
            {
                BattleItemView[] items = UiTestUtil.Views<BattleItemView>(view);
                for (int i = 0; i < items.Length; i++)
                {
                    Color expected = view.Unit.Items[i].Active ? UiPalette.Text : UiPalette.TextDim;
                    Assert.AreEqual(expected, items[i].GetComponentInChildren<TMPro.TMP_Text>().color, UiText.Name(view.Unit.Items[i].Equipped.Item.Name));
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
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryShow), UiTestUtil.TextAt(map, "Frame/NodeInfo/InventoryToggle/InventoryToggleLabel"));

            // Win the first battle; its reward goes straight into the inventory.
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            yield return UiTestUtil.FinishBattle();
            RewardScreen reward = UiTestUtil.Screen<RewardScreen>();
            Assert.IsFalse(UiTestUtil.ButtonAt(reward, "Frame/RewardToInventory").interactable, "Nothing is picked yet.");
            int item = expedition.PendingRewards.FindIndex(r => r.Kind == RewardKind.Item);
            string rewardId = expedition.PendingRewards[item].Id;
            UiTestUtil.Click(UiTestUtil.Views<RewardOptionView>(reward)[item].Button);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(reward, "Frame/RewardToInventory");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            map = UiTestUtil.Screen<NodeMapScreen>();
            party = map.GetComponentInChildren<PartySideView>();
            Assert.AreEqual(rewardId, expedition.Inventory.Single().Item.Id);

            // The popup opens from the node panel and lists the item.
            UiTestUtil.Click(map, "Frame/NodeInfo/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.InventoryPanel.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryHide), UiTestUtil.TextAt(map, "Frame/NodeInfo/InventoryToggle/InventoryToggleLabel"));
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
            UiTestUtil.Click(map, "Frame/NodeInfo/InventoryToggle");
            yield return null;
            Assert.IsFalse(party.InventoryPanel.activeSelf);
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
            UiTestUtil.Click(map, "Frame/NodeInfo/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
        }
    }
}
