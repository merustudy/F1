using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Probe
{
    // Investigation only, in a clone of the project: which import settings give an uncompressed texture.
    public static class TextureProbe
    {
        const string Tmp = "Assets/ProbeTmp";
        const string Figure = "Assets/@Art/Unit/Job/valkyrie.png";

        public static void Run()
        {
            var log = new StringBuilder("\nPROBE_BEGIN\n");
            log.AppendLine($"unity={Application.unityVersion} target={EditorUserBuildSettings.activeBuildTarget} " +
                $"overrideCompression={EditorUserBuildSettings.overrideTextureCompression} overrideMax={EditorUserBuildSettings.overrideMaxTextureSize} gfx={SystemInfo.graphicsDeviceType}");
            foreach (string path in new[] {
                Figure, "Assets/@Art/Pose/Job/valkyrie_attack.png", "Assets/@Art/Face/Job/valkyrie.png",
                "Assets/@Art/Item/sword.png", "Assets/@Art/Potion/healing_potion.png", "Assets/@Art/Background/Dungeon/abandoned_mine.png" })
                Report(log, path, path);
            string[] uiFrames = Directory.GetFiles("Assets/@Art/UI/Frame", "*.png");
            if (uiFrames.Length > 0) Report(log, uiFrames[0].Replace('\\', '/'), uiFrames[0].Replace('\\', '/'));

            Variant(log, "a_policy", "pad_672x896.png", i => { });
            Variant(log, "b_nomips", "pad_672x896.png", i => i.mipmapEnabled = false);
            Variant(log, "c_npot_to_nearest", "pad_672x896.png", i => i.npotScale = TextureImporterNPOTScale.ToNearest);
            Variant(log, "d_normal_quality", "pad_672x896.png", i => i.textureCompression = TextureImporterCompression.Compressed);
            Variant(log, "e_bc7_override", "pad_672x896.png", i => i.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Standalone", overridden = true, format = TextureImporterFormat.BC7, maxTextureSize = 1024, textureCompression = TextureImporterCompression.CompressedHQ }));
            Variant(log, "f_dxt5_override", "pad_672x896.png", i => i.SetPlatformTextureSettings(new TextureImporterPlatformSettings {
                name = "Standalone", overridden = true, format = TextureImporterFormat.DXT5, maxTextureSize = 1024, textureCompression = TextureImporterCompression.Compressed }));
            Variant(log, "g_default_type", "pad_672x896.png", i => i.textureType = TextureImporterType.Default);
            Variant(log, "h_alpha_not_transparency", "pad_672x896.png", i => i.alphaIsTransparency = false);
            Variant(log, "i_768x1024", "pad_768x1024.png", i => { });
            Variant(log, "j_1024x1024", "pad_1024x1024.png", i => { });
            Variant(log, "k_768x1024_nomips", "pad_768x1024.png", i => i.mipmapEnabled = false);
            log.AppendLine("PROBE_END");
            Debug.Log(log.ToString());
        }

        static void Variant(StringBuilder log, string name, string source, Action<TextureImporter> change)
        {
            string path = $"{Tmp}/{name}.png";
            File.Copy($"{Tmp}/{source}", path, true);
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            var policy = (TextureImporter)AssetImporter.GetAtPath(Figure);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var settings = new TextureImporterSettings();
            policy.ReadTextureSettings(settings);
            importer.SetTextureSettings(settings);
            importer.textureCompression = policy.textureCompression;
            importer.maxTextureSize = policy.maxTextureSize;
            change(importer);
            importer.SaveAndReimport();
            Report(log, name, path);
        }

        static void Report(StringBuilder log, string label, string path)
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            TextureImporterPlatformSettings standalone = importer.GetPlatformTextureSettings("Standalone");
            long bytes = UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(texture);
            log.AppendLine($"{label}: {texture.width}x{texture.height} mips={texture.mipmapCount} format={texture.format} " +
                $"auto={importer.GetAutomaticFormat("Standalone")} standaloneOverride={standalone.overridden}/{standalone.format} " +
                $"compression={importer.textureCompression} mipmapOn={importer.mipmapEnabled} npot={importer.npotScale} type={importer.textureType} " +
                $"alphaSource={importer.alphaSource} hasAlpha={importer.DoesSourceTextureHaveAlpha()} runtimeBytes={bytes}");
        }
    }
}
