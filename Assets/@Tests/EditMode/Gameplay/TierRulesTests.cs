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

        static EquippedItem Item(StaticData data, string id, ItemTier tier, int grade = 8)
        {
            return new EquippedItem(data.Items.Get(id), grade, tier: tier);
        }

        /// <summary>A board as "blade:Common knife:Bronze".</summary>
        static string Board(ExpeditionMember member)
        {
            return string.Join(" ", member.Items.Select(i => $"{i.Item.Id}:{i.Tier}"));
        }

        // ---- Data ----------------------------------------------------------------------------

        [Test]
        public void Balance_RefusesTiersThatDoNotGrow()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("TierBronzePercent", 100)), "Bronze is not above Common.");
            Assert.Throws<DataException>(() => TestData.Balance(("TierSilverPercent", 200)), "Silver is not above Bronze.");
            Assert.Throws<DataException>(() => TestData.Balance(("TierGoldPercent", 250)), "Gold is under Silver.");
            Assert.DoesNotThrow(() => TestData.Balance(("TierBronzePercent", 150), ("TierSilverPercent", 200), ("TierGoldPercent", 250)));
        }

        [Test]
        public void Dungeon_RefusesTierFloorsOutOfOrder_OrOffTheMap()
        {
            var potions = new List<string>();
            DungeonData Make(int bronze, int silver, int gold)
            {
                return new DungeonData("d", TestData.Text("d"), "swift", 10, 2, 3, 2, 8, 0, potions, bronzeFloor: bronze, silverFloor: silver, goldFloor: gold);
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
            BalanceData balance = TestData.Balance(("TierGoldPercent", 450));
            ItemData knife = TestData.Item("knife", 1500, EffectKind.Damage, TargetMode.EnemyFront, 50);
            ItemEffect cut = knife.Effects[0];

            Assert.AreEqual(5, new EquippedItem(knife, 10).Magnitude(balance, cut), "Common: grade 10 x 50%.");
            Assert.AreEqual(10, new EquippedItem(knife, 10, tier: ItemTier.Bronze).Magnitude(balance, cut), "200%.");
            Assert.AreEqual(15, new EquippedItem(knife, 10, tier: ItemTier.Silver).Magnitude(balance, cut), "300%.");
            Assert.AreEqual(22, new EquippedItem(knife, 10, tier: ItemTier.Gold).Magnitude(balance, cut), "450% of 5 is 22.5, rounded down.");
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
                    TestData.Units(TestData.Mercenary("a", 1, 100, new EquippedItem(blow, 7, tier: tier))),
                    TestData.Units(TestData.Enemy("e", 1, 1000))));
                battle.AdvanceTo(1000);
                return battle.Events.Single(e => e.Kind == BattleEventKind.Damaged).A;
            }

            Assert.AreEqual(7, FirstHit(ItemTier.Common));
            Assert.AreEqual(21, FirstHit(ItemTier.Silver));
        }

        // ---- The loot ------------------------------------------------------------------------

        [Test]
        public void ItemTier_IsTheDeepestTierStartedByTheFloor_OneUpForAnElitesLoot()
        {
            var potions = new List<string>();
            var dungeon = new DungeonData("d", TestData.Text("d"), "swift", 12, 2, 3, 2, 8, 0, potions, bronzeFloor: 4, silverFloor: 8, goldFloor: 12);

            Assert.AreEqual(ItemTier.Common, dungeon.ItemTierAt(3, elite: false));
            Assert.AreEqual(ItemTier.Bronze, dungeon.ItemTierAt(3, elite: true));
            Assert.AreEqual(ItemTier.Bronze, dungeon.ItemTierAt(4, elite: false));
            Assert.AreEqual(ItemTier.Silver, dungeon.ItemTierAt(8, elite: false));
            Assert.AreEqual(ItemTier.Gold, dungeon.ItemTierAt(11, elite: true));
            Assert.AreEqual(ItemTier.Gold, dungeon.ItemTierAt(12, elite: true), "Gold is the last.");

            var bronze = new DungeonData("e", TestData.Text("e"), "swift", 12, 2, 3, 2, 8, 0, potions);
            Assert.AreEqual(ItemTier.Common, bronze.ItemTierAt(12, elite: false), "No tier floors: Common everywhere.");
            Assert.AreEqual(ItemTier.Bronze, bronze.ItemTierAt(12, elite: true));
        }

        [Test]
        public void TheLootOfABattle_IsOfItsFloorsTier_AndIsTakenAtIt()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }, bronzeFloor: 1),
            };
            var data = new StaticData(parts);
            ExpeditionState state = Expedition(data);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            battle.RunToEnd();
            ExpeditionRules.CompleteBattle(data, state, battle);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase);

            Assert.IsTrue(state.Loot.All(d => d.Tier == ItemTier.Bronze), "Bronze from floor 1.");
            ExpeditionRules.TakeLootToInventory(data, state, 0);
            Assert.AreEqual(ItemTier.Bronze, state.Inventory.Single().Tier);
        }

        // ---- Merging -------------------------------------------------------------------------

        [Test]
        public void TwoOfTheSameItemAtTheSameTier_MergeIntoOneATierUp_WhenOneIsPutOnTheOther()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            ExpeditionMember ben = state.Members[1];
            anna.Items.Add(Item(data, "knife", ItemTier.Bronze, grade: 8));
            ben.Items.Add(Item(data, "knife", ItemTier.Bronze, grade: 9));

            Assert.IsTrue(ExpeditionRules.CanMoveItem(state, 0, 1, 1, 1));
            ExpeditionRules.MoveItem(state, 0, 1, 1, 1);

            Assert.AreEqual("blade:Common", Board(anna), "The knife left anna's board.");
            Assert.AreEqual("staff:Common knife:Silver", Board(ben), "Ben's knife is a tier up, where it was.");
            Assert.AreEqual(9, ben.Items[1].Grade, "At the better grade of the two.");
            Assert.IsFalse(ben.Items[1].IsBase);
        }

        [Test]
        public void Merging_WorksOnOneBoard_FromTheInventory_AndFromTheLoot_EvenWhereNothingHasRoom()
        {
            StaticData data = TestData.Data(("InventoryCells", 4));
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            anna.Items.Add(Item(data, "knife", ItemTier.Common));
            anna.Items.Add(Item(data, "knife", ItemTier.Common));

            // On one board: the second knife onto the first.
            ExpeditionRules.MoveItem(state, 0, 2, 0, 1);
            Assert.AreEqual("blade:Common knife:Bronze", Board(anna));

            // From a full inventory onto a full board.
            anna.Items.Add(Item(data, "charm", ItemTier.Common));
            state.Inventory.Add(new EquippedItem(data.Items.Get("ballista"), 8));
            state.Inventory.Add(Item(data, "knife", ItemTier.Bronze));
            Assert.AreEqual(0, ExpeditionRules.FreeInventoryCells(data, state));
            Assert.IsTrue(ExpeditionRules.CanPlaceFromInventory(data, state, 1, 0, 1));
            ExpeditionRules.PlaceFromInventory(data, state, 1, 0, 1);
            Assert.AreEqual("blade:Common knife:Silver charm:Common", Board(anna));
            Assert.AreEqual("ballista", state.Inventory.Single().Item.Id);

            // From the loot: one drop lying there, so taking it ends the loot.
            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", 8, ItemTier.Silver));
            Assert.IsTrue(ExpeditionRules.CanTakeLoot(data, state, 0, 0, 1));
            ExpeditionRules.TakeLoot(data, state, 0, 0, 1);
            Assert.AreEqual("blade:Common knife:Gold charm:Common", Board(anna));
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsEmpty(state.Loot);
        }

        [Test]
        public void DifferentTiers_DifferentItems_AGold_AndBaseWeapons_DoNotMerge()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            ExpeditionMember ben = state.Members[1];
            anna.Items.Add(Item(data, "knife", ItemTier.Common));
            ben.Items.Add(Item(data, "knife", ItemTier.Bronze));

            Assert.IsFalse(ExpeditionRules.CanMerge(anna.Items[1], ben.Items[1]), "Different tiers.");
            ExpeditionRules.MoveItem(state, 0, 1, 1, 1);
            Assert.AreEqual("blade:Common knife:Bronze", Board(anna), "They trade places instead.");
            Assert.AreEqual("staff:Common knife:Common", Board(ben));

            Assert.IsFalse(ExpeditionRules.CanMerge(Item(data, "knife", ItemTier.Common), Item(data, "charm", ItemTier.Common)), "Different items.");
            Assert.IsFalse(ExpeditionRules.CanMerge(Item(data, "knife", ItemTier.Gold), Item(data, "knife", ItemTier.Gold)), "Gold is the last.");

            // Anna (tank) and cora (striker) both left with a blade.
            EquippedItem annas = anna.Items[0];
            EquippedItem coras = state.Members[2].Items[0];
            Assert.IsTrue(annas.IsBase && coras.IsBase && annas.Tier == coras.Tier);
            coras = new EquippedItem(coras.Item, annas.Grade, isBase: true);
            Assert.IsFalse(ExpeditionRules.CanMerge(coras, annas), "Base weapons do not merge.");
            Assert.IsFalse(ExpeditionRules.CanMerge(new EquippedItem(annas.Item, annas.Grade), annas), "Not even with a found one.");
        }

        [Test]
        public void MergingTwoWeapons_LeavesOneToPayFatigueFor()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember anna = state.Members[0];
            anna.Items.Add(Item(data, "knife", ItemTier.Common));
            anna.Items.Add(Item(data, "knife", ItemTier.Common));
            Assert.AreEqual(2 * data.Balance.FatigueEquipment, FatigueRules.EquipmentCost(data.Balance, anna.Items));

            ExpeditionRules.MoveItem(state, 0, 2, 0, 1);

            Assert.AreEqual(data.Balance.FatigueEquipment, FatigueRules.EquipmentCost(data.Balance, anna.Items));
        }

        [Test]
        public void WhereAnItemWouldMerge_IsAskedPerCell_AndWhetherAnyBoardHoldsATarget()
        {
            StaticData data = TestData.Data();
            ExpeditionState state = Expedition(data);
            ExpeditionMember ben = state.Members[1];
            ben.Items.Add(Item(data, "knife", ItemTier.Common));
            EquippedItem knife = Item(data, "knife", ItemTier.Common);
            EquippedItem silverKnife = Item(data, "knife", ItemTier.Bronze);

            Assert.IsTrue(ExpeditionRules.MergesAt(state, knife, 1, 1));
            Assert.IsFalse(ExpeditionRules.MergesAt(state, knife, 1, 0), "The staff.");
            Assert.IsFalse(ExpeditionRules.MergesAt(state, knife, 1, 2), "Nothing there.");
            Assert.IsFalse(ExpeditionRules.MergesAt(state, silverKnife, 1, 1), "Another tier.");
            Assert.IsTrue(ExpeditionRules.HasMergeTarget(state, knife));
            Assert.IsFalse(ExpeditionRules.HasMergeTarget(state, silverKnife));
            Assert.IsFalse(ExpeditionRules.HasMergeTarget(state, ben.Items[1]), "Not into itself.");

            state.Phase = ExpeditionPhase.PickingLoot;
            state.Loot.Add(new ItemOffer(OfferKind.Item, "knife", 8, ItemTier.Common));
            state.Loot.Add(new ItemOffer(OfferKind.Potion, "tonic", 0));
            Assert.IsTrue(ExpeditionRules.LootMergesAt(state, 0, 1, 1));
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 1, 1, 1), "A potion.");
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 0, 0, 0), "The blade.");
            Assert.IsFalse(ExpeditionRules.LootMergesAt(state, 5, 1, 1), "No such drop.");
        }

        // ---- The camp's upkeep ---------------------------------------------------------------

        /// <summary>The test cave whose second floor is all camps; the boss on the third.</summary>
        static StaticData CaveWithACamp()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }, campFloor: 2),
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

            Assert.IsTrue(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0));
            ExpeditionRules.UpgradeAtCamp(state, 0, 0);

            Assert.AreEqual(ItemTier.Bronze, anna.Items[0].Tier);
            Assert.IsTrue(anna.Items[0].IsBase, "A base weapon stays one.");
            Assert.AreEqual(10, anna.Items[0].Grade);
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0), "Once per camp: the party has gone on.");
        }

        [Test]
        public void TheCampsUpkeep_RefusesAGold_AnEmptyCell_TheDead_AndAnywhereButACamp()
        {
            StaticData data = CaveWithACamp();
            ExpeditionState state = AtTheCamp(data);
            state.Members[0].Items.Add(Item(data, "knife", ItemTier.Gold));
            state.Members[1].Alive = false;
            state.Members[1].Hp = 0;

            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 1), "Gold is the last.");
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 2), "Nothing there.");
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 1, 0), "The dead.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.UpgradeAtCamp(state, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(Expedition(data), 0, 0), "Only at a camp.");
        }
    }
}
