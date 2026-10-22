using System.Collections.Generic;

namespace F1.UI
{
    /// <summary>
    /// Fatigue as pips (2026-10-06 rounds 33 and 36, "C", Darkest Dungeon's way), in two laps since Round 95 (2026-10-22, the user's "0-100까지 보라
    /// 색으로 끝까지 표시 / 100-200은 그 위에 빨간 색으로 중첩"): ten bars side by side. The first lap, 0 to the breakdown threshold, fills them from the
    /// left in the fatigue's violet, a tenth of the threshold each; the second lap, the threshold to the most fatigue, fills the same pips again from the
    /// left in red, over the violet, a tenth of that span each. The lobby's roster lines, the strip's chips, the inn's lines, the expedition screen's
    /// party, the party side's columns and the battle's party units all show them (Docs/Architecture/12_UI.md "피로도와 붕괴의 상태").
    /// </summary>
    public static class FatiguePips
    {
        /// <summary>How many pips a row has: one per tenth of a lap.</summary>
        public const int Count = 10;

        /// <summary>Fills a row of pips (built with UiBuild.PipBar: violet fill, red second fill over it) for a fatigue.</summary>
        public static void Show(IReadOnlyList<UiBar> pips, int fatigue, int maxFatigue, int breakdown)
        {
            float first = (float)breakdown / pips.Count;
            float second = (float)(maxFatigue - breakdown) / pips.Count;
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].SetRatio(first > 0f ? (fatigue - i * first) / first : 0f);
                pips[i].SetOverRatio(second > 0f ? (fatigue - breakdown - i * second) / second : 0f);
            }
        }
    }
}
