using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The shop and the region coins of Slice B stage 17 (Docs/Design/03_Dungeon_Structure.md §1·§5): shop nodes on the map, the
    /// stock drawn from the seed, prices by tier, buying onto a board or into the inventory or a potion, the refresh whose cost
    /// climbs within a shop, leaving, and the coins a won battle brings.
    /// </summary>
    public sealed class ShopRulesTests
    {
        static readonly PartyMember[] Party =
        {
            new PartyMember("anna", "tank", 1),
            new PartyMember("ben", "healer", 2),
            new PartyMember("cora", "striker", 3),
        };

        /// <summary>
        /// The test data with an eight-floor cave: shops by chance from floor 3, every node of floor 8 a camp, Bronze rewards from
        /// floor 4, the boss on floor 9; groups "pair" (floors 1..8), "trio" (2..8), the elite "guard" (3..8) and the boss "lair".
        /// The reward items and the tonic are priced (TestData); "trinket" is a reward item without a price, which no shop stocks.
        /// </summary>
        static StaticData ShopCave(int shopPercent = 40, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            parts.Items = parts.Items.Concat(new[] { TestData.Item("trinket", 3000, EffectKind.Shield, TargetMode.Self, category: ItemCategory.Other, rewardWeight: 5) }).ToList();
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 8, 2, 4, 3, 8, 1, new List<string> { "tonic" }, null,
                    campFloor: 8, bronzeFloor: 4, shopMinFloor: 3, shopChancePercent: shopPercent),
            };
            parts.EnemyGroups = new List<EnemyGroupData>
            {
                new EnemyGroupData("pair", "cave", 1, 8, false, new[] { "grunt", "grunt" }),
                new EnemyGroupData("trio", "cave", 2, 8, false, new[] { "grunt", "grunt", "grunt" }),
                new EnemyGroupData("guard", "cave", 3, 8, false, new[] { "chief", "chief" }, isElite: true),
                new EnemyGroupData("lair", "cave", 0, 0, true, new[] { "chief", "grunt" }),
            };
            return new StaticData(parts);
        }

        /// <summary>An expedition whose next choice is a shop node: walked along the map directly (seeds tried in turn) until one is offered.</summary>
        static ExpeditionState AtAShopNode(StaticData data, out MapNode shop, int coins = 100)
        {
            for (ulong seed = 1; seed <= 40; seed++)
            {
                ExpeditionState state = ExpeditionRules.Create(data, "cave", seed, Party);
                state.Coins = coins;
                for (int guard = 0; guard < 20; guard++)
                {
                    List<MapNode> next = ExpeditionRules.AvailableNodes(state);
                    if (next.Count == 0)
                    {
                        break;
                    }

                    shop = next.FirstOrDefault(n => n.Kind == MapNodeKind.Shop);
                    if (shop != null)
                    {
                        return state;
                    }

                    state.CurrentNodeId = next[0].Id;
                }
            }

            throw new InvalidOperationException("No shop was reached.");
        }

        static ExpeditionState InAShop(StaticData data, out MapNode shop, int coins = 100)
        {
            ExpeditionState state = AtAShopNode(data, out shop, coins);
            ExpeditionRules.EnterShop(data, state, shop.Id);
            return state;
        }

        static int SlotOf(ExpeditionState state, RewardKind kind, string id = null)
        {
            return state.Shop.Stock.FindIndex(o => o != null && o.Kind == kind && (id == null || o.Id == id));
        }

        // ---- The map and the data ------------------------------------------------------------

        [Test]
        public void Map_ShopsAppearByChanceFromTheirFloor_NeverOnTheFirstOrTheCampFloor_AndHoldNoGroup()
        {
            StaticData data = ShopCave();
            DungeonData dungeon = data.Dungeons.Get("cave");
            int shops = 0, beforeTheCampFloor = 0;

            for (ulong seed = 1; seed <= 300; seed++)
            {
                NodeMap map = MapGenerator.Generate(data, dungeon, seed);
                foreach (MapNode node in map.Nodes.Where(n => n.Kind == MapNodeKind.Shop))
                {
                    shops++;
                    beforeTheCampFloor += node.Floor == 7 ? 1 : 0;
                    Assert.GreaterOrEqual(node.Floor, 3, "Not before the shop's first floor.");
                    Assert.AreNotEqual(8, node.Floor, "Never on the camp floor.");
                    Assert.IsNull(node.EnemyGroupId);
                    Assert.IsFalse(node.IsFought);
                }
            }

            Assert.Greater(shops, 0, "Shops appear by chance.");
            Assert.Greater(beforeTheCampFloor, 0, "A shop may stand just before the camp floor (a camp may not).");
            Assert.IsTrue(dungeon.ShopCanStandOn(7));
            Assert.IsFalse(dungeon.ShopCanStandOn(1) || dungeon.ShopCanStandOn(2) || dungeon.ShopCanStandOn(8) || dungeon.ShopCanStandOn(9));

            StaticData none = ShopCave(shopPercent: 0);
            for (ulong seed = 1; seed <= 50; seed++)
            {
                Assert.IsTrue(MapGenerator.Generate(none, none.Dungeons.Get("cave"), seed).Nodes.All(n => n.Kind != MapNodeKind.Shop), "No chance: no shop.");
            }
        }

        [Test]
        public void Data_RefusesABadShopChance_AndANegativePrice()
        {
            var potions = new List<string>();
            Assert.Throws<DataException>(() => new DungeonData("d", TestData.Text("d"), "swift", 5, 2, 3, 2, 8, 2, potions, null, shopChancePercent: 101));
            Assert.Throws<DataException>(() => new DungeonData("d", TestData.Text("d"), "swift", 5, 2, 3, 2, 8, 2, potions, null, shopMinFloor: -1));
            Assert.Throws<DataException>(() => TestData.Item("x", 1000, EffectKind.Damage, TargetMode.EnemyFront, price: -1));
            Assert.Throws<DataException>(() => new PotionData("p", TestData.Text("p"), PotionEffect.Heal, 10, 1, null, -1));
            Assert.Throws<DataException>(() => TestData.Balance(("ShopSlots", BalanceData.MaxShopSlots + 1)));
            Assert.Throws<DataException>(() => TestData.Balance(("EliteCoinPercent", 99)));
        }

        // ---- Entering and the stock ----------------------------------------------------------

        [Test]
        public void EnteringAShop_DrawsTheStockFromTheSeed_OfThePricedItems_AtTheFloorsTier_AndNoPotionWhileNoSlotIsEmpty()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode shop);

            Assert.AreEqual(ExpeditionPhase.AtShop, state.Phase);
            Assert.AreEqual(shop.Id, state.CurrentNodeId);
            Assert.IsEmpty(ExpeditionRules.AvailableNodes(state), "The party stays until it leaves.");
            Assert.AreEqual(0, state.Shop.Refreshes);
            List<RewardOption> stock = state.Shop.Stock;
            Assert.AreEqual(data.Balance.ShopSlots, stock.Count, "Three priced items and the tonic fill the four slots.");
            CollectionAssert.AreEquivalent(new[] { "charm", "knife", "bow", "tonic" }, stock.Select(o => o.Id), "Every priced item and the potion, once each; never the unpriced trinket.");
            ItemTier tier = data.Dungeons.Get("cave").RewardTierAt(shop.Floor, false);
            Assert.IsTrue(stock.Where(o => o.Kind == RewardKind.Item).All(o => o.Tier == tier && o.Grade == data.Dungeons.Get("cave").RewardGradeAt(shop.Floor)), "The floor's tier and grade, as a reward's.");

            // The same seed draws the same stock in the same order.
            ExpeditionState again = InAShop(data, out MapNode _);
            CollectionAssert.AreEqual(stock.Select(o => o.Id), again.Shop.Stock.Select(o => o.Id));

            // With every potion slot full, no potion is on offer.
            ExpeditionState full = AtAShopNode(data, out MapNode shopNode);
            for (int i = 0; i < full.Potions.Length; i++)
            {
                full.Potions[i] = "tonic";
            }

            ExpeditionRules.EnterShop(data, full, shopNode.Id);
            Assert.IsTrue(full.Shop.Stock.All(o => o.Kind == RewardKind.Item));
            Assert.AreEqual(3, full.Shop.Stock.Count);
        }

        [Test]
        public void Prices_FollowTheTier_AndAPotionsIsItsOwn()
        {
            StaticData data = ShopCave();
            Assert.AreEqual(10, ExpeditionRules.PriceOf(data, new RewardOption(RewardKind.Item, "knife", 8)));
            Assert.AreEqual(20, ExpeditionRules.PriceOf(data, new RewardOption(RewardKind.Item, "knife", 8, ItemTier.Bronze)), "Bronze: twice, as its effects.");
            Assert.AreEqual(40, ExpeditionRules.PriceOf(data, new RewardOption(RewardKind.Item, "knife", 8, ItemTier.Gold)));
            Assert.AreEqual(5, ExpeditionRules.PriceOf(data, new RewardOption(RewardKind.Potion, "tonic", 0)));
        }

        // ---- Buying --------------------------------------------------------------------------

        [Test]
        public void Buying_PaysTheCoins_MarksTheSlotSold_AndPutsTheItemOnTheBoard_OrMergesItIntoTheSameOne()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int slot = SlotOf(state, RewardKind.Item, "knife");
            RewardOption knife = state.Shop.Stock[slot];
            int price = ExpeditionRules.PriceOf(data, knife);
            ExpeditionMember anna = state.Members[0];

            Assert.IsTrue(ExpeditionRules.CanAfford(data, state, slot));
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, 1), "Onto the free cell behind the blade.");
            ExpeditionRules.BuyToBoard(data, state, slot, 0, 1);

            Assert.AreEqual(100 - price, state.Coins);
            Assert.IsNull(state.Shop.Stock[slot], "Sold.");
            Assert.AreEqual(2, anna.Items.Count);
            Assert.AreEqual("knife", anna.Items[1].Item.Id);
            Assert.AreEqual(knife.Tier, anna.Items[1].Tier);
            Assert.IsFalse(anna.Items[1].IsBase);
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, 2), "A sold slot cannot be bought again.");
            Assert.IsNull(ExpeditionRules.OfferAt(state, slot));

            // The same knife on offer again (a refresh draws it anew) merges into the one on the board, a tier up, when bought onto it.
            ExpeditionRules.RefreshShop(data, state);
            int again = SlotOf(state, RewardKind.Item, "knife");
            Assume.That(again, Is.GreaterThanOrEqualTo(0), "The knife is on offer again.");
            Assert.IsTrue(ExpeditionRules.ShopMergesAt(state, again, 0, 1));
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, again, 0, 1));
            int before = state.Coins;
            ExpeditionRules.BuyToBoard(data, state, again, 0, 1);
            Assert.AreEqual(2, anna.Items.Count, "Merged: still one knife.");
            Assert.AreEqual(knife.Tier + 1, anna.Items[1].Tier);
            Assert.AreEqual(before - price, state.Coins);
        }

        [Test]
        public void Buying_IntoTheInventory_AndAPotion_WhileTheyHaveRoom()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int bow = SlotOf(state, RewardKind.Item, "bow");
            int tonic = SlotOf(state, RewardKind.Potion);
            int coins = state.Coins;

            Assert.IsTrue(ExpeditionRules.CanBuyToInventory(data, state, bow));
            ExpeditionRules.BuyToInventory(data, state, bow);
            Assert.AreEqual("bow", state.Inventory.Single().Item.Id);
            Assert.AreEqual(coins - ExpeditionRules.PriceOf(data, new RewardOption(RewardKind.Item, "bow", 8, state.Inventory[0].Tier)), state.Coins);
            Assert.IsNull(state.Shop.Stock[bow]);

            coins = state.Coins;
            Assert.IsTrue(ExpeditionRules.CanBuyPotion(data, state, tonic));
            ExpeditionRules.BuyPotion(data, state, tonic);
            Assert.AreEqual(2, state.Potions.Count(p => p != null), "The starting tonic and the bought one.");
            Assert.AreEqual(coins - 5, state.Coins);
            Assert.IsNull(state.Shop.Stock[tonic]);

            // No room: every potion slot taken before the stock is drawn (so no potion is on offer), the inventory full of what is left.
            ExpeditionState cramped = AtAShopNode(data, out MapNode crampedShop, coins: 100);
            for (int i = 0; i < cramped.Potions.Length; i++)
            {
                cramped.Potions[i] = "tonic";
            }

            while (ExpeditionRules.FreeInventoryCells(data, cramped) > 0)
            {
                cramped.Inventory.Add(new EquippedItem(data.Items.Get("knife"), 8));
            }

            ExpeditionRules.EnterShop(data, cramped, crampedShop.Id);
            Assert.IsTrue(cramped.Shop.Stock.All(o => o.Kind == RewardKind.Item), "Drawn with the potion slots full.");
            Assert.IsFalse(ExpeditionRules.CanBuyToInventory(data, cramped, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyToInventory(data, cramped, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyPotion(data, cramped, 0), "Not a potion.");
        }

        [Test]
        public void Buying_IsRefused_WithoutTheCoins_ForASoldSlot_AndOutsideTheShop()
        {
            StaticData data = ShopCave();
            ExpeditionState poor = InAShop(data, out MapNode _, coins: 4);
            int slot = SlotOf(poor, RewardKind.Item);
            Assert.IsFalse(ExpeditionRules.CanAfford(data, poor, slot));
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, poor, slot, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanBuyToInventory(data, poor, slot));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyToBoard(data, poor, slot, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanBuyPotion(data, poor, SlotOf(poor, RewardKind.Potion)), "A coin short of a tonic.");
            Assert.IsTrue(ExpeditionRules.CanRefreshShop(data, poor), "Four coins cover a first refresh of three.");
            ExpeditionRules.RefreshShop(data, poor);
            Assert.AreEqual(1, poor.Coins);
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, poor), "One coin covers nothing.");

            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            ExpeditionRules.LeaveShop(state);
            Assert.IsNull(ExpeditionRules.OfferAt(state, 0));
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, 0, 0, 1));
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshShop(data, state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.LeaveShop(state), "Once.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshCost(data, state));
        }

        // ---- The refresh and leaving ---------------------------------------------------------

        [Test]
        public void Refreshing_CostsTheBaseThenMoreEachTime_DrawsEverySlotAnew_AndANewShopStartsOver()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 18);
            BalanceData balance = data.Balance;
            ExpeditionRules.BuyPotion(data, state, SlotOf(state, RewardKind.Potion));
            Assert.AreEqual(13, state.Coins);
            Assert.AreEqual(balance.ShopRefreshBase, ExpeditionRules.RefreshCost(data, state));

            Assert.IsTrue(ExpeditionRules.CanRefreshShop(data, state));
            ExpeditionRules.RefreshShop(data, state);
            Assert.AreEqual(13 - balance.ShopRefreshBase, state.Coins);
            Assert.AreEqual(1, state.Shop.Refreshes);
            Assert.AreEqual(balance.ShopRefreshBase + balance.ShopRefreshStep, ExpeditionRules.RefreshCost(data, state), "The next one costs more.");
            Assert.AreEqual(balance.ShopSlots, state.Shop.Stock.Count, "Every slot is filled again, the sold one too.");
            Assert.IsTrue(state.Shop.Stock.All(o => o != null));

            ExpeditionRules.RefreshShop(data, state);
            Assert.AreEqual(13 - 2 * balance.ShopRefreshBase - balance.ShopRefreshStep, state.Coins);
            Assert.AreEqual(balance.ShopRefreshBase + 2 * balance.ShopRefreshStep, ExpeditionRules.RefreshCost(data, state));
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, state), "Five coins left, seven wanted.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshShop(data, state));

            // The same shop refreshed the same number of times from the same seed shows the same stock.
            ExpeditionState twin = InAShop(data, out MapNode _, coins: 18);
            ExpeditionRules.BuyPotion(data, twin, SlotOf(twin, RewardKind.Potion));
            ExpeditionRules.RefreshShop(data, twin);
            ExpeditionRules.RefreshShop(data, twin);
            CollectionAssert.AreEqual(state.Shop.Stock.Select(o => o.Id), twin.Shop.Stock.Select(o => o.Id));

            // Leaving, then another shop further on: its refresh starts at the base again.
            ExpeditionRules.LeaveShop(state);
            state.Coins = 100;
            for (int guard = 0; guard < 20; guard++)
            {
                List<MapNode> next = ExpeditionRules.AvailableNodes(state);
                MapNode another = next.FirstOrDefault(n => n.Kind == MapNodeKind.Shop);
                if (another != null)
                {
                    ExpeditionRules.EnterShop(data, state, another.Id);
                    Assert.AreEqual(balance.ShopRefreshBase, ExpeditionRules.RefreshCost(data, state), "Each shop starts over.");
                    return;
                }

                if (next.Count == 0)
                {
                    break;
                }

                state.CurrentNodeId = next[0].Id;
            }

            Assert.Inconclusive("No second shop on this path.");
        }

        [Test]
        public void Leaving_DropsTheStock_AndThePartyGoesOnFromTheShop()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode shop);

            ExpeditionRules.LeaveShop(state);

            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase);
            Assert.IsNull(state.Shop);
            CollectionAssert.AreEqual(shop.NextNodeIds, ExpeditionRules.AvailableNodes(state).Select(n => n.Id), "On from the shop.");
        }

        [Test]
        public void AtTheShop_TheBoardsAndRowsCanBeRearranged_ButNothingIsFoughtThere()
        {
            StaticData data = ShopCave();
            ExpeditionState state = AtAShopNode(data, out MapNode shop);

            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BeginBattle(data, state, shop.Id), "Nobody is fought at a shop.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.EnterCamp(state, shop.Id), "Nor is it a camp.");
            ExpeditionRules.EnterShop(data, state, shop.Id);
            Assert.IsTrue(ExpeditionRules.CanMoveToRow(state, 0, 2));
            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.EnterShop(data, state, shop.Id), "Once.");

            MapNode battle = ExpeditionRules.AvailableNodes(ExpeditionRules.Create(data, "cave", 1, Party))[0];
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.EnterShop(data, ExpeditionRules.Create(data, "cave", 1, Party), battle.Id), "Only a shop node.");
        }

        // ---- The coins -----------------------------------------------------------------------

        [Test]
        public void Coins_ComeFromAWonBattle_PerEnemyAndPerFloor_DoubleForAnElite_NoneForTheBossOrARestingNode()
        {
            StaticData data = ShopCave();
            var none = new List<int>();
            Assert.AreEqual(4, ExpeditionRules.CoinsFor(data, new MapNode(0, 1, 0, MapNodeKind.Battle, "pair", none)), "Two enemies, the first floor.");
            Assert.AreEqual(10, ExpeditionRules.CoinsFor(data, new MapNode(0, 5, 0, MapNodeKind.Battle, "trio", none)), "Three enemies and four floors down.");
            Assert.AreEqual(16, ExpeditionRules.CoinsFor(data, new MapNode(0, 5, 0, MapNodeKind.Elite, "guard", none)), "An elite: twice (2 x 2 + 4) x 2.");
            Assert.AreEqual(0, ExpeditionRules.CoinsFor(data, new MapNode(0, 9, 0, MapNodeKind.Boss, "lair", none)));
            Assert.AreEqual(0, ExpeditionRules.CoinsFor(data, new MapNode(0, 5, 0, MapNodeKind.Camp, null, none)));
            Assert.AreEqual(0, ExpeditionRules.CoinsFor(data, new MapNode(0, 5, 0, MapNodeKind.Shop, null, none)));

            // A battle won on the first floor brings its coins to the expedition; a lost one brings none.
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);
            foreach (ExpeditionMember member in state.Members)
            {
                member.Items[0] = new EquippedItem(member.Items[0].Item, 999, isBase: true);
            }

            MapNode first = ExpeditionRules.AvailableNodes(state)[0];
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, first.Id));
            while (battle.Result == BattleResult.Ongoing)
            {
                battle.AdvanceTo(battle.NextAutomaticEventMs());
            }

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            ExpeditionRules.CompleteBattle(data, state, battle);
            Assert.AreEqual(ExpeditionRules.CoinsFor(data, first), state.Coins);
            Assert.AreEqual(2 * data.Balance.CoinsPerEnemy, state.Coins, "Two grunts on the first floor.");
        }
    }
}
