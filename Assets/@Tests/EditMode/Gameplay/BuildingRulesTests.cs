using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The town's buildings (Slice C stage 30, Docs/Design/04_Lobby_100Day_Economy.md §7): every building starts at level 1, an upgrade pays
    /// the next level's price and the level arrives when the run's days reach the finish day, a building under construction is locked (no
    /// hiring, no treatment, no dismissing, no leaving through the gate; resting goes on), nothing is built past the last day and no lock holds
    /// there, and a level adds its bonus to the balance's base. The test data: two levels each, 40 gold, 3 days (the inn 2), bonuses 2 / 20 / 1 / 10%.
    /// </summary>
    public sealed class BuildingRulesTests
    {
        static RunState RunWithParty(StaticData data, int gold = 100)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, FlowTestKit.Party(("anna", 1), ("ben", 2), ("cora", 3)));
            run.Gold = gold;
            return run;
        }

        static ExpeditionState Ended(StaticData data, RunState run, ExpeditionResult result, int floor)
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            expedition.CurrentNodeId = expedition.Map.Nodes.First(n => n.Floor == floor).Id;
            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = result;
            return expedition;
        }

        [Test]
        public void ARunStartsWithEveryBuildingAtLevelOne_AndNothingBeingBuilt()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 7);
            foreach (string id in BuildingId.All)
            {
                Assert.AreEqual(1, BuildingRules.LevelOf(run, id));
                Assert.IsNull(BuildingRules.ConstructionOf(run, id));
                Assert.IsFalse(BuildingRules.IsLocked(data, run, id));
                Assert.AreEqual(2, BuildingRules.NextLevel(data, run, id).Level);
            }

            Assert.AreEqual(UpgradeCheck.NotEnoughGold, BuildingRules.CanUpgrade(data, run, BuildingId.Barracks), "A run starts with no gold.");
        }

        [Test]
        public void AnUpgrade_PaysThePrice_AndPutsTheBuildingUnderConstruction_UntilTheDaysPass()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            Assert.AreEqual(UpgradeCheck.Ok, BuildingRules.CanUpgrade(data, run, BuildingId.Barracks));

            ConstructionState construction = BuildingRules.Upgrade(data, run, BuildingId.Barracks);
            Assert.AreEqual(60, run.Gold, "40 gold left.");
            Assert.AreEqual(2, construction.ToLevel);
            Assert.AreEqual(4, construction.FinishDay, "Day 1 plus 3 days.");
            Assert.AreEqual(1, BuildingRules.LevelOf(run, BuildingId.Barracks), "The level arrives with the days, not with the gold.");
            Assert.AreEqual(3, BuildingRules.DaysLeft(run, construction));
            Assert.IsTrue(BuildingRules.IsLocked(data, run, BuildingId.Barracks));
            Assert.AreEqual(UpgradeCheck.UnderConstruction, BuildingRules.CanUpgrade(data, run, BuildingId.Barracks));

            // Two days of rest: still building. The third day finishes it, and Rest says so.
            Assert.IsEmpty(RunRules.Rest(data, run));
            Assert.IsEmpty(RunRules.Rest(data, run));
            Assert.AreEqual(1, BuildingRules.DaysLeft(run, construction));
            CollectionAssert.AreEqual(new[] { BuildingId.Barracks }, RunRules.Rest(data, run));
            Assert.AreEqual(2, BuildingRules.LevelOf(run, BuildingId.Barracks));
            Assert.IsNull(BuildingRules.ConstructionOf(run, BuildingId.Barracks));
            Assert.IsFalse(BuildingRules.IsLocked(data, run, BuildingId.Barracks));
            Assert.AreEqual(UpgradeCheck.MaxLevel, BuildingRules.CanUpgrade(data, run, BuildingId.Barracks), "Two levels in the test data.");
            Assert.IsNull(BuildingRules.NextLevel(data, run, BuildingId.Barracks));
        }

        [Test]
        public void ASettlement_FinishesTheConstructionsItsDaysReach_AndReportsThem()
        {
            // The cave takes 2 days: the inn (2 days) is done at the settlement, the stagecoach (3 days) is not.
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            BuildingRules.Upgrade(data, run, BuildingId.Inn);
            BuildingRules.Upgrade(data, run, BuildingId.Coach);
            Assert.AreEqual(20, run.Gold);

            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Retreated, 1));
            Assert.AreEqual(3, run.Day);
            CollectionAssert.AreEqual(new[] { BuildingId.Inn }, report.BuildingsCompleted);
            Assert.AreEqual(2, BuildingRules.LevelOf(run, BuildingId.Inn));
            Assert.AreEqual(1, BuildingRules.LevelOf(run, BuildingId.Coach));
            Assert.IsTrue(BuildingRules.IsLocked(data, run, BuildingId.Coach));
        }

        [Test]
        public void AnyNumberOfBuildings_MayBeBuiltAtOnce_ButNotWithoutTheGold()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data, gold: 90);
            BuildingRules.Upgrade(data, run, BuildingId.Barracks);
            BuildingRules.Upgrade(data, run, BuildingId.Inn);
            Assert.AreEqual(10, run.Gold);
            Assert.AreEqual(2, run.Constructions.Count);
            Assert.AreEqual(UpgradeCheck.NotEnoughGold, BuildingRules.CanUpgrade(data, run, BuildingId.Coach));
            Assert.Throws<InvalidOperationException>(() => BuildingRules.Upgrade(data, run, BuildingId.Coach));
        }

        [Test]
        public void AStagecoachUnderConstruction_DoesNotHire_AnInnDoesNotTreat_ButRests_ABarracksDoesNotDismiss()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data, gold: 200);
            run.Roster[0].Fatigue = 50;
            Assert.AreEqual(HireCheck.Ok, RunRules.CanHire(data, run, 0));
            Assert.AreEqual(TreatCheck.Ok, RunRules.CanTreat(data, run, "anna"));
            Assert.IsTrue(RunRules.CanDismiss(data, run, "dan"));

            BuildingRules.Upgrade(data, run, BuildingId.Coach);
            BuildingRules.Upgrade(data, run, BuildingId.Inn);
            BuildingRules.Upgrade(data, run, BuildingId.Barracks);
            Assert.AreEqual(HireCheck.CoachLocked, RunRules.CanHire(data, run, 0));
            Assert.AreEqual(TreatCheck.InnLocked, RunRules.CanTreat(data, run, "anna"));
            Assert.IsFalse(RunRules.CanDismiss(data, run, "dan"));
            Assert.Throws<InvalidOperationException>(() => RunRules.Hire(data, run, 0));
            Assert.Throws<InvalidOperationException>(() => RunRules.Treat(data, run, "anna"));
            Assert.Throws<InvalidOperationException>(() => RunRules.Dismiss(data, run, "dan"));
            Assert.IsTrue(RunRules.CanRest(data, run), "Resting goes on while the inn is built: that is how the days pass.");
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"), "No construction keeps the party home: the gate has no level (2026-10-18).");
        }

        [Test]
        public void PastTheLastDay_NothingIsBuilt_AndNoLockHolds()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            BuildingRules.Upgrade(data, run, BuildingId.Coach);
            run.Day = data.Balance.TotalDays + 1;
            Assert.IsTrue(RunRules.IsFinalStandDue(data, run));
            Assert.IsFalse(BuildingRules.IsLocked(data, run, BuildingId.Coach), "The days do not pass any more: the lock would never lift.");
            Assert.AreEqual(HireCheck.Ok, RunRules.CanHire(data, run, 0), "The stagecoach being built does not lock past the last day.");
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "pit"));
            Assert.AreEqual(UpgradeCheck.FinalStandDue, BuildingRules.CanUpgrade(data, run, BuildingId.Barracks));
            Assert.IsNotNull(BuildingRules.ConstructionOf(run, BuildingId.Coach), "The construction stays, unfinished.");
        }

        [Test]
        public void TheLevels_AddTheirBonus_ToTheRosterLimit_TheCandidates_AndTheRelief()
        {
            StaticData data = TestData.Data();
            BalanceData balance = data.Balance;
            RunState run = RunWithParty(data, gold: 500);
            Assert.AreEqual(balance.RosterLimit, BuildingRules.RosterLimit(data, run));
            Assert.AreEqual(balance.RecruitCount, BuildingRules.RecruitCount(data, run));
            Assert.AreEqual(balance.TreatmentRelief, BuildingRules.TreatmentRelief(data, run));

            foreach (string id in BuildingId.All)
            {
                run.BuildingLevels[id] = 2;
            }

            Assert.AreEqual(balance.RosterLimit + 2, BuildingRules.RosterLimit(data, run));
            Assert.AreEqual(balance.RecruitCount + 1, BuildingRules.RecruitCount(data, run));
            Assert.AreEqual(balance.TreatmentRelief + 20, BuildingRules.TreatmentRelief(data, run));

            // The treatment takes the inn's relief; the stagecoach draws the extra candidate; the settlement's gold is the base (no gate bonus since 2026-10-18).
            run.Roster[0].Fatigue = 100;
            RunRules.Treat(data, run, "anna");
            Assert.AreEqual(100 - balance.TreatmentRelief - 20, run.Roster[0].Fatigue);
            RunRules.DrawRecruits(data, run);
            Assert.AreEqual(balance.RecruitCount + 1, run.Recruits.Count);
            int goldBefore = run.Gold;
            SettlementReport report = RunRules.Settle(data, run, Ended(data, run, ExpeditionResult.Cleared, 3));
            Assert.AreEqual(24, report.GoldEarned, "2 floors x 2 and the clear's 20.");
            Assert.AreEqual(goldBefore + 24, run.Gold);

            // The roster grows to the raised limit.
            run.Gold = 500;
            while (RunRules.CanHire(data, run, 0) == HireCheck.Ok)
            {
                RunRules.Hire(data, run, 0);
                RunRules.DrawRecruits(data, run);
            }

            Assert.AreEqual(Math.Min(balance.RosterLimit + 2, data.Mercenaries.Count), run.Roster.Count);
        }

        [Test]
        public void TheSameSeed_BuildsTheSameTown_AndDrawsTheSameCandidates()
        {
            StaticData data = TestData.Data();
            RunState a = RunWithParty(data, gold: 500);
            RunState b = RunWithParty(data, gold: 500);
            foreach (RunState run in new[] { a, b })
            {
                BuildingRules.Upgrade(data, run, BuildingId.Coach);
                RunRules.Rest(data, run);
                RunRules.Rest(data, run);
                RunRules.Rest(data, run);
                RunRules.DrawRecruits(data, run);
            }

            Assert.AreEqual(2, BuildingRules.LevelOf(b, BuildingId.Coach));
            CollectionAssert.AreEqual(a.Recruits.Select(r => r.Id + ":" + r.JobId).ToList(), b.Recruits.Select(r => r.Id + ":" + r.JobId).ToList());
        }
    }
}
