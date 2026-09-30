using System;
using System.IO;
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
        public void SetLocale_WhenSupported_SavesThenRaisesEvent()
        {
            var setting = new SettingManager(_save);
            setting.Load("ko-KR");
            string raised = null;
            string savedAtEvent = null;
            setting.LocaleChanged += code =>
            {
                raised = code;
                savedAtEvent = ReadSaved().LocaleCode;
            };

            setting.SetLocale("en-US");

            Assert.AreEqual("en-US", setting.LocaleCode);
            Assert.AreEqual("en-US", raised);
            Assert.AreEqual("en-US", savedAtEvent, "The event is raised only after the save succeeded.");
        }

        [Test]
        public void SetLocale_WhenSameLocale_DoesNotRaiseEvent()
        {
            var setting = new SettingManager(_save);
            setting.Load("ko-KR");
            bool raised = false;
            setting.LocaleChanged += _ => raised = true;

            setting.SetLocale("ko-KR");

            Assert.IsFalse(raised);
        }

        [Test]
        public void SetLocale_WhenUnsupported_ThrowsAndKeepsLocale()
        {
            var setting = new SettingManager(_save);
            setting.Load("ko-KR");

            Assert.Throws<ArgumentException>(() => setting.SetLocale("ja-JP"));
            Assert.AreEqual("ko-KR", setting.LocaleCode);
        }

        [Test]
        public void SetLocale_WhenSaveFails_KeepsLocaleAndDoesNotRaiseEvent()
        {
            var setting = new SettingManager(_save);
            setting.Load("ko-KR");
            bool raised = false;
            setting.LocaleChanged += _ => raised = true;

            // Replace the save directory with a file so the next write fails.
            Directory.Delete(_root, true);
            File.WriteAllText(_root, "blocked");
            try
            {
                Assert.Throws<SaveWriteException>(() => setting.SetLocale("en-US"));
                Assert.AreEqual("ko-KR", setting.LocaleCode);
                Assert.IsFalse(raised);
            }
            finally
            {
                File.Delete(_root);
            }
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
