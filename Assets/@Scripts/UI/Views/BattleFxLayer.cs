using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// What the battle shows on top of the stage and the panel when something happens: numbers
    /// and words that rise from a unit, the ghost of a fallen enemy fading away, the grave a fallen
    /// mercenary turns into for a moment, a flash of
    /// lightning, the red at the stage's edges while an ally is at death's door, the shake of
    /// the stage under a heavy hit, and the moment of a breakdown (2026-10-06 round 38, "B"): the glow and the effect burst
    /// behind the unit and the state's word over its head.
    /// (The darkness the storm brings is the storm candle's: its light
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

        /// <summary>
        /// The moment of a breakdown (round 38, "B"): behind the unit the glow in the state's colour and the effect burst (the ink burst of
        /// an affliction and of the collapse, the light burst of a virtue) come in over BurstIn from BurstFrom of their size, stay while the
        /// stage is dark and go with it (the screen gives the hold and the fade, divided by the speed); a virtue's burst turns slowly, an
        /// affliction's trembles. Over the unit's head the state's word, large, with the unit's name under it: it comes WordDelay after the
        /// breakdown rising WordRise over WordRiseFor, stays until WordHold and fades over WordOut (divided by the speed).
        /// </summary>
        const float BurstIn = 0.18f;
        const float BurstFrom = 0.35f;
        const float BurstTurn = 12f;
        const float BurstTremble = 3f;
        const float BurstTrembleFor = 0.5f;
        const float GlowSize = 352f;
        const float GlowAlpha = 0.55f;
        const float BurstHeight = 170f / 300f;
        const float WordDelay = 0.1f;
        const float WordRise = 30f;
        const float WordRiseFor = 0.4f;
        const float WordIn = 0.15f;
        const float WordHold = 1.5f;
        const float WordOut = 0.3f;
        const float WordAbove = 20f;

        [SerializeField] FloatingTextView _floatingTemplate;
        [SerializeField] Image _ghostTemplate;
        [SerializeField] Image _flash;
        [SerializeField] Image _dangerVignette;
        [SerializeField] RectTransform _shaken;
        [SerializeField] RectTransform _momentFx;
        [SerializeField] Image _momentGlow;
        [SerializeField] Image _momentBurst;
        [SerializeField] RectTransform _word;
        [SerializeField] CanvasGroup _wordGroup;
        [SerializeField] TMP_Text _wordText;
        [SerializeField] TMP_Text _wordName;

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
        float _burstAge = -1f;
        float _burstHold;
        float _burstOut;
        float _burstSize;
        bool _burstTurning;
        Vector2 _burstPoint;
        float _wordAge = -1f;
        float _wordPace = 1f;
        Vector2 _wordPoint;
        Color _wordColor;

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

        /// <summary>How many bursts of a breakdown have been shown since the screen opened.</summary>
        public int BurstsShown { get; private set; }

        /// <summary>True while the glow and the burst of a breakdown stand behind a unit.</summary>
        public bool BurstShown => _burstAge >= 0f;

        /// <summary>True while the word of a breakdown stands over a unit's head.</summary>
        public bool WordShown => _wordAge >= 0f;

        /// <summary>The state's word last shown over a unit's head.</summary>
        public string WordText => _wordText.text;

        /// <summary>The colour the word was shown in: the state's.</summary>
        public Color WordColor => _wordColor;

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
            rect.localScale = ScaleOf(from);
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
            rect.localScale = ScaleOf(from);

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

        /// <summary>
        /// The glow and the effect burst of a breakdown behind a unit (round 38): placed on the moment's layer in the field, at the
        /// unit's chest, so that they stand behind the lit unit and over the dark. They come in, hold this long and fade over this long
        /// (the screen's times, divided by the speed); a second breakdown meanwhile takes them over.
        /// </summary>
        /// <param name="over">The unit's figure place.</param>
        /// <param name="size">How large the burst is drawn at full, in the field's units.</param>
        /// <param name="turning">True for a virtue: the burst turns slowly. An affliction's trembles instead.</param>
        public void Burst(RectTransform over, Sprite burst, Color color, float size, bool turning, float hold, float fade)
        {
            if (over == null || burst == null)
            {
                return;
            }

            Rect rect = over.rect;
            _burstPoint = LocalPointIn(_momentFx, over, new Vector2(rect.center.x, rect.yMin + rect.height * BurstHeight));
            _burstSize = size;
            _burstTurning = turning;
            _burstHold = Mathf.Max(0.01f, hold);
            _burstOut = Mathf.Max(0.01f, fade);
            _burstAge = 0f;
            _momentGlow.color = new Color(color.r, color.g, color.b, 0f);
            _momentBurst.sprite = burst;
            foreach (RectTransform piece in new[] { _momentGlow.rectTransform, _momentBurst.rectTransform })
            {
                piece.anchorMin = piece.anchorMax = _momentFx.pivot;
                piece.pivot = new Vector2(0.5f, 0.5f);
            }

            _momentGlow.enabled = true;
            _momentBurst.enabled = true;
            BurstsShown++;
            StepBurst(0f);
        }

        void StepBurst(float dt)
        {
            _burstAge += dt;
            if (_burstAge >= _burstHold + _burstOut)
            {
                _burstAge = -1f;
                _momentGlow.enabled = false;
                _momentBurst.enabled = false;
                return;
            }

            float k = EaseOut(_burstAge / BurstIn);
            float alpha = _burstAge < _burstHold ? 1f : 1f - (_burstAge - _burstHold) / _burstOut;

            RectTransform glow = _momentGlow.rectTransform;
            float glowSize = GlowSize * (0.5f + 0.5f * k);
            glow.sizeDelta = new Vector2(glowSize, glowSize);
            glow.anchoredPosition = _burstPoint;
            Color c = _momentGlow.color;
            _momentGlow.color = new Color(c.r, c.g, c.b, GlowAlpha * alpha);

            RectTransform burst = _momentBurst.rectTransform;
            float size = _burstSize * (BurstFrom + (1f - BurstFrom) * k);
            burst.sizeDelta = new Vector2(size, size);
            Vector2 tremble = Vector2.zero;
            if (_burstTurning)
            {
                burst.localRotation = Quaternion.Euler(0f, 0f, -BurstTurn * _burstAge);
            }
            else
            {
                burst.localRotation = Quaternion.identity;
                if (_burstAge < BurstTrembleFor)
                {
                    tremble = Random.insideUnitCircle * BurstTremble * (1f - _burstAge / BurstTrembleFor);
                }
            }

            burst.anchoredPosition = _burstPoint + tremble;
            _momentBurst.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>
        /// The word of a breakdown over a unit's head (round 38): the state's name (or the collapse's words) large in the state's colour,
        /// the unit's name under it. It rises a little as it comes, stays and fades; the times are divided by the speed.
        /// </summary>
        public void Word(RectTransform over, string word, string name, Color color, float pace)
        {
            if (over == null)
            {
                return;
            }

            Rect rect = over.rect;
            _wordPoint = LocalPoint(over, new Vector2(rect.center.x, rect.yMax)) + new Vector2(0f, WordAbove + _word.rect.height / 2f);
            _wordPace = Mathf.Max(0.01f, pace);
            _wordColor = color;
            _wordText.text = word;
            _wordText.color = color;
            _wordName.text = name;
            _wordAge = 0f;
            _word.SetAsLastSibling();
            _word.gameObject.SetActive(true);
            StepWord(0f);
        }

        void StepWord(float dt)
        {
            _wordAge += dt;
            float t = _wordAge * _wordPace;
            if (t >= WordHold + WordOut)
            {
                _wordAge = -1f;
                _word.gameObject.SetActive(false);
                return;
            }

            float since = t - WordDelay;
            float alpha = since < 0f ? 0f : Mathf.Min(1f, since / WordIn);
            if (t > WordHold)
            {
                alpha = Mathf.Min(alpha, 1f - (t - WordHold) / WordOut);
            }

            _wordGroup.alpha = alpha;
            _word.anchoredPosition = _wordPoint + new Vector2(0f, WordRise * EaseOut(Mathf.Max(0f, since) / WordRiseFor));
        }

        static float EaseOut(float x)
        {
            x = Mathf.Clamp01(x);
            return 1f - (1f - x) * (1f - x);
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

            // The moment of a breakdown runs on real time, as the kill moment does (BattleScreen): its burst and its word keep pace with the
            // dark and the zoom while the battle and the motions run slow.
            if (_burstAge >= 0f)
            {
                StepBurst(Time.unscaledDeltaTime);
            }

            if (_wordAge >= 0f)
            {
                StepWord(Time.unscaledDeltaTime);
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

        /// <summary>
        /// How large another rect is drawn against this layer: a copy made here of art on the stage keeps the size the art had
        /// on the screen, while the stage draws in for a kill moment as well.
        /// </summary>
        Vector3 ScaleOf(RectTransform from)
        {
            Vector3 art = from.lossyScale;
            Vector3 layer = _rect.lossyScale;
            return new Vector3(art.x / layer.x, art.y / layer.y, 1f);
        }

        /// <summary>A point of another rect, in this layer's coordinates (every rect is under the same canvas).</summary>
        Vector2 LocalPoint(RectTransform from, Vector2 pointInFrom)
        {
            return LocalPointIn(_rect, from, pointInFrom);
        }

        /// <summary>A point of another rect, in the coordinates of a layer (from its pivot).</summary>
        static Vector2 LocalPointIn(RectTransform layer, RectTransform from, Vector2 pointInFrom)
        {
            Vector3 world = from.TransformPoint(pointInFrom);
            Vector3 local = layer.InverseTransformPoint(world);
            return new Vector2(local.x, local.y);
        }
    }
}
