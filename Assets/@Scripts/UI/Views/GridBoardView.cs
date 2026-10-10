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
    /// only while a bag is held), the items as dark blue pieces over their squares (the lines between them kept, round 53), and the held
    /// thing's ghost where the pointer is: its squares one block (round 53), green where it can go (the one item it would push out turns dark
    /// gold), red where it cannot; since round 54 (Diablo II) only that colour and its words, the held thing itself lifted off the board and
    /// drawn on the pointer (<see cref="HeldPointerView"/>). The squares take the pointer
    /// and raise the board's events; the pieces and the ghost take none.
    /// </summary>
    public sealed class GridBoardView : MonoBehaviour
    {
        const float GhostAlpha = 0.45f;
        const float BagPad = GridGeometry.BagRim;
        const float LabelHeight = 22f;
        const float LabelPadX = 6f;

        [SerializeField] RectTransform _grid;
        [SerializeField] RectTransform _bagLayer;
        [SerializeField] RectTransform _squareLayer;
        [SerializeField] RectTransform _pieceLayer;
        [SerializeField] RectTransform _starLayer;
        [SerializeField] RectTransform _ghostLayer;
        [SerializeField] Image _bagTemplate;
        [SerializeField] GridSquareView _squareTemplate;
        [SerializeField] ItemSlotView _pieceTemplate;
        [SerializeField] GridStarView _starTemplate;
        [SerializeField] Image _ghostFill;
        [SerializeField] Image _ghostFloor;
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
        readonly List<GridStarView> _stars = new List<GridStarView>();
        int _piecesShown;
        int _starsShown;

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

        /// <summary>The stars on show (Slice B stage 20).</summary>
        public IEnumerable<GridStarView> Stars
        {
            get
            {
                for (int i = 0; i < _starsShown; i++)
                {
                    yield return _stars[i];
                }
            }
        }

        /// <summary>The star on show on a square, or null.</summary>
        public GridStarView StarAt(int x, int y)
        {
            for (int i = 0; i < _starsShown; i++)
            {
                if (_stars[i].X == x && _stars[i].Y == y)
                {
                    return _stars[i];
                }
            }

            return null;
        }

        /// <summary>How many bags show.</summary>
        public int BagsShown { get; private set; }

        /// <summary>The ghost's kind on show, or null while there is none.</summary>
        public GhostKind? GhostShown { get; private set; }

        /// <summary>The rect the squares are laid in (round 54: where the pointer is read against).</summary>
        public RectTransform SquareArea => _squareLayer;

        /// <summary>Whether the ghost draws the held thing itself (an icon, a bag's rim): never since round 54, where it rides the pointer.</summary>
        public bool GhostShowsTheHeldThing => _ghostArt.gameObject.activeSelf || _ghostBag.gameObject.activeSelf;

        /// <summary>The ghost's block on show, or null.</summary>
        public RectTransform GhostBlock => _ghostFill.gameObject.activeSelf ? _ghostFill.rectTransform : null;

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

        /// <param name="picked">The board's item chosen now, or null: lifted off the board while held (<paramref name="lifted"/>), else its piece is the lighter one (the camp's item to mend).</param>
        /// <param name="pickedBag">The board's bag held now, or null; lifted off with the items in it while held.</param>
        /// <param name="fatigueCost">What an item adds to its owner's fatigue when a battle starts (its tag).</param>
        /// <param name="merges">Whether the held item would merge into an item of the board (its piece is marked); null for none.</param>
        /// <param name="ghost">The held thing's ghost on this board, or null.</param>
        /// <param name="showFrame">Whether the frame's squares outside the bags show: while a bag is held.</param>
        /// <param name="stars">The stars to draw on their squares (Slice B stage 20: the held star item's, or the one the pointer is on), or null for none.</param>
        /// <param name="lifted">Whether the picked item or bag is in the hand (round 54): not drawn, its squares as if it were not there.</param>
        public void Show(ItemBoard board, ExpeditionArt art, BoardItem picked, BoardBag pickedBag, Func<EquippedItem, int> fatigueCost,
            Func<BoardItem, bool> merges, GridGhost ghost, bool showFrame, IReadOnlyList<StarMark> stars = null, bool lifted = false)
        {
            EnsureSquares();
            BoardBag liftedBag = lifted ? pickedBag : null;
            var liftedItems = new HashSet<BoardItem>();
            if (lifted && picked != null)
            {
                liftedItems.Add(picked);
            }

            if (liftedBag != null)
            {
                liftedItems.UnionWith(board.ItemsIn(liftedBag));
            }

            ShowBags(board, pickedBag, liftedBag);

            for (int y = 0; y < BoardFrame.Height; y++)
            {
                for (int x = 0; x < BoardFrame.Width; x++)
                {
                    BoardBag bag = board.BagAt(x, y);
                    BoardItem item = board.ItemAt(x, y);
                    GridSquareLook look = bag == null || bag == liftedBag ? (showFrame ? GridSquareLook.Frame : GridSquareLook.Hidden)
                        : item != null && !liftedItems.Contains(item) ? GridSquareLook.Hidden : GridSquareLook.Empty;
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
                if (liftedItems.Contains(item))
                {
                    continue;
                }

                ItemSlotView piece = NextPiece();
                GridGeometry.Lay(piece.Rect, item.At.X, item.At.Y, item.Width, item.Height);
                piece.ShowItem(item.Item, art?.OfItem(item.Item.Item.Id), item.At, item == picked, fatigueCost(item.Item), merges != null && merges(item),
                    displaced != null && displaced.Contains(item));
            }

            for (int i = _piecesShown; i < _pieces.Count; i++)
            {
                _pieces[i].gameObject.SetActive(false);
            }

            ShowStars(stars);
            ShowGhost(ghost);
        }

        void ShowStars(IReadOnlyList<StarMark> marks)
        {
            _starsShown = 0;
            if (marks != null)
            {
                foreach (StarMark mark in marks)
                {
                    if (_starsShown == _stars.Count)
                    {
                        _stars.Add(Instantiate(_starTemplate, _starLayer));
                    }

                    _stars[_starsShown++].Show(mark.X, mark.Y, mark.Lit);
                }
            }

            for (int i = _starsShown; i < _stars.Count; i++)
            {
                _stars[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// The ghost's squares inside the frame as one block of its colour (round 53, "초록 배경은 격자 구분없이"), and under the pieces the
        /// squares' black over the same squares so that no line shows through it; a piece under it (one it would push out, one it would merge
        /// into) still shows.
        /// </summary>
        void ShowGhostBlock(GridGhost ghost)
        {
            int x0 = ghost == null ? 0 : Mathf.Max(ghost.At.X, 0);
            int y0 = ghost == null ? 0 : Mathf.Max(ghost.At.Y, 0);
            int x1 = ghost == null ? 0 : Mathf.Min(ghost.At.X + ghost.Width, BoardFrame.Width);
            int y1 = ghost == null ? 0 : Mathf.Min(ghost.At.Y + ghost.Height, BoardFrame.Height);
            bool shown = x1 > x0 && y1 > y0;
            _ghostFill.gameObject.SetActive(shown);
            _ghostFloor.gameObject.SetActive(shown);
            if (!shown)
            {
                return;
            }

            Color colour = ghost.Kind == GhostKind.Refused ? UiPalette.GhostRefused : UiPalette.GhostFits;
            GridGeometry.Lay(_ghostFill.rectTransform, x0, y0, x1 - x0, y1 - y0);
            _ghostFill.color = new Color(colour.r, colour.g, colour.b, GhostAlpha);
            GridGeometry.Lay(_ghostFloor.rectTransform, x0, y0, x1 - x0, y1 - y0);
            _ghostFloor.transform.SetAsLastSibling();
        }

        /// <param name="lifted">The bag in the hand (round 54), not drawn; or null.</param>
        void ShowBags(ItemBoard board, BoardBag picked, BoardBag lifted)
        {
            while (_bags.Count < board.Bags.Count)
            {
                Image bag = Instantiate(_bagTemplate, _bagLayer);
                _bags.Add(bag);
            }

            int drawn = 0;
            for (int i = 0; i < _bags.Count; i++)
            {
                bool shown = i < board.Bags.Count && board.Bags[i] != lifted;
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

                drawn++;
            }

            BagsShown = drawn;
        }

        void ShowGhost(GridGhost ghost)
        {
            GhostShown = ghost?.Kind;
            // Round 54: where it already lay is empty now, so putting it back there shows green as any free place does.
            bool coloured = ghost != null;
            ShowGhostBlock(coloured ? ghost : null);
            if (coloured)
            {
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

            // Round 54 (Diablo II, "그 칸에 초록색 배경색만"): the colour and the words only; the held thing (an item's icon, a bag's rim)
            // is on the pointer, not here.
            _ghostEdge.gameObject.SetActive(false);
            _ghostBag.gameObject.SetActive(false);
            _ghostArt.gameObject.SetActive(false);
            _ghostLabelPlate.SetAsLastSibling();
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
