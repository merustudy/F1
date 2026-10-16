using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// One mercenary on the lobby's roster strip (Round 71, 안 1): the name, the row it holds in the party (nothing when it is not in the
    /// party), the job with the affliction it came home in, and the fatigue pips. The level comes with stage 27.
    /// </summary>
    public sealed class RosterChipView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _job;
        [SerializeField] UiBar[] _pips;

        public string Name => _name.text;
        public string Row => _row.text;
        public string Job => _job.text;
        public System.Collections.Generic.IReadOnlyList<UiBar> Pips => _pips;

        public void Show(string name, string row, string jobLine, bool afflicted, int fatigue, int maxFatigue, int breakdown)
        {
            _name.text = name;
            _row.text = row ?? string.Empty;
            _job.text = jobLine;
            _job.color = afflicted ? UiPalette.FatigueDanger : UiPalette.TextDim;
            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
        }
    }
}
