using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The lobby's gold, the stagecoach and the inn (Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §3·§5·§6·§9): the settlement pays
    /// for the floors reached and a clear, candidates are drawn from the recruit pool at the start and at every settlement, hiring costs the price
    /// and fills the roster up to its limit, dismissing is free and final, the treatment takes fatigue off one mercenary for gold without a day
    /// passing. The test data's pool is eve, finn, gil, hana and ivo; its jobs tank, healer and striker; its roster limit 6.
    /// </summary>
    public sealed class LobbyEconomyTests
    {
        static RunState RunWithParty(StaticData data, ulong seed = 7)
        {
            RunState run = RunRules.NewRun(data, seed);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            return run;
        }

        /// <summary>An expedition to the cave that ended on the node of the given floor (the boss node is floor 3 of the two-floor cave).</summary>
        static ExpeditionState Ended(StaticData data, RunState run, ExpeditionResult result, int floor, string dungeonId = "cave")
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, dungeonId);
            expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == floor).Id;
            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = result;
            return expedition;
        }

        static string[] Pool(StaticData data)
        {
            return data.RecruitPool.Select(m => m.Id).ToArray();
        }

        // ---- Gold ----------------------------------------------------------------------------

        [Test]
        public void ARunStartsWithNoGold_AndTheSettlementPaysForTheFloorsReachedAndAClear()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            Assert.AreEqual(0, run.Gold);

            // Retreated from floor 2: two floors, no clear.
            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 2));
            Assert.AreEqual(2, report.FloorsReached);
            Assert.AreEqual(4, report.GoldEarned, "GoldPerFloor 2 for each floor.");
            Assert.AreEqual(4, report.GoldAfter);
            Assert.AreEqual(4, run.Gold);

            // Cleared at the boss: the boss floor counts as the last map floor, and the clear pays on top.
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Cleared, 3));
            Assert.AreEqual(2, report.FloorsReached);
            Assert.AreEqual(24, report.GoldEarned, "2 floors x 2, and GoldClear 20.");
            Assert.AreEqual(28, run.Gold);
            Assert.AreEqual(0, report.CoinsConverted, "No coins and nothing but base weapons: nothing to turn into gold.");
        }

        [Test]
        public void AClearedExpeditionsCoins_AndTheItemsWorth_ComeHomeAsGold_CoinsPerGoldToOne_ButNotFromARetreat()
        {
            // 2026-10-17 (the user's request): the coins left plus what the shop would pay for the items brought home, 20 coins to a gold.
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            ExpeditionState cleared = Ended(data, run, ExpeditionResult.Cleared, 3);
            cleared.Coins = 45;
            TestBoards.Put(cleared.Members[0], new EquippedItem(data.Items.Get("knife")), 2, 0);
            cleared.Inventory.Add(new EquippedItem(data.Items.Get("bow"), tier: ItemTier.Common + 1));
            int knife = ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("knife")));
            int bow = ExpeditionRules.SellPriceOf(data, new EquippedItem(data.Items.Get("bow"), tier: ItemTier.Common + 1));
            Assert.AreEqual(knife + bow, RunRules.ItemCoinsFor(data, cleared), "The base weapons count for nothing.");

            SettlementReport report = RunRules.Settle(data, run, cleared);

            Assert.AreEqual(knife + bow, report.CoinsFromItems);
            Assert.AreEqual(45 + knife + bow, report.CoinsConverted);
            Assert.AreEqual((45 + knife + bow) / data.Balance.CoinsPerGold, report.GoldFromCoins, "Whole gold; the remainder is lost with the expedition.");
            Assert.Greater(report.GoldFromCoins, 0);
            Assert.AreEqual(24 + report.GoldFromCoins, report.GoldEarned, "On top of the floors and the clear.");
            Assert.AreEqual(report.GoldEarned, run.Gold);

            // A retreat brings nothing home: the coins and the items are lost.
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            ExpeditionState retreated = Ended(data, run, ExpeditionResult.Retreated, 2);
            retreated.Coins = 400;
            TestBoards.Put(retreated.Members[0], new EquippedItem(data.Items.Get("knife")), 2, 0);
            int gold = run.Gold;
            report = RunRules.Settle(data, run, retreated);
            Assert.AreEqual(0, report.CoinsConverted);
            Assert.AreEqual(0, report.GoldFromCoins);
            Assert.AreEqual(4, report.GoldEarned);
            Assert.AreEqual(gold + 4, run.Gold);
        }

        [Test]
        public void AnExpeditionThatNeverEnteredANode_PaysNothing_AndTheFinalStandPaysOnlyTheClear()
        {
            StaticData data = TestData.Data(("TotalDays", 1));
            RunState run = RunWithParty(data);
            ExpeditionState none = RunRules.BeginExpedition(data, run, "cave");
            none.Phase = ExpeditionPhase.Finished;
            none.Result = ExpeditionResult.Retreated;
            Assert.AreEqual(0, RunRules.GoldFor(data, none));
            RunRules.Settle(data, run, none);

            // Past the last day: the final stand against the pit's boss, cleared.
            Assert.IsTrue(RunRules.IsFinalStandDue(data, run));
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            ExpeditionState stand = RunRules.BeginExpedition(data, run, "pit");
            stand.CurrentNodeId = 0;
            stand.Phase = ExpeditionPhase.Finished;
            stand.Result = ExpeditionResult.Cleared;
            Assert.AreEqual(0, RunRules.FloorsReached(data, stand), "No map floors at the final stand.");
            Assert.AreEqual(20, RunRules.GoldFor(data, stand));
        }

        [Test]
        public void BeginExpedition_CountsTheExpeditionsPerDungeon()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.ClearedDungeons["cave"] = 1;

            RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1, "pit"));

            Assert.AreEqual(3, run.ExpeditionCount);
            Assert.AreEqual(2, run.ExpeditionsByDungeon["cave"]);
            Assert.AreEqual(1, run.ExpeditionsByDungeon["pit"]);
        }

        // ---- The stagecoach ------------------------------------------------------------------

        [Test]
        public void NewRun_DrawsRecruitCountCandidates_FromThePool_WithAJobEach_FreshAndTheSameForTheSameSeed()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 7);

            Assert.AreEqual(3, run.Recruits.Count);
            Assert.AreEqual(3, run.Recruits.Select(r => r.Id).Distinct().Count(), "Three different names.");
            CollectionAssert.IsSubsetOf(run.Recruits.Select(r => r.Id), Pool(data));
            Assert.IsTrue(run.Recruits.All(r => data.Jobs.Contains(r.JobId)));

            RunState same = RunRules.NewRun(data, 7);
            CollectionAssert.AreEqual(run.Recruits.Select(r => r.Id + ":" + r.JobId), same.Recruits.Select(r => r.Id + ":" + r.JobId), "The draw is a function of the seed.");

            // Over many seeds every name of the pool and every job comes up: the draw is not stuck.
            var names = new HashSet<string>();
            var jobs = new HashSet<string>();
            for (ulong seed = 1; seed <= 40; seed++)
            {
                RunState other = RunRules.NewRun(data, seed);
                names.UnionWith(other.Recruits.Select(r => r.Id));
                jobs.UnionWith(other.Recruits.Select(r => r.JobId));
            }

            CollectionAssert.AreEquivalent(Pool(data), names);
            CollectionAssert.AreEquivalent(data.Jobs.Ordered.Select(j => j.Id), jobs);
        }

        [Test]
        public void TheSettlement_DrawsNewCandidates_ButNotWhileTheRunIsOver()
        {
            // A death limit of three: the party of three dying ends the run.
            StaticData data = TestData.Data(("DeathLimit", 3));
            RunState run = RunWithParty(data);
            string[] before = run.Recruits.Select(r => r.Id + ":" + r.JobId).ToArray();

            RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            Assert.AreEqual(3, run.Recruits.Count);
            string[] after = run.Recruits.Select(r => r.Id + ":" + r.JobId).ToArray();
            Assert.AreNotEqual(before, after, "A new draw (the stream index is the number of expeditions).");

            // The party dies and the deaths reach the limit: the run is over and the candidates stay as they were.
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            ExpeditionState wipe = Ended(data, run, ExpeditionResult.Wiped, 1);
            foreach (ExpeditionMember member in wipe.Members)
            {
                member.Alive = false;
                member.Hp = 0;
            }

            RunRules.Settle(data, run, wipe);
            Assert.IsTrue(run.IsOver);
            Assert.AreEqual(DefeatCause.DeathLimit, run.DefeatCause);
            CollectionAssert.AreEqual(after, run.Recruits.Select(r => r.Id + ":" + r.JobId));
        }

        [Test]
        public void Hire_TakesThePrice_AddsTheCandidateFreshToTheRoster_AndTakesItOffTheCoach()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            Assert.AreEqual(HireCheck.NotEnoughGold, RunRules.CanHire(data, run, 0), "A run starts with no gold.");
            Assert.Throws<InvalidOperationException>(() => RunRules.Hire(data, run, 0));

            run.Gold = 45;
            RecruitState candidate = run.Recruits[1];
            Assert.AreEqual(HireCheck.Ok, RunRules.CanHire(data, run, 1));
            MercenaryState hired = RunRules.Hire(data, run, 1);

            Assert.AreEqual(15, run.Gold);
            Assert.AreSame(hired, RunRules.FindMercenary(run, candidate.Id));
            Assert.AreEqual(candidate.JobId, hired.JobId);
            Assert.AreEqual(0, hired.Fatigue);
            Assert.IsNull(hired.AfflictionId);
            Assert.AreEqual(5, run.Roster.Count);
            Assert.AreEqual(2, run.Recruits.Count);
            Assert.IsFalse(run.Recruits.Any(r => r.Id == candidate.Id));
            Assert.AreEqual(HireCheck.NotEnoughGold, RunRules.CanHire(data, run, 0), "15 left of a price of 30.");
            Assert.AreEqual(HireCheck.NoCandidate, RunRules.CanHire(data, run, 2));
        }

        [Test]
        public void Hire_StopsAtTheRosterLimit_AndWhenTheRunIsOver()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.Gold = 1000;
            RunRules.Hire(data, run, 0);
            RunRules.Hire(data, run, 0);
            Assert.AreEqual(6, run.Roster.Count, "The limit of the test data.");
            Assert.AreEqual(HireCheck.RosterFull, RunRules.CanHire(data, run, 0));
            Assert.Throws<InvalidOperationException>(() => RunRules.Hire(data, run, 0));

            run.Outcome = RunOutcome.Defeat;
            run.DefeatCause = DefeatCause.DeathLimit;
            Assert.AreEqual(HireCheck.RunIsOver, RunRules.CanHire(data, run, 0));
        }

        [Test]
        public void AHiredMercenary_GoesOnExpeditionsAsItsJob_AndItsNameIsNeverDrawnAgainWhileItLivesOrAfterItDies()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.Gold = 30;
            MercenaryState hired = RunRules.Hire(data, run, 0);
            RunRules.SetParty(data, run, FlowTestKit.Party((hired.Id, 1)));

            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            Assert.AreEqual(hired.JobId, expedition.Members[0].JobId);
            Assert.AreEqual(hired.JobId, ExpeditionRules.BeginBattle(data, expedition, expedition.Map.Nodes[0].Id).Party[0].JobId, "The battle shows the job of the roster.");

            for (int draw = 0; draw < 20; draw++)
            {
                run.ExpeditionCount = draw;
                RunRules.DrawRecruits(data, run);
                Assert.IsFalse(run.Recruits.Any(r => r.Id == hired.Id), "In the roster.");
            }

            run.Roster.Remove(hired);
            run.Fallen.Add(hired.Id);
            for (int draw = 0; draw < 20; draw++)
            {
                run.ExpeditionCount = draw;
                RunRules.DrawRecruits(data, run);
                Assert.IsFalse(run.Recruits.Any(r => r.Id == hired.Id), "Dead.");
            }
        }

        [Test]
        public void DrawRecruits_DrawsFewerWhenThePoolRunsShort()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 7);
            run.Dismissed.AddRange(new[] { "eve", "finn", "gil" });
            RunRules.DrawRecruits(data, run);
            CollectionAssert.AreEquivalent(new[] { "hana", "ivo" }, run.Recruits.Select(r => r.Id));

            run.Dismissed.AddRange(new[] { "hana", "ivo" });
            RunRules.DrawRecruits(data, run);
            Assert.IsEmpty(run.Recruits);
        }

        // ---- Dismissing ----------------------------------------------------------------------

        [Test]
        public void Dismiss_IsFree_TakesTheMercenaryOutOfThePartyAndTheRoster_ForGood_ButNotTheLastOne()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.Gold = 5;

            Assert.IsTrue(RunRules.CanDismiss(data, run, "anna"));
            RunRules.Dismiss(data, run, "anna");

            Assert.AreEqual(5, run.Gold);
            Assert.IsNull(RunRules.FindMercenary(run, "anna"));
            CollectionAssert.AreEqual(new[] { "anna" }, run.Dismissed);
            Assert.IsEmpty(run.Fallen, "Dismissed, not dead.");
            Assert.AreEqual("ben:1 cora:2", string.Join(" ", run.Party.Select(s => s.MercenaryId + ":" + s.Row)), "Those behind advance.");
            Assert.IsFalse(RunRules.CanDismiss(data, run, "anna"));
            Assert.Throws<InvalidOperationException>(() => RunRules.Dismiss(data, run, "anna"));

            RunRules.Dismiss(data, run, "ben");
            RunRules.Dismiss(data, run, "dan");
            Assert.AreEqual(1, run.Roster.Count);
            Assert.IsFalse(RunRules.CanDismiss(data, run, "cora"), "The last mercenary stays: the run ends only by its outcomes.");
            Assert.Throws<InvalidOperationException>(() => RunRules.Dismiss(data, run, "cora"));
        }

        [Test]
        public void ADismissedName_IsNeverDrawnAgain()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            run.Roster.Add(new MercenaryState { Id = "eve", JobId = "healer" });
            RunRules.Dismiss(data, run, "eve");

            for (int draw = 0; draw < 20; draw++)
            {
                run.ExpeditionCount = draw;
                RunRules.DrawRecruits(data, run);
                Assert.IsFalse(run.Recruits.Any(r => r.Id == "eve"));
                Assert.AreEqual(3, run.Recruits.Count, "Four names are left in the pool.");
            }
        }

        // ---- The inn -------------------------------------------------------------------------

        [Test]
        public void Treat_TakesThePriceAndTheReliefOffTheFatigue_EndsAnAfflictionUnderTheThreshold_AndNoDayPasses()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            MercenaryState anna = RunRules.FindMercenary(run, "anna");
            string affliction = data.FatigueStatesOf(FatigueStateKind.Affliction)[0].Id;
            anna.Fatigue = 128;
            anna.AfflictionId = affliction;
            Assert.AreEqual(TreatCheck.NotEnoughGold, RunRules.CanTreat(data, run, "anna"));
            Assert.Throws<InvalidOperationException>(() => RunRules.Treat(data, run, "anna"));

            run.Gold = 50;
            Assert.AreEqual(TreatCheck.Ok, RunRules.CanTreat(data, run, "anna"));
            RunRules.Treat(data, run, "anna");

            Assert.AreEqual(30, run.Gold);
            Assert.AreEqual(88, anna.Fatigue, "128 - 40.");
            Assert.IsNull(anna.AfflictionId, "Under the breakdown threshold the affliction ends, as on a day off.");
            Assert.AreEqual(1, run.Day);
            Assert.AreEqual(0, RunRules.FindMercenary(run, "ben").Fatigue, "The others are untouched.");

            // Treated again: 88 - 40 = 48, and once more to 8, then 0: not below zero and not when there is nothing to take off.
            RunRules.Treat(data, run, "anna");
            Assert.AreEqual(48, anna.Fatigue);
            run.Gold = 100;
            RunRules.Treat(data, run, "anna");
            RunRules.Treat(data, run, "anna");
            Assert.AreEqual(0, anna.Fatigue);
            Assert.AreEqual(TreatCheck.NotTired, RunRules.CanTreat(data, run, "anna"));
            Assert.AreEqual(TreatCheck.NotInRoster, RunRules.CanTreat(data, run, "nobody"));
        }

        [Test]
        public void TheInnAndTheStagecoach_WorkPastTheLastDay_WhileRestingDoesNot()
        {
            StaticData data = TestData.Data(("TotalDays", 1));
            RunState run = RunWithParty(data);
            RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            Assert.IsTrue(RunRules.IsFinalStandDue(data, run));
            Assert.IsFalse(RunRules.CanRest(data, run));

            run.Gold = 100;
            RunRules.FindMercenary(run, "dan").Fatigue = 50;
            Assert.AreEqual(TreatCheck.Ok, RunRules.CanTreat(data, run, "dan"));
            RunRules.Treat(data, run, "dan");
            Assert.AreEqual(HireCheck.Ok, RunRules.CanHire(data, run, 0));
            RunRules.Hire(data, run, 0);
            Assert.IsTrue(RunRules.CanDismiss(data, run, "dan"));
            RunRules.Dismiss(data, run, "dan");
            Assert.AreEqual(4, run.Roster.Count);
        }
    }
}
