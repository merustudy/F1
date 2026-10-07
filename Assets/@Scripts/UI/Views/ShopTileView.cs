using F1.Data;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>How an offer of the shop stands on its tile (round 44).</summary>
    public enum ShopTileState
    {
        /// <summary>For sale, and the coins cover it.</summary>
        OnSale,
        /// <summary>The offer the player picked: the boards wait for the cell to put it in.</summary>
        Picked,
        /// <summary>For sale, but the coins do not cover it (or no potion slot is empty for a potion): dimmed, its price red.</summary>
        Unaffordable,
        /// <summary>Bought: the tile is empty and says so.</summary>
        Sold,
    }

    /// <summary>
    /// One offer of the shop's window (2026-10-07 round 44, A; Docs/Architecture/12_UI.md "상점"): the offer as a board cell
    /// (an item with its icon and tier marks) or as a potion in its pocket, its name and kind under it, and its price with the
    /// coin at the bottom; "sold" over an empty tile. The whole tile is the button; the cell inside takes no click. The rim is
    /// brass while the offer is picked, and the tile is dimmed while the coins do not cover it.
    /// </summary>
    public sealed class ShopTileView : MonoBehaviour
    {
        const float UnaffordableAlpha = 0.55f;
        const float SoldAlpha = 0.7f;

        [SerializeField] Button _button;
        [SerializeField] Image _rim;
        [SerializeField] CanvasGroup _group;
        [SerializeField] ItemSlotView _cell;
        [SerializeField] GameObject _potionPocket;
        [SerializeField] Image _potionIcon;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _sub;
        [SerializeField] GameObject _priceRow;
        [SerializeField] TMP_Text _price;
        [SerializeField] TMP_Text _sold;

        public Button Button => _button;

        /// <summary>The slot of the shop's stock the tile shows; the screen sets it when it opens.</summary>
        public int Slot { get; set; }

        /// <summary>The offer on show, or null for a sold tile.</summary>
        public RewardOption Offer { get; private set; }

        public ShopTileState State { get; private set; }

        /// <summary>True while the tile shows a sold slot.</summary>
        public bool IsSold => State == ShopTileState.Sold;

        /// <summary>The name on the tile, or an empty string while it is sold.</summary>
        public string Name => State == ShopTileState.Sold ? string.Empty : _name.text;

        /// <summary>The price on the tile, or an empty string while it is sold.</summary>
        public string Price => State == ShopTileState.Sold ? string.Empty : _price.text;

        /// <summary>The cell the item on offer is shown in: its icon and tier marks, as on a board.</summary>
        public ItemSlotView Cell => _cell;

        /// <param name="sub">The line under the name: the item's category and cells.</param>
        public void ShowItem(RewardOption offer, EquippedItem item, Sprite icon, string sub, int price, ShopTileState state)
        {
            Begin(offer, state);
            _cell.gameObject.SetActive(true);
            _cell.Show(item, icon, selected: false, interactable: true, fatigueCost: 0, merges: false);
            _potionPocket.SetActive(false);
            Words(UiText.ItemTitle(item), sub, price, state);
        }

        /// <param name="sub">The line under the name: that it is a potion.</param>
        public void ShowPotion(RewardOption offer, PotionData potion, Sprite icon, string sub, int price, ShopTileState state)
        {
            Begin(offer, state);
            _cell.gameObject.SetActive(false);
            _potionPocket.SetActive(true);
            _potionIcon.sprite = icon;
            _potionIcon.enabled = icon != null;
            Words(UiText.Name(potion.Name), sub, price, state);
        }

        public void ShowSold()
        {
            Begin(null, ShopTileState.Sold);
            _cell.gameObject.SetActive(false);
            _potionPocket.SetActive(false);
            _name.gameObject.SetActive(false);
            _sub.gameObject.SetActive(false);
            _priceRow.SetActive(false);
            _sold.gameObject.SetActive(true);
        }

        void Begin(RewardOption offer, ShopTileState state)
        {
            Offer = offer;
            State = state;
            _rim.color = state == ShopTileState.Picked ? UiPalette.Selected : UiPalette.Line;
            _group.alpha = state == ShopTileState.Unaffordable ? UnaffordableAlpha : state == ShopTileState.Sold ? SoldAlpha : 1f;
            _button.interactable = state == ShopTileState.OnSale || state == ShopTileState.Picked;
            _sold.gameObject.SetActive(false);
        }

        void Words(string name, string sub, int price, ShopTileState state)
        {
            _name.gameObject.SetActive(true);
            _sub.gameObject.SetActive(true);
            _priceRow.SetActive(true);
            _name.text = name;
            _sub.text = sub;
            _price.text = UiStrings.Get(UiKeys.Map.Coins, price);
            _price.color = state == ShopTileState.Unaffordable ? UiPalette.Danger : UiPalette.Virtue;
        }
    }
}
