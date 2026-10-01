using System.Collections.Generic;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>One party member on the party board: HP, row, the buttons that trade places and item slots.</summary>
    public sealed class BoardMemberView : MonoBehaviour
    {
        [SerializeField] Image _background;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _job;
        [SerializeField] TMP_Text _hp;
        [SerializeField] UiBar _hpBar;
        [SerializeField] TMP_Text _rowLabel;
        [SerializeField] Button _forward;
        [SerializeField] Button _back;
        [SerializeField] ItemSlotView _slotTemplate;
        [SerializeField] Transform _slotParent;

        const float MaxSlotWidth = 220f;

        readonly List<ItemSlotView> _slots = new List<ItemSlotView>();

        /// <summary>Moves the member one row towards the enemy.</summary>
        public Button Forward => _forward;

        /// <summary>Moves the member one row away from the enemy.</summary>
        public Button Back => _back;

        public IReadOnlyList<ItemSlotView> Slots => _slots;

        /// <summary>Creates one slot view per item slot, as wide as the row allows. Called once.</summary>
        public void Build(int slotCount)
        {
            var row = (RectTransform)_slotParent;
            var layout = row.GetComponent<HorizontalLayoutGroup>();
            float width = Mathf.Min(MaxSlotWidth, (row.rect.width - layout.spacing * (slotCount - 1)) / slotCount);

            for (int i = 0; i < slotCount; i++)
            {
                ItemSlotView slot = Instantiate(_slotTemplate, _slotParent);
                var rect = (RectTransform)slot.transform;
                rect.sizeDelta = new Vector2(width, rect.sizeDelta.y);
                slot.gameObject.SetActive(true);
                _slots.Add(slot);
            }
        }

        /// <param name="selectedSlot">Index of the highlighted slot, or -1.</param>
        public void Show(ExpeditionMember member, bool canMoveForward, bool canMoveBack, int selectedSlot)
        {
            _name.text = UiText.Mercenary(member.MercenaryId);
            _job.text = UiText.Job(member.JobId);
            _background.color = member.Alive ? UiPalette.PanelLight : UiPalette.Dead;

            if (member.Alive)
            {
                _hp.text = UiStrings.Get(UiKeys.Board.Hp, member.Hp, member.MaxHp);
                _hpBar.Set(member.Hp, member.MaxHp);
                _rowLabel.text = UiText.Row(member.Row);
            }
            else
            {
                _hp.text = UiStrings.Get(UiKeys.Board.Dead);
                _hpBar.Set(0, 1);
                _rowLabel.text = string.Empty;
            }

            _forward.gameObject.SetActive(member.Alive);
            _back.gameObject.SetActive(member.Alive);
            _forward.interactable = canMoveForward;
            _back.interactable = canMoveBack;
            for (int i = 0; i < _slots.Count; i++)
            {
                _slots[i].Show(member.Items[i], i == selectedSlot, member.Alive);
            }
        }
    }
}
