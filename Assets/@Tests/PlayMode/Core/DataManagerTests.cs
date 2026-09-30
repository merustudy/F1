using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class DataManagerTests
    {
        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return BootTestUtil.ShutdownApp();
        }

        [UnityTest]
        public IEnumerator Boot_LoadsStaticDataThroughAddressables()
        {
            BootTestUtil.UseTemporarySaveRoot();
            SceneManager.LoadScene(SceneManagerEx.BootSceneName);
            yield return BootTestUtil.WaitForBootToFinish();

            Assert.AreEqual(InitializationState.Initialized, AppRoot.Current.State);
            Assert.IsTrue(Managers.Data.IsLoaded);

            StaticData data = Managers.Data.Data;
            Assert.AreEqual("기사", data.Jobs.Get("knight").Name.Resolve("ko-KR"));
            Assert.AreEqual("Knight", data.Jobs.Get("knight").Name.Resolve("en-US"));
            Assert.Throws<KeyNotFoundException>(() => data.Jobs.Get("samurai"));
            Assert.IsNotEmpty(data.Dungeons.Ordered);
            Assert.Greater(data.Balance.PartySize, 0);
        }

        [UnityTest]
        public IEnumerator ResourceManager_LoadsRegisteredAddressOncePerScope()
        {
            var resource = new ResourceManager();
            yield return TaskUtil.Await(resource.InitializeAsync());
            resource.BeginScope(ResourceScope.App);

            Task<TextAsset> first = resource.LoadAsync<TextAsset>(StaticDataFiles.Job.Address, ResourceScope.App);
            Task<TextAsset> second = resource.LoadAsync<TextAsset>(StaticDataFiles.Job.Address, ResourceScope.App);
            yield return TaskUtil.Await(first);
            yield return TaskUtil.Await(second);

            Assert.IsNotNull(first.Result);
            Assert.AreSame(first.Result, second.Result);
            StringAssert.Contains("\"SchemaVersion\"", first.Result.text);

            resource.ReleaseScope(ResourceScope.App);
            Assert.IsFalse(resource.IsScopeOpen(ResourceScope.App));
        }

        [UnityTest]
        public IEnumerator DataManager_WhenLoadedTwice_Throws()
        {
            var resource = new ResourceManager();
            yield return TaskUtil.Await(resource.InitializeAsync());
            resource.BeginScope(ResourceScope.App);
            var data = new DataManager(resource);
            yield return TaskUtil.Await(data.LoadAsync());

            Task again = data.LoadAsync();
            yield return TaskUtil.AwaitIgnoringFailure(again);

            Assert.IsInstanceOf<System.InvalidOperationException>(TaskUtil.FailureOf(again));
            resource.ReleaseAllScopes();
        }
    }
}
