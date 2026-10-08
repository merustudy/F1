using System.Collections.Generic;
using System.Threading.Tasks;
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
    /// The loot of a won battle (Slice B stage 18; the reward choice before it). The party stands on the left as in battle; the drops
    /// are stacked on the right, one card each. A drop is picked first and then put at a cell of a board (whatever was there goes to the
    /// inventory; the same item merges) or taken straight into the inventory; its card stays as taken and the next drop can be picked.
    /// The last drop taken ends the loot; "leave" goes on with whatever still lies there. While a drop is picked, the boards enable
    /// only the cells that can take it.
    /// </summary>
    public sealed class LootScreen : UIScreen
    {
        [SerializeField] LootCardView _cardTemplate;
        [SerializeField] Transform _cardParent;
        [SerializeField] Button _leave;
        [SerializeField] Button _toInventory;
        [SerializeField] Button _inventoryToggle;
        [SerializeField] TMP_Text _inventoryToggleLabel;
        [SerializeField] PartySideView _party;
        [SerializeField] TMP_Text _coins;

        readonly List<LootCardView> _cards = new List<LootCardView>();

        /// <summary>The title each card showed while its drop lay there, so that a taken card can still say what it was.</summary>
        readonly List<string> _titles = new List<string>();

        int _picked = -1;

        ExpeditionArt _art;

        /// <summary>The art of the units is loaded before the screen opens, so nothing waits for it afterwards.</summary>
        public override async Task PrepareAsync()
        {
            _art = await ExpeditionArt.LoadAsync(Managers.Resource, Managers.Data.Data);
        }

        protected override void OnOpen()
        {
            ExpeditionState expedition = Managers.Expedition.Expedition;
            for (int i = 0; i < expedition.Loot.Count; i++)
            {
                LootCardView view = Instantiate(_cardTemplate, _cardParent);
                view.gameObject.SetActive(true);
                _cards.Add(view);
                _titles.Add(string.Empty);
                int slot = i;
                view.Button.onClick.AddListener(() => OnCardClicked(slot));
            }

            _leave.onClick.AddListener(OnLeave);
            _toInventory.onClick.AddListener(OnToInventory);
            _inventoryToggle.onClick.AddListener(OnInventoryToggle);
            _party.Open(_art);
            _party.CellClickOverride = PlaceSelectedItem;
            Managers.Sound.PlayMusic(MusicTrack.Dungeon);
        }

        public override void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            StaticData data = Managers.Data.Data;
            List<ItemOffer> loot = manager.Expedition.Loot;
            _coins.text = UiStrings.Get(UiKeys.Map.Coins, manager.Expedition.Coins);

            for (int i = 0; i < _cards.Count; i++)
            {
                ItemOffer drop = i < loot.Count ? loot[i] : null;
                if (drop == null)
                {
                    _cards[i].ShowTaken(UiStrings.Get(UiKeys.Loot.Item), _titles[i], UiStrings.Get(UiKeys.Loot.Taken));
                    continue;
                }

                var item = new EquippedItem(data.Items.Get(drop.Id), drop.Grade, tier: drop.Tier);
                bool selected = i == _picked;
                _titles[i] = UiText.ItemTitle(item);
                _cards[i].Show(
                    UiStrings.Get(UiKeys.Loot.Item),
                    _titles[i],
                    UiText.ItemDetails(item),
                    UiStrings.Get(selected ? UiKeys.Loot.Selected : UiKeys.Loot.Take),
                    selected,
                    true,
                    drop.Tier);
            }

            // With a drop picked, the boards show where it can go; otherwise the party side is its usual self.
            int picked = _picked;
            _party.ExternalCanPlace = picked < 0 ? null : (member, cell) => manager.CanTakeLoot(picked, member, cell);
            _party.ExternalMerges = picked < 0 ? null : (member, cell) => manager.LootMergesAt(picked, member, cell);
            _toInventory.interactable = picked >= 0 && manager.CanTakeLootToInventory(picked);
            _inventoryToggleLabel.text = UiStrings.Get(_party.InventoryOpen ? UiKeys.Board.InventoryHide : UiKeys.Board.InventoryShow);
            _party.Refresh();
        }

        /// <summary>The cards are built silent: a card picked or put down clicks; a drop taken sounds as put in.</summary>
        void OnCardClicked(int slot)
        {
            if (Managers.Expedition.Expedition.Loot[slot] == null)
            {
                return;
            }

            Managers.Sound.PlayEffect(SoundEffect.Button);
            _picked = _picked == slot ? -1 : slot;
            _party.ClearSelection();
            Refresh();
        }

        /// <summary>With a drop picked, a click on a cell that can take it puts the drop there (and sounds so; a cell that cannot, as a click).</summary>
        bool PlaceSelectedItem(int member, int cell)
        {
            if (_picked < 0)
            {
                return false;
            }

            ExpeditionManager manager = Managers.Expedition;
            if (manager.CanTakeLoot(_picked, member, cell))
            {
                manager.TakeLoot(_picked, member, cell);
                Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
                AfterTaking();
            }
            else
            {
                Managers.Sound.PlayEffect(SoundEffect.Button);
            }

            return true;
        }

        void OnToInventory()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_picked < 0 || !manager.CanTakeLootToInventory(_picked))
            {
                return;
            }

            manager.TakeLootToInventory(_picked);
            Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            AfterTaking();
        }

        /// <summary>After a drop was taken: the loot goes on with the next drop to pick, or, when it was the last, the node map comes.</summary>
        void AfterTaking()
        {
            _picked = -1;
            if (Managers.Expedition.Phase == GamePhase.Loot)
            {
                _party.ClearSelection();
                Refresh();
            }
            else
            {
                GoToCurrentPhase();
            }
        }

        void OnInventoryToggle()
        {
            _party.ToggleInventory();
            Refresh();
        }

        void OnLeave()
        {
            Managers.Expedition.LeaveLoot();
            GoToCurrentPhase();
        }
    }
}
