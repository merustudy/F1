using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A bar drawn as a rectangle whose width follows a ratio. It needs no sprite. Every bar has a second fill over the first, empty until set (Round 95):
    /// a fatigue pip draws the fatigue past the breakdown threshold with it, in red; the other bars leave it empty.
    /// </summary>
    public sealed class UiBar : MonoBehaviour
    {
        [SerializeField] RectTransform _fill;
        [SerializeField] Image _fillImage;
        [SerializeField] RectTransform _overFill;
        [SerializeField] Image _overFillImage;

        /// <summary>The ratio on show, 0..1.</summary>
        public float Ratio { get; private set; }

        /// <summary>The second fill's ratio on show, 0..1 (Round 95: a fatigue pip's second lap); 0 on the bars that leave it empty.</summary>
        public float OverRatio { get; private set; }

        public void Set(int value, int max)
        {
            SetRatio(max > 0 ? (float)value / max : 0f);
        }

        public void SetRatio(float ratio)
        {
            Ratio = Mathf.Clamp01(ratio);
            Fill(_fill, Ratio);
        }

        /// <summary>Stretches the second fill, drawn over the first from the left (the fatigue pips' red: UiBuild.PipBar gives it the colour).</summary>
        public void SetOverRatio(float ratio)
        {
            OverRatio = Mathf.Clamp01(ratio);
            Fill(_overFill, OverRatio);
        }

        public void SetColor(Color color)
        {
            _fillImage.color = color;
        }

        /// <summary>The colour of the fill.</summary>
        public Color FillColor => _fillImage.color;

        /// <summary>The colour of the second fill.</summary>
        public Color OverColor => _overFillImage.color;

        /// <summary>Stretches a fill of a bar to this share of its area, from the left.</summary>
        public static void Fill(RectTransform rect, float ratio)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
