using System;
using System.Collections.Generic;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The run save DTO and its validation. Every case breaks one thing in an otherwise valid file
    /// and expects the whole file to be refused.
    /// </summary>
    public sealed class RunSaveMapperTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        /// <summary>A valid save of a run whose party is on the node map, after one won battle.</summary>
        static RunSaveData ValidSave(out StaticData data, ExpeditionPhase phase = ExpeditionPhase.ChoosingNode)
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            data = kit.Data;
            kit.Run.Rest();
            kit.Expedition.Depart("cave");
            kit.Expedition.EnterNode(kit.Expedition.AvailableNodes()[0].Id);
            if (phase == ExpeditionPhase.InBattle)
            {
                kit.Expedition.AdvanceBattle(900);
                kit.Expedition.TryUsePotion(0, 0);
            }
            else
            {
                kit.FightToTheEnd();
                kit.Expedition.CloseBattle();
                if (phase == ExpeditionPhase.ChoosingNode)
                {
                    kit.Expedition.SkipReward();
                }
            }

            return kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
        }

        static void AssertRefused(RunSaveData save, StaticData data)
        {
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _));
        }

        [TestCase(ExpeditionPhase.ChoosingNode)]
        [TestCase(ExpeditionPhase.InBattle)]
        [TestCase(ExpeditionPhase.ChoosingReward)]
        public void Read_WhatWasSaved_GivesBackTheSameState(ExpeditionPhase phase)
        {
            RunSaveData save = ValidSave(out StaticData data, phase);

            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);

            Assert.AreEqual(phase, expedition.Phase);
            BattleEngine battle = phase == ExpeditionPhase.InBattle
                ? BattleEngine.Replay(ExpeditionRules.BuildBattleSetup(data, expedition), RunSaveMapper.ToInputs(save.Expedition.Battle), save.Expedition.Battle.ConfirmedTimeMs)
                : null;
            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, battle));
            Assert.AreEqual(
                System.Text.Encoding.UTF8.GetString(SaveManager.Serialize(save)),
                System.Text.Encoding.UTF8.GetString(SaveManager.Serialize(again)));
        }

        [Test]
        public void Save_StoresEnumsByName_AndSeedsAsText()
        {
            RunSaveData save = ValidSave(out StaticData _, ExpeditionPhase.InBattle);

            Assert.AreEqual("InBattle", save.Expedition.Phase);
            Assert.AreEqual("Front", save.Expedition.Members[0].Row);
            Assert.AreEqual("UsePotion", save.Expedition.Battle.Inputs[0].Kind);
            Assert.AreEqual(FlowTestKit.Seed.ToString(), save.Run.Seed);
            Assert.AreEqual(900, save.Expedition.Battle.ConfirmedTimeMs);
        }

        static IEnumerable<TestCaseData> BrokenRuns()
        {
            TestCaseData Case(string name, Action<RunRecord> breakIt)
            {
                return new TestCaseData(breakIt).SetName("Read_Refuses_Run_" + name);
            }

            yield return Case("SeedIsNotANumber", r => r.Seed = "abc");
            yield return Case("SeedIsNegative", r => r.Seed = "-1");
            yield return Case("DayZero", r => r.Day = 0);
            yield return Case("RosterMissing", r => r.Roster = null);
            yield return Case("UnknownMercenary", r => r.Roster[3].Id = "nobody");
            yield return Case("UnknownJob", r => r.Roster[3].JobId = "nojob");
            yield return Case("MercenaryTwice", r => r.Roster[3].Id = r.Roster[0].Id);
            yield return Case("FatigueNegative", r => r.Roster[3].Fatigue = -1);
            yield return Case("FatigueAboveMaximum", r => r.Roster[3].Fatigue = 101);
            yield return Case("FallenIsAlsoAlive", r => r.Fallen.Add(r.Roster[3].Id));
            yield return Case("FallenUnknown", r => r.Fallen.Add("nobody"));
            yield return Case("PartyMemberNotInRoster", r => r.Party[0].MercenaryId = "nobody");
            yield return Case("PartyRowUnknown", r => r.Party[0].Row = "Middle");
            yield return Case("PartyRowIsANumber", r => r.Party[0].Row = "0");
            yield return Case("ClearedUnknownDungeon", r => r.ClearedDungeons.Add(new ClearRecord { DungeonId = "nowhere", Count = 1 }));
            yield return Case("ClearedZeroTimes", r => r.ClearedDungeons.Add(new ClearRecord { DungeonId = "cave", Count = 0 }));
            yield return Case("OverWithALivingRoster", r => r.IsOver = true);
            yield return Case("NegativeExpeditionCount", r => r.ExpeditionCount = -1);
        }

        [TestCaseSource(nameof(BrokenRuns))]
        public void Read_Refuses_BrokenRun(Action<RunRecord> breakIt)
        {
            RunSaveData save = ValidSave(out StaticData data);
            save.Expedition = null;
            save.Run.ExpeditionCount = 0;
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _), "The case starts from a valid file.");

            breakIt(save.Run);

            AssertRefused(save, data);
        }

        static IEnumerable<TestCaseData> BrokenExpeditions()
        {
            TestCaseData Case(string name, Action<ExpeditionRecord> breakIt, ExpeditionPhase phase = ExpeditionPhase.ChoosingNode)
            {
                return new TestCaseData(breakIt, phase).SetName("Read_Refuses_Expedition_" + name);
            }

            yield return Case("UnknownDungeon", e => e.DungeonId = "nowhere");
            yield return Case("SeedOfAnotherRun", e => e.Seed = "1");
            yield return Case("PhaseFinished", e => e.Phase = "Finished");
            yield return Case("PhaseUnknown", e => e.Phase = "Resting");
            yield return Case("NoMembers", e => e.Members.Clear());
            yield return Case("MemberNotInRoster", e => e.Members[0].MercenaryId = "dan2");
            yield return Case("MemberTwice", e => e.Members[1].MercenaryId = e.Members[0].MercenaryId);
            yield return Case("MemberJobDiffers", e => e.Members[0].JobId = "healer");
            yield return Case("HpAboveMaximum", e => e.Members[0].Hp = e.Members[0].MaxHp + 1);
            yield return Case("HpNegative", e => e.Members[0].Hp = -1);
            yield return Case("DeadWithHp", e => e.Members[0].Alive = false);
            yield return Case("NoItemSlots", e => e.Members[0].Items.Clear());
            yield return Case("UnknownItem", e => e.Members[0].Items[0].ItemId = "excalibur");
            yield return Case("ItemGradeZero", e => e.Members[0].Items[0].Grade = 0);
            yield return Case("EveryoneDead", e => e.Members.ForEach(m => { m.Alive = false; m.Hp = 0; }));
            yield return Case("RowUnknown", e => e.Members[0].Row = "Top");
            yield return Case("PotionSlotMissing", e => e.Potions.RemoveAt(0));
            yield return Case("UnknownPotion", e => e.Potions[1] = "elixir");
            yield return Case("NodeNotOnTheMap", e => e.CurrentNodeId = 999);
            yield return Case("NoNodeAfterABattle", e => e.CurrentNodeId = -1);
            yield return Case("RewardsOutsideTheRewardPhase", e => e.PendingRewards.Add(new RewardRecord { Kind = "Item", Id = "knife", Grade = 8 }));
            yield return Case("BattleRecordOutsideABattle", e => e.Battle = new BattleRecord { Inputs = new List<BattleInputRecord>(), LogHash = "0" });
            yield return Case("NoRewardsInTheRewardPhase", e => e.PendingRewards.Clear(), ExpeditionPhase.ChoosingReward);
            yield return Case("RewardKindUnknown", e => e.PendingRewards[0].Kind = "Gold", ExpeditionPhase.ChoosingReward);
            yield return Case("RewardItemUnknown", e => e.PendingRewards.Add(new RewardRecord { Kind = "Item", Id = "excalibur", Grade = 8 }), ExpeditionPhase.ChoosingReward);
            yield return Case("RewardPotionWithGrade", e => e.PendingRewards.Add(new RewardRecord { Kind = "Potion", Id = "tonic", Grade = 3 }), ExpeditionPhase.ChoosingReward);
            yield return Case("NoBattleRecordInABattle", e => e.Battle = null, ExpeditionPhase.InBattle);
            yield return Case("ConfirmedTimeNegative", e => e.Battle.ConfirmedTimeMs = -1, ExpeditionPhase.InBattle);
            yield return Case("InputAfterTheConfirmedTime", e => e.Battle.Inputs[0].TimeMs = e.Battle.ConfirmedTimeMs + 1, ExpeditionPhase.InBattle);
            yield return Case("InputKindUnknown", e => e.Battle.Inputs[0].Kind = "Dance", ExpeditionPhase.InBattle);
            yield return Case("InputIndexNegative", e => e.Battle.Inputs[0].PartyIndex = -1, ExpeditionPhase.InBattle);
            yield return Case("InputsMissing", e => e.Battle.Inputs = null, ExpeditionPhase.InBattle);
        }

        [TestCaseSource(nameof(BrokenExpeditions))]
        public void Read_Refuses_BrokenExpedition(Action<ExpeditionRecord> breakIt, ExpeditionPhase phase)
        {
            RunSaveData save = ValidSave(out StaticData data, phase);

            breakIt(save.Expedition);

            AssertRefused(save, data);
        }

        [Test]
        public void Read_WhenThePartyIsAtHome_GivesNoExpedition()
        {
            RunSaveData save = ValidSave(out StaticData data);
            save.Expedition = null;

            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);

            Assert.IsNull(expedition);
            Assert.AreEqual(2, run.Day);
        }

        [Test]
        public void Read_Refuses_AnEmptyFile()
        {
            AssertRefused(null, TestData.Data());
            AssertRefused(new RunSaveData(), TestData.Data());
        }

        [Test]
        public void Migrate_AcceptsTheCurrentVersion_AndRefusesOthers()
        {
            Assert.DoesNotThrow(() => RunSaveMigrator.Migrate(new RunSaveData()));
            Assert.Throws<RunSaveException>(() => RunSaveMigrator.Migrate(new RunSaveData { SchemaVersion = RunSaveMigrator.OldestReadableVersion - 1 }));
            Assert.Throws<RunSaveException>(() => RunSaveMigrator.Migrate(new RunSaveData { SchemaVersion = RunSaveData.CurrentSchemaVersion + 1 }));
        }
    }
}
