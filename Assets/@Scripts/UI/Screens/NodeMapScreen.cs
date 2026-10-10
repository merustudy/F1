using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// The expedition map. The player arranges the party on the left (the battle screen's shape)
    /// and picks the next node on the right. Who waits at a node is not shown: that is found out
    /// in the battle (Docs/Design/03_Dungeon_Structure.md §1). The map scrolls up from the first floor
    /// to the boss; at a camp, its window lies over the map (2026-10-06 round 34), and so does the shop's (2026-10-07 round 44):
    /// an offer is picked on a tile and bought by the cell it is put in, or straight into the inventory.
    /// </summary>
    public sealed class NodeMapScreen : UIScreen
    {
        /// <summary>How far apart the floors are: as on the first, short maps (four floors in 354), however many floors a map has.</summary>
        public const float FloorSpacing = 88.5f;

        /// <summary>The room the names of a floor's nodes take under them.</summary>
        const float NameRoom = 26f;

        [SerializeField] TMP_Text _dungeon;
        [SerializeField] TMP_Text _progress;
        [SerializeField] ScrollRect _mapScroll;
        [SerializeField] RectTransform _mapArea;
        [SerializeField] MapNodeView _nodeTemplate;
        [SerializeField] Image _edgeTemplate;
        [SerializeField] TMP_Text _nodeTitle;
        [SerializeField] TMP_Text _nodeHint;
        [SerializeField] Button _enter;
        [SerializeField] TMP_Text _enterLabel;
        [SerializeField] GameObject _camp;
        [SerializeField] TMP_Text _campTitle;
        [SerializeField] TMP_Text _campHint;
        [SerializeField] GameObject _campChoices;
        [SerializeField] Button _rest;
        [SerializeField] TMP_Text _restBody;
        [SerializeField] Button _mend;
        [SerializeField] GameObject _campMend;
        [SerializeField] GameObject _mendPlate;
        [SerializeField] Image _mendIcon;
        [SerializeField] TMP_Text _mendChange;
        [SerializeField] TMP_Text _mendEffects;
        [SerializeField] Button _mendBack;
        [SerializeField] Button _mendConfirm;
        [SerializeField] Button _inventoryToggle;
        [SerializeField] TMP_Text _inventoryToggleLabel;
        [SerializeField] PartySideView _party;
        [SerializeField] TMP_Text _coins;
        [SerializeField] GameObject _shop;
        [SerializeField] TMP_Text _shopTitle;
        [SerializeField] TMP_Text _shopHint;
        [SerializeField] TMP_Text _shopCoins;
        [SerializeField] MerchantGridView _merchant;
        [SerializeField] GameObject _merchantInfoBox;
        [SerializeField] TMP_Text _merchantInfo;
        [SerializeField] Button _refresh;
        [SerializeField] TMP_Text _refreshCost;
        [SerializeField] Button _leave;
        [SerializeField] Button _buyToInventory;

        readonly Dictionary<int, MapNodeView> _nodes = new Dictionary<int, MapNodeView>();
        int _selectedNodeId = -1;
        float _floorSpacing;

        // The shop (round 44): the slot of the picked offer, and the detail line's note after a purchase.
        int _pick = -1;
        string _shopNote;

        // The merchant (stage 21): where its goods lie, kept while the same stock is shown so that a good bought leaves its place empty
        // (the places are not saved: after the app opens again what is left is laid anew), and the good the pointer is on.
        Dictionary<int, Placement> _merchantPlaces;
        string _merchantShown;
        int _merchantRows;
        int _pointedGood = -1;

        // The camp's mend step (round 35): choosing on the boards the item to raise a tier, then confirming it in the window.
        bool _mending;
        int _mendMember = -1;
        int _mendX;
        int _mendY;

        /// <summary>The scroll of the map, for tests: where it shows and how far it goes.</summary>
        public ScrollRect MapScroll => _mapScroll;

        /// <summary>The view of a node of the map.</summary>
        public MapNodeView NodeView(int nodeId)
        {
            return _nodes[nodeId];
        }

        /// <summary>The merchant's grid (stage 21): the shop's goods by slot.</summary>
        public MerchantGridView Merchant => _merchant;

        /// <summary>The merchant's tooltip over the good the pointer is on (round 57); an empty string while there is none.</summary>
        public string MerchantInfo => _merchantInfoBox.activeSelf ? _merchantInfo.text : string.Empty;

        /// <summary>The slot of the picked offer of the shop, or -1.</summary>
        public int PickedOffer => _pick;

        ExpeditionArt _art;

        /// <summary>The art of the units is loaded before the screen opens, so nothing waits for it afterwards.</summary>
        public override async Task PrepareAsync()
        {
            _art = await ExpeditionArt.LoadAsync(Managers.Resource, Managers.Data.Data);
        }

        protected override void OnOpen()
        {
            BuildMap(Managers.Expedition.Expedition.Map);
            _enter.onClick.AddListener(OnEnter);
            _rest.onClick.AddListener(OnRest);
            _mend.onClick.AddListener(OnMend);
            _mendBack.onClick.AddListener(OnMendBack);
            _mendConfirm.onClick.AddListener(OnMendConfirm);
            _merchant.GoodClicked += OnTileClicked;
            _merchant.GoodEntered += slot =>
            {
                _pointedGood = slot;
                ShowMerchantInfo();
            };
            _merchant.GoodExited += slot =>
            {
                if (_pointedGood == slot)
                {
                    _pointedGood = -1;
                    ShowMerchantInfo();
                }
            };

            _refresh.onClick.AddListener(OnRefreshShop);
            _leave.onClick.AddListener(OnLeaveShop);
            _buyToInventory.onClick.AddListener(OnBuyToInventory);
            _inventoryToggle.onClick.AddListener(OnInventoryToggle);
            _party.Open(_art);
            // Round 54: the buttons that work on what is held still take a press while it rides the pointer.
            _party.LetThroughWhileHeld(_inventoryToggle);
            _party.LetThroughWhileHeld(_buyToInventory);
            _party.HandReleased += () =>
            {
                _pick = -1;
                Refresh();
            };
            Managers.Sound.PlayMusic(MusicTrack.Dungeon);
            SelectTheOnlyWay();
            ScrollToCurrentFloor();
        }

        /// <summary>With a single way forward there is nothing to choose: select it.</summary>
        void SelectTheOnlyWay()
        {
            IReadOnlyList<MapNode> available = Managers.Expedition.AvailableNodes();
            _selectedNodeId = available.Count == 1 ? available[0].Id : -1;
        }

        public override void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;

            _dungeon.text = UiText.Name(data.Dungeons.Get(expedition.DungeonId).Name);
            int currentFloor = CurrentFloor(expedition);
            _progress.text = UiStrings.Get(UiKeys.Map.Progress, currentFloor, expedition.Map.FloorCount);

            var available = new HashSet<int>();
            foreach (MapNode node in manager.AvailableNodes())
            {
                available.Add(node.Id);
            }

            foreach (MapNode node in expedition.Map.Nodes)
            {
                MapNodeState state;
                if (available.Contains(node.Id))
                {
                    state = MapNodeState.Available;
                }
                else if (node.Id == expedition.CurrentNodeId)
                {
                    state = MapNodeState.Current;
                }
                else
                {
                    state = node.Floor <= currentFloor ? MapNodeState.Passed : MapNodeState.Ahead;
                }

                _nodes[node.Id].Show(KindText(node.Kind), node.Kind, state, node.Id == _selectedNodeId);
            }

            // At a camp or a shop, the panel only points at the window over the map; the window says the rest. While an item is being
            // chosen to mend, the boards serve that choice: the cells that can be mended take the click, the chosen one is the brass cell,
            // and the inventory stays out of it. While an offer of the shop is picked, the boards serve the purchase as they serve a drop of loot:
            // the cells that can take it take the click, and so does buying it straight into the inventory.
            bool atCamp = manager.Phase == GamePhase.Camp;
            bool atShop = manager.Phase == GamePhase.Shop;
            _mending &= atCamp;
            if (!atShop)
            {
                _pick = -1;
                _shopNote = null;
                _pointedGood = -1;
                _merchantInfoBox.SetActive(false);
            }

            bool picking = atShop && _pick >= 0 && _pick < manager.ShopStock.Count && manager.ShopStock[_pick] != null;
            _pick = picking ? _pick : -1;
            int pick = _pick;
            _coins.text = UiStrings.Get(UiKeys.Map.Coins, expedition.Coins);
            _camp.SetActive(atCamp);
            _shop.SetActive(atShop);
            _enter.gameObject.SetActive(!atCamp && !atShop);
            _buyToInventory.gameObject.SetActive(atShop);
            _buyToInventory.interactable = picking && manager.CanBuyToInventory(pick);
            _inventoryToggle.gameObject.SetActive(!_mending);
            _party.ToInventory.gameObject.SetActive(!_mending);
            // The picked offer is in the party's hand (stage 19): it is aimed at the boards' squares and bought where it is put.
            if (picking && !(_party.Hand.Outside is ShopHand held && held.Slot == pick))
            {
                _party.Hand.HoldOutside(new ShopHand(this, pick));
            }
            else if (!picking && _party.Hand.Outside is ShopHand)
            {
                _party.Hand.Clear();
            }

            _party.SquareClickOverride = _mending ? ChooseMendItem : (Func<int, int, int, bool>)null;
            _party.ExternalSelectedMember = _mending ? _mendMember : -1;
            _party.ExternalSelectedX = _mendX;
            _party.ExternalSelectedY = _mendY;
            _party.DetailOverride = _mending ? UiStrings.Get(UiKeys.Map.MendHint) : picking ? UiStrings.Get(UiKeys.Map.ShopBuyHint) : _shopNote;
            if (atShop)
            {
                RefreshShop(manager, data, expedition, picking);
            }
            else if (atCamp)
            {
                MapNode camp = expedition.Map.Get(expedition.CurrentNodeId);
                _nodeTitle.text = UiStrings.Get(UiKeys.Map.NodeTitle, camp.Floor, KindText(camp.Kind));
                _nodeHint.text = UiStrings.Get(UiKeys.Map.AtCamp);
                _campTitle.text = _mending ? UiStrings.Get(UiKeys.Map.MendTitle, camp.Floor) : _nodeTitle.text;
                _campHint.text = UiStrings.Get(_mending ? UiKeys.Map.MendHint : UiKeys.Map.CampChoose);
                _campChoices.SetActive(!_mending);
                _campMend.SetActive(_mending);
                _restBody.text = UiStrings.Get(UiKeys.Map.RestHeal, data.Balance.CampHealPercent) + "\n"
                    + UiStrings.Get(UiKeys.Map.RestFatigue, data.Balance.CampFatigueRelief);
                RefreshMendPlate(manager, data.Balance);
            }
            else
            {
                MapNode selected = _selectedNodeId >= 0 ? expedition.Map.Get(_selectedNodeId) : null;
                _enter.interactable = selected != null;
                _enterLabel.text = UiStrings.Get(EnterKey(selected));
                _nodeTitle.text = selected != null
                    ? UiStrings.Get(UiKeys.Map.NodeTitle, selected.Floor, KindText(selected.Kind))
                    : UiStrings.Get(UiKeys.Map.SelectNode);
                _nodeHint.text = UiStrings.Get(HintKey(selected));
            }

            _inventoryToggleLabel.text = UiStrings.Get(_party.InventoryOpen ? UiKeys.Board.InventoryHide : UiKeys.Board.InventoryShow);
            _party.Refresh();
        }

        /// <summary>The chosen item's change on the mend plate: its tier before and after, and each effect's size before and after.</summary>
        void RefreshMendPlate(ExpeditionManager manager, BalanceData balance)
        {
            bool chosen = _mending && _mendMember >= 0 && manager.CanUpgradeAtCamp(_mendMember, _mendX, _mendY);
            _mendPlate.SetActive(chosen);
            _mendConfirm.interactable = chosen;
            if (!chosen)
            {
                return;
            }

            EquippedItem item = manager.Expedition.Members[_mendMember].Board.ItemAt(_mendX, _mendY).Item;
            EquippedItem mended = item.TierUp();
            _mendIcon.sprite = _art.OfItem(item.Item.Id);
            _mendIcon.enabled = _mendIcon.sprite != null;
            _mendChange.text = UiText.Name(item.Item.Name) + "  " + UiText.Change(UiText.TierWord(item.Tier), UiText.TierWord(mended.Tier));
            var lines = new List<string>();
            foreach (ItemEffect effect in item.Item.Effects)
            {
                lines.Add(UiText.Change(UiText.Effect(effect, item.Magnitude(balance, effect)), mended.Magnitude(balance, effect).ToString(CultureInfo.InvariantCulture)));
            }

            _mendEffects.text = string.Join("\n", lines);
        }

        /// <summary>
        /// The shop's window (round 44; the merchant since stage 21): the title and the party's coins, the goods on the merchant's grid
        /// (picked, on sale, or beyond the coins; a good bought leaves its place empty), what the next refresh costs; and the panel's words
        /// (how to buy, while an offer is picked).
        /// </summary>
        void RefreshShop(ExpeditionManager manager, StaticData data, ExpeditionState expedition, bool picking)
        {
            MapNode shop = expedition.Map.Get(expedition.CurrentNodeId);
            _nodeTitle.text = UiStrings.Get(UiKeys.Map.NodeTitle, shop.Floor, KindText(shop.Kind));
            _nodeHint.text = UiStrings.Get(UiKeys.Map.AtShop);
            _shopTitle.text = _nodeTitle.text;
            _shopHint.text = UiStrings.Get(UiKeys.Map.ShopChoose);
            _shopCoins.text = UiStrings.Get(UiKeys.Map.Coins, expedition.Coins);

            ShowMerchant(manager, data, expedition, shop);

            bool canRefresh = manager.CanRefreshShop;
            _refresh.interactable = canRefresh;
            _refreshCost.text = UiStrings.Get(UiKeys.Map.Coins, manager.RefreshCost);
            _refreshCost.color = canRefresh ? UiPalette.Virtue : UiPalette.Danger;
        }

        /// <summary>
        /// The merchant's goods on its grid (stage 21): laid out once for a stock (a shop and its refreshes) and kept, so that a good bought
        /// leaves its place empty until the next refresh; each one picked (the hand holds it), on sale, or beyond what can be bought now (the
        /// coins; a potion without an empty slot) with its price.
        /// </summary>
        void ShowMerchant(ExpeditionManager manager, StaticData data, ExpeditionState expedition, MapNode shop)
        {
            IReadOnlyList<ItemOffer> stock = manager.ShopStock;
            string shown = shop.Id + ":" + expedition.Shop.Refreshes;
            if (_merchantPlaces == null || _merchantShown != shown)
            {
                // Round 56: the goods biggest first in the first eight columns, the potions down the last one in their order (a potion bought
                // leaves its square empty: its row is its place among the potions). Round 58: the bags along the last row, the items above
                // the empty row over it.
                int goodsCount = manager.ShopGoodsCount;
                var goods = new List<MerchantGood>();
                for (int i = 0; i < stock.Count; i++)
                {
                    if (stock[i] != null)
                    {
                        OfferSize(data, stock[i], out int w, out int h);
                        bool potion = stock[i].Kind == OfferKind.Potion;
                        goods.Add(new MerchantGood(i, w, h, stock[i].Kind, potion && i >= goodsCount ? i - goodsCount : -1));
                    }
                }

                _merchantPlaces = MerchantLayout.Lay(goods, MerchantGridView.GoodsColumns, MerchantGridView.ItemRows, MerchantGridView.BagRow, MerchantGridView.PotionColumn,
                    out _merchantRows);
                _merchantShown = shown;
                _pointedGood = -1;
            }

            var pieces = new List<MerchantPiece>();
            for (int i = 0; i < stock.Count; i++)
            {
                ItemOffer offer = stock[i];
                if (offer == null || !_merchantPlaces.TryGetValue(i, out Placement at))
                {
                    continue;
                }

                OfferSize(data, offer, out int w, out int h);
                var piece = new MerchantPiece { Slot = i, At = at, Width = w, Height = h, Price = manager.PriceOf(offer) };
                if (offer.Kind == OfferKind.Item)
                {
                    piece.Item = new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier);
                    piece.Icon = _art.OfItem(offer.Id);
                }
                else if (offer.Kind == OfferKind.Bag)
                {
                    piece.Bag = data.Bags.Get(offer.Id);
                }
                else
                {
                    piece.Icon = _art.OfPotion(offer.Id);
                }

                bool can = offer.Kind == OfferKind.Potion ? manager.CanBuyPotion(i) : manager.CanAfford(i);
                piece.State = i == _pick ? MerchantGoodState.Picked : can ? MerchantGoodState.OnSale : MerchantGoodState.Unaffordable;
                pieces.Add(piece);
            }

            _merchant.Show(pieces, _merchantRows, _art);
            ShowMerchantInfo();
        }

        /// <summary>The good's squares across and down: an item's or a bag's, a potion's one.</summary>
        static void OfferSize(StaticData data, ItemOffer offer, out int width, out int height)
        {
            switch (offer.Kind)
            {
                case OfferKind.Item:
                    ItemData item = data.Items.Get(offer.Id);
                    width = item.Width;
                    height = item.Height;
                    break;
                case OfferKind.Bag:
                    BagData bag = data.Bags.Get(offer.Id);
                    width = bag.Width;
                    height = bag.Height;
                    break;
                default:
                    width = 1;
                    height = 1;
                    break;
            }
        }

        /// <summary>
        /// The merchant's tooltip (stage 21, Diablo's; round 57: over the good, Docs/Architecture/12_UI.md "상인"): the good the pointer is
        /// on, with what merging it would make when the boards have its match (the right-click card said so before) and its price, centred
        /// over its piece (under it when there is no room above) and kept on the screen. Nothing while the pointer is on none, or while
        /// something is held: it rides the pointer.
        /// </summary>
        void ShowMerchantInfo()
        {
            ExpeditionManager manager = Managers.Expedition;
            IReadOnlyList<ItemOffer> stock = manager.Phase == GamePhase.Shop ? manager.ShopStock : null;
            bool holding = _pick >= 0 || _party.Hand.Holding;
            ItemOffer offer = stock != null && !holding && _pointedGood >= 0 && _pointedGood < stock.Count ? stock[_pointedGood] : null;
            ItemSlotView piece = offer != null ? _merchant.PieceOf(_pointedGood) : null;
            _merchantInfoBox.SetActive(piece != null);
            if (piece == null)
            {
                return;
            }

            StaticData data = Managers.Data.Data;
            string lines;
            if (offer.Kind == OfferKind.Item)
            {
                var item = new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier);
                lines = UiText.TooltipLines(item);
                if (manager.HasMergeTarget(item))
                {
                    lines += "\n" + UiText.MergeHint(item);
                }
            }
            else if (offer.Kind == OfferKind.Bag)
            {
                BagData bag = data.Bags.Get(offer.Id);
                lines = UiText.Colored(UiText.Name(bag.Name), UiPalette.BagName) + "\n" + UiStrings.Get(UiKeys.Map.ShopBagSub, bag.Width, bag.Height) + "\n"
                    + UiStrings.Get(UiKeys.Map.BagFacts, bag.Area);
            }
            else
            {
                PotionData potion = data.Potions.Get(offer.Id);
                lines = UiText.Name(potion.Name) + "\n" + UiStrings.Get(UiKeys.Map.ShopPotion) + "\n" + UiText.PotionDetails(potion);
            }

            _merchantInfo.text = lines + "\n" + UiText.Colored(UiStrings.Get(UiKeys.Map.OfferPrice, manager.PriceOf(offer)), UiPalette.DiabloGold);
            var box = (RectTransform)_merchantInfoBox.transform;
            var screen = (RectTransform)box.parent;
            LayoutRebuilder.ForceRebuildLayoutImmediate(box);
            Rect placed = TooltipPlacement.Over(TooltipPlacement.In(piece.Rect, screen), box.rect.size, new Rect(0f, 0f, screen.rect.width, screen.rect.height));
            box.anchoredPosition = new Vector2(placed.x, -placed.y);
        }

        static int CurrentFloor(ExpeditionState expedition)
        {
            return expedition.CurrentNodeId < 0 ? 0 : expedition.Map.Get(expedition.CurrentNodeId).Floor;
        }

        static string KindText(MapNodeKind kind)
        {
            switch (kind)
            {
                case MapNodeKind.Boss: return UiStrings.Get(UiKeys.Map.Boss);
                case MapNodeKind.Elite: return UiStrings.Get(UiKeys.Map.Elite);
                case MapNodeKind.Camp: return UiStrings.Get(UiKeys.Map.Camp);
                case MapNodeKind.Shop: return UiStrings.Get(UiKeys.Map.Shop);
                default: return UiStrings.Get(UiKeys.Map.Battle);
            }
        }

        /// <summary>The way-in button's words for the chosen node: into a camp, into a shop, or into a fight.</summary>
        static string EnterKey(MapNode selected)
        {
            if (selected == null)
            {
                return UiKeys.Map.Enter;
            }

            switch (selected.Kind)
            {
                case MapNodeKind.Camp: return UiKeys.Map.EnterCamp;
                case MapNodeKind.Shop: return UiKeys.Map.EnterShop;
                default: return UiKeys.Map.Enter;
            }
        }

        /// <summary>The panel's hint for the chosen node: who waits stays unknown, but an elite is known to be strong and a camp to be safe.</summary>
        static string HintKey(MapNode selected)
        {
            if (selected == null)
            {
                return UiKeys.Map.Unknown;
            }

            switch (selected.Kind)
            {
                case MapNodeKind.Elite: return UiKeys.Map.EliteHint;
                case MapNodeKind.Camp: return UiKeys.Map.CampHint;
                case MapNodeKind.Shop: return UiKeys.Map.ShopHint;
                default: return UiKeys.Map.Unknown;
            }
        }

        /// <summary>
        /// Lays the nodes out floor by floor, first floor at the bottom over <see cref="BottomMargin"/>, <see cref="FloorSpacing"/>
        /// apart (a map short enough to fit is spread over the whole view), and draws the paths between them.
        /// </summary>
        void BuildMap(NodeMap map)
        {
            float viewHeight = _mapScroll.viewport.rect.height;
            float margin = BottomMargin(viewHeight);
            _floorSpacing = Mathf.Max(FloorSpacing, (viewHeight - margin) / map.FloorCount);
            _mapArea.sizeDelta = new Vector2(_mapArea.sizeDelta.x, margin + map.FloorCount * _floorSpacing);
            float width = _mapArea.rect.width;

            var positions = new Dictionary<int, Vector2>();
            for (int floor = 1; floor <= map.FloorCount; floor++)
            {
                List<MapNode> nodes = map.OnFloor(floor);
                foreach (MapNode node in nodes)
                {
                    float x = (node.Column + 0.5f) / nodes.Count * width;
                    float y = margin + (floor - 0.5f) * _floorSpacing;
                    positions.Add(node.Id, new Vector2(x, y));
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                foreach (int nextId in node.NextNodeIds)
                {
                    DrawEdge(positions[node.Id], positions[nextId]);
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                MapNodeView view = Instantiate(_nodeTemplate, _mapArea);
                view.gameObject.SetActive(true);
                view.Rect.anchorMin = Vector2.zero;
                view.Rect.anchorMax = Vector2.zero;
                view.Rect.anchoredPosition = positions[node.Id];
                _nodes.Add(node.Id, view);

                int id = node.Id;
                view.Button.onClick.AddListener(() => OnNodeClicked(id));
            }
        }

        void DrawEdge(Vector2 from, Vector2 to)
        {
            Image edge = Instantiate(_edgeTemplate, _mapArea);
            edge.gameObject.SetActive(true);
            RectTransform rect = edge.rectTransform;
            Vector2 delta = to - from;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, rect.sizeDelta.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        /// <summary>
        /// The room under the first floor: the names of its nodes, and as much more as puts the top of the view through the
        /// middle of a floor's nodes while the map is at its bottom, so that no names stand at the top without their nodes.
        /// </summary>
        static float BottomMargin(float viewHeight)
        {
            float margin = viewHeight % FloorSpacing - FloorSpacing / 2f;
            while (margin < NameRoom)
            {
                margin += FloorSpacing;
            }

            return margin;
        }

        /// <summary>
        /// Scrolls the map so that the floor the party stands on is where the first floor is at the start: a part of the floor
        /// it came from shows under it, and the top of the view passes through the middle of a floor's nodes further up.
        /// </summary>
        void ScrollToCurrentFloor()
        {
            float range = _mapArea.rect.height - _mapScroll.viewport.rect.height;
            int floor = Mathf.Max(1, CurrentFloor(Managers.Expedition.Expedition));
            _mapScroll.StopMovement();
            _mapScroll.verticalNormalizedPosition = range <= 0f ? 0f : Mathf.Clamp01((floor - 1) * _floorSpacing / range);
        }

        void OnNodeClicked(int nodeId)
        {
            _selectedNodeId = nodeId;
            Refresh();
        }

        void OnInventoryToggle()
        {
            _party.ToggleInventory();
            Refresh();
        }

        /// <summary>The I key (round 52) does what the inventory button does, while the button is there to press.</summary>
        void Update()
        {
            if (InventoryKey.Pressed())
            {
                PressInventoryKey();
            }
        }

        /// <summary>What the I key does: opens or closes the inventory window when its button is shown and works. Tests call it in place of the key.</summary>
        public void PressInventoryKey()
        {
            if (_inventoryToggle.gameObject.activeInHierarchy && _inventoryToggle.interactable)
            {
                Managers.Sound.PlayEffect(SoundEffect.Button);
                OnInventoryToggle();
            }
        }

        void OnEnter()
        {
            if (_selectedNodeId < 0)
            {
                return;
            }

            Managers.Expedition.EnterNode(_selectedNodeId);
            Managers.Sound.PlayEffect(SoundEffect.NodeMove);
            if (Managers.Expedition.Phase != GamePhase.Camp && Managers.Expedition.Phase != GamePhase.Shop)
            {
                GoToCurrentPhase();
                return;
            }

            // A camp or a shop is on this screen: its window opens over the map, which follows the party up a floor.
            _selectedNodeId = -1;
            ScrollToCurrentFloor();
            Refresh();
        }

        void OnRest()
        {
            Managers.Expedition.RestAtCamp();
            SelectTheOnlyWay();
            Refresh();
        }

        /// <summary>The mend card: the window turns to the mend step and the boards wait for the item to raise. The inventory popup closes.</summary>
        void OnMend()
        {
            _mending = true;
            _mendMember = -1;
            if (_party.InventoryOpen)
            {
                _party.ToggleInventory();
            }

            _party.ClearSelection();
            Refresh();
        }

        void OnMendBack()
        {
            _mending = false;
            _mendMember = -1;
            Refresh();
        }

        /// <summary>A click on a board square while mending chooses the item there, if it can be mended; the click sounds here.</summary>
        bool ChooseMendItem(int member, int x, int y)
        {
            if (Managers.Expedition.CanUpgradeAtCamp(member, x, y))
            {
                _mendMember = member;
                _mendX = x;
                _mendY = y;
            }

            Managers.Sound.PlayEffect(SoundEffect.Button);
            Refresh();
            return true;
        }

        /// <summary>
        /// A good of the shop (round 44; on the merchant's grid since stage 21): a potion is bought with one click and sounds as put in (a
        /// click when it cannot be); an item or a bag is picked, or put down, with a click. Its facts and price show in the tooltip beside the grid.
        /// </summary>
        void OnTileClicked(int slot)
        {
            ExpeditionManager manager = Managers.Expedition;
            ItemOffer offer = slot < manager.ShopStock.Count ? manager.ShopStock[slot] : null;
            if (offer == null)
            {
                return;
            }

            if (offer.Kind == OfferKind.Potion)
            {
                if (manager.CanBuyPotion(slot))
                {
                    int before = manager.Expedition.Coins;
                    manager.BuyPotion(slot);
                    Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
                    _shopNote = BoughtNote(UiText.Name(Managers.Data.Data.Potions.Get(offer.Id).Name), before);
                    _pick = -1;
                }
                else
                {
                    Managers.Sound.PlayEffect(SoundEffect.Button);
                }

                Refresh();
                return;
            }

            Managers.Sound.PlayEffect(SoundEffect.Button);
            _pick = _pick == slot ? -1 : slot;
            _shopNote = null;
            _party.ClearSelection();
            Refresh();
        }

        /// <summary>
        /// The picked offer of the shop in the party's hand (Slice B stage 19): it can go where the manager says it can be bought onto a
        /// board, merges as a purchase would, and is bought where it is put (the hand sounds it).
        /// </summary>
        sealed class ShopHand : IOutsideHand
        {
            readonly NodeMapScreen _screen;

            public ShopHand(NodeMapScreen screen, int slot)
            {
                _screen = screen;
                Slot = slot;
                ItemOffer offer = Managers.Expedition.ShopStock[slot];
                StaticData data = Managers.Data.Data;
                Item = offer.Kind == OfferKind.Item ? new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier) : null;
                Bag = offer.Kind == OfferKind.Bag ? data.Bags.Get(offer.Id) : null;
            }

            public int Slot { get; }
            public EquippedItem Item { get; }
            public BagData Bag { get; }

            public bool CanPlace(int member, Placement at)
            {
                return Managers.Expedition.CanBuyToBoard(Slot, member, at);
            }

            public bool MergesAt(int member, int x, int y)
            {
                return Managers.Expedition.ShopMergesAt(Slot, member, x, y);
            }

            public void Place(int member, Placement at)
            {
                int slot = Slot;
                _screen.Bought(() => Managers.Expedition.BuyToBoard(slot, member, at), sound: false);
                _screen.Refresh();
            }

            public bool CanPlaceInInventory(Placement at)
            {
                return Managers.Expedition.CanBuyToInventoryAt(Slot, at);
            }

            public void PlaceInInventory(Placement at)
            {
                int slot = Slot;
                _screen.Bought(() => Managers.Expedition.BuyToInventoryAt(slot, at), sound: false);
                _screen.Refresh();
            }
        }

        void OnBuyToInventory()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (_pick < 0 || !manager.CanBuyToInventory(_pick))
            {
                return;
            }

            int slot = _pick;
            Bought(() => manager.BuyToInventory(slot));
            Refresh();
        }

        /// <summary>Buys the picked item or bag by the command given: the detail line's note, the sound of an item put in (unless the hand sounds it), and the pick is spent.</summary>
        void Bought(Action buy, bool sound = true)
        {
            ExpeditionManager manager = Managers.Expedition;
            ItemOffer offer = manager.ShopStock[_pick];
            StaticData data = Managers.Data.Data;
            string what = offer.Kind == OfferKind.Bag
                ? UiText.Name(data.Bags.Get(offer.Id).Name)
                : UiText.ItemTitle(new EquippedItem(data.Items.Get(offer.Id), offer.Grade, tier: offer.Tier));
            int before = manager.Expedition.Coins;
            buy();
            if (sound)
            {
                Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            }

            _shopNote = BoughtNote(what, before);
            _pick = -1;
        }

        string BoughtNote(string what, int coinsBefore)
        {
            return UiStrings.Get(UiKeys.Map.ShopBought, what, coinsBefore, Managers.Expedition.Expedition.Coins);
        }

        /// <summary>Pays the refresh and shows the new stock; whatever was picked is gone with the old one.</summary>
        void OnRefreshShop()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (!manager.CanRefreshShop)
            {
                return;
            }

            manager.RefreshShop();
            _pick = -1;
            _shopNote = null;
            _party.ClearSelection();
            Refresh();
        }

        /// <summary>Leaves the shop: the window closes and the next floor's nodes are offered (the only way on is chosen).</summary>
        void OnLeaveShop()
        {
            Managers.Expedition.LeaveShop();
            _pick = -1;
            _shopNote = null;
            _party.ClearSelection();
            SelectTheOnlyWay();
            Refresh();
        }

        /// <summary>Mends the chosen item (a tier up) and sounds it as put in; the party goes on to the next floor.</summary>
        void OnMendConfirm()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (!_mending || !manager.CanUpgradeAtCamp(_mendMember, _mendX, _mendY))
            {
                return;
            }

            manager.UpgradeAtCamp(_mendMember, _mendX, _mendY);
            Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            _mending = false;
            _mendMember = -1;
            SelectTheOnlyWay();
            Refresh();
        }
    }
}
