using System;
using System.IO;
using F1.Core;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The play log of stage 16 (Docs/Architecture/10_TESTING_VALIDATION.md "플레이 기록").</summary>
    public sealed class PlayLogTests
    {
        string _root;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        [Test]
        public void ALine_IsTheTimeToTheMillisecond_TheScreen_AndTheState_SeparatedByTabs()
        {
            string line = PlayLog.FormatLine(new DateTime(2026, 10, 7, 21, 3, 4, 120), "Battle", "phase=Battle day=3 floor=6/16");

            Assert.AreEqual("2026-10-07 21:03:04.120\tBattle\tphase=Battle day=3 floor=6/16", line);
        }

        [Test]
        public void Append_MakesTheFolder_NamesTheFileAfterTheFirstLine_AndAddsEachLine()
        {
            var clock = new DateTime(2026, 10, 7, 21, 3, 4);
            var log = new PlayLog(Path.Combine(_root, PlayLog.FolderName), () => clock);
            Assert.IsNull(log.FilePath, "No file before a line.");

            Assert.IsTrue(log.Append("Boot", "run=Missing"));
            clock = clock.AddSeconds(5.5);
            Assert.IsTrue(log.Append("Title", "phase=NoRun"));

            Assert.AreEqual(Path.Combine(_root, PlayLog.FolderName, "play-20261007-210304.log"), log.FilePath);
            CollectionAssert.AreEqual(
                new[] { "2026-10-07 21:03:04.000\tBoot\trun=Missing", "2026-10-07 21:03:09.500\tTitle\tphase=NoRun" },
                File.ReadAllLines(log.FilePath));
            Assert.IsFalse(log.Failed);
        }

        [Test]
        public void Append_WhenTheFolderCannotBeMade_FailsQuietly_AndStopsTrying()
        {
            // A file stands where the folder's parent should be.
            Directory.CreateDirectory(_root);
            string file = Path.Combine(_root, "not-a-folder");
            File.WriteAllText(file, "x");
            var log = new PlayLog(Path.Combine(file, PlayLog.FolderName));

            Assert.IsFalse(log.Append("Boot", "run=Missing"));
            Assert.IsTrue(log.Failed);
            Assert.IsFalse(log.Append("Title", "phase=NoRun"), "After a failure nothing more is tried.");
            Assert.IsNull(log.FilePath);
        }
    }
}
