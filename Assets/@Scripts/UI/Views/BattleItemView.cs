using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// One item of a unit in battle: a cell of the item board. A geometric placeholder icon (one
    /// per effect kind, until there is art) sits behind the cooldown fill, the name over it. A big
    /// item can take more than one cell; the cell then grows downward.
    /// </summary>
    public sealed class BattleItemView : MonoBehaviour
    {
        public const float CellHeight = 48f;
        public const float CellGap = 4f;

        [SerializeField] UiBar _cooldown;
        [SerializeField] TMP_Text _name;
        [SerializeField] GameObject _iconDamage;
        [SerializeField] GameObject _iconHeal;
        [SerializeField] GameObject _iconShield;
        [SerializeField] GameObject _iconBurn;

        BattleItemState _item;
        bool _shownActive;

        /// <summary>The height of a board of this many cells.</summary>
        public static float BoardHeight(int cells)
        {
            return cells * CellHeight + (cells - 1) * CellGap;
        }

        /// <param name="cells">How many cells of the board the item takes.</param>
        public void Bind(BattleItemState item, int cells)
        {
            _item = item;
            ItemData data = item.Equipped.Item;
            _name.text = UiText.Name(data.Name);

            var rect = (RectTransform)transform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, BoardHeight(cells));

            EffectKind kind = data.Effects[0].Kind;
            _iconDamage.SetActive(kind == EffectKind.Damage);
            _iconHeal.SetActive(kind == EffectKind.Heal);
            _iconShield.SetActive(kind == EffectKind.Shield);
            _iconBurn.SetActive(kind == EffectKind.Burn);

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

        /// <summary>An item that cannot be used in the owner's row is dimmed.</summary>
        void ShowActive(bool active)
        {
            _shownActive = active;
            _name.color = active ? UiPalette.Text : UiPalette.TextDim;
        }
    }
}
