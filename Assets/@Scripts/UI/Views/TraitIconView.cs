using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>A trait's icon alone (Slice D stage 37, Round 84: the party popup's stage cards): the name is on its card.</summary>
    public sealed class TraitIconView : TraitChipBase
    {
        [SerializeField] Image _rim;
        [SerializeField] TMP_Text _glyph;
        [SerializeField] float _iconSize;

        public override string Name => null;
        public override string Glyph => _glyph.text;
        public override Color RimColor => _rim.color;

        public override float WidthOf(TraitChip chip)
        {
            return _iconSize;
        }

        protected override void Fill(TraitChip chip, float width)
        {
            _rim.color = chip.Rim;
            _glyph.text = chip.Glyph;
            _glyph.color = chip.Rim;
        }
    }
}
