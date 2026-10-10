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
    /// battle; the potions in the strip above; and the inventory as a window over the stage, above the boards (round 52; round 49's popup lay over the other half).
    /// The boards are used with a hand (<see cref="BoardHand"/>): a click on an item picks it up and a click on a square puts it
    /// where its ghost shows (one item in the way goes to the inventory); a click on an empty square of a bag picks the bag up with
    /// what lies in it; while something is held, the right button, the wheel or R turns it (Backpack Battles' turning) and Escape lets
    /// go. Since round 54 (Diablo II) what is held leaves its place and rides the pointer (<see cref="HeldPointerView"/>), aimed with its
    /// centre where the pointer is; every press goes to it first: on a board or the inventory it is put there, on a button that works on
    /// it ("to inventory", the inventory's button) that button works, anywhere else it goes back where it was. "To inventory" takes the held board item off its board to the inventory's first room. The inventory is a grid of its own in the
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
        [SerializeField] InventoryWindowView _inventory;
        [SerializeField] ItemTooltipView _tooltip;
        [SerializeField] RectTransform _boardPanel;
        [SerializeField] HeldPointerView _heldPointer;

        /// <summary>How far outside a grid's squares the pointer still aims at it (round 54): a near miss is pulled in, not a press elsewhere.</summary>
        const float AimMargin = 16f;

        readonly List<PotionSlotView> _potions = new List<PotionSlotView>();
        readonly List<Button> _heldButtons = new List<Button>();
        readonly BoardHand _hand = new BoardHand();

        /// <summary>The star item whose stars the boards show for the pointer (stage 20), or null.</summary>
        BoardItem _starsShownFor;

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
        public InventoryGridView InventoryGrid => _inventory.Grid;

        /// <summary>The held item's tooltip in the popup: its lines, or an empty string while it is hidden.</summary>
        public string InventoryInfo => _inventory.Info;

        public Button ToInventory => _toInventory;
        public GameObject InventoryPanel => _inventory.gameObject;

        /// <summary>The inventory window (round 52: over the stage).</summary>
        public InventoryWindowView InventoryWindow => _inventory;

        /// <summary>The picked item's card (round 42).</summary>
        public ItemTooltipView Tooltip => _tooltip;

        /// <summary>What is held, on the pointer (round 54).</summary>
        public HeldPointerView HeldPointer => _heldPointer;

        /// <summary>A button that still works while something is held (round 54): a press on it is not a press elsewhere.</summary>
        public void LetThroughWhileHeld(Button button)
        {
            if (button != null && !_heldButtons.Contains(button))
            {
                _heldButtons.Add(button);
            }
        }

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
            _inventory.Grid.SquareClicked += OnInventorySquareClicked;
            _inventory.Grid.SquareRightClicked += ShowInventoryTooltip;
            _inventory.Grid.SquareEntered += OnInventorySquareEntered;
            _inventory.Grid.SquareExited += OnInventorySquareExited;
            _inventory.SetOpen(false);
            _tooltip.Hide();
            LetThroughWhileHeld(_toInventory);
            _heldPointer.Pressed += OnHeldPressed;
            _heldPointer.Hide();
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
                return;
            }

            if (step != 0)
            {
                TurnHeld(step);
            }

            // Round 54: the pointer aims what is held wherever it moves; the boards redraw only when where it would land changes.
            if (_heldPointer.PointerMoved(out Vector2 screen))
            {
                AimFrom(screen);
                if (_hand.AimChanged(Managers.Expedition.Expedition))
                {
                    Refresh();
                }
            }
        }

        /// <summary>Aims the hand where a screen position is: at a member's board, at the inventory's grid while it is open, or at neither.</summary>
        void AimFrom(Vector2 screen)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            Camera camera = _heldPointer.EventCamera;
            if (InventoryOpen && GridGeometry.PointIn(_inventory.Grid.SquareArea, screen, camera, AimMargin, out Vector2 inInventory))
            {
                _hand.AimInventory(inInventory);
                return;
            }

            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                int m = MemberInRow(expedition, row);
                if (m >= 0 && GridGeometry.PointIn(ColumnOfRow(row).BoardView.Grid.SquareArea, screen, camera, AimMargin, out Vector2 onBoard))
                {
                    _hand.AimBoard(m, onBoard);
                    return;
                }
            }

            _hand.AimNowhere();
        }

        /// <summary>
        /// A press while something is held (round 54; "아이템 이동 이벤트가 우선", "이상한 곳 클릭시 다시 아이템 복귀"): on a button that works on
        /// what is held, that button; over a board or the inventory's grid, the hand puts it there (or keeps it where it cannot go);
        /// anywhere else the hand lets go and it is back where it was, and the press does nothing more.
        /// </summary>
        void OnHeldPressed(Vector2 screen)
        {
            if (!_hand.Holding || Managers.Expedition?.Expedition == null)
            {
                return;
            }

            foreach (Button button in _heldButtons)
            {
                if (button.isActiveAndEnabled && button.interactable
                    && RectTransformUtility.RectangleContainsScreenPoint((RectTransform)button.transform, screen, _heldPointer.EventCamera))
                {
                    button.onClick.Invoke();
                    return;
                }
            }

            _selectedPotion = -1;
            _selectedState = -1;
            _tooltip.Hide();
            AimFrom(screen);
            if (_hand.HoverMember >= 0 || _hand.OverInventory)
            {
                Managers.Sound.PlayEffect(_hand.Commit(Managers.Expedition));
                Refresh();
                return;
            }

            Managers.Sound.PlayEffect(SoundEffect.Button);
            _hand.Clear();
            HandReleased?.Invoke();
            Refresh();
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
            ItemBoard board = expedition.Members[member].Board;
            _tooltip.ShowAbove(item.Item, merge, piece.Rect, _boardPanel, UiText.StarLine(board, item), StarRules.DamageOn(board, item));
        }

        /// <summary>Round 47 (a grid since round 49): the card of the item on a square of the inventory, beside its piece inside the popup, while nothing is held.</summary>
        void ShowInventoryTooltip(int x, int y)
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            BoardItem item = expedition?.Inventory.ItemAt(x, y);
            ItemSlotView piece = _inventory.Grid.PieceAt(x, y);
            if (item == null || piece == null || _hand.Holding)
            {
                return;
            }

            string merge = Managers.Expedition.HasMergeTarget(item.Item) ? UiText.MergeHint(item.Item) : null;
            _tooltip.ShowBeside(item.Item, piece.Rect, true, _inventory.Rect, true, merge);
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
                    bagHeld,
                    _hand.StarsFor(m, manager),
                    m == _hand.ItemMember || m == _hand.BagMember);
            }

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), potionId == null ? null : _art.OfPotion(potionId), i == _selectedPotion, potionId != null);
            }

            _inventory.SetOpen(InventoryOpen);
            if (InventoryOpen)
            {
                _inventory.Show(expedition, _art, _hand);
            }

            _heldPointer.ShowHand(_hand, expedition, _art);

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
            if (_hand.Holding || StarsMoved())
            {
                Refresh();
            }
        }

        void OnSquareExited(int member, int x, int y)
        {
            // Round 54: while something is held the layer over the screen takes the pointer, and the pointer's moves aim it (Update).
            if (_hand.Holding)
            {
                return;
            }

            _hand.Unhover(member, x, y);
            if (StarsMoved())
            {
                Refresh();
            }
        }

        /// <summary>Whether the star item under the pointer changed since the boards were drawn (stage 20): its stars show or go.</summary>
        bool StarsMoved()
        {
            BoardItem pointed = _hand.HoveredStarItem(Managers.Expedition.Expedition);
            if (pointed == _starsShownFor)
            {
                return false;
            }

            _starsShownFor = pointed;
            return true;
        }

        /// <summary>
        /// A square of the inventory's grid clicked (round 49): picks up the item there, or lays what is held there (the hand decides; since
        /// round 55 a shop's offer too, bought there). Not while the screen takes the clicks over (the camp's upkeep).
        /// </summary>
        void OnInventorySquareClicked(int x, int y)
        {
            if (SquareClickOverride != null)
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
            if (_hand.Holding)
            {
                return;
            }

            _hand.UnhoverInventory(x, y);
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
