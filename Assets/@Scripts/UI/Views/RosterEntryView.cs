using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One mercenary of the roster in the lobby, with the buttons that put it in a row of the party. Its fatigue is pips and
    /// words (round 33, C); a mercenary that came home in an affliction has its name after the words and what it does at the
    /// right end of the passive's line (round 36). The lobby never shows a virtue: it ends with the expedition.
    /// </summary>
    public sealed class RosterEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _passive;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _state;
        [SerializeField] UiBar[] _fatiguePips;
        [SerializeField] Button[] _rows;
        [SerializeField] Image[] _rowFrames;
        [SerializeField] Button _remove;

        /// <summary>One button per battle row; index 0 is row 1.</summary>
        public IReadOnlyList<Button> Rows => _rows;
        public Button Remove => _remove;

        /// <summary>The fatigue pips, left to right: each a tenth of the most fatigue (round 33, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The fatigue words: "피로도 N/200", and " · 공포" after them in an affliction.</summary>
        public TMP_Text Fatigue => _fatigue;

        /// <summary>What the affliction does, at the right end of the passive's line; empty without one.</summary>
        public TMP_Text State => _state;

        /// <param name="row">The row the mercenary holds in the party, or 0 when it is not in the party.</param>
        /// <param name="canTake">
        /// For each row the party can stand in, whether the mercenary can take it now. Buttons of rows
        /// beyond that are not shown: a side has more rows than the party has members.
        /// </param>
        /// <param name="affliction">The name of the affliction the mercenary is in, or null.</param>
        /// <param name="afflictionEffect">What the affliction does, or null.</param>
        public void Show(
            string mercenaryName,
            string jobName,
            string passive,
            int fatigue,
            int maxFatigue,
            int breakdown,
            int row,
            IReadOnlyList<bool> canTake,
            string affliction = null,
            string afflictionEffect = null)
        {
            _name.text = mercenaryName;
            _job.text = jobName;
            _passive.text = passive;

            // Fatigue (round 33, C): the pips fill up to it in the fatigue's violet; the pips from the breakdown on, and the
            // words once it is reached, are red. An affliction's name follows the words and what it does stands on the passive's line (round 36).
            string words = UiStrings.Get(UiKeys.Lobby.Fatigue, fatigue, maxFatigue);
            _fatigue.text = affliction == null ? words : UiStrings.Get(UiKeys.Lobby.FatigueState, words, affliction);
            _fatigue.color = fatigue >= breakdown ? UiPalette.FatigueDanger : UiPalette.Fatigue;
            _state.text = afflictionEffect ?? string.Empty;
            UI.FatiguePips.Show(_fatiguePips, fatigue, maxFatigue, breakdown);

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
