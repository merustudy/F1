using System;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The stats (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2, 02_Combat_System.md §4): the arithmetic of StatRules, where a mercenary's
    /// stats start and how they travel to the battle, the HP constitution adds, and the balance's checks. One rule per test.
    /// </summary>
    public sealed class StatRulesTests
    {
        static readonly StatBlock TankStats = new StatBlock(12, 9, 12, 6, 9, 11);

        /// <summary>The test data with the tank's base stats given (the other test jobs name none: the baseline).</summary>
        static StaticData DataWithTankStats(params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.MaxHp, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, TankStats) : job)
                .ToList();
            return new StaticData(parts);
        }

        [Test]
        public void PowerPercent_IsOneHundredAtTheBaseline_AndMovesByThePointsAboveOrBelow()
        {
            BalanceData balance = TestData.Balance();
            Assert.AreEqual(100, StatRules.PowerPercent(balance, 10));
            Assert.AreEqual(120, StatRules.PowerPercent(balance, 12));
            Assert.AreEqual(150, StatRules.PowerPercent(balance, 15));
            Assert.AreEqual(60, StatRules.PowerPercent(balance, 6));
            Assert.AreEqual(10, StatRules.PowerPercent(balance, 1), "The lowest stat keeps some power.");
            Assert.AreEqual(110, StatRules.PowerPercent(TestData.Balance(("StatPowerPercentPerPoint", 5)), 12), "The percent per point is data.");
        }

        [Test]
        public void PowerPercent_OfAnItem_IsItsOwnersStatOfItsAxis_AndFullWithoutEither()
        {
            BalanceData balance = TestData.Balance();
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront, scalesWith: Stat.Str);
            ItemData plain = TestData.Item("plain", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            Assert.AreEqual(120, StatRules.PowerPercent(balance, TankStats, blade), "Strength 12.");
            Assert.AreEqual(100, StatRules.PowerPercent(balance, null, blade), "An enemy has no stats.");
            Assert.AreEqual(100, StatRules.PowerPercent(balance, TankStats, plain), "An item that grows with no stat.");
            ItemData bow = TestData.Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyBack, scalesWith: Stat.Dex);
            Assert.AreEqual(90, StatRules.PowerPercent(balance, TankStats, bow), "Dexterity 9: under the baseline.");
        }

        [Test]
        public void Scale_RoundsDown_AndScaleStacks_RoundsHalfUp_BothAtLeastOne()
        {
            Assert.AreEqual(321, StatRules.Scale(268, 120), "2.68 x 1.2 = 3.216 -> 3.21.");
            Assert.AreEqual(268, StatRules.Scale(268, 100));
            Assert.AreEqual(1, StatRules.Scale(5, 10), "0.5 hundredths -> at least 1.");
            Assert.AreEqual(2, StatRules.ScaleStacks(2, 120), "2.4 stacks -> 2.");
            Assert.AreEqual(3, StatRules.ScaleStacks(2, 125), "2.5 -> 3: half up.");
            Assert.AreEqual(3, StatRules.ScaleStacks(2, 130));
            Assert.AreEqual(1, StatRules.ScaleStacks(2, 10), "0.2 -> at least 1.");
        }

        [Test]
        public void BaseOf_IsTheJobsStats_OrTheBaselineForAJobThatNamesNone()
        {
            StaticData data = DataWithTankStats();
            Assert.AreEqual(TankStats, StatRules.BaseOf(data, "tank"));
            Assert.AreEqual(StatBlock.Uniform(10), StatRules.BaseOf(data, "healer"));
        }

        [Test]
        public void MaxHp_AddsConstitutionsPercent()
        {
            StaticData data = DataWithTankStats();
            // The tank's 100 hundredths: constitution 12 adds 10% (stage 34: the level adds nothing of its own).
            Assert.AreEqual(110, LevelRules.MaxHpOf(data, "tank"));
            Assert.AreEqual(85, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10).With(Stat.Con, 7)), "Constitution 7: 15% less.");
            Assert.AreEqual(100, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10)));
            Assert.AreEqual(10, StatRules.HpPercent(data.Balance, TankStats));
        }

        [Test]
        public void NewRun_HiresAndDeparture_CarryTheStats_ToTheBattle()
        {
            StaticData data = DataWithTankStats(("RosterLimit", 8));
            RunState run = RunRules.NewRun(data, 7);
            MercenaryState tank = run.Roster.First(m => m.JobId == "tank");
            Assert.AreEqual(TankStats, tank.Stats, "A new run's roster has its jobs' base stats.");
            Assert.IsTrue(run.Roster.Where(m => m.JobId != "tank").All(m => m.Stats.Equals(StatBlock.Uniform(10))));
            Assert.IsTrue(run.Recruits.Count > 0);
            Assert.IsTrue(run.Recruits.All(r => r.Stats.Equals(StatRules.BaseOf(data, r.JobId))), "A candidate has its job's base stats (the spread is stage 34).");

            run.Gold = 1000;
            RecruitState candidate = run.Recruits[0];
            MercenaryState hired = RunRules.Hire(data, run, 0);
            Assert.AreSame(candidate.Stats, hired.Stats, "The hired one joins with the candidate's stats.");

            // The tank alone in the party: the expedition member and the battle unit carry the stats; the HP counts constitution.
            tank.Stats = TankStats.With(Stat.Str, 14);
            RunRules.PlaceInParty(data, run, tank.Id, 1);
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            ExpeditionMember member = expedition.Members.Single();
            Assert.AreSame(tank.Stats, member.Stats);
            Assert.AreEqual(110, member.MaxHp);
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1);
            Assert.AreSame(tank.Stats, setup.Party.Single().Stats);
            Assert.IsTrue(setup.Enemies.All(e => e.Stats == null), "Enemies have no stats.");
        }

        [Test]
        public void Balance_RefusesAStatRange_ThatLeavesTheLowestStatNoPower_OrNoHp()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("StatBaseline", 31)), "The baseline lies within StatMin..StatMax.");
            Assert.Throws<DataException>(() => TestData.Balance(("StatMin", 11)));
            Assert.Throws<DataException>(() => TestData.Balance(("StatPowerPercentPerPoint", 12)), "10 - 9 x 12 < 1%.");
            Assert.Throws<DataException>(() => TestData.Balance(("HpPercentPerConPoint", 12)));
            Assert.DoesNotThrow(() => TestData.Balance(("StatPowerPercentPerPoint", 11)), "100 - 9 x 11 = 1%.");
        }

        [Test]
        public void Data_RefusesJobStatsOutsideTheRange_AndAnItemAxisThatIsNoPowerStat()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs.Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.MaxHp, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, TankStats.With(Stat.Int, 31)) : job).ToList();
            StringAssert.Contains("Int = 31 is outside StatMin..StatMax", Assert.Throws<DataValidationException>(() => new StaticData(parts)).Message);

            Assert.Throws<DataException>(() => TestData.Item("tough", 1000, EffectKind.Damage, TargetMode.EnemyFront, scalesWith: Stat.Con), "Constitution is no item's axis.");
            Assert.Throws<DataException>(() => TestData.Item("calm", 1000, EffectKind.Shield, TargetMode.Self, category: ItemCategory.Support, scalesWith: Stat.End), "Nor endurance.");
            Assert.Throws<DataException>(
                () => new ItemData("stone", TestData.Text("stone"), ItemCategory.Other, 1, 1, 0, RowSpan.All, new ItemEffect[0], 0, stars: new[] { new StarSquare(1, 0) }, starDamage: 100, scalesWith: Stat.Str),
                "An item without effects has no power to grow.");
            Assert.AreEqual(Stat.Spi, TestData.Item("charm", 1000, EffectKind.Heal, TargetMode.Self, category: ItemCategory.Support, scalesWith: Stat.Spi).ScalesWith);
        }
    }
}
