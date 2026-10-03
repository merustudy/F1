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
    /// shape: on the stage, the row's column with the figure, the plate (the row, the name, HP and
    /// the job) and forward and back under it; in the board panel under the stage, the row's line
    /// with the face cut out of the figure and the item board side by side on its bag, whose cells
    /// are the battle's (<see cref="BattleItemView"/>) and show the same icons, each with its grade
    /// on a badge. It shows whoever stands in its row; an empty row shows nothing.
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
        [SerializeField] GameObject _line;
        [SerializeField] Image _face;
        [SerializeField] GameObject _facePlaceholder;
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

        /// <summary>The row's line of the board panel: the face and the cells. Shown and hidden with the column.</summary>
        public GameObject Line => _line;

        /// <summary>The face on show in the panel, or null while the placeholder stands in.</summary>
        public Sprite Face => _face.enabled ? _face.sprite : null;

        /// <summary>The cell views in board order: one per item, then one per empty cell. The rest are hidden.</summary>
        public IReadOnlyList<ItemSlotView> Slots => _views;

        /// <summary>The width of the board on show: the member's cells side by side. The bag behind them is as long.</summary>
        public float BoardWidth => _slotParent.sizeDelta.x;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked;

        /// <summary>Nobody stands in this row.</summary>
        public void Clear()
        {
            Member = -1;
            _figure.SetActive(false);
            _card.SetActive(false);
            _line.SetActive(false);
        }

        /// <param name="art">Where the figure and the face of the member's job and the icons of its items come from.</param>
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

            _line.SetActive(true);
            Sprite face = art.FaceOfJob(member.JobId);
            _face.sprite = face;
            _face.enabled = face != null;
            _facePlaceholder.SetActive(face == null);

            // The board is as wide as the member's cells, so the bag behind the cells is as long as the board.
            _slotParent.sizeDelta = new Vector2(BattleItemView.BoardWidth(member.ItemSlots), BattleItemView.CellHeight);

            // One view per cell at most; whoever stands here decides how many are used.
            while (_views.Count < member.ItemSlots)
            {
                ItemSlotView view = Instantiate(_slotTemplate, _slotParent);
                ItemSlotView clicked = view;
                view.Button.onClick.AddListener(() => CellClicked?.Invoke(clicked.Index));
                _views.Add(view);
            }

            // Views in board order: one per item, as wide as its cells, then one per empty cell.
            int next = 0;
            int cell = 0;
            foreach (EquippedItem item in member.Items)
            {
                int size = item.Item.Size;
                ShowCell(_views[next++], cell, item, art.OfItem(item.Item.Id), BattleItemView.BoardWidth(size), selectedCell, canClickCell);
                cell += size;
            }

            for (; cell < member.ItemSlots; cell++)
            {
                ShowCell(_views[next++], cell, null, null, BattleItemView.CellWidth, selectedCell, canClickCell);
            }

            for (; next < _views.Count; next++)
            {
                _views[next].gameObject.SetActive(false);
            }
        }

        static void ShowCell(ItemSlotView view, int cell, EquippedItem item, Sprite icon, float width, int selectedCell, Func<int, bool> canClickCell)
        {
            view.Index = cell;
            view.SetWidth(width);
            view.Show(item, icon, cell == selectedCell, canClickCell(cell));
            view.gameObject.SetActive(true);
        }
    }
}
