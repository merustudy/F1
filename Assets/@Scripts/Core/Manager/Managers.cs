using System;
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

        public static SceneManagerEx Scene => Require().Scene;

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
        public SceneManagerEx Scene;

        internal void Validate()
        {
            RequireManager(Scene, nameof(Scene));
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
