using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One item of a unit in battle: a cell of the item board. The cell itself is the cooldown
    /// gauge: it fills from the left and the item fires when it is full. The item's icon lies over
    /// the gauge, inside the cell's rim; an item without an icon shows its name instead. The cells
    /// of a board are stacked top to bottom in the unit's column of the board panel (2026-10-03
    /// mockup V), so a big item takes more than one cell and the cell grows downwards; its icon is
    /// scaled to fit that shape. An enemy's icon is mirrored so that its weapon points at the
    /// party. When the item fires, the cell flashes and the icon pops for a moment (<see cref="Pulse"/>).
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

        [SerializeField] UiBar _cooldown;
        [SerializeField] Image _icon;
        [SerializeField] TMP_Text _name;
        [SerializeField] Image _flash;

        BattleItemState _item;
        bool _shownActive;
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

            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(CellWidth, BoardHeight(cells));

            ShowActive(item.Active);
        }

        public void Render(int timeMs, bool ownerAlive)
        {
            // An item starts or stops working when its owner advances to another row.
            if (_item.Active != _shownActive)
            {
                ShowActive(_item.Active);
            }

            if (!_item.Active || !ownerAlive)
            {
                _cooldown.SetRatio(0f);
                return;
            }

            // The fill is empty right after the item fired and full at the moment it fires again.
            int remaining = _item.NextFireMs - timeMs;
            _cooldown.SetRatio(1f - (float)remaining / _item.CooldownMs);
        }

        /// <summary>The item fired: the cell flashes and the icon pops.</summary>
        public void Pulse()
        {
            _pulseAge = 0f;
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
                return;
            }

            float pop = 1f + PulsePop * Mathf.Sin(t * Mathf.PI);
            _icon.rectTransform.localScale = new Vector3(_mirror * pop, pop, 1f);
            _flash.color = new Color(FlashLight.r, FlashLight.g, FlashLight.b, PulseFlash * (1f - t));
            _flash.enabled = true;
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
