using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One reward choice: an item or a potion.</summary>
    public sealed class RewardOptionView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _kind;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _body;
        [SerializeField] TMP_Text _action;

        public Button Button => _button;

        public void Show(string kind, string title, string body, string action, bool selected, bool interactable)
        {
            _kind.text = kind;
            _title.text = title;
            _body.text = body;
            _action.text = action;
            _frame.color = selected ? UiPalette.Selected : UiPalette.PanelLight;
            _button.interactable = interactable;
        }
    }
}
