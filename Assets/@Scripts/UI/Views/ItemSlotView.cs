using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// An item's piece on a board grid (Slice B stage 19; round 49's Diablo II look, Docs/Architecture/12_UI.md "격자 보드"): the item's
    /// squares dark blue one by one with the grid's grey line between them (round 53; the held one a lighter blue, the one a held item would
    /// push out a dark gold), its icon over the lines turned as
    /// the item lies, and over it the tier's stars in Diablo's rarity colours at the bottom-left, the violet "+1" of fatigue at the top-right
    /// for equipment that costs fatigue when a battle starts (round 32; a dot on a piece of one square), and the merge mark with the tier a
    /// merge would make (round 35). An item without an icon shows its name. A piece takes no pointer: the squares under it do. The
    /// merchant's grid shows its goods with the same view, a bag as Diablo's well (its rim and black squares).
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
        [SerializeField] SquareGrid _ground;
        [SerializeField] Image _picked;
        [SerializeField] RectTransform _art;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _text;
        [SerializeField] GameObject _fatigue;
        [SerializeField] TMP_Text _fatigueText;
        [SerializeField] Image _tierTag;
        [SerializeField] Image _glow;
        [SerializeField] Image[] _pips;
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

        /// <summary>Whether a merchant's good shows as one that cannot be bought now (<see cref="ShowUnaffordable"/>).</summary>
        public bool IsUnaffordable { get; private set; }

        /// <summary>The piece's ground colour on show: its squares' for an item, the rim's for a bag.</summary>
        public Color Ground => _ground.enabled ? _ground.color : _frame.color;

        /// <summary>The piece's rim: a bag's goes dark red when it cannot be bought (<see cref="ShowUnaffordable"/>).</summary>
        public Color Rim => _frame.color;

        /// <summary>The ground's squares across and down (an item's as it lies; round 53: the grid's line between them).</summary>
        public Vector2Int GroundSquares => new Vector2Int(_ground.Columns, _ground.Rows);

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

        /// <summary>How many pips the tier tag shows (the tier's stars); 0 while it is hidden.</summary>
        public int Pips { get; private set; }

        /// <summary>The pips' colour (the tier's one colour, round 69), or null while none show.</summary>
        public Color? PipColor => Pips > 0 ? _pips[0].color : (Color?)null;

        /// <summary>Whether the gold glow of a unique item (stage 23) shows behind its icon.</summary>
        public bool UniqueGlowShown => _glow.enabled;

        /// <summary>The words of the merge mark, or an empty string while it is hidden.</summary>
        public string MergeMark => _merge.activeSelf ? _mergeText.text : string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        void Awake()
        {
            _fatigueSize = ((RectTransform)_fatigue.transform).sizeDelta;
        }

        /// <param name="icon">The item's icon, or null when it has none.</param>
        /// <param name="at">Where it lies: the turn of its icon (the piece's size is set by whoever lays it).</param>
        /// <param name="fatigueCost">What the item adds to its owner's fatigue when a battle starts; 0 hides the tag.</param>
        /// <param name="merges">Whether the held item would merge into this one: the piece is marked with the tier the merge makes.</param>
        /// <param name="displaced">Whether a held item put where its ghost is would push this one out to the inventory.</param>
        /// <param name="glow">The gold glow behind a unique item's icon (stage 23), or null.</param>
        public void ShowItem(EquippedItem item, Sprite icon, Placement at, bool picked, int fatigueCost, bool merges, bool displaced = false, Sprite glow = null)
        {
            Item = item;
            Bag = null;
            At = at;
            _bagWell.SetActive(false);
            _frame.color = UiPalette.GridSquareLine;
            _ground.enabled = true;
            _ground.color = picked ? UiPalette.GridPiecePicked : displaced ? UiPalette.GridPieceDisplaced : UiPalette.GridPiece;
            _ground.Shape(at.WidthOf(item.Item.Width, item.Item.Height), at.HeightOf(item.Item.Width, item.Item.Height), GridGeometry.Gap);
            _frameLine.enabled = false;
            _picked.enabled = picked;
            IsDisplaced = displaced;

            bool pictured = icon != null;
            TierShown = merges ? item.Tier + 1 : item.Tier > ItemTier.Common ? item.Tier : (ItemTier?)null;
            _merge.SetActive(merges);
            if (merges)
            {
                _mergeText.text = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(TierShown.Value));
                _mergeText.color = UiPalette.Rarity(TierShown.Value);
            }

            LayArt(at.Turns);
            _icon.sprite = icon;
            _icon.preserveAspect = true;
            _icon.enabled = pictured;
            _icon.color = Color.white;
            ShowGlow(glow);
            ShowTier(TierShown, pictured);

            _text.enabled = !pictured;
            _text.text = pictured ? string.Empty : UiText.Name(item.Item.Name);
            _text.color = TierShown.HasValue ? UiPalette.Rarity(TierShown.Value) : UiPalette.Text;

            ShowFatigue(fatigueCost);
        }

        /// <summary>
        /// A bag on a tile: Diablo's well (round 49), its stone rim tinted by its leather round its black squares, nothing on them. A bag
        /// the hand holds from the merchant (stage 21) has the gold line round it.
        /// </summary>
        /// <param name="icon">The bag's picture (round 79), or null: then the well. With a picture the tile is the leather's colour under the picture, drawn like an item's icon.</param>
        public void ShowBag(BagData bag, Sprite icon = null, bool picked = false)
        {
            Item = null;
            Bag = bag;
            At = default;
            IsDisplaced = false;
            bool pictured = icon != null;
            _frame.color = UiPalette.BagRim(bag.Id);
            _ground.enabled = pictured;
            _ground.color = UiPalette.BagRim(bag.Id);
            _ground.Shape(bag.Width, bag.Height, GridGeometry.Gap);
            _bagWell.SetActive(!pictured);
            _bagSquares.Shape(bag.Width, bag.Height, GridGeometry.Gap);
            _frameLine.enabled = picked;
            _frameLine.effectColor = UiPalette.Virtue;
            // The picture fills the piece edge to edge (2026-10-17 "여백 0"): the bag's own outline is the tile's edge.
            LayArt(0);
            _art.sizeDelta = Rect.sizeDelta;
            _icon.sprite = icon;
            _icon.preserveAspect = false;
            _icon.color = Color.white;
            _picked.enabled = false;
            _merge.SetActive(false);
            _icon.enabled = pictured;
            _text.enabled = false;
            TierShown = null;
            Pips = 0;
            _tierTag.gameObject.SetActive(false);
            _glow.enabled = false;
            _fatigue.SetActive(false);
        }

        /// <summary>
        /// A potion among the merchant's goods (stage 21): one square of the boards' blue with its icon, no tier, no fatigue. Without an icon
        /// (stage 46: a potion before its icon round) the square carries the potion's name, like an item without a picture.
        /// </summary>
        public void ShowPotion(PotionData potion, Sprite icon)
        {
            Item = null;
            Bag = null;
            At = default;
            IsDisplaced = false;
            _bagWell.SetActive(false);
            _frame.color = UiPalette.GridSquareLine;
            _ground.enabled = true;
            _ground.color = UiPalette.GridPiece;
            _ground.Shape(1, 1, GridGeometry.Gap);
            _frameLine.enabled = false;
            _picked.enabled = false;
            _merge.SetActive(false);
            LayArt(0);
            _icon.preserveAspect = true;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _icon.color = Color.white;
            _text.enabled = icon == null && potion != null;
            _text.text = _text.enabled ? UiText.Name(potion.Name) : string.Empty;
            _text.color = UiPalette.Text;
            TierShown = null;
            Pips = 0;
            _tierTag.gameObject.SetActive(false);
            _glow.enabled = false;
            _fatigue.SetActive(false);
        }

        /// <summary>
        /// A merchant's good the party cannot buy now (stage 21: beyond its coins, a potion without an empty slot): since round 57 its squares
        /// (a bag: its rim) a dark red. Called after the good is shown.
        /// </summary>
        public void ShowUnaffordable(bool unaffordable)
        {
            IsUnaffordable = unaffordable;
            if (!unaffordable)
            {
                return;
            }

            if (Bag != null)
            {
                _frame.color = UiPalette.GridPieceUnaffordable;
            }
            else
            {
                _ground.color = UiPalette.GridPieceUnaffordable;
            }
        }

        /// <summary>The icon's box inside the piece, turned as the item lies: drawn for the unturned item, so its box swaps sides on an odd turn.</summary>
        /// <summary>
        /// The gold glow of a unique item (stage 23) behind its icon: the icon's silhouette widened 2px, drawn by the art pipeline, laid
        /// over the icon at the scale the icon is shown at so that the two coincide. Always as bright; it spills a little over the piece.
        /// </summary>
        void ShowGlow(Sprite glow)
        {
            _glow.enabled = glow != null && _icon.enabled;
            if (_glow.enabled)
            {
                _glow.sprite = glow;
                GridGeometry.LayGlow(_glow.rectTransform, _art.sizeDelta, _icon.sprite, glow);
            }
        }

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
        /// The tier's pips at the bottom-left in the tier's colour on no pill (round 69; round 41 stars, round 49 Diablo's colours, R12 took the
        /// icon's outline parts); nothing for Common. An item without an icon shows its words in the rarity colour instead.
        /// </summary>
        void ShowTier(ItemTier? tier, bool pictured)
        {
            Pips = tier.HasValue ? TierStyle.Pips(tier.Value) : 0;
            _tierTag.gameObject.SetActive(Pips > 0);
            if (Pips > 0)
            {
                _tierTag.color = Color.clear;
                _tierTag.rectTransform.sizeDelta = new Vector2(TierStyle.TagWidth(Pips), TierStyle.TagHeight);
                for (int i = 0; i < _pips.Length; i++)
                {
                    _pips[i].enabled = i < Pips;
                    _pips[i].color = UiPalette.Rarity(tier.Value);
                }
            }
        }
    }
}
