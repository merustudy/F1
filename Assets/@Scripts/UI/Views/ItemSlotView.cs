using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One item slot of a mercenary on the party board.</summary>
    public sealed class ItemSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _grade;

        public Button Button => _button;

        public void Show(EquippedItem item, bool selected, bool interactable)
        {
            _name.text = item == null ? UiStrings.Get(UiKeys.Board.EmptySlot) : UiText.Name(item.Item.Name);
            _name.color = item == null ? UiPalette.TextDim : UiPalette.Text;
            _grade.text = item == null ? string.Empty : UiStrings.Get(UiKeys.Board.Grade, item.Grade);
            _frame.color = selected ? UiPalette.Selected : UiPalette.Slot;
            _button.interactable = interactable;
        }
    }
}
