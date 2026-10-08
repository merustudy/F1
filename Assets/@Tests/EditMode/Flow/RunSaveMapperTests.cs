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

            ItemRecord Ballista()
            {
                return new ItemRecord { ItemId = "ballista", Grade = 8 };
            }

            OfferRecord Claw()
            {
                return new OfferRecord { Kind = "Item", Id = "claw", Grade = 8 };
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
            yield return Case("ItemGradeZero", e => e.Members[0].Items[0].Grade = 0);
            yield return Case("ItemTierUnknown", e => e.Members[0].Items[0].Tier = "Platinum");
            yield return Case("ItemTierMissing", e => e.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8, Tier = null }));
            yield return Case("MemberFatigueNegative", e => e.Members[0].Fatigue = -1);
            yield return Case("MemberFatigueAboveMaximum", e => e.Members[0].Fatigue = 201);
            yield return Case("MemberStateUnknown", e => e.Members[0].State = "sleepy");
            yield return Case("MemberAfflictedUnderTheBreakdown", e => { e.Members[0].State = "fearful"; e.Members[0].Fatigue = 50; });
            yield return Case("FoundItemMarkedAsABaseWeapon", e => e.Inventory.Add(new ItemRecord { ItemId = "ballista", Grade = 8, Base = true }));
            yield return Case("BoardOverItsCells", e => e.Members[0].Items.Add(new ItemRecord { ItemId = "ballista", Grade = 8 }), ExpeditionPhase.ChoosingNode);
            yield return Case("InventoryMissing", e => e.Inventory = null);
            yield return Case("InventoryUnknownItem", e => e.Inventory.Add(new ItemRecord { ItemId = "excalibur", Grade = 8 }));
            yield return Case("InventoryEmptyEntry", e => e.Inventory.Add(null));
            yield return Case("InventoryOverItsCells", e => e.Inventory.AddRange(new[] { Ballista(), Ballista(), Ballista(), Ballista() }));
            yield return Case("EveryoneDead", e => e.Members.ForEach(m => { m.Alive = false; m.Hp = 0; }));
            yield return Case("RowZero", e => e.Members[0].Row = 0);
            yield return Case("RowBeyondTheLast", e => e.Members[2].Row = BattleRows.Count + 1);
            yield return Case("RowTakenTwice", e => e.Members[1].Row = e.Members[0].Row);
            yield return Case("TheLivingLeaveARowEmptyInFront", e => { e.Members[1].Alive = false; e.Members[1].Hp = 0; });
            yield return Case("PotionSlotMissing", e => e.Potions.RemoveAt(0));
            yield return Case("UnknownPotion", e => e.Potions[1] = "elixir");
            yield return Case("NodeNotOnTheMap", e => e.CurrentNodeId = 999);
            yield return Case("NoNodeAfterABattle", e => e.CurrentNodeId = -1);
            yield return Case("LootOutsideTheLootPhase", e => e.Loot.Add(new OfferRecord { Kind = "Item", Id = "knife", Grade = 8 }));
            yield return Case("BattleRecordOutsideABattle", e => e.Battle = new BattleRecord { Inputs = new List<BattleInputRecord>(), LogHash = "0" });
            yield return Case("NoLootInTheLootPhase", e => e.Loot.Clear(), ExpeditionPhase.PickingLoot);
            yield return Case("EveryDropTaken", e => e.Loot[0] = null, ExpeditionPhase.PickingLoot);
            yield return Case("MoreDropsThanCards", e => e.Loot.AddRange(new[] { Claw(), Claw() }), ExpeditionPhase.PickingLoot);
            yield return Case("DropKindUnknown", e => e.Loot[0].Kind = "Silver", ExpeditionPhase.PickingLoot);
            yield return Case("DropItemUnknown", e => e.Loot[0] = new OfferRecord { Kind = "Item", Id = "excalibur", Grade = 8 }, ExpeditionPhase.PickingLoot);
            yield return Case("DropIsAPotion", e => e.Loot[0] = new OfferRecord { Kind = "Potion", Id = "tonic", Grade = 0 }, ExpeditionPhase.PickingLoot);
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
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8 });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "pike", Grade = 9 });
            save.Expedition.Members[0].Items.Clear();

            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);

            CollectionAssert.AreEqual(new[] { "knife", "pike" }, expedition.Inventory.ConvertAll(i => i.Item.Id));
            Assert.AreEqual(9, expedition.Inventory[1].Grade);
            Assert.IsEmpty(expedition.Members[0].Items);
            Assert.AreEqual(3, expedition.Members[0].ItemSlots, "The cells come from the job.");
        }

        [Test]
        public void Read_AcceptsAnInventoryFilledToItsCells()
        {
            RunSaveData save = ValidSave(out StaticData data);
            for (int i = 0; i < 3; i++)
            {
                save.Expedition.Inventory.Add(new ItemRecord { ItemId = "ballista", Grade = 8 });
            }

            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8 });

            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);

            Assert.AreEqual(0, ExpeditionRules.FreeInventoryCells(data, expedition), "Ten cells: three ballistas and a knife.");
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
        public void Migrate_From2_CompactsTheBoards_AndAddsAnEmptyInventory()
        {
            // Version 2 stored one entry per slot, null for an empty one, and no inventory.
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 2;
            save.Expedition.Inventory = null;
            save.Expedition.Members[0].Items = new List<ItemRecord> { null, new ItemRecord { ItemId = "blade", Grade = 10 }, null };
            save.Expedition.Members[1].Items = new List<ItemRecord> { null, null, null };
            save.Run.Roster.ForEach(m => m.Fatigue = 100);

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.IsEmpty(save.Expedition.Inventory);
            CollectionAssert.AreEqual(new[] { "blade" }, save.Expedition.Members[0].Items.ConvertAll(i => i.ItemId));
            Assert.IsEmpty(save.Expedition.Members[1].Items);
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _), "The migrated file reads.");

            var home = new RunSaveData { SchemaVersion = 2, Run = save.Run, Expedition = null };
            RunSaveMigrator.Migrate(home, data);
            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, home.SchemaVersion);
            Assert.IsNull(home.Expedition);
        }

        [Test]
        public void Migrate_From3_TurnsTheFatigueLeftIntoTheFatigueSpent_AndMarksTheJobWeaponsAsBase()
        {
            // Version 3 counted fatigue down from 100, members carried none, and nothing was marked as a base weapon.
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 3;
            save.Run.Roster.ForEach(m => m.Fatigue = 100);
            save.Run.Roster.Find(m => m.Id == "anna").Fatigue = 70;
            save.Expedition.Members.ForEach(m => { m.Fatigue = 0; m.Items.ForEach(i => i.Base = false); });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "blade", Grade = 10 });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8 });

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.AreEqual(30, save.Run.Roster.Find(m => m.Id == "anna").Fatigue, "70 left of 100: 30 spent.");
            Assert.AreEqual(0, save.Run.Roster.Find(m => m.Id == "dan").Fatigue, "Fresh.");
            Assert.AreEqual(30, save.Expedition.Members.Find(m => m.MercenaryId == "anna").Fatigue, "She carries what she left with.");
            Assert.IsTrue(save.Expedition.Members.TrueForAll(m => m.Items[0].Base), "Everyone's job weapon.");
            CollectionAssert.AreEqual(new[] { true, false }, save.Expedition.Inventory.ConvertAll(i => i.Base), "A job weapon in the inventory too; the knife was found.");
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _), "The migrated file reads.");
        }

        [Test]
        public void Read_Refuses_ARosterWithAVirtue_OrAnAfflictionUnderTheBreakdown_AndKeepsAValidAffliction()
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
            AssertRefused(save, data);
            dan.Affliction = "nothing";
            dan.Fatigue = 120;
            AssertRefused(save, data);
        }

        [Test]
        public void Save_KeepsTheStateOfAMemberAndTheAfflictionOfAMercenary_AndVersion5ReadsWithNone()
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

            // Version 5 knew no states.
            RunSaveData old = ValidSave(out data);
            old.SchemaVersion = 5;
            old.Run.Roster.ForEach(m => m.Affliction = null);
            old.Expedition.Members.ForEach(m => m.State = null);
            RunSaveMigrator.Migrate(old, data);
            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, old.SchemaVersion);
            Assert.DoesNotThrow(() => RunSaveMapper.Read(old, data, out RunState _, out ExpeditionState _));
        }

        [Test]
        public void Migrate_From4_MakesEveryItemBronze()
        {
            // Version 4 had no tiers. (Its reward choice is left behind: From8To9.)
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 4;
            save.Expedition.Members.ForEach(m => m.Items.ForEach(i => i.Tier = null));
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8, Tier = null });

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.IsTrue(save.Expedition.Members.TrueForAll(m => m.Items.TrueForAll(i => i.Tier == "Common")));
            Assert.AreEqual("Common", save.Expedition.Inventory.Single().Tier);
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _), "The migrated file reads.");
        }

        [Test]
        public void Migrate_From8_LeavesTheRewardChoiceBehind_WithAnEmptyLoot()
        {
            // Version 8 chose a reward after a battle; a file in the middle of that choice goes on to the node choice.
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 8;
            save.Expedition.Phase = "ChoosingReward";
            save.Expedition.Loot = null;

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.AreEqual("ChoosingNode", save.Expedition.Phase);
            Assert.IsEmpty(save.Expedition.Loot);
            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, expedition.Phase);
        }

        [Test]
        public void Migrate_From6_RenamesTheTiers()
        {
            // Version 6 named the tiers Bronze, Silver, Gold and Diamond; version 7 calls the same four Common, Bronze, Silver and Gold (round 41).
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 6;
            ItemRecord first = save.Expedition.Members[0].Items[0];
            first.Tier = "Bronze";
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8, Tier = "Silver" });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8, Tier = "Gold" });
            save.Expedition.Inventory.Add(new ItemRecord { ItemId = "knife", Grade = 8, Tier = "Diamond" });

            RunSaveMigrator.Migrate(save, data);

            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            Assert.AreEqual("Common", first.Tier);
            Assert.AreEqual("Bronze", save.Expedition.Inventory[0].Tier);
            Assert.AreEqual("Silver", save.Expedition.Inventory[1].Tier);
            Assert.AreEqual("Gold", save.Expedition.Inventory[2].Tier);
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState _), "The migrated file reads.");
        }

        [Test]
        public void Migrate_From7_ReadsWithNoCoinsAndNoShop_AndTheCoinsAreKept()
        {
            // Version 7 had no region coins and no shop (stage 17): such a file comes back with 0 coins at no shop.
            RunSaveData save = ValidSave(out StaticData data);
            save.SchemaVersion = 7;
            save.Expedition.Coins = 0;
            save.Expedition.Shop = null;

            RunSaveMigrator.Migrate(save, data);
            Assert.AreEqual(RunSaveData.CurrentSchemaVersion, save.SchemaVersion);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            Assert.AreEqual(0, expedition.Coins);
            Assert.IsNull(expedition.Shop);

            expedition.Coins = 42;
            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            Assert.AreEqual(42, again.Expedition.Coins);
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);
            Assert.AreEqual(42, read.Coins);

            again.Expedition.Coins = -1;
            AssertRefused(again, data);
            again.Expedition.Coins = 0;
            again.Expedition.Shop = new ShopRecord { Stock = new List<OfferRecord>(), Refreshes = 0 };
            AssertRefused(again, data);
        }

        [Test]
        public void Save_KeepsTheTierOfEveryItemAndDrop()
        {
            RunSaveData save = ValidSave(out StaticData data, ExpeditionPhase.PickingLoot);
            RunSaveMapper.Read(save, data, out RunState run, out ExpeditionState expedition);
            ExpeditionMember first = expedition.Members[0];
            first.Items[0] = new EquippedItem(first.Items[0].Item, first.Items[0].Grade, isBase: true, tier: ItemTier.Silver);
            expedition.Inventory.Add(new EquippedItem(data.Items.Get("knife"), 8, tier: ItemTier.Bronze));
            ItemOffer drop = expedition.Loot.First(d => d != null);
            expedition.Loot[expedition.Loot.IndexOf(drop)] = new ItemOffer(OfferKind.Item, drop.Id, drop.Grade, ItemTier.Gold);

            RunSaveData again = RunSaveMapper.ToSave(run, RunSaveMapper.ToRecord(expedition, null));
            RunSaveMapper.Read(again, data, out RunState _, out ExpeditionState read);

            Assert.AreEqual(ItemTier.Silver, read.Members[0].Items[0].Tier);
            Assert.IsTrue(read.Members[0].Items[0].IsBase);
            Assert.AreEqual(ItemTier.Bronze, read.Inventory.Single(i => i.Item.Id == "knife").Tier);
            Assert.AreEqual(ItemTier.Gold, read.Loot.First(d => d != null).Tier);
            Assert.IsNull(read.Loot[1], "The taken slot stays empty.");
            Assert.AreEqual("Silver", again.Expedition.Members[0].Items[0].Tier, "Stored by name.");
        }

        [Test]
        public void Save_KeepsEachMembersFatigue_AndWhichItemIsABaseWeapon()
        {
            RunSaveData save = ValidSave(out StaticData data);

            RunSaveMapper.Read(save, data, out RunState _, out ExpeditionState expedition);

            Assert.IsTrue(expedition.Members.TrueForAll(m => m.Fatigue == 0 + data.Balance.FatigueBattleEntry), "One battle was entered with only the base weapons.");
            Assert.IsTrue(expedition.Members.TrueForAll(m => m.Items[0].IsBase), "Everyone left with their base weapon.");
            Assert.IsTrue(save.Expedition.Members.TrueForAll(m => m.Items[0].Base));
        }
    }
}
