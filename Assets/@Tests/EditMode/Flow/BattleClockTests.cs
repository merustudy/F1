using System;
using F1.Flow;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class BattleClockTests
    {
        [Test]
        public void Step_ReturnsWholeMilliseconds_AndCarriesTheFraction()
        {
            var clock = new BattleClock();

            int total = 0;
            for (int i = 0; i < 60; i++)
            {
                total += clock.Step(1f / 60f);
            }

            Assert.That(total, Is.InRange(999, 1000), "Sixty frames of 1/60 s are one second of battle.");
        }

        [Test]
        public void Step_AtDoubleSpeed_PassesTwiceTheTime()
        {
            var clock = new BattleClock { SpeedPercent = 200 };

            Assert.AreEqual(200, clock.Step(0.1f));
        }

        [Test]
        public void Step_WhenPaused_PassesNoTime()
        {
            var clock = new BattleClock { Paused = true };

            Assert.AreEqual(0, clock.Step(0.1f));

            clock.Paused = false;
            Assert.AreEqual(100, clock.Step(0.1f));
        }

        [Test]
        public void Step_WhenAFrameIsVeryLong_IsCapped()
        {
            var clock = new BattleClock();

            Assert.AreEqual(BattleClock.MaxStepMs, clock.Step(3f));
        }

        [Test]
        public void Step_WithZeroOrNegativeDelta_PassesNoTime()
        {
            var clock = new BattleClock();

            Assert.AreEqual(0, clock.Step(0f));
            Assert.AreEqual(0, clock.Step(-1f));
        }

        [Test]
        public void SpeedPercent_MustBePositive()
        {
            var clock = new BattleClock();

            Assert.Throws<ArgumentOutOfRangeException>(() => clock.SpeedPercent = 0);
        }
    }
}
