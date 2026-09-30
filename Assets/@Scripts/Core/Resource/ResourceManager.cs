using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.AddressableAssets.ResourceLocators;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace F1.Core
{
    public sealed class ResourceLoadException : Exception
    {
        public ResourceLoadException(string message, Exception inner = null)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// The only runtime code that touches Addressables. It owns every handle and releases them per scope.
    /// A failed load throws; nothing is silently replaced.
    /// </summary>
    public sealed class ResourceManager
    {
        sealed class ScopeState
        {
            public readonly Dictionary<string, AsyncOperationHandle> Assets = new Dictionary<string, AsyncOperationHandle>();
            public readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> Instances =
                new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();
        }

        readonly Dictionary<ResourceScope, ScopeState> _open = new Dictionary<ResourceScope, ScopeState>();

        public bool IsInitialized { get; private set; }

        public async Task InitializeAsync()
        {
            if (IsInitialized)
            {
                throw new InvalidOperationException("ResourceManager is already initialized.");
            }

            AsyncOperationHandle<IResourceLocator> handle = Addressables.InitializeAsync(false);
            await handle.Task;
            bool succeeded = handle.Status == AsyncOperationStatus.Succeeded;
            Exception error = handle.OperationException;
            Addressables.Release(handle);

            if (!succeeded)
            {
                throw new ResourceLoadException("Addressables failed to initialize.", error);
            }

            IsInitialized = true;
        }

        public bool IsScopeOpen(ResourceScope scope)
        {
            return _open.ContainsKey(scope);
        }

        public void BeginScope(ResourceScope scope)
        {
            RequireInitialized();
            if (_open.ContainsKey(scope))
            {
                throw new InvalidOperationException($"Scope {scope} is already open.");
            }

            _open.Add(scope, new ScopeState());
        }

        /// <summary>Loads an asset into a scope. Loading the same address again returns the same asset.</summary>
        public async Task<T> LoadAsync<T>(string address, ResourceScope scope)
            where T : UnityEngine.Object
        {
            ScopeState state = RequireOpen(scope, address);

            if (!state.Assets.TryGetValue(address, out AsyncOperationHandle handle))
            {
                await RequireLocationAsync(address, typeof(T));
                RequireStillOpen(scope, state, address);

                // Another caller may have started the same load while the location was being checked.
                if (!state.Assets.TryGetValue(address, out handle))
                {
                    handle = Addressables.LoadAssetAsync<T>(address);
                    state.Assets.Add(address, handle);
                }
            }

            await handle.Task;
            RequireStillOpen(scope, state, address);

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Exception error = handle.OperationException;
                state.Assets.Remove(address);
                Addressables.Release(handle);
                throw new ResourceLoadException($"Could not load '{address}'.", error);
            }

            if (!(handle.Result is T asset))
            {
                throw new ResourceLoadException(
                    $"'{address}' is loaded in scope {scope} as {handle.Result.GetType().Name}, not {typeof(T).Name}.");
            }

            return asset;
        }

        public async Task<GameObject> InstantiateAsync(string address, Transform parent, ResourceScope scope)
        {
            ScopeState state = RequireOpen(scope, address);
            await RequireLocationAsync(address, typeof(GameObject));
            RequireStillOpen(scope, state, address);

            AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address, parent, false);
            await handle.Task;

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Exception error = handle.OperationException;
                Addressables.Release(handle);
                throw new ResourceLoadException($"Could not instantiate '{address}'.", error);
            }

            if (!_open.TryGetValue(scope, out ScopeState current) || current != state)
            {
                Addressables.ReleaseInstance(handle);
                throw new ResourceLoadException($"Scope {scope} was released while '{address}' was being instantiated.");
            }

            state.Instances.Add(handle.Result, handle);
            return handle.Result;
        }

        public void ReleaseInstance(GameObject instance)
        {
            foreach (ScopeState state in _open.Values)
            {
                if (state.Instances.Remove(instance, out AsyncOperationHandle<GameObject> handle))
                {
                    ReleaseInstanceHandle(handle);
                    return;
                }
            }

            throw new InvalidOperationException("The instance was not created by ResourceManager or is already released.");
        }

        /// <summary>Releases every asset and instance of the scope and closes it.</summary>
        public void ReleaseScope(ResourceScope scope)
        {
            if (!_open.Remove(scope, out ScopeState state))
            {
                throw new InvalidOperationException($"Scope {scope} is not open.");
            }

            Release(state);
        }

        internal void ReleaseAllScopes()
        {
            foreach (ScopeState state in _open.Values)
            {
                Release(state);
            }

            _open.Clear();
        }

        static void Release(ScopeState state)
        {
            foreach (AsyncOperationHandle<GameObject> handle in state.Instances.Values)
            {
                ReleaseInstanceHandle(handle);
            }

            foreach (AsyncOperationHandle handle in state.Assets.Values)
            {
                Addressables.Release(handle);
            }

            state.Instances.Clear();
            state.Assets.Clear();
        }

        /// <summary>
        /// A scene that unloads takes its instances with it, and Addressables then releases their
        /// handles by itself. Such a handle is no longer valid and must not be released again.
        /// </summary>
        static void ReleaseInstanceHandle(AsyncOperationHandle<GameObject> handle)
        {
            if (handle.IsValid())
            {
                Addressables.ReleaseInstance(handle);
            }
        }

        void RequireInitialized()
        {
            if (!IsInitialized)
            {
                throw new InvalidOperationException("ResourceManager is not initialized.");
            }
        }

        ScopeState RequireOpen(ResourceScope scope, string address)
        {
            RequireInitialized();
            LogicalAddress.Require(address);
            if (!_open.TryGetValue(scope, out ScopeState state))
            {
                throw new InvalidOperationException($"Scope {scope} is not open. Cannot load '{address}'.");
            }

            return state;
        }

        void RequireStillOpen(ResourceScope scope, ScopeState state, string address)
        {
            if (!_open.TryGetValue(scope, out ScopeState current) || current != state)
            {
                throw new ResourceLoadException($"Scope {scope} was released while '{address}' was loading.");
            }
        }

        /// <summary>
        /// Checks that the address exists before loading, so an unknown address fails with a clear
        /// exception instead of an Addressables error log.
        /// </summary>
        static async Task RequireLocationAsync(string address, Type type)
        {
            AsyncOperationHandle<IList<IResourceLocation>> handle = Addressables.LoadResourceLocationsAsync(address, type);
            await handle.Task;
            bool found = handle.Status == AsyncOperationStatus.Succeeded && handle.Result != null && handle.Result.Count > 0;
            Addressables.Release(handle);

            if (!found)
            {
                throw new ResourceLoadException($"Address '{address}' is not registered for type {type.Name}.");
            }
        }
    }
}
