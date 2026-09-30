using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One unit in battle: HP, shield, burn, the death's door state and item cooldowns. It shows
    /// what the engine says; texts are rebuilt only when their numbers change.
    /// </summary>
    public sealed class BattleUnitView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] Image _frame;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _status;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] TMP_Text _shield;
        [SerializeField] TMP_Text _burn;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] Transform _itemParent;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        long _shownStatus = -1;

        public Button Button => _button;

        public void Bind(BattleUnit unit)
        {
            _unit = unit;
            _name.text = UiText.Name(unit.Setup.Name);
            foreach (BattleItemState item in unit.Items)
            {
                BattleItemView view = Instantiate(_itemTemplate, _itemParent);
                view.gameObject.SetActive(true);
                view.Bind(item);
                _items.Add(view);
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
                _items[i].Bind(_unit.Items[i]);
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
            if (!_unit.Alive)
            {
                _frame.color = UiPalette.Dead;
            }
            else if (_unit.InDog)
            {
                _frame.color = UiPalette.Danger;
            }
            else if (targetable)
            {
                _frame.color = UiPalette.PartyTarget;
            }
            else
            {
                _frame.color = _unit.Side == BattleSide.Party ? UiPalette.Party : UiPalette.Enemy;
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
            if (!_unit.Alive)
            {
                status = 1;
            }
            else if (!_unit.InDog)
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
            else if (status == 1)
            {
                _status.text = UiStrings.Get(UiKeys.Battle.Dead);
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
