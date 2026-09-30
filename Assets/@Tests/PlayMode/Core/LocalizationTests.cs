using System.Collections;
using F1.Core;
using F1.Save;
using F1.UI;
using NUnit.Framework;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class LocalizationTests
    {
        string _saveRoot;

        [SetUp]
        public void SetUp()
        {
            _saveRoot = BootTestUtil.UseTemporarySaveRoot();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return BootTestUtil.ShutdownApp();
        }

        IEnumerator BootWithLocale(string localeCode)
        {
            BootTestUtil.WriteSettings(_saveRoot, localeCode);
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();
            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
        }

        [UnityTest]
        public IEnumerator Boot_WhenSettingsSayKorean_UiTextIsKorean()
        {
            yield return BootWithLocale("ko-KR");

            Assert.AreEqual("ko-KR", Managers.Setting.LocaleCode);
            Assert.AreEqual("새 런", UiStrings.Get(UiKeys.Title.NewRun));
        }

        [UnityTest]
        public IEnumerator Boot_WhenSettingsSayEnglish_UiTextIsEnglish()
        {
            yield return BootWithLocale("en-US");

            Assert.AreEqual("en-US", Managers.Setting.LocaleCode);
            Assert.AreEqual("New Run", UiStrings.Get(UiKeys.Title.NewRun));
        }

        [UnityTest]
        public IEnumerator ChangeLocale_SwitchesUiTextAndIsSaved()
        {
            yield return BootWithLocale("ko-KR");
            string raised = null;
            Managers.Setting.LocaleChanged += code => raised = code;

            yield return TaskUtil.Await(Managers.Setting.ChangeLocaleAsync("en-US"));

            Assert.AreEqual("en-US", raised);
            Assert.AreEqual("New Run", UiStrings.Get(UiKeys.Title.NewRun));
            var saved = new SaveManager(_saveRoot).Load<SettingsData>(SettingManager.FileName);
            Assert.AreEqual("en-US", saved.Value.LocaleCode);

            yield return TaskUtil.Await(Managers.Setting.ChangeLocaleAsync("ko-KR"));

            Assert.AreEqual("새 런", UiStrings.Get(UiKeys.Title.NewRun));
        }

        [UnityTest]
        public IEnumerator Get_WithArguments_FillsThePlaceholders()
        {
            yield return BootWithLocale("ko-KR");

            Assert.AreEqual("언어: 한국어", UiStrings.Get(UiKeys.Title.Language, UiStrings.Get(UiKeys.Title.LanguageName)));
        }

        [UnityTest]
        public IEnumerator Get_WhenKeyDoesNotExist_ShowsTheMissingMarker()
        {
            yield return BootWithLocale("en-US");

            Assert.AreEqual("[Missing:Nope.Missing]", UiStrings.Get("Nope.Missing"));
        }
    }
}
