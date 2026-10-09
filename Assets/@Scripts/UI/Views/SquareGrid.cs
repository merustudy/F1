using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// Squares in columns and rows over its rect with gaps between them (round 49): a bag shown as a thing (a shop's tile, the loot on the
    /// floor) draws its black squares with it, Diablo II's well, inside its rim. Drawn as quads, so it needs no picture; it takes no pointer.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class SquareGrid : MaskableGraphic
    {
        [SerializeField] int _columns = 1;
        [SerializeField] int _rows = 1;
        [SerializeField] float _gap = 2f;

        public int Columns => _columns;
        public int Rows => _rows;

        /// <param name="columns">Squares across.</param>
        /// <param name="rows">Squares down.</param>
        /// <param name="gap">The space between two squares.</param>
        public void Shape(int columns, int rows, float gap)
        {
            _columns = Mathf.Max(1, columns);
            _rows = Mathf.Max(1, rows);
            _gap = Mathf.Max(0f, gap);
            raycastTarget = false;
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = GetPixelAdjustedRect();
            float width = (r.width - (_columns - 1) * _gap) / _columns;
            float height = (r.height - (_rows - 1) * _gap) / _rows;
            if (width <= 0f || height <= 0f)
            {
                return;
            }

            Color32 c = color;
            for (int row = 0; row < _rows; row++)
            {
                for (int column = 0; column < _columns; column++)
                {
                    float xMin = r.xMin + column * (width + _gap);
                    float yMax = r.yMax - row * (height + _gap);
                    int start = vh.currentVertCount;
                    vh.AddVert(new Vector3(xMin, yMax - height), c, Vector2.zero);
                    vh.AddVert(new Vector3(xMin, yMax), c, Vector2.zero);
                    vh.AddVert(new Vector3(xMin + width, yMax), c, Vector2.zero);
                    vh.AddVert(new Vector3(xMin + width, yMax - height), c, Vector2.zero);
                    vh.AddTriangle(start, start + 1, start + 2);
                    vh.AddTriangle(start + 2, start + 3, start);
                }
            }
        }
    }
}
