using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One reward choice: an item or a potion. An item above Common has the stripe of its tier at the card's left edge (round 35).</summary>
    public sealed class RewardOptionView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] Image _stripe;
        [SerializeField] TMP_Text _kind;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _body;
        [SerializeField] TMP_Text _action;

        public Button Button => _button;

        /// <summary>The colour of the tier stripe, or null while there is none (Common, a potion).</summary>
        public Color? Stripe => _stripe.enabled ? _stripe.color : (Color?)null;

        /// <param name="tier">An item's tier; Common (no stripe) for a potion.</param>
        public void Show(string kind, string title, string body, string action, bool selected, bool interactable, ItemTier tier = ItemTier.Common)
        {
            _stripe.enabled = tier > ItemTier.Common;
            _stripe.color = UiPalette.TierMark(tier);
            _kind.text = kind;
            _title.text = title;
            _body.text = body;
            _action.text = action;
            _frame.color = selected ? UiPalette.Selected : UiPalette.PanelLight;
            _button.interactable = interactable;
        }
    }
}
