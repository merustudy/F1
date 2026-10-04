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
        public static readonly Color PartyTarget = Rgb(0x3F, 0x74, 0xB0);
        public static readonly Color Enemy = Rgb(0x70, 0x35, 0x2C);
        public static readonly Color Shield = Rgb(0x8F, 0xD3, 0xF4);
        public static readonly Color Burn = Rgb(0xF3, 0x9C, 0x12);
        /// <summary>The charge of an item's cell: dried blood on the bone cell (2026-10-04 Diablo kit).</summary>
        public static readonly Color Gauge = Rgb(0x8C, 0x3A, 0x2C);
        public static readonly Color Dead = Rgb(0x33, 0x33, 0x38);
        public static readonly Color Line = Rgb(0x55, 0x5C, 0x6B);
        /// <summary>The brass of the art: the row badge of a unit's plate, and the gold of the Diablo kit's titles and map paths.</summary>
        public static readonly Color Brass = Rgb(0xB8, 0x94, 0x4E);

        /// <summary>The blood of the Diablo kit (2026-10-04): the HP fill, and the lighter tone of the HP just lost that trails behind it.</summary>
        public static readonly Color Blood = Rgb(0x8A, 0x16, 0x18);
        public static readonly Color BloodLight = Rgb(0xD8, 0x6A, 0x5A);

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
