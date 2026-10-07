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
        SaveErrorOverlay _saveError;

        public UIManager(ResourceManager resource)
        {
            _resource = resource ?? throw new ArgumentNullException(nameof(resource));
        }

        /// <summary>The screen on display, or null before the first one is shown.</summary>
        public UIScreen Current { get; private set; }

        public ScreenId? CurrentId { get; private set; }

        /// <summary>True while a screen is being replaced.</summary>
        public bool IsBusy { get; private set; }

        /// <summary>A screen has come up (after <see cref="UIScreen.Open"/>). The play log of the tuning stage listens (Docs/Architecture/10_TESTING_VALIDATION.md "플레이 기록").</summary>
        public event Action<ScreenId> ScreenShown;

        /// <summary>The overlay that blocks the game while a save has failed.</summary>
        public SaveErrorOverlay SaveError => _saveError;

        /// <summary>
        /// Gives the manager the UI root of the Main scene and creates what stays for the whole
        /// session: the overlay shown when saving fails.
        /// </summary>
        public async Task BindAsync(RectTransform root)
        {
            _root = root != null ? root : throw new ArgumentNullException(nameof(root));

            GameObject overlay = await _resource.InstantiateAsync(SaveErrorOverlay.Address, _root, ResourceScope.App);
            _saveError = overlay.GetComponent<SaveErrorOverlay>();
            if (_saveError == null)
            {
                throw new InvalidOperationException("The save error overlay prefab has no SaveErrorOverlay component.");
            }

            _saveError.Open();
        }

        /// <summary>
        /// Replaces the current screen. The new screen is created and opened before the old one is
        /// removed, so there is no empty frame in between.
        /// </summary>
        public async Task ShowAsync(ScreenId id)
        {
            if (_root == null || _saveError == null)
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

                // The screen loads what it shows before it is seen: it stays hidden meanwhile, and the
                // previous screen stays on display.
                instance.SetActive(false);
                try
                {
                    await screen.PrepareAsync();
                }
                catch
                {
                    _resource.ReleaseInstance(instance);
                    throw;
                }

                instance.SetActive(true);

                Current = screen;
                CurrentId = id;
                screen.Open();
                ScreenShown?.Invoke(id);

                // Screens are added last, so the overlay is moved back on top of them.
                _saveError.transform.SetAsLastSibling();

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
