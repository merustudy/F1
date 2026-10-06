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

        /// <summary>The carved stone panel behind a header, a box, the board panel's table top and the node map's tablet (2026-10-04 Diablo kit). Tiled: a band of gothic arches runs along its edges.</summary>
        public const string Panel = "Frame/panel";
        public const string Table = "Frame/table";
        public const string Tablet = "Frame/tablet";

        /// <summary>The iron belt the potion slots sit on. Tiled: rivets run along its edges.</summary>
        public const string Belt = "Frame/belt";

        /// <summary>
        /// The head of a unit's board (2026-10-05 round 29): a slim strip of blackened iron with a thin gold line inside its edge and
        /// a rivet at each end, and on its left end the gold stud the row is written on. The stud is a whole piece.
        /// </summary>
        public const string NameTag = "Frame/name_tag";
        public const string GoldStud = "Frame/gold_stud";

        /// <summary>The plate a line of text is written on so that it reads over the background.</summary>
        public const string PlateLabel = "Frame/plate_label";

        /// <summary>An item cell of a unit's inventory panel: a plain bone cell with a thin dark line (drawn: Archive/14-ui-diablo/draw_pieces.py); and the same in gold when it is the chosen one.</summary>
        public const string Slot = "Frame/slot";
        public const string SlotSelected = "Frame/slot_selected";

        /// <summary>
        /// The fatigue tag (2026-10-06 round 32, B1): a plum pill with a violet rim. On a party-side cell whose item costs
        /// fatigue it says "+1"; on the head of the board it says the total. Round ends: its border is half its height, so
        /// it stretches sideways only. Drawn (Archive/32-equipment-fatigue/draw_tag.py).
        /// </summary>
        public const string FatigueTag = "Frame/fatigue_tag";

        /// <summary>
        /// The rim of an item cell at a tier above Bronze (2026-10-06 round 35, A): a white band with a dark hairline inside,
        /// tinted with the tier's colour by the cell. Drawn (Archive/35-tiers/draw_rim.py); its border is the band and the line.
        /// </summary>
        public const string TierRim = "Frame/tier_rim";

        /// <summary>The trough of a bar: a dark iron recess (the HP bar under a unit's feet).</summary>
        public const string Trough = "Frame/trough";

        /// <summary>A potion slot: a square pocket of blackened iron on the belt; and the same in gold when the potion is the chosen one.</summary>
        public const string PotionSlot = "Frame/potion_slot";
        public const string PotionSlotSelected = "Frame/potion_slot_selected";

        /// <summary>A white button: the screen tints it.</summary>
        public const string Button = "Frame/button";

        /// <summary>The inventory panel of blackened iron behind a unit's item cells, as long as its board.</summary>
        public const string Bag = "Frame/bag";

        /// <summary>
        /// The storm candle in the middle of the board panel: the skull holder, the body that burns down (filled from the
        /// bottom), its flame, its molten top and the smoke when it is out. Whole pieces; the top and the smoke are drawn
        /// (Archive/14-ui-diablo/draw_pieces.py).
        /// </summary>
        public const string CandleHolder = "Frame/candle_holder";
        public const string CandleBody = "Frame/candle_body";
        public const string CandleFlame = "Frame/candle_flame";
        public const string CandleTop = "Icon/candle_top";
        public const string Smoke = "Icon/smoke";

        /// <summary>A heap of skulls on the stone of the board panel. A whole piece.</summary>
        public const string Skulls = "Frame/skulls";

        /// <summary>
        /// The grave a fallen mercenary turns into for a moment in battle, the same for everyone: a wooden cross in a cairn of
        /// grey stones (2026-10-04 round 21, C). A whole piece, drawn on the canvas every figure shares (type prop), so it
        /// stands on the floor line of the figure it replaces.
        /// </summary>
        public const string Grave = "Frame/grave";

        /// <summary>The iron chains hanging at the ends of the board panel (drawn), and the warm light behind the candle's flame (drawn, Archive/13-ui-decor).</summary>
        public const string Chain = "Icon/chain";
        public const string Glow = "Icon/glow";

        /// <summary>The markers of the node map: a battle (crossed swords) and the boss (a crowned skull).</summary>
        public const string NodeBattle = "Icon/node_battle";
        public const string NodeBoss = "Icon/node_boss";

        /// <summary>The markers of the long expedition's nodes (2026-10-06 round 34): an elite (the battle's swords on a red diamond) and a camp (crossed logs and a flame). Drawn (Archive/34-long-map/draw_icons.py).</summary>
        public const string NodeElite = "Icon/node_elite";
        public const string NodeCamp = "Icon/node_camp";

        /// <summary>A soft darkening towards the edges, clear in the middle, stretched over the stage (the red of death's door) and over the whole screen (the Diablo kit's gloom). Drawn (Archive/16-candle-light/draw_pieces.py, which turned round 09's inside-out ramp the right way).</summary>
        public const string Vignette = "Icon/vignette";

        /// <summary>
        /// The candle's light on the stage (2026-10-04 mockup B): the darkness around it and its warm light on the background,
        /// each the upper half of a disc around the flame. Drawn (Archive/16-candle-light/draw_pieces.py); the battle
        /// screen stretches and tints them (UiPrefabSetup.Battle, CandleView).
        /// </summary>
        public const string CandleDark = "Icon/candle_dark";
        public const string CandleWarm = "Icon/candle_warm";

        /// <summary>
        /// The soft ramp of an item's cooldown light (2026-10-04 round 18): white, clear on the left and opaque on the
        /// right. The item's cell tints and stretches it as the glow behind the front of the charge and as the soft left
        /// end of the dark. Drawn (Archive/18-cooldown-light/draw_pieces.py).
        /// </summary>
        public const string ChargeRamp = "Icon/charge_ramp";

        /// <summary>
        /// The effect bursts of the moment of a breakdown (2026-10-06 round 38, "B"): the ink burst of an affliction and of the collapse, the
        /// light burst of a virtue, laid behind the unit with the glow in the state's colour. Drawn in round 38 (`Archive/38-breakdown-fx`),
        /// kept at 512 from the 1024 raws.
        /// </summary>
        public const string InkBurst = "Icon/ink_burst";
        public const string LightBurst = "Icon/light_burst";

        public const string Shield = "Icon/shield";
        public const string Burn = "Icon/burn";
        public const string DeathsDoor = "Icon/deaths_door";
        public const string Storm = "Icon/storm";

        /// <summary>Every piece, with the border of each frame in the pixels of its sprite.</summary>
        public static readonly IReadOnlyList<Piece> All = new[]
        {
            new Piece(Panel, 40, tiled: true),
            new Piece(Table, 40, tiled: true),
            new Piece(Tablet, 40, tiled: true),
            new Piece(Belt, 40, tiled: true),
            new Piece(NameTag, 30),
            new Piece(GoldStud),
            new Piece(PlateLabel, 44),
            new Piece(Slot, 16),
            new Piece(SlotSelected, 16),
            new Piece(FatigueTag, 20),
            new Piece(TierRim, 12),
            new Piece(Trough, 30),
            new Piece(PotionSlot, 30),
            new Piece(PotionSlotSelected, 30),
            new Piece(Button, 32),
            new Piece(Bag, 40),
            new Piece(CandleHolder),
            new Piece(CandleBody),
            new Piece(CandleFlame),
            new Piece(CandleTop),
            new Piece(Smoke),
            new Piece(Skulls),
            new Piece(Grave),
            new Piece(Chain),
            new Piece(Glow),
            new Piece(NodeBattle),
            new Piece(NodeBoss),
            new Piece(NodeElite),
            new Piece(NodeCamp),
            new Piece(Vignette),
            new Piece(CandleDark),
            new Piece(CandleWarm),
            new Piece(ChargeRamp),
            new Piece(InkBurst),
            new Piece(LightBurst),
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
