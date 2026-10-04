using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The storm candle of the battle screen (2026-10-04 Diablo kit): it burns down as the storm comes
    /// and is out once the storm is here. The body is filled from the bottom to the share of the time
    /// left, the molten top and the flame stand at the body's top, the flame flickers (and gutters as
    /// the storm nears) and smoke rises once the candle is out. Presentation only: the share comes
    /// from the battle time, nothing here changes the battle, and the flicker runs on real time like
    /// a figure's breathing.
    /// </summary>
    public sealed class CandleView : MonoBehaviour
    {
        /// <summary>The body's sprite holds the wax at 84% of its height, centred (ui_piece fit): the wax starts and ends inside the box.</summary>
        const float ArtBottom = 0.08f;
        const float ArtTop = 0.92f;

        /// <summary>The stub that is left when the candle is out: a little wax above the sprite's empty bottom.</summary>
        const float Stub = ArtBottom + 0.05f;

        const float FlickerSpeed = 11f;
        const float FlickerAmount = 0.1f;
        const float SwayDegrees = 4f;

        /// <summary>How small the flame is right before the storm, and how much more restless.</summary>
        const float GutterScale = 0.55f;
        const float GutterRestlessness = 1.6f;

        const float SmokeDuration = 3f;
        const float SmokeRise = 70f;

        [SerializeField] Image _body;
        [SerializeField] RectTransform _top;
        [SerializeField] RectTransform _flame;
        [SerializeField] Image _glow;
        [SerializeField] Image _smoke;

        float _progress;
        float _closeness;
        bool _lit = true;
        float _phase;
        float _smokeAge = -1f;
        float _bodyBottom;
        float _bodyHeight;
        bool _measured;
        Color _glowColor;

        /// <summary>How far the storm has come, 0..1: the share of the candle that has burnt.</summary>
        public float Progress => _progress;

        /// <summary>False once the storm is here and the candle is out.</summary>
        public bool Lit => _lit;

        /// <summary>Where the candle stands: what the storm's words rise from.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <param name="progress">How far the storm has come, 0..1.</param>
        /// <param name="closeness">How near the storm is over its last seconds, 0..1: the flame gutters as it grows.</param>
        /// <param name="storm">True once the storm is here: the candle goes out.</param>
        public void Show(float progress, float closeness, bool storm)
        {
            Measure();
            _progress = Mathf.Clamp01(progress);
            _closeness = Mathf.Clamp01(closeness);

            // The wax left, as a share of the sprite: never below the stub, so a burnt candle still has a foot.
            float remaining = storm ? Stub : Mathf.Max(Stub, ArtBottom + (ArtTop - ArtBottom) * (1f - _progress));
            _body.fillAmount = remaining;
            float topY = _bodyBottom + _bodyHeight * remaining;
            _top.anchoredPosition = new Vector2(0f, topY);
            _flame.anchoredPosition = new Vector2(0f, topY - 2f);

            if (storm && _lit)
            {
                _lit = false;
                _flame.gameObject.SetActive(false);
                _smoke.rectTransform.anchoredPosition = new Vector2(0f, topY);
                _smoke.color = Color.white;
                _smoke.enabled = true;
                _smokeAge = 0f;
            }
            else if (!storm && !_lit)
            {
                // Another battle: the candle is whole and lit again.
                _lit = true;
                _flame.gameObject.SetActive(true);
                _smoke.enabled = false;
                _smokeAge = -1f;
            }
        }

        void Measure()
        {
            if (_measured)
            {
                return;
            }

            _bodyBottom = _body.rectTransform.anchoredPosition.y;
            _bodyHeight = _body.rectTransform.sizeDelta.y;
            _glowColor = _glow.color;
            _phase = Random.value * 10f;
            _measured = true;
        }

        void Update()
        {
            if (!_measured)
            {
                return;
            }

            float t = Time.time * FlickerSpeed + _phase;
            if (_lit)
            {
                // The flame stretches and leans a little, lower and more restless as the storm nears.
                float gutter = Mathf.Lerp(1f, GutterScale, _closeness);
                float restless = 1f + _closeness * GutterRestlessness;
                float stretch = 1f + FlickerAmount * restless * (0.6f * Mathf.Sin(t) + 0.4f * Mathf.Sin(t * 2.7f + 1.3f));
                float sway = SwayDegrees * restless * Mathf.Sin(t * 0.8f + 0.7f);
                _flame.localScale = new Vector3(gutter * (1f - (stretch - 1f) * 0.5f), gutter * stretch, 1f);
                _flame.localRotation = Quaternion.Euler(0f, 0f, sway);
                _glow.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, _glowColor.a * gutter * (0.9f + 0.1f * Mathf.Sin(t * 1.7f)));
            }

            if (_smokeAge >= 0f)
            {
                _smokeAge += Time.deltaTime;
                float k = _smokeAge / SmokeDuration;
                if (k >= 1f)
                {
                    _smokeAge = -1f;
                    _smoke.enabled = false;
                    return;
                }

                _smoke.rectTransform.anchoredPosition = new Vector2(0f, _bodyBottom + _bodyHeight * Stub + SmokeRise * k);
                _smoke.color = new Color(1f, 1f, 1f, 1f - k);
            }
        }
    }
}
