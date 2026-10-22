using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A plate with rounded corners and a rim round it (the unique's plate under the battle's band, Round 96): the fill in the graphic's colour, the rim
    /// in its own, in one mesh where the two do not overlap, so that the plate's fade (a CanvasGroup) never shows the rim through the fill.
    /// </summary>
    public sealed class RimmedPlate : MaskableGraphic
    {
        const int CornerSteps = 6;

        [SerializeField] Color _rim = Color.white;
        [SerializeField] float _rimWidth = 2f;
        [SerializeField] float _radius = 6f;

        public void SetRim(Color rim, float width, float radius)
        {
            _rim = rim;
            _rimWidth = width;
            _radius = radius;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect outer = rectTransform.rect;
            var inner = new Rect(outer.x + _rimWidth, outer.y + _rimWidth, outer.width - 2f * _rimWidth, outer.height - 2f * _rimWidth);
            if (inner.width <= 0f || inner.height <= 0f)
            {
                return;
            }

            float radius = Mathf.Min(_radius, outer.width / 2f, outer.height / 2f);
            Vector2[] outerPoints = Round(outer, radius);
            Vector2[] innerPoints = Round(inner, Mathf.Max(0f, radius - _rimWidth));
            int count = outerPoints.Length;

            // The fill: a fan from the middle over the inner edge.
            vh.AddVert(inner.center, color, Vector2.zero);
            foreach (Vector2 point in innerPoints)
            {
                vh.AddVert(point, color, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                vh.AddTriangle(0, 1 + i, 1 + (i + 1) % count);
            }

            // The rim: the band between the outer edge and the inner one, point by point at the same angles.
            int start = vh.currentVertCount;
            for (int i = 0; i < count; i++)
            {
                vh.AddVert(outerPoints[i], _rim, Vector2.zero);
                vh.AddVert(innerPoints[i], _rim, Vector2.zero);
            }

            for (int i = 0; i < count; i++)
            {
                int a = start + 2 * i;
                int b = start + 2 * ((i + 1) % count);
                vh.AddTriangle(a, b, b + 1);
                vh.AddTriangle(a, b + 1, a + 1);
            }
        }

        /// <summary>The edge of a rect with rounded corners, counter-clockwise from the top-right corner's right end.</summary>
        static Vector2[] Round(Rect rect, float radius)
        {
            var corners = new[]
            {
                new Vector2(rect.xMax - radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMax - radius),
                new Vector2(rect.xMin + radius, rect.yMin + radius),
                new Vector2(rect.xMax - radius, rect.yMin + radius),
            };
            var points = new Vector2[corners.Length * (CornerSteps + 1)];
            for (int c = 0; c < corners.Length; c++)
            {
                for (int s = 0; s <= CornerSteps; s++)
                {
                    float angle = (c + (float)s / CornerSteps) * Mathf.PI / 2f;
                    points[c * (CornerSteps + 1) + s] = corners[c] + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                }
            }

            return points;
        }
    }
}
