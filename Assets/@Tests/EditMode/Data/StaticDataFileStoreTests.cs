using System;
using System.Collections.Generic;
using System.IO;
using F1.Data;
using F1.Editor.Data;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class StaticDataFileStoreTests
    {
        const string ValidJobs = "Id,Name.ko-KR,Name.en-US\nknight,기사,Knight\n";

        string _root;
        StaticDataFileStore _store;

        string SourcePath => Path.Combine(_root, StaticDataFiles.SourceDirectory, StaticDataFiles.Job.SourceFileName);
        string GeneratedPath => Path.Combine(_root, StaticDataFiles.GeneratedDirectory, StaticDataFiles.Job.GeneratedFileName);

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path.GetDirectoryName(SourcePath));
            File.WriteAllText(SourcePath, ValidJobs);
            _store = new StaticDataFileStore(_root);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        TransformResult Transform()
        {
            return StaticDataTransformer.Transform(_store.ReadSource);
        }

        [Test]
        public void WriteGenerated_WhenNothingExists_WritesEveryFileWithoutBomOrTempFiles()
        {
            List<string> changed = _store.WriteGenerated(Transform());

            CollectionAssert.AreEqual(new[] { StaticDataFiles.Job.GeneratedFileName }, changed);
            byte[] bytes = File.ReadAllBytes(GeneratedPath);
            Assert.AreEqual((byte)'{', bytes[0], "No byte order mark.");
            Assert.IsEmpty(Directory.GetFiles(Path.GetDirectoryName(GeneratedPath), "*.tmp"));
        }

        [Test]
        public void WriteGenerated_WhenUpToDate_DoesNotTouchFiles()
        {
            _store.WriteGenerated(Transform());
            DateTime stamp = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            File.SetLastWriteTimeUtc(GeneratedPath, stamp);

            List<string> changed = _store.WriteGenerated(Transform());

            Assert.IsEmpty(changed);
            Assert.AreEqual(stamp, File.GetLastWriteTimeUtc(GeneratedPath));
        }

        [Test]
        public void FindStale_WhenSourceChangedOrGeneratedEdited_ReportsFile()
        {
            _store.WriteGenerated(Transform());
            Assert.IsEmpty(_store.FindStale(Transform()));

            File.WriteAllText(SourcePath, ValidJobs + "bishop,주교,Bishop\n");
            CollectionAssert.AreEqual(new[] { StaticDataFiles.Job.GeneratedFileName }, _store.FindStale(Transform()));

            _store.WriteGenerated(Transform());
            File.AppendAllText(GeneratedPath, " ");
            CollectionAssert.AreEqual(new[] { StaticDataFiles.Job.GeneratedFileName }, _store.FindStale(Transform()));
        }

        [Test]
        public void Transform_WhenSourceBecomesInvalid_LeavesGeneratedUntouched()
        {
            _store.WriteGenerated(Transform());
            byte[] before = File.ReadAllBytes(GeneratedPath);

            File.WriteAllText(SourcePath, ValidJobs + "Broken Id,깨짐,Broken\n");

            Assert.Throws<DataTransformException>(() => _store.WriteGenerated(Transform()));
            CollectionAssert.AreEqual(before, File.ReadAllBytes(GeneratedPath));
        }

        [Test]
        public void ReadSource_WhenFileMissing_ReturnsNull()
        {
            Assert.IsNull(_store.ReadSource("NoSuchData.csv"));
            Assert.IsNull(_store.ReadGenerated("NoSuchData.json"));
        }
    }
}
