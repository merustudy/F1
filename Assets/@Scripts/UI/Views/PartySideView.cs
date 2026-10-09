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
    /// under the stage, each row's board as a grid (Slice B stage 19) in the panel column under its figure, as in
    /// battle; the potions in the strip above; and the inventory as a popup over the other half of the screen, above the panel.
    /// The boards are used with a hand (<see cref="BoardHand"/>): a click on an item picks it up and a click on a square puts it
    /// where its ghost shows (one item in the way goes to the inventory); a click on an empty square of a bag picks the bag up with
    /// what lies in it; while something is held, the right button, the wheel or R turns it (Backpack Battles' turning) and Escape lets
    /// go. "To inventory" takes the held board item off its board to the inventory's first room. The inventory is a grid of its own in the
    /// popup (round 49, Diablo II's): an item there is picked by a click and put on a board, or elsewhere on the grid, the same way, and a
    /// board item held can be laid on free squares of the grid; what would have no room there is not offered. While an item is held, the
    /// popup shows it in Diablo's tooltip. Which squares take what is asked of the manager.
    /// Clicking the name of a member's state under its feet (round 36) explains the state on the detail line. A right click on an
    /// item with nothing held opens its card (round 47). The node map shows it.
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
        [SerializeField] InventoryGridView _inventoryGrid;
        [SerializeField] TMP_Text _inventoryCoins;
        [SerializeField] GameObject _inventoryInfoBox;
        [SerializeField] TMP_Text _inventoryInfo;
        [SerializeField] ItemTooltipView _tooltip;
        [SerializeField] RectTransform _boardPanel;

        readonly List<PotionSlotView> _potions = new List<PotionSlotView>();
        readonly BoardHand _hand = new BoardHand();

        // Besides the hand, at most one of these is chosen: a potion (whose words then show on the detail line; the potions cannot be
        // used here) or a member's state (the same).
        int _selectedPotion = -1;
        int _selectedState = -1;
        ExpeditionArt _art;

        /// <summary>The hand over the boards (Slice B stage 19). A screen with something of its own to put on a board holds it here (<see cref="BoardHand.HoldOutside"/>).</summary>
        public BoardHand Hand => _hand;

        /// <summary>
        /// Lets the owning screen take over a click on a square (member index, square). When it returns true the view does nothing; the
        /// camp's upkeep uses this to choose the item to mend.
        /// </summary>
        public Func<int, int, int, bool> SquareClickOverride { get; set; }

        /// <summary>
        /// The item the owning screen has chosen on the boards (the camp's item to mend): its piece is the gold one and it is described on
        /// the detail line. -1 for none.
        /// </summary>
        public int ExternalSelectedMember { get; set; } = -1;
        public int ExternalSelectedX { get; set; }
        public int ExternalSelectedY { get; set; }

        /// <summary>What the detail line says while nothing is chosen, in place of how to use the boards. Null for the usual words.</summary>
        public string DetailOverride { get; set; }

        /// <summary>Raised when the hand lets go by itself (Escape), so that a screen holding something of its own can follow.</summary>
        public event Action HandReleased;

        /// <summary>Whether the inventory popup is open.</summary>
        public bool InventoryOpen { get; private set; }

        /// <summary>The inventory popup's grid (round 49).</summary>
        public InventoryGridView InventoryGrid => _inventoryGrid;

        /// <summary>The held item's tooltip in the popup: its lines, or an empty string while it is hidden.</summary>
        public string InventoryInfo => _inventoryInfoBox.activeSelf ? _inventoryInfo.text : string.Empty;

        public Button ToInventory => _toInventory;
        public GameObject InventoryPanel => _inventoryPanel;

        /// <summary>The picked item's card (round 42).</summary>
        public ItemTooltipView Tooltip => _tooltip;

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
                column.SquareClicked += (x, y) => OnSquareClicked(column.Member, x, y);
                column.SquareRightClicked += (x, y) => OnSquareRightClicked(column.Member, x, y);
                column.SquareEntered += (x, y) => OnSquareEntered(column.Member, x, y);
                column.SquareExited += (x, y) => OnSquareExited(column.Member, x, y);
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
            _inventoryGrid.SquareClicked += OnInventorySquareClicked;
            _inventoryGrid.SquareRightClicked += ShowInventoryTooltip;
            _inventoryGrid.SquareEntered += OnInventorySquareEntered;
            _inventoryGrid.SquareExited += OnInventorySquareExited;
            _inventoryPanel.SetActive(false);
            _tooltip.Hide();
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
            _hand.Clear();
            _selectedPotion = -1;
            _selectedState = -1;
            _tooltip.Hide();
        }

        /// <summary>
        /// Round 42: the next press anywhere closes the picked item's card; the press's own click still does what it does. Stage 19: while
        /// something is held, the right button, the wheel or R turns it and Escape lets go.
        /// </summary>
        void Update()
        {
            PointerPress.Poll();
            if (_tooltip.IsShown && PointerPress.PressedSince(_tooltip.ShownFrame))
            {
                _tooltip.Hide();
            }

            if (!_hand.Holding || Managers.Expedition?.Expedition == null)
            {
                return;
            }

            int step = TurnInput.Poll(out bool cancel);
            if (cancel)
            {
                _hand.Clear();
                HandReleased?.Invoke();
                Refresh();
            }
            else if (step != 0)
            {
                TurnHeld(step);
            }
        }

        /// <summary>Turns what is held a quarter clockwise (1) or anticlockwise (-1). Tests call it in place of the keys.</summary>
        public void TurnHeld(int step)
        {
            if (!_hand.Holding)
            {
                return;
            }

            _hand.Turn(step);
            Refresh();
        }

        /// <summary>Round 42 (a right click since round 47): the card of the item on a square, above the panel over its piece, with what merging it would do.</summary>
        void ShowTooltip(int member, int x, int y)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            if (member < 0 || expedition == null)
            {
                return;
            }

            BoardItem item = expedition.Members[member].Board.ItemAt(x, y);
            ItemSlotView piece = ColumnOfRow(expedition.Members[member].Row).BoardView.PieceAt(x, y);
            if (item == null || piece == null)
            {
                return;
            }

            string merge = Managers.Expedition.HasMergeTarget(item.Item) ? UiText.MergeHint(item.Item) : null;
            _tooltip.ShowAbove(item.Item, merge, piece.Rect, _boardPanel);
        }

        /// <summary>Round 47 (a grid since round 49): the card of the item on a square of the inventory, beside its piece inside the popup, while nothing is held.</summary>
        void ShowInventoryTooltip(int x, int y)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            BoardItem item = expedition?.Inventory.ItemAt(x, y);
            ItemSlotView piece = _inventoryGrid.PieceAt(x, y);
            if (item == null || piece == null || _hand.Holding)
            {
                return;
            }

            string merge = Managers.Expedition.HasMergeTarget(item.Item) ? UiText.MergeHint(item.Item) : null;
            _tooltip.ShowBeside(item.Item, piece.Rect, true, (RectTransform)_inventoryPanel.transform, true, merge);
        }

        /// <summary>
        /// Opens or closes the inventory popup. Closing it lets go of an inventory item held. The owning screen refreshes afterwards
        /// (its toggle label changes too), which refreshes this view.
        /// </summary>
        public void ToggleInventory()
        {
            InventoryOpen = !InventoryOpen;
            if (_hand.InventoryIndex >= 0)
            {
                _hand.Clear();
            }
        }

        public void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;

            EquippedItem held = _hand.HeldItem(expedition);
            bool bagHeld = _hand.BagMember >= 0 || _hand.Outside?.Bag != null;
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
                ExpeditionMember shown = expedition.Members[m];
                BoardItem picked = m == _hand.ItemMember ? _hand.HeldBoardItem(expedition)
                    : m == ExternalSelectedMember ? shown.Board.ItemAt(ExternalSelectedX, ExternalSelectedY) : null;
                BoardBag pickedBag = m == _hand.BagMember ? _hand.HeldBag(expedition) : null;
                Func<BoardItem, bool> merges = null;
                if (_hand.Outside != null && _hand.Outside.Item != null)
                {
                    merges = item => _hand.Outside.MergesAt(member, item.At.X, item.At.Y);
                }
                else if (held != null)
                {
                    merges = item => manager.MergesAt(held, member, item.At.X, item.At.Y);
                }

                column.Show(
                    m,
                    shown,
                    _art,
                    manager.CanMoveToRow(m, row - 1),
                    manager.CanMoveToRow(m, row + 1),
                    picked,
                    pickedBag,
                    merges,
                    _hand.GhostFor(m, manager, _art),
                    bagHeld);
            }

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), potionId == null ? null : _art.OfPotion(potionId), i == _selectedPotion, potionId != null);
            }

            _inventoryPanel.SetActive(InventoryOpen);
            if (InventoryOpen)
            {
                RefreshInventory(expedition, held);
            }

            _toInventory.interactable = _hand.ItemMember >= 0 && manager.CanMoveToInventory(_hand.ItemMember, _hand.ItemX, _hand.ItemY);

            EquippedItem selected = held ?? ExternalSelectedItem(expedition);
            BagData heldBag = _hand.BagMember >= 0 ? _hand.HeldBag(expedition)?.Bag : _hand.Outside?.Bag;
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
            else if (_hand.Outside != null && DetailOverride != null)
            {
                // What the screen holds of its own (a shop's offer): its tile shows its facts, so the line says how to put it down.
                _detail.text = DetailOverride;
            }
            else if (heldBag != null)
            {
                _detail.text = UiText.BagDetail(heldBag);
            }
            else if (selected == null)
            {
                _detail.text = DetailOverride ?? UiStrings.Get(UiKeys.Board.Hint);
            }
            else
            {
                // The chosen item's facts; and, while a board holds what it merges into, what putting it there does (round 35).
                string detail = UiText.ItemTitle(selected) + " — " + UiText.ItemSummary(selected);
                if (manager.HasMergeTarget(selected))
                {
                    detail += "\n" + UiText.MergeHint(selected);
                }

                _detail.text = detail;
            }
        }

        /// <summary>
        /// The popup (round 49): the squares used of the grid's, the coins, the grid with the held item's ghost where the pointer is, and the
        /// held item (of a board, of the inventory or from outside) in Diablo's tooltip. An item in the inventory costs no fatigue (its tag is off).
        /// </summary>
        void RefreshInventory(ExpeditionState expedition, EquippedItem held)
        {
            InventoryGrid grid = expedition.Inventory;
            _inventoryTitle.text = UiStrings.Get(UiKeys.Board.InventoryTitle, grid.UsedSquares, grid.Width * grid.Height);
            _inventoryCoins.text = expedition.Coins.ToString(System.Globalization.CultureInfo.InvariantCulture);
            _inventoryGrid.Show(grid, _art, _hand.InventoryIndex, _ => 0, _hand.InventoryGhost(Managers.Expedition, _art));
            _inventoryInfoBox.SetActive(held != null);
            if (held != null)
            {
                _inventoryInfo.text = UiText.TooltipLines(held) + "\n" + UiText.Colored(UiStrings.Get(UiKeys.Board.TurnHint), UiPalette.TextDim);
            }
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

        /// <summary>The item at the square the owning screen has chosen, or null.</summary>
        EquippedItem ExternalSelectedItem(ExpeditionState expedition)
        {
            if (ExternalSelectedMember < 0 || ExternalSelectedMember >= expedition.Members.Count)
            {
                return null;
            }

            return expedition.Members[ExternalSelectedMember].Board.ItemAt(ExternalSelectedX, ExternalSelectedY)?.Item;
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
            if (_hand.Outside == null)
            {
                _hand.Clear();
            }

            _selectedState = -1;
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
            if (_hand.Outside == null)
            {
                _hand.Clear();
            }

            _selectedPotion = -1;
            _selectedState = again ? -1 : member;
            Refresh();
        }

        /// <summary>
        /// A click on a square picks up, puts down or moves (the hand decides). The squares are silent: the click sounds here, once, as
        /// what it did: something put in sounds as put in, anything else as a click.
        /// </summary>
        void OnSquareClicked(int member, int x, int y)
        {
            if (member < 0)
            {
                return;
            }

            // The screen that overrides the click (the item to mend) makes its own sound.
            if (SquareClickOverride != null && SquareClickOverride(member, x, y))
            {
                return;
            }

            _selectedPotion = -1;
            _selectedState = -1;
            _tooltip.Hide();
            Managers.Sound.PlayEffect(_hand.Click(member, x, y, Managers.Expedition));
            Refresh();
        }

        /// <summary>A right click on a square: the card of the item there while nothing is held (a held thing turns instead, in Update).</summary>
        void OnSquareRightClicked(int member, int x, int y)
        {
            if (!_hand.Holding)
            {
                ShowTooltip(member, x, y);
            }
        }

        void OnSquareEntered(int member, int x, int y)
        {
            if (member < 0)
            {
                return;
            }

            _hand.Hover(member, x, y);
            if (_hand.Holding)
            {
                Refresh();
            }
        }

        void OnSquareExited(int member, int x, int y)
        {
            _hand.Unhover(member, x, y);
            if (_hand.Holding && _hand.HoverMember < 0)
            {
                Refresh();
            }
        }

        /// <summary>
        /// A square of the inventory's grid clicked (round 49): picks up the item there, or lays the held item there (the hand decides). Not
        /// while the screen holds something of its own or takes the clicks over (the camp's upkeep).
        /// </summary>
        void OnInventorySquareClicked(int x, int y)
        {
            if (SquareClickOverride != null || _hand.Outside != null)
            {
                return;
            }

            _selectedPotion = -1;
            _selectedState = -1;
            _tooltip.Hide();
            Managers.Sound.PlayEffect(_hand.ClickInventory(x, y, Managers.Expedition));
            Refresh();
        }

        void OnInventorySquareEntered(int x, int y)
        {
            _hand.HoverInventory(x, y);
            if (_hand.Holding)
            {
                Refresh();
            }
        }

        void OnInventorySquareExited(int x, int y)
        {
            _hand.UnhoverInventory(x, y);
            if (_hand.Holding && !_hand.OverInventory)
            {
                Refresh();
            }
        }

        void OnToInventoryClicked()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_hand.ItemMember >= 0 && manager.CanMoveToInventory(_hand.ItemMember, _hand.ItemX, _hand.ItemY))
            {
                manager.MoveToInventory(_hand.ItemMember, _hand.ItemX, _hand.ItemY);
                Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            }

            ClearSelection();
            Refresh();
        }
    }
}
