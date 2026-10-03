using System;
using System.Collections.Generic;
using System.Linq;
using F1.Core;
using F1.Editor.Setup;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Audits the shipped art against the static data and the import policy.</summary>
    public sealed class ArtSetupTests
    {
        [Test]
        public void FindProblems_ForShippedArt_IsEmpty()
        {
            List<string> problems = ArtSetup.FindProblems();

            Assert.IsEmpty(problems, string.Join("\n", problems));
        }

        [TestCase("unit/job/knight", "Assets/@Art/Unit/Job/knight.png")]
        [TestCase("unit/enemy/goblin-raider", "Assets/@Art/Unit/Enemy/goblin_raider.png")]
        [TestCase("face/job/knight", "Assets/@Art/Face/Job/knight.png")]
        [TestCase("face/enemy/goblin-raider", "Assets/@Art/Face/Enemy/goblin_raider.png")]
        [TestCase("background/dungeon/abandoned-mine", "Assets/@Art/Background/Dungeon/abandoned_mine.png")]
        [TestCase("item/herb-pouch", "Assets/@Art/Item/herb_pouch.png")]
        [TestCase("item/small-icon/herb-pouch", "Assets/@Art/Item/SmallIcon/herb_pouch.png")]
        public void AssetPath_NamesTheFoldersAfterTheSegments_AndTheFileAfterTheDataId(string address, string expected)
        {
            Assert.AreEqual(expected, ArtSetup.AssetPath(address));
        }

        [TestCase("unit/job/Knight")]
        [TestCase("knight")]
        [TestCase("")]
        public void AssetPath_WhenTheAddressIsNotValid_Throws(string address)
        {
            Assert.Throws<ArgumentException>(() => ArtSetup.AssetPath(address));
        }

        [Test]
        public void Entries_AreTheArtOfTheData_InTheArtGroupAndTheExpeditionScope()
        {
            List<AddressEntry> entries = ArtSetup.Entries();

            Assert.IsNotEmpty(entries);
            Assert.AreEqual(entries.Count, entries.Select(e => e.Address).Distinct().Count(), "A picture is listed once.");
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("unit/", StringComparison.Ordinal)), "The figures of the units.");
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("face/", StringComparison.Ordinal)), "The faces cut out of the figures.");
            foreach (AddressEntry figure in entries.Where(e => e.Address.StartsWith("unit/", StringComparison.Ordinal)))
            {
                Assert.IsTrue(entries.Any(e => e.Address == F1.Data.ArtAddress.FaceOf(figure.Address)), $"{figure.Address} has its face.");
            }
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("background/dungeon/", StringComparison.Ordinal)), "The backgrounds of the dungeons.");
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("item/", StringComparison.Ordinal)), "The icons of the items.");
            foreach (AddressEntry entry in entries)
            {
                Assert.AreEqual(ArtSetup.ArtGroup, entry.Group, entry.Address);
                Assert.AreEqual(ResourceScope.Expedition, entry.Scope, entry.Address);
                Assert.AreEqual(ArtSetup.AssetPath(entry.Address), entry.AssetPath, entry.Address);
            }
        }
    }
}
