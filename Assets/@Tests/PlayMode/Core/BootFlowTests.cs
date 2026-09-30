using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using F1.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class BootFlowTests
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

        [UnityTest]
        public IEnumerator Boot_WhenStarted_ReachesInitializedInMainScene()
        {
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);

            yield return BootTestUtil.WaitForBootToFinish();

            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.IsNull(AppRoot.Current.FailedStep);
            Assert.AreEqual(SceneManagerEx.MainSceneName, SceneManager.GetActiveScene().name);
            Assert.IsTrue(Managers.IsConfigured);
            Assert.AreEqual(_saveRoot, Managers.Save.RootPath, "Tests never use the real save directory.");
            Assert.IsTrue(Managers.Setting.IsLoaded);
            Assert.IsTrue(Managers.Resource.IsScopeOpen(ResourceScope.App));

            MainSceneRoot main = Object.FindFirstObjectByType<MainSceneRoot>();
            Assert.IsNotNull(main);
            Assert.IsTrue(main.IsBound);
            Assert.IsNotNull(main.UiRoot);
            Assert.IsFalse(AppRoot.Current.View.gameObject.activeSelf, "The boot view is hidden after a successful boot.");
        }

        [UnityTest]
        public IEnumerator Main_WhenPlayedWithoutBoot_GoesThroughBoot()
        {
            SceneManager.LoadScene(SceneManagerEx.MainSceneName);

            yield return BootTestUtil.WaitForBootToFinish();

            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.AreEqual(SceneManagerEx.MainSceneName, SceneManager.GetActiveScene().name);
        }

        [UnityTest]
        public IEnumerator Boot_WhenSaveStorageFails_StopsAtThatStepAndShowsErrorCode()
        {
            // A save root that is a file cannot be created as a directory.
            Directory.CreateDirectory(Path.GetDirectoryName(_saveRoot));
            File.WriteAllText(_saveRoot, "blocked");
            LogAssert.Expect(LogType.Exception, new Regex("IOException"));

            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();

            Assert.AreEqual(InitializationState.Failed, AppRoot.Current.State);
            Assert.AreEqual(BootStep.InitializeSaveStorage.ErrorCode, AppRoot.Current.FailedStep.Value.ErrorCode);
            Assert.AreEqual(SceneManagerEx.BootSceneName, SceneManager.GetActiveScene().name, "A failed boot never enters Main.");
            Assert.IsTrue(AppRoot.Current.View.gameObject.activeSelf);
            StringAssert.Contains(BootStep.InitializeSaveStorage.ErrorCode, AppRoot.Current.View.ErrorText);

            File.Delete(_saveRoot);
        }

        [UnityTest]
        public IEnumerator Boot_WhenLoadedTwice_KeepsSingleAppRoot()
        {
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();
            AppRoot first = AppRoot.Current;

            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return null;
            yield return null;

            Assert.AreSame(first, AppRoot.Current);
            Assert.AreEqual(1, Object.FindObjectsByType<AppRoot>(FindObjectsSortMode.None).Length);
        }
    }
}
