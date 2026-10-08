using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A member's board between battles (2026-10-08 round 47, split out of <see cref="PartyColumnView"/> so that the battle screen can
    /// show it after a win): in a column of the board panel, the board's head (the row and the name, with the fatigue the board's
    /// equipment costs at its right end; round 32, B1) and the item board stacked on its bag, whose cells are
    /// <see cref="ItemSlotView"/>s with the battle's icons. A cell takes a click (<see cref="CellClicked"/>: the first cell of an item,
    /// or an empty cell) and a right click (<see cref="CellRightClicked"/>: the item's card).
    /// </summary>
    public sealed class PartyBoardView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] ItemSlotView _slotTemplate;
        [SerializeField] RectTransform _slotParent;
        [SerializeField] GameObject _fatigueTotal;
        [SerializeField] TMP_Text _fatigueTotalText;

        /// <summary>The fatigue tag at the head's right end is as wide as its words and this much on either side.</summary>
        const float FatigueTotalPad = 8f;

        readonly List<ItemSlotView> _views = new List<ItemSlotView>();

        /// <summary>The cell views in board order: one per item, then one per empty cell. The rest are hidden.</summary>
        public IReadOnlyList<ItemSlotView> Slots => _views;

        /// <summary>The height of the board on show: the member's cells stacked. The bag behind them is as long.</summary>
        public float BoardHeight => _slotParent.sizeDelta.y;

        /// <summary>The words on the head's fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTotal => _fatigueTotal.activeSelf ? _fatigueTotalText.text : string.Empty;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked;

        /// <summary>A cell of the board was right-clicked (round 47): the item's card.</summary>
        public event Action<int> CellRightClicked;

        /// <param name="art">Where the icons of the items come from.</param>
        /// <param name="selectedCell">The first cell of the highlighted item, or -1.</param>
        /// <param name="canClickCell">Whether a cell (the first of an item, or an empty one) takes a click now.</param>
        /// <param name="mergesCell">Whether the chosen item would merge into the item at a cell (round 35): the cell is marked.</param>
        public void Show(ExpeditionMember member, ExpeditionArt art, int selectedCell, Func<int, bool> canClickCell, Func<int, bool> mergesCell)
        {
            gameObject.SetActive(true);
            BalanceData balance = Managers.Data.Data.Balance;
            _name.text = UiText.Mercenary(member.MercenaryId);

            // The fatigue the board's equipment costs when a battle starts, in all; nothing when it costs nothing.
            int fatigue = FatigueRules.EquipmentCost(balance, member.Items);
            _fatigueTotal.SetActive(fatigue > 0);
            if (fatigue > 0)
            {
                _fatigueTotalText.text = UiStrings.Get(UiKeys.Board.FatigueTotal, fatigue);
                var tag = (RectTransform)_fatigueTotal.transform;
                tag.sizeDelta = new Vector2(Mathf.Ceil(_fatigueTotalText.GetPreferredValues(_fatigueTotalText.text).x) + 2f * FatigueTotalPad, tag.sizeDelta.y);
            }

            // The board is as high as the member's cells, so the bag behind the cells is as long as the board.
            _slotParent.sizeDelta = new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(member.ItemSlots));

            // One view per cell at most; whoever stands here decides how many are used.
            while (_views.Count < member.ItemSlots)
            {
                ItemSlotView view = Instantiate(_slotTemplate, _slotParent);
                ItemSlotView clicked = view;
                view.Button.onClick.AddListener(() => CellClicked?.Invoke(clicked.Index));
                view.RightClick.Clicked += () => CellRightClicked?.Invoke(clicked.Index);
                _views.Add(view);
            }

            // Views in board order from the top: one per item, as high as its cells, then one per empty cell.
            int next = 0;
            int cell = 0;
            foreach (EquippedItem item in member.Items)
            {
                int size = item.Item.Size;
                ShowCell(_views[next++], cell, item, art.OfItem(item.Item.Id), BattleItemView.BoardHeight(size), selectedCell, canClickCell, FatigueRules.ItemCost(balance, item), mergesCell(cell));
                cell += size;
            }

            for (; cell < member.ItemSlots; cell++)
            {
                ShowCell(_views[next++], cell, null, null, BattleItemView.CellHeight, selectedCell, canClickCell, 0, false);
            }

            for (; next < _views.Count; next++)
            {
                _views[next].gameObject.SetActive(false);
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>The shown cell whose first cell is the given one, or null.</summary>
        public ItemSlotView SlotAt(int cell)
        {
            foreach (ItemSlotView view in _views)
            {
                if (view.gameObject.activeSelf && view.Index == cell)
                {
                    return view;
                }
            }

            return null;
        }

        static void ShowCell(ItemSlotView view, int cell, EquippedItem item, Sprite icon, float height, int selectedCell, Func<int, bool> canClickCell, int fatigueCost, bool merges)
        {
            view.Index = cell;
            view.SetHeight(height);
            view.Show(item, icon, cell == selectedCell, canClickCell(cell), fatigueCost, merges);
            view.gameObject.SetActive(true);
        }
    }
}
