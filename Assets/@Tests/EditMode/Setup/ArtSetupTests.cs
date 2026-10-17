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
        [TestCase("pose/job/knight-attack", "Assets/@Art/Pose/Job/knight_attack.png")]
        [TestCase("pose/enemy/goblin-raider-hit", "Assets/@Art/Pose/Enemy/goblin_raider_hit.png")]
        [TestCase("background/dungeon/abandoned-mine", "Assets/@Art/Background/Dungeon/abandoned_mine.png")]
        [TestCase("background/expedition/map", "Assets/@Art/Background/Expedition/map.png")]
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
        public void Entries_AreTheArtOfTheData_InTheArtGroup_InTheExpeditionScope_ButTheMapOfTheLandsAndTheTownInTheLobbys()
        {
            List<AddressEntry> entries = ArtSetup.Entries();

            Assert.IsNotEmpty(entries);
            Assert.AreEqual(entries.Count, entries.Select(e => e.Address).Distinct().Count(), "A picture is listed once.");
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("unit/", StringComparison.Ordinal)), "The figures of the units.");

            // A figure comes with its attack and hit poses, a mercenary's and a monster's (Docs/Design/10 §5).
            foreach (AddressEntry figure in entries.Where(e => e.Address.StartsWith("unit/", StringComparison.Ordinal)))
            {
                Assert.IsTrue(entries.Any(e => e.Address == F1.Data.ArtAddress.PoseOf(figure.Address, F1.Data.ArtAddress.Attack)), $"{figure.Address} has its attack pose.");
                Assert.IsTrue(entries.Any(e => e.Address == F1.Data.ArtAddress.PoseOf(figure.Address, F1.Data.ArtAddress.Hit)), $"{figure.Address} has its hit pose.");
            }
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("background/dungeon/", StringComparison.Ordinal)), "The backgrounds of the dungeons.");
            Assert.IsTrue(entries.Any(e => e.Address.StartsWith("item/", StringComparison.Ordinal)), "The icons of the items.");
            Assert.IsTrue(entries.Any(e => e.Address == "item/gladius-glow"), "The glow of the unique item (stage 23), with its icon.");
            Assert.IsTrue(entries.Any(e => e.Address == F1.Data.ArtAddress.Building(F1.Data.BuildingId.Gate, 3)), "The buildings at their levels (stage 30).");
            Assert.IsTrue(entries.Any(e => e.Address == F1.Data.ArtAddress.Scaffold), "The scaffold (stage 30).");
            Assert.IsTrue(entries.Any(e => e.Address == "bag/leather-pouch"), "The bags' pictures (round 79), in the expedition scope like an item's icon.");
            foreach (AddressEntry entry in entries)
            {
                Assert.AreEqual(ArtSetup.ArtGroup, entry.Group, entry.Address);
                // The map of the lands is shown by the expedition screen, which is the lobby's (round 71 E2), the town by the lobby itself (stage 26),
                // and the town's buildings and the scaffold stand on it (round 78, stage 30).
                bool lobbys = entry.Address == F1.Data.ArtAddress.ExpeditionMap || entry.Address == F1.Data.ArtAddress.LobbyTown
                    || entry.Address.StartsWith("building/", StringComparison.Ordinal);
                ResourceScope scope = lobbys ? ResourceScope.Lobby : ResourceScope.Expedition;
                Assert.AreEqual(scope, entry.Scope, entry.Address);
                Assert.AreEqual(ArtSetup.AssetPath(entry.Address), entry.AssetPath, entry.Address);
            }
        }
    }
}
