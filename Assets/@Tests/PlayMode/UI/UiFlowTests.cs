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
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Strip/Depart");
            CollectionAssert.AreEqual(new[] { SoundEffect.Button }, Managers.Sound.Asked, "The way to the expedition screen is a button like any other (round 71 E2).");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            Assert.AreEqual(MusicTrack.Lobby, Managers.Sound.Track, "The expedition screen is the lobby's still.");
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Screen<ExpeditionScreen>(), "Frame/Details/Depart");
            CollectionAssert.AreEqual(new[] { SoundEffect.Button }, Managers.Sound.Asked, "The screen's [출발] opens the party popup (round 75 B): a click like any other.");
            yield return UiTestUtil.WaitForRedraw();
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Screen<ExpeditionScreen>(), "Frame/PartyWindow/PartyDepart");
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
            Assert.AreEqual(data.StartingMercenaries.Count, UiTestUtil.Views<RosterChipView>(lobby).Length, "A chip per mercenary on the strip (stage 26).");
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable, "An empty party no longer keeps the expedition screen shut: the party is formed there (round 75 B).");
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Gate").interactable);

            // Party: the first mercenaries of the roster, one per row from the front (the party popup's command; Round 81).
            RunState run = Managers.Run.Run;
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            Assert.AreEqual(data.StartingMercenaries.Count, entries.Length);
            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.FillParty(lobby);

            Assert.AreEqual(data.Balance.PartySize, run.Party.Count);
            Assert.IsFalse(Managers.Run.CanPlaceInParty(run.Roster[data.Balance.PartySize].Id, data.Balance.PartySize + 1), "The party is full.");

            // Lobby -> node map
            yield return UiTestUtil.DepartFromLobby();
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
            string settlementExp = UiTestUtil.TextAt(settlement, "Frame/Panel/Exp");
            string settlementReady = settlement.Ready.text;
            // Slice D stage 38 (Round 84 판정 7): a line for each survivor the expedition left traits on, its name and the traits' chips.
            Assert.AreEqual(report.NewTraits.Count, settlement.NewTraitRows.Count(row => row.gameObject.activeSelf));
            for (int i = 0; i < report.NewTraits.Count; i++)
            {
                StringAssert.Contains(UiText.Mercenary(report.NewTraits[i].MercenaryId), settlement.NewTraitLabels[i].text);
                CollectionAssert.AreEqual(report.NewTraits[i].TraitIds.Select(id => UiText.Name(data.Traits.Get(id).Name)).ToList(),
                    settlement.NewTraitLines[i].Shown.Select(c => c.Name).ToList());
                CollectionAssert.IsSubsetOf(report.NewTraits[i].TraitIds, RunRules.FindMercenary(run, report.NewTraits[i].MercenaryId).Traits, "The roster has them.");
            }

            yield return StagedNewTraits(settlement);
            UiTestUtil.Click(settlement, "Frame/Panel/Confirm");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);

            lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.IsFalse(Managers.Resource.IsScopeOpen(ResourceScope.Expedition), "The expedition's resources are released on return.");
            Assert.AreEqual(1 + data.Dungeons.Get(dungeonId).DurationDays, run.Day);
            Assert.AreEqual(run.Roster.Count, UiTestUtil.Views<RosterChipView>(lobby).Length, "The dead are gone from the strip.");
            yield return UiTestUtil.OpenBarracks(lobby);
            Assert.AreEqual(run.Roster.Count, UiTestUtil.RosterEntries(lobby).Length, "And from the barracks.");
            UiTestUtil.CloseQuarter(lobby);
            Assert.AreEqual(data.StartingMercenaries.Count - report.FallenIds.Count, run.Roster.Count);
            Assert.AreEqual(report.GoldAfter, run.Gold);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Gold, run.Gold), UiTestUtil.TextAt(lobby, "Frame/Header/Gold"), "The settlement's gold is in the header (stage 26).");
            if (report.SurvivorIds.Count > 0)
            {
                Assert.AreEqual(UiStrings.Get(UiKeys.Settle.Exp, report.ExpEarned), settlementExp, "The settlement said what experience the survivors got (stage 27).");
                // Slice D stage 35 (Round 82): one line names who can level up in the barracks; nothing is rolled here.
                Assert.AreEqual(report.ReadyToLevelUp.Count > 0, settlementReady.Length > 0);
                foreach (string id in report.ReadyToLevelUp)
                {
                    StringAssert.Contains(UiText.Mercenary(id), settlementReady);
                    Assert.IsTrue(Managers.Run.CanLevelUp(id), "And the barracks can level them up.");
                }
            }

            // A second expedition can depart, or the lobby says why not.
            if (Managers.Run.CanDepart(dungeonId) == DepartCheck.Ok)
            {
                yield return UiTestUtil.DepartFromLobby();
                Assert.AreEqual(2, run.ExpeditionCount);
            }
            else
            {
                Assert.AreEqual(!run.IsOver, UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable, "The expedition screen opens unless the run is over (round 75 B: a short party is filled there).");
            }
        }

        /// <summary>
        /// The expedition screen (round 71 E2, Docs/Design/04_Lobby_100Day_Economy.md §7): a marker per dungeon where the data puts it on
        /// the painted map, the locked one closed with its reason under it, the first open dungeon chosen with its details and the party
        /// as the lobby formed it, the way back to the lobby, and the departure with its own sound to the chosen dungeon's map.
        /// </summary>
        [UnityTest]
        public IEnumerator Expedition_ShowsEveryDungeonOnTheMap_KeepsTheLockedOneClosed_AndDepartsToTheChosenOne()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            StaticData data = Managers.Data.Data;
            DungeonData mine = data.Dungeons.Get("abandoned_mine");
            DungeonData abyss = data.Dungeons.Get("abyss");
            Assume.That(abyss.IsFinal && abyss.UnlockDungeonId == mine.Id, "The shipped data: clears of the mine open the final abyss.");

            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            Assert.AreEqual("원정 화면으로", UiTestUtil.TextAt(lobby, "Frame/Strip/Depart/DepartLabel"), "The lobby's button opens the screen; the departure is there.");
            yield return UiTestUtil.FillParty(lobby);
            UiTestUtil.Click(lobby, "Frame/Strip/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            ExpeditionScreen screen = UiTestUtil.Screen<ExpeditionScreen>();
            Assert.AreEqual(GamePhase.Lobby, Managers.Expedition.Phase, "Nothing has departed yet.");
            Assert.IsTrue(Managers.Resource.IsScopeOpen(ResourceScope.Lobby), "The screen and its map live in the lobby's scope.");
            Assert.IsNotNull(UiTestUtil.At(screen, "Frame/Map").GetComponent<Image>().sprite, "The painted map.");

            // One marker per dungeon, where the data puts it (percents of the map), named and stated.
            DungeonMarkerView[] markers = UiTestUtil.Views<DungeonMarkerView>(screen);
            Assert.AreEqual(data.Dungeons.Count, markers.Length, "The template stays hidden.");
            DungeonMarkerView mineMarker = markers.Single(m => m.DungeonId == mine.Id);
            DungeonMarkerView abyssMarker = markers.Single(m => m.DungeonId == abyss.Id);
            var expected = new Vector2(
                ExpeditionScreen.MapWidth * mine.MapX / 100f - DungeonMarkerView.DiscCenter.x,
                -(ExpeditionScreen.MapHeight * mine.MapY / 100f - DungeonMarkerView.DiscCenter.y));
            Assert.AreEqual(expected.x, mineMarker.Rect.anchoredPosition.x, 0.01f, "The disc's centre is at the data's point.");
            Assert.AreEqual(expected.y, mineMarker.Rect.anchoredPosition.y, 0.01f, "The disc's centre is at the data's point.");
            Assert.AreEqual("버려진 광산", mineMarker.Name);
            Assert.IsFalse(mineMarker.Locked);
            Assert.IsTrue(mineMarker.Selected, "The first open dungeon is chosen.");
            Assert.AreEqual($"클리어 0회 · {mine.DurationDays}일", UiTestUtil.TextAt(mineMarker, "MarkerPlate/MarkerDetail"));
            Assert.AreEqual("심연 (최종 던전)", abyssMarker.Name, "The final dungeon's marker says so.");
            Assert.IsTrue(abyssMarker.Locked);
            Assert.IsFalse(abyssMarker.Button.interactable, "A locked dungeon takes no click.");
            Assert.IsFalse(abyssMarker.Selected);
            Assert.AreEqual($"잠김 · 버려진 광산 {abyss.UnlockClears}회 클리어", UiTestUtil.TextAt(abyssMarker, "MarkerPlate/MarkerDetail"));

            // The chosen dungeon's details (rows of label and value, the hints under them), and the party as the lobby formed it.
            Assert.AreEqual("버려진 광산", UiTestUtil.TextAt(screen, "Frame/Details/Name"));
            Assert.AreEqual(UiText.Name(data.Affinities.Get(mine.AffinityId).Name), UiTestUtil.TextAt(screen, "Frame/Details/Affinity"));
            Assert.AreEqual($"{mine.Floors}층 + 보스", UiTestUtil.TextAt(screen, "Frame/Details/Floors"));
            Assert.AreEqual($"{mine.DurationDays}일 (→ {1 + mine.DurationDays}일차)", UiTestUtil.TextAt(screen, "Frame/Details/Days"));
            Assert.AreEqual("치유 포션 ×2", UiTestUtil.TextAt(screen, "Frame/Details/Potions"), "The potions by kind, with their count.");
            Assert.AreEqual("0회", UiTestUtil.TextAt(screen, "Frame/Details/Cleared"));
            Assert.AreEqual($"층마다 금 {data.Balance.GoldPerFloor} · 클리어 금 {data.Balance.GoldClear}", UiTestUtil.TextAt(screen, "Frame/Details/Reward"), "What the settlement pays (stage 26).");
            Assert.AreEqual("금 0", UiTestUtil.TextAt(screen, "Frame/Header/Gold"));
            Assert.AreEqual(
                $"심연 해금: 이 던전을 {abyss.UnlockClears}회 클리어\n{data.Balance.TotalDays}일을 넘기면 심연의 보스와 강제로 싸운다",
                UiTestUtil.TextAt(screen, "Frame/Details/Hint"),
                "What this dungeon's clears open, and the final stand past the last day.");
            RunState run = Managers.Run.Run;
            Assert.AreEqual($"파티 {run.Party.Count}명 · 출발할 수 있습니다", UiTestUtil.TextAt(screen, "Frame/Details/Status"), "Under the details: the party's size, then the status (round 77: no strip).");
            Assert.IsTrue(UiTestUtil.ButtonAt(screen, "Frame/Details/Depart").interactable);
            Assert.AreEqual("용병 목록", UiTestUtil.TextAt(screen, "Frame/Details/Roster/RosterLabel"), "The other way into the party popup.");
            Assert.IsTrue(UiTestUtil.ButtonAt(screen, "Frame/Details/Roster").interactable);

            // Back to the lobby and in again.
            UiTestUtil.Click(screen, "Frame/Header/ToLobby");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            Assert.AreEqual(GamePhase.Lobby, Managers.Expedition.Phase);
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/Strip/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            screen = UiTestUtil.Screen<ExpeditionScreen>();
            Assert.AreEqual(mine.Id, screen.SelectedDungeonId, "The choice is made here each time.");

            // The departure (round 75 B): [출발] opens the party popup over the map with a click; the popup's own [출발] sets out with its
            // own sound, to the chosen dungeon's map, the lobby's scope released.
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(screen, "Frame/Details/Depart");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(screen.IsPartyOpen);
            Assert.IsTrue(UiTestUtil.At(screen, "Frame/PartyWindow").gameObject.activeSelf);
            CollectionAssert.AreEqual(new[] { SoundEffect.Button }, Managers.Sound.Asked, "Opening the popup is a click.");
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(screen, "Frame/PartyWindow/PartyDepart");
            CollectionAssert.AreEqual(new[] { SoundEffect.Depart }, Managers.Sound.Asked);
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            Assert.AreEqual(mine.Id, Managers.Expedition.Expedition.DungeonId);
            Assert.IsFalse(Managers.Resource.IsScopeOpen(ResourceScope.Lobby), "The lobby's scope, with the map, goes with the departure.");
        }

        /// <summary>
        /// Round 75 B (2026-10-17, the user: the party is formed in a popup the expedition button opens) and Round 76 D (the party
        /// standing on a stage at the left as the node map stands it, the roster's list at the right): the lobby opens the screen with
        /// an empty party, [출발] opens the popup, the list's buttons put mercenaries in (the next row from the front) and take them
        /// out, the moves under the feet trade rows and those behind one taken out advance, closing keeps the party on the strip, and
        /// the popup's [출발] departs.
        /// </summary>
        [UnityTest]
        public IEnumerator Expedition_FormsThePartyInThePopup_OnTheStageAndFromTheList_AndDepartsFromIt()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            DungeonData mine = data.Dungeons.Get("abandoned_mine");
            Assume.That(data.StartingMercenaries.Count > balance.PartySize, "The shipped roster is larger than a party.");

            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RunState run = Managers.Run.Run;
            Assert.AreEqual(0, run.Party.Count);
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable, "An empty party no longer keeps the screen shut: the party is formed there.");
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Gate").interactable);
            UiTestUtil.Click(lobby, "Frame/Strip/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            ExpeditionScreen screen = UiTestUtil.Screen<ExpeditionScreen>();
            Assert.AreEqual($"파티 0명 · 파티에 용병을 {balance.MinPartySize}명 이상 넣으세요", UiTestUtil.TextAt(screen, "Frame/Details/Status"), "No strip any more (round 77): the party's size and the status under the details.");
            Assert.IsNull(UiTestUtil.At(screen, "Frame").Find("Bottom"), "The strip is gone.");
            Assert.IsTrue(UiTestUtil.ButtonAt(screen, "Frame/Details/Depart").interactable, "[출발] opens the popup even while the party is short.");
            Assert.IsTrue(UiTestUtil.ButtonAt(screen, "Frame/Details/Roster").interactable, "[용병 목록] opens it too.");
            Assert.IsFalse(screen.IsPartyOpen);
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/PartyWindow").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/Shade").gameObject.activeSelf);

            // [용병 목록] opens the popup to look and form; closing it comes back. [출발] opens the same popup.
            UiTestUtil.Click(screen, "Frame/Details/Roster");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(screen.IsPartyOpen, "[용병 목록] opens the party popup.");
            UiTestUtil.Click(screen, "Frame/PartyWindow/PartyClose");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(screen.IsPartyOpen);

            // The popup: the dungeon's line, the stage's rows (empty: the dim place, the hint, no moves), the roster's list and its room.
            UiTestUtil.Click(screen, "Frame/Details/Depart");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(screen.IsPartyOpen);
            Assert.IsTrue(UiTestUtil.At(screen, "Frame/Shade").gameObject.activeSelf, "The map is shaded under the popup.");
            Assert.AreEqual("원정 파티", UiTestUtil.TextAt(screen, "Frame/PartyWindow/PartyWindowTitle"));
            Assert.AreEqual($"버려진 광산 · {mine.DurationDays}일 (→ {1 + mine.DurationDays}일차)", UiTestUtil.TextAt(screen, "Frame/PartyWindow/PartyDungeon"));
            PartyRowSlotView[] slots = UiTestUtil.Views<PartyRowSlotView>(screen);
            Assert.AreEqual(balance.PartySize, slots.Length);
            Assert.IsTrue(slots.All(s => s.IsEmpty));
            Assert.AreEqual("비어 있음", UiTestUtil.TextAt(screen, "Frame/PartyWindow/Stage/PartySlot1/SlotEmpty1/SlotEmptyLabel1"));
            Assert.AreEqual("오른쪽 목록에서 넣으세요", slots[0].Detail);
            Assert.IsFalse(slots[0].Figure.gameObject.activeSelf, "No figure in an empty row.");
            Assert.IsFalse(slots[0].Forward.gameObject.activeSelf, "An empty row has no moves.");
            Assert.IsTrue(slots[0].Stats.All(t => t.text == string.Empty), "An empty row has no stats (Slice D stage 32).");
            Assert.AreEqual("1열", UiTestUtil.TextAt(screen, "Frame/PartyWindow/Stage/PartySlot1/SlotRowLabel1"));
            Assert.Less(
                UiTestUtil.At(screen, "Frame/PartyWindow/Stage/PartySlot4").GetComponent<RectTransform>().anchoredPosition.x,
                UiTestUtil.At(screen, "Frame/PartyWindow/Stage/PartySlot1").GetComponent<RectTransform>().anchoredPosition.x,
                "The stage stands the party as the node map does: 1열 at the right.");
            PartyListRowView[] rows = UiTestUtil.Views<PartyListRowView>(screen);
            Assert.AreEqual(run.Roster.Count, rows.Length, "A line per mercenary of the roster; the template stays hidden.");
            Assert.AreEqual(balance.RosterLimit - run.Roster.Count, screen.EmptySlotCount, "An empty line per place left in the roster.");
            Assert.AreEqual($"로스터 {run.Roster.Count}/{balance.RosterLimit} — 넣기로 비어 있는 맨 앞 열에 서고, 빼면 뒤가 전진한다", UiTestUtil.TextAt(screen, "Frame/PartyWindow/RosterHeader"));
            Assert.IsTrue(rows.All(r => !r.InParty && r.Row == string.Empty && r.FrameColor == UiPalette.PanelLight && r.ButtonLabel == "넣기" && r.Button.interactable));
            MercenaryState firstOfRoster = run.Roster[0];
            Assert.AreEqual(UiText.Mercenary(firstOfRoster.Id), rows[0].Name, "In the roster's order.");
            Assert.AreEqual(UiText.Job(firstOfRoster.JobId), rows[0].Job);
            Assert.AreEqual("Lv 1", rows[0].Level);
            // The traits where the passive stood (Slice D stage 37, Round 84): the job's trait first, then the mercenary's own, as the barracks lists them.
            List<TraitChip> traits = UiText.TraitChips(data, firstOfRoster.JobId, firstOfRoster.Traits);
            CollectionAssert.AreEqual(traits.Select(t => t.Name).ToList(), rows[0].Traits.Shown.Select(t => t.Name).ToList(), "The traits, as the barracks lists them.");
            Assert.AreEqual(UiText.Passive(data.Jobs.Get(firstOfRoster.JobId)), rows[0].Traits.Shown[0].Effect, "The job's trait is its passive.");
            Assert.IsNotNull(rows[0].Portrait, "The job's figure, small.");
            Assert.AreEqual(FatiguePips.Count, rows[0].Pips.Count);
            Assert.AreEqual($"파티에 용병을 {balance.MinPartySize}명 이상 넣으세요", UiTestUtil.TextAt(screen, "Frame/PartyWindow/PartyStatus"));
            Assert.IsFalse(UiTestUtil.ButtonAt(screen, "Frame/PartyWindow/PartyDepart").interactable, "Nobody to set out with.");

            // [넣기] puts its mercenary in the next row from the front; the line is lit with the row's tag and its button reads [빼기]; a full
            // party's [넣기] is off.
            for (int i = 0; i < balance.PartySize; i++)
            {
                UiTestUtil.Click(rows[i].Button);
                Assert.AreEqual(i + 1, run.Party.Single(s => s.MercenaryId == rows[i].MercenaryId).Row);
                Assert.IsTrue(rows[i].InParty);
                Assert.AreEqual($"{i + 1}열", rows[i].Row);
                Assert.AreEqual(UiPalette.Selected, rows[i].FrameColor);
                Assert.AreEqual("빼기", rows[i].ButtonLabel);
            }

            // The rows' figures and buttons were turned on by the clicks: they take a click (and a raycast) once the canvas has drawn them.
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(rows[0].MercenaryId, slots[0].MercenaryId);
            Assert.IsFalse(slots[0].IsEmpty);
            Assert.AreEqual(UiText.Mercenary(rows[0].MercenaryId), slots[0].Name);
            Assert.AreEqual(UiText.Job(firstOfRoster.JobId), slots[0].Detail);
            // The traits' icons after the job's words (Slice D stage 37): the line starts past them.
            Assert.AreEqual(1 + firstOfRoster.Traits.Count, slots[0].Traits.Shown.Count);
            TMP_Text detail = UiTestUtil.At(slots[0], "SlotDetail1").GetComponent<TMP_Text>();
            Assert.Greater(((RectTransform)slots[0].Traits.transform).anchoredPosition.x, detail.rectTransform.anchoredPosition.x + 30f, "The icons do not cover the job.");
            Assert.IsTrue(slots[0].Figure.gameObject.activeSelf);
            Assert.AreEqual(rows[0].Portrait, slots[0].Figure.Art, "The member's figure stands on the stage: the job's, the same art as the list's.");
            Assert.IsFalse(slots[0].Figure.ShowsPlaceholder);
            // The stage's card: the six stats in two columns between the job and the moves (Slice D stage 32, Round 81).
            MercenaryState standing = RunRules.FindMercenary(run, slots[0].MercenaryId);
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                Assert.AreEqual(UiText.StatPair(StatBlock.All[i], standing.Stats[StatBlock.All[i]]), slots[0].Stats[i].text);
            }

            Assert.Greater(
                -slots[0].Forward.GetComponent<RectTransform>().anchoredPosition.y,
                -slots[0].Stats[StatBlock.All.Length - 1].GetComponent<RectTransform>().anchoredPosition.y,
                "The moves stand under the stats.");

            // The pointer on a list line opens its stat tooltip at the line's left; leaving the line closes it.
            StatTooltipView tooltip = UiTestUtil.At(screen, "Frame/PartyWindow/StatTooltip").GetComponent<StatTooltipView>();
            Assert.IsFalse(tooltip.IsShown);
            MercenaryState benched = RunRules.FindMercenary(run, rows[balance.PartySize].MercenaryId);
            rows[balance.PartySize].OnPointerEnter(null);
            Assert.IsTrue(tooltip.IsShown);
            Assert.IsTrue(rows[balance.PartySize].Hovered, "The line's rim lights.");
            Assert.AreEqual(benched.Id, tooltip.MercenaryId);
            Assert.AreEqual($"{UiText.Mercenary(benched.Id)} · {UiText.Job(benched.JobId)} Lv {benched.Level}", tooltip.Title);
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                Assert.AreEqual(UiText.StatPair(StatBlock.All[i], benched.Stats[StatBlock.All[i]]), tooltip.Stats[i].text);
            }

            Vector3[] card = new Vector3[4];
            Vector3[] line = new Vector3[4];
            tooltip.GetComponent<RectTransform>().GetWorldCorners(card);
            rows[balance.PartySize].GetComponent<RectTransform>().GetWorldCorners(line);
            Assert.Less(card[2].x, line[0].x, "The card stands left of the line.");
            Assert.Less(card[0].y, (line[0].y + line[1].y) / 2f);
            Assert.Greater(card[1].y, (line[0].y + line[1].y) / 2f, "Centred on the line.");
            rows[balance.PartySize].OnPointerExit(null);
            Assert.IsFalse(tooltip.IsShown, "Leaving the line closes it.");
            Assert.IsFalse(rows[balance.PartySize].Hovered);

            Assert.IsTrue(slots[0].Forward.gameObject.activeSelf);
            Assert.IsFalse(slots[0].Forward.interactable, "The front row has no row before it.");
            Assert.IsTrue(slots[0].Back.interactable);
            Assert.IsTrue(slots[balance.PartySize - 1].Forward.interactable);
            Assert.IsFalse(slots[balance.PartySize - 1].Back.interactable, "The last row has nobody behind.");
            Assert.IsFalse(rows[balance.PartySize].InParty);
            Assert.AreEqual("넣기", rows[balance.PartySize].ButtonLabel);
            Assert.IsFalse(rows[balance.PartySize].Button.interactable, "The party is full: nobody more can join.");
            Assert.AreEqual("출발할 수 있습니다", UiTestUtil.TextAt(screen, "Frame/PartyWindow/PartyStatus"));
            Assert.IsTrue(UiTestUtil.ButtonAt(screen, "Frame/PartyWindow/PartyDepart").interactable);

            // The moves: back trades places with the row behind, forward with the row before; [빼기] takes one out and those behind advance.
            string first = rows[0].MercenaryId;
            string second = rows[1].MercenaryId;
            UiTestUtil.Click(slots[0].Back);
            Assert.AreEqual(2, run.Party.Single(s => s.MercenaryId == first).Row);
            Assert.AreEqual(1, run.Party.Single(s => s.MercenaryId == second).Row);
            Assert.AreEqual(UiText.Mercenary(second), slots[0].Name);
            Assert.AreEqual("1열", rows[1].Row);
            UiTestUtil.Click(slots[1].Forward);
            Assert.AreEqual(1, run.Party.Single(s => s.MercenaryId == first).Row);
            Assert.AreEqual(2, run.Party.Single(s => s.MercenaryId == second).Row);
            UiTestUtil.Click(rows[1].Button);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(run.Party.Any(s => s.MercenaryId == second));
            Assert.AreEqual(balance.PartySize - 1, run.Party.Count);
            Assert.AreEqual(UiText.Mercenary(rows[2].MercenaryId), slots[1].Name, "Those behind advanced a row.");
            Assert.IsTrue(slots[balance.PartySize - 1].IsEmpty);
            Assert.IsFalse(rows[1].InParty);
            Assert.AreEqual(string.Empty, rows[1].Row);
            Assert.AreEqual("넣기", rows[1].ButtonLabel);
            Assert.IsTrue(rows[balance.PartySize].Button.interactable, "A place opened: the others can join again.");
            Assert.AreEqual(balance.PartySize - 1 >= balance.MinPartySize, UiTestUtil.ButtonAt(screen, "Frame/PartyWindow/PartyDepart").interactable, "The departure follows the party's size.");

            // [빼기] on another; [넣기] puts one back at the back.
            UiTestUtil.Click(rows[0].Button);
            Assert.IsFalse(run.Party.Any(s => s.MercenaryId == first));
            UiTestUtil.Click(rows[0].Button);
            Assert.AreEqual(balance.PartySize - 1, run.Party.Single(s => s.MercenaryId == first).Row, "Back in, at the back.");
            UiTestUtil.Click(rows[1].Button);
            Assert.AreEqual(balance.PartySize, run.Party.Count);
            Assert.AreEqual(balance.PartySize, run.Party.Single(s => s.MercenaryId == second).Row);

            // Closing keeps the party and the status under the details counts it; [출발] opens the popup again, and its own [출발] sets out with the party as formed.
            UiTestUtil.Click(screen, "Frame/PartyWindow/PartyClose");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsFalse(screen.IsPartyOpen);
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/Shade").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(screen, "Frame/PartyWindow").gameObject.activeSelf);
            Assert.AreEqual($"파티 {balance.PartySize}명 · 출발할 수 있습니다", UiTestUtil.TextAt(screen, "Frame/Details/Status"));
            var formed = run.Party.OrderBy(s => s.Row).Select(s => s.MercenaryId).ToList();
            yield return UiTestUtil.DepartFromExpedition(screen);
            Assert.AreEqual(mine.Id, Managers.Expedition.Expedition.DungeonId);
            CollectionAssert.AreEqual(formed, Managers.Expedition.Expedition.Members.OrderBy(m => m.Row).Select(m => m.MercenaryId).ToList(), "The party as formed set out.");
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
            StaticData data = Managers.Data.Data;
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/RunOverWindow").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/Shade").gameObject.activeSelf);

            // Put the run into its end state directly; reaching it by play takes several lost expeditions. Two expeditions to the cave are counted.
            RunState run = Managers.Run.Run;
            foreach (MercenaryState mercenary in run.Roster.ToList())
            {
                run.Fallen.Add(mercenary.Id);
            }

            run.Roster.Clear();
            run.Party.Clear();
            run.Day = 11;
            run.ExpeditionCount = 2;
            run.ExpeditionsByDungeon[data.Dungeons.Ordered[0].Id] = 2;
            run.Outcome = RunOutcome.Defeat;
            run.DefeatCause = DefeatCause.Wipeout;
            lobby.Refresh();
            yield return null;

            // The result window (Round 72) over the shaded town: the title says how it ended, then the day, the expeditions by dungeon, the dead and the living.
            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/RunOverWindow").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/Shade").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.RunOver), UiTestUtil.TextAt(lobby, "Frame/RunOverWindow/RunOverTitle"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.DayValue, 11), UiTestUtil.TextAt(lobby, "Frame/RunOverWindow/RunOverDay"));
            string byDungeon = UiStrings.Get(UiKeys.Lobby.ExpeditionsOf, UiText.Name(data.Dungeons.Ordered[0].Name), 2);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.ExpeditionsValue, 2, byDungeon), UiTestUtil.TextAt(lobby, "Frame/RunOverWindow/RunOverExpeditions"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.FallenValue, data.StartingMercenaries.Count), UiTestUtil.TextAt(lobby, "Frame/RunOverWindow/RunOverFallen"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Common.None), UiTestUtil.TextAt(lobby, "Frame/RunOverWindow/RunOverSurvivors"));
            Assert.AreEqual(0, UiTestUtil.Views<RosterChipView>(lobby).Length);
            Assert.IsFalse(UiTestUtil.PointerReaches(UiTestUtil.ButtonAt(lobby, "Frame/Barracks")), "The shade takes the town's clicks.");

            UiTestUtil.Click(lobby, "Frame/RunOverWindow/RunOverNewRun");
            yield return null;
            yield return null;

            Assert.IsFalse(Managers.Run.Run.IsOver);
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/RunOverWindow").gameObject.activeSelf);
            Assert.AreEqual(data.StartingMercenaries.Count, UiTestUtil.Views<RosterChipView>(lobby).Length);
            yield return UiTestUtil.OpenBarracks(lobby);
            Assert.AreEqual(data.StartingMercenaries.Count, UiTestUtil.RosterEntries(lobby).Length);
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
        public IEnumerator Lobby_FatigueShowsAsTenPips_TheSecondLapRedOverTheFirst()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            BalanceData balance = Managers.Data.Data.Balance;
            Assume.That(balance.MaxFatigue, Is.EqualTo(200), "The cases below count pips of 10 a lap.");
            Assume.That(balance.FatigueBreakdown, Is.EqualTo(100));

            // A fresh mercenary: ten empty pips, violet words (the barracks' line; the strip's chip has the same pips).
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView fresh = UiTestUtil.RosterEntries(lobby)[0];
            Assert.AreEqual(10, fresh.FatiguePips.Count);
            Assert.IsTrue(fresh.FatiguePips.All(p => p.Ratio == 0f && p.OverRatio == 0f));
            Assert.AreEqual(UiPalette.Fatigue, fresh.Fatigue.color);

            // 128 (Round 95): the first lap fills all ten pips violet; the second lap's 28 lies over them in red, two full and one 8/10; the words red.
            RunRules.FindMercenary(Managers.Run.Run, Managers.Run.Run.Roster[0].Id).Fatigue = 128;
            lobby.Refresh();
            yield return null;
            RosterEntryView tired = UiTestUtil.RosterEntries(lobby)[0];
            Assert.IsTrue(tired.FatiguePips.All(p => p.Ratio == 1f && p.FillColor == UiPalette.FatigueBar));
            CollectionAssert.AreEqual(new[] { 1f, 1f, 0.8f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }, tired.FatiguePips.Select(p => Mathf.Round(p.OverRatio * 10f) / 10f));
            Assert.IsTrue(tired.FatiguePips.All(p => p.OverColor == UiPalette.FatigueDanger));
            Assert.AreEqual(UiPalette.FatigueDanger, tired.Fatigue.color);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Fatigue, 128, 200), tired.Fatigue.text);

            UiTestUtil.CloseQuarter(lobby);
            RosterChipView chip = UiTestUtil.Views<RosterChipView>(lobby)[0];
            Assert.IsTrue(chip.Pips.All(p => p.Ratio == 1f), "The strip's chip shows the same.");
            CollectionAssert.AreEqual(new[] { 1f, 1f, 0.8f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }, chip.Pips.Select(p => Mathf.Round(p.OverRatio * 10f) / 10f));
            Assert.IsTrue(chip.Pips.All(p => p.OverColor == UiPalette.FatigueDanger));
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

            RosterChipView[] chips = UiTestUtil.Views<RosterChipView>(lobby);
            Assert.AreEqual(UiText.JobLine(tired.JobId, fearful, true), chips[0].Job, "The strip's chip names the affliction after the job (stage 48: and the overwork, at 128).");
            Assert.AreEqual(UiPalette.FatigueDanger, UiTestUtil.At(chips[0], "ChipJob").GetComponent<TMP_Text>().color);
            Assert.AreEqual(UiText.Job(Managers.Run.Run.Roster[1].JobId), chips[1].Job);

            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            string words = UiStrings.Get(UiKeys.Lobby.Fatigue, 128, data.Balance.MaxFatigue);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.FatigueState, words, UiText.FatigueStateName(fearful)), entries[0].Fatigue.text);
            Assert.AreEqual(UiPalette.FatigueDanger, entries[0].Fatigue.color);
            Assert.AreEqual(UiText.Name(fearful.Description), entries[0].State.text);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Fatigue, 0, data.Balance.MaxFatigue), entries[1].Fatigue.text, "The others: the words alone.");
            Assert.AreEqual(string.Empty, entries[1].State.text);
        }

        /// <summary>
        /// The barracks' lines (Slice D stage 32, Round 81 안 B): no row buttons and no "remove" any more — the party is formed in the expedition
        /// screen's popup — and in their place the six stats, each name over its value, the value green above the baseline, red below it, white at it.
        /// </summary>
        [UnityTest]
        public IEnumerator Lobby_Barracks_ShowsTheSixStats_WhereTheRowButtonsWere()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RunState run = Managers.Run.Run;
            StaticData data = Managers.Data.Data;
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            Assert.AreEqual(run.Roster.Count, entries.Length);
            for (int e = 0; e < entries.Length; e++)
            {
                MercenaryState mercenary = run.Roster[e];
                Assert.AreEqual(StatRules.BaseOf(data, mercenary.JobId), mercenary.Stats, "A new run's roster has its jobs' base stats.");
                Assert.IsNull(entries[e].transform.Find("EntryRows"), "No row buttons in the barracks (Round 81).");
                Assert.IsNull(entries[e].transform.Find("EntryRemove"), "No \"remove\" in the barracks.");
                Assert.IsNotNull(entries[e].Dismiss, "Dismissing stays.");
                Assert.AreEqual(StatBlock.All.Length, entries[e].StatValues.Count);
                for (int i = 0; i < StatBlock.All.Length; i++)
                {
                    Stat stat = StatBlock.All[i];
                    int value = mercenary.Stats[stat];
                    Assert.AreEqual(UiText.StatName(stat), entries[e].StatNames[i].text);
                    Assert.AreEqual(value.ToString(), entries[e].StatValues[i].text);
                    // 2026-10-20 스탯 개정: the colour counts from the power baseline (5), the middle of a job's 1..8.
                    Color expected = value > data.Balance.PowerBaseline ? UiPalette.Good : value < data.Balance.PowerBaseline ? UiPalette.BloodLight : UiPalette.Text;
                    Assert.AreEqual(expected, entries[e].StatValues[i].color, $"{mercenary.Id} {stat} {value}");
                }
            }

            // The warrior's sheet in Korean (2026-10-20 스탯 개정: 힘 6 over the power baseline 5 green, 지식 3 red).
            int knight = run.Roster.FindIndex(m => m.JobId == "warrior");
            Assert.AreEqual("힘", entries[knight].StatNames[0].text);
            Assert.AreEqual(UiPalette.Good, entries[knight].StatValues[0].color);
            Assert.AreEqual(UiPalette.BloodLight, entries[knight].StatValues[3].color);
        }

        /// <summary>
        /// The traits (Slice D stage 37, Round 84 안 A and 안 1): each barracks line's trait line, where the passive stood — the job's trait first with
        /// its gold rim, then the mercenary's own, a strength green and a weakness red, each its letter and its name; a right click on a chip opens the
        /// window's card with the name, the kind and what it does (2026-10-19 사용자: on a right click, not under the pointer), the next press closes
        /// it; a line too short for them all ends with "+N", whose card names the rest. The stagecoach's candidates show theirs the same way.
        /// </summary>
        [UnityTest]
        public IEnumerator Lobby_Barracks_AndTheStagecoach_ShowTheTraits_AndTheirCards()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RunState run = Managers.Run.Run;
            StaticData data = Managers.Data.Data;
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            for (int e = 0; e < entries.Length; e++)
            {
                MercenaryState mercenary = run.Roster[e];
                List<TraitChip> expected = UiText.TraitChips(data, mercenary.JobId, mercenary.Traits);
                Assert.AreEqual(1 + mercenary.Traits.Count, expected.Count, "The job's trait and the mercenary's.");
                CollectionAssert.AreEqual(expected.Select(c => c.Name).ToList(), entries[e].Traits.Shown.Select(c => c.Name).ToList(), mercenary.Id);
                Assert.AreEqual(0, entries[e].Traits.Hidden.Count, "A line of the barracks holds a new run's traits.");
                Assert.AreEqual(UiPalette.TraitJob, entries[e].Traits.Chips[0].RimColor);
                Assert.AreEqual(UiText.Name(data.Jobs.Get(mercenary.JobId).PassiveName), entries[e].Traits.Chips[0].Name);
            }

            // 아스트리드 (the data: 강골, 서투름): the gold 광기, the green 강골, the red 서투름; the pointer on 서투름 opens its card.
            int astrid = run.Roster.FindIndex(m => m.Id == "astrid");
            TraitLineView line = entries[astrid].Traits;
            CollectionAssert.AreEqual(new[] { "광기", "강골", "서투름" }, line.Shown.Select(c => c.Name).ToList());
            Assert.AreEqual((UiPalette.TraitStrength, UiPalette.TraitWeakness), (line.Chips[1].RimColor, line.Chips[2].RimColor));
            Assert.AreEqual("서", line.Chips[2].Glyph);
            TraitTooltipView card = UiTestUtil.At(lobby, "Frame/BarracksWindow/BarracksTraitTooltip").GetComponent<TraitTooltipView>();
            Assert.IsFalse(card.IsShown);
            Assert.IsFalse(typeof(UnityEngine.EventSystems.IPointerEnterHandler).IsAssignableFrom(line.Chips[2].GetType()), "The pointer over a chip opens nothing.");
            RightClick(line.Chips[2]);
            Assert.IsTrue(card.IsShown);
            Assert.AreEqual(("서투름", "타고난 특성 · 약점", "명중 -5%p"), (card.Title, card.Kind, card.Effect));
            yield return null;
            Assert.IsTrue(card.IsShown, "It stays open until the next press.");
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(card.IsShown, "A press anywhere closes it.");
            RightClick(line.Chips[2]);
            yield return null;
            RightClick(line.Chips[0]);
            yield return null;
            Assert.IsTrue(card.IsShown, "A right click on another chip opens that one's.");
            Assert.AreEqual(("광기", "직업 특성 · 바이킹", UiText.Passive(data.Jobs.Get("viking"))), (card.Title, card.Kind, card.Effect));
            UiTestUtil.PressTheBackground();
            yield return null;

            // Too short a line: what does not fit is the pill, and its card names it.
            List<TraitChip> chips = UiText.TraitChips(data, "viking", new[] { "iron_bones", "clumsy", "frail", "timid" });
            line.Show(chips, 260f);
            Assert.Greater(line.Hidden.Count, 0);
            Assert.AreEqual(chips.Count, line.Shown.Count + line.Hidden.Count);
            Assert.IsTrue(line.More.gameObject.activeSelf);
            Assert.AreEqual("+" + line.Hidden.Count, line.More.Name);
            RightClick(line.More);
            Assert.AreEqual(UiStrings.Get(UiKeys.Trait.MoreTitle, line.Hidden.Count), card.Title);
            StringAssert.Contains(line.Hidden.Last().Name, card.Kind);
            yield return null;
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(card.IsShown);
            lobby.Refresh();
            yield return null;
            Assert.IsFalse(entries[astrid].Traits.More.gameObject.activeSelf, "Drawn again at full width: all fit.");

            // The stagecoach: each candidate's job trait and what it was born with.
            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Coach);
            for (int i = 0; i < run.Recruits.Count; i++)
            {
                RecruitState recruit = run.Recruits[i];
                Assert.LessOrEqual(recruit.Traits.Count, 2, "Born with 0~2.");
                CollectionAssert.AreEqual(UiText.TraitChips(data, recruit.JobId, recruit.Traits).Select(c => c.Name).ToList(),
                    lobby.Candidates[i].Traits.Shown.Select(c => c.Name).ToList(), recruit.Id);
                // A candidate is a fresh clone shown at once: its chips are as wide as their names (TMP's measure answered 0 there).
                Assert.Greater(((RectTransform)lobby.Candidates[i].Traits.Chips[0].transform).sizeDelta.x, 24f + TraitChipView.NameGap + 10f, recruit.Id);
            }
        }

        /// <summary>
        /// The settlement's "new traits" lines (Slice D stage 38, Round 84 판정 7), staged on the report: four survivors under the "ready" line, each
        /// line its words and its chips (a weakness red, a strength green); a right click opens the trait's card, which says what left it. The
        /// panel grows by each line and stays in the frame, [확인] under the last. Then the report as it was.
        /// </summary>
        IEnumerator StagedNewTraits(SettlementScreen settlement)
        {
            SettlementReport report = Managers.Expedition.Report;
            List<string> ready = report.ReadyToLevelUp;
            List<AcquiredTraits> newTraits = report.NewTraits;
            var panel = (RectTransform)UiTestUtil.At(settlement, "Frame/Panel").transform;
            var confirm = (RectTransform)UiTestUtil.At(settlement, "Frame/Panel/Confirm").transform;
            float builtHeight = 760f;

            report.ReadyToLevelUp = new List<string> { "astrid" };
            report.NewTraits = new List<AcquiredTraits>
            {
                new AcquiredTraits { MercenaryId = "astrid", TraitIds = new List<string> { "shaken", "scarred" } },
                new AcquiredTraits { MercenaryId = "rowan", TraitIds = new List<string> { "seasoned", "survivor", "hardened" } },
                new AcquiredTraits { MercenaryId = "ella", TraitIds = new List<string> { "shaken", "limp", "nightmares" } },
                new AcquiredTraits { MercenaryId = "mira", TraitIds = new List<string> { "seasoned" } },
            };
            settlement.Refresh();
            yield return null;

            Assert.IsTrue(settlement.NewTraitRows.All(row => row.gameObject.activeSelf));
            Assert.AreEqual(UiStrings.Get(UiKeys.Settle.NewTraits, "아스트리드"), settlement.NewTraitLabels[0].text);
            TraitLineView astrid = settlement.NewTraitLines[0];
            CollectionAssert.AreEqual(new[] { "패배의 기억", "흉터" }, astrid.Shown.Select(c => c.Name).ToList(), "The job's trait is no new trait.");
            Assert.AreEqual(UiPalette.TraitWeakness, astrid.Chips[1].RimColor);
            Assert.AreEqual(UiPalette.TraitStrength, settlement.NewTraitLines[1].Chips[0].RimColor);
            Assert.AreEqual(3, settlement.NewTraitLines[2].Shown.Count, "Three fit, the most one expedition leaves.");
            var label = settlement.NewTraitLabels[0].rectTransform;
            Assert.Greater(((RectTransform)astrid.transform).anchoredPosition.x, label.anchoredPosition.x + label.sizeDelta.x, "The chips come after the words.");
            for (int i = 1; i < settlement.NewTraitRows.Count; i++)
            {
                Assert.AreEqual(46f, settlement.NewTraitRows[i - 1].anchoredPosition.y - settlement.NewTraitRows[i].anchoredPosition.y, 0.01f, "One under another.");
            }

            Assert.AreEqual(builtHeight + 5 * 46f, panel.sizeDelta.y, 0.01f, "The ready line and four.");
            Assert.GreaterOrEqual(-panel.anchoredPosition.y, 0f);
            Assert.LessOrEqual(-panel.anchoredPosition.y + panel.sizeDelta.y, 1080f, "The panel stays in the frame.");
            Assert.Less(confirm.anchoredPosition.y, settlement.NewTraitRows[3].anchoredPosition.y - 34f, "[확인] under the last line.");

            TraitTooltipView card = UiTestUtil.At(settlement, "Frame/Panel/SettleTraitTooltip").GetComponent<TraitTooltipView>();
            RightClick(astrid.Chips[1]);
            Assert.IsTrue(card.IsShown);
            Assert.AreEqual(("흉터", "얻은 특성 · 약점 — 빈사에서 살아 돌아와", "최대 HP -5%"), (card.Title, card.Kind, card.Effect));
            yield return null;
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(card.IsShown);

            report.ReadyToLevelUp = ready;
            report.NewTraits = newTraits;
            settlement.Refresh();
            yield return null;
            Assert.AreEqual(newTraits.Count, settlement.NewTraitRows.Count(row => row.gameObject.activeSelf));
        }

        /// <summary>A right click on a trait's chip, as the test kit clicks: the press, then the click.</summary>
        static void RightClick(TraitChipBase chip)
        {
            UiTestUtil.PressTheBackground();
            chip.RightClickNow();
        }

        /// <summary>
        /// The barracks' level-up (Slice D stage 35, Round 82 안 A): a mercenary whose experience meets its level has a gold ▲ on the strip, the
        /// barracks' plate counts it, and its barracks line a gold bar with [▲ 레벨 업]. A click climbs one level (the rest carried), rolls the
        /// job's growth and saves; the line shows +N beside what grew under a gold rim until the window closes.
        /// </summary>
        [UnityTest]
        public IEnumerator Lobby_Barracks_LevelsUpWhoseExperienceMeetsTheLevel_ShowsTheRoll_AndTheLobbySaysWhoCan()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            RunState run = Managers.Run.Run;
            StaticData data = Managers.Data.Data;
            Assert.IsFalse(lobby.Barracks.CountShown, "Nobody can level up in a new run.");

            // Staged: the first of the roster came home with its level's experience and five to spare.
            MercenaryState first = run.Roster[0];
            first.Exp = LevelRules.ExpToNext(data, first) + 5;
            StatBlock before = first.Stats;
            lobby.Refresh();
            yield return null;

            RosterChipView[] chips = UiTestUtil.Views<RosterChipView>(lobby);
            Assert.AreEqual(UiText.ChipLevel(1, true), chips[0].Level, "A gold ▲ before the level on the strip.");
            StringAssert.Contains(UiStrings.Get(UiKeys.Lobby.LevelUpMark), chips[0].Level);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Level, 1), chips[1].Level);
            Assert.IsTrue(lobby.Barracks.CountShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.LevelUpCount, 1), lobby.Barracks.CountWords);

            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            Assert.IsTrue(entries[0].LevelUp.gameObject.activeSelf, "The button stands on the line whose experience meets the level.");
            Assert.IsFalse(entries[1].LevelUp.gameObject.activeSelf);
            Assert.AreEqual(UiPalette.StarLit, entries[0].Exp.color);
            Assert.IsFalse(entries[0].RimShown);

            UiTestUtil.Click(entries[0].LevelUp);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(2, first.Level);
            Assert.AreEqual(5, first.Exp, "The rest is carried.");
            Assert.IsFalse(Managers.Run.IsSaveBlocked, "Saved with the click.");
            entries = UiTestUtil.RosterEntries(lobby);
            Assert.IsFalse(entries[0].LevelUp.gameObject.activeSelf, "5 of the next level's: the button goes.");
            Assert.IsTrue(entries[0].RimShown, "The gold rim round the line just rolled.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Level, 2), entries[0].Level.text);
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                Stat stat = StatBlock.All[i];
                Assert.AreEqual(UiText.StatValue(first.Stats[stat], first.Stats[stat] - before[stat]), entries[0].StatValues[i].text, $"{stat}: the value with what grew.");
            }

            Assert.IsFalse(lobby.Barracks.CountShown, "Nobody left to level up.");

            // Closing the window takes the roll's marks away; the stats stay grown.
            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.OpenBarracks(lobby);
            entries = UiTestUtil.RosterEntries(lobby);
            Assert.IsFalse(entries[0].RimShown);
            Assert.AreEqual(first.Stats.Str.ToString(), entries[0].StatValues[0].text);
        }

        [UnityTest]
        public IEnumerator PartySide_ForwardAndBack_TradePlacesWithTheNextRow_AndTheColumnsFollowTheRows()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();
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

        /// <summary>
        /// A miss (Slice D stage 33, Round 81 판정 6): in a real battle of the shipped data an enemy's blow misses a mercenary at some point; the
        /// ticker's and the log's line says so with the hit chance and the roll, and nothing was dealt by that blow.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AMiss_IsALineOfTheLog_WithTheChanceAndTheRoll()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterALongFirstBattle();
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            int steps = 0;
            while (!engine.Events.Any(e => e.Kind == BattleEventKind.Missed))
            {
                Assert.Less(steps++, 600, "A minute of blows without a miss.");
                Managers.Expedition.AdvanceBattle(100);
            }

            BattleEvent miss = engine.Events.First(e => e.Kind == BattleEventKind.Missed);
            Assert.AreEqual(BattleSide.Enemy, miss.Source.Side, "The party carries nothing in this battle: an enemy missed.");
            Assert.GreaterOrEqual(miss.A, miss.B, "A miss is a roll at or over the chance.");
            string line = BattleLogText.Caption(miss, engine);
            Assert.AreEqual(UiStrings.Get(UiKeys.Log.Missed, BattleLogText.Source(engine, miss.Source, miss.Id), BattleLogText.UnitName(engine, miss.Target), UiText.Percent(miss.B), UiText.Percent(miss.A)), line);
            StringAssert.Contains("빗나감 (명중 " + UiText.Percent(miss.B) + "%", line);
            int index = engine.Events.ToList().IndexOf(miss);
            Assert.IsFalse(
                index + 1 < engine.Events.Count && engine.Events[index + 1].Kind == BattleEventKind.Damaged && engine.Events[index + 1].Target.Equals(miss.Target) && engine.Events[index + 1].Source.Equals(miss.Source),
                "Nothing was dealt by the blow that missed.");
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
            // The party's dead are named too (since Slice D stage 33 a miss may cost the party someone in this fight; before, nobody fell).
            string partyFallen = UiTestUtil.TextAt(screen, "Frame/LogPanel/LogBox/LogPartyFallen");
            BattleUnit[] partyDead = engine.Party.Where(u => !u.Alive).ToArray();
            if (partyDead.Length == 0)
            {
                Assert.IsEmpty(partyFallen, "Nobody of the party died.");
            }

            foreach (BattleUnit unit in partyDead)
            {
                StringAssert.Contains(UiText.Name(unit.Setup.Name), partyFallen);
            }

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
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();
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
            StringAssert.Contains(UiText.ItemTitle(expedition.Inventory[0]), party.InventoryInfo, "The held item in Diablo's tooltip.");
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
                expedition.Inventory.Add(new EquippedItem(data.Items.Get("buckler")));
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
            // Slice D stage 42: another board takes it only where its member can wield it (the priest's staff asks Wis 5: the mage's board, not the warrior's).
            PartyBoardView board2 = party.ColumnOfRow(RowOfAWielder(expedition, data.Items.Get(weapon), front.Row)).BoardView;
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
            grid.SquareAt(0, 0).RightClickNow();
            yield return null;
            Assert.IsTrue(party.Tooltip.IsShown, "The card of an item in the inventory.");
            map.PressInventoryKey();
            yield return null;
            Assert.IsFalse(party.InventoryPanel.activeSelf, "I closes it.");
            Assert.IsFalse(party.Tooltip.IsShown, "And the card with it, as after a win.");
        }

        [UnityTest]
        public IEnumerator PartySide_EquipmentShowsItsFatigue_OnItsCell_AndInAllOnTheHeadOfTheBoard()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();

            // Found on the way: a weapon and an armor cost fatigue, a support item does not; the base weapon never does.
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(string.Empty, row1.FatigueTotal, "Only the base weapon: nothing on the head.");
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("dagger")), 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("buckler")), 2, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("herb_pouch")), 0, 2);
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
            Assume.That(data.Balance.MaxFatigue, Is.EqualTo(200), "The cases below count pips of 10 a lap.");
            Assume.That(data.Balance.FatigueBreakdown, Is.EqualTo(100));
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(1);
            PartyColumnView row2 = party.ColumnOfRow(2);
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(10, row1.FatiguePips.Count);
            Assert.IsTrue(row1.FatiguePips.All(p => p.Ratio == 0f && p.OverRatio == 0f), "Fresh.");
            Assert.AreEqual(UiText.Job(front.JobId), row1.JobLine);
            Assert.IsFalse(row1.StateButton.interactable, "No state: the line takes no click.");

            // 128 and fearful (Round 95): ten full violet pips with two full red ones and one 8/10 over them; the job, then the affliction in red.
            FatigueStateData fearful = data.FatigueStates.Get("fearful");
            front.Fatigue = 128;
            front.StateId = fearful.Id;
            map.Refresh();
            yield return null;
            Assert.IsTrue(row1.FatiguePips.All(p => p.Ratio == 1f && p.FillColor == UiPalette.FatigueBar));
            CollectionAssert.AreEqual(new[] { 1f, 1f, 0.8f, 0f, 0f, 0f, 0f, 0f, 0f, 0f }, row1.FatiguePips.Select(p => Mathf.Round(p.OverRatio * 10f) / 10f));
            Assert.IsTrue(row1.FatiguePips.All(p => p.OverColor == UiPalette.FatigueDanger));
            Assert.AreEqual(UiText.JobLine(front.JobId, fearful, true), row1.JobLine, "Stage 48: at 128 the breakdown overworks.");
            StringAssert.Contains(UiText.FatigueStateName(fearful), row1.JobLine);
            StringAssert.Contains(ColorUtility.ToHtmlStringRGB(UiPalette.FatigueDanger), row1.JobLine);
            Assert.IsTrue(row1.StateButton.interactable);
            Assert.IsTrue(row2.FatiguePips.All(p => p.Ratio == 0f && p.OverRatio == 0f), "The others are fresh.");
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
            // Round 95: the first lap is a tenth of the threshold a pip, the second a tenth of what lies past it.
            float first = (float)balance.FatigueBreakdown / FatiguePips.Count;
            float second = (float)(balance.MaxFatigue - balance.FatigueBreakdown) / FatiguePips.Count;

            BattleUnitView[] units = UiTestUtil.Views<BattleUnitView>(battle);
            foreach (BattleUnitView unit in units)
            {
                Assert.AreEqual(unit.Unit.Side == BattleSide.Party, unit.PipsShown, "Only a mercenary has fatigue: " + unit.name);
                if (unit.Unit.Side == BattleSide.Party)
                {
                    int fatigue = unit.Unit.Fatigue;
                    Assert.AreEqual(Mathf.Min(fatigue, balance.FatigueBreakdown) / first, unit.FatiguePips.Sum(p => p.Ratio), 0.01f, "The violet lap holds the fatigue up to the threshold.");
                    Assert.AreEqual(Mathf.Max(0, fatigue - balance.FatigueBreakdown) / second, unit.FatiguePips.Sum(p => p.OverRatio), 0.01f, "The red lap holds what lies past it.");
                }
            }

            BattleEvent down = engine.Events.First(e => e.Kind == BattleEventKind.BrokeDown);
            FatigueStateData state = Managers.Data.Data.FatigueStates.Get(down.Id);
            bool virtue = state.Kind == FatigueStateKind.Virtue;
            BattleUnitView broken = units.Single(u => u.Unit.Ref.Equals(down.Target));
            Assert.AreEqual("viking", broken.Unit.Setup.JobId, "The viking, staged in row 2 behind the decoy at death's door, broke down.");

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
            // Stage 48: an affliction at the threshold overworks, and "· 과로" follows the name.
            Assert.AreEqual(UiText.FatigueStateLabel(state, FatigueRules.IsOverworked(Managers.Data.Data.Balance, state, broken.Unit.Fatigue)), broken.FatigueStateShown, "The state's name under its feet.");
            Assert.IsTrue(units.Where(u => u != broken).All(u => u.FatigueStateShown == string.Empty), "Nobody else is in a state.");
            Assert.IsTrue(battle.CaptionsPushed.Any(c => c.Contains(UiText.FatigueStateName(state))),
                "The breakdown reads as a caption (whether or not the captions of the same moment have pushed it out of the header since).");
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
            Assert.AreEqual(UiText.FatigueStateLabel(state, FatigueRules.IsOverworked(Managers.Data.Data.Balance, state, broken.Unit.Fatigue)), broken.FatigueStateShown);
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
            // Stage 48: the third line says the rest ends every mental breakdown.
            Assert.AreEqual($"HP {balance.CampHealPercent}% 회복\n피로도 -{balance.CampFatigueRelief}\n정신 붕괴 해제", UiTestUtil.TextAt(map, UiTestUtil.CampRest + "/RestBody"));
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
            Assert.IsNotNull(shop, "The map of the tests' run seed has a shop node (BootTestUtil.RunSeed).");

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
            // Ten across and eight down (round 56); rows are added when the goods need them (MerchantLayout): stage 46's catalog has more
            // 2x2 and tall pieces and five bags, so the run seed's shop may take a ninth row — the items end above an empty row, the bags
            // start at the bags' row (moved down when the items overflow their rows) and may take a second row.
            Assert.GreaterOrEqual(map.Merchant.Rows, MerchantGridView.Height, "Ten across and at least eight down (round 56).");
            int itemsEnd = 0;
            int bagStart = int.MaxValue;
            for (int i = 0; i < goodsCount; i++)
            {
                Placement at = map.Merchant.PlacementOf(i).Value;
                bool bag = stock[i].Kind == OfferKind.Bag;
                int width = bag ? data.Bags.Get(stock[i].Id).Width : data.Items.Get(stock[i].Id).Width;
                int height = bag ? data.Bags.Get(stock[i].Id).Height : data.Items.Get(stock[i].Id).Height;
                Assert.LessOrEqual(at.X + width, MerchantGridView.GoodsColumns, "The goods keep to the first eight columns; the ninth stays empty.");
                if (bag)
                {
                    bagStart = System.Math.Min(bagStart, at.Y);
                }
                else
                {
                    itemsEnd = System.Math.Max(itemsEnd, at.Y + height);
                }
            }

            for (int i = goodsCount; i < stock.Count; i++)
            {
                Assert.AreEqual(new Placement(MerchantGridView.PotionColumn, i - goodsCount), map.Merchant.PlacementOf(i).Value, "The potions down the last column, from the top.");
            }

            if (bagStart < int.MaxValue)
            {
                Assert.GreaterOrEqual(bagStart, MerchantGridView.BagRow, "The bags along the last row (round 58), or lower when the items overflow their rows.");
                Assert.LessOrEqual(itemsEnd, bagStart - 1, "An empty row between the items and the bags (round 58).");
            }
            else
            {
                Assert.LessOrEqual(itemsEnd, map.Merchant.Rows);
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

            // Round 79: a bag on offer shows its picture over a tile of its leather's colour, not the well (the shipped bags all have one).
            foreach (int bagSlot in Enumerable.Range(0, goodsCount).Where(i => stock[i].Kind == OfferKind.Bag))
            {
                ItemSlotView bagPiece = map.Merchant.PieceOf(bagSlot);
                Assert.IsNotNull(bagPiece.Icon, "The bag's picture.");
                Assert.AreEqual(UiPalette.BagRim(stock[bagSlot].Id), bagPiece.Ground, "On a tile of its leather's colour.");
            }

            // Round 57: the pointer over a good shows Diablo's tooltip over it (its facts without the grade, and its price), centred on its
            // piece; it takes no pointer, and goes when the pointer leaves. An item picked: its piece is the held one's lighter blue, and no
            // tooltip shows (it rides the pointer); no card opens; the panel says how to buy.
            // One no taller than the start bag's two free rows (Slice B stage 19), with an effect to read (not the whetstone, stage 20), and
            // one the row-1 member may wield (Slice D stage 42: the stat it asks for — stage 46 fills the shelves with gear for other stats).
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            int frontMember = party.ColumnOfRow(UiTestUtil.FlatRow()).Member;
            int slot = Enumerable.Range(0, stock.Count).Where(i => stock[i].Kind == OfferKind.Item && data.Items.Get(stock[i].Id).Height <= 2 && !data.Items.Get(stock[i].Id).IsPassive
                && ExpeditionRules.MeetsRequirement(Managers.Expedition.Expedition, frontMember, data.Items.Get(stock[i].Id))).DefaultIfEmpty(-1).First();
            Assert.GreaterOrEqual(slot, 0, "An item on offer fits the start bag's free rows and the front member's stats (BootTestUtil.RunSeed).");
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
            var offered = new EquippedItem(data.Items.Get(bought), tier: stock[slot].Tier);
            StringAssert.Contains(UiText.TooltipLines(offered), map.MerchantInfo, "Its facts in Diablo's tooltip.");
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

            // Round 74 B (2026-10-17): at the shop, the card of an item of the party's carries the sell button with what the shop pays (half
            // of its price at its tier); pressed, the coins come, the item goes and the card closes. The base weapon's card says it cannot be sold.
            UiTestUtil.RightClickSquare(row1.BoardView, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.Tooltip.IsShown, "The bought item's card.");
            Assert.IsTrue(party.Tooltip.SellShown);
            int sellPrice = Managers.Expedition.SellPriceOf(UiTestUtil.ItemAt(front, 0, 1));
            Assert.AreEqual(Mathf.Max(1, price * data.Balance.SellPercent / 100), sellPrice);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.Sell, sellPrice), party.Tooltip.SellLabel);
            Assert.IsTrue(party.Tooltip.SellButton.interactable);
            int coinsBeforeSale = Managers.Expedition.Expedition.Coins;
            UiTestUtil.Click(party.Tooltip.SellButton);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(coinsBeforeSale + sellPrice, Managers.Expedition.Expedition.Coins);
            Assert.IsNull(UiTestUtil.ItemAt(front, 0, 1), "Sold: gone from the board.");
            Assert.IsFalse(party.Tooltip.IsShown, "The card closed with the sale.");
            Assert.AreEqual((coinsBeforeSale + sellPrice).ToString(), UiTestUtil.TextAt(map, UiTestUtil.ShopBox + "/ShopCoins"));
            Assert.AreEqual((coinsBeforeSale + sellPrice).ToString(), UiTestUtil.TextAt(map, "Frame/Header/Coins"));
            UiTestUtil.RightClickSquare(row1.BoardView, 0, 0);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(party.Tooltip.IsShown, "The base weapon's card.");
            Assert.IsTrue(party.Tooltip.SellShown);
            Assert.IsFalse(party.Tooltip.SellButton.interactable, "The base weapon cannot be sold.");
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.CannotSell), party.Tooltip.SellLabel);
            PointerPress.Simulate();
            yield return null;
            Assert.IsFalse(party.Tooltip.IsShown, "A press elsewhere closes it.");

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
                bool bag = Managers.Expedition.ShopStock[shown].Kind == OfferKind.Bag;
                Assert.AreEqual(UiPalette.GridPieceUnaffordable, bag ? piece.Rim : piece.Ground, $"Slot {shown}: dark red (a bag: its rim).");
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
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("dagger"), tier: ItemTier.Tier1), 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("buckler"), tier: ItemTier.Tier2), 2, 1);
            map.Refresh();
            yield return null;

            PartyBoardView board = row1.BoardView;
            ItemSlotView weapon = board.PieceAt(0, 0);
            ItemSlotView daggerPiece = board.PieceAt(0, 1);
            ItemSlotView bucklerPiece = board.PieceAt(2, 1);
            Assert.IsNull(weapon.TierShown, "Common: the plain piece, no pips.");
            Assert.AreEqual(0, weapon.Pips);
            Assert.AreEqual(UiPalette.GridPiece, weapon.Ground, "Diablo's dark blue under an item between battles (round 49).");
            Assert.AreEqual(new Vector2Int(weapon.Item.Item.Width, weapon.Item.Item.Height), weapon.GroundSquares, "Square by square, the grid's line between them (round 53).");
            Assert.AreEqual(ItemTier.Tier1, daggerPiece.TierShown);
            Assert.AreEqual(1, daggerPiece.Pips);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Tier1), daggerPiece.PipColor, "The one colour of a tier (round 69): 1성 the magic blue.");
            Assert.AreEqual(ItemTier.Tier2, bucklerPiece.TierShown);
            Assert.AreEqual(2, bucklerPiece.Pips);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Tier2), bucklerPiece.PipColor, "2성 silver.");
            Assert.IsNull(board.PieceAt(0, 2), "An empty square.");
            Assert.AreEqual(string.Empty, daggerPiece.MergeMark, "Nothing merges into it.");

            // The held item's title names its tier.
            UiTestUtil.ClickSquare(board, 1, 1);
            yield return null;
            StringAssert.Contains(UiText.TierWord(ItemTier.Tier1), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"));
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
            Assert.AreEqual(ItemTier.Tier1, dagger.TierShown);
            Assert.AreEqual(1, dagger.Pips);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Tier1), dagger.PipColor);
            Assert.AreEqual(0f, dagger.Ground.a, "No blue behind an item on the battle screen (round 49).");
            Assert.AreEqual(ItemTier.Tier2, buckler.TierShown);
            Assert.AreEqual(2, buckler.Pips);
            Assert.AreEqual(UiPalette.Rarity(ItemTier.Tier2), buckler.PipColor);
            Assert.IsTrue(cells.Where(c => c.Mirrored).All(c => c.TierShown == null && c.Pips == 0));
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
            // A 2x1 weapon everyone wields (Slice D stage 42: the rusty blade asks Str 3; the dagger's Dex 5 neither the priest nor the warrior has).
            UiTestUtil.Put(first, new EquippedItem(data.Items.Get("rusty_blade")), 0, 1);
            UiTestUtil.Put(second, new EquippedItem(data.Items.Get("rusty_blade")), 1, 2);
            map.Refresh();
            yield return null;
            PartyBoardView board1 = row1.BoardView;
            PartyBoardView board2 = row2.BoardView;
            Assert.AreEqual(string.Empty, board2.PieceAt(1, 2).MergeMark, "Nothing is held.");

            // The row-1 dagger held: row 2's dagger is marked with the tier the merge makes, and the detail line says so.
            EquippedItem chosen = UiTestUtil.ItemAt(first, 0, 1);
            UiTestUtil.ClickSquare(board1, 0, 1);
            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Tier1)), board2.PieceAt(1, 2).MergeMark);
            Assert.AreEqual(ItemTier.Tier1, board2.PieceAt(1, 2).TierShown);
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
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierWord(ItemTier.Tier1)), board2.Grid.GhostLabel, "The ghost's label colours the tier word (round 69).");

            // Put there, they merge: one 1-star blade on row 2, where it lay; none on row 1.
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(board2, 1, 2);
            yield return null;
            EquippedItem merged = UiTestUtil.ItemAt(second, 1, 2);
            Assert.AreEqual(ItemTier.Tier1, merged.Tier);
            Assert.AreEqual(1, first.Board.Items.Count, "The dagger left row 1's board.");
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.AreEqual(ItemTier.Tier1, board2.PieceAt(1, 2).TierShown);
            Assert.AreEqual(string.Empty, board2.PieceAt(1, 2).MergeMark);
        }

        /// <summary>
        /// A bag on the party side (Slice B stage 19; round 79, 2026-10-17): drawn as Diablo's well, and once put down it stays: its empty
        /// square picks nothing up (as the start bag's never did), no frame shows, and the items in it still move.
        /// </summary>
        [UnityTest]
        public IEnumerator PartySide_ABagOnTheBoard_StaysPut_AndItsEmptySquarePicksNothing()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            front.Board.Bags.Add(new BoardBag(data.Bags.Get("leather_pouch"), new Placement(0, 3)));
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("rusty_blade")), 0, 3);   // a 2x1 weapon the priest wields (Slice D stage 42: Str 3)
            map.Refresh();
            yield return null;
            PartyBoardView board = row1.BoardView;
            Assert.AreEqual(2, board.Grid.BagsShown);
            Assert.AreEqual(2, board.Grid.GetComponentsInChildren<Image>().Count(i => i.name.EndsWith("BagTemplateWell")), "Each bag is Diablo's well (round 49): a rim round the grey of its squares.");
            Assert.AreEqual(GridSquareLook.Empty, board.Grid.SquareAt(2, 3).Look, "The pouch's empty square.");
            Assert.AreEqual(GridSquareLook.Hidden, board.Grid.SquareAt(0, 5).Look, "Outside every bag: nothing shows.");

            // Neither bag's empty square picks it up; the frame never shows for a placed bag.
            UiTestUtil.ClickSquare(board, 2, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "The start bag never moves.");
            UiTestUtil.ClickSquare(board, 2, 3);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Nor does a placed bag (round 79).");
            Assert.IsFalse(board.Grid.SquareAt(0, 5).ShowsDashes, "No frame: nothing is held.");
            Assert.AreEqual(new Placement(0, 3), front.Board.BagAt(1, 3).At);

            // The dagger in it still moves: picked by its square, put down two rows lower on the pack's free row.
            UiTestUtil.ClickSquare(board, 0, 3);
            yield return null;
            Assert.IsTrue(party.Hand.Holding);
            Assert.AreEqual(row1.Member, party.Hand.ItemMember);
            UiTestUtil.ClickSquare(board, 0, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding);
            Assert.IsNotNull(front.Board.ItemAt(0, 1), "The dagger moved up into the pack.");
            Assert.AreEqual(new Placement(0, 3), front.Board.BagAt(0, 3).At, "The pouch stayed.");
        }

        [UnityTest]
        public IEnumerator PartySide_AHeldItem_LeavesItsPlace_RidesThePointer_TheRightButtonSendsItBack_AndAPressElsewhereSendsItToTheInventory()
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

            // Escape lets go: it is back where it was (2026-10-19: the right button turns it again, as R does; on 2026-10-18 it let go).
            party.LetGo();
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Let go.");
            Assert.IsFalse(party.HeldPointer.Shown, "The pointer is back.");
            Assert.AreSame(weapon, front.Board.ItemAt(0, 0));
            Assert.IsNotNull(board.PieceAt(0, 0), "Back on its place.");
            Assert.IsEmpty(expedition.Inventory, "Nothing went to the inventory.");

            // A press on a potion's slot is no place for it (2026-10-18, the user: "이상한 곳에 클릭 시 인벤창으로 자동으로"): a board's item goes
            // into the inventory, at its first room, and the press does nothing more (no potion chosen).
            string potion = UiText.Name(data.Potions.Get(expedition.Potions.First(p => p != null)).Name);
            UiTestUtil.ClickSquare(board, 0, 0);
            yield return null;
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Views<PotionSlotView>(map).First(v => v.Icon != null).Button);
            yield return null;
            Assert.IsFalse(party.Hand.Holding, "Let go.");
            Assert.IsFalse(party.HeldPointer.Shown, "The pointer is back.");
            Assert.IsNull(front.Board.ItemAt(0, 0), "Off the board.");
            Assert.AreSame(weapon.Item, expedition.Inventory.Single(), "Into the inventory, at its first room.");
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked, "Put in, once.");
            StringAssert.DoesNotContain(potion, UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"), "The potion was not chosen.");

            // An item of the inventory pressed elsewhere is back where it lay (there is nowhere else for it to go).
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            Placement lay = expedition.Inventory.Items[0].At;
            UiTestUtil.ClickSquare(party.InventoryGrid, lay.X, lay.Y);
            yield return null;
            Assert.AreEqual(0, party.Hand.InventoryIndex);
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(UiTestUtil.Views<PotionSlotView>(map).First(v => v.Icon != null).Button);
            yield return null;
            Assert.IsFalse(party.Hand.Holding);
            Assert.AreEqual(lay, expedition.Inventory.Items[0].At, "Where it lay.");
            Assert.AreEqual(1, expedition.Inventory.Count);
            CollectionAssert.AreEqual(new[] { SoundEffect.Button }, Managers.Sound.Asked, "A click, once.");
        }

        [UnityTest]
        public IEnumerator PartySide_AHeldItem_OverTheSameItemInTheInventory_SaysMerge_AndMergesThere()
        {
            // 2026-10-18 (the user: "인벤창에서도 합치기 가능하게"): the inventory's grid merges as a board's does — the piece it would merge
            // into is marked, the ghost says the tier, and putting it there merges.
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow());
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ExpeditionMember first = expedition.Members[row1.Member];
            UiTestUtil.Put(first, new EquippedItem(data.Items.Get("dagger")), 0, 1);
            expedition.Inventory.Add(new EquippedItem(data.Items.Get("dagger")), new Placement(3, 1));
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return UiTestUtil.WaitForRedraw();
            map.Refresh();
            yield return null;
            PartyBoardView board1 = row1.BoardView;
            InventoryGridView grid = party.InventoryGrid;
            Assert.AreEqual(string.Empty, grid.PieceAt(3, 1).MergeMark, "Nothing is held.");

            // The board's dagger held: the inventory's dagger is marked with the tier the merge makes, and the card would say so.
            EquippedItem chosen = UiTestUtil.ItemAt(first, 0, 1);
            Assert.IsTrue(Managers.Expedition.HasMergeTarget(chosen), "The inventory holds a target.");
            UiTestUtil.ClickSquare(board1, 0, 1);
            yield return null;
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Tier1)), grid.PieceAt(3, 1).MergeMark);
            UiTestUtil.HoverSquare(grid, 3, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Merges, grid.GhostShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierWord(ItemTier.Tier1)), grid.GhostLabel);
            UiTestUtil.HoverSquare(grid, 7, 0);
            yield return null;
            Assert.AreEqual(GhostKind.Fits, grid.GhostShown, "A free place is just green.");
            Assert.AreEqual(string.Empty, grid.GhostLabel);

            // Put there, they merge: one Tier1 dagger in the inventory where it lay; none on the board.
            Managers.Sound.ForgetEffects();
            UiTestUtil.ClickSquare(grid, 3, 1);
            yield return null;
            Assert.IsFalse(party.Hand.Holding);
            Assert.AreEqual(1, expedition.Inventory.Count);
            Assert.AreEqual(ItemTier.Tier1, expedition.Inventory[0].Tier);
            Assert.AreEqual(new Placement(3, 1), expedition.Inventory.Items[0].At);
            Assert.IsNull(first.Board.ItemAt(0, 1), "The dagger left the board.");
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.AreEqual(ItemTier.Tier1, grid.PieceAt(3, 1).TierShown);
            Assert.AreEqual(string.Empty, grid.PieceAt(3, 1).MergeMark);
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
            // A 2x1 weapon everyone wields (Slice D stage 42: the rusty blade asks Str 3; the dagger's Dex 5 neither the priest nor the warrior has).
            UiTestUtil.Put(first, new EquippedItem(data.Items.Get("rusty_blade")), 0, 1);
            UiTestUtil.Put(second, new EquippedItem(data.Items.Get("rusty_blade")), 1, 2);
            map.Refresh();
            yield return null;
            PartyBoardView board1 = row1.BoardView;
            PartyBoardView board2 = row2.BoardView;
            ItemTooltipView card = party.Tooltip;
            Assert.IsFalse(card.IsShown, "No card before a click.");

            // Nothing held: a right click on any square of the row-1 dagger opens its card just above the panel, centred on its piece,
            // with its words.
            EquippedItem dagger = UiTestUtil.ItemAt(first, 0, 1);
            string mergeMark = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Tier1));
            UiTestUtil.RightClickSquare(board1, 1, 1);
            yield return null;
            Assert.IsTrue(card.IsShown);
            Assert.AreSame(dagger, card.Item);
            Assert.AreEqual(UiText.ItemTitle(dagger), card.Title);
            // The card counts its owner's stats (Slice D stage 32; 2026-10-20 Round 88 2차: no stat tail — the effects' values already count them, and they stand first).
            UiText.ItemCard(dagger, out string facts, out string effects, out string fatigue, 0, first.Stats);
            Assert.AreEqual(facts, UiTestUtil.TextAt(card, "ItemTooltipFacts"));
            Assert.AreEqual(effects, UiTestUtil.TextAt(card, "ItemTooltipEffects"));
            Assert.Less(UiTestUtil.At(card, "ItemTooltipEffects").GetSiblingIndex(), UiTestUtil.At(card, "ItemTooltipFacts").GetSiblingIndex(), "The effects stand above the facts (Round 88 2차).");
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
            Assert.AreEqual(ItemTier.Tier1, UiTestUtil.ItemAt(second, 1, 2).Tier);
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
            yield return EnterFirstBattle(champion: true);
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

            // Staged: the first drop is a 1-star rusty blade, and row 1 already holds a 1-star rusty blade under its weapon. The battle's boards gave
            // way to the node map's: the member's board stands in row 1's column with the weapon and the staged item on it.
            int index = 0;
            ItemOffer drop = new ItemOffer(OfferKind.Item, "rusty_blade", ItemTier.Tier1);   // a 2x1 weapon the front member wields (Slice D stage 42: Str 3)
            expedition.Loot[index] = drop;
            int flat = UiTestUtil.FlatRow();
            PartyBoardView row1 = battle.LootBoardOfRow(flat);
            int front = expedition.Members.FindIndex(m => m.Alive && m.Row == flat);
            UiTestUtil.Put(expedition.Members[front], new EquippedItem(data.Items.Get(drop.Id), tier: ItemTier.Tier1), 0, 1);
            battle.Refresh();
            yield return null;
            Assert.IsTrue(row1.gameObject.activeSelf);
            Assert.AreEqual(2, row1.Pieces.Count(), "The weapon and the staged item.");
            Assert.IsFalse(UiTestUtil.Views<BattleBoardView>(battle).Any(b => b.Unit.Side == BattleSide.Party && b.gameObject.activeSelf), "The battle's party boards are away.");
            LootDropView first = battle.Drops[index];
            StringAssert.Contains(UiText.TierWord(ItemTier.Tier1), first.Title);
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
            Assert.AreEqual(UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(ItemTier.Tier2)), row1.PieceAt(0, 1).MergeMark);
            UiTestUtil.HoverSquare(row1, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Merges, row1.Grid.GhostShown);
            UiTestUtil.ClickSquare(row1, 0, 1);
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(2, expedition.Members[front].Board.Items.Count, "The base weapon and the merge.");
            Assert.AreEqual(ItemTier.Tier2, UiTestUtil.ItemAt(expedition.Members[front], 0, 1).Tier);
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

        /// <summary>The A key after a win (2026-10-17): every drop left on the floor goes into the inventory and the loot ends, the screen staying for [계속].</summary>
        [UnityTest]
        public IEnumerator Loot_TheAKey_TakesEveryDropLeft_IntoTheInventory_AndEndsTheLoot()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle(champion: true);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            ExpeditionState expedition = Managers.Expedition.Expedition;
            Assert.AreEqual(ExpeditionPhase.PickingLoot, expedition.Phase);
            ItemOffer first = expedition.Loot[0];
            expedition.Loot.Add(new ItemOffer(OfferKind.Item, first.Id, first.Tier));
            yield return UiTestUtil.WaitForResult(battle);
            Assert.AreEqual(2, battle.Drops.Count);
            Assert.IsEmpty(expedition.Inventory.Items);
            int asked = Managers.Sound.Asked.Count;

            battle.PressTakeAllKey();
            yield return UiTestUtil.WaitForRedraw();

            Assert.AreEqual(2, expedition.Inventory.Count, "Both drops went in.");
            Assert.IsFalse(Managers.Expedition.LootOpen, "Nothing is left: the loot ended.");
            Assert.IsTrue(battle.Drops.All(d => !d.gameObject.activeSelf), "The floor is empty.");
            Assert.AreEqual(1, Managers.Sound.Asked.Skip(asked).Count(e => e == SoundEffect.ItemPlace), "One sound of a drop put in.");
            Assert.IsTrue(UiTestUtil.ButtonAt(battle, "Frame/BoardPanel/LootContinue").interactable, "The screen stays for [계속].");

            // Pressed again with nothing on the floor: only a click.
            asked = Managers.Sound.Asked.Count;
            battle.PressTakeAllKey();
            yield return null;
            Assert.IsFalse(Managers.Sound.Asked.Skip(asked).Contains(SoundEffect.ItemPlace));
        }

        [UnityTest]
        public IEnumerator Loot_AfterTheWin_ItemsMoveBetweenTheBoards_AndContinuingLeavesTheRest()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return EnterFirstBattle(champion: true);

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
            expedition.Loot.Add(new ItemOffer(OfferKind.Item, first.Id, first.Tier));
            yield return UiTestUtil.WaitForResult(battle);
            Assert.AreEqual(2, battle.Drops.Count, "A drop per slot, at the first two enemy columns.");
            Assert.Less(((RectTransform)battle.Drops[0].transform).anchoredPosition.x, ((RectTransform)battle.Drops[1].transform).anchoredPosition.x);

            // The boards work as on the node map while nothing is picked: row 1's weapon picked up (the hint tells it), put on row 2's
            // empty row, moved. A right click on an item opens its card above the panel.
            PartyBoardView row1 = battle.LootBoardOfRow(1);
            int front = expedition.Members.FindIndex(m => m.Alive && m.Row == 1);
            EquippedItem weapon = UiTestUtil.ItemAt(expedition.Members[front], 0, 0);
            // Slice D stage 42: the weapon goes to a member who can wield it — the viking's axe asks Str 5, which the warrior in row 3 has and the priest in row 2 has not.
            int secondRow = RowOfAWielder(expedition, weapon.Item, 1);
            PartyBoardView row2 = battle.LootBoardOfRow(secondRow);
            int second = expedition.Members.FindIndex(m => m.Alive && m.Row == secondRow);
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
            Assert.AreEqual(GhostKind.Fits, row2.Grid.GhostShown, "The wielder's empty row can take it.");
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
            yield return EnterFirstBattle(champion: true);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            while (!Managers.Expedition.Battle.IsFinished)
            {
                Managers.Expedition.AdvanceBattle(250);
            }

            yield return UiTestUtil.WaitForResult(battle);
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ItemOffer drop = expedition.Loot[0];
            Assert.AreEqual(OfferKind.Item, drop.Kind, "The first battle drops an item.");
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
            StringAssert.Contains(UiText.Change(UiText.TierWord(ItemTier.Common), UiText.TierWord(ItemTier.Tier1)), change);
            StringAssert.Contains(
                UiText.Change(UiText.Effect(effect, weapon, balance), UiText.EffectValue(effect, weapon.TierUp(), balance)),
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
            Assert.AreEqual(ItemTier.Tier1, UiTestUtil.ItemAt(front, 0, 0).Tier);
            Assert.IsTrue(UiTestUtil.ItemAt(front, 0, 0).IsBase);
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.IsFalse(UiTestUtil.At(map, "Frame/Map/Camp").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(map, "Frame/BoardPanel/InventoryToggle").gameObject.activeSelf);
            Assert.AreEqual(ItemTier.Tier1, board.PieceAt(0, 0).TierShown);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.NodeTitle, nodes.FloorCount, "보스"), UiTestUtil.TextAt(map, "Frame/BoardPanel/NodeHeading/NodeTitle"));
        }

        /// <summary>
        /// Stage 23 (round 70): with a 3-star longsword and a 3-star whetstone lit under it, the camp offers the forge as a third card
        /// (the three share the window), its step shows what the recipe makes, and forging puts the gladius in the longsword's place as the
        /// base weapon, with the whetstone gone, the camp spent, the gold rim on its piece and the plate head on its card.
        /// </summary>
        [UnityTest]
        public IEnumerator Camp_Forge_OffersTheRecipeAsAThirdCard_ShowsWhatItMakes_AndForgesTheUniqueInPlace()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMap nodes = Managers.Expedition.Expedition.Map;
            ExpeditionMember smith = Managers.Expedition.Expedition.Members.First(m => m.Board.ItemAt(0, 0)?.Item.Item.Id == "longsword");
            BoardItem sword = smith.Board.ItemAt(0, 0);
            sword.Item = new EquippedItem(sword.Item.Item, isBase: true, tier: ItemTier.Tier3);
            UiTestUtil.Put(smith, new EquippedItem(data.Items.Get("whetstone"), tier: ItemTier.Tier3), 0, 1);
            ItemData gladius = data.Items.Get("gladius");

            MapNode before = nodes.OnFloor(nodes.FloorCount - 2)[0];
            yield return StandOn(before);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(map.NodeView(before.NextNodeIds[0]).Button);
            yield return null;
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForRedraw();

            // Three cards: the forge card names the smith, the recipe and the gladius; rest and mend narrowed to share the window.
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampForge).gameObject.activeSelf, "A recipe is ready on a board.");
            Assert.AreEqual(CampCards.Width(3), ((RectTransform)UiTestUtil.At(map, UiTestUtil.CampRest)).sizeDelta.x, 0.5f);
            Assert.AreEqual(CampCards.Left(3, 2), ((RectTransform)UiTestUtil.At(map, UiTestUtil.CampForge)).anchoredPosition.x, 0.5f);
            string body = UiTestUtil.TextAt(map, UiTestUtil.CampForge + "/ForgeBody");
            StringAssert.Contains(UiText.Name(data.Mercenaries.Get(smith.MercenaryId).Name), body);
            StringAssert.Contains(UiText.Name(gladius.Name), body);

            // The forge step: the smith's sword is chosen already, the plate says what goes in and what comes out, the boards wait for a click.
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyBoardView board = party.ColumnOfRow(smith.Row).BoardView;
            UiTestUtil.Click(map, UiTestUtil.CampForge);
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampForge").gameObject.activeSelf);
            Assert.IsFalse(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampChoices").gameObject.activeSelf);
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.ForgeTitle, nodes.FloorCount - 1), UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampTitle"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Map.ForgeHint), UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampHint"));
            StringAssert.Contains(UiText.ItemTitle(sword.Item), UiTestUtil.TextAt(map, "Frame/BoardPanel/PartyDetail"), "The chosen sword's facts (the step's hint shows while nothing is chosen).");
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampBox + "/CampForge/ForgePlate").gameObject.activeSelf, "The first ready forge is chosen.");
            Assert.IsTrue(UiTestUtil.ButtonAt(map, UiTestUtil.CampBox + "/CampForge/ForgeConfirm").interactable);
            Assert.IsTrue(board.PieceAt(0, 0).IsPicked, "The sword is the chosen piece.");
            string change = UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampForge/ForgePlate/ForgeChange");
            StringAssert.Contains(UiText.ItemTitle(sword.Item), change);
            StringAssert.Contains(UiText.Name(gladius.Name), change);
            var result = new EquippedItem(gladius, isBase: true);
            StringAssert.Contains(UiText.Effect(gladius.Effects[0], result, data.Balance), UiTestUtil.TextAt(map, UiTestUtil.CampBox + "/CampForge/ForgePlate/ForgeEffects"));

            // Back leaves the step with the three cards; forge again and confirm.
            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampForge/ForgeBack");
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(UiTestUtil.At(map, UiTestUtil.CampForge).gameObject.activeSelf);
            UiTestUtil.Click(map, UiTestUtil.CampForge);
            yield return UiTestUtil.WaitForRedraw();
            Managers.Sound.ForgetEffects();
            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampForge/ForgeConfirm");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(GamePhase.NodeMap, Managers.Expedition.Phase, "The camp is spent.");
            EquippedItem made = UiTestUtil.ItemAt(smith, 0, 0);
            Assert.AreEqual("gladius", made.Item.Id);
            Assert.IsTrue(made.IsBase);
            Assert.AreEqual(ItemTier.Common, made.Tier, "A unique has no tier (round 70 follow-up 3).");
            Assert.IsNull(smith.Board.ItemAt(0, 1), "The whetstone was used up.");
            CollectionAssert.AreEqual(new[] { SoundEffect.ItemPlace }, Managers.Sound.Asked);
            Assert.IsTrue(board.PieceAt(0, 0).UniqueGlowShown, "The unique's gold glow behind its icon.");
            Assert.IsNull(board.PieceAt(0, 0).TierShown, "No pips: a unique has no tier.");

            // Its card: the name on the plate head, "유니크" under it, the one-per-party line in Diablo's tooltip words.
            UiTestUtil.RightClickSquare(board, 1, 0);
            yield return null;
            Assert.IsTrue(party.Tooltip.IsShown);
            Assert.IsTrue(party.Tooltip.PlateShown);
            Assert.AreEqual(UiText.Name(gladius.Name), party.Tooltip.Title);
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.UniqueTitle), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipUnique"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.UniqueLine), UiText.TooltipLines(made));
            UiTestUtil.PressTheBackground();
            yield return null;
            Assert.IsFalse(party.Tooltip.IsShown);
        }

        [UnityTest]
        public IEnumerator PartySide_AWhetstonesStars_ShowWhilePointedAtOrHeld_LitOnAMeleeWeapon_AndTheCardsSaySo()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow(melee: true));
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            BoardItem weapon = front.Board.ItemAt(0, 0);
            Assert.AreEqual(1, weapon.Height, "The staging needs a base weapon of one row.");
            Assert.IsTrue(weapon.Item.Item.Melee);

            // Slice B stage 20: a whetstone under the base weapon's left end, a herb pouch under the whetstone.
            var whetstone = new EquippedItem(data.Items.Get("whetstone"));
            UiTestUtil.Put(front, whetstone, 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("herb_pouch")), 0, 2);
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
            StringAssert.Contains(UiText.Effect(damage, weapon.Item, data.Balance, weapon.Item.Item.Category == ItemCategory.Weapon ? whetstone.StarDamage : 0, front.Stats), weaponLines, "The damage range counts the star's 1 on both ends, and the owner's stat.");
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarBonus, UiText.Name(whetstone.Item.Name), 1), weaponLines);
            UiTestUtil.RightClickSquare(board, 2, 1);
            yield return null;
            Assert.AreSame(whetstone, party.Tooltip.Item);
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarDamage, 1), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.StarLit, 1, 2, UiText.Name(weapon.Item.Item.Name)), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.NoActivation), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts"));
            Assert.AreEqual(0, FatigueRules.ItemCost(data.Balance, whetstone), "Not equipment: no fatigue.");
        }

        /// <summary>
        /// 2026-10-19 Round 85 (play feedback): in battle too, the pointer on a party's whetstone shows its stars over the board — lit on the
        /// melee weapon, hollow on the pouch — and they go when the pointer leaves; an item without stars shows none. The card stays a right click.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_AWhetstonesStars_ShowWhilePointedAt_AsOnTheNodeMap()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow(melee: true));
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(1, front.Board.ItemAt(0, 0).Height, "The staging needs a base weapon of one row.");
            var whetstone = new EquippedItem(data.Items.Get("whetstone"));
            UiTestUtil.Put(front, whetstone, 0, 1);
            UiTestUtil.Put(front, new EquippedItem(data.Items.Get("herb_pouch")), 0, 2);
            map.Refresh();
            while (Managers.Expedition.Phase != GamePhase.Battle)
            {
                yield return UiTestUtil.GoIntoTheFirstNode();
            }

            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            yield return null;
            BattleBoardView board = UiTestUtil.Views<BattleBoardView>(battle).Single(b => b.Unit.Setup.SourceId == front.MercenaryId);
            int stoneIndex = board.Unit.Items.ToList().FindIndex(i => i.Equipped == whetstone);
            int weaponIndex = board.Unit.Items.ToList().FindIndex(i => i.Equipped.Item.Melee);
            Assert.IsEmpty(board.Stars, "No stars while nothing is pointed at.");

            board.Items[stoneIndex].Hover.SimulateEnter();
            yield return null;
            Assert.AreEqual(2, board.Stars.Count());
            Assert.IsTrue(board.StarAt(0, 0).Lit, "On the base weapon: lit.");
            Assert.IsFalse(board.StarAt(0, 2).Lit, "On the herb pouch: hollow.");
            Assert.IsTrue(board.StarAt(0, 0).gameObject.activeInHierarchy);
            Assert.AreEqual(board.transform.Find("BoardCells").childCount - 1, board.StarAt(0, 0).transform.parent.GetSiblingIndex(), "The stars lie over the pieces.");

            board.Items[stoneIndex].Hover.SimulateExit();
            yield return null;
            Assert.IsEmpty(board.Stars, "Off the whetstone the stars go.");

            board.Items[weaponIndex].Hover.SimulateEnter();
            yield return null;
            Assert.IsEmpty(board.Stars, "A weapon has no stars of its own.");
            board.Items[weaponIndex].Hover.SimulateExit();
        }

        /// <summary>
        /// Slice D stage 39 (Round 87): the armour. On the node map the helm's card says it activates at the start of battle (no cooldown), what it
        /// takes off the critical chance and its fatigue; in battle both pieces fired once at 0 ms — their cells stay lit with no charge, their
        /// "방어 +N" rises once the screen is laid out, and the shield counts them.
        /// </summary>
        [UnityTest]
        public IEnumerator Armour_TheCardSaysWhenItActivates_AndTheBattleOpensWithItsShield()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            PartyColumnView row1 = party.ColumnOfRow(UiTestUtil.FlatRow(melee: true));
            ExpeditionMember front = Managers.Expedition.Expedition.Members[row1.Member];
            Assert.AreEqual(1, front.Board.ItemAt(0, 0).Height, "The staging needs a base weapon of one row.");
            var armour = new EquippedItem(data.Items.Get("iron_armor"));
            var helm = new EquippedItem(data.Items.Get("iron_helm"));
            UiTestUtil.Put(front, armour, 0, 1);
            UiTestUtil.Put(front, helm, 2, 1, turns: 1);
            map.Refresh();
            yield return null;

            UiTestUtil.RightClickSquare(row1.BoardView, 2, 1);
            yield return null;
            Assert.AreSame(helm, party.Tooltip.Item);
            string facts = UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts");
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.BattleStart), facts);
            StringAssert.DoesNotContain(UiStrings.Get(UiKeys.Item.Cooldown, UiText.RoundedSeconds(0)), facts, "No cooldown of 0 seconds.");
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.CritTaken, helm.Item.CritTakenPercent), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipEffects"));
            StringAssert.Contains(UiStrings.Get(UiKeys.Item.FatigueCost, 1), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFatigue"), "Defensive gear: fatigue +1.");
            UiTestUtil.PressTheBackground();
            yield return null;

            while (Managers.Expedition.Phase != GamePhase.Battle)
            {
                yield return UiTestUtil.GoIntoTheFirstNode();
            }

            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            BattleBoardView board = UiTestUtil.Views<BattleBoardView>(battle).Single(b => b.Unit.Setup.SourceId == front.MercenaryId);
            List<BattleItemState> items = board.Unit.Items.ToList();
            int armourIndex = items.FindIndex(i => i.Equipped == armour);
            int helmIndex = items.FindIndex(i => i.Equipped == helm);
            Assert.IsTrue(board.Unit.Shield >= armour.Item.Effects[0].Min + helm.Item.Effects[0].Min, "Both pieces shielded their owner at 0 ms.");
            BattleFxLayer fx = battle.GetComponentInChildren<BattleFxLayer>(true);
            for (int frame = 0; frame < 5 && fx.FloatingPlayed < 2; frame++)
            {
                yield return null;
            }

            Assert.GreaterOrEqual(fx.FloatingPlayed, 2, "The two pieces' \"방어 +N\" rose when the battle opened.");
            Assert.AreEqual(1f, board.Items[armourIndex].Charge, "The armour never charges: always lit.");
            Assert.AreEqual(1f, board.Items[helmIndex].Charge);
        }

        /// <summary>
        /// Equipment requirements on the screens (Slice D stage 42, Round 89 "권장안 구현"): the last fact of a piece of equipment is "요구 힘 N" — plain on the
        /// board of an owner who meets it, in the blood red on a card without an owner when nobody living of the party does; a held item's ghost over the
        /// board of a member who cannot wield it is refused with "힘 N 필요" and a click there lays nothing down, while a wielder's board refuses it only for
        /// want of squares, without those words.
        /// </summary>
        [UnityTest]
        public IEnumerator Requirement_TheCardSaysWhatTheItemAsks_AndTheGhostSaysWhyABoardRefusesIt()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return DepartToTheMap();
            StaticData data = Managers.Data.Data;
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            PartySideView party = map.GetComponentInChildren<PartySideView>();
            ExpeditionState expedition = Managers.Expedition.Expedition;
            ItemData halberd = data.Items.Get("halberd");
            Assume.That(halberd.ReqStat == Stat.Str && halberd.ReqValue == 7, "The shipped halberd asks Str 7.");
            ExpeditionMember warrior = expedition.Members.First(m => m.JobId == "warrior");
            ExpeditionMember priest = expedition.Members.First(m => m.JobId == "priest");
            ExpeditionMember viking = expedition.Members.First(m => m.JobId == "viking");
            Assume.That(warrior.Stats.Str < 7 && priest.Stats.Str < 7 && viking.Stats.Str >= 7, "The shipped base values: only the viking lifts the halberd.");

            // The warrior's longsword on his board: "요구 힘 5", plain, the last fact.
            EquippedItem longsword = UiTestUtil.ItemAt(warrior, 0, 0);
            string requires = UiStrings.Get(UiKeys.Item.Requires, UiText.StatName(Stat.Str), longsword.Item.ReqValue);
            UiTestUtil.RightClickSquare(party.ColumnOfRow(warrior.Row).BoardView, 0, 0);
            yield return null;
            Assert.AreSame(longsword, party.Tooltip.Item);
            string facts = UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts");
            StringAssert.EndsWith("<nobr>" + requires + "</nobr>", facts, "The requirement is the last fact.");
            StringAssert.DoesNotContain(UiText.Colored(requires, UiPalette.BloodLight), facts, "He wields it: plain.");
            UiTestUtil.PressTheBackground();
            yield return null;

            // A halberd in the inventory with nobody of the party able to lift it (the viking staged a point short): its card's line is red.
            viking.Stats = viking.Stats.With(Stat.Str, 6);
            expedition.Inventory.Add(new EquippedItem(halberd));
            map.Refresh();
            yield return null;
            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
            yield return null;
            InventoryGridView grid = party.InventoryGrid;
            Placement at = expedition.Inventory.Items[0].At;
            string asked = UiStrings.Get(UiKeys.Item.Requires, UiText.StatName(Stat.Str), halberd.ReqValue);
            grid.SquareAt(at.X, at.Y).RightClickNow();
            yield return null;
            Assert.IsTrue(party.Tooltip.IsShown);
            Assert.AreEqual(halberd, party.Tooltip.Item.Item);
            StringAssert.Contains(UiText.Colored(asked, UiPalette.BloodLight), UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts"), "Nobody of the party wields it: red.");
            UiTestUtil.PressTheBackground();
            yield return null;

            // With the viking at his base value again the same card is plain: someone of the party wields it.
            viking.Stats = viking.Stats.With(Stat.Str, 7);
            grid.SquareAt(at.X, at.Y).RightClickNow();
            yield return null;
            Assert.IsTrue(party.Tooltip.IsShown);
            StringAssert.Contains("<nobr>" + asked + "</nobr>", UiTestUtil.TextAt(party.Tooltip, "ItemTooltipFacts"), "The viking wields it: plain.");
            UiTestUtil.PressTheBackground();
            yield return null;

            // Held over the priest's board: refused, and the ghost says why; a click there lays nothing down. Over the viking's it is not the stat
            // that stands in the way (his axe does: it would be pushed out), so the words are not there.
            grid.SquareAt(at.X, at.Y).Click();
            yield return null;
            Assert.IsTrue(party.Hand.Holding);
            string needs = UiStrings.Get(UiKeys.Board.GhostRequires, UiText.StatName(Stat.Str), halberd.ReqValue);
            PartyBoardView priestBoard = party.ColumnOfRow(priest.Row).BoardView;
            UiTestUtil.HoverSquare(priestBoard, 0, 1);
            yield return null;
            Assert.AreEqual(GhostKind.Refused, priestBoard.Grid.GhostShown);
            Assert.AreEqual(needs, priestBoard.Grid.GhostLabel);
            UiTestUtil.ClickSquare(priestBoard, 0, 1);
            yield return null;
            Assert.IsTrue(party.Hand.Holding, "Refused: still held.");
            Assert.IsFalse(priest.Board.Items.Any(i => i.Item.Item == halberd), "Nothing landed on the priest's board.");
            PartyBoardView vikingBoard = party.ColumnOfRow(viking.Row).BoardView;
            UiTestUtil.HoverSquare(vikingBoard, 0, 0);
            yield return null;
            Assert.AreNotEqual(GhostKind.Refused, vikingBoard.Grid.GhostShown, "The viking wields it: his board takes it (pushing the axe out).");
            Assert.AreNotEqual(needs, vikingBoard.Grid.GhostLabel);
        }

        /// <summary>New run, the first mercenaries of the roster one per row, and depart: the node map before the first floor.</summary>
        /// <summary>The level and the experience (stage 27, Docs/Design/05_Mercenary_Growth.md §2): "Lv N" on the strip's chip, and on the barracks' line with the experience toward the next level and its bar; "경험치 최대" at the last level.</summary>
        [UnityTest]
        public IEnumerator Lobby_ShowsTheLevelAndExperience_OnTheChipAndTheBarracksLine()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            StaticData data = Managers.Data.Data;
            RunState run = Managers.Run.Run;
            Assume.That(data.MaxLevel, Is.GreaterThanOrEqualTo(3), "The cases below stage level 2 and the last level.");
            int toLeaveTwo = data.LevelAt(2).ExpToNext;

            MercenaryState second = run.Roster[0];
            second.Level = 2;
            second.Exp = 4;
            MercenaryState top = run.Roster[1];
            top.Level = data.MaxLevel;
            top.Exp = 0;
            lobby.Refresh();
            yield return null;

            RosterChipView[] chips = UiTestUtil.Views<RosterChipView>(lobby);
            Assert.AreEqual("Lv 2", chips[0].Level);
            Assert.AreEqual($"Lv {data.MaxLevel}", chips[1].Level);
            Assert.AreEqual("Lv 1", chips[2].Level);

            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            Assert.AreEqual("Lv 2", entries[0].Level.text);
            Assert.AreEqual($"경험치 4/{toLeaveTwo}", entries[0].Exp.text);
            Assert.AreEqual(4f / toLeaveTwo, entries[0].ExpBar.Ratio, 0.001f);
            Assert.AreEqual("경험치 최대", entries[1].Exp.text, "The last level takes nothing more.");
            Assert.AreEqual(1f, entries[1].ExpBar.Ratio, 0.001f);
            Assert.AreEqual($"경험치 0/{data.LevelAt(1).ExpToNext}", entries[2].Exp.text);
            Assert.AreEqual(0f, entries[2].ExpBar.Ratio, 0.001f);
        }

        /// <summary>
        /// The lobby as the town (Round 71, stage 26, Docs/Design/04_Lobby_100Day_Economy.md §3·§5·§6·§7): the painting with a plate per building, the
        /// pointer lighting a building and its bubble, the stagecoach's window where a candidate is hired for gold, the inn's where a mercenary is
        /// treated for gold without a day passing, the barracks' where one is dismissed; and past the last day the header, the gate's plate, the
        /// inn's plate and the strip's button all say "final stand" while the gate still opens the expedition screen (Round 72).
        /// </summary>
        [UnityTest]
        public IEnumerator Lobby_TheTown_LightsABuildingUnderThePointer_OpensItsWindows_HiresTreatsAndDismisses_AndSaysFinalStandPastTheLastDay()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            RunState run = Managers.Run.Run;
            Assume.That(balance.HirePrice, Is.EqualTo(20));
            Assume.That(balance.TreatmentPrice, Is.EqualTo(20));
            Assume.That(balance.TreatmentRelief, Is.EqualTo(40));

            // The town and its plates; nothing is lit, no window is open.
            Assert.IsNotNull(UiTestUtil.At(lobby, "Frame/Town").GetComponent<Image>().sprite, "The painted town.");
            Assert.AreEqual("막사  Lv 1", lobby.Barracks.Name, "The plate carries the level (stage 30).");
            Assert.AreEqual("쉼터  Lv 1", lobby.Inn.Name);
            Assert.AreEqual("역마차  Lv 1", lobby.Coach.Name);
            Assert.AreEqual("원정", lobby.Gate.Name, "The gate has no level (2026-10-18).");
            Assert.IsNull(lobby.Gate.Picture, "The gate is the arch painted on the town (2026-10-18): a hotspot without a picture.");
            Assert.AreEqual(LobbyScreen.GatePlot.Width, lobby.Gate.Rect.sizeDelta.x);
            foreach (BuildingView building in new[] { lobby.Barracks, lobby.Inn, lobby.Coach })
            {
                Assert.IsNotNull(building.Picture, "A picture of its own at its level (Round 78).");
            }

            foreach (BuildingView building in new[] { lobby.Barracks, lobby.Inn, lobby.Coach, lobby.Gate })
            {
                Assert.IsFalse(building.ScaffoldShown || building.BadgeShown, "Nothing is being built.");
            }

            Assert.Greater(lobby.Barracks.Rect.sizeDelta.x, lobby.Inn.Rect.sizeDelta.x, "The barracks' plot is the widest.");
            Assert.IsFalse(lobby.Barracks.IsLit || lobby.Inn.IsLit || lobby.Coach.IsLit || lobby.Gate.IsLit);
            Assert.IsFalse(lobby.BubbleShown);
            Assert.AreEqual(LobbyScreen.Quarter.None, lobby.OpenQuarter);
            Assert.AreEqual("금 0", UiTestUtil.TextAt(lobby, "Frame/Header/Gold"));
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.Day, 1, balance.TotalDays), UiTestUtil.TextAt(lobby, "Frame/Header/Day"));

            // The pointer over the stagecoach: the wash and the bubble with the candidates and the price.
            lobby.Coach.Hover();
            Assert.IsTrue(lobby.Coach.IsLit);
            Assert.IsTrue(lobby.BubbleShown);
            Assert.AreEqual("역마차  Lv 1", lobby.BubbleName);
            Assert.AreEqual($"후보 {run.Recruits.Count}명 · 고용 금 20", lobby.BubbleDetail, "No gold: no upgrade hint.");
            lobby.Coach.Leave();
            Assert.IsFalse(lobby.Coach.IsLit);
            Assert.IsFalse(lobby.BubbleShown);
            lobby.Gate.Hover();
            Assert.AreEqual("버려진 광산 · 심연 잠김", lobby.BubbleDetail, "The gate's bubble: the dungeons, the locked one saying so.");
            Assert.IsFalse(lobby.Gate.IsLit, "No picture, no wash: the plate's brass line alone says the pointer is on the gate.");
            lobby.Gate.Leave();

            // The stagecoach: three candidates, none affordable with no gold; with gold the first is hired and joins the roster and the strip.
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Coach);
            Assert.AreEqual(LobbyScreen.Quarter.Coach, lobby.OpenQuarter);
            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/Shade").gameObject.activeSelf);
            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/CoachWindow").gameObject.activeSelf);
            Assert.AreEqual(balance.RecruitCount, run.Recruits.Count);
            Assert.AreEqual(run.Recruits.Count, lobby.Candidates.Count);
            Assert.IsTrue(lobby.Candidates.All(c => !c.Hire.interactable), "No gold yet.");
            Assert.AreEqual(UiText.Mercenary(run.Recruits[0].Id), lobby.Candidates[0].Name);
            Assert.AreEqual(UiText.Job(run.Recruits[0].JobId), lobby.Candidates[0].Job);
            Assert.AreEqual("고용 금 20", UiTestUtil.TextAt(lobby.Candidates[0], "CandidateHire/CandidateHireLabel"));
            Assert.AreEqual($"Lv 1 · 피로도 0/{balance.MaxFatigue}", UiTestUtil.TextAt(lobby.Candidates[0], "CandidateDetail"), "A candidate is always level 1 and fresh.");
            // The stat line under the passive (Slice D stage 32, Round 81): the six pairs; a candidate's stats lie within the spread of its job's base
            // values (stage 34), and those that differ carry a small ▲/▼.
            StatBlock jobBase = StatRules.BaseOf(Managers.Data.Data, run.Recruits[0].JobId);
            int spread = Managers.Data.Data.Balance.RecruitStatSpread;
            Assert.IsTrue(StatBlock.All.All(s => System.Math.Abs(run.Recruits[0].Stats[s] - jobBase[s]) <= spread));
            foreach (Stat stat in StatBlock.All)
            {
                int delta = run.Recruits[0].Stats[stat] - jobBase[stat];
                if (delta != 0)
                {
                    StringAssert.Contains(UiStrings.Get(delta > 0 ? UiKeys.Stat.Up : UiKeys.Stat.Down, System.Math.Abs(delta)), lobby.Candidates[0].Stats.text);
                }
            }

            foreach (Stat stat in StatBlock.All)
            {
                StringAssert.Contains(UiText.StatPair(stat, run.Recruits[0].Stats[stat]), lobby.Candidates[0].Stats.text);
            }

            Assert.IsFalse(UiTestUtil.PointerReaches(UiTestUtil.ButtonAt(lobby, "Frame/Barracks")), "The shade takes the town's clicks while a window is open.");

            run.Gold = 100;
            lobby.Refresh();
            yield return UiTestUtil.WaitForRedraw();
            Assert.IsTrue(lobby.Candidates.All(c => c.Hire.interactable));
            string hiredId = run.Recruits[0].Id;
            string hiredJob = run.Recruits[0].JobId;
            StatBlock hiredStats = run.Recruits[0].Stats;
            int rosterBefore = run.Roster.Count;
            UiTestUtil.Click(lobby.Candidates[0].Hire);
            Assert.AreEqual(80, run.Gold);
            Assert.AreEqual(rosterBefore + 1, run.Roster.Count);
            Assert.AreEqual(hiredStats, run.Roster[run.Roster.Count - 1].Stats, "The hired one joins with the candidate's stats.");
            Assert.AreEqual(hiredJob, RunRules.FindMercenary(run, hiredId).JobId);
            Assert.AreEqual(balance.RecruitCount - 1, lobby.Candidates.Count, "The hired one left the coach.");
            Assert.AreEqual("금 80", UiTestUtil.TextAt(lobby, "Frame/Header/Gold"));
            Assert.AreEqual($"금 80 · 로스터 {run.Roster.Count}/8", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachSummary"));
            UiTestUtil.CloseQuarter(lobby);
            Assert.AreEqual(LobbyScreen.Quarter.None, lobby.OpenQuarter);
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/Shade").gameObject.activeSelf);
            RosterChipView[] chips = UiTestUtil.Views<RosterChipView>(lobby);
            Assert.AreEqual(run.Roster.Count, chips.Length);
            Assert.AreEqual(UiText.Mercenary(hiredId), chips[chips.Length - 1].Name, "The hired one is last on the strip.");
            Assert.AreEqual(UiText.Job(hiredJob), chips[chips.Length - 1].Job);
            Assert.AreEqual("Lv 1", chips[chips.Length - 1].Level, "Hired at level 1 (stage 27).");

            // The inn: a line per mercenary; a tired, afflicted one is treated for gold, no day passes, and the affliction ends under the threshold.
            MercenaryState tired = run.Roster[0];
            tired.Fatigue = 128;
            tired.AfflictionId = "fearful";
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Inn);
            Assert.AreEqual(LobbyScreen.Quarter.Inn, lobby.OpenQuarter);
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/InnWindow/Rest").interactable);
            PatientView[] patients = UiTestUtil.Views<PatientView>(lobby);
            Assert.AreEqual(run.Roster.Count, patients.Length);
            Assert.AreEqual(UiText.Mercenary(tired.Id), patients[0].Name);
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.FatigueState, UiStrings.Get(UiKeys.Lobby.Fatigue, 128, balance.MaxFatigue), UiText.FatigueStateName(data.FatigueStates.Get("fearful"))), patients[0].Fatigue.text);
            Assert.AreEqual("치료 금 20 · 피로 -40", UiTestUtil.TextAt(patients[0], "PatientTreat/PatientTreatLabel"));
            Assert.IsTrue(patients[0].Treat.interactable);
            Assert.IsFalse(patients[1].Treat.interactable, "Nothing to take off a fresh mercenary.");
            UiTestUtil.Click(patients[0].Treat);
            Assert.AreEqual(88, tired.Fatigue);
            Assert.IsNull(tired.AfflictionId);
            Assert.AreEqual(60, run.Gold);
            Assert.AreEqual(1, run.Day, "The treatment takes no day.");
            UiTestUtil.CloseQuarter(lobby);

            // The barracks: the last line (not in the party) is dismissed for free and leaves the roster, the strip and the file.
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            Assert.AreEqual(run.Roster.Count, entries.Length);
            Assert.IsTrue(entries.All(e => e.Dismiss.interactable), "Everyone can be dismissed while more than one is left.");
            string dismissedId = run.Roster[run.Roster.Count - 1].Id;
            UiTestUtil.Click(entries[entries.Length - 1].Dismiss);
            yield return null;
            Assert.AreEqual(60, run.Gold, "Dismissing is free.");
            Assert.IsNull(RunRules.FindMercenary(run, dismissedId));
            CollectionAssert.AreEqual(new[] { dismissedId }, run.Dismissed);
            Assert.AreEqual(run.Roster.Count, UiTestUtil.Views<RosterEntryView>(lobby).Length);
            StringAssert.Contains($"로스터 {run.Roster.Count}/8", UiTestUtil.TextAt(lobby, "Frame/BarracksWindow/BarracksSummary"));
            StringAssert.Contains("심연 잠김(버려진 광산 2회)", UiTestUtil.TextAt(lobby, "Frame/BarracksWindow/BarracksSummary"));
            UiTestUtil.CloseQuarter(lobby);
            Assert.AreEqual(run.Roster.Count, UiTestUtil.Views<RosterChipView>(lobby).Length);
            yield return UiTestUtil.FillParty(lobby);

            // Past the last day (staged): the header, the gate, the inn and the strip say so; resting is off, the gate still opens the expedition screen.
            run.Day = balance.TotalDays + 1;
            lobby.Refresh();
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(UiStrings.Get(UiKeys.Lobby.DayFinalStand, balance.TotalDays + 1, balance.TotalDays), UiTestUtil.TextAt(lobby, "Frame/Header/Day"));
            Assert.AreEqual(UiPalette.Virtue, UiTestUtil.At(lobby, "Frame/Header/Day").GetComponent<TMP_Text>().color);
            Assert.AreEqual("최종 결전", lobby.Gate.Name);
            Assert.AreEqual("심연의 보스와 싸운다", lobby.Gate.Sub);
            Assert.AreEqual("쉴 날이 없다", lobby.Inn.Sub);
            Assert.AreEqual("최종 결전으로", UiTestUtil.TextAt(lobby, "Frame/Strip/Depart/DepartLabel"));
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable);
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Inn);
            Assert.IsFalse(UiTestUtil.ButtonAt(lobby, "Frame/InnWindow/Rest").interactable, "No day is left to rest.");
            Assert.AreEqual("쉴 날이 없다", UiTestUtil.TextAt(lobby, "Frame/InnWindow/InnNote"));
            UiTestUtil.CloseQuarter(lobby);
            Assert.IsNull(lobby.transform.Find("Frame/GateWindow"), "The gate has no window (2026-10-18): it is the way out, nothing to upgrade.");
            lobby.Gate.Hover();
            Assert.AreEqual("심연의 보스와 싸운다", lobby.BubbleDetail);
            lobby.Gate.Leave();
            UiTestUtil.Click(lobby, "Frame/Gate");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            Assert.AreEqual("abyss", UiTestUtil.Screen<ExpeditionScreen>().SelectedDungeonId, "Only the final stand is left.");
        }

        [UnityTest]
        public IEnumerator Lobby_TheTown_UpgradesABuilding_ForGoldAndDays_LocksItWhileItIsBuilt_AndGrowsItWhenTheDaysPass()
        {
            // Stage 30 (Round 78): the stagecoach's window carries the building line; [업그레이드] pays 40 gold and puts the scaffold and the badge
            // on the stagecoach for 3 days, during which it does not hire; a gate being built keeps the party home; three days of rest finish both.
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            StaticData data = Managers.Data.Data;
            RunState run = Managers.Run.Run;
            Assume.That(data.BuildingAt(BuildingId.Coach, 2).Price, Is.EqualTo(40));
            Assume.That(data.BuildingAt(BuildingId.Coach, 2).Days, Is.EqualTo(3));
            run.Gold = 100;
            lobby.Refresh();
            yield return UiTestUtil.WaitForRedraw();

            lobby.Coach.Hover();
            Assert.AreEqual($"후보 {run.Recruits.Count}명 · 고용 금 20 · 업그레이드 금 40", lobby.BubbleDetail, "With the gold, the bubble names the upgrade.");
            lobby.Coach.Leave();

            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Coach);
            Assert.AreEqual("Lv 1", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachLine/CoachLevel"));
            Assert.AreEqual("Lv 1 → Lv 2: 후보 3명 → 4명 · 금 40 · 3일", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachLine/CoachEffect"));
            Assert.AreEqual("업그레이드 금 40 · 3일", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachLine/CoachUpgrade/CoachUpgradeLabel"));
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/CoachWindow/CoachLine/CoachUpgrade").interactable);
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/CoachWindow/CoachLocked").gameObject.activeSelf);
            UiTestUtil.Click(lobby, "Frame/CoachWindow/CoachLine/CoachUpgrade");
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(60, run.Gold);
            Assert.AreEqual(1, Managers.Run.BuildingLevel(BuildingId.Coach), "The level comes with the days.");
            Assert.AreEqual("Lv 1 → Lv 2 건설 중 · 3일 남음", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachLine/CoachEffect"));
            Assert.IsFalse(UiTestUtil.At(lobby, "Frame/CoachWindow/CoachLine/CoachUpgrade").gameObject.activeSelf, "No second upgrade while one is built.");
            Assert.IsTrue(UiTestUtil.At(lobby, "Frame/CoachWindow/CoachLocked").gameObject.activeSelf, "Closed while under construction.");
            Assert.IsTrue(lobby.Candidates.All(c => !c.Hire.interactable), "No hiring at a stagecoach being built.");
            Assert.AreEqual(HireCheck.CoachLocked, Managers.Run.CanHire(0));
            UiTestUtil.CloseQuarter(lobby);

            // The town: the scaffold and the badge on the stagecoach, its plate dimmed, its bubble saying the days.
            Assert.IsTrue(lobby.Coach.ScaffoldShown);
            Assert.IsTrue(lobby.Coach.BadgeShown);
            Assert.AreEqual("건설 중 · 3일 남음", lobby.Coach.BadgeWords);
            Assert.IsFalse(lobby.Barracks.ScaffoldShown);
            lobby.Coach.Hover();
            Assert.AreEqual("건설 중 · 3일 남음", lobby.BubbleDetail);
            lobby.Coach.Leave();

            // The gate is never built (2026-10-18): no scaffold, no level, and the way out stays open while the stagecoach is built.
            Assert.IsFalse(lobby.Gate.ScaffoldShown || lobby.Gate.BadgeShown);
            Assert.AreEqual("원정", lobby.Gate.Name);
            Assert.AreEqual(DepartCheck.PartyTooSmall, Managers.Run.CanDepart(data.Dungeons.Ordered[0].Id), "No construction locks the way out: only the party is still empty.");
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable);

            // Three days of rest at the inn (open while it is not the one being built): the badge counts down, then the stagecoach is done and grown.
            for (int day = 0; day < 3; day++)
            {
                yield return UiTestUtil.RestAtTheInn(lobby);
            }

            Assert.AreEqual(4, run.Day);
            Assert.AreEqual(2, Managers.Run.BuildingLevel(BuildingId.Coach));
            Assert.IsFalse(lobby.Coach.ScaffoldShown || lobby.Coach.BadgeShown || lobby.Gate.ScaffoldShown);
            Assert.AreEqual("역마차  Lv 2", lobby.Coach.Name);
            Assert.AreEqual("원정", lobby.Gate.Name);
            Assert.Greater(lobby.Coach.Rect.sizeDelta.x, LobbyScreen.Plots[BuildingId.Coach].Width * LobbyScreen.LevelScale(1), "The picture grew with the level.");
            Assert.IsTrue(UiTestUtil.ButtonAt(lobby, "Frame/Strip/Depart").interactable);
            Assert.AreEqual(HireCheck.Ok, Managers.Run.CanHire(0));
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Coach);
            Assert.AreEqual("Lv 2 → Lv 3: 후보 4명 → 5명 · 금 80 · 5일", UiTestUtil.TextAt(lobby, "Frame/CoachWindow/CoachLine/CoachEffect"));
            Assert.IsFalse(UiTestUtil.ButtonAt(lobby, "Frame/CoachWindow/CoachLine/CoachUpgrade").interactable, "60 gold does not cover level 3.");
            UiTestUtil.CloseQuarter(lobby);
        }

        /// <summary>
        /// The row of the first living member, other than one row, who can wield an item (Slice D stage 42, 장비 요구 스탯): where a test may lay it down.
        /// The shipped party of these tests stands astrid (viking), ella (priest), kai (warrior), mira (mage) in rows 1..4 (the roster in id order).
        /// </summary>
        static int RowOfAWielder(ExpeditionState expedition, ItemData item, int notRow)
        {
            return expedition.Members.First(m => m.Alive && m.Row != notRow && StatRules.MeetsRequirement(m.Stats, item)).Row;
        }

        static IEnumerator DepartToTheMap()
        {
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();
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
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();

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
                    ? data.Jobs.Get(unit.Unit.Setup.JobId).Figure
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

                // The board is a grid (Slice B stage 19): a mercenary's bag in leather (an enemy's the start bag in red stone, Round 86), and a
                // piece per item as big as its squares; each piece shows the icon its item's data names instead of the name; an enemy's icons are mirrored.
                Assert.AreEqual(1, board.BagsShown, id);
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
            cell.Bind(units[0].Unit.Items[0], cell.At, null, null, mirrored: false);
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

            // Past its grace, the chip says the chance the battle rolls: the base moved by a fatigue state (a reckless one dies more easily), by the
            // unit's constitution and by its traits (Slice D stage 37: a starting mercenary may be born iron-boned).
            StaticData data = Managers.Data.Data;
            FatigueStateData reckless = data.FatigueStates.Get("reckless");
            int chance = FatigueRules.DogDeathChancePercent(data.Balance, reckless, unit.Unit.Setup.Stats, unit.Unit.Setup.Traits);
            Assert.AreNotEqual(FatigueRules.DogDeathChancePercent(data.Balance, null, unit.Unit.Setup.Stats, unit.Unit.Setup.Traits), chance, "Staged so that the state moves the chance.");
            unit.Unit.State = reckless;
            unit.Unit.GraceEndMs = Managers.Expedition.Battle.Engine.TimeMs;
            yield return UiTestUtil.WaitForRedraw();
            Assert.AreEqual(UiStrings.Get(UiKeys.Battle.DogRolling, chance), UiTestUtil.TextAt(unit, "UnitMarks/UnitStates/UnitStatusChip/UnitStatus"));

            // Everyone else has neither the rim nor the state.
            foreach (BattleUnitView other in UiTestUtil.Views<BattleUnitView>(UiTestUtil.Screen<BattleScreen>()).Where(u => !u.Unit.InDog))
            {
                Assert.AreNotEqual(BattleUnitView.Rim.Danger, other.RimShown);
                Assert.IsFalse(UiTestUtil.At(other, "UnitMarks/UnitStates/UnitStatusChip").gameObject.activeSelf);
            }
        }

        /// <summary>
        /// Slice B stage 22 (Round 60 mock "A 빨강"): a weapon's critical hit rises in the damage red, larger, with the small word
        /// "치명타" over its number, and the log says so. Staged: row 1's weapon is a copy that always lands one.
        /// </summary>
        [UnityTest]
        public IEnumerator Battle_ACriticalHit_RisesAsItsNumberWithTheWord_AndTheLogSaysSo()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return UiTestUtil.FillParty(UiTestUtil.Screen<LobbyScreen>());
            yield return UiTestUtil.DepartFromLobby();

            ExpeditionMember front = Managers.Expedition.Expedition.Members.Single(m => m.Row == BattleRows.Front);
            BoardItem weapon = front.Board.Items[0];
            ItemData data = weapon.Item.Item;
            var sure = new ItemData(data.Id, data.Name, data.Category, data.Width, data.Height, data.CooldownMs, data.Rows, data.Effects, data.ShopWeight,
                data.Icon, data.Price, data.Melee, data.Stars, data.StarDamage, critChancePercent: 100);
            weapon.Item = new EquippedItem(sure, weapon.Item.IsBase, weapon.Item.Tier);

            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            BattleEngine engine = Managers.Expedition.Battle.Engine;
            UnitRef row1 = engine.Party.Single(u => u.Row == BattleRows.Front).Ref;
            System.Func<BattleEvent, bool> isRow1sCritical = e => e.Kind == BattleEventKind.CriticalHit && e.Source.Equals(row1);

            // Row 1's critical hit, whether or not an enemy's weapon landed one first (theirs are a matter of luck). Small steps, so that the
            // battle stops at the moment of it and its number is the last critical to rise.
            int guard = 0;
            while (!engine.Events.Any(isRow1sCritical) && !Managers.Expedition.Battle.IsFinished && guard++ < 1500)
            {
                Managers.Expedition.AdvanceBattle(20);
                yield return null;
            }

            yield return null;
            BattleEvent critical = engine.Events.FirstOrDefault(isRow1sCritical);
            Assert.IsNotNull(critical, "Row 1's weapon landed it.");
            Assert.Greater(battle.Fx.CriticalsPlayed, 0, "It rose as a critical hit.");
            StringAssert.Contains(UiStrings.Get(UiKeys.Fx.Crit), battle.Fx.LastCriticalText, "With the word over it.");
            BattleEvent damaged = engine.Events[engine.Events.ToList().IndexOf(critical) + 1];
            int lost = UiText.Amount(damaged.A - damaged.B);
            StringAssert.Contains(UiStrings.Get(UiKeys.Fx.CritDamage, lost), battle.Fx.LastCriticalText, "And its number with the mark.");
            Assert.IsNotNull(BattleLogText.Line(critical, engine), "The log says it was a critical hit.");
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

            // Every board shows its bag and its empty squares: a mercenary's start bag, and an enemy's (the start bag too, Round 86).
            foreach (BattleBoardView board in boards)
            {
                Assert.AreEqual(1, board.BagsShown, board.Unit.Setup.SourceId);
                Assert.Greater(board.EmptySquaresShown, 0, "The start bag has room besides the items.");
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
            // The poses are named after the figure, which a job may share with an older one (Slice D stage 36: the mage stands in the archmage's).
            string figureAddress = Managers.Data.Data.Jobs.Get(unit.Unit.Setup.JobId).Figure;
            string job = figureAddress.Substring(figureAddress.LastIndexOf('/') + 1);
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
        /// <param name="champion">Staged so that the battle is won (<see cref="UiTestUtil.StageChampion"/>), for a test that needs the win.</param>
        static IEnumerator EnterFirstBattle(bool champion = false)
        {
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            yield return UiTestUtil.FillParty(lobby);

            yield return UiTestUtil.DepartFromLobby();
            if (champion)
            {
                UiTestUtil.StageChampion();
            }

            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
        }
    }
}
