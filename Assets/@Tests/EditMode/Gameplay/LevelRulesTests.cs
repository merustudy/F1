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
            Assert.AreEqual(0, data.LevelAt(1).MaxHpBonusPercent);
            Assert.IsTrue(data.LevelAt(3).IsLast);
            Assert.Throws<ArgumentOutOfRangeException>(() => data.LevelAt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => data.LevelAt(4));
        }

        [Test]
        public void Gain_ClimbsALevelWhenTheExperienceMeetsIt_CarriesTheRest_AndKeepsNothingAtTheLast()
        {
            StaticData data = TestData.Data();
            var mercenary = new MercenaryState { Id = "anna", JobId = "tank", Level = 1 };

            Assert.AreEqual(0, LevelRules.Gain(data, mercenary, 7));
            Assert.AreEqual(1, mercenary.Level);
            Assert.AreEqual(7, mercenary.Exp);
            Assert.AreEqual(10, LevelRules.ExpToNext(data, mercenary));

            Assert.AreEqual(1, LevelRules.Gain(data, mercenary, 5), "7 + 5 = 12: level 2 with 2 to spare.");
            Assert.AreEqual(2, mercenary.Level);
            Assert.AreEqual(2, mercenary.Exp);
            Assert.AreEqual(20, LevelRules.ExpToNext(data, mercenary));

            // Two levels in one leap are climbed one after the other; at the last level the experience is 0 and stays so.
            var fresh = new MercenaryState { Id = "ben", JobId = "healer", Level = 1 };
            Assert.AreEqual(2, LevelRules.Gain(data, fresh, 100));
            Assert.AreEqual(3, fresh.Level);
            Assert.AreEqual(0, fresh.Exp);
            Assert.AreEqual(0, LevelRules.ExpToNext(data, fresh));
            Assert.AreEqual(0, LevelRules.Gain(data, fresh, 50));
            Assert.AreEqual(0, fresh.Exp);
            Assert.Throws<ArgumentOutOfRangeException>(() => LevelRules.Gain(data, fresh, -1));
        }

        [Test]
        public void ALevel_AddsItsBonusToTheJobsMaximumHp_WhenTheMercenaryLeaves()
        {
            StaticData data = TestData.Data();
            Assert.AreEqual(100, LevelRules.MaxHpOf(data, "tank", 1), "The job's 100 (hundredths).");
            Assert.AreEqual(110, LevelRules.MaxHpOf(data, "tank", 2));
            Assert.AreEqual(125, LevelRules.MaxHpOf(data, "tank", 3));
            Assert.AreEqual(66, LevelRules.MaxHpOf(data, "healer", 2), "60 + 10%.");

            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "anna").Level = 3;
            RunRules.FindMercenary(run, "ben").Level = 2;
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");

            Assert.AreEqual(125, expedition.Members[0].MaxHp);
            Assert.AreEqual(125, expedition.Members[0].Hp, "Full at the start.");
            Assert.AreEqual(66, expedition.Members[1].MaxHp);
            Assert.AreEqual(80, expedition.Members[2].MaxHp, "Level 1: the job's.");
            Assert.AreEqual(125, ExpeditionRules.BeginBattle(data, expedition, expedition.Map.Nodes[0].Id).Party[0].MaxHp, "The battle takes the member's maximum.");
        }

        [Test]
        public void TheSettlement_GivesEverySurvivorTheSameExperience_ForTheFloorsAndAClear_AndNamesWhoLeveledUp()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "ben").Exp = 5;

            // Cleared at the boss: 2 floors x 1 and 5 for the clear = 7. ben had 5: 12 takes him to level 2 with 2 to spare.
            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Cleared, 3, "cora"));

            Assert.AreEqual(7, report.ExpEarned);
            Assert.AreEqual(7, RunRules.FindMercenary(run, "anna").Exp);
            Assert.AreEqual(1, RunRules.FindMercenary(run, "anna").Level);
            Assert.AreEqual(2, RunRules.FindMercenary(run, "ben").Level);
            Assert.AreEqual(2, RunRules.FindMercenary(run, "ben").Exp);
            Assert.AreEqual(0, RunRules.FindMercenary(run, "dan").Exp, "Stayed home: nothing.");
            CollectionAssert.AreEqual(new[] { "ben" }, report.LevelUps);
            Assert.IsNull(RunRules.FindMercenary(run, "cora"), "Dead: the level dies with her.");

            // Retreated from floor 1: 1 experience, nobody levels.
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2)));
            report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            Assert.AreEqual(1, report.ExpEarned);
            Assert.IsEmpty(report.LevelUps);
            Assert.AreEqual(8, RunRules.FindMercenary(run, "anna").Exp);
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
