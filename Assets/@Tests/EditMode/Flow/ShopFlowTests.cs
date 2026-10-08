using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>A shop node through the application layer (Slice B stage 17): entering it, buying and refreshing there, leaving, and coming back to it after a restart.</summary>
    public sealed class ShopFlowTests
    {
        [TearDown]
        public void TearDown()
        {
            FlowTestKit.DeleteSaveRoots();
        }

        /// <summary>The strong party of the flow kit in a cave whose second floor is all shops: a battle, a shop, then the boss.</summary>
        static StaticData CaveWithAShop()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Jobs = new List<JobData>
            {
                new JobData("tank", TestData.Text("tank"), 100, 3, "blade", 200, 1, null),
                new JobData("healer", TestData.Text("healer"), 60, 3, "staff", 10, 2, null),
                new JobData("striker", TestData.Text("striker"), 80, 2, "blade", 200, 3, null),
            };
            parts.Dungeons = new List<DungeonData>
            {
                new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }, null, shopMinFloor: 2, shopChancePercent: 100),
            };
            return new StaticData(parts);
        }

        /// <summary>Wins the first floor's battle, leaves its loot and goes into the shop on the second floor.</summary>
        static FlowTestKit AtTheShop(out int coinsWon)
        {
            FlowTestKit kit = new FlowTestKit(CaveWithAShop()).OnNodeMap();
            MapNode first = kit.Expedition.AvailableNodes()[0];
            kit.Expedition.EnterNode(first.Id);
            kit.FightToTheEnd();
            Assert.AreEqual(ExpeditionRules.CoinsFor(kit.Data, first), kit.Expedition.BattleCoins, "The result window's line.");
            coinsWon = kit.Expedition.Expedition.Coins;
            Assert.AreEqual(kit.Expedition.BattleCoins, coinsWon, "The victory's coins are in the expedition at once.");
            kit.Expedition.CloseBattle();
            Assert.AreEqual(0, kit.Expedition.BattleCoins, "No battle on show.");
            kit.Expedition.LeaveLoot();
            MapNode shop = kit.Expedition.AvailableNodes().First();
            Assert.AreEqual(MapNodeKind.Shop, shop.Kind, "The second floor is all shops.");
            kit.Expedition.EnterNode(shop.Id);
            return kit;
        }

        [Test]
        public void EnteringAShopNode_StartsNoBattle_DrawsTheStock_AndIsSaved_AndComesBackAfterARestart()
        {
            FlowTestKit kit = AtTheShop(out int coinsWon);

            Assert.AreEqual(GamePhase.Shop, kit.Expedition.Phase);
            Assert.IsNull(kit.Expedition.Battle);
            Assert.IsEmpty(kit.Expedition.AvailableNodes(), "No node is offered until the party leaves.");
            Assert.AreEqual(kit.Data.Balance.ShopSlots, kit.Expedition.ShopStock.Count);
            Assert.AreEqual(kit.Data.Balance.ShopRefreshBase, kit.Expedition.RefreshCost);
            ExpeditionRecord saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition;
            Assert.AreEqual("AtShop", saved.Phase);
            Assert.AreEqual(coinsWon, saved.Coins);
            Assert.AreEqual(kit.Data.Balance.ShopSlots, saved.Shop.Stock.Count);
            Assert.AreEqual(0, saved.Shop.Refreshes);

            FlowTestKit restarted = kit.Restart();
            Assert.AreEqual(GamePhase.Shop, restarted.Expedition.Phase, "Closing the app at a shop comes back to the shop.");
            Assert.AreEqual(coinsWon, restarted.Expedition.Expedition.Coins);
            CollectionAssert.AreEqual(kit.Expedition.ShopStock.Select(o => o.Id), restarted.Expedition.ShopStock.Select(o => o.Id), "The same stock.");
        }

        [Test]
        public void BuyingAndRefreshing_PayTheCoins_AndAreSaved()
        {
            FlowTestKit kit = AtTheShop(out int _);
            kit.Expedition.Expedition.Coins = 100;
            IReadOnlyList<ItemOffer> stock = kit.Expedition.ShopStock;
            int item = Enumerable.Range(0, stock.Count).First(i => stock[i].Kind == OfferKind.Item);
            int price = kit.Expedition.PriceOf(stock[item]);

            Assert.IsTrue(kit.Expedition.CanAfford(item));
            Assert.IsTrue(kit.Expedition.CanBuyToBoard(item, 0, 1));
            kit.Expedition.BuyToBoard(item, 0, 1);
            Assert.AreEqual(100 - price, kit.Expedition.Expedition.Coins);
            Assert.AreEqual(2, kit.Expedition.Expedition.Members[0].Items.Count);
            Assert.IsNull(kit.Expedition.ShopStock[item], "Sold.");
            ExpeditionRecord saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition;
            Assert.AreEqual(100 - price, saved.Coins, "The purchase is saved.");
            Assert.IsNull(saved.Shop.Stock[item]);

            int coins = kit.Expedition.Expedition.Coins;
            Assert.IsTrue(kit.Expedition.CanRefreshShop);
            kit.Expedition.RefreshShop();
            Assert.AreEqual(coins - kit.Data.Balance.ShopRefreshBase, kit.Expedition.Expedition.Coins);
            Assert.AreEqual(kit.Data.Balance.ShopRefreshBase + kit.Data.Balance.ShopRefreshStep, kit.Expedition.RefreshCost);
            Assert.IsTrue(kit.Expedition.ShopStock.All(o => o != null), "Drawn anew.");
            saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition;
            Assert.AreEqual(1, saved.Shop.Refreshes, "The refresh is saved.");

            FlowTestKit restarted = kit.Restart();
            Assert.AreEqual(kit.Data.Balance.ShopRefreshBase + kit.Data.Balance.ShopRefreshStep, restarted.Expedition.RefreshCost, "The climbed cost comes back.");
            CollectionAssert.AreEqual(kit.Expedition.ShopStock.Select(o => o.Id), restarted.Expedition.ShopStock.Select(o => o.Id));

            int potion = restarted.Expedition.ShopStock.ToList().FindIndex(o => o != null && o.Kind == OfferKind.Potion);
            if (potion >= 0)
            {
                Assert.IsTrue(restarted.Expedition.CanBuyPotion(potion));
                restarted.Expedition.BuyPotion(potion);
                Assert.AreEqual(2, restarted.Expedition.Expedition.Potions.Count(p => p != null));
            }

            int inventory = restarted.Expedition.ShopStock.ToList().FindIndex(o => o != null && o.Kind == OfferKind.Item);
            Assert.IsTrue(restarted.Expedition.CanBuyToInventory(inventory));
            restarted.Expedition.BuyToInventory(inventory);
            Assert.AreEqual(1, restarted.Expedition.Expedition.Inventory.Count);
        }

        [Test]
        public void LeavingTheShop_GoesOnToTheBoss_AndIsSaved_AndTheCoinsGoWithTheExpedition()
        {
            FlowTestKit kit = AtTheShop(out int coinsWon);
            Assert.Greater(coinsWon, 0);

            kit.Expedition.LeaveShop();

            Assert.AreEqual(GamePhase.NodeMap, kit.Expedition.Phase);
            Assert.AreEqual(MapNodeKind.Boss, kit.Expedition.AvailableNodes().Single().Kind);
            ExpeditionRecord saved = kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition;
            Assert.AreEqual("ChoosingNode", saved.Phase, "Leaving is saved.");
            Assert.IsNull(saved.Shop);
            Assert.AreEqual(coinsWon, saved.Coins, "The coins stay until the expedition ends.");

            kit.PlayExpeditionToTheEnd();
            Assert.AreEqual(GamePhase.Settlement, kit.Expedition.Phase);
            Assert.IsNull(kit.Save.Load<RunSaveData>(RunManager.FileName).Value.Expedition, "The expedition, its coins with it, is gone.");
        }

        [Test]
        public void AtTheShop_TheBoardsAndRowsCanBeRearranged_ButShopCommandsAreForTheShopOnly()
        {
            FlowTestKit kit = AtTheShop(out int _);
            Assert.IsTrue(kit.Expedition.CanMoveToRow(0, 2));
            kit.Expedition.MoveToRow(0, 2);
            Assert.IsTrue(kit.Expedition.CanPickItem(0, 0));
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.AdvanceBattle(100));
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.RestAtCamp());

            kit.Expedition.LeaveShop();
            Assert.IsEmpty(kit.Expedition.ShopStock);
            Assert.AreEqual(0, kit.Expedition.RefreshCost);
            Assert.IsFalse(kit.Expedition.CanRefreshShop);
            Assert.IsFalse(kit.Expedition.CanBuyToBoard(0, 0, 1));
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.RefreshShop());
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.LeaveShop());
            Assert.Throws<System.InvalidOperationException>(() => kit.Expedition.BuyToInventory(0));
        }

        [Test]
        public void ASaveAtTheShop_IsRefused_WhenItsShopRecordIsOff()
        {
            FlowTestKit kit = AtTheShop(out int _);
            RunSaveData save = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            Assert.DoesNotThrow(() => RunSaveMapper.Read(save, kit.Data, out RunState _, out ExpeditionState _));

            RunSaveData broken = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            broken.Expedition.Shop = null;
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(broken, kit.Data, out RunState _, out ExpeditionState _), "At a shop without a shop record.");

            broken = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            broken.Expedition.Shop.Stock.Add(new OfferRecord { Kind = "Item", Id = "knife", Grade = 8, Tier = "Common" });
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(broken, kit.Data, out RunState _, out ExpeditionState _), "More offers than slots.");

            broken = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            broken.Expedition.Shop.Stock[0] = new OfferRecord { Kind = "Item", Id = "nothing", Grade = 8, Tier = "Common" };
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(broken, kit.Data, out RunState _, out ExpeditionState _), "An unknown item on offer.");

            broken = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            broken.Expedition.Shop.Refreshes = -1;
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(broken, kit.Data, out RunState _, out ExpeditionState _));

            broken = kit.Save.Load<RunSaveData>(RunManager.FileName).Value;
            broken.Expedition.Phase = "AtCamp";
            Assert.Throws<RunSaveException>(() => RunSaveMapper.Read(broken, kit.Data, out RunState _, out ExpeditionState _), "A shop node is shopped at, not camped at.");
        }
    }
}
