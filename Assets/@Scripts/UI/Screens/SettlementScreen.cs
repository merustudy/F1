using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>What the expedition cost: who came back, who died, fatigue and days. The run has already been settled.</summary>
    public sealed class SettlementScreen : UIScreen
    {
        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text _dungeon;
        [SerializeField] TMP_Text _survivors;
        [SerializeField] TMP_Text _fallen;
        [SerializeField] TMP_Text _fatigue;
        [SerializeField] TMP_Text _days;
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
            _runOver.text = report.RunIsOver ? UiStrings.Get(UiKeys.Settle.RunOver) : string.Empty;
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
    }
}
