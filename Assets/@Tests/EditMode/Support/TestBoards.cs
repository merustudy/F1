using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;

namespace F1.Tests
{
    /// <summary>
    /// Board helpers for the tests (Slice B stage 19): what a member's grid board holds, and boards laid by hand. A laid board has the
    /// start bag at the top-left and its items unturned at the left edge one row after another (as the 9 -> 10 save conversion lays
    /// an old board), with a one-row bag added for every row below the start bag that the items need.
    /// </summary>
    public static class TestBoards
    {
        public static Placement At(int x, int y, int turns = 0)
        {
            return new Placement(x, y, turns);
        }

        /// <summary>The member's items in reading order (the battle's order).</summary>
        public static List<EquippedItem> Items(ExpeditionMember member)
        {
            return member.Board.InReadingOrder();
        }

        /// <summary>The ids of the member's items in reading order.</summary>
        public static List<string> Ids(ExpeditionMember member)
        {
            return member.Board.InReadingOrder().Select(item => item.Item.Id).ToList();
        }

        /// <summary>The item covering a square of the member's board, or null.</summary>
        public static EquippedItem ItemAt(ExpeditionMember member, int x, int y)
        {
            return member.Board.ItemAt(x, y)?.Item;
        }

        /// <summary>Lays the member's board anew: the start bag, the items one row after another, a one-row bag for each row past the start bag.</summary>
        public static void Lay(StaticData data, ExpeditionMember member, params EquippedItem[] items)
        {
            BagData start = data.StartBag;
            BagData row = data.Bags.Ordered.First(bag => !bag.Start && bag.Width == BoardFrame.Width && bag.Height == 1);
            var board = new ItemBoard();
            board.Bags.Add(new BoardBag(start, At(0, 0)));
            int y = 0;
            foreach (EquippedItem item in items)
            {
                board.Items.Add(new BoardItem(item, At(0, y)));
                y += item.Item.Height;
            }

            for (int extra = start.Height; extra < y; extra++)
            {
                board.Bags.Add(new BoardBag(row, At(0, extra)));
            }

            member.Board = board;
        }

        /// <summary>Puts an item on the member's board at a placement, as it is (no rule checked).</summary>
        public static void Put(ExpeditionMember member, EquippedItem item, int x, int y, int turns = 0)
        {
            member.Board.Items.Add(new BoardItem(item, At(x, y, turns)));
        }

        /// <summary>Adds a bag to the member's frame at a placement, as it is (no rule checked).</summary>
        public static void AddBag(StaticData data, ExpeditionMember member, string bagId, int x, int y, int turns = 0)
        {
            member.Board.Bags.Add(new BoardBag(data.Bags.Get(bagId), At(x, y, turns)));
        }
    }
}
