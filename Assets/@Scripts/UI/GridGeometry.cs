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

        /// <summary>
        /// The top-left square a held shape lies from with the pointer at a point of a grid (round 54, Diablo II: the held thing's centre on
        /// the pointer): the squares nearest under the shape centred there, pulled in so it fits a grid of so many squares. The point is in
        /// squares from the grid's top-left, a square's centre at its index + 0.5 (<see cref="PointIn"/>); a tie (an even side's centre on a
        /// square's centre) goes right and down.
        /// </summary>
        public static Placement Anchor(Vector2 point, int width, int height, int turns, int gridWidth, int gridHeight)
        {
            var turned = new Placement(0, 0, turns);
            int w = turned.WidthOf(width, height);
            int h = turned.HeightOf(width, height);
            int x = Mathf.FloorToInt(point.x - w / 2f + 0.5f);
            int y = Mathf.FloorToInt(point.y - h / 2f + 0.5f);
            x = Mathf.Clamp(x, 0, Mathf.Max(0, gridWidth - w));
            y = Mathf.Clamp(y, 0, Mathf.Max(0, gridHeight - h));
            return new Placement(x, y, turns);
        }

        /// <summary>The point at the centre of a square (round 54): where a square's own events aim, as the pointer there would.</summary>
        public static Vector2 Centre(int x, int y)
        {
            return new Vector2(x + 0.5f, y + 0.5f);
        }

        /// <summary>
        /// The point of a grid under a screen position (round 54): in squares from the top-left of the grid's squares (<paramref name="area"/>,
        /// the rect they are laid in), each square with half the gap on either side of it, so that a square's centre is its index + 0.5.
        /// False when the position is more than <paramref name="margin"/> pixels outside the squares.
        /// </summary>
        public static bool PointIn(RectTransform area, Vector2 screen, Camera camera, float margin, out Vector2 point)
        {
            point = default;
            if (area == null || !RectTransformUtility.ScreenPointToLocalPointInRectangle(area, screen, camera, out Vector2 local))
            {
                return false;
            }

            Rect rect = area.rect;
            float x = local.x - rect.xMin;
            float y = rect.yMax - local.y;
            if (x < -margin || y < -margin || x > rect.width + margin || y > rect.height + margin)
            {
                return false;
            }

            point = new Vector2((x + Gap / 2f) / (Square + Gap), (y + Gap / 2f) / (Square + Gap));
            return true;
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
