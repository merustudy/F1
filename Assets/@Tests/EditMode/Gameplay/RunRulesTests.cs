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
        static List<PartySlot> Party(params (string Id, int Row)[] slots)
        {
            return slots.Select(s => new PartySlot { MercenaryId = s.Id, Row = s.Row }).ToList();
        }

        static RunState RunWithParty(StaticData data)
        {
            RunState run = RunRules.NewRun(data, 7);
            RunRules.SetParty(data, run, Party(("anna", 1), ("ben", 2), ("cora", 3)));
            return run;
        }

        /// <summary>Who stands where, as "anna:1 ben:2", in the order the party lists them.</summary>
        static string Rows(RunState run)
        {
            return string.Join(" ", run.Party.Select(slot => $"{slot.MercenaryId}:{slot.Row}"));
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
        public void NewRun_StartsOnDayOne_WithTheStartingRosterFresh_AndNoParty()
        {
            StaticData data = TestData.Data();

            RunState run = RunRules.NewRun(data, 123);

            Assert.AreEqual(123UL, run.Seed);
            Assert.AreEqual(1, run.Day);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora", "dan" }, run.Roster.Select(m => m.Id));
            Assert.IsTrue(run.Roster.All(m => m.Fatigue == 0), "Fatigue builds up from 0.");
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
            List<PartySlot> party = Party(("anna", 1), ("ben", 2));

            RunRules.SetParty(data, run, party);
            party[0].Row = 3;

            Assert.AreEqual(2, run.Party.Count);
            Assert.AreEqual(1, run.Party[0].Row);
        }

        [Test]
        public void SetParty_RejectsUnknownDuplicateAndOversizedParties()
        {
            StaticData data = TestData.Data(("PartySize", 3));
            RunState run = RunRules.NewRun(data, 1);

            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("nobody", 1))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("anna", 2))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("ben", 2), ("cora", 3), ("dan", 4))), "Four in a party of three.");
            Assert.IsNull(RunRules.PartyProblem(data, run, Party()));
            Assert.Throws<InvalidOperationException>(() => RunRules.SetParty(data, run, Party(("nobody", 1))));
        }

        [Test]
        public void SetParty_RejectsFormationsThatAreNotOnePerRowFromTheFront()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);

            Assert.IsNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("ben", 2), ("cora", 3))));
            Assert.IsNull(RunRules.PartyProblem(data, run, Party(("ben", 2), ("anna", 1))), "The order of the list does not matter.");
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("ben", 1))), "Two in one row.");
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 2))), "Nobody in row 1.");
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("ben", 3))), "Nobody in row 2.");
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 0))));
            Assert.IsNotNull(RunRules.PartyProblem(data, run, Party(("anna", 1), ("ben", 2), ("cora", BattleRows.Count + 1))));
        }

        [Test]
        public void PlaceInParty_AMercenaryOutsideTheParty_JoinsTheFirstEmptyRowOnly()
        {
            StaticData data = TestData.Data(("PartySize", 3));
            RunState run = RunRules.NewRun(data, 1);

            Assert.IsTrue(RunRules.CanPlaceInParty(data, run, "anna", 1));
            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "anna", 2), "Row 1 would be empty in front.");
            Assert.Throws<InvalidOperationException>(() => RunRules.PlaceInParty(data, run, "anna", 2));

            RunRules.PlaceInParty(data, run, "anna", 1);
            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "ben", 1), "Taken: one mercenary per row.");
            Assert.IsTrue(RunRules.CanPlaceInParty(data, run, "ben", 2));
            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "ben", 3));

            RunRules.PlaceInParty(data, run, "ben", 2);
            RunRules.PlaceInParty(data, run, "cora", 3);
            Assert.AreEqual("anna:1 ben:2 cora:3", Rows(run));

            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "dan", row), "The party is full.");
            }

            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "nobody", 1));
            Assert.IsNull(RunRules.PartyProblem(data, run, run.Party));
        }

        [Test]
        public void PlaceInParty_AMember_TradesPlacesWithWhoStandsInThatRow()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);

            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "anna", 1), "Already there.");
            Assert.IsTrue(RunRules.CanPlaceInParty(data, run, "anna", 3));

            RunRules.PlaceInParty(data, run, "anna", 3);
            Assert.AreEqual("anna:3 ben:2 cora:1", Rows(run));

            RunRules.PlaceInParty(data, run, "ben", 1);
            Assert.AreEqual("anna:3 ben:1 cora:2", Rows(run));
        }

        [Test]
        public void PlaceInParty_AMemberOfASmallerParty_CannotMoveToAnEmptyRow()
        {
            StaticData data = TestData.Data();
            RunState run = RunRules.NewRun(data, 1);
            RunRules.SetParty(data, run, Party(("anna", 1), ("ben", 2)));

            Assert.IsFalse(RunRules.CanPlaceInParty(data, run, "anna", 3), "Nobody stands in row 3.");
            Assert.IsTrue(RunRules.CanPlaceInParty(data, run, "anna", 2));
        }

        [Test]
        public void RemoveFromParty_ThoseBehindAdvance()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);

            RunRules.RemoveFromParty(run, "anna");
            Assert.AreEqual("ben:1 cora:2", Rows(run));

            RunRules.RemoveFromParty(run, "cora");
            Assert.AreEqual("ben:1", Rows(run));

            Assert.Throws<InvalidOperationException>(() => RunRules.RemoveFromParty(run, "cora"), "Not in the party.");
        }

        [Test]
        public void CanDepart_NeedsAPartyOfMinimumSize_AndFatigueKeepsNobodyHome()
        {
            StaticData data = TestData.Data(("MinPartySize", 1), ("MaxFatigue", 200));
            RunState run = RunRules.NewRun(data, 1);

            Assert.AreEqual(DepartCheck.PartyTooSmall, RunRules.CanDepart(data, run, "cave"));

            RunRules.SetParty(data, run, Party(("anna", 1)));
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"));

            RunRules.FindMercenary(run, "anna").Fatigue = 200;
            Assert.AreEqual(DepartCheck.Ok, RunRules.CanDepart(data, run, "cave"), "Sending a tired mercenary is the player's call.");

            run.IsOver = true;
            Assert.AreEqual(DepartCheck.RunIsOver, RunRules.CanDepart(data, run, "cave"));
        }

        [Test]
        public void BeginExpedition_EveryMemberLeavesWithTheirFatigue()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "ben").Fatigue = 45;

            ExpeditionState expedition = RunRules.BeginExpedition(data, run, "cave");

            Assert.AreEqual(45, expedition.Members.Single(m => m.MercenaryId == "ben").Fatigue);
            Assert.AreEqual(0, expedition.Members.Single(m => m.MercenaryId == "anna").Fatigue);
        }

        [Test]
        public void BeginExpedition_UsesThePartyAndASeedDerivedFromTheRun()
        {
            StaticData data = TestData.Data();
            RunState run = RunWithParty(data);

            ExpeditionState first = RunRules.BeginExpedition(data, run, "cave");

            Assert.AreEqual(1, run.ExpeditionCount);
            CollectionAssert.AreEqual(new[] { "anna", "ben", "cora" }, first.Members.Select(m => m.MercenaryId));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, first.Members.Select(m => m.Row));
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
        public void Rest_PassesRestDays_AndEveryonesFatigueComesDown_NotBelowZero()
        {
            StaticData data = TestData.Data(("RestDays", 1), ("FatigueRecoveryPerDay", 10));
            RunState run = RunRules.NewRun(data, 1);
            RunRules.FindMercenary(run, "anna").Fatigue = 40;
            RunRules.FindMercenary(run, "ben").Fatigue = 5;

            RunRules.Rest(data, run);

            Assert.AreEqual(2, run.Day);
            Assert.AreEqual(30, RunRules.FindMercenary(run, "anna").Fatigue);
            Assert.AreEqual(0, RunRules.FindMercenary(run, "ben").Fatigue);
        }

        [Test]
        public void Settle_SurvivorsKeepTheirFatigue_DaysPass_ThoseAtHomeRecover_AndTheClearIsRecorded()
        {
            StaticData data = TestData.Data(("FatigueRecoveryPerDay", 10));
            RunState run = RunWithParty(data);
            RunRules.FindMercenary(run, "dan").Fatigue = 50;
            ExpeditionState expedition = FinishedExpedition(data, run, ExpeditionResult.Cleared);
            expedition.Members.Single(m => m.MercenaryId == "anna").Fatigue = 37;

            SettlementReport report = RunRules.Settle(data, run, expedition);

            Assert.AreEqual(37, RunRules.FindMercenary(run, "anna").Fatigue, "What she came back with, and no recovery while away.");
            Assert.AreEqual(30, RunRules.FindMercenary(run, "dan").Fatigue, "Stayed home for 2 days: 50 - 20.");
            Assert.AreEqual(3, run.Day, "The dungeon takes 2 days.");
            Assert.AreEqual(1, run.ClearedDungeons["cave"]);
            Assert.IsFalse(run.IsOver);

            Assert.AreEqual(ExpeditionResult.Cleared, report.Result);
            CollectionAssert.AreEqual(new[] { 37, 0, 0 }, report.SurvivorFatigue, "In the order of the survivors.");
            CollectionAssert.AreEqual(new string[] { null, null, null }, report.SurvivorStates, "Nobody came home afflicted.");
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
            Assert.AreEqual("ben:1 cora:2", Rows(run), "Those who stood behind the dead advance.");
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
            RunRules.SetParty(data, run, Party(("anna", 1), ("ben", 2), ("cora", 3)));
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
            Assert.Greater(RunRules.FindMercenary(run, "anna").Fatigue, 0, "Battles built her fatigue up.");
            Assert.AreEqual(0, RunRules.FindMercenary(run, "dan").Fatigue, "Dan stayed home and was fresh.");
        }
    }
}
