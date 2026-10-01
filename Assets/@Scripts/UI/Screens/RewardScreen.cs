using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The reward choice after a won battle. The party stands on the left as in battle; the rewards
    /// are stacked on the right. An item is picked first and then put at a cell of a board (whatever
    /// was there goes to the inventory) or taken straight into the inventory; a potion is taken with
    /// one click; or the reward is skipped. While an item is picked, the boards enable only the
    /// cells that can take it.
    /// </summary>
    public sealed class RewardScreen : UIScreen
    {
        [SerializeField] RewardOptionView _optionTemplate;
        [SerializeField] Transform _optionParent;
        [SerializeField] Button _skip;
        [SerializeField] Button _toInventory;
        [SerializeField] Button _inventoryToggle;
        [SerializeField] TMP_Text _inventoryToggleLabel;
        [SerializeField] PartySideView _party;

        readonly List<RewardOptionView> _options = new List<RewardOptionView>();
        int _selectedOption = -1;

        protected override void OnOpen()
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            for (int i = 0; i < expedition.PendingRewards.Count; i++)
            {
                RewardOptionView view = Instantiate(_optionTemplate, _optionParent);
                view.gameObject.SetActive(true);
                _options.Add(view);
                int option = i;
                view.Button.onClick.AddListener(() => OnOptionClicked(option));
            }

            _skip.onClick.AddListener(OnSkip);
            _toInventory.onClick.AddListener(OnToInventory);
            _inventoryToggle.onClick.AddListener(OnInventoryToggle);
            _party.Open();
            _party.CellClickOverride = PlaceSelectedItem;
        }

        public override void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            StaticData data = Managers.Data.Data;
            List<RewardOption> rewards = manager.Expedition.PendingRewards;

            for (int i = 0; i < _options.Count; i++)
            {
                RewardOption reward = rewards[i];
                if (reward.Kind == RewardKind.Item)
                {
                    var item = new EquippedItem(data.Items.Get(reward.Id), reward.Grade);
                    bool selected = i == _selectedOption;
                    _options[i].Show(
                        UiStrings.Get(UiKeys.Reward.Item),
                        UiText.ItemTitle(item),
                        UiText.ItemDetails(item),
                        UiStrings.Get(selected ? UiKeys.Reward.Selected : UiKeys.Reward.Select),
                        selected,
                        true);
                }
                else
                {
                    PotionData potion = data.Potions.Get(reward.Id);
                    bool canTake = manager.CanTakePotionReward;
                    _options[i].Show(
                        UiStrings.Get(UiKeys.Reward.Potion),
                        UiText.Name(potion.Name),
                        UiText.PotionDetails(potion),
                        UiStrings.Get(canTake ? UiKeys.Reward.TakePotion : UiKeys.Reward.PotionFull),
                        false,
                        canTake);
                }
            }

            // With an item picked, the boards show where it can go; otherwise the party side is its usual self.
            int option = _selectedOption;
            _party.ExternalCanPlace = option < 0 ? null : (member, cell) => manager.CanPlaceReward(option, member, cell);
            _toInventory.interactable = option >= 0 && manager.CanTakeRewardToInventory(option);
            _inventoryToggleLabel.text = UiStrings.Get(_party.InventoryOpen ? UiKeys.Board.InventoryHide : UiKeys.Board.InventoryShow);
            _party.Refresh();
        }

        void OnOptionClicked(int option)
        {
            ExpeditionManager manager = Managers.Expedition;
            RewardOption reward = manager.Expedition.PendingRewards[option];
            if (reward.Kind == RewardKind.Potion)
            {
                if (manager.CanTakePotionReward)
                {
                    manager.TakePotionReward(option);
                    GoToCurrentPhase();
                }

                return;
            }

            _selectedOption = _selectedOption == option ? -1 : option;
            _party.ClearSelection();
            Refresh();
        }

        /// <summary>With an item reward picked, a click on a cell that can take it puts the item there.</summary>
        bool PlaceSelectedItem(int member, int cell)
        {
            if (_selectedOption < 0)
            {
                return false;
            }

            ExpeditionManager manager = Managers.Expedition;
            if (manager.CanPlaceReward(_selectedOption, member, cell))
            {
                manager.TakeItemReward(_selectedOption, member, cell);
                GoToCurrentPhase();
            }

            return true;
        }

        void OnToInventory()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_selectedOption < 0 || !manager.CanTakeRewardToInventory(_selectedOption))
            {
                return;
            }

            manager.TakeItemRewardToInventory(_selectedOption);
            GoToCurrentPhase();
        }

        void OnInventoryToggle()
        {
            _party.ToggleInventory();
            Refresh();
        }

        void OnSkip()
        {
            Managers.Expedition.SkipReward();
            GoToCurrentPhase();
        }
    }
}
