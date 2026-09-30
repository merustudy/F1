using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Data
{
    /// <summary>Unity entry point of the static data transform (menu and batch).</summary>
    public static class DataTransformMenu
    {
        public const string DoneToken = "F1_DATA_TRANSFORM_DONE";

        [MenuItem("F1/Data/Transform Static Data")]
        public static void TransformMenu()
        {
            Transform();
            Debug.Log(DoneToken);
        }

        /// <summary>Converts CSV sources to generated JSON. Throws when any source is invalid; nothing is written then.</summary>
        public static void Transform()
        {
            StaticDataFileStore store = CreateStore();
            TransformResult result = StaticDataTransformer.Transform(store.ReadSource);
            List<string> changed = store.WriteGenerated(result);
            if (changed.Count > 0)
            {
                AssetDatabase.Refresh();
                Debug.Log("Static data regenerated: " + string.Join(", ", changed));
            }
        }

        /// <summary>Generated files that no longer match their sources. Used by tests; writes nothing.</summary>
        public static List<string> FindStaleFiles()
        {
            StaticDataFileStore store = CreateStore();
            return store.FindStale(StaticDataTransformer.Transform(store.ReadSource));
        }

        public static StaticDataFileStore CreateStore()
        {
            // Application.dataPath is "<project>/Assets".
            return new StaticDataFileStore(Path.GetDirectoryName(Application.dataPath));
        }
    }
}
