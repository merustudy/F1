using System.Globalization;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item on a member's board (as high as the cells it takes, stacked as in battle) or
    /// one empty cell. An item shows its icon, as in battle, with its grade on the badge at the
    /// bottom-left corner; an item without an icon shows its name with the grade under it, smaller
    /// and dimmer; an empty cell says so. Equipment that costs fatigue when a battle starts has the
    /// cost on the violet tag at the top-right corner (round 32, B1). An item above Bronze has the rim
    /// inside the cell's line in its tier's colour, and a cell the chosen item would merge into shows the
    /// tier the merge makes on a veil, with the rim in that colour (round 35, A). The chosen item's cell is
    /// the brass one. A cell that takes no click now is dimmed by its button.
    /// <see cref="Index"/> is the first cell the view covers.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        /// <summary>The grade's line inside the text: its size against the name's, and its color.</summary>
        static readonly string GradeLine = "\n<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(UiPalette.InkTextDim) + ">{0}</color></size>";

        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;
        [SerializeField] Image _icon;
        [SerializeField] GameObject _badge;
        [SerializeField] TMP_Text _grade;
        [SerializeField] TMP_Text _text;
        [SerializeField] GameObject _fatigue;
        [SerializeField] TMP_Text _fatigueText;
        [SerializeField] Image _tierRim;
        [SerializeField] GameObject _merge;
        [SerializeField] TMP_Text _mergeText;

        public Button Button => _button;

        /// <summary>The item shown, or null for an empty cell.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The first cell of the board the view covers.</summary>
        public int Index { get; set; }

        /// <summary>The icon on show, or null while words stand in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The grade on the badge, or an empty string while the badge is hidden.</summary>
        public string Grade => _badge.activeSelf ? _grade.text : string.Empty;

        /// <summary>The words on the fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTag => _fatigue.activeSelf ? _fatigueText.text : string.Empty;

        /// <summary>The colour of the rim, or null while the cell has none (an empty cell, a Bronze item that nothing merges into).</summary>
        public Color? TierRim => _tierRim.enabled ? _tierRim.color : (Color?)null;

        /// <summary>The words of the merge mark, or an empty string while it is hidden.</summary>
        public string MergeMark => _merge.activeSelf ? _mergeText.text : string.Empty;

        /// <param name="icon">The item's icon, or null when it has none or the cell is empty.</param>
        /// <param name="fatigueCost">What the item adds to its owner's fatigue when a battle starts; 0 hides the tag.</param>
        /// <param name="merges">Whether the chosen item would merge into this one: the cell is marked with the tier the merge makes.</param>
        public void Show(EquippedItem item, Sprite icon, bool selected, bool interactable, int fatigueCost, bool merges)
        {
            _fatigue.SetActive(item != null && fatigueCost > 0);
            _fatigueText.text = _fatigue.activeSelf ? UiStrings.Get(UiKeys.Board.FatigueTag, fatigueCost) : string.Empty;

            // The rim in the item's tier above Bronze; while the chosen item would merge in here, in the tier the merge makes, over the veil and its words.
            merges &= item != null;
            _merge.SetActive(merges);
            _tierRim.enabled = item != null && (merges || item.Tier > ItemTier.Bronze);
            if (_tierRim.enabled)
            {
                ItemTier shown = merges ? item.Tier + 1 : item.Tier;
                _tierRim.color = UiPalette.TierRim(shown);
                _mergeText.text = merges ? UiStrings.Get(UiKeys.Board.MergeInto, UiText.TierName(shown)) : string.Empty;
                _mergeText.color = UiPalette.TierText(shown);
            }

            Item = item;
            bool pictured = item != null && icon != null;

            _icon.sprite = icon;
            _icon.enabled = pictured;
            _badge.SetActive(pictured);
            _grade.text = pictured ? item.Grade.ToString(CultureInfo.InvariantCulture) : string.Empty;

            _text.enabled = !pictured;
            _text.text = item == null
                ? UiStrings.Get(UiKeys.Board.EmptySlot)
                : pictured ? string.Empty : UiText.Name(item.Item.Name) + string.Format(GradeLine, UiStrings.Get(UiKeys.Board.Grade, item.Grade));
            _text.color = item == null ? UiPalette.InkTextDim : UiPalette.InkText;

            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }

        /// <summary>The height of the view in its column: the cells the item takes, stacked.</summary>
        public void SetHeight(float height)
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }
    }
}
