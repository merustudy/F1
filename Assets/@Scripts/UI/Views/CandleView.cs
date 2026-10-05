using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The storm candle of the battle screen (2026-10-04 Diablo kit): it burns down as the storm comes
    /// and is out once the storm is here. The body is filled from the bottom to the share of the time
    /// left, the molten top and the flame stand at the body's top, the flame flickers (and gutters as
    /// the storm nears) and smoke rises once the candle is out.
    ///
    /// It is also the stage's only light (2026-10-04 mockup B, ArtPipeline/Archive/16-candle-light):
    /// the stage is lit in a half disc around the flame and darkens with the distance from it. The
    /// light's layers lie over the background, under the units, far from the candle in the hierarchy;
    /// this puts them on the flame's middle, so the light goes down as the candle burns. Over the
    /// storm's last seconds the light pulls in as the flame gutters, and when the flame goes out the
    /// light fades and the whole stage takes the darkness that lay beyond its reach.
    ///
    /// Presentation only: the share comes from the battle time, nothing here changes the battle, and
    /// the flicker and the fading run on the frame clock (Time.time) like a figure's breathing, so a
    /// kill moment slows them with the rest of the stage.
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

        /// <summary>How much of its reach the stage's light loses over the storm's last seconds, as the flame gutters.</summary>
        const float LightPull = 0.2f;

        /// <summary>How much the light's reach breathes, and its warmth flickers, with the flame.</summary>
        const float LightBreath = 0.012f;
        const float LightFlicker = 0.08f;

        /// <summary>How long the stage takes to go dark once the flame is out.</summary>
        const float LightOutSeconds = 0.4f;

        [SerializeField] Image _body;
        [SerializeField] RectTransform _top;
        [SerializeField] RectTransform _flame;
        [SerializeField] Image _glow;
        [SerializeField] Image _smoke;

        /// <summary>The stage's light: a point kept on the flame, the darkness around it and its warm light (both its children), and the dark over the whole stage once the candle is out.</summary>
        [SerializeField] RectTransform _light;
        [SerializeField] Image _lightDark;
        [SerializeField] Image _lightWarm;
        [SerializeField] Image _lightOut;

        float _progress;
        float _closeness;
        bool _lit = true;
        float _phase;
        float _smokeAge = -1f;
        float _bodyBottom;
        float _bodyHeight;
        bool _measured;
        Color _glowColor;
        float _flameHeight;
        float _darkness;
        Color _warmColor;

        /// <summary>1 while the flame burns; once it is out, down to 0 as the light fades.</summary>
        float _lightLeft = 1f;

        /// <summary>How far the storm has come, 0..1: the share of the candle that has burnt.</summary>
        public float Progress => _progress;

        /// <summary>False once the storm is here and the candle is out.</summary>
        public bool Lit => _lit;

        /// <summary>Where the candle stands: what the storm's words rise from.</summary>
        public RectTransform Rect => (RectTransform)transform;

        /// <summary>How dark the stage is where the light does not reach (the alpha of black over the background).</summary>
        public float Darkness => _darkness;

        /// <summary>How dark the stage is right above the flame: none while it burns, as dark as beyond the light's reach once it is out and the light has faded.</summary>
        public float DarknessAtFlame => _lightOut.enabled ? _lightOut.color.a : 0f;

        /// <summary>The light's reach, 1 at full: it pulls in over the storm's last seconds.</summary>
        public float LightReach => 1f - LightPull * _closeness;

        /// <summary>Where the light stands: on the middle of the flame.</summary>
        public Vector3 LightPosition => _light.position;

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

            // The light lies under the stage's units, far from here in the hierarchy: put it on the flame's middle.
            _light.position = _flame.parent.TransformPoint(_flame.localPosition + new Vector3(0f, _flameHeight / 2f, 0f));

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
                // Another battle: the candle is whole and lit again, and so is the stage.
                _lit = true;
                _flame.gameObject.SetActive(true);
                _smoke.enabled = false;
                _smokeAge = -1f;
                _lightLeft = 1f;
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
            _flameHeight = _flame.sizeDelta.y;
            _darkness = _lightDark.color.a;
            _warmColor = _lightWarm.color;
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
            float gutter = Mathf.Lerp(1f, GutterScale, _closeness);
            float pulse = Mathf.Sin(t * 1.7f);
            if (_lit)
            {
                // The flame stretches and leans a little, lower and more restless as the storm nears.
                float restless = 1f + _closeness * GutterRestlessness;
                float stretch = 1f + FlickerAmount * restless * (0.6f * Mathf.Sin(t) + 0.4f * Mathf.Sin(t * 2.7f + 1.3f));
                float sway = SwayDegrees * restless * Mathf.Sin(t * 0.8f + 0.7f);
                _flame.localScale = new Vector3(gutter * (1f - (stretch - 1f) * 0.5f), gutter * stretch, 1f);
                _flame.localRotation = Quaternion.Euler(0f, 0f, sway);
                _glow.color = new Color(_glowColor.r, _glowColor.g, _glowColor.b, _glowColor.a * gutter * (0.9f + 0.1f * pulse));
            }
            else if (_lightLeft > 0f)
            {
                _lightLeft = Mathf.Max(0f, _lightLeft - Time.deltaTime / LightOutSeconds);
            }

            RenderLight(gutter, pulse);

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

        /// <summary>
        /// The stage's light: its reach pulls in as the storm nears and breathes with the flame, and its warmth fades as
        /// the flame gutters. Once the flame is out the light fades: the darkness around it goes and a dark over the whole
        /// stage comes, so that what lay beyond the reach stays as dark as it was and the rest becomes as dark.
        /// </summary>
        void RenderLight(float gutter, float pulse)
        {
            float reach = LightReach * (1f + LightBreath * pulse);
            _light.localScale = new Vector3(reach, reach, 1f);

            float around = _darkness * _lightLeft;
            _lightDark.color = new Color(0f, 0f, 0f, around);
            float cover = _lightLeft >= 1f ? 0f : 1f - (1f - _darkness) / (1f - around);
            _lightOut.color = new Color(0f, 0f, 0f, cover);
            _lightOut.enabled = cover > 0.001f;

            _lightWarm.color = new Color(_warmColor.r, _warmColor.g, _warmColor.b, _warmColor.a * gutter * (1f + LightFlicker * pulse) * _lightLeft);
        }
    }
}
