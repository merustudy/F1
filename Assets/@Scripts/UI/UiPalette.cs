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
        public static readonly Color Party = Rgb(0x2C, 0x4A, 0x70);
        public static readonly Color Enemy = Rgb(0x70, 0x35, 0x2C);
        public static readonly Color Shield = Rgb(0x8F, 0xD3, 0xF4);
        public static readonly Color Burn = Rgb(0xF3, 0x9C, 0x12);

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
        /// <summary>
        /// The cooldown of an item's cell as light (2026-10-04 round 18, the candle's gold): the smoky dark over the part
        /// not charged yet (and over an empty cell of a battle board), the gold on the charged part of the cell, and the
        /// gold of the line and the glow at the front of the charge.
        /// </summary>
        public static readonly Color ChargeDark = Rgb(0x1E, 0x10, 0x06);
        public static readonly Color ChargeLight = Rgb(0xFF, 0xB8, 0x48);
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

        /// <summary>
        /// The tiers of an item (2026-10-07 round 41, palette P3): the outline around an icon, the stars on the tier tag, the stripe
        /// of a loot card and the mark of a cell an item would merge into take the tier's colour — copper, silver and gold for
        /// the three tiers above Common, deep enough to read on a bone cell; a tier's name in words takes the lighter tone, which
        /// reads on the dark panels. Common has no mark (a Common cell is the plain cell) and its word is a plain bone tone, used
        /// only where a tier word is needed ("일반 → 동" at the camp).
        /// </summary>
        public static readonly Color TierBronze = Rgb(0xA8, 0x68, 0x3A);
        public static readonly Color TierSilver = Rgb(0x9A, 0xA7, 0xB8);
        public static readonly Color TierGold = Rgb(0xD4, 0xA2, 0x32);
        public static readonly Color TierCommonText = Rgb(0xC9, 0xC2, 0xB0);
        public static readonly Color TierBronzeText = Rgb(0xD5, 0x9A, 0x66);
        public static readonly Color TierSilverText = Rgb(0xD3, 0xDB, 0xE4);
        public static readonly Color TierGoldText = Rgb(0xF0, 0xC8, 0x5A);

        /// <summary>The colour of a tier's marks (outline, stars, stripe, merge mark). Clear for Common, which has none.</summary>
        public static Color TierMark(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return TierBronze;
                case ItemTier.Silver: return TierSilver;
                case ItemTier.Gold: return TierGold;
                default: return Color.clear;
            }
        }

        /// <summary>The colour of a tier's name in words.</summary>
        public static Color TierText(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return TierBronzeText;
                case ItemTier.Silver: return TierSilverText;
                case ItemTier.Gold: return TierGoldText;
                default: return TierCommonText;
            }
        }

        /// <summary>The outline tone of the art, for a shape that stands next to it without a sprite.</summary>
        public static readonly Color Ink = Rgb(0x18, 0x09, 0x07);

        /// <summary>Text written on the light pieces of the camp kit (the canvas pockets, the parchment), and its dimmed tone.</summary>
        public static readonly Color InkText = Rgb(0x2A, 0x1A, 0x12);
        public static readonly Color InkTextDim = Rgb(0x6B, 0x55, 0x40);

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
        /// An item's squares as one piece between battles: Diablo's dark blue (round 49); the held one a lighter blue; the one a held item
        /// would push out a dark gold. In battle a piece has no ground (the user: "전투 화면에서는 아이템 뒤 파란색 배경 없음").
        /// </summary>
        public static readonly Color GridPiece = new Color(0x16 / 255f, 0x20 / 255f, 0x58 / 255f, 0.82f);
        public static readonly Color GridPiecePicked = new Color(0x36 / 255f, 0x48 / 255f, 0x9C / 255f, 0.86f);
        public static readonly Color GridPieceDisplaced = new Color(0x60 / 255f, 0x52 / 255f, 0x1C / 255f, 0.86f);

        /// <summary>The held item's squares while it is picked: no colour of its own any more (the piece's blue says it).</summary>
        public static readonly Color GridPicked = Color.clear;

        /// <summary>The ghost of a held thing: the squares green where it fits (pushing one out or merging too), red where it cannot go; nothing else (round 49, "빨강 칠만").</summary>
        public static readonly Color GhostFits = Rgb(0x1A, 0x96, 0x30);
        public static readonly Color GhostRefused = Rgb(0xB0, 0x1E, 0x1E);

        /// <summary>The words over a ghost and other small labels: Diablo's black box with a grey line, white words (red where it cannot go).</summary>
        public static readonly Color LabelBox = new Color(0f, 0f, 0f, 0.88f);
        public static readonly Color LabelLine = Rgb(0x46, 0x46, 0x46);

        /// <summary>A bag's rim round its squares: the stone with the bag's leather a little in it, sunk (dark top and left, light bottom and right).</summary>
        public static readonly Color BevelDark = Rgb(0x0A, 0x0A, 0x0A);
        public static readonly Color BevelLight = Rgb(0x70, 0x6A, 0x62);
        static readonly Color RimStone = Rgb(0x22, 0x21, 0x1F);

        /// <summary>The inventory's rim round its squares, and the dark of a small plate (a heading, the coins): Diablo's stone.</summary>
        public static readonly Color InventoryRim = RimStone;
        public static readonly Color StonePlate = Rgb(0x0E, 0x0D, 0x0C);

        /// <summary>Diablo's words on stone: bone for plain words, gold for a heading.</summary>
        public static readonly Color Bone = Rgb(0xCD, 0xC0, 0xA0);
        public static readonly Color DiabloGold = Rgb(0xC7, 0xB3, 0x77);

        /// <summary>Diablo II's rarity colours for the tiers (round 49): Common white, Bronze magic blue, Silver rare yellow, Gold unique gold.</summary>
        public static Color Rarity(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return Rgb(0x70, 0x70, 0xFF);
                case ItemTier.Silver: return Rgb(0xFF, 0xFF, 0x6E);
                case ItemTier.Gold: return Rgb(0xC7, 0xB3, 0x77);
                default: return Rgb(0xEE, 0xEE, 0xEE);
            }
        }

        /// <summary>Diablo's tooltip: a black box, a grey line, effects in the magic blue; a bag's name in gold.</summary>
        public static readonly Color TooltipFill = new Color(0f, 0f, 0f, 0.86f);
        public static readonly Color TooltipLine = Rgb(0x40, 0x40, 0x40);
        public static readonly Color TooltipEffect = Rgb(0x70, 0x70, 0xFF);
        public static readonly Color BagName = Rgb(0xC7, 0xB3, 0x77);

        /// <summary>An item that cannot be used where its owner stands, in battle: grey (it has no ground there).</summary>
        public static readonly Color GridPieceUnusable = Color.clear;

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
