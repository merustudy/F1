using System;
using System.Collections;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// Where an item or a bag lies on a board (Slice B stage 19, Docs/Design/02_Combat_System.md §4): the top-left square it covers and
    /// how many quarter turns clockwise it is turned (0..3, Backpack Battles' turning). A turned shape is Height x Width.
    /// </summary>
    public readonly struct Placement : IEquatable<Placement>
    {
        public Placement(int x, int y, int turns = 0)
        {
            X = x;
            Y = y;
            Turns = ((turns % 4) + 4) % 4;
        }

        public int X { get; }
        public int Y { get; }

        /// <summary>Quarter turns clockwise, 0..3.</summary>
        public int Turns { get; }

        /// <summary>The squares across of a Width x Height shape lying this way.</summary>
        public int WidthOf(int width, int height)
        {
            return Turns % 2 == 0 ? width : height;
        }

        public int HeightOf(int width, int height)
        {
            return Turns % 2 == 0 ? height : width;
        }

        public bool Equals(Placement other)
        {
            return X == other.X && Y == other.Y && Turns == other.Turns;
        }

        public override bool Equals(object obj)
        {
            return obj is Placement other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (X * 31 + Y) * 4 + Turns;
        }

        public override string ToString()
        {
            return $"({X},{Y}) turned {Turns}";
        }
    }

    /// <summary>An item lying on a board. Merging and the camp's upkeep change its <see cref="Item"/>; moving it changes <see cref="At"/>.</summary>
    public sealed class BoardItem
    {
        public BoardItem(EquippedItem item, Placement at)
        {
            Item = item ?? throw new ArgumentNullException(nameof(item));
            At = at;
        }

        public EquippedItem Item { get; set; }
        public Placement At { get; set; }

        public int Width => At.WidthOf(Item.Item.Width, Item.Item.Height);
        public int Height => At.HeightOf(Item.Item.Width, Item.Item.Height);

        public bool Covers(int x, int y)
        {
            return x >= At.X && x < At.X + Width && y >= At.Y && y < At.Y + Height;
        }
    }

    /// <summary>A bag lying in a board's frame. Its squares are where items can lie.</summary>
    public sealed class BoardBag
    {
        public BoardBag(BagData bag, Placement at)
        {
            Bag = bag ?? throw new ArgumentNullException(nameof(bag));
            At = at;
        }

        public BagData Bag { get; }
        public Placement At { get; set; }

        public int Width => At.WidthOf(Bag.Width, Bag.Height);
        public int Height => At.HeightOf(Bag.Width, Bag.Height);

        public bool Covers(int x, int y)
        {
            return x >= At.X && x < At.X + Width && y >= At.Y && y < At.Y + Height;
        }
    }

    /// <summary>
    /// A mercenary's item board (Slice B stage 19, Docs/Design/02_Combat_System.md §4, 03_Dungeon_Structure.md §5): bags lying in the
    /// <see cref="BoardFrame"/> without overlapping, and items lying on the bags' squares without overlapping (an item may lie across
    /// two bags). The order of <see cref="Items"/> means nothing: the battle reads them in reading order (<see cref="InReadingOrder"/>).
    /// This type answers questions of squares; what may be done is <see cref="ExpeditionRules"/>'.
    /// </summary>
    public sealed class ItemBoard
    {
        public List<BoardBag> Bags = new List<BoardBag>();
        public List<BoardItem> Items = new List<BoardItem>();

        /// <summary>A board as an expedition leaves: the start bag at the top-left and the base weapon at its top-left, both unturned.</summary>
        public static ItemBoard Starting(BagData startBag, EquippedItem baseWeapon)
        {
            var board = new ItemBoard();
            board.Bags.Add(new BoardBag(startBag, new Placement(0, 0)));
            board.Items.Add(new BoardItem(baseWeapon, new Placement(0, 0)));
            return board;
        }

        /// <summary>The item covering a square, or null.</summary>
        public BoardItem ItemAt(int x, int y)
        {
            foreach (BoardItem item in Items)
            {
                if (item.Covers(x, y))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>The bag covering a square, or null.</summary>
        public BoardBag BagAt(int x, int y)
        {
            foreach (BoardBag bag in Bags)
            {
                if (bag.Covers(x, y))
                {
                    return bag;
                }
            }

            return null;
        }

        /// <summary>Whether every square of a rectangle is inside the frame and on a bag.</summary>
        public bool OnBags(int x, int y, int width, int height)
        {
            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    if (!BoardFrame.Contains(x + dx, y + dy) || BagAt(x + dx, y + dy) == null)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>The items a rectangle overlaps, leaving one out (null for none). Each once, in the order of <see cref="Items"/>.</summary>
        public List<BoardItem> ItemsUnder(int x, int y, int width, int height, BoardItem except)
        {
            var under = new List<BoardItem>();
            foreach (BoardItem item in Items)
            {
                if (item != except && Overlap(item.At.X, item.At.Y, item.Width, item.Height, x, y, width, height))
                {
                    under.Add(item);
                }
            }

            return under;
        }

        /// <summary>Whether a rectangle is inside the frame and overlaps no bag, leaving one out (null for none).</summary>
        public bool FreeOfBags(int x, int y, int width, int height, BoardBag except)
        {
            if (!BoardFrame.Contains(x, y) || !BoardFrame.Contains(x + width - 1, y + height - 1))
            {
                return false;
            }

            foreach (BoardBag bag in Bags)
            {
                if (bag != except && Overlap(bag.At.X, bag.At.Y, bag.Width, bag.Height, x, y, width, height))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The items lying wholly inside a bag.</summary>
        public List<BoardItem> ItemsIn(BoardBag bag)
        {
            var inside = new List<BoardItem>();
            foreach (BoardItem item in Items)
            {
                if (Inside(item.At.X, item.At.Y, item.Width, item.Height, bag.At.X, bag.At.Y, bag.Width, bag.Height))
                {
                    inside.Add(item);
                }
            }

            return inside;
        }

        /// <summary>Whether an item lies partly in a bag and partly in another: such a bag cannot be moved apart from the other.</summary>
        public bool HasItemAcross(BoardBag bag)
        {
            foreach (BoardItem item in Items)
            {
                if (Overlap(item.At.X, item.At.Y, item.Width, item.Height, bag.At.X, bag.At.Y, bag.Width, bag.Height)
                    && !Inside(item.At.X, item.At.Y, item.Width, item.Height, bag.At.X, bag.At.Y, bag.Width, bag.Height))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// The items in reading order: by the row of their top-left square, then its column. This is the battle's activation order
        /// (Docs/Design/02_Combat_System.md §4): the board of a battle is this list.
        /// </summary>
        public List<BoardItem> PlacedInReadingOrder()
        {
            var ordered = new List<BoardItem>(Items);
            ordered.Sort((a, b) => a.At.Y != b.At.Y ? a.At.Y.CompareTo(b.At.Y) : a.At.X.CompareTo(b.At.X));
            return ordered;
        }

        public List<EquippedItem> InReadingOrder()
        {
            var items = new List<EquippedItem>();
            foreach (BoardItem placed in PlacedInReadingOrder())
            {
                items.Add(placed.Item);
            }

            return items;
        }

        /// <summary>
        /// The first placement, in reading order of its top-left square and then unturned before turned, where a shape lies on bags
        /// over no item. False when there is none. The simulator's policies and the 9 -> 10 save conversion place with it.
        /// </summary>
        public bool FindRoom(int width, int height, out Placement at)
        {
            for (int y = 0; y < BoardFrame.Height; y++)
            {
                for (int x = 0; x < BoardFrame.Width; x++)
                {
                    for (int turns = 0; turns < 2; turns++)
                    {
                        var spot = new Placement(x, y, turns);
                        int w = spot.WidthOf(width, height);
                        int h = spot.HeightOf(width, height);
                        if (OnBags(x, y, w, h) && ItemsUnder(x, y, w, h, null).Count == 0)
                        {
                            at = spot;
                            return true;
                        }

                        if (width == height)
                        {
                            break;
                        }
                    }
                }
            }

            at = default;
            return false;
        }

        /// <summary>The first placement, in reading order and unturned before turned, where a bag's shape lies in the frame over no bag.</summary>
        public bool FindBagRoom(int width, int height, out Placement at)
        {
            for (int y = 0; y < BoardFrame.Height; y++)
            {
                for (int x = 0; x < BoardFrame.Width; x++)
                {
                    for (int turns = 0; turns < 2; turns++)
                    {
                        var spot = new Placement(x, y, turns);
                        if (FreeOfBags(x, y, spot.WidthOf(width, height), spot.HeightOf(width, height), null))
                        {
                            at = spot;
                            return true;
                        }

                        if (width == height)
                        {
                            break;
                        }
                    }
                }
            }

            at = default;
            return false;
        }

        internal static bool Overlap(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
        {
            return ax < bx + bw && bx < ax + aw && ay < by + bh && by < ay + ah;
        }

        static bool Inside(int ax, int ay, int aw, int ah, int bx, int by, int bw, int bh)
        {
            return ax >= bx && ay >= by && ax + aw <= bx + bw && ay + ah <= by + bh;
        }
    }

    /// <summary>
    /// The expedition's inventory (round 49, Docs/Design/03_Dungeon_Structure.md §5: Diablo II's): a grid of squares, its size the balance
    /// data's, where items lie at their placements without overlapping. Read as a list it gives the items in the order they came in (the
    /// order of <see cref="Items"/>): an inventory index is an index of both. This type answers questions of squares; what may be done is
    /// <see cref="ExpeditionRules"/>'.
    /// </summary>
    public sealed class InventoryGrid : IReadOnlyList<EquippedItem>
    {
        public InventoryGrid(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(width), $"An inventory of {width} x {height} squares.");
            }

            Width = width;
            Height = height;
        }

        public int Width { get; }
        public int Height { get; }
        public List<BoardItem> Items = new List<BoardItem>();

        public int Count => Items.Count;
        public EquippedItem this[int index] => Items[index].Item;

        public bool Contains(int x, int y)
        {
            return x >= 0 && x < Width && y >= 0 && y < Height;
        }

        /// <summary>The item covering a square, or null.</summary>
        public BoardItem ItemAt(int x, int y)
        {
            foreach (BoardItem item in Items)
            {
                if (item.Covers(x, y))
                {
                    return item;
                }
            }

            return null;
        }

        /// <summary>Whether a rectangle lies inside the grid over no item, leaving one out (null for none).</summary>
        public bool IsFree(int x, int y, int width, int height, BoardItem except)
        {
            if (!Contains(x, y) || !Contains(x + width - 1, y + height - 1))
            {
                return false;
            }

            foreach (BoardItem item in Items)
            {
                if (item != except && ItemBoard.Overlap(item.At.X, item.At.Y, item.Width, item.Height, x, y, width, height))
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// The first placement where a shape lies over no item (leaving one out: null for none): unturned, in reading order of its top-left
        /// square; only when it fits nowhere so, turned. False when there is none. Where the buttons and a displaced item put an item.
        /// </summary>
        public bool FindRoom(int width, int height, BoardItem except, out Placement at)
        {
            for (int turns = 0; turns < (width == height ? 1 : 2); turns++)
            {
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        var spot = new Placement(x, y, turns);
                        if (IsFree(x, y, spot.WidthOf(width, height), spot.HeightOf(width, height), except))
                        {
                            at = spot;
                            return true;
                        }
                    }
                }
            }

            at = default;
            return false;
        }

        public bool HasRoomFor(ItemData item)
        {
            return FindRoom(item.Width, item.Height, null, out _);
        }

        /// <summary>Lays an item at the first room (<see cref="FindRoom"/>). The caller checked there is one.</summary>
        public void Add(EquippedItem item)
        {
            if (!FindRoom(item.Item.Width, item.Item.Height, null, out Placement at))
            {
                throw new InvalidOperationException($"No room in the inventory for {item.Item.Id}.");
            }

            Items.Add(new BoardItem(item, at));
        }

        /// <summary>Lays an item at a placement. The caller checked it is free.</summary>
        public void Add(EquippedItem item, Placement at)
        {
            if (!IsFree(at.X, at.Y, at.WidthOf(item.Item.Width, item.Item.Height), at.HeightOf(item.Item.Width, item.Item.Height), null))
            {
                throw new InvalidOperationException($"{at} of the inventory is not free for {item.Item.Id}.");
            }

            Items.Add(new BoardItem(item, at));
        }

        public void RemoveAt(int index)
        {
            Items.RemoveAt(index);
        }

        /// <summary>How many squares the items cover.</summary>
        public int UsedSquares
        {
            get
            {
                int used = 0;
                foreach (BoardItem item in Items)
                {
                    used += item.Width * item.Height;
                }

                return used;
            }
        }

        public IEnumerator<EquippedItem> GetEnumerator()
        {
            foreach (BoardItem item in Items)
            {
                yield return item.Item;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }

    /// <summary>
    /// How a battle unit's board is laid out, for the screen only (the battle reads the items in order and ignores this): where each item
    /// of <see cref="BattleUnitSetup.Items"/> lies (same order), and the bags. An enemy has no bags: its items stand one under another.
    /// </summary>
    public sealed class BoardLayout
    {
        public BoardLayout(IReadOnlyList<Placement> items, IReadOnlyList<BoardBag> bags)
        {
            Items = items ?? throw new ArgumentNullException(nameof(items));
            Bags = bags ?? throw new ArgumentNullException(nameof(bags));
        }

        public IReadOnlyList<Placement> Items { get; }
        public IReadOnlyList<BoardBag> Bags { get; }

        /// <summary>A mercenary's board: its items in reading order (the battle's order) and copies of its bags.</summary>
        public static BoardLayout Of(ItemBoard board)
        {
            var items = new List<Placement>();
            foreach (BoardItem placed in board.PlacedInReadingOrder())
            {
                items.Add(placed.At);
            }

            var bags = new List<BoardBag>();
            foreach (BoardBag bag in board.Bags)
            {
                bags.Add(new BoardBag(bag.Bag, bag.At));
            }

            return new BoardLayout(items, bags);
        }

        /// <summary>An enemy's board: its items one under another from the top-left, unturned.</summary>
        public static BoardLayout Stacked(IReadOnlyList<EquippedItem> items)
        {
            var placements = new List<Placement>();
            int y = 0;
            foreach (EquippedItem item in items)
            {
                placements.Add(new Placement(0, y));
                y += item.Item.Height;
            }

            return new BoardLayout(placements, new List<BoardBag>());
        }
    }
}
