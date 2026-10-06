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
        /// take the red of the fatigue past the threshold (FatigueDanger); a virtue's take this gold (the gold of the Gold tier's words).
        /// </summary>
        public static readonly Color Virtue = Rgb(0xF7, 0xC8, 0x4A);

        /// <summary>The colour of a state of the breakdown: red for an affliction, gold for a virtue.</summary>
        public static Color FatigueState(FatigueStateKind kind)
        {
            return kind == FatigueStateKind.Virtue ? Virtue : FatigueDanger;
        }

        /// <summary>
        /// The tiers of an item (2026-10-06 round 35, A): the rim of a cell, the stripe of a reward card and the mark of a cell an
        /// item would merge into take the tier's colour; its name in words takes the lighter tone, which reads on the dark panels.
        /// Bronze is never a rim (a Bronze cell is the plain cell), only a word.
        /// </summary>
        public static readonly Color TierBronze = Rgb(0xB0, 0x6E, 0x3A);
        public static readonly Color TierSilver = Rgb(0x8C, 0x9C, 0xB2);
        public static readonly Color TierGold = Rgb(0xE2, 0xA2, 0x1E);
        public static readonly Color TierDiamond = Rgb(0x2E, 0xC4, 0xE8);
        public static readonly Color TierBronzeText = Rgb(0xD8, 0x92, 0x58);
        public static readonly Color TierSilverText = Rgb(0xD5, 0xDE, 0xEA);
        public static readonly Color TierGoldText = Rgb(0xF7, 0xC8, 0x4A);
        public static readonly Color TierDiamondText = Rgb(0x6F, 0xE3, 0xF8);

        /// <summary>The colour of a tier's rim, stripe or mark.</summary>
        public static Color TierRim(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Silver: return TierSilver;
                case ItemTier.Gold: return TierGold;
                case ItemTier.Diamond: return TierDiamond;
                default: return TierBronze;
            }
        }

        /// <summary>The colour of a tier's name in words.</summary>
        public static Color TierText(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Silver: return TierSilverText;
                case ItemTier.Gold: return TierGoldText;
                case ItemTier.Diamond: return TierDiamondText;
                default: return TierBronzeText;
            }
        }

        /// <summary>The outline tone of the art, for a shape that stands next to it without a sprite.</summary>
        public static readonly Color Ink = Rgb(0x18, 0x09, 0x07);

        /// <summary>Text written on the light pieces of the camp kit (the canvas pockets, the parchment), and its dimmed tone.</summary>
        public static readonly Color InkText = Rgb(0x2A, 0x1A, 0x12);
        public static readonly Color InkTextDim = Rgb(0x6B, 0x55, 0x40);

        /// <summary>Multiplied into an icon that is shown but cannot be used now, as TextDim is for a name.</summary>
        public static readonly Color IconDim = Rgb(0x6E, 0x6E, 0x74);

        static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
