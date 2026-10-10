using System.Collections.Generic;
using F1.Gameplay;

namespace F1.UI
{
    /// <summary>
    /// A good of the merchant to lay on its grid: its slot in the stock, its squares across and down, what it is (an item, a bag or a
    /// potion), and a potion's row in the potions' column (its place among the shop's potions, so that one bought leaves its square empty;
    /// -1 for the next free row).
    /// </summary>
    public readonly struct MerchantGood
    {
        public MerchantGood(int slot, int width, int height, OfferKind kind, int potionRow = -1)
        {
            Slot = slot;
            Width = width;
            Height = height;
            Kind = kind;
            PotionRow = potionRow;
        }

        public int Slot { get; }
        public int Width { get; }
        public int Height { get; }
        public OfferKind Kind { get; }
        public int PotionRow { get; }
    }

    /// <summary>
    /// Where the merchant's goods lie on its grid (Slice B stage 21; Docs/Architecture/12_UI.md "상인"): the items in the first columns'
    /// top rows, the bags along a row of their own under them (round 58: the grid's last row, one empty row between), each the widest
    /// first (by squares, then the taller) at the first place in reading order where it lies over no other, unturned; the potions down a
    /// column of their own from the top (round 56: the last column, one empty column between). When the items need more rows than theirs,
    /// rows are added and the empty row and the bags move down with them; bags that do not fit their row go on under it. The places come
    /// from the goods alone, so they are not saved: the screen keeps them while the shop is open, so that a good bought leaves its place empty.
    /// </summary>
    public static class MerchantLayout
    {
        /// <param name="goods">The goods in stock order.</param>
        /// <param name="width">The squares across the items and the bags (not the potions) lie in.</param>
        /// <param name="itemRows">The rows the items lie in from the top.</param>
        /// <param name="bagRow">The row the bags lie along; the grid's squares down are at the least the rows down to it.</param>
        /// <param name="potionColumn">The column the potions lie down.</param>
        /// <param name="rows">The rows the goods take, at the least the grid's.</param>
        /// <returns>Each good's place, by slot.</returns>
        public static Dictionary<int, Placement> Lay(IReadOnlyList<MerchantGood> goods, int width, int itemRows, int bagRow, int potionColumn, out int rows)
        {
            var places = new Dictionary<int, Placement>();
            var items = new List<MerchantGood>();
            var bags = new List<MerchantGood>();
            int nextPotion = 0;
            rows = bagRow + 1;
            foreach (MerchantGood good in goods)
            {
                switch (good.Kind)
                {
                    case OfferKind.Potion:
                        int row = good.PotionRow >= 0 ? good.PotionRow : nextPotion;
                        nextPotion = row + 1;
                        places[good.Slot] = new Placement(potionColumn, row);
                        rows = System.Math.Max(rows, row + 1);
                        break;
                    case OfferKind.Bag:
                        bags.Add(good);
                        break;
                    default:
                        items.Add(good);
                        break;
                }
            }

            int itemsEnd = Pack(items, width, 0, places);
            int bagsEnd = Pack(bags, width, bagRow + System.Math.Max(0, itemsEnd - itemRows), places);
            rows = System.Math.Max(rows, System.Math.Max(itemsEnd, bagsEnd));
            return places;
        }

        /// <summary>
        /// Lays goods from a row down, the widest first (by squares, then the taller, then the earlier slot), each at the first place in
        /// reading order where it lies over no other; returns the row under the lowest (the top row when there are none).
        /// </summary>
        static int Pack(List<MerchantGood> goods, int width, int top, Dictionary<int, Placement> places)
        {
            goods.Sort((a, b) =>
            {
                int area = (b.Width * b.Height).CompareTo(a.Width * a.Height);
                if (area != 0)
                {
                    return area;
                }

                int tall = b.Height.CompareTo(a.Height);
                return tall != 0 ? tall : a.Slot.CompareTo(b.Slot);
            });

            var taken = new List<bool[]>();
            int end = top;
            foreach (MerchantGood good in goods)
            {
                int w = System.Math.Min(good.Width, width);
                for (int y = 0; ; y++)
                {
                    int x = FirstFree(taken, width, y, w, good.Height);
                    if (x < 0)
                    {
                        continue;
                    }

                    Take(taken, width, x, y, w, good.Height);
                    places[good.Slot] = new Placement(x, top + y);
                    end = System.Math.Max(end, top + y + good.Height);
                    break;
                }
            }

            return end;
        }

        static int FirstFree(List<bool[]> taken, int width, int y, int w, int h)
        {
            for (int x = 0; x + w <= width; x++)
            {
                bool free = true;
                for (int dy = 0; dy < h && free; dy++)
                {
                    for (int dx = 0; dx < w && free; dx++)
                    {
                        free = !IsTaken(taken, x + dx, y + dy);
                    }
                }

                if (free)
                {
                    return x;
                }
            }

            return -1;
        }

        static bool IsTaken(List<bool[]> taken, int x, int y)
        {
            return y < taken.Count && taken[y][x];
        }

        static void Take(List<bool[]> taken, int width, int x, int y, int w, int h)
        {
            while (taken.Count < y + h)
            {
                taken.Add(new bool[width]);
            }

            for (int dy = 0; dy < h; dy++)
            {
                for (int dx = 0; dx < w; dx++)
                {
                    taken[y + dy][x + dx] = true;
                }
            }
        }
    }
}
