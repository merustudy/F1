using System;
using System.Collections;
using System.Threading.Tasks;
using F1.Core;
using F1.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class ResourceManagerTests
    {
        /// <summary>Any registered prefab will do; this one does nothing until a manager opens it.</summary>
        static readonly string PrefabAddress = ScreenCatalog.Address(ScreenId.Settlement);

        ResourceManager _resource;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _resource = new ResourceManager();
            yield return TaskUtil.Await(_resource.InitializeAsync());
        }

        [TearDown]
        public void TearDown()
        {
            _resource.ReleaseAllScopes();
        }

        [Test]
        public void BeginScope_WhenAlreadyOpen_Throws()
        {
            _resource.BeginScope(ResourceScope.App);

            Assert.IsTrue(_resource.IsScopeOpen(ResourceScope.App));
            Assert.Throws<InvalidOperationException>(() => _resource.BeginScope(ResourceScope.App));
        }

        [Test]
        public void ReleaseScope_WhenNotOpen_Throws()
        {
            Assert.Throws<InvalidOperationException>(() => _resource.ReleaseScope(ResourceScope.Lobby));
        }

        [Test]
        public void ReleaseScope_WhenOpen_ClosesScope()
        {
            _resource.BeginScope(ResourceScope.Lobby);

            _resource.ReleaseScope(ResourceScope.Lobby);

            Assert.IsFalse(_resource.IsScopeOpen(ResourceScope.Lobby));
        }

        [UnityTest]
        public IEnumerator LoadAsync_WhenScopeNotOpen_Throws()
        {
            Task<TextAsset> task = _resource.LoadAsync<TextAsset>("data/app/anything", ResourceScope.App);
            yield return TaskUtil.AwaitIgnoringFailure(task);

            Assert.IsInstanceOf<InvalidOperationException>(TaskUtil.FailureOf(task));
        }

        [UnityTest]
        public IEnumerator LoadAsync_WhenAddressFormatInvalid_Throws()
        {
            _resource.BeginScope(ResourceScope.App);

            Task<TextAsset> task = _resource.LoadAsync<TextAsset>("Assets/@Data/Job.json", ResourceScope.App);
            yield return TaskUtil.AwaitIgnoringFailure(task);

            Assert.IsInstanceOf<ArgumentException>(TaskUtil.FailureOf(task));
        }

        [UnityTest]
        public IEnumerator LoadAsync_WhenAddressNotRegistered_ThrowsResourceLoadException()
        {
            _resource.BeginScope(ResourceScope.App);

            Task<TextAsset> task = _resource.LoadAsync<TextAsset>("data/app/does-not-exist", ResourceScope.App);
            yield return TaskUtil.AwaitIgnoringFailure(task);

            Assert.IsInstanceOf<ResourceLoadException>(TaskUtil.FailureOf(task));
        }

        [UnityTest]
        public IEnumerator InstantiateAsync_CreatesAnInstance_AndReleaseInstanceDestroysIt()
        {
            _resource.BeginScope(ResourceScope.Expedition);

            Task<GameObject> task = _resource.InstantiateAsync(PrefabAddress, null, ResourceScope.Expedition);
            yield return TaskUtil.Await(task);
            GameObject instance = task.Result;
            Assert.IsNotNull(instance);

            _resource.ReleaseInstance(instance);
            yield return null;

            Assert.IsTrue(instance == null, "The instance is destroyed.");
            Assert.Throws<InvalidOperationException>(() => _resource.ReleaseInstance(instance), "It cannot be released twice.");
        }

        [UnityTest]
        public IEnumerator ReleaseScope_DestroysTheInstancesOfTheScope()
        {
            _resource.BeginScope(ResourceScope.Expedition);
            Task<GameObject> task = _resource.InstantiateAsync(PrefabAddress, null, ResourceScope.Expedition);
            yield return TaskUtil.Await(task);
            GameObject instance = task.Result;

            _resource.ReleaseScope(ResourceScope.Expedition);
            yield return null;

            Assert.IsTrue(instance == null);
        }

        [UnityTest]
        public IEnumerator ReleaseScope_AfterASceneUnloadDestroyedAnInstance_StillWorks()
        {
            // The instance is created under a parent that lives in a scene of its own.
            Scene scene = SceneManager.CreateScene("ResourceManagerTestsScene");
            var parent = new GameObject("Parent");
            SceneManager.MoveGameObjectToScene(parent, scene);

            _resource.BeginScope(ResourceScope.Expedition);
            Task<GameObject> task = _resource.InstantiateAsync(PrefabAddress, parent.transform, ResourceScope.Expedition);
            yield return TaskUtil.Await(task);
            GameObject instance = task.Result;

            yield return SceneManager.UnloadSceneAsync(scene);
            Assert.IsTrue(instance == null, "The scene took the instance with it.");

            Assert.DoesNotThrow(() => _resource.ReleaseScope(ResourceScope.Expedition));
        }

        [UnityTest]
        public IEnumerator InstantiateAsync_WhenAddressNotRegistered_ThrowsResourceLoadException()
        {
            _resource.BeginScope(ResourceScope.App);

            Task<GameObject> task = _resource.InstantiateAsync("ui/app/does-not-exist", null, ResourceScope.App);
            yield return TaskUtil.AwaitIgnoringFailure(task);

            Assert.IsInstanceOf<ResourceLoadException>(TaskUtil.FailureOf(task));
        }
    }
}
