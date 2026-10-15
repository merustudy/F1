using F1.Data;

namespace F1.UI
{
    /// <summary>
    /// How an item piece marks a tier above Common (2026-10-07 round 41; round 49 took the icon's outline away, R12 its parts): the
    /// tier tag at the piece's bottom-left corner with one star for Bronze, two for Silver and three for Gold. Common has none. The
    /// party side and the battle draw the same mark; the colours are UiPalette's (Rarity).
    /// </summary>
    public static class TierStyle
    {
        public const int MostStars = 3;

        /// <summary>The tier tag: its height, the pad at either end, and the size and pitch of its stars, on screen.</summary>
        public const float TagHeight = 20f;
        public const float TagPad = 6f;
        public const float StarSize = 12f;
        public const float StarPitch = 13f;

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
