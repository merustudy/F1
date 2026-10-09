using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A dashed line round its rect (Slice B stage 19, round 48's mockups): a bag's stitching just outside its squares, and a square of
    /// the frame where a bag can go while one is held. Drawn as quads, so it needs no picture; it takes no pointer. The dashes start at
    /// each corner and run clockwise along the sides.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class DashedFrame : MaskableGraphic
    {
        [SerializeField] float _thickness = 1f;
        [SerializeField] float _dash = 4f;
        [SerializeField] float _gap = 3f;
        [SerializeField] float _inset;

        /// <param name="thickness">The line's width.</param>
        /// <param name="dash">The length of a dash.</param>
        /// <param name="gap">The space between two dashes.</param>
        /// <param name="inset">How far inside the rect's edge the line's outer edge lies.</param>
        public void Shape(float thickness, float dash, float gap, float inset)
        {
            _thickness = Mathf.Max(0.5f, thickness);
            _dash = Mathf.Max(0.5f, dash);
            _gap = Mathf.Max(0f, gap);
            _inset = Mathf.Max(0f, inset);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float left = r.xMin + _inset;
            float right = r.xMax - _inset;
            float bottom = r.yMin + _inset;
            float top = r.yMax - _inset;
            if (right - left < 2f * _thickness || top - bottom < 2f * _thickness)
            {
                return;
            }

            // Top and bottom run the whole width; the sides run between them.
            Along(vh, left, right, top - _thickness, top, true);
            Along(vh, left, right, bottom, bottom + _thickness, true);
            Along(vh, bottom + _thickness, top - _thickness, left, left + _thickness, false);
            Along(vh, bottom + _thickness, top - _thickness, right - _thickness, right, false);
        }

        /// <summary>Dashes from one end of a side to the other; the last one is cut at the end.</summary>
        void Along(VertexHelper vh, float from, float to, float across0, float across1, bool horizontal)
        {
            for (float at = from; at < to; at += _dash + _gap)
            {
                float end = Mathf.Min(at + _dash, to);
                if (horizontal)
                {
                    Quad(vh, at, across0, end, across1);
                }
                else
                {
                    Quad(vh, across0, at, across1, end);
                }
            }
        }

        void Quad(VertexHelper vh, float xMin, float yMin, float xMax, float yMax)
        {
            int start = vh.currentVertCount;
            Color32 c = color;
            vh.AddVert(new Vector3(xMin, yMin), c, Vector2.zero);
            vh.AddVert(new Vector3(xMin, yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(xMax, yMax), c, Vector2.zero);
            vh.AddVert(new Vector3(xMax, yMin), c, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start + 2, start + 3, start);
        }
    }
}
