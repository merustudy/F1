using F1.Data;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// Colors of the placeholder UI. Prefab setup code and screens both read these, so a state
    /// (selected, disabled, dead) looks the same everywhere.
    /// </summary>
    public static class UiPalette
    {
        public static readonly Color Background = Rgb(0x12, 0x14, 0x1A);
        public static readonly Color Panel = Rgb(0x1E, 0x22, 0x2B);
        public static readonly Color PanelLight = Rgb(0x2A, 0x30, 0x3C);
        public static readonly Color Slot = Rgb(0x15, 0x18, 0x1F);
        public static readonly Color Overlay = new Color(0f, 0f, 0f, 0.75f);

        public static readonly Color Text = Rgb(0xEB, 0xEB, 0xE6);
        public static readonly Color TextDim = Rgb(0x9A, 0xA0, 0xAC);

        public static readonly Color Button = Rgb(0x3B, 0x6F, 0xB5);
        public static readonly Color ButtonQuiet = Rgb(0x3A, 0x41, 0x50);
        public static readonly Color Selected = Rgb(0x80, 0x66, 0x14);

        public static readonly Color Good = Rgb(0x58, 0xB3, 0x68);
        public static readonly Color Danger = Rgb(0xC0, 0x39, 0x2B);
        public static readonly Color Shield = Rgb(0x8F, 0xD3, 0xF4);
        public static readonly Color Burn = Rgb(0xF3, 0x9C, 0x12);

        /// <summary>The state line's chips of the effect vocabulary (Slice D stage 44, Round 90 "이걸로 가자 연결"): bleed in the blood's light, regen in the good green, thorns in the plain text white.</summary>
        public static readonly Color Bleed = Rgb(0xD8, 0x6A, 0x5A);
        public static readonly Color Regen = Rgb(0x58, 0xB3, 0x68);
        public static readonly Color Thorns = Rgb(0xEB, 0xEB, 0xE6);

        /// <summary>
        /// The state line's chips of the seven states (Slice D stage 45, Round 91 "연결", the colours of the approved sample `mock-chips-v2.png`): freeze
        /// in an icy blue, blind in a dusty violet, aim in a sand yellow, keen in a steel white, fury in a hot red, haste in a pale yellow, vulnerable in
        /// a worn orange.
        /// </summary>
        public static readonly Color Freeze = Rgb(0x9F, 0xD3, 0xF0);
        public static readonly Color Blind = Rgb(0xB9, 0x8F, 0xE0);
        public static readonly Color Aim = Rgb(0xF0, 0xD4, 0x7A);
        public static readonly Color Keen = Rgb(0xDC, 0xE6, 0xF0);
        public static readonly Color Fury = Rgb(0xF0, 0x6E, 0x5A);
        public static readonly Color Haste = Rgb(0xF3, 0xE2, 0x9A);
        public static readonly Color Vulnerable = Rgb(0xE0, 0xA0, 0x60);

        /// <summary>
        /// The marks under a unit's feet (2026-10-05 round 29): the rim around the HP bar that tells a shield, death's door or
        /// a potion's target, the light on the floor at a target's feet (the target's rim, faint), and a monster's name on the
        /// head of its board, a pale red so that the sides read apart (the party's names are Text).
        /// </summary>
        public static readonly Color RimShield = Rgb(0x6E, 0xA8, 0xE8);
        public static readonly Color RimDanger = Rgb(0xE2, 0x3C, 0x30);
        public static readonly Color RimTarget = Rgb(0x6E, 0xAA, 0xFF);
        public static readonly Color TargetLight = new Color(RimTarget.r, RimTarget.g, RimTarget.b, 0.6f);
        public static readonly Color EnemyName = Rgb(0xF2, 0x8C, 0x7E);
        /// <summary>The candle's gold (2026-10-04 round 18): the warm light a support item lights behind a figure (FigureView.Pulse).</summary>
        public static readonly Color ChargeEdge = Rgb(0xFF, 0xCE, 0x68);
        public static readonly Color Dead = Rgb(0x33, 0x33, 0x38);
        public static readonly Color Line = Rgb(0x55, 0x5C, 0x6B);
        /// <summary>The brass of the art: the row badge of a unit's plate, and the gold of the Diablo kit's titles and map paths.</summary>
        public static readonly Color Brass = Rgb(0xB8, 0x94, 0x4E);

        /// <summary>The blood of the Diablo kit (2026-10-04): the HP fill, and the lighter tone of the HP just lost that trails behind it.</summary>
        public static readonly Color Blood = Rgb(0x8A, 0x16, 0x18);
        public static readonly Color BloodLight = Rgb(0xD8, 0x6A, 0x5A);

        /// <summary>
        /// Fatigue (2026-10-06 round 32): a pale violet that no other mark uses, so the cost of equipment is not read as a burn's
        /// orange or the cooldown's gold. The words on the fatigue tag and the cost in an item's facts.
        /// </summary>
        public static readonly Color Fatigue = Rgb(0xCF, 0xBA, 0xF7);

        /// <summary>
        /// The lobby's fatigue pips (2026-10-06 round 33, C): a pip below the breakdown in the violet of the fatigue tag's rim, one past it
        /// in a red that keeps some of the violet; the words past the breakdown take the red too.
        /// </summary>
        public static readonly Color FatigueBar = Rgb(0x9E, 0x84, 0xDA);
        public static readonly Color FatigueDanger = Rgb(0xC8, 0x4B, 0x6E);

        /// <summary>
        /// The states of the breakdown (2026-10-06 round 36): an affliction's name, the lines of its banner and the lobby's words
        /// take the red of the fatigue past the threshold (FatigueDanger); a virtue's take this gold (the gold of the Gold tier's words, round 41).
        /// </summary>
        public static readonly Color Virtue = Rgb(0xF0, 0xC8, 0x5A);

        /// <summary>The colour of a state of the breakdown: red for an affliction, gold for a virtue.</summary>
        public static Color FatigueState(FatigueStateKind kind)
        {
            return kind == FatigueStateKind.Virtue ? Virtue : FatigueDanger;
        }

        /// <summary>The outline tone of the art, for a shape that stands next to it without a sprite.</summary>
        public static readonly Color Ink = Rgb(0x18, 0x09, 0x07);

        /// <summary>Multiplied into an icon that is shown but cannot be used now, as TextDim is for a name.</summary>
        public static readonly Color IconDim = Rgb(0x6E, 0x6E, 0x74);

        // ---- The grid board in Diablo II's look (round 49, Docs/Design/10_Art_Direction.md §5; Docs/Architecture/12_UI.md "격자 보드") ----

        /// <summary>An empty square of a bag or of the inventory: Diablo's black. The gaps between squares show <see cref="GridSquareLine"/>.</summary>
        public static readonly Color GridSquare = Rgb(0x09, 0x09, 0x0B);
        public static readonly Color GridSquareLine = Rgb(0x38, 0x38, 0x3C);

        /// <summary>The frame's squares outside every bag, drawn only while a bag is held: sunk stone with a dashed line (round 48's cue).</summary>
        public static readonly Color GridFrameFill = Rgb(0x24, 0x23, 0x21);
        public static readonly Color GridFrameLine = Rgb(0x78, 0x6C, 0x5C);

        /// <summary>
        /// An item's squares between battles, square by square with the grid's line between them (round 53): Diablo's dark blue (round 49);
        /// the held one a lighter blue; the one a held item would push out a dark gold. Solid: the colours round 49's see-through blues made
        /// over the well's grey, so a piece is the same blue wherever it lies (a board, the inventory, the merchant's grid; round 53,
        /// "인벤토리 아이템 배경색도 용병 보드의 파란색과 같은 색상으로"). In battle a piece has no ground (the user: "전투 화면에서는 아이템 뒤 파란색 배경 없음").
        /// </summary>
        public static readonly Color GridPiece = Rgb(0x1F, 0x26, 0x54);
        public static readonly Color GridPiecePicked = Rgb(0x36, 0x46, 0x93);
        public static readonly Color GridPieceDisplaced = Rgb(0x5C, 0x4F, 0x22);

        /// <summary>A merchant's good the party cannot buy now (round 57, the user: "살수없을때는 배경색을 검붉은 색으로"): its squares a dark red.</summary>
        public static readonly Color GridPieceUnaffordable = Rgb(0x52, 0x18, 0x18);

        /// <summary>The held item's squares while it is picked: no colour of its own any more (the piece's blue says it).</summary>
        public static readonly Color GridPicked = Color.clear;

        /// <summary>The ghost of a held thing: its squares one block, green where it fits (pushing one out or merging too), red where it cannot go; nothing else (round 49, "빨강 칠만"; round 53: no lines in it).</summary>
        public static readonly Color GhostFits = Rgb(0x1A, 0x96, 0x30);
        public static readonly Color GhostRefused = Rgb(0xB0, 0x1E, 0x1E);

        /// <summary>
        /// A star of an item on its star square (Slice B stage 20, Backpack Battles' stars, Docs/Design/10_Art_Direction.md §5): lit gold with a
        /// dark edge and a soft glow behind when a melee weapon lies there; else a hollow star, a dark fill in a pale edge.
        /// </summary>
        public static readonly Color StarLit = Rgb(0xEC, 0xC4, 0x5C);
        public static readonly Color StarLitEdge = Rgb(0x0A, 0x08, 0x06);
        public static readonly Color StarGlow = new Color(1f, 0xD6 / 255f, 0x6E / 255f, 0.45f);
        public static readonly Color StarHollow = Rgb(0x3A, 0x38, 0x34);
        public static readonly Color StarHollowEdge = Rgb(0x96, 0x92, 0x88);

        /// <summary>The words over a ghost and other small labels: Diablo's black box with a grey line, white words (red where it cannot go).</summary>
        public static readonly Color LabelBox = new Color(0f, 0f, 0f, 0.88f);
        public static readonly Color LabelLine = Rgb(0x46, 0x46, 0x46);

        /// <summary>A bag's rim round its squares: the stone with the bag's leather a little in it, sunk (dark top and left, light bottom and right).</summary>
        public static readonly Color BevelDark = Rgb(0x0A, 0x0A, 0x0A);
        public static readonly Color BevelLight = Rgb(0x70, 0x6A, 0x62);
        static readonly Color RimStone = Rgb(0x22, 0x21, 0x1F);

        /// <summary>The stage under the inventory window while it is open (round 52): a little darker, the figures and their marks still seen.
        /// The mockup's 35% in sRGB; blended in the project's linear space it takes about 55%.</summary>
        public static readonly Color InventoryShade = new Color(0f, 0f, 0f, 0.55f);

        /// <summary>The inventory's rim round its squares, and the dark of a small plate (a heading, the coins): Diablo's stone.</summary>
        public static readonly Color InventoryRim = RimStone;
        public static readonly Color StonePlate = Rgb(0x0E, 0x0D, 0x0C);

        /// <summary>Diablo's words on stone: bone for plain words, gold for a heading.</summary>
        public static readonly Color Bone = Rgb(0xCD, 0xC0, 0xA0);
        public static readonly Color DiabloGold = Rgb(0xC7, 0xB3, 0x77);

        /// <summary>The level-up's button and count (Slice D stage 35, Round 82): a warm gold with dark words.</summary>
        public static readonly Color LevelUp = Rgb(0xC9, 0x9A, 0x3A);
        public static readonly Color LevelUpText = Rgb(0x1C, 0x1A, 0x14);

        /// <summary>
        /// A trait's icon (Slice D stage 37, Round 84 안 1): the rim says what it does to its holder — the job's trait gold, a strength green, a
        /// weakness red (the stats' colours) — on a dark square; the more pill is the quiet button grey.
        /// </summary>
        public static readonly Color TraitJob = StarLit;
        public static readonly Color TraitStrength = Good;
        public static readonly Color TraitWeakness = BloodLight;
        public static readonly Color TraitIconFill = Rgb(0x14, 0x18, 0x1E);
        public static readonly Color TraitMore = ButtonQuiet;

        /// <summary>A unique item (stage 23): Diablo's unique gold for its name, its plate and the rim round its piece.</summary>
        public static readonly Color Unique = DiabloGold;

        /// <summary>
        /// The line between a recipe's pieces on a board (Slice D stage 50, Round 96): gold when the next won battle combines them, grey when a tier
        /// is short, a dark rim round both (the mockup's colours).
        /// </summary>
        public static readonly Color LinkReady = Rgb(0xF2, 0xC9, 0x4C);
        public static readonly Color LinkShort = Rgb(0x8E, 0x8E, 0x8A);
        public static readonly Color LinkRim = Rgb(0x14, 0x10, 0x08);

        /// <summary>The gold that spreads over a base when its recipe merges (Round 96 V1 "금빛 번짐").</summary>
        public static readonly Color MergeGold = Rgb(0xFF, 0xD6, 0x5A);

        /// <summary>The plate under the band naming the unique a won battle made (Round 96): dark, its rim the unique's gold.</summary>
        public static readonly Color UniquePlate = new Color(0x12 / 255f, 0x14 / 255f, 0x1C / 255f, 0.9f);

        /// <summary>The shade over the panel's buttons while a unique merges (Round 96 "막음"): dark and solid, the words with them.</summary>
        public static readonly Color LockShade = new Color(0f, 0f, 0f, 0.5f);

        /// <summary>
        /// The one colour of a tier, for its pips and its word (round 49 Diablo II's rarity colours; round 69 the user's ladder on the pips —
        /// 1성 the magic blue, 2성 silver, 3성 the rare yellow — and the words follow the pips, so a tier reads the same everywhere). Common
        /// is Diablo's white, used where a Common item's name is coloured.
        /// </summary>
        public static Color Rarity(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return Rgb(0x70, 0x70, 0xFF);
                case ItemTier.Tier2: return Rgb(0xD3, 0xDB, 0xE4);
                case ItemTier.Tier3: return Rgb(0xFF, 0xFF, 0x6E);
                default: return Rgb(0xEE, 0xEE, 0xEE);
            }
        }

        /// <summary>Diablo's tooltip: a black box, a grey line, effects in the magic blue; a bag's name in gold.</summary>
        public static readonly Color TooltipFill = new Color(0f, 0f, 0f, 0.86f);
        public static readonly Color TooltipLine = Rgb(0x40, 0x40, 0x40);
        public static readonly Color TooltipEffect = Rgb(0x70, 0x70, 0xFF);
        public static readonly Color BagName = Rgb(0xC7, 0xB3, 0x77);

        /// <summary>Battle (round 48, the fifth ask's 안 2): the item's dark before it charges, and how much brighter it flashes as it fires.</summary>
        public const float CooldownDark = 0.4f;
        public const float FireBrighten = 0.3f;

        /// <summary>The leather of a bag, by bag id (the round 48 mockups' colours); any other bag is the pack's brown.</summary>
        public static Color Bag(string bagId)
        {
            switch (bagId)
            {
                case "leather_pouch": return Rgb(0x46, 0x52, 0x3A);
                case "belt_pouch": return Rgb(0x6E, 0x36, 0x2A);
                default: return Rgb(0x60, 0x40, 0x26);
            }
        }

        /// <summary>The rim of an enemy item's well in battle (an enemy has no bags): the stone a little red.</summary>
        public static readonly Color EnemyRim = Color.Lerp(RimStone, Rgb(0x6E, 0x28, 0x20), 0.45f);

        /// <summary>A bag's rim (round 49, "처음 디아블로 안의 돌 테"): the stone with 45% of the bag's leather in it.</summary>
        public static Color BagRim(string bagId)
        {
            return Color.Lerp(RimStone, Bag(bagId), 0.45f);
        }

        static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
