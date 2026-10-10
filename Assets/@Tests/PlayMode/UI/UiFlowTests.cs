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
            Assert.AreEqual("언어: 한국어", UiTestUtil.TextAt(title, "Frame/Settings/Language/LanguageLabel"));
            Assert.IsFalse(UiTestUtil.At(title, "Frame/Buttons/Continue").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(title, "Frame/ConfirmPanel").gameObject.activeSelf);
        }

        [UnityTest]
        public IEnumerator Title_LanguageButton_SwitchesEveryLabelAndIsSaved()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();

            UiTestUtil.Click(title, "Frame/Settings/Language");
            float deadline = Time.realtimeSinceStartup + UiTestUtil.DefaultTimeoutSeconds;
            while (Managers.Setting.LocaleCode != "en-US" && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            yield return null;

            Assert.AreEqual("en-US", Managers.Setting.LocaleCode);
            Assert.AreEqual("New Run", UiTestUtil.TextAt(title, "Frame/Buttons/NewRun/NewRunLabel"));
            Assert.AreEqual("Language: English", UiTestUtil.TextAt(title, "Frame/Settings/Language/LanguageLabel"));
        }

        /// <summary>
        /// The title's music and effect buttons step on, low, off (Docs/Design/12 §3): the setting is saved, the sound follows
        /// it at once, and the next start of the app keeps it.
        /// </summary>
        [UnityTest]
        public IEnumerator Title_MusicAndEffectButtons_StepOnLowOff_AndTheNextStartKeepsThem()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            TitleScreen title = UiTestUtil.Screen<TitleScreen>();
            Assert.AreEqual("음악: 켬", UiTestUtil.TextAt(title, "Frame/Settings/Music/MusicLabel"));
            Assert.AreEqual("효과음: 켬", UiTestUtil.TextAt(title, "Frame/Settings/Effects/EffectsLabel"));

            UiTestUtil.Click(title, "Frame/Settings/Music");
            Assert.AreEqual(VolumeLevels.Low, Managers.Setting.MusicVolume);
            Assert.AreEqual("음악: 작게", UiTestUtil.TextAt(title, "Frame/Settings/Music/MusicLabel"));
            Assert.AreEqual(SoundManager.MusicLevel * 0.3f, Managers.Sound.Output.MusicVolume, 1e-4f, "The music follows the setting at once.");

            UiTestUtil.Click(title, "Frame/Settings/Music");
            UiTestUtil.Click(title, "Frame/Settings/Effects");
            Assert.AreEqual("음악: 끔", UiTestUtil.TextAt(title, "Frame/Settings/Music/MusicLabel"));
            Assert.AreEqual("효과음: 작게", UiTestUtil.TextAt(title, "Frame/Settings/Effects/EffectsLabel"));
            Assert.AreEqual(0f, Managers.Sound.Output.MusicVolume);
            Assert.AreEqual(0.3f, Managers.Sound.Output.EffectVolume, 1e-4f);

            yield return BootTestUtil.RestartApp();

            title = UiTestUtil.Screen<TitleScreen>();
            Assert.AreEqual(VolumeLevels.Off, Managers.Setting.MusicVolume);
            Assert.AreEqual(VolumeLevels.Low, Managers.Setting.EffectVolume);
            Assert.AreEqual("음악: 끔", UiTestUtil.TextAt(title, "Frame/Settings/Music/MusicLabel"));
            Assert.AreEqual(0.3f, Managers.Sound.Output.EffectVolume, 1e-4f, "The sound starts at the saved volume.");
        }

        /// <summary>
        /// The music follows the screens (Docs/Design/12 §2): the lobby's on the title and in the lobby, the dungeon's from the
        /// node map on, each streaming its own clip.
        /// </summary>
        [UnityTest]
        public IEnumerator Music_FollowsTheScreens()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            Assert.IsTrue(Managers.Sound.IsLoaded);
            Assert.AreEqual(MusicTrack.Lobby, Managers.Sound.Track);
            Assert.IsNotNull(Managers.Sound.Output.MusicClip, "The lobby's music plays on the title.");
            AudioClip lobbyMusic = Managers.Sound.Output.MusicClip;

            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            Assert.AreEqual(MusicTrack.Lobby, Managers.Sound.Track);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            CollectionAssert.AreEqual(new[] { SoundEffect.Depart }, Managers.Sound.Asked, "Setting out makes its own sound, not a click.");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            Assert.AreEqual(MusicTrack.Dungeon, Managers.Sound.Track);
            Assert.IsNotNull(Managers.Sound.Output.MusicClip, "The dungeon's music plays.");
            Assert.AreNotSame(lobbyMusic, Managers.Sound.Output.MusicClip, "The music changed with the screen.");
            Assert.AreEqual(AudioClipLoadType.Streaming, Managers.Sound.Output.MusicClip.loadType, "The music streams.");
        }

        /// <summary>
        /// An effect asked for again within a moment plays once; another effect at the same moment plays as well
        /// (Docs/Architecture/14_SOUND.md "재생").
        /// </summary>
        [UnityTest]
        public IEnumerator Sound_TheSameEffectTwiceInAMoment_PlaysOnce()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            Managers.Sound.ForgetEffects();

            Managers.Sound.PlayEffect(SoundEffect.Hit);
            Managers.Sound.PlayEffect(SoundEffect.Hit);
            Managers.Sound.PlayEffect(SoundEffect.Heal);
            CollectionAssert.AreEqual(new[] { SoundEffect.Hit, SoundEffect.Hit, SoundEffect.Heal }, Managers.Sound.Asked);
            CollectionAssert.AreEqual(new[] { SoundEffect.Hit, SoundEffect.Heal }, Managers.Sound.Played);

            yield return new WaitForSecondsRealtime(SoundManager.SameEffectGapSeconds * 2f);
            Managers.Sound.PlayEffect(SoundEffect.Hit);
            CollectionAssert.AreEqual(new[] { SoundEffect.Hit, SoundEffect.Heal, SoundEffect.Hit }, Managers.Sound.Played);
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
                        yield return UiTestUtil.GoIntoTheFirstNode();
                        break;

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

                        yield return UiTestUtil.WaitForResult(battle);
                        Assert.IsTrue(battle.ResultShown);
                        UiTestUtil.ContinueAfterBattle(battle);
                        yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
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
        public IEnumerator Lobby_FatigueShowsAsTenPips_RedFromTheBreakdownOn()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            BalanceData balance = Managers.Data.Data.Balance;
            Assume.That(balance.MaxFatigue, Is.EqualTo(200), "The cases below count pips of 20.");
            Assume.That(balance.FatigueBreakdown, Is.EqualTo(100));

            // A fresh mercenary: ten empty pips, violet words.
            RosterEntryView fresh = UiTestUtil.Views<RosterEntryView>(lobby)[0];
            Assert.AreEqual(10, fresh.FatiguePips.Count);
            Assert.IsTrue(fresh.FatiguePips.All(p => p.Ratio == 0f));
            Assert.AreEqual(UiPalette.Fatigue, fresh.Fatigue.color);

            // 128: five full violet pips, then one full and one 8/20 red one, then nothing; the words red.
            RunRules.FindMercenary(Managers.Run.Run, Managers.Run.Run.Roster[0].Id).Fatigue = 128;
            lobby.Refresh();
            yield return null;
            RosterEntryView tired = UiTestUtil.Views<RosterEntryView>(lobby)[0];
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 1f, 0.4f, 0f, 0f, 0f }, tired.FatiguePips.Select(p => Mathf.Round(p.Ratio * 10f) / 10f));
            Assert.IsTrue(tired.FatiguePips.Take(5).All(p => p.FillColor == UiPalette.FatigueBar));
            Assert.IsTrue(tired.FatiguePips.Skip(5).All(p => p.FillColor == UiPalette.FatigueDanger));
            Assert.AreEqual(UiPalette.FatigueDanger, tired.Fatigue.color);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Fatigue, 128, 200), tired.Fatigue.text);
        }

        /// <summary>A mercenary that came home in an affliction (round 36): its name after the fatigue words, and what it does at the right end of the passive's line.</summary>
        [UnityTest]
        public IEnumerator Lobby_AMercenaryThatCameHomeAfflicted_IsNamedWithItsStateAndWhatItDoes()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            StaticData data = Managers.Data.Data;
            FatigueStateData fearful = data.FatigueStates.Get("fearful");
            MercenaryState tired = Managers.Run.Run.Roster[0];
            tired.Fatigue = 128;
            tired.AfflictionId = fearful.Id;
            lobby.Refresh();
            yield return null;

            RosterEntryView[] entries = UiTestUtil.Views<RosterEntryView>(lobby);
            string words = UiStrings.Get(UiKeys.Lobby.Fatigue, 128, data.Balance.MaxFatigue);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.FatigueState, words, UiText.FatigueStateName(fearful)), entries[0].Fatigue.text);
            Assert.AreEqual(UiPalette.FatigueDanger, entries[0].Fatigue.color);
            Assert.AreEqual(UiText.Name(fearful.Description), entries[0].State.text);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Fatigue, 0, data.Balance.MaxFatigue), entries[1].Fatigue.text, "The others: the words alone.");
            Assert.AreEqual(string.Empty, entries[1].State.text);
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
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(row1.Board.transform, "Party1Board/Party1BoardHead/Party1BoardName"));
            Assert.AreEqual("1", UiTestUtil.TextAt(row1.Board.transform, "Party1Board/Party1BoardHead/Party1BoardStud/Party1BoardRow"), "The head of a row's board says its row.");
            Assert.IsFalse(row1.Forward.interactable);
            Assert.IsTrue(row1.Back.interactable);
            PartyColumnView last = party.ColumnOfRow(partySize);
            Assert.IsTrue(last.Forward.interactable);
            Assert.IsFalse(last.Back.interactable);

            UiTestUtil.Click(row1.Back);
            yield return null;

            Assert.AreEqual(2, expedition.Members.Single(m => m.MercenaryId == front).Row);
            Assert.AreEqual(1, expedition.Members.Single(m => m.MercenaryId == second).Row);
            Assert.AreEqual(UiText.Mercenary(second), UiTestUtil.TextAt(row1.Board.transform, "Party1Board/Party1BoardHead/Party1BoardName"), "A column shows whoever stands in its row now.");
            Assert.AreEqual(UiText.Mercenary(front), UiTestUtil.TextAt(party.ColumnOfRow(2).Board.transform, "Party2Board/Party2BoardHead/Party2BoardName"));

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

            // An item that cannot be used in the new row is grey (its piece and icon, and the name that stands in for a missing icon) and its
            // cooldown does not fill: its icon stays dark.
            foreach (BattleBoardView board in shownBoards)
            {
                BattleItemView[] items = UiTestUtil.Views<BattleItemView>(board);
                for (int i = 0; i < items.Length; i++)
                {
                    bool active = board.Unit.Items[i].Active;
                    string name = UiText.Name(board.Unit.Items[i].Equipped.Item.Name);
                    Assert.AreEqual(active ? UiPalette.Text : UiPalette.TextDim, items[i].GetComponentInChildren<TMPro.TMP_Text>().color, name);
                    Assert.AreEqual(active, items[i].ShowsActive, name);
                    if (!active)
                    {
                        Assert.AreEqual(0f, items[i].LitTo, 0.001f, name);
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
        public IEnumerator PartySide_InventoryPopup_TakesADropAndItemsMoveBetweenTheBoardsAndTheInventory()
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

            // The board as a grid (Slice B stage 19): the start bag's squares, the weapon's piece over the top row, nothing outside the bag.
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            PartyBoardView board1 = row1.BoardView;
            Assert.AreEqual(1, board1.Grid.BagsShown, "The start bag.");
            Assert.AreEqual(GridSquareLook.Hidden, board1.Grid.SquareAt(0, 0).Look, "Under the weapon.");
            Assert.AreEqual(GridSquareLook.Empty, board1.Grid.SquareAt(0, 1).Look, "An empty square of the start bag.");
            Assert.AreEqual(GridSquareLook.Hidden, board1.Grid.SquareAt(0, 3).Look, "Outside every bag: nothing shows.");
            Assert.IsNotNull(board1.PieceAt(0, 0));
            int weaponEnd = UiTestUtil.ItemAt(expedition.Members[row1.Member], 0, 0).Item.Width - 1;
            Assert.AreSame(board1.PieceAt(0, 0), board1.PieceAt(weaponEnd, 0), "One piece over all the weapon's squares.");
            Assert.IsNull(board1.PieceAt(0, 1));
            Assert.IsFalse(party.InventoryPanel.activeSelf);
            Assert.IsFalse(party.ToInventory.interactable);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryShow), UiTestUtil.TextAt(map, "Frame/BoardPanel/InventoryToggle/InventoryToggleLabel"));

            // Win the first battle; on the battle screen after the win (round 47) one of its drops goes straight into the inventory.
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return UiTestUtil.EndBattle(battle);
            Assert.IsTrue(battle.AfterWin);
            Assert.IsFalse(UiTestUtil.ButtonAt(battle, "Frame/BoardPanel/LootToInventory").interactable, "Nothing is picked yet.");
            int item = expedition.Loot.FindIndex(d => d != null);
            string rewardId = expedition.Loot[item].Id;
            int drops = expedition.Loot.Count;
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(battle.Drops[item].Button);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(battle, "Frame/BoardPanel/LootToInventory");
            CollectionAssert.AreEqual(new[] { SoundEffect.Button, SoundEffect.ItemPlace }, Managers.Sound.Asked,
                "Picking the drop clicks; putting the item away sounds as put in, without a click as well.");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.Drops[item].gameObject.activeSelf, "The taken drop is gone from the floor.");
            Assert.AreSame(battle, UiTestUtil.Screen<BattleScreen>(), "The screen stays after a take, whether drops are left or not.");
            if (drops > 1)
            {
                Assert.IsTrue(Managers.Expedition.LootOpen, "The other drops still lie there.");
            }

            UiTestUtil.Click(battle, "Frame/BoardPanel/LootContinue");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            map = UiTestUtil.Screen<NodeMapScreen>();
            party = map.GetComponentInChildren<PartySideView>();
            Assert.AreEqual(rewardId, expedition.Inventory.Single().Item.Id);

            // The popup opens from the node panel and shows the item on its grid (round 49), at its first room.
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.InventoryPanel.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryHide), UiTestUtil.TextAt(map, "Frame/BoardPanel/InventoryToggle/InventoryToggleLabel"));
            InventoryGridView grid = party.InventoryGrid;
            Assert.AreEqual(rewardId, grid.PieceAt(0, 0).Item.Item.Id);
            ItemSlotView rewardPiece = grid.PieceAt(0, 0);
            Assert.AreEqual(UiPalette.GridPiece, rewardPiece.Ground, "The board's blue (round 53).");
            Assert.AreEqual(1f, rewardPiece.Ground.a, "Solid, so the same blue over any ground.");
            Assert.AreEqual(new Vector2Int(rewardPiece.Item.Item.Width, rewardPiece.Item.Item.Height), rewardPiece.GroundSquares,
                "Square by square, the grid's line between them (round 53).");

            // Round 52 ("B안"): the window lies over the mercenaries' stage (the left half), its squares a board's size.
            Assert.AreEqual(960f, party.InventoryWindow.Rect.rect.width, 0.5f);
            Assert.AreEqual(0f, party.InventoryWindow.Rect.anchoredPosition.x, 0.5f);
            Assert.AreEqual(GridGeometry.Square, ((RectTransform)grid.SquareAt(0, 0).transform).rect.width, 0.5f);
            StaticData data = Managers.Data.Data;
            int squares = data.Balance.InventoryWidth * data.Balance.InventoryHeight;
            Assert.AreEqual(
                UiStrings.Get(UiKeys.Board.InventoryTitle, expedition.Inventory.UsedSquares, squares),
                UiTestUtil.TextAt(map, "Frame/InventoryPanel/InventoryBox/InventoryTitle"),
                "The title counts the squares in use out of all the squares.");
            Assert.AreEqual(string.Empty, party.InventoryInfo, "Nothing held, no tooltip.");

            // The entry, then the row-1 member's weapon: the item lies with its top-left there, over the weapon, which goes to the inventory.
            row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            board1 = row1.BoardView;
            int frontIndex = row1.Member;
            ExpeditionMember front = expedition.Members[frontIndex];
            string weapon = UiTestUtil.ItemAt(front, 0, 0).Item.Id;
            Managers.Sound.ForgetEffects();
            grid.SquareAt(0, 0).Click();
            yield return null;
            StringAssert.Contains(UiText.TooltipTitle(expedition.Inventory[0]), party.InventoryInfo, "The held item in Diablo's tooltip.");
            StringAssert.DoesNotContain(UiStrings.Get(UiKeys.Board.Grade, expedition.Inventory[0].Grade), party.InventoryInfo, "Without its grade (round 57).");
            UiTestUtil.HoverSquare(board1, 0, 0);
            yield return null;
            Assert.AreEqual(GhostKind.Displaces, board1.Grid.GhostShown, "The ghost says the weapon would go to the inventory.");
            Assert.AreEqual(GridGeometry.Span(expedition.Inventory[0].Item.Width), board1.Grid.GhostBlock.rect.width, 0.5f,
                "The ghost one block over the squares and the lines between them (round 53).");
            Assert.IsTrue(board1.PieceAt(0, 0).IsDisplaced, "The weapon it would push out turns dark gold (round 49).");
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundEffect.Button, SoundEffect.ItemPlace }, Managers.Sound.Asked, "A square that takes the item sounds once, as put in.");
            Assert.AreEqual(rewardId, UiTestUtil.ItemAt(front, 0, 0).Item.Id);
            Assert.AreEqual(weapon, expedition.Inventory.Single().Item.Id);
            Assert.IsTrue(party.InventoryPanel.activeSelf, "The popup stays open.");
            Assert.AreEqual(weapon, grid.PieceAt(0, 0).Item.Item.Id, "The weapon lies where the reward lay.");

            // The board item, then "to inventory": the board is empty and the inventory holds both.
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            Assert.IsTrue(party.ToInventory.interactable);
            UiTestUtil.Click(party.ToInventory);
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundEffect.Button, SoundEffect.ItemPlace }, Managers.Sound.Asked, "Picking the item up clicks; putting it away sounds as put in.");
            Assert.IsEmpty(front.Board.Items);
            CollectionAssert.AreEqual(new[] { weapon, rewardId }, expedition.Inventory.Select(i => i.Item.Id));
            Assert.IsFalse(party.ToInventory.interactable);

            // On the grid (round 49): the weapon held from it is red over the reward and green on free squares, where a click lays it.
            BoardItem reward = expedition.Inventory.Items[1];
            int freeRow = expedition.Inventory.Height - 1;
            grid.SquareAt(0, 0).Click();
            yield return null;
            Assert.AreEqual(0, party.Hand.InventoryIndex);
            grid.SquareAt(reward.At.X, reward.At.Y).Hover();
            yield return null;
            Assert.AreEqual(GhostKind.Refused, grid.GhostShown, "Over the reward: no swap.");
            grid.SquareAt(0, freeRow).Hover();
            yield return null;
            Assert.AreEqual(GhostKind.Fits, grid.GhostShown);
            Assert.AreEqual(GridGeometry.Span(expedition.Inventory[0].Item.Width), grid.GhostBlock.rect.width, 0.5f, "One block on the inventory too (round 53).");
            grid.SquareAt(0, freeRow).Click();
            yield return null;
            Assert.AreEqual(new Placement(0, freeRow), expedition.Inventory.Items[0].At);
            Assert.IsFalse(party.Hand.Holding);

            // Back to the board: an empty board still takes an item out of the grid.
            grid.SquareAt(0, freeRow).Click();
            yield return null;
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            Assert.AreEqual(weapon, UiTestUtil.ItemAt(front, 0, 0).Item.Id);

            // A board item held is laid on free squares of the grid where the pointer is.
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            grid.SquareAt(0, freeRow).Click();
            yield return null;
            Assert.IsEmpty(front.Board.Items);
            Assert.AreEqual(new Placement(0, freeRow), expedition.Inventory.Items.Single(i => i.Item.Item.Id == weapon).At);
            grid.SquareAt(0, freeRow).Click();
            yield return null;
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            Assert.AreEqual(weapon, UiTestUtil.ItemAt(front, 0, 0).Item.Id);

            // Filled to its squares, the inventory takes nothing more: a board item cannot go in ("to inventory" is off) while it can
            // still go to another board.
            while (expedition.Inventory.HasRoomFor(data.Items.Get("buckler")))
            {
                expedition.Inventory.Add(new EquippedItem(data.Items.Get("buckler"), 1));
            }

            map.Refresh();
            yield return null;
            Assert.AreEqual(
                UiStrings.Get(UiKeys.Board.InventoryTitle, squares, squares),
                UiTestUtil.TextAt(map, "Frame/InventoryPanel/InventoryBox/InventoryTitle"));
            UiTestUtil.ClickSquare(board1, 1, 0);
            yield return null;
            Assert.AreEqual(frontIndex, party.Hand.ItemMember, "Picked up by any of its squares.");
            Assert.IsFalse(party.ToInventory.interactable, "No room for it in the inventory.");
            PartyBoardView board2 = party.ColumnOfRow(UiTestUtil.FlatRow(1)).BoardView;
            UiTestUtil.HoverSquare(board2, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board2.Grid.GhostShown, "Another board's empty row still takes it.");
            Assert.IsNull(board1.Grid.GhostShown, "The ghost shows on the board under the pointer only.");
            UiTestUtil.ClickSquare(board1, 0, 0);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Put back where it lay: let go.");

            // Closing the popup; I opens and closes it as the button does (round 52).
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return null;
            Assert.IsFalse(party.InventoryPanel.activeSelf);
            map.PressInventoryKey();
            yield return null;
            Assert.IsTrue(party.InventoryPanel.activeSelf, "I opens it.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryHide), UiTestUtil.TextAt(map, "Frame/BoardPanel/InventoryToggle/InventoryToggleLabel"));
            map.PressInventoryKey();
            yield return null;
            Assert.IsFalse(party.InventoryPanel.activeSelf, "I closes it.");
        }

        [UnityTest]
        public IEnumerator PartySide_EquipmentShowsItsFatigue_OnItsCell_AndInAllOnTheHeadOfTheBoard()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);

            // Found on the way: a weapon and an armor cost fatigue, a support item does not; the base weapon never does.
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(string.Empty, row1.FatigueTotal, "Only the base weapon: nothing on the head.");
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("dagger"), 8), 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("buckler"), 8), 2, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("herb_pouch"), 8), 0, 2);
            map.Refresh();
            yield return null;

            int cost = data.Balance.FatigueEquipment;
            PartyBoardView board = row1.BoardView;
            Assert.AreEqual(string.Empty, board.PieceAt(0, 0).FatigueTag, "The base weapon.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.FatigueTag, cost), board.PieceAt(0, 1).FatigueTag, "The dagger.");
            Assert.IsTrue(board.PieceAt(2, 1).ShowsFatigue, "The buckler: one square, so its tag is a dot.");
            Assert.AreEqual(string.Empty, board.PieceAt(0, 2).FatigueTag, "The herb pouch.");
            Assert.IsFalse(board.PieceAt(0, 2).ShowsFatigue);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.FatigueTotal, 2 * cost), row1.FatigueTotal);
            Assert.AreEqual(string.Empty, party.ColumnOfRow(UiTestUtil.FlatRow(1)).FatigueTotal, "Another board with only its base weapon.");

            // The item held says on the detail line what it costs, or that a base weapon costs nothing.
            string detail = UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail");
            UiTestUtil.ClickSquare(board, 1, 1);
            yield return null;
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.FatigueCost, cost), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            UiTestUtil.ClickSquare(board, 0, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Put back where it lay.");
            UiTestUtil.ClickSquare(board, 0, 0);
            yield return null;
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.BaseWeapon), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            Assert.AreNotEqual(detail, UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
        }

        /// <summary>
        /// The fatigue under a member's feet (round 36, C) and its state: the pips fill as in the lobby, the state line names the job
        /// and the state in its colour, and a click on the line explains the state on the detail line; a click again, or a click on
        /// a cell, puts it down.
        /// </summary>
        [UnityTest]
        public IEnumerator PartySide_FatigueShowsAsPipsUnderTheHpBar_AndAStateIsNamedAndExplainedOnClick()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            Assume.That(data.Balance.MaxFatigue, Is.EqualTo(200), "The cases below count pips of 20.");
            Assume.That(data.Balance.FatigueBreakdown, Is.EqualTo(100));
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(1);
            PartyColumnView row2 = party.ColumnOfRow(2);
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(10, row1.FatiguePips.Count);
            Assert.IsTrue(row1.FatiguePips.All(p => p.Ratio == 0f), "Fresh.");
            Assert.AreEqual(UiText.Job(front.JobId), row1.JobLine);
            Assert.IsFalse(row1.StateButton.interactable, "No state: the line takes no click.");

            // 128 and fearful: five full violet pips, one full and one 8/20 red one; the job, then the affliction in red.
            FatigueStateData fearful = data.FatigueStates.Get("fearful");
            front.Fatigue = 128;
            front.StateId = fearful.Id;
            map.Refresh();
            yield return null;
            CollectionAssert.AreEqual(new[] { 1f, 1f, 1f, 1f, 1f, 1f, 0.4f, 0f, 0f, 0f }, row1.FatiguePips.Select(p => Mathf.Round(p.Ratio * 10f) / 10f));
            Assert.IsTrue(row1.FatiguePips.Take(5).All(p => p.FillColor == UiPalette.FatigueBar));
            Assert.IsTrue(row1.FatiguePips.Skip(5).All(p => p.FillColor == UiPalette.FatigueDanger));
            Assert.AreEqual(UiText.JobLine(front.JobId, fearful), row1.JobLine);
            StringAssert.Contains(UiText.FatigueStateName(fearful), row1.JobLine);
            StringAssert.Contains(ColorUtility.ToHtmlStringRGB(UiPalette.FatigueDanger), row1.JobLine);
            Assert.IsTrue(row1.StateButton.interactable);
            Assert.IsTrue(row2.FatiguePips.All(p => p.Ratio == 0f), "The others are fresh.");
            Assert.IsFalse(row2.StateButton.interactable);

            // The click explains the state; another puts it down.
            string hint = UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail");
            UiTestUtil.Click(row1.StateButton);
            yield return null;
            Assert.AreEqual(UiText.FatigueStateDetail(fearful), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            StringAssert.Contains(UiText.Name(fearful.Description), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            UiTestUtil.Click(row1.StateButton);
            yield return null;
            Assert.AreEqual(hint, UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));

            // A virtue in gold; a cell click puts the state down.
            FatigueStateData focused = data.FatigueStates.Get("focused");
            front.Fatigue = 40;
            front.StateId = focused.Id;
            map.Refresh();
            yield return null;
            StringAssert.Contains(ColorUtility.ToHtmlStringRGB(UiPalette.Virtue), row1.JobLine);
            UiTestUtil.Click(row1.StateButton);
            yield return null;
            StringAssert.Contains(UiText.Name(focused.Description), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            UiTestUtil.ClickSquare(row1.BoardView, 0, 0);
            yield return null;
            StringAssert.DoesNotContain(UiText.Name(focused.Description), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
        }

        /// <summary>
        /// In battle the party's units carry the fatigue pips (the enemies none), and the breakdown at the threshold is a moment on the
        /// stage (round 38, B): the battle slows, the stage darkens but the unit and draws in on it, the unit holds its state pose with the
        /// glow and the burst behind it and the state's word over its head, a caption and a sound; then everything goes and the state's
        /// name stays under its feet. The valkyrie breaks down: hers are the state poses drawn so far.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_PartyUnitsShowFatiguePips_AndABreakdownIsAMomentOnTheStage()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachABreakdownInTheFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            BalanceData balance = engine.Setup.Balance;
            float pip = (float)balance.MaxFatigue / FatiguePips.Count;

            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(battle);
            foreach (BattleUnitView unit in units)
            {
                Assert.AreEqual(unit.Unit.Side == BattleSide.Party, unit.PipsShown, "Only a mercenary has fatigue: " + unit.name);
                if (unit.Unit.Side == BattleSide.Party)
                {
                    Assert.AreEqual(unit.Unit.Fatigue / pip, unit.FatiguePips.Sum(p => p.Ratio), 0.01f, "The pips hold the fatigue, a tenth each.");
                }
            }

            BattleEvent down = engine.Events.First(e => e.Kind == BattleEventKind.BrokeDown);
            FatigueStateData state = Managers.Data.Data.FatigueStates.Get(down.Id);
            bool virtue = state.Kind == FatigueStateKind.Virtue;
            BattleUnitView broken = units.Single(u => u.Unit.Ref.Equals(down.Target));
            Assert.AreEqual("valkyrie", Managers.Data.Data.Mercenaries.Get(broken.Unit.Setup.SourceId).JobId, "The valkyrie, staged in row 1, broke down.");

            // A quarter of a second in: the slow stretch, the dark and the zoom at full, the pose held, the burst and the word on.
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.IsTrue(battle.BreakdownMomentShown, "The breakdown is a moment on the stage.");
            Assert.IsTrue(battle.MomentSlows, "The moment holds the battle slow.");
            Assert.Greater(battle.KillDarkness, 0.5f, "The rest of the stage is dark.");
            Assert.Greater(battle.StageZoom, 1.1f, "The stage is drawn in on the unit.");
            Assert.Less(battle.KillDarkLayer.GetSiblingIndex(), broken.transform.parent.GetSiblingIndex(), "The unit stands over the dark.");
            Assert.IsTrue(broken.HoldsPose, "The unit holds its state pose.");
            Assert.IsTrue(broken.Figure.ShowsPose);
            Assert.AreEqual(virtue ? "valkyrie_resolute" : "valkyrie_broken", broken.Figure.Art.name, "The pose of the state's kind.");
            Assert.IsTrue(battle.Fx.BurstShown, "The glow and the burst stand behind the unit.");
            Assert.AreEqual(1, battle.Fx.BurstsShown);
            Assert.IsTrue(battle.Fx.WordShown, "The state's word stands over its head.");
            Assert.AreEqual(UiText.FatigueStateName(state), battle.Fx.WordText);
            Assert.AreEqual(UiPalette.FatigueState(state.Kind), battle.Fx.WordColor);
            Assert.AreEqual(UiText.FatigueStateName(state), broken.FatigueStateShown, "The state's name under its feet.");
            Assert.IsTrue(units.Where(u => u != broken).All(u => u.FatigueStateShown == string.Empty), "Nobody else is in a state.");
            Assert.IsTrue(battle.Captions.Any(c => c.Contains(UiText.FatigueStateName(state))), "The breakdown reads as a caption.");
            Assert.IsTrue(Managers.Sound.Asked.Contains(virtue ? SoundEffect.Survived : SoundEffect.DeathsDoor), "The breakdown sounds.");

            // Then everything of the moment goes by itself; the state stays.
            yield return new WaitForSecondsRealtime(3f);
            Assert.IsFalse(battle.MomentShown, "The moment is over.");
            Assert.AreEqual(1f, battle.StageZoom, 1e-4f, "The zoom is back.");
            Assert.AreEqual(0f, battle.KillDarkness, "The dark is gone.");
            Assert.IsFalse(battle.Fx.BurstShown);
            Assert.IsFalse(battle.Fx.WordShown);
            Assert.IsFalse(broken.HoldsPose);
            Assert.IsFalse(broken.Figure.ShowsPose, "The figure is back.");
            Assert.AreEqual(UiText.FatigueStateName(state), broken.FatigueStateShown);
        }

        [UnityTest]
        public IEnumerator NodeMap_TheLongMapScrollsUp_AndOpensOnTheFloorThePartyStandsOn()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            ScrollRect scroll = map.MapScroll;

            Assert.AreEqual(NodeMapScreen.FloorSpacing,
                map.NodeView(nodes.OnFloor(2)[0].Id).Rect.anchoredPosition.y - map.NodeView(nodes.OnFloor(1)[0].Id).Rect.anchoredPosition.y, 0.01f,
                "The floors stand FloorSpacing apart.");
            Assert.Greater(scroll.content.rect.height, scroll.viewport.rect.height, "The whole map does not fit: it scrolls.");
            Assert.AreEqual(0f, scroll.verticalNormalizedPosition, 0.001f, "It opens at the first floor.");
            Assert.IsTrue(nodes.OnFloor(1).All(node => InView(map, node)));
            Assert.IsFalse(nodes.OnFloor(nodes.FloorCount).Any(node => InView(map, node)), "The boss is out of view.");

            // Deep in the map (staged): the map opens on the floor the party stands on, with the way on above it.
            MapNode deep = nodes.OnFloor(10)[0];
            yield return StandOn(deep);
            map = UiTestUtil.Screen<NodeMapScreen>();
            Assert.IsTrue(InView(map, deep));
            foreach (int next in deep.NextNodeIds)
            {
                Assert.IsTrue(UiTestUtil.PointerReaches(map.NodeView(next).Button), $"Node {next} on the next floor can be clicked.");
            }

            Assert.IsFalse(nodes.OnFloor(1).Any(node => InView(map, node)), "The first floor has scrolled away.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.Progress, 10, nodes.FloorCount), UiTestUtil.TextAt(map, "Frame/Header/Progress"));
        }

        [UnityTest]
        public IEnumerator NodeMap_EveryNodeShowsTheMarkerAndNameOfItsKind_AndAnEliteSaysItIsStrong()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            var names = new Dictionary<MapNodeKind, string>
            {
                { MapNodeKind.Battle, "전투" }, { MapNodeKind.Elite, "정예" }, { MapNodeKind.Camp, "야영지" }, { MapNodeKind.Shop, "상점" }, { MapNodeKind.Boss, "보스" },
            };
            var markers = new Dictionary<MapNodeKind, string>
            {
                { MapNodeKind.Battle, "node_battle" }, { MapNodeKind.Elite, "node_elite" }, { MapNodeKind.Camp, "node_camp" }, { MapNodeKind.Shop, "node_shop" }, { MapNodeKind.Boss, "node_boss" },
            };

            foreach (MapNode node in nodes.Nodes)
            {
                MapNodeView view = map.NodeView(node.Id);
                Assert.AreEqual(names[node.Kind], view.Label, $"Node {node.Id}.");
                Assert.AreEqual(markers[node.Kind], view.Icon.name, $"Node {node.Id}.");
            }

            Assert.IsTrue(nodes.OnFloor(nodes.FloorCount - 1).All(node => node.Kind == MapNodeKind.Camp), "The floor before the boss is all camps.");
            Assert.AreEqual("누가 기다리는지는 들어가 봐야 압니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));

            // An elite chosen (when the map has one; most maps do): its hint says that a strong band waits, not who.
            MapNode elite = nodes.Nodes.FirstOrDefault(node => node.Kind == MapNodeKind.Elite);
            if (elite != null)
            {
                yield return StandOn(nodes.Nodes.First(node => node.NextNodeIds.Contains(elite.Id)));
                map = UiTestUtil.Screen<NodeMapScreen>();
                UiTestUtil.Click(map.NodeView(elite.Id).Button);
                yield return null;
                Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, elite.Floor, "정예"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
                Assert.AreEqual("강한 적 무리가 기다립니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
                Assert.AreEqual("전투 시작", UiTestUtil.TextAt(map, "Frame/BoardPanel/Enter/EnterLabel"));
            }
        }

        [UnityTest]
        public IEnumerator Camp_ItsWindowOpensOverTheMap_AndRestingHealsAndGoesOn()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            BalanceData balance = Managers.Data.Data.Balance;

            // Staged on the floor before the camp floor: every way on is a camp.
            MapNode before = nodes.OnFloor(nodes.FloorCount - 2)[0];
            yield return StandOn(before);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            MapNode camp = nodes.Get(before.NextNodeIds[0]);
            Assert.AreEqual(MapNodeKind.Camp, camp.Kind);
            UiTestUtil.Click(map.NodeView(camp.Id).Button);
            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, camp.Floor, "야영지"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
            Assert.AreEqual("싸움 없이 쉬어 가는 곳입니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
            Assert.AreEqual("야영지로", UiTestUtil.TextAt(map, "Frame/BoardPanel/Enter/EnterLabel"));

            // In: the window over the map, the panel pointing at it, and no battle: the node map stays.
            ExpeditionMember hurt = Managers.Expedition.Expedition.Members[0];
            hurt.Hp = 1;
            hurt.Fatigue = 50;
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.Camp, Managers.Expedition.Phase);
            Assert.AreSame(map, Managers.UI.Current, "A camp is on the node map.");
            Assert.IsTrue(UiTestUtil.At(map, "Frame/Map/Camp").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/BoardPanel/Enter").gameObject.activeSelf);
            Assert.AreEqual("지도 위의 창에서 고릅니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
            Assert.AreEqual(UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"), UiTestUtil.TextAt(map, "Frame/Map/Camp/CampWindow/CampBox/CampTitle"));
            Assert.AreEqual($"HP {balance.CampHealPercent}% 회복\n피로도 -{balance.CampFatigueRelief}", UiTestUtil.TextAt(map, UiTestUtil.CampRest + "/RestBody"));
            Assert.IsTrue(InView(map, camp), "The map followed the party up to the camp's floor.");
            Assert.IsFalse(UiTestUtil.PointerReaches(map.NodeView(camp.Id).Button), "The window's shade takes the map's clicks.");

            // Rest: the living heal and shed fatigue, the window closes, and the only way on, the boss, is chosen.
            UiTestUtil.Click(map, UiTestUtil.CampRest);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase);
            Assert.AreEqual(1 + hurt.MaxHp * balance.CampHealPercent / 100, hurt.Hp);
            Assert.AreEqual(50 - balance.CampFatigueRelief, hurt.Fatigue);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/Map/Camp").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, nodes.FloorCount, "보스"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
            Assert.AreEqual("전투 시작", UiTestUtil.TextAt(map, "Frame/BoardPanel/Enter/EnterLabel"));
        }

        [UnityTest]
        public IEnumerator Shop_ItsWindowOpensOverTheMap_AnOfferIsPickedAndBoughtOntoABoard_AndTheRefreshCostsMoreEachTime()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            MapNode shop = nodes.Nodes.FirstOrDefault(n => n.Kind == MapNodeKind.Shop);
            Assume.That(shop, Is.Not.Null, "This map has no shop node (a few maps in a hundred have none): nothing to test on it.");

            // Staged before the shop with coins to spend: the chosen node says what it is and how to go in, and the header the coins.
            yield return StandOn(nodes.Nodes.First(n => n.NextNodeIds.Contains(shop.Id)));
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            Managers.Expedition.Expedition.Coins = 100;
            UiTestUtil.Click(map.NodeView(shop.Id).Button);
            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, shop.Floor, "상점"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
            Assert.AreEqual("지역 코인으로 아이템을 사는 곳입니다. 싸움은 없습니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
            Assert.AreEqual("상점으로", UiTestUtil.TextAt(map, "Frame/BoardPanel/Enter/EnterLabel"));
            Assert.AreEqual("100", UiTestUtil.TextAt(map, "Frame/Header/Coins"));

            // In: the window over the map with a tile per offer, the coins and the refresh at its base cost; the way in makes room for buying into the inventory; no battle.
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.Shop, Managers.Expedition.Phase);
            Assert.AreSame(map, Managers.UI.Current, "A shop is on the node map.");
            Assert.IsTrue(UiTestUtil.At(map, "Frame/Map/Shop").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/BoardPanel/Enter").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(map, "Frame/BoardPanel/ShopBuy").gameObject.activeSelf);
            Assert.AreEqual("지도 위의 창에서 삽니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
            IReadOnlyList<ItemOffer> stock = Managers.Expedition.ShopStock;
            int goodsCount = Managers.Expedition.ShopGoodsCount;
            Assert.AreEqual(data.Balance.ShopSlots, goodsCount, "Eight goods (stage 21: Diablo II's merchant).");
            Assert.GreaterOrEqual(stock.Count - goodsCount, 1, "At least one potion at every shop (round 56).");
            CollectionAssert.AreEquivalent(Enumerable.Range(0, stock.Count), map.Merchant.ShownSlots, "Every good on the merchant's one grid: items, bags and potions together.");
            Assert.AreEqual(MerchantGridView.Height, map.Merchant.Rows, "Ten across and eight down (round 56).");
            for (int i = 0; i < stock.Count; i++)
            {
                Placement at = map.Merchant.PlacementOf(i).Value;
                if (i >= goodsCount)
                {
                    Assert.AreEqual(new Placement(MerchantGridView.PotionColumn, i - goodsCount), at, "The potions down the last column, from the top.");
                    continue;
                }

                bool bag = stock[i].Kind == OfferKind.Bag;
                int width = bag ? data.Bags.Get(stock[i].Id).Width : data.Items.Get(stock[i].Id).Width;
                int height = bag ? data.Bags.Get(stock[i].Id).Height : data.Items.Get(stock[i].Id).Height;
                Assert.LessOrEqual(at.X + width, MerchantGridView.GoodsColumns, "The goods keep to the first eight columns; the ninth stays empty.");
                if (bag)
                {
                    Assert.AreEqual(MerchantGridView.BagRow, at.Y, "The bags along the last row (round 58).");
                }
                else
                {
                    Assert.LessOrEqual(at.Y + height, MerchantGridView.ItemRows, "The items above the empty row over the bags' (round 58).");
                }
            }
            Assert.AreEqual("100", UiTestUtil.TextAt(map, UiTestUtil.ShopBox + "/ShopCoins"));
            Assert.AreEqual(data.Balance.ShopRefreshBase.ToString(), UiTestUtil.TextAt(map, UiTestUtil.ShopRefreshCost));
            Assert.IsFalse(UiTestUtil.PointerReaches(map.NodeView(shop.Id).Button), "The window's shade takes the map's clicks.");

            // The price stands at every good's bottom-right, drawn whole (a label cut to its own width with an ellipsis lost every glyph of
            // a two-digit price to a rounding error once, round 44).
            foreach (int shown in map.Merchant.ShownSlots)
            {
                TMP_Text priceText = map.Merchant.PriceLabelOf(shown);
                Assert.AreEqual(Managers.Expedition.PriceOf(stock[shown]).ToString(), priceText.text);
                Assert.Greater(priceText.rectTransform.rect.width, 0f, $"Slot {shown}: the price label has no width.");
                Assert.AreEqual(priceText.text.Length, priceText.textInfo.characterCount, $"Slot {shown}: the price '{priceText.text}' is not drawn whole.");
            }

            var cost = UiTestUtil.At(map, UiTestUtil.ShopRefreshCost).GetComponent<TMP_Text>();
            Assert.AreEqual(cost.text.Length, cost.textInfo.characterCount, "The refresh cost is drawn whole.");

            // Round 57: the pointer over a good shows Diablo's tooltip over it (its facts without the grade, and its price), centred on its
            // piece; it takes no pointer, and goes when the pointer leaves. An item picked: its piece is the held one's lighter blue, and no
            // tooltip shows (it rides the pointer); no card opens; the panel says how to buy.
            // One no taller than the start bag's two free rows (Slice B stage 19) and with an effect to read (not the whetstone, stage 20).
            int slot = Enumerable.Range(0, stock.Count).Where(i => stock[i].Kind == OfferKind.Item && data.Items.Get(stock[i].Id).Height <= 2 && !data.Items.Get(stock[i].Id).IsPassive).DefaultIfEmpty(-1).First();
            Assume.That(slot, Is.GreaterThanOrEqualTo(0), "No item on offer fits the start bag's free rows.");
            string bought = stock[slot].Id;
            int price = Managers.Expedition.PriceOf(stock[slot]);
            ItemData offeredData = data.Items.Get(bought);
            ItemSlotView good = map.Merchant.PieceOf(slot);
            Assert.AreEqual(UiPalette.GridPiece, good.Ground, "A good's piece in the boards' blue (round 53).");
            Assert.IsFalse(good.IsUnaffordable, "The coins cover it.");
            Assert.AreEqual(new Vector2Int(offeredData.Width, offeredData.Height), good.GroundSquares, "At its own size, square by square, as on a board.");
            int other = map.Merchant.ShownSlots.First(s => s != slot);
            Placement otherAt = map.Merchant.PlacementOf(other).Value;
            Assert.AreEqual(string.Empty, map.MerchantInfo, "Nothing pointed at, no tooltip.");
            UiTestUtil.HoverGood(map.Merchant, slot);
            yield return null;
            var offered = new EquippedItem(data.Items.Get(bought), stock[slot].Grade, tier: stock[slot].Tier);
            StringAssert.Contains(UiText.TooltipLines(offered), map.MerchantInfo, "Its facts in Diablo's tooltip.");
            StringAssert.DoesNotContain(UiStrings.Get(UiKeys.Board.Grade, offered.Grade), map.MerchantInfo, "Without its grade (round 57).");
            StringAssert.Contains(UiStrings.Get(UiKeys.Map.OfferPrice, price), map.MerchantInfo, "And its price.");
            var tip = (RectTransform)UiTestUtil.At(map, "Frame/MerchantInfoBox");
            var screenRect = (RectTransform)tip.parent;
            Rect tipAt = TooltipPlacement.In(tip, screenRect);
            Rect goodAt = TooltipPlacement.In(good.Rect, screenRect);
            Assert.AreEqual(goodAt.center.x, tipAt.center.x, 1f, "Centred on the good's piece.");
            if (goodAt.yMin - TooltipPlacement.Gap - tipAt.height >= TooltipPlacement.ScreenMargin)
            {
                Assert.AreEqual(goodAt.yMin - TooltipPlacement.Gap, tipAt.yMax, 1f, "Just over the good.");
            }
            else
            {
                Assert.AreEqual(goodAt.yMax + TooltipPlacement.Gap, tipAt.yMin, 1f, "Under the good: no room over it.");
            }

            Assert.IsFalse(tip.GetComponentsInChildren<Graphic>().Any(g => g.raycastTarget), "The tooltip takes no pointer: the goods under it keep theirs.");
            UiTestUtil.LeaveGood(map.Merchant, slot);
            yield return null;
            Assert.AreEqual(string.Empty, map.MerchantInfo, "The pointer gone, the tooltip goes.");

            UiTestUtil.ClickGood(map.Merchant, slot);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(slot, map.PickedOffer);
            Assert.IsTrue(map.Merchant.PieceOf(slot).IsPicked, "The held one's lighter blue.");
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            Assert.IsFalse(party.Tooltip.IsShown);
            Assert.AreEqual(string.Empty, map.MerchantInfo, "Held, it rides the pointer: no tooltip.");
            Assert.AreEqual("지도 위의 창에서 삽니다.", UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeHint"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.ShopBuyHint), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            Assert.IsTrue(UiTestUtil.ButtonAt(map, "Frame/BoardPanel/ShopBuy").interactable, "It fits the inventory.");

            // Round 55: held over the open inventory, the offer shows green on its free squares (a press there buys it there); the
            // inventory's button works on it while it is held, and closing the window keeps it held.
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.InventoryPanel.activeSelf);
            Assert.AreEqual(slot, map.PickedOffer, "Still held.");
            UiTestUtil.HoverSquare(party.InventoryGrid, offeredData.Width - 1, Managers.Expedition.Expedition.Inventory.Height - 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, party.InventoryGrid.GhostShown);
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(slot, map.PickedOffer, "Still held.");

            // Bought onto the start bag's free rows of the row-1 board: the ghost shows where it goes, the coins are paid, the good leaves
            // the merchant's grid, the item lies where the pointer put it.
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            int carried = front.Board.Items.Count;
            UiTestUtil.HoverSquare(row1.BoardView, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, row1.BoardView.Grid.GhostShown);
            UiTestUtil.ClickSquare(row1.BoardView, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(100 - price, Managers.Expedition.Expedition.Coins);
            Assert.AreEqual(carried + 1, front.Board.Items.Count);
            Assert.AreEqual(bought, UiTestUtil.ItemAt(front, 0, 1).Item.Id, "The stock's slot is empty now: the id was kept from before.");
            Assert.IsNull(map.Merchant.PieceOf(slot), "Bought: its place on the grid is empty until a refresh.");
            Assert.AreEqual(otherAt, map.Merchant.PlacementOf(other).Value, "The other goods stay where they lie.");
            Assert.AreEqual(-1, map.PickedOffer);
            Assert.AreEqual((100 - price).ToString(), UiTestUtil.TextAt(map, "Frame/Header/Coins"));
            Assert.AreEqual((100 - price).ToString(), UiTestUtil.TextAt(map, UiTestUtil.ShopBox + "/ShopCoins"));

            // The refresh: every slot is drawn anew (the sold one too), the coins pay its cost, and the next one costs more.
            int before = Managers.Expedition.Expedition.Coins;
            UiTestUtil.Click(map, UiTestUtil.ShopRefresh);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(before - data.Balance.ShopRefreshBase, Managers.Expedition.Expedition.Coins);
            Assert.IsNotNull(map.Merchant.PieceOf(slot), "The sold slot is filled again.");
            Assert.AreEqual((data.Balance.ShopRefreshBase + data.Balance.ShopRefreshStep).ToString(), UiTestUtil.TextAt(map, UiTestUtil.ShopRefreshCost));

            // Round 57: a good the party cannot buy now (beyond the coins; a potion, nor without an empty slot) has dark red squares (a bag:
            // its rim); its price stays gold. With no coins, a press on a potion buys nothing and draws the merchant again.
            Managers.Expedition.Expedition.Coins = 0;
            UiTestUtil.ClickGood(map.Merchant, goodsCount);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(-1, map.PickedOffer);
            foreach (int shown in map.Merchant.ShownSlots)
            {
                ItemSlotView piece = map.Merchant.PieceOf(shown);
                Assert.IsTrue(piece.IsUnaffordable, $"Slot {shown}: no coins, nothing can be bought.");
                Assert.AreEqual(UiPalette.GridPieceUnaffordable, piece.Ground, $"Slot {shown}: dark red.");
                Assert.AreEqual(UiPalette.DiabloGold, map.Merchant.PriceLabelOf(shown).color, $"Slot {shown}: the price stays gold.");
            }

            StringAssert.Contains(UiStrings.Get(UiKeys.Map.OfferPrice, Managers.Expedition.PriceOf(stock[goodsCount])), map.MerchantInfo, "The potion under the pointer still shows its tooltip.");

            // Leave: the window closes and the way on is offered.
            UiTestUtil.Click(map, UiTestUtil.ShopLeave);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/Map/Shop").gameObject.activeSelf);
            Assert.AreEqual(string.Empty, map.MerchantInfo, "The tooltip goes with the shop.");
            Assert.IsTrue(UiTestUtil.At(map, "Frame/BoardPanel/Enter").gameObject.activeSelf);
            Assert.IsTrue(shop.NextNodeIds.All(id => map.NodeView(id).Button.interactable), "The next floor is offered.");
        }

        [UnityTest]
        public IEnumerator Cells_ShowATierAboveCommon_AsAnOutlineAndStars_OnThePartySideAndInBattle()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("dagger"), 8, tier: ItemTier.Bronze), 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("buckler"), 8, tier: ItemTier.Silver), 2, 1);
            map.Refresh();
            yield return null;

            PartyBoardView board = row1.BoardView;
            ItemSlotView weapon = board.PieceAt(0, 0);
            ItemSlotView daggerPiece = board.PieceAt(0, 1);
            ItemSlotView bucklerPiece = board.PieceAt(2, 1);
            Assert.IsNull(weapon.TierShown, "Common: the plain piece, no stars.");
            Assert.AreEqual(0, weapon.Stars);
            Assert.AreEqual(UiPalette.GridPiece, weapon.Ground, "Diablo's dark blue under an item between battles (round 49).");
            Assert.AreEqual(new Vector2Int(weapon.Item.Item.Width, weapon.Item.Item.Height), weapon.GroundSquares, "Square by square, the grid's line between them (round 53).");
            Assert.IsNull(weapon.OutlineColor, "No outline round an icon in Diablo's look.");
            Assert.AreEqual(ItemTier.Bronze, daggerPiece.TierShown);
            Assert.AreEqual(1, daggerPiece.Stars);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Bronze), daggerPiece.StarColor, "Diablo's rarity colours: Bronze the magic blue.");
            Assert.IsNull(daggerPiece.OutlineColor);
            Assert.AreEqual(ItemTier.Silver, bucklerPiece.TierShown);
            Assert.AreEqual(2, bucklerPiece.Stars);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Silver), bucklerPiece.StarColor, "Silver the rare yellow.");
            Assert.IsNull(board.PieceAt(0, 2), "An empty square.");
            Assert.AreEqual(string.Empty, daggerPiece.MergeMark, "Nothing merges into it.");

            // The held item's title names its tier.
            UiTestUtil.ClickSquare(board, 1, 1);
            yield return null;
            StringAssert.Contains(UiText.TierWord(ItemTier.Bronze), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            UiTestUtil.ClickSquare(board, 0, 1);
            yield return null;

            // In battle the party's items carry the same stars, with no ground under them (round 49); the enemies' items are Common.
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return UiTestUtil.WaitForRedraw();
            BattleItemView[] cells = UiTestUtil.Views<BattleItemView>(battle);
            BattleItemView dagger = cells.Single(c => !c.Mirrored && c.Icon != null && c.Icon.name == "dagger");
            BattleItemView buckler = cells.Single(c => !c.Mirrored && c.Icon != null && c.Icon.name == "buckler");
            Assert.AreEqual(ItemTier.Bronze, dagger.TierShown);
            Assert.AreEqual(1, dagger.Stars);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Bronze), dagger.StarColor);
            Assert.AreEqual(0f, dagger.Ground.a, "No blue behind an item on the battle screen (round 49).");
            Assert.AreEqual(ItemTier.Silver, buckler.TierShown);
            Assert.AreEqual(2, buckler.Stars);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Silver), buckler.StarColor);
            Assert.IsTrue(cells.Where(c => c.Mirrored).All(c => c.TierShown == null && c.Stars == 0 && c.OutlineColor == null));
        }

        [UnityTest]
        public IEnumerator PartySide_AChosenItem_MarksTheCellItWouldMergeInto_AndPuttingItThereMerges()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            PartyColumnView row2 = party.ColumnOfRow(UiTestUtil.FlatRow(1));
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ExpeditionMember first = expedition.Members[row1.Member];
            ExpeditionMember second = expedition.Members[row2.Member];
            UiTestUtil.Put(first, new EquippedItem(data.Items.Get("dagger"), 8), 0, 1);
            UiTestUtil.Put(second, new EquippedItem(data.Items.Get("dagger"), 9), 1, 2);
            map.Refresh();
            yield return null;
            PartyBoardView board1 = row1.BoardView;
            PartyBoardView board2 = row2.BoardView;
            Assert.AreEqual(string.Empty, board2.PieceAt(1, 2).MergeMark, "Nothing is held.");

            // The row-1 dagger held: row 2's dagger is marked with the tier the merge makes, and the detail line says so.
            EquippedItem chosen = UiTestUtil.ItemAt(first, 0, 1);
            UiTestUtil.ClickSquare(board1, 0, 1);
            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Bronze)), board2.PieceAt(1, 2).MergeMark);
            Assert.AreEqual(ItemTier.Bronze, board2.PieceAt(1, 2).TierShown);
            Assert.AreEqual(string.Empty, board2.PieceAt(0, 0).MergeMark, "Another item.");
            Assert.IsNull(board1.PieceAt(0, 1), "Lifted off its board while held (round 54), so not marked as merging into itself.");
            StringAssert.Contains(UiText.MergeHint(chosen), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));

            // With its top-left on the dagger's top-left square the ghost says it merges; lying over the dagger from another square it
            // would push it out instead.
            UiTestUtil.HoverSquare(board2, 0, 2);
            yield return null;
            Assert.AreEqual(GhostKind.Displaces, board2.Grid.GhostShown, "Top-left beside the dagger: lying over half of it.");
            UiTestUtil.HoverSquare(board2, 1, 2);
            yield return null;
            Assert.AreEqual(GhostKind.Merges, board2.Grid.GhostShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Bronze)), board2.Grid.GhostLabel);

            // Put there, they merge: one Bronze dagger at the better grade on row 2, where it lay; none on row 1.
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(board2, 1, 2);
            yield return null;
            EquippedItem merged = UiTestUtil.ItemAt(second, 1, 2);
            Assert.AreEqual(ItemTier.Bronze, merged.Tier);
            Assert.AreEqual(9, merged.Grade, "The better grade of the two.");
            Assert.AreEqual(1, first.Board.Items.Count, "The dagger left row 1's board.");
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.AreEqual(ItemTier.Bronze, board2.PieceAt(1, 2).TierShown);
            Assert.AreEqual(string.Empty, board2.PieceAt(1, 2).MergeMark);
        }

        /// <summary>
        /// A bag on the party side (Slice B stage 19): picked up by an empty square of it, the frame shows while it is held, its ghost says
        /// where it goes, and put down it carries the items wholly inside it; turned, they turn with it. The start bag is never picked up.
        /// </summary>
        [UnityTest]
        public IEnumerator PartySide_ABagIsPickedByAnEmptySquare_ShowsTheFrameWhileHeld_AndCarriesItsItems_TurnedWithIt()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            front.Board.Bags.Add(new BoardBag(data.Bags.Get("leather_pouch"), new Placement(0, 3)));
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("dagger"), 8), 0, 3);
            map.Refresh();
            yield return null;
            PartyBoardView board = row1.BoardView;
            Assert.AreEqual(2, board.Grid.BagsShown);
            Assert.AreEqual(2, board.Grid.GetComponentsInChildren<Image>().Count(i => i.name.EndsWith("BagTemplateWell")), "Each bag is Diablo's well (round 49): a rim round the grey of its squares.");
            Assert.AreEqual(GridSquareLook.Empty, board.Grid.SquareAt(2, 3).Look, "The pouch's empty square.");
            Assert.IsFalse(board.Grid.SquareAt(0, 5).ShowsDashes, "No frame while no bag is held.");
            Assert.AreEqual(GridSquareLook.Hidden, board.Grid.SquareAt(0, 5).Look, "Outside every bag: nothing shows.");

            // The start bag's empty square picks nothing up; the pouch's does, and the frame shows.
            UiTestUtil.ClickSquare(board, 2, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "The start bag never moves.");
            UiTestUtil.ClickSquare(board, 2, 3);
            yield return null;
            Assert.AreEqual(row1.Member, party.Hand.BagMember);
            Assert.AreEqual(GridSquareLook.Frame, board.Grid.SquareAt(0, 5).Look, "While a bag is held the frame shows where it can go.");
            Assert.IsTrue(board.Grid.SquareAt(0, 5).ShowsDashes, "As dashed squares.");
            StringAssert.Contains(UiText.Name(data.Bags.Get("leather_pouch").Name), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            UiTestUtil.HoverSquare(board, 0, 2);
            yield return null;
            Assert.AreEqual(GhostKind.Refused, board.Grid.GhostShown, "Over the start bag.");
            UiTestUtil.HoverSquare(board, 0, 5);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board.Grid.GhostShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.GhostBag), board.Grid.GhostLabel);

            // Put down two rows lower, the dagger goes with it.
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(board, 0, 5);
            yield return null;
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.IsFalse(party.Hand.Holding);
            Assert.AreEqual(new Placement(0, 5), front.Board.BagAt(1, 5).At);
            Assert.AreEqual(new Placement(0, 5), front.Board.ItemAt(0, 5).At, "The dagger kept its place in the pouch.");
            Assert.IsNull(front.Board.BagAt(0, 3));
            Assert.AreEqual(GridSquareLook.Hidden, board.Grid.SquareAt(2, 3).Look);
            Assert.IsFalse(board.Grid.SquareAt(2, 3).ShowsDashes, "Let go: the frame is gone.");

            // Picked again and turned a quarter: the pouch stands up in the first column and the dagger turns with it. Its centre rides the
            // pointer (round 54), so the pointer on the first column's row 6 stands it from row 5.
            UiTestUtil.ClickSquare(board, 2, 5);
            yield return null;
            party.TurnHeld(1);
            UiTestUtil.HoverSquare(board, 0, 6);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board.Grid.GhostShown);
            UiTestUtil.ClickSquare(board, 0, 6);
            yield return null;
            Assert.AreEqual(new Placement(0, 5, 1), front.Board.BagAt(0, 7).At, "One square across and three down.");
            BoardItem dagger = front.Board.ItemAt(0, 6);
            Assert.AreEqual(new Placement(0, 5, 1), dagger.At, "The dagger stands up with the pouch.");
            Assert.AreEqual(1, board.PieceAt(0, 5).ArtTurns);
        }

        [UnityTest]
        public IEnumerator PartySide_AHeldItem_LeavesItsPlace_RidesThePointer_AndAPressElsewhereSendsItBack()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            ExpeditionState expedition = Managers.Expedition.Expedition;
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            PartyBoardView board = row1.BoardView;
            ExpeditionMember front = expedition.Members[row1.Member];
            BoardItem weapon = front.Board.ItemAt(0, 0);
            Assert.IsFalse(party.HeldPointer.Shown, "Nothing held: the pointer is the pointer.");

            // Round 54 (Diablo II): picked up, the weapon leaves its place (no piece, its squares empty) and rides the pointer as its icon
            // at its squares' size. It stays on its board until it is put down.
            UiTestUtil.ClickSquare(board, 0, 0);
            yield return null;
            Assert.IsTrue(party.Hand.Holding);
            Assert.IsNull(board.PieceAt(0, 0), "Lifted off the board.");
            Assert.AreEqual(GridSquareLook.Empty, board.Grid.SquareAt(0, 0).Look);
            Assert.AreSame(weapon, front.Board.ItemAt(0, 0));
            Assert.IsTrue(party.HeldPointer.Shown);
            Assert.IsNotNull(party.HeldPointer.Icon);
            Assert.AreEqual(GridGeometry.Span(weapon.Item.Item.Width), party.HeldPointer.Size.x, 0.5f);
            Assert.AreEqual(0, party.HeldPointer.Turns);

            // Where it would land shows the colour only, its centre on the pointer: the middle square aims at the whole row, and where it
            // lay is free now, so that is green too.
            UiTestUtil.HoverSquare(board, 1, 0);
            yield return null;
            Assert.AreEqual(GhostKind.Same, board.Grid.GhostShown);
            Assert.IsNotNull(board.Grid.GhostBlock, "Green where it lay.");
            Assert.IsFalse(board.Grid.GhostShowsTheHeldThing, "No icon in the squares: it is on the pointer.");
            UiTestUtil.HoverSquare(board, 1, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board.Grid.GhostShown);
            var corners = new Vector3[4];
            ((RectTransform)board.Grid.SquareAt(1, 1).transform).GetWorldCorners(corners);
            Assert.Less(Vector2.Distance((corners[0] + corners[2]) * 0.5f, party.HeldPointer.DrawnAt), 1f, "Drawn with its centre where the pointer is.");
            Assert.AreEqual(GridGeometry.Corner(0, 1).x, board.Grid.GhostBlock.anchoredPosition.x, 0.5f, "The row under it, from its first square.");
            Assert.AreEqual(GridGeometry.Corner(0, 1).y, board.Grid.GhostBlock.anchoredPosition.y, 0.5f);

            // Turned, the icon on the pointer turns with it.
            party.TurnHeld(1);
            yield return null;
            Assert.AreEqual(1, party.HeldPointer.Turns);
            party.TurnHeld(-1);
            yield return null;

            // A press on a potion's slot is no place for it: it goes back where it was and the press does nothing more (no potion chosen).
            string potion = UiText.Name(data.Potions.Get(expedition.Potions.First(p => p != null)).Name);
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Views<PotionSlotView>(map).First(v => v.Icon != null).Button);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Let go.");
            Assert.IsFalse(party.HeldPointer.Shown, "The pointer is back.");
            Assert.AreSame(weapon, front.Board.ItemAt(0, 0));
            Assert.IsNotNull(board.PieceAt(0, 0), "Back on its place.");
            CollectionAssert.AreEqual(new[] { SoundEffect.Button }, Managers.Sound.Asked, "A click, once.");
            StringAssert.DoesNotContain(potion, UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"), "The potion was not chosen.");

            // A button that works on what is held takes the press: "to inventory" puts it away.
            UiTestUtil.ClickSquare(board, 0, 0);
            yield return null;
            UiTestUtil.Click(party.ToInventory);
            yield return null;
            Assert.IsFalse(party.Hand.Holding);
            Assert.IsNull(front.Board.ItemAt(0, 0));
            Assert.IsTrue(expedition.Inventory.Any(i => i.Item.Id == weapon.Item.Item.Id), "Put away by its button.");
        }

        [UnityTest]
        public IEnumerator PartySide_RightClickingAnItem_OpensItsCardAboveThePanel_AndTheNextPressClosesOnlyTheCard()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            PartyColumnView row2 = party.ColumnOfRow(UiTestUtil.FlatRow(1));
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ExpeditionMember first = expedition.Members[row1.Member];
            ExpeditionMember second = expedition.Members[row2.Member];
            UiTestUtil.Put(first, new EquippedItem(data.Items.Get("dagger"), 8), 0, 1);
            UiTestUtil.Put(second, new EquippedItem(data.Items.Get("dagger"), 9), 1, 2);
            map.Refresh();
            yield return null;
            PartyBoardView board1 = row1.BoardView;
            PartyBoardView board2 = row2.BoardView;
            ItemTooltipView card = party.Tooltip;
            Assert.IsFalse(card.IsShown, "No card before a click.");

            // Nothing held: a right click on any square of the row-1 dagger opens its card just above the panel, centred on its piece,
            // with its words.
            EquippedItem dagger = UiTestUtil.ItemAt(first, 0, 1);
            string mergeMark = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Bronze));
            UiTestUtil.RightClickSquare(board1, 1, 1);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreSame(dagger, card.Item);
            Assert.AreEqual(UiText.ItemTitle(dagger), card.Title);
            UiText.ItemCard(dagger, out string facts, out string effects, out string fatigue);
            Assert.AreEqual(facts, UiTestUtil.TextAt(card, "ItemTooltipFacts"));
            Assert.AreEqual(effects, UiTestUtil.TextAt(card, "ItemTooltipEffects"));
            Assert.AreEqual(fatigue, UiTestUtil.TextAt(card, "ItemTooltipFatigue"));
            Assert.AreEqual(UiText.MergeHint(dagger), UiTestUtil.TextAt(card, "ItemTooltipMerge"));
            Assert.IsFalse(party.Hand.Holding, "A right click picks nothing up.");
            float panelTop = -((RectTransform)UiTestUtil.At(map, "Frame/BoardPanel")).anchoredPosition.y;
            Assert.AreEqual(panelTop - TooltipPlacement.Gap, card.Placed.yMax, 0.5f, "Just above the board panel.");
            Assert.AreEqual(card.Anchor.center.x, card.Placed.center.x, 0.5f, "Centred on the piece.");
            Assert.AreEqual(GridGeometry.Span(2), card.Anchor.width, 0.5f, "As wide as the dagger's piece.");
            Assert.AreEqual(ItemTooltipView.Notch.Bottom, card.NotchShown);
            Assert.AreEqual(card.Anchor.center.x - card.Placed.x, card.NotchAt, 0.5f, "The notch under the piece's middle.");
            Assert.Greater(card.Placed.height, 100f, "The card grew to its lines.");

            // A press anywhere closes the card and nothing else.
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(card.IsShown, "A press anywhere closes the card.");
            Assert.IsFalse(party.Hand.Holding);

            // A left click picks the dagger up and opens no card; while it is held a right click opens none either (it turns what is held).
            UiTestUtil.ClickSquare(board1, 0, 1);
            yield return null;
            Assert.IsFalse(card.IsShown, "A left click picks the item; it opens no card.");
            Assert.AreEqual(mergeMark, board2.PieceAt(1, 2).MergeMark, "Held.");
            UiTestUtil.RightClickSquare(board1, 2, 2);
            yield return null;
            Assert.IsFalse(card.IsShown, "No card while something is held.");

            // Turned a quarter (as the right button, the wheel or R do), it stands up: its ghost is one square across and two down,
            // and put there it lies turned, its icon with it.
            party.TurnHeld(1);
            Assert.AreEqual(1, party.Hand.Turns);
            UiTestUtil.HoverSquare(board1, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board1.Grid.GhostShown);
            UiTestUtil.ClickSquare(board1, 0, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding);
            Assert.AreEqual(new Placement(0, 1, 1), first.Board.ItemAt(0, 2).At, "Standing in the first column.");
            Assert.AreSame(board1.PieceAt(0, 1), board1.PieceAt(0, 2));
            Assert.AreEqual(1, board1.PieceAt(0, 2).ArtTurns, "The icon turned with it.");

            // A right click on row 2's dagger shows that one's card; an empty square has none.
            UiTestUtil.RightClickSquare(board2, 2, 2);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreSame(UiTestUtil.ItemAt(second, 1, 2), card.Item);
            UiTestUtil.PressTheBackground();
            yield return null;
            UiTestUtil.RightClickSquare(board1, 2, 2);
            yield return null;
            Assert.IsFalse(card.IsShown, "An empty square has no card.");

            // Its card up, the click that picks the dagger closes it; put on row 2's dagger, they merge.
            UiTestUtil.RightClickSquare(board1, 0, 2);
            yield return null;
            Assert.IsTrue(card.IsShown);
            UiTestUtil.ClickSquare(board1, 0, 2);
            yield return null;
            Assert.IsFalse(card.IsShown, "The press that picks closes the card.");
            Assert.AreEqual(1, party.Hand.Turns, "Held as it lies.");
            UiTestUtil.ClickSquare(board2, 1, 2);
            yield return null;
            Assert.AreEqual(ItemTier.Bronze, UiTestUtil.ItemAt(second, 1, 2).Tier);
            Assert.AreEqual(new Placement(1, 2), second.Board.ItemAt(1, 2).At, "The merged dagger lies where the one merged into lay.");
            Assert.IsNull(card.Item);
        }

        [UnityTest]
        public IEnumerator Battle_RightClickingAnItem_OpensItsCardBesideTheBoard_PartyLeftEnemyRight_NotWhileAPotionIsArmed()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleBoardView[] boards = UiTestUtil.Views<BattleBoardView>(battle);
            BattleBoardView partyBoard = boards.First(b => b.Unit.Side == BattleSide.Party && b.Items.Count > 0);
            BattleBoardView enemyBoard = boards.First(b => b.Unit.Side == BattleSide.Enemy && b.Items.Count > 0);
            ItemTooltipView card = battle.Tooltip;
            Assert.IsFalse(card.IsShown);

            // An enemy's item right-clicked (round 47; a left click on a cell does nothing): its card to the right of the board at the
            // cell's height, the notch on its left edge at the cell's middle; the battle is not touched.
            int timeBefore = Managers.Expedition.Battle.Engine.TimeMs;
            EquippedItem enemyItem = enemyBoard.Unit.Items[0].Equipped;
            UiTestUtil.Click(enemyBoard.Items[0].Button);
            yield return null;
            Assert.IsFalse(card.IsShown, "A left click opens no card.");
            UiTestUtil.RightClick(enemyBoard.Items[0]);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreSame(enemyItem, card.Item);
            Assert.AreEqual(UiText.ItemTitle(enemyItem), card.Title);
            Assert.AreEqual(card.Anchor.xMax + TooltipPlacement.BesideGap, card.Placed.x, 0.5f, "To the right of the enemy's board.");
            Assert.AreEqual(card.Anchor.y, card.Placed.y, 0.5f, "At the cell's height.");
            Assert.AreEqual(ItemTooltipView.Notch.Left, card.NotchShown, "The notch on the edge facing the board.");
            Assert.AreEqual(card.Anchor.center.y - card.Placed.y, card.NotchAt, 0.5f, "The notch at the cell's middle height.");
            Assert.IsFalse(UiTestUtil.At(card, "ItemTooltipMerge").gameObject.activeSelf, "No merging in battle.");
            Assert.IsFalse(UiTestUtil.At(card, "ItemTooltipFatigue").gameObject.activeSelf, "An enemy has no fatigue.");
            Assert.AreEqual(timeBefore, Managers.Expedition.Battle.Engine.TimeMs, "Paused: the card changed nothing of the battle.");

            // A party member's item: its card takes the enemy's place, to the left of the board when there is room (the rear rows of
            // a full party have none: then to the right), the notch on the edge facing the board.
            EquippedItem partyItem = partyBoard.Unit.Items[0].Equipped;
            UiTestUtil.RightClick(partyBoard.Items[0]);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreSame(partyItem, card.Item);
            bool roomOnTheLeft = card.Anchor.x - TooltipPlacement.BesideGap - card.Placed.width >= TooltipPlacement.ScreenMargin;
            if (roomOnTheLeft)
            {
                Assert.AreEqual(card.Anchor.x - TooltipPlacement.BesideGap, card.Placed.xMax, 0.5f, "To the left of the party's board.");
                Assert.AreEqual(ItemTooltipView.Notch.Right, card.NotchShown);
            }
            else
            {
                Assert.AreEqual(card.Anchor.xMax + TooltipPlacement.BesideGap, card.Placed.x, 0.5f, "No room on the left: to the right of the board.");
                Assert.AreEqual(ItemTooltipView.Notch.Left, card.NotchShown);
            }
            UiText.ItemCard(partyItem, out _, out _, out string partyFatigue);
            Assert.AreEqual(partyFatigue, UiTestUtil.TextAt(card, "ItemTooltipFatigue"), "A member's item tells its fatigue.");

            // A press anywhere closes it.
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(card.IsShown);

            // A potion armed: the cells give their clicks up to the boards (the potion's targets), so no card can open; put down, they take them again.
            PotionSlotView[] potions = UiTestUtil.Views<PotionSlotView>(battle);
            Assume.That(potions.Length, Is.GreaterThan(0), "The expedition starts with a potion.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            Assert.IsFalse(partyBoard.Items[0].Button.interactable, "The cell gives its clicks up while a potion waits for a target.");
            Assert.IsFalse(UiTestUtil.PointerReaches(partyBoard.Items[0].Button), "The click falls through to the board.");
            UiTestUtil.Click(potions[0].Button);
            yield return null;
            Assert.IsTrue(partyBoard.Items[0].Button.interactable);
            Assert.IsTrue(UiTestUtil.PointerReaches(partyBoard.Items[0].Button));
        }

        [UnityTest]
        public IEnumerator Loot_AfterTheWin_TheBandIsUp_ADropLiesOnTheFloor_AndTakenOntoTheSameItem_Merges()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return UiTestUtil.EndBattle(battle);
            ExpeditionState expedition = Managers.Expedition.Expedition;
            StaticData data = Managers.Data.Data;

            // The win keeps the screen (round 47): no result window, the band over the stage, the header's battle controls gone, the
            // panel's loot buttons up, and a drop on the floor per slot of the loot.
            Assert.IsTrue(battle.AfterWin);
            Assert.AreEqual(GamePhase.Battle, Managers.Expedition.Phase, "The ended battle still shows.");
            Assert.IsTrue(Managers.Expedition.LootOpen);
            Assert.IsFalse(UiTestUtil.At(battle, "Frame/ResultPanel").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(battle, "Frame/VictoryBand").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.Victory), UiTestUtil.TextAt(battle, "Frame/VictoryBand/BandRow/BandTitle"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.LootCount, expedition.Loot.Count), UiTestUtil.TextAt(battle, "Frame/VictoryBand/BandRow/BandLoot"));
            Assert.IsFalse(UiTestUtil.At(battle, "Frame/Header/Retreat").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(battle, "Frame/Header/Pause").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(battle, "Frame/BoardPanel/LootContinue").gameObject.activeSelf);
            Assert.AreEqual(expedition.Loot.Count, battle.Drops.Count, "A drop per slot.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.LootHint), UiTestUtil.TextAt(battle, "Frame/BoardPanel/LootHint"));

            // Staged: the first drop is a Bronze dagger, and row 1 already holds a Bronze dagger under its weapon. The battle's boards gave
            // way to the node map's: the member's board stands in row 1's column with the weapon and the staged item on it.
            int index = 0;
            ItemOffer drop = new ItemOffer(OfferKind.Item, "dagger", expedition.Loot[index].Grade, ItemTier.Bronze);
            expedition.Loot[index] = drop;
            int flat = UiTestUtil.FlatRow();
            PartyBoardView row1 = battle.LootBoardOfRow(flat);
            int front = expedition.Members.FindIndex(m => m.Alive && m.Row == flat);
            UiTestUtil.Put(expedition.Members[front], new EquippedItem(data.Items.Get(drop.Id), 8, tier: ItemTier.Bronze), 0, 1);
            battle.Refresh();
            yield return null;
            Assert.IsTrue(row1.gameObject.activeSelf);
            Assert.AreEqual(2, row1.Pieces.Count(), "The weapon and the staged item.");
            Assert.IsFalse(UiTestUtil.Views<BattleBoardView>(battle).Any(b => b.Unit.Side == BattleSide.Party && b.gameObject.activeSelf), "The battle's party boards are away.");
            LootDropView first = battle.Drops[index];
            StringAssert.Contains(UiText.TierWord(ItemTier.Bronze), first.Title);
            Assert.IsTrue(first.gameObject.activeSelf);
            Assert.IsFalse(first.IsPicked);

            // A right click on the drop: its card beside it, with the merge hint; a press closes it.
            UiTestUtil.RightClick(first.Button);
            yield return null;
            Assert.IsTrue(battle.Tooltip.IsShown);
            Assert.AreEqual(drop.Id, battle.Tooltip.Item.Item.Id);
            Assert.AreEqual(UiText.MergeHint(battle.Tooltip.Item), UiTestUtil.TextAt(battle.Tooltip, "ItemTooltipMerge"));
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(battle.Tooltip.IsShown);

            // The drop picked: its word asks for a square, the hint says how, the piece of the same item is marked with Silver, the ghost
            // over its top-left square says so too, and a click there merges them. The last drop taken keeps the screen until continue.
            UiTestUtil.Click(first.Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(index, battle.PickedDrop);
            Assert.IsTrue(first.IsPicked);
            Assert.IsTrue(battle.LootHand.Holding, "The drop is in the hand.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.LootPickedHint), UiTestUtil.TextAt(battle, "Frame/BoardPanel/LootHint"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Silver)), row1.PieceAt(0, 1).MergeMark);
            UiTestUtil.HoverSquare(row1, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Merges, row1.Grid.GhostShown);
            UiTestUtil.ClickSquare(row1, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(2, expedition.Members[front].Board.Items.Count, "The base weapon and the merge.");
            Assert.AreEqual(ItemTier.Silver, UiTestUtil.ItemAt(expedition.Members[front], 0, 1).Tier);
            Assert.AreEqual(drop.Id, UiTestUtil.ItemAt(expedition.Members[front], 0, 1).Item.Id);
            Assert.IsFalse(battle.LootHand.Holding);
            Assert.IsFalse(first.gameObject.activeSelf, "The taken drop is gone from the floor.");
            Assert.AreSame(battle, UiTestUtil.Screen<BattleScreen>(), "The screen stays.");
            Assert.AreEqual(-1, battle.PickedDrop);

            // Round 52: the last drop taken, the boards still take moves until continue (they froze before).
            Assert.IsFalse(Managers.Expedition.LootOpen, "Every drop was taken.");
            EquippedItem merged = UiTestUtil.ItemAt(expedition.Members[front], 0, 1);
            UiTestUtil.ClickSquare(row1, 0, 1);
            yield return null;
            Assert.IsTrue(battle.LootHand.Holding, "Picked up after the loot is all taken.");
            UiTestUtil.ClickSquare(row1, 0, 2);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreSame(merged, UiTestUtil.ItemAt(expedition.Members[front], 0, 2), "Moved a row down.");
            Assert.IsFalse(battle.LootHand.Holding);

            UiTestUtil.Click(battle, "Frame/BoardPanel/LootContinue");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase);
            Assert.IsEmpty(expedition.Loot);
        }

        [UnityTest]
        public IEnumerator Loot_AfterTheWin_ItemsMoveBetweenTheBoards_AndContinuingLeavesTheRest()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();

            // Staged: a second drop beside the one the first battle drops (an elite drops two, and the first battle never is one).
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            ExpeditionState expedition = Managers.Expedition.Expedition;
            Assert.AreEqual(ExpeditionPhase.PickingLoot, expedition.Phase);
            ItemOffer first = expedition.Loot[0];
            expedition.Loot.Add(new ItemOffer(OfferKind.Item, first.Id, first.Grade, first.Tier));
            yield return UiTestUtil.WaitForResult(battle);
            Assert.AreEqual(2, battle.Drops.Count, "A drop per slot, at the first two enemy columns.");
            Assert.Less(((RectTransform)battle.Drops[0].transform).anchoredPosition.x, ((RectTransform)battle.Drops[1].transform).anchoredPosition.x);

            // The boards work as on the node map while nothing is picked: row 1's weapon picked up (the hint tells it), put on row 2's
            // empty row, moved. A right click on an item opens its card above the panel.
            PartyBoardView row1 = battle.LootBoardOfRow(1);
            PartyBoardView row2 = battle.LootBoardOfRow(2);
            int front = expedition.Members.FindIndex(m => m.Alive && m.Row == 1);
            int second = expedition.Members.FindIndex(m => m.Alive && m.Row == 2);
            EquippedItem weapon = UiTestUtil.ItemAt(expedition.Members[front], 0, 0);
            UiTestUtil.RightClickSquare(row1, weapon.Item.Width - 1, 0);
            yield return null;
            Assert.IsTrue(battle.Tooltip.IsShown);
            Assert.AreSame(weapon, battle.Tooltip.Item);
            Assert.AreEqual(ItemTooltipView.Notch.Bottom, battle.Tooltip.NotchShown);
            UiTestUtil.ClickSquare(row1, 0, 0);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.Tooltip.IsShown, "The press closes the card.");
            StringAssert.Contains(UiText.ItemTitle(weapon), UiTestUtil.TextAt(battle, "Frame/BoardPanel/LootHint"));
            UiTestUtil.HoverSquare(row2, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, row2.Grid.GhostShown, "Row 2's empty row can take it.");
            UiTestUtil.ClickSquare(row2, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(0, expedition.Members[front].Board.Items.Count);
            Assert.AreSame(weapon, UiTestUtil.ItemAt(expedition.Members[second], 0, 1));

            // The second drop taken into the inventory: it is gone from the floor, the first still lies there, and continuing leaves it.
            // Round 54: picked, it leaves the floor and rides the pointer; a press elsewhere (the other drop) puts it back and picks nothing.
            UiTestUtil.Click(battle.Drops[1].Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.Drops[1].OnTheFloor);
            Assert.IsTrue(battle.HeldPointer.Shown);
            UiTestUtil.Click(battle.Drops[0].Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(-1, battle.PickedDrop);
            Assert.IsTrue(battle.Drops[1].OnTheFloor);
            Assert.IsFalse(battle.HeldPointer.Shown);
            UiTestUtil.Click(battle.Drops[1].Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.ButtonAt(battle, "Frame/BoardPanel/LootToInventory").interactable);
            UiTestUtil.Click(battle, "Frame/BoardPanel/LootToInventory");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(Managers.Expedition.LootOpen, "The first drop still lies there.");
            Assert.IsFalse(battle.Drops[1].gameObject.activeSelf);
            Assert.IsTrue(battle.Drops[0].gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.LootCount, 1), UiTestUtil.TextAt(battle, "Frame/VictoryBand/BandRow/BandLoot"));
            Assert.AreEqual(1, expedition.Inventory.Count);
            Assert.IsFalse(UiTestUtil.ButtonAt(battle, "Frame/BoardPanel/LootToInventory").interactable, "Nothing is picked after a take.");

            // Round 52: the inventory window after a win, over the party's side of the stage, opened and closed by its button or I; an
            // item of it is laid on a board as on the node map.
            Assert.IsFalse(battle.InventoryOpen);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryShow), UiTestUtil.TextAt(battle, "Frame/BoardPanel/LootInventoryToggle/LootInventoryToggleLabel"));
            battle.PressInventoryKey();
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(battle.InventoryOpen);
            Assert.IsTrue(battle.InventoryWindow.gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.InventoryHide), UiTestUtil.TextAt(battle, "Frame/BoardPanel/LootInventoryToggle/LootInventoryToggleLabel"));
            Assert.AreEqual(960f, battle.InventoryWindow.Rect.rect.width, 0.5f, "The left half: the party's side of the stage.");
            Assert.AreEqual(0f, battle.InventoryWindow.Rect.anchoredPosition.x, 0.5f);
            Assert.IsTrue(battle.Drops[0].gameObject.activeSelf, "The drop left on the floor still shows.");
            BoardItem kept = expedition.Inventory.Items[0];
            Assert.IsNotNull(battle.InventoryWindow.Grid.PieceAt(kept.At.X, kept.At.Y), "The drop taken in lies on the window's grid.");
            battle.InventoryWindow.Grid.SquareAt(kept.At.X, kept.At.Y).Click();
            yield return null;
            Assert.AreEqual(0, battle.LootHand.InventoryIndex, "Picked from the window's grid.");
            UiTestUtil.ClickSquare(row1, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(0, expedition.Inventory.Count, "Laid on row 1's empty row.");
            Assert.AreEqual(first.Id, UiTestUtil.ItemAt(expedition.Members[front], 0, 1).Item.Id);
            UiTestUtil.Click(battle, "Frame/BoardPanel/LootInventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(battle.InventoryOpen);
            Assert.IsFalse(battle.InventoryWindow.gameObject.activeSelf);

            UiTestUtil.Click(battle, "Frame/BoardPanel/LootContinue");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            Assert.IsEmpty(expedition.Loot);
            Assert.AreEqual(1, expedition.Members[front].Board.Items.Count(i => i.Item.Item.Id == first.Id), "What was taken is kept.");
        }

        [UnityTest]
        public IEnumerator Loot_ADropLiesAtAnItemWindowsSize_AndHeldOverTheOpenInventory_IsTakenWhereItIsLaid()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return UiTestUtil.WaitForResult(battle);
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ItemOffer drop = expedition.Loot[0];
            Assume.That(drop.Kind, Is.EqualTo(OfferKind.Item), "The first battle drops an item.");
            ItemData item = Managers.Data.Data.Items.Get(drop.Id);

            // Round 55: on the floor the drop's icon is the size it has in an item window (its squares, less the margin), tilted as before.
            Vector2 window = GridGeometry.ArtBox(new Vector2(GridGeometry.Span(item.Width), GridGeometry.Span(item.Height)), 0, ItemSlotView.ArtMargin);
            Assert.AreEqual(window.x, battle.Drops[0].IconRect.sizeDelta.x, 0.5f);
            Assert.AreEqual(window.y, battle.Drops[0].IconRect.sizeDelta.y, 0.5f);

            // Picked with the inventory open, it rides the pointer; over the inventory's free squares it shows green, and a press there
            // takes it there (the button still takes it to the first room).
            battle.PressInventoryKey();
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(battle.Drops[0].Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(0, battle.PickedDrop);
            int x = item.Width - 1;
            int y = expedition.Inventory.Height - 1;
            UiTestUtil.HoverSquare(battle.InventoryWindow.Grid, x, y);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, battle.InventoryWindow.Grid.GhostShown);
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(battle.InventoryWindow.Grid, x, y);
            yield return UiTestUtil.WaitForRedraw();
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked, "Put in.");
            BoardItem taken = expedition.Inventory.Items.Single(i => i.Item.Item.Id == drop.Id);
            Assert.AreEqual(GridGeometry.Anchor(GridGeometry.Centre(x, y), item.Width, item.Height, 0, expedition.Inventory.Width, expedition.Inventory.Height), taken.At,
                "Its centre where the pointer pressed.");
            Assert.AreEqual(-1, battle.PickedDrop);
            Assert.IsFalse(battle.LootHand.Holding);
            Assert.IsFalse(battle.Drops[0].gameObject.activeSelf, "Gone from the floor.");
        }

        [UnityTest]
        public IEnumerator Camp_Mend_ChoosesAnItemOnTheBoards_ShowsItsChange_AndRaisesItATier()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            BalanceData balance = Managers.Data.Data.Balance;
            MapNode before = nodes.OnFloor(nodes.FloorCount - 2)[0];
            yield return StandOn(before);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(map.NodeView(before.NextNodeIds[0]).Button);
            yield return null;
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampChoices").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampMend").gameObject.activeSelf);

            // The mend card: the window turns to the mend step, the boards wait for the item and the inventory stays out of it.
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            UiTestUtil.Click(map, UiTestUtil.CampMend);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampMend").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampChoices").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.MendTitle, nodes.FloorCount - 1), UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampTitle"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.MendHint), UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampHint"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.MendHint), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
            Assert.IsFalse(UiTestUtil.At(map, "Frame/BoardPanel/InventoryToggle").gameObject.activeSelf);
            Assert.IsFalse(party.ToInventory.gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampMend/MendPlate").gameObject.activeSelf, "Nothing is chosen yet.");
            Assert.IsFalse(UiTestUtil.ButtonAt(map, UiTestUtil.CampBox + "/CampMend/MendConfirm").interactable);
            PartyBoardView board = row1.BoardView;
            Assert.IsTrue(Managers.Expedition.CanUpgradeAtCamp(row1.Member, 0, 0), "The base weapon can be mended.");
            UiTestUtil.ClickSquare(board, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampMend/MendPlate").gameObject.activeSelf, "An empty square chooses nothing.");
            Assert.IsFalse(party.Hand.Holding, "Nothing is picked up while mending.");

            // The weapon chosen by any of its squares: its piece is the gold one, the plate says its tier and its effects before and
            // after, and the detail line its facts.
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            EquippedItem weapon = UiTestUtil.ItemAt(front, 0, 0);
            ItemEffect effect = weapon.Item.Effects[0];
            UiTestUtil.ClickSquare(board, weapon.Item.Width - 1, 0);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(board.PieceAt(0, 0).IsPicked);
            Assert.IsFalse(party.Hand.Holding);
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampMend/MendPlate").gameObject.activeSelf);
            string change = UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampMend/MendPlate/MendChange");
            StringAssert.Contains(UiText.Name(weapon.Item.Name), change);
            StringAssert.Contains(UiText.Change(UiText.TierWord(ItemTier.Common), UiText.TierWord(ItemTier.Bronze)), change);
            StringAssert.Contains(
                UiText.Change(UiText.Effect(effect, weapon.Magnitude(balance, effect)), weapon.TierUp().Magnitude(balance, effect).ToString()),
                UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampMend/MendPlate/MendEffects"));
            Assert.IsTrue(UiTestUtil.ButtonAt(map, UiTestUtil.CampBox + "/CampMend/MendConfirm").interactable);
            StringAssert.Contains(UiText.ItemTitle(weapon), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));

            // Back leaves the choice; mend again and confirm: the weapon is Bronze and still the base weapon, and the boss is next.
            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampMend/MendBack");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampChoices").gameObject.activeSelf);
            Assert.AreEqual(GamePhase.Camp, Managers.Expedition.Phase);
            UiTestUtil.Click(map, UiTestUtil.CampMend);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.ClickSquare(board, 0, 0);
            yield return UiTestUtil.WaitForRedraw();
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampMend/MendConfirm");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase);
            Assert.AreEqual(ItemTier.Bronze, UiTestUtil.ItemAt(front, 0, 0).Tier);
            Assert.IsTrue(UiTestUtil.ItemAt(front, 0, 0).IsBase);
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/Map/Camp").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(map, "Frame/BoardPanel/InventoryToggle").gameObject.activeSelf);
            Assert.AreEqual(ItemTier.Bronze, board.PieceAt(0, 0).TierShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, nodes.FloorCount, "보스"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
        }

        [UnityTest]
        public IEnumerator PartySide_AWhetstonesStars_ShowWhilePointedAtOrHeld_LitOnAMeleeWeapon_AndTheCardsSaySo()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            BoardItem weapon = front.Board.ItemAt(0, 0);
            Assert.AreEqual(1, weapon.Height, "The staging needs a base weapon of one row.");
            Assert.IsTrue(weapon.Item.Item.Melee);

            // Slice B stage 20: a whetstone under the base weapon's left end, a herb pouch under the whetstone.
            var whetstone = new EquippedItem(data.Items.Get("whetstone"), 8);
            UiTestUtil.Put(front, whetstone, 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("herb_pouch"), 8), 0, 2);
            map.Refresh();
            yield return null;
            PartyBoardView board = row1.BoardView;
            Assert.IsEmpty(board.Grid.Stars, "No stars while nothing shows them.");

            // The pointer on the whetstone: its stars on the square above (on the weapon: lit) and the square below (on the pouch: hollow).
            UiTestUtil.HoverSquare(board, 0, 1);
            yield return null;
            Assert.AreEqual(2, board.Grid.Stars.Count());
            Assert.IsTrue(board.Grid.StarAt(0, 0).Lit, "The base weapon is a melee weapon.");
            Assert.IsFalse(board.Grid.StarAt(0, 2).Lit, "The herb pouch is not.");
            UiTestUtil.HoverSquare(board, 2, 2);
            yield return null;
            Assert.IsEmpty(board.Grid.Stars, "Off the whetstone the stars go.");

            // Held, it shows its stars where its ghost lies: over the weapon's right end and an empty square.
            UiTestUtil.ClickSquare(board, 0, 1);
            yield return null;
            Assert.AreEqual(row1.Member, party.Hand.ItemMember);
            UiTestUtil.HoverSquare(board, 2, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, board.Grid.GhostShown);
            Assert.IsTrue(board.Grid.StarAt(2, 0).Lit);
            Assert.IsFalse(board.Grid.StarAt(2, 2).Lit, "An empty square: a hollow star.");

            // Put down there, the cards say it: the weapon's what the star adds, the whetstone's which of its stars are lit.
            UiTestUtil.ClickSquare(board, 2, 1);
            yield return null;
            Assert.AreSame(whetstone, UiTestUtil.ItemAt(front, 2, 1));
            UiTestUtil.RightClickSquare(board, 0, 0);
            yield return null;
            ItemEffect damage = weapon.Item.Item.Effects[0];
            string weaponLines = UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects");
            StringAssert.Contains(UiText.Effect(damage, weapon.Item.Magnitude(data.Balance, damage) + 1), weaponLines, "The damage line counts the star's 1.");
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarBonus, UiText.Name(whetstone.Item.Name), 1), weaponLines);
            UiTestUtil.RightClickSquare(board, 2, 1);
            yield return null;
            Assert.AreSame(whetstone, party.Tooltip.Item);
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarDamage, 1), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarLit, 1, 2, UiText.Name(weapon.Item.Item.Name)), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.NoActivation), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts"));
            Assert.AreEqual(0, FatigueRules.ItemCost(data.Balance, whetstone), "Not equipment: no fatigue.");
        }

        /// <summary>New run, the first mercenaries of the roster one per row, and depart: the node map before the first floor.</summary>
        static IEnumerator DepartToTheMap()
        {
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Expedition/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
        }

        /// <summary>Stands the party on a node (staged: a test moves it along the map directly) and opens the node map again, as after a battle there.</summary>
        static IEnumerator StandOn(MapNode node)
        {
            Managers.Expedition.Expedition.CurrentNodeId = node.Id;
            yield return TaskUtil.Await(Managers.UI.ShowAsync(ScreenId.NodeMap));
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
        }

        /// <summary>Whether the middle of a node is inside the map's view.</summary>
        static bool InView(NodeMapScreen map, MapNode node)
        {
            var corners = new Vector3[4];
            map.MapScroll.viewport.GetWorldCorners(corners);
            Vector3 middle = map.NodeView(node.Id).Rect.position;
            return middle.x > corners[0].x && middle.x < corners[2].x && middle.y > corners[0].y && middle.y < corners[2].y;
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

            // The party side: every member stands as the figure of its job, and its board shows its bag in leather, its empty squares
            // dark, and a piece per item as big as its squares with the item's icon, without a tier mark (Common).
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

                Assert.IsNotEmpty(member.Board.Items, member.MercenaryId);
                PartyBoardView board = column.BoardView;
                Assert.AreEqual(member.Board.Bags.Count, board.Grid.BagsShown, member.MercenaryId);
                foreach (BoardItem placed in member.Board.Items)
                {
                    EquippedItem item = placed.Item;
                    ItemSlotView piece = board.PieceAt(placed.At.X, placed.At.Y);
                    Assert.AreSame(item, piece.Item, item.Item.Id);
                    Assert.AreEqual(GridGeometry.Span(placed.Width), piece.Rect.rect.width, 0.01f, item.Item.Id);
                    Assert.AreEqual(GridGeometry.Span(placed.Height), piece.Rect.rect.height, 0.01f, item.Item.Id);
                    Assert.IsNotNull(piece.Icon, item.Item.Id);
                    Assert.AreEqual(item.Item.Id, piece.Icon.name, item.Item.Id);
                    Assert.IsNull(piece.TierShown, $"{item.Item.Id}: a Common item wears no tier.");
                    Assert.IsFalse(piece.GetComponentsInChildren<TMPro.TMP_Text>().First(text => text.name.EndsWith("Text")).enabled, $"The words of {item.Item.Id} are hidden behind its icon.");
                }

                // The square under the base weapon (one row high, or two for the valkyrie's battle axe since round 51) is empty.
                int below = member.Board.ItemAt(0, 0).Height;
                Assert.AreEqual(GridSquareLook.Empty, board.Grid.SquareAt(0, below).Look, member.MercenaryId);
                Assert.IsNull(board.PieceAt(0, below), member.MercenaryId);
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

                // The board is a grid (Slice B stage 19): a mercenary's bag in leather (an enemy has none), and a piece per item as big as
                // its squares; each piece shows the icon its item's data names instead of the name; an enemy's icons are mirrored.
                Assert.AreEqual(unit.Unit.Side == BattleSide.Party ? 1 : 0, board.BagsShown, id);
                BattleItemView[] cells = board.Items.ToArray();
                Assert.AreEqual(unit.Unit.Items.Count, UiTestUtil.Views<BattleItemView>(board).Length, id);
                for (int i = 0; i < cells.Length; i++)
                {
                    ItemData item = unit.Unit.Items[i].Equipped.Item;
                    Assert.AreEqual(GridGeometry.Span(cells[i].At.WidthOf(item.Width, item.Height)), cells[i].Rect.rect.width, 0.01f, item.Id);
                    Assert.AreEqual(GridGeometry.Span(cells[i].At.HeightOf(item.Width, item.Height)), cells[i].Rect.rect.height, 0.01f, item.Id);
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
            cell.Bind(units[0].Unit.Items[0], cell.At, null, mirrored: false);
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
            Assert.IsTrue(Managers.Sound.Asked.Any(e => e == SoundEffect.Hit || e == SoundEffect.HitHeavy || e == SoundEffect.Blocked), "A hit sounds.");
            Assert.IsTrue(Managers.Sound.Asked.Contains(SoundEffect.ItemWeapon), "A weapon that fires sounds.");
            Assert.AreEqual(MusicTrack.Dungeon, Managers.Sound.Track, "A battle that is not the boss's has the dungeon's music.");
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
            Assert.IsTrue(Managers.Sound.Asked.Contains(SoundEffect.CandleOut), "The candle goes out with a sound.");
            Assert.IsTrue(Managers.Sound.Asked.Contains(SoundEffect.Thunder), "The storm's tick thunders.");
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

            // 0.7 s in, before any item's first firing (every cooldown is longer): each working item's icon is lit from its left as far
            // as it has charged (round 48, the fifth ask's 안 2: its own colours, no gold, no front line).
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
                    Assert.AreEqual(charge, cell.LitTo, 0.001f, $"The icon of {name} is lit as far as it has charged.");
                    Assert.AreEqual(1f, cell.Swell, 0.001f, name);
                    Assert.AreEqual(item.Active, cell.ShowsActive, name);
                    if (charge > 0f)
                    {
                        charging++;
                    }
                }
            }

            Assert.Greater(charging, 0, "Items charge in the first seconds.");

            // Right after a firing the icon is dark all over; at the moment of the next it is fully lit; while its owner is down it stays dark.
            BattleBoardView partyBoard = boards.First(b => b.Unit.Side == BattleSide.Party && b.Items.Count > 0);
            BattleItemState first = partyBoard.Unit.Items[0];
            BattleItemView firstCell = partyBoard.Items[0];
            Assume.That(first.Active, "The first mercenary's weapon works where it stands.");
            firstCell.Render(first.NextFireMs - first.CooldownMs, ownerAlive: true);
            Assert.AreEqual(0f, firstCell.LitTo, 0.001f, "Nothing charged: the icon is dark all over.");
            firstCell.Render(first.NextFireMs, ownerAlive: true);
            Assert.AreEqual(1f, firstCell.LitTo, 0.001f, "Fully charged: the icon is lit all over.");
            firstCell.Render(engine.TimeMs, ownerAlive: false);
            Assert.AreEqual(0f, firstCell.LitTo, 0.001f, "An item that does not charge stays dark.");

            // A firing: the icon is all lit and swells (to 1.12 at its largest), then is back to its size with the new cooldown's dark.
            // Each frame is a fixed 20 ms here, so the swell is caught on its way up.
            Time.captureDeltaTime = 0.02f;
            try
            {
                firstCell.Render(engine.TimeMs, ownerAlive: true);
                float charge = firstCell.Charge;
                firstCell.Pulse();
                Assert.AreEqual(1f, firstCell.LitTo, 0.001f, "All lit as it fires.");
                yield return null;
                Assert.Greater(firstCell.Swell, 1f, "It swells.");
                Assert.LessOrEqual(firstCell.Swell, BattleItemView.SwellScale + 0.001f);
                yield return new WaitForSeconds(0.4f);
                Assert.AreEqual(1f, firstCell.Swell, 0.001f, "Back to its size.");
                Assert.AreEqual(firstCell.Charge, firstCell.LitTo, 0.001f, "The dark is back to where it has charged.");
                Assert.Less(firstCell.LitTo, 1f);
                Assert.AreEqual(charge, firstCell.Charge, 0.001f, "The paused battle has not moved.");
            }
            finally
            {
                Time.captureDeltaTime = 0f;
            }

            // A mercenary's board shows its bag and its empty squares; an enemy's has neither.
            foreach (BattleBoardView board in boards)
            {
                bool party = board.Unit.Side == BattleSide.Party;
                Assert.AreEqual(party ? 1 : 0, board.BagsShown, board.Unit.Setup.SourceId);
                if (party)
                {
                    Assert.Greater(board.EmptySquaresShown, 0, "The start bag has room besides the weapon.");
                }
                else
                {
                    Assert.AreEqual(0, board.EmptySquaresShown);
                }
            }
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
            Assert.IsTrue(Managers.Sound.Asked.Contains(SoundEffect.MercenaryDeath), "A mercenary's death sounds (Docs/Design/12 §2).");
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

            Assert.IsTrue(battle.MomentSlows, "A unit's item felled it: a kill moment.");
            Assert.IsTrue(Managers.Sound.Asked.Contains(SoundEffect.KillMoment), "The kill moment has its sound.");
            Assert.AreEqual(MusicTrack.Boss, Managers.Sound.Track, "The boss battle has the boss's music.");
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
            while (battle.MomentSlows && Time.realtimeSinceStartup < until)
            {
                yield return null;
            }

            yield return null;
            Assert.IsFalse(battle.MomentSlows, "The slow time is over.");
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
            // A member worn down by the long battle's blows may break down (round 38) and slow the stage on its own; that moment is
            // not the kill moment this test is about (the hit's fatigue is 2 since stage 16, so it happens in some runs).
            if (!battle.BreakdownMomentShown)
            {
                Assert.AreEqual(1f, Time.timeScale);
            }

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
