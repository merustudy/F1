using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// Turns a silhouette into an outline (2026-10-07 round 41): the graphic's mesh is drawn again around a circle of the
    /// thickness, sixteen times at the full radius and eight at half of it, so that the union of the copies grows the shape
    /// evenly on every side. On an Image that draws its sprite in one colour (the silhouette material), laid behind the
    /// same sprite, the copies that stick out are the outline. A thickness of 0 draws nothing more.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public sealed class SilhouetteOutline : BaseMeshEffect
    {
        const int OuterCopies = 16;
        const int InnerCopies = 8;

        static readonly List<UIVertex> Stream = new List<UIVertex>();

        [SerializeField] float _thickness;

        /// <summary>The outline's thickness in the graphic's units (pixels of the design space).</summary>
        public float Thickness
        {
            get => _thickness;
            set
            {
                if (Mathf.Approximately(_thickness, value))
                {
                    return;
                }

                _thickness = value;
                if (graphic != null)
                {
                    graphic.SetVerticesDirty();
                }
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || _thickness <= 0f)
            {
                return;
            }

            Stream.Clear();
            vh.GetUIVertexStream(Stream);
            int count = Stream.Count;
            AddRing(count, _thickness, OuterCopies);
            AddRing(count, _thickness * 0.5f, InnerCopies);
            vh.Clear();
            vh.AddUIVertexTriangleStream(Stream);
            Stream.Clear();
        }

        /// <summary>Adds the first `count` vertices of the stream again, shifted to `copies` points around a circle of this radius.</summary>
        static void AddRing(int count, float radius, int copies)
        {
            for (int i = 0; i < copies; i++)
            {
                float angle = 2f * Mathf.PI * i / copies;
                var shift = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                for (int v = 0; v < count; v++)
                {
                    UIVertex vertex = Stream[v];
                    vertex.position += shift;
                    Stream.Add(vertex);
                }
            }
        }
    }
}
