using System.Collections.Generic;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// A mercenary's traits in a row (Slice D stage 37, Round 84 안 A): the barracks' and the stagecoach's lines and the party popup's list show the
    /// icon and the name of each, the popup's stage cards the icons alone. There is no cap on traits (Design/16 판정 11), so what the given width
    /// cannot hold becomes one "+N" pill at the end, whose card names the rest.
    /// </summary>
    public sealed class TraitLineView : MonoBehaviour
    {
        [SerializeField] TraitChipBase[] _chips;
        [SerializeField] TraitMoreView _more;
        [SerializeField] float _gap;

        readonly List<TraitChip> _shown = new List<TraitChip>();
        readonly List<TraitChip> _hidden = new List<TraitChip>();

        /// <summary>The traits the line shows, in order, and those the more pill stands for.</summary>
        public IReadOnlyList<TraitChip> Shown => _shown;
        public IReadOnlyList<TraitChip> Hidden => _hidden;
        public IReadOnlyList<TraitChipBase> Chips => _chips;
        public TraitMoreView More => _more;

        /// <summary>The line as wide as it may grow (from its left).</summary>
        public void Show(IReadOnlyList<TraitChip> chips, float width)
        {
            _shown.Clear();
            _hidden.Clear();
            float x = 0f;
            int fit = 0;
            for (; fit < chips.Count && fit < _chips.Length; fit++)
            {
                int rest = chips.Count - fit - 1;
                float pill = rest > 0 ? _gap + _more.WidthOf(UiText.MoreChip(rest)) : 0f;
                float w = _chips[fit].WidthOf(chips[fit]);
                if (x + w + pill > width)
                {
                    break;
                }

                x += _chips[fit].Show(chips[fit], x) + _gap;
                _shown.Add(chips[fit]);
            }

            for (int i = fit; i < _chips.Length; i++)
            {
                _chips[i].Hide();
            }

            for (int i = fit; i < chips.Count; i++)
            {
                _hidden.Add(chips[i]);
            }

            if (_hidden.Count > 0)
            {
                _more.Show(UiText.MoreChip(_hidden), x);
            }
            else
            {
                _more.Hide();
            }
        }
    }
}
