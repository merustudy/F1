using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>A star square of an item on a board and whether a melee weapon lies on it (Slice B stage 20): what the screen draws.</summary>
    public readonly struct StarMark
    {
        public StarMark(int x, int y, bool lit)
        {
            X = x;
            Y = y;
            Lit = lit;
        }

        public int X { get; }
        public int Y { get; }
        public bool Lit { get; }
    }

    /// <summary>
    /// The stars of items on a board (Slice B stage 20, Docs/Design/02_Combat_System.md §4: Backpack Battles' stars). A star is a square round
    /// its item that turns with it; a melee weapon lying on one is strengthened by the item's <see cref="EquippedItem.StarDamage"/>. An item
    /// fills one star of another item at most, so each star item counts once for a weapon; stars of different items count each.
    /// Stars count on their own board only.
    /// </summary>
    public static class StarRules
    {
        /// <summary>
        /// The board squares of an item's stars when it lies at a placement: each star square of its unturned shape, turned with it a quarter
        /// clockwise at a time ((x, y) → (height − 1 − y, x) of the shape before the turn, as a bag turns what it holds). Squares may be outside the frame.
        /// </summary>
        public static List<(int X, int Y)> StarSquares(ItemData item, Placement at)
        {
            var squares = new List<(int X, int Y)>();
            foreach (StarSquare star in item.Stars)
            {
                int x = star.X;
                int y = star.Y;
                int height = item.Height;
                int width = item.Width;
                for (int turn = 0; turn < at.Turns; turn++)
                {
                    (x, y) = (height - 1 - y, x);
                    (width, height) = (height, width);
                }

                squares.Add((at.X + x, at.Y + y));
            }

            return squares;
        }

        /// <summary>
        /// The stars an item would show lying at a placement on a board: one for each star square on a bag, lit when a melee weapon lies there.
        /// <paramref name="leaveOut"/> is an item of the board that does not count where it lies (the one held from this board), or null.
        /// </summary>
        public static List<StarMark> Marks(ItemBoard board, ItemData item, Placement at, BoardItem leaveOut)
        {
            var marks = new List<StarMark>();
            foreach ((int x, int y) in StarSquares(item, at))
            {
                if (!BoardFrame.Contains(x, y) || board.BagAt(x, y) == null)
                {
                    continue;
                }

                BoardItem there = board.ItemAt(x, y);
                marks.Add(new StarMark(x, y, there != null && there != leaveOut && there.Item.Item.Melee));
            }

            return marks;
        }

        /// <summary>What a weapon on a board deals more by the stars on it: each other star item one of whose stars it covers, once. 0 for anything but a melee weapon.</summary>
        public static int DamageOn(ItemBoard board, BoardItem weapon)
        {
            int bonus = 0;
            foreach (BoardItem source in Sources(board, weapon))
            {
                bonus += source.Item.StarDamage;
            }

            return bonus;
        }

        /// <summary>The star damage of a board's items in reading order: the battle's <see cref="BattleUnitSetup.StarDamage"/>.</summary>
        public static List<int> DamageInReadingOrder(ItemBoard board)
        {
            var damage = new List<int>();
            foreach (BoardItem placed in board.PlacedInReadingOrder())
            {
                damage.Add(DamageOn(board, placed));
            }

            return damage;
        }

        /// <summary>The star items of a board that strengthen a weapon (each once, in the order of the board's items): what its card names.</summary>
        public static List<BoardItem> Sources(ItemBoard board, BoardItem weapon)
        {
            var sources = new List<BoardItem>();
            if (!weapon.Item.Item.Melee)
            {
                return sources;
            }

            foreach (BoardItem other in board.Items)
            {
                if (other == weapon || other.Item.Item.Stars.Count == 0)
                {
                    continue;
                }

                foreach ((int x, int y) in StarSquares(other.Item.Item, other.At))
                {
                    if (weapon.Covers(x, y))
                    {
                        sources.Add(other);
                        break;
                    }
                }
            }

            return sources;
        }

        /// <summary>The melee weapons a star item lights on its board, each once, in the order of its star squares: "걸린 ★ n/m" of its card.</summary>
        public static List<BoardItem> Lit(ItemBoard board, BoardItem starItem)
        {
            var lit = new List<BoardItem>();
            foreach ((int x, int y) in StarSquares(starItem.Item.Item, starItem.At))
            {
                BoardItem there = board.ItemAt(x, y);
                if (there != null && there != starItem && there.Item.Item.Melee && !lit.Contains(there))
                {
                    lit.Add(there);
                }
            }

            return lit;
        }
    }
}
