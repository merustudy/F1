using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One potion slot, on the party board and in battle: the bottle alone, in a square pocket. The chosen potion's slot is the brass one; its
    /// words are shown elsewhere while it is chosen. A potion that has no bottle yet (stage 46: before its icon round) shows its name instead,
    /// like an item piece without a picture.
    /// </summary>
    public sealed class PotionSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _label;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;

        public Button Button => _button;

        /// <summary>The bottle on show, or null while the slot is empty or the potion has no icon.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The potion on show, or null while the slot is empty.</summary>
        public PotionData Potion { get; private set; }

        /// <param name="potion">Null for an empty slot.</param>
        /// <param name="icon">The potion's bottle, or null when it has none: then the name stands in.</param>
        public void Show(PotionData potion, Sprite icon, bool selected, bool interactable)
        {
            Potion = potion;
            _icon.sprite = icon;
            _icon.enabled = potion != null && icon != null;
            if (_label != null)
            {
                _label.enabled = potion != null && icon == null;
                _label.text = _label.enabled ? UiText.Name(potion.Name) : string.Empty;
            }

            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }
    }
}
