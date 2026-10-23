using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.UI
{
    /// <summary>
    /// The intro's film as numbers (Slice D stage 55): which scene shows at a moment, where its camera looks, how two scenes cross, how dark
    /// the screen is and how far each line has come in. It is the Round 98 motion mockup (ArtPipeline/Archive/98-intro-storyboard/make_motion.py,
    /// the user's choice) written for the screen: the scenes, their times, cameras, effects and lines are data (StorySceneData); the ways they
    /// move are here. No Unity type, so EditMode tests read it as it is. Times are in milliseconds from the film's start.
    /// </summary>
    public sealed class IntroTimeline
    {
        /// <summary>Two scenes cross over a second centred on the cut.</summary>
        public const float CrossMs = 1000f;

        /// <summary>A line comes in over half a second.</summary>
        public const float LineFadeMs = 500f;

        /// <summary>The film comes up out of black.</summary>
        public const float OpenMs = 1200f;

        /// <summary>A scene that ends in black sinks over its last second; the next comes up over 1.6 seconds.</summary>
        public const float BlackOutMs = 1000f;
        public const float BlackInMs = 1600f;

        /// <summary>The film's end goes to black over half a second before the next screen (the lobby's banner comes up out of it).</summary>
        public const float EndMs = 500f;

        /// <summary>The screen's aspect: the camera's view is 16:9.</summary>
        public const float ScreenAspect = 1080f / 1920f;

        public sealed class Shot
        {
            internal Shot(StorySceneData scene, float start, float duration, IReadOnlyList<string> lines)
            {
                Scene = scene;
                Start = start;
                Duration = duration;
                Lines = lines;
                LineStarts = LineStartsOf(lines, duration);
            }

            public StorySceneData Scene { get; }
            public float Start { get; }
            public float Duration { get; }

            /// <summary>The lines this cut shows, in the screen's locale.</summary>
            public IReadOnlyList<string> Lines { get; }

            /// <summary>When each line starts to come in, from the scene's start.</summary>
            public IReadOnlyList<float> LineStarts { get; }

            public float End => Start + Duration;
        }

        /// <summary>What the screen draws at a moment: a scene, maybe another crossing over it, and how dark the whole is.</summary>
        public readonly struct Frame
        {
            public Frame(int shot, float local, int over, float overLocal, float overWeight, float darkness, float bandAlpha)
            {
                ShotIndex = shot;
                Local = local;
                OverIndex = over;
                OverLocal = overLocal;
                OverWeight = overWeight;
                Darkness = darkness;
                BandAlpha = bandAlpha;
            }

            public int ShotIndex { get; }

            /// <summary>Milliseconds into the shot.</summary>
            public float Local { get; }

            /// <summary>The shot crossing over it, or −1.</summary>
            public int OverIndex { get; }
            public float OverLocal { get; }

            /// <summary>0..1: how much of the crossing shot shows.</summary>
            public float OverWeight { get; }

            /// <summary>0..1: black over everything.</summary>
            public float Darkness { get; }

            /// <summary>0..1: the subtitles' band and the hint.</summary>
            public float BandAlpha { get; }
        }

        /// <summary>A view of a picture, in fractions of its width and height from its top left.</summary>
        public readonly struct View
        {
            public View(float left, float top, float width, float height)
            {
                Left = left;
                Top = top;
                Width = width;
                Height = height;
            }

            public float Left { get; }
            public float Top { get; }
            public float Width { get; }
            public float Height { get; }
        }

        readonly List<Shot> _shots;

        IntroTimeline(List<Shot> shots, StorySceneData banner, bool shortCut)
        {
            _shots = shots;
            Banner = banner;
            ShortCut = shortCut;
            Total = shots.Count == 0 ? 0f : shots[shots.Count - 1].End;
        }

        public IReadOnlyList<Shot> Shots => _shots;

        /// <summary>The scene without a picture that stands as a banner over the run's first lobby, or null.</summary>
        public StorySceneData Banner { get; }

        public bool ShortCut { get; }

        /// <summary>The film's length (the banner is not part of it).</summary>
        public float Total { get; }

        /// <summary>
        /// The film of a sequence's scenes (in their order) for a cut: the short cut leaves out the scenes it gives no time and keeps only its lines.
        /// <paramref name="text"/> turns a line into the screen's words (the locale).
        /// </summary>
        public static IntroTimeline Build(IEnumerable<StorySceneData> scenes, bool shortCut, Func<LocalizedText, string> text)
        {
            var shots = new List<Shot>();
            StorySceneData banner = null;
            float start = 0f;
            foreach (StorySceneData scene in scenes)
            {
                int duration = scene.DurationOf(shortCut);
                if (duration <= 0)
                {
                    continue;
                }

                if (scene.IsLobbyBanner)
                {
                    banner = scene;
                    continue;
                }

                var lines = new List<string>();
                foreach (LocalizedText line in scene.LinesOf(shortCut))
                {
                    lines.Add(text(line));
                }

                shots.Add(new Shot(scene, start, duration, lines));
                start += duration;
            }

            return new IntroTimeline(shots, banner, shortCut);
        }

        /// <summary>Each line comes in after the lines before it had time in proportion to their length (0.5 s in, the last 2 s left to read).</summary>
        static List<float> LineStartsOf(IReadOnlyList<string> lines, float duration)
        {
            int total = 0;
            foreach (string line in lines)
            {
                total += line.Length;
            }

            total = Math.Max(total, 1);
            float span = Math.Max(duration - 2000f, 500f);
            var starts = new List<float>();
            int done = 0;
            foreach (string line in lines)
            {
                starts.Add(500f + span * done / total);
                done += line.Length;
            }

            return starts;
        }

        /// <summary>The shot showing at a moment (the last once the film is over).</summary>
        public int ShotAt(float t)
        {
            int index = 0;
            for (int i = 0; i < _shots.Count; i++)
            {
                if (_shots[i].Start <= t)
                {
                    index = i;
                }
            }

            return index;
        }

        public Frame At(float t)
        {
            int i = ShotAt(t);
            Shot shot = _shots[i];
            float local = t - shot.Start;
            float light = 1f;
            int over = -1;
            float overLocal = 0f;
            float weight = 0f;
            int baseIndex = i;
            float baseLocal = local;

            if (shot.Scene.Transition == StoryTransition.Black)
            {
                light *= Clamp01((shot.Duration - local) / BlackOutMs);
            }

            if (i > 0 && _shots[i - 1].Scene.Transition == StoryTransition.Black)
            {
                light *= Clamp01(local / BlackInMs);
            }
            else if (i > 0 && local < CrossMs / 2f)
            {
                // The cut's first half: the scene before still shows under this one coming in.
                baseIndex = i - 1;
                baseLocal = t - _shots[i - 1].Start;
                over = i;
                overLocal = local;
                weight = (local + CrossMs / 2f) / CrossMs;
            }

            if (i + 1 < _shots.Count && shot.Scene.Transition != StoryTransition.Black && shot.Duration - local < CrossMs / 2f)
            {
                over = i + 1;
                overLocal = t - _shots[i + 1].Start;
                weight = (local - (shot.Duration - CrossMs / 2f)) / CrossMs;
            }

            light *= Clamp01(t / OpenMs) * Clamp01((Total - t) / EndMs);
            float band = Clamp01(t / OpenMs) * Clamp01((Total - t) / EndMs);
            return new Frame(baseIndex, baseLocal, over, overLocal, Clamp01(weight), 1f - light, band);
        }

        /// <summary>
        /// Where the camera looks in a shot <paramref name="local"/> ms in: from CameraFrom to CameraTo through the crossings on both sides, a
        /// view of 16:9 at the zoom (100 = the picture's full width), pushed back inside the picture, with the shake of a shaking scene.
        /// </summary>
        public static View Camera(StorySceneData scene, float local, float duration, float pictureWidth, float pictureHeight)
        {
            float u = Clamp01((local + CrossMs / 2f) / (duration + CrossMs));
            StoryCamera from = scene.CameraFrom;
            StoryCamera to = scene.CameraTo;
            float cx = Lerp(from.X, to.X, u) / 1000f;
            float cy = Lerp(from.Y, to.Y, u) / 1000f;
            float zoom = Lerp(from.Zoom, to.Zoom, u) / 100f;
            float w = pictureWidth / zoom;
            float h = w * ScreenAspect;
            if (h > pictureHeight)
            {
                h = pictureHeight;
                w = h / ScreenAspect;
            }

            float left = Clamp(cx * pictureWidth - w / 2f, 0f, pictureWidth - w);
            float top = Clamp(cy * pictureHeight - h / 2f, 0f, pictureHeight - h);
            if (scene.Effect == StoryEffect.Shake)
            {
                float s = local / 1000f;
                float fade = Math.Max(0f, 1f - u * 1.4f);
                left += pictureWidth * 0.0028f * fade * (float)(Math.Sin(s * 2 * Math.PI * 7.3) + 0.5 * Math.Sin(s * 2 * Math.PI * 12.7));
                top += pictureWidth * 0.0020f * fade * (float)Math.Sin(s * 2 * Math.PI * 9.1 + 1.0);
                left = Clamp(left, 0f, pictureWidth - w);
                top = Clamp(top, 0f, pictureHeight - h);
            }

            return new View(left / pictureWidth, top / pictureHeight, w / pictureWidth, h / pictureHeight);
        }

        /// <summary>How dark the heartbeat's edges are (0..1 of the vignette): a double beat every 1.1 seconds.</summary>
        public static float Heartbeat(float local)
        {
            double beat = ((local / 1000.0) % 1.1 + 1.1) % 1.1;
            double pulse = Math.Exp(-Math.Pow(beat / 0.08, 2)) + 0.6 * Math.Exp(-Math.Pow((beat - 0.26) / 0.08, 2));
            return (float)(0.35 + 0.25 * pulse);
        }

        /// <summary>How far a line of a shot has come in (0..1); every line goes out over the scene's last 0.6 seconds.</summary>
        public static float LineAlpha(Shot shot, int line, float local)
        {
            float comeIn = Clamp01((local - shot.LineStarts[line]) / LineFadeMs);
            float goOut = Clamp01((shot.Duration - 150f - local) / 450f);
            return comeIn * goOut;
        }

        /// <summary>
        /// Where a click takes the film (Round 101 판정 3): the moment every line of the shot is in, if some is still coming; else the next shot,
        /// past its crossing; past the last shot, the end (<see cref="Total"/>).
        /// </summary>
        public float NextStop(float t)
        {
            int i = ShotAt(t);
            Shot shot = _shots[i];
            float local = t - shot.Start;
            if (shot.Lines.Count > 0)
            {
                float allIn = shot.LineStarts[shot.Lines.Count - 1] + LineFadeMs;
                if (local < allIn)
                {
                    return shot.Start + allIn;
                }
            }

            return i + 1 < _shots.Count ? _shots[i + 1].Start + CrossMs / 2f : Total;
        }

        /// <summary>The lobby's banner at <paramref name="local"/> ms (Round 98: the countdown at 0.8 s, the guide line at 1.6 s): (cover, countdown, guide), each 0..1.</summary>
        public static (float Cover, float Countdown, float Guide) BannerAt(float local, float duration)
        {
            float fadeOut = Clamp01((duration - local) / EndMs);
            return (1f - Clamp01(local / EndMs), Clamp01((local - 800f) / 600f) * fadeOut, Clamp01((local - 1600f) / 600f) * fadeOut);
        }

        static float Lerp(float a, float b, float u)
        {
            return a + (b - a) * u;
        }

        static float Clamp(float value, float min, float max)
        {
            return value < min ? min : value > max ? max : value;
        }

        static float Clamp01(float value)
        {
            return Clamp(value, 0f, 1f);
        }
    }
}
