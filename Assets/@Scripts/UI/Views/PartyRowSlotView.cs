using System.Collections.Generic;
using F1.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A row of the party on the party popup's stage (Round 76 D, 2026-10-17): the member's figure standing on the floor as the node
    /// map's party side stands it, and under the feet the row with the name and the level, the job with the member's state, the
    /// fatigue pips every screen shows, and the moves — a row to the front, a row to the back (trading places with whoever stands
    /// there). Between the pips and the moves, the six stats in two columns (Slice D stage 32, Round 81). The traits' icons stand after the job
    /// (Slice D stage 37, Round 84). An empty row shows a dim place with the words and, under it, how to fill the row; it has no stats, traits or moves.
    /// </summary>
    public sealed class PartyRowSlotView : MonoBehaviour
    {
        [SerializeField] FigureView _figure;
        [SerializeField] GameObject _empty;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _detail;
        [SerializeField] UiBar[] _pips;
        [SerializeField] TMP_Text[] _stats;
        [SerializeField] TraitLineView _traits;
        [SerializeField] float _detailRight;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;

        /// <summary>Who stands in the row, or null while it is empty.</summary>
        public string MercenaryId { get; private set; }

        public FigureView Figure => _figure;
        public bool IsEmpty => _empty.activeSelf;
        public string Name => _name.text;
        public string Level => _level.text;
        public string Detail => _detail.text;
        public IReadOnlyList<UiBar> Pips => _pips;

        /// <summary>The six stats as pairs "힘 12", in <see cref="StatBlock.All"/>'s order; empty in an empty row.</summary>
        public IReadOnlyList<TMP_Text> Stats => _stats;
        public Button Forward => _forward;

        /// <summary>The traits' icons after the job (Slice D stage 37).</summary>
        public TraitLineView Traits => _traits;

        /// <summary>Between the job's words and the first icon.</summary>
        public const float TraitGap = 10f;
        public Button Back => _back;

        public void ShowEmpty(string hint)
        {
            MercenaryId = null;
            _figure.gameObject.SetActive(false);
            _empty.SetActive(true);
            _name.text = string.Empty;
            _level.text = string.Empty;
            _detail.text = hint;
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(false);
            }

            foreach (TMP_Text stat in _stats)
            {
                stat.text = string.Empty;
            }

            _traits.gameObject.SetActive(false);
            _forward.gameObject.SetActive(false);
            _back.gameObject.SetActive(false);
        }

        /// <param name="art">The job's figure, or null for a job without one (the placeholder stands in).</param>
        /// <param name="stats">The mercenary's stats (Slice D stage 32).</param>
        /// <param name="traits">Its traits (Slice D stage 37): icons after the job's words, as many as the line's end holds.</param>
        public void Show(string mercenaryId, Sprite art, string name, string level, string detail, int fatigue, int maxFatigue, int breakdown, StatBlock stats,
            IReadOnlyList<TraitChip> traits, bool canForward, bool canBack)
        {
            for (int i = 0; i < StatBlock.All.Length; i++)
            {
                _stats[i].text = UiText.StatPair(StatBlock.All[i], stats[StatBlock.All[i]]);
            }

            MercenaryId = mercenaryId;
            _empty.SetActive(false);
            _figure.gameObject.SetActive(true);
            _figure.Show(art);
            _name.text = name;
            _level.text = level;
            _detail.text = detail;
            var line = (RectTransform)_traits.transform;
            float left = _detail.rectTransform.anchoredPosition.x + Mathf.Ceil(TextWidth.Of(_detail, detail)) + TraitGap;
            line.anchoredPosition = new Vector2(left, line.anchoredPosition.y);
            _traits.gameObject.SetActive(true);
            _traits.Show(traits, _detailRight - left);
            foreach (UiBar pip in _pips)
            {
                pip.gameObject.SetActive(true);
            }

            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
            _forward.gameObject.SetActive(true);
            _back.gameObject.SetActive(true);
            _forward.interactable = canForward;
            _back.interactable = canBack;
        }
    }
}
