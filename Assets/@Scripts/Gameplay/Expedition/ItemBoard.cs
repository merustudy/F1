using System;
using System.Collections.Generic;

namespace F1.Gameplay
{
    /// <summary>
    /// The item board of a unit (Docs/Design/02_Combat_System.md §4, 03_Dungeon_Structure.md §5):
    /// an ordered list of items on a fixed number of cells. Each item takes its size in cells, from
    /// the first cell with no gap between items, so the order of the list is both the layout and the
    /// activation order. Every function takes the list and the number of cells; the state types
    /// hold them, and this is the one place that turns cells into items.
    /// </summary>
    public static class ItemBoard
    {
        public static int UsedCells(IReadOnlyList<EquippedItem> items)
        {
            int cells = 0;
            foreach (EquippedItem item in items)
            {
                cells += item.Item.Size;
            }

            return cells;
        }

        public static int FreeCells(IReadOnlyList<EquippedItem> items, int cells)
        {
            return cells - UsedCells(items);
        }

        /// <summary>The first cell an item takes.</summary>
        public static int FirstCellOf(IReadOnlyList<EquippedItem> items, int index)
        {
            int cell = 0;
            for (int i = 0; i < index; i++)
            {
                cell += items[i].Item.Size;
            }

            return cell;
        }

        /// <summary>The index of the item that takes a cell, or -1 when the cell is empty.</summary>
        public static int IndexAtCell(IReadOnlyList<EquippedItem> items, int cell)
        {
            if (cell < 0)
            {
                return -1;
            }

            int first = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (cell < first + items[i].Item.Size)
                {
                    return i;
                }

                first += items[i].Item.Size;
            }

            return -1;
        }

        /// <summary>
        /// Whether an item of a size can be put at a cell: into the free cells when the cell is empty
        /// (it goes to the end of the board), or in place of the item there, which leaves the board.
        /// </summary>
        public static bool CanPut(IReadOnlyList<EquippedItem> items, int cells, int cell, int size)
        {
            if (cell < 0 || cell >= cells)
            {
                return false;
            }

            int index = IndexAtCell(items, cell);
            int freed = index < 0 ? 0 : items[index].Item.Size;
            return UsedCells(items) - freed + size <= cells;
        }

        /// <summary>Puts an item at a cell (see <see cref="CanPut"/>). Returns the item that left the board, or null.</summary>
        public static EquippedItem Put(List<EquippedItem> items, int cells, int cell, EquippedItem item)
        {
            if (!CanPut(items, cells, cell, item.Item.Size))
            {
                throw new InvalidOperationException($"'{item.Item.Id}' does not fit at cell {cell} of a board of {cells} cells.");
            }

            int index = IndexAtCell(items, cell);
            if (index < 0)
            {
                items.Add(item);
                return null;
            }

            EquippedItem left = items[index];
            items[index] = item;
            return left;
        }
    }
}
