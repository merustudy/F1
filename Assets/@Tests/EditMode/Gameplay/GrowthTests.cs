using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The spread and the growth (Slice D stage 34, Docs/Design/04_Lobby_100Day_Economy.md §6, 05_Mercenary_Growth.md §2): a candidate's stats lie
    /// within RecruitStatSpread of its job's base values, a level-up grows each stat by its job's grade (the hundreds sure, the rest by chance), and the
    /// settlement says what grew. One rule per test.
    /// </summary>
    public sealed class GrowthTests
    {
        static readonly StatBlock TankStats = new StatBlock(12, 9, 12, 6, 9, 11);

        /// <summary>The test data with the tank's base stats and growth grades, and the spread given.</summary>
        static StaticData Data(StatBlock tankGrowth, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Jobs = parts.Jobs
                .Select(job => job.Id == "tank" ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, TankStats, tankGrowth) : job)
                .ToList();
            return new StaticData(parts);
        }

        [Test]
        public void ACandidatesStats_LieWithinTheSpreadOfItsJobsBase_TheSameOnTheSameRun_AndTheStartingRosterHasNone()
        {
            StaticData data = Data(null, ("RecruitStatSpread", 2), ("RecruitCount", 3), ("RosterLimit", 8));
            RunState run = RunRules.NewRun(data, 11);
            Assert.IsTrue(run.Roster.All(m => m.Stats.Equals(StatRules.BaseOf(data, m.JobId))), "No spread for the starting roster.");
            Assert.AreEqual(3, run.Recruits.Count);
            foreach (RecruitState recruit in run.Recruits)
            {
                StatBlock jobBase = StatRules.BaseOf(data, recruit.JobId);
                Assert.IsTrue(StatBlock.All.All(s => System.Math.Abs(recruit.Stats[s] - jobBase[s]) <= 2), recruit.Id);
            }

            Assert.IsTrue(run.Recruits.Any(r => !r.Stats.Equals(StatRules.BaseOf(data, r.JobId))), "Somebody differs from the job's base.");
            RunState again = RunRules.NewRun(data, 11);
            CollectionAssert.AreEqual(run.Recruits.Select(r => r.Id + r.Stats), again.Recruits.Select(r => r.Id + r.Stats), "The same run draws the same candidates.");
        }

        [Test]
        public void ACandidatesStats_StayWithinStatMinAndStatMax()
        {
            // A spread of 10 around the test jobs' baseline 10 would reach 0 and 20: StatMin 5 and StatMax 15 hold them.
            StaticData data = Data(null, ("RecruitStatSpread", 10), ("RecruitCount", 3), ("StatMin", 5), ("StatMax", 15), ("StatPowerPercentPerPoint", 5));
            for (ulong seed = 1; seed <= 20; seed++)
            {
                foreach (RecruitState recruit in RunRules.NewRun(data, seed).Recruits)
                {
                    Assert.IsNull(recruit.Stats.FirstOutside(5, 15), recruit.Id);
                }
            }
        }

        [Test]
        public void ALevelUp_GrowsEachStat_ByTheHundredsSure_AndTheRestByChance()
        {
            // 200 is two sure points, 100 one, 0 none; 50 one by a coin's chance.
            StaticData data = Data(new StatBlock(200, 100, 0, 50, 50, 50));
            var tank = new MercenaryState { Id = "anna", JobId = "tank", Level = 2, Stats = TankStats };
            var rng = new Pcg32(7, RngStream.Growth);
            StatBlock gains = LevelRules.Grow(data, tank, 1, rng);
            Assert.AreEqual(2, gains.Str);
            Assert.AreEqual(1, gains.Dex);
            Assert.AreEqual(0, gains.Con);
            Assert.IsTrue(new[] { gains.Int, gains.Wis, gains.End }.All(g => g == 0 || g == 1));
            Assert.AreEqual(TankStats.Str + 2, tank.Stats.Str, "The mercenary's stats grew.");

            // Over many level-ups the chance shows: about half of them give the point.
            var many = new MercenaryState { Id = "anna", JobId = "tank", Level = 1, Stats = StatBlock.Uniform(1) };
            StatBlock total = LevelRules.Grow(Data(new StatBlock(0, 0, 0, 50, 0, 0), ("StatMin", 1), ("StatMax", 100)), many, 40, new Pcg32(3, RngStream.Growth));
            Assert.That(total.Int, Is.InRange(10, 30), "About 20 of 40.");
        }

        [Test]
        public void Growth_StopsAtStatMax_AndAJobWithoutGradesGrowsNothing()
        {
            StaticData data = Data(new StatBlock(200, 0, 0, 0, 0, 0));
            var tank = new MercenaryState { Id = "anna", JobId = "tank", Level = 1, Stats = TankStats.With(Stat.Str, 29) };
            StatBlock gains = LevelRules.Grow(data, tank, 3, new Pcg32(1, RngStream.Growth));
            Assert.AreEqual(30, tank.Stats.Str, "StatMax 30.");
            Assert.AreEqual(1, gains.Str);

            var healer = new MercenaryState { Id = "ben", JobId = "healer", Level = 1, Stats = StatBlock.Uniform(10) };
            Assert.AreEqual(StatBlock.Uniform(0), LevelRules.Grow(data, healer, 2, new Pcg32(1, RngStream.Growth)));
            Assert.AreEqual(StatBlock.Uniform(10), healer.Stats);
        }

        [Test]
        public void TheBarracksLevelUp_GrowsByTheJob_TheSameForTheMercenaryAndLevel_WhateverTheOrderOfTheClicks()
        {
            // Slice D stage 35: the settlement only names who can level up; the barracks rolls, from the run seed for this mercenary and this level.
            StaticData data = Data(new StatBlock(110, 50, 90, 20, 50, 50), ("RosterLimit", 8));
            RunState Home(out SettlementReport report)
            {
                RunState run = RunRules.NewRun(data, 5);
                foreach (MercenaryState mercenary in run.Roster.Where(m => m.JobId == "tank"))
                {
                    mercenary.Exp = 9;
                }

                MercenaryState tank = run.Roster.First(m => m.JobId == "tank");
                RunRules.PlaceInParty(data, run, tank.Id, 1);
                ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
                expedition.Phase = ExpeditionPhase.Finished;
                expedition.Result = ExpeditionResult.Retreated;
                expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == 1).Id;
                report = RunRules.Settle(data, run, expedition);
                return run;
            }

            RunState first = Home(out SettlementReport settled);
            Assert.AreEqual(1, settled.ReadyToLevelUp.Count, "One floor's experience takes the tank from 9 to its level's 10.");
            string id = settled.ReadyToLevelUp[0];
            MercenaryState before = RunRules.FindMercenary(first, id);
            Assert.AreEqual(TankStats, before.Stats, "Nothing grows at the settlement.");

            StatBlock gains = RunRules.LevelUp(data, first, id);
            Assert.That(gains.Str, Is.InRange(1, 2), "110: one sure, one by 10%.");
            Assert.AreEqual(TankStats.Str + gains.Str, RunRules.FindMercenary(first, id).Stats.Str);

            // The same run, the same mercenary and level: the same roll, even after another mercenary was levelled up first.
            RunState second = Home(out SettlementReport _);
            MercenaryState other = second.Roster.First(m => m.Id != id);
            other.Exp = 10;
            RunRules.LevelUp(data, second, other.Id);
            Assert.AreEqual(gains, RunRules.LevelUp(data, second, id), "The order of the clicks does not change the roll.");
            Assert.AreEqual(RunRules.FindMercenary(first, id).Stats, RunRules.FindMercenary(second, id).Stats);
        }
    }
}
