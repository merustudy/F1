using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// A mercenary of the roster in the party popup's list (Round 76 D, 2026-10-17), as the barracks lists them: a small figure, the
    /// name, the job with the affliction the mercenary came home in, the level, the passive, the fatigue pips, and the party button —
    /// [넣기] puts the mercenary in the party's next row (off once the party is full), [빼기] takes it out. In the party the line is lit
    /// and tagged with its row. The pointer on the line opens its stat tooltip (Slice D stage 32, Round 81): the screen listens.
    /// </summary>
    public sealed class PartyListRowView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] Image _frame;
        [SerializeField] Image _portrait;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _level;
        [SerializeField] TMP_Text _passive;
        [SerializeField] TMP_Text _row;
        [SerializeField] UiBar[] _pips;
        [SerializeField] Button _button;
        [SerializeField] TMP_Text _buttonLabel;
        [SerializeField] Outline _hover;

        public string MercenaryId { get; set; }

        /// <summary>The pointer came onto the line, or left it (its button included).</summary>
        public event Action<PartyListRowView> PointerEntered;
        public event Action<PartyListRowView> PointerExited;

        /// <summary>The pointer is on the line: its rim lights in the target blue (Round 81's mock) while its tooltip is open.</summary>
        public bool Hovered => _hover.enabled;

        public void OnPointerEnter(PointerEventData eventData)
        {
            _hover.enabled = true;
            PointerEntered?.Invoke(this);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            _hover.enabled = false;
            PointerExited?.Invoke(this);
        }
        public bool InParty { get; private set; }
        public Button Button => _button;
        public Color FrameColor => _frame.color;
        public Sprite Portrait => _portrait.enabled ? _portrait.sprite : null;
        public string Name => _name.text;
        public string Job => _job.text;
        public string Level => _level.text;
        public string Passive => _passive.text;
        public string Row => _row.text;
        public string ButtonLabel => _buttonLabel.text;
        public IReadOnlyList<UiBar> Pips => _pips;

        /// <param name="art">The job's figure, or null for a job without one.</param>
        /// <param name="row">The row's tag while in the party, else null.</param>
        /// <param name="buttonWords">What the button does now: in, or out.</param>
        public void Show(
            Sprite art, string name, string jobLine, bool afflicted, string level, string passive, string row, bool inParty, bool canJoin,
            string buttonWords, int fatigue, int maxFatigue, int breakdown)
        {
            InParty = inParty;
            _frame.color = inParty ? UiPalette.Selected : UiPalette.PanelLight;
            _portrait.sprite = art;
            _portrait.enabled = art != null;
            _name.text = name;
            _job.text = jobLine;
            _job.color = afflicted ? UiPalette.FatigueDanger : UiPalette.TextDim;
            _level.text = level;
            _passive.text = passive;
            _row.text = row ?? string.Empty;
            _buttonLabel.text = buttonWords;
            _button.interactable = inParty || canJoin;
            FatiguePips.Show(_pips, fatigue, maxFatigue, breakdown);
        }
    }
}
