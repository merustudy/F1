using System;
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
    /// One unit's board of the board panel under the stage: its head, with the row the unit stands
    /// in on a gold stud and its name (a monster's in a pale red, 2026-10-05 round 29), and under it
    /// its item cells stacked top to bottom, each the cooldown gauge of its item
    /// (<see cref="BattleItemView"/>), on a bag as long as the cells the unit has. The board sits in
    /// the panel's column of the row the unit stands in, right under its figure and marks (2026-10-03
    /// mockup V), so it moves when the unit advances and leaves the panel with the dead, as the
    /// figure leaves the stage. The row on the head is the row of the column the screen has put the
    /// board in, which can be behind the engine for a moment: while a fallen mercenary's grave stands,
    /// the party keeps its places (2026-10-04, round 21). The whole board is the button a potion is
    /// aimed at.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _name;
        [SerializeField] RectTransform _cells;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] GameObject _emptyCellTemplate;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        ExpeditionArt _art;
        int _standRow;
        int _shownRow = -1;

        public Button Button => _button;
        public BattleUnit Unit => _unit;

        /// <summary>The cells of the items in board order. The empty cells after them are not views.</summary>
        public IReadOnlyList<BattleItemView> Items => _items;

        /// <summary>The height of the board: the unit's cells stacked. The bag behind them is as long.</summary>
        public float BoardHeight => _cells.sizeDelta.y;

        /// <param name="art">Where the icons of the items come from.</param>
        public void Bind(BattleUnit unit, ExpeditionArt art)
        {
            _unit = unit;
            _art = art;
            _standRow = unit.Row;
            _name.text = UiText.Name(unit.Setup.Name);
            _name.color = unit.Side == BattleSide.Enemy ? UiPalette.EnemyName : UiPalette.Text;
            _cells.sizeDelta = new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(unit.Setup.ItemSlots));

            // The board in order from the top: an item takes as many cells as its size and shows
            // its cooldown; the cells after the last item stay faint.
            int cells = 0;
            foreach (BattleItemState item in unit.Items)
            {
                BattleItemView view = Instantiate(_itemTemplate, _cells);
                view.gameObject.SetActive(true);
                BindItem(view, item);
                BattleItemView clicked = view;
                BattleItemState state = item;
                view.RightClick.Clicked += () => ItemRightClicked?.Invoke(clicked, state);
                _items.Add(view);
                cells += item.Equipped.Item.Size;
            }

            for (; cells < unit.Setup.ItemSlots; cells++)
            {
                Instantiate(_emptyCellTemplate, _cells).SetActive(true);
            }
        }

        /// <summary>A right click on an item's cell (round 42, right-click since round 47): the screen opens the item's card.</summary>
        public event Action<BattleItemView, BattleItemState> ItemRightClicked;

        /// <summary>Round 42: whether the cells take clicks for their cards. Off while a potion waits for this board to be clicked.</summary>
        public void SetItemsClickable(bool clickable)
        {
            foreach (BattleItemView item in _items)
            {
                item.SetClickable(clickable);
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

        /// <summary>The screen has put the board in the column of this row: its head shows the row.</summary>
        public void StandIn(int row)
        {
            _standRow = row;
        }

        /// <summary>Forgets what was drawn, so the next render rebuilds every text (after a locale change).</summary>
        public void Invalidate()
        {
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
            if (_standRow != _shownRow)
            {
                _shownRow = _standRow;
                _row.text = _standRow.ToString(CultureInfo.InvariantCulture);
            }

            _button.interactable = targetable && _unit.Alive;
            foreach (BattleItemView item in _items)
            {
                item.Render(engine.TimeMs, _unit.Alive);
            }
        }
    }
}
