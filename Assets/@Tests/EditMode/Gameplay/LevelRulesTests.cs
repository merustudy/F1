using System;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Levels and experience (Slice C stage 27, Docs/Design/05_Mercenary_Growth.md §2): the settlement gives every survivor the same experience
    /// (floors reached and a clear), a mercenary climbs LevelData's ladder as it meets each level's ExpToNext and keeps nothing at the last
    /// level, a level adds its bonus to the job's maximum HP when the mercenary leaves, a hired mercenary starts at level 1, and a dead one's
    /// level goes with it. The test data's ladder: 10 to leave level 1, 20 to leave level 2, level 3 last; +10% and +25% HP.
    /// </summary>
    public sealed class LevelRulesTests
    {
        static RunState RunWithParty(StaticData data)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            return run;
        }

        /// <summary>An expedition to the cave that ended on the node of the given floor (the boss node is floor 3 of the two-floor cave).</summary>
        static ExpeditionState Ended(StaticData data, RunState run, ExpeditionResult result, int floor, params string[] dead)
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == floor).Id;
            foreach (ExpeditionMember member in expedition.Members.Where(m => dead.Contains(m.MercenaryId)))
            {
                member.Alive = false;
                member.Hp = 0;
            }

            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = result;
            return expedition;
        }

        [Test]
        public void TheLadder_RunsFromOne_AndTheLastLevelTakesNothing()
        {
            StaticData data = TestData.Data();
            Assert.AreEqual(3, data.MaxLevel);
            Assert.AreEqual(1, data.LevelAt(1).Level);
            Assert.IsTrue(data.LevelAt(3).IsLast);
            Assert.Throws<ArgumentOutOfRangeException>(() => data.LevelAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => data.LevelAt(4));
        }

        [Test]
        public void Gain_KeepsTheExperience_TheBarracksClimbsOneLevelAClick_AndTheLastLevelKeepsNothing()
        {
            // Slice D stage 35: the experience only builds up; LevelUp climbs one level and carries the rest.
            StaticData data = TestData.Data();
            var mercenary = new MercenaryState { Id = "anna", JobId = "tank", Level = 1 };

            Assert.IsFalse(LevelRules.Gain(data, mercenary, 7));
            Assert.AreEqual(7, mercenary.Exp);
            Assert.AreEqual(10, LevelRules.ExpToNext(data, mercenary));
            Assert.Throws<InvalidOperationException>(() => LevelRules.LevelUp(data, mercenary, 1UL), "7 of 10: not yet.");

            Assert.IsTrue(LevelRules.Gain(data, mercenary, 5), "7 + 5 = 12 meets the 10.");
            Assert.AreEqual(1, mercenary.Level, "The settlement climbs nothing.");
            Assert.AreEqual(12, mercenary.Exp);
            LevelRules.LevelUp(data, mercenary, 1UL);
            Assert.AreEqual(2, mercenary.Level);
            Assert.AreEqual(2, mercenary.Exp, "The rest is carried.");
            Assert.IsFalse(LevelRules.CanLevelUp(data, mercenary));
            Assert.AreEqual(20, LevelRules.ExpToNext(data, mercenary));

            // Two levels' worth wait for two clicks; at the last level the experience is 0 and stays so.
            var fresh = new MercenaryState { Id = "ben", JobId = "healer", Level = 1 };
            Assert.IsTrue(LevelRules.Gain(data, fresh, 100));
            LevelRules.LevelUp(data, fresh, 1UL);
            Assert.AreEqual(2, fresh.Level);
            Assert.AreEqual(90, fresh.Exp);
            Assert.IsTrue(LevelRules.CanLevelUp(data, fresh));
            LevelRules.LevelUp(data, fresh, 1UL);
            Assert.AreEqual(3, fresh.Level);
            Assert.AreEqual(0, fresh.Exp);
            Assert.IsFalse(LevelRules.CanLevelUp(data, fresh), "The last level.");
            Assert.AreEqual(0, LevelRules.ExpToNext(data, fresh));
            Assert.IsFalse(LevelRules.Gain(data, fresh, 50));
            Assert.AreEqual(0, fresh.Exp);
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelRules.Gain(data, fresh, -1));
        }

        [Test]
        public void ALevel_NoLongerAddsToTheMaximumHp_OnlyTheStatsItGrewDo()
        {
            // Slice D stage 34: the level's HP bonus is gone; the maximum is constitution's (2026-10-20 스탯 개정: 0.10 a point in the test balance; the test
            // healer's constitution is 6, the striker's 8).
            StaticData data = TestData.Data();
            Assert.AreEqual(100, LevelRules.MaxHpOf(data, "tank"), "Constitution 10: 1.00 (hundredths).");
            Assert.AreEqual(60, LevelRules.MaxHpOf(data, "healer"));
            Assert.AreEqual(120, LevelRules.MaxHpOf(data, "tank", StatBlock.Uniform(10).With(Stat.Con, 12)), "Constitution 12: two points more.");

            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "anna").Level = 3;
            RunRules.FindMercenary(run, "ben").Stats = StatBlock.Uniform(10).With(Stat.Con, 12);
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");

            Assert.AreEqual(100, expedition.Members[0].MaxHp, "Level 3 and the baseline: constitution 10's.");
            Assert.AreEqual(100, expedition.Members[0].Hp, "Full at the start.");
            Assert.AreEqual(120, expedition.Members[1].MaxHp, "Constitution 12, whatever the job.");
            Assert.AreEqual(80, expedition.Members[2].MaxHp);
            Assert.AreEqual(100, ExpeditionRules.BeginBattle(data, expedition, expedition.Map.Nodes[0].Id).Party[0].MaxHp, "The battle takes the member's maximum.");
        }

        [Test]
        public void TheSettlement_GivesEverySurvivorTheSameExperience_ForTheFloorsAndAClear_AndNamesWhoCanLevelUp()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "ben").Exp = 5;

            // Cleared at the boss: 2 floors x 1 and 5 for the clear = 7. ben had 5: 12 meets his level's 10 — the barracks levels him up (stage 35).
            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Cleared, 3, "cora"));

            Assert.AreEqual(7, report.ExpEarned);
            Assert.AreEqual(7, RunRules.FindMercenary(run, "anna").Exp);
            Assert.AreEqual(1, RunRules.FindMercenary(run, "anna").Level);
            Assert.AreEqual(1, RunRules.FindMercenary(run, "ben").Level, "The settlement climbs nothing.");
            Assert.AreEqual(12, RunRules.FindMercenary(run, "ben").Exp);
            Assert.AreEqual(0, RunRules.FindMercenary(run, "dan").Exp, "Stayed home: nothing.");
            CollectionAssert.AreEqual(new[] { "ben" }, report.ReadyToLevelUp);
            Assert.IsNull(RunRules.FindMercenary(run, "cora"), "Dead: the level dies with her.");

            // Retreated from floor 1: 1 experience. ben left his level-up waiting: he is named again.
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2)));
            report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            Assert.AreEqual(1, report.ExpEarned);
            CollectionAssert.AreEqual(new[] { "ben" }, report.ReadyToLevelUp);
            Assert.AreEqual(8, RunRules.FindMercenary(run, "anna").Exp);
            Assert.AreEqual(13, RunRules.FindMercenary(run, "ben").Exp);

            // The barracks levels him up: one level, the rest carried, saved by the manager (RunManager.LevelUp).
            Assert.IsTrue(RunRules.CanLevelUp(data, run, "ben"));
            Assert.IsFalse(RunRules.CanLevelUp(data, run, "anna"));
            Assert.IsFalse(RunRules.CanLevelUp(data, run, "nobody"));
            RunRules.LevelUp(data, run, "ben");
            Assert.AreEqual(2, RunRules.FindMercenary(run, "ben").Level);
            Assert.AreEqual(3, RunRules.FindMercenary(run, "ben").Exp);
            Assert.Throws<InvalidOperationException>(() => RunRules.LevelUp(data, run, "anna"));
        }

        [Test]
        public void AHiredMercenary_StartsAtLevelOne_AndANewRunsRosterToo()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            Assert.IsTrue(run.Roster.All(m => m.Level == 1 && m.Exp == 0));
            run.Gold = 30;
            MercenaryState hired = RunRules.Hire(data, run, 0);
            Assert.AreEqual(1, hired.Level);
            Assert.AreEqual(0, hired.Exp);
        }
    }
}
