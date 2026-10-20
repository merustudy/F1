using System;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The stats (Slice D stage 32; 2026-10-20 스탯 개정: Docs/Design/05_Mercenary_Growth.md §2, 02_Combat_System.md §4): the arithmetic of StatRules
    /// (power, cooldown, HP, the critical multiplier), an item's school and the stats it answers to, where a mercenary's stats start and how they travel
    /// to the battle, and the balance's and the data's checks. One rule per test.
    /// </summary>
    public sealed class StatRulesTests
    {
        static readonly StatBlock TankStats = new StatBlock(12, 9, 12, 6, 9, 11);

        /// <summary>The test data with the tank's base stats given (the healer's and the striker's name only their constitution).</summary>
        static StaticData DataWithTankStats(params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, TankStats) : job)
                .ToList();
            return new StaticData(parts);
        }

        [Test]
        public void PowerPercent_IsOneHundredAtThePowerBaseline_AndMovesByThePointsAboveOrBelow()
        {
            BalanceData balance = TestData.Balance();
            Assert.AreEqual(100, StatRules.PowerPercent(balance, 10), "The test balance's power baseline is 10.");
            Assert.AreEqual(120, StatRules.PowerPercent(balance, 12));
            Assert.AreEqual(150, StatRules.PowerPercent(balance, 15));
            Assert.AreEqual(60, StatRules.PowerPercent(balance, 6));
            Assert.AreEqual(10, StatRules.PowerPercent(balance, 1), "The lowest stat keeps some power.");
            Assert.AreEqual(110, StatRules.PowerPercent(TestData.Balance(("StatPowerPercentPerPoint", 5)), 12), "The percent per point is data.");
            // The shipped baseline (2026-10-20 스탯 개정): 5, so a job's base values 1..8 stand around it.
            BalanceData shipped = TestData.Balance(("PowerBaseline", 5));
            Assert.AreEqual(100, StatRules.PowerPercent(shipped, 5));
            Assert.AreEqual(130, StatRules.PowerPercent(shipped, 8));
            Assert.AreEqual(60, StatRules.PowerPercent(shipped, 1));
        }

        [Test]
        public void AnItemsSchoolAndCategory_SayWhichStatGrowsIt_AndWhichQuickensIt()
        {
            // 2026-10-20 스탯 개정 (Docs/Design/02 §4): a physical weapon grows with strength, a magic one with intellect (damage and heals alike), an attack
            // or support item with wisdom; defensive gear and the rest with none. Dexterity quickens physical equipment, wisdom magic equipment.
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            ItemData wand = TestData.Item("wand", 1000, EffectKind.Damage, TargetMode.EnemyFront, school: ItemSchool.Magic);
            ItemData staff = TestData.Item("staff", 1000, EffectKind.Heal, TargetMode.AllyLowestHp, school: ItemSchool.Magic);
            ItemData flask = TestData.Item("flask", 1000, EffectKind.Burn, TargetMode.EnemyFront, 2, ItemCategory.Attack, school: ItemSchool.Magic);
            ItemData herbs = TestData.Item("herbs", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Support);
            ItemData buckler = TestData.Item("buckler", 5000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Armor, school: ItemSchool.Physical);
            ItemData plate = TestData.Item("plate", 5000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Armor);
            Assert.AreEqual((Stat?)Stat.Str, blade.ScalesWith);
            Assert.AreEqual((Stat?)Stat.Int, wand.ScalesWith);
            Assert.AreEqual((Stat?)Stat.Int, staff.ScalesWith, "A magic weapon that heals grows with intellect too.");
            Assert.AreEqual((Stat?)Stat.Wis, flask.ScalesWith);
            Assert.AreEqual((Stat?)Stat.Wis, herbs.ScalesWith);
            Assert.IsNull(buckler.ScalesWith, "Defensive gear is outside the power stats.");
            Assert.IsNull(plate.ScalesWith);
            Assert.AreEqual((Stat?)Stat.Dex, blade.QuickenedBy);
            Assert.AreEqual((Stat?)Stat.Wis, wand.QuickenedBy);
            Assert.AreEqual((Stat?)Stat.Dex, buckler.QuickenedBy, "Defensive gear with a school cycles with dexterity.");
            Assert.IsNull(plate.QuickenedBy);
            Assert.IsNull(herbs.QuickenedBy, "No stat quickens an attack or a support item.");
            Assert.IsNull(flask.QuickenedBy);
        }

        [Test]
        public void PowerPercent_OfAnItem_IsItsOwnersStatOfItsAxis_AndFullWithoutEither()
        {
            BalanceData balance = TestData.Balance();
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            ItemData plate = TestData.Item("plate", 5000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Armor);
            Assert.AreEqual(120, StatRules.PowerPercent(balance, TankStats, blade), "Strength 12.");
            Assert.AreEqual(100, StatRules.PowerPercent(balance, null, blade), "An enemy has no stats.");
            Assert.AreEqual(100, StatRules.PowerPercent(balance, TankStats, plate), "An item that grows with no stat.");
            ItemData wand = TestData.Item("wand", 1000, EffectKind.Damage, TargetMode.EnemyBack, school: ItemSchool.Magic);
            Assert.AreEqual(60, StatRules.PowerPercent(balance, TankStats, wand), "Intellect 6: under the baseline.");
            ItemData bow = TestData.Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyBack);
            Assert.AreEqual(120, StatRules.PowerPercent(balance, TankStats, bow), "A physical bow grows with strength too (2026-10-20), not dexterity.");
        }

        [Test]
        public void CooldownPercent_IsTheOwnersQuickeningStat_FromThePowerBaseline_AndFullWithoutOne()
        {
            BalanceData balance = TestData.Balance(("CooldownPercentPerPoint", 1));
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            ItemData wand = TestData.Item("wand", 1000, EffectKind.Damage, TargetMode.EnemyFront, school: ItemSchool.Magic);
            ItemData herbs = TestData.Item("herbs", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Support);
            Assert.AreEqual(96, StatRules.CooldownPercent(balance, StatBlock.Uniform(10).With(Stat.Dex, 14), blade), "Dexterity 14: 4% quicker.");
            Assert.AreEqual(103, StatRules.CooldownPercent(balance, StatBlock.Uniform(10).With(Stat.Dex, 7), blade), "Under the baseline: slower.");
            Assert.AreEqual(100, StatRules.CooldownPercent(balance, StatBlock.Uniform(10).With(Stat.Dex, 14), wand), "Dexterity does not quicken a magic weapon.");
            Assert.AreEqual(96, StatRules.CooldownPercent(balance, StatBlock.Uniform(10).With(Stat.Wis, 14), wand), "Wisdom does.");
            Assert.AreEqual(100, StatRules.CooldownPercent(balance, StatBlock.Uniform(10).With(Stat.Wis, 14), herbs), "Nothing quickens a support item.");
            Assert.AreEqual(100, StatRules.CooldownPercent(balance, null, blade), "An enemy has no stats.");
            Assert.AreEqual(100, StatRules.CooldownPercent(TestData.Balance(), StatBlock.Uniform(10).With(Stat.Dex, 30), blade), "The test balance's 0 per point: nothing quickens.");
            Assert.AreEqual(4, StatRules.CooldownPercent(TestData.Balance(("CooldownPercentPerPoint", 4), ("StatMax", 34)), StatBlock.Uniform(10).With(Stat.Dex, 34), blade), "4 a point at the top of the range: 4%. The balance refuses what would reach 0 (below).");

            Assert.AreEqual(960, StatRules.CooldownMs(balance, 1000, 96));
            Assert.AreEqual(1030, StatRules.CooldownMs(balance, 1000, 103));
            Assert.AreEqual(1000, StatRules.CooldownMs(balance, 1000, 100));
            Assert.AreEqual(200, StatRules.CooldownMs(balance, 205, 90), "Never below MinCooldownMs.");
            Assert.AreEqual(ScaledByTheBattle(1000, 96), StatRules.CooldownMs(balance, 1000, 96), "The battle rounds the same way.");
        }

        /// <summary>The battle's arithmetic for a party item (no affinity): base x 1000 x percent, rounded to the millisecond.</summary>
        static int ScaledByTheBattle(int baseMs, int percent)
        {
            return (int)(((long)baseMs * 1000 * percent + 50000) / 100000);
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
            Assert.AreEqual(StatBlock.Uniform(10).With(Stat.Con, 6), StatRules.BaseOf(data, "healer"), "The test healer names its constitution only.");

            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs.Select(job => job.Id == "healer" ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure) : job).ToList();
            Assert.AreEqual(StatBlock.Uniform(10), StatRules.BaseOf(new StaticData(parts), "healer"), "A job without stats: the baseline, six times.");
        }

        [Test]
        public void MaxHp_IsConstitutionsBands_AndTheJobHasNoneOfItsOwn()
        {
            // 2026-10-20 스탯 개정 (Docs/Design/05 §2): the test balance gives 0.10 a point in every band.
            StaticData data = DataWithTankStats();
            Assert.AreEqual(120, LevelRules.MaxHpOf(data, "tank"), "Constitution 12: 1.20.");
            Assert.AreEqual(70, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10).With(Stat.Con, 7)));
            Assert.AreEqual(100, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10)));
            Assert.AreEqual(60, LevelRules.MaxHpOf(data, "healer"), "The healer's constitution 6, whatever the job.");
            Assert.AreEqual(60, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10).With(Stat.Con, 6)), "The same constitution, the same HP in any job.");

            // The shipped bands: 5 a point to 10, 10 a point to 20, 15 a point above (hundredths in the balance).
            BalanceData shipped = TestData.Balance(("HpPerConPointLow", 500), ("HpPerConPointMid", 1000), ("HpPerConPointHigh", 1500));
            Assert.AreEqual(500, StatRules.MaxHpOf(shipped, 1));
            Assert.AreEqual(3000, StatRules.MaxHpOf(shipped, 6), "The shipped warrior's 30.");
            Assert.AreEqual(5000, StatRules.MaxHpOf(shipped, 10));
            Assert.AreEqual(7000, StatRules.MaxHpOf(shipped, 12), "50 + 2 x 10.");
            Assert.AreEqual(22500, StatRules.MaxHpOf(shipped, 25), "50 + 100 + 5 x 15.");
            Assert.AreEqual(0, StatRules.MaxHpOf(shipped, 0), "No constitution: nothing (StatMin keeps it at 1).");
        }

        [Test]
        public void CritDamagePercent_IsThePhysicalMultiplier_WithTheWeaponsAndTheTraitsPoints_OrTheMagicOne()
        {
            // 2026-10-20 스탯 개정 (Docs/Design/02 §4): the physical multiplier is easy to raise, by a heavy weapon and the traits; the magic one is the balance's.
            BalanceData balance = TestData.Balance(("PhysicalCritDamagePercent", 150), ("MagicCritDamagePercent", 125));
            ItemData blade = TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront);
            ItemData axe = new ItemData("axe", TestData.Text("axe"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, new[] { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront) }, 0,
                critChancePercent: 5, school: ItemSchool.Physical, critDamagePercent: 50);
            ItemData wand = TestData.Item("wand", 1000, EffectKind.Damage, TargetMode.EnemyFront, school: ItemSchool.Magic);
            var traits = new TraitBonus(0, 0, 0, 0, 0, 0, 0, 25);
            Assert.AreEqual(150, StatRules.CritDamagePercent(balance, blade));
            Assert.AreEqual(200, StatRules.CritDamagePercent(balance, axe), "The axe's 50 points.");
            Assert.AreEqual(225, StatRules.CritDamagePercent(balance, axe, traits), "And the traits' 25.");
            Assert.AreEqual(125, StatRules.CritDamagePercent(balance, wand, traits), "Magic: the balance's, whatever the traits.");
        }

        [Test]
        public void NewRun_HiresAndDeparture_CarryTheStats_ToTheBattle()
        {
            StaticData data = DataWithTankStats(("RosterLimit", 8));
            RunState run = RunRules.NewRun(data, 7);
            MercenaryState tank = run.Roster.First(m => m.JobId == "tank");
            Assert.AreEqual(TankStats, tank.Stats, "A new run's roster has its jobs' base stats.");
            Assert.IsTrue(run.Roster.All(m => m.Stats.Equals(StatRules.BaseOf(data, m.JobId))));
            Assert.IsTrue(run.Recruits.Count > 0);
            Assert.IsTrue(run.Recruits.All(r => r.Stats.Equals(StatRules.BaseOf(data, r.JobId))), "A candidate has its job's base stats (the spread is stage 34).");

            run.Gold = 1000;
            RecruitState candidate = run.Recruits[0];
            MercenaryState hired = RunRules.Hire(data, run, 0);
            Assert.AreSame(candidate.Stats, hired.Stats, "The hired one joins with the candidate's stats.");

            // The tank alone in the party: the expedition member and the battle unit carry the stats; the HP is its constitution's.
            tank.Stats = TankStats.With(Stat.Str, 14);
            RunRules.PlaceInParty(data, run, tank.Id, 1);
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            ExpeditionMember member = expedition.Members.Single();
            Assert.AreSame(tank.Stats, member.Stats);
            Assert.AreEqual(120, member.MaxHp);
            BattleSetup setup = ExpeditionRules.BuildBattleSetup(data, expedition, "pair", 1);
            Assert.AreSame(tank.Stats, setup.Party.Single().Stats);
            Assert.IsTrue(setup.Enemies.All(e => e.Stats == null), "Enemies have no stats.");
        }

        [Test]
        public void Balance_RefusesAStatRange_ThatLeavesTheLowestStatNoPower_TheHighestNoCooldown_OrBandsOutOfOrder()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("StatBaseline", 31)), "The baseline lies within StatMin..StatMax.");
            Assert.Throws<DataException>(() => TestData.Balance(("StatMin", 11)));
            Assert.Throws<DataException>(() => TestData.Balance(("PowerBaseline", 31)), "So does the power baseline.");
            Assert.Throws<DataException>(() => TestData.Balance(("FatigueBaseline", 0)), "And the fatigue baseline.");
            Assert.Throws<DataException>(() => TestData.Balance(("StatPowerPercentPerPoint", 12)), "10 - 9 x 12 < 1%.");
            Assert.DoesNotThrow(() => TestData.Balance(("StatPowerPercentPerPoint", 11)), "100 - 9 x 11 = 1%.");
            Assert.Throws<DataException>(() => TestData.Balance(("CooldownPercentPerPoint", 5)), "100 - 20 x 5 < 1%.");
            Assert.DoesNotThrow(() => TestData.Balance(("CooldownPercentPerPoint", 4)));
            Assert.Throws<DataException>(() => TestData.Balance(("HpBandLow", 20)), "The low band ends under the middle one.");
        }

        [Test]
        public void Data_RefusesJobStatsOutsideTheRange_ASupportItemWithASchool_AndACritMultiplierOffAPhysicalWeapon_AndAWeaponWithoutOneIsPhysical()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = parts.Jobs.Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, TankStats.With(Stat.Int, 31)) : job).ToList();
            StringAssert.Contains("Int = 31 is outside StatMin..StatMax", Assert.Throws<DataValidationException>(() => new StaticData(parts)).Message);

            var damage = new[] { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront) };
            var shield = new[] { TestData.Effect(EffectKind.Shield, TargetMode.Self) };
            Assert.AreEqual((ItemSchool?)ItemSchool.Physical, new ItemData("bare", TestData.Text("bare"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, damage, 0).School, "A weapon without a school is physical.");
            Assert.AreEqual((ItemSchool?)ItemSchool.Physical, new ItemData("bare", TestData.Text("bare"), ItemCategory.Attack, 1, 1, 1000, RowSpan.All, damage, 0).School);
            Assert.AreEqual((ItemSchool?)ItemSchool.Magic, new ItemData("wand", TestData.Text("wand"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, damage, 0, school: ItemSchool.Magic).School);
            StringAssert.Contains("only a weapon, an attack item or defensive gear has a School",
                Assert.Throws<DataException>(() => new ItemData("calm", TestData.Text("calm"), ItemCategory.Support, 1, 1, 1000, RowSpan.All, shield, 0, school: ItemSchool.Magic)).Message);
            StringAssert.Contains("only a weapon, an attack item or defensive gear has a School",
                Assert.Throws<DataException>(() => new ItemData("stone", TestData.Text("stone"), ItemCategory.Other, 1, 1, 0, RowSpan.All, new ItemEffect[0], 0, stars: new[] { new StarSquare(1, 0) }, starDamage: 100, school: ItemSchool.Physical)).Message);
            Assert.DoesNotThrow(() => new ItemData("buckler", TestData.Text("buckler"), ItemCategory.Armor, 1, 1, 5000, RowSpan.All, shield, 0, school: ItemSchool.Physical), "Defensive gear may have one.");
            Assert.DoesNotThrow(() => new ItemData("plate", TestData.Text("plate"), ItemCategory.Armor, 1, 1, 5000, RowSpan.All, shield, 0), "Or none.");

            StringAssert.Contains("only a physical weapon with a CritChancePercent can have a CritDamagePercent",
                Assert.Throws<DataException>(() => TestData.Item("wand", 1000, EffectKind.Damage, TargetMode.EnemyFront, school: ItemSchool.Magic, critDamagePercent: 50)).Message);
            StringAssert.Contains("only a physical weapon with a CritChancePercent can have a CritDamagePercent",
                Assert.Throws<DataException>(() => TestData.Item("dull", 1000, EffectKind.Damage, TargetMode.EnemyFront, critDamagePercent: 50)).Message);
            StringAssert.Contains("CritDamagePercent must be 0..500",
                Assert.Throws<DataException>(() => new ItemData("axe", TestData.Text("axe"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, damage, 0, critChancePercent: 5, school: ItemSchool.Physical, critDamagePercent: 501)).Message);
            Assert.AreEqual(50, new ItemData("axe", TestData.Text("axe"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, damage, 0, critChancePercent: 5, school: ItemSchool.Physical, critDamagePercent: 50).CritDamagePercent);
        }
    }
}
