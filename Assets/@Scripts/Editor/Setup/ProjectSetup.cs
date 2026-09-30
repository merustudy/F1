using F1.Editor.Data;
using UnityEditor;
using UnityEditor.Build;
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

        /// <summary>
        /// The save directory is named after the company and product on Windows and after this
        /// identifier in a macOS player. Changing any of them makes existing saves unreachable.
        /// </summary>
        const string ApplicationIdentifier = "com.funitup.f1";
        const int DefaultScreenWidth = 1920;
        const int DefaultScreenHeight = 1080;

        [MenuItem("F1/Setup/Apply Project Setup")]
        public static void ApplyMenu()
        {
            ApplyPlayerSettings();
            SceneSetup.EnsureScenes();
            DataTransformMenu.Transform();
            LocalizationSetup.Sync();

            // After the data and the UI strings: the atlas holds the characters of both.
            FontSetup.Sync();
            UiPrefabSetup.Sync();
            AddressablesSetup.Sync();

            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        static void ApplyPlayerSettings()
        {
            PlayerSettings.companyName = CompanyName;
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationIdentifier);
            PlayerSettings.defaultScreenWidth = DefaultScreenWidth;
            PlayerSettings.defaultScreenHeight = DefaultScreenHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
            PlayerSettings.resizableWindow = true;
        }
    }
}
