using System.Collections.Generic;

namespace F1.UI
{
    /// <summary>
    /// Fatigue as pips (2026-10-06 rounds 33 and 36, "C", Darkest Dungeon's way): bars side by side, each a tenth of the
    /// most fatigue, filled from the left as far as the fatigue goes (the last one in part). A pip from the breakdown
    /// threshold on is red; the rest are the fatigue's violet. The lobby's roster lines, the party side's columns and the
    /// battle's party units all show them (<see cref="RosterEntryView"/>, <see cref="PartyColumnView"/>, <see cref="BattleUnitView"/>).
    /// </summary>
    public static class FatiguePips
    {
        /// <summary>How many pips a row has: one per tenth of the most fatigue.</summary>
        public const int Count = 10;

        /// <summary>Fills and colours a row of pips for a fatigue.</summary>
        public static void Show(IReadOnlyList<UiBar> pips, int fatigue, int maxFatigue, int breakdown)
        {
            float pip = (float)maxFatigue / pips.Count;
            for (int i = 0; i < pips.Count; i++)
            {
                pips[i].SetRatio((fatigue - i * pip) / pip);
                pips[i].SetColor(i * pip >= breakdown ? UiPalette.FatigueDanger : UiPalette.FatigueBar);
            }
        }
    }
}
