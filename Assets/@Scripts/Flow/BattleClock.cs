using System;

namespace F1.Flow
{
    /// <summary>
    /// Turns frame time into battle milliseconds. Speed and pause only decide how fast the battle
    /// is shown; the outcome depends on the setup and the recorded inputs alone.
    /// </summary>
    public sealed class BattleClock
    {
        /// <summary>A frame longer than this advances the battle by this much only, so a hitch never skips seconds of battle.</summary>
        public const int MaxStepMs = 250;

        double _carryMs;
        int _speedPercent = 100;
        int _slowPercent = 100;

        public bool Paused { get; set; }

        /// <summary>100 is real time.</summary>
        public int SpeedPercent
        {
            get => _speedPercent;
            set
            {
                if (value < 1)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "Speed must be positive.");
                }

                _speedPercent = value;
            }
        }

        /// <summary>
        /// How much of the chosen speed the battle keeps for a moment: 100 is all of it. The screen slows the battle while it
        /// shows a kill moment (Docs/Design/10 §5) without changing the speed the player chose.
        /// </summary>
        public int SlowPercent
        {
            get => _slowPercent;
            set
            {
                if (value < 1 || value > 100)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, "A slowing keeps between 1% and all of the speed.");
                }

                _slowPercent = value;
            }
        }

        /// <summary>Whole battle milliseconds that passed during a frame. The fraction is carried to the next frame.</summary>
        public int Step(float deltaSeconds)
        {
            if (Paused || deltaSeconds <= 0f)
            {
                return 0;
            }

            double frameMs = Math.Min(deltaSeconds * 1000.0, MaxStepMs);
            _carryMs += frameMs * _speedPercent / 100.0 * _slowPercent / 100.0;
            int whole = (int)_carryMs;
            _carryMs -= whole;
            return whole;
        }
    }
}
