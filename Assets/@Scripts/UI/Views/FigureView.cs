using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The place of a unit's full-body art: the art when the unit has one, the placeholder
    /// silhouette when it has none. Every figure is drawn on the same canvas with the same floor
    /// line, so the art stands at the bottom centre of the place at one size, whatever the width
    /// of the place is. The art breathes (a slow, slight stretch from the feet) so that a unit
    /// never stands frozen, and it can flash a tint for a moment and be moved as a whole for a
    /// lunge, a recoil or a walk; none of that changes where the place is laid out.
    /// </summary>
    public sealed class FigureView : MonoBehaviour
    {
        const float BreathPeriod = 1.8f;
        const float BreathAmount = 0.02f;
        const float FlashDuration = 0.28f;

        [SerializeField] Image _art;
        [SerializeField] GameObject _placeholder;

        RectTransform _rect;
        Vector2 _base;
        bool _baseKnown;
        float _phase;
        float _flash;
        Color _flashTint = Color.white;

        /// <summary>The art on show, or null while the placeholder stands in.</summary>
        public Sprite Art => _art.enabled ? _art.sprite : null;

        public bool ShowsPlaceholder => _placeholder.activeSelf;

        /// <summary>The image of the art, for a ghost of it. Disabled while the placeholder stands in.</summary>
        public Image ArtImage => _art;

        /// <param name="art">Null for a unit without art.</param>
        public void Show(Sprite art)
        {
            _art.sprite = art;
            _art.enabled = art != null;
            _art.color = Color.white;
            _placeholder.SetActive(art == null);
        }

        /// <summary>Moves the whole place by this much from where it is laid out.</summary>
        public void SetMotion(Vector2 offset)
        {
            if (_rect == null)
            {
                _rect = (RectTransform)transform;
            }

            if (!_baseKnown)
            {
                _base = _rect.anchoredPosition;
                _baseKnown = true;
            }

            _rect.anchoredPosition = _base + offset;
        }

        /// <summary>Tints the art for a moment: a hit, a heal.</summary>
        public void Flash(Color tint)
        {
            _flash = FlashDuration;
            _flashTint = tint;
        }

        void OnEnable()
        {
            // Units do not breathe in step.
            _phase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (_art.enabled)
            {
                float breath = 1f + BreathAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * (Mathf.PI * 2f / BreathPeriod) + _phase));
                _art.rectTransform.localScale = new Vector3(1f, breath, 1f);
            }

            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                _art.color = Color.Lerp(Color.white, _flashTint, Mathf.Clamp01(_flash / FlashDuration));
            }
        }
    }
}
