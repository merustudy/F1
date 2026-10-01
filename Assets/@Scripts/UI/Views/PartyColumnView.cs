using System;
using System.Collections.Generic;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One row's column on the party side of the node map and the reward screen: the figure, the
    /// info card (name, job, HP, forward and back) and the item board stacked under it, in the
    /// battle screen's shape; the board's cells are the battle card's (<see cref="BattleItemView"/>).
    /// It shows whoever stands in its row; an empty row shows only its label.
    /// </summary>
    public sealed class PartyColumnView : MonoBehaviour
    {
        [SerializeField] GameObject _figure;
        [SerializeField] GameObject _card;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] ItemSlotView _slotTemplate;
        [SerializeField] Transform _slotParent;

        readonly List<ItemSlotView> _views = new List<ItemSlotView>();

        /// <summary>The index of the member shown, or -1 while the row is empty.</summary>
        public int Member { get; private set; } = -1;

        /// <summary>Moves the member one row towards the enemy.</summary>
        public Button Forward => _forward;

        /// <summary>Moves the member one row away from the enemy.</summary>
        public Button Back => _back;

        /// <summary>The cell views in board order: one per item, then one per empty cell. The rest are hidden.</summary>
        public IReadOnlyList<ItemSlotView> Slots => _views;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked;

        /// <summary>Nobody stands in this row.</summary>
        public void Clear()
        {
            Member = -1;
            _figure.SetActive(false);
            _card.SetActive(false);
        }

        /// <param name="selectedCell">The first cell of the highlighted item, or -1.</param>
        /// <param name="canClickCell">Whether a cell (the first of an item, or an empty one) takes a click now.</param>
        public void Show(int memberIndex, ExpeditionMember member, bool canMoveForward, bool canMoveBack, int selectedCell, Func<int, bool> canClickCell)
        {
            Member = memberIndex;
            _figure.SetActive(true);
            _card.SetActive(true);
            _name.text = UiText.Mercenary(member.MercenaryId);
            _job.text = UiText.Job(member.JobId);
            _hp.text = UiStrings.Get(UiKeys.Board.Hp, member.Hp, member.MaxHp);
            _hpBar.Set(member.Hp, member.MaxHp);
            _forward.interactable = canMoveForward;
            _back.interactable = canMoveBack;

            // One view per cell at most; whoever stands here decides how many are used.
            while (_views.Count < member.ItemSlots)
            {
                ItemSlotView view = Instantiate(_slotTemplate, _slotParent);
                ItemSlotView clicked = view;
                view.Button.onClick.AddListener(() => CellClicked?.Invoke(clicked.Index));
                _views.Add(view);
            }

            // Views in board order: one per item, as tall as its cells, then one per empty cell.
            int next = 0;
            int cell = 0;
            foreach (EquippedItem item in member.Items)
            {
                int size = item.Item.Size;
                ShowCell(_views[next++], cell, item, BattleItemView.BoardHeight(size), selectedCell, canClickCell);
                cell += size;
            }

            for (; cell < member.ItemSlots; cell++)
            {
                ShowCell(_views[next++], cell, null, BattleItemView.CellHeight, selectedCell, canClickCell);
            }

            for (; next < _views.Count; next++)
            {
                _views[next].gameObject.SetActive(false);
            }
        }

        static void ShowCell(ItemSlotView view, int cell, EquippedItem item, float height, int selectedCell, Func<int, bool> canClickCell)
        {
            view.Index = cell;
            view.SetHeight(height);
            view.Show(item, cell == selectedCell, canClickCell(cell));
            view.gameObject.SetActive(true);
        }
    }
}
