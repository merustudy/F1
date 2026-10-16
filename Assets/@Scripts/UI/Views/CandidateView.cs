using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>A candidate at the stagecoach (Slice C stage 26): the name, the job it was drawn with and that job's passive, and the button that hires it at the price.</summary>
    public sealed class CandidateView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _passive;
        [SerializeField] Button _hire;
        [SerializeField] TMP_Text _hireLabel;

        public string Name => _name.text;
        public string Job => _job.text;
        public Button Hire => _hire;

        public void Show(string name, string job, string passive, string hireLabel, bool canHire)
        {
            _name.text = name;
            _job.text = job;
            _passive.text = passive;
            _hireLabel.text = hireLabel;
            _hire.interactable = canHire;
        }
    }
}
