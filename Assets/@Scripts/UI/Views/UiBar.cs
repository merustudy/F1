using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>A bar drawn as a rectangle whose width follows a ratio. It needs no sprite.</summary>
    public sealed class UiBar : MonoBehaviour
    {
        [SerializeField] RectTransform _fill;
        [SerializeField] Image _fillImage;

        /// <summary>The ratio on show, 0..1.</summary>
        public float Ratio { get; private set; }

        public void Set(int value, int max)
        {
            SetRatio(max > 0 ? (float)value / max : 0f);
        }

        public void SetRatio(float ratio)
        {
            Ratio = Mathf.Clamp01(ratio);
            Fill(_fill, Ratio);
        }

        public void SetColor(Color color)
        {
            _fillImage.color = color;
        }

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
