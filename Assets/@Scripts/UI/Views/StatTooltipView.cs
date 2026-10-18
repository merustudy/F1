using System.Collections.Generic;
using F1.Data;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The stat tooltip of the party popup's list (Slice D stage 32, Round 81 판정 4): while the pointer is on a line, a black card at the
    /// line's left — the item card's look, its notch on the right edge pointing at the line — with "이름 · 직업 Lv N" and the six stats in
    /// three columns and two rows. It takes no clicks.
    /// </summary>
    public sealed class StatTooltipView : MonoBehaviour
    {
        /// <summary>How far left of the line the card's notch stands.</summary>
        const float Gap = 14f;

        [SerializeField] TMP_Text _title;
        [SerializeField] TMP_Text[] _stats;

        /// <summary>Whose stats the card shows, or null while it is hidden.</summary>
        public string MercenaryId { get; private set; }
        public bool IsShown => gameObject.activeSelf;
        public string Title => _title.text;
        public IReadOnlyList<TMP_Text> Stats => _stats;

        /// <param name="line">The list line the pointer is on: the card stands at its left, centred on it.</param>
        public void Show(string mercenaryId, RectTransform line, string title, StatBlock stats)
        {
            MercenaryId = mercenaryId;
            _title.text = title;
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                _stats[i].text = UiText.StatPair(StatBlock.All[i], stats[StatBlock.All[i]]);
            }

            var corners = new Vector3[4];
            line.GetWorldCorners(corners);
            var parent = (RectTransform)transform.parent;
            Vector3 leftMiddle = parent.InverseTransformPoint((corners[0] + corners[1]) / 2f);
            transform.localPosition = new Vector3(leftMiddle.x - Gap, leftMiddle.y, 0f);
            gameObject.SetActive(true);
            transform.SetAsLastSibling();
        }

        public void Hide()
        {
            MercenaryId = null;
            gameObject.SetActive(false);
        }
    }
}
