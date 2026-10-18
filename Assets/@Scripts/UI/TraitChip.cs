using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// One trait as the screens show it (Slice D stage 37, Round 84): the first letter of its name in a square whose rim says what it is (the job's
    /// gold, a strength green, a weakness red), its name, and its card's two lines — what kind it is and what it does. The more pill of a line that
    /// cannot hold them all is a chip too: no letter, "+N", and the names it stands for on its card.
    /// </summary>
    public readonly struct TraitChip
    {
        /// <param name="moreCount">For the more pill, how many traits it stands for; 0 for a trait.</param>
        public TraitChip(string name, string glyph, Color rim, string kind, string effect, int moreCount = 0)
        {
            MoreCount = moreCount;
            Name = name;
            Glyph = glyph;
            Rim = rim;
            Kind = kind;
            Effect = effect;
        }

        public string Name { get; }

        /// <summary>The placeholder icon's letter (the name's first); null for the more pill.</summary>
        public string Glyph { get; }

        public Color Rim { get; }

        /// <summary>The card's second line: "타고난 특성 · 장점", "직업 특성 · 전사".</summary>
        public string Kind { get; }

        /// <summary>The card's third line: what the trait does ("명중 +5%p"); empty for the more pill.</summary>
        public string Effect { get; }

        /// <summary>How many traits the more pill stands for; 0 for a trait.</summary>
        public int MoreCount { get; }

        public bool IsMore => MoreCount > 0;
    }
}
