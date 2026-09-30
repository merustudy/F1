using System;
using System.IO;
using F1.Save;
using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// The single composition root. It creates every manager, registers them once and runs the boot
    /// sequence. A failed step stops the sequence; the app never enters Main partially initialized.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class AppRoot : MonoBehaviour
    {
        [SerializeField] BootstrapView _view;

        /// <summary>Tests point this at a temporary directory so they never touch real saves.</summary>
        internal static string SaveRootOverride;

        ResourceManager _resource;

        public static AppRoot Current { get; private set; }

        public InitializationState State { get; private set; } = InitializationState.NotStarted;
        public BootStep? FailedStep { get; private set; }
        public BootstrapView View => _view;

        void Awake()
        {
            if (Current != null && Current != this)
            {
                Destroy(gameObject);
                return;
            }

            Current = this;
            DontDestroyOnLoad(gameObject);
        }

        async void Start()
        {
            if (Current != this)
            {
                return;
            }

            await InitializeAsync();
        }

        void OnDestroy()
        {
            if (Current != this)
            {
                return;
            }

            Current = null;
            _resource?.ReleaseAllScopes();
            Managers.Reset();
        }

        async Awaitable InitializeAsync()
        {
            State = InitializationState.Initializing;
            BootStep step = BootStep.CreateManagers;
            try
            {
                _view.ShowLoading();
                string saveRoot = SaveRootOverride ?? Path.Combine(Application.persistentDataPath, "Saves");
                var save = new SaveManager(saveRoot);
                var setting = new SettingManager(save);
                var resource = new ResourceManager();
                var scene = new SceneManagerEx();
                _resource = resource;

                step = BootStep.ConfigureManagers;
                Managers.Configure(new ManagerSet
                {
                    Resource = resource,
                    Save = save,
                    Setting = setting,
                    Scene = scene,
                });

                step = BootStep.InitializeSaveStorage;
                save.Initialize();

                step = BootStep.LoadSettings;
                setting.Load(SettingManager.SystemLocaleCode(Application.systemLanguage));

                step = BootStep.InitializeResources;
                await resource.InitializeAsync();
                resource.BeginScope(ResourceScope.App);

                step = BootStep.LoadMainScene;
                await scene.LoadMainAsync();

                step = BootStep.BindMainScene;
                MainSceneRoot main = FindFirstObjectByType<MainSceneRoot>();
                if (main == null)
                {
                    throw new InvalidOperationException("MainSceneRoot was not found in the Main scene.");
                }

                main.Bind();

                State = InitializationState.Initialized;
                _view.Hide();
            }
            catch (Exception exception)
            {
                State = InitializationState.Failed;
                FailedStep = step;
                Debug.LogException(exception);
                if (_view != null)
                {
                    _view.ShowError(step);
                }
            }
        }
    }
}
