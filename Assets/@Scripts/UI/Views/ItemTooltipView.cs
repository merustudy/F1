using System;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// An item's card (2026-10-07 round 42, Docs/Architecture/12_UI.md "툴팁"): its title, facts, effects, fatigue and, on the
    /// party side, what merging it does, in Diablo II's tooltip since round 49 (a black see-through card with a grey hairline, the lines
    /// centred, the title in the item's rarity colour, the effects in the magic blue; no stripe). The party side floats it
    /// above the board panel over the cell's column with a notch down towards the cell; the battle stands it beside the board
    /// with a notch on the edge facing the cell (mockup 6). The merchant's goods show theirs in a tooltip over the good (round 57). It takes no clicks: the screen closes it on the next press anywhere (<see cref="PointerPress"/>).
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

        [SerializeField] RectTransform _notchBottom;
        [SerializeField] RectTransform _notchLeft;
        [SerializeField] RectTransform _notchRight;
        [SerializeField] TMP_Text _title;
        [SerializeField] Image _plate;
        [SerializeField] TMP_Text _plateTitle;
        [SerializeField] TMP_Text _uniqueLine;
        [SerializeField] TMP_Text _facts;
        [SerializeField] TMP_Text _effects;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _merge;
        [SerializeField] Button _sell;
        [SerializeField] TMP_Text _sellLabel;

        /// <summary>The sell button at the card's foot was clicked (Round 74 B, 2026-10-17: selling at a shop).</summary>
        public event Action SellClicked;

        public bool IsShown => gameObject.activeSelf;

        /// <summary>Whether the card carries the sell button now (at a shop: what the shop pays, or that it cannot be sold).</summary>
        public bool SellShown => _sell != null && _sell.gameObject.activeSelf;
        public Button SellButton => _sell;
        public string SellLabel => _sellLabel != null ? _sellLabel.text : string.Empty;

        void Awake()
        {
            if (_sell != null)
            {
                _sell.onClick.AddListener(() => SellClicked?.Invoke());
            }
        }

        /// <summary>Whether the pointer is over the card now: a press there is the card's own (its sell button), not the press that closes it.</summary>
        public bool PointerIsOver()
        {
            Mouse mouse = Mouse.current;
            if (!IsShown || mouse == null)
            {
                return false;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
            return RectTransformUtility.RectangleContainsScreenPoint((RectTransform)transform, mouse.position.ReadValue(), camera);
        }

        /// <summary>The item on the card, or null when hidden.</summary>
        public EquippedItem Item { get; private set; }

        /// <summary>The frame the card was shown in; a press in a later frame closes it.</summary>
        public int ShownFrame { get; private set; } = -1;

        /// <summary>The card's box in its parent's top-left pixel space (x to the right, y down), as placed.</summary>
        public Rect Placed { get; private set; }

        /// <summary>The cell's box in the same space: what the card was placed against.</summary>
        public Rect Anchor { get; private set; }

        /// <summary>The name on the card: on the plate for a unique item (stage 23), on the title line otherwise.</summary>
        public string Title => _plate.gameObject.activeSelf ? _plateTitle.text : _title.text;

        /// <summary>Whether the card wears the plate head of a unique item (stage 23; round 42's mockup 7).</summary>
        public bool PlateShown => _plate.gameObject.activeSelf;

        /// <summary>The notch shown, and where along its edge it stands (x from the card's left for the bottom, y from its top for a side).</summary>
        public Notch NotchShown { get; private set; }

        public float NotchAt { get; private set; }

        /// <summary>The party side: above the panel, over the cell's column, the notch pointing down at the cell.</summary>
        /// <param name="starLine">What the stars on the board say of the item (stage 20, <see cref="UiText.StarLine"/>), or null.</param>
        /// <param name="starDamage">What they add to its weapon damage, which its damage line counts (<see cref="StarRules.DamageOn"/>).</param>
        /// <param name="sellLabel">Round 74 B: the sell button's words while at a shop (what the shop pays, or that it cannot be sold); null hides the button.</param>
        /// <param name="sellEnabled">Whether the sell button can be pressed.</param>
        /// <param name="ownerStats">The stats of the mercenary whose board the item is on (Slice D stage 32): the card counts them. Null without an owner.</param>
        /// <param name="ownerPassive">That mercenary's job passive (Slice D stage 36): the ranger's adds to a weapon's critical chance. Null without one.</param>
        public void ShowAbove(EquippedItem item, string mergeHint, RectTransform cell, RectTransform panel, string starLine = null, int starDamage = 0,
            string sellLabel = null, bool sellEnabled = false, StatBlock ownerStats = null, PassiveSpec ownerPassive = null)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, mergeHint, true, starLine, starDamage, sellLabel, sellEnabled, ownerStats, 0, ownerPassive);
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
            string starLine = null, int starDamage = 0, string sellLabel = null, bool sellEnabled = false, StatBlock ownerStats = null, int ownerAccuracy = 0,
            PassiveSpec ownerPassive = null)
        {
            var parent = (RectTransform)transform.parent;
            Vector2 size = Fill(item, mergeHint, withFatigue, starLine, starDamage, sellLabel, sellEnabled, ownerStats, ownerAccuracy, ownerPassive);
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
        Vector2 Fill(EquippedItem item, string mergeHint, bool withFatigue, string starLine, int starDamage, string sellLabel = null, bool sellEnabled = false, StatBlock ownerStats = null, int ownerAccuracy = 0, PassiveSpec ownerPassive = null)
        {
            if (_sell != null)
            {
                bool selling = !string.IsNullOrEmpty(sellLabel);
                _sell.gameObject.SetActive(selling);
                _sell.interactable = selling && sellEnabled;
                _sellLabel.text = selling ? sellLabel : string.Empty;
            }

            Item = item;
            bool unique = item.Item.Unique;
            _title.text = UiText.ItemTitle(item);
            _title.gameObject.SetActive(!unique);
            _plate.gameObject.SetActive(unique);
            _plateTitle.text = UiText.Name(item.Item.Name);
            Set(_uniqueLine, unique ? UiStrings.Get(UiKeys.Item.UniqueTitle) : null);
            string recipe = UiText.RecipeHint(item);
            mergeHint = recipe == null ? mergeHint : string.IsNullOrEmpty(mergeHint) ? recipe : mergeHint + "\n" + recipe;
            UiText.ItemCard(item, out string facts, out string effects, out string fatigue, starDamage, ownerStats, ownerAccuracy, ownerPassive);
            Set(_facts, facts);
            // Under the effects, one line: what the stars add and what the owner's stat multiplies (Round 81: "(★ 숫돌 +1)  (힘 12 ×1.2)").
            string statScale = UiText.StatScale(item, ownerStats);
            string tail = string.IsNullOrEmpty(starLine) ? statScale : string.IsNullOrEmpty(statScale) ? starLine : starLine + "  " + statScale;
            Set(_effects, string.IsNullOrEmpty(tail) ? effects : string.IsNullOrEmpty(effects) ? tail : effects + "\n" + tail);
            Set(_fatigue, withFatigue ? fatigue : null);
            Set(_merge, mergeHint);
            _title.color = UiText.TitleColor(item);

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
