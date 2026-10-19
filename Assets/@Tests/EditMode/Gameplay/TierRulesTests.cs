using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Item tiers, merging and the camp's upkeep (Docs/Design/02_Combat_System.md §4, 03_Dungeon_Structure.md §1 and §5:
    /// Slice B).
    /// </summary>
    public sealed class TierRulesTests
    {
        static readonly PartyMember[] Party =
        {
            new PartyMember("anna", "tank", 1),
            new PartyMember("ben", "healer", 2),
            new PartyMember("cora", "striker", 3),
        };

        /// <summary>A new expedition of the test cave, on its map before the first floor: between battles.</summary>
        static ExpeditionState Expedition(StaticData data)
        {
            return ExpeditionRules.Create(data, "cave", 1, Party);
        }

        static EquippedItem Item(StaticData data, string id, ItemTier tier)
        {
            return new EquippedItem(data.Items.Get(id), tier: tier);
        }

        /// <summary>A board as "blade:Common knife:Tier1".</summary>
        static string Board(ExpeditionMember member)
        {
            return string.Join(" ", TestBoards.Items(member).Select(i => $"{i.Item.Id}:{i.Tier}"));
        }

        // ---- Data ----------------------------------------------------------------------------

        [Test]
        public void Balance_RefusesTiersThatDoNotGrow()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("Tier1Percent", 100)), "Bronze is not above Common.");
            Assert.Throws<DataException>(() => TestData.Balance(("Tier2Percent", 200)), "Silver is not above Bronze.");
            Assert.Throws<DataException>(() => TestData.Balance(("Tier3Percent", 250)), "Gold is under Silver.");
            Assert.DoesNotThrow(() => TestData.Balance(("Tier1Percent", 150), ("Tier2Percent", 200), ("Tier3Percent", 250)));
        }

        [Test]
        public void Dungeon_RefusesTierFloorsOutOfOrder_OrOffTheMap()
        {
            var potions = new List<string>();
            DungeonData Make(int bronze, int silver, int gold)
            {
                return new DungeonData("d", TestData.Text("d"), "swift", 10, 2, 3, 2, potions, tier1Floor: bronze, tier2Floor: silver, tier3Floor: gold);
            }

            Assert.DoesNotThrow(() => Make(3, 6, 9));
            Assert.DoesNotThrow(() => Make(0, 0, 0), "Common everywhere.");
            Assert.DoesNotThrow(() => Make(4, 0, 0));
            Assert.Throws<DataException>(() => Make(6, 3, 0), "Silver before Bronze.");
            Assert.Throws<DataException>(() => Make(3, 3, 0), "Two tiers from one floor.");
            Assert.Throws<DataException>(() => Make(3, 6, 11), "Off the map.");
            Assert.Throws<DataException>(() => Make(0, 5, 0), "Silver where Bronze never starts.");
            Assert.Throws<DataException>(() => Make(-1, 0, 0));
        }

        // ---- Effect size ---------------------------------------------------------------------

        [Test]
        public void ATier_MakesAnItemsEffects_ItsPercentOfBronzes()
        {
            BalanceData balance = TestData.Balance(("Tier3Percent", 450));
            ItemData knife = TestData.Item("knife", 1500, EffectKind.Damage, TargetMode.EnemyFront, 50);
            ItemEffect cut = knife.Effects[0];

            Assert.AreEqual(5, new EquippedItem(knife, powerPercent: 10).Magnitude(balance, cut), "Common: 50 at power 10%.");
            Assert.AreEqual(10, new EquippedItem(knife, tier: ItemTier.Tier1, powerPercent: 10).Magnitude(balance, cut), "200%.");
            Assert.AreEqual(15, new EquippedItem(knife, tier: ItemTier.Tier2, powerPercent: 10).Magnitude(balance, cut), "300%.");
            Assert.AreEqual(22, new EquippedItem(knife, tier: ItemTier.Tier3, powerPercent: 10).Magnitude(balance, cut), "450% of 5 is 22.5, rounded down.");
        }

        [Test]
        public void InBattle_AGoldAttack_HitsThreeTimesAsHardAsABronzeOne()
        {
            BalanceData balance = TestData.Balance();
            ItemData blow = TestData.Item("blow", 1000, EffectKind.Damage, TargetMode.EnemyFront);

            int FirstHit(ItemTier tier)
            {
                var battle = new BattleEngine(TestData.Setup(
                    balance,
                    TestData.Units(TestData.Mercenary("a", 1, 100, new EquippedItem(blow, tier: tier, powerPercent: 7))),
                    TestData.Units(TestData.Enemy("e", 1, 1000))));
                battle.AdvanceTo(1000);
                return battle.Events.Single(e => e.Kind == BattleEventKind.Damaged).A;
            }

            Assert.AreEqual(7, FirstHit(ItemTier.Common));
            Assert.AreEqual(21, FirstHit(ItemTier.Tier2));
        }

        // ---- The loot ------------------------------------------------------------------------

        [Test]
        public void ItemTier_IsTheDeepestTierStartedByTheFloor_OneUpForAnElitesLoot()
        {
            var potions = new List<string>();
            var dungeon = new DungeonData("d", TestData.Text("d"), "swift", 12, 2, 3, 2, potions, tier1Floor: 4, tier2Floor: 8, tier3Floor: 12);

            Assert.AreEqual(ItemTier.Common, dungeon.ItemTierAt(3, elite: false));
            Assert.AreEqual(ItemTier.Tier1, dungeon.ItemTierAt(3, elite: true));
            Assert.AreEqual(ItemTier.Tier1, dungeon.ItemTierAt(4, elite: false));
            Assert.AreEqual(ItemTier.Tier2, dungeon.ItemTierAt(8, elite: false));
            Assert.AreEqual(ItemTier.Tier3, dungeon.ItemTierAt(11, elite: true));
            Assert.AreEqual(ItemTier.Tier3, dungeon.ItemTierAt(12, elite: true), "Gold is the last.");

            var bronze = new DungeonData("e", TestData.Text("e"), "swift", 12, 2, 3, 2, potions);
            Assert.AreEqual(ItemTier.Common, bronze.ItemTierAt(12, elite: false), "No tier floors: Common everywhere.");
            Assert.AreEqual(ItemTier.Tier1, bronze.ItemTierAt(12, elite: true));
        }

        [Test]
        public void TheLootOfABattle_IsOfItsFloorsTier_AndIsTakenAtIt()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData>
            {
                TestData.Pit(),
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, new List<string> { "tonic" }, tier1Floor: 1),
            };
            var data = new StaticData(parts);
            ExpeditionState state = Expedition(data);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            battle.RunToEnd();
            ExpeditionRules.CompleteBattle(data, state, battle);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase);

            Assert.IsTrue(state.Loot.All(d => d.Tier == ItemTier.Tier1), "Bronze from floor 1.");
            ExpeditionRules.TakeLootToInventory(data, state, 0);
            Assert.AreEqual(ItemTier.Tier1, state.Inventory.Single().Tier);
        }

        // ---- Merging (on the grid boards of Slice B stage 19: an item merges into the same item at its placement's top-left square) --

        static Placement At(int x, int y, int turns = 0)
        {
            return new Placement(x, y, turns);
        }

        [Test]
        public void TwoOfTheSameItemAtTheSameTier_MergeIntoOneATierUp_WhenOneIsPutOnTheOther()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            ExpeditionMember ben = state.Members[1];
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Tier1), 2, 0);
            TestBoards.Put(ben, Item(data, "knife", ItemTier.Tier1), 0, 1);

            Assert.IsTrue(ExpeditionRules.CanMoveItem(data, state, 0, 2, 0, 1, At(0, 1)));
            ExpeditionRules.MoveItem(data, state, 0, 2, 0, 1, At(0, 1));

            Assert.AreEqual("blade:Common", Board(anna), "The knife left anna's board.");
            Assert.AreEqual("staff:Common knife:Tier2", Board(ben), "Ben's knife is a tier up, where it was.");
            EquippedItem merged = TestBoards.ItemAt(ben, 0, 1);
            Assert.IsFalse(merged.IsBase);
            Assert.IsEmpty(state.Inventory);
        }

        [Test]
        public void Merging_WorksOnOneBoard_FromTheInventory_AndFromTheLoot_EvenWhereNothingHasRoom()
        {
            StaticData data = TestData.Data(("InventoryWidth", 4), ("InventoryHeight", 3));
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Common), 2, 0);
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Common), 0, 1);

            // On one board: the second knife onto the first.
            ExpeditionRules.MoveItem(data, state, 0, 0, 1, 0, At(2, 0));
            Assert.AreEqual("blade:Common knife:Tier1", Board(anna));

            // From a full inventory onto a full board.
            TestBoards.Put(anna, Item(data, "charm", ItemTier.Common), 0, 1);
            TestBoards.Put(anna, Item(data, "claw", ItemTier.Common), 1, 1);
            TestBoards.Put(anna, Item(data, "claw", ItemTier.Common), 2, 1);
            state.Inventory.Add(new EquippedItem(data.Items.Get("ballista"), powerPercent: 8), At(0, 0));
            state.Inventory.Add(Item(data, "knife", ItemTier.Tier1), At(3, 0));
            state.Inventory.Add(Item(data, "charm", ItemTier.Common), At(3, 1));
            state.Inventory.Add(Item(data, "charm", ItemTier.Common), At(3, 2));
            Assert.AreEqual(12, state.Inventory.UsedSquares, "Full.");
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 1, 0, At(2, 0)));
            ExpeditionRules.PlaceFromInventory(data, state, 1, 0, At(2, 0));
            Assert.AreEqual("blade:Common knife:Tier2 charm:Common claw:Common claw:Common", Board(anna));
            Assert.AreEqual("ballista charm charm", string.Join(" ", state.Inventory.Select(i => i.Item.Id)));

            // From the loot: one drop lying there, so taking it ends the loot.
            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", ItemTier.Tier2));
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, At(2, 0)));
            ExpeditionRules.TakeLoot(data, state, 0, 0, At(2, 0));
            Assert.AreEqual("blade:Common knife:Tier3 charm:Common claw:Common claw:Common", Board(anna));
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsEmpty(state.Loot);
        }

        [Test]
        public void DifferentTiers_DifferentItems_AndAGold_DoNotMerge()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            ExpeditionMember ben = state.Members[1];
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Common), 2, 0);
            TestBoards.Put(ben, Item(data, "knife", ItemTier.Tier1), 0, 1);

            Assert.IsFalse(ExpeditionRules.CanMerge(TestBoards.ItemAt(anna, 2, 0), TestBoards.ItemAt(ben, 0, 1)), "Different tiers.");
            ExpeditionRules.MoveItem(data, state, 0, 2, 0, 1, At(0, 1));
            Assert.AreEqual("blade:Common", Board(anna));
            Assert.AreEqual("staff:Common knife:Common", Board(ben), "It lies over the other knife instead...");
            Assert.AreEqual(ItemTier.Tier1, state.Inventory.Single().Tier, "...which went to the inventory.");

            Assert.IsFalse(ExpeditionRules.CanMerge(Item(data, "knife", ItemTier.Common), Item(data, "charm", ItemTier.Common)), "Different items.");
            Assert.IsFalse(ExpeditionRules.CanMerge(Item(data, "knife", ItemTier.Tier3), Item(data, "knife", ItemTier.Tier3)), "Gold is the last.");
        }

        [Test]
        public void BaseWeapons_Merge_AndTheMergeIsABaseWeapon_ThatCostsNoFatigue()
        {
            // 2026-10-19 (the user): base weapons merge a tier up, with a found one or with each other, and stay base weapons.
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            ExpeditionMember cora = state.Members[2];
            EquippedItem annas = TestBoards.ItemAt(anna, 0, 0);
            EquippedItem coras = TestBoards.ItemAt(cora, 0, 0);
            Assert.IsTrue(annas.IsBase && coras.IsBase && annas.Item == coras.Item && annas.Tier == coras.Tier, "Anna (tank) and cora (striker) both left with a blade.");

            // A found blade onto anna's base blade: a base blade a tier up.
            var found = new EquippedItem(annas.Item);
            Assert.IsTrue(ExpeditionRules.CanMerge(found, annas));
            state.Inventory.Add(found, At(0, 0));
            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(0, 0));
            EquippedItem merged = TestBoards.ItemAt(anna, 0, 0);
            Assert.AreEqual(ItemTier.Tier1, merged.Tier);
            Assert.IsTrue(merged.IsBase, "Still a base weapon.");
            Assert.AreEqual(0, FatigueRules.ItemCost(data.Balance, merged), "A base weapon costs no fatigue at any tier.");

            // Cora's base blade put onto a found one in the inventory: the merge lies there and is a base weapon; cora is left without.
            var bought = new EquippedItem(annas.Item);
            state.Inventory.Add(bought, At(0, 0));
            Assert.IsTrue(ExpeditionRules.CanMoveToInventoryAt(state, 2, 0, 0, At(0, 0)));
            ExpeditionRules.MoveToInventoryAt(state, 2, 0, 0, At(0, 0));
            EquippedItem onFound = state.Inventory.Items.Single().Item;
            Assert.AreEqual(ItemTier.Tier1, onFound.Tier);
            Assert.IsTrue(onFound.IsBase, "The base weapon carries over to the merge.");
            Assert.IsNull(TestBoards.ItemAt(cora, 0, 0));

            // Two base weapons (both 1-star now) merge into one 2-star base weapon on anna's board.
            Assert.IsTrue(ExpeditionRules.CanMerge(onFound, merged));
            ExpeditionRules.PlaceFromInventory(data, state, 0, 0, At(0, 0));
            Assert.AreEqual(ItemTier.Tier2, TestBoards.ItemAt(anna, 0, 0).Tier);
            Assert.IsTrue(TestBoards.ItemAt(anna, 0, 0).IsBase);
            Assert.IsEmpty(state.Inventory);
        }

        [Test]
        public void MergingTwoWeapons_LeavesOneToPayFatigueFor()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Common), 2, 0);
            TestBoards.Put(anna, Item(data, "knife", ItemTier.Common), 0, 1);
            Assert.AreEqual(2 * data.Balance.FatigueEquipment, FatigueRules.EquipmentCost(data.Balance, TestBoards.Items(anna)));

            ExpeditionRules.MoveItem(data, state, 0, 0, 1, 0, At(2, 0));

            Assert.AreEqual(data.Balance.FatigueEquipment, FatigueRules.EquipmentCost(data.Balance, TestBoards.Items(anna)));
        }

        [Test]
        public void AUniqueItem_CostsNoFatigue_EvenWhenNotABaseWeapon()
        {
            // 2026-10-19 (the user): a unique item costs no fatigue, whatever it was forged from; other found weapons still do.
            StaticData data = TestData.Data();
            var unique = new EquippedItem(data.Items.Get("gladius_t"));
            Assert.IsFalse(unique.IsBase);
            Assert.IsTrue(unique.Item.Unique && unique.Item.Category == ItemCategory.Weapon);
            Assert.AreEqual(0, FatigueRules.ItemCost(data.Balance, unique));
            Assert.AreEqual(data.Balance.FatigueEquipment, FatigueRules.ItemCost(data.Balance, Item(data, "knife", ItemTier.Common)));
        }

        [Test]
        public void WhereAnItemWouldMerge_IsAskedPerSquare_AndWhetherAnyBoardHoldsATarget()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember ben = state.Members[1];
            TestBoards.Put(ben, Item(data, "knife", ItemTier.Common), 0, 1);
            EquippedItem knife = Item(data, "knife", ItemTier.Common);
            EquippedItem silverKnife = Item(data, "knife", ItemTier.Tier1);

            Assert.IsTrue(ExpeditionRules.MergesAt(state, knife, 1, 0, 1));
            Assert.IsFalse(ExpeditionRules.MergesAt(state, knife, 1, 0, 0), "The staff.");
            Assert.IsFalse(ExpeditionRules.MergesAt(state, knife, 1, 2, 1), "Nothing there.");
            Assert.IsFalse(ExpeditionRules.MergesAt(state, silverKnife, 1, 0, 1), "Another tier.");
            Assert.IsTrue(ExpeditionRules.HasMergeTarget(state, knife));
            Assert.IsFalse(ExpeditionRules.HasMergeTarget(state, silverKnife));
            Assert.IsFalse(ExpeditionRules.HasMergeTarget(state, TestBoards.ItemAt(ben, 0, 1)), "Not into itself.");

            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", ItemTier.Common));
            state.Loot.Add(new ItemOffer(OfferKind.Potion, "tonic"));
            Assert.IsTrue(ExpeditionRules.LootMergesAt(state, 0, 1, 0, 1));
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 1, 1, 0, 1), "A potion.");
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 0, 0, 0), "The blade.");
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 5, 1, 0, 1), "No such drop.");
        }

        [Test]
        public void TheInventory_MergesToo_FromABoard_InsideItself_AndFromADrop()
        {
            // 2026-10-18 (the user: "인벤창에서도 합치기 가능하게"): the inventory merges as a board does — the top-left on the same item at the same tier.
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember ben = state.Members[1];
            state.Inventory.Add(Item(data, "knife", ItemTier.Common), new Placement(2, 0));
            TestBoards.Put(ben, Item(data, "knife", ItemTier.Common), 0, 1);
            EquippedItem knife = Item(data, "knife", ItemTier.Common);

            Assert.IsTrue(ExpeditionRules.MergesInInventoryAt(state, knife, 2, 0));
            Assert.IsFalse(ExpeditionRules.MergesInInventoryAt(state, knife, 3, 0), "Nothing there.");
            Assert.IsFalse(ExpeditionRules.MergesInInventoryAt(state, Item(data, "knife", ItemTier.Tier1), 2, 0), "Another tier.");
            Assert.IsFalse(ExpeditionRules.MergesInInventoryAt(state, state.Inventory[0], 2, 0), "Not into itself.");
            Assert.IsTrue(ExpeditionRules.HasMergeTarget(state, knife), "The inventory holds a target.");

            // The board's knife laid with its top-left on the inventory's: one Tier1 knife in the inventory where it lay, none on the board.
            Assert.IsTrue(ExpeditionRules.CanMoveToInventoryAt(state, 1, 0, 1, new Placement(2, 0)));
            ExpeditionRules.MoveToInventoryAt(state, 1, 0, 1, new Placement(2, 0));
            Assert.AreEqual(1, state.Inventory.Count);
            Assert.AreEqual(ItemTier.Tier1, state.Inventory[0].Tier);
            Assert.AreEqual(new Placement(2, 0), state.Inventory.Items[0].At);
            Assert.IsNull(TestBoards.ItemAt(ben, 0, 1));

            // Inside the inventory: a second Tier1 knife moved onto the first makes a Tier2 one; an item never merges into itself.
            state.Inventory.Add(Item(data, "knife", ItemTier.Tier1), new Placement(5, 1));
            Assert.IsTrue(ExpeditionRules.CanMoveInInventory(state, 1, new Placement(2, 0)));
            ExpeditionRules.MoveInInventory(state, 1, new Placement(2, 0));
            Assert.AreEqual(1, state.Inventory.Count);
            Assert.AreEqual(ItemTier.Tier2, state.Inventory[0].Tier);
            Assert.IsFalse(ExpeditionRules.CanMoveInInventory(state, 0, new Placement(2, 0)), "Where it lies already.");

            // A drop laid on it: Tier2 and Tier2 make Tier3, which merges no further; over an item it does not merge into, a drop cannot go.
            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", ItemTier.Tier2));
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", ItemTier.Tier2));
            Assert.IsTrue(ExpeditionRules.LootMergesInInventoryAt(state, 0, 2, 0));
            Assert.IsTrue(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 0, new Placement(2, 0)));
            ExpeditionRules.TakeLootToInventoryAt(data, state, 0, new Placement(2, 0));
            Assert.AreEqual(1, state.Inventory.Count);
            Assert.AreEqual(ItemTier.Tier3, state.Inventory[0].Tier);
            Assert.IsNull(state.Loot[0], "Taken off the floor.");
            Assert.IsFalse(ExpeditionRules.LootMergesInInventoryAt(state, 1, 2, 0), "Tier3 merges no further.");
            Assert.IsFalse(ExpeditionRules.CanTakeLootToInventoryAt(data, state, 1, new Placement(2, 0)), "Over an item it does not merge into.");
        }

        // ---- The camp's upkeep ---------------------------------------------------------------

        /// <summary>The test cave whose second floor is all camps; the boss on the third.</summary>
        static StaticData CaveWithACamp()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData>
            {
                TestData.Pit(),
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, new List<string> { "tonic" }, campFloor: 2),
            };
            return new StaticData(parts);
        }

        /// <summary>Staged past the first floor (a test moves the party along directly) and into a camp of the second.</summary>
        static ExpeditionState AtTheCamp(StaticData data)
        {
            ExpeditionState state = Expedition(data);
            state.CurrentNodeId = state.Map.OnFloor(1)[0].Id;
            ExpeditionRules.EnterCamp(state, ExpeditionRules.AvailableNodes(state)[0].Id);
            return state;
        }

        [Test]
        public void TheCampsUpkeep_RaisesAnItemATier_ABaseWeaponToo_ThenThePartyGoesOn()
        {
            StaticData data = CaveWithACamp();
            ExpeditionState state = AtTheCamp(data);
            ExpeditionMember anna = state.Members[0];

            Assert.IsTrue(ExpeditionRules.CanUpgradeAtCamp(state, 0, 1, 0), "By any square of the item.");
            ExpeditionRules.UpgradeAtCamp(state, 0, 1, 0);

            EquippedItem blade = TestBoards.ItemAt(anna, 0, 0);
            Assert.AreEqual(ItemTier.Tier1, blade.Tier);
            Assert.IsTrue(blade.IsBase, "A base weapon stays one.");
            Assert.AreEqual(new Placement(0, 0), anna.Board.Items[0].At, "Where it lay.");
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0, 0), "Once per camp: the party has gone on.");
        }

        [Test]
        public void TheCampsUpkeep_RefusesAGold_AnEmptySquare_TheDead_AndAnywhereButACamp()
        {
            StaticData data = CaveWithACamp();
            ExpeditionState state = AtTheCamp(data);
            TestBoards.Put(state.Members[0], Item(data, "knife", ItemTier.Tier3), 2, 0);
            state.Members[1].Alive = false;
            state.Members[1].Hp = 0;

            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 2, 0), "Gold is the last.");
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0, 1), "Nothing there.");
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 1, 0, 0), "The dead.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.UpgradeAtCamp(state, 0, 2, 0));
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(Expedition(data), 0, 0, 0), "Only at a camp.");
        }
    }
}
