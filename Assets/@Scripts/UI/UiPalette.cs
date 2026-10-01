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
        public static readonly Color Gauge = Rgb(0x6B, 0x5B, 0x1E);
        public static readonly Color Dead = Rgb(0x33, 0x33, 0x38);
        public static readonly Color Line = Rgb(0x55, 0x5C, 0x6B);
        public static readonly Color Icon = Rgb(0x6C, 0x78, 0x91);

        static Color Rgb(int r, int g, int b)
        {
            return new Color(r / 255f, g / 255f, b / 255f, 1f);
        }
    }
}
