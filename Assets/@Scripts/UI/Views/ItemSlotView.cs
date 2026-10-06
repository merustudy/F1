using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item on a member's board (as high as the cells it takes, stacked as in battle) or
    /// one empty cell. An item shows its icon, as in battle; an item without an icon shows its
    /// name with the grade under it, smaller and dimmer; an empty cell says so. Equipment that
    /// costs fatigue when a battle starts has the cost on the violet tag at the top-right corner
    /// (round 32, B1). An item above Common wears its tier (2026-10-07 round 41): the outline around
    /// its icon in the tier's colour, thicker at a higher tier, and the tier tag with the tier's stars
    /// at the bottom-left corner; a cell the chosen item would merge into shows the tier the merge makes
    /// on a veil, with that tier's outline and stars (round 35). The chosen item's cell is the brass one.
    /// A cell that takes no click now is dimmed by its button. The grade badge of 2026-10-03 is gone
    /// (round 41): the grade is in the item's title. <see cref="Index"/> is the first cell the view covers.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        /// <summary>The grade's line inside the text: its size against the name's, and its color.</summary>
        static readonly string GradeLine = "\n<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(UiPalette.InkTextDim) + ">{0}</color></size>";

        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;
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

        public Button Button => _button;

        /// <summary>The item shown, or null for an empty cell.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The first cell of the board the view covers.</summary>
        public int Index { get; set; }

        /// <summary>The icon on show, or null while words stand in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The words on the fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTag => _fatigue.activeSelf ? _fatigueText.text : string.Empty;

        /// <summary>The tier the cell marks (the item's above Common, or the tier a merge into it would make), or null while it marks none.</summary>
        public ItemTier? TierShown { get; private set; }

        /// <summary>How many stars the tier tag shows; 0 while it is hidden.</summary>
        public int Stars { get; private set; }

        /// <summary>The outline's colour, or null while the icon has no outline (no tier, or no icon to outline).</summary>
        public Color? OutlineColor => _outline.enabled ? _outline.color : (Color?)null;

        /// <summary>The outline's thickness on show; 0 while there is none.</summary>
        public float OutlineThickness => _outline.enabled ? _outlineEffect.Thickness : 0f;

        /// <summary>The words of the merge mark, or an empty string while it is hidden.</summary>
        public string MergeMark => _merge.activeSelf ? _mergeText.text : string.Empty;

        /// <param name="icon">The item's icon, or null when it has none or the cell is empty.</param>
        /// <param name="fatigueCost">What the item adds to its owner's fatigue when a battle starts; 0 hides the tag.</param>
        /// <param name="merges">Whether the chosen item would merge into this one: the cell is marked with the tier the merge makes.</param>
        public void Show(EquippedItem item, Sprite icon, bool selected, bool interactable, int fatigueCost, bool merges)
        {
            _fatigue.SetActive(item != null && fatigueCost > 0);
            _fatigueText.text = _fatigue.activeSelf ? UiStrings.Get(UiKeys.Board.FatigueTag, fatigueCost) : string.Empty;

            Item = item;
            bool pictured = item != null && icon != null;
            merges &= item != null;

            // The tier the cell marks: the item's own above Common, or the one a merge into it would make (round 35), over the veil and its words.
            TierShown = item == null ? (ItemTier?)null : merges ? item.Tier + 1 : item.Tier > ItemTier.Common ? item.Tier : (ItemTier?)null;
            _merge.SetActive(merges);
            if (merges)
            {
                _mergeText.text = UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(TierShown.Value));
                _mergeText.color = UiPalette.TierText(TierShown.Value);
            }

            _icon.sprite = icon;
            _icon.enabled = pictured;
            ShowTier(TierShown, pictured);

            _text.enabled = !pictured;
            _text.text = item == null
                ? UiStrings.Get(UiKeys.Board.EmptySlot)
                : pictured ? string.Empty : UiText.Name(item.Item.Name) + string.Format(GradeLine, UiStrings.Get(UiKeys.Board.Grade, item.Grade));
            _text.color = item == null ? UiPalette.InkTextDim : TierShown.HasValue && !pictured ? UiPalette.TierMark(TierShown.Value) : UiPalette.InkText;

            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }

        /// <summary>
        /// The outline behind the icon and the tier tag with its stars, in the tier's colours; nothing for Common (round 41).
        /// An item without an icon has nothing to outline: its words take the tier's colour instead.
        /// </summary>
        void ShowTier(ItemTier? tier, bool pictured)
        {
            Stars = tier.HasValue ? TierStyle.Stars(tier.Value) : 0;
            _outline.enabled = tier.HasValue && pictured;
            if (_outline.enabled)
            {
                _outline.sprite = _icon.sprite;
                _outline.color = UiPalette.TierMark(tier.Value);
                _outlineEffect.Thickness = TierStyle.Outline(tier.Value);
            }

            _tierTag.gameObject.SetActive(Stars > 0);
            if (Stars > 0)
            {
                _tierTag.color = UiPalette.TierMark(tier.Value);
                _tierTag.rectTransform.sizeDelta = new Vector2(TierStyle.TagWidth(Stars), TierStyle.TagHeight);
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].enabled = i < Stars;
                    _stars[i].color = UiPalette.TierText(tier.Value);
                }
            }
        }

        /// <summary>The height of the view in its column: the cells the item takes, stacked.</summary>
        public void SetHeight(float height)
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }
    }
}
