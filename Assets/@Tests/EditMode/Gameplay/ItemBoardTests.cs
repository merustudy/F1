using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The grid board of Docs/Design/02_Combat_System.md §4 (Slice B stage 19): bags in the frame, items lying on the bags' squares,
    /// turned a quarter at a time (Backpack Battles' turning), read in reading order.
    /// </summary>
    public sealed class ItemBoardTests
    {
        static readonly BagData Pack = new BagData("pack", TestData.Text("pack"), 3, 2, true, 0, 0, 0);
        static readonly BagData Pouch = new BagData("pouch", TestData.Text("pouch"), 3, 1, false, 6, 5, 1);

        static EquippedItem Item(string id, int width, int height)
        {
            return new EquippedItem(TestData.Item(id, 1000, EffectKind.Damage, TargetMode.EnemyFront, width: width, height: height), 5);
        }

        static ItemBoard PackBoard()
        {
            var board = new ItemBoard();
            board.Bags.Add(new BoardBag(Pack, new Placement(0, 0)));
            return board;
        }

        [Test]
        public void Placement_TurnsAQuarterAtATime_AndATurnedShapeSwapsItsSides()
        {
            Assert.AreEqual(1, new Placement(0, 0, 5).Turns, "Turns wrap round four.");
            Assert.AreEqual(3, new Placement(0, 0, -1).Turns);
            Assert.AreEqual(3, new Placement(0, 0, 0).WidthOf(3, 1));
            Assert.AreEqual(1, new Placement(0, 0, 1).WidthOf(3, 1), "A quarter turn stands a 3x1 item up: 1x3.");
            Assert.AreEqual(3, new Placement(0, 0, 1).HeightOf(3, 1));
            Assert.AreEqual(3, new Placement(0, 0, 2).WidthOf(3, 1), "Half a turn lies the same way round.");
        }

        [Test]
        public void ABoardItem_CoversItsSquares_AsItLies()
        {
            var lying = new BoardItem(Item("spear", 3, 2), new Placement(0, 1));
            Assert.IsTrue(lying.Covers(2, 2));
            Assert.IsFalse(lying.Covers(0, 3));

            var standing = new BoardItem(Item("spear", 3, 2), new Placement(1, 0, 1));
            Assert.AreEqual(2, standing.Width);
            Assert.AreEqual(3, standing.Height);
            Assert.IsTrue(standing.Covers(2, 2));
            Assert.IsFalse(standing.Covers(0, 0));
        }

        [Test]
        public void Starting_LaysTheStartBagAndTheBaseWeaponAtTheTopLeft()
        {
            EquippedItem weapon = Item("blade", 2, 1);
            ItemBoard board = ItemBoard.Starting(Pack, weapon);

            Assert.AreEqual(1, board.Bags.Count);
            Assert.AreEqual(new Placement(0, 0), board.Bags[0].At);
            Assert.AreSame(weapon, board.ItemAt(1, 0).Item);
            Assert.IsNull(board.ItemAt(2, 0));
        }

        [Test]
        public void OnBags_IsInsideTheFrameAndOnBagSquares_AcrossTwoBagsToo()
        {
            ItemBoard board = PackBoard();
            Assert.IsTrue(board.OnBags(0, 0, 3, 2));
            Assert.IsFalse(board.OnBags(0, 1, 1, 2), "The third row has no bag.");
            Assert.IsFalse(board.OnBags(2, 0, 2, 1), "Past the frame's right edge.");

            board.Bags.Add(new BoardBag(Pouch, new Placement(0, 2)));
            Assert.IsTrue(board.OnBags(0, 1, 1, 2), "An item may lie across two touching bags.");
        }

        [Test]
        public void ItemsUnder_ListsWhatARectangleOverlaps_LeavingOneOut()
        {
            ItemBoard board = PackBoard();
            var a = new BoardItem(Item("a", 2, 1), new Placement(0, 0));
            var b = new BoardItem(Item("b", 1, 1), new Placement(2, 1));
            board.Items.Add(a);
            board.Items.Add(b);

            CollectionAssert.AreEqual(new[] { a, b }, board.ItemsUnder(1, 0, 2, 2, null));
            CollectionAssert.AreEqual(new[] { b }, board.ItemsUnder(1, 0, 2, 2, a));
            CollectionAssert.IsEmpty(board.ItemsUnder(0, 1, 2, 1, null));
        }

        [Test]
        public void FreeOfBags_KeepsBagsApartAndInsideTheFrame()
        {
            ItemBoard board = PackBoard();
            Assert.IsTrue(board.FreeOfBags(0, 2, 3, 1, null));
            Assert.IsFalse(board.FreeOfBags(0, 1, 3, 1, null), "Over the pack.");
            Assert.IsTrue(board.FreeOfBags(0, 1, 3, 1, board.Bags[0]), "A bag is not in its own way.");
            Assert.IsFalse(board.FreeOfBags(0, BoardFrame.Height - 1, 1, 2, null), "Past the frame's bottom.");
        }

        [Test]
        public void ABagsItems_AreThoseWhollyInside_AndAnItemAcrossTwoBagsIsNoticed()
        {
            ItemBoard board = PackBoard();
            var pouch = new BoardBag(Pouch, new Placement(0, 2));
            board.Bags.Add(pouch);
            var inside = new BoardItem(Item("in", 2, 1), new Placement(0, 2));
            board.Items.Add(inside);
            CollectionAssert.AreEqual(new[] { inside }, board.ItemsIn(pouch));
            Assert.IsFalse(board.HasItemAcross(pouch));

            board.Items.Add(new BoardItem(Item("across", 1, 2), new Placement(2, 1)));
            Assert.IsTrue(board.HasItemAcross(pouch));
            Assert.IsTrue(board.HasItemAcross(board.Bags[0]));
        }

        [Test]
        public void ReadingOrder_IsByTheTopLeftSquare_RowFirst()
        {
            ItemBoard board = PackBoard();
            board.Bags.Add(new BoardBag(Pouch, new Placement(0, 2)));
            board.Items.Add(new BoardItem(Item("c", 3, 1), new Placement(0, 2)));
            board.Items.Add(new BoardItem(Item("b", 1, 1), new Placement(2, 0)));
            board.Items.Add(new BoardItem(Item("a", 2, 2), new Placement(0, 0)));

            CollectionAssert.AreEqual(new[] { "a", "b", "c" }, board.InReadingOrder().Select(item => item.Item.Id));
        }

        [Test]
        public void FindRoom_TakesTheFirstFreeSpotInReadingOrder_TurningWhenItMustOnly()
        {
            ItemBoard board = PackBoard();
            board.Items.Add(new BoardItem(Item("a", 2, 1), new Placement(0, 0)));

            Assert.IsTrue(board.FindRoom(1, 1, out Placement small));
            Assert.AreEqual(new Placement(2, 0), small);
            Assert.IsTrue(board.FindRoom(1, 2, out Placement tall));
            Assert.AreEqual(new Placement(2, 0), tall, "A 1x2 stands in the pack's last column.");
            Assert.IsTrue(board.FindRoom(2, 2, out _) == false, "No 2x2 room is left.");
            Assert.IsTrue(board.FindRoom(3, 1, out Placement wide));
            Assert.AreEqual(new Placement(0, 1), wide);
        }

        [Test]
        public void FindBagRoom_IsTheFirstSpotOfTheFrameOverNoBag()
        {
            ItemBoard board = PackBoard();
            Assert.IsTrue(board.FindBagRoom(3, 1, out Placement at));
            Assert.AreEqual(new Placement(0, 2), at);
        }

        [Test]
        public void TheLayout_OfAMercenaryListsWhereEachItemLies_InReadingOrder_AndAnEnemysStacksThem()
        {
            ItemBoard board = PackBoard();
            board.Items.Add(new BoardItem(Item("b", 1, 1), new Placement(2, 1)));
            board.Items.Add(new BoardItem(Item("a", 2, 1), new Placement(0, 0, 2)));

            BoardLayout layout = BoardLayout.Of(board);
            CollectionAssert.AreEqual(new[] { new Placement(0, 0, 2), new Placement(2, 1) }, layout.Items);
            Assert.AreEqual(1, layout.Bags.Count);

            BoardLayout stacked = BoardLayout.Stacked(new List<EquippedItem> { Item("x", 1, 2), Item("y", 2, 1) });
            CollectionAssert.AreEqual(new[] { new Placement(0, 0), new Placement(0, 2) }, stacked.Items);
            CollectionAssert.IsEmpty(stacked.Bags);
        }
    }
}
