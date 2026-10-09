using F1.Data;
using F1.Gameplay;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The board's squares on the screen (Slice B stage 19, Docs/Architecture/12_UI.md "격자 보드"): a square of <see cref="Square"/>
    /// with <see cref="Gap"/> between, the frame's <see cref="BoardFrame.Width"/> x <see cref="BoardFrame.Height"/> squares in the panel's
    /// column (user 2026-10-08 "틀은 3x8": 50-pixel squares so that eight rows fit the panel). Positions are in the grid's own space: the
    /// origin at its top-left corner, y going down as the rows do.
    /// </summary>
    public static class GridGeometry
    {
        public const float Square = 50f;
        public const float Gap = 2f;

        /// <summary>How far a bag's rim reaches past its squares (round 49: Diablo's well, the stone rim round the black squares).</summary>
        public const float BagRim = 5f;

        /// <summary>The frame across and down, in pixels.</summary>
        public static float Width => Span(BoardFrame.Width);
        public static float Height => Span(BoardFrame.Height);

        /// <summary>How far a run of squares reaches, gaps between included.</summary>
        public static float Span(int squares)
        {
            return squares <= 0 ? 0f : squares * Square + (squares - 1) * Gap;
        }

        /// <summary>The top-left corner of a square, as an anchored position under a top-left anchor (y negative going down).</summary>
        public static Vector2 Corner(int x, int y)
        {
            return new Vector2(x * (Square + Gap), -y * (Square + Gap));
        }

        /// <summary>Lays a rect over the squares from (x, y), this many across and down: top-left anchored and pivoted.</summary>
        public static void Lay(RectTransform rect, int x, int y, int width, int height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Corner(x, y);
            rect.sizeDelta = new Vector2(Span(width), Span(height));
        }

        /// <summary>The top-left square a held shape lies from when the pointer is over a square: that square, pulled in so it fits the frame.</summary>
        public static Placement Anchor(int pointerX, int pointerY, int width, int height, int turns)
        {
            var turned = new Placement(0, 0, turns);
            int w = turned.WidthOf(width, height);
            int h = turned.HeightOf(width, height);
            int x = Mathf.Clamp(pointerX, 0, Mathf.Max(0, BoardFrame.Width - w));
            int y = Mathf.Clamp(pointerY, 0, Mathf.Max(0, BoardFrame.Height - h));
            return new Placement(x, y, turns);
        }

        /// <summary>
        /// How an icon drawn for an unturned item lies in a piece turned so many quarters: the size of its box before the turn (the
        /// piece's sides swapped for an odd turn) and the turn as a rotation about the piece's centre, clockwise.
        /// </summary>
        public static Vector2 ArtBox(Vector2 piece, int turns, float margin)
        {
            Vector2 inner = new Vector2(Mathf.Max(1f, piece.x - 2f * margin), Mathf.Max(1f, piece.y - 2f * margin));
            return turns % 2 == 0 ? inner : new Vector2(inner.y, inner.x);
        }

        public static Quaternion ArtTurn(int turns)
        {
            return Quaternion.Euler(0f, 0f, -90f * turns);
        }
    }
}
