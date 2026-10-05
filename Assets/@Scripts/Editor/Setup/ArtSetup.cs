using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using F1.Core;
using F1.Data;
using F1.Editor.Data;
using UnityEditor;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

namespace F1.Editor.Setup
{
    /// <summary>
    /// The art the game shows. The static data names the art of the units, the dungeons and the
    /// items (the Figure of a job or an enemy, the Background of a dungeon, the Icon of an item):
    /// the address says where its file is. The art of the user interface is listed by UiArt and
    /// goes into the screen prefabs.
    /// Every file is imported by the policy of its kind, which lives here. Art is wired by copying
    /// an approved picture to its place and naming it; no entry and no import setting is made by
    /// hand (Docs/Architecture/13_ART_PIPELINE.md).
    /// </summary>
    public static class ArtSetup
    {
        public const string DoneToken = "F1_ART_SYNC_DONE";
        public const string ArtDirectory = "Assets/@Art";
        public const string ArtGroup = AddressablesSetup.GroupPrefix + "Art";

        /// <summary>How a kind of art is imported. Everything else is the same for all art.</summary>
        readonly struct ImportPolicy
        {
            public ImportPolicy(bool cutOut, bool mipMaps, bool compressed, int maxTextureSize, float pixelsPerUnit, int border = 0)
            {
                CutOut = cutOut;
                MipMaps = mipMaps;
                Compressed = compressed;
                MaxTextureSize = maxTextureSize;
                PixelsPerUnit = pixelsPerUnit;
                Border = border;
            }

            /// <summary>Drawn on a transparent canvas. An opaque picture has no alpha to keep.</summary>
            public bool CutOut { get; }

            /// <summary>For art that is shown much smaller than it is drawn.</summary>
            public bool MipMaps { get; }

            /// <summary>
            /// The texture comes out block-compressed: a quarter of the memory of an uncompressed one, or less. Unity compresses
            /// a mip-mapped texture only when both of its sides are powers of two and keeps any other size uncompressed (RGBA32),
            /// whatever compression is asked for; a kind whose canvas is not such a size says so here (2026-10-05).
            /// </summary>
            public bool Compressed { get; }

            public int MaxTextureSize { get; }
            public float PixelsPerUnit { get; }

            /// <summary>Pixels on every side that are not stretched when the sprite is stretched as a nine-slice.</summary>
            public int Border { get; }
        }

        /// <summary>
        /// A full-body figure: its canvas is 672x896 and it is shown much smaller than that. Uncompressed: the canvas (3:4) is
        /// not a power of two on both sides.
        /// </summary>
        static readonly ImportPolicy Figure = new ImportPolicy(cutOut: true, mipMaps: true, compressed: false, maxTextureSize: 1024, pixelsPerUnit: 100f);

        /// <summary>A background: its canvas is 2304x1536 and it is shown at about that size, so it is not scaled down.</summary>
        static readonly ImportPolicy Scene = new ImportPolicy(cutOut: false, mipMaps: false, compressed: true, maxTextureSize: 4096, pixelsPerUnit: 100f);

        /// <summary>A piece of the user interface is drawn at twice the size it has on screen. Uncompressed, like a figure: its sizes are not powers of two.</summary>
        static ImportPolicy Interface(int border)
        {
            return new ImportPolicy(cutOut: true, mipMaps: true, compressed: false, maxTextureSize: 512, pixelsPerUnit: 200f, border: border);
        }

        /// <summary>An item's icon is drawn at twice the size of its cell, like an icon of the interface.</summary>
        static readonly ImportPolicy Icon = Interface(border: 0);

        /// <summary>A unit's face, cut out of its figure at twice the size of its place in the board panel, like an icon.</summary>
        static readonly ImportPolicy Face = Interface(border: 0);

        /// <summary>
        /// A unit's attack or hit pose: the figure canvas in the middle of a canvas three times as wide and an eighth deeper
        /// (2016x1008), in a file of 2048x1024 (scaled by 64/63, powers of two) so that it is compressed (2026-10-05).
        /// Kept at its size, so that it is as sharp as the figure it stands in for (2026-10-04).
        /// </summary>
        static readonly ImportPolicy Pose = new ImportPolicy(cutOut: true, mipMaps: true, compressed: true, maxTextureSize: 2048, pixelsPerUnit: 100f);

        readonly struct ArtFile
        {
            public ArtFile(string name, string assetPath, ImportPolicy policy, AddressEntry? entry)
            {
                Name = name;
                AssetPath = assetPath;
                Policy = policy;
                Entry = entry;
            }

            /// <summary>What a message calls the file: its address, or its name in UiArt.</summary>
            public string Name { get; }
            public string AssetPath { get; }
            public ImportPolicy Policy { get; }

            /// <summary>The addressable entry of art the data names. The art of the interface has none.</summary>
            public AddressEntry? Entry { get; }
        }

        [MenuItem("F1/Setup/Sync Art")]
        public static void SyncMenu()
        {
            Sync();
            AssetDatabase.SaveAssets();
            Debug.Log(DoneToken);
        }

        /// <summary>
        /// Where the file of an art address is: its segments are the folders and the last one is
        /// the file, named after the data id ("unit/enemy/goblin-raider" is
        /// "Assets/@Art/Unit/Enemy/goblin_raider.png").
        /// </summary>
        public static string AssetPath(string address)
        {
            if (!LogicalAddress.IsValid(address))
            {
                throw new ArgumentException($"'{address}' is not a valid logical address.", nameof(address));
            }

            string[] segments = address.Split('/');
            var path = new StringBuilder(ArtDirectory);
            for (int i = 0; i < segments.Length - 1; i++)
            {
                path.Append('/');
                foreach (string word in segments[i].Split('-'))
                {
                    path.Append(char.ToUpperInvariant(word[0])).Append(word, 1, word.Length - 1);
                }
            }

            return path.Append('/').Append(segments[segments.Length - 1].Replace('-', '_')).Append(".png").ToString();
        }

        /// <summary>
        /// One entry per piece of art the generated static data names: the figures of the jobs,
        /// those of the enemies, the faces cut out of those figures, the backgrounds of the
        /// dungeons, the icons of the items, then the bottles of the potions.
        /// </summary>
        public static List<AddressEntry> Entries()
        {
            return Files().Where(f => f.Entry.HasValue).Select(f => f.Entry.Value).ToList();
        }

        /// <summary>Every art file of the game: what the data names, then the art of the interface.</summary>
        static List<ArtFile> Files()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            StaticData data = StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));

            // A unit that has a figure has a face too: it is cut out of the figure, and its address follows the figure's.
            // A job or an enemy that has a figure has its attack and hit poses as well, drawn after the figure.
            IEnumerable<(string Address, ImportPolicy Policy)> named = data.Jobs.Ordered.Select(j => (j.Figure, Figure))
                .Concat(data.Enemies.Ordered.Select(e => (e.Figure, Figure)))
                .Concat(data.Jobs.Ordered.Select(j => (j.Face, Face)))
                .Concat(data.Enemies.Ordered.Select(e => (e.Face, Face)))
                .Concat(data.Jobs.Ordered.Select(j => (j.AttackPose, Pose)))
                .Concat(data.Jobs.Ordered.Select(j => (j.HitPose, Pose)))
                .Concat(data.Enemies.Ordered.Select(e => (e.AttackPose, Pose)))
                .Concat(data.Enemies.Ordered.Select(e => (e.HitPose, Pose)))
                .Concat(data.Dungeons.Ordered.Select(d => (d.Background, Scene)))
                .Concat(data.Items.Ordered.Select(i => (i.Icon, Icon)))
                .Concat(data.Potions.Ordered.Select(p => (p.Icon, Icon)));

            var files = new List<ArtFile>();
            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach ((string address, ImportPolicy policy) in named)
            {
                // Two definitions may share a picture. An address that is not valid is listed as it is: FindProblems names it.
                if (address != null && seen.Add(address))
                {
                    string path = LogicalAddress.IsValid(address) ? AssetPath(address) : ArtDirectory + "/" + address;
                    files.Add(new ArtFile(address, path, policy, new AddressEntry(path, address, ResourceScope.Expedition, ArtGroup)));
                }
            }

            foreach (UiArt.Piece piece in UiArt.All)
            {
                files.Add(new ArtFile(piece.Name, piece.AssetPath, Interface(piece.Border), null));
            }

            return files;
        }

        /// <summary>Brings every art file to the import policy of its kind. Files already there are not touched.</summary>
        public static void Sync()
        {
            foreach (ArtFile file in Files())
            {
                var importer = AssetImporter.GetAtPath(file.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    throw new InvalidOperationException($"Art '{file.Name}' has no image at {file.AssetPath}.");
                }

                if (PolicyProblems(importer, file.Policy).Count > 0)
                {
                    ApplyPolicy(importer, file.Policy);
                    importer.SaveAndReimport();
                }
            }
        }

        /// <summary>Returns a description of everything that does not match: missing files, import settings, files nothing names.</summary>
        public static List<string> FindProblems()
        {
            var problems = new List<string>();
            var listed = new HashSet<string>(StringComparer.Ordinal);
            foreach (ArtFile file in Files())
            {
                listed.Add(file.AssetPath);
                if (file.Entry.HasValue && !LogicalAddress.IsValid(file.Name))
                {
                    problems.Add($"{file.Name}: the data names art by something that is not a valid logical address.");
                    continue;
                }

                if (!File.Exists(file.AssetPath))
                {
                    problems.Add($"{file.Name}: image is missing ({file.AssetPath}).");
                    continue;
                }

                var importer = AssetImporter.GetAtPath(file.AssetPath) as TextureImporter;
                if (importer == null)
                {
                    problems.Add($"{file.Name}: {file.AssetPath} is not imported as a texture.");
                    continue;
                }

                List<string> policyProblems = PolicyProblems(importer, file.Policy);
                foreach (string problem in policyProblems)
                {
                    problems.Add($"{file.Name}: {problem}. Run F1/Setup/Sync Art.");
                }

                if (policyProblems.Count == 0 && file.Policy.Compressed)
                {
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(file.AssetPath);
                    if (!GraphicsFormatUtility.IsCompressedFormat(texture.graphicsFormat))
                    {
                        problems.Add($"{file.Name}: stored uncompressed ({texture.format}, {texture.width}x{texture.height}). " +
                            "Unity compresses a mip-mapped picture only when both sides are powers of two.");
                    }
                }
            }

            if (Directory.Exists(ArtDirectory))
            {
                foreach (string file in Directory.GetFiles(ArtDirectory, "*.png", SearchOption.AllDirectories))
                {
                    string path = file.Replace('\\', '/');
                    if (!listed.Contains(path))
                    {
                        problems.Add($"{path}: nothing names this image: no Figure (or its face or poses), Background or Icon (of an item or a potion) in the static data and no piece of UiArt.");
                    }
                }
            }

            return problems;
        }

        /// <summary>The import policy of art. A full rect mesh because a UI Image does not use a tight one.</summary>
        static void ApplyPolicy(TextureImporter importer, ImportPolicy policy)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = policy.PixelsPerUnit;
            importer.spriteBorder = BorderOf(policy);
            importer.alphaIsTransparency = policy.CutOut;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = policy.MipMaps;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.maxTextureSize = policy.MaxTextureSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
        }

        static List<string> PolicyProblems(TextureImporter importer, ImportPolicy policy)
        {
            var problems = new List<string>();
            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);

            Check(problems, importer.textureType == TextureImporterType.Sprite, "texture type must be Sprite");
            Check(problems, importer.spriteImportMode == SpriteImportMode.Single, "sprite mode must be Single");
            Check(problems, Mathf.Approximately(importer.spritePixelsPerUnit, policy.PixelsPerUnit), $"pixels per unit must be {policy.PixelsPerUnit}");
            Check(problems, importer.spriteBorder == BorderOf(policy), $"sprite border must be {policy.Border} on every side");
            Check(problems, importer.alphaIsTransparency == policy.CutOut, policy.CutOut ? "alpha must be transparency" : "alpha must not be transparency");
            Check(problems, importer.sRGBTexture, "must be an sRGB texture");
            Check(problems, importer.mipmapEnabled == policy.MipMaps, policy.MipMaps ? "mip maps must be on" : "mip maps must be off");
            Check(problems, !importer.isReadable, "must not be readable");
            Check(problems, importer.wrapMode == TextureWrapMode.Clamp, "wrap mode must be Clamp");
            Check(problems, importer.filterMode == FilterMode.Bilinear, "filter mode must be Bilinear");
            Check(problems, importer.npotScale == TextureImporterNPOTScale.None, "non power of two size must be kept");
            Check(problems, importer.maxTextureSize == policy.MaxTextureSize, $"max size must be {policy.MaxTextureSize}");
            Check(problems, importer.textureCompression == TextureImporterCompression.CompressedHQ, "compression must be high quality");
            Check(problems, settings.spriteMeshType == SpriteMeshType.FullRect, "sprite mesh must be Full Rect");
            return problems;
        }

        static Vector4 BorderOf(ImportPolicy policy)
        {
            return new Vector4(policy.Border, policy.Border, policy.Border, policy.Border);
        }

        static void Check(List<string> problems, bool ok, string rule)
        {
            if (!ok)
            {
                problems.Add(rule);
            }
        }
    }
}
