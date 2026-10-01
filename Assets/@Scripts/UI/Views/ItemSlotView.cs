using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item on a member's board (as tall as the cells it takes) or one empty cell: the name on
    /// the left, the grade on the right. <see cref="Index"/> is the first cell the view covers.
    /// </summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _grade;

        public Button Button => _button;

        /// <summary>The item shown, or null for an empty cell.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The first cell of the board the view covers.</summary>
        public int Index { get; set; }

        public void Show(EquippedItem item, bool selected, bool interactable)
        {
            Item = item;
            _name.text = item == null ? UiStrings.Get(UiKeys.Board.EmptySlot) : UiText.Name(item.Item.Name);
            _name.color = item == null ? UiPalette.TextDim : UiPalette.Text;
            _grade.text = item == null ? string.Empty : UiStrings.Get(UiKeys.Board.Grade, item.Grade);
            _frame.color = selected ? UiPalette.Selected : UiPalette.Slot;
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
