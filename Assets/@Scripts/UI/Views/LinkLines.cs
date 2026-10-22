using System.Collections.Generic;
using F1.Gameplay;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The lines between a recipe's pieces on a board (Slice D stage 50, Round 96, Docs/Architecture/12_UI.md "유니크의 조합"): from the middle of one
    /// piece to the middle of the other, gold for a link the next won battle combines and grey for one a tier short, with a dot at each end and a
    /// dark rim round both. Drawn over the pieces in the grid's own space (top-left origin, y down: <see cref="GridGeometry"/>); takes no pointer.
    /// </summary>
    public sealed class LinkLines : MaskableGraphic
    {
        public const float LineWidth = 4f;
        public const float DotRadius = 5f;
        const float Rim = 1.5f;
        const int DotSides = 16;

        readonly List<(Vector2 From, Vector2 To, bool Ready)> _segments = new List<(Vector2, Vector2, bool)>();

        /// <summary>How many lines are drawn, and whether each is ready (gold), in the order given.</summary>
        public IReadOnlyList<(Vector2 From, Vector2 To, bool Ready)> Segments => _segments;

        /// <summary>Makes the layer under a parent laid like the grid (its top-left the grid's), the last child so that it draws over the pieces.</summary>
        public static LinkLines Under(RectTransform parent)
        {
            var go = new GameObject("LinkLines", typeof(RectTransform), typeof(CanvasRenderer), typeof(LinkLines));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(GridGeometry.Width, GridGeometry.Height);
            var lines = go.GetComponent<LinkLines>();
            lines.raycastTarget = false;
            return lines;
        }

        /// <summary>The middle of a piece in the grid's space.</summary>
        public static Vector2 Middle(BoardItem item)
        {
            return GridGeometry.Corner(item.At.X, item.At.Y) + new Vector2(GridGeometry.Span(item.Width) / 2f, -GridGeometry.Span(item.Height) / 2f);
        }

        /// <summary>Draws these links, or none; a link touching a piece in <paramref name="hidden"/> (one held off the board) is left out.</summary>
        public void Show(IEnumerable<ForgeLink> links, ICollection<BoardItem> hidden = null)
        {
            _segments.Clear();
            if (links != null)
            {
                foreach (ForgeLink link in links)
                {
                    if (hidden != null && (hidden.Contains(link.BaseItem) || hidden.Contains(link.Catalyst)))
                    {
                        continue;
                    }

                    _segments.Add((Middle(link.BaseItem), Middle(link.Catalyst), link.Ready));
                }
            }

            transform.SetAsLastSibling();
            gameObject.SetActive(_segments.Count > 0);
            SetVerticesDirty();
        }

        /// <summary>Draws one gold line (the merge's, a moment before the pieces merge: Round 96).</summary>
        public void ShowOne(Vector2 from, Vector2 to)
        {
            _segments.Clear();
            _segments.Add((from, to, true));
            transform.SetAsLastSibling();
            gameObject.SetActive(true);
            SetVerticesDirty();
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            foreach ((Vector2 from, Vector2 to, bool ready) in _segments)
            {
                Color colour = ready ? UiPalette.LinkReady : UiPalette.LinkShort;
                Line(vh, from, to, LineWidth + 2f * Rim, UiPalette.LinkRim);
                Dot(vh, from, DotRadius + Rim, UiPalette.LinkRim);
                Dot(vh, to, DotRadius + Rim, UiPalette.LinkRim);
                Line(vh, from, to, LineWidth, colour);
                Dot(vh, from, DotRadius, colour);
                Dot(vh, to, DotRadius, colour);
            }
        }

        static void Line(VertexHelper vh, Vector2 a, Vector2 b, float width, Color colour)
        {
            Vector2 along = b - a;
            if (along.sqrMagnitude < 0.01f)
            {
                return;
            }

            Vector2 side = new Vector2(-along.y, along.x).normalized * (width / 2f);
            int start = vh.currentVertCount;
            vh.AddVert(a + side, colour, Vector2.zero);
            vh.AddVert(a - side, colour, Vector2.zero);
            vh.AddVert(b - side, colour, Vector2.zero);
            vh.AddVert(b + side, colour, Vector2.zero);
            vh.AddTriangle(start, start + 1, start + 2);
            vh.AddTriangle(start, start + 2, start + 3);
        }

        static void Dot(VertexHelper vh, Vector2 centre, float radius, Color colour)
        {
            int start = vh.currentVertCount;
            vh.AddVert(centre, colour, Vector2.zero);
            for (int i = 0; i <= DotSides; i++)
            {
                float angle = i * 2f * Mathf.PI / DotSides;
                vh.AddVert(centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius, colour, Vector2.zero);
                if (i > 0)
                {
                    vh.AddTriangle(start, start + i, start + i + 1);
                }
            }
        }
    }
}
