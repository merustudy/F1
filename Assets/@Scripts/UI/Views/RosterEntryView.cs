using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One mercenary of the roster in the lobby, with the buttons that put it in the party.</summary>
    public sealed class RosterEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _passive;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] UiBar _fatigueBar;
        [SerializeField] Button _front;
        [SerializeField] Image _frontFrame;
        [SerializeField] Button _rear;
        [SerializeField] Image _rearFrame;
        [SerializeField] Button _remove;

        public Button Front => _front;
        public Button Rear => _rear;
        public Button Remove => _remove;

        /// <param name="row">The row the mercenary holds in the party, or null when it is not in the party.</param>
        public void Show(
            string mercenaryName,
            string jobName,
            string passive,
            int fatigue,
            int maxFatigue,
            BattleRow? row,
            bool canFront,
            bool canRear)
        {
            _name.text = mercenaryName;
            _job.text = jobName;
            _passive.text = passive;
            _fatigue.text = UiStrings.Get(UiKeys.Lobby.Fatigue, fatigue, maxFatigue);
            _fatigueBar.Set(fatigue, maxFatigue);

            _frontFrame.color = row == BattleRow.Front ? UiPalette.Selected : UiPalette.ButtonQuiet;
            _rearFrame.color = row == BattleRow.Rear ? UiPalette.Selected : UiPalette.ButtonQuiet;
            _front.interactable = canFront;
            _rear.interactable = canRear;
            _remove.gameObject.SetActive(row != null);
        }
    }
}
