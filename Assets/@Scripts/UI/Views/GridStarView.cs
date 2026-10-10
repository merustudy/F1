using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A star of an item drawn on its star square (Slice B stage 20; Docs/Architecture/12_UI.md "격자 보드"; Backpack Battles' stars): a star
    /// over whatever lies on the square, gold with a soft glow behind when lit, hollow otherwise. Takes no pointer.
    /// </summary>
    public sealed class GridStarView : MonoBehaviour
    {
        /// <summary>
        /// The fill inside the edge (round 51's mockup): wide and gold when lit, small and dark when hollow so that a pale band of the edge
        /// shows round it. The star art has its own dark rim, which stands for the mockup's black outline.
        /// </summary>
        const float LitFill = 26f;
        const float HollowFill = 20f;

        [SerializeField] Image _glow;
        [SerializeField] Image _edge;
        [SerializeField] Image _fill;

        /// <summary>Whether the star shows lit.</summary>
        public bool Lit { get; private set; }

        /// <summary>The square it stands on.</summary>
        public int X { get; private set; }
        public int Y { get; private set; }

        public void Show(int x, int y, bool lit)
        {
            X = x;
            Y = y;
            Lit = lit;
            gameObject.SetActive(true);
            GridGeometry.Lay((RectTransform)transform, x, y, 1, 1);
            _glow.enabled = lit;
            _glow.color = UiPalette.StarGlow;
            _edge.color = lit ? UiPalette.StarLitEdge : UiPalette.StarHollowEdge;
            _fill.color = lit ? UiPalette.StarLit : UiPalette.StarHollow;
            _fill.rectTransform.sizeDelta = new Vector2(lit ? LitFill : HollowFill, lit ? LitFill : HollowFill);
        }
    }
}
