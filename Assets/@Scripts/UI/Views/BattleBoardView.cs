using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One unit's board of the board panel under the stage: its item cells stacked top to bottom,
    /// each the cooldown gauge of its item (<see cref="BattleItemView"/>), on a bag as long as the
    /// cells the unit has. The board sits in the panel's column of the row the unit stands in,
    /// right under its figure and plate (2026-10-03 mockup V), so it moves when the unit advances
    /// and leaves the panel with the dead, as the figure leaves the stage. The plate above says
    /// whose side the unit is on and what state it is in; the whole board is the button a potion
    /// is aimed at.
    /// </summary>
    public sealed class BattleBoardView : MonoBehaviour
    {
        [SerializeField] Button _button;
        [SerializeField] RectTransform _cells;
        [SerializeField] BattleItemView _itemTemplate;
        [SerializeField] GameObject _emptyCellTemplate;

        readonly List<BattleItemView> _items = new List<BattleItemView>();
        BattleUnit _unit;
        ExpeditionArt _art;

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
            _cells.sizeDelta = new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(unit.Setup.ItemSlots));

            // The board in order from the top: an item takes as many cells as its size and shows
            // its cooldown; the cells after the last item stay faint.
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
            _button.interactable = targetable && _unit.Alive;
            foreach (BattleItemView item in _items)
            {
                item.Render(engine.TimeMs, _unit.Alive);
            }
        }
    }
}
