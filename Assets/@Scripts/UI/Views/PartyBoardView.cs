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
    /// equipment costs at its right end; round 32, B1) and under it the board as a grid (Slice B stage 19, <see cref="GridBoardView"/>).
    /// The grid's squares raise the board's events with the member's squares: a click, a right click (the item's card, or a turn while
    /// something is held), and the pointer coming over a square or leaving it.
    /// </summary>
    public sealed class PartyBoardView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] GridBoardView _grid;
        [SerializeField] GameObject _fatigueTotal;
        [SerializeField] TMP_Text _fatigueTotalText;

        /// <summary>The fatigue tag at the head's right end is as wide as its words and this much on either side.</summary>
        const float FatigueTotalPad = 8f;

        bool _wired;

        /// <summary>The board's grid.</summary>
        public GridBoardView Grid => _grid;

        /// <summary>The pieces on show, one per item.</summary>
        public IEnumerable<ItemSlotView> Pieces => _grid.Pieces;

        /// <summary>The words on the head's fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTotal => _fatigueTotal.activeSelf ? _fatigueTotalText.text : string.Empty;

        public event Action<int, int> SquareClicked;
        public event Action<int, int> SquareRightClicked;
        public event Action<int, int> SquareEntered;
        public event Action<int, int> SquareExited;

        /// <param name="art">Where the icons of the items come from.</param>
        /// <param name="picked">The member's board item held now, or null.</param>
        /// <param name="pickedBag">The member's bag held now, or null.</param>
        /// <param name="merges">Whether the held item would merge into an item of the board (round 35): its piece is marked. Null for none.</param>
        /// <param name="ghost">The held thing's ghost on this board, or null.</param>
        /// <param name="showFrame">Whether the frame shows outside the bags: while a bag is held.</param>
        /// <param name="lifted">Whether the picked item or bag is in the hand (round 54): lifted off the board.</param>
        public void Show(ExpeditionMember member, ExpeditionArt art, BoardItem picked, BoardBag pickedBag, Func<BoardItem, bool> merges, GridGhost ghost, bool showFrame,
            IReadOnlyList<StarMark> stars = null, bool lifted = false)
        {
            Wire();
            gameObject.SetActive(true);
            BalanceData balance = Managers.Data.Data.Balance;
            _name.text = UiText.Mercenary(member.MercenaryId);

            // The fatigue the board's equipment costs when a battle starts, in all; nothing when it costs nothing.
            int fatigue = FatigueRules.EquipmentCost(balance, member.Board.InReadingOrder());
            _fatigueTotal.SetActive(fatigue > 0);
            if (fatigue > 0)
            {
                _fatigueTotalText.text = UiStrings.Get(UiKeys.Board.FatigueTotal, fatigue);
                var tag = (RectTransform)_fatigueTotal.transform;
                tag.sizeDelta = new Vector2(Mathf.Ceil(_fatigueTotalText.GetPreferredValues(_fatigueTotalText.text).x) + 2f * FatigueTotalPad, tag.sizeDelta.y);
            }

            _grid.Show(member.Board, art, picked, pickedBag, item => FatigueRules.ItemCost(balance, item), merges, ghost, showFrame, stars, lifted);
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        /// <summary>The piece covering a square, or null.</summary>
        public ItemSlotView PieceAt(int x, int y)
        {
            return _grid.PieceAt(x, y);
        }

        void Wire()
        {
            if (_wired)
            {
                return;
            }

            _wired = true;
            _grid.SquareClicked += (x, y) => SquareClicked?.Invoke(x, y);
            _grid.SquareRightClicked += (x, y) => SquareRightClicked?.Invoke(x, y);
            _grid.SquareEntered += (x, y) => SquareEntered?.Invoke(x, y);
            _grid.SquareExited += (x, y) => SquareExited?.Invoke(x, y);
        }
    }
}
