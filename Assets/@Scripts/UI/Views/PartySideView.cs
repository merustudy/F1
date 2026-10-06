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
    /// (row 1 on the right) with the figure, the marks and the move buttons under it, placed where
    /// the battle screen places its party columns (<see cref="FieldLayout"/>); in the board panel
    /// under the stage, each row's board stacked in the panel column under its figure, as in
    /// battle; the potions in the strip above the right half; and the inventory as a popup over
    /// the other half of the screen under the potions, above the panel. Clicking an item and then a cell moves it or trades places;
    /// "to inventory" takes the clicked item off its board; an inventory item and then a cell puts
    /// it on a board (whatever was there goes to the inventory). The inventory has a fixed number
    /// of cells, so what would not fit there is not offered. Which cells and buttons take a click
    /// is asked of the manager, cell by cell. Clicking the name of a member's state under its feet
    /// (round 36) explains the state on the detail line. The node map and the reward screen both show it.
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

        // At most one of these is selected: an item on a board (member and its first cell), an item of the inventory,
        // a potion (whose words then show on the detail line; the potions cannot be used here), or a member's state (the same).
        int _selectedMember = -1;
        int _selectedCell = -1;
        int _selectedInventory = -1;
        int _selectedPotion = -1;
        int _selectedState = -1;
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

        /// <summary>With <see cref="ExternalCanPlace"/>: which cells the screen's chosen item would merge into (the reward screen's item). Null for none.</summary>
        public Func<int, int, bool> ExternalMerges { get; set; }

        /// <summary>
        /// With <see cref="ExternalCanPlace"/>: the cell the screen has chosen on the boards (the camp's item to mend), shown as the
        /// brass cell and described on the detail line. -1 for none.
        /// </summary>
        public int ExternalSelectedMember { get; set; } = -1;
        public int ExternalSelectedCell { get; set; } = -1;

        /// <summary>What the detail line says while nothing is chosen, in place of how to use the boards. Null for the usual words.</summary>
        public string DetailOverride { get; set; }

        /// <summary>Whether the inventory popup is open.</summary>
        public bool InventoryOpen { get; private set; }

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
                column.StateClicked += () => OnStateClicked(column.Member);
            }

            for (int i = 0; i < expedition.Potions.Length; i++)
            {
                PotionSlotView view = Instantiate(_potionTemplate, _potionParent);
                view.gameObject.SetActive(true);
                int slot = i;
                view.Button.onClick.AddListener(() => OnPotionClicked(slot));
                _potions.Add(view);
            }

            _toInventory.onClick.AddListener(OnToInventoryClicked);
            _inventoryPanel.SetActive(false);
        }

        /// <summary>
        /// Puts each column where the battle screen puts the party column of the same row for a
        /// party of this size, and the row's column of the panel right under it (the panel spans
        /// the frame, so it is offset by the field's left edge). The rows the party cannot stand
        /// in are not shown, as in battle: neither their column on the stage nor their column in the panel.
        /// </summary>
        void LayoutColumns(int partyRows)
        {
            float width = FieldLayout.ColumnWidth(_field.rect.width, partyRows, BattleRows.Count);
            float panelLeft = _field.anchoredPosition.x;
            for (int i = 0; i < _columns.Length; i++)
            {
                bool used = i < partyRows;
                _columns[i].gameObject.SetActive(used);
                _columns[i].Board.SetActive(used);
                if (used)
                {
                    float x = FieldLayout.PartyColumnX(width, partyRows, i);
                    var column = (RectTransform)_columns[i].transform;
                    column.anchoredPosition = new Vector2(x, column.anchoredPosition.y);
                    column.sizeDelta = new Vector2(width, column.sizeDelta.y);
                    var board = (RectTransform)_columns[i].Board.transform;
                    board.anchoredPosition = new Vector2(panelLeft + x, board.anchoredPosition.y);
                    board.sizeDelta = new Vector2(width, board.sizeDelta.y);
                }
            }
        }

        public void ClearSelection()
        {
            _selectedMember = -1;
            _selectedCell = -1;
            _selectedInventory = -1;
            _selectedPotion = -1;
            _selectedState = -1;
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
                int selectedMember = external ? ExternalSelectedMember : _selectedMember;
                int selectedCell = external ? ExternalSelectedCell : _selectedCell;
                EquippedItem chosen = external ? null : SelectedItem(expedition);
                column.Show(
                    m,
                    expedition.Members[m],
                    _art,
                    manager.CanMoveToRow(m, row - 1),
                    manager.CanMoveToRow(m, row + 1),
                    m == selectedMember ? selectedCell : -1,
                    cell => external ? ExternalCanPlace(member, cell) : CanClickCell(member, cell),
                    cell => external ? ExternalMerges != null && ExternalMerges(member, cell) : chosen != null && manager.MergesAt(chosen, member, cell));
            }

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), potionId == null ? null : _art.OfPotion(potionId), i == _selectedPotion, potionId != null);
            }

            _inventoryPanel.SetActive(InventoryOpen);
            if (InventoryOpen)
            {
                RefreshInventory(expedition, data.Balance.InventoryCells, !external);
            }

            _toInventory.interactable = !external && _selectedMember >= 0 && manager.CanMoveToInventory(_selectedMember, _selectedCell);

            EquippedItem selected = external ? ExternalSelectedItem(expedition) : SelectedItem(expedition);
            string chosenPotion = _selectedPotion >= 0 ? expedition.Potions[_selectedPotion] : null;
            string chosenState = _selectedState >= 0 && _selectedState < expedition.Members.Count ? expedition.Members[_selectedState].StateId : null;
            if (chosenPotion != null)
            {
                PotionData potion = data.Potions.Get(chosenPotion);
                _detail.text = UiText.Name(potion.Name) + " — " + UiText.PotionDetails(potion);
            }
            else if (chosenState != null)
            {
                // The state whose name was clicked under a member's feet: what it does (round 36).
                _detail.text = UiText.FatigueStateDetail(data.FatigueStates.Get(chosenState));
            }
            else if (selected == null)
            {
                _detail.text = DetailOverride ?? UiStrings.Get(UiKeys.Board.Hint);
            }
            else
            {
                // The chosen item's facts; and, while a board holds what it merges into, what putting it there does (round 35).
                string detail = UiText.ItemTitle(selected) + " — " + UiText.ItemSummary(selected);
                if (!external && manager.HasMergeTarget(selected))
                {
                    detail += "\n" + UiText.MergeHint(selected);
                }

                _detail.text = detail;
            }
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

        /// <summary>The item at the cell the owning screen has chosen, or null.</summary>
        EquippedItem ExternalSelectedItem(ExpeditionState expedition)
        {
            if (ExternalSelectedMember < 0 || ExternalSelectedMember >= expedition.Members.Count)
            {
                return null;
            }

            List<EquippedItem> board = expedition.Members[ExternalSelectedMember].Items;
            int index = ItemBoard.IndexAtCell(board, ExternalSelectedCell);
            return index < 0 ? null : board[index];
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

        /// <summary>A potion's slot shows its words on the detail line while it is chosen; clicking it again puts it down. Nothing else changes: potions are used in battle.</summary>
        void OnPotionClicked(int slot)
        {
            bool again = slot == _selectedPotion;
            ClearSelection();
            _selectedPotion = again ? -1 : slot;
            Refresh();
        }

        /// <summary>The name of a member's state under its feet shows what the state does on the detail line while it is chosen; clicking it again puts it down (round 36).</summary>
        void OnStateClicked(int member)
        {
            if (member < 0)
            {
                return;
            }

            bool again = member == _selectedState;
            ClearSelection();
            _selectedState = again ? -1 : member;
            Refresh();
        }

        /// <summary>
        /// A click on a cell picks an item up, puts it down or moves it. The cells are built silent: the click sounds here,
        /// once, as what it did: an item that went into a cell sounds as put in, anything else as a click.
        /// </summary>
        void OnCellClicked(int member, int cell)
        {
            if (member < 0)
            {
                return;
            }

            // The screen that overrides the click (a reward put in a cell) makes its own sound.
            if (CellClickOverride != null && CellClickOverride(member, cell))
            {
                return;
            }

            _selectedPotion = -1;
            _selectedState = -1;
            ExpeditionManager manager = Managers.Expedition;
            SoundEffect sound = SoundEffect.Button;
            if (_selectedInventory >= 0)
            {
                if (manager.CanPlaceFromInventory(_selectedInventory, member, cell))
                {
                    manager.PlaceFromInventory(_selectedInventory, member, cell);
                    sound = SoundEffect.ItemPlace;
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
                    sound = SoundEffect.ItemPlace;
                }

                ClearSelection();
            }

            Managers.Sound.PlayEffect(sound);
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
                Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            }

            ClearSelection();
            Refresh();
        }
    }
}
