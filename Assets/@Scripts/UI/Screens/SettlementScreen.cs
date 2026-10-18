using System.Collections.Generic;
using System.Globalization;
using F1.Core;
using F1.Data;
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
        [SerializeField] TMP_Text _exp;
        [SerializeField] TMP_Text _runOver;
        [SerializeField] Button _confirm;
        [SerializeField] TMP_Text _ready;
        [SerializeField] TMP_Text _itemsLost;
        [SerializeField] RectTransform _panel;

        /// <summary>How tall the "ready to level up" line is, and the gap under it (Slice D stage 35, Round 82: the panel grows by 46).</summary>
        const float ReadyLine = 34f;
        const float ReadyGap = 12f;

        /// <summary>Where the panel and the things under the line stand without it (the builder's places), kept to move them back.</summary>
        Vector2 _panelPosition;
        Vector2 _panelSize;
        float _itemsLostY;
        float _runOverY;
        float _confirmY;
        bool _placesKept;

        /// <summary>The line naming who can level up in the barracks (Slice D stage 35), empty when nobody can.</summary>
        public TMP_Text Ready => _ready;

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
            _gold.text = report.CoinsConverted > 0
                ? UiStrings.Get(UiKeys.Settle.GoldWithCoins, report.GoldEarned, report.FloorsReached, report.CoinsConverted, report.GoldFromCoins, report.GoldAfter)
                : UiStrings.Get(UiKeys.Settle.Gold, report.GoldEarned, report.FloorsReached, report.GoldAfter);
            _exp.text = ExpText(report);
            ShowReady(report);
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

        /// <summary>The experience every survivor got (stage 27): "경험치 +20". Nothing without survivors.</summary>
        static string ExpText(SettlementReport report)
        {
            return report.SurvivorIds.Count == 0 ? string.Empty : UiStrings.Get(UiKeys.Settle.Exp, report.ExpEarned);
        }

        /// <summary>
        /// Who can level up now (Slice D stage 35, Round 82): "▲ 레벨 업 가능: 로언, 카이 — 막사에서 올린다" under the experience, in gold — the roll
        /// waits for the barracks. The panel grows by the line, half up and half down, and the lines under it (the lost items, the run's end,
        /// [확인]) move down as much; without anyone the panel stays as built.
        /// </summary>
        void ShowReady(SettlementReport report)
        {
            KeepPlaces();
            bool any = report.ReadyToLevelUp.Count > 0;
            _ready.text = any ? UiStrings.Get(UiKeys.Settle.Ready, UiText.MercenaryList(report.ReadyToLevelUp)) : string.Empty;
            float grown = any ? ReadyLine + ReadyGap : 0f;
            _ready.rectTransform.sizeDelta = new Vector2(_ready.rectTransform.sizeDelta.x, any ? ReadyLine : 0f);
            _panel.anchoredPosition = _panelPosition + new Vector2(0f, grown / 2f);
            _panel.sizeDelta = _panelSize + new Vector2(0f, grown);
            Move(_itemsLost.rectTransform, _itemsLostY - grown);
            Move(_runOver.rectTransform, _runOverY - grown);
            Move((RectTransform)_confirm.transform, _confirmY - grown);
        }

        void KeepPlaces()
        {
            if (_placesKept)
            {
                return;
            }

            _panelPosition = _panel.anchoredPosition;
            _panelSize = _panel.sizeDelta;
            _itemsLostY = _itemsLost.rectTransform.anchoredPosition.y;
            _runOverY = _runOver.rectTransform.anchoredPosition.y;
            _confirmY = ((RectTransform)_confirm.transform).anchoredPosition.y;
            _placesKept = true;
        }

        static void Move(RectTransform rect, float y)
        {
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, y);
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
