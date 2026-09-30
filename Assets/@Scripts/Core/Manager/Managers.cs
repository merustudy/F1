using System;
using F1.Flow;
using F1.Save;
using UnityEngine;

namespace F1.Core
{
    /// <summary>
    /// Service container configured once by AppRoot. It holds no logic.
    /// Managers receive their dependencies through constructors, never through this class.
    /// </summary>
    public static class Managers
    {
        static ManagerSet _set;

        public static bool IsConfigured => _set != null;

        public static ResourceManager Resource => Require().Resource;
        public static SaveManager Save => Require().Save;
        public static SettingManager Setting => Require().Setting;
        public static DataManager Data => Require().Data;
        public static SceneManagerEx Scene => Require().Scene;
        public static RunManager Run => Require().Run;
        public static ExpeditionManager Expedition => Require().Expedition;

        public static void Configure(ManagerSet set)
        {
            if (set == null)
            {
                throw new ArgumentNullException(nameof(set));
            }

            if (_set != null)
            {
                throw new InvalidOperationException("Managers is already configured.");
            }

            set.Validate();
            _set = set;
        }

        internal static void Reset()
        {
            _set = null;
        }

        static ManagerSet Require()
        {
            return _set ?? throw new InvalidOperationException(
                "Managers is not configured. AppRoot must call Managers.Configure first.");
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetOnLoad()
        {
            _set = null;
        }
    }

    /// <summary>The full set of managers. Every field is required.</summary>
    public sealed class ManagerSet
    {
        public ResourceManager Resource;
        public SaveManager Save;
        public SettingManager Setting;
        public DataManager Data;
        public SceneManagerEx Scene;
        public RunManager Run;
        public ExpeditionManager Expedition;

        internal void Validate()
        {
            RequireManager(Resource, nameof(Resource));
            RequireManager(Save, nameof(Save));
            RequireManager(Setting, nameof(Setting));
            RequireManager(Data, nameof(Data));
            RequireManager(Scene, nameof(Scene));
            RequireManager(Run, nameof(Run));
            RequireManager(Expedition, nameof(Expedition));
        }

        static void RequireManager(object manager, string name)
        {
            if (manager == null)
            {
                throw new ArgumentException($"ManagerSet.{name} is missing.");
            }
        }
    }
}
