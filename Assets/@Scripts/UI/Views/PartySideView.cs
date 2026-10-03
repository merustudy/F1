using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The party between battles, in the battle screen's shape: on the stage, one column per row
    /// (row 1 on the right) with the figure, the plate and the move buttons under it, placed where
    /// the battle screen places its party columns (<see cref="FieldLayout"/>); in the board panel
    /// under the stage, one line per row with the face and the board side by side, as in battle;
    /// the potions in the strip under the header; and the inventory as a popup over the other half
    /// of the screen above the panel. Clicking an item and then a cell moves it or trades places;
    /// "to inventory" takes the clicked item off its board; an inventory item and then a cell puts
    /// it on a board (whatever was there goes to the inventory). The inventory has a fixed number
    /// of cells, so what would not fit there is not offered. Which cells and buttons take a click
    /// is asked of the manager, cell by cell. The node map and the reward screen both show it.
    /// </summary>
    public sealed class PartySideView : MonoBehaviour
    {
        [SerializeField] RectTransform _field;
        [SerializeField] PartyColumnView[] _columns;
        [SerializeField] PotionSlotView _potionTemplate;
        [SerializeField] Transform _potionParent;
        [SerializeField] TMP_Text _detail;
        [SerializeField] Button _toInventory;
        [SerializeField] GameObject _inventoryPanel;
        [SerializeField] TMP_Text _inventoryTitle;
        [SerializeField] GameObject _inventoryEmpty;
        [SerializeField] InventoryEntryView _entryTemplate;
        [SerializeField] Transform _entryParent;

        readonly List<PotionSlotView> _potions = new List<PotionSlotView>();
        readonly List<InventoryEntryView> _entries = new List<InventoryEntryView>();

        // At most one of the two is selected: an item on a board (member and its first cell) or an item of the inventory.
        int _selectedMember = -1;
        int _selectedCell = -1;
        int _selectedInventory = -1;
        ExpeditionArt _art;

        /// <summary>
        /// Set by a screen that holds a selection of its own (the reward screen's chosen item): which
        /// cells take it. While it is set, the view's own selection is off, its cells are enabled by
        /// this alone and the inventory popup does not take clicks.
        /// </summary>
        public Func<int, int, bool> ExternalCanPlace { get; set; }

        /// <summary>
        /// Lets the owning screen take over a cell click (member index, cell). When it returns true
        /// the view does nothing; the reward screen uses this to place a chosen item.
        /// </summary>
        public Func<int, int, bool> CellClickOverride { get; set; }

        /// <summary>Whether the inventory popup is open.</summary>
        public bool InventoryOpen { get; private set; }

        /// <summary>The columns by row: index 0 is row 1.</summary>
        public IReadOnlyList<PartyColumnView> Columns => _columns;

        /// <summary>The entries of the inventory popup in inventory order; the ones beyond the inventory are hidden.</summary>
        public IReadOnlyList<InventoryEntryView> InventoryEntries => _entries;

        public Button ToInventory => _toInventory;
        public GameObject InventoryPanel => _inventoryPanel;

        public PartyColumnView ColumnOfRow(int row)
        {
            return _columns[row - BattleRows.Front];
        }

        /// <summary>Places and wires the columns and makes the potion slots. Called once by the owning screen.</summary>
        /// <param name="art">The art the owning screen loaded before it opened.</param>
        public void Open(ExpeditionArt art)
        {
            _art = art;
            ExpeditionState expedition = Managers.Expedition.Expedition;
            LayoutColumns(Mathf.Min(Managers.Data.Data.Balance.PartySize, _columns.Length));
            for (int i = 0; i < _columns.Length; i++)
            {
                PartyColumnView column = _columns[i];
                column.Forward.onClick.AddListener(() => OnMoveClicked(column, -1));
                column.Back.onClick.AddListener(() => OnMoveClicked(column, 1));
                column.CellClicked += cell => OnCellClicked(column.Member, cell);
            }

            for (int i = 0; i < expedition.Potions.Length; i++)
            {
                PotionSlotView view = Instantiate(_potionTemplate, _potionParent);
                view.gameObject.SetActive(true);
                _potions.Add(view);
            }

            _toInventory.onClick.AddListener(OnToInventoryClicked);
            _inventoryPanel.SetActive(false);
        }

        /// <summary>
        /// Puts each column where the battle screen puts the party column of the same row for a
        /// party of this size. The rows the party cannot stand in are not shown, as in battle:
        /// neither their column on the stage nor their line in the panel.
        /// </summary>
        void LayoutColumns(int partyRows)
        {
            float width = FieldLayout.ColumnWidth(_field.rect.width, partyRows, BattleRows.Count);
            for (int i = 0; i < _columns.Length; i++)
            {
                bool used = i < partyRows;
                _columns[i].gameObject.SetActive(used);
                _columns[i].Line.SetActive(used);
                if (used)
                {
                    var column = (RectTransform)_columns[i].transform;
                    column.anchoredPosition = new Vector2(FieldLayout.PartyColumnX(width, partyRows, i), column.anchoredPosition.y);
                    column.sizeDelta = new Vector2(width, column.sizeDelta.y);
                }
            }
        }

        public void ClearSelection()
        {
            _selectedMember = -1;
            _selectedCell = -1;
            _selectedInventory = -1;
        }

        /// <summary>
        /// Opens or closes the inventory popup. Closing it drops whatever was picked there. The
        /// owning screen refreshes afterwards (its toggle label changes too), which refreshes this view.
        /// </summary>
        public void ToggleInventory()
        {
            InventoryOpen = !InventoryOpen;
            _selectedInventory = -1;
        }

        public void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;
            bool external = ExternalCanPlace != null;

            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                PartyColumnView column = ColumnOfRow(row);
                int m = MemberInRow(expedition, row);
                if (m < 0)
                {
                    column.Clear();
                    continue;
                }

                int member = m;
                column.Show(
                    m,
                    expedition.Members[m],
                    _art,
                    manager.CanMoveToRow(m, row - 1),
                    manager.CanMoveToRow(m, row + 1),
                    m == _selectedMember ? _selectedCell : -1,
                    cell => external ? ExternalCanPlace(member, cell) : CanClickCell(member, cell));
            }

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), false, false);
            }

            _inventoryPanel.SetActive(InventoryOpen);
            if (InventoryOpen)
            {
                RefreshInventory(expedition, data.Balance.InventoryCells, !external);
            }

            _toInventory.interactable = !external && _selectedMember >= 0 && manager.CanMoveToInventory(_selectedMember, _selectedCell);

            EquippedItem selected = SelectedItem(expedition);
            _detail.text = selected == null
                ? UiStrings.Get(UiKeys.Board.Hint)
                : UiText.ItemTitle(selected) + " — " + UiText.ItemSummary(selected);
        }

        void RefreshInventory(ExpeditionState expedition, int cells, bool interactable)
        {
            _inventoryTitle.text = UiStrings.Get(UiKeys.Board.InventoryTitle, ItemBoard.UsedCells(expedition.Inventory), cells);
            while (_entries.Count < expedition.Inventory.Count)
            {
                InventoryEntryView view = Instantiate(_entryTemplate, _entryParent);
                InventoryEntryView clicked = view;
                view.Button.onClick.AddListener(() => OnInventoryClicked(clicked.Index));
                _entries.Add(view);
            }

            for (int i = 0; i < _entries.Count; i++)
            {
                bool shown = i < expedition.Inventory.Count;
                _entries[i].gameObject.SetActive(shown);
                if (shown)
                {
                    _entries[i].Index = i;
                    _entries[i].Show(expedition.Inventory[i], i == _selectedInventory, interactable);
                }
            }

            _inventoryEmpty.SetActive(expedition.Inventory.Count == 0);
        }

        /// <summary>The living member standing in a row, or -1. The dead keep a row but are not shown.</summary>
        static int MemberInRow(ExpeditionState expedition, int row)
        {
            for (int i = 0; i < expedition.Members.Count; i++)
            {
                ExpeditionMember member = expedition.Members[i];
                if (member.Alive && member.Row == row)
                {
                    return i;
                }
            }

            return -1;
        }

        EquippedItem SelectedItem(ExpeditionState expedition)
        {
            if (_selectedInventory >= 0 && _selectedInventory < expedition.Inventory.Count)
            {
                return expedition.Inventory[_selectedInventory];
            }

            if (_selectedMember < 0)
            {
                return null;
            }

            List<EquippedItem> board = expedition.Members[_selectedMember].Items;
            int index = ItemBoard.IndexAtCell(board, _selectedCell);
            return index < 0 ? null : board[index];
        }

        /// <summary>Whether a cell takes a click with the view's own selection: it holds an item, or it can receive the selected one.</summary>
        bool CanClickCell(int member, int cell)
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_selectedInventory >= 0)
            {
                return manager.CanPlaceFromInventory(_selectedInventory, member, cell);
            }

            if (_selectedMember >= 0)
            {
                bool itself = member == _selectedMember && cell == _selectedCell;
                return itself || manager.CanMoveItem(_selectedMember, _selectedCell, member, cell);
            }

            // Nothing is selected: only a cell that holds an item can be picked up.
            return manager.CanPickItem(member, cell);
        }

        /// <param name="step">-1 moves the member one row forward, 1 one row back.</param>
        void OnMoveClicked(PartyColumnView column, int step)
        {
            if (column.Member < 0)
            {
                return;
            }

            ExpeditionManager manager = Managers.Expedition;
            int row = manager.Expedition.Members[column.Member].Row + step;
            if (manager.CanMoveToRow(column.Member, row))
            {
                manager.MoveToRow(column.Member, row);
                Refresh();
            }
        }

        void OnCellClicked(int member, int cell)
        {
            if (member < 0)
            {
                return;
            }

            if (CellClickOverride != null && CellClickOverride(member, cell))
            {
                return;
            }

            ExpeditionManager manager = Managers.Expedition;
            if (_selectedInventory >= 0)
            {
                if (manager.CanPlaceFromInventory(_selectedInventory, member, cell))
                {
                    manager.PlaceFromInventory(_selectedInventory, member, cell);
                }

                ClearSelection();
            }
            else if (_selectedMember < 0)
            {
                if (manager.CanPickItem(member, cell))
                {
                    _selectedMember = member;
                    _selectedCell = cell;
                }
            }
            else if (member == _selectedMember && cell == _selectedCell)
            {
                ClearSelection();
            }
            else
            {
                if (manager.CanMoveItem(_selectedMember, _selectedCell, member, cell))
                {
                    manager.MoveItem(_selectedMember, _selectedCell, member, cell);
                }

                ClearSelection();
            }

            Refresh();
        }

        void OnInventoryClicked(int index)
        {
            if (ExternalCanPlace != null)
            {
                return;
            }

            bool again = index == _selectedInventory;
            ClearSelection();
            _selectedInventory = again ? -1 : index;
            Refresh();
        }

        void OnToInventoryClicked()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_selectedMember >= 0 && manager.CanMoveToInventory(_selectedMember, _selectedCell))
            {
                manager.MoveToInventory(_selectedMember, _selectedCell);
            }

            ClearSelection();
            Refresh();
        }
    }
}
