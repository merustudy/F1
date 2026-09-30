using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace F1.Core
{
    public sealed class SceneManagerEx
    {
        public const string BootSceneName = "Boot";
        public const string MainSceneName = "Main";

        public string ActiveSceneName => SceneManager.GetActiveScene().name;

        public async Awaitable LoadMainAsync()
        {
            AsyncOperation operation = SceneManager.LoadSceneAsync(MainSceneName, LoadSceneMode.Single);
            if (operation == null)
            {
                throw new InvalidOperationException($"Scene '{MainSceneName}' is not in Build Settings.");
            }

            await operation;
        }
    }
}
