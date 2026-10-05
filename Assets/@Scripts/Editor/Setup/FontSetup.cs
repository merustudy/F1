using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using F1.Data;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace F1.Editor.Setup
{
    /// <summary>
    /// Bakes the UI font asset. The atlas is static and holds exactly the characters that can reach
    /// the screen: ASCII, every UI string and every static data source. It is rebuilt only when that
    /// set changes, so the asset is never edited by hand and never changes while playing. Next to it
    /// lies the same font's material with a dark outline, kept in step with it (<see cref="OutlineMaterialPath"/>).
    /// </summary>
    public static class FontSetup
    {
        public const string DoneToken = "F1_FONT_SYNC_DONE";
        public const string SourceFontPath = "Assets/@Fonts/Source/Pretendard/Pretendard-Medium.ttf";
        public const string FontAssetDirectory = "Assets/@Fonts/TMP";
        public const string FontAssetPath = FontAssetDirectory + "/Pretendard-Medium SDF.asset";
        public const string FamilyName = "Pretendard";

        /// <summary>
        /// The font's material with a dark outline (the ink of the art), for words that stand on the stage with no plate behind
        /// them: the marks under a unit's feet and the names on the heads of the boards (2026-10-05 round 29). It is the font's own
        /// material with the outline on, made again from it whenever the font is synced, so it always reads the same atlas.
        /// </summary>
        public const string OutlineMaterialPath = FontAssetDirectory + "/Pretendard-Medium SDF Outline.mat";

        /// <summary>
        /// The outline's thickness, in the shader's units (0..1 of the distance the atlas keeps around a glyph). The outline
        /// straddles a glyph's edge, so the face grows by as much (dilate): the outline lies outside the letters, which keep
        /// their weight. (A thick outline alone ate the small letters from inside.)
        /// </summary>
        const float OutlineWidth = 0.3f;
        const float OutlineDilate = 0.3f;

        const int SamplingPointSize = 36;
        const int AtlasPadding = 4;
        const int AtlasSize = 1024;
        const uint FirstPrintableAscii = 0x20;
        const uint LastPrintableAscii = 0x7E;
        const uint Ellipsis = 0x2026;

        [MenuItem("F1/Setup/Sync Font Asset")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        /// <summary>The UI font asset. Throws when it has not been baked yet.</summary>
        public static TMP_FontAsset LoadFontAsset()
        {
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                throw new InvalidOperationException($"UI font asset not found at '{FontAssetPath}'. Run F1/Setup/Sync Font Asset.");
            }

            return fontAsset;
        }

        /// <summary>The outlined material of the UI font. Throws when it has not been made yet.</summary>
        public static Material LoadOutlineMaterial()
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (material == null)
            {
                throw new InvalidOperationException($"UI font outline material not found at '{OutlineMaterialPath}'. Run F1/Setup/Sync Font Asset.");
            }

            return material;
        }

        /// <summary>Every code point the UI can show, in ascending order.</summary>
        public static List<uint> BuildCorpus()
        {
            var corpus = new SortedSet<uint>();
            for (uint c = FirstPrintableAscii; c <= LastPrintableAscii; c++)
            {
                corpus.Add(c);
            }

            // TextMeshPro draws this itself where a text is cut off.
            corpus.Add(Ellipsis);

            foreach (UiStringRow row in LocalizationSetup.ReadSource().Rows)
            {
                foreach (string value in row.Values.Values)
                {
                    AddCodePoints(corpus, value);
                }
            }

            // Whole source files are read, not only the name columns: ids and numbers are ASCII anyway.
            foreach (StaticDataFiles.Entry file in StaticDataFiles.All)
            {
                string path = StaticDataFiles.SourceDirectory + "/" + file.SourceFileName;
                if (!File.Exists(path))
                {
                    throw new DataException($"{path}: file is missing.");
                }

                AddCodePoints(corpus, File.ReadAllText(path, Encoding.UTF8));
            }

            return corpus.ToList();
        }

        static void AddCodePoints(SortedSet<uint> corpus, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                int codePoint = char.ConvertToUtf32(text, i);
                if (char.IsHighSurrogate(text[i]))
                {
                    i++;
                }

                // Line breaks, tabs and the byte order mark are layout, not glyphs.
                if (codePoint < FirstPrintableAscii || codePoint == 0x7F || codePoint == 0xFEFF)
                {
                    continue;
                }

                corpus.Add((uint)codePoint);
            }
        }

        public static void Sync()
        {
            List<uint> corpus = BuildCorpus();
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null || !IsBakedFor(fontAsset, corpus))
            {
                if (fontAsset == null)
                {
                    fontAsset = Create();
                }

                Bake(fontAsset, corpus);
            }

            SyncOutlineMaterial(fontAsset);
        }

        /// <summary>The outlined material: the font's material copied, with the outline in the art's ink.</summary>
        static void SyncOutlineMaterial(TMP_FontAsset fontAsset)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            bool created = material == null;
            if (created)
            {
                material = new Material(fontAsset.material);
            }
            else
            {
                material.shader = fontAsset.material.shader;
                material.CopyPropertiesFromMaterial(fontAsset.material);
            }

            material.name = Path.GetFileNameWithoutExtension(OutlineMaterialPath);
            material.EnableKeyword(ShaderUtilities.Keyword_Outline);
            material.SetFloat("_OutlineWidth", OutlineWidth);
            material.SetFloat("_FaceDilate", OutlineDilate);
            material.SetColor("_OutlineColor", F1.UI.UiPalette.Ink);
            ShaderUtilities.GetShaderPropertyIDs();
            ShaderUtilities.UpdateShaderRatios(material);
            if (created)
            {
                AssetDatabase.CreateAsset(material, OutlineMaterialPath);
            }
            else
            {
                EditorUtility.SetDirty(material);
            }

            AssetDatabase.SaveAssetIfDirty(material);
        }

        static TMP_FontAsset Create()
        {
            var font = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (font == null)
            {
                throw new InvalidOperationException($"Source font not found at '{SourceFontPath}'.");
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(
                font,
                SamplingPointSize,
                AtlasPadding,
                GlyphRenderMode.SDFAA,
                AtlasSize,
                AtlasSize,
                AtlasPopulationMode.Dynamic,
                true);
            if (fontAsset == null)
            {
                throw new InvalidOperationException($"Could not create a font asset from '{SourceFontPath}'.");
            }

            string name = Path.GetFileNameWithoutExtension(FontAssetPath);
            Directory.CreateDirectory(FontAssetDirectory);
            AssetDatabase.CreateAsset(fontAsset, FontAssetPath);

            fontAsset.material.name = name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            fontAsset.atlasTextures[0].name = name + " Atlas";
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTextures[0], fontAsset);
            return fontAsset;
        }

        static void Bake(TMP_FontAsset fontAsset, List<uint> corpus)
        {
            // Characters are added in code point order so the same corpus always packs the same atlas.
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            fontAsset.ClearFontAssetData(true);
            fontAsset.TryAddCharacters(corpus.ToArray(), out uint[] missing);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;

            if (missing != null && missing.Length > 0)
            {
                throw new InvalidOperationException(
                    $"{FamilyName} has no glyph for: {string.Join(" ", missing.Select(Describe))}. Remove the character from the text.");
            }

            if (!HasInk(fontAsset))
            {
                throw new InvalidOperationException("The baked font atlas is empty.");
            }

            EditorUtility.SetDirty(fontAsset);
            EditorUtility.SetDirty(fontAsset.material);
            foreach (Texture2D texture in fontAsset.atlasTextures)
            {
                EditorUtility.SetDirty(texture);
            }

            AssetDatabase.SaveAssetIfDirty(fontAsset);
            Debug.Log($"Font atlas baked: {corpus.Count} characters, {fontAsset.atlasTextures.Length} texture(s).");
        }

        static bool IsBakedFor(TMP_FontAsset fontAsset, List<uint> corpus)
        {
            return fontAsset.atlasPopulationMode == AtlasPopulationMode.Static
                && CharactersOf(fontAsset).SequenceEqual(corpus)
                && HasInk(fontAsset);
        }

        static List<uint> CharactersOf(TMP_FontAsset fontAsset)
        {
            return fontAsset.characterTable.Select(c => c.unicode).Distinct().OrderBy(c => c).ToList();
        }

        /// <summary>False when no glyph was drawn into the atlas.</summary>
        static bool HasInk(TMP_FontAsset fontAsset)
        {
            Texture2D atlas = fontAsset.atlasTextures != null && fontAsset.atlasTextures.Length > 0 ? fontAsset.atlasTextures[0] : null;
            if (atlas == null || atlas.width != AtlasSize || atlas.height != AtlasSize || !atlas.isReadable)
            {
                return false;
            }

            foreach (byte value in atlas.GetRawTextureData<byte>())
            {
                if (value != 0)
                {
                    return true;
                }
            }

            return false;
        }

        static string Describe(uint codePoint)
        {
            return $"'{char.ConvertFromUtf32((int)codePoint)}' (U+{codePoint:X4})";
        }

        /// <summary>Every reason the font asset cannot show the current corpus.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();

            List<uint> corpus;
            try
            {
                corpus = BuildCorpus();
            }
            catch (DataException exception)
            {
                problems.Add(exception.Message);
                return problems;
            }

            var fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            if (fontAsset == null)
            {
                problems.Add($"UI font asset not found at '{FontAssetPath}'. Run F1/Setup/Sync Font Asset.");
                return problems;
            }

            if (fontAsset.faceInfo.familyName != FamilyName)
            {
                problems.Add($"Font family is '{fontAsset.faceInfo.familyName}', expected '{FamilyName}'.");
            }

            if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Static)
            {
                problems.Add("The font atlas must be static.");
            }

            if (!HasInk(fontAsset))
            {
                problems.Add("The font atlas is empty.");
            }

            var baked = new HashSet<uint>(CharactersOf(fontAsset));
            List<uint> missing = corpus.Where(c => !baked.Contains(c)).ToList();
            if (missing.Count > 0)
            {
                problems.Add($"Characters not in the atlas: {string.Join(" ", missing.Select(Describe))}. Run F1/Setup/Sync Font Asset.");
            }

            var wanted = new HashSet<uint>(corpus);
            List<uint> extra = baked.Where(c => !wanted.Contains(c)).OrderBy(c => c).ToList();
            if (extra.Count > 0)
            {
                problems.Add($"Characters in the atlas but not in the corpus: {string.Join(" ", extra.Select(Describe))}. Run F1/Setup/Sync Font Asset.");
            }

            var outline = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
            if (outline == null)
            {
                problems.Add($"UI font outline material not found at '{OutlineMaterialPath}'. Run F1/Setup/Sync Font Asset.");
            }
            else if (outline.mainTexture != fontAsset.atlasTexture || !outline.IsKeywordEnabled(ShaderUtilities.Keyword_Outline))
            {
                problems.Add($"The material at '{OutlineMaterialPath}' does not read the font's atlas with its outline on. Run F1/Setup/Sync Font Asset.");
            }

            return problems;
        }
    }
}
