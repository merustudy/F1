using F1.Data;

namespace F1.UI
{
    /// <summary>
    /// How an item piece marks a tier above Common (2026-10-07 round 41; round 49 took the icon's outline away, R12 its parts; round 69
    /// pips for the stars): the tier tag at the piece's bottom-left corner with one pip for 1성, two for 2성 and three for 3성, each
    /// the size the stars had. Common has none. The party side and the battle draw the same mark; the colours are UiPalette's (Rarity).
    /// </summary>
    public static class TierStyle
    {
        public const int MostPips = 3;

        /// <summary>The tier tag: its height, the pad at either end, and the size and pitch of its pips, on screen.</summary>
        public const float TagHeight = 20f;
        public const float TagPad = 6f;
        public const float PipSize = 12f;
        public const float PipPitch = 13f;

        /// <summary>How many pips the tier tag carries (the tier's number of stars); 0 for Common, which has no tag.</summary>
        public static int Pips(ItemTier tier)
        {
            return (int)tier - (int)ItemTier.Common;
        }

        /// <summary>The width of the tier tag carrying this many pips (one or more).</summary>
        public static float TagWidth(int pips)
        {
            return 2f * TagPad + PipSize + (pips - 1) * PipPitch;
        }
    }
}
