using System;
using System.IO;
using System.Text;
using F1.Save;
using NUnit.Framework;

namespace F1.Tests
{
    public sealed class SaveManagerTests
    {
        const string FileName = "sample.json";

        sealed class SampleDto
        {
            public int SchemaVersion = 1;
            public string Text;
            public int Number;
        }

        string _root;
        SaveManager _save;

        [SetUp]
        public void SetUp()
        {
            _root = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
            _save = new SaveManager(_root);
            _save.Initialize();
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_root))
            {
                Directory.Delete(_root, true);
            }
        }

        string PrimaryPath => Path.Combine(_root, FileName);
        string BackupPath => PrimaryPath + SaveStorage.BackupSuffix;
        string TempPath => PrimaryPath + SaveStorage.TempSuffix;

        [Test]
        public void Load_WhenNothingSaved_ReturnsMissing()
        {
            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName);

            Assert.AreEqual(SaveLoadStatus.Missing, result.Status);
            Assert.IsFalse(result.HasValue);
            Assert.IsNull(result.Value);
        }

        [Test]
        public void Save_ThenLoad_RoundTripsAndLeavesNoTempFile()
        {
            _save.Save(FileName, new SampleDto { Text = "용병", Number = 7 });

            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName);

            Assert.AreEqual(SaveLoadStatus.Loaded, result.Status);
            Assert.AreEqual("용병", result.Value.Text);
            Assert.AreEqual(7, result.Value.Number);
            Assert.IsFalse(File.Exists(TempPath));
            Assert.IsFalse(File.Exists(BackupPath), "The first save has no previous generation.");
        }

        [Test]
        public void Save_WhenFileExists_KeepsPreviousGenerationAsBackup()
        {
            _save.Save(FileName, new SampleDto { Number = 1 });
            byte[] firstBytes = File.ReadAllBytes(PrimaryPath);

            _save.Save(FileName, new SampleDto { Number = 2 });

            CollectionAssert.AreEqual(firstBytes, File.ReadAllBytes(BackupPath));
            Assert.AreEqual(2, _save.Load<SampleDto>(FileName).Value.Number);
        }

        [Test]
        public void Load_WhenPrimaryIsCorrupt_RestoresFromBackupWithoutTouchingBackup()
        {
            _save.Save(FileName, new SampleDto { Number = 1 });
            _save.Save(FileName, new SampleDto { Number = 2 });
            byte[] backupBytes = File.ReadAllBytes(BackupPath);
            File.WriteAllText(PrimaryPath, "{ not json");

            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName);

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(1, result.Value.Number);
            CollectionAssert.AreEqual(backupBytes, File.ReadAllBytes(BackupPath), "A good backup is never overwritten by a repair.");
            CollectionAssert.AreEqual(backupBytes, File.ReadAllBytes(PrimaryPath), "The primary is repaired from the backup.");
            Assert.AreEqual(SaveLoadStatus.Loaded, _save.Load<SampleDto>(FileName).Status);
        }

        [Test]
        public void Load_WhenPrimaryIsMissingButBackupExists_RestoresFromBackup()
        {
            _save.Save(FileName, new SampleDto { Number = 1 });
            _save.Save(FileName, new SampleDto { Number = 2 });
            File.Delete(PrimaryPath);

            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName);

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(1, result.Value.Number);
            Assert.IsTrue(File.Exists(PrimaryPath));
        }

        [Test]
        public void Load_WhenPrimaryAndBackupAreCorrupt_ReturnsCorrupt()
        {
            _save.Save(FileName, new SampleDto { Number = 1 });
            _save.Save(FileName, new SampleDto { Number = 2 });
            File.WriteAllText(PrimaryPath, "garbage");
            File.WriteAllBytes(BackupPath, new byte[] { 0xFF, 0xFE, 0x00 });

            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName);

            Assert.AreEqual(SaveLoadStatus.Corrupt, result.Status);
            Assert.IsNull(result.Value);
        }

        [Test]
        public void Load_WhenValidatorRejectsPrimary_FallsBackToBackup()
        {
            _save.Save(FileName, new SampleDto { SchemaVersion = 1, Number = 1 });
            _save.Save(FileName, new SampleDto { SchemaVersion = 99, Number = 2 });

            SaveLoadResult<SampleDto> result = _save.Load<SampleDto>(FileName, dto => dto.SchemaVersion == 1);

            Assert.AreEqual(SaveLoadStatus.RestoredFromBackup, result.Status);
            Assert.AreEqual(1, result.Value.Number);
        }

        [Test]
        public void Load_NeverUsesTempFile()
        {
            File.WriteAllText(TempPath, Encoding.UTF8.GetString(SaveManager.Serialize(new SampleDto { Number = 5 })));

            Assert.AreEqual(SaveLoadStatus.Missing, _save.Load<SampleDto>(FileName).Status);
        }

        [Test]
        public void Initialize_RemovesStaleTempFiles()
        {
            File.WriteAllText(TempPath, "interrupted write");

            new SaveManager(_root).Initialize();

            Assert.IsFalse(File.Exists(TempPath));
        }

        [Test]
        public void Save_WhenDirectoryCannotBeWritten_ThrowsSaveWriteException()
        {
            // A save root that is a file makes every write fail.
            string blocked = Path.Combine(_root, "blocked");
            File.WriteAllText(blocked, "not a directory");
            var save = new SaveManager(blocked);

            Assert.Throws<SaveWriteException>(() => save.Save(FileName, new SampleDto()));
        }

        [Test]
        public void Delete_RemovesPrimaryAndBackup()
        {
            _save.Save(FileName, new SampleDto { Number = 1 });
            _save.Save(FileName, new SampleDto { Number = 2 });

            _save.Delete(FileName);

            Assert.IsFalse(File.Exists(PrimaryPath));
            Assert.IsFalse(File.Exists(BackupPath));
            Assert.AreEqual(SaveLoadStatus.Missing, _save.Load<SampleDto>(FileName).Status);
        }

        [Test]
        public void Save_WhenFileNameHasDirectory_Throws()
        {
            Assert.Throws<ArgumentException>(() => _save.Save("../escape.json", new SampleDto()));
        }
    }
}
