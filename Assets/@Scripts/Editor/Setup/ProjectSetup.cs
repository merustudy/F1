using F1.Editor.Data;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Setup
{
    /// <summary>
    /// Single entry point of the validation chain's setup step. Every call is idempotent:
    /// it brings project settings and generated assets to the state the code describes.
    /// </summary>
    public static class ProjectSetup
    {
        public const string DoneToken = "F1_PROJECT_SETUP_DONE";

        const string CompanyName = "funitup";
        const int DefaultScreenWidth = 1920;
        const int DefaultScreenHeight = 1080;

        [MenuItem("F1/Setup/Apply Project Setup")]
        public static void ApplyMenu()
        {
            ApplyPlayerSettings();
            SceneSetup.EnsureScenes();
            DataTransformMenu.Transform();
            AddressablesSetup.Sync();

            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.defaultScreenWidth = DefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = DefaultScreenHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
        }
    }
}
