using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The lobby and run rules of Docs/Design/04_Lobby_100Day_Economy.md.</summary>
    public sealed class RunRulesTests
    {
        static List<PartySlot> Party(params (string Id, BattleRow Row)[] slots)
        {
            return slots.Select(s => new PartySlot { MercenaryId = s.Id, Row = s.Row }).ToList();
        }

        static RunState RunWithParty(StaticData data)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, Party(("anna", BattleRow.Front), ("ben", BattleRow.Rear), ("cora", BattleRow.Rear)));
            return run;
        }

        /// <summary>An expedition that has ended, with the given members dead.</summary>
        static ExpeditionState FinishedExpedition(StaticData data, RunState run, ExpeditionResult result, params string[] dead)
        {
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
            foreach (ExpeditionMember member in expedition.Members)
            {
                if (dead.Contains(member.MercenaryId))
                {
                    member.Alive = false;
                    member.Hp = 0;
                }
            }

            expedition.Phase = ExpeditionPhase.Finished;
            expedition.Result = result;
            return expedition;
        }

        [Test]
        public void NewRun_StartsOnDayOne_WithTheStartingRosterAtFullFatigue_AndNoParty()
        {
            StaticData data = TestData.Data(("MaxFatigue", 100));

            RunState run = RunRules.NewRun(data, 123);

            Assert.AreEqual(123UL, run.Seed);
            Assert.AreEqual(1, run.Day);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora", "dan" }, run.Roster.Select(m => m.Id));
            Assert.IsTrue(run.Roster.All(m => m.Fatigue == 100));
            Assert.AreEqual("tank", RunRules.FindMercenary(run, "anna").JobId);
            Assert.IsEmpty(run.Party);
            Assert.IsEmpty(run.Fallen);
            Assert.IsEmpty(run.ClearedDungeons);
            Assert.IsFalse(run.IsOver);
        }

        [Test]
        public void SetParty_StoresACopy()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);
            List<PartySlot> party = Party(("anna", BattleRow.Front), ("ben", BattleRow.Rear));

            RunRules.SetParty(data, run, party);
            party[0].Row = BattleRow.Rear;

            Assert.AreEqual(2, run.Party.Count);
            Assert.AreEqual(BattleRow.Front, run.Party[0].Row);
        }

        [Test]
        public void SetParty_RejectsUnknownDuplicateOversizedAndOvercrowdedParties()
        {
            StaticData data = TestData.Data(("PartySize", 3), ("RowCapacity", 2));
            RunState run = RunRules.NewRun(data, 1);

            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("nobody", BattleRow.Front))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", BattleRow.Front), ("anna", BattleRow.Rear))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", BattleRow.Front), ("ben", BattleRow.Front), ("cora", BattleRow.Front))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", BattleRow.Front), ("ben", BattleRow.Rear), ("cora", BattleRow.Rear), ("dan", BattleRow.Front))));
            Assert.IsNull(RunRules.PartyProblem(data, run, Party()));
            Assert.Throws<InvalidOperationException>(() => RunRules.SetParty(data, run, Party(("nobody", BattleRow.Front))));
        }

        [Test]
        public void CanDepart_NeedsAPartyOfMinimumSize_WithEnoughFatigue()
        {
            StaticData data = TestData.Data(("MinPartySize", 1));
            RunState run = RunRules.NewRun(data, 1);

            Assert.AreEqual(DepartCheck.PartyTooSmall, RunRules.CanDepart(data, run, "cave"));

            RunRules.SetParty(data, run, Party(("anna", BattleRow.Front)));
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"));

            RunRules.FindMercenary(run, "anna").Fatigue = 29;
            Assert.AreEqual(DepartCheck.NotEnoughFatigue, RunRules.CanDepart(data, run, "cave"), "The dungeon costs 30.");

            RunRules.FindMercenary(run, "anna").Fatigue = 30;
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"));

            run.IsOver = true;
            Assert.AreEqual(DepartCheck.RunIsOver, RunRules.CanDepart(data, run, "cave"));
        }

        [Test]
        public void BeginExpedition_UsesThePartyAndASeedDerivedFromTheRun()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);

            ExpeditionState first = RunRules.BeginExpedition(data, run, "cave");

            Assert.AreEqual(1, run.ExpeditionCount);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, first.Members.Select(m => m.MercenaryId));
            Assert.AreEqual(BattleRow.Front, first.Members[0].Row);
            Assert.AreEqual(SeedDeriver.Derive(7, "expedition", 0), first.Seed);

            ExpeditionState second = RunRules.BeginExpedition(data, run, "cave");
            Assert.AreEqual(SeedDeriver.Derive(7, "expedition", 1), second.Seed);
            Assert.AreNotEqual(first.Seed, second.Seed);
        }

        [Test]
        public void BeginExpedition_WhenItCannotDepart_Throws()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);

            Assert.Throws<InvalidOperationException>(() => RunRules.BeginExpedition(data, run, "cave"));
            Assert.AreEqual(0, run.ExpeditionCount);
        }

        [Test]
        public void Rest_PassesRestDays_AndEveryoneRecoversUpToTheMaximum()
        {
            StaticData data = TestData.Data(("RestDays", 1), ("FatigueRecoveryPerDay", 10), ("MaxFatigue", 100));
            RunState run = RunRules.NewRun(data, 1);
            RunRules.FindMercenary(run, "anna").Fatigue = 40;
            RunRules.FindMercenary(run, "ben").Fatigue = 95;

            RunRules.Rest(data, run);

            Assert.AreEqual(2, run.Day);
            Assert.AreEqual(50, RunRules.FindMercenary(run, "anna").Fatigue);
            Assert.AreEqual(100, RunRules.FindMercenary(run, "ben").Fatigue);
        }

        [Test]
        public void Settle_SurvivorsPayFatigue_DaysPass_ThoseAtHomeRecover_AndTheClearIsRecorded()
        {
            StaticData data = TestData.Data(("FatigueRecoveryPerDay", 10), ("MaxFatigue", 100));
            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "dan").Fatigue = 50;
            ExpeditionState expedition = FinishedExpedition(data, run, ExpeditionResult.Cleared);

            SettlementReport report = RunRules.Settle(data, run, expedition);

            Assert.AreEqual(70, RunRules.FindMercenary(run, "anna").Fatigue, "100 - 30, and no recovery while away.");
            Assert.AreEqual(70, RunRules.FindMercenary(run, "dan").Fatigue, "Stayed home for 2 days: 50 + 20.");
            Assert.AreEqual(3, run.Day, "The dungeon takes 2 days.");
            Assert.AreEqual(1, run.ClearedDungeons["cave"]);
            Assert.IsFalse(run.IsOver);

            Assert.AreEqual(ExpeditionResult.Cleared, report.Result);
            Assert.AreEqual(30, report.FatigueCost);
            Assert.AreEqual(2, report.DaysPassed);
            Assert.AreEqual(3, report.DayAfter);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, report.SurvivorIds);
            Assert.IsEmpty(report.FallenIds);
        }

        [Test]
        public void Settle_TheDeadLeaveTheRosterAndThePartyForGood()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            ExpeditionState expedition = FinishedExpedition(data, run, ExpeditionResult.Retreated, "anna");

            SettlementReport report = RunRules.Settle(data, run, expedition);

            Assert.IsNull(RunRules.FindMercenary(run, "anna"));
            CollectionAssert.AreEqual(new[] { "anna" }, run.Fallen);
            CollectionAssert.AreEqual(new[] { "ben", "cora" }, run.Party.Select(s => s.MercenaryId));
            CollectionAssert.AreEqual(new[] { "anna" }, report.FallenIds);
            Assert.IsFalse(run.ClearedDungeons.ContainsKey("cave"), "A retreat is not a clear.");
            Assert.AreEqual(3, run.Roster.Count);
        }

        [Test]
        public void Settle_WhenNobodyIsLeft_TheRunIsOver()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);
            run.Roster.RemoveAll(m => m.Id == "dan");
            RunRules.SetParty(data, run, Party(("anna", BattleRow.Front), ("ben", BattleRow.Rear), ("cora", BattleRow.Rear)));
            ExpeditionState expedition = FinishedExpedition(data, run, ExpeditionResult.Wiped, "anna", "ben", "cora");

            SettlementReport report = RunRules.Settle(data, run, expedition);

            Assert.IsTrue(run.IsOver);
            Assert.IsTrue(report.RunIsOver);
            Assert.IsEmpty(run.Roster);
            Assert.IsEmpty(run.Party);
            Assert.AreEqual(DepartCheck.RunIsOver, RunRules.CanDepart(data, run, "cave"));
            Assert.Throws<InvalidOperationException>(() => RunRules.Rest(data, run));
        }

        [Test]
        public void Settle_WhenExpeditionNotFinished_Throws()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");

            Assert.Throws<InvalidOperationException>(() => RunRules.Settle(data, run, expedition));
        }

        [Test]
        public void Fatigue_NeverDropsBelowZero()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            ExpeditionState expedition = FinishedExpedition(data, run, ExpeditionResult.Retreated);
            RunRules.FindMercenary(run, "anna").Fatigue = 10;

            RunRules.Settle(data, run, expedition);

            Assert.AreEqual(0, RunRules.FindMercenary(run, "anna").Fatigue);
        }

        [Test]
        public void ALoop_LobbyExpeditionReturnLobby_ChangesTheRunAndCanBeRepeated()
        {
            StaticData data = TestData.Data(("DogDeathChancePercent", 0));
            RunState run = RunWithParty(data);

            for (int loop = 0; loop < 2; loop++)
            {
                Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"));
                ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");
                while (expedition.Phase != ExpeditionPhase.Finished)
                {
                    if (expedition.Phase == ExpeditionPhase.ChoosingReward)
                    {
                        ExpeditionRules.SkipReward(expedition);
                        continue;
                    }

                    var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, expedition, ExpeditionRules.AvailableNodes(expedition)[0].Id));
                    battle.RunToEnd();
                    ExpeditionRules.CompleteBattle(data, expedition, battle);
                }

                RunRules.Settle(data, run, expedition);
            }

            Assert.AreEqual(5, run.Day, "Two expeditions of two days from day 1.");
            Assert.AreEqual(2, run.ExpeditionCount);
            Assert.AreEqual(40, RunRules.FindMercenary(run, "anna").Fatigue, "100 - 30 - 30.");
            Assert.AreEqual(100, RunRules.FindMercenary(run, "dan").Fatigue);
        }
    }
}
