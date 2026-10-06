using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using F1.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    /// <summary>
    /// Builds the screen prefabs from code. Prefabs are never edited by hand: change the builder
    /// and run the setup. They are rebuilt only when the builder code changed, which a stamp
    /// file records, so an unchanged builder never touches the prefabs.
    /// </summary>
    public static partial class UiPrefabSetup
    {
        public const string DoneToken = "F1_UI_PREFAB_SYNC_DONE";
        public const string PrefabDirectory = "Assets/@Prefabs/UI";
        public const string StampPath = PrefabDirectory + "/UiPrefabStamp.txt";

        const string BuilderDirectory = "Assets/@Scripts/Editor/Setup/Ui";
        const string PalettePath = "Assets/@Scripts/UI/UiPalette.cs";

        public const string SaveErrorOverlayPath = PrefabDirectory + "/SaveErrorOverlay.prefab";

        /// <summary>
        /// The shader that draws a sprite as a one-colour silhouette (2026-10-07 round 41, the tier outline) and the material made from it:
        /// a generated asset next to the prefabs, made once and kept, which the prefabs reference so that the shader ships with them.
        /// </summary>
        public const string SilhouetteShaderPath = "Assets/@Shaders/Silhouette.shader";
        public const string SilhouetteMaterialPath = PrefabDirectory + "/Silhouette.mat";

        public static string PrefabPath(ScreenId id)
        {
            return $"{PrefabDirectory}/{id}Screen.prefab";
        }

        /// <summary>Every prefab this setup builds: one per screen and the save error overlay.</summary>
        public static List<string> AllPrefabPaths()
        {
            List<string> paths = ScreenCatalog.All.Select(PrefabPath).ToList();
            paths.Add(SaveErrorOverlayPath);
            return paths;
        }

        static Material SilhouetteMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(SilhouetteMaterialPath);
            if (material == null)
            {
                var shader = AssetDatabase.LoadAssetAtPath<Shader>(SilhouetteShaderPath);
                if (shader == null)
                {
                    throw new InvalidOperationException($"The silhouette shader is not at {SilhouetteShaderPath}.");
                }

                material = new Material(shader);
                AssetDatabase.CreateAsset(material, SilhouetteMaterialPath);
            }

            return material;
        }

        [MenuItem("F1/Setup/Rebuild UI Prefabs")]
        public static void RebuildMenu()
        {
            Build(ComputeStamp());
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        /// <summary>Rebuilds every screen prefab when the builder changed or a prefab is missing.</summary>
        public static void Sync()
        {
            string stamp = ComputeStamp();
            if (!IsUpToDate(stamp))
            {
                Build(stamp);
            }
        }

        static bool IsUpToDate(string stamp)
        {
            return File.Exists(StampPath)
                && File.ReadAllText(StampPath).Trim() == stamp
                && AllPrefabPaths().All(File.Exists);
        }

        static void Build(string stamp)
        {
            UiBuild.ResetCache();
            Directory.CreateDirectory(PrefabDirectory);

            // Everything is built under an inactive holder so that no component runs while the
            // hierarchy is being put together.
            var holder = new GameObject("UiPrefabBuild");
            holder.SetActive(false);
            try
            {
                Save(BuildTitle(holder.transform), PrefabPath(ScreenId.Title));
                Save(BuildLobby(holder.transform), PrefabPath(ScreenId.Lobby));
                Save(BuildNodeMap(holder.transform), PrefabPath(ScreenId.NodeMap));
                Save(BuildBattle(holder.transform), PrefabPath(ScreenId.Battle));
                Save(BuildReward(holder.transform), PrefabPath(ScreenId.Reward));
                Save(BuildSettlement(holder.transform), PrefabPath(ScreenId.Settlement));
                Save(BuildSaveErrorOverlay(holder.transform), SaveErrorOverlayPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(holder);
            }

            File.WriteAllText(StampPath, stamp + "\n", new UTF8Encoding(false));
            AssetDatabase.ImportAsset(StampPath);
            Debug.Log("UI prefabs rebuilt.");
        }

        static void Save(Component root, string path)
        {
            // Unity keeps the ids of a rebuilt prefab by matching object names. A repeated name gets
            // new ids on every rebuild, which would rewrite the file each time.
            List<string> repeated = root.GetComponentsInChildren<Transform>(true)
                .GroupBy(t => t.name)
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();
            if (repeated.Count > 0)
            {
                throw new InvalidOperationException($"{path}: object names must be unique; repeated: {string.Join(", ", repeated)}.");
            }
            PrefabUtility.SaveAsPrefabAsset(root.gameObject, path, out bool success);
            if (!success)
            {
                throw new InvalidOperationException($"Could not save {path}.");
            }
        }

        /// <summary>A digest of the builder sources. Line endings are ignored so every OS gets the same value.</summary>
        public static string ComputeStamp()
        {
            List<string> files = Directory.GetFiles(BuilderDirectory, "*.cs").Select(f => f.Replace('\\', '/')).ToList();
            files.Add(PalettePath);
            files.Sort(StringComparer.Ordinal);

            using (var sha = SHA256.Create())
            {
                var all = new StringBuilder();
                foreach (string file in files)
                {
                    all.Append(file).Append('\n').Append(File.ReadAllText(file).Replace("\r", string.Empty)).Append('\n');
                }

                byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(all.ToString()));
                return BitConverter.ToString(hash).Replace("-", string.Empty).ToLowerInvariant();
            }
        }

        /// <summary>Every reason the screen prefabs do not match the builder, the font or the string table.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            if (!IsUpToDate(ComputeStamp()))
            {
                problems.Add("UI prefabs are older than their builder. Run F1/Setup/Rebuild UI Prefabs (or Tools/chain.sh setup).");
                return problems;
            }

            TMP_FontAsset font = FontSetup.LoadFontAsset();
            var keys = new HashSet<string>(LocalizationSetup.ReadSource().Rows.Select(r => r.Key), StringComparer.Ordinal);

            foreach (string path in AllPrefabPaths())
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool isOverlay = path == SaveErrorOverlayPath;
                if (prefab == null || (isOverlay ? prefab.GetComponent<SaveErrorOverlay>() == null : prefab.GetComponent<UIScreen>() == null))
                {
                    problems.Add($"{path}: the root does not have its script.");
                    continue;
                }

                foreach (MonoBehaviour behaviour in prefab.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    if (behaviour == null)
                    {
                        problems.Add($"{path}: a script is missing.");
                        continue;
                    }

                    string where = $"{path} [{PathOf(behaviour.transform, prefab.transform)}] {behaviour.GetType().Name}";
                    if (behaviour is Text)
                    {
                        problems.Add($"{where}: legacy Text is not allowed.");
                    }

                    if (behaviour is TMP_Text text && text.font != font)
                    {
                        problems.Add($"{where}: the font must be {font.name}.");
                    }

                    if (behaviour is LocalizeStringEvent localize)
                    {
                        string key = localize.StringReference.TableEntryReference.Key;
                        if (localize.StringReference.TableReference.TableCollectionName != UiStrings.TableName || !keys.Contains(key))
                        {
                            problems.Add($"{where}: '{key}' is not a key of {UiStrings.TableName}.");
                        }
                    }

                    if (behaviour.GetType().Namespace == typeof(UIScreen).Namespace)
                    {
                        AddMissingReferences(behaviour, where, problems);
                    }
                }
            }

            return problems;
        }

        /// <summary>Every serialized object reference of a UI script must be set: the builder forgot it otherwise.</summary>
        static void AddMissingReferences(MonoBehaviour behaviour, string where, List<string> problems)
        {
            var serialized = new SerializedObject(behaviour);
            SerializedProperty property = serialized.GetIterator();
            bool enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = true;
                if (property.name == "m_Script")
                {
                    continue;
                }

                if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null)
                {
                    problems.Add($"{where}: '{property.propertyPath}' is not set.");
                }
                else if (property.isArray && property.propertyType != SerializedPropertyType.String && property.arraySize == 0)
                {
                    problems.Add($"{where}: '{property.propertyPath}' is empty.");
                }
            }
        }

        static string PathOf(Transform transform, Transform root)
        {
            var names = new List<string>();
            for (Transform t = transform; t != null && t != root; t = t.parent)
            {
                names.Add(t.name);
            }

            names.Reverse();
            return names.Count == 0 ? root.name : string.Join("/", names);
        }

        /// <summary>The root of a screen with its script, and the 1920x1080 frame everything is laid out in.</summary>
        static T Screen<T>(string name, Transform holder, out RectTransform frame)
            where T : UIScreen
        {
            RectTransform root = UiBuild.ScreenRoot(name, holder, out frame);
            return root.gameObject.AddComponent<T>();
        }
    }
}
