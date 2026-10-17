using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A mercenary of the roster in the expedition screen's party popup (Round 75 B, 2026-10-17): a button that puts the mercenary in
    /// the party's next row, or takes it out when it is in. In the party the chip is lit and tagged with its row. The name with the
    /// level, the job with the affliction the mercenary came home in, and the fatigue pips, as the lobby's strip shows them.
    /// </summary>
    public sealed class PartyPickChipView : MonoBehaviour
    {
        [SerializeField] Image _frame;
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _row;
        [SerializeField] TMP_Text _job;
        [SerializeField] UiBar[] _pips;

        public string MercenaryId { get; set; }
        public bool InParty { get; private set; }
        public Button Button => _button;
        public Color FrameColor => _frame.color;
        public string Name => _name.text;
        public string Level => _level.text;
        public string Row => _row.text;
        public string Job => _job.text;
        public IReadOnlyList<UiBar> Pips => _pips;

        public void Show(string name, string level, string row, string jobLine, bool afflicted, bool inParty, int fatigue, int maxFatigue, int breakdown)
        {
            InParty = inParty;
            _frame.color = inParty ? UiPalette.Selected : UiPalette.PanelLight;
            _name.text = name;
            _level.text = level;
            _row.text = row ?? string.Empty;
            _job.text = jobLine;
            _job.color = afflicted ? UiPalette.FatigueDanger : UiPalette.TextDim;
            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
        }
    }
}
