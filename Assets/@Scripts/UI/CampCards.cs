namespace F1.UI
{
    /// <summary>
    /// Where the camp window's cards stand (round 35: rest and mend side by side; stage 23: the forge card beside them while a recipe is
    /// ready). The Builder lays the two, the screen re-lays all three when the third shows, from the same numbers.
    /// </summary>
    public static class CampCards
    {
        public const float WindowWidth = 580f;
        public const float Rim = 3f;
        public const float SideMargin = 22f;
        public const float Gap = 10f;
        public const float Top = 131f;
        public const float Height = 160f;

        /// <summary>The width inside the window's rim and side margins that the cards share.</summary>
        public static float Inner => WindowWidth - 2f * Rim - 2f * SideMargin;

        /// <summary>The width of one card when this many stand side by side.</summary>
        public static float Width(int count)
        {
            return (Inner - (count - 1) * Gap) / count;
        }

        /// <summary>The left edge of the card at this index (from the window's inner left).</summary>
        public static float Left(int count, int index)
        {
            return SideMargin + index * (Width(count) + Gap);
        }
    }
}
