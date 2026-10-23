using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using F1.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The intro (Slice D stage 55, Docs/Design/33_Black_Lily_Story_Summary.md §9): the scenes of the "intro" sequence played as the Round 98 motion
    /// mockup — the camera moving over each picture, the crossings, the dip to black, the shake and the heartbeat, the lines coming in over a soft
    /// band, a faint hint at the bottom right. Everything it says and shows comes from StorySceneData; <see cref="IntroTimeline"/> computes the film.
    /// After a new run it plays the full cut the first time and the short cut after (the setting IntroSeen) and goes on to the lobby, leaving it the
    /// banner of the last scene; from the title's [인트로 보기] it plays the full cut and goes back. A click or space goes on, Esc skips to the end.
    /// It is presentation only: the run was saved before it opened, and nothing waits for it.
    /// </summary>
    public sealed class IntroScreen : UIScreen
    {
        /// <summary>The sequence of StorySceneData this screen plays.</summary>
        public const string Sequence = "intro";

        /// <summary>The subtitles: 30 px at 720 in the mockup, 45 at 1080, 18 between lines, the last line's foot 69 over the bottom.</summary>
        public const float LineSize = 45f;
        public const float LineGap = 18f;
        public const float LinesBottom = 69f;

        [SerializeField] RawImage _pictureBase;
        [SerializeField] RawImage _pictureOver;
        [SerializeField] RawImage _vignette;
        [SerializeField] RawImage _band;
        [SerializeField] RectTransform[] _lineRows;
        [SerializeField] CanvasGroup[] _lineGroups;
        [SerializeField] TMP_Text[] _lineTexts;
        [SerializeField] TMP_Text[] _lineShadows;
        [SerializeField] TMP_Text _hint;
        [SerializeField] Image _black;
        [SerializeField] Button _catcher;

        readonly Dictionary<string, Sprite> _pictures = new Dictionary<string, Sprite>(System.StringComparer.Ordinal);
        List<StorySceneData> _scenes;
        IntroTimeline _timeline;
        float _t;
        bool _finished;

        /// <summary>The film's clock, in ms (tests read and move it).</summary>
        public float Time => _t;

        public IntroTimeline Timeline => _timeline;
        public bool Finished => _finished;

        /// <summary>The shot showing now (the scene's id), for the tests.</summary>
        public string CurrentScene => _timeline.Shots.Count == 0 ? null : _timeline.Shots[_timeline.ShotAt(_t)].Scene.Id;

        /// <summary>The words of the subtitles now (rich text), for the tests.</summary>
        public IReadOnlyList<TMP_Text> Lines => _lineTexts;

        bool ShortCut => StoryHandoff.Intro == StoryHandoff.IntroMode.NewRun && Managers.Setting.IntroSeen;

        /// <summary>Every picture of the sequence, into the story's scope (released when the screen goes).</summary>
        public override async Task PrepareAsync()
        {
            _scenes = Managers.Data.Data.ScenesOf(Sequence);
            foreach (StorySceneData scene in _scenes)
            {
                if (scene.Image != null && !_pictures.ContainsKey(scene.Image))
                {
                    _pictures[scene.Image] = await Managers.Resource.LoadAsync<Sprite>(scene.Image, ResourceScope.Story);
                }
            }
        }

        protected override void OnOpen()
        {
            _catcher.onClick.AddListener(Next);
            _vignette.texture = VignetteTexture();
            _band.texture = BandTexture();
            _hint.text = UiStrings.Get(UiKeys.Story.IntroHint);
            _t = 0f;
        }

        public override void Refresh()
        {
            // The words follow the locale: the film is rebuilt at the same moment.
            _timeline = IntroTimeline.Build(_scenes, ShortCut, UiText.Name);
            _hint.text = UiStrings.Get(UiKeys.Story.IntroHint);
            if (_timeline.Shots.Count == 0)
            {
                Finish();
                return;
            }

            Draw();
        }

        void Update()
        {
            if (_finished || _timeline == null || _timeline.Shots.Count == 0)
            {
                return;
            }

            if (StoryKeys.Skip())
            {
                Skip();
                return;
            }

            if (StoryKeys.Next())
            {
                Next();
                return;
            }

            Advance(UnityEngine.Time.unscaledDeltaTime * 1000f);
        }

        /// <summary>Moves the film on by <paramref name="ms"/> (the frame's real time; tests step it).</summary>
        public void Advance(float ms)
        {
            if (_finished)
            {
                return;
            }

            _t += ms;
            if (_t >= _timeline.Total)
            {
                Finish();
                return;
            }

            Draw();
        }

        /// <summary>A click or space: every line of the scene in, else the next scene, else the end.</summary>
        public void Next()
        {
            if (_finished)
            {
                return;
            }

            _t = _timeline.NextStop(_t);
            Advance(0f);
        }

        /// <summary>Esc: to the end at once.</summary>
        public void Skip()
        {
            Finish();
        }

        void Draw()
        {
            IntroTimeline.Frame frame = _timeline.At(_t);
            IntroTimeline.Shot baseShot = _timeline.Shots[frame.ShotIndex];
            Show(_pictureBase, baseShot, frame.Local, 1f);
            float heartbeat = baseShot.Scene.Effect == StoryEffect.Heartbeat ? IntroTimeline.Heartbeat(frame.Local) * (frame.OverIndex >= 0 ? 1f - frame.OverWeight : 1f) : 0f;
            if (frame.OverIndex >= 0)
            {
                IntroTimeline.Shot over = _timeline.Shots[frame.OverIndex];
                Show(_pictureOver, over, frame.OverLocal, frame.OverWeight);
                if (over.Scene.Effect == StoryEffect.Heartbeat)
                {
                    heartbeat += IntroTimeline.Heartbeat(frame.OverLocal) * frame.OverWeight;
                }
            }
            else
            {
                _pictureOver.gameObject.SetActive(false);
            }

            _vignette.gameObject.SetActive(heartbeat > 0f);
            _vignette.color = new Color(1f, 1f, 1f, heartbeat);

            // The lines are the current shot's (the one the clock is in), whichever picture is under it in a crossing.
            IntroTimeline.Shot current = _timeline.Shots[_timeline.ShotAt(_t)];
            float local = _t - current.Start;
            int count = current.Lines.Count;
            float top = 1080f - LinesBottom - (count * LineSize + (count - 1) * LineGap);
            for (int i = 0; i < _lineRows.Length; i++)
            {
                bool shown = i < count;
                _lineRows[i].gameObject.SetActive(shown);
                if (!shown)
                {
                    continue;
                }

                _lineRows[i].anchoredPosition = new Vector2(0f, -(top + i * (LineSize + LineGap)));
                _lineTexts[i].text = Colored(current.Lines[i]);
                _lineShadows[i].text = current.Lines[i];
                _lineGroups[i].alpha = IntroTimeline.LineAlpha(current, i, local);
            }

            _band.color = new Color(1f, 1f, 1f, frame.BandAlpha);
            _hint.alpha = frame.BandAlpha;
            _black.color = new Color(0f, 0f, 0f, frame.Darkness);
        }

        void Show(RawImage target, IntroTimeline.Shot shot, float local, float alpha)
        {
            Sprite sprite = _pictures[shot.Scene.Image];
            Texture texture = sprite.texture;
            IntroTimeline.View view = IntroTimeline.Camera(shot.Scene, local, shot.Duration, texture.width, texture.height);
            target.gameObject.SetActive(true);
            target.texture = texture;
            // UV runs from the bottom left; the view is measured from the top left.
            target.uvRect = new Rect(view.Left, 1f - view.Top - view.Height, view.Width, view.Height);
            target.color = new Color(1f, 1f, 1f, alpha);
        }

        /// <summary>A line as rich text: the words in quotation marks (“…”) in the pale gold of another's voice, the rest in the captain's white.</summary>
        public static string Colored(string line)
        {
            var text = new StringBuilder();
            bool quoted = false;
            foreach (char c in line)
            {
                if (c == '“' && !quoted)
                {
                    text.Append("<color=").Append(UiPalette.StoryGoldHex).Append('>');
                    quoted = true;
                }

                text.Append(c);
                if (c == '”' && quoted)
                {
                    text.Append("</color>");
                    quoted = false;
                }
            }

            if (quoted)
            {
                text.Append("</color>");
            }

            return text.ToString();
        }

        void Finish()
        {
            if (_finished)
            {
                return;
            }

            _finished = true;
            try
            {
                Managers.Setting.MarkIntroSeen();
            }
            catch (SaveWriteException)
            {
                // The next new run shows the full intro again; that is all that is lost.
            }

            if (StoryHandoff.Intro == StoryHandoff.IntroMode.Replay)
            {
                GoTo(ScreenId.Title);
                return;
            }

            StoryHandoff.PendingBanner = _timeline?.Banner;
            StoryHandoff.PendingBannerShort = _timeline != null && _timeline.ShortCut;
            GoToCurrentPhase();
        }

        /// <summary>The heartbeat's dark edges (the mockup's radial vignette): black, clear in the middle, darkening past a quarter of the way out.</summary>
        static Texture2D VignetteTexture()
        {
            const int width = 160;
            const int height = 90;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float d = Mathf.Sqrt(Mathf.Pow((x - 79.5f) / 80f, 2f) + Mathf.Pow((y - 44.5f) / 45f, 2f)) / Mathf.Sqrt(2f);
                    float a = Mathf.Pow(Mathf.Clamp01((d - 0.25f) / 0.55f), 1.6f);
                    pixels[y * width + x] = new Color32(0, 0, 0, (byte)Mathf.RoundToInt(255f * a));
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            return texture;
        }

        /// <summary>The band under the subtitles: clear at its top, as dark as the mockup's (170/255) from 110/230 of the way down.</summary>
        static Texture2D BandTexture()
        {
            const int height = 230;
            var texture = new Texture2D(1, height, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear };
            Color32 shade = UiPalette.StoryShade;
            for (int y = 0; y < height; y++)
            {
                // Row 0 is the texture's bottom.
                float fromTop = height - 1 - y;
                float a = Mathf.Clamp01(fromTop / 110f) * 170f;
                texture.SetPixel(0, y, new Color32(shade.r, shade.g, shade.b, (byte)Mathf.RoundToInt(a)));
            }

            texture.Apply();
            return texture;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            if (_vignette != null && _vignette.texture != null)
            {
                Destroy(_vignette.texture);
            }

            if (_band != null && _band.texture != null)
            {
                Destroy(_band.texture);
            }
        }
    }
}
