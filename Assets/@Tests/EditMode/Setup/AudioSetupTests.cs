using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Editor.Setup;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the shipped sounds against the catalog and the import policy (Docs/Architecture/14_SOUND.md).</summary>
    public sealed class AudioSetupTests
    {
        [Test]
        public void FindProblems_ForShippedSounds_IsEmpty()
        {
            List<string> problems = AudioSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [Test]
        public void AssetPath_IsTheKeyUnderSfxOrBgm()
        {
            Assert.AreEqual("Assets/@Audio/Sfx/mercenary-death.wav", AudioSetup.AssetPath(SoundEffect.MercenaryDeath));
            Assert.AreEqual("Assets/@Audio/Bgm/dungeon.wav", AudioSetup.AssetPath(MusicTrack.Dungeon));
        }

        [Test]
        public void Entries_AreTheWiredSounds_InTheAudioGroupAndTheAppScope()
        {
            List<AddressEntry> entries = AudioSetup.Entries();

            CollectionAssert.AreEquivalent(
                SoundCatalog.Effects.Select(SoundCatalog.Address).Concat(SoundCatalog.Tracks.Select(SoundCatalog.Address)),
                entries.Select(e => e.Address));
            foreach (AddressEntry entry in entries)
            {
                Assert.AreEqual(AudioSetup.AudioGroup, entry.Group, entry.Address);
                Assert.AreEqual(ResourceScope.App, entry.Scope, entry.Address);
            }
        }
    }
}
