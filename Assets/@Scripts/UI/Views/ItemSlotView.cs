using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// An item's piece on a board grid (Slice B stage 19; round 49's Diablo II look, Docs/Architecture/12_UI.md "격자 보드"): the item's
    /// squares as one piece of dark blue (the held one a lighter blue, the one a held item would push out a dark gold), its icon turned as
    /// the item lies, and over it the tier's stars in Diablo's rarity colours at the bottom-left, the violet "+1" of fatigue at the top-right
    /// for equipment that costs fatigue when a battle starts (round 32; a dot on a piece of one square), and the merge mark with the tier a
    /// merge would make (round 35). An item without an icon shows its name. A piece takes no pointer: the squares under it do. A shop's or
    /// the loot's tile shows its item, or a bag as Diablo's well (its rim and black squares), with the same view.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        /// <summary>The icon's margin inside its piece, on every side.</summary>
        public const float ArtMargin = 5f;

        /// <summary>A piece no bigger than this (one square) shows the fatigue tag as a dot without words.</summary>
        const float DotPiece = GridGeometry.Square + 1f;
        const float FatigueDot = 12f;

        [SerializeField] Image _frame;
        [SerializeField] Outline _frameLine;
        [SerializeField] Image _picked;
        [SerializeField] RectTransform _art;
        [SerializeField] Image _outline;
        [SerializeField] SilhouetteOutline _outlineEffect;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _text;
        [SerializeField] GameObject _fatigue;
        [SerializeField] TMP_Text _fatigueText;
        [SerializeField] Image _tierTag;
        [SerializeField] Image[] _stars;
        [SerializeField] GameObject _merge;
        [SerializeField] TMP_Text _mergeText;
        [SerializeField] GameObject _bagWell;
        [SerializeField] SquareGrid _bagSquares;

        Vector2 _fatigueSize;

        /// <summary>The item shown, or null for a bag.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The bag shown (a shop's or the loot's tile), or null.</summary>
        public BagData Bag { get; private set; }

        /// <summary>Where the item lies on its board (the default on a tile).</summary>
        public Placement At { get; private set; }

        /// <summary>Whether the piece is the picked one.</summary>
        public bool IsPicked => _picked.enabled;

        /// <summary>Whether the piece shows that a held item would push it out (its dark gold).</summary>
        public bool IsDisplaced { get; private set; }

        /// <summary>The piece's ground colour on show.</summary>
        public Color Ground => _frame.color;

        /// <summary>The icon on show, or null while words stand in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The icon's turn on show, in quarters clockwise.</summary>
        public int ArtTurns => Mathf.RoundToInt(-_art.localEulerAngles.z / 90f + 4f) % 4;

        /// <summary>The words on the fatigue tag, or an empty string while it is hidden or a dot.</summary>
        public string FatigueTag => _fatigue.activeSelf && _fatigueText.enabled ? _fatigueText.text : string.Empty;

        /// <summary>Whether the fatigue tag shows (as words or a dot).</summary>
        public bool ShowsFatigue => _fatigue.activeSelf;

        /// <summary>The tier the piece marks (the item's above Common, or the tier a merge into it would make), or null while it marks none.</summary>
        public ItemTier? TierShown { get; private set; }

        /// <summary>How many stars the tier tag shows; 0 while it is hidden.</summary>
        public int Stars { get; private set; }

        /// <summary>The stars' colour (Diablo's rarity colour, round 49), or null while none show.</summary>
        public Color? StarColor => Stars > 0 ? _stars[0].color : (Color?)null;

        /// <summary>The outline's colour, or null while the icon has no outline (no tier, or no icon to outline).</summary>
        public Color? OutlineColor => _outline.enabled ? _outline.color : (Color?)null;

        /// <summary>The outline's thickness on show; 0 while there is none.</summary>
        public float OutlineThickness => _outline.enabled ? _outlineEffect.Thickness : 0f;

        /// <summary>The words of the merge mark, or an empty string while it is hidden.</summary>
        public string MergeMark => _merge.activeSelf ? _mergeText.text : string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        void Awake()
        {
            _fatigueSize = ((RectTransform)_fatigue.transform).sizeDelta;
        }

        /// <summary>Sizes the piece to so many squares across and down (gaps included).</summary>
        public void SetSquares(int width, int height)
        {
            Rect.sizeDelta = new Vector2(GridGeometry.Span(width), GridGeometry.Span(height));
        }

        /// <param name="icon">The item's icon, or null when it has none.</param>
        /// <param name="at">Where it lies: the turn of its icon (the piece's size is set by whoever lays it).</param>
        /// <param name="fatigueCost">What the item adds to its owner's fatigue when a battle starts; 0 hides the tag.</param>
        /// <param name="merges">Whether the held item would merge into this one: the piece is marked with the tier the merge makes.</param>
        /// <param name="displaced">Whether a held item put where its ghost is would push this one out to the inventory.</param>
        public void ShowItem(EquippedItem item, Sprite icon, Placement at, bool picked, int fatigueCost, bool merges, bool displaced = false)
        {
            Item = item;
            Bag = null;
            At = at;
            _bagWell.SetActive(false);
            _frame.color = picked ? UiPalette.GridPiecePicked : displaced ? UiPalette.GridPieceDisplaced : UiPalette.GridPiece;
            _frameLine.enabled = false;
            _picked.enabled = picked;
            IsDisplaced = displaced;

            bool pictured = icon != null;
            TierShown = merges ? item.Tier + 1 : item.Tier > ItemTier.Common ? item.Tier : (ItemTier?)null;
            _merge.SetActive(merges);
            if (merges)
            {
                _mergeText.text = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(TierShown.Value));
                _mergeText.color = UiPalette.TierText(TierShown.Value);
            }

            LayArt(at.Turns);
            _icon.sprite = icon;
            _icon.enabled = pictured;
            ShowTier(TierShown, pictured);

            _text.enabled = !pictured;
            _text.text = pictured ? string.Empty : UiText.Name(item.Item.Name);
            _text.color = TierShown.HasValue ? UiPalette.Rarity(TierShown.Value) : UiPalette.Text;

            ShowFatigue(fatigueCost);
        }

        /// <summary>A bag on a tile: Diablo's well (round 49), its stone rim tinted by its leather round its black squares, nothing on them.</summary>
        public void ShowBag(BagData bag)
        {
            Item = null;
            Bag = bag;
            At = default;
            IsDisplaced = false;
            _frame.color = UiPalette.BagRim(bag.Id);
            _bagWell.SetActive(true);
            _bagSquares.Shape(bag.Width, bag.Height, GridGeometry.Gap);
            _frameLine.enabled = false;
            _picked.enabled = false;
            _merge.SetActive(false);
            _icon.enabled = false;
            _outline.enabled = false;
            _text.enabled = false;
            TierShown = null;
            Stars = 0;
            _tierTag.gameObject.SetActive(false);
            _fatigue.SetActive(false);
        }

        /// <summary>The icon's box inside the piece, turned as the item lies: drawn for the unturned item, so its box swaps sides on an odd turn.</summary>
        void LayArt(int turns)
        {
            _art.anchorMin = new Vector2(0.5f, 0.5f);
            _art.anchorMax = new Vector2(0.5f, 0.5f);
            _art.pivot = new Vector2(0.5f, 0.5f);
            _art.anchoredPosition = Vector2.zero;
            _art.sizeDelta = GridGeometry.ArtBox(Rect.sizeDelta, turns, ArtMargin);
            _art.localRotation = GridGeometry.ArtTurn(turns);
        }

        /// <summary>The fatigue tag at the top-right: "+1" words, or on a piece of one square a dot without words.</summary>
        void ShowFatigue(int fatigueCost)
        {
            _fatigue.SetActive(fatigueCost > 0);
            if (fatigueCost <= 0)
            {
                return;
            }

            bool dot = Rect.sizeDelta.x <= DotPiece && Rect.sizeDelta.y <= DotPiece;
            var tag = (RectTransform)_fatigue.transform;
            // Round 49: Diablo's look has no pill, only the violet words (a dot keeps its violet).
            Image pill = _fatigue.GetComponent<Image>();
            if (pill != null)
            {
                pill.color = dot ? UiPalette.Fatigue : Color.clear;
            }

            if (_fatigueSize == Vector2.zero)
            {
                _fatigueSize = tag.sizeDelta;
            }

            tag.sizeDelta = dot ? new Vector2(FatigueDot, FatigueDot) : _fatigueSize;
            _fatigueText.enabled = !dot;
            _fatigueText.text = UiStrings.Get(UiKeys.Board.FatigueTag, fatigueCost);
        }

        /// <summary>
        /// The tier's stars at the bottom-left in Diablo II's rarity colours on no pill, and no outline round the icon (round 49; round 41
        /// had both); nothing for Common. An item without an icon shows its words in the rarity colour instead.
        /// </summary>
        void ShowTier(ItemTier? tier, bool pictured)
        {
            Stars = tier.HasValue ? TierStyle.Stars(tier.Value) : 0;
            _outline.enabled = false;
            _tierTag.gameObject.SetActive(Stars > 0);
            if (Stars > 0)
            {
                _tierTag.color = Color.clear;
                _tierTag.rectTransform.sizeDelta = new Vector2(TierStyle.TagWidth(Stars), TierStyle.TagHeight);
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].enabled = i < Stars;
                    _stars[i].color = UiPalette.Rarity(tier.Value);
                }
            }
        }
    }
}
