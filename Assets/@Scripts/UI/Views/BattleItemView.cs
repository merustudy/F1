using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item of a unit in battle (Slice B stage 19): its piece on the unit's board grid, the item's icon turned as the item lies on its
    /// black squares with no ground (round 49: Diablo's look, "전투 화면에서는 아이템 뒤 파란색 배경 없음"). The cooldown is the item's own light (round 48, the fifth ask's 안 2): the
    /// icon starts dark (<see cref="UiPalette.CooldownDark"/>) and its own colours come back from its left edge with a sharp edge as it
    /// charges, without gold or a front line; when the whole icon is lit the item fires: it swells to 1.12 (largest at 0.07 s, back by
    /// 0.30 s) and is a moment brighter (<see cref="UiPalette.FireBrighten"/>), and from 0.18 s the new cooldown's dark comes back
    /// (<see cref="Pulse"/>). An item that cannot be used where its owner stands is grey and neither brightens nor swells. An item
    /// without an icon shows its name. An enemy's icon is mirrored so that its weapon points at the party. An item above Common wears
    /// its tier's stars at the bottom-left in Diablo's rarity colours, always bright (round 49; round 41's outline round the icon is off).
    /// The piece takes a right click for the item's card (round 47).
    /// </summary>
    public sealed class BattleItemView : MonoBehaviour
    {
        /// <summary>The swell's top and its timing, and when the dark comes back (round 48, the second and fifth asks).</summary>
        public const float SwellScale = 1.12f;
        const float SwellPeak = 0.07f;
        const float SwellTime = 0.30f;
        const float DarkReturns = 0.18f;

        [SerializeField] Image _frame;
        [SerializeField] Outline _frameLine;
        [SerializeField] RectTransform _darkArt;
        [SerializeField] Image _darkOutline;
        [SerializeField] SilhouetteOutline _darkOutlineEffect;
        [SerializeField] Image _darkIcon;
        [SerializeField] RectTransform _litMask;
        [SerializeField] RectTransform _litArt;
        [SerializeField] Image _outline;
        [SerializeField] SilhouetteOutline _outlineEffect;
        [SerializeField] Image _icon;
        [SerializeField] Image _flash;
        [SerializeField] Button _button;
        [SerializeField] RightClick _rightClick;
        [SerializeField] TMP_Text _name;
        [SerializeField] Image _tierTag;
        [SerializeField] Image[] _stars;

        BattleItemState _item;
        bool _shownActive;

        /// <summary>Whether the item never activates (stage 20): always shown all lit.</summary>
        bool _passive;
        float _mirror = 1f;
        float _pulseAge = -1f;
        float _charge;

        /// <summary>The icon on show, or null while the name stands in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>The piece's button: silent and without a deed of its own, it holds the piece's click off the board while no potion is armed (round 42).</summary>
        public Button Button => _button;

        /// <summary>The right click that opens the item's card (round 47).</summary>
        public RightClick RightClick => _rightClick;

        /// <summary>Where the item lies on its owner's board (its icon's turn).</summary>
        public Placement At { get; private set; }

        /// <summary>
        /// Round 42: whether the piece takes clicks (a right click opens the item's card). Off while a potion waits for a target, so
        /// that the click falls through to the board, which is the potion's target.
        /// </summary>
        public void SetClickable(bool clickable)
        {
            _button.interactable = clickable;
            _button.targetGraphic.raycastTarget = clickable;
        }

        /// <summary>True when the icon is drawn mirrored: an enemy's.</summary>
        public bool Mirrored => _litArt.localScale.x < 0f;

        /// <summary>How far the item has charged as its icon shows it: 0 all dark, 1 all lit.</summary>
        public float Charge => _charge;

        /// <summary>How far from the left the icon is lit, as a share of the piece (the charge, or more while it fires).</summary>
        public float LitTo => _litMask.anchorMax.x;

        /// <summary>The icon's size against its resting size: the swell while it fires, 1 otherwise.</summary>
        public float Swell => Mathf.Abs(_litArt.localScale.y);

        /// <summary>Whether the item shows as usable where its owner stands (not grey).</summary>
        public bool ShowsActive => _shownActive;

        /// <summary>The tier the piece marks (the item's, above Common), or null for a Common item.</summary>
        public ItemTier? TierShown { get; private set; }

        /// <summary>How many stars the tier tag shows; 0 while it is hidden.</summary>
        public int Stars { get; private set; }

        /// <summary>The outline's colour, or null while the icon has no outline (a Common item, or no icon to outline).</summary>
        /// <summary>The stars' colour (Diablo's rarity colour, round 49), or null while none show.</summary>
        public Color? StarColor => Stars > 0 ? _stars[0].color : (Color?)null;

        /// <summary>The piece's ground colour on show: none in battle (round 49).</summary>
        public Color Ground => _frame.color;

        public Color? OutlineColor => _outline.enabled ? _outline.color : (Color?)null;

        public RectTransform Rect => (RectTransform)transform;

        /// <param name="at">Where the item lies (the piece is laid by the board; this turns the icon).</param>
        /// <param name="icon">The item's icon, or null when it has none.</param>
        /// <param name="mirrored">Whether the icon points the other way: the enemy's side.</param>
        public void Bind(BattleItemState item, Placement at, Sprite icon, bool mirrored)
        {
            _item = item;
            At = at;
            _name.text = UiText.Name(item.Equipped.Item.Name);
            _name.enabled = icon == null;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _darkIcon.sprite = icon;
            _darkIcon.enabled = icon != null;
            _flash.sprite = icon;
            _flash.enabled = false;
            _mirror = mirrored ? -1f : 1f;
            LayArt(_darkArt, at.Turns, false);
            LayArt(_litArt, at.Turns, true);
            ScaleArt(1f);
            ShowTier(item.Equipped.Tier, icon);
            _passive = item.Equipped.Item.IsPassive;
            ShowActive(item.Active);
            _pulseAge = -1f;
            ShowCharge(_passive ? 1f : 0f);
        }

        public void Render(int timeMs, bool ownerAlive)
        {
            // An item that never activates (stage 20) is always all lit: it has no cooldown and does not fire.
            if (_passive)
            {
                return;
            }

            // An item starts or stops working when its owner advances to another row.
            if (_item.Active != _shownActive)
            {
                ShowActive(_item.Active);
            }

            // An item that does not work where its owner stands does not charge: its icon stays dark.
            if (!_item.Active || !ownerAlive)
            {
                ShowCharge(0f);
                return;
            }

            // The icon is dark right after the item fired and fully lit at the moment it fires again.
            int remaining = _item.NextFireMs - timeMs;
            ShowCharge(Mathf.Clamp01(1f - (float)remaining / _item.CooldownMs));
        }

        /// <summary>The item fired: the icon swells and is a moment brighter, all lit; from <see cref="DarkReturns"/> the dark comes back.</summary>
        public void Pulse()
        {
            if (!_shownActive)
            {
                return;
            }

            _pulseAge = 0f;
            ShowCharge(_charge);
        }

        void Update()
        {
            if (_pulseAge < 0f)
            {
                return;
            }

            _pulseAge += Time.deltaTime;
            if (_pulseAge >= SwellTime)
            {
                _pulseAge = -1f;
                ScaleArt(1f);
                _flash.enabled = false;
                ShowCharge(_charge);
                return;
            }

            float swell = _pulseAge < SwellPeak
                ? 1f + (SwellScale - 1f) * (1f - Sq(1f - _pulseAge / SwellPeak))
                : 1f + (SwellScale - 1f) * Sq(1f - (_pulseAge - SwellPeak) / (SwellTime - SwellPeak));
            ScaleArt(swell);

            // A moment brighter at the top of the swell: the icon's own shape in white over it, fading either side.
            float bump = Mathf.Clamp01(1f - Mathf.Abs(_pulseAge - SwellPeak) / SwellPeak);
            _flash.enabled = _icon.enabled && bump > 0f;
            _flash.color = new Color(1f, 1f, 1f, UiPalette.FireBrighten * bump);
            ShowCharge(_charge);
        }

        static float Sq(float x)
        {
            return x * x;
        }

        /// <summary>
        /// Lights the icon from the left as far as the item has charged, with a sharp edge: the lit icon lies in a mask that grows from
        /// the piece's left. While the item fires it is all lit until the dark comes back, which then closes in to the new charge.
        /// </summary>
        void ShowCharge(float charge)
        {
            _charge = charge;
            float lit = charge;
            if (_pulseAge >= 0f)
            {
                lit = _pulseAge < DarkReturns ? 1f : Mathf.Lerp(1f, charge, (_pulseAge - DarkReturns) / (SwellTime - DarkReturns));
            }

            _litMask.anchorMin = new Vector2(0f, 0f);
            _litMask.anchorMax = new Vector2(Mathf.Clamp01(lit), 1f);
            _litMask.offsetMin = Vector2.zero;
            _litMask.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// An icon's box, turned as the item lies and mirrored for an enemy, centred on the piece. The lit one hangs from the mask's left
        /// edge at the piece's centre, so it stays put while the mask grows.
        /// </summary>
        void LayArt(RectTransform art, int turns, bool inMask)
        {
            Vector2 piece = Rect.sizeDelta;
            art.anchorMin = inMask ? new Vector2(0f, 0.5f) : new Vector2(0.5f, 0.5f);
            art.anchorMax = art.anchorMin;
            art.pivot = new Vector2(0.5f, 0.5f);
            art.anchoredPosition = inMask ? new Vector2(piece.x / 2f, 0f) : Vector2.zero;
            art.sizeDelta = GridGeometry.ArtBox(piece, turns, ItemSlotView.ArtMargin);
            art.localRotation = GridGeometry.ArtTurn(turns);
        }

        /// <summary>The icons and their outlines, mirrored for an enemy, swelled by this much while the item fires.</summary>
        void ScaleArt(float swell)
        {
            var scale = new Vector3(_mirror * swell, swell, 1f);
            _litArt.localScale = scale;
            _darkArt.localScale = scale;
        }

        /// <summary>The tier's stars in Diablo II's rarity colours on no pill, and no outline round the icon (round 49); nothing for Common.</summary>
        void ShowTier(ItemTier tier, Sprite icon)
        {
            TierShown = tier > ItemTier.Common ? tier : (ItemTier?)null;
            Stars = TierStyle.Stars(tier);
            _outline.enabled = false;
            _darkOutline.enabled = false;
            _tierTag.gameObject.SetActive(Stars > 0);
            if (Stars > 0)
            {
                _tierTag.color = Color.clear;
                _tierTag.rectTransform.sizeDelta = new Vector2(TierStyle.TagWidth(Stars), TierStyle.TagHeight);
                for (int i = 0; i < _stars.Length; i++)
                {
                    _stars[i].enabled = i < Stars;
                    _stars[i].color = UiPalette.Rarity(tier);
                }
            }
        }

        /// <summary>
        /// An item that cannot be used in the owner's row is grey, its piece too, and its icon stays dark. An item that never activates
        /// (stage 20) is shown as one that works, all lit, though the battle never makes it active.
        /// </summary>
        void ShowActive(bool active)
        {
            active |= _passive;
            _shownActive = active;
            float dark = UiPalette.CooldownDark;
            Color tier = TierShown.HasValue ? UiPalette.TierMark(TierShown.Value) : Color.white;
            _name.color = active ? UiPalette.Text : UiPalette.TextDim;
            // Round 49: in battle an item lies on its black squares with no ground ("전투 화면에서는 아이템 뒤 파란색 배경 없음").
            _frame.color = Color.clear;
            _frameLine.enabled = false;
            _icon.color = Color.white;
            _outline.color = tier;
            _darkIcon.color = active ? new Color(dark, dark, dark, 1f) : new Color(UiPalette.IconDim.r * dark, UiPalette.IconDim.g * dark, UiPalette.IconDim.b * dark, 1f);
            _darkOutline.color = new Color(tier.r * dark, tier.g * dark, tier.b * dark, 1f);
            _litMask.gameObject.SetActive(active);
            if (!active)
            {
                _pulseAge = -1f;
                ScaleArt(1f);
                _flash.enabled = false;
            }
        }
    }
}
