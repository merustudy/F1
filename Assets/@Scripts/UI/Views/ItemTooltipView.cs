using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// An item's card (2026-10-07 round 42, Docs/Architecture/12_UI.md "툴팁"): its title, facts, effects, fatigue and, on the
    /// party side, what merging it does, on an ink card with a brass hairline and the tier's stripe. The party side floats it
    /// above the board panel over the cell's column with a notch down towards the cell; the battle stands it beside the board
    /// with a notch on the edge facing the cell (mockup 6). It takes no clicks: the screen closes it on the next press anywhere
    /// (<see cref="PointerPress"/>).
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
        public void ShowAbove(EquippedItem item, string mergeHint, RectTransform cell, RectTransform panel)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, mergeHint, true);
            Rect anchor = LocalRect(cell, parent);
            Rect placed = TooltipPlacement.Above(anchor, size, LocalRect(panel, parent).yMin, Screen(parent), out float notchX);
            ShowNotch(Notch.Bottom, notchX);
            Place(placed, anchor);
        }

        /// <summary>
        /// Battle: beside the board, to its left for the party and to its right for the enemy (or the other side when there is
        /// no room), the notch on the edge facing the cell. An enemy's card has no fatigue line (<paramref name="withFatigue"/>
        /// false): the enemy has no fatigue.
        /// </summary>
        public void ShowBeside(EquippedItem item, RectTransform cell, bool left, RectTransform panel, bool withFatigue)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, null, withFatigue);
            Rect anchor = LocalRect(cell, parent);
            Rect placed = TooltipPlacement.Beside(anchor, size, left, LocalRect(panel, parent), Screen(parent), out float notchY, out bool cardIsLeftOfCell);
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
        Vector2 Fill(EquippedItem item, string mergeHint, bool withFatigue)
        {
            Item = item;
            _title.text = UiText.ItemTitle(item);
            UiText.ItemCard(item, out string facts, out string effects, out string fatigue);
            Set(_facts, facts);
            Set(_effects, effects);
            Set(_fatigue, withFatigue ? fatigue : null);
            Set(_merge, mergeHint);
            Color stripe = UiPalette.TierMark(item.Tier);
            _stripe.enabled = stripe.a > 0f;
            _stripe.color = stripe;

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

        /// <summary>A rect in the parent's top-left pixel space: x from the parent's left edge, y down from its top.</summary>
        static Rect LocalRect(RectTransform rect, RectTransform parent)
        {
            var corners = new Vector3[4];
            rect.GetWorldCorners(corners);
            Vector3 min = parent.InverseTransformPoint(corners[0]);
            Vector3 max = parent.InverseTransformPoint(corners[2]);
            Rect p = parent.rect;
            return new Rect(min.x - p.xMin, p.yMax - max.y, max.x - min.x, max.y - min.y);
        }

        static Rect Screen(RectTransform parent)
        {
            return new Rect(0f, 0f, parent.rect.width, parent.rect.height);
        }
    }
}
