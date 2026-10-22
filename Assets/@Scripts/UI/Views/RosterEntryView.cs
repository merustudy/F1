using System.Collections.Generic;
using System.Globalization;
using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// One mercenary of the roster in the barracks (Round 71: the lobby's barracks window): its fatigue as pips and words (round 33, C), an
    /// affliction's name after the words and what it does at the right end of the traits' line (round 36; the line was the passive's until Slice D
    /// stage 37, Round 84 안 A: the job's trait first, then the mercenary's, each an icon and a name), its six stats as a sheet (Slice D
    /// stage 32, Round 81 안 B: where the row buttons and "remove" stood — the party is formed in the expedition screen's popup only), its level
    /// and experience (stage 27) with the level-up button when the experience meets the level (Slice D stage 35, Round 82 안 A: the bar shortens and
    /// turns gold, the button stands at its end; what the roll grew shows beside the values under a gold rim), and the button that dismisses it
    /// (stage 26). The lobby never shows a virtue: it ends with the expedition.
    /// </summary>
    public sealed class RosterEntryView : MonoBehaviour
    {
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TraitLineView _traits;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _state;
        [SerializeField] UiBar[] _fatiguePips;
        [SerializeField] TMP_Text[] _statNames;
        [SerializeField] TMP_Text[] _statValues;
        [SerializeField] Button _dismiss;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _exp;
        [SerializeField] UiBar _expBar;
        [SerializeField] Button _levelUp;
        [SerializeField] GameObject _rim;

        /// <summary>The level block's two layouts (Round 82 안 A): the experience's words and bar as built, and shorter beside the level-up button.</summary>
        public const float ReadyExpLeft = 1212f;
        public const float ReadyBarWidth = 192f;

        /// <summary>How far the traits' line may reach (Round 84): to the affliction's words, or the pips' end without them.</summary>
        public const float TraitLineWidth = 590f;
        public const float TraitStateGap = 16f;

        Vector2 _expPosition;
        Vector2 _barSize;
        bool _layoutKept;

        /// <summary>Dismisses the mercenary from the roster for good (Slice C stage 26). Off for the last mercenary left.</summary>
        public Button Dismiss => _dismiss;

        /// <summary>Levels the mercenary up by one (Slice D stage 35). Shown only while its experience meets the level's.</summary>
        public Button LevelUp => _levelUp;

        /// <summary>The gold rim round a line whose level-up the barracks just rolled (Round 82 안 A).</summary>
        public bool RimShown => _rim.activeSelf;

        /// <summary>The fatigue pips, left to right: each a tenth of the most fatigue (round 33, C).</summary>
        public IReadOnlyList<UiBar> FatiguePips => _fatiguePips;

        /// <summary>The fatigue words: "피로도 N/200", and " · 공포" after them in an affliction.</summary>
        public TMP_Text Fatigue => _fatigue;

        /// <summary>What the affliction does, at the right end of the traits' line; empty without one.</summary>
        public TMP_Text State => _state;

        /// <summary>The traits' line (Slice D stage 37): the job's trait and the mercenary's own.</summary>
        public TraitLineView Traits => _traits;

        /// <summary>The level and the experience toward the next one (stage 27): "Lv 2", "경험치 4/25" and the bar.</summary>
        public TMP_Text Level => _level;
        public TMP_Text Exp => _exp;
        public UiBar ExpBar => _expBar;

        /// <summary>The six stats' names and values, in <see cref="StatBlock.All"/>'s order (Slice D stage 32).</summary>
        public IReadOnlyList<TMP_Text> StatNames => _statNames;
        public IReadOnlyList<TMP_Text> StatValues => _statValues;

        /// <param name="affliction">The name of the affliction the mercenary is in, or null.</param>
        /// <param name="afflictionEffect">What the affliction does, or null.</param>
        /// <param name="canDismiss">Whether the mercenary can be dismissed now.</param>
        public void Show(
            string mercenaryName,
            string jobName,
            IReadOnlyList<TraitChip> traits,
            int fatigue,
            int maxFatigue,
            int breakdown,
            string affliction = null,
            string afflictionEffect = null,
            bool canDismiss = false)
        {
            _dismiss.interactable = canDismiss;
            _name.text = mercenaryName;
            ShowLevel(1, 0, 1);
            _job.text = jobName;

            // Fatigue (round 33, C): the pips fill up to it in the fatigue's violet; the pips from the breakdown on, and the
            // words once it is reached, are red. An affliction's name follows the words and what it does stands on the passive's line (round 36).
            string words = UiStrings.Get(UiKeys.Lobby.Fatigue, fatigue, maxFatigue);
            _fatigue.text = affliction == null ? words : UiStrings.Get(UiKeys.Lobby.FatigueState, words, affliction);
            // Stage 48: a mental breakdown may last under the threshold now, and its words stay red while it does.
            _fatigue.color = fatigue >= breakdown || affliction != null ? UiPalette.FatigueDanger : UiPalette.Fatigue;
            _state.text = afflictionEffect ?? string.Empty;
            UI.FatiguePips.Show(_fatiguePips, fatigue, maxFatigue, breakdown);
            float stateWidth = string.IsNullOrEmpty(afflictionEffect) ? 0f : TextWidth.Of(_state, afflictionEffect) + TraitStateGap;
            _traits.Show(traits, TraitLineWidth - stateWidth);
        }

        /// <summary>
        /// The level and the experience (stage 27): the words and the bar, which is full at the last level (expToNext 0). When the barracks can level
        /// the mercenary up (Slice D stage 35) the words and the full bar turn gold, the bar shortens and the level-up button stands at its end.
        /// </summary>
        public void ShowLevel(int level, int exp, int expToNext, bool canLevelUp = false)
        {
            KeepLayout();
            _level.text = UiStrings.Get(UiKeys.Lobby.Level, level);
            _exp.text = expToNext > 0 ? UiStrings.Get(UiKeys.Lobby.Exp, exp, expToNext) : UiStrings.Get(UiKeys.Lobby.ExpMax);
            _exp.color = canLevelUp ? UiPalette.StarLit : UiPalette.TextDim;
            _expBar.SetRatio(expToNext > 0 ? Mathf.Clamp01((float)exp / expToNext) : 1f);
            _expBar.SetColor(canLevelUp ? UiPalette.StarLit : UiPalette.Bone);
            _levelUp.gameObject.SetActive(canLevelUp);
            _exp.rectTransform.anchoredPosition = canLevelUp ? new Vector2(ReadyExpLeft, _expPosition.y) : _expPosition;
            ((RectTransform)_expBar.transform).sizeDelta = canLevelUp ? new Vector2(ReadyBarWidth, _barSize.y) : _barSize;
        }

        /// <summary>
        /// The six stats (Slice D stage 32, Round 81): each name over its value, the value green above the baseline, red below it. With the gains of a
        /// level-up just rolled (stage 35) each grown value carries a small green +N and the line a gold rim; null takes them away.
        /// </summary>
        public void ShowStats(StatBlock stats, StatBlock gains = null)
        {
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                Stat stat = StatBlock.All[i];
                _statNames[i].text = UiText.StatName(stat);
                _statValues[i].text = UiText.StatValue(stats[stat], gains == null ? 0 : gains[stat]);
                _statValues[i].color = UiText.StatColor(stats[stat]);
            }

            _rim.SetActive(gains != null);
        }

        void KeepLayout()
        {
            if (_layoutKept)
            {
                return;
            }

            _expPosition = _exp.rectTransform.anchoredPosition;
            _barSize = ((RectTransform)_expBar.transform).sizeDelta;
            _layoutKept = true;
        }
    }
}
