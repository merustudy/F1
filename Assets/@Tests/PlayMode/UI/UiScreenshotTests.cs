using System;
using System.Collections;
using System.Collections.Generic;
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

        /// <summary>
        /// The settlement with the traits an expedition left (Slice D stage 38), staged on the report for one picture as the Round 84 mock has it — 아스트리드
        /// home from a retreat with fatigue 64, ready to level up — with the lines given, then as it was.
        /// </summary>
        IEnumerator StagedNewTraits(SettlementScreen screen, string name, List<AcquiredTraits> newTraits)
        {
            SettlementReport report = Managers.Expedition.Report;
            ExpeditionResult result = report.Result;
            List<string> survivors = report.SurvivorIds, fallen = report.FallenIds, states = report.SurvivorStates, ready = report.ReadyToLevelUp;
            List<int> fatigue = report.SurvivorFatigue;
            List<AcquiredTraits> was = report.NewTraits;
            int exp = report.ExpEarned;

            List<string> home = newTraits.Select(n => n.MercenaryId).ToList();
            report.Result = ExpeditionResult.Retreated;
            report.SurvivorIds = home;
            report.FallenIds = new List<string>();
            report.SurvivorFatigue = home.Select(_ => 64).ToList();
            report.SurvivorStates = home.Select(_ => (string)null).ToList();
            report.ExpEarned = 20;
            report.ReadyToLevelUp = new List<string> { "astrid" };
            report.NewTraits = newTraits;
            screen.Refresh();
            yield return Capture(name);

            report.Result = result;
            report.SurvivorIds = survivors;
            report.FallenIds = fallen;
            report.SurvivorFatigue = fatigue;
            report.SurvivorStates = states;
            report.ExpEarned = exp;
            report.ReadyToLevelUp = ready;
            report.NewTraits = was;
            screen.Refresh();
            yield return null;
        }

        /// <summary>The settlement with survivors who can level up (Slice D stage 35, Round 82), staged on the report for one picture, then as it was.</summary>
        IEnumerator StagedReady(SettlementScreen screen, string name)
        {
            SettlementReport report = Managers.Expedition.Report;
            RunState run = Managers.Run.Run;
            ExpeditionResult result = report.Result;
            List<string> survivors = report.SurvivorIds, fallen = report.FallenIds, states = report.SurvivorStates, ready = report.ReadyToLevelUp;
            List<int> fatigue = report.SurvivorFatigue;
            int exp = report.ExpEarned;

            List<string> home = run.Roster.Take(2).Select(m => m.Id).ToList();
            report.Result = ExpeditionResult.Retreated;
            report.SurvivorIds = home;
            report.FallenIds = new List<string>();
            report.SurvivorFatigue = new List<int> { 64, 30 };
            report.SurvivorStates = new List<string> { null, null };
            report.ExpEarned = 20;
            report.ReadyToLevelUp = home;
            screen.Refresh();
            yield return Capture(name);

            report.Result = result;
            report.SurvivorIds = survivors;
            report.FallenIds = fallen;
            report.SurvivorFatigue = fatigue;
            report.SurvivorStates = states;
            report.ExpEarned = exp;
            report.ReadyToLevelUp = ready;
            screen.Refresh();
            yield return null;
        }

        /// <summary>
        /// The barracks' level-up (Slice D stage 35, Round 82 안 A), staged on the roster: the last four came home with their level's experience and
        /// some to spare, the last one is levelled up (the window open: the +N and the rim), then the town with the others still waiting (the strip's
        /// ▲ and the barracks' count). Then the roster as it was: levels, experience and stats.
        /// </summary>
        IEnumerator StagedLevelUp(LobbyScreen lobby, string prefix)
        {
            StaticData data = Managers.Data.Data;
            List<MercenaryState> roster = Managers.Run.Run.Roster.ToList();
            var kept = roster.Select(m => (m.Level, m.Exp, m.Stats)).ToList();
            foreach (MercenaryState mercenary in roster.Skip(Mathf.Max(0, roster.Count - 4)))
            {
                mercenary.Exp = LevelRules.ExpToNext(data, mercenary) + 5;
            }

            lobby.Refresh();
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(UiTestUtil.RosterEntries(lobby)[roster.Count - 1].LevelUp);
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture(prefix + "_73_barracks_levelup");
            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture(prefix + "_74_lobby_levelup_ready");

            for (int i = 0; i < roster.Count; i++)
            {
                roster[i].Level = kept[i].Level;
                roster[i].Exp = kept[i].Exp;
                roster[i].Stats = kept[i].Stats;
            }

            lobby.Refresh();
            yield return null;
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

        /// <summary>
        /// The boss battle as an enemy row empties: first the kill moment of the boss felled by Astrid's greataxe, a quarter of a
        /// second in (the two lit over the dark, the stage drawn in; Docs/Design/10 §5), then the battle right after it: who
        /// advanced, who fell and which items stopped.
        /// </summary>
        [UnityTest]
        public IEnumerator AfterAnAdvance_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachAnEnemyAdvanceInTheBossBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("ko_29_battle_kill_moment");
            yield return UiTestUtil.WaitForTheKillMoment(battle);
            yield return Capture("ko_15_battle_after_advance");
        }

        /// <summary>A battle at the moment a mercenary is at death's door: the plate that says so.</summary>
        [UnityTest]
        public IEnumerator DeathsDoor_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachDeathsDoorInTheFirstBattle();
            yield return Capture("ko_18_battle_deaths_door");
        }

        /// <summary>
        /// A battle a quarter of a second into the moment a mercenary (the valkyrie) broke down (round 38, B): the stage dark but her,
        /// drawn in on her, her state pose with the glow and the burst behind and the state's word over her head, the pips under her
        /// feet; then the same battle once the moment has gone, the state's name staying under her feet.
        /// </summary>
        [UnityTest]
        public IEnumerator Breakdown_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.ReachABreakdownInTheFirstBattle();
            yield return new WaitForSecondsRealtime(0.25f);
            yield return Capture("ko_38_battle_breakdown");
            yield return new WaitForSecondsRealtime(2.5f);
            yield return Capture("ko_39_battle_state");
        }

        /// <summary>The candle's light: a battle five seconds before the storm (the candle low, its light pulled in) and once the storm has put it out.</summary>
        [UnityTest]
        public IEnumerator TheStorm_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterALongFirstBattle();
            BalanceData balance = Managers.Expedition.Battle.Engine.Setup.Balance;
            Managers.Expedition.AdvanceBattle(balance.StormStartMs - 5000 - Managers.Expedition.Battle.Engine.TimeMs);
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_19_battle_storm_near");

            Managers.Expedition.AdvanceBattle(balance.StormStartMs + balance.StormTickMs - Managers.Expedition.Battle.Engine.TimeMs);
            yield return new WaitForSeconds(0.6f);
            yield return Capture("ko_20_battle_storm");
        }

        /// <summary>
        /// A battle a moment after the mercenary in row 1 fell: its grave where it stood, and those behind still in their
        /// places, waiting for it to go (2026-10-04, round 21); then the same battle once it has gone and they have walked on.
        /// </summary>
        [UnityTest]
        public IEnumerator AGrave_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterAFirstBattleWhereRow1Falls();
            UiTestUtil.Screen<BattleScreen>().Clock.SpeedPercent = 100;
            yield return null;
            UiTestUtil.AdvanceUntilFallen(Managers.Expedition.Battle.Engine.Party.Single(unit => unit.Row == BattleRows.Front));
            yield return new WaitForSeconds(0.3f);
            yield return Capture("ko_21_battle_grave");

            yield return new WaitForSeconds(1.2f);
            yield return Capture("ko_22_battle_after_grave");
        }

        /// <summary>
        /// The poses and the pulse in battle (Docs/Design/10 §5): the mercenary in row 1 lunging in its attack pose, the one in
        /// row 2 knocked back in its hit pose, and the one in row 3 swelling with a support item's light.
        /// </summary>
        [UnityTest]
        public IEnumerator PosesAndPulse_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterALongFirstBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return UiTestUtil.WaitForRedraw();
            BattleUnitView[] party = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Party)
                .OrderBy(view => view.Unit.Row).ToArray();

            party[0].Lunge(1f, withPose: true);
            yield return Capture("ko_23_battle_attack_pose");
            yield return new WaitForSeconds(0.5f);

            party[1].Recoil(-1f);
            yield return Capture("ko_24_battle_hit_pose");
            yield return new WaitForSeconds(0.5f);

            party[2].Pulse();
            yield return Capture("ko_25_battle_support_pulse");
            yield return new WaitForSeconds(0.5f);

            // Slice B stage 22 (Round 60 mock "A 빨강"): a critical hit's number over an enemy beside an ordinary one over row 1.
            BattleUnitView enemy = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Enemy).OrderBy(view => view.Unit.Row).First();
            battle.Fx.FloatCritical(enemy.FigureRect, UiStrings.Get(UiKeys.Fx.CritDamage, 5), UiStrings.Get(UiKeys.Fx.Crit), UiPalette.Danger);
            battle.Fx.Float(party[0].FigureRect, UiStrings.Get(UiKeys.Fx.Damage, 3), UiPalette.Danger, false);
            yield return new WaitForSeconds(0.25f);
            yield return Capture("ko_51_battle_critical");

            // Slice D stage 33 (Round 81 판정 6): a miss's grey words over the second enemy, and its line in the ticker.
            BattleUnitView[] enemies = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Enemy).OrderBy(view => view.Unit.Row).ToArray();
            BattleUnitView missed = enemies.Length > 1 ? enemies[1] : enemies[0];
            yield return new WaitForSeconds(1.5f);
            battle.Fx.Float(missed.FigureRect, UiStrings.Get(UiKeys.Fx.Miss), UiPalette.TextDim, false);
            yield return new WaitForSeconds(0.25f);
            yield return Capture("ko_71_battle_miss");
        }

        /// <summary>
        /// The monsters' poses in the boss battle (Docs/Design/10 §5): the overseer in row 1 lunging in its attack pose at 150%,
        /// its maul over the party; the shaman in the last row lunging with its lantern staff, drawn over the raider in front of
        /// it while it attacks; and the raider knocked back in its hit pose.
        /// </summary>
        [UnityTest]
        public IEnumerator MonsterPoses_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            yield return UiTestUtil.EnterTheBossBattle();
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            yield return UiTestUtil.WaitForRedraw();
            BattleUnitView[] enemies = UiTestUtil.Views<BattleUnitView>(battle).Where(view => view.Unit.Side == BattleSide.Enemy)
                .OrderBy(view => view.Unit.Row).ToArray();

            enemies[0].Lunge(-1f, withPose: true);
            yield return Capture("ko_26_battle_boss_attack_pose");
            yield return new WaitForSeconds(0.5f);

            enemies[enemies.Length - 1].Lunge(-1f, withPose: true);
            yield return Capture("ko_27_battle_back_row_attack");
            yield return new WaitForSeconds(0.5f);

            enemies[1].Recoil(1f);
            yield return Capture("ko_28_battle_enemy_hit_pose");
        }

        /// <summary>
        /// The final dungeon's boss battle (stage 28, round 73): the Abyss Lord at 150% between its two skeleton guards on the abyss's
        /// background, then the moment its HP falls to 75% and it takes up its first phase's items: "페이즈 1" over its head, its board
        /// re-bound with the cleaver and the howl, and the caption of the phase.
        /// </summary>
        [UnityTest]
        public IEnumerator AbyssBoss_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            // The champion fights the boss with a strong but not instant weapon (600%), so that the lord crosses 75% over a few blows and
            // nobody needs luck; the engine is advanced by hand in small steps and stopped at the phase's own event, young enough for
            // the presenter to play it (RecentMs).
            yield return UiTestUtil.EnterTheAbyssBossBattle(600);
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_63_battle_abyss_boss");

            BattleEngine engine = Managers.Expedition.Battle.Engine;
            BattleUnit lord = engine.Enemies.Single(e => e.Setup.SourceId == "abyss_lord");
            int guard = 0;
            while (!engine.Events.Any(e => e.Kind == BattleEventKind.PhaseChanged))
            {
                Assert.Less(++guard, 2000, "The lord never reached its first phase.");
                Assert.IsFalse(Managers.Expedition.Battle.IsFinished, "The battle ended before the lord's first phase.");
                Managers.Expedition.AdvanceBattle(50);
            }

            Assert.AreEqual(1, lord.Phase);
            yield return null;
            yield return null;
            yield return Capture("ko_64_battle_abyss_phase");
        }

        /// <summary>
        /// The unique in battle (stage 23, round 70 follow-up 2): the gladius in the knight's place, its gold glow whole behind the icon
        /// while the item charges (half lit, a second and a half in): the glow lies outside the charge mask.
        /// </summary>
        [UnityTest]
        public IEnumerator TheUniqueInBattle_Korean()
        {
            yield return UiTestUtil.BootToTitle(_saveRoot, "ko-KR");
            StaticData data = Managers.Data.Data;
            UiTestUtil.Click(UiTestUtil.Screen<TitleScreen>(), "Frame/Buttons/NewRun");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            LobbyScreen lobby = UiTestUtil.Screen<LobbyScreen>();
            string[] ids = { "rowan", "kai", "ella", "mira" };
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            for (int i = 0; i < Mathf.Min(ids.Length, data.Balance.PartySize); i++)
            {
                int index = Managers.Run.Run.Roster.FindIndex(m => m.Id == ids[i]);
                UiTestUtil.PlaceInParty(lobby, Managers.Run.Run.Roster[index].Id, i + 1);
            }

            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.DepartFromLobby();

            // The row-1 weapon (the knight's longsword, 3x1) becomes the gladius, as the forge would leave it (no tier).
            ExpeditionMember front = Managers.Expedition.Expedition.Members.OrderBy(m => m.Row).First();
            BoardItem weapon = front.Board.ItemAt(0, 0);
            weapon.Item = new EquippedItem(data.Items.Get("gladius"), isBase: true);
            // Round 85: a whetstone under the gladius' right end, for the stars while it is pointed at (below).
            var whetstone = new EquippedItem(data.Items.Get("whetstone"));
            UiTestUtil.Put(front, whetstone, 2, 1);
            while (Managers.Expedition.Phase != GamePhase.Battle)
            {
                yield return UiTestUtil.GoIntoTheFirstNode();
            }

            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            Managers.Expedition.AdvanceBattle(1500 - Managers.Expedition.Battle.Engine.TimeMs);
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_55_battle_unique");

            // 2026-10-19 Round 85: the pointer on the whetstone in battle — its stars over the board, lit on the gladius, hollow below.
            BattleBoardView board = UiTestUtil.Views<BattleBoardView>(battle).Single(b => b.Unit.Setup.SourceId == front.MercenaryId);
            BattleItemView stone = board.Items[board.Unit.Items.ToList().FindIndex(i => i.Equipped == whetstone)];
            stone.Hover.SimulateEnter();
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture("ko_76_battle_star_hover");
            stone.Hover.SimulateExit();
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

            // The town (stage 26): the stagecoach under the pointer with its bubble, then the barracks window where the party is formed:
            // rowan and kai (warriors), ella (priest), mira (mage) from row 1 back (Slice D stage 36: the five jobs; the ranger comes from the stagecoach). One mercenary is staged at level 2
            // with some experience for the pictures (stage 27) and put back before the party leaves.
            MercenaryState leveled = Managers.Run.Run.Roster[0];
            leveled.Level = 2;
            leveled.Exp = 4;
            lobby.Refresh();
            // The town's buildings (stage 30, Round 78): with gold staged, the barracks is put under construction (the scaffold, the badge, the
            // bubble) and stays so for the lap: it is done when the party comes home (3 days against the mine's 5), and the lobby after shows it grown.
            Managers.Run.Run.Gold = 120;
            lobby.Refresh();
            Managers.Run.Upgrade(BuildingId.Barracks);
            lobby.Refresh();
            lobby.Barracks.Hover();
            yield return Capture(prefix + "_67_lobby_construction");
            lobby.Barracks.Leave();
            lobby.Coach.Hover();
            yield return Capture(prefix + "_61_lobby_hover");
            lobby.Coach.Leave();
            string[] ids = { "rowan", "kai", "ella", "mira" };
            yield return UiTestUtil.OpenBarracks(lobby);
            RosterEntryView[] entries = UiTestUtil.RosterEntries(lobby);
            for (int i = 0; i < Mathf.Min(ids.Length, data.Balance.PartySize); i++)
            {
                int index = Managers.Run.Run.Roster.FindIndex(m => m.Id == ids[i]);
                UiTestUtil.PlaceInParty(lobby, Managers.Run.Run.Roster[index].Id, i + 1);
            }

            yield return Capture(prefix + "_57_lobby_barracks");
            // The trait card (Slice D stage 37, Round 84; 2026-10-19 on a right click): 아스트리드's weakness right-clicked.
            int astrid = Managers.Run.Run.Roster.FindIndex(m => m.Id == "astrid");
            TraitChipBase weakness = UiTestUtil.RosterEntries(lobby)[astrid].Traits.Chips[2];
            UiTestUtil.PressTheBackground();
            weakness.RightClickNow();
            yield return Capture(prefix + "_75_barracks_trait_card");
            UiTestUtil.PressTheBackground();
            yield return null;
            yield return StagedLevelUp(lobby, prefix);
            UiTestUtil.CloseQuarter(lobby);
            yield return Capture(prefix + "_03_lobby_party");

            // The stagecoach and the inn with some gold (staged), then the lobby past the last day (staged): the final stand.
            Managers.Run.Run.Gold = 120;
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Coach);
            yield return Capture(prefix + "_58_lobby_coach");
            UiTestUtil.CloseQuarter(lobby);
            yield return UiTestUtil.OpenQuarter(lobby, LobbyScreen.Quarter.Inn);
            yield return Capture(prefix + "_59_lobby_inn");
            UiTestUtil.CloseQuarter(lobby);
            Managers.Run.Run.Gold = 0;
            Managers.Run.Run.Day = data.Balance.TotalDays + 1;
            lobby.Refresh();
            yield return Capture(prefix + "_60_lobby_final_stand");
            Managers.Run.Run.Day = 1;
            leveled.Level = 1;
            leveled.Exp = 0;
            lobby.Refresh();
            yield return null;

            // The expedition screen (round 71 E2): the map, the first open dungeon chosen, the party as formed.
            UiTestUtil.Click(lobby, "Frame/Strip/Depart");
            yield return UiTestUtil.WaitForScreen(ScreenId.Expedition);
            yield return Capture(prefix + "_56_expedition");
            // The party popup (round 75 B): [출발] opens it over the shaded map; its own [출발] sets out.
            UiTestUtil.Click(UiTestUtil.Screen<ExpeditionScreen>(), "Frame/Details/Depart");
            yield return UiTestUtil.WaitForRedraw();
            yield return Capture(prefix + "_66_expedition_party");
            // The stat tooltip of a list line (Slice D stage 32, Round 81): the pointer on the first line not in the party.
            PartyListRowView benched = UiTestUtil.Views<PartyListRowView>(UiTestUtil.Screen<ExpeditionScreen>()).First(r => !r.InParty);
            benched.OnPointerEnter(null);
            yield return Capture(prefix + "_70_expedition_stat_tooltip");
            benched.OnPointerExit(null);
            UiTestUtil.Click(UiTestUtil.Screen<ExpeditionScreen>(), "Frame/PartyWindow/PartyDepart");
            yield return UiTestUtil.WaitForScreen(ScreenId.NodeMap);
            NodeMapScreen map = UiTestUtil.Screen<NodeMapScreen>();
            UiTestUtil.Click(UiTestUtil.Views<MapNodeView>(map).First(n => n.Button.interactable).Button);
            yield return Capture(prefix + "_04_map");

            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
            yield return UiTestUtil.WaitForScreen(ScreenId.Battle);
            BattleScreen battle = UiTestUtil.Screen<BattleScreen>();
            battle.Clock.Paused = true;
            Managers.Expedition.AdvanceBattle(7300 - Managers.Expedition.Battle.Engine.TimeMs);
            UiTestUtil.Click(UiTestUtil.Views<PotionSlotView>(battle)[0].Button);
            yield return Capture(prefix + "_05_battle");

            bool capturedLoot = false;
            bool capturedBoard = false;
            bool capturedLongMap = false;
            bool capturedBoss = false;
            int guard = 0;
            while (Managers.Expedition.Phase != GamePhase.Settlement)
            {
                Assert.Less(++guard, 200, "The expedition did not end.");
                switch (Managers.Expedition.Phase)
                {
                    case GamePhase.NodeMap:
                        map = UiTestUtil.Screen<NodeMapScreen>();
                        PartySideView party = map.GetComponentInChildren<PartySideView>();
                        if (!capturedBoard && Managers.Expedition.Expedition.Inventory.Count > 0)
                        {
                            // The first drop went to the inventory: the weapon of the row-1 member held with its ghost on the row under it,
                            // then the popup with its item picked.
                            capturedBoard = true;
                            PartyBoardView board1 = party.ColumnOfRow(1).BoardView;
                            UiTestUtil.ClickSquare(board1, 0, 0);
                            UiTestUtil.HoverSquare(board1, 1, 1);
                            yield return Capture(prefix + "_08_map_item_selected");
                            UiTestUtil.ClickSquare(board1, 0, 0);
                            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
                            yield return UiTestUtil.WaitForRedraw();
                            // Round 49: the item picked from the inventory's grid, its tooltip in the popup and its ghost on the row under the weapon.
                            BoardItem kept = Managers.Expedition.Expedition.Inventory.Items[0];
                            party.InventoryGrid.SquareAt(kept.At.X, kept.At.Y).Click();
                            UiTestUtil.HoverSquare(board1, 0, 1);
                            yield return Capture(prefix + "_17_map_inventory_selected");
                            party.InventoryGrid.SquareAt(kept.At.X, kept.At.Y).Click();
                            // Round 53: nothing held, the inventory's piece in the boards' blue with the grid's line through it; then the
                            // row-1 member's weapon held over the inventory, its ghost one block.
                            UiTestUtil.HoverSquare(board1, 2, 2);
                            yield return Capture(prefix + "_49_map_inventory_open");
                            UiTestUtil.ClickSquare(board1, 0, 0);
                            UiTestUtil.HoverSquare(party.InventoryGrid, 1, 1);
                            yield return Capture(prefix + "_50_map_inventory_ghost");
                            UiTestUtil.ClickSquare(board1, 0, 0);
                            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
                            yield return UiTestUtil.WaitForRedraw();

                            // Equipment found on the way costs fatigue (round 32, B1): staged on the row-1 member's board for one picture,
                            // a weapon, an armor and a support item under the job weapon, then the board as it was, so the rest of the run is the same.
                            StaticData shotData = Managers.Data.Data;
                            ExpeditionMember front = Managers.Expedition.Expedition.Members[party.ColumnOfRow(1).Member];
                            var frontKept = new List<BoardItem>(front.Board.Items);
                            BoardItem jobWeapon = front.Board.ItemAt(0, 0);
                            front.Board.Items.Clear();
                            front.Board.Items.Add(jobWeapon);
                            UiTestUtil.Put(front, new EquippedItem(shotData.Items.Get("dagger")), 0, 1);
                            UiTestUtil.Put(front, new EquippedItem(shotData.Items.Get("buckler")), 2, 1);
                            UiTestUtil.Put(front, new EquippedItem(shotData.Items.Get("herb_pouch")), 0, 2);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.ClickSquare(board1, 0, 1);
                            UiTestUtil.HoverSquare(board1, 0, 1);
                            yield return Capture(prefix + "_30_map_fatigue");

                            // Turning (Slice B stage 19): the held dagger turned a quarter, its ghost standing in the last column.
                            party.TurnHeld(1);
                            UiTestUtil.HoverSquare(board1, 2, 1);
                            yield return Capture(prefix + "_44_map_turned");
                            party.TurnHeld(-1);
                            UiTestUtil.ClickSquare(board1, 0, 1);

                            // Tiers (round 35): the buckler Silver and the herb pouch Gold on their pieces, a second Common dagger on row 2's
                            // board, and the row-1 dagger held over it, which marks the piece it would merge into and shows the merge.
                            front.Board.ItemAt(2, 1).Item = new EquippedItem(shotData.Items.Get("buckler"), tier: ItemTier.Tier2);
                            front.Board.ItemAt(0, 2).Item = new EquippedItem(shotData.Items.Get("herb_pouch"), tier: ItemTier.Tier3);
                            ExpeditionMember second = Managers.Expedition.Expedition.Members[party.ColumnOfRow(2).Member];
                            var secondKept = new List<BoardItem>(second.Board.Items);
                            second.Board.Items.RemoveAll(placed => placed.At.Y > 0);
                            UiTestUtil.Put(second, new EquippedItem(shotData.Items.Get("dagger")), 0, 1);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.ClickSquare(board1, 0, 1);
                            UiTestUtil.HoverSquare(party.ColumnOfRow(2).BoardView, 0, 1);
                            yield return Capture(prefix + "_35_map_tiers");
                            UiTestUtil.ClickSquare(board1, 0, 1);

                            // The unique's card (stage 23): the gladius in the row-1 weapon's place, its gold rim and the plate head card.
                            BoardItem rowOneWeapon = front.Board.ItemAt(0, 0);
                            EquippedItem rowOneWeaponBefore = rowOneWeapon.Item;
                            rowOneWeapon.Item = new EquippedItem(shotData.Items.Get("gladius"), isBase: true);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.RightClickSquare(board1, 1, 0);
                            yield return null;
                            yield return Capture(prefix + "_54_map_unique_card");
                            UiTestUtil.PressTheBackground();
                            yield return null;
                            rowOneWeapon.Item = rowOneWeaponBefore;
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();

                            // A bag (Slice B stage 19; round 79): a pouch staged under the start bag as Diablo's well. It stays put: a placed
                            // bag is never picked up, so the picture shows the board with the two bags.
                            front.Board.Bags.Add(new BoardBag(shotData.Bags.Get("leather_pouch"), new Placement(0, 3)));
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_45_map_bag_placed");
                            front.Board.Bags.RemoveAt(front.Board.Bags.Count - 1);

                            // The whetstone's stars (Slice B stage 20, Backpack Battles' way): in the buckler's square, under the job weapon's
                            // right end and over an empty square. Pointed at, its stars show, lit on the weapon and hollow on the empty square;
                            // then the weapon's card says what the star adds.
                            front.Board.Items.Remove(front.Board.ItemAt(2, 1));
                            UiTestUtil.Put(front, new EquippedItem(shotData.Items.Get("whetstone")), 2, 1);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.HoverSquare(board1, 2, 1);
                            yield return Capture(prefix + "_46_map_whetstone_stars");
                            UiTestUtil.RightClickSquare(board1, 0, 0);
                            yield return Capture(prefix + "_47_map_star_card");
                            party.Tooltip.Hide();
                            UiTestUtil.HoverSquare(board1, 0, 2);

                            second.Board.Items.Clear();
                            second.Board.Items.AddRange(secondKept);
                            front.Board.Items.Clear();
                            front.Board.Items.AddRange(frontKept);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();

                            // Fatigue and a state under the feet (round 36): the row-1 member tired and fearful, its state clicked, so
                            // that the detail line explains it; then as before, so the rest of the run is the same.
                            int fatigue = front.Fatigue;
                            front.Fatigue = 128;
                            front.StateId = "fearful";
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(party.ColumnOfRow(1).StateButton);
                            yield return Capture(prefix + "_36_map_state");
                            UiTestUtil.Click(party.ColumnOfRow(1).StateButton);
                            front.Fatigue = fatigue;
                            front.StateId = null;
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                        }

                        if (capturedBoard && !capturedLongMap)
                        {
                            // The long map (round 34), staged: deep in it with an elite chosen (or the first way on, on a map
                            // without one), then on the floor before the camp floor with a camp chosen, its window, and the
                            // rest; the boss is next.
                            capturedLongMap = true;
                            NodeMap nodes = Managers.Expedition.Expedition.Map;
                            MapNode deep = nodes.Nodes.FirstOrDefault(n => n.Floor >= 8 && n.NextNodeIds.Any(id => nodes.Get(id).Kind == MapNodeKind.Elite))
                                ?? nodes.OnFloor(10)[0];
                            Managers.Expedition.Expedition.CurrentNodeId = deep.Id;
                            yield return TaskUtil.Await(Managers.UI.ShowAsync(ScreenId.NodeMap));
                            map = UiTestUtil.Screen<NodeMapScreen>();
                            MapNode ahead = deep.NextNodeIds.Select(nodes.Get).OrderByDescending(n => n.Kind == MapNodeKind.Elite).First();
                            UiTestUtil.Click(map.NodeView(ahead.Id).Button);
                            yield return Capture(prefix + "_31_map_deep");

                            // The shop (round 44), staged like the elite when the map has one (most do): before a shop node with coins
                            // to spend, the node chosen; its window with the pointer over an offer the coins cover (round 57: its tooltip over it); that
                            // offer picked (it rides the pointer).
                            MapNode shopNode = nodes.Nodes.FirstOrDefault(n => n.Kind == MapNodeKind.Shop);
                            if (shopNode != null)
                            {
                                Managers.Expedition.Expedition.CurrentNodeId = nodes.Nodes.First(n => n.NextNodeIds.Contains(shopNode.Id)).Id;
                                Managers.Expedition.Expedition.Coins = 37;
                                yield return TaskUtil.Await(Managers.UI.ShowAsync(ScreenId.NodeMap));
                                map = UiTestUtil.Screen<NodeMapScreen>();
                                UiTestUtil.Click(map.NodeView(shopNode.Id).Button);
                                yield return Capture(prefix + "_41_map_shop");
                                UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
                                yield return UiTestUtil.WaitForRedraw();
                                IReadOnlyList<ItemOffer> stock = Managers.Expedition.ShopStock;
                                int pick = Enumerable.Range(0, stock.Count).Where(i => stock[i].Kind == OfferKind.Item && Managers.Expedition.CanAfford(i)).DefaultIfEmpty(-1).First();
                                // Round 57: the pointer over that item: Diablo's tooltip over it.
                                if (pick >= 0)
                                {
                                    UiTestUtil.HoverGood(map.Merchant, pick);
                                    yield return UiTestUtil.WaitForRedraw();
                                }

                                yield return Capture(prefix + "_42_shop");

                                // Round 79: the merchant's bag row with the bags' pictures (staged: the last two goods become the two bags on sale,
                                // put back after the picture).
                                List<ItemOffer> shopStock = Managers.Expedition.Expedition.Shop.Stock;
                                int goods = Managers.Expedition.ShopGoodsCount;
                                if (goods >= 2)
                                {
                                    ItemOffer[] stagedGoods = { shopStock[goods - 2], shopStock[goods - 1] };
                                    shopStock[goods - 2] = new ItemOffer(OfferKind.Bag, "leather_pouch");
                                    shopStock[goods - 1] = new ItemOffer(OfferKind.Bag, "belt_pouch");
                                    // The screen remembers a stock's layout by the node and the refresh count: a staged stock needs a new count to be laid out anew.
                                    Managers.Expedition.Expedition.Shop.Refreshes++;
                                    map.Refresh();
                                    yield return UiTestUtil.WaitForRedraw();
                                    yield return Capture(prefix + "_69_shop_bags");
                                    shopStock[goods - 2] = stagedGoods[0];
                                    shopStock[goods - 1] = stagedGoods[1];
                                    Managers.Expedition.Expedition.Shop.Refreshes--;
                                    map.Refresh();
                                    yield return UiTestUtil.WaitForRedraw();
                                }
                                if (pick >= 0)
                                {
                                    UiTestUtil.ClickGood(map.Merchant, pick);
                                    yield return UiTestUtil.WaitForRedraw();
                                    yield return Capture(prefix + "_43_shop_pick");

                                    // Round 54: the picked offer rides the pointer; a press elsewhere (its tile again) sends it back first.
                                    UiTestUtil.ClickGood(map.Merchant, pick);
                                    yield return UiTestUtil.WaitForRedraw();

                                    // Round 74 (2026-10-17): the good bought onto row 1's free row, then its card at the shop with the sell button.
                                    PartySideView shopParty = map.GetComponentInChildren<PartySideView>();
                                    PartyColumnView shopRow1 = shopParty.ColumnOfRow(1);
                                    int shopFront = Managers.Expedition.Expedition.Members.ToList().FindIndex(m => m.Row == BattleRows.Front);
                                    if (Managers.Expedition.CanBuyToBoard(pick, shopFront, new Placement(0, 1)))
                                    {
                                        UiTestUtil.ClickGood(map.Merchant, pick);
                                        yield return UiTestUtil.WaitForRedraw();
                                        UiTestUtil.ClickSquare(shopRow1.BoardView, 0, 1);
                                        yield return UiTestUtil.WaitForRedraw();
                                        UiTestUtil.RightClickSquare(shopRow1.BoardView, 0, 1);
                                        yield return UiTestUtil.WaitForRedraw();
                                        yield return Capture(prefix + "_65_shop_sell_card");
                                        PointerPress.Simulate();
                                        yield return null;
                                    }
                                }

                                UiTestUtil.PressTheBackground();
                                UiTestUtil.Click(map, UiTestUtil.ShopLeave);
                                yield return UiTestUtil.WaitForRedraw();
                            }

                            MapNode before = nodes.OnFloor(nodes.FloorCount - 2)[0];
                            Managers.Expedition.Expedition.CurrentNodeId = before.Id;
                            yield return TaskUtil.Await(Managers.UI.ShowAsync(ScreenId.NodeMap));
                            map = UiTestUtil.Screen<NodeMapScreen>();
                            UiTestUtil.Click(map.NodeView(before.NextNodeIds[0]).Button);
                            yield return Capture(prefix + "_32_map_camp");
                            UiTestUtil.Click(map, "Frame/BoardPanel/Enter");
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_33_camp");

                            // The mend step (round 35) with the row-1 weapon chosen, then back to the two cards and the rest.
                            UiTestUtil.Click(map, UiTestUtil.CampMend);
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.ClickSquare(map.GetComponentInChildren<PartySideView>().ColumnOfRow(1).BoardView, 0, 0);
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_34_camp_mend");
                            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampMend/MendBack");
                            yield return UiTestUtil.WaitForRedraw();

                            // The forge (stage 23): a 3-star longsword with a 3-star whetstone lit under it makes the third card; its step
                            // with the sword chosen. Then the staging is undone and the party rests as before.
                            ExpeditionMember smith = Managers.Expedition.Expedition.Members.First(m => m.Board.ItemAt(0, 0)?.Item.Item.Id == "longsword");
                            BoardItem smithsSword = smith.Board.ItemAt(0, 0);
                            EquippedItem swordBefore = smithsSword.Item;
                            smithsSword.Item = new EquippedItem(swordBefore.Item, isBase: true, tier: ItemTier.Tier3);
                            UiTestUtil.Put(smith, new EquippedItem(data.Items.Get("whetstone"), tier: ItemTier.Tier3), 0, 1);
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_52_camp_forge");
                            UiTestUtil.Click(map, UiTestUtil.CampForge);
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_53_camp_forge_step");
                            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampForge/ForgeBack");
                            yield return UiTestUtil.WaitForRedraw();
                            smith.Board.Items.Remove(smith.Board.ItemAt(0, 1));
                            smithsSword.Item = swordBefore;
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(map, UiTestUtil.CampRest);
                            yield return UiTestUtil.WaitForRedraw();

                            // The tiers in battle (round 41): the rearmost member's weapon as if mended once (Bronze), so that the boss
                            // battle shows a cell's outline under the charge's dark and the tier tag above it (the rear survives the longest).
                            ExpeditionMember rear = Managers.Expedition.Expedition.Members.OrderByDescending(m => m.Row).First(m => m.Alive);
                            BoardItem rearWeapon = rear.Board.ItemAt(0, 0);
                            rearWeapon.Item = new EquippedItem(rearWeapon.Item.Item, isBase: true, tier: ItemTier.Tier1);
                        }

                        yield return UiTestUtil.GoIntoTheFirstNode();
                        break;

                    case GamePhase.Battle:
                        battle = UiTestUtil.Screen<BattleScreen>();
                        battle.Clock.Paused = true;
                        bool boss = Managers.Expedition.Battle.Node.Kind == MapNodeKind.Boss;
                        if (boss && !capturedBoss)
                        {
                            capturedBoss = true;
                            // The card of an item in battle (round 42), before the fight has gone anywhere (every enemy still stands): the boss's
                            // first item, to the right of its board; a press closes it.
                            BattleBoardView bossBoard = UiTestUtil.Views<BattleBoardView>(battle).First(b => b.Unit.Side == BattleSide.Enemy && b.Items.Count > 0);
                            UiTestUtil.RightClick(bossBoard.Items[0]);
                            yield return Capture(prefix + "_40_battle_item_card");
                            UiTestUtil.PressTheBackground();
                            yield return null;

                            Managers.Expedition.AdvanceBattle(30000);
                            yield return Capture(prefix + "_09_boss_battle");
                        }

                        while (!Managers.Expedition.Battle.IsFinished)
                        {
                            Managers.Expedition.AdvanceBattle(250);
                        }

                        yield return UiTestUtil.WaitForResult(battle);
                        if (boss)
                        {
                            yield return Capture(prefix + "_10_boss_result");
                            UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/ShowLog");
                            yield return Capture(prefix + "_16_battle_log");
                            UiTestUtil.Click(battle, "Frame/LogPanel/LogBox/LogClose");
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(battle, "Frame/ResultPanel/ResultBox/Continue");
                            yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                            break;
                        }

                        // After the win (round 47): the band, the drops on the floor and the node map's boards on the battle screen. The
                        // drops one by one: the first ever goes to the inventory (pictured first: the win, then the drop picked), the later
                        // ones behind the row-1 member's weapon while that cell can take them; what cannot go anywhere is left.
                        if (!capturedLoot)
                        {
                            // The last blows' numbers and the fallen's ghosts fade first, so the picture shows the win as it settles.
                            yield return new WaitForSecondsRealtime(1.5f);
                            yield return Capture(prefix + "_06_battle_won");
                        }

                        if (!capturedLoot && Managers.Expedition.Expedition.Loot.Count > 0 && Managers.Expedition.Expedition.Loot[0] != null)
                        {
                            // Round 79: a bag lying on the floor (staged in the first drop's place: an elite's bag), its picture at its squares'
                            // size and its name with its squares on the plate; the drop is put back before the lap goes on.
                            ItemOffer stagedDrop = Managers.Expedition.Expedition.Loot[0];
                            Managers.Expedition.Expedition.Loot[0] = new ItemOffer(OfferKind.Bag, "leather_pouch");
                            battle.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_68_loot_bag");
                            Managers.Expedition.Expedition.Loot[0] = stagedDrop;
                            battle.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                        }

                        int rowOne = Managers.Expedition.Expedition.Members.FindIndex(m => m.Alive && m.Row == 1);
                        for (int drop = 0; drop < battle.Drops.Count; drop++)
                        {
                            if (Managers.Expedition.Expedition.Loot[drop] == null)
                            {
                                continue;
                            }

                            UiTestUtil.Click(battle.Drops[drop].Button);
                            yield return UiTestUtil.WaitForRedraw();
                            if (!capturedLoot)
                            {
                                capturedLoot = true;
                                yield return Capture(prefix + "_07_loot_picked");
                                UiTestUtil.Click(battle, "Frame/BoardPanel/LootToInventory");
                                yield return UiTestUtil.WaitForRedraw();

                                // Round 52: the inventory window over the party's side of the stage (I), the drop just taken picked from its
                                // grid with its tooltip beside it and its ghost on row 1's board.
                                battle.PressInventoryKey();
                                yield return UiTestUtil.WaitForRedraw();
                                BoardItem taken = Managers.Expedition.Expedition.Inventory.Items[0];
                                battle.InventoryWindow.Grid.SquareAt(taken.At.X, taken.At.Y).Click();
                                UiTestUtil.HoverSquare(battle.LootBoardOfRow(1), 0, 1);
                                yield return Capture(prefix + "_48_battle_won_inventory");
                                battle.InventoryWindow.Grid.SquareAt(taken.At.X, taken.At.Y).Click();
                                battle.PressInventoryKey();
                            }
                            else if (Managers.Expedition.CanTakeLoot(drop, rowOne, new Placement(0, 1)))
                            {
                                UiTestUtil.ClickSquare(battle.LootBoardOfRow(1), 0, 1);
                            }
                            else
                            {
                                UiTestUtil.Click(battle.Drops[drop].Button);
                            }

                            yield return UiTestUtil.WaitForRedraw();
                        }

                        UiTestUtil.Click(battle, "Frame/BoardPanel/LootContinue");
                        yield return UiTestUtil.WaitForScreen(ScreenCatalog.ForPhase(Managers.Expedition.Phase));
                        break;
                }
            }

            yield return UiTestUtil.WaitForScreen(ScreenId.Settlement);
            yield return Capture(prefix + "_11_settlement");

            // Slice D stage 35 (Round 82): the line naming who can level up, staged on the report for one picture — the first two of the roster came
            // home with their level's experience; then the report as it was.
            yield return StagedReady(UiTestUtil.Screen<SettlementScreen>(), prefix + "_72_settlement_ready");

            // Slice D stage 38 (Round 84 판정 7): the "new traits" lines, staged — as the mock (아스트리드 retreated from death's door: 패배의 기억,
            // 흉터), then four survivors with the most an expedition leaves, to see the panel hold them.
            yield return StagedNewTraits(UiTestUtil.Screen<SettlementScreen>(), prefix + "_77_settlement_new_traits", new List<AcquiredTraits>
            {
                new AcquiredTraits { MercenaryId = "astrid", TraitIds = new List<string> { "shaken", "scarred" } },
            });
            yield return StagedNewTraits(UiTestUtil.Screen<SettlementScreen>(), prefix + "_78_settlement_new_traits_four", new List<AcquiredTraits>
            {
                new AcquiredTraits { MercenaryId = "astrid", TraitIds = new List<string> { "shaken", "scarred", "nightmares" } },
                new AcquiredTraits { MercenaryId = "rowan", TraitIds = new List<string> { "seasoned", "survivor", "hardened" } },
                new AcquiredTraits { MercenaryId = "ella", TraitIds = new List<string> { "shaken", "limp" } },
                new AcquiredTraits { MercenaryId = "mira", TraitIds = new List<string> { "seasoned" } },
            });

            UiTestUtil.Click(UiTestUtil.Screen<SettlementScreen>(), "Frame/Panel/Confirm");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return Capture(prefix + "_12_lobby_after");

            // A mercenary that came home afflicted (round 36), staged on the first of the roster for one picture of the barracks, then as before.
            MercenaryState first = Managers.Run.Run.Roster[0];
            int homeFatigue = first.Fatigue;
            first.Fatigue = 128;
            first.AfflictionId = "fearful";
            yield return UiTestUtil.OpenBarracks(UiTestUtil.Screen<LobbyScreen>());
            yield return Capture(prefix + "_37_lobby_affliction");
            first.Fatigue = homeFatigue;
            first.AfflictionId = null;
            UiTestUtil.CloseQuarter(UiTestUtil.Screen<LobbyScreen>());
            yield return null;

            // A failed save: the save directory is made unwritable for one command (resting at the inn).
            yield return UiTestUtil.OpenQuarter(UiTestUtil.Screen<LobbyScreen>(), LobbyScreen.Quarter.Inn);
            Directory.Delete(_saveRoot, true);
            File.WriteAllText(_saveRoot, "blocked");
            UiTestUtil.Click(UiTestUtil.Screen<LobbyScreen>(), "Frame/InnWindow/Rest");
            yield return Capture(prefix + "_14_save_failed");
            File.Delete(_saveRoot);
            Directory.CreateDirectory(_saveRoot);
            yield return UiTestUtil.WaitForRedraw();
            UiTestUtil.Click(Managers.UI.SaveError, "Blocker/Box/Retry");
            Assert.IsFalse(Managers.Run.IsSaveBlocked);
            UiTestUtil.CloseQuarter(UiTestUtil.Screen<LobbyScreen>());

            // The end of a run, staged directly: the result window over the town (Round 72) — a defeat at the death limit with two left, then a victory.
            RunState run = Managers.Run.Run;
            List<MercenaryState> living = run.Roster.Take(2).ToList();
            run.Fallen.AddRange(run.Roster.Skip(2).Select(m => m.Id));
            run.Fallen.AddRange(data.RecruitPool.Take(data.Balance.DeathLimit - run.Fallen.Count).Select(m => m.Id));
            run.Roster.Clear();
            run.Roster.AddRange(living);
            run.Party.Clear();
            run.Day = 63;
            run.ExpeditionCount = 10;
            run.ExpeditionsByDungeon["abandoned_mine"] = 9;
            run.ExpeditionsByDungeon["abyss"] = 1;
            run.Outcome = RunOutcome.Defeat;
            run.DefeatCause = DefeatCause.DeathLimit;
            UiTestUtil.Screen<LobbyScreen>().Refresh();
            yield return null;
            yield return Capture(prefix + "_13_run_over");
            run.Outcome = RunOutcome.Victory;
            run.DefeatCause = DefeatCause.None;
            UiTestUtil.Screen<LobbyScreen>().Refresh();
            yield return null;
            yield return Capture(prefix + "_62_run_victory");
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
