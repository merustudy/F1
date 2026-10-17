using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A row of the party in the expedition screen's party popup (Round 75 B, 2026-10-17): the member's name and level, the job with
    /// the member's state, the fatigue pips every screen shows, and the moves — a row to the front, a row to the back (trading places
    /// with whoever stands there), out of the party. An empty row says so and how to fill it, and has no moves.
    /// </summary>
    public sealed class PartyRowSlotView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _detail;
        [SerializeField] TMP_Text _hint;
        [SerializeField] UiBar[] _pips;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] Button _remove;

        /// <summary>Who stands in the row, or null while it is empty.</summary>
        public string MercenaryId { get; private set; }

        public string Name => _name.text;
        public string Level => _level.text;
        public string Detail => _detail.text;
        public string Hint => _hint.text;
        public IReadOnlyList<UiBar> Pips => _pips;
        public Button Forward => _forward;
        public Button Back => _back;
        public Button Remove => _remove;

        public void ShowEmpty(string words, string hint)
        {
            MercenaryId = null;
            _name.text = words;
            _name.color = UiPalette.TextDim;
            _level.text = string.Empty;
            _detail.text = string.Empty;
            _hint.text = hint;
            _hint.gameObject.SetActive(true);
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(false);
            }

            _forward.gameObject.SetActive(false);
            _back.gameObject.SetActive(false);
            _remove.gameObject.SetActive(false);
        }

        public void Show(string mercenaryId, string name, string level, string detail, int fatigue, int maxFatigue, int breakdown, bool canForward, bool canBack)
        {
            MercenaryId = mercenaryId;
            _name.text = name;
            _name.color = UiPalette.Text;
            _level.text = level;
            _detail.text = detail;
            _hint.text = string.Empty;
            _hint.gameObject.SetActive(false);
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(true);
            }

            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
            _forward.gameObject.SetActive(true);
            _back.gameObject.SetActive(true);
            _remove.gameObject.SetActive(true);
            _forward.interactable = canForward;
            _back.interactable = canBack;
        }
    }
}
