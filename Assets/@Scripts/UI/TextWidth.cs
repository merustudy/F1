using TMPro;

namespace F1.UI
{
    /// <summary>
    /// How wide a line of text is in a label's font and size, summed from the font's glyph advances (Slice D stage 37). TextMeshPro's own measure
    /// answers 0 for a label that has never been active — a clone not yet shown, a line of a window that is closed while the screen redraws — so
    /// the trait lines, which lay their chips out by the words' widths, measure here. Rich-text tags count nothing.
    /// </summary>
    public static class TextWidth
    {
        public static float Of(TMP_Text label, string text)
        {
            if (string.IsNullOrEmpty(text) || label.font == null)
            {
                return 0f;
            }

            TMP_FontAsset font = label.font;
            float scale = label.fontSize / font.faceInfo.pointSize * font.faceInfo.scale;
            float width = 0f;
            bool inTag = false;
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                if (label.richText && (inTag || c == '<'))
                {
                    inTag = c != '>';
                    continue;
                }

                uint code = c;
                if (char.IsHighSurrogate(c) && i + 1 < text.Length)
                {
                    code = (uint)char.ConvertToUtf32(c, text[i + 1]);
                    i++;
                }

                if (font.characterLookupTable.TryGetValue(code, out TMP_Character character))
                {
                    width += character.glyph.metrics.horizontalAdvance * scale;
                }
            }

            return width;
        }
    }
}
