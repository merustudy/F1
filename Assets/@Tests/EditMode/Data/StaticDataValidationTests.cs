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
                p.Jobs = Replace(p.Jobs, j => j.Id == "tank", new JobData("tank", TestData.Text("tank"), 100, 3, "axe", 10, 1, null))));

            StringAssert.Contains("Enemy 'grunt' Items: 'fang' does not exist", ProblemOf(p =>
                p.Enemies = Replace(p.Enemies, e => e.Id == "grunt", new EnemyData("grunt", TestData.Text("grunt"), 2, 30, new List<ItemGrant> { new ItemGrant("fang", 5) }))));

            StringAssert.Contains("EnemyGroup 'pair' enemies: 'ghost' does not exist", ProblemOf(p =>
                p.EnemyGroups = Replace(p.EnemyGroups, g => g.Id == "pair", new EnemyGroupData("pair", "cave", 1, 2, false, new[] { "ghost" }))));

            StringAssert.Contains("EnemyGroup 'pair' DungeonId: 'tower' does not exist", ProblemOf(p =>
                p.EnemyGroups = Replace(p.EnemyGroups, g => g.Id == "pair", new EnemyGroupData("pair", "tower", 1, 2, false, new[] { "grunt" }))));

            StringAssert.Contains("Dungeon 'cave' AffinityId: 'regen' does not exist", ProblemOf(p =>
                p.Dungeons = new List<DungeonData> { new DungeonData("cave", TestData.Text("cave"), "regen", 2, 2, 3, 30, 2, 8, 2, new List<string> { "tonic" }) }));

            StringAssert.Contains("Dungeon 'cave' StartingPotions: 'elixir' does not exist", ProblemOf(p =>
                p.Dungeons = new List<DungeonData> { new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 30, 2, 8, 2, new List<string> { "elixir" }) }));

            StringAssert.Contains("Mercenary 'anna' JobId: 'samurai' does not exist", ProblemOf(p =>
                p.Mercenaries = Replace(p.Mercenaries, m => m.Id == "anna", new MercenaryData("anna", TestData.Text("anna"), "samurai"))));
        }

        [Test]
        public void EnemyLevel_AtOrAboveFinalBossLevel_IsRejected()
        {
            string problem = ProblemOf(
                p => p.Enemies = Replace(p.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 14, 120, new List<ItemGrant> { new ItemGrant("claw", 12) })),
                ("FinalBossLevel", 14));

            StringAssert.Contains("'chief'", problem);
            StringAssert.Contains("reserved for the final boss", problem);
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
        public void Roster_CannotBeSmallerThanMinPartySize()
        {
            string problem = ProblemOf(p => p.Mercenaries = p.Mercenaries.Take(1).ToList(), ("MinPartySize", 2));

            StringAssert.Contains("starting roster is smaller than MinPartySize", problem);
        }

        [Test]
        public void SomethingMustBeRewardable()
        {
            string problem = ProblemOf(p =>
            {
                p.Items = p.Items.Select(i => new ItemData(i.Id, i.Name, i.Category, i.Size, i.CooldownMs, i.Rows, i.Effects, 0)).ToList();
                p.Potions = p.Potions.Select(x => new PotionData(x.Id, x.Name, x.Effect, x.Magnitude, 0)).ToList();
            });

            StringAssert.Contains("RewardWeight", problem);
        }

        [Test]
        public void AJobsWeapon_MustFitItsBoard()
        {
            // The striker has two cells; a three-cell weapon cannot even be carried.
            string problem = ProblemOf(p =>
                p.Jobs = Replace(p.Jobs, j => j.Id == "striker", new JobData("striker", TestData.Text("striker"), 80, 2, "ballista", 12, 3, null)));

            StringAssert.Contains("Job 'striker'", problem);
            StringAssert.Contains("'ballista' takes 3 cells but the board has 2", problem);
        }

        [Test]
        public void AnEnemysItems_CannotTakeMoreCellsThanABoardCanHave()
        {
            string problem = ProblemOf(p =>
                p.Enemies = Replace(p.Enemies, e => e.Id == "chief", new EnemyData("chief", TestData.Text("chief"), 9, 120, new List<ItemGrant>
                {
                    new ItemGrant("ballista", 12),
                    new ItemGrant("ballista", 12),
                    new ItemGrant("ballista", 12),
                })));

            StringAssert.Contains("Enemy 'chief'", problem);
            StringAssert.Contains("take 9 cells", problem);
        }

        [Test]
        public void DuplicateIds_AndMissingDefinitions_AreAllReported()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs.Concat(new[] { new JobData("tank", TestData.Text("again"), 100, 3, "blade", 10, 1, null) }).ToList();
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
