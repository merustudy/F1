using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>What the expedition cost and paid: who came back, who died, fatigue, days and the gold (stage 26). The run has already been settled.</summary>
    public sealed class SettlementScreen : UIScreen
    {
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _dungeon;
        [SerializeField] TMP_Text _survivors;
        [SerializeField] TMP_Text _fallen;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _days;
        [SerializeField] TMP_Text _gold;
        [SerializeField] TMP_Text _runOver;
        [SerializeField] Button _confirm;

        protected override void OnOpen()
        {
            _confirm.onClick.AddListener(OnConfirm);
            Managers.Sound.PlayMusic(MusicTrack.Lobby);
        }

        public override void Refresh()
        {
            SettlementReport report = Managers.Expedition.Report;

            switch (report.Result)
            {
                case ExpeditionResult.Cleared:
                    _title.text = UiStrings.Get(UiKeys.Settle.Cleared);
                    _title.color = UiPalette.Good;
                    break;
                case ExpeditionResult.Retreated:
                    _title.text = UiStrings.Get(UiKeys.Settle.Retreated);
                    _title.color = UiPalette.Text;
                    break;
                default:
                    _title.text = UiStrings.Get(UiKeys.Settle.Wiped);
                    _title.color = UiPalette.Danger;
                    break;
            }

            _dungeon.text = UiText.Name(Managers.Data.Data.Dungeons.Get(report.DungeonId).Name);
            _survivors.text = UiStrings.Get(UiKeys.Settle.Survivors, UiText.MercenaryList(report.SurvivorIds));
            _fallen.text = UiStrings.Get(UiKeys.Settle.Fallen, UiText.MercenaryList(report.FallenIds));
            _fallen.color = report.FallenIds.Count > 0 ? UiPalette.Danger : UiPalette.Text;
            _fatigue.text = report.SurvivorIds.Count > 0 ? UiStrings.Get(UiKeys.Settle.Fatigue, SurvivorFatigue(report)) : string.Empty;
            _days.text = UiStrings.Get(UiKeys.Settle.Days, report.DaysPassed, report.DayAfter);
            _gold.text = UiStrings.Get(UiKeys.Settle.Gold, report.GoldEarned, report.FloorsReached, report.GoldAfter);
            _runOver.text = RunOverText(report);
        }

        /// <summary>Each survivor with the fatigue they came back with, and the affliction they came home in: "로언 128 (공포), 카이 30".</summary>
        static string SurvivorFatigue(SettlementReport report)
        {
            var entries = new List<string>();
            for (int i = 0; i < report.SurvivorIds.Count; i++)
            {
                string entry = UiText.Mercenary(report.SurvivorIds[i]) + " " + report.SurvivorFatigue[i].ToString(CultureInfo.InvariantCulture);
                string stateId = report.SurvivorStates[i];
                entries.Add(stateId == null ? entry : UiStrings.Get(UiKeys.Settle.SurvivorState, entry, UiText.FatigueStateName(Managers.Data.Data.FatigueStates.Get(stateId))));
            }

            return string.Join(", ", entries);
        }

        void OnConfirm()
        {
            Managers.Expedition.AcknowledgeReport();
            GoToCurrentPhase();
        }

        /// <summary>The line that says the run ended with this expedition, and how (Docs/Design/04_Lobby_100Day_Economy.md §8).</summary>
        static string RunOverText(SettlementReport report)
        {
            if (!report.RunIsOver)
            {
                return string.Empty;
            }

            if (report.Outcome == RunOutcome.Victory)
            {
                return UiStrings.Get(UiKeys.Settle.Victory);
            }

            switch (report.DefeatCause)
            {
                case DefeatCause.DeathLimit: return UiStrings.Get(UiKeys.Settle.DeathLimit);
                case DefeatCause.FinalStandLost: return UiStrings.Get(UiKeys.Settle.FinalStandLost);
                default: return UiStrings.Get(UiKeys.Settle.RunOver);
            }
        }

    }
}
