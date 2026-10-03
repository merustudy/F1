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
            public Piece(string name, int border = 0, bool tiled = false)
            {
                Name = name;
                Border = border;
                Tiled = tiled;
            }

            /// <summary>True for a frame whose edges and middle are tiled instead of stretched: its border carries rivets or stitches that must keep their shape.</summary>
            public bool Tiled { get; }

            /// <summary>The folder under the UI art directory and the file without its extension: "Frame/panel".</summary>
            public string Name { get; }

            /// <summary>Pixels of the sprite that stay unstretched on every side. 0 for an icon.</summary>
            public int Border { get; }

            public string AssetPath => ArtDirectory + "/" + Name + ".png";
        }

        /// <summary>The dark leather panel behind a header, a strip of slots or a box. Tiled: rivets run along its edges.</summary>
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

        /// <summary>The leather bag behind a unit's item cells, as long as its board. Tiled: a stitch runs along its edges.</summary>
        public const string Bag = "Frame/bag";

        /// <summary>The dial of the storm clock in the middle of the board panel: a whole piece, never stretched (ui_piece).</summary>
        public const string Dial = "Frame/dial";

        /// <summary>The ring of the storm clock, filled radially as the storm comes. Drawn (same script).</summary>
        public const string Ring = "Icon/ring";

        /// <summary>A soft darkening towards the edges, stretched over the stage: the storm's dusk and the red of death's door. Drawn (same script).</summary>
        public const string Vignette = "Icon/vignette";

        public const string Shield = "Icon/shield";
        public const string Burn = "Icon/burn";
        public const string DeathsDoor = "Icon/deaths_door";
        public const string Storm = "Icon/storm";

        /// <summary>Every piece, with the border of each frame in the pixels of its sprite.</summary>
        public static readonly IReadOnlyList<Piece> All = new[]
        {
            new Piece(Panel, 40, tiled: true),
            new Piece(PlateParty, 44),
            new Piece(PlateEnemy, 44),
            new Piece(PlateDanger, 44),
            new Piece(PlateTarget, 44),
            new Piece(PlateLabel, 44),
            new Piece(Slot, 30),
            new Piece(SlotSelected, 30),
            new Piece(Button, 32),
            new Piece(Bag, 40, tiled: true),
            new Piece(Dial),
            new Piece(Ring),
            new Piece(Vignette),
            new Piece(Shield),
            new Piece(Burn),
            new Piece(DeathsDoor),
            new Piece(Storm),
        };

        /// <summary>Whether the piece of this name is tiled rather than stretched.</summary>
        public static bool IsTiled(string name)
        {
            foreach (Piece piece in All)
            {
                if (piece.Name == name)
                {
                    return piece.Tiled;
                }
            }

            return false;
        }

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
