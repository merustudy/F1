using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One mercenary of the roster in the lobby, with the buttons that put it in a row of the party.</summary>
    public sealed class RosterEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _passive;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] UiBar _fatigueBar;
        [SerializeField] Button[] _rows;
        [SerializeField] Image[] _rowFrames;
        [SerializeField] Button _remove;

        /// <summary>One button per battle row; index 0 is row 1.</summary>
        public IReadOnlyList<Button> Rows => _rows;
        public Button Remove => _remove;

        /// <param name="row">The row the mercenary holds in the party, or 0 when it is not in the party.</param>
        /// <param name="canTake">
        /// For each row the party can stand in, whether the mercenary can take it now. Buttons of rows
        /// beyond that are not shown: a side has more rows than the party has members.
        /// </param>
        public void Show(
            string mercenaryName,
            string jobName,
            string passive,
            int fatigue,
            int maxFatigue,
            int row,
            IReadOnlyList<bool> canTake)
        {
            _name.text = mercenaryName;
            _job.text = jobName;
            _passive.text = passive;
            _fatigue.text = UiStrings.Get(UiKeys.Lobby.Fatigue, fatigue, maxFatigue);
            _fatigueBar.Set(fatigue, maxFatigue);

            for (int i = 0; i < _rows.Length; i++)
            {
                bool shown = i < canTake.Count;
                _rows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                // The button of the row the mercenary stands in stays lit; clicking it changes nothing.
                bool own = row == i + 1;
                _rowFrames[i].color = own ? UiPalette.Selected : UiPalette.ButtonQuiet;
                _rows[i].interactable = own || canTake[i];
            }

            _remove.gameObject.SetActive(row != 0);
        }
    }
}
