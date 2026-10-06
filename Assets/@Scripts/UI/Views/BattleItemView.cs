using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item of a unit in battle: a cell of the item board. The cell itself shows the cooldown
    /// as light (2026-10-04 round 18): it starts dark, the dark withdraws from the left as the item
    /// charges and the charged part is lit in the candle's gold, and the item fires when the cell is
    /// fully lit. The dark fills the cell inside its rim and lies over the icon; the gold lies under
    /// the icon, so that a lit icon keeps its own colours. A line of light with a glow behind it marks
    /// the front of the charge. An item without an icon shows its name instead. The cells of a board
    /// are stacked top to bottom in the unit's column of the board panel (2026-10-03 mockup V), so a
    /// big item takes more than one cell and the cell grows downwards; its icon is scaled to fit that
    /// shape. An enemy's icon is mirrored so that its weapon points at the party. When the item fires,
    /// the cell flashes and the icon pops for a moment, and the dark comes back as the flash fades
    /// (<see cref="Pulse"/>).
    /// </summary>
    public sealed class BattleItemView : MonoBehaviour
    {
        const float PulseDuration = 0.3f;
        const float PulsePop = 0.3f;
        const float PulseFlash = 0.6f;

        /// <summary>The light of the flash: a warm brass, not plain white, so that it reads as the item waking.</summary>
        static readonly Color FlashLight = new Color(1f, 0.92f, 0.66f, 1f);

        /// <summary>
        /// A cell of the board panel: a strip as wide as a column of the stage (2026-10-03 mockup V),
        /// and the gap between two cells stacked. A big item's cells make a taller strip. The party
        /// side of the node map and the reward screen uses the same cells. The 2026-10-04 mockup B
        /// made the cell 60 high with 2 between (from 50 and 4) and the most cells 7 (from 8), so
        /// that the panel keeps its height.
        /// </summary>
        public const float CellWidth = 180f;
        public const float CellHeight = 60f;
        public const float CellGapY = 2f;

        /// <summary>
        /// The cooldown's light (2026-10-04 round 18): the cell's dark rim, inside which its layers lie,
        /// and how dark the part not charged yet is (black of the candle's smoke over it). An empty cell of
        /// a battle board is as dark.
        /// </summary>
        public const float Rim = 2f;
        public const float ChargeShade = 0.85f;

        /// <summary>The soft left end of the dark, and the glow and the line at the front of the charge, in pixels.</summary>
        const float DarkFeather = 6f;
        const float FrontGlowWidth = 20f;
        const float FrontLineWidth = 2f;

        [SerializeField] RectTransform _light;
        [SerializeField] Image _dark;
        [SerializeField] Image _darkEdge;
        [SerializeField] Image _glow;
        [SerializeField] Image _front;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _name;
        [SerializeField] Image _flash;
        [SerializeField] Image _tierRim;

        BattleItemState _item;
        bool _shownActive;
        bool _shownCharging;
        bool _drawn;
        float _mirror = 1f;
        float _pulseAge = -1f;

        /// <summary>The height of a board of this many cells stacked.</summary>
        public static float BoardHeight(int cells)
        {
            return cells * CellHeight + (cells - 1) * CellGapY;
        }

        /// <summary>The icon on show, or null while the name stands in for it.</summary>
        public Sprite Icon => _icon.enabled ? _icon.sprite : null;

        /// <summary>True when the icon is drawn mirrored: an enemy's.</summary>
        public bool Mirrored => _icon.rectTransform.localScale.x < 0f;

        /// <summary>How far the item has charged as the cell shows it: 0 all dark, 1 fully lit.</summary>
        public float Charge { get; private set; }

        /// <summary>Where the dark begins, as a share of the inside of the cell from the left; the cell is lit before it.</summary>
        public float DarkFrom => _dark.rectTransform.anchorMin.x;

        /// <summary>How far the gold of the charged part reaches, as a share of the inside of the cell from the left.</summary>
        public float LitTo => _light.anchorMax.x;

        /// <summary>Whether the front of the charge (its line and glow) shows: while a working item charges.</summary>
        public bool ShowsFront => _front.enabled;

        /// <summary>How dark the part not charged yet is now: ChargeShade, less while the flash of a firing plays.</summary>
        public float Darkness => _dark.color.a;

        /// <summary>The colour of the rim of a tier above Bronze (round 35, A), or null for a Bronze item.</summary>
        public Color? TierRim => _tierRim.enabled ? _tierRim.color : (Color?)null;

        /// <param name="cells">How many cells of the board the item takes.</param>
        /// <param name="icon">The item's icon, or null when it has none.</param>
        /// <param name="mirrored">Whether the icon points the other way: the enemy's side.</param>
        public void Bind(BattleItemState item, int cells, Sprite icon, bool mirrored)
        {
            _item = item;
            _name.text = UiText.Name(item.Equipped.Item.Name);
            _name.enabled = icon == null;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            _mirror = mirrored ? -1f : 1f;
            _icon.rectTransform.localScale = new Vector3(_mirror, 1f, 1f);
            _tierRim.enabled = item.Equipped.Tier > ItemTier.Bronze;
            _tierRim.color = UiPalette.TierRim(item.Equipped.Tier);

            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(CellWidth, BoardHeight(cells));

            ShowActive(item.Active);
            _drawn = false;
            ShowCharge(0f, false);
        }

        public void Render(int timeMs, bool ownerAlive)
        {
            // An item starts or stops working when its owner advances to another row.
            if (_item.Active != _shownActive)
            {
                ShowActive(_item.Active);
            }

            // An item that does not work where its owner stands does not charge: its cell stays dark.
            if (!_item.Active || !ownerAlive)
            {
                ShowCharge(0f, false);
                return;
            }

            // The cell is dark right after the item fired and fully lit at the moment it fires again.
            int remaining = _item.NextFireMs - timeMs;
            ShowCharge(Mathf.Clamp01(1f - (float)remaining / _item.CooldownMs), true);
        }

        /// <summary>The item fired: the cell flashes and the icon pops; the cell is lit under the flash.</summary>
        public void Pulse()
        {
            _pulseAge = 0f;
            SetDarkness(0f);
        }

        void Update()
        {
            if (_pulseAge < 0f)
            {
                return;
            }

            _pulseAge += Time.deltaTime;
            float t = _pulseAge / PulseDuration;
            if (t >= 1f)
            {
                _pulseAge = -1f;
                _icon.rectTransform.localScale = new Vector3(_mirror, 1f, 1f);
                _flash.color = new Color(FlashLight.r, FlashLight.g, FlashLight.b, 0f);
                _flash.enabled = false;
                SetDarkness(ChargeShade);
                return;
            }

            float pop = 1f + PulsePop * Mathf.Sin(t * Mathf.PI);
            _icon.rectTransform.localScale = new Vector3(_mirror * pop, pop, 1f);
            _flash.color = new Color(FlashLight.r, FlashLight.g, FlashLight.b, PulseFlash * (1f - t));
            _flash.enabled = true;

            // The dark comes back as the flash fades: a flash over the dark would turn into a muddy tone.
            SetDarkness(ChargeShade * t);
        }

        /// <summary>
        /// Lights the cell from the left as far as the item has charged: the gold up to the front, the
        /// dark from the front to the right (its first pixels soft), and the line and the glow at the
        /// front while the item charges. Before any charge the dark covers the whole cell.
        /// </summary>
        void ShowCharge(float charge, bool charging)
        {
            if (_drawn && charge == Charge && charging == _shownCharging)
            {
                return;
            }

            _drawn = true;
            Charge = charge;
            _shownCharging = charging;

            float inside = CellWidth - 2f * Rim;
            float front = charge * inside;

            _light.anchorMax = new Vector2(charge, 1f);

            float feather = charge > 0f ? Mathf.Min(DarkFeather, inside - front) : 0f;
            RectTransform dark = _dark.rectTransform;
            dark.anchorMin = new Vector2(charge, 0f);
            dark.anchorMax = Vector2.one;
            dark.offsetMin = new Vector2(feather, 0f);
            dark.offsetMax = Vector2.zero;
            _darkEdge.enabled = feather > 0f;
            Strip(_darkEdge.rectTransform, charge, 0f, feather);

            bool edge = charging && charge > 0f && charge < 1f;
            _glow.enabled = edge;
            _front.enabled = edge;
            if (edge)
            {
                Strip(_glow.rectTransform, charge, 1f, Mathf.Min(FrontGlowWidth, front));
                Strip(_front.rectTransform, charge, 0.5f, FrontLineWidth);
            }
        }

        /// <summary>
        /// A strip as tall as the inside of the cell and this wide, standing at a share of its width:
        /// pivot 0 puts it right of that point, 1 left of it, 0.5 across it.
        /// </summary>
        static void Strip(RectTransform rect, float at, float pivot, float width)
        {
            rect.anchorMin = new Vector2(at, 0f);
            rect.anchorMax = new Vector2(at, 1f);
            rect.pivot = new Vector2(pivot, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(width, 0f);
        }

        void SetDarkness(float alpha)
        {
            Color color = _dark.color;
            color.a = alpha;
            _dark.color = color;
            _darkEdge.color = color;
        }

        /// <summary>An item that cannot be used in the owner's row is dimmed.</summary>
        void ShowActive(bool active)
        {
            _shownActive = active;
            _name.color = active ? UiPalette.Text : UiPalette.TextDim;
            _icon.color = active ? Color.white : UiPalette.IconDim;
        }
    }
}
