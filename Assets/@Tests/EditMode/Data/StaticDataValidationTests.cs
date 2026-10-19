using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Data set checks: references between definitions and rules that span definitions.</summary>
    public sealed class StaticDataValidationTests
    {
        static string ProblemOf(Action<StaticDataParts> breakIt, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            breakIt(parts);
            var exception = Assert.Throws<DataValidationException>(() => new StaticData(parts));
            return string.Join("\n", exception.Problems);
        }

        static List<T> Replace<T>(IEnumerable<T> items, Func<T, bool> match, T replacement)
        {
            return items.Select(item => match(item) ? replacement : item).ToList();
        }

        [Test]
        public void ATraitsOpposite_MustExist_AndTheStartingRostersTraits_MustExist_BeHeldOnce_AndNotClash()
        {
            // Slice D stage 37.
            StringAssert.Contains("Excludes: 'ghost' does not exist", ProblemOf(parts => parts.Traits = new List<TraitData> { TestData.Trait("lone", TraitPolarity.Positive, hit: 1, excludes: "ghost") }));
            foreach ((string[] traits, string problem) in new[]
            {
                (new[] { "nobody" }, "'nobody' does not exist"),
                (new[] { "keen", "keen" }, "is there twice"),
                (new[] { "keen", "dull" }, "clashes with another"),
            })
            {
                string problems = ProblemOf(parts =>
                {
                    parts.Traits = TestData.Traits();
                    parts.Mercenaries = Replace(parts.Mercenaries, m => m.Id == "anna", new MercenaryData("anna", TestData.Text("anna"), "tank", true, traits));
                });
                StringAssert.Contains(problem, problems);
            }
        }

        [Test]
        public void ATrait_MustChangeSomething_WithinItsBounds_AndAPoolNameHasNoTraits()
        {
            Assert.Throws<DataException>(() => TestData.Trait("idle", TraitPolarity.Positive), "Changes nothing.");
            Assert.Throws<DataException>(() => TestData.Trait("wild", TraitPolarity.Positive, hit: 101));
            Assert.Throws<DataException>(() => TestData.Trait("self", TraitPolarity.Positive, hit: 1, excludes: "self"));
            Assert.Throws<DataException>(() => TestData.Trait("heavy", TraitPolarity.Positive, hit: 1, weight: -1));
            // Stage 38: an acquired trait names what leaves it; an innate one names nothing.
            Assert.Throws<DataException>(() => new TraitData("lost", TestData.Text("lost"), TraitKind.Acquired, TraitPolarity.Negative, null, 10, null, -1, 0, 0, 0, 0, 0, 0));
            Assert.Throws<DataException>(() => new TraitData("born", TestData.Text("born"), TraitKind.Innate, TraitPolarity.Positive, TraitSource.Clear, 10, null, 1, 0, 0, 0, 0, 0, 0));
            Assert.Throws<DataException>(() => new MercenaryData("eve", TestData.Text("eve"), null, false, new[] { "keen" }), "A candidate draws its own.");
            Assert.Throws<DataException>(() => TestData.Balance(("RecruitTraitWeight0", 0)), "Some count of traits must be possible.");
        }

        [Test]
        public void ValidParts_BuildData()
        {
            StaticData data = TestData.Data();

            Assert.AreEqual(3, data.Jobs.Count);
            Assert.AreEqual("lair", data.BossGroupOf("cave").Id);
            CollectionAssert.AreEqual(new[] { "pair" }, data.GroupsFor("cave", 1).Select(g => g.Id));
            CollectionAssert.AreEqual(new[] { "pair", "trio" }, data.GroupsFor("cave", 2).Select(g => g.Id));
            CollectionAssert.IsEmpty(data.GroupsFor("cave", 3));
        }

        [Test]
        public void MissingReferences_AreReportedWithTheReferringDefinition()
        {
            StringAssert.Contains("Job 'tank' WeaponItemId: 'axe' does not exist", ProblemOf(p =>
                p.Jobs = Replace(p.Jobs, j => j.Id == "tank", new JobData("tank", TestData.Text("tank"), 100, "axe", 1, null))));

            StringAssert.Contains("Enemy 'grunt' Items: 'fang' does not exist", ProblemOf(p =>
                p.Enemies = Replace(p.Enemies, e => e.Id == "grunt", new EnemyData("grunt", TestData.Text("grunt"), 2, 30, new List<ItemGrant> { new ItemGrant("fang", 5) }))));

            StringAssert.Contains("EnemyGroup 'pair' enemies: 'ghost' does not exist", ProblemOf(p =>
                p.EnemyGroups = Replace(p.EnemyGroups, g => g.Id == "pair", new EnemyGroupData("pair", "cave", 1, 2, false, new[] { "ghost" }))));

            StringAssert.Contains("EnemyGroup 'pair' DungeonId: 'tower' does not exist", ProblemOf(p =>
                p.EnemyGroups = Replace(p.EnemyGroups, g => g.Id == "pair", new EnemyGroupData("pair", "tower", 1, 2, false, new[] { "grunt" }))));

            StringAssert.Contains("Dungeon 'cave' AffinityId: 'regen' does not exist", ProblemOf(p =>
                p.Dungeons = new List<DungeonData> { new DungeonData("cave", TestData.Text("cave"), "regen", 2, 2, 3, 2, new List<string> { "tonic" }) }));

            StringAssert.Contains("Dungeon 'cave' StartingPotions: 'elixir' does not exist", ProblemOf(p =>
                p.Dungeons = new List<DungeonData> { new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, new List<string> { "elixir" }) }));

            StringAssert.Contains("Mercenary 'anna' JobId: 'samurai' does not exist", ProblemOf(p =>
                p.Mercenaries = Replace(p.Mercenaries, m => m.Id == "anna", new MercenaryData("anna", TestData.Text("anna"), "samurai"))));
        }

        [Test]
        public void EnemyLevel_AtOrAboveFinalBossLevel_IsRejected_UnlessTheEnemyIsOfTheFinalDungeonsBossGroup()
        {
            // The chief of the cave's lair at the final boss's level, standing nowhere in the pit: refused. Above it: refused even in the pit's boss group (stage 28).
            string problem = ProblemOf(
                p =>
                {
                    p.EnemyGroups = p.EnemyGroups.Where(g => g.Id != "pit_lord").Concat(new[] { new EnemyGroupData("pit_lord", "pit", 0, 0, true, new[] { "grunt", "grunt" }) }).ToList();
                    p.Enemies = Replace(p.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 14, 120, new List<ItemGrant> { new ItemGrant("claw", 12) }));
                },
                ("FinalBossLevel", 14));
            StringAssert.Contains("'chief'", problem);
            StringAssert.Contains("reserved for the final boss", problem);

            // The pit's lord (pit_lord: chief + chief) is the final dungeon's boss group: a chief that stands only there may carry the level.
            StaticDataParts parts = TestData.Parts(("FinalBossLevel", 14));
            parts.EnemyGroups = parts.EnemyGroups.Where(g => g.Id != "lair").Concat(new[] { new EnemyGroupData("lair", "cave", 0, 0, true, new[] { "grunt", "grunt" }) }).ToList();
            parts.Enemies = Replace(parts.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 14, 120, new List<ItemGrant> { new ItemGrant("claw", 12) }));
            Assert.DoesNotThrow(() => new StaticData(parts));
            parts.Enemies = Replace(parts.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 15, 120, new List<ItemGrant> { new ItemGrant("claw", 12) }));
            StringAssert.Contains("reserved for the final boss", string.Join("\n", Assert.Throws<DataValidationException>(() => new StaticData(parts)).Problems));
        }

        [Test]
        public void Dungeon_NeedsExactlyOneBossGroup()
        {
            StringAssert.Contains("needs exactly one boss group, found 0", ProblemOf(p =>
                p.EnemyGroups = p.EnemyGroups.Where(g => !g.IsBoss).ToList()));

            StringAssert.Contains("needs exactly one boss group, found 2", ProblemOf(p =>
                p.EnemyGroups = p.EnemyGroups.Concat(new[] { new EnemyGroupData("lair_two", "cave", 0, 0, true, new[] { "chief" }) }).ToList()));
        }

        [Test]
        public void Dungeon_NeedsAGroupForEveryBattleFloor()
        {
            string problem = ProblemOf(p => p.EnemyGroups = p.EnemyGroups.Where(g => g.Id != "pair").ToList());

            StringAssert.Contains("no enemy group can appear on floor 1", problem);
        }

        [Test]
        public void StartingPotions_CannotExceedPotionSlots()
        {
            string problem = ProblemOf(p => { }, ("PotionSlots", 0));

            StringAssert.Contains("more starting potions than PotionSlots", problem);
        }

        [Test]
        public void Roster_CannotBeLargerThanRosterLimit_AndThePoolNotSmallerThanRecruitCount()
        {
            StringAssert.Contains("starting roster is larger than RosterLimit", ProblemOf(p => { }, ("RosterLimit", 3)));
            StringAssert.Contains("recruit pool (5 names) is smaller than RecruitCount", ProblemOf(p => { }, ("RecruitCount", 6)));
            Assert.DoesNotThrow(() => TestData.Data(("RecruitCount", 5)));
        }

        [Test]
        public void Roster_CannotBeSmallerThanMinPartySize()
        {
            string problem = ProblemOf(p => p.Mercenaries = p.Mercenaries.Where(m => !m.Starting || m.Id == "anna").ToList(), ("MinPartySize", 2));

            StringAssert.Contains("starting roster is smaller than MinPartySize", problem);
        }

        [Test]
        public void SomethingMustBeStockable()
        {
            string problem = ProblemOf(p =>
            {
                p.Items = p.Items.Select(i => new ItemData(i.Id, i.Name, i.Category, i.Width, i.Height, i.CooldownMs, i.Rows, i.Effects, 0)).ToList();
                p.Potions = p.Potions.Select(x => new PotionData(x.Id, x.Name, x.Effect, x.Magnitude, 0)).ToList();
            });

            StringAssert.Contains("ShopWeight", problem);
        }

        [Test]
        public void AJobsWeapon_MustFitTheStartBag()
        {
            // Slice B stage 19: the weapon lies unturned at the top-left of the start bag (the test pack is 3x2; the ballista is 3x3).
            string problem = ProblemOf(p =>
                p.Jobs = Replace(p.Jobs, j => j.Id == "striker", new JobData("striker", TestData.Text("striker"), 80, "ballista", 3, null)));

            StringAssert.Contains("Job 'striker'", problem);
            StringAssert.Contains("'ballista' (3x3) does not fit the start bag 'pack' (3x2)", problem);
        }

        [Test]
        public void ExactlyOneBag_IsTheStartBag()
        {
            StringAssert.Contains("exactly one bag is the start bag, not 0", ProblemOf(p =>
                p.Bags = p.Bags.Where(bag => !bag.Start).ToList()));
            StringAssert.Contains("exactly one bag is the start bag, not 2", ProblemOf(p =>
                p.Bags = p.Bags.Concat(new[] { new BagData("pack_two", TestData.Text("pack_two"), 3, 3, true, 0, 0, 0) }).ToList()));
        }

        [Test]
        public void AnEnemysItems_MustFitTheStartBagGrownWithinTheFrame()
        {
            string problem = ProblemOf(p =>
                p.Enemies = Replace(p.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 9, 120, new List<ItemGrant>
                {
                    new ItemGrant("ballista", 12),
                    new ItemGrant("ballista", 12),
                    new ItemGrant("ballista", 12),
                })));

            StringAssert.Contains("Enemy 'chief'", problem);
            StringAssert.Contains("need 9 rows of the start bag 'pack' (3 wide), more than a board frame (8)", problem);
        }

        [Test]
        public void DuplicateIds_AndMissingDefinitions_AreAllReported()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs.Concat(new[] { new JobData("tank", TestData.Text("again"), 100, "blade", 1, null) }).ToList();
            parts.Affinities = null;

            var exception = Assert.Throws<DataValidationException>(() => new StaticData(parts));

            string all = string.Join("\n", exception.Problems);
            StringAssert.Contains("Job: duplicate id 'tank'", all);
            StringAssert.Contains("Affinity: no data", all);
            Assert.GreaterOrEqual(exception.Problems.Count, 3, "The dungeon's affinity reference is reported as well.");
        }

        [Test]
        public void InvalidBalance_IsReportedAsAProblem()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Balance = TestData.BalanceEntries().Where(e => e.Key != "RestDays").ToList();

            var exception = Assert.Throws<DataValidationException>(() => new StaticData(parts));

            StringAssert.Contains("'RestDays' is missing", exception.Problems[0]);
        }
    }
}
