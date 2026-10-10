using System;
using System.Collections.Generic;
using F1.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The inventory popup's grid (round 49, Docs/Design/03_Dungeon_Structure.md §5: Diablo II's inventory; Docs/Architecture/12_UI.md
    /// "인벤토리 팝업"): the inventory's squares (black, grey lines between them), its items as dark blue pieces where they lie (the lines kept
    /// and the board's blue, round 53), and the held item's ghost where the pointer is as one block (round 53; green where it can go, red where it
    /// cannot). Round 54 (Diablo II): the held item is lifted off the grid and drawn on the pointer, so the ghost is the colour only. Its squares are a board's (<see cref="Square"/>,
    /// round 52); the pieces and the ghost are the board's. The squares take the pointer and raise the grid's events.
    /// </summary>
    public sealed class InventoryGridView : MonoBehaviour
    {
        /// <summary>A square of the inventory: a board's (round 52, "B안": the window lies over the stage next to the boards; round 49 had 66).</summary>
        public const float Square = GridGeometry.Square;

        /// <summary>The stone rim round the squares (the view's own rect is the rim; its layers lie inside it).</summary>
        public const float Rim = 6f;

        const float GhostAlpha = 0.45f;

        [SerializeField] RectTransform _squareLayer;
        [SerializeField] RectTransform _pieceLayer;
        [SerializeField] RectTransform _ghostLayer;
        [SerializeField] GridSquareView _squareTemplate;
        [SerializeField] ItemSlotView _pieceTemplate;
        [SerializeField] Image _ghostFill;
        [SerializeField] Image _ghostFloor;
        [SerializeField] RectTransform _ghostArt;
        [SerializeField] Image _ghostIcon;

        readonly List<GridSquareView> _squares = new List<GridSquareView>();
        readonly List<ItemSlotView> _pieces = new List<ItemSlotView>();
        int _width;
        int _height;
        int _piecesShown;

        public event Action<int, int> SquareClicked;
        public event Action<int, int> SquareRightClicked;
        public event Action<int, int> SquareEntered;
        public event Action<int, int> SquareExited;

        /// <summary>The pieces on show, one per item, in inventory order.</summary>
        public IEnumerable<ItemSlotView> Pieces
        {
            get
            {
                for (int i = 0; i < _piecesShown; i++)
                {
                    yield return _pieces[i];
                }
            }
        }

        /// <summary>The ghost's kind on show, or null while there is none.</summary>
        public GhostKind? GhostShown { get; private set; }

        /// <summary>The rect the squares are laid in (round 54: where the pointer is read against).</summary>
        public RectTransform SquareArea => _squareLayer;

        /// <summary>The ghost's block on show, or null.</summary>
        public RectTransform GhostBlock => _ghostFill.gameObject.activeSelf ? _ghostFill.rectTransform : null;

        /// <summary>The whole grid's size for a grid of so many squares across and down.</summary>
        public static Vector2 Size(int width, int height)
        {
            return new Vector2(Span(width), Span(height));
        }

        public GridSquareView SquareAt(int x, int y)
        {
            return _squares[y * _width + x];
        }

        /// <summary>The piece covering a square, or null.</summary>
        public ItemSlotView PieceAt(int x, int y)
        {
            for (int i = 0; i < _piecesShown; i++)
            {
                ItemSlotView piece = _pieces[i];
                int width = piece.At.WidthOf(piece.Item.Item.Width, piece.Item.Item.Height);
                int height = piece.At.HeightOf(piece.Item.Item.Width, piece.Item.Item.Height);
                if (x >= piece.At.X && x < piece.At.X + width && y >= piece.At.Y && y < piece.At.Y + height)
                {
                    return piece;
                }
            }

            return null;
        }

        /// <param name="picked">The inventory index held now, or -1: lifted off the grid (round 54), its squares empty.</param>
        /// <param name="fatigueCost">What an item adds to its owner's fatigue when a battle starts (its tag).</param>
        /// <param name="ghost">The held item's ghost on the inventory, or null.</param>
        public void Show(InventoryGrid grid, ExpeditionArt art, int picked, Func<EquippedItem, int> fatigueCost, GridGhost ghost)
        {
            EnsureSquares(grid.Width, grid.Height);
            ((RectTransform)transform).sizeDelta = Size(grid.Width, grid.Height) + 2f * Rim * Vector2.one;
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    BoardItem there = grid.ItemAt(x, y);
                    bool lifted = there != null && picked >= 0 && picked < grid.Items.Count && there == grid.Items[picked];
                    _squares[y * _width + x].Show(there != null && !lifted ? GridSquareLook.Hidden : GridSquareLook.Empty);
                }
            }

            _piecesShown = 0;
            for (int i = 0; i < grid.Items.Count; i++)
            {
                BoardItem item = grid.Items[i];
                if (i == picked)
                {
                    continue;
                }

                if (_piecesShown == _pieces.Count)
                {
                    _pieces.Add(Instantiate(_pieceTemplate, _pieceLayer));
                }

                ItemSlotView piece = _pieces[_piecesShown++];
                piece.gameObject.SetActive(true);
                Lay(piece.Rect, item.At.X, item.At.Y, item.Width, item.Height);
                piece.ShowItem(item.Item, art?.OfItem(item.Item.Item.Id), item.At, false, fatigueCost(item.Item), false);
            }

            for (int i = _piecesShown; i < _pieces.Count; i++)
            {
                _pieces[i].gameObject.SetActive(false);
            }

            ShowGhost(ghost);
        }

        void ShowGhost(GridGhost ghost)
        {
            GhostShown = ghost?.Kind;
            // Round 54: where it already lay is empty now, so putting it back there shows green as any free place does.
            bool coloured = ghost != null;

            // Round 53 ("초록 배경은 격자 구분없이"): the squares inside the grid as one block, and among the squares their black over the lines
            // under it so that none shows through; a piece under it still shows.
            int x0 = coloured ? Mathf.Max(ghost.At.X, 0) : 0;
            int y0 = coloured ? Mathf.Max(ghost.At.Y, 0) : 0;
            int x1 = coloured ? Mathf.Min(ghost.At.X + ghost.Width, _width) : 0;
            int y1 = coloured ? Mathf.Min(ghost.At.Y + ghost.Height, _height) : 0;
            bool block = x1 > x0 && y1 > y0;
            _ghostFill.gameObject.SetActive(block);
            _ghostFloor.gameObject.SetActive(block);
            if (block)
            {
                Color colour = ghost.Kind == GhostKind.Refused ? UiPalette.GhostRefused : UiPalette.GhostFits;
                Lay(_ghostFill.rectTransform, x0, y0, x1 - x0, y1 - y0);
                _ghostFill.color = new Color(colour.r, colour.g, colour.b, GhostAlpha);
                Lay(_ghostFloor.rectTransform, x0, y0, x1 - x0, y1 - y0);
                _ghostFloor.transform.SetAsLastSibling();
            }

            // Round 54 (Diablo II): the colour only; the held item's icon is on the pointer.
            _ghostArt.gameObject.SetActive(false);
        }

        /// <summary>Makes the grid's squares once for its size, wired to the grid's events.</summary>
        void EnsureSquares(int width, int height)
        {
            if (_squares.Count > 0 && width == _width && height == _height)
            {
                return;
            }

            foreach (GridSquareView old in _squares)
            {
                Destroy(old.gameObject);
            }

            _squares.Clear();
            _width = width;
            _height = height;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    GridSquareView square = Instantiate(_squareTemplate, _squareLayer);
                    square.gameObject.SetActive(true);
                    square.X = x;
                    square.Y = y;
                    Lay((RectTransform)square.transform, x, y, 1, 1);
                    square.Clicked += (sx, sy) => SquareClicked?.Invoke(sx, sy);
                    square.RightClicked += (sx, sy) => SquareRightClicked?.Invoke(sx, sy);
                    square.Entered += (sx, sy) => SquareEntered?.Invoke(sx, sy);
                    square.Exited += (sx, sy) => SquareExited?.Invoke(sx, sy);
                    _squares.Add(square);
                }
            }
        }

        static float Span(int squares)
        {
            return squares <= 0 ? 0f : squares * Square + (squares - 1) * GridGeometry.Gap;
        }

        static Vector2 Corner(int x, int y)
        {
            return new Vector2(x * (Square + GridGeometry.Gap), -y * (Square + GridGeometry.Gap));
        }

        static void Lay(RectTransform rect, int x, int y, int width, int height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Corner(x, y);
            rect.sizeDelta = new Vector2(Span(width), Span(height));
        }
    }
}
