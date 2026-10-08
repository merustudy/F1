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
    /// One row of the party side of the node map, in the battle screen's shape: on the stage, the row's column with the figure,
    /// the marks (HP, the fatigue as pips, and the job with the member's state on the state line; round 36) and forward and back
    /// under it; in the board panel under the stage, the row's column with the member's board (<see cref="PartyBoardView"/>: the
    /// head with the row and the name, the item board on its bag; round 47 split it out so that the battle screen shows the same
    /// board after a win). It shows whoever stands in its row; an empty row shows nothing. The state's name on the state line takes
    /// a click (<see cref="StateClicked"/>): the party side then explains the state on its detail line.
    /// </summary>
    public sealed class PartyColumnView : MonoBehaviour
    {
        [SerializeField] GameObject _figure;
        [SerializeField] FigureView _figureView;
        [SerializeField] GameObject _info;
        [SerializeField] TMP_Text _job;
        [SerializeField] Button _state;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] UiBar[] _fatiguePips;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] GameObject _board;
        [SerializeField] PartyBoardView _boardView;

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
        public IReadOnlyList<ItemSlotView> Slots => _boardView.Slots;

        /// <summary>The height of the board on show: the member's cells stacked. The bag behind them is as long.</summary>
        public float BoardHeight => _boardView.BoardHeight;

        /// <summary>The words on the head's fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTotal => _boardView.FatigueTotal;

        /// <summary>The fatigue pips under the HP bar, left to right: each a tenth of the most fatigue (round 36, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The state line's words: the job, and the member's state after it while it is in one.</summary>
        public string JobLine => _job.text;

        /// <summary>The click on the state line. Live only while the member is in a state.</summary>
        public Button StateButton => _state;

        /// <summary>A cell of the board was clicked: the first cell of an item, or an empty cell.</summary>
        public event Action<int> CellClicked
        {
            add => _boardView.CellClicked += value;
            remove => _boardView.CellClicked -= value;
        }

        /// <summary>A cell of the board was right-clicked (round 47): the item's card.</summary>
        public event Action<int> CellRightClicked
        {
            add => _boardView.CellRightClicked += value;
            remove => _boardView.CellRightClicked -= value;
        }

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
            _boardView.Show(member, art, selectedCell, canClickCell, mergesCell);
        }
    }
}
