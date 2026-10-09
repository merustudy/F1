using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>What a held thing would do where the pointer is (Slice B stage 19, Docs/Architecture/12_UI.md "격자 보드"; round 49's colours).</summary>
    public enum GhostKind
    {
        /// <summary>It lies there over nothing: green.</summary>
        Fits,

        /// <summary>It lies there over one item, which goes to the inventory: green, that item's piece dark gold.</summary>
        Displaces,

        /// <summary>It merges into the same item at its top-left square: green, with the words.</summary>
        Merges,

        /// <summary>It cannot go there: red.</summary>
        Refused,

        /// <summary>It is where it already lies: no colour, putting it down there lets it go.</summary>
        Same,
    }

    /// <summary>Where a held thing would land on a member's board while the pointer is over the board.</summary>
    public sealed class GridGhost
    {
        public Placement At;

        /// <summary>The squares it covers there, turned.</summary>
        public int Width;
        public int Height;
        public GhostKind Kind;

        /// <summary>The words over it, or null.</summary>
        public string Label;

        /// <summary>The held item's icon (null for a bag or an item without one), drawn for its unturned shape.</summary>
        public Sprite Icon;
        public int ItemWidth;
        public int ItemHeight;

        /// <summary>The held bag, or null for an item.</summary>
        public BagData Bag;
    }

    /// <summary>
    /// Something held that is on no board yet (Slice B stage 19): a drop of a won battle's loot or an offer of a shop. The screen that
    /// owns it says where it can go and puts it there; the hand only aims it.
    /// </summary>
    public interface IOutsideHand
    {
        /// <summary>The item, or null for a bag.</summary>
        EquippedItem Item { get; }

        /// <summary>The bag, or null for an item.</summary>
        BagData Bag { get; }

        bool CanPlace(int member, Placement at);
        bool MergesAt(int member, int x, int y);
        void Place(int member, Placement at);
    }

    /// <summary>
    /// The hand over the party's boards between battles (Slice B stage 19; Docs/Architecture/12_UI.md "격자 보드"): what it holds (an item
    /// of a board, a bag of a board, an item of the inventory, or something from outside), how it is turned (Backpack Battles' turning: a
    /// quarter clockwise at a time while held), where the pointer is (over a board or over the inventory's grid, round 49), and what a
    /// click on a square does. The node map's party side and the battle screen after a win both aim with it. It asks the manager
    /// everything and decides nothing itself.
    /// </summary>
    public sealed class BoardHand
    {
        int _hoverMember = -1;
        int _hoverX;
        int _hoverY;
        bool _hoverInventory;
        int _inventoryX;
        int _inventoryY;

        /// <summary>The member whose board item is held, or -1; the item lies from (<see cref="ItemX"/>, <see cref="ItemY"/>).</summary>
        public int ItemMember { get; private set; } = -1;
        public int ItemX { get; private set; }
        public int ItemY { get; private set; }

        /// <summary>The member whose bag is held, or -1; the bag lies from (<see cref="BagX"/>, <see cref="BagY"/>).</summary>
        public int BagMember { get; private set; } = -1;
        public int BagX { get; private set; }
        public int BagY { get; private set; }

        /// <summary>The inventory entry held, or -1.</summary>
        public int InventoryIndex { get; private set; } = -1;

        /// <summary>The thing from outside the boards held, or null.</summary>
        public IOutsideHand Outside { get; private set; }

        /// <summary>Quarter turns clockwise of what is held.</summary>
        public int Turns { get; private set; }

        public bool Holding => ItemMember >= 0 || BagMember >= 0 || InventoryIndex >= 0 || Outside != null;

        /// <summary>The member whose board the pointer is over, or -1.</summary>
        public int HoverMember => _hoverMember;

        public void Clear()
        {
            ItemMember = -1;
            BagMember = -1;
            InventoryIndex = -1;
            Outside = null;
            Turns = 0;
        }

        /// <param name="turns">How the item lies in the inventory: it is held turned so.</param>
        public void HoldInventory(int index, int turns = 0)
        {
            Clear();
            InventoryIndex = index;
            Turns = ((turns % 4) + 4) % 4;
        }

        public void HoldOutside(IOutsideHand outside)
        {
            Clear();
            Outside = outside;
        }

        /// <summary>Turns what is held a quarter clockwise (step 1) or anticlockwise (step -1). Nothing held, nothing turns.</summary>
        public void Turn(int step)
        {
            if (Holding)
            {
                Turns = ((Turns + step) % 4 + 4) % 4;
            }
        }

        public void Hover(int member, int x, int y)
        {
            _hoverInventory = false;
            _hoverMember = member;
            _hoverX = x;
            _hoverY = y;
        }

        public void Unhover(int member, int x, int y)
        {
            if (_hoverMember == member && _hoverX == x && _hoverY == y)
            {
                _hoverMember = -1;
            }
        }

        /// <summary>Whether the pointer is over the inventory's grid (round 49).</summary>
        public bool OverInventory => _hoverInventory;

        public void HoverInventory(int x, int y)
        {
            _hoverMember = -1;
            _hoverInventory = true;
            _inventoryX = x;
            _inventoryY = y;
        }

        public void UnhoverInventory(int x, int y)
        {
            if (_hoverInventory && _inventoryX == x && _inventoryY == y)
            {
                _hoverInventory = false;
            }
        }

        /// <summary>
        /// Where the held item would lie on the inventory's grid with the pointer over a square: that square as its top-left, pulled into
        /// the grid. False when no item is held (a bag never goes there) or the inventory item held is gone.
        /// </summary>
        public bool InventoryAnchorAt(ExpeditionState expedition, int x, int y, out Placement at)
        {
            at = default;
            EquippedItem item = BagMember >= 0 || Outside?.Bag != null ? null : HeldItem(expedition);
            if (item == null)
            {
                return false;
            }

            var turned = new Placement(0, 0, Turns);
            int width = turned.WidthOf(item.Item.Width, item.Item.Height);
            int height = turned.HeightOf(item.Item.Width, item.Item.Height);
            InventoryGrid grid = expedition.Inventory;
            at = new Placement(Mathf.Clamp(x, 0, Mathf.Max(0, grid.Width - width)), Mathf.Clamp(y, 0, Mathf.Max(0, grid.Height - height)), Turns);
            return true;
        }

        /// <summary>
        /// Where and how the held item would land on the inventory's grid with the pointer over it (round 49): green where it can go (an
        /// item of a board or of the inventory, to free squares), red where it cannot (over another item; anything from outside, which goes
        /// there by its button). Null when the pointer is not over the grid or no item is held.
        /// </summary>
        public GridGhost InventoryGhost(ExpeditionManager manager, ExpeditionArt art)
        {
            ExpeditionState expedition = manager.Expedition;
            if (!_hoverInventory || expedition == null || !Holding || !InventoryAnchorAt(expedition, _inventoryX, _inventoryY, out Placement at))
            {
                return null;
            }

            EquippedItem item = HeldItem(expedition);
            var ghost = new GridGhost
            {
                At = at,
                Width = at.WidthOf(item.Item.Width, item.Item.Height),
                Height = at.HeightOf(item.Item.Width, item.Item.Height),
                Icon = art?.OfItem(item.Item.Id),
                ItemWidth = item.Item.Width,
                ItemHeight = item.Item.Height,
            };

            if (InventoryIndex >= 0 && expedition.Inventory.Items[InventoryIndex].At.Equals(at))
            {
                ghost.Kind = GhostKind.Same;
                return ghost;
            }

            bool can = InventoryIndex >= 0 ? manager.CanMoveInInventory(InventoryIndex, at)
                : ItemMember >= 0 && manager.CanMoveToInventoryAt(ItemMember, ItemX, ItemY, at);
            ghost.Kind = can ? GhostKind.Fits : GhostKind.Refused;
            return ghost;
        }

        /// <summary>
        /// A click on a square of the inventory's grid (round 49): with nothing held, picks up the item there; holding an item of a board or
        /// of the inventory, lays it where its ghost is when the squares are free (and lets go), or lets go when it is put back where it
        /// lies; anything else held stays held. Returns the sound the click makes.
        /// </summary>
        public SoundEffect ClickInventory(int x, int y, ExpeditionManager manager)
        {
            ExpeditionState expedition = manager.Expedition;
            if (!Holding)
            {
                BoardItem there = expedition.Inventory.ItemAt(x, y);
                if (there != null)
                {
                    HoldInventory(expedition.Inventory.Items.IndexOf(there), there.At.Turns);
                }

                return SoundEffect.Button;
            }

            if (!InventoryAnchorAt(expedition, x, y, out Placement at))
            {
                return SoundEffect.Button;
            }

            if (InventoryIndex >= 0)
            {
                if (expedition.Inventory.Items[InventoryIndex].At.Equals(at))
                {
                    Clear();
                    return SoundEffect.Button;
                }

                if (manager.CanMoveInInventory(InventoryIndex, at))
                {
                    manager.MoveInInventory(InventoryIndex, at);
                    Clear();
                    return SoundEffect.ItemPlace;
                }

                return SoundEffect.Button;
            }

            if (ItemMember >= 0 && manager.CanMoveToInventoryAt(ItemMember, ItemX, ItemY, at))
            {
                manager.MoveToInventoryAt(ItemMember, ItemX, ItemY, at);
                Clear();
                return SoundEffect.ItemPlace;
            }

            return SoundEffect.Button;
        }

        /// <summary>The board item held, or null.</summary>
        public BoardItem HeldBoardItem(ExpeditionState expedition)
        {
            return ItemMember >= 0 && ItemMember < expedition.Members.Count ? expedition.Members[ItemMember].Board.ItemAt(ItemX, ItemY) : null;
        }

        /// <summary>The board bag held, or null.</summary>
        public BoardBag HeldBag(ExpeditionState expedition)
        {
            return BagMember >= 0 && BagMember < expedition.Members.Count ? expedition.Members[BagMember].Board.BagAt(BagX, BagY) : null;
        }

        /// <summary>The item held (of a board, of the inventory or from outside), or null.</summary>
        public EquippedItem HeldItem(ExpeditionState expedition)
        {
            if (ItemMember >= 0)
            {
                return HeldBoardItem(expedition)?.Item;
            }

            if (InventoryIndex >= 0)
            {
                return InventoryIndex < expedition.Inventory.Count ? expedition.Inventory[InventoryIndex] : null;
            }

            return Outside?.Item;
        }

        /// <summary>The unturned squares of what is held across and down; false when nothing (or nothing left) is held.</summary>
        bool Shape(ExpeditionState expedition, out int width, out int height, out BagData bag)
        {
            bag = BagMember >= 0 ? HeldBag(expedition)?.Bag : Outside?.Bag;
            if (bag != null)
            {
                width = bag.Width;
                height = bag.Height;
                return true;
            }

            EquippedItem item = HeldItem(expedition);
            width = item?.Item.Width ?? 0;
            height = item?.Item.Height ?? 0;
            return item != null;
        }

        /// <summary>Where what is held would lie with the pointer over a square: that square as its top-left, pulled into the frame.</summary>
        public bool AnchorAt(ExpeditionState expedition, int x, int y, out Placement at)
        {
            at = default;
            if (!Shape(expedition, out int width, out int height, out _))
            {
                return false;
            }

            at = GridGeometry.Anchor(x, y, width, height, Turns);
            return true;
        }

        /// <summary>Where and how what is held would land on a member's board with the pointer over it; null when it is not over that board or nothing is held.</summary>
        public GridGhost GhostFor(int member, ExpeditionManager manager, ExpeditionArt art)
        {
            ExpeditionState expedition = manager.Expedition;
            if (_hoverMember != member || expedition == null || !Holding || !Shape(expedition, out int width, out int height, out BagData bag))
            {
                return null;
            }

            Placement at = GridGeometry.Anchor(_hoverX, _hoverY, width, height, Turns);
            var ghost = new GridGhost
            {
                At = at,
                Width = at.WidthOf(width, height),
                Height = at.HeightOf(width, height),
                Bag = bag,
                ItemWidth = width,
                ItemHeight = height,
            };

            if (bag != null)
            {
                BoardBag held = HeldBag(expedition);
                bool same = held != null && member == BagMember && held.At.Equals(at);
                bool fits = held != null ? manager.CanMoveBag(BagMember, BagX, BagY, member, at) : Outside.CanPlace(member, at);
                ghost.Kind = same ? GhostKind.Same : fits ? GhostKind.Fits : GhostKind.Refused;
                ghost.Label = fits ? UiStrings.Get(UiKeys.Board.GhostBag) : null;
                return ghost;
            }

            EquippedItem item = HeldItem(expedition);
            ghost.Icon = art?.OfItem(item.Item.Id);
            BoardItem moving = HeldBoardItem(expedition);
            if (moving != null && member == ItemMember && moving.At.Equals(at))
            {
                ghost.Kind = GhostKind.Same;
                return ghost;
            }

            bool merges = Outside != null ? Outside.MergesAt(member, at.X, at.Y) : manager.MergesAt(item, member, at.X, at.Y);
            if (merges)
            {
                ghost.Kind = GhostKind.Merges;
                ghost.Label = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(item.Tier + 1));
                return ghost;
            }

            bool can = moving != null ? manager.CanMoveItem(ItemMember, ItemX, ItemY, member, at)
                : InventoryIndex >= 0 ? manager.CanPlaceFromInventory(InventoryIndex, member, at)
                : Outside.CanPlace(member, at);
            if (!can)
            {
                ghost.Kind = GhostKind.Refused;
                return ghost;
            }

            var under = expedition.Members[member].Board.ItemsUnder(at.X, at.Y, ghost.Width, ghost.Height, member == ItemMember ? moving : null);
            ghost.Kind = under.Count == 0 ? GhostKind.Fits : GhostKind.Displaces;
            ghost.Label = under.Count == 0 ? null : UiStrings.Get(UiKeys.Board.GhostToInventory, UiText.Name(under[0].Item.Item.Name));
            return ghost;
        }

        /// <summary>
        /// A click on a square of a member's board: with nothing held, picks up the item there (or the bag, from an empty square of a bag
        /// that can move); with something held, puts it where its ghost is when it can go there (and lets go), or lets go when it is put
        /// back where it lies; a click where it cannot go keeps it held. Returns the sound the click makes: an item put in, or a click.
        /// </summary>
        public SoundEffect Click(int member, int x, int y, ExpeditionManager manager)
        {
            ExpeditionState expedition = manager.Expedition;
            if (!Holding)
            {
                ItemBoard board = expedition.Members[member].Board;
                if (manager.CanPickItem(member, x, y))
                {
                    BoardItem item = board.ItemAt(x, y);
                    ItemMember = member;
                    ItemX = item.At.X;
                    ItemY = item.At.Y;
                    Turns = item.At.Turns;
                }
                else if (manager.CanPickBag(member, x, y))
                {
                    BoardBag bag = board.BagAt(x, y);
                    BagMember = member;
                    BagX = bag.At.X;
                    BagY = bag.At.Y;
                    Turns = bag.At.Turns;
                }

                return SoundEffect.Button;
            }

            if (!AnchorAt(expedition, x, y, out Placement at))
            {
                Clear();
                return SoundEffect.Button;
            }

            if (ItemMember >= 0)
            {
                BoardItem held = HeldBoardItem(expedition);
                if (held == null || (member == ItemMember && held.At.Equals(at)))
                {
                    Clear();
                    return SoundEffect.Button;
                }

                if (manager.CanMoveItem(ItemMember, ItemX, ItemY, member, at))
                {
                    manager.MoveItem(ItemMember, ItemX, ItemY, member, at);
                    Clear();
                    return SoundEffect.ItemPlace;
                }

                return SoundEffect.Button;
            }

            if (BagMember >= 0)
            {
                BoardBag held = HeldBag(expedition);
                if (held == null || (member == BagMember && held.At.Equals(at)))
                {
                    Clear();
                    return SoundEffect.Button;
                }

                if (manager.CanMoveBag(BagMember, BagX, BagY, member, at))
                {
                    manager.MoveBag(BagMember, BagX, BagY, member, at);
                    Clear();
                    return SoundEffect.ItemPlace;
                }

                return SoundEffect.Button;
            }

            if (InventoryIndex >= 0)
            {
                if (manager.CanPlaceFromInventory(InventoryIndex, member, at))
                {
                    manager.PlaceFromInventory(InventoryIndex, member, at);
                    Clear();
                    return SoundEffect.ItemPlace;
                }

                return SoundEffect.Button;
            }

            if (Outside.CanPlace(member, at))
            {
                Outside.Place(member, at);
                Clear();
                return SoundEffect.ItemPlace;
            }

            return SoundEffect.Button;
        }
    }
}
