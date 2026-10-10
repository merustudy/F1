using System.Collections.Generic;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Where the merchant's goods lie on its grid (Slice B stage 21): the widest first in reading order, the potions down their own column
    /// (round 56), the bags along the last row under an empty one (round 58).
    /// </summary>
    public sealed class MerchantLayoutTests
    {
        [Test]
        public void TheWidestLieFirst_InReadingOrder_AndThePotionsDownTheirColumn()
        {
            var goods = new List<MerchantGood>
            {
                new MerchantGood(0, 2, 1, OfferKind.Item),         // a dagger
                new MerchantGood(1, 2, 2, OfferKind.Item),         // a spear
                new MerchantGood(2, 3, 1, OfferKind.Item),         // a wide item
                new MerchantGood(3, 1, 1, OfferKind.Item),         // a buckler
                new MerchantGood(4, 1, 1, OfferKind.Potion, 0),    // a potion
                new MerchantGood(6, 1, 1, OfferKind.Potion, 2),    // the third potion (the second was bought)
            };

            Dictionary<int, Placement> places = MerchantLayout.Lay(goods, 8, 6, 7, 9, out int rows);

            Assert.AreEqual(new Placement(0, 0), places[1], "The spear (four squares) first.");
            Assert.AreEqual(new Placement(2, 0), places[2], "Then the wide one (three), beside it.");
            Assert.AreEqual(new Placement(5, 0), places[0], "Then the dagger.");
            Assert.AreEqual(new Placement(7, 0), places[3], "Then the buckler, in the eighth column: the goods keep to the first eight.");
            Assert.AreEqual(new Placement(9, 0), places[4], "The potions down the last column.");
            Assert.AreEqual(new Placement(9, 2), places[6], "At its place among the potions: the bought one's square stays empty.");
            Assert.AreEqual(8, rows, "The grid's rows at the least.");
        }

        [Test]
        public void TheBags_LieAlongTheLastRow_UnderAnEmptyRow_TheWidestFirst()
        {
            var goods = new List<MerchantGood>
            {
                new MerchantGood(0, 2, 1, OfferKind.Bag),     // a belt pouch
                new MerchantGood(1, 2, 3, OfferKind.Item),    // a halberd
                new MerchantGood(2, 3, 1, OfferKind.Bag),     // a leather pouch
                new MerchantGood(3, 2, 1, OfferKind.Item),    // a dagger
            };

            Dictionary<int, Placement> places = MerchantLayout.Lay(goods, 8, 6, 7, 9, out int rows);

            Assert.AreEqual(new Placement(0, 0), places[1], "The items from the top, as before.");
            Assert.AreEqual(new Placement(2, 0), places[3]);
            Assert.AreEqual(new Placement(0, 7), places[2], "The wider bag first, at the last row's left.");
            Assert.AreEqual(new Placement(3, 7), places[0], "Then the other beside it.");
            Assert.AreEqual(8, rows);
        }

        [Test]
        public void WhenTheItemsNeedMoreRows_RowsAreAdded_AndTheBagsMoveDownWithThem()
        {
            var goods = new List<MerchantGood>();
            for (int slot = 0; slot < 4; slot++)
            {
                goods.Add(new MerchantGood(slot, 3, 2, OfferKind.Item));
            }

            goods.Add(new MerchantGood(4, 2, 1, OfferKind.Bag));

            Dictionary<int, Placement> places = MerchantLayout.Lay(goods, 6, 3, 4, 7, out int rows);

            Assert.AreEqual(new Placement(0, 0), places[0]);
            Assert.AreEqual(new Placement(3, 0), places[1]);
            Assert.AreEqual(new Placement(0, 2), places[2], "Below the first two, past the items' three rows.");
            Assert.AreEqual(new Placement(0, 5), places[4], "The items took a row more: the empty row and the bags' row move down one.");
            Assert.AreEqual(6, rows);
        }

        [Test]
        public void TheSameGoods_LieTheSameWay()
        {
            var goods = new List<MerchantGood>
            {
                new MerchantGood(0, 2, 3, OfferKind.Item), new MerchantGood(1, 2, 1, OfferKind.Item), new MerchantGood(2, 2, 3, OfferKind.Item),
                new MerchantGood(3, 2, 1, OfferKind.Bag), new MerchantGood(4, 2, 1, OfferKind.Bag),
            };
            Dictionary<int, Placement> first = MerchantLayout.Lay(goods, 8, 6, 7, 9, out _);
            Dictionary<int, Placement> again = MerchantLayout.Lay(goods, 8, 6, 7, 9, out _);
            CollectionAssert.AreEquivalent(first, again);
            Assert.AreEqual(new Placement(0, 0), first[0], "Of two of a size, the earlier slot first.");
            Assert.AreEqual(new Placement(2, 0), first[2]);
            Assert.AreEqual(new Placement(0, 7), first[3], "Of two bags of a size, the earlier slot first.");
            Assert.AreEqual(new Placement(2, 7), first[4]);
        }
    }
}
