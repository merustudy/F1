using System;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The end of a run (Slice C stage 25, Docs/Design/04_Lobby_100Day_Economy.md §2, §4, §8): past the last day only the final stand is left,
    /// clearing the final dungeon wins, and the death limit, a wipeout or a lost final stand loses. The test data's final dungeon is the
    /// pit (one floor and its boss), open after one clear of the cave.
    /// </summary>
    public sealed class RunEndTests
    {
        static RunState RunWithParty(StaticData data)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            return run;
        }

        /// <summary>An expedition to the dungeon that has ended, with the given members dead.</summary>
        static ExpeditionState Finished(StaticData data, RunState run, string dungeonId, ExpeditionResult result, params string[] dead)
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, dungeonId);
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
        public void ALockedDungeon_OpensWithTheClearsOfTheOneItNames()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);

            Assert.AreEqual(DepartCheck.Locked, RunRules.CanDepart(data, run, "pit"));
            Assert.IsFalse(RunRules.IsUnlocked(run, data.Dungeons.Get("pit")));
            Assert.IsTrue(RunRules.IsUnlocked(run, data.Dungeons.Get("cave")), "No unlock named: open from the start.");

            RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Cleared));

            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "pit"));
            Assert.IsFalse(RunRules.BeginExpedition(data, run, "pit").FinalStand, "Going by choice is no final stand.");
        }

        [Test]
        public void PastTheLastDay_OnlyTheFinalStandIsLeft_AndTheFinalDungeonIsOpenForIt()
        {
            // The cave takes 2 days: back on day 3, past a last day of 1.
            StaticData data = TestData.Data(("TotalDays", 1));
            RunState run = RunWithParty(data);
            Assert.IsFalse(RunRules.IsFinalStandDue(data, run));

            RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Retreated));

            Assert.AreEqual(3, run.Day);
            Assert.IsFalse(run.IsOver, "Passing the last day is not a defeat.");
            Assert.IsTrue(RunRules.IsFinalStandDue(data, run));
            Assert.IsFalse(RunRules.CanRest(data, run));
            Assert.Throws<InvalidOperationException>(() => RunRules.Rest(data, run));
            Assert.AreEqual(DepartCheck.FinalStandDue, RunRules.CanDepart(data, run, "cave"));
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "pit"), "Not unlocked (no clear), but open for the final stand.");

            ExpeditionState stand = RunRules.BeginExpedition(data, run, "pit");
            Assert.IsTrue(stand.FinalStand);
            Assert.AreEqual(1, stand.Map.Nodes.Count, "The boss alone.");
            Assert.AreEqual(1, stand.Map.FloorCount);
            Assert.AreEqual(MapNodeKind.Boss, stand.Map.Nodes[0].Kind);
            Assert.AreEqual("pit_lord", stand.Map.Nodes[0].EnemyGroupId);
            CollectionAssert.AreEqual(new[] { 0 }, ExpeditionRules.AvailableNodes(stand).Select(n => n.Id));
        }

        [Test]
        public void ClearingTheFinalDungeon_WinsTheRun()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.ClearedDungeons["cave"] = 1;

            SettlementReport report = RunRules.Settle(data, run, Finished(data, run, "pit", ExpeditionResult.Cleared));

            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
            Assert.AreEqual(DefeatCause.None, run.DefeatCause);
            Assert.IsTrue(run.IsOver);
            Assert.AreEqual(RunOutcome.Victory, report.Outcome);
            Assert.IsTrue(report.RunIsOver);
            Assert.AreEqual(DepartCheck.RunIsOver, RunRules.CanDepart(data, run, "pit"));
            Assert.IsFalse(RunRules.CanRest(data, run));
        }

        [Test]
        public void WinningTheFinalStand_WinsTheRun()
        {
            StaticData data = TestData.Data(("TotalDays", 1));
            RunState run = RunWithParty(data);
            RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Cleared));

            RunRules.Settle(data, run, Finished(data, run, "pit", ExpeditionResult.Cleared));

            Assert.AreEqual(RunOutcome.Victory, run.Outcome);
        }

        [TestCase(ExpeditionResult.Retreated)]
        [TestCase(ExpeditionResult.Wiped)]
        public void LosingTheFinalStand_LosesTheRun(ExpeditionResult result)
        {
            StaticData data = TestData.Data(("TotalDays", 1));
            RunState run = RunWithParty(data);
            RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Cleared));
            string[] dead = result == ExpeditionResult.Wiped ? new[] { "anna", "ben", "cora" } : new string[0];

            SettlementReport report = RunRules.Settle(data, run, Finished(data, run, "pit", result, dead));

            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
            Assert.AreEqual(DefeatCause.FinalStandLost, run.DefeatCause, "Even when the party was wiped out: dan is still home.");
            Assert.AreEqual(DefeatCause.FinalStandLost, report.DefeatCause);
            Assert.IsTrue(run.IsOver);
        }

        [Test]
        public void ReachingTheDeathLimit_LosesTheRun_WhileSomeoneIsStillAlive()
        {
            StaticData data = TestData.Data(("DeathLimit", 2));
            RunState run = RunWithParty(data);

            SettlementReport report = RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Retreated, "anna"));
            Assert.AreEqual(RunOutcome.Ongoing, run.Outcome, "One short of the limit.");

            RunRules.SetParty(data, run, FlowTestKit.Party(("ben", 1), ("cora", 2)));
            report = RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Retreated, "ben"));

            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
            Assert.AreEqual(DefeatCause.DeathLimit, run.DefeatCause);
            Assert.AreEqual(2, run.Roster.Count, "cora and dan live on; the company is finished anyway.");
            Assert.IsTrue(report.RunIsOver);
        }

        [Test]
        public void LosingTheLastMercenary_IsAWipeout_EvenUnderTheDeathLimit()
        {
            StaticData data = TestData.Data(("DeathLimit", 15));
            RunState run = RunRules.NewRun(data, 1);
            run.Roster.RemoveAll(m => m.Id == "dan");
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));

            RunRules.Settle(data, run, Finished(data, run, "cave", ExpeditionResult.Wiped, "anna", "ben", "cora"));

            Assert.AreEqual(RunOutcome.Defeat, run.Outcome);
            Assert.AreEqual(DefeatCause.Wipeout, run.DefeatCause);
        }
    }
}
