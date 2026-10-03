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
    /// One unit's line of the board panel under the stage: its face at the end of the line (the
    /// party's at the left edge of the panel, the enemy's at the right edge) with the row it
    /// stands in on a badge, and its item cells side by side from the face towards the middle,
    /// each the cooldown gauge of its item (<see cref="BattleItemView"/>), on a bag as long as the
    /// cells the unit has. The line sits in the panel's line of the unit's row, so it moves when
    /// the unit advances and leaves the panel with the dead, as the figure leaves the stage. The face's frame says whose side the unit is
    /// on, that it is at death's door or that a potion can be used on it, like the plate under the
    /// figure; the whole line is the button a potion is aimed at.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] HorizontalLayoutGroup _layout;
        [SerializeField] HorizontalLayoutGroup _cellsLayout;
        [SerializeField] RectTransform _cells;
        [SerializeField] Image _frame;
        [SerializeField] Sprite _plateParty;
        [SerializeField] Sprite _plateEnemy;
        [SerializeField] Sprite _plateDanger;
        [SerializeField] Sprite _plateTarget;
        [SerializeField] Image _face;
        [SerializeField] GameObject _facePlaceholder;
        [SerializeField] TMP_Text _row;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] GameObject _emptyCellTemplate;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        ExpeditionArt _art;
        int _shownRow = -1;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The frame of the face on show: the plate of the unit's side or state.</summary>
        public Sprite Frame => _frame.sprite;

        /// <summary>The face on show, or null while the placeholder stands in.</summary>
        public Sprite Face => _face.enabled ? _face.sprite : null;

        /// <summary>The cells of the items in board order. The empty cells after them are not views.</summary>
        public IReadOnlyList<BattleItemView> Items => _items;

        /// <summary>True when the line reads from the right: an enemy's.</summary>
        public bool Mirrored => _layout.reverseArrangement;

        /// <summary>The width of the board: the unit's cells side by side. The bag behind them is as long.</summary>
        public float BoardWidth => _cells.sizeDelta.x;

        /// <param name="face">The face cut out of the unit's figure, or null when it has none.</param>
        /// <param name="art">Where the icons of the items come from.</param>
        public void Bind(BattleUnit unit, Sprite face, ExpeditionArt art)
        {
            _unit = unit;
            _art = art;
            _face.sprite = face;
            _face.enabled = face != null;
            _facePlaceholder.SetActive(face == null);

            // The party's line reads from its face at the left end; the enemy's line is its mirror image,
            // the face at the right end and the first item next to it.
            bool enemy = unit.Side == BattleSide.Enemy;
            TextAnchor alignment = enemy ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            _layout.reverseArrangement = enemy;
            _layout.childAlignment = alignment;
            _cellsLayout.reverseArrangement = enemy;
            _cellsLayout.childAlignment = alignment;
            _cells.sizeDelta = new Vector2(BattleItemView.BoardWidth(unit.Setup.ItemSlots), BattleItemView.CellHeight);

            // The board in order: an item takes as many cells as its size and shows its cooldown;
            // the cells after the last item stay faint.
            int cells = 0;
            foreach (BattleItemState item in unit.Items)
            {
                BattleItemView view = Instantiate(_itemTemplate, _cells);
                view.gameObject.SetActive(true);
                BindItem(view, item);
                _items.Add(view);
                cells += item.Equipped.Item.Size;
            }

            for (; cells < unit.Setup.ItemSlots; cells++)
            {
                Instantiate(_emptyCellTemplate, _cells).SetActive(true);
            }
        }

        /// <summary>The cell of the item in this slot of the board, or null when the unit has no such item.</summary>
        public BattleItemView ItemOfSlot(int slotIndex)
        {
            for (int i = 0; i < _items.Count; i++)
            {
                if (_unit.Items[i].SlotIndex == slotIndex)
                {
                    return _items[i];
                }
            }

            return null;
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
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
            // The line moves to another line of the panel when the unit advances; its badge follows.
            if (_unit.Row != _shownRow)
            {
                _shownRow = _unit.Row;
                _row.text = _unit.Row.ToString(CultureInfo.InvariantCulture);
            }

            _button.interactable = targetable && _unit.Alive;
            Sprite frame = BattleUnitView.PlateFor(_unit, targetable, _plateParty, _plateEnemy, _plateDanger, _plateTarget);
            if (_frame.sprite != frame)
            {
                _frame.sprite = frame;
            }

            foreach (BattleItemView item in _items)
            {
                item.Render(engine.TimeMs, _unit.Alive);
            }
        }
    }
}
