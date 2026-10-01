using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One living unit in battle: the figure (a placeholder until there is art) over the info card
    /// with HP, shield, burn, the death's door state and the item board. It shows what the engine
    /// says; texts are rebuilt only when their numbers change. The battle screen takes a dead unit
    /// off the field.
    /// </summary>
    public sealed class BattleUnitView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _figure;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _status;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] TMP_Text _shield;
        [SerializeField] TMP_Text _burn;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] GameObject _emptyCellTemplate;
        [SerializeField] Transform _itemParent;

        /// <summary>How much of the figure frame's color shows: the background will be seen through it.</summary>
        const float FigureAlpha = 0.35f;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        long _shownStatus = -1;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The figure frame's color for a side's card color.</summary>
        public static Color FigureTint(Color frame)
        {
            return new Color(frame.r, frame.g, frame.b, FigureAlpha);
        }

        public void Bind(BattleUnit unit)
        {
            _unit = unit;
            _name.text = UiText.Name(unit.Setup.Name);

            // The board in order: an item takes as many cells as its size and shows its cooldown;
            // the cells after the last item stay faint.
            int cells = 0;
            foreach (BattleItemState item in unit.Items)
            {
                BattleItemView view = Instantiate(_itemTemplate, _itemParent);
                view.gameObject.SetActive(true);
                view.Bind(item, item.Equipped.Item.Size);
                _items.Add(view);
                cells += item.Equipped.Item.Size;
            }

            for (; cells < unit.Setup.ItemSlots; cells++)
            {
                Instantiate(_emptyCellTemplate, _itemParent).SetActive(true);
            }
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
            _shownHp = -1;
            _shownShield = -1;
            _shownBurn = -1;
            _shownStatus = -1;
            _name.text = UiText.Name(_unit.Setup.Name);
            for (int i = 0; i < _items.Count; i++)
            {
                _items[i].Bind(_unit.Items[i], _unit.Items[i].Equipped.Item.Size);
            }
        }

        /// <param name="targetable">True while a potion is waiting for this unit to be clicked.</param>
        public void Render(BattleEngine engine, bool targetable)
        {
            BalanceData balance = engine.Setup.Balance;
            int timeMs = engine.TimeMs;

            if (_unit.Hp != _shownHp)
            {
                _shownHp = _unit.Hp;
                _hp.text = UiStrings.Get(UiKeys.Battle.Hp, _unit.Hp, _unit.MaxHp);
                _hpBar.Set(_unit.Hp, _unit.MaxHp);
            }

            if (_unit.Shield != _shownShield)
            {
                _shownShield = _unit.Shield;
                _shield.text = _unit.Shield > 0 ? UiStrings.Get(UiKeys.Battle.Shield, _unit.Shield) : string.Empty;
            }

            if (_unit.Burn != _shownBurn)
            {
                _shownBurn = _unit.Burn;
                _burn.text = _unit.Burn > 0 ? UiStrings.Get(UiKeys.Battle.Burn, _unit.Burn) : string.Empty;
            }

            RenderStatus(balance, timeMs);

            _button.interactable = targetable && _unit.Alive;
            Color frame;
            if (_unit.InDog)
            {
                frame = UiPalette.Danger;
            }
            else if (targetable)
            {
                frame = UiPalette.PartyTarget;
            }
            else
            {
                frame = _unit.Side == BattleSide.Party ? UiPalette.Party : UiPalette.Enemy;
            }

            if (_frame.color != frame)
            {
                _frame.color = frame;
                _figure.color = FigureTint(frame);
            }

            foreach (BattleItemView item in _items)
            {
                item.Render(timeMs, _unit.Alive);
            }
        }

        void RenderStatus(BalanceData balance, int timeMs)
        {
            // One number that changes exactly when the status text has to change.
            long status;
            int graceTenths = 0;
            if (!_unit.InDog)
            {
                status = 0;
            }
            else if (timeMs < _unit.GraceEndMs)
            {
                graceTenths = (_unit.GraceEndMs - timeMs + 99) / 100;
                status = 1000L + graceTenths * 100L + _unit.GraceHits;
            }
            else
            {
                status = 2;
            }

            if (status == _shownStatus)
            {
                return;
            }

            _shownStatus = status;
            if (status == 0)
            {
                _status.text = string.Empty;
            }
            else if (status == 2)
            {
                _status.text = UiStrings.Get(UiKeys.Battle.DogRolling, balance.DogDeathChancePercent);
            }
            else
            {
                _status.text = UiStrings.Get(
                    UiKeys.Battle.DogGrace,
                    UiText.Seconds(graceTenths * 100),
                    _unit.GraceHits,
                    balance.DogGraceBreakHits);
            }
        }
    }
}
