using System;
using System.IO;
using System.Security.Cryptography;
using F1.Flow;
using F1.Gameplay;
using F1.Save;
using F1.UI;
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

        /// <summary>The seed of a new run comes from the OS random source, never from game randomness.</summary>
        static ulong NewRunSeed()
        {
            var bytes = new byte[8];
            using (var random = RandomNumberGenerator.Create())
            {
                random.GetBytes(bytes);
            }

            return BitConverter.ToUInt64(bytes, 0);
        }

        /// <summary>
        /// What the play log writes beside a screen: the game phase, the day and, on an expedition, the floor the party
        /// stands on (0 before the first node), the node and the battle speed last chosen.
        /// </summary>
        static string PlayLogState(RunManager run, ExpeditionManager expedition)
        {
            if (!run.HasRun)
            {
                return "phase=NoRun";
            }

            string state = $"phase={expedition.Phase} day={run.Run.Day}";
            ExpeditionState trip = expedition.Expedition;
            if (trip != null)
            {
                int floor = trip.CurrentNodeId >= 0 ? trip.Map.Get(trip.CurrentNodeId).Floor : 0;
                state += $" floor={floor}/{trip.Map.FloorCount} node={trip.CurrentNodeId}";
            }

            return state + $" speed={BattleScreen.PreferredSpeedPercent}";
        }

        async Awaitable InitializeAsync()
        {
            State = InitializationState.Initializing;
            BootStep step = BootStep.CreateManagers;
            try
            {
                _view.ShowLoading();
                string saveRoot = SaveRootOverride ?? Path.Combine(Application.persistentDataPath, "Saves");
                var locale = new UnityLocaleAdapter();
                var save = new SaveManager(saveRoot);
                var setting = new SettingManager(save, locale.ApplyAsync);
                var resource = new ResourceManager();
                var data = new DataManager(resource);
                var scene = new SceneManagerEx();
                var run = new RunManager(data, save, NewRunSeed);
                var expedition = new ExpeditionManager(data, run);
                var ui = new UIManager(resource);
                var sound = new SoundManager(resource, setting, SoundOutput.Create(transform));
                _resource = resource;

                step = BootStep.ConfigureManagers;
                Managers.Configure(new ManagerSet
                {
                    Resource = resource,
                    Save = save,
                    Setting = setting,
                    Data = data,
                    Scene = scene,
                    Run = run,
                    Expedition = expedition,
                    UI = ui,
                    Sound = sound,
                });

                step = BootStep.InitializeSaveStorage;
                save.Initialize();

                step = BootStep.LoadSettings;
                setting.Load(SettingManager.SystemLocaleCode(Application.systemLanguage));

                step = BootStep.InitializeResources;
                await resource.InitializeAsync();
                resource.BeginScope(ResourceScope.App);

                step = BootStep.InitializeLocalization;
                await locale.InitializeAsync();

                step = BootStep.ApplyLocale;
                await locale.ApplyAsync(setting.LocaleCode);

                step = BootStep.LoadStaticData;
                await data.LoadAsync();

                step = BootStep.LoadSounds;
                await sound.LoadAsync();

                // An unreadable run file is not a boot failure: the game starts without a run.
                step = BootStep.LoadRun;
                run.Load();
                expedition.Restore();
                if (expedition.LastResume == BattleResume.Diverged || expedition.LastResume == BattleResume.Restarted)
                {
                    Debug.LogWarning($"The saved battle could not be reproduced exactly ({expedition.LastResume}). Rules or data changed since it was saved.");
                }

                step = BootStep.LoadMainScene;
                await scene.LoadMainAsync();

                step = BootStep.BindMainScene;
                MainSceneRoot main = FindFirstObjectByType<MainSceneRoot>();
                if (main == null)
                {
                    throw new InvalidOperationException("MainSceneRoot was not found in the Main scene.");
                }

                main.Bind();

                step = BootStep.ShowMainUi;
                await ui.BindAsync(main.UiRoot);
                // The play log of the tuning stage (Docs/Architecture/10_TESTING_VALIDATION.md "플레이 기록"): one line per
                // screen, beside the saves and never in them. Tests keep it inside their temporary save root.
                var playLog = new PlayLog(Path.Combine(SaveRootOverride != null ? saveRoot : Application.persistentDataPath, PlayLog.FolderName));
                ui.ScreenShown += id => playLog.Append(id.ToString(), PlayLogState(run, expedition));
                playLog.Append("Boot", $"locale={setting.LocaleCode} run={run.LoadStatus}");
                await ui.ShowAsync(ScreenId.Title);

                State = InitializationState.Initialized;
                _view.Hide();
                Debug.Log($"[F1] Boot finished. Locale {setting.LocaleCode}, saved run: {run.LoadStatus}.");
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
