using System;
using System.Collections.Generic;
using F1.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The inventory popup's grid (round 49, Docs/Design/03_Dungeon_Structure.md §5: Diablo II's inventory; Docs/Architecture/12_UI.md
    /// "인벤토리 팝업"): the inventory's squares (black, grey lines between them), its items as dark blue pieces where they lie, and the held
    /// item's ghost where the pointer is (green where it can go, red where it cannot). Its squares are bigger than a board's
    /// (<see cref="Square"/>); the pieces and the ghost are the board's. The squares take the pointer and raise the grid's events.
    /// </summary>
    public sealed class InventoryGridView : MonoBehaviour
    {
        /// <summary>A square of the inventory: bigger than a board's 50, as round 49's mockup drew it.</summary>
        public const float Square = 66f;

        /// <summary>The stone rim round the squares (the view's own rect is the rim; its layers lie inside it).</summary>
        public const float Rim = 6f;

        const float GhostAlpha = 0.45f;
        const float GhostArtAlpha = 0.88f;

        [SerializeField] RectTransform _squareLayer;
        [SerializeField] RectTransform _pieceLayer;
        [SerializeField] RectTransform _ghostLayer;
        [SerializeField] GridSquareView _squareTemplate;
        [SerializeField] ItemSlotView _pieceTemplate;
        [SerializeField] Image _ghostSquareTemplate;
        [SerializeField] RectTransform _ghostArt;
        [SerializeField] Image _ghostIcon;

        readonly List<GridSquareView> _squares = new List<GridSquareView>();
        readonly List<ItemSlotView> _pieces = new List<ItemSlotView>();
        readonly List<Image> _ghostSquares = new List<Image>();
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

        /// <param name="picked">The inventory index held now, or -1; its piece is the lighter blue.</param>
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
                    _squares[y * _width + x].Show(grid.ItemAt(x, y) != null ? GridSquareLook.Hidden : GridSquareLook.Empty);
                }
            }

            _piecesShown = 0;
            for (int i = 0; i < grid.Items.Count; i++)
            {
                BoardItem item = grid.Items[i];
                if (_piecesShown == _pieces.Count)
                {
                    _pieces.Add(Instantiate(_pieceTemplate, _pieceLayer));
                }

                ItemSlotView piece = _pieces[_piecesShown++];
                piece.gameObject.SetActive(true);
                Lay(piece.Rect, item.At.X, item.At.Y, item.Width, item.Height);
                piece.ShowItem(item.Item, art?.OfItem(item.Item.Item.Id), item.At, i == picked, fatigueCost(item.Item), false);
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
            int shown = 0;
            bool coloured = ghost != null && ghost.Kind != GhostKind.Same;
            if (coloured)
            {
                Color colour = ghost.Kind == GhostKind.Refused ? UiPalette.GhostRefused : UiPalette.GhostFits;
                for (int dy = 0; dy < ghost.Height; dy++)
                {
                    for (int dx = 0; dx < ghost.Width; dx++)
                    {
                        int x = ghost.At.X + dx;
                        int y = ghost.At.Y + dy;
                        if (x < 0 || x >= _width || y < 0 || y >= _height)
                        {
                            continue;
                        }

                        if (shown == _ghostSquares.Count)
                        {
                            _ghostSquares.Add(Instantiate(_ghostSquareTemplate, _ghostLayer));
                        }

                        Image square = _ghostSquares[shown++];
                        square.gameObject.SetActive(true);
                        Lay(square.rectTransform, x, y, 1, 1);
                        square.color = new Color(colour.r, colour.g, colour.b, GhostAlpha);
                    }
                }
            }

            for (int i = shown; i < _ghostSquares.Count; i++)
            {
                _ghostSquares[i].gameObject.SetActive(false);
            }

            _ghostArt.SetAsLastSibling();
            bool icon = coloured && ghost.Icon != null;
            _ghostArt.gameObject.SetActive(icon);
            if (icon)
            {
                Vector2 piece = new Vector2(Span(ghost.Width), Span(ghost.Height));
                Vector2 corner = Corner(ghost.At.X, ghost.At.Y);
                _ghostArt.anchorMin = new Vector2(0f, 1f);
                _ghostArt.anchorMax = new Vector2(0f, 1f);
                _ghostArt.pivot = new Vector2(0.5f, 0.5f);
                _ghostArt.anchoredPosition = new Vector2(corner.x + piece.x / 2f, corner.y - piece.y / 2f);
                _ghostArt.sizeDelta = GridGeometry.ArtBox(piece, ghost.At.Turns, ItemSlotView.ArtMargin);
                _ghostArt.localRotation = GridGeometry.ArtTurn(ghost.At.Turns);
                _ghostIcon.sprite = ghost.Icon;
                _ghostIcon.color = new Color(1f, 1f, 1f, GhostArtAlpha);
            }
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
