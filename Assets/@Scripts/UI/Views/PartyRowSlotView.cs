using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A row of the party on the party popup's stage (Round 76 D, 2026-10-17): the member's figure standing on the floor as the node
    /// map's party side stands it, and under the feet the row with the name and the level, the job with the member's state, the
    /// fatigue pips every screen shows, and the moves — a row to the front, a row to the back (trading places with whoever stands
    /// there). An empty row shows a dim place with the words and, under it, how to fill the row; it has no moves.
    /// </summary>
    public sealed class PartyRowSlotView : MonoBehaviour
    {
        [SerializeField] FigureView _figure;
        [SerializeField] GameObject _empty;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _detail;
        [SerializeField] UiBar[] _pips;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;

        /// <summary>Who stands in the row, or null while it is empty.</summary>
        public string MercenaryId { get; private set; }

        public FigureView Figure => _figure;
        public bool IsEmpty => _empty.activeSelf;
        public string Name => _name.text;
        public string Level => _level.text;
        public string Detail => _detail.text;
        public IReadOnlyList<UiBar> Pips => _pips;
        public Button Forward => _forward;
        public Button Back => _back;

        public void ShowEmpty(string hint)
        {
            MercenaryId = null;
            _figure.gameObject.SetActive(false);
            _empty.SetActive(true);
            _name.text = string.Empty;
            _level.text = string.Empty;
            _detail.text = hint;
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(false);
            }

            _forward.gameObject.SetActive(false);
            _back.gameObject.SetActive(false);
        }

        /// <param name="art">The job's figure, or null for a job without one (the placeholder stands in).</param>
        public void Show(string mercenaryId, Sprite art, string name, string level, string detail, int fatigue, int maxFatigue, int breakdown, bool canForward, bool canBack)
        {
            MercenaryId = mercenaryId;
            _empty.SetActive(false);
            _figure.gameObject.SetActive(true);
            _figure.Show(art);
            _name.text = name;
            _level.text = level;
            _detail.text = detail;
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(true);
            }

            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
            _forward.gameObject.SetActive(true);
            _back.gameObject.SetActive(true);
            _forward.interactable = canForward;
            _back.interactable = canBack;
        }
    }
}
