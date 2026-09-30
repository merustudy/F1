using System;
using System.Collections;
using System.Threading.Tasks;
using F1.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace F1.Tests
{
    public sealed class ResourceManagerTests
    {
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
        public IEnumerator InstantiateAsync_WhenAddressNotRegistered_ThrowsResourceLoadException()
        {
            _resource.BeginScope(ResourceScope.App);

            Task<GameObject> task = _resource.InstantiateAsync("ui/app/does-not-exist", null, ResourceScope.App);
            yield return TaskUtil.AwaitIgnoringFailure(task);

            Assert.IsInstanceOf<ResourceLoadException>(TaskUtil.FailureOf(task));
        }
    }
}
