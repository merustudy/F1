using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>How the merchant shows a good: on sale, held by the hand (bought where it is put), or beyond what the party can buy now.</summary>
    public enum MerchantGoodState
    {
        OnSale,
        Picked,
        Unaffordable,
    }

    /// <summary>A good of the merchant as drawn: what it is (an item, a bag or a potion), where it lies, its price and its state.</summary>
    public sealed class MerchantPiece
    {
        public int Slot;
        public Placement At;
        public int Width;
        public int Height;
        public EquippedItem Item;
        public BagData Bag;
        public Sprite Icon;
        public int Price;
        public MerchantGoodState State;
    }

    /// <summary>
    /// The merchant's grid (Slice B stage 21, Diablo II's merchant; Docs/Architecture/12_UI.md "상인"): black squares with grey lines
    /// between, the goods on them as pieces at their own size (an item, a bag's well, a potion; the held one lighter, one that cannot be
    /// bought now dark red: round 57), and each one's price in small gold at its piece's bottom-right. The squares take the pointer and
    /// raise the grid's events for the good lying there; an empty square (a good bought) raises nothing.
    /// </summary>
    public sealed class MerchantGridView : MonoBehaviour
    {
        /// <summary>The grid's squares across, and down at the least (more rows are added when the goods need them): 10 x 8 (round 56).</summary>
        public const int Width = 10;
        public const int Height = 8;

        /// <summary>The columns the goods lie in (round 56): the first eight; the ninth stays empty and the potions lie down the tenth.</summary>
        public const int GoodsColumns = 8;
        public const int PotionColumn = 9;

        /// <summary>The bags' row (round 58): the last; the row above it stays empty and the items lie in the rows above that.</summary>
        public const int BagRow = Height - 1;
        public const int ItemRows = BagRow - 1;

        /// <summary>The stone rim round the squares (the view's own rect is the rim; its layers lie inside it).</summary>
        public const float Rim = 6f;
        const float PricePad = 3f;

        [SerializeField] RectTransform _squareLayer;
        [SerializeField] RectTransform _pieceLayer;
        [SerializeField] RectTransform _priceLayer;
        [SerializeField] GridSquareView _squareTemplate;
        [SerializeField] ItemSlotView _pieceTemplate;
        [SerializeField] TMP_Text _priceTemplate;

        readonly List<GridSquareView> _squares = new List<GridSquareView>();
        readonly List<ItemSlotView> _pieces = new List<ItemSlotView>();
        readonly List<TMP_Text> _prices = new List<TMP_Text>();
        readonly Dictionary<int, int> _shownIndex = new Dictionary<int, int>();
        readonly List<MerchantPiece> _shown = new List<MerchantPiece>();
        int _rows;

        public event Action<int> GoodClicked;
        public event Action<int> GoodEntered;
        public event Action<int> GoodExited;

        /// <summary>The rows on show.</summary>
        public int Rows => _rows;

        /// <summary>The goods on show, by slot (a good bought is not among them).</summary>
        public IEnumerable<int> ShownSlots => _shownIndex.Keys;

        /// <summary>The piece of a good on show, or null.</summary>
        public ItemSlotView PieceOf(int slot)
        {
            return _shownIndex.TryGetValue(slot, out int i) ? _pieces[i] : null;
        }

        /// <summary>The price label of a good on show, or null.</summary>
        public TMP_Text PriceLabelOf(int slot)
        {
            return _shownIndex.TryGetValue(slot, out int i) ? _prices[i] : null;
        }

        /// <summary>Where a good on show lies, or null.</summary>
        public Placement? PlacementOf(int slot)
        {
            return _shownIndex.TryGetValue(slot, out int i) ? _shown[i].At : (Placement?)null;
        }

        /// <summary>The square at the top-left of a good on show (where a test presses it), or null.</summary>
        public GridSquareView SquareOf(int slot)
        {
            Placement? at = PlacementOf(slot);
            return at.HasValue ? SquareAt(at.Value.X, at.Value.Y) : null;
        }

        public GridSquareView SquareAt(int x, int y)
        {
            return _squares[y * Width + x];
        }

        /// <summary>The whole grid's size for so many rows, the rim included.</summary>
        public static Vector2 Size(int rows)
        {
            return new Vector2(GridGeometry.Span(Width), GridGeometry.Span(rows)) + 2f * Rim * Vector2.one;
        }

        /// <param name="pieces">The goods to draw, at their places.</param>
        /// <param name="rows">The rows the goods take (at the least <see cref="Height"/>).</param>
        public void Show(IReadOnlyList<MerchantPiece> pieces, int rows, ExpeditionArt art)
        {
            EnsureSquares(Math.Max(rows, Height));
            ((RectTransform)transform).sizeDelta = Size(_rows);
            _shown.Clear();
            _shownIndex.Clear();
            for (int i = 0; i < pieces.Count; i++)
            {
                MerchantPiece good = pieces[i];
                if (i == _pieces.Count)
                {
                    _pieces.Add(Instantiate(_pieceTemplate, _pieceLayer));
                    _prices.Add(Instantiate(_priceTemplate, _priceLayer));
                }

                ItemSlotView piece = _pieces[i];
                piece.gameObject.SetActive(true);
                GridGeometry.Lay(piece.Rect, good.At.X, good.At.Y, good.Width, good.Height);
                bool picked = good.State == MerchantGoodState.Picked;
                if (good.Item != null)
                {
                    piece.ShowItem(good.Item, good.Icon, default, picked, 0, false);
                }
                else if (good.Bag != null)
                {
                    piece.ShowBag(good.Bag, picked);
                }
                else
                {
                    piece.ShowPotion(good.Icon);
                }

                piece.ShowUnaffordable(good.State == MerchantGoodState.Unaffordable);

                TMP_Text price = _prices[i];
                price.gameObject.SetActive(true);
                Vector2 corner = GridGeometry.Corner(good.At.X, good.At.Y);
                RectTransform rect = price.rectTransform;
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(1f, 0f);
                rect.anchoredPosition = new Vector2(corner.x + GridGeometry.Span(good.Width) - PricePad, corner.y - GridGeometry.Span(good.Height) + PricePad - 1f);
                price.text = good.Price.ToString(System.Globalization.CultureInfo.InvariantCulture);
                price.color = UiPalette.DiabloGold;

                _shownIndex[good.Slot] = i;
                _shown.Add(good);
            }

            for (int i = pieces.Count; i < _pieces.Count; i++)
            {
                _pieces[i].gameObject.SetActive(false);
                _prices[i].gameObject.SetActive(false);
            }
        }

        /// <summary>The good lying on a square, or -1.</summary>
        int SlotAt(int x, int y)
        {
            foreach (MerchantPiece good in _shown)
            {
                if (x >= good.At.X && x < good.At.X + good.Width && y >= good.At.Y && y < good.At.Y + good.Height)
                {
                    return good.Slot;
                }
            }

            return -1;
        }

        /// <summary>Makes the grid's squares for so many rows (more are added when needed), wired to the grid's events.</summary>
        void EnsureSquares(int rows)
        {
            while (_squares.Count < rows * Width)
            {
                int index = _squares.Count;
                GridSquareView square = Instantiate(_squareTemplate, _squareLayer);
                square.X = index % Width;
                square.Y = index / Width;
                GridGeometry.Lay((RectTransform)square.transform, square.X, square.Y, 1, 1);
                square.Show(GridSquareLook.Empty);
                square.Clicked += (x, y) => Raise(GoodClicked, x, y);
                square.Entered += (x, y) => Raise(GoodEntered, x, y);
                square.Exited += (x, y) => Raise(GoodExited, x, y);
                _squares.Add(square);
            }

            for (int i = 0; i < _squares.Count; i++)
            {
                _squares[i].gameObject.SetActive(i < rows * Width);
            }

            _rows = rows;
        }

        void Raise(Action<int> handler, int x, int y)
        {
            int slot = SlotAt(x, y);
            if (slot >= 0)
            {
                handler?.Invoke(slot);
            }
        }
    }
}
