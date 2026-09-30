using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>A bar drawn as a rectangle whose width follows a ratio. It needs no sprite.</summary>
    public sealed class UiBar : MonoBehaviour
    {
        [SerializeField] RectTransform _fill;
        [SerializeField] Image _fillImage;

        public void Set(int value, int max)
        {
            SetRatio(max > 0 ? (float)value / max : 0f);
        }

        public void SetRatio(float ratio)
        {
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;
        }

        public void SetColor(Color color)
        {
            _fillImage.color = color;
        }
    }
}
