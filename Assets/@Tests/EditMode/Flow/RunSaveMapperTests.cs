using System;
using System.Collections.Generic;
using System.Linq;
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
                    kit.Expedition.LeaveLoot();
                }
                else if (phase == ExpeditionPhase.PickingLoot)
                {
                    // The second of the two drops taken: a drop still lying there and a taken slot (null) are both saved.
                    kit.Expedition.TakeLootToInventory(1);
                }
            }

            return kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
        }

        /// <summary>Every stat at the test data's baseline (version 18): what the test jobs, which name no stats, give.</summary>
        static StatsRecord Baseline()
        {
            return new StatsRecord { Str = 10, Dex = 10, Con = 10, Int = 10, Wis = 10, End = 10 };
        }

        static void AssertRefused(RunSaveData save, StaticData data)
        {
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _));
        }

        [TestCase(ExpeditionPhase.ChoosingNode)]
        [TestCase(ExpeditionPhase.InBattle)]
        [TestCase(ExpeditionPhase.PickingLoot)]
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
        public void Save_StoresEnumsByName_RowsAsNumbers_AndSeedsAsText()
        {
            RunSaveData save = ValidSave(out StaticData _, ExpeditionPhase.InBattle);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.AreEqual("InBattle", save.Expedition.Phase);
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, save.Expedition.Members.ConvertAll(m => m.Row));
            CollectionAssert.AreEqual(new[] { 1, 2, 3 }, save.Run.Party.ConvertAll(p => p.Row));
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
            yield return Case("FatigueAboveMaximum", r => r.Roster[3].Fatigue = 201);
            yield return Case("FallenIsAlsoAlive", r => r.Fallen.Add(r.Roster[3].Id));
            yield return Case("FallenUnknown", r => r.Fallen.Add("nobody"));
            yield return Case("PartyMemberNotInRoster", r => r.Party[0].MercenaryId = "nobody");
            yield return Case("PartyRowZero", r => r.Party[0].Row = 0);
            yield return Case("PartyRowBeyondTheLast", r => r.Party[2].Row = BattleRows.Count + 1);
            yield return Case("PartyRowTakenTwice", r => r.Party[1].Row = r.Party[0].Row);
            yield return Case("PartyLeavesARowEmptyInFront", r => r.Party.RemoveAt(1));
            yield return Case("ClearedUnknownDungeon", r => r.ClearedDungeons.Add(new ClearRecord { DungeonId = "nowhere", Count = 1 }));
            yield return Case("ClearedZeroTimes", r => r.ClearedDungeons.Add(new ClearRecord { DungeonId = "cave", Count = 0 }));
            yield return Case("WipedOutWithALivingRoster", r => { r.Outcome = "Defeat"; r.DefeatCause = "Wipeout"; });
            yield return Case("OutcomeUnknown", r => r.Outcome = "Won");
            yield return Case("DefeatWithoutACause", r => { r.Outcome = "Defeat"; r.DefeatCause = "None"; });
            yield return Case("ACauseWhileOngoing", r => r.DefeatCause = "DeathLimit");
            yield return Case("NegativeExpeditionCount", r => r.ExpeditionCount = -1);

            // Version 18 (Slice D stage 32): the six stats of a mercenary and of a candidate, each within StatMin..StatMax.
            yield return Case("StatsMissing", r => r.Roster[0].Stats = null);
            yield return Case("StatOverStatMax", r => r.Roster[0].Stats.Str = 31);
            yield return Case("StatUnderStatMin", r => r.Roster[1].Stats.Con = 0);
            yield return Case("RecruitStatsMissing", r => r.Recruits[0].Stats = null);
            yield return Case("RecruitStatOverStatMax", r => r.Recruits[0].Stats.End = 31);

            // Version 14 (stage 26): the gold, the candidates, the dismissed and the expeditions per dungeon.
            yield return Case("NegativeGold", r => r.Gold = -1);
            yield return Case("RecruitsMissing", r => r.Recruits = null);
            yield return Case("RecruitUnknown", r => r.Recruits = new List<RecruitRecord> { new RecruitRecord { Id = "nobody", JobId = "tank", Stats = Baseline() } });
            yield return Case("RecruitIsAStartingMercenary", r => r.Recruits = new List<RecruitRecord> { new RecruitRecord { Id = "anna", JobId = "tank", Stats = Baseline() } });
            yield return Case("RecruitUnknownJob", r => r.Recruits = new List<RecruitRecord> { new RecruitRecord { Id = "eve", JobId = "nojob", Stats = Baseline() } });
            yield return Case("RecruitTwice", r => r.Recruits = new List<RecruitRecord> { new RecruitRecord { Id = "eve", JobId = "tank", Stats = Baseline() }, new RecruitRecord { Id = "eve", JobId = "healer", Stats = Baseline() } });
            yield return Case("RecruitIsAlsoInTheRoster", r => { r.Roster.Add(new MercenaryRecord { Id = "eve", JobId = "tank", Stats = Baseline() }); r.Recruits = new List<RecruitRecord> { new RecruitRecord { Id = "eve", JobId = "tank", Stats = Baseline() } }; });
            yield return Case("MoreRecruitsThanRecruitCount", r => r.Recruits = new List<RecruitRecord>
            {
                new RecruitRecord { Id = "eve", JobId = "tank", Stats = Baseline() }, new RecruitRecord { Id = "finn", JobId = "tank", Stats = Baseline() }, new RecruitRecord { Id = "gil", JobId = "tank", Stats = Baseline() }, new RecruitRecord { Id = "hana", JobId = "tank", Stats = Baseline() },
            });
            yield return Case("DismissedUnknown", r => r.Dismissed.Add("nobody"));
            yield return Case("DismissedIsAlsoAlive", r => r.Dismissed.Add(r.Roster[3].Id));
            yield return Case("DismissedIsAlsoDead", r => { r.Dismissed.Add("eve"); r.Fallen.Add("eve"); });
            yield return Case("RosterOverTheLimit", r => r.Roster.AddRange(new[] { "eve", "finn", "gil" }.Select(id => new MercenaryRecord { Id = id, JobId = "tank", Stats = Baseline() })));
            yield return Case("ExpeditionsUnknownDungeon", r => r.Expeditions.Add(new ExpeditionCountRecord { DungeonId = "nowhere", Count = 1 }));
            yield return Case("ExpeditionsZeroTimes", r => r.Expeditions.Add(new ExpeditionCountRecord { DungeonId = "pit", Count = 0 }));
            yield return Case("ExpeditionsDungeonTwice", r => r.Expeditions.Add(new ExpeditionCountRecord { DungeonId = "cave", Count = 1 }));

            // Version 15 (stage 27): the level on the ladder, the experience under what the level takes, none at the last level.
            yield return Case("LevelZero", r => r.Roster[3].Level = 0);
            yield return Case("LevelPastTheLast", r => r.Roster[3].Level = 4);
            yield return Case("ExpNegative", r => r.Roster[3].Exp = -1);
            yield return Case("ExpAtTheLastLevel", r => { r.Roster[3].Level = 3; r.Roster[3].Exp = 1; });

            // Version 16 (stage 30): the buildings' levels on their ladders, one construction per building building the next level, not yet due.
            yield return Case("BuildingsMissing", r => r.Buildings = null);
            yield return Case("BuildingUnknown", r => r.Buildings.Add(new BuildingLevelRecord { BuildingId = "tower", Level = 2 }));
            yield return Case("BuildingLevelZero", r => r.Buildings.Add(new BuildingLevelRecord { BuildingId = "barracks", Level = 0 }));
            yield return Case("BuildingLevelPastTheLast", r => r.Buildings.Add(new BuildingLevelRecord { BuildingId = "barracks", Level = 3 }));
            yield return Case("BuildingTwice", r => r.Buildings.AddRange(new[] { new BuildingLevelRecord { BuildingId = "inn", Level = 2 }, new BuildingLevelRecord { BuildingId = "inn", Level = 2 } }));
            yield return Case("ConstructionUnknownBuilding", r => r.Constructions.Add(new ConstructionRecord { BuildingId = "tower", ToLevel = 2, FinishDay = 9 }));
            yield return Case("ConstructionTwice", r => r.Constructions.AddRange(new[] { new ConstructionRecord { BuildingId = "coach", ToLevel = 2, FinishDay = 9 }, new ConstructionRecord { BuildingId = "coach", ToLevel = 2, FinishDay = 9 } }));
            yield return Case("ConstructionSkipsALevel", r => r.Constructions.Add(new ConstructionRecord { BuildingId = "coach", ToLevel = 3, FinishDay = 9 }));
            yield return Case("ConstructionOfTheLevelHeld", r => { r.Buildings.Add(new BuildingLevelRecord { BuildingId = "coach", Level = 2 }); r.Constructions.Add(new ConstructionRecord { BuildingId = "coach", ToLevel = 2, FinishDay = 9 }); });
            yield return Case("ConstructionAlreadyDue", r => r.Constructions.Add(new ConstructionRecord { BuildingId = "coach", ToLevel = 2, FinishDay = r.Day }));
            // Version 17 (2026-10-18): the gate is no building; a version 17 file naming it is broken (older files are not read since version 21).
            yield return Case("GateAsABuilding", r => r.Buildings.Add(new BuildingLevelRecord { BuildingId = "gate", Level = 2 }));
            yield return Case("RosterOverTheRaisedLimit", r => { r.Buildings.Add(new BuildingLevelRecord { BuildingId = "barracks", Level = 2 }); r.Roster.AddRange(new[] { "eve", "finn", "gil", "hana", "ivo" }.Select(id => new MercenaryRecord { Id = id, JobId = "tank", Level = 1, Stats = Baseline() })); });
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

            OfferRecord Claw()
            {
                return new OfferRecord { Kind = "Item", Id = "claw" };
            }

            ItemRecord Knife(int x, int y, int turns = 0)
            {
                return new ItemRecord { ItemId = "knife", X = x, Y = y, Turns = turns };
            }

            BagRecord Bag(string id, int x, int y, int turns = 0)
            {
                return new BagRecord { BagId = id, X = x, Y = y, Turns = turns };
            }

            yield return Case("UnknownDungeon", e => e.DungeonId = "nowhere");
            yield return Case("SeedOfAnotherRun", e => e.Seed = "1");
            yield return Case("PhaseFinished", e => e.Phase = "Finished");
            yield return Case("PhaseUnknown", e => e.Phase = "Resting");
            yield return Case("CampingAtABattleNode", e => e.Phase = "AtCamp");
            yield return Case("NoMembers", e => e.Members.Clear());
            yield return Case("MemberNotInRoster", e => e.Members[0].MercenaryId = "dan2");
            yield return Case("MemberTwice", e => e.Members[1].MercenaryId = e.Members[0].MercenaryId);
            yield return Case("MemberJobDiffers", e => e.Members[0].JobId = "healer");
            yield return Case("HpAboveMaximum", e => e.Members[0].Hp = e.Members[0].MaxHp + 1);
            yield return Case("HpNegative", e => e.Members[0].Hp = -1);
            yield return Case("DeadWithHp", e => e.Members[0].Alive = false);
            yield return Case("BoardMissing", e => e.Members[0].Items = null);
            yield return Case("EmptyEntryOnTheBoard", e => e.Members[0].Items.Add(null));
            yield return Case("UnknownItem", e => e.Members[0].Items[0].ItemId = "excalibur");
            yield return Case("ItemTierUnknown", e => e.Members[0].Items[0].Tier = "Platinum");
            yield return Case("ItemTierMissing", e => e.Inventory.Add(new ItemRecord { ItemId = "knife", Tier = null }));
            yield return Case("MemberFatigueNegative", e => e.Members[0].Fatigue = -1);
            yield return Case("MemberFatigueAboveMaximum", e => e.Members[0].Fatigue = 201);
            yield return Case("MemberStateUnknown", e => e.Members[0].State = "sleepy");
            yield return Case("MemberAfflictedAtTheRecovery", e => { e.Members[0].State = "fearful"; e.Members[0].Fatigue = 0; });
            yield return Case("FoundItemMarkedAsABaseWeapon", e => e.Inventory.Add(new ItemRecord { ItemId = "ballista", Base = true }));
            yield return Case("ItemOverAnotherItem", e => e.Members[0].Items.Add(Knife(1, 0)));
            yield return Case("ItemOffTheBags", e => e.Members[0].Items.Add(Knife(0, 2)));
            yield return Case("ItemOutsideTheFrame", e => e.Members[0].Items.Add(Knife(BoardFrame.Width, 0)));
            yield return Case("ItemHangingOffItsBag", e => e.Members[0].Items.Add(new ItemRecord { ItemId = "pike", X = 0, Y = 1 }));
            yield return Case("ItemTurnedFourTimes", e => e.Members[0].Items[0].Turns = 4);
            yield return Case("BagsMissing", e => e.Members[0].Bags = null);
            yield return Case("NoBags", e => e.Members[0].Bags.Clear());
            yield return Case("EmptyBagEntry", e => e.Members[0].Bags.Add(null));
            yield return Case("UnknownBag", e => e.Members[0].Bags.Add(Bag("sack", 0, 2)));
            yield return Case("StartBagNotFirst", e => e.Members[0].Bags.Insert(0, Bag("pouch", 0, 2)));
            yield return Case("StartBagTwice", e => e.Members[0].Bags.Add(Bag("pack", 0, 2)));
            yield return Case("StartBagMoved", e => e.Members[0].Bags[0].Y = 1);
            yield return Case("StartBagTurned", e => e.Members[0].Bags[0].Turns = 2);
            yield return Case("BagOverAnotherBag", e => e.Members[0].Bags.Add(Bag("pouch", 0, 1)));
            yield return Case("BagOutsideTheFrame", e => e.Members[0].Bags.Add(Bag("pouch", 0, BoardFrame.Height - 1, 1)));
            yield return Case("BagTurnedFourTimes", e => e.Members[0].Bags.Add(Bag("pouch", 0, 2, 4)));
            yield return Case("InventoryMissing", e => e.Inventory = null);
            yield return Case("InventoryUnknownItem", e => e.Inventory.Add(new ItemRecord { ItemId = "excalibur" }));
            yield return Case("InventoryEmptyEntry", e => e.Inventory.Add(null));
            yield return Case("InventoryItemsOverlap", e => e.Inventory.AddRange(new[] { Inv("knife", 1, 0), Inv("blade", 0, 0) }));
            yield return Case("InventoryItemOutsideTheGrid", e => e.Inventory.Add(Inv("blade", 9, 0)));
            yield return Case("InventoryItemTurnedFourTimes", e => e.Inventory.Add(Inv("knife", 0, 0, 4)));
            yield return Case("EveryoneDead", e => e.Members.ForEach(m => { m.Alive = false; m.Hp = 0; }));
            yield return Case("RowZero", e => e.Members[0].Row = 0);
            yield return Case("RowBeyondTheLast", e => e.Members[2].Row = BattleRows.Count + 1);
            yield return Case("RowTakenTwice", e => e.Members[1].Row = e.Members[0].Row);
            yield return Case("TheLivingLeaveARowEmptyInFront", e => { e.Members[1].Alive = false; e.Members[1].Hp = 0; });
            yield return Case("PotionSlotMissing", e => e.Potions.RemoveAt(0));
            yield return Case("UnknownPotion", e => e.Potions[1] = "elixir");
            yield return Case("NodeNotOnTheMap", e => e.CurrentNodeId = 999);
            yield return Case("NoNodeAfterABattle", e => e.CurrentNodeId = -1);
            yield return Case("LootOutsideTheLootPhase", e => e.Loot.Add(new OfferRecord { Kind = "Item", Id = "knife" }));
            yield return Case("BattleRecordOutsideABattle", e => e.Battle = new BattleRecord { Inputs = new List<BattleInputRecord>(), LogHash = "0" });
            yield return Case("NoLootInTheLootPhase", e => e.Loot.Clear(), ExpeditionPhase.PickingLoot);
            yield return Case("EveryDropTaken", e => e.Loot[0] = null, ExpeditionPhase.PickingLoot);
            yield return Case("MoreDropsThanCards", e => e.Loot.AddRange(new[] { Claw(), Claw() }), ExpeditionPhase.PickingLoot);
            yield return Case("DropKindUnknown", e => e.Loot[0].Kind = "Silver", ExpeditionPhase.PickingLoot);
            yield return Case("DropItemUnknown", e => e.Loot[0] = new OfferRecord { Kind = "Item", Id = "excalibur" }, ExpeditionPhase.PickingLoot);
            yield return Case("DropIsAPotion", e => e.Loot[0] = new OfferRecord { Kind = "Potion", Id = "tonic" }, ExpeditionPhase.PickingLoot);
            yield return Case("DropIsTheStartBag", e => e.Loot[0] = new OfferRecord { Kind = "Bag", Id = "pack" }, ExpeditionPhase.PickingLoot);
            yield return Case("DropBagUnknown", e => e.Loot[0] = new OfferRecord { Kind = "Bag", Id = "sack" }, ExpeditionPhase.PickingLoot);
            yield return Case("DropTierUnknown", e => e.Loot[0].Tier = "Copper", ExpeditionPhase.PickingLoot);
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
        public void Read_KeepsTheInventory_AndAnEmptyBoard()
        {
            RunSaveData save = ValidSave(out StaticData data);
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife" });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "pike", X = 1 });
            save.Expedition.Members[0].Items.Clear();

            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);

            CollectionAssert.AreEqual(new[] { "knife", "pike" }, expedition.Inventory.Select(i => i.Item.Id));
            Assert.IsEmpty(expedition.Members[0].Board.Items);
            Assert.AreEqual("pack", expedition.Members[0].Board.Bags.Single().Bag.Id, "The start bag stays.");
        }

        [Test]
        public void Read_AcceptsAFullInventory_AndKeepsWhereEachItemLies()
        {
            RunSaveData save = ValidSave(out StaticData data);
            for (int x = 0; x < 9; x += 3)
            {
                save.Expedition.Inventory.Add(Inv("ballista", x, 0));
            }

            save.Expedition.Inventory.Add(Inv("staff", 9, 0, 1));

            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);

            Assert.AreEqual(30, expedition.Inventory.UsedSquares, "Ten by three, full.");
            Assert.AreEqual(new Placement(9, 0, 1), expedition.Inventory.Items[3].At, "The staff stands in the last column.");
            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            CollectionAssert.AreEqual(new[] { 0, 3, 6, 9 }, again.Expedition.Inventory.ConvertAll(i => i.X));
            Assert.AreEqual(1, again.Expedition.Inventory[3].Turns);
        }

        [Test]
        public void Migrate_AcceptsTheCurrentVersion_AndRefusesOthers()
        {
            StaticData data = TestData.Data();
            Assert.DoesNotThrow(() => RunSaveMigrator.Migrate(new RunSaveData(), data));
            Assert.Throws<RunSaveException>(() => RunSaveMigrator.Migrate(new RunSaveData { SchemaVersion = RunSaveMigrator.OldestReadableVersion - 1 }, data));
            Assert.Throws<RunSaveException>(() => RunSaveMigrator.Migrate(new RunSaveData { SchemaVersion = RunSaveData.CurrentSchemaVersion + 1 }, data));
        }

        [Test]
        public void Read_Refuses_ARosterWithAVirtue_OrAnAfflictionAtTheRecovery_AndKeepsAValidAffliction()
        {
            RunSaveData save = ValidSave(out StaticData data);
            MercenaryRecord dan = save.Run.Roster.Find(m => m.Id == "dan");

            dan.Fatigue = 120;
            dan.Affliction = "fearful";
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState _);
            Assert.AreEqual("fearful", run.Roster.Find(m => m.Id == "dan").AfflictionId);

            dan.Affliction = "focused";
            AssertRefused(save, data);
            dan.Affliction = "fearful";
            dan.Fatigue = 99;
            RunSaveMapper.Read(save, data, out run, out ExpeditionState _);
            Assert.AreEqual("fearful", run.Roster.Find(m => m.Id == "dan").AfflictionId, "Stage 48: a mental breakdown may last under the threshold.");
            dan.Fatigue = 0;
            AssertRefused(save, data);
            dan.Affliction = "nothing";
            dan.Fatigue = 120;
            AssertRefused(save, data);
        }

        [Test]
        public void Save_KeepsTheStateOfAMemberAndTheAfflictionOfAMercenary()
        {
            RunSaveData save = ValidSave(out StaticData data);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            expedition.Members[0].Fatigue = 110;
            expedition.Members[0].StateId = "hopeless";
            expedition.Members[1].Fatigue = 40;
            expedition.Members[1].StateId = "stalwart";
            run.Roster.Find(m => m.Id == "dan").Fatigue = 150;
            run.Roster.Find(m => m.Id == "dan").AfflictionId = "reckless";

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            Assert.AreEqual("hopeless", again.Expedition.Members[0].State);
            Assert.AreEqual("stalwart", again.Expedition.Members[1].State, "A virtue on the way.");
            Assert.AreEqual("reckless", again.Run.Roster.Find(m => m.Id == "dan").Affliction);
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);
            Assert.AreEqual("hopeless", read.Members[0].StateId);
            Assert.AreEqual("stalwart", read.Members[1].StateId);
        }

        [Test]
        public void Save_KeepsTheStats_OfTheRosterAndTheCandidates_AndTheMembersTakeTheRosters()
        {
            // Slice D stage 32 (version 18, stage 34 version 19): the stats written and read back; a member has none of its own in the file.
            RunSaveData save = ValidSave(out StaticData data);
            Assert.AreEqual(26, RunSaveData.CurrentSchemaVersion, "Stage 47 (version 26): the same shape, the jobs are the eight.");
            Assert.IsTrue(save.Run.Roster.All(m => m.Stats != null) && save.Run.Recruits.All(r => r.Stats != null));
            save.Run.Roster[0].Stats.Str = 13;
            save.Run.Roster[0].Stats.End = 7;
            save.Run.Recruits[0].Stats.Int = 15;

            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);

            Assert.AreEqual(new StatBlock(13, 10, 10, 10, 10, 7), run.Roster[0].Stats);
            Assert.AreEqual(15, run.Recruits[0].Stats.Int);
            ExpeditionMember member = expedition.Members.FirstOrDefault(m => m.MercenaryId == run.Roster[0].Id);
            if (member != null)
            {
                Assert.AreSame(run.Roster[0].Stats, member.Stats);
            }

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            Assert.AreEqual(13, again.Run.Roster[0].Stats.Str);
            Assert.AreEqual(15, again.Run.Recruits[0].Stats.Int);
        }

        [Test]
        public void Save_KeepsTheTraits_OfTheRosterAndTheCandidates_AndTheMembersTakeTheRosters()
        {
            // Slice D stage 37 (version 22): the traits written and read back in their order; an expedition member has none of its own in the file.
            var kit = new FlowTestKit(TestData.DataWithTraits(("RecruitTraitWeight0", 0), ("RecruitTraitWeight2", 1))).InLobby();
            RunState run = kit.Run.Run;
            run.Roster[0].Traits = new List<string> { "sturdy", "keen" };
            Assert.IsTrue(run.Recruits.All(r => r.Traits.Count == 2), "The candidates drew two each.");
            kit.Expedition.Depart("cave");
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            CollectionAssert.AreEqual(new[] { "sturdy", "keen" }, save.Run.Roster[0].Traits);

            RunSaveMapper.Read(save, kit.Data, out RunState read, out ExpeditionState expedition);
            CollectionAssert.AreEqual(new[] { "sturdy", "keen" }, read.Roster[0].Traits);
            CollectionAssert.AreEqual(run.Recruits.Select(r => string.Join("+", r.Traits)).ToList(), read.Recruits.Select(r => string.Join("+", r.Traits)).ToList());
            ExpeditionMember member = expedition.Members.FirstOrDefault(m => m.MercenaryId == read.Roster[0].Id);
            if (member != null)
            {
                CollectionAssert.AreEqual(new[] { "sturdy", "keen" }, member.Traits, "A member takes the roster's.");
            }
        }

        [Test]
        public void Read_RefusesAnUnknownTrait_ATraitTwice_AndTwoThatClash()
        {
            var kit = new FlowTestKit(TestData.DataWithTraits()).InLobby();
            foreach (string[] traits in new[] { new[] { "nobody_knows" }, new[] { "keen", "keen" }, new[] { "dull", "keen" } })
            {
                RunSaveData save = RunSaveMapper.ToSave(kit.Run.Run, null);
                save.Run.Roster[0].Traits = traits.ToList();
                Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(save, kit.Data, out RunState _, out ExpeditionState _), string.Join("+", traits));
            }

            RunSaveData missing = RunSaveMapper.ToSave(kit.Run.Run, null);
            missing.Run.Recruits.ForEach(r => r.Traits = null);
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(missing, kit.Data, out RunState _, out ExpeditionState _), "A version 22 file lists them.");
        }

        [Test]
        public void Migrate_From21_GivesEveryMercenaryAndCandidateNoTraits()
        {
            // Version 22 (Slice D stage 37) added the traits; a version 21 run had none (Design/16 §5-7).
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 21;
            save.Run.Roster.ForEach(m => m.Traits = null);
            save.Run.Recruits.ForEach(r => r.Traits = null);

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion, "Brought on through 22, 23 and 24 to the current version.");
            Assert.IsTrue(save.Run.Roster.All(m => m.Traits != null && m.Traits.Count == 0) && save.Run.Recruits.All(r => r.Traits != null && r.Traits.Count == 0));
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState _);
            Assert.IsTrue(run.Roster.All(m => m.Traits.Count == 0));
        }

        [Test]
        public void Save_KeepsWhoWasAtDeathsDoor_AndAVersion22ExpeditionHadNobodyThere()
        {
            // Slice D stage 38 (version 23): whether a member was at death's door on the expedition, written and read back; a version 22 file
            // has no such field, so its members were at none (판정 7).
            RunSaveData save = ValidSave(out StaticData data);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            Assert.IsTrue(expedition.Members.All(m => !m.DeathsDoor));
            expedition.Members[1].DeathsDoor = true;

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            CollectionAssert.AreEqual(expedition.Members.Select(m => m.DeathsDoor).ToList(), again.Expedition.Members.Select(m => m.DeathsDoor).ToList());
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);
            Assert.IsTrue(read.Members[1].DeathsDoor);
            Assert.IsFalse(read.Members[0].DeathsDoor);

            again.SchemaVersion = 22;
            again.Expedition.Members.ForEach(m => m.DeathsDoor = false);
            RunSaveMigrator.Migrate(again, data);
            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, again.SchemaVersion);
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState migrated);
            Assert.IsTrue(migrated.Members.All(m => !m.DeathsDoor));
        }

        [Test]
        public void Migrate_From23_RenamesSpiritToWisdom_HalvesTheStats_AndGivesAMemberItsNewConstitutionsHp()
        {
            // Version 24 (2026-10-20 스탯 개정): a version 23 file's stats stood around 10; they are halved, rounded half up ((old + 1) / 2), Spi read into Wis,
            // and a member of the expedition going on gets the HP of its new constitution (0.10 a point in the test balance), its HP in proportion.
            RunSaveData save = ValidSave(out StaticData data, ExpeditionPhase.ChoosingNode);
            save.SchemaVersion = 23;
            MercenaryRecord first = save.Run.Roster[0];
            first.Stats = new StatsRecord { Str = 13, Dex = 9, Con = 12, Int = 6, Spi = 14, End = 11 };
            save.Run.Recruits[0].Stats = new StatsRecord { Str = 5, Dex = 10, Con = 15, Int = 1, Spi = 30, End = 7 };
            MemberRecord member = save.Expedition.Members.Single(m => m.MercenaryId == first.Id);
            member.MaxHp = 120;
            member.Hp = 90;

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.AreEqual((7, 5, 6, 3, 7, 6), (first.Stats.Str, first.Stats.Dex, first.Stats.Con, first.Stats.Int, first.Stats.Wis, first.Stats.End));
            Assert.IsNull(first.Stats.Spi, "Spirit is read once and written no more.");
            Assert.AreEqual((3, 5, 8, 1, 15, 4), (save.Run.Recruits[0].Stats.Str, save.Run.Recruits[0].Stats.Dex, save.Run.Recruits[0].Stats.Con, save.Run.Recruits[0].Stats.Int, save.Run.Recruits[0].Stats.Wis, save.Run.Recruits[0].Stats.End));
            Assert.AreEqual(60, member.MaxHp, "Constitution 6 now.");
            Assert.AreEqual(45, member.Hp, "90 of 120 becomes 45 of 60.");
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            Assert.AreEqual(new StatBlock(7, 5, 6, 3, 7, 6), run.Roster[0].Stats);
            Assert.AreEqual(60, expedition.Members.Single(m => m.MercenaryId == first.Id).MaxHp);

            // Nothing left of a member at 0 HP stays at 0; one with any HP keeps at least 1.
            RunSaveData low = ValidSave(out StaticData _, ExpeditionPhase.ChoosingNode);
            low.SchemaVersion = 23;
            MemberRecord weak = low.Expedition.Members[0];
            weak.MaxHp = 10000;
            weak.Hp = 1;
            RunSaveMigrator.Migrate(low, data);
            Assert.AreEqual(1, weak.Hp);
        }

        [Test]
        public void Migrate_From24_RaisesTheStatAJobsBaseWeaponAsks_ForTheRosterAndTheCandidates()
        {
            // Version 25 (Slice D stage 42): every mercenary of a job wields its base weapon. The blade asks Str 9 here; the staff (the healer's) asks nothing.
            StaticDataParts parts = TestData.Parts();
            parts.Items = parts.Items.Select(i => i.Id == "blade" ? TestData.Item("blade", 2000, EffectKind.Damage, TargetMode.EnemyFront, 10, width: 2, reqStat: Stat.Str, reqValue: 9) : i).ToList();
            var asking = new StaticData(parts);
            RunSaveData save = ValidSave(out StaticData _, ExpeditionPhase.ChoosingNode);
            save.SchemaVersion = 24;
            MercenaryRecord tank = save.Run.Roster.First(m => m.JobId == "tank");
            MercenaryRecord healer = save.Run.Roster.First(m => m.JobId == "healer");
            MercenaryRecord striker = save.Run.Roster.First(m => m.JobId == "striker");
            tank.Stats.Str = 7;
            healer.Stats.Str = 7;
            striker.Stats.Str = 12;
            RecruitRecord candidate = save.Run.Recruits[0];
            candidate.JobId = "striker";
            candidate.Stats.Str = 4;
            candidate.Stats.Dex = 6;

            RunSaveMigrator.Migrate(save, asking);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion, "Lifted to 25 and on to the current version (26 renames only the ranger).");
            Assert.AreEqual(9, tank.Stats.Str, "Raised to what the blade asks.");
            Assert.AreEqual(7, healer.Stats.Str, "The staff asks nothing: untouched.");
            Assert.AreEqual(12, striker.Stats.Str, "Already above: untouched.");
            Assert.AreEqual((9, 6), (candidate.Stats.Str, candidate.Stats.Dex), "A candidate is lifted the same, and only in that stat.");
            RunSaveMapper.Read(save, asking, out RunState run, out ExpeditionState expedition);
            Assert.AreEqual(9, run.Roster.First(m => m.JobId == "tank").Stats.Str);
            Assert.AreEqual(9, expedition.Members.First(m => m.JobId == "tank").Stats.Str, "A member going on takes the roster's stats.");
            Assert.IsTrue(run.Roster.Concat(run.Recruits.Select(r => new MercenaryState { JobId = r.JobId, Stats = r.Stats }))
                .All(m => StatRules.MeetsRequirement(m.Stats, asking.Items.Get(asking.Jobs.Get(m.JobId).WeaponItemId))), "Everyone wields their base weapon.");
        }

        [Test]
        public void Migrate_From25_RenamesTheRangerToTheArcher_WhereverAJobIdLives()
        {
            // Version 26 (Slice D stage 47, Design/20): the ranger is the archer under its new id — the roster, the candidates and the members going on.
            RunSaveData save = ValidSave(out StaticData _, ExpeditionPhase.ChoosingNode);
            save.SchemaVersion = 25;
            MercenaryRecord ranger = save.Run.Roster[0];
            MercenaryRecord other = save.Run.Roster[1];
            string othersJob = other.JobId;
            ranger.JobId = "ranger";
            save.Run.Recruits[0].JobId = "ranger";
            MemberRecord member = save.Expedition.Members[0];
            member.JobId = "ranger";

            RunSaveMigrator.Migrate(save, new StaticData(TestData.Parts()));

            Assert.AreEqual(26, save.SchemaVersion);
            Assert.AreEqual("archer", ranger.JobId);
            Assert.AreEqual("archer", save.Run.Recruits[0].JobId);
            Assert.AreEqual("archer", member.JobId);
            Assert.AreEqual(othersJob, other.JobId, "Any other job is untouched.");
        }

        [Test]
        public void Save_KeepsTheBuildingsLevels_AndTheConstructions_WithTheRosterTheyAllow()
        {
            // Stage 30: the barracks at level 2 holds a roster over the base limit; the stagecoach is being built, due on day 9.
            FlowTestKit kit = new FlowTestKit().InLobby();
            RunState run = kit.Run.Run;
            run.Gold = 100;
            run.BuildingLevels[BuildingId.Barracks] = 2;
            kit.Run.Upgrade(BuildingId.Coach);
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            CollectionAssert.AreEqual(new[] { "barracks:2" }, save.Run.Buildings.Select(b => b.BuildingId + ":" + b.Level).ToList());
            Assert.AreEqual(1, save.Run.Constructions.Count);
            Assert.AreEqual(("coach", 2, run.Day + 3), (save.Run.Constructions[0].BuildingId, save.Run.Constructions[0].ToLevel, save.Run.Constructions[0].FinishDay));
            RunSaveMapper.Read(save, kit.Data, out RunState read, out ExpeditionState _);
            Assert.AreEqual(2, BuildingRules.LevelOf(read, BuildingId.Barracks));
            Assert.AreEqual(1, BuildingRules.LevelOf(read, BuildingId.Inn));
            Assert.AreEqual(run.Day + 3, BuildingRules.ConstructionOf(read, BuildingId.Coach).FinishDay);
            Assert.AreEqual(60, read.Gold);
        }

        [Test]
        public void Save_KeepsTheLevelAndTheExperience()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            RunState run = kit.Run.Run;
            RunRules.FindMercenary(run, "ben").Level = 2;
            RunRules.FindMercenary(run, "ben").Exp = 7;
            kit.Run.Rest();
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;

            MercenaryRecord ben = save.Run.Roster.Find(m => m.Id == "ben");
            Assert.AreEqual(2, ben.Level);
            Assert.AreEqual(7, ben.Exp);
            RunSaveMapper.Read(save, kit.Data, out RunState read, out ExpeditionState _);
            Assert.AreEqual(2, RunRules.FindMercenary(read, "ben").Level);
            Assert.AreEqual(7, RunRules.FindMercenary(read, "ben").Exp);
            Assert.AreEqual(1, RunRules.FindMercenary(read, "anna").Level);
        }

        [Test]
        public void Save_KeepsExperiencePastTheLevel_AndTheBarracksLevelUpSavesAtOnce()
        {
            // Slice D stage 35 (version 20): the experience may pass what the level takes until the barracks levels the mercenary up.
            FlowTestKit kit = new FlowTestKit().InLobby();
            RunState run = kit.Run.Run;
            RunRules.FindMercenary(run, "ben").Exp = 25;
            kit.Run.Rest();
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            Assert.AreEqual(25, save.Run.Roster.Find(m => m.Id == "ben").Exp);
            RunSaveMapper.Read(save, kit.Data, out RunState read, out ExpeditionState _);
            Assert.AreEqual(25, RunRules.FindMercenary(read, "ben").Exp, "Past the level's 10: kept.");

            Assert.IsTrue(kit.Run.CanLevelUp("ben"));
            Assert.IsFalse(kit.Run.CanLevelUp("anna"));
            kit.Run.LevelUp("ben");
            MercenaryRecord ben = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Run.Roster.Find(m => m.Id == "ben");
            Assert.AreEqual(2, ben.Level, "Saved with the click.");
            Assert.AreEqual(15, ben.Exp);
            Assert.IsFalse(kit.Run.CanLevelUp("ben"), "15 of the next level's 20.");
        }

        [Test]
        public void Save_KeepsTheGoldTheCandidatesTheDismissedAndTheExpeditionsByDungeon()
        {
            FlowTestKit kit = new FlowTestKit().InLobby();
            RunState run = kit.Run.Run;
            run.Gold = 100;
            kit.Run.Hire(0);
            kit.Run.Dismiss("dan");
            kit.Expedition.Depart("cave");
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;

            Assert.AreEqual(70, save.Run.Gold);
            CollectionAssert.AreEqual(run.Recruits.Select(r => r.Id + ":" + r.JobId), save.Run.Recruits.ConvertAll(r => r.Id + ":" + r.JobId));
            CollectionAssert.AreEqual(new[] { "dan" }, save.Run.Dismissed);
            Assert.AreEqual("cave", save.Run.Expeditions.Single().DungeonId);
            Assert.AreEqual(1, save.Run.Expeditions.Single().Count);

            RunSaveMapper.Read(save, kit.Data, out RunState read, out ExpeditionState _);
            Assert.AreEqual(70, read.Gold);
            CollectionAssert.AreEqual(run.Recruits.Select(r => r.Id), read.Recruits.Select(r => r.Id));
            CollectionAssert.AreEqual(new[] { "dan" }, read.Dismissed);
            Assert.AreEqual(1, read.ExpeditionsByDungeon["cave"]);
            Assert.AreEqual(4, read.Roster.Count, "Four, one hired, one dismissed.");
        }

        [Test]
        public void Save_KeepsTheTierOfEveryItemAndDrop()
        {
            RunSaveData save = ValidSave(out StaticData data, ExpeditionPhase.PickingLoot);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            ExpeditionMember first = expedition.Members[0];
            BoardItem weapon = first.Board.Items[0];
            weapon.Item = new EquippedItem(weapon.Item.Item, isBase: true, tier: ItemTier.Tier2);
            expedition.Inventory.Add(new EquippedItem(data.Items.Get("knife"), tier: ItemTier.Tier1));
            ItemOffer drop = expedition.Loot.First(d => d != null);
            expedition.Loot[expedition.Loot.IndexOf(drop)] = new ItemOffer(OfferKind.Item, drop.Id, ItemTier.Tier3);

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);

            Assert.AreEqual(ItemTier.Tier2, read.Members[0].Board.Items[0].Item.Tier);
            Assert.IsTrue(read.Members[0].Board.Items[0].Item.IsBase);
            Assert.AreEqual(ItemTier.Tier1, read.Inventory.Single(i => i.Item.Id == "knife").Tier);
            Assert.AreEqual(ItemTier.Tier3, read.Loot.First(d => d != null).Tier);
            Assert.IsNull(read.Loot[1], "The taken slot stays empty.");
            Assert.AreEqual("Tier2", again.Expedition.Members[0].Items[0].Tier, "Stored by name.");
        }

        [Test]
        public void Save_KeepsEachMembersFatigue_AndWhichItemIsABaseWeapon()
        {
            RunSaveData save = ValidSave(out StaticData data);

            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);

            Assert.IsTrue(expedition.Members.TrueForAll(m => m.Fatigue == 0 + data.Balance.FatigueBattleEntry), "One battle was entered with only the base weapons.");
            Assert.IsTrue(expedition.Members.TrueForAll(m => m.Board.Items[0].Item.IsBase), "Everyone left with their base weapon.");
            Assert.IsTrue(save.Expedition.Members.TrueForAll(m => m.Items[0].Base));
        }

        [Test]
        public void Save_KeepsWhereEveryBagAndItemLies_AndHowTheyAreTurned()
        {
            RunSaveData save = ValidSave(out StaticData data);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            ItemBoard board = expedition.Members[0].Board;
            board.Bags.Add(new BoardBag(data.Bags.Get("pouch"), new Placement(0, 2)));
            board.Bags.Add(new BoardBag(data.Bags.Get("strap"), new Placement(2, 3, 1)));
            board.Items.Add(new BoardItem(new EquippedItem(data.Items.Get("bow"), powerPercent: 8), new Placement(2, 1, 1)));
            board.Items.Add(new BoardItem(new EquippedItem(data.Items.Get("knife"), powerPercent: 8), new Placement(2, 4)));

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            MemberRecord record = again.Expedition.Members[0];
            CollectionAssert.AreEqual(new[] { "pack", "pouch", "strap" }, record.Bags.ConvertAll(b => b.BagId));
            Assert.AreEqual(1, record.Bags[2].Turns, "The strap stands up.");
            Assert.AreEqual(1, record.Items.Find(i => i.ItemId == "bow").Turns);

            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);
            ItemBoard readBoard = read.Members[0].Board;
            CollectionAssert.AreEqual(board.Bags.Select(b => b.At), readBoard.Bags.Select(b => b.At));
            CollectionAssert.AreEqual(board.Items.Select(i => i.At), readBoard.Items.Select(i => i.At));
            Assert.AreEqual("bow", readBoard.ItemAt(2, 2).Item.Item.Id, "The bow stands across the pack and the pouch.");
        }

        [Test]
        public void Read_RefusesAPartyHoldingTwoUniqueItems()
        {
            // Stage 23: one unique per party, on the boards and in the inventory together.
            RunSaveData save = ValidSave(out StaticData data);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            ItemData unique = data.Items.Get("gladius_t");
            TestBoards.Put(expedition.Members[0], new EquippedItem(unique), 0, 1);
            RunSaveData one = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            Assert.DoesNotThrow(() => RunSaveMapper.Read(one, data, out RunState _, out ExpeditionState _), "One unique reads.");

            expedition.Inventory.Add(new EquippedItem(unique), new Placement(0, 0));
            RunSaveData two = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(two, data, out RunState _, out ExpeditionState _));
        }

        [Test]
        public void Read_RefusesAUniqueItemAtATier()
        {
            // Round 70 follow-up 3: a unique has no tier; the forge makes it at Common and nothing raises it.
            RunSaveData save = ValidSave(out StaticData data);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            TestBoards.Put(expedition.Members[0], new EquippedItem(data.Items.Get("gladius_t"), tier: ItemTier.Tier3), 0, 1);
            RunSaveData tiered = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            RunSaveException refused = Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(tiered, data, out RunState _, out ExpeditionState _));
            StringAssert.Contains("no tier", refused.Message);
        }


        static ItemRecord Inv(string id, int x, int y, int turns = 0)
        {
            return new ItemRecord { ItemId = id, X = x, Y = y, Turns = turns };
        }

    }
}
