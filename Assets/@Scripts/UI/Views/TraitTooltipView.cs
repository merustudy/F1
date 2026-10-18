using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The trait card (Slice D stage 37, Round 84 판정 6; 2026-10-19 the user: on a right click): a right click on a trait's chip opens a card above
    /// it, and the next press anywhere closes it (the item cards' rule, PointerPress) — the mock's dark panel in a brass
    /// rim, its brass notch at the bottom pointing at the chip — with the icon and the name, what kind the trait is ("타고난 특성 · 장점"), and what it does. The
    /// more pill's card has no icon: its title counts the rest and its line names them. One card per window; it takes no clicks.
    /// </summary>
    public sealed class TraitTooltipView : MonoBehaviour
    {
        /// <summary>The card's padding, its narrowest width, and how far above the chip its notch's point stands.</summary>
        public const float Padding = 20f;
        public const float MinWidth = 300f;
        public const float Gap = 14f;

        [SerializeField] GameObject _icon;
        [SerializeField] Image _iconRim;
        [SerializeField] TMP_Text _glyph;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _kind;
        [SerializeField] TMP_Text _effect;
        [SerializeField] RectTransform _notch;
        [SerializeField] float _nameLeft;

        TraitChipBase _owner;
        bool _hideWhenShownAgain;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>The frame the card was opened in: a press after it closes the card.</summary>
        public int ShownFrame { get; private set; } = -1;
        public string Title => _name.text;
        public string Kind => _kind.text;
        public string Effect => _effect.text;
        public TraitChipBase Owner => _owner;

        public void Show(TraitChipBase owner, TraitChip chip)
        {
            _owner = owner;
            ShownFrame = Time.frameCount;
            _hideWhenShownAgain = false;
            bool more = chip.IsMore;
            _icon.SetActive(!more);
            if (!more)
            {
                _iconRim.color = chip.Rim;
                _glyph.text = chip.Glyph;
                _glyph.color = chip.Rim;
            }

            float nameLeft = more ? Padding : _nameLeft;
            _name.text = more ? UiStrings.Get(UiKeys.Trait.MoreTitle, chip.MoreCount) : chip.Name;
            _name.rectTransform.anchoredPosition = new Vector2(nameLeft, _name.rectTransform.anchoredPosition.y);
            _kind.text = chip.Kind;
            _effect.text = chip.Effect;
            _effect.color = chip.IsMore ? UiPalette.Text : chip.Rim == UiPalette.TraitJob ? UiPalette.Bone : chip.Rim;

            float width = Mathf.Max(MinWidth, nameLeft + TextWidth.Of(_name, _name.text) + Padding,
                2f * Padding + TextWidth.Of(_kind, _kind.text), 2f * Padding + TextWidth.Of(_effect, _effect.text));
            width = Mathf.Ceil(width);
            var card = (RectTransform)transform;
            card.sizeDelta = new Vector2(width, card.sizeDelta.y);
            _name.rectTransform.sizeDelta = new Vector2(width - nameLeft - Padding, _name.rectTransform.sizeDelta.y);
            _kind.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _kind.rectTransform.sizeDelta.y);
            _effect.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _effect.rectTransform.sizeDelta.y);

            // Above the chip, centred on it, kept inside the window; the notch points at the chip's middle wherever the card is.
            var corners = new Vector3[4];
            ((RectTransform)owner.transform).GetWorldCorners(corners);
            var parent = (RectTransform)card.parent;
            Vector3 topMiddle = parent.InverseTransformPoint((corners[1] + corners[2]) / 2f);
            Rect bounds = parent.rect;
            float x = Mathf.Clamp(topMiddle.x, bounds.xMin + width / 2f, bounds.xMax - width / 2f);
            card.localPosition = new Vector3(x, topMiddle.y + Gap, 0f);
            _notch.anchoredPosition = new Vector2(topMiddle.x - x, _notch.anchoredPosition.y);
            gameObject.SetActive(true);
            card.SetAsLastSibling();
        }

        /// <summary>Hides the card if that chip opened it (a chip the pointer left after another opened it leaves it be).</summary>
        public void Hide(TraitChipBase owner)
        {
            if (owner != null && owner != _owner)
            {
                return;
            }

            _owner = null;
            gameObject.SetActive(false);
        }

        void Update()
        {
            // A card left open when its window closed (Esc) is not shown again with the window.
            if (_hideWhenShownAgain)
            {
                Hide(null);
                return;
            }

            PointerPress.Poll();
            if (PointerPress.PressedSince(ShownFrame))
            {
                Hide(null);
            }
        }

        void OnDisable()
        {
            _hideWhenShownAgain = _owner != null;
        }
    }
}
