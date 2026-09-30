using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using TMPro;
using UnityEngine;

namespace F1.UI
{
    /// <summary>
    /// The party between battles: each member's HP, row and item slots, and the potions.
    /// Clicking a slot and then another swaps them; the row button switches front and rear.
    /// The node map and the reward screen both show it.
    /// </summary>
    public sealed class PartyBoardView : MonoBehaviour
    {
        [SerializeField] BoardMemberView _memberTemplate;
        [SerializeField] Transform _memberParent;
        [SerializeField] PotionSlotView _potionTemplate;
        [SerializeField] Transform _potionParent;
        [SerializeField] TMP_Text _detail;

        readonly List<BoardMemberView> _members = new List<BoardMemberView>();
        readonly List<PotionSlotView> _potions = new List<PotionSlotView>();
        int _selectedMember = -1;
        int _selectedSlot = -1;

        /// <summary>
        /// Lets the owning screen take over a slot click (member index, slot index). When it returns
        /// true the board does nothing; the reward screen uses this to place a chosen item.
        /// </summary>
        public Func<int, int, bool> SlotClickOverride { get; set; }

        /// <summary>Builds the member cards for the current expedition. Called once by the owning screen.</summary>
        public void Open()
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            for (int m = 0; m < expedition.Members.Count; m++)
            {
                BoardMemberView view = Instantiate(_memberTemplate, _memberParent);
                view.gameObject.SetActive(true);
                view.Build(expedition.Members[m].Items.Length);
                _members.Add(view);

                int member = m;
                view.RowButton.onClick.AddListener(() => OnRowClicked(member));
                for (int s = 0; s < view.Slots.Count; s++)
                {
                    int slot = s;
                    view.Slots[s].Button.onClick.AddListener(() => OnSlotClicked(member, slot));
                }
            }

            for (int i = 0; i < expedition.Potions.Length; i++)
            {
                PotionSlotView view = Instantiate(_potionTemplate, _potionParent);
                view.gameObject.SetActive(true);
                _potions.Add(view);
            }
        }

        public void ClearSelection()
        {
            _selectedMember = -1;
            _selectedSlot = -1;
        }

        public void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;

            for (int m = 0; m < _members.Count; m++)
            {
                ExpeditionMember member = expedition.Members[m];
                BattleRow other = member.Row == BattleRow.Front ? BattleRow.Rear : BattleRow.Front;
                _members[m].Show(member, manager.CanSetRow(m, other), m == _selectedMember ? _selectedSlot : -1);
            }

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), false, false);
            }

            EquippedItem selected = _selectedMember < 0 ? null : expedition.Members[_selectedMember].Items[_selectedSlot];
            _detail.text = selected == null ? string.Empty : UiText.ItemTitle(selected) + "\n" + UiText.ItemSummary(selected);
        }

        void OnRowClicked(int member)
        {
            ExpeditionManager manager = Managers.Expedition;
            BattleRow row = manager.Expedition.Members[member].Row;
            BattleRow other = row == BattleRow.Front ? BattleRow.Rear : BattleRow.Front;
            if (manager.CanSetRow(member, other))
            {
                manager.SetRow(member, other);
                Refresh();
            }
        }

        void OnSlotClicked(int member, int slot)
        {
            if (SlotClickOverride != null && SlotClickOverride(member, slot))
            {
                return;
            }

            if (_selectedMember < 0)
            {
                _selectedMember = member;
                _selectedSlot = slot;
            }
            else if (_selectedMember == member && _selectedSlot == slot)
            {
                ClearSelection();
            }
            else
            {
                Managers.Expedition.SwapItems(_selectedMember, _selectedSlot, member, slot);
                ClearSelection();
            }

            Refresh();
        }
    }
}
