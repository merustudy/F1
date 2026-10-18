using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A trait with its name on a trait line (Slice D stage 37, Round 84 안 A): the placeholder icon — the name's first letter in a dark square,
    /// its rim in the trait's colour (판정 1, 안 1) — and the name beside it.
    /// </summary>
    public sealed class TraitChipView : TraitChipBase
    {
        /// <summary>Between the icon and the name.</summary>
        public const float NameGap = 6f;

        [SerializeField] Image _rim;
        [SerializeField] TMP_Text _glyph;
        [SerializeField] TMP_Text _name;
        [SerializeField] float _iconSize;

        public override string Name => _name.text;
        public override string Glyph => _glyph.text;
        public override Color RimColor => _rim.color;

        public override float WidthOf(TraitChip chip)
        {
            return _iconSize + NameGap + Mathf.Ceil(TextWidth.Of(_name, chip.Name));
        }

        protected override void Fill(TraitChip chip, float width)
        {
            _rim.color = chip.Rim;
            _glyph.text = chip.Glyph;
            _glyph.color = chip.Rim;
            _name.text = chip.Name;
            _name.rectTransform.sizeDelta = new Vector2(width - _iconSize - NameGap, _name.rectTransform.sizeDelta.y);
        }
    }
}
