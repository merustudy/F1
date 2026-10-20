using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Equipment requirements (Slice D stage 42, Docs/Design/02_Combat_System.md §4 "장비 요구 스탯", 04 §6, 05 §2): an item asks its owner for a stat. A
    /// member under it can have the item neither laid on, nor merged into, nor moved onto its board, while the inventory, the floor and the shop ask nothing;
    /// an enemy is never asked; every mercenary of a job wields the job's base weapon, the stagecoach lifting a candidate to it.
    /// </summary>
    public sealed class ItemRequirementTests
    {
        const string Greatsword = "greatsword";
        const string Plate = "plate";

        static readonly List<PartyMember> Party = new List<PartyMember>
        {
            new PartyMember("anna", "tank", 1),
            new PartyMember("ben", "healer", 2),
            new PartyMember("cora", "striker", 3),
        };

        /// <summary>
        /// The test data with the tank at strength 14 (the healer and the striker stay at the baseline 10), the blade asking Str 9 unless told not to, and two
        /// pieces asking Str 12: a greatsword (a weapon, sold and dropped) and a plate (defensive gear, sold).
        /// </summary>
        static StaticData Data(bool bladeAsks = true)
        {
            StaticDataParts parts = TestData.Parts(("RecruitStatSpread", 2), ("RecruitCount", 3), ("RosterLimit", 8));
            parts.Jobs = parts.Jobs.Select(job => job.Id == "tank"
                ? new JobData(job.Id, job.Name, job.WeaponItemId, job.RecommendedRow, job.Passive, job.PassiveText, job.Figure, stats: StatBlock.Uniform(10).With(Stat.Str, 14))
                : job).ToList();
            List<ItemData> items = parts.Items.Select(item => item.Id == "blade" && bladeAsks
                ? TestData.Item("blade", 2000, EffectKind.Damage, TargetMode.EnemyFront, 10, width: 2, reqStat: Stat.Str, reqValue: 9)
                : item).ToList();
            items.Add(TestData.Item(Greatsword, 2500, EffectKind.Damage, TargetMode.EnemyFront, 20, width: 2, shopWeight: 5, price: 10, dropWeight: 10, reqStat: Stat.Str, reqValue: 12));
            items.Add(TestData.Item(Plate, 5000, EffectKind.Shield, TargetMode.Self, 100, category: ItemCategory.Armor, width: 2, shopWeight: 5, price: 10, reqStat: Stat.Str, reqValue: 12));
            parts.Items = items;
            return new StaticData(parts);
        }

        /// <summary>A new expedition of the test cave, before its first node: between battles, with the party at its jobs' base values.</summary>
        static ExpeditionState Expedition(StaticData data)
        {
            return ExpeditionRules.Create(data, "cave", 1, Party);
        }

        static void Drop(ExpeditionState state, params string[] itemIds)
        {
            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot = itemIds.Select(id => new ItemOffer(OfferKind.Item, id)).ToList();
        }

        static EquippedItem Item(StaticData data, string id)
        {
            return new EquippedItem(data.Items.Get(id));
        }

        [Test]
        public void StatRules_MeetsRequirement_AsksTheStatTheItemNames_AndNothingOfAnOwnerWithoutStats()
        {
            StaticData data = Data();
            ItemData greatsword = data.Items.Get(Greatsword);

            Assert.IsTrue(StatRules.MeetsRequirement(StatBlock.Uniform(10).With(Stat.Str, 12), greatsword), "At the value asked.");
            Assert.IsTrue(StatRules.MeetsRequirement(StatBlock.Uniform(10).With(Stat.Str, 30), greatsword));
            Assert.IsFalse(StatRules.MeetsRequirement(StatBlock.Uniform(10).With(Stat.Str, 11), greatsword), "One under.");
            Assert.IsFalse(StatRules.MeetsRequirement(StatBlock.Uniform(30).With(Stat.Str, 1), greatsword), "No other stat stands in.");
            Assert.IsTrue(StatRules.MeetsRequirement(null, greatsword), "No stats (an enemy, a card without an owner): nothing is asked.");
            Assert.IsTrue(StatRules.MeetsRequirement(StatBlock.Uniform(1), data.Items.Get("knife")), "An item that asks nothing.");
        }

        [Test]
        public void ADrop_GoesOnlyOntoTheBoardOfAMemberWhoMeetsWhatItAsks_TheInventoryTakesItFromAnyone()
        {
            StaticData data = Data();
            ExpeditionState state = Expedition(data);
            Drop(state, Greatsword);
            Placement free = TestBoards.At(0, 1);
            Assert.IsTrue(state.Members[1].Board.FindRoom(2, 1, out _), "The healer has room: it is the requirement that refuses.");

            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, free), "The tank (Str 14) wields it.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 1, free), "The healer (Str 10) does not.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 2, free), "Nor the striker (Str 10).");
            Assert.IsFalse(ExpeditionRules.CanPlaceItem(data, state, data.Items.Get(Greatsword), 1, free));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.TakeLoot(data, state, 0, 1, free));
            Assert.IsTrue(ExpeditionRules.CanTakeLootToInventory(data, state, 0), "The inventory asks nothing.");

            ExpeditionRules.TakeLootToInventory(data, state, 0);
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 1, free), "From the inventory the healer is refused the same.");
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, free));
            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, free);
            Assert.AreEqual(Greatsword, state.Members[0].Board.ItemAt(0, 1).Item.Item.Id);
        }

        [Test]
        public void TheShop_SellsOntoABoardOnlyToAMemberWhoMeetsWhatItAsks_DefensiveGearIncluded()
        {
            StaticData data = Data();
            ExpeditionState state = Expedition(data);
            state.Phase = ExpeditionPhase.AtShop;
            state.Shop = new ShopState { Stock = new List<ItemOffer> { new ItemOffer(OfferKind.Item, Plate) } };
            state.Coins = 100;
            Placement free = TestBoards.At(0, 1);

            Assert.IsTrue(ExpeditionRules.CanAfford(data, state, 0));
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, 0, 0, free), "The tank wears the plate.");
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, 0, 2, free), "The striker (Str 10) cannot: the coins are not the question.");
            Assert.IsTrue(ExpeditionRules.CanBuyToInventory(data, state, 0), "Into the inventory anyone buys it.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyToBoard(data, state, 0, 2, free));
        }

        [Test]
        public void MovingAnItemBetweenBoards_AsksTheMemberItGoesTo_LayingItDownAgainIncluded()
        {
            StaticData data = Data();
            ExpeditionState state = Expedition(data);
            TestBoards.Put(state.Members[0], Item(data, Greatsword), 0, 1);

            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 0, 1, 0, TestBoards.At(1, 1)), "Within the tank's own board.");
            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 1, 1, TestBoards.At(0, 1)), "Onto the healer's: refused, room or not.");
            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state, 0, 0, 1));

            // One lying where it should not (a file from before the rule): picking it up and laying it down is laying it down — refused; the inventory takes it.
            TestBoards.Put(state.Members[1], Item(data, Greatsword), 0, 1);
            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 1, 0, 1, 1, TestBoards.At(1, 1)), "The healer cannot lay it down again on its own board.");
            Assert.IsTrue(ExpeditionRules.CanMoveToInventory(data, state, 1, 0, 1), "The inventory takes it from the healer.");
        }

        [Test]
        public void MergingIntoTheSameItem_AsksTheMemberToo_SoTheGhostAndTheCommandAgree()
        {
            StaticData data = Data();
            ExpeditionState state = Expedition(data);
            TestBoards.Put(state.Members[0], Item(data, Greatsword), 0, 1);
            TestBoards.Put(state.Members[1], Item(data, Greatsword), 0, 1);

            // A drop with its top-left on the same item: the tank's merges, the healer's does not.
            Drop(state, Greatsword);
            Assert.IsTrue(ExpeditionRules.LootMergesAt(state, 0, 0, 0, 1));
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, TestBoards.At(0, 1)));
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 1, 0, 1), "The healer cannot wield it, so nothing merges into its greatsword.");
            Assert.IsFalse(ExpeditionRules.CanTakeLoot(data, state, 0, 1, TestBoards.At(0, 1)));
            ExpeditionRules.LeaveLoot(state);

            // The healer's greatsword moved onto the tank's merges (the tank wields it); the tank's onto the healer's does not.
            Assert.IsTrue(ExpeditionRules.MergesAt(state, state.Members[1].Board.ItemAt(0, 1).Item, 0, 0, 1));
            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 1, 0, 1, 0, TestBoards.At(0, 1)));
            Assert.IsFalse(ExpeditionRules.MergesAt(state, state.Members[0].Board.ItemAt(0, 1).Item, 1, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanMoveItem(data, state, 0, 0, 1, 1, TestBoards.At(0, 1)));

            // From the inventory the same.
            state.Inventory.Add(Item(data, Greatsword));
            Assert.IsTrue(ExpeditionRules.MergesAt(state, state.Inventory[0], 0, 0, 1));
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 0, TestBoards.At(0, 1)));
            Assert.IsFalse(ExpeditionRules.MergesAt(state, state.Inventory[0], 1, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanPlaceFromInventory(data, state, 0, 1, TestBoards.At(0, 1)));
            Assert.IsTrue(ExpeditionRules.MergesInInventoryAt(state, state.Members[0].Board.ItemAt(0, 1).Item, state.Inventory.Items[0].At.X, state.Inventory.Items[0].At.Y),
                "The inventory asks nothing: anything merges there.");
        }

        [Test]
        public void AnItemThatAsksNothing_GoesAnywhereAsBefore()
        {
            StaticData data = Data();
            ExpeditionState state = Expedition(data);
            Drop(state, "knife");

            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 1, TestBoards.At(0, 1)), "The knife asks nothing of the healer.");
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 2, TestBoards.At(0, 1)));
        }

        [Test]
        public void AnEnemy_IsNeverAsked()
        {
            // The claw the grunts carry asks Str 30 of a mercenary; the grunts, which have no stats, carry it all the same.
            StaticDataParts parts = TestData.Parts();
            parts.Items = parts.Items.Select(item => item.Id == "claw"
                ? TestData.Item("claw", 3000, EffectKind.Damage, TargetMode.EnemyFront, reqStat: Stat.Str, reqValue: 30)
                : item).ToList();
            var data = new StaticData(parts);
            ExpeditionState state = Expedition(data);

            BattleSetup setup = ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.IsTrue(setup.Enemies.All(enemy => enemy.Items.Any(item => item.Item.Id == "claw")), "Every grunt still bites.");
            Assert.IsTrue(StatRules.MeetsRequirement(setup.Enemies[0].Stats, data.Items.Get("claw")), "An enemy has no stats to ask.");
        }

        [Test]
        public void TheStagecoach_LiftsACandidateToWieldItsJobsBaseWeapon_InThatStatOnly_SpendingNoDraw()
        {
            // The blade asks Str 9; the tank (base 14) never falls under it, the striker (base 10) rolls 8..12 and is lifted from 8; the healer's staff asks nothing.
            StaticData asking = Data();
            StaticData silent = Data(bladeAsks: false);
            int lifted = 0;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                List<RecruitState> with = RunRules.NewRun(asking, seed).Recruits;
                List<RecruitState> without = RunRules.NewRun(silent, seed).Recruits;
                Assert.AreEqual(without.Count, with.Count);
                for (int i = 0; i < with.Count; i++)
                {
                    Assert.AreEqual(without[i].Id + "/" + without[i].JobId, with[i].Id + "/" + with[i].JobId, "The same names and jobs: the lift draws nothing.");
                    CollectionAssert.AreEqual(without[i].Traits, with[i].Traits, "The same traits after the stats: the lift draws nothing.");
                    foreach (Stat stat in StatBlock.All.Where(s => s != Stat.Str))
                    {
                        Assert.AreEqual(without[i].Stats[stat], with[i].Stats[stat], $"{with[i].Id}: {stat} is not the blade's.");
                    }

                    bool wieldsABlade = with[i].JobId != "healer";
                    Assert.AreEqual(wieldsABlade ? Math.Max(9, without[i].Stats.Str) : without[i].Stats.Str, with[i].Stats.Str, with[i].Id);
                    lifted += wieldsABlade && without[i].Stats.Str < 9 ? 1 : 0;
                }
            }

            Assert.Greater(lifted, 0, "Some striker rolled under the blade and was lifted.");
        }

        [Test]
        public void StatRules_MeetingBaseWeapon_RaisesTheOneStatTheWeaponAsks_AndLeavesTheRest()
        {
            StaticData data = Data();
            StatBlock weak = StatBlock.Uniform(10).With(Stat.Str, 7).With(Stat.Dex, 3);

            Assert.AreEqual(StatBlock.Uniform(10).With(Stat.Str, 9).With(Stat.Dex, 3), StatRules.MeetingBaseWeapon(data, "striker", weak));
            Assert.AreEqual(weak, StatRules.MeetingBaseWeapon(data, "healer", weak), "The staff asks nothing.");
            Assert.AreEqual(StatBlock.Uniform(10).With(Stat.Str, 12), StatRules.MeetingBaseWeapon(data, "tank", StatBlock.Uniform(10).With(Stat.Str, 12)), "Already above.");
            Assert.IsTrue(RunRules.NewRun(data, 3).Roster.All(m => StatRules.MeetsRequirement(m.Stats, data.Items.Get(data.Jobs.Get(m.JobId).WeaponItemId))),
                "The starting roster stands at its jobs' base values, which the data checks against the base weapons.");
        }
    }
}
