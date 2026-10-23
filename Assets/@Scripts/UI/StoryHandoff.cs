using F1.Data;

namespace F1.UI
{
    /// <summary>
    /// What one story screen leaves for the next (Slice D stage 55): the title tells the intro why it opens, and the intro leaves the lobby the
    /// banner it ends on. Screens are opened by id alone (UIManager), so the hand-over is this small static note, taken once by the screen it is for.
    /// </summary>
    public static class StoryHandoff
    {
        /// <summary>Why the intro opens: after a new run (it goes on to the lobby) or from the title's [인트로 보기] (it goes back to the title, always the full cut).</summary>
        public enum IntroMode
        {
            NewRun,
            Replay,
        }

        public static IntroMode Intro = IntroMode.NewRun;

        /// <summary>The banner the lobby shows when it opens next (the intro's last scene), or null.</summary>
        public static StorySceneData PendingBanner;

        /// <summary>Whether the banner is the short cut's (its time).</summary>
        public static bool PendingBannerShort;

        /// <summary>Takes the pending banner (null when there is none): the lobby shows it once.</summary>
        public static StorySceneData TakeBanner(out bool shortCut)
        {
            StorySceneData banner = PendingBanner;
            shortCut = PendingBannerShort;
            PendingBanner = null;
            return banner;
        }
    }
}
