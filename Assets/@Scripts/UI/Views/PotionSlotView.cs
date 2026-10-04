using F1.Data;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One potion slot, on the party board and in battle: the bottle alone, in a square pocket. The chosen potion's slot is the brass one; its words are shown elsewhere while it is chosen.</summary>
    public sealed class PotionSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Image _icon;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;

        public Button Button => _button;

        /// <summary>The bottle on show, or null while the slot is empty or the potion has no icon.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <param name="potion">Null for an empty slot.</param>
        /// <param name="icon">The potion's bottle, or null when it has none.</param>
        public void Show(PotionData potion, Sprite icon, bool selected, bool interactable)
        {
            _icon.sprite = icon;
            _icon.enabled = potion != null && icon != null;
            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }
    }
}
