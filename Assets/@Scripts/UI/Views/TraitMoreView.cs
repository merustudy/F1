using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The "+N" pill at the end of a trait line that cannot hold every trait (Slice D stage 37, Round 84 판정 3: there is no cap): a quiet rounded
    /// pill round the count; its card names the traits it stands for.
    /// </summary>
    public sealed class TraitMoreView : TraitChipBase
    {
        /// <summary>The pill's room on either side of its words.</summary>
        public const float Padding = 8f;

        [SerializeField] Image _pill;
        [SerializeField] TMP_Text _label;

        public override string Name => _label.text;
        public override string Glyph => null;
        public override Color RimColor => _pill.color;

        public override float WidthOf(TraitChip chip)
        {
            return Mathf.Ceil(TextWidth.Of(_label, chip.Name)) + 2f * Padding;
        }

        protected override void Fill(TraitChip chip, float width)
        {
            _label.text = chip.Name;
            _label.rectTransform.sizeDelta = new Vector2(width - 2f * Padding, _label.rectTransform.sizeDelta.y);
        }
    }
}
