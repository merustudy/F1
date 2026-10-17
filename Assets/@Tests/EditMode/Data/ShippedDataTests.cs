using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the static data that ships: sources convert, generated files are current and load.</summary>
    public sealed class ShippedDataTests
    {
        [Test]
        public void GeneratedJson_IsUpToDateWithSources()
        {
            List<string> stale = DataTransformMenu.FindStaleFiles();

            Assert.IsEmpty(stale, "Stale generated files. Run F1/Data/Transform Static Data: " + string.Join(", ", stale));
        }

        [Test]
        public void GeneratedJson_LoadsThroughTheRuntimeLoader()
        {
            StaticData data = LoadShipped();

            Assert.IsNotEmpty(data.Jobs.Ordered);
            Assert.IsNotEmpty(data.Dungeons.Ordered);
        }

        [Test]
        public void EveryDefinition_HasSourceAndGeneratedFile()
        {
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.SourceDirectory, file.SourceFileName)), file.SourceFileName);
                Assert.IsTrue(File.Exists(Path.Combine(StaticDataFiles.GeneratedDirectory, file.GeneratedFileName)), file.GeneratedFileName);
            }
        }

        /// <summary>The first mercenaries of the roster, lined up from row 1 back.</summary>
        static List<PartyMember> FirstParty(StaticData data)
        {
            return data.StartingMercenaries
                .Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index))
                .ToList();
        }

        [Test]
        public void EveryShippedDungeon_ProducesAMapAndBattleSetupsForEveryFoughtNode()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                for (ulong seed = 1; seed <= 20; seed++)
                {
                    ExpeditionState state = ExpeditionRules.Create(data, dungeon.Id, seed, party);
                    foreach (MapNode node in state.Map.Nodes)
                    {
                        if (!node.IsFought)
                        {
                            Assert.IsNull(node.EnemyGroupId, $"{dungeon.Id}: nobody waits at camp {node.Id}");
                            continue;
                        }

                        BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, state, node.EnemyGroupId, seed, node.Floor);
                        Assert.DoesNotThrow(() => new BattleEngine(setup), $"{dungeon.Id} {node.EnemyGroupId} on floor {node.Floor}");
                    }
                }
            }
        }

        [Test]
        public void ShippedBattles_AlwaysEnd()
        {
            StaticData data = LoadShipped();
            List<PartyMember> party = FirstParty(data);

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                ExpeditionState state = ExpeditionRules.Create(data, group.DungeonId, 1, party);
                var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, group.Id, 1));

                Assert.DoesNotThrow(() => battle.RunToEnd(), group.Id);
                Assert.AreNotEqual(BattleResult.Ongoing, battle.Result);
            }
        }

        [Test]
        public void EveryShippedJob_CanUseItsWeaponInItsRecommendedRow_OfAFullParty()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.IsTrue(
                    data.Items.Get(job.WeaponItemId).UsableIn(job.RecommendedRow, data.Balance.PartySize),
                    $"{job.Id}: {job.WeaponItemId} in row {job.RecommendedRow} of {data.Balance.PartySize}");
            }
        }

        [Test]
        public void EveryShippedEnemyGroup_ThreatensFromTheStart()
        {
            // Where an item works is counted on the living line, so nobody stays dead weight for good: the
            // line only shortens. What matters is that the group acts before anyone has died.
            StaticData data = LoadShipped();

            foreach (EnemyGroupData group in data.EnemyGroups.Ordered)
            {
                bool someoneActsAtTheStart = false;
                for (int i = 0; i < group.Enemies.Count; i++)
                {
                    int row = BattleRows.Front + i;
                    EnemyData enemy = data.Enemies.Get(group.Enemies[i]);
                    someoneActsAtTheStart |= enemy.Items.Any(grant => data.Items.Get(grant.ItemId).UsableIn(row, group.Enemies.Count));
                }

                Assert.IsTrue(someoneActsAtTheStart, $"{group.Id}: nobody can use an item where they stand at the start");
            }
        }

        /// <summary>
        /// The items are classed as the design says (Docs/Design/02_Combat_System.md §4, 2026-10-04): every job's own weapon is a
        /// weapon (the bishop's healing staff too), the buckler is defensive gear, the flask and the spit are attacks that are not
        /// weapons, and the charm, the pouch and the chant are support items.
        /// </summary>
        [Test]
        public void ShippedItems_AreClassedAsDecided()
        {
            StaticData data = LoadShipped();

            foreach (JobData job in data.Jobs.Ordered)
            {
                Assert.AreEqual(ItemCategory.Weapon, data.Items.Get(job.WeaponItemId).Category, job.Id);
            }

            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("healing_staff").Category);
            Assert.AreEqual(ItemCategory.Armor, data.Items.Get("buckler").Category);
            Assert.AreEqual(ItemCategory.Attack, data.Items.Get("ember_flask").Category);
            Assert.AreEqual(ItemCategory.Weapon, data.Items.Get("lantern_staff").Category);
            foreach (string support in new[] { "ward_charm", "herb_pouch", "mending_chant" })
            {
                Assert.AreEqual(ItemCategory.Support, data.Items.Get(support).Category, support);
            }
        }

        /// <summary>Slice B stage 20 and round 51: the whetstone and its stars, the melee weapons, the valkyrie's battle axe.</summary>
        [Test]
        public void ShippedStarsAndShapes_AreAsDecided()
        {
            StaticData data = LoadShipped();

            ItemData whetstone = data.Items.Get("whetstone");
            Assert.AreEqual(ItemCategory.Other, whetstone.Category);
            Assert.IsTrue(whetstone.IsPassive, "It never activates.");
            Assert.AreEqual((1, 1), (whetstone.Width, whetstone.Height));
            CollectionAssert.AreEqual(new[] { (0, -1), (0, 1) }, whetstone.Stars.Select(s => (s.X, s.Y)), "A star above and one below.");
            Assert.AreEqual(100, whetstone.StarDamage, "+1, in hundredths (stage 22).");
            Assert.Greater(whetstone.ShopWeight, 0, "Sold in shops.");
            Assert.Greater(whetstone.Price, 0);
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Items.Any(g => g.ItemId == "whetstone")), "No enemy carries it: shops only.");

            CollectionAssert.AreEquivalent(
                new[] { "longsword", "greataxe", "mace", "dagger", "spear", "halberd", "rat_bite", "rusty_blade", "overseer_maul", "gladius", "abyss_cleaver" },
                data.Items.Ordered.Where(i => i.Melee).Select(i => i.Id));

            ItemData axe = data.Items.Get("greataxe");
            Assert.AreEqual("전투도끼", axe.Name.Resolve("ko-KR"));
            Assert.AreEqual((3, 2), (axe.Width, axe.Height), "The valkyrie's battle axe is the one base weapon of 3x2.");
            foreach (JobData job in data.Jobs.Ordered.Where(j => j.WeaponItemId != "greataxe"))
            {
                ItemData weapon = data.Items.Get(job.WeaponItemId);
                Assert.AreEqual((3, 1), (weapon.Width, weapon.Height), job.Id);
            }

            Assert.AreEqual("쥐 발톱", data.Items.Get("rat_bite").Name.Resolve("ko-KR"));
        }

        [Test]
        public void TheGladius_IsTheMinesUnique_ForgedFromTheLongswordAndAWhetstone()
        {
            // Stage 23 (round 70): the first unique item and its recipe.
            StaticData data = LoadShipped();
            ItemData gladius = data.Items.Get("gladius");
            Assert.IsTrue(gladius.Unique);
            Assert.AreEqual(0, gladius.ShopWeight, "Never in a shop.");
            Assert.AreEqual(0, gladius.Price);
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Items.Any(g => g.ItemId == "gladius")), "No enemy carries it: forged only.");
            Assert.AreEqual((3, 1), (gladius.Width, gladius.Height), "The longsword's place.");
            Assert.IsTrue(gladius.Melee);
            Assert.AreEqual(2, gladius.Effects[0].Reach, "Hits one enemy more than the longsword.");
            Assert.AreEqual(10, gladius.CritChancePercent);
            CollectionAssert.AreEqual(new[] { (1, -1), (1, 1) }, gladius.Stars.Select(s => (s.X, s.Y)), "A star above and below its centre square.");
            Assert.AreEqual(400, gladius.StarDamage, "A Tier3 whetstone's +4, flat: a unique has no tier (round 70 follow-up 3).");
            Assert.AreEqual((804, 1340), (gladius.Effects[0].Min, gladius.Effects[0].Max), "8.04-13.4 in hundredths: 12-20 (the longsword 2-4 + a whetstone, x4, flat) scaled by 0.67 with every damage value (2026-10-17).");
            Assert.AreEqual("item/gladius-glow", gladius.GlowIcon, "The gold glow behind its icon, made from the icon.");
            Assert.IsNull(data.Items.Get("longsword").GlowIcon, "Only a unique item glows.");

            RecipeData recipe = data.Recipes.Get("gladius");
            Assert.AreEqual(("gladius", "longsword", ItemTier.Tier3, "whetstone", ItemTier.Tier3), (recipe.Result, recipe.BaseItem, recipe.BaseTier, recipe.Catalyst, recipe.CatalystTier));
            Assert.AreEqual(1, data.Recipes.Count);
            Assert.AreEqual(1, data.Items.Ordered.Count(i => i.Unique));
        }

        [Test]
        public void ShippedInventory_IsTheSizeItsWindowIsLaidOutFor()
        {
            BalanceData balance = LoadShipped().Balance;

            // The data allows a larger inventory than the window holds: a size of its own would lay the grid past the window's edge.
            Assert.AreEqual(
                (InventoryGridView.LaidOutWidth, InventoryGridView.LaidOutHeight),
                (balance.InventoryWidth, balance.InventoryHeight),
                "The inventory window is laid out for another size: change InventoryGridView.LaidOutWidth/Height and the window with the data.");
        }

        [Test]
        public void ShippedAbyssLord_CarriesThreeFallingPhases_IntoTheBattleSetup()
        {
            // Slice C stage 28 (Docs/Design/02_Combat_System.md §9): the final boss starts with its cleaver and changes its items at 75, 50 and 25%.
            StaticData data = LoadShipped();
            EnemyData lord = data.Enemies.Get("abyss_lord");
            Assert.AreEqual(data.Balance.FinalBossLevel, lord.Level);
            CollectionAssert.AreEqual(new[] { 75, 50, 25 }, lord.Phases.Select(p => p.HpPercent));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver" }, lord.Items.Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl" }, lord.Phases[0].Items.Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl", "abyss_grasp" }, lord.Phases[2].Items.Select(g => g.ItemId));
            Assert.IsTrue(lord.Phases[2].Items.All(g => g.PowerPercent > lord.Phases[0].Items[0].PowerPercent), "The last phase hits harder.");
            Assert.IsTrue(data.Dungeons.Ordered.Single(d => d.IsFinal).Id == "abyss");
            Assert.IsTrue(data.EnemyGroups.Ordered.Any(g => g.IsBoss && g.DungeonId == "abyss" && g.Enemies[0] == "abyss_lord"), "The lord leads the final dungeon's boss group.");
            Assert.IsFalse(data.Enemies.Ordered.Any(e => e.Id != "abyss_lord" && e.Level >= data.Balance.FinalBossLevel), "The level is the lord's alone.");

            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, F1.Tests.FlowTestKit.Party(data.StartingMercenaries.Take(data.Balance.MinPartySize).Select((m, i) => (m.Id, i + 1)).ToArray()));
            run.ClearedDungeons["abandoned_mine"] = data.Dungeons.Get("abyss").UnlockClears;
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "abyss");
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "abyss_lord_court", 1);
            BattleUnitSetup boss = setup.Enemies[0];
            Assert.AreEqual("abyss_lord", boss.SourceId);
            Assert.AreEqual(3, boss.Phases.Count);
            CollectionAssert.AreEqual(new[] { 75, 50, 25 }, boss.Phases.Select(p => p.HpPercent));
            CollectionAssert.AreEqual(new[] { "abyss_cleaver", "abyss_howl", "abyss_grasp" }, boss.Phases[1].Items.Select(i => i.Item.Id));
            Assert.IsNotNull(boss.Phases[1].Layout, "The screen draws the phase's board from its layout.");
            Assert.IsEmpty(setup.Enemies[1].Phases, "The guards have none.");
        }

        [Test]
        public void ShippedEnemyItems_HaveAPriceToSellFor_ButNoShopWeight_AndSellForHalf()
        {
            // Round 74 (2026-10-17): the enemies' items got a price so that they sell at a shop; their weight 0 keeps them off the shop's grid.
            StaticData data = LoadShipped();
            Assert.AreEqual(50, data.Balance.SellPercent);
            foreach (string id in new[] { "rat_bite", "rusty_blade", "crude_bow", "lantern_staff", "mending_chant", "overseer_roar", "overseer_maul", "abyss_howl", "abyss_grasp", "abyss_cleaver" })
            {
                ItemData item = data.Items.Get(id);
                Assert.Greater(item.Price, 0, id);
                Assert.AreEqual(0, item.ShopWeight, id + " never on a shop's grid");
            }

            Assert.AreEqual(3, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("rusty_blade"))), "6 at Common, half.");
            Assert.AreEqual(4, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("whetstone"))), "8 at Common (restored 2026-10-17), half.");
            Assert.AreEqual(1, ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("rat_bite"))), "2 at Common: half is 1, the least.");
            Assert.AreEqual(0, data.Items.Get("gladius").Price, "A unique has no price (and cannot be sold).");
        }

        [Test]
        public void ShippedBaseWeapons_AreOnSale_AndTheGeneralItems_DropByWeight()
        {
            // 2026-10-17: the jobs' base weapons priced and weighted for the shop (the party's own only: ExpeditionRules), the shop's general
            // items with a drop weight, and nothing else dropping as a general item (the whetstone and the base weapons come from shops).
            StaticData data = LoadShipped();
            Assert.AreEqual(30, data.Dungeons.Get("abandoned_mine").GeneralDropChancePercent);
            Assert.AreEqual(0, data.Dungeons.Get("abyss").GeneralDropChancePercent, "The pit drops only bones: a general item there made it twice as easy (Design/08 §32).");
            foreach (JobData job in data.Jobs.Ordered)
            {
                ItemData weapon = data.Items.Get(job.WeaponItemId);
                Assert.Greater(weapon.Price, 0, job.Id);
                Assert.Greater(weapon.ShopWeight, 0, job.Id);
                Assert.AreEqual(0, weapon.DropWeight, job.Id + ": a base weapon comes from a shop or a camp, never a drop.");
            }

            foreach (string id in new[] { "dagger", "longbow", "spear", "halberd", "buckler", "ward_charm", "herb_pouch", "ember_flask" })
            {
                Assert.Greater(data.Items.Get(id).DropWeight, 0, id);
            }

            Assert.AreEqual(0, data.Items.Get("whetstone").DropWeight, "Shops only (round 51).");
            Assert.AreEqual(0, data.Items.Get("gladius").DropWeight);
            Assert.IsTrue(data.Items.Ordered.All(i => i.DropWeight == 0 || i.ShopWeight > 0), "A general drop is one of the shop's general items.");
            Assert.IsTrue(data.Enemies.Ordered.SelectMany(e => e.Items).Select(g => data.Items.Get(g.ItemId)).All(i => i.ShopWeight > 0 || i.DropWeight == 0),
                "An enemy's own item (one no shop sells) drops only as the enemy's; the buckler is both a shop's general item and the veteran's.");
        }

        static StaticData LoadShipped()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            return StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
        }
    }
}
