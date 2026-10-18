using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A candidate at the stagecoach (Slice C stage 26): the name, the job it was drawn with, its level and fatigue (always level 1, fresh), that job's
    /// passive, its six stats on a line under a hairline (Slice D stage 32, Round 81), and the button that hires it at the price.
    /// </summary>
    public sealed class CandidateView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _detail;
        [SerializeField] TMP_Text _passive;
        [SerializeField] TMP_Text _stats;
        [SerializeField] Button _hire;
        [SerializeField] TMP_Text _hireLabel;

        public string Name => _name.text;
        public string Job => _job.text;
        public Button Hire => _hire;

        /// <summary>The stat line (Round 81): six pairs "힘 12" in columns.</summary>
        public TMP_Text Stats => _stats;

        /// <param name="detail">What a candidate always is: "Lv 1 · 피로도 0/200".</param>
        /// <param name="stats">The stat line (<see cref="UiText.StatColumns"/>).</param>
        public void Show(string name, string job, string detail, string passive, string stats, string hireLabel, bool canHire)
        {
            _stats.text = stats;
            _name.text = name;
            _job.text = job;
            _detail.text = detail;
            _passive.text = passive;
            _hireLabel.text = hireLabel;
            _hire.interactable = canHire;
        }
    }
}
