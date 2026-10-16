using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A party member's card on the expedition screen's party strip (round 71 E2): the name, the job with the member's state
    /// after it while it is in one, and the fatigue pips every screen shows (Docs/Architecture/12_UI.md "피로도와 붕괴의 상태").
    /// An empty row says so.
    /// </summary>
    public sealed class ExpeditionPartyCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _detail;
        [SerializeField] UiBar[] _pips;

        public string Name => _name.text;
        public string Detail => _detail.text;
        public IReadOnlyList<UiBar> Pips => _pips;

        public void ShowEmpty(string words)
        {
            _name.text = words;
            _name.color = UiPalette.TextDim;
            _detail.text = string.Empty;
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(false);
            }
        }

        public void Show(string name, string detail, int fatigue, int maxFatigue, int breakdown)
        {
            _name.text = name;
            _name.color = UiPalette.Text;
            _detail.text = detail;
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(true);
            }

            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
        }
    }
}
