using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The item board of Docs/Design/02_Combat_System.md §4: items in order, each taking its size in cells.</summary>
    public sealed class ItemBoardTests
    {
        static EquippedItem Item(string id, int size)
        {
            return new EquippedItem(TestData.Item(id, 1000, EffectKind.Damage, TargetMode.EnemyFront, size: size), 5);
        }

        static List<EquippedItem> Board(params EquippedItem[] items)
        {
            return new List<EquippedItem>(items);
        }

        [Test]
        public void Items_TakeTheirSizeInCells_FromTheFrontWithNoGap()
        {
            List<EquippedItem> board = Board(Item("a", 1), Item("b", 2), Item("c", 1));

            Assert.AreEqual(4, ItemBoard.UsedCells(board));
            Assert.AreEqual(2, ItemBoard.FreeCells(board, 6));
            CollectionAssert.AreEqual(new[] { 0, 1, 3 }, new[] { ItemBoard.FirstCellOf(board, 0), ItemBoard.FirstCellOf(board, 1), ItemBoard.FirstCellOf(board, 2) });
            CollectionAssert.AreEqual(
                new[] { 0, 1, 1, 2, -1, -1 },
                Array.ConvertAll(new[] { 0, 1, 2, 3, 4, 5 }, cell => ItemBoard.IndexAtCell(board, cell)),
                "Each cell belongs to the item over it; the cells after the last item are empty.");
            Assert.AreEqual(-1, ItemBoard.IndexAtCell(board, -1));
        }

        [Test]
        public void CanPut_IntoAnEmptyCell_NeedsEnoughFreeCells()
        {
            List<EquippedItem> board = Board(Item("a", 1));

            Assert.IsTrue(ItemBoard.CanPut(board, 3, 1, 2), "Two free cells take a size-2 item.");
            Assert.IsTrue(ItemBoard.CanPut(board, 3, 2, 2), "Any empty cell means the same: the item goes to the end.");
            Assert.IsFalse(ItemBoard.CanPut(board, 3, 1, 3));
            Assert.IsFalse(ItemBoard.CanPut(board, 3, 3, 1), "Beyond the board.");
            Assert.IsFalse(ItemBoard.CanPut(board, 3, -1, 1));
        }

        [Test]
        public void CanPut_OntoAnItem_CountsThatItemAsLeaving()
        {
            List<EquippedItem> board = Board(Item("a", 1), Item("b", 2));

            Assert.IsTrue(ItemBoard.CanPut(board, 3, 0, 1), "In place of a: 2 + 1 fits 3.");
            Assert.IsFalse(ItemBoard.CanPut(board, 3, 0, 2), "In place of a: 2 + 2 does not fit 3.");
            Assert.IsTrue(ItemBoard.CanPut(board, 3, 2, 2), "In place of b (its second cell): 1 + 2 fits.");
            Assert.IsFalse(ItemBoard.CanPut(board, 3, 1, 3));
        }

        [Test]
        public void Put_IntoEmptyCells_AppendsAndReturnsNothing()
        {
            List<EquippedItem> board = Board(Item("a", 1));
            EquippedItem pike = Item("pike", 2);

            Assert.IsNull(ItemBoard.Put(board, 3, 2, pike));

            CollectionAssert.AreEqual(new[] { "a", "pike" }, board.ConvertAll(i => i.Item.Id));
        }

        [Test]
        public void Put_OntoAnItem_TakesItsPlaceAndReturnsIt()
        {
            EquippedItem a = Item("a", 1);
            EquippedItem b = Item("b", 2);
            List<EquippedItem> board = Board(a, b);
            EquippedItem pike = Item("pike", 2);

            EquippedItem left = ItemBoard.Put(board, 3, 1, pike);

            Assert.AreSame(b, left);
            CollectionAssert.AreEqual(new[] { "a", "pike" }, board.ConvertAll(i => i.Item.Id), "The newcomer stands where the other was.");
        }

        [Test]
        public void Put_WhereItDoesNotFit_ThrowsAndChangesNothing()
        {
            List<EquippedItem> board = Board(Item("a", 1), Item("b", 2));

            Assert.Throws<InvalidOperationException>(() => ItemBoard.Put(board, 3, 0, Item("big", 3)));

            CollectionAssert.AreEqual(new[] { "a", "b" }, board.ConvertAll(i => i.Item.Id));
        }
    }
}
