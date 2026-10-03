using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace F1.Editor.Setup
{
    /// <summary>
    /// The art of the user interface: frames that are stretched as nine-slices, and small icons
    /// (Docs/Architecture/12_UI.md, "UI의 그림"). A screen's builder puts these sprites into its
    /// prefab, so no data names them and they have no address. The border of a frame (the part
    /// that is not stretched) is set here, because how a frame is stretched is the screen's
    /// business; ArtSetup writes it into the import settings. A sprite is drawn at twice the size
    /// it has on screen.
    /// </summary>
    public static class UiArt
    {
        public const string ArtDirectory = ArtSetup.ArtDirectory + "/UI";

        public readonly struct Piece
        {
            public Piece(string name, int border = 0)
            {
                Name = name;
                Border = border;
            }

            /// <summary>The folder under the UI art directory and the file without its extension: "Frame/panel".</summary>
            public string Name { get; }

            /// <summary>Pixels of the sprite that stay unstretched on every side. 0 for an icon.</summary>
            public int Border { get; }

            public string AssetPath => ArtDirectory + "/" + Name + ".png";
        }

        /// <summary>The dark panel behind a header, a strip of slots or a box.</summary>
        public const string Panel = "Frame/panel";

        /// <summary>The plate under a unit, in the color of its side or state.</summary>
        public const string PlateParty = "Frame/plate_party";
        public const string PlateEnemy = "Frame/plate_enemy";
        public const string PlateDanger = "Frame/plate_danger";
        public const string PlateTarget = "Frame/plate_target";

        /// <summary>The plate a line of text is written on so that it reads over the background.</summary>
        public const string PlateLabel = "Frame/plate_label";

        /// <summary>An item cell, a potion slot and the trough of a bar; and the same when it is the chosen one.</summary>
        public const string Slot = "Frame/slot";
        public const string SlotSelected = "Frame/slot_selected";

        /// <summary>A white button: the screen tints it.</summary>
        public const string Button = "Frame/button";

        public const string Shield = "Icon/shield";
        public const string Burn = "Icon/burn";
        public const string DeathsDoor = "Icon/deaths_door";
        public const string Storm = "Icon/storm";

        /// <summary>Every piece, with the border of each frame in the pixels of its sprite.</summary>
        public static readonly IReadOnlyList<Piece> All = new[]
        {
            new Piece(Panel, 40),
            new Piece(PlateParty, 44),
            new Piece(PlateEnemy, 44),
            new Piece(PlateDanger, 44),
            new Piece(PlateTarget, 44),
            new Piece(PlateLabel, 44),
            new Piece(Slot, 30),
            new Piece(SlotSelected, 30),
            new Piece(Button, 32),
            new Piece(Shield),
            new Piece(Burn),
            new Piece(DeathsDoor),
            new Piece(Storm),
        };

        /// <summary>The sprite of a piece. Its file must have been imported as a sprite (ArtSetup.Sync).</summary>
        public static Sprite Load(string name)
        {
            string path = ArtDirectory + "/" + name + ".png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException($"UI art '{name}' is not a sprite at {path}. Run F1/Setup/Sync Art.");
            }

            return sprite;
        }
    }
}
