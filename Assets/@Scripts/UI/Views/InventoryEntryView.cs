using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One item of the inventory popup: its title over a one-line summary of its facts.</summary>
    public sealed class InventoryEntryView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _facts;

        public Button Button => _button;

        /// <summary>The item shown.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The place in the inventory.</summary>
        public int Index { get; set; }

        public void Show(EquippedItem item, bool selected, bool interactable)
        {
            Item = item;
            _title.text = UiText.ItemTitle(item);
            _facts.text = UiText.ItemSummary(item);
            _frame.color = selected ? UiPalette.Selected : UiPalette.Slot;
            _button.interactable = interactable;
        }
    }
}
