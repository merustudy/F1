using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// An item's card (2026-10-07 round 42, Docs/Architecture/12_UI.md "툴팁"): its title, facts, effects, fatigue and, on the
    /// party side, what merging it does, in Diablo II's tooltip since round 49 (a black see-through card with a grey hairline, the lines
    /// centred, the title in the item's rarity colour, the effects in the magic blue; no stripe). The party side floats it
    /// above the board panel over the cell's column with a notch down towards the cell; the battle stands it beside the board
    /// with a notch on the edge facing the cell (mockup 6). The shop's tiles show their facts themselves (round 46, S1). It takes no clicks: the screen closes it on the next press anywhere (<see cref="PointerPress"/>).
    /// </summary>
    public sealed class ItemTooltipView : MonoBehaviour
    {
        /// <summary>Which edge of the card carries the notch that points at the cell.</summary>
        public enum Notch
        {
            None,
            Bottom,
            Left,
            Right,
        }

        [SerializeField] Image _stripe;
        [SerializeField] RectTransform _notchBottom;
        [SerializeField] RectTransform _notchLeft;
        [SerializeField] RectTransform _notchRight;
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _facts;
        [SerializeField] TMP_Text _effects;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _merge;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>The item on the card, or null when hidden.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The frame the card was shown in; a press in a later frame closes it.</summary>
        public int ShownFrame { get; private set; } = -1;

        /// <summary>The card's box in its parent's top-left pixel space (x to the right, y down), as placed.</summary>
        public Rect Placed { get; private set; }

        /// <summary>The cell's box in the same space: what the card was placed against.</summary>
        public Rect Anchor { get; private set; }

        public string Title => _title.text;

        /// <summary>The notch shown, and where along its edge it stands (x from the card's left for the bottom, y from its top for a side).</summary>
        public Notch NotchShown { get; private set; }

        public float NotchAt { get; private set; }

        /// <summary>The party side: above the panel, over the cell's column, the notch pointing down at the cell.</summary>
        /// <param name="starLine">What the stars on the board say of the item (stage 20, <see cref="UiText.StarLine"/>), or null.</param>
        /// <param name="starDamage">What they add to its weapon damage, which its damage line counts (<see cref="StarRules.DamageOn"/>).</param>
        public void ShowAbove(EquippedItem item, string mergeHint, RectTransform cell, RectTransform panel, string starLine = null, int starDamage = 0)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, mergeHint, true, starLine, starDamage);
            Rect anchor = TooltipPlacement.In(cell, parent);
            Rect placed = TooltipPlacement.Above(anchor, size, TooltipPlacement.In(panel, parent).yMin, Screen(parent), out float notchX);
            ShowNotch(Notch.Bottom, notchX);
            Place(placed, anchor);
        }

        /// <summary>
        /// Battle: beside the board, to its left for the party and to its right for the enemy (or the other side when there is
        /// no room), the notch on the edge facing the cell. An enemy's card has no fatigue line (<paramref name="withFatigue"/>
        /// false): the enemy has no fatigue.
        /// </summary>
        public void ShowBeside(EquippedItem item, RectTransform cell, bool left, RectTransform panel, bool withFatigue, string mergeHint = null,
            string starLine = null, int starDamage = 0)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, mergeHint, withFatigue, starLine, starDamage);
            Rect anchor = TooltipPlacement.In(cell, parent);
            Rect placed = TooltipPlacement.Beside(anchor, size, left, TooltipPlacement.In(panel, parent), Screen(parent), out float notchY, out bool cardIsLeftOfCell);
            ShowNotch(cardIsLeftOfCell ? Notch.Right : Notch.Left, notchY);
            Place(placed, anchor);
        }

        public void Hide()
        {
            Item = null;
            ShownFrame = -1;
            gameObject.SetActive(false);
        }

        /// <summary>Writes the card and measures it. The card is active and on top afterwards: the layout needs it active.</summary>
        Vector2 Fill(EquippedItem item, string mergeHint, bool withFatigue, string starLine, int starDamage)
        {
            Item = item;
            _title.text = UiText.ItemTitle(item);
            UiText.ItemCard(item, out string facts, out string effects, out string fatigue, starDamage);
            Set(_facts, facts);
            Set(_effects, string.IsNullOrEmpty(starLine) ? effects : string.IsNullOrEmpty(effects) ? starLine : effects + "\n" + starLine);
            Set(_fatigue, withFatigue ? fatigue : null);
            Set(_merge, mergeHint);
            _title.color = UiPalette.Rarity(item.Tier);
            _stripe.enabled = false;

            gameObject.SetActive(true);
            transform.SetAsLastSibling();
            var rect = (RectTransform)transform;
            LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
            return rect.rect.size;
        }

        static void Set(TMP_Text text, string value)
        {
            bool shown = !string.IsNullOrEmpty(value);
            text.gameObject.SetActive(shown);
            text.text = shown ? value : string.Empty;
        }

        /// <summary>One notch at a time. Each sits a pixel inside its edge, over the hairline, and shows only what lies outside the card.</summary>
        void ShowNotch(Notch notch, float at)
        {
            _notchBottom.gameObject.SetActive(notch == Notch.Bottom);
            _notchLeft.gameObject.SetActive(notch == Notch.Left);
            _notchRight.gameObject.SetActive(notch == Notch.Right);
            switch (notch)
            {
                case Notch.Bottom: _notchBottom.anchoredPosition = new Vector2(at, 1f); break;
                case Notch.Left: _notchLeft.anchoredPosition = new Vector2(1f, -at); break;
                case Notch.Right: _notchRight.anchoredPosition = new Vector2(-1f, -at); break;
            }

            NotchShown = notch;
            NotchAt = at;
        }

        void Place(Rect placed, Rect anchor)
        {
            ((RectTransform)transform).anchoredPosition = new Vector2(placed.x, -placed.y);
            Placed = placed;
            Anchor = anchor;
            ShownFrame = Time.frameCount;
        }

        static Rect Screen(RectTransform parent)
        {
            return new Rect(0f, 0f, parent.rect.width, parent.rect.height);
        }
    }
}
