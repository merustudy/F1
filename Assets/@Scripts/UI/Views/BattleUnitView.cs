using System.Collections.Generic;
using System.Globalization;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One living unit in battle: the figure (its art, or a placeholder when it has none), the
    /// plate under its feet and its item cells under the plate, each cell showing its item's icon. The plate holds the row the unit
    /// stands in, its name, its HP, and a line of states: shield and burn as an icon with a
    /// number, or the death's door state. The plate's color says whose side the unit is on, that
    /// it is at death's door or that a potion can be used on it. It shows what the engine says;
    /// texts are rebuilt only when their numbers change. The battle screen takes a dead unit off
    /// the field.
    /// </summary>
    public sealed class BattleUnitView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] FigureView _figureView;
        [SerializeField] Image _plate;
        [SerializeField] Sprite _plateParty;
        [SerializeField] Sprite _plateEnemy;
        [SerializeField] Sprite _plateDanger;
        [SerializeField] Sprite _plateTarget;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] GameObject _shieldChip;
        [SerializeField] TMP_Text _shield;
        [SerializeField] GameObject _burnChip;
        [SerializeField] TMP_Text _burn;
        [SerializeField] GameObject _statusChip;
        [SerializeField] TMP_Text _status;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] GameObject _emptyCellTemplate;
        [SerializeField] Transform _itemParent;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        ExpeditionArt _art;
        int _shownRow = -1;
        int _shownHp = -1;
        int _shownShield = -1;
        int _shownBurn = -1;
        long _shownStatus = -1;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The unit's art, or its placeholder.</summary>
        public FigureView Figure => _figureView;

        /// <summary>The plate on show: the one of the unit's side or state.</summary>
        public Sprite Plate => _plate.sprite;

        /// <param name="figure">The unit's art, or null when it has none.</param>
        /// <param name="art">Where the icons of the items come from.</param>
        public void Bind(BattleUnit unit, Sprite figure, ExpeditionArt art)
        {
            _unit = unit;
            _art = art;
            _name.text = UiText.Name(unit.Setup.Name);
            _figureView.Show(figure);

            // The board in order: an item takes as many cells as its size and shows its cooldown;
            // the cells after the last item stay faint. An enemy's icons point at the party.
            int cells = 0;
            foreach (BattleItemState item in unit.Items)
            {
                BattleItemView view = Instantiate(_itemTemplate, _itemParent);
                view.gameObject.SetActive(true);
                BindItem(view, item);
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
                BindItem(_items[i], _unit.Items[i]);
            }
        }

        void BindItem(BattleItemView view, BattleItemState item)
        {
            ItemData data = item.Equipped.Item;
            view.Bind(item, data.Size, _art.OfItem(data.Id), mirrored: _unit.Side == BattleSide.Enemy);
        }

        /// <param name="targetable">True while a potion is waiting for this unit to be clicked.</param>
        public void Render(BattleEngine engine, bool targetable)
        {
            BalanceData balance = engine.Setup.Balance;
            int timeMs = engine.TimeMs;

            // The unit moves to another column when it advances; its badge follows.
            if (_unit.Row != _shownRow)
            {
                _shownRow = _unit.Row;
                _row.text = _unit.Row.ToString(CultureInfo.InvariantCulture);
            }

            if (_unit.Hp != _shownHp)
            {
                _shownHp = _unit.Hp;
                _hp.text = UiStrings.Get(UiKeys.Battle.Hp, _unit.Hp, _unit.MaxHp);
                _hpBar.Set(_unit.Hp, _unit.MaxHp);
            }

            if (_unit.Shield != _shownShield)
            {
                _shownShield = _unit.Shield;
                _shield.text = UiStrings.Get(UiKeys.Battle.Shield, _unit.Shield);
            }

            if (_unit.Burn != _shownBurn)
            {
                _shownBurn = _unit.Burn;
                _burn.text = UiStrings.Get(UiKeys.Battle.Burn, _unit.Burn);
            }

            RenderStatus(balance, timeMs);

            // The state line holds either the death's door state or what the unit carries.
            SetShown(_statusChip, _unit.InDog);
            SetShown(_shieldChip, !_unit.InDog && _unit.Shield > 0);
            SetShown(_burnChip, !_unit.InDog && _unit.Burn > 0);

            _button.interactable = targetable && _unit.Alive;
            Sprite plate;
            if (_unit.InDog)
            {
                plate = _plateDanger;
            }
            else if (targetable)
            {
                plate = _plateTarget;
            }
            else
            {
                plate = _unit.Side == BattleSide.Party ? _plateParty : _plateEnemy;
            }

            if (_plate.sprite != plate)
            {
                _plate.sprite = plate;
            }

            foreach (BattleItemView item in _items)
            {
                item.Render(timeMs, _unit.Alive);
            }
        }

        static void SetShown(GameObject chip, bool shown)
        {
            if (chip.activeSelf != shown)
            {
                chip.SetActive(shown);
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
