using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One potion slot, on the party board and in battle. The chosen potion's slot is the brass one.</summary>
    public sealed class PotionSlotView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Sprite _plain;
        [SerializeField] Sprite _selected;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _effect;

        public Button Button => _button;

        /// <param name="potion">Null for an empty slot.</param>
        public void Show(PotionData potion, bool selected, bool interactable)
        {
            _name.text = potion == null ? UiStrings.Get(UiKeys.Board.EmptySlot) : UiText.Name(potion.Name);
            _name.color = potion == null ? UiPalette.TextDim : UiPalette.Text;
            _effect.text = potion == null ? string.Empty : UiText.PotionDetails(potion);
            _frame.sprite = selected ? _selected : _plain;
            _button.interactable = interactable;
        }
    }
}
