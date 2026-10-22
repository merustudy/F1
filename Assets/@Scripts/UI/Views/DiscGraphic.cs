using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>A filled disc as wide as its rect (the merge's gold, Round 96 V1).</summary>
    public sealed class DiscGraphic : MaskableGraphic
    {
        const int Sides = 40;

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            Rect r = rectTransform.rect;
            Vector2 centre = r.center;
            float radius = Mathf.Min(r.width, r.height) / 2f;
            if (radius <= 0f)
            {
                return;
            }

            vh.AddVert(centre, color, Vector2.zero);
            for (int i = 0; i <= Sides; i++)
            {
                float angle = i * 2f * Mathf.PI / Sides;
                vh.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, color, Vector2.zero);
                if (i > 0)
                {
                    vh.AddTriangle(0, i, i + 1);
                }
            }
        }
    }
}
