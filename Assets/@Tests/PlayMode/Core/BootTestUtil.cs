using System;
using System.Collections;
using System.IO;
using F1.Core;
using NUnit.Framework;
using UnityEngine;

namespace F1.Tests
{
    internal static class BootTestUtil
    {
        public const float DefaultTimeoutSeconds = 20f;

        /// <summary>Waits until the boot sequence has finished, successfully or not.</summary>
        public static IEnumerator WaitForBootToFinish(float timeoutSeconds = DefaultTimeoutSeconds)
        {
            float deadline = Time.realtimeSinceStartup + timeoutSeconds;
            while (AppRoot.Current == null
                || AppRoot.Current.State == InitializationState.NotStarted
                || AppRoot.Current.State == InitializationState.Initializing)
            {
                if (Time.realtimeSinceStartup > deadline)
                {
                    Assert.Fail("Boot did not finish within the timeout.");
                }

                yield return null;
            }
        }

        /// <summary>Points the next boot at a fresh temporary save directory and returns its path.</summary>
        public static string UseTemporarySaveRoot()
        {
            string root = Path.Combine(Path.GetTempPath(), "F1Tests", Guid.NewGuid().ToString("N"));
            AppRoot.SaveRootOverride = root;
            return root;
        }

        /// <summary>
        /// Destroys the persistent AppRoot so the next test boots from a clean state, and removes the
        /// temporary save directory.
        /// </summary>
        public static IEnumerator ShutdownApp()
        {
            if (AppRoot.Current != null)
            {
                UnityEngine.Object.Destroy(AppRoot.Current.gameObject);
                yield return null;
            }

            Managers.Reset();

            string root = AppRoot.SaveRootOverride;
            AppRoot.SaveRootOverride = null;
            if (root != null && Directory.Exists(root))
            {
                Directory.Delete(root, true);
            }
        }
    }
}
