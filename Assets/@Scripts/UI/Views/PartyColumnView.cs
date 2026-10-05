using System;
using System.Collections.Generic;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One row of the party side of the node map and the reward screen, in the battle screen's
    /// shape: on the stage, the row's column with the figure, the marks (HP, and the job on the
    /// state line) and forward and back under it; in the board panel under the stage, the row's
    /// column with the board's head (the row and the name) and the item board stacked on its bag,
    /// whose cells are the battle's
    /// (<see cref="BattleItemView"/>) and show the same icons, each with its grade on a badge. It
    /// shows whoever stands in its row; an empty row shows nothing.
    /// </summary>
    public sealed class PartyColumnView : MonoBehaviour
    {
        [SerializeField] GameObject _figure;
        [SerializeField] FigureView _figureView;
        [SerializeField] GameObject _card;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] GameObject _board;
        [SerializeField] ItemSlotView _slotTemplate;
        [SerializeField] RectTransform _slotParent;

        readonly List<ItemSlotView> _views = new List<ItemSlotView>();

        /// <summary>The index of the member shown, or -1 while the row is empty.</summary>
        public int Member { get; private set; } = -1;

        /// <summary>Moves the member one row towards the enemy.</summary>
        public Button Forward => _forward;

        /// <summary>Moves the member one row away from the enemy.</summary>
        public Button Back => _back;

        /// <summary>The art of whoever stands here, or its placeholder.</summary>
        public FigureView Figure => _figureView;

        /// <summary>The row's column of the board panel, under the stage column: the cells. Shown and hidden with the column, and placed under it by the party side.</summary>
        public GameObject Board => _board;

        /// <summary>The cell views in board order: one per item, then one per empty cell. The rest are hidden.</summary>
        public IReadOnlyList<ItemSlotView> Slots => _views;

        /// <summary>The height of the board on show: the member's cells stacked. The bag behind them is as long.</summary>
        public float BoardHeight => _slotParent.sizeDelta.y;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked;

        /// <summary>Nobody stands in this row.</summary>
        public void Clear()
        {
            Member = -1;
            _figure.SetActive(false);
            _card.SetActive(false);
            _board.SetActive(false);
        }

        /// <param name="art">Where the figure of the member's job and the icons of its items come from.</param>
        /// <param name="selectedCell">The first cell of the highlighted item, or -1.</param>
        /// <param name="canClickCell">Whether a cell (the first of an item, or an empty one) takes a click now.</param>
        public void Show(int memberIndex, ExpeditionMember member, ExpeditionArt art, bool canMoveForward, bool canMoveBack, int selectedCell, Func<int, bool> canClickCell)
        {
            Member = memberIndex;
            _figure.SetActive(true);
            _figureView.Show(art.OfJob(member.JobId));
            _card.SetActive(true);
            _name.text = UiText.Mercenary(member.MercenaryId);
            _job.text = UiText.Job(member.JobId);
            _hp.text = UiStrings.Get(UiKeys.Board.Hp, member.Hp, member.MaxHp);
            _hpBar.Set(member.Hp, member.MaxHp);
            _forward.interactable = canMoveForward;
            _back.interactable = canMoveBack;

            _board.SetActive(true);

            // The board is as high as the member's cells, so the bag behind the cells is as long as the board.
            _slotParent.sizeDelta = new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(member.ItemSlots));

            // One view per cell at most; whoever stands here decides how many are used.
            while (_views.Count < member.ItemSlots)
            {
                ItemSlotView view = Instantiate(_slotTemplate, _slotParent);
                ItemSlotView clicked = view;
                view.Button.onClick.AddListener(() => CellClicked?.Invoke(clicked.Index));
                _views.Add(view);
            }

            // Views in board order from the top: one per item, as high as its cells, then one per empty cell.
            int next = 0;
            int cell = 0;
            foreach (EquippedItem item in member.Items)
            {
                int size = item.Item.Size;
                ShowCell(_views[next++], cell, item, art.OfItem(item.Item.Id), BattleItemView.BoardHeight(size), selectedCell, canClickCell);
                cell += size;
            }

            for (; cell < member.ItemSlots; cell++)
            {
                ShowCell(_views[next++], cell, null, null, BattleItemView.CellHeight, selectedCell, canClickCell);
            }

            for (; next < _views.Count; next++)
            {
                _views[next].gameObject.SetActive(false);
            }
        }

        static void ShowCell(ItemSlotView view, int cell, EquippedItem item, Sprite icon, float height, int selectedCell, Func<int, bool> canClickCell)
        {
            view.Index = cell;
            view.SetHeight(height);
            view.Show(item, icon, cell == selectedCell, canClickCell(cell));
            view.gameObject.SetActive(true);
        }
    }
}
