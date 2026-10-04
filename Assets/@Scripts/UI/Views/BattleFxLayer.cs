using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// What the battle shows on top of the stage and the panel when something happens: numbers
    /// and words that rise from a unit, the ghost of a fallen enemy fading away, the grave a fallen
    /// mercenary turns into for a moment, a flash of
    /// lightning, the red at the stage's edges while an ally is at death's door, and the shake of
    /// the stage under a heavy hit. (The darkness the storm brings is the storm candle's: its light
    /// pulls in and goes out, CandleView.) It decides nothing: the
    /// presenter reads the event log and calls it (Docs/Architecture/12_UI.md, "연출"). Nothing
    /// here takes a click.
    /// </summary>
    public sealed class BattleFxLayer : MonoBehaviour
    {
        const float ShakeFall = 36f;
        const float FlashFall = 11f;
        const float GhostLife = 0.55f;
        const float GhostSink = 26f;
        const float DangerPulse = 2.4f;
        const float DangerAlphaMin = 0.16f;
        const float DangerAlphaMax = 0.42f;

        /// <summary>Texts that start within this time and this distance of each other are stacked, so that they do not cover each other.</summary>
        const float StackWindow = 0.4f;
        const float StackDistance = 60f;
        const float StackStep = 36f;

        [SerializeField] FloatingTextView _floatingTemplate;
        [SerializeField] Image _ghostTemplate;
        [SerializeField] Image _flash;
        [SerializeField] Image _dangerVignette;
        [SerializeField] RectTransform _shaken;

        readonly List<FloatingTextView> _floats = new List<FloatingTextView>();
        readonly List<Fading> _ghosts = new List<Fading>();
        readonly List<Standing> _graves = new List<Standing>();
        readonly List<Launch> _launches = new List<Launch>();
        RectTransform _rect;
        float _shake;
        Vector2 _shakeBase;
        bool _shaking;
        float _flashAlpha;
        bool _danger;
        float _dangerPhase;

        /// <summary>Where and when a text started, so that the next one near it starts higher.</summary>
        sealed class Launch
        {
            public Vector2 Point;
            public float Time;
        }

        /// <summary>A ghost on its way out.</summary>
        sealed class Fading
        {
            public Image Image;
            public Vector2 Start;
            public float Age;
        }

        /// <summary>A grave that stands for a while and then goes.</summary>
        sealed class Standing
        {
            public Image Image;
            public float Born;
            public float Hold;
            public float Vanish;

            public float Left => Born + Hold + Vanish - Time.time;
        }

        /// <summary>How many texts have risen since the screen opened.</summary>
        public int FloatingPlayed { get; private set; }

        /// <summary>How many ghosts have started to fade since the screen opened.</summary>
        public int GhostsShown { get; private set; }

        /// <summary>Seconds until the last grave that stands has gone; 0 while none stands. The battle screen keeps the party's places until then.</summary>
        public float GravesLeft
        {
            get
            {
                float left = 0f;
                foreach (Standing grave in _graves)
                {
                    left = Mathf.Max(left, grave.Left);
                }

                return left;
            }
        }

        /// <summary>True while the red at the edges says an ally is at death's door.</summary>
        public bool DangerShown => _danger;

        void Awake()
        {
            _rect = (RectTransform)transform;
        }

        /// <summary>A text that rises from the top centre of a place on the screen.</summary>
        /// <param name="big">True for a heavy moment: a large hit, death's door, a death.</param>
        public void Float(RectTransform over, string text, Color color, bool big)
        {
            if (over == null)
            {
                return;
            }

            FloatingTextView view = null;
            foreach (FloatingTextView candidate in _floats)
            {
                if (!candidate.Playing)
                {
                    view = candidate;
                    break;
                }
            }

            if (view == null)
            {
                view = Instantiate(_floatingTemplate, _rect);
                _floats.Add(view);
            }

            Vector2 top = LocalPoint(over, new Vector2(over.rect.center.x, over.rect.yMax));
            top += new Vector2(0f, StackStep * CountRecentNear(top));
            _launches.Add(new Launch { Point = top, Time = Time.time });
            view.Play(top, text, color, big ? 1.4f : 1f, big ? 1.4f : 1.05f);
            FloatingPlayed++;
        }

        /// <summary>How many texts started near this point a moment ago. Old launches are forgotten here.</summary>
        int CountRecentNear(Vector2 point)
        {
            int count = 0;
            for (int i = _launches.Count - 1; i >= 0; i--)
            {
                if (Time.time - _launches[i].Time > StackWindow)
                {
                    _launches.RemoveAt(i);
                }
                else if (Mathf.Abs(_launches[i].Point.x - point.x) < StackDistance)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>A copy of a unit's art that fades and sinks where the unit fell. The unit itself leaves the stage at once.</summary>
        public void Ghost(Image art)
        {
            if (art == null || !art.enabled || art.sprite == null)
            {
                return;
            }

            Image ghost = Instantiate(_ghostTemplate, _rect);
            RectTransform from = art.rectTransform;
            RectTransform rect = ghost.rectTransform;
            ghost.sprite = art.sprite;
            ghost.preserveAspect = art.preserveAspect;
            rect.sizeDelta = from.rect.size;
            rect.anchoredPosition = LocalPoint(from, from.rect.center);
            ghost.color = Color.white;
            ghost.gameObject.SetActive(true);
            _ghosts.Add(new Fading { Image = ghost, Start = rect.anchoredPosition, Age = 0f });
            GhostsShown++;
        }

        /// <summary>
        /// A grave where a unit's art stood: it is there at once in place of the figure, stands `hold` seconds and fades
        /// out over `vanish` (2026-10-04, round 21). The grave is drawn on the canvas every figure shares, so it fills the
        /// figure's place and stands on its floor line. The unit itself leaves the stage at once. Its time is counted from
        /// the frame it appears in, so a long frame before it does not cut it short.
        /// </summary>
        public void Grave(Image art, Sprite grave, float hold, float vanish)
        {
            if (art == null || grave == null)
            {
                return;
            }

            Image image = Instantiate(_ghostTemplate, _rect);
            RectTransform from = art.rectTransform;
            RectTransform rect = image.rectTransform;
            image.sprite = grave;
            image.preserveAspect = true;
            rect.sizeDelta = from.rect.size;

            // From the feet: the art breathes by stretching up from them, so its centre is not where it stands.
            rect.anchoredPosition = LocalPoint(from, new Vector2(from.rect.center.x, from.rect.yMin)) + new Vector2(0f, from.rect.height * 0.5f);
            image.color = Color.white;
            image.gameObject.SetActive(true);
            _graves.Add(new Standing { Image = image, Born = Time.time, Hold = hold, Vanish = Mathf.Max(0.01f, vanish) });
        }

        /// <summary>A flash of light over the stage that dies down at once: lightning.</summary>
        public void Flash(float alpha)
        {
            _flashAlpha = Mathf.Max(_flashAlpha, alpha);
        }

        /// <summary>Shakes the stage by up to this many pixels, dying down over a few frames.</summary>
        public void Shake(float amplitude)
        {
            if (!_shaking)
            {
                _shakeBase = _shaken.anchoredPosition;
                _shaking = true;
            }

            _shake = Mathf.Max(_shake, amplitude);
        }

        public void SetDanger(bool danger)
        {
            _danger = danger;
            if (!danger)
            {
                _dangerVignette.enabled = false;
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;

            if (_shaking)
            {
                _shake = Mathf.MoveTowards(_shake, 0f, ShakeFall * dt);
                if (_shake <= 0.01f)
                {
                    _shaking = false;
                    _shaken.anchoredPosition = _shakeBase;
                }
                else
                {
                    _shaken.anchoredPosition = _shakeBase + Random.insideUnitCircle * _shake;
                }
            }

            if (_flashAlpha > 0f)
            {
                _flashAlpha = Mathf.MoveTowards(_flashAlpha, 0f, FlashFall * dt * Mathf.Max(0.08f, _flashAlpha));
                _flash.color = new Color(1f, 1f, 1f, _flashAlpha);
                _flash.enabled = _flashAlpha > 0.005f;
            }

            if (_danger)
            {
                _dangerPhase += dt * DangerPulse;
                float alpha = Mathf.Lerp(DangerAlphaMin, DangerAlphaMax, 0.5f + 0.5f * Mathf.Sin(_dangerPhase));
                _dangerVignette.color = new Color(UiPalette.Danger.r, UiPalette.Danger.g, UiPalette.Danger.b, alpha);
                _dangerVignette.enabled = true;
            }

            for (int i = _graves.Count - 1; i >= 0; i--)
            {
                Standing grave = _graves[i];
                if (grave.Left <= 0f)
                {
                    Destroy(grave.Image.gameObject);
                    _graves.RemoveAt(i);
                    continue;
                }

                grave.Image.color = new Color(1f, 1f, 1f, Mathf.Clamp01(grave.Left / grave.Vanish));
            }

            for (int i = _ghosts.Count - 1; i >= 0; i--)
            {
                Fading ghost = _ghosts[i];
                ghost.Age += dt;
                float t = ghost.Age / GhostLife;
                if (t >= 1f)
                {
                    Destroy(ghost.Image.gameObject);
                    _ghosts.RemoveAt(i);
                    continue;
                }

                ghost.Image.color = new Color(1f, 1f, 1f, 1f - t);
                ghost.Image.rectTransform.anchoredPosition = ghost.Start - new Vector2(0f, GhostSink * t);
            }
        }

        /// <summary>A point of another rect, in this layer's coordinates (every rect is under the same canvas).</summary>
        Vector2 LocalPoint(RectTransform from, Vector2 pointInFrom)
        {
            Vector3 world = from.TransformPoint(pointInFrom);
            Vector3 local = _rect.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }
    }
}
