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
    /// One offer of the shop's window (2026-10-07 round 44, A; 2026-10-08 round 46, S1; Docs/Architecture/12_UI.md "상점"): the item
    /// tile (<see cref="ItemTileView"/>: the offer as a board cell, its name and kind, its facts) or a potion in its pocket, with
    /// its price and the coin at the foot; "sold" over an empty tile. The tile is dimmed while the coins do not cover it.
    /// </summary>
    public sealed class ShopTileView : ItemTileView
    {
        const float UnaffordableAlpha = 0.55f;
        const float SoldAlpha = 0.7f;

        [SerializeField] GameObject _potionPocket;
        [SerializeField] Image _potionIcon;
        [SerializeField] GameObject _priceRow;
        [SerializeField] TMP_Text _price;
        [SerializeField] TMP_Text _sold;
        [SerializeField] GameObject _held;

        /// <summary>Whether the tile shows the small "held" label (round 49): while its offer is picked.</summary>
        public bool ShowsHeld => _held.activeSelf;

        /// <summary>The slot of the shop's stock the tile shows; the screen sets it when it opens.</summary>
        public int Slot { get; set; }

        /// <summary>The offer on show, or null for a sold tile.</summary>
        public ItemOffer Offer { get; private set; }

        public ShopTileState State { get; private set; }

        /// <summary>True while the tile shows a sold slot.</summary>
        public bool IsSold => State == ShopTileState.Sold;

        /// <summary>The name on the tile, or an empty string while it is sold.</summary>
        public string Name => State == ShopTileState.Sold ? string.Empty : NameText;

        /// <summary>The price on the tile, or an empty string while it is sold.</summary>
        public string Price => State == ShopTileState.Sold ? string.Empty : _price.text;

        /// <param name="sub">The line under the name: the item's category and cells.</param>
        /// <param name="facts">The facts under the rule (UiText.ItemTileFacts).</param>
        public void ShowItem(ItemOffer offer, EquippedItem item, Sprite icon, string sub, string facts, int price, ShopTileState state)
        {
            Begin(offer, state);
            ShowCell(item, icon);
            _potionPocket.SetActive(false);
            ShowWords(UiText.ItemTitle(item), sub, facts, UiPalette.Rarity(item.Tier));
            ShowPrice(price, state);
        }

        /// <param name="sub">The line under the name: that it is a bag and its squares (Slice B stage 19).</param>
        /// <param name="facts">What the bag adds.</param>
        public void ShowBag(ItemOffer offer, BagData bag, string sub, string facts, int price, ShopTileState state)
        {
            Begin(offer, state);
            ShowBagCell(bag);
            _potionPocket.SetActive(false);
            ShowWords(UiText.Name(bag.Name), sub, facts, UiPalette.BagName);
            ShowPrice(price, state);
        }

        /// <param name="sub">The line under the name: that it is a potion.</param>
        /// <param name="facts">What the potion does.</param>
        public void ShowPotion(ItemOffer offer, PotionData potion, Sprite icon, string sub, string facts, int price, ShopTileState state)
        {
            Begin(offer, state);
            HideCell();
            _potionPocket.SetActive(true);
            _potionIcon.sprite = icon;
            _potionIcon.enabled = icon != null;
            ShowWords(UiText.Name(potion.Name), sub, facts, UiPalette.Text);
            ShowPrice(price, state);
        }

        public void ShowSold()
        {
            Begin(null, ShopTileState.Sold);
            HideBody();
            _potionPocket.SetActive(false);
            _priceRow.SetActive(false);
            _sold.gameObject.SetActive(true);
        }

        void Begin(ItemOffer offer, ShopTileState state)
        {
            Offer = offer;
            State = state;
            Look(state == ShopTileState.Picked,
                state == ShopTileState.Unaffordable ? UnaffordableAlpha : state == ShopTileState.Sold ? SoldAlpha : 1f,
                state == ShopTileState.OnSale || state == ShopTileState.Picked);
            _sold.gameObject.SetActive(false);
            _held.SetActive(state == ShopTileState.Picked);
        }

        void ShowPrice(int price, ShopTileState state)
        {
            _priceRow.SetActive(true);
            _price.text = UiStrings.Get(UiKeys.Map.Coins, price);
            _price.color = state == ShopTileState.Unaffordable ? UiPalette.Danger : UiPalette.Virtue;
        }
    }
}
