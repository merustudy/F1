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
    /// The reward choice after a won battle. An item is picked first and then put into a slot on
    /// the party board; a potion is taken with one click; or the reward is skipped.
    /// </summary>
    public sealed class RewardScreen : UIScreen
    {
        [SerializeField] RewardOptionView _optionTemplate;
        [SerializeField] Transform _optionParent;
        [SerializeField] Button _skip;
        [SerializeField] PartyBoardView _board;

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
            _board.Open();
            _board.SlotClickOverride = PlaceSelectedItem;
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

            _board.Refresh();
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
            _board.ClearSelection();
            Refresh();
        }

        /// <summary>With an item reward selected, a click on a living member's slot puts the item there.</summary>
        bool PlaceSelectedItem(int member, int slot)
        {
            if (_selectedOption < 0)
            {
                return false;
            }

            Managers.Expedition.TakeItemReward(_selectedOption, member, slot);
            GoToCurrentPhase();
            return true;
        }

        void OnSkip()
        {
            Managers.Expedition.SkipReward();
            GoToCurrentPhase();
        }
    }
}
