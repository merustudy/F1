using System.Globalization;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item on a member's board (as tall as the cells it takes) or one empty cell. An item
    /// shows its icon, as in battle, with its grade on the badge at the bottom-left corner; an
    /// item without an icon shows its name with the grade under it, smaller and dimmer (a column
    /// is too narrow for both on one line); an empty cell says so. The chosen item's cell is the
    /// brass one. A cell that takes no click now is dimmed by its button.
    /// <see cref="Index"/> is the first cell the view covers.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        /// <summary>The grade's line inside the text: its size against the name's, and its color.</summary>
        static readonly string GradeLine = "\n<size=75%><color=#" + ColorUtility.ToHtmlStringRGB(UiPalette.TextDim) + ">{0}</color></size>";

        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;
        [SerializeField] Image _icon;
        [SerializeField] GameObject _badge;
        [SerializeField] TMP_Text _grade;
        [SerializeField] TMP_Text _text;

        public Button Button => _button;

        /// <summary>The item shown, or null for an empty cell.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The first cell of the board the view covers.</summary>
        public int Index { get; set; }

        /// <summary>The icon on show, or null while words stand in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The grade on the badge, or an empty string while the badge is hidden.</summary>
        public string Grade => _badge.activeSelf ? _grade.text : string.Empty;

        /// <param name="icon">The item's icon, or null when it has none or the cell is empty.</param>
        public void Show(EquippedItem item, Sprite icon, bool selected, bool interactable)
        {
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
            _text.color = item == null ? UiPalette.TextDim : UiPalette.Text;

            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }

        /// <summary>The height of the view inside its column: the cells the item takes.</summary>
        public void SetHeight(float height)
        {
            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, height);
        }
    }
}
