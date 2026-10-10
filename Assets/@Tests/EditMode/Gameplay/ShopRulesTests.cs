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
        /// The test pouch (a bag, Slice B stage 19) is on sale only when asked for, so that the other tests know the stock.
        /// </summary>
        static StaticData ShopCave(int shopPercent = 40, bool bagsOnSale = false, params (string Key, int Value)[] balance)
        {
            StaticDataParts parts = TestData.Parts(balance);
            if (!bagsOnSale)
            {
                parts.Bags = parts.Bags.Select(b => new BagData(b.Id, b.Name, b.Width, b.Height, b.Start, b.Price, 0, b.LootWeight)).ToList();
            }

            parts.Items = parts.Items.Concat(new[] { TestData.Item("trinket", 3000, EffectKind.Shield, TargetMode.Self, category: ItemCategory.Other, shopWeight: 5) }).ToList();
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

        static int SlotOf(ExpeditionState state, OfferKind kind, string id = null)
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
        public void EnteringAShop_DrawsTheStockFromTheSeed_OfThePricedItems_AtTheFloorsTier_ThenItsPotions()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode shop);

            Assert.AreEqual(ExpeditionPhase.AtShop, state.Phase);
            Assert.AreEqual(shop.Id, state.CurrentNodeId);
            Assert.IsEmpty(ExpeditionRules.AvailableNodes(state), "The party stays until it leaves.");
            Assert.AreEqual(0, state.Shop.Refreshes);
            List<ItemOffer> stock = state.Shop.Stock;
            Assert.AreEqual(3, ExpeditionRules.ShopGoodsCount(data), "Three priced items for four slots: the goods are three.");
            Assert.AreEqual(4, stock.Count, "The three goods, then the potion (round 56: drawn apart, at least one).");
            CollectionAssert.AreEquivalent(new[] { "charm", "knife", "bow" }, stock.Take(3).Select(o => o.Id), "Every priced item once; never the unpriced trinket.");
            Assert.AreEqual("tonic", stock[3].Id, "The potion after the goods.");
            ItemTier tier = data.Dungeons.Get("cave").ItemTierAt(shop.Floor, false);
            Assert.IsTrue(stock.Where(o => o.Kind == OfferKind.Item).All(o => o.Tier == tier && o.Grade == data.Dungeons.Get("cave").ItemGradeAt(shop.Floor)), "The floor's tier and grade, as a reward's.");

            // The same seed draws the same stock in the same order.
            ExpeditionState again = InAShop(data, out MapNode _);
            CollectionAssert.AreEqual(stock.Select(o => o.Id), again.Shop.Stock.Select(o => o.Id));

            // With every potion slot full, the potion is on offer still (round 56: at least one at every shop), and cannot be bought.
            ExpeditionState full = AtAShopNode(data, out MapNode shopNode);
            for (int i = 0; i < full.Potions.Length; i++)
            {
                full.Potions[i] = "tonic";
            }

            ExpeditionRules.EnterShop(data, full, shopNode.Id);
            Assert.AreEqual(4, full.Shop.Stock.Count);
            Assert.AreEqual(OfferKind.Potion, full.Shop.Stock[3].Kind);
            Assert.IsFalse(ExpeditionRules.CanBuyPotion(data, full, 3), "No empty slot.");
        }

        [Test]
        public void EveryShop_HasAPotion_ByItsChance_OrElseOneByItsWeight_AndARefreshKeepsThePotions()
        {
            // Round 56: each potion comes by ShopPotionChancePercent; when none does, one does by weight, so a shop always has one.
            foreach (int chance in new[] { 0, 100 })
            {
                StaticData data = ShopCave(balance: ("ShopPotionChancePercent", chance));
                ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
                Assert.AreEqual(1, state.Shop.Stock.Count(o => o.Kind == OfferKind.Potion), $"At {chance}%: the one potion the cave stocks.");
                Assert.AreEqual(OfferKind.Potion, state.Shop.Stock.Last().Kind, "After the goods.");

                string potion = state.Shop.Stock.Last().Id;
                ExpeditionRules.RefreshShop(data, state);
                Assert.AreEqual(potion, state.Shop.Stock.Last()?.Id, "A refresh draws the goods anew and keeps the potions.");
            }
        }

        [Test]
        public void Prices_FollowTheTier_AndAPotionsIsItsOwn()
        {
            StaticData data = ShopCave();
            Assert.AreEqual(10, ExpeditionRules.PriceOf(data, new ItemOffer(OfferKind.Item, "knife", 8)));
            Assert.AreEqual(20, ExpeditionRules.PriceOf(data, new ItemOffer(OfferKind.Item, "knife", 8, ItemTier.Bronze)), "Bronze: twice, as its effects.");
            Assert.AreEqual(40, ExpeditionRules.PriceOf(data, new ItemOffer(OfferKind.Item, "knife", 8, ItemTier.Gold)));
            Assert.AreEqual(5, ExpeditionRules.PriceOf(data, new ItemOffer(OfferKind.Potion, "tonic", 0)));
        }

        // ---- Buying --------------------------------------------------------------------------

        [Test]
        public void Buying_PaysTheCoins_MarksTheSlotSold_AndPutsTheItemOnTheBoard_OrMergesItIntoTheSameOne()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int slot = SlotOf(state, OfferKind.Item, "knife");
            ItemOffer knife = state.Shop.Stock[slot];
            int price = ExpeditionRules.PriceOf(data, knife);
            ExpeditionMember anna = state.Members[0];

            Assert.IsTrue(ExpeditionRules.CanAfford(data, state, slot));
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(2, 0)), "Onto the free square behind the blade.");
            ExpeditionRules.BuyToBoard(data, state, slot, 0, TestBoards.At(2, 0));

            Assert.AreEqual(100 - price, state.Coins);
            Assert.IsNull(state.Shop.Stock[slot], "Sold.");
            Assert.AreEqual(2, anna.Board.Items.Count);
            EquippedItem bought = TestBoards.ItemAt(anna, 2, 0);
            Assert.AreEqual("knife", bought.Item.Id);
            Assert.AreEqual(knife.Tier, bought.Tier);
            Assert.IsFalse(bought.IsBase);
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(0, 1)), "A sold slot cannot be bought again.");
            Assert.IsNull(ExpeditionRules.OfferAt(state, slot));

            // The same knife on offer again (a refresh draws it anew) merges into the one on the board, a tier up, when bought onto it.
            ExpeditionRules.RefreshShop(data, state);
            int again = SlotOf(state, OfferKind.Item, "knife");
            Assume.That(again, Is.GreaterThanOrEqualTo(0), "The knife is on offer again.");
            Assert.IsTrue(ExpeditionRules.ShopMergesAt(state, again, 0, 2, 0));
            Assert.IsFalse(ExpeditionRules.ShopMergesAt(state, again, 0, 0, 0), "The blade.");
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, again, 0, TestBoards.At(2, 0)));
            int before = state.Coins;
            ExpeditionRules.BuyToBoard(data, state, again, 0, TestBoards.At(2, 0));
            Assert.AreEqual(2, anna.Board.Items.Count, "Merged: still one knife.");
            Assert.AreEqual(knife.Tier + 1, TestBoards.ItemAt(anna, 2, 0).Tier);
            Assert.AreEqual(before - price, state.Coins);
        }

        [Test]
        public void Buying_IntoTheInventory_AndAPotion_WhileTheyHaveRoom()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int bow = SlotOf(state, OfferKind.Item, "bow");
            int tonic = SlotOf(state, OfferKind.Potion);
            int coins = state.Coins;

            Assert.IsTrue(ExpeditionRules.CanBuyToInventory(data, state, bow));
            ExpeditionRules.BuyToInventory(data, state, bow);
            Assert.AreEqual("bow", state.Inventory.Single().Item.Id);
            Assert.AreEqual(coins - ExpeditionRules.PriceOf(data, new ItemOffer(OfferKind.Item, "bow", 8, state.Inventory[0].Tier)), state.Coins);
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

            while (cramped.Inventory.HasRoomFor(data.Items.Get("knife")))
            {
                cramped.Inventory.Add(new EquippedItem(data.Items.Get("knife"), 8));
            }

            ExpeditionRules.EnterShop(data, cramped, crampedShop.Id);
            Assert.IsFalse(ExpeditionRules.CanBuyPotion(data, cramped, SlotOf(cramped, OfferKind.Potion)), "On offer with the potion slots full (round 56), but not to be bought.");
            Assert.IsFalse(ExpeditionRules.CanBuyToInventory(data, cramped, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyToInventory(data, cramped, 0));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyPotion(data, cramped, 0), "Not a potion.");
        }

        [Test]
        public void AnOffer_CanBeBoughtOntoFreeSquaresOfTheInventory_ForItsPrice()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int bow = SlotOf(state, OfferKind.Item, "bow");
            ItemOffer offer = state.Shop.Stock[bow];
            ItemData item = data.Items.Get("bow");
            state.Inventory.Add(new EquippedItem(data.Items.Get("knife"), 8), TestBoards.At(0, 0));
            int coins = state.Coins;

            // Round 55: an offer held over the inventory's grid is bought where it is laid, over no item.
            Assert.IsFalse(ExpeditionRules.CanBuyToInventoryAt(data, state, bow, TestBoards.At(0, 0)), "Over the knife.");
            Placement free = TestBoards.At(state.Inventory.Width - item.Width, 0);
            Assert.IsTrue(ExpeditionRules.CanBuyToInventoryAt(data, state, bow, free));
            ExpeditionRules.BuyToInventoryAt(data, state, bow, free);
            Assert.AreEqual(free, state.Inventory.Items.Single(i => i.Item.Item.Id == "bow").At);
            Assert.AreEqual(coins - ExpeditionRules.PriceOf(data, offer), state.Coins);
            Assert.IsNull(state.Shop.Stock[bow]);
            Assert.IsFalse(ExpeditionRules.CanBuyToInventoryAt(data, state, SlotOf(state, OfferKind.Potion), TestBoards.At(0, 2)), "A potion goes to a potion slot.");

            ExpeditionState poor = InAShop(data, out MapNode _, coins: 4);
            Assert.IsFalse(ExpeditionRules.CanBuyToInventoryAt(data, poor, SlotOf(poor, OfferKind.Item), TestBoards.At(0, 2)), "The coins do not cover it.");
        }

        [Test]
        public void Buying_IsRefused_WithoutTheCoins_ForASoldSlot_AndOutsideTheShop()
        {
            StaticData data = ShopCave();
            ExpeditionState poor = InAShop(data, out MapNode _, coins: 4);
            int slot = SlotOf(poor, OfferKind.Item);
            Assert.IsFalse(ExpeditionRules.CanAfford(data, poor, slot));
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, poor, slot, 0, TestBoards.At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanBuyToInventory(data, poor, slot));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.BuyToBoard(data, poor, slot, 0, TestBoards.At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanBuyPotion(data, poor, SlotOf(poor, OfferKind.Potion)), "A coin short of a tonic.");
            Assert.IsTrue(ExpeditionRules.CanRefreshShop(data, poor), "Four coins cover a first refresh of three.");
            ExpeditionRules.RefreshShop(data, poor);
            Assert.AreEqual(1, poor.Coins);
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, poor), "One coin covers nothing.");

            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            ExpeditionRules.LeaveShop(state);
            Assert.IsNull(ExpeditionRules.OfferAt(state, 0));
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, 0, 0, TestBoards.At(2, 0)));
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshShop(data, state));
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.LeaveShop(state), "Once.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshCost(data, state));
        }

        [Test]
        public void ABag_IsOnSale_AndIsBoughtIntoTheFrameOverNoBag_NeverIntoTheInventory()
        {
            StaticData data = ShopCave(bagsOnSale: true);
            ExpeditionState state = InAShop(data, out MapNode _, coins: 100);
            int slot = SlotOf(state, OfferKind.Bag, "pouch");
            for (int guard = 0; slot < 0 && guard < 10; guard++)
            {
                state.Coins = 100;
                ExpeditionRules.RefreshShop(data, state);
                slot = SlotOf(state, OfferKind.Bag, "pouch");
            }

            Assert.GreaterOrEqual(slot, 0, "The pouch comes on offer.");
            Assert.AreEqual(0, state.Shop.Stock[slot].Grade);
            Assert.AreEqual(6, ExpeditionRules.PriceOf(data, state.Shop.Stock[slot]), "A bag's own price.");
            ExpeditionMember anna = state.Members[0];
            int coins = state.Coins;

            Assert.IsFalse(ExpeditionRules.CanBuyToInventory(data, state, slot), "A bag never goes to the inventory.");
            Assert.IsFalse(ExpeditionRules.ShopMergesAt(state, slot, 0, 0, 0));
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(0, 1)), "Over the start bag.");
            Assert.IsFalse(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(0, BoardFrame.Height - 1, 1)), "Standing out of the frame.");
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(0, 5, 1)), "Stood up in the frame.");
            Assert.IsTrue(ExpeditionRules.CanBuyToBoard(data, state, slot, 0, TestBoards.At(0, 2)));
            ExpeditionRules.BuyToBoard(data, state, slot, 0, TestBoards.At(0, 2));

            Assert.AreEqual(coins - 6, state.Coins);
            Assert.IsNull(state.Shop.Stock[slot], "Sold.");
            Assert.AreEqual(2, anna.Board.Bags.Count);
            Assert.AreEqual("pouch", anna.Board.BagAt(1, 2).Bag.Id);
            Assert.IsTrue(anna.Board.OnBags(0, 2, 3, 1), "Its squares now take items.");
        }

        // ---- The refresh and leaving ---------------------------------------------------------

        [Test]
        public void Refreshing_CostsTheBaseThenMoreEachTime_DrawsEverySlotAnew_AndANewShopStartsOver()
        {
            StaticData data = ShopCave();
            ExpeditionState state = InAShop(data, out MapNode _, coins: 18);
            BalanceData balance = data.Balance;
            ExpeditionRules.BuyPotion(data, state, SlotOf(state, OfferKind.Potion));
            Assert.AreEqual(13, state.Coins);
            Assert.AreEqual(balance.ShopRefreshBase, ExpeditionRules.RefreshCost(data, state));

            Assert.IsTrue(ExpeditionRules.CanRefreshShop(data, state));
            ExpeditionRules.RefreshShop(data, state);
            Assert.AreEqual(13 - balance.ShopRefreshBase, state.Coins);
            Assert.AreEqual(1, state.Shop.Refreshes);
            Assert.AreEqual(balance.ShopRefreshBase + balance.ShopRefreshStep, ExpeditionRules.RefreshCost(data, state), "The next one costs more.");
            Assert.AreEqual(4, state.Shop.Stock.Count);
            Assert.IsTrue(state.Shop.Stock.Take(3).All(o => o != null), "Every good is drawn anew.");
            Assert.IsNull(state.Shop.Stock[3], "Round 56: the potions stay as they were — the one bought stays gone.");

            ExpeditionRules.RefreshShop(data, state);
            Assert.AreEqual(13 - 2 * balance.ShopRefreshBase - balance.ShopRefreshStep, state.Coins);
            Assert.AreEqual(balance.ShopRefreshBase + 2 * balance.ShopRefreshStep, ExpeditionRules.RefreshCost(data, state));
            Assert.IsFalse(ExpeditionRules.CanRefreshShop(data, state), "Five coins left, seven wanted.");
            Assert.Throws<InvalidOperationException>(() => ExpeditionRules.RefreshShop(data, state));

            // The same shop refreshed the same number of times from the same seed shows the same stock.
            ExpeditionState twin = InAShop(data, out MapNode _, coins: 18);
            ExpeditionRules.BuyPotion(data, twin, SlotOf(twin, OfferKind.Potion));
            ExpeditionRules.RefreshShop(data, twin);
            ExpeditionRules.RefreshShop(data, twin);
            CollectionAssert.AreEqual(state.Shop.Stock.Select(o => o?.Id), twin.Shop.Stock.Select(o => o?.Id));

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
            Assert.IsTrue(ExpeditionRules.CanPickItem(state, 0, 0, 0));
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
                BoardItem weapon = member.Board.Items[0];
                weapon.Item = new EquippedItem(weapon.Item.Item, 999, isBase: true);
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
