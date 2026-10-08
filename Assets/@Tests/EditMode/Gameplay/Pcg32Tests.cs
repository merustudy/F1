using System;
using System.Collections.Generic;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class Pcg32Tests
    {
        [Test]
        public void NextUInt_MatchesReferenceImplementation()
        {
            // Output of the PCG reference demo for pcg32_srandom_r(&rng, 42u, 54u).
            var rng = new Pcg32(42UL, 54UL);

            var expected = new uint[] { 0xa15c02b7, 0x7b47f409, 0xba1d3330, 0x83d2f293, 0xbfa4784b, 0xcbed606e };
            foreach (uint value in expected)
            {
                Assert.AreEqual(value, rng.NextUInt());
            }
        }

        [Test]
        public void SameSeedAndStream_ProduceSameSequence()
        {
            var a = new Pcg32(123UL, RngStream.Battle);
            var b = new Pcg32(123UL, RngStream.Battle);

            for (int i = 0; i < 100; i++)
            {
                Assert.AreEqual(a.NextUInt(), b.NextUInt());
            }
        }

        [Test]
        public void DifferentStreams_ProduceDifferentSequences()
        {
            var battle = new Pcg32(123UL, RngStream.Battle);
            var input = new Pcg32(123UL, RngStream.Input);

            bool anyDifferent = false;
            for (int i = 0; i < 10; i++)
            {
                anyDifferent |= battle.NextUInt() != input.NextUInt();
            }

            Assert.IsTrue(anyDifferent);
        }

        [Test]
        public void Restore_ContinuesExactlyWhereTheGeneratorWas()
        {
            var original = new Pcg32(7UL, RngStream.Map);
            for (int i = 0; i < 5; i++)
            {
                original.NextUInt();
            }

            Pcg32 restored = Pcg32.Restore(original.State, original.Increment);

            for (int i = 0; i < 20; i++)
            {
                Assert.AreEqual(original.NextUInt(), restored.NextUInt());
            }
        }

        [Test]
        public void Restore_WhenIncrementIsEven_Throws()
        {
            Assert.Throws<ArgumentException>(() => Pcg32.Restore(1UL, 2UL));
        }

        [Test]
        public void NextInt_StaysInRangeAndCoversIt()
        {
            var rng = new Pcg32(99UL, RngStream.Loot);
            var seen = new HashSet<int>();

            for (int i = 0; i < 2000; i++)
            {
                int value = rng.NextInt(6);
                Assert.That(value, Is.InRange(0, 5));
                seen.Add(value);
            }

            Assert.AreEqual(6, seen.Count);
        }

        [Test]
        public void NextInt_WithMinAndMax_IsInclusive()
        {
            var rng = new Pcg32(5UL, RngStream.Map);
            var seen = new HashSet<int>();

            for (int i = 0; i < 500; i++)
            {
                seen.Add(rng.NextInt(2, 4));
            }

            CollectionAssert.AreEquivalent(new[] { 2, 3, 4 }, seen);
            Assert.AreEqual(9, rng.NextInt(9, 9));
        }

        [Test]
        public void NextInt_WhenBoundNotPositive_Throws()
        {
            var rng = new Pcg32(1UL, 1UL);

            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => rng.NextInt(5, 4));
        }

        [Test]
        public void Derive_IsDeterministicAndSeparatesPurposeAndIndex()
        {
            ulong seed = 2026UL;

            Assert.AreEqual(SeedDeriver.Derive(seed, "battle", 3), SeedDeriver.Derive(seed, "battle", 3));
            Assert.AreNotEqual(SeedDeriver.Derive(seed, "battle", 3), SeedDeriver.Derive(seed, "battle", 4));
            Assert.AreNotEqual(SeedDeriver.Derive(seed, "battle", 3), SeedDeriver.Derive(seed, "loot", 3));
            Assert.AreNotEqual(SeedDeriver.Derive(seed, "battle", 3), SeedDeriver.Derive(seed + 1, "battle", 3));
        }

        [Test]
        public void Derive_WhenPurposeNull_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SeedDeriver.Derive(1UL, null, 0));
        }
    }
}
