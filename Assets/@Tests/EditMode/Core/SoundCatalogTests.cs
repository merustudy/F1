using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using F1.Core;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The sounds the screens can ask for, those wired so far, and the volume steps (Docs/Architecture/14_SOUND.md).</summary>
    public sealed class SoundCatalogTests
    {
        const string Rosters = "SoundPipeline/Rosters";

        static HashSet<string> RosterKeys(string file)
        {
            return new HashSet<string>(File.ReadAllLines(Path.Combine(Rosters, file)).Skip(1)
                .Where(line => line.Length > 0)
                .Select(line => line.Substring(0, line.IndexOf(',')))
                .ToList(), StringComparer.Ordinal);
        }

        [TestCase(SoundEffect.Hit, "hit")]
        [TestCase(SoundEffect.MercenaryDeath, "mercenary-death")]
        [TestCase(SoundEffect.DeathsDoor, "deaths-door")]
        [TestCase(SoundEffect.ItemWeapon, "item-weapon")]
        public void AnEffectsKey_IsItsNameInKebabCase(SoundEffect effect, string key)
        {
            Assert.AreEqual(key, SoundCatalog.Key(effect));
            Assert.AreEqual("sound/sfx/" + key, SoundCatalog.Address(effect));
        }

        [Test]
        public void EverySound_IsARowOfTheGenerationRosters_AndHasAValidAddress()
        {
            HashSet<string> effects = RosterKeys("sfx.csv");
            foreach (SoundEffect effect in SoundCatalog.AllEffects())
            {
                Assert.IsTrue(effects.Contains(SoundCatalog.Key(effect)), $"{effect} is a row of Rosters/sfx.csv.");
                Assert.IsTrue(LogicalAddress.IsValid(SoundCatalog.Address(effect)), effect.ToString());
            }

            HashSet<string> tracks = RosterKeys("bgm.csv");
            foreach (MusicTrack track in SoundCatalog.AllTracks())
            {
                Assert.IsTrue(tracks.Contains(SoundCatalog.Key(track)), $"{track} is a row of Rosters/bgm.csv.");
                Assert.IsTrue(LogicalAddress.IsValid(SoundCatalog.Address(track)), track.ToString());
            }

            Assert.AreEqual(effects.Count, SoundCatalog.AllEffects().Count(), "Every roster row is a sound the screens can ask for.");
        }

        [Test]
        public void EverySound_IsWired_Once()
        {
            // All approved (2026-10-05: round 02 by the user, round 03 by the recommendation the user left it to).
            CollectionAssert.AreEquivalent(SoundCatalog.AllEffects(), SoundCatalog.Effects);
            CollectionAssert.AreEquivalent(SoundCatalog.AllTracks(), SoundCatalog.Tracks);
            Assert.IsTrue(SoundCatalog.Has(SoundEffect.Heal));
            Assert.IsTrue(SoundCatalog.Has(MusicTrack.Lobby));
        }

        [TestCase(100, 30)]
        [TestCase(30, 0)]
        [TestCase(0, 100)]
        [TestCase(55, 0)]
        [TestCase(80, 30)]
        public void AVolumeButton_StepsOnLowOff_FromTheNearestStep(int volume, int next)
        {
            Assert.AreEqual(next, VolumeLevels.Next(volume));
        }

        [TestCase(-5, 0)]
        [TestCase(150, 100)]
        [TestCase(42, 42)]
        public void Clamp_KeepsAVolumeIn0To100(int volume, int clamped)
        {
            Assert.AreEqual(clamped, VolumeLevels.Clamp(volume));
        }
    }
}
