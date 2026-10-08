using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One row of the party side of the node map and the loot screen, in the battle screen's
    /// shape: on the stage, the row's column with the figure, the marks (HP, the fatigue as pips, and
    /// the job with the member's state on the state line; round 36) and forward and back under it; in the board panel under the stage, the row's
    /// column with the board's head (the row and the name) and the item board stacked on its bag,
    /// whose cells are the battle's
    /// (<see cref="BattleItemView"/>) and show the same icons, each with its grade on a badge. It
    /// shows whoever stands in its row; an empty row shows nothing. The fatigue the board's equipment
    /// costs when a battle starts is on the tags of its cells and, in all, at the head's right end
    /// (round 32, B1); the rule is <see cref="FatigueRules"/>. The state's name on the state line takes
    /// a click (<see cref="StateClicked"/>): the party side then explains the state on its detail line.
    /// </summary>
    public sealed class PartyColumnView : MonoBehaviour
    {
        [SerializeField] GameObject _figure;
        [SerializeField] FigureView _figureView;
        [SerializeField] GameObject _info;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] Button _state;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] UiBar[] _fatiguePips;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] GameObject _board;
        [SerializeField] ItemSlotView _slotTemplate;
        [SerializeField] RectTransform _slotParent;
        [SerializeField] GameObject _fatigueTotal;
        [SerializeField] TMP_Text _fatigueTotalText;

        /// <summary>The fatigue tag at the head's right end is as wide as its words and this much on either side.</summary>
        const float FatigueTotalPad = 8f;

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

        /// <summary>The words on the head's fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTotal => _fatigueTotal.activeSelf ? _fatigueTotalText.text : string.Empty;

        /// <summary>The fatigue pips under the HP bar, left to right: each a tenth of the most fatigue (round 36, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The state line's words: the job, and the member's state after it while it is in one.</summary>
        public string JobLine => _job.text;

        /// <summary>The click on the state line. Live only while the member is in a state.</summary>
        public Button StateButton => _state;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked;

        /// <summary>The state's name on the state line was clicked.</summary>
        public event Action StateClicked;

        void Awake()
        {
            _state.onClick.AddListener(() => StateClicked?.Invoke());
        }

        /// <summary>Nobody stands in this row.</summary>
        public void Clear()
        {
            Member = -1;
            _figure.SetActive(false);
            _info.SetActive(false);
            _board.SetActive(false);
        }

        /// <param name="art">Where the figure of the member's job and the icons of its items come from.</param>
        /// <param name="selectedCell">The first cell of the highlighted item, or -1.</param>
        /// <param name="canClickCell">Whether a cell (the first of an item, or an empty one) takes a click now.</param>
        /// <param name="mergesCell">Whether the chosen item would merge into the item at a cell (round 35): the cell is marked.</param>
        public void Show(int memberIndex, ExpeditionMember member, ExpeditionArt art, bool canMoveForward, bool canMoveBack, int selectedCell, Func<int, bool> canClickCell, Func<int, bool> mergesCell)
        {
            Member = memberIndex;
            _figure.SetActive(true);
            _figureView.Show(art.OfJob(member.JobId));
            _info.SetActive(true);
            _name.text = UiText.Mercenary(member.MercenaryId);
            _hp.text = UiStrings.Get(UiKeys.Board.Hp, member.Hp, member.MaxHp);
            _hpBar.Set(member.Hp, member.MaxHp);
            _forward.interactable = canMoveForward;
            _back.interactable = canMoveBack;

            // The fatigue as pips, and the state of the breakdown named after the job (round 36); its name takes a click.
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            FatigueStateData state = member.StateId == null ? null : data.FatigueStates.Get(member.StateId);
            UI.FatiguePips.Show(_fatiguePips, member.Fatigue, balance.MaxFatigue, balance.FatigueBreakdown);
            _job.text = UiText.JobLine(member.JobId, state);
            _state.interactable = state != null;

            _board.SetActive(true);

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

        static void ShowCell(ItemSlotView view, int cell, EquippedItem item, Sprite icon, float height, int selectedCell, Func<int, bool> canClickCell, int fatigueCost, bool merges)
        {
            view.Index = cell;
            view.SetHeight(height);
            view.Show(item, icon, cell == selectedCell, canClickCell(cell), fatigueCost, merges);
            view.gameObject.SetActive(true);
        }
    }
}
