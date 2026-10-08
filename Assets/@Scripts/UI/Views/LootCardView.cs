using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One drop of loot (Slice B stage 18; a reward choice before it). An item above Common has the stripe of its tier at the card's left edge (round 35). A taken drop stays as a dimmed card.</summary>
    public sealed class LootCardView : MonoBehaviour
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

        /// <summary>True while the card shows a drop already taken: dimmed, no facts, not a button.</summary>
        public bool IsTaken { get; private set; }

        /// <param name="tier">An item's tier; Common (no stripe) for a potion.</param>
        public void Show(string kind, string title, string body, string action, bool selected, bool interactable, ItemTier tier = ItemTier.Common)
        {
            IsTaken = false;
            _stripe.enabled = tier > ItemTier.Common;
            _stripe.color = UiPalette.TierMark(tier);
            _kind.text = kind;
            _title.text = title;
            _body.text = body;
            _action.text = action;
            _frame.color = selected ? UiPalette.Selected : UiPalette.PanelLight;
            _button.interactable = interactable;
        }

        /// <summary>A drop that was taken: the card stays in its place, darker, with the title of what was taken and the word for it.</summary>
        public void ShowTaken(string kind, string title, string taken)
        {
            IsTaken = true;
            _stripe.enabled = false;
            _kind.text = kind;
            _title.text = title;
            _body.text = string.Empty;
            _action.text = taken;
            _frame.color = UiPalette.Panel;
            _button.interactable = false;
        }
    }
}
