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

        /// <summary>The member's board in the panel: its head and its grid (Slice B stage 19).</summary>
        public PartyBoardView BoardView => _boardView;

        /// <summary>The pieces on show, one per item.</summary>
        public IEnumerable<ItemSlotView> Pieces => _boardView.Pieces;

        /// <summary>The words on the head's fatigue tag, or an empty string while it is hidden.</summary>
        public string FatigueTotal => _boardView.FatigueTotal;

        /// <summary>The fatigue pips under the HP bar, left to right: each a tenth of the most fatigue (round 36, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The state line's words: the job, and the member's state after it while it is in one.</summary>
        public string JobLine => _job.text;

        /// <summary>The click on the state line. Live only while the member is in a state.</summary>
        public Button StateButton => _state;

        /// <summary>A square of the board was clicked.</summary>
        public event Action<int, int> SquareClicked
        {
            add => _boardView.SquareClicked += value;
            remove => _boardView.SquareClicked -= value;
        }

        /// <summary>A square of the board was right-clicked (round 47): the item's card, or a turn while something is held.</summary>
        public event Action<int, int> SquareRightClicked
        {
            add => _boardView.SquareRightClicked += value;
            remove => _boardView.SquareRightClicked -= value;
        }

        /// <summary>The pointer came over a square of the board.</summary>
        public event Action<int, int> SquareEntered
        {
            add => _boardView.SquareEntered += value;
            remove => _boardView.SquareEntered -= value;
        }

        /// <summary>The pointer left a square of the board.</summary>
        public event Action<int, int> SquareExited
        {
            add => _boardView.SquareExited += value;
            remove => _boardView.SquareExited -= value;
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
        /// <param name="picked">The member's board item held now, or null.</param>
        /// <param name="pickedBag">The member's bag held now, or null.</param>
        /// <param name="merges">Whether the held item would merge into an item of the board (round 35): its piece is marked. Null for none.</param>
        /// <param name="ghost">The held thing's ghost on this board, or null.</param>
        /// <param name="showFrame">Whether the frame shows outside the bags: while a bag is held.</param>
        public void Show(int memberIndex, ExpeditionMember member, ExpeditionArt art, bool canMoveForward, bool canMoveBack, BoardItem picked, BoardBag pickedBag,
            Func<BoardItem, bool> merges, GridGhost ghost, bool showFrame)
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
            _boardView.Show(member, art, picked, pickedBag, merges, ghost, showFrame);
        }
    }
}
