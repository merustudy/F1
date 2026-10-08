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
                            // The first drop went to the inventory: the weapon of the row-1 member selected, then the popup with its item picked.
                            capturedBoard = true;
                            ItemSlotView weapon = party.ColumnOfRow(1).Slots[0];
                            UiTestUtil.Click(weapon.Button);
                            yield return Capture(prefix + "_08_map_item_selected");
                            UiTestUtil.Click(weapon.Button);
                            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(party.InventoryEntries[0].Button);
                            yield return Capture(prefix + "_17_map_inventory_selected");
                            UiTestUtil.Click(map, "Frame/BoardPanel/InventoryToggle");
                            yield return UiTestUtil.WaitForRedraw();

                            // Equipment found on the way costs fatigue (round 32, B1): staged on the row-1 member's board for one picture,
                            // a weapon, an armor and a support item, then taken away again so the rest of the run is the same.
                            ExpeditionMember front = Managers.Expedition.Expedition.Members[party.ColumnOfRow(1).Member];
                            int carried = front.Items.Count;
                            foreach (string found in new[] { "dagger", "buckler", "herb_pouch" })
                            {
                                front.Items.Add(new EquippedItem(Managers.Data.Data.Items.Get(found), 8));
                            }

                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(party.ColumnOfRow(1).Slots[carried].Button);
                            yield return Capture(prefix + "_30_map_fatigue");
                            UiTestUtil.Click(party.ColumnOfRow(1).Slots[carried].Button);

                            // Tiers (round 35): the buckler Silver and the herb pouch Gold on their cells, a second Common dagger on row 2's
                            // board, and the row-1 dagger chosen, which marks the cell it would merge into.
                            front.Items[carried + 1] = new EquippedItem(front.Items[carried + 1].Item, 8, tier: ItemTier.Silver);
                            front.Items[carried + 2] = new EquippedItem(front.Items[carried + 2].Item, 8, tier: ItemTier.Gold);
                            ExpeditionMember second = Managers.Expedition.Expedition.Members[party.ColumnOfRow(2).Member];
                            second.Items.Add(new EquippedItem(Managers.Data.Data.Items.Get("dagger"), 9));
                            map.Refresh();
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(party.ColumnOfRow(1).Slots[carried].Button);
                            yield return Capture(prefix + "_35_map_tiers");
                            UiTestUtil.Click(party.ColumnOfRow(1).Slots[carried].Button);
                            second.Items.RemoveAt(second.Items.Count - 1);
                            front.Items.RemoveRange(carried, front.Items.Count - carried);

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
                            // to spend, the node chosen; its window; an offer the coins cover picked, its tile brass (round 46: the tile shows the facts, no card).
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
                                yield return Capture(prefix + "_42_shop");
                                IReadOnlyList<ItemOffer> stock = Managers.Expedition.ShopStock;
                                int pick = Enumerable.Range(0, stock.Count).Where(i => stock[i].Kind == OfferKind.Item && Managers.Expedition.CanAfford(i)).DefaultIfEmpty(-1).First();
                                if (pick >= 0)
                                {
                                    UiTestUtil.Click(map.ShopTiles[pick].Button);
                                    yield return UiTestUtil.WaitForRedraw();
                                    yield return Capture(prefix + "_43_shop_pick");
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
                            UiTestUtil.Click(map.GetComponentInChildren<PartySideView>().ColumnOfRow(1).Slots[0].Button);
                            yield return UiTestUtil.WaitForRedraw();
                            yield return Capture(prefix + "_34_camp_mend");
                            UiTestUtil.Click(map, UiTestUtil.CampBox + "/CampMend/MendBack");
                            yield return UiTestUtil.WaitForRedraw();
                            UiTestUtil.Click(map, UiTestUtil.CampRest);
                            yield return UiTestUtil.WaitForRedraw();

                            // The tiers in battle (round 41): the rearmost member's weapon as if mended once (Bronze), so that the boss
                            // battle shows a cell's outline under the charge's dark and the tier tag above it (the rear survives the longest).
                            ExpeditionMember rear = Managers.Expedition.Expedition.Members.OrderByDescending(m => m.Row).First(m => m.Alive);
                            rear.Items[0] = new EquippedItem(rear.Items[0].Item, rear.Items[0].Grade, isBase: true, tier: ItemTier.Bronze);
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
                            }
                            else if (Managers.Expedition.CanTakeLoot(drop, rowOne, 1))
                            {
                                UiTestUtil.Click(battle.LootBoardOfRow(1).Slots[1].Button);
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

            UiTestUtil.Click(UiTestUtil.Screen<SettlementScreen>(), "Frame/Panel/Confirm");
            yield return UiTestUtil.WaitForScreen(ScreenId.Lobby);
            yield return Capture(prefix + "_12_lobby_after");

            // A mercenary that came home afflicted (round 36), staged on the first of the roster for one picture, then as before.
            MercenaryState first = Managers.Run.Run.Roster[0];
            int homeFatigue = first.Fatigue;
            first.Fatigue = 128;
            first.AfflictionId = "fearful";
            UiTestUtil.Screen<LobbyScreen>().Refresh();
            yield return Capture(prefix + "_37_lobby_affliction");
            first.Fatigue = homeFatigue;
            first.AfflictionId = null;
            UiTestUtil.Screen<LobbyScreen>().Refresh();
            yield return null;

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
