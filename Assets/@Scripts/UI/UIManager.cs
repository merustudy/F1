using System;
using System.Threading.Tasks;
using F1.Core;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Shows one screen at a time under the UI root of the Main scene. It loads the screen's
    /// prefab through ResourceManager and keeps only the resource scope of the shown screen open.
    /// </summary>
    public sealed class UIManager
    {
        static readonly ResourceScope[] ScreenScopes = { ResourceScope.Lobby, ResourceScope.Expedition };

        readonly ResourceManager _resource;
        RectTransform _root;

        public UIManager(ResourceManager resource)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        /// <summary>The screen on display, or null before the first one is shown.</summary>
        public UIScreen Current { get; private set; }

        public ScreenId? CurrentId { get; private set; }

        /// <summary>True while a screen is being replaced.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>Gives the manager the UI root of the Main scene.</summary>
        public void Bind(RectTransform root)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));
        }

        /// <summary>
        /// Replaces the current screen. The new screen is created and opened before the old one is
        /// removed, so there is no empty frame in between.
        /// </summary>
        public async Task ShowAsync(ScreenId id)
        {
            if (_root == null)
            {
                throw new InvalidOperationException("UIManager is not bound to a UI root.");
            }

            if (IsBusy)
            {
                throw new InvalidOperationException("A screen is already being replaced.");
            }

            IsBusy = true;
            UIScreen previous = Current;
            try
            {
                if (previous != null)
                {
                    previous.SetInteractable(false);
                }

                ResourceScope scope = ScreenCatalog.Scope(id);
                if (!_resource.IsScopeOpen(scope))
                {
                    _resource.BeginScope(scope);
                }

                GameObject instance = await _resource.InstantiateAsync(ScreenCatalog.Address(id), _root, scope);
                var screen = instance.GetComponent<UIScreen>();
                if (screen == null)
                {
                    _resource.ReleaseInstance(instance);
                    throw new InvalidOperationException($"The prefab of screen {id} has no UIScreen component.");
                }

                Current = screen;
                CurrentId = id;
                screen.Open();

                if (previous != null)
                {
                    _resource.ReleaseInstance(previous.gameObject);
                }

                foreach (ResourceScope other in ScreenScopes)
                {
                    if (other != scope && _resource.IsScopeOpen(other))
                    {
                        _resource.ReleaseScope(other);
                    }
                }
            }
            catch
            {
                // The new screen did not come up: the old one stays and works again.
                if (previous != null && Current == previous)
                {
                    previous.SetInteractable(true);
                }

                throw;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}
