using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One mercenary in the inn's window (Slice C stage 26): the name, the job with its affliction, the fatigue as pips and words, and the button that treats it for gold.</summary>
    public sealed class PatientView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] UiBar[] _pips;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] Button _treat;
        [SerializeField] TMP_Text _treatLabel;

        public string Name => _name.text;
        public IReadOnlyList<UiBar> Pips => _pips;
        public TMP_Text Fatigue => _fatigue;
        public Button Treat => _treat;

        public void Show(string name, string jobLine, bool afflicted, int fatigue, int maxFatigue, int breakdown, string fatigueWords, string treatLabel, bool canTreat)
        {
            _name.text = name;
            _job.text = jobLine;
            _job.color = afflicted ? UiPalette.FatigueDanger : UiPalette.TextDim;
            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
            _fatigue.text = fatigueWords;
            _fatigue.color = fatigue >= breakdown ? UiPalette.FatigueDanger : UiPalette.Fatigue;
            _treatLabel.text = treatLabel;
            _treat.interactable = canTreat;
        }
    }
}
