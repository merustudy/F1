using System.Globalization;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The inventory window (round 49: Diablo II's window; round 52: over the mercenaries' stage, next to the boards, its squares a board's
    /// size; Docs/Architecture/12_UI.md "인벤토리 팝업"): the squares used of all, the grid with the hand's ghost, the coins, and the held item
    /// in Diablo's tooltip beside the grid. The node map's party side and the battle screen after a win each own one; the screen opens and
    /// closes it (its button and the I key) and answers its grid's events with its hand.
    /// </summary>
    public sealed class InventoryWindowView : MonoBehaviour
    {
        [SerializeField] TMP_Text _title;
        [SerializeField] InventoryGridView _grid;
        [SerializeField] TMP_Text _coins;
        [SerializeField] GameObject _infoBox;
        [SerializeField] TMP_Text _info;

        public InventoryGridView Grid => _grid;

        public bool IsOpen => gameObject.activeSelf;

        /// <summary>The held item's lines on show in the tooltip, or an empty string.</summary>
        public string Info => _infoBox.activeSelf ? _info.text : string.Empty;

        public RectTransform Rect => (RectTransform)transform;

        public void SetOpen(bool open)
        {
            gameObject.SetActive(open);
        }

        /// <summary>
        /// Fills the window: the squares used of the grid's, the coins, the grid with the hand's ghost where the pointer is, and the item the
        /// hand holds (of a board, of the inventory or from outside) in Diablo's tooltip. An item in the inventory costs no fatigue (its tag is off).
        /// </summary>
        public void Show(ExpeditionState expedition, ExpeditionArt art, BoardHand hand)
        {
            InventoryGrid grid = expedition.Inventory;
            _title.text = UiStrings.Get(UiKeys.Board.InventoryTitle, grid.UsedSquares, grid.Width * grid.Height);
            _coins.text = expedition.Coins.ToString(CultureInfo.InvariantCulture);
            _grid.Show(grid, art, hand.InventoryIndex, _ => 0, hand.InventoryGhost(Managers.Expedition, art));
            EquippedItem held = hand.HeldItem(expedition);
            _infoBox.SetActive(held != null);
            if (held != null)
            {
                _info.text = UiText.TooltipLines(held) + "\n" + UiText.Colored(UiStrings.Get(UiKeys.Board.TurnHint), UiPalette.TextDim);
            }
        }
    }
}
