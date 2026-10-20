using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Deep items (2026-10-20, Docs/Design/03_Dungeon_Structure.md §4 "깊은 층의 아이템"): what an enemy carries from a floor down joins its base
    /// items in the battle setup and in the loot pool; the first floor and a boss group carry the base alone; the data refuses bundles below the
    /// second floor, out of order or over the frame. One rule per test.
    /// </summary>
    public sealed class DeepItemsTests
    {
        static List<ItemGrant> Grants(params (string Id, int Power)[] grants)
        {
            return grants.Select(g => new ItemGrant(g.Id, g.Power)).ToList();
        }

        /// <summary>The test grunt (a claw at 5%) with bundles: a knife from floor 2, a charm and a harder claw from floor 3.</summary>
        static EnemyData DeepGrunt(IReadOnlyList<EnemyPhase> phases = null, params EnemyDepthItems[] bundles)
        {
            return new EnemyData("grunt", TestData.Text("grunt"), 2, 30, Grants(("claw", 5)), phases: phases, deepItems: bundles.Length == 0
                ? new[] { new EnemyDepthItems(2, Grants(("knife", 100))), new EnemyDepthItems(3, Grants(("charm", 100), ("claw", 7))) }
                : bundles);
        }

        static StaticData DataWithDeepGrunts(EnemyData grunt = null, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Enemies = parts.Enemies.Select(e => e.Id == "grunt" ? grunt ?? DeepGrunt() : e).ToList();
            return new StaticData(parts);
        }

        /// <summary>An expedition into the cave with one mercenary in the party.</summary>
        static ExpeditionState Expedition(StaticData data)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.PlaceInParty(data, run, run.Roster[0].Id, 1);
            return RunRules.BeginExpedition(data, run, "cave");
        }

        static IEnumerable<string> Ids(IEnumerable<EquippedItem> items)
        {
            return items.Select(item => item.Item.Id);
        }

        [Test]
        public void ItemsAt_IsTheBaseOnTheFirstFloor_AndAddsTheBundlesDownToTheFloor_InOrder()
        {
            EnemyData grunt = DeepGrunt();
            CollectionAssert.AreEqual(new[] { "claw" }, grunt.ItemsAt(1).Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "claw", "knife" }, grunt.ItemsAt(2).Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "claw", "knife", "charm", "claw" }, grunt.ItemsAt(3).Select(g => g.ItemId));
            CollectionAssert.AreEqual(new[] { "claw", "knife", "charm", "claw" }, grunt.ItemsAt(30).Select(g => g.ItemId), "Below the last bundle: all of them.");
            Assert.AreEqual(7, grunt.ItemsAt(3)[3].PowerPercent, "A bundle's grant keeps its own power percent.");
            Assert.AreSame(grunt.Items, grunt.ItemsAt(1), "Above the first bundle nothing is copied.");

            EnemyData plain = TestData.Data().Enemies.Get("grunt");
            Assert.IsEmpty(plain.DeepItems);
            Assert.AreSame(plain.Items, plain.ItemsAt(30), "An enemy without bundles carries its items on every floor.");
        }

        [Test]
        public void BattleSetup_CarriesTheBundlesOfTheNodesFloor_WithTheDepthsPower_AndTheBaseOnTheFirstFloor()
        {
            StaticData data = DataWithDeepGrunts();
            ExpeditionState expedition = Expedition(data);
            DungeonData cave = data.Dungeons.Get("cave");

            BattleSetup first = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1, 1);
            CollectionAssert.AreEqual(new[] { "claw" }, Ids(first.Enemies[0].Items));

            BattleSetup third = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1, 3);
            foreach (BattleUnitSetup grunt in third.Enemies)
            {
                CollectionAssert.AreEqual(new[] { "claw", "knife", "charm", "claw" }, Ids(grunt.Items), "Both grunts, in the data's order: the base, then floor 2's, then floor 3's.");
                Assert.AreEqual(4, grunt.Layout.Items.Count, "The screen's layout has a place for each.");
                Assert.AreEqual(100 + 2 * cave.EnemyPowerPerFloorPercent, grunt.Items[1].PowerPercent, "A bundle's item grows with the depth as the base items do.");
                Assert.AreEqual(7 + 2 * cave.EnemyPowerPerFloorPercent, grunt.Items[3].PowerPercent);
                Assert.AreEqual(ExpeditionRules.DeeperMaxHp(30, 2, cave.EnemyHpPerFloorPercent), grunt.MaxHp, "The HP is the depth's as before.");
            }
        }

        [Test]
        public void BattleSetup_ABossGroup_CarriesTheBaseItemsAlone_ButStillItsDepthsHp()
        {
            StaticData data = DataWithDeepGrunts();
            ExpeditionState expedition = Expedition(data);
            DungeonData cave = data.Dungeons.Get("cave");

            // "lair" is the cave's boss group: the chief and a grunt, on the floor past the last.
            BattleSetup boss = ExpeditionRules.BuildBattleSetup(data, expedition, "lair", 1, 3);
            CollectionAssert.AreEqual(new[] { "claw" }, Ids(boss.Enemies[1].Items), "The escort carries no bundle in the boss group.");
            Assert.AreEqual(ExpeditionRules.DeeperMaxHp(30, 2, cave.EnemyHpPerFloorPercent), boss.Enemies[1].MaxHp);
        }

        [Test]
        public void BattleSetup_APhase_CarriesTheSameBundles_AfterItsOwnItems()
        {
            var phases = new List<EnemyPhase> { new EnemyPhase(50, Grants(("claw", 9))) };
            StaticData data = DataWithDeepGrunts(DeepGrunt(phases));
            ExpeditionState expedition = Expedition(data);

            BattleSetup second = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1, 2);
            Assert.AreEqual(1, second.Enemies[0].Phases.Count);
            CollectionAssert.AreEqual(new[] { "claw", "knife" }, Ids(second.Enemies[0].Phases[0].Items));
            Assert.AreEqual(2, second.Enemies[0].Phases[0].Layout.Items.Count);

            BattleSetup first = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1, 1);
            CollectionAssert.AreEqual(new[] { "claw" }, Ids(first.Enemies[0].Phases[0].Items));
        }

        [Test]
        public void DropLoot_DrawsFromTheBundlesOfTheFloorToo_AndFromTheBaseAloneOnTheFirst()
        {
            // A grunt alone, with a knife from floor 2 and a charm from floor 3; DropCount 3 (the most the balance allows) drops everything it carried,
            // so the pool itself is visible.
            StaticDataParts parts = TestData.Parts(("DropCount", 3));
            parts.Enemies = parts.Enemies.Select(e => e.Id == "grunt"
                ? DeepGrunt(null, new EnemyDepthItems(2, Grants(("knife", 100))), new EnemyDepthItems(3, Grants(("charm", 100))))
                : e).ToList();
            parts.EnemyGroups = parts.EnemyGroups.Concat(new[] { new EnemyGroupData("lone", "cave", 1, 2, false, new[] { "grunt" }) }).ToList();
            var data = new StaticData(parts);
            ExpeditionState expedition = Expedition(data);

            List<ItemOffer> first = ExpeditionRules.DropLoot(data, expedition, new MapNode(99, 1, 0, MapNodeKind.Battle, "lone", new int[0]));
            CollectionAssert.AreEqual(new[] { "claw" }, first.Select(d => d.Id), "The first floor: the base alone.");

            List<ItemOffer> second = ExpeditionRules.DropLoot(data, expedition, new MapNode(98, 2, 0, MapNodeKind.Battle, "lone", new int[0]));
            CollectionAssert.AreEqual(new[] { "claw", "knife" }, second.Select(d => d.Id).OrderBy(id => id), "Floor 2: the base and the first bundle.");

            List<ItemOffer> third = ExpeditionRules.DropLoot(data, expedition, new MapNode(97, 3, 0, MapNodeKind.Battle, "lone", new int[0]));
            CollectionAssert.AreEqual(new[] { "charm", "claw", "knife" }, third.Select(d => d.Id).OrderBy(id => id), "Floor 3: both bundles.");
            Assert.IsTrue(third.All(d => d.Kind == OfferKind.Item && d.Tier == data.Dungeons.Get("cave").ItemTierAt(3, false)));
        }

        [Test]
        public void Data_RefusesABundle_BelowTheSecondFloor_Empty_OutOfOrder_UnknownOrOverTheFrame()
        {
            StringAssert.Contains("Floor must be at least 2", Assert.Throws<DataException>(() => new EnemyDepthItems(1, Grants(("claw", 100)))).Message);
            StringAssert.Contains("a bundle carries 1..", Assert.Throws<DataException>(() => new EnemyDepthItems(2, new List<ItemGrant>())).Message);
            StringAssert.Contains("rising floors (3 then 2)",
                Assert.Throws<DataException>(() => DeepGrunt(null, new EnemyDepthItems(3, Grants(("claw", 100))), new EnemyDepthItems(2, Grants(("claw", 100))))).Message);
            StringAssert.Contains("rising floors (2 then 2)",
                Assert.Throws<DataException>(() => DeepGrunt(null, new EnemyDepthItems(2, Grants(("claw", 100))), new EnemyDepthItems(2, Grants(("knife", 100))))).Message);

            StringAssert.Contains("DeepItems: 'axe' does not exist",
                Assert.Throws<DataValidationException>(() => DataWithDeepGrunts(DeepGrunt(null, new EnemyDepthItems(2, Grants(("axe", 100)))))).Message);
            // Three 3x3 ballistas under the claw need ten rows of the start bag: more than the frame's eight.
            StringAssert.Contains("its items from floor 2",
                Assert.Throws<DataValidationException>(() => DataWithDeepGrunts(DeepGrunt(null, new EnemyDepthItems(2, Grants(("ballista", 100), ("ballista", 100), ("ballista", 100)))))).Message);
            Assert.DoesNotThrow(() => DataWithDeepGrunts(DeepGrunt(null, new EnemyDepthItems(2, Grants(("ballista", 100), ("ballista", 100))))), "Seven rows fit.");
        }
    }
}
