using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using F1.Save;
using NUnit.Framework;
using UnityEngine;

namespace F1.Tests
{
    public sealed class SettingManagerTests
    {
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

        SettingsData ReadSaved()
        {
            return _save.Load<SettingsData>(SettingManager.FileName).Value;
        }

        /// <summary>The test delegates complete synchronously, so the change is finished when this returns.</summary>
        static void Change(SettingManager setting, string localeCode)
        {
            setting.ChangeLocaleAsync(localeCode).GetAwaiter().GetResult();
        }

        [Test]
        public void Load_WhenFirstRunAndSystemLocaleSupported_UsesSystemLocaleAndSaves()
        {
            var setting = new SettingManager(_save);

            setting.Load("en-US");

            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", ReadSaved().LocaleCode);
            Assert.AreEqual(SettingsData.CurrentSchemaVersion, ReadSaved().SchemaVersion);
        }

        [Test]
        public void Load_WhenFirstRunAndSystemLocaleUnsupported_UsesDefault()
        {
            var setting = new SettingManager(_save);

            setting.Load(null);

            Assert.AreEqual(LocalePolicy.DefaultCode, setting.LocaleCode);
            Assert.AreEqual(LocalePolicy.DefaultCode, ReadSaved().LocaleCode);
        }

        [Test]
        public void Load_WhenSaved_SavedLocaleWinsOverSystemLocale()
        {
            _save.Save(SettingManager.FileName, new SettingsData { LocaleCode = "en-US" });
            var setting = new SettingManager(_save);

            setting.Load("ko-KR");

            Assert.AreEqual("en-US", setting.LocaleCode);
        }

        [Test]
        public void Load_WhenSavedLocaleDiffersOnlyByCase_NormalizesAndSaves()
        {
            _save.Save(SettingManager.FileName, new SettingsData { LocaleCode = "EN-us" });
            var setting = new SettingManager(_save);

            setting.Load("ko-KR");

            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", ReadSaved().LocaleCode);
        }

        [TestCase("")]
        [TestCase("ja-JP")]
        public void Load_WhenSavedLocaleInvalid_FallsBackToDefaultAndSaves(string stored)
        {
            _save.Save(SettingManager.FileName, new SettingsData { LocaleCode = stored });
            var setting = new SettingManager(_save);

            setting.Load("en-US");

            Assert.AreEqual(LocalePolicy.DefaultCode, setting.LocaleCode);
            Assert.AreEqual(LocalePolicy.DefaultCode, ReadSaved().LocaleCode);
        }

        [Test]
        public void Load_WhenFileIsCorrupt_RecreatesFromSystemLocale()
        {
            File.WriteAllText(Path.Combine(_root, SettingManager.FileName), "{{{");
            var setting = new SettingManager(_save);

            setting.Load("en-US");

            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", ReadSaved().LocaleCode);
        }

        [Test]
        public void LocaleCode_WhenNotLoaded_Throws()
        {
            var setting = new SettingManager(_save);

            Assert.IsFalse(setting.IsLoaded);
            Assert.Throws<InvalidOperationException>(() => { string unused = setting.LocaleCode; });
        }

        [Test]
        public void ChangeLocale_WhenSupported_AppliesRuntimeThenSavesThenRaisesEvent()
        {
            var applied = new List<string>();
            string savedAtApply = null;
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                savedAtApply = ReadSaved().LocaleCode;
                return Task.CompletedTask;
            });
            setting.Load("ko-KR");
            string raised = null;
            string savedAtEvent = null;
            setting.LocaleChanged += code =>
            {
                raised = code;
                savedAtEvent = ReadSaved().LocaleCode;
            };

            Change(setting, "en-US");

            CollectionAssert.AreEqual(new[] { "en-US" }, applied);
            Assert.AreEqual("ko-KR", savedAtApply, "The runtime locale is applied before the setting is saved.");
            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", raised);
            Assert.AreEqual("en-US", savedAtEvent, "The event is raised only after the save succeeded.");
        }

        [Test]
        public void ChangeLocale_WhenNoRuntimeToFollow_StillSavesAndRaisesEvent()
        {
            var setting = new SettingManager(_save);
            setting.Load("ko-KR");
            string raised = null;
            setting.LocaleChanged += code => raised = code;

            Change(setting, "en-US");

            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", ReadSaved().LocaleCode);
            Assert.AreEqual("en-US", raised);
        }

        [Test]
        public void ChangeLocale_WhenCodeDiffersOnlyByCase_UsesCanonicalCode()
        {
            var applied = new List<string>();
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                return Task.CompletedTask;
            });
            setting.Load("ko-KR");

            Change(setting, "EN-us");

            CollectionAssert.AreEqual(new[] { "en-US" }, applied);
            Assert.AreEqual("en-US", setting.LocaleCode);
        }

        [Test]
        public void ChangeLocale_WhenSameLocale_DoesNothing()
        {
            var applied = new List<string>();
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                return Task.CompletedTask;
            });
            setting.Load("ko-KR");
            bool raised = false;
            setting.LocaleChanged += _ => raised = true;

            Change(setting, "ko-KR");

            Assert.IsEmpty(applied);
            Assert.IsFalse(raised);
        }

        [Test]
        public void ChangeLocale_WhenUnsupported_ThrowsAndKeepsLocale()
        {
            var applied = new List<string>();
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                return Task.CompletedTask;
            });
            setting.Load("ko-KR");

            Assert.Throws<ArgumentException>(() => Change(setting, "ja-JP"));
            Assert.AreEqual("ko-KR", setting.LocaleCode);
            Assert.IsEmpty(applied);
        }

        [Test]
        public void ChangeLocale_WhenSaveFails_PutsRuntimeLocaleBackAndDoesNotRaiseEvent()
        {
            var applied = new List<string>();
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                return Task.CompletedTask;
            });
            setting.Load("ko-KR");
            bool raised = false;
            setting.LocaleChanged += _ => raised = true;

            // Replace the save directory with a file so the next write fails.
            Directory.Delete(_root, true);
            File.WriteAllText(_root, "blocked");
            try
            {
                Assert.Throws<SaveWriteException>(() => Change(setting, "en-US"));
                CollectionAssert.AreEqual(new[] { "en-US", "ko-KR" }, applied);
                Assert.AreEqual("ko-KR", setting.LocaleCode);
                Assert.IsFalse(raised);
            }
            finally
            {
                File.Delete(_root);
            }
        }

        [Test]
        public void ChangeLocale_WhenRuntimeApplyFails_PutsRuntimeLocaleBackAndDoesNotSave()
        {
            var applied = new List<string>();
            var setting = new SettingManager(_save, code =>
            {
                applied.Add(code);
                return code == "en-US"
                    ? Task.FromException(new InvalidOperationException("table load failed"))
                    : Task.CompletedTask;
            });
            setting.Load("ko-KR");
            bool raised = false;
            setting.LocaleChanged += _ => raised = true;

            Assert.Throws<InvalidOperationException>(() => Change(setting, "en-US"));

            CollectionAssert.AreEqual(new[] { "en-US", "ko-KR" }, applied);
            Assert.AreEqual("ko-KR", setting.LocaleCode);
            Assert.AreEqual("ko-KR", ReadSaved().LocaleCode);
            Assert.IsFalse(raised);
        }

        [TestCase(SystemLanguage.Korean, "ko-KR")]
        [TestCase(SystemLanguage.English, "en-US")]
        [TestCase(SystemLanguage.Japanese, null)]
        public void SystemLocaleCode_MapsOnlySupportedLanguages(SystemLanguage language, string expected)
        {
            Assert.AreEqual(expected, SettingManager.SystemLocaleCode(language));
        }
    }
}
