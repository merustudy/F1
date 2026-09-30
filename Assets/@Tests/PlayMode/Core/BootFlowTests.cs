using System.Collections;
using F1.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class BootFlowTests
    {
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
