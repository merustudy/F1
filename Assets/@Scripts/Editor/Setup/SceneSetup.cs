using System;
using System.IO;
using System.Linq;
using F1.Core;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;

namespace F1.Editor.Setup
{
    /// <summary>
    /// Builds the Boot and Main scenes from code. Scenes are never edited by hand:
    /// change this file and rebuild.
    /// </summary>
    public static class SceneSetup
    {
        public const string SceneFolder = "Assets/@Scenes";
        public const string BootScenePath = SceneFolder + "/Boot.unity";
        public const string MainScenePath = SceneFolder + "/Main.unity";
        public const string BootFontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

        static readonly Color BackgroundColor = new Color(0.07f, 0.08f, 0.10f, 1f);
        static readonly Color TextColor = new Color(0.92f, 0.92f, 0.90f, 1f);
        static readonly Color ErrorColor = new Color(0.95f, 0.45f, 0.40f, 1f);

        [MenuItem("F1/Setup/Rebuild Boot And Main Scenes")]
        public static void RebuildMenu()
        {
            if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            BuildBootScene();
            BuildMainScene();
            RegisterBuildScenes();
            Debug.Log("F1_SCENE_REBUILD_DONE");
        }

        /// <summary>Creates the scenes only when they are missing, then makes Build Settings match.</summary>
        public static void EnsureScenes()
        {
            if (!File.Exists(BootScenePath))
            {
                BuildBootScene();
            }

            if (!File.Exists(MainScenePath))
            {
                BuildMainScene();
            }

            RegisterBuildScenes();
        }

        static void BuildBootScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera("Boot Camera");

            var appRootObject = new GameObject("AppRoot");
            var appRoot = appRootObject.AddComponent<AppRoot>();
            BootstrapView view = CreateBootstrapView(appRootObject.transform);
            UiBuild.SetReference(appRoot, "_view", view);

            Directory.CreateDirectory(SceneFolder);
            if (!EditorSceneManager.SaveScene(scene, BootScenePath))
            {
                throw new InvalidOperationException($"Could not save {BootScenePath}.");
            }
        }

        static void BuildMainScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            CreateCamera("Main Camera");

            var eventSystemObject = new GameObject("EventSystem");
            eventSystemObject.AddComponent<EventSystem>();
            var inputModule = eventSystemObject.AddComponent<InputSystemUIInputModule>();
            inputModule.AssignDefaultActions();

            Canvas uiRoot = UiBuild.Canvas("UIRoot", null, sortingOrder: 0, raycaster: true);

            var rootObject = new GameObject("MainSceneRoot");
            var root = rootObject.AddComponent<MainSceneRoot>();
            UiBuild.SetReference(root, "_uiRoot", (RectTransform)uiRoot.transform);

            Directory.CreateDirectory(SceneFolder);
            if (!EditorSceneManager.SaveScene(scene, MainScenePath))
            {
                throw new InvalidOperationException($"Could not save {MainScenePath}.");
            }
        }

        static void RegisterBuildScenes()
        {
            string[] expected = { BootScenePath, MainScenePath };
            string[] current = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (current.SequenceEqual(expected) && EditorBuildSettings.scenes.Length == expected.Length)
            {
                return;
            }

            EditorBuildSettings.scenes = expected.Select(path => new EditorBuildSettingsScene(path, true)).ToArray();
        }

        static void CreateCamera(string name)
        {
            var cameraObject = new GameObject(name);
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0f, 0f, -10f);

            var camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = BackgroundColor;

            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            cameraObject.AddComponent<AudioListener>();
        }

        static BootstrapView CreateBootstrapView(Transform parent)
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(BootFontPath);
            if (font == null)
            {
                throw new InvalidOperationException(
                    $"Boot font not found at '{BootFontPath}'. TMP Essential Resources must be imported first.");
            }

            // Sorted above every other canvas so a boot error is never hidden.
            Canvas canvas = UiBuild.Canvas("BootstrapView", parent, sortingOrder: 1000, raycaster: false);

            UnityEngine.UI.Image background = UiBuild.Image("Background", canvas.transform, BackgroundColor);
            UiBuild.Stretch(background.rectTransform);

            TextMeshProUGUI status = UiBuild.Text("Status", canvas.transform, font, "Loading...", 56f, TextColor);
            UiBuild.Place(status.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(1400f, 90f));

            TextMeshProUGUI error = UiBuild.Text("Error", canvas.transform, font, string.Empty, 36f, ErrorColor);
            UiBuild.Place(error.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -50f), new Vector2(1400f, 70f));

            var view = canvas.gameObject.AddComponent<BootstrapView>();
            UiBuild.SetReference(view, "_status", status);
            UiBuild.SetReference(view, "_error", error);
            return view;
        }
    }
}
