using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A member's board as a grid between battles (Slice B stage 19; round 49's Diablo II look; Docs/Architecture/12_UI.md "격자 보드"):
    /// each bag a well (a sunk stone rim tinted by its leather, grey lines between black squares), nothing where no bag is (the frame shows
    /// only while a bag is held), the items as dark blue pieces over their squares, and the held thing's ghost where the pointer is: its
    /// squares green where it can go (the one item it would push out turns dark gold), red where it cannot. The squares take the pointer
    /// and raise the board's events; the pieces and the ghost take none.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        const float GhostAlpha = 0.45f;
        const float GhostArtAlpha = 0.88f;
        const float BagPad = GridGeometry.BagRim;
        const float LabelHeight = 22f;
        const float LabelPadX = 6f;

        [SerializeField] RectTransform _grid;
        [SerializeField] RectTransform _bagLayer;
        [SerializeField] RectTransform _squareLayer;
        [SerializeField] RectTransform _pieceLayer;
        [SerializeField] RectTransform _ghostLayer;
        [SerializeField] Image _bagTemplate;
        [SerializeField] GridSquareView _squareTemplate;
        [SerializeField] ItemSlotView _pieceTemplate;
        [SerializeField] Image _ghostSquareTemplate;
        [SerializeField] Image _ghostBag;
        [SerializeField] RectTransform _ghostArt;
        [SerializeField] Image _ghostIcon;
        [SerializeField] RectTransform _ghostLabelPlate;
        [SerializeField] TMP_Text _ghostLabel;
        [SerializeField] RectTransform _ghostEdge;
        [SerializeField] Image[] _ghostEdgeLines;

        readonly List<Image> _bags = new List<Image>();
        readonly List<GridSquareView> _squares = new List<GridSquareView>();
        readonly List<ItemSlotView> _pieces = new List<ItemSlotView>();
        readonly List<Image> _ghostSquares = new List<Image>();
        int _piecesShown;

        public event Action<int, int> SquareClicked;
        public event Action<int, int> SquareRightClicked;
        public event Action<int, int> SquareEntered;
        public event Action<int, int> SquareExited;

        /// <summary>The pieces on show, one per item, in the order of the board's items.</summary>
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

        /// <summary>How many bags show.</summary>
        public int BagsShown { get; private set; }

        /// <summary>The ghost's kind on show, or null while there is none.</summary>
        public GhostKind? GhostShown { get; private set; }

        /// <summary>The ghost's words on show, or an empty string.</summary>
        public string GhostLabel => _ghostLabelPlate.gameObject.activeSelf ? _ghostLabel.text : string.Empty;

        /// <summary>The square at a place of the frame (every square of the frame exists; outside the bags it shows nothing).</summary>
        public GridSquareView SquareAt(int x, int y)
        {
            EnsureSquares();
            return _squares[y * BoardFrame.Width + x];
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

        /// <param name="picked">The board's item held now, or null; its piece is the gold one.</param>
        /// <param name="pickedBag">The board's bag held now, or null; its leather has the gold line.</param>
        /// <param name="fatigueCost">What an item adds to its owner's fatigue when a battle starts (its tag).</param>
        /// <param name="merges">Whether the held item would merge into an item of the board (its piece is marked); null for none.</param>
        /// <param name="ghost">The held thing's ghost on this board, or null.</param>
        /// <param name="showFrame">Whether the frame's squares outside the bags show: while a bag is held.</param>
        public void Show(ItemBoard board, ExpeditionArt art, BoardItem picked, BoardBag pickedBag, Func<EquippedItem, int> fatigueCost,
            Func<BoardItem, bool> merges, GridGhost ghost, bool showFrame)
        {
            EnsureSquares();
            ShowBags(board, pickedBag);

            for (int y = 0; y < BoardFrame.Height; y++)
            {
                for (int x = 0; x < BoardFrame.Width; x++)
                {
                    GridSquareLook look = board.BagAt(x, y) == null ? (showFrame ? GridSquareLook.Frame : GridSquareLook.Hidden)
                        : board.ItemAt(x, y) != null ? GridSquareLook.Hidden : GridSquareLook.Empty;
                    _squares[y * BoardFrame.Width + x].Show(look);
                }
            }

            // The one item a held item would push out where its ghost is shows it (round 49: its piece dark gold).
            List<BoardItem> displaced = ghost != null && ghost.Kind == GhostKind.Displaces
                ? board.ItemsUnder(ghost.At.X, ghost.At.Y, ghost.Width, ghost.Height, picked)
                : null;
            _piecesShown = 0;
            foreach (BoardItem item in board.Items)
            {
                ItemSlotView piece = NextPiece();
                GridGeometry.Lay(piece.Rect, item.At.X, item.At.Y, item.Width, item.Height);
                piece.ShowItem(item.Item, art?.OfItem(item.Item.Item.Id), item.At, item == picked, fatigueCost(item.Item), merges != null && merges(item),
                    displaced != null && displaced.Contains(item));
            }

            for (int i = _piecesShown; i < _pieces.Count; i++)
            {
                _pieces[i].gameObject.SetActive(false);
            }

            ShowGhost(ghost);
        }

        void ShowBags(ItemBoard board, BoardBag picked)
        {
            while (_bags.Count < board.Bags.Count)
            {
                Image bag = Instantiate(_bagTemplate, _bagLayer);
                _bags.Add(bag);
            }

            for (int i = 0; i < _bags.Count; i++)
            {
                bool shown = i < board.Bags.Count;
                _bags[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                BoardBag bag = board.Bags[i];
                LayPadded(_bags[i].rectTransform, bag.At.X, bag.At.Y, bag.Width, bag.Height, BagPad);
                _bags[i].color = UiPalette.BagRim(bag.Bag.Id);

                Outline line = _bags[i].GetComponent<Outline>();
                if (line != null)
                {
                    line.effectColor = bag == picked ? UiPalette.Virtue : UiPalette.Ink;
                }
            }

            BagsShown = board.Bags.Count;
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
                        if (!BoardFrame.Contains(x, y))
                        {
                            continue;
                        }

                        if (shown == _ghostSquares.Count)
                        {
                            _ghostSquares.Add(Instantiate(_ghostSquareTemplate, _ghostLayer));
                        }

                        Image square = _ghostSquares[shown++];
                        square.gameObject.SetActive(true);
                        GridGeometry.Lay(square.rectTransform, x, y, 1, 1);
                        square.color = new Color(colour.r, colour.g, colour.b, GhostAlpha);
                    }
                }

                // Round 49 ("빨강 칠만"): the squares are filled and nothing goes round the shape.
                _ghostLabelPlate.gameObject.SetActive(!string.IsNullOrEmpty(ghost.Label));
                if (_ghostLabelPlate.gameObject.activeSelf)
                {
                    _ghostLabel.text = ghost.Label;
                    _ghostLabel.color = ghost.Kind == GhostKind.Refused ? UiPalette.GhostRefused : UiPalette.Text;
                    float width = Mathf.Ceil(_ghostLabel.GetPreferredValues(ghost.Label).x) + 2f * LabelPadX;
                    Vector2 corner = GridGeometry.Corner(ghost.At.X, ghost.At.Y);
                    float x = Mathf.Clamp(corner.x + GridGeometry.Span(ghost.Width) / 2f - width / 2f, -8f, GridGeometry.Width + 8f - width);
                    _ghostLabelPlate.anchorMin = new Vector2(0f, 1f);
                    _ghostLabelPlate.anchorMax = new Vector2(0f, 1f);
                    _ghostLabelPlate.pivot = new Vector2(0f, 0f);
                    _ghostLabelPlate.anchoredPosition = new Vector2(x, corner.y + 2f);
                    _ghostLabelPlate.sizeDelta = new Vector2(width, LabelHeight);
                }
            }
            else
            {
                _ghostLabelPlate.gameObject.SetActive(false);
            }

            for (int i = shown; i < _ghostSquares.Count; i++)
            {
                _ghostSquares[i].gameObject.SetActive(false);
            }

            // Drawn in this order: a held bag's rim, the squares, the icon, the words.
            _ghostEdge.gameObject.SetActive(false);
            _ghostBag.transform.SetAsFirstSibling();
            _ghostEdge.SetAsLastSibling();
            _ghostArt.SetAsLastSibling();
            _ghostLabelPlate.SetAsLastSibling();

            bool bag = coloured && ghost.Bag != null;
            _ghostBag.gameObject.SetActive(bag);
            if (bag)
            {
                LayPadded(_ghostBag.rectTransform, ghost.At.X, ghost.At.Y, ghost.Width, ghost.Height, BagPad);
                Color rim = UiPalette.BagRim(ghost.Bag.Id);
                _ghostBag.color = new Color(rim.r, rim.g, rim.b, GhostArtAlpha);
            }

            bool icon = coloured && ghost.Bag == null && ghost.Icon != null;
            _ghostArt.gameObject.SetActive(icon);
            if (icon)
            {
                Vector2 piece = new Vector2(GridGeometry.Span(ghost.Width), GridGeometry.Span(ghost.Height));
                Vector2 corner = GridGeometry.Corner(ghost.At.X, ghost.At.Y);
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

        ItemSlotView NextPiece()
        {
            if (_piecesShown == _pieces.Count)
            {
                _pieces.Add(Instantiate(_pieceTemplate, _pieceLayer));
            }

            ItemSlotView piece = _pieces[_piecesShown++];
            piece.gameObject.SetActive(true);
            return piece;
        }

        /// <summary>Makes the frame's squares once: every square of the frame, wired to the board's events.</summary>
        void EnsureSquares()
        {
            if (_squares.Count > 0)
            {
                return;
            }

            for (int y = 0; y < BoardFrame.Height; y++)
            {
                for (int x = 0; x < BoardFrame.Width; x++)
                {
                    GridSquareView square = Instantiate(_squareTemplate, _squareLayer);
                    square.gameObject.SetActive(true);
                    square.X = x;
                    square.Y = y;
                    GridGeometry.Lay((RectTransform)square.transform, x, y, 1, 1);
                    square.Clicked += (sx, sy) => SquareClicked?.Invoke(sx, sy);
                    square.RightClicked += (sx, sy) => SquareRightClicked?.Invoke(sx, sy);
                    square.Entered += (sx, sy) => SquareEntered?.Invoke(sx, sy);
                    square.Exited += (sx, sy) => SquareExited?.Invoke(sx, sy);
                    _squares.Add(square);
                }
            }
        }

        static void LayPadded(RectTransform rect, int x, int y, int width, int height, float pad)
        {
            GridGeometry.Lay(rect, x, y, width, height);
            rect.anchoredPosition += new Vector2(-pad, pad);
            rect.sizeDelta += new Vector2(2f * pad, 2f * pad);
        }
    }
}
