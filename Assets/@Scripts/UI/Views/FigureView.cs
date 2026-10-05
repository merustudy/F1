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
    ///
    /// For a moment it can show a pose instead of the figure (a unit's attack or hit pose,
    /// Docs/Design/10 §5). A pose is drawn on a canvas three figure canvases wide and an eighth
    /// deeper, with the figure canvas in the middle and its floor line where the figure's is
    /// (ArtPipeline: Docs/Architecture/13), so the art's place widens to that and stands on the
    /// pose's floor line: the feet stay on the floor and the figure stays in its place. It can
    /// also swell from the feet with a warm light behind it: a support item was used.
    /// </summary>
    public sealed class FigureView : MonoBehaviour
    {
        const float BreathPeriod = 1.8f;
        const float BreathAmount = 0.02f;
        const float FlashDuration = 0.28f;

        /// <summary>The pose canvas against the figure canvas: three times as wide, an eighth deeper, its floor line 112 of 1008 from the bottom.</summary>
        const float PoseWidth = 3f;
        const float PoseHeight = 1008f / 896f;
        const float PoseFloor = 112f / 1008f;

        /// <summary>A support item's pulse: the swell (from the feet) and how long it and the light last.</summary>
        const float PulseSwell = 0.06f;
        const float PulseOut = 0.1f;
        const float PulseLength = 0.3f;
        const float PulseLight = 0.6f;

        [SerializeField] Image _art;
        [SerializeField] GameObject _placeholder;
        [SerializeField] Image _glow;

        RectTransform _rect;
        Vector2 _base;
        bool _baseKnown;
        Vector2 _artBase;
        bool _artBaseKnown;
        float _phase;
        float _flash;
        Color _flashTint = Color.white;
        Sprite _figure;
        float _scale = 1f;
        float _pulse = -1f;

        /// <summary>The art on show (the figure, or the pose shown for a moment), or null while the placeholder stands in.</summary>
        public Sprite Art => _art.enabled ? _art.sprite : null;

        /// <summary>True while a pose stands in for the figure.</summary>
        public bool ShowsPose => _art.enabled && _figure != null && _art.sprite != _figure;

        /// <summary>True while a support item's pulse plays.</summary>
        public bool Pulsing => _pulse >= 0f;

        public bool ShowsPlaceholder => _placeholder.activeSelf;

        /// <summary>The image of the art, for a ghost of it. Disabled while the placeholder stands in.</summary>
        public Image ArtImage => _art;

        /// <param name="art">Null for a unit without art.</param>
        public void Show(Sprite art)
        {
            _figure = art;
            _art.sprite = art;
            _art.enabled = art != null;
            _art.color = Color.white;
            _placeholder.SetActive(art == null);
        }

        /// <summary>
        /// Draws the art this many times the common size of a figure place, keeping its feet on the floor line
        /// (the art is anchored at the bottom centre). A boss is drawn larger than its place and may overlap its
        /// neighbours; the place itself, and so the layout, does not change.
        /// </summary>
        public void SetScale(float scale)
        {
            if (!_artBaseKnown)
            {
                _artBase = _art.rectTransform.sizeDelta;
                _artBaseKnown = true;
            }

            _scale = Mathf.Max(0.1f, scale);
            _art.rectTransform.sizeDelta = _artBase * _scale;
        }

        /// <summary>
        /// Shows a pose in place of the figure until <see cref="ShowFigure"/>: the art's place widens to the pose canvas
        /// and stands on its floor line. Nothing happens without a figure or a pose.
        /// </summary>
        public void ShowPose(Sprite pose)
        {
            if (pose == null || _figure == null || !_art.enabled)
            {
                return;
            }

            SetScale(_scale);
            _art.sprite = pose;
            RectTransform rect = _art.rectTransform;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x * PoseWidth, rect.sizeDelta.y * PoseHeight);
            rect.pivot = new Vector2(0.5f, PoseFloor);
        }

        /// <summary>Shows the figure again after a pose.</summary>
        public void ShowFigure()
        {
            if (_figure == null || _art.sprite == _figure)
            {
                return;
            }

            _art.sprite = _figure;
            _art.rectTransform.pivot = new Vector2(0.5f, 0f);
            SetScale(_scale);
        }

        /// <summary>A support item was used: the art swells a little from the feet and a warm light behind it fades.</summary>
        public void Pulse()
        {
            _pulse = 0f;
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
            float swell = 1f;
            if (_pulse >= 0f)
            {
                _pulse += Time.deltaTime;
                if (_pulse >= PulseLength)
                {
                    _pulse = -1f;
                }
                else
                {
                    swell += PulseSwell * (_pulse < PulseOut ? _pulse / PulseOut : 1f - (_pulse - PulseOut) / (PulseLength - PulseOut));
                }
            }

            if (_glow != null)
            {
                bool lit = _pulse >= 0f;
                if (_glow.enabled != lit)
                {
                    _glow.enabled = lit;
                }

                if (lit)
                {
                    Color c = _glow.color;
                    _glow.color = new Color(c.r, c.g, c.b, PulseLight * (1f - _pulse / PulseLength));
                }
            }

            if (_art.enabled)
            {
                float breath = 1f + BreathAmount * (0.5f + 0.5f * Mathf.Sin(Time.time * (Mathf.PI * 2f / BreathPeriod) + _phase));
                _art.rectTransform.localScale = new Vector3(swell, breath * swell, 1f);
            }

            if (_flash > 0f)
            {
                _flash -= Time.deltaTime;
                _art.color = Color.Lerp(Color.white, _flashTint, Mathf.Clamp01(_flash / FlashDuration));
            }
        }
    }
}
