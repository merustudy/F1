using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Where an item's card goes (2026-10-07 round 42, placement P1; mockup 6 for the battle's notch). Rects are in a screen's
    /// top-left pixel space: x to the right from its left edge, y down from its top, as the builder lays things out (UiBuild.Box).
    /// </summary>
    public static class TooltipPlacement
    {
        /// <summary>Between the card's bottom edge and the board panel's top (the party side).</summary>
        public const float Gap = 8f;

        /// <summary>Between the card and the board it stands beside (battle).</summary>
        public const float BesideGap = 10f;

        /// <summary>How far the card keeps from the screen's edges.</summary>
        public const float ScreenMargin = 12f;

        /// <summary>How far the card keeps from the panel's top edge when it stands inside the panel.</summary>
        public const float PanelInset = 6f;

        /// <summary>How far a notch keeps from the card's corners.</summary>
        public const float NotchInset = 16f;

        /// <summary>
        /// The party side: the card floats above the board panel, centred on the cell's column and pushed in from the screen's
        /// edges, so that it covers no board. <paramref name="notchX"/> is where, along the card's bottom edge, the notch
        /// points down at the cell.
        /// </summary>
        public static Rect Above(Rect cell, Vector2 size, float panelTop, Rect screen, out float notchX)
        {
            float left = Mathf.Clamp(cell.center.x - size.x / 2f, screen.xMin + ScreenMargin, screen.xMax - ScreenMargin - size.x);
            notchX = Mathf.Clamp(cell.center.x - left, NotchInset, size.x - NotchInset);
            return new Rect(left, panelTop - Gap - size.y, size.x, size.y);
        }

        /// <summary>
        /// Battle: the card stands beside the board, on the side asked for (the party's cards to the left, the enemy's to the
        /// right), at the cell's height and inside the panel and the screen. Pushed in from the screen's edge over the board it
        /// belongs to, it goes to the board's other side instead. <paramref name="cardIsLeftOfCell"/> says which side it ended on,
        /// and <paramref name="notchY"/> is where, down the card's edge facing the board, the notch points at the cell's middle.
        /// </summary>
        public static Rect Beside(Rect cell, Vector2 size, bool left, Rect panel, Rect screen, out float notchY, out bool cardIsLeftOfCell)
        {
            float x = SideX(cell, size, left, screen);
            if (x < cell.xMax && x + size.x > cell.xMin)
            {
                left = !left;
                x = SideX(cell, size, left, screen);
            }

            float lowest = Mathf.Max(panel.yMin + PanelInset, panel.yMax - ScreenMargin - size.y);
            float top = Mathf.Clamp(cell.yMin, panel.yMin + PanelInset, lowest);
            cardIsLeftOfCell = left;
            notchY = Mathf.Clamp(cell.center.y - top, NotchInset, size.y - NotchInset);
            return new Rect(x, top, size.x, size.y);
        }

        static float SideX(Rect cell, Vector2 size, bool left, Rect screen)
        {
            float x = left ? cell.xMin - BesideGap - size.x : cell.xMax + BesideGap;
            return Mathf.Clamp(x, screen.xMin + ScreenMargin, screen.xMax - ScreenMargin - size.x);
        }
    }
}
