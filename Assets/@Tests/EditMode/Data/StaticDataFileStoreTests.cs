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
        string _root;
        StaticDataFileStore _store;

        string SourceDirectory => Path.Combine(_root, StaticDataFiles.SourceDirectory);
        string SourcePath => Path.Combine(SourceDirectory, StaticDataFiles.Potion.SourceFileName);
        string GeneratedPath => Path.Combine(_root, StaticDataFiles.GeneratedDirectory, StaticDataFiles.Potion.GeneratedFileName);

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(SourceDirectory);
            foreach (KeyValuePair<string, string> source in TestCsv.ValidSources())
            {
                File.WriteAllText(Path.Combine(SourceDirectory, source.Key), source.Value);
            }

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

            Assert.AreEqual(StaticDataFiles.All.Count, changed.Count);
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

            File.WriteAllText(SourcePath, TestCsv.Potions + "salve,연고,Salve,Shield,20,1,,8\n");
            CollectionAssert.AreEqual(new[] { StaticDataFiles.Potion.GeneratedFileName }, _store.FindStale(Transform()), "Only the changed definition is stale.");

            CollectionAssert.AreEqual(new[] { StaticDataFiles.Potion.GeneratedFileName }, _store.WriteGenerated(Transform()));
            File.AppendAllText(GeneratedPath, " ");
            CollectionAssert.AreEqual(new[] { StaticDataFiles.Potion.GeneratedFileName }, _store.FindStale(Transform()));
        }

        [Test]
        public void Transform_WhenSourceBecomesInvalid_LeavesGeneratedUntouched()
        {
            _store.WriteGenerated(Transform());
            byte[] before = File.ReadAllBytes(GeneratedPath);

            File.WriteAllText(SourcePath, TestCsv.Potions + "Broken Id,깨짐,Broken,Heal,1,1,,8\n");

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
