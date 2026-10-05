namespace F1.Core
{
    /// <summary>
    /// The volumes the title screen offers for music and for effects, in the order a click steps through them: on, low, off
    /// (Docs/Design/12_Sound_Direction.md §3). Low is 30, about half as loud to the ear. The settings keep any value from
    /// <see cref="Off"/> to <see cref="Full"/>.
    /// </summary>
    public static class VolumeLevels
    {
        public const int Full = 100;
        public const int Low = 30;
        public const int Off = 0;

        static readonly int[] Steps = { Full, Low, Off };

        /// <summary>The step a value shows as: the nearest of on, low and off.</summary>
        public static int Nearest(int volume)
        {
            int nearest = Steps[0];
            foreach (int step in Steps)
            {
                if (System.Math.Abs(step - volume) < System.Math.Abs(nearest - volume))
                {
                    nearest = step;
                }
            }

            return nearest;
        }

        /// <summary>The step after the one a value shows as, going round: on, low, off, on.</summary>
        public static int Next(int volume)
        {
            int index = System.Array.IndexOf(Steps, Nearest(volume));
            return Steps[(index + 1) % Steps.Length];
        }

        public static int Clamp(int volume)
        {
            return volume < Off ? Off : volume > Full ? Full : volume;
        }
    }
}
