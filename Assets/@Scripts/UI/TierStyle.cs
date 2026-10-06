using F1.Data;

namespace F1.UI
{
    /// <summary>
    /// How an item cell marks a tier above Common (2026-10-07 round 41, palette P3): the outline around the icon in the tier's
    /// colour, thicker at a higher tier, and the tier tag at the cell's bottom-left corner with one star for Bronze, two for
    /// Silver and three for Gold. Common has neither. The party side and the battle draw the same marks; the colours are
    /// UiPalette's (TierMark for the outline and the tag's rim, TierText for the stars).
    /// </summary>
    public static class TierStyle
    {
        public const int MostStars = 3;

        /// <summary>The tier tag: its height, the pad at either end, and the size and pitch of its stars, on screen.</summary>
        public const float TagHeight = 20f;
        public const float TagPad = 6f;
        public const float StarSize = 12f;
        public const float StarPitch = 13f;

        /// <summary>The outline's thickness around the icon, in pixels on screen; 0 for Common, which has none.</summary>
        public static float Outline(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return 2f;
                case ItemTier.Silver: return 3f;
                case ItemTier.Gold: return 4f;
                default: return 0f;
            }
        }

        /// <summary>How many stars the tier tag carries; 0 for Common, which has no tag.</summary>
        public static int Stars(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return 1;
                case ItemTier.Silver: return 2;
                case ItemTier.Gold: return 3;
                default: return 0;
            }
        }

        /// <summary>The width of the tier tag carrying this many stars (one or more).</summary>
        public static float TagWidth(int stars)
        {
            return 2f * TagPad + StarSize + (stars - 1) * StarPitch;
        }
    }
}
