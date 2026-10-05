using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Probe
{
    // Investigation only, in a clone: each variant of a picture drawn by the GPU at the sizes the game draws it, read back.
    public static class TextureCompare
    {
        const string Tmp = "Assets/ProbeTmp";

        public static void Run()
        {
            string outDir = Environment.GetEnvironmentVariable("PROBE_OUT");
            Directory.CreateDirectory(outDir);
            var log = new StringBuilder("\nCOMPARE_BEGIN\n");
            foreach (string path in Directory.GetFiles(Tmp, "*.png"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.mipmapEnabled = !name.Contains("_nomips");
                importer.isReadable = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.maxTextureSize = 2048;
                importer.textureCompression = name.EndsWith("raw", StringComparison.Ordinal) ? TextureImporterCompression.Uncompressed
                    : name.EndsWith("_crunchn", StringComparison.Ordinal) ? TextureImporterCompression.Compressed : TextureImporterCompression.CompressedHQ;
                importer.crunchedCompression = name.Contains("_crunch");
                importer.compressionQuality = 100;
                importer.SaveAndReimport();
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                log.AppendLine($"{name}: {texture.width}x{texture.height} {texture.format} mips={texture.mipmapCount} bytes={UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture)}");
                foreach (Vector2Int size in Sizes(name))
                {
                    var rt = new RenderTexture(size.x, size.y, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                    Graphics.Blit(texture, rt);
                    var read = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false, false);
                    RenderTexture.active = rt;
                    read.ReadPixels(new Rect(0, 0, size.x, size.y), 0, 0);
                    read.Apply();
                    RenderTexture.active = null;
                    File.WriteAllBytes(Path.Combine(outDir, $"{name}@{size.x}x{size.y}.png"), read.EncodeToPNG());
                    rt.Release();
                }
            }

            log.AppendLine("COMPARE_END");
            Debug.Log(log.ToString());
        }

        static Vector2Int[] Sizes(string name)
        {
            if (name.StartsWith("fig", StringComparison.Ordinal)) return new[] { new Vector2Int(225, 300), new Vector2Int(338, 450), new Vector2Int(450, 600) };
            if (name.StartsWith("pose", StringComparison.Ordinal)) return new[] { new Vector2Int(675, 338), new Vector2Int(1013, 506), new Vector2Int(1350, 675) };
            return new[] { new Vector2Int(164, 94), new Vector2Int(328, 188) };
        }
    }
}
