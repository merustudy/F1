using System.Linq;
using F1.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The intro's film (Slice D stage 55, the Round 98 motion mockup): the cuts, the crossings and the dip to black, the camera inside the picture,
    /// the lines in proportion to their length, where a click takes it, and the lobby's banner.
    /// </summary>
    public sealed class IntroTimelineTests
    {
        static IntroTimeline Build(bool shortCut)
        {
            StaticData data = new StaticData(TestData.WithStory(TestData.Parts()));
            return IntroTimeline.Build(data.ScenesOf("tale"), shortCut, text => text.Resolve("en-US"));
        }

        [Test]
        public void Build_TheFullCut_HasThePictureScenesInOrder_AndTheBannerApart()
        {
            IntroTimeline film = Build(false);

            CollectionAssert.AreEqual(new[] { "tale_one", "tale_two" }, film.Shots.Select(s => s.Scene.Id));
            Assert.AreEqual(7000f, film.Total, "4 s + 3 s; the banner is not part of the film.");
            Assert.AreEqual("tale_end", film.Banner.Id);
            CollectionAssert.AreEqual(new[] { "one", "“two”" }, film.Shots[0].Lines);
        }

        [Test]
        public void Build_TheShortCut_LeavesOutTheSceneWithoutTime_AndKeepsItsLines()
        {
            IntroTimeline film = Build(true);

            Assert.AreEqual(1, film.Shots.Count);
            Assert.AreEqual(2000f, film.Total);
            CollectionAssert.AreEqual(new[] { "“two”" }, film.Shots[0].Lines);
            Assert.IsTrue(film.ShortCut);
        }

        [Test]
        public void Lines_ComeInAfterTheLinesBefore_InProportionToTheirLength()
        {
            IntroTimeline.Shot shot = Build(false).Shots[0];

            // 4 s: 0.5 s in, then the 2 s before the end are left to read; "one" is 3 of the 8 letters.
            Assert.AreEqual(500f, shot.LineStarts[0]);
            Assert.AreEqual(500f + 2000f * 3f / 8f, shot.LineStarts[1], 0.01f);
            Assert.AreEqual(0f, IntroTimeline.LineAlpha(shot, 1, 1000f));
            Assert.AreEqual(1f, IntroTimeline.LineAlpha(shot, 0, 1500f));
            Assert.AreEqual(0f, IntroTimeline.LineAlpha(shot, 0, 3900f), "Every line goes out at the scene's end.");
        }

        [Test]
        public void At_TheFilmComesUpOutOfBlack_TheSecondSceneSinksToBlack_AndTheEndIsBlack()
        {
            IntroTimeline film = Build(false);

            Assert.AreEqual(1f, film.At(0f).Darkness, 0.001f);
            Assert.AreEqual(0f, film.At(2000f).Darkness, 0.001f);
            Assert.AreEqual(-1, film.At(2000f).OverIndex);
            Assert.Greater(film.At(6800f).Darkness, 0.5f, "A Black scene sinks over its last second, and the film's end too.");
        }

        [Test]
        public void At_TwoScenesCrossOverASecondAroundTheCut()
        {
            IntroTimeline film = Build(false);

            IntroTimeline.Frame before = film.At(3750f);
            Assert.AreEqual(0, before.ShotIndex);
            Assert.AreEqual(1, before.OverIndex);
            Assert.AreEqual(0.25f, before.OverWeight, 0.001f);

            IntroTimeline.Frame after = film.At(4250f);
            Assert.AreEqual(0, after.ShotIndex, "The scene before is still under the one coming in.");
            Assert.AreEqual(1, after.OverIndex);
            Assert.AreEqual(0.75f, after.OverWeight, 0.001f);
            Assert.AreEqual(1, film.ShotAt(4250f), "The clock is in the second scene: its lines show.");
        }

        [Test]
        public void Camera_MovesFromItsStartToItsEnd_InsideThePicture()
        {
            StorySceneData scene = Build(false).Shots[0].Scene;

            IntroTimeline.View start = IntroTimeline.Camera(scene, -500f, 4000f, 2304f, 1536f);
            Assert.AreEqual(1f, start.Width, 0.001f, "Zoom 100 is the full width.");
            Assert.AreEqual(2304f * 1080f / 1920f / 1536f, start.Height, 0.001f);
            Assert.GreaterOrEqual(start.Top, 0f);

            IntroTimeline.View end = IntroTimeline.Camera(scene, 4500f, 4000f, 2304f, 1536f);
            Assert.AreEqual(1f / 1.2f, end.Width, 0.001f);
            Assert.AreEqual(0f, end.Left, 0.001f, "Centred at 0.4 it would leave the picture: it is pushed back inside.");
            Assert.LessOrEqual(end.Top + end.Height, 1f);
        }

        [Test]
        public void Heartbeat_BeatsTwiceEvery1100Ms()
        {
            Assert.AreEqual(0.6f, IntroTimeline.Heartbeat(0f), 0.01f);
            Assert.AreEqual(IntroTimeline.Heartbeat(260f), IntroTimeline.Heartbeat(1360f), 0.001f);
            Assert.AreEqual(0.35f, IntroTimeline.Heartbeat(700f), 0.01f);
        }

        [Test]
        public void NextStop_BringsInEveryLine_ThenTheNextScene_ThenTheEnd()
        {
            IntroTimeline film = Build(false);
            IntroTimeline.Shot first = film.Shots[0];

            float allIn = first.LineStarts[1] + IntroTimeline.LineFadeMs;
            Assert.AreEqual(allIn, film.NextStop(600f), 0.01f);
            Assert.AreEqual(4000f + IntroTimeline.CrossMs / 2f, film.NextStop(allIn), 0.01f);
            Assert.AreEqual(film.Total, film.NextStop(6900f));
        }

        [Test]
        public void Banner_ComesUpOutOfBlack_CountdownAt800_GuideAt1600_AndGoes()
        {
            (float cover, float countdown, float guide) = IntroTimeline.BannerAt(0f, 5000f);
            Assert.AreEqual(1f, cover);
            Assert.AreEqual(0f, countdown);

            (cover, countdown, guide) = IntroTimeline.BannerAt(1400f, 5000f);
            Assert.AreEqual(0f, cover);
            Assert.AreEqual(1f, countdown, 0.001f);
            Assert.AreEqual(0f, guide);

            (_, countdown, guide) = IntroTimeline.BannerAt(4750f, 5000f);
            Assert.AreEqual(0.5f, guide, 0.001f);
        }

        [Test]
        public void Colored_PutsTheWordsInQuotationMarksInGold()
        {
            Assert.AreEqual("The king said <color=#D6BE82>“go”</color>.", IntroScreen.Colored("The king said “go”."));
            Assert.AreEqual("no quote", IntroScreen.Colored("no quote"));
        }
    }
}
