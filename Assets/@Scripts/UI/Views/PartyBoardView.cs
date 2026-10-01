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
    /// Members are listed from row 1 back. Clicking a slot and then another swaps them; Forward
    /// and Back trade places with the member in the next row.
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
                view.Forward.onClick.AddListener(() => OnMoveClicked(member, -1));
                view.Back.onClick.AddListener(() => OnMoveClicked(member, 1));
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
                _members[m].Show(
                    member,
                    manager.CanMoveToRow(m, member.Row - 1),
                    manager.CanMoveToRow(m, member.Row + 1),
                    m == _selectedMember ? _selectedSlot : -1);
            }

            OrderCards(expedition);

            for (int i = 0; i < _potions.Count; i++)
            {
                string potionId = expedition.Potions[i];
                _potions[i].Show(potionId == null ? null : data.Potions.Get(potionId), false, false);
            }

            EquippedItem selected = _selectedMember < 0 ? null : expedition.Members[_selectedMember].Items[_selectedSlot];
            _detail.text = selected == null ? string.Empty : UiText.ItemTitle(selected) + "\n" + UiText.ItemSummary(selected);
        }

        /// <summary>Lists the living from row 1 back, then the dead. Only the order on screen changes.</summary>
        void OrderCards(ExpeditionState expedition)
        {
            var order = new List<int>();
            for (int m = 0; m < _members.Count; m++)
            {
                order.Add(m);
            }

            // Ties fall back to the member order, so the result does not depend on the sort being stable.
            order.Sort((a, b) =>
            {
                ExpeditionMember x = expedition.Members[a];
                ExpeditionMember y = expedition.Members[b];
                if (x.Alive != y.Alive)
                {
                    return x.Alive ? -1 : 1;
                }

                return x.Alive && x.Row != y.Row ? x.Row.CompareTo(y.Row) : a.CompareTo(b);
            });

            foreach (int m in order)
            {
                _members[m].transform.SetAsLastSibling();
            }
        }

        /// <param name="step">-1 moves the member one row forward, 1 one row back.</param>
        void OnMoveClicked(int member, int step)
        {
            ExpeditionManager manager = Managers.Expedition;
            int row = manager.Expedition.Members[member].Row + step;
            if (manager.CanMoveToRow(member, row))
            {
                manager.MoveToRow(member, row);
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
