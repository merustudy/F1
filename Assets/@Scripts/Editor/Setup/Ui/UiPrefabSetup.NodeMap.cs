using F1.Data;
using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The map scrolls up (2026-10-06 round 34, A): its view is the stone inside the tablet's frame, MapViewInset in
        /// from its edges, with the scroll bar MapScrollbarGap in from the view's right end; the nodes are laid out in the view
        /// less MapScrollbarRoom on the right, clear of the bar. One tick of the wheel (6 by the input module) moves the map
        /// about a floor (NodeMapScreen.FloorSpacing).
        /// </summary>
        const float MapViewInset = 14f;
        const float MapScrollbarWidth = 6f;
        const float MapScrollbarGap = 4f;
        const float MapScrollbarRoom = 20f;
        const float MapScrollSensitivity = 15f;

        /// <summary>How far the map fades out at the view's top and bottom, so that what scrolls out of view goes softly.</summary>
        const int MapEdgeSoftness = 16;

        /// <summary>
        /// The camp's window (2026-10-06 round 34, B; round 35): over the map, darkened, a box with a brass rim; the fire, the
        /// title and the hint at its top, and under them either the two cards (rest, mend) side by side, or the mend step: the
        /// chosen item's change on a plate and the back and confirm buttons.
        /// </summary>
        const float CampShadeAlpha = 0.6f;
        const float CampWindowWidth = 580f;
        const float CampWindowHeight = 320f;
        const float CampRim = 3f;
        const float CampCardWidth = 260f;
        const float CampCardHeight = 160f;
        const float CampCardsTop = 131f;
        const float CampSideMargin = 22f;
        const float CampCardGap = 10f;
        const float MendPlateTop = 126f;
        const float MendPlateHeight = 104f;
        const float MendButtonsTop = 240f;
        const float MendButtonHeight = 56f;

        /// <summary>
        /// The shop's window (2026-10-07 round 44, A): over the map like the camp's but wider, the offers as tiles side by side under
        /// the title (BuildShopTile: the offer as a board cell, its name and kind, its price), the refresh and leave buttons below.
        /// The party's coins stand at the window's top right, and beside the floor count in the header of every expedition screen.
        /// </summary>
        const float ShopWindowWidth = 820f;
        const float ShopWindowHeight = 360f;
        const float ShopTileWidth = 186f;
        const float ShopTileHeight = 160f;
        const float ShopTileGap = 10f;
        const float ShopTilesTop = 108f;
        const float ShopButtonsTop = 296f;
        const float ShopButtonHeight = 48f;
        const float ShopRefreshWidth = 250f;
        const float ShopLeaveWidth = 190f;
        const float ShopCoinSize = 30f;
        const float ShopCoinsWidth = 70f;
        const float ShopTileCellTop = 10f;
        const float ShopTileNameTop = 78f;
        const float ShopTileSubTop = 104f;
        const float ShopTilePriceTop = 124f;
        const float ShopTileCoinSize = 22f;
        const float HeaderCoinX = 1560f;
        const float HeaderCoinSize = 26f;

        /// <summary>
        /// The node map: the party as in battle (the stage on the left, the boards in the panel
        /// under it), the potions and the map on the right above the panel, and the chosen node's words and the
        /// buttons in the panel's right half. The panel names no enemies: who waits at a node is
        /// not shown (Docs/Design/03_Dungeon_Structure.md §1). At a camp, its window lies over the map.
        /// </summary>
        static UIScreen BuildNodeMap(Transform holder)
        {
            NodeMapScreen screen = Screen<NodeMapScreen>("NodeMapScreen", holder, out RectTransform frame);

            Image header = KitFrame("Header", frame, UiArt.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 84f);
            Image titlePlate = KitFrame("TitlePlate", header.transform, UiArt.PlateLabel);
            UiBuild.Box(titlePlate, 960f - TitlePlateWidth / 2f, 4f, TitlePlateWidth, TitlePlateHeight);
            TextMeshProUGUI dungeon = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label("Dungeon", titlePlate.transform, 26f, UiPalette.Brass, TextAlignmentOptions.Center)), 18f);
            UiBuild.Stretch(dungeon.rectTransform, 20f, 8f, 20f, 8f);
            TextMeshProUGUI progress = UiBuild.Label("Progress", header.transform, 30f, UiPalette.TextDim, TextAlignmentOptions.Right);
            UiBuild.Box(progress, 1280f, 18f, 600f, 44f);
            TextMeshProUGUI coins = BuildHeaderCoins(header.transform);

            // Map: it starts under the potions and ends above the panel. Nodes and paths are created at runtime inside the
            // area, measured from its bottom-left corner; the area is the scroll's content, standing on the view's bottom,
            // and the screen sets its height.
            float mapHeight = BoardPanelTop - 16f - PartyRightTop;
            Image map = KitFrame("Map", frame, UiArt.Tablet);
            UiBuild.Box(map, 980f, PartyRightTop, 920f, mapHeight);

            // The view takes clicks, so that the wheel and a drag reach the scroll rect anywhere on the map.
            Image view = UiBuild.Image("MapView", map.transform, Color.clear, raycastTarget: true);
            UiBuild.Stretch(view.rectTransform, MapViewInset, MapViewInset, MapViewInset, MapViewInset);
            view.gameObject.AddComponent<RectMask2D>().softness = new Vector2Int(0, MapEdgeSoftness);
            RectTransform mapArea = UiBuild.Rect("MapArea", view.transform);
            mapArea.anchorMin = Vector2.zero;
            mapArea.anchorMax = new Vector2(1f, 0f);
            mapArea.pivot = new Vector2(0.5f, 0f);
            mapArea.offsetMin = Vector2.zero;
            mapArea.offsetMax = new Vector2(-MapScrollbarRoom, mapHeight - 2f * MapViewInset);

            Image track = Rounded("MapScrollbar", map.transform, Tinted(UiPalette.Ink, 0.6f), MapScrollbarWidth / 2f);
            track.raycastTarget = true;
            track.rectTransform.anchorMin = new Vector2(1f, 0f);
            track.rectTransform.anchorMax = Vector2.one;
            track.rectTransform.offsetMin = new Vector2(-(MapViewInset + MapScrollbarGap + MapScrollbarWidth), MapViewInset + MapScrollbarGap);
            track.rectTransform.offsetMax = new Vector2(-(MapViewInset + MapScrollbarGap), -(MapViewInset + MapScrollbarGap));
            Image handle = Rounded("MapScrollHandle", track.transform, UiPalette.Brass, MapScrollbarWidth / 2f);
            handle.raycastTarget = true;
            UiBuild.Stretch(handle.rectTransform);
            var scrollbar = track.gameObject.AddComponent<Scrollbar>();
            scrollbar.targetGraphic = handle;
            scrollbar.handleRect = handle.rectTransform;
            scrollbar.direction = Scrollbar.Direction.BottomToTop;
            scrollbar.navigation = new Navigation { mode = Navigation.Mode.None };

            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = view.rectTransform;
            scroll.content = mapArea;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = MapScrollSensitivity;
            scroll.verticalScrollbar = scrollbar;
            scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

            // The paths are lines of gold carved in the stone tablet; a node is a disc in the color of its state with the marker of its kind on it and its name under it.
            Image edgeTemplate = UiBuild.Image("EdgeTemplate", mapArea, new Color(UiPalette.Brass.r, UiPalette.Brass.g, UiPalette.Brass.b, 0.85f));
            UiBuild.Place(edgeTemplate.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 4f));
            edgeTemplate.gameObject.SetActive(false);

            Image nodeFrame = UiBuild.Image("NodeTemplate", mapArea, UiPalette.ButtonQuiet);
            nodeFrame.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Place(nodeFrame.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 74f));
            Button nodeButton = UiBuild.MakeButton(nodeFrame);
            Image nodeIcon = KitIcon("NodeIcon", nodeFrame.transform, UiArt.NodeBattle);
            UiBuild.Stretch(nodeIcon.rectTransform, 13f, 13f, 13f, 13f);

            // Only the nodes that can be chosen are clickable. The others must keep their state color
            // instead of looking dimmed.
            ColorBlock nodeColors = nodeButton.colors;
            nodeColors.disabledColor = Color.white;
            nodeButton.colors = nodeColors;
            TextMeshProUGUI nodeLabel = UiBuild.SingleLine(UiBuild.Label("NodeLabel", nodeFrame.transform, 18f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Place(nodeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(140f, 24f));
            var nodeTemplate = nodeFrame.gameObject.AddComponent<MapNodeView>();
            UiBuild.SetReference(nodeTemplate, "_button", nodeButton);
            UiBuild.SetReference(nodeTemplate, "_frame", nodeFrame);
            UiBuild.SetReference(nodeTemplate, "_icon", nodeIcon);
            UiBuild.SetReference(nodeTemplate, "_battleIcon", UiArt.Load(UiArt.NodeBattle));
            UiBuild.SetReference(nodeTemplate, "_bossIcon", UiArt.Load(UiArt.NodeBoss));
            UiBuild.SetReference(nodeTemplate, "_eliteIcon", UiArt.Load(UiArt.NodeElite));
            UiBuild.SetReference(nodeTemplate, "_campIcon", UiArt.Load(UiArt.NodeCamp));
            UiBuild.SetReference(nodeTemplate, "_shopIcon", UiArt.Load(UiArt.NodeShop));
            UiBuild.SetReference(nodeTemplate, "_label", nodeLabel);
            nodeFrame.gameObject.SetActive(false);

            // The camp's window, over the map and under the party side's inventory popup. The shade takes the map's clicks.
            Image campCover = UiBuild.Image("Camp", map.transform, new Color(0f, 0f, 0f, CampShadeAlpha), raycastTarget: true);
            UiBuild.Stretch(campCover.rectTransform, MapViewInset, MapViewInset, MapViewInset, MapViewInset);
            Image campWindow = Rounded("CampWindow", campCover.transform, UiPalette.Brass, 10f);
            UiBuild.Place(campWindow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(CampWindowWidth, CampWindowHeight));
            Image campBox = Rounded("CampBox", campWindow.transform, UiPalette.Panel, 10f - CampRim);
            UiBuild.Stretch(campBox.rectTransform, CampRim, CampRim, CampRim, CampRim);
            UiBuild.Box(KitIcon("CampFire", campBox.transform, UiArt.NodeCamp), 21f, 19f, 80f, 80f);
            TextMeshProUGUI campTitle = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CampTitle", campBox.transform, 32f, UiPalette.Text), 121f, 21f, 430f, 44f));
            TextMeshProUGUI campHint = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CampHint", campBox.transform, 19f, UiPalette.TextDim), 121f, 69f, 430f, 28f));

            // What the party can do at the camp: a card each, side by side, the whole card the button (rest on the left, mend on the right).
            RectTransform choices = UiBuild.Rect("CampChoices", campBox.transform);
            UiBuild.Stretch(choices);
            Image rest = Rounded("Rest", choices, UiPalette.Button, 8f);
            UiBuild.Box(rest, CampSideMargin, CampCardsTop, CampCardWidth, CampCardHeight);
            Button restButton = UiBuild.MakeButton(rest);
            UiBuild.Box(UiBuild.LocalizedLabel("RestTitle", rest.transform, UiKeys.Map.Rest, 30f, UiPalette.Text), 18f, 12f, CampCardWidth - 36f, 44f);
            TextMeshProUGUI restBody = UiBuild.Label("RestBody", rest.transform, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(restBody, 18f, 64f, CampCardWidth - 36f, CampCardHeight - 76f);
            Image mend = Rounded("Mend", choices, UiPalette.Button, 8f);
            UiBuild.Box(mend, CampSideMargin + CampCardWidth + CampCardGap, CampCardsTop, CampCardWidth, CampCardHeight);
            Button mendButton = UiBuild.MakeButton(mend);
            UiBuild.Box(UiBuild.LocalizedLabel("MendTitle", mend.transform, UiKeys.Map.Mend, 30f, UiPalette.Text), 18f, 12f, CampCardWidth - 36f, 44f);
            UiBuild.Box(UiBuild.LocalizedLabel("MendBody", mend.transform, UiKeys.Map.MendBody, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft), 18f, 64f, CampCardWidth - 36f, CampCardHeight - 76f);

            // The mend step (round 35): the chosen item on a plate (its icon, its tier before and after, its effects before and
            // after), then back and confirm. The plate shows once an item is chosen on the boards.
            RectTransform mendStep = UiBuild.Rect("CampMend", campBox.transform);
            UiBuild.Stretch(mendStep);
            float inner = CampWindowWidth - 2f * CampRim - 2f * CampSideMargin;
            Image plate = Rounded("MendPlate", mendStep, UiPalette.PanelLight, 8f);
            UiBuild.Box(plate, CampSideMargin, MendPlateTop, inner, MendPlateHeight);
            Image mendIcon = UiBuild.Image("MendIcon", plate.transform, Color.white);
            mendIcon.preserveAspect = true;
            UiBuild.Box(mendIcon, 14f, 12f, 150f, MendPlateHeight - 24f);
            TextMeshProUGUI mendChange = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("MendChange", plate.transform, 24f, UiPalette.Text), 184f, 12f, inner - 198f, 34f));
            TextMeshProUGUI mendEffects = UiBuild.Label("MendEffects", plate.transform, 18f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(mendEffects, 184f, 50f, inner - 198f, MendPlateHeight - 56f);
            plate.gameObject.SetActive(false);
            float buttonWidth = (inner - CampCardGap) / 2f;
            Image back = Rounded("MendBack", mendStep, UiPalette.ButtonQuiet, 8f);
            UiBuild.Box(back, CampSideMargin, MendButtonsTop, buttonWidth, MendButtonHeight);
            Button backButton = UiBuild.MakeButton(back);
            UiBuild.Stretch(UiBuild.LocalizedLabel("MendBackLabel", back.transform, UiKeys.Map.MendBack, 24f, UiPalette.Text, TextAlignmentOptions.Center).rectTransform);
            Image confirm = Rounded("MendConfirm", mendStep, UiPalette.Button, 8f);
            UiBuild.Box(confirm, CampSideMargin + buttonWidth + CampCardGap, MendButtonsTop, buttonWidth, MendButtonHeight);
            Button confirmButton = UiBuild.MakeButton(confirm);
            UiBuild.Silence(confirmButton);
            UiBuild.Stretch(UiBuild.LocalizedLabel("MendConfirmLabel", confirm.transform, UiKeys.Map.MendConfirm, 24f, UiPalette.Text, TextAlignmentOptions.Center).rectTransform);
            mendStep.gameObject.SetActive(false);
            campCover.gameObject.SetActive(false);

            // The shop's window (round 44), over the map like the camp's: the coin stack, the title and the hint at its top with the
            // party's coins at the right, the offers as tiles under them, and the refresh (with its cost) and leave buttons below.
            Image shopCover = UiBuild.Image("Shop", map.transform, new Color(0f, 0f, 0f, CampShadeAlpha), raycastTarget: true);
            UiBuild.Stretch(shopCover.rectTransform, MapViewInset, MapViewInset, MapViewInset, MapViewInset);
            Image shopWindow = Rounded("ShopWindow", shopCover.transform, UiPalette.Brass, 10f);
            UiBuild.Place(shopWindow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(ShopWindowWidth, ShopWindowHeight));
            Image shopBox = Rounded("ShopBox", shopWindow.transform, UiPalette.Panel, 10f - CampRim);
            UiBuild.Stretch(shopBox.rectTransform, CampRim, CampRim, CampRim, CampRim);
            UiBuild.Box(KitIcon("ShopIcon", shopBox.transform, UiArt.NodeShop), 21f, 19f, 80f, 80f);
            TextMeshProUGUI shopTitle = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("ShopTitle", shopBox.transform, 32f, UiPalette.Text), 121f, 21f, 430f, 44f));
            TextMeshProUGUI shopHint = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("ShopHint", shopBox.transform, 19f, UiPalette.TextDim), 121f, 69f, 560f, 28f));
            float shopInner = ShopWindowWidth - 2f * CampRim;
            float coinsRight = shopInner - CampSideMargin;
            TextMeshProUGUI shopCoins = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("ShopCoins", shopBox.transform, 30f, UiPalette.Text, TextAlignmentOptions.Right), coinsRight - ShopCoinsWidth, 26f, ShopCoinsWidth, 40f));
            float shopCoinX = coinsRight - ShopCoinsWidth - 8f - ShopCoinSize;
            UiBuild.Box(KitIcon("ShopCoinIcon", shopBox.transform, UiArt.Coin), shopCoinX, 31f, ShopCoinSize, ShopCoinSize);
            UiBuild.Box(UiBuild.SingleLine(UiBuild.LocalizedLabel("ShopCoinsLabel", shopBox.transform, UiKeys.Map.ShopCoinsLabel, 19f, UiPalette.TextDim, TextAlignmentOptions.Right)), shopCoinX - 8f - 160f, 34f, 160f, 28f);

            RectTransform tiles = UiBuild.Box(UiBuild.Rect("ShopTiles", shopBox.transform), CampSideMargin, ShopTilesTop, shopInner - 2f * CampSideMargin, ShopTileHeight);
            UiBuild.Horizontal(tiles, ShopTileGap, 0, TextAnchor.MiddleCenter);
            var shopTiles = new ShopTileView[BalanceData.MaxShopSlots];
            for (int i = 0; i < shopTiles.Length; i++)
            {
                shopTiles[i] = BuildShopTile(tiles, "ShopTile" + (i + 1));
            }

            // Refresh: the word, the coin and the cost in a row in the middle of the button; the screen writes the cost and colours it.
            Image refresh = Rounded("ShopRefresh", shopBox.transform, UiPalette.ButtonQuiet, 8f);
            UiBuild.Box(refresh, CampSideMargin, ShopButtonsTop, ShopRefreshWidth, ShopButtonHeight);
            Button refreshButton = UiBuild.MakeButton(refresh);
            RectTransform refreshRow = UiBuild.Rect("ShopRefreshRow", refresh.transform);
            UiBuild.Stretch(refreshRow);
            HorizontalLayoutGroup refreshLayout = UiBuild.Horizontal(refreshRow, 8f, 0, TextAnchor.MiddleCenter);
            refreshLayout.childControlWidth = true;
            UiBuild.Size(UiBuild.SingleLine(UiBuild.LocalizedLabel("ShopRefreshLabel", refreshRow, UiKeys.Map.ShopRefresh, 24f, UiPalette.Text, TextAlignmentOptions.Center)), 120f, 32f);
            Image refreshCoin = KitIcon("ShopRefreshCoin", refreshRow, UiArt.Coin);
            UiBuild.Size(refreshCoin, ShopTileCoinSize, ShopTileCoinSize);
            refreshCoin.gameObject.AddComponent<LayoutElement>().preferredWidth = ShopTileCoinSize;
            TextMeshProUGUI refreshCost = UiBuild.Size(Numeral(UiBuild.Label("ShopRefreshCost", refreshRow, 24f, UiPalette.Virtue, TextAlignmentOptions.Center)), 60f, 32f);

            Image leave = Rounded("ShopLeave", shopBox.transform, UiPalette.Button, 8f);
            UiBuild.Box(leave, shopInner - CampSideMargin - ShopLeaveWidth, ShopButtonsTop, ShopLeaveWidth, ShopButtonHeight);
            Button leaveButton = UiBuild.MakeButton(leave);
            UiBuild.Stretch(UiBuild.LocalizedLabel("ShopLeaveLabel", leave.transform, UiKeys.Map.ShopLeave, 24f, UiPalette.Text, TextAlignmentOptions.Center).rectTransform);
            shopCover.gameObject.SetActive(false);

            PartySideView party = BuildPartySide(frame, PanelRightX, 200f, out Image panel);

            // The chosen node in the panel's right half: its floor and kind, its hint (the enemies stay unknown), then the inventory and the way in.
            TextMeshProUGUI nodeTitle = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("NodeTitle", panel.transform, 34f, UiPalette.Text), PanelRightX, PanelTitleTop, PanelRightWidth, 48f));
            TextMeshProUGUI nodeHint = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("NodeHint", panel.transform, 20f, UiPalette.TextDim), PanelRightX, PanelHintTop, PanelRightWidth, 30f));
            ButtonParts inventoryToggle = KitButton("InventoryToggle", panel.transform, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(inventoryToggle.Rect, 1220f, PanelButtonsTop, 236f, PanelButtonHeight);
            ButtonParts enter = KitButton("Enter", panel.transform, UiPalette.Button, 30f);
            UiBuild.Silence(enter.Button);
            UiBuild.Box(enter.Rect, 1644f, PanelButtonsTop, 236f, PanelButtonHeight);

            // At a shop (round 44), the way in makes room for buying the picked offer straight into the inventory.
            ButtonParts buyToInventory = KitLocalizedButton("ShopBuy", panel.transform, UiKeys.Loot.ToInventory, UiPalette.Button, 26f);
            UiBuild.Silence(buyToInventory.Button);
            UiBuild.Box(buyToInventory.Rect, 1644f, PanelButtonsTop, 236f, PanelButtonHeight);
            buyToInventory.Rect.gameObject.SetActive(false);

            UiBuild.SetReference(screen, "_dungeon", dungeon);
            UiBuild.SetReference(screen, "_progress", progress);
            UiBuild.SetReference(screen, "_mapScroll", scroll);
            UiBuild.SetReference(screen, "_mapArea", mapArea);
            UiBuild.SetReference(screen, "_nodeTemplate", nodeTemplate);
            UiBuild.SetReference(screen, "_edgeTemplate", edgeTemplate);
            UiBuild.SetReference(screen, "_nodeTitle", nodeTitle);
            UiBuild.SetReference(screen, "_nodeHint", nodeHint);
            UiBuild.SetReference(screen, "_enter", enter.Button);
            UiBuild.SetReference(screen, "_enterLabel", enter.Label);
            UiBuild.SetReference(screen, "_camp", campCover.gameObject);
            UiBuild.SetReference(screen, "_campTitle", campTitle);
            UiBuild.SetReference(screen, "_campHint", campHint);
            UiBuild.SetReference(screen, "_campChoices", choices.gameObject);
            UiBuild.SetReference(screen, "_rest", restButton);
            UiBuild.SetReference(screen, "_restBody", restBody);
            UiBuild.SetReference(screen, "_mend", mendButton);
            UiBuild.SetReference(screen, "_campMend", mendStep.gameObject);
            UiBuild.SetReference(screen, "_mendPlate", plate.gameObject);
            UiBuild.SetReference(screen, "_mendIcon", mendIcon);
            UiBuild.SetReference(screen, "_mendChange", mendChange);
            UiBuild.SetReference(screen, "_mendEffects", mendEffects);
            UiBuild.SetReference(screen, "_mendBack", backButton);
            UiBuild.SetReference(screen, "_mendConfirm", confirmButton);
            UiBuild.SetReference(screen, "_inventoryToggle", inventoryToggle.Button);
            UiBuild.SetReference(screen, "_inventoryToggleLabel", inventoryToggle.Label);
            UiBuild.SetReference(screen, "_party", party);
            UiBuild.SetReference(screen, "_coins", coins);
            UiBuild.SetReference(screen, "_shop", shopCover.gameObject);
            UiBuild.SetReference(screen, "_shopWindow", shopWindow.rectTransform);
            UiBuild.SetReference(screen, "_shopTitle", shopTitle);
            UiBuild.SetReference(screen, "_shopHint", shopHint);
            UiBuild.SetReference(screen, "_shopCoins", shopCoins);
            UiBuild.SetReferences(screen, "_shopTiles", shopTiles);
            UiBuild.SetReference(screen, "_refresh", refreshButton);
            UiBuild.SetReference(screen, "_refreshCost", refreshCost);
            UiBuild.SetReference(screen, "_leave", leaveButton);
            UiBuild.SetReference(screen, "_buyToInventory", buyToInventory.Button);
            return screen;
        }

        /// <summary>
        /// A number beside a coin in a row that sizes it to its text (the layout gives it exactly its preferred width): it must
        /// overflow rather than be cut short, since a text cut to its own width with an ellipsis can lose every glyph to a
        /// rounding error and draw nothing (a two-digit price did, round 44). One line, no wrapping.
        /// </summary>
        static TextMeshProUGUI Numeral(TextMeshProUGUI text)
        {
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        /// <summary>The party's region coins in the header of an expedition screen (round 44): the coin and, after it, the number the screen writes.</summary>
        static TextMeshProUGUI BuildHeaderCoins(Transform header)
        {
            UiBuild.Box(KitIcon("CoinIcon", header, UiArt.Coin), HeaderCoinX, (84f - HeaderCoinSize) / 2f, HeaderCoinSize, HeaderCoinSize);
            TextMeshProUGUI coins = UiBuild.SingleLine(UiBuild.Label("Coins", header, 30f, UiPalette.TextDim));
            UiBuild.Box(coins, HeaderCoinX + HeaderCoinSize + 8f, 18f, 120f, 44f);
            return coins;
        }

        /// <summary>
        /// One offer of the shop (round 44, A): a tile with a rim (brass while picked) holding the offer as a board cell (an item with
        /// its icon and tier marks) or as a potion in its pocket, its name and kind under it, and its price with the coin at the
        /// bottom; "sold" over an empty tile. The whole tile is the button, built silent (the screen sounds what a click did); the
        /// cell inside takes no click, and a tile the coins do not cover is dimmed by the view, not by the button.
        /// </summary>
        static ShopTileView BuildShopTile(Transform parent, string name)
        {
            Image rim = Rounded(name, parent, UiPalette.Line, 8f);
            UiBuild.Size(rim, ShopTileWidth, ShopTileHeight);
            Button button = UiBuild.MakeButton(rim);
            UiBuild.Silence(button);
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            var group = rim.gameObject.AddComponent<CanvasGroup>();
            Image fill = Rounded(name + "Fill", rim.transform, UiPalette.Slot, 6f);
            UiBuild.Stretch(fill.rectTransform, 2f, 2f, 2f, 2f);

            ItemSlotView cell = BuildItemSlot(fill.transform, name + "Cell");
            cell.gameObject.SetActive(true);
            cell.GetComponent<Image>().raycastTarget = false;
            UiBuild.Place((RectTransform)cell.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -ShopTileCellTop), new Vector2(BattleItemView.CellWidth, BattleItemView.CellHeight));

            Image pocket = KitFrame(name + "Potion", fill.transform, UiArt.PotionSlot);
            UiBuild.Place(pocket.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -ShopTileCellTop), new Vector2(BattleItemView.CellHeight, BattleItemView.CellHeight));
            Image potionIcon = UiBuild.Image(name + "PotionIcon", pocket.transform, Color.white);
            potionIcon.preserveAspect = true;
            UiBuild.Stretch(potionIcon.rectTransform, 7f, 7f, 7f, 7f);
            pocket.gameObject.SetActive(false);

            TextMeshProUGUI tileName = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label(name + "Name", fill.transform, 19f, UiPalette.Text, TextAlignmentOptions.Center)), 13f);
            UiBuild.Line(tileName, ShopTileNameTop, 26f, 6f, 6f);
            TextMeshProUGUI sub = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label(name + "Sub", fill.transform, 16f, UiPalette.TextDim, TextAlignmentOptions.Center)), 11f);
            UiBuild.Line(sub, ShopTileSubTop, 22f, 6f, 6f);

            RectTransform priceRow = UiBuild.Line(UiBuild.Rect(name + "Price", fill.transform), ShopTilePriceTop, 28f);
            HorizontalLayoutGroup priceLayout = UiBuild.Horizontal(priceRow, 6f, 0, TextAnchor.MiddleCenter);
            priceLayout.childControlWidth = true;
            Image coin = KitIcon(name + "Coin", priceRow, UiArt.Coin);
            UiBuild.Size(coin, ShopTileCoinSize, ShopTileCoinSize);
            coin.gameObject.AddComponent<LayoutElement>().preferredWidth = ShopTileCoinSize;
            TextMeshProUGUI price = UiBuild.Size(Numeral(UiBuild.Label(name + "PriceText", priceRow, 24f, UiPalette.Virtue, TextAlignmentOptions.Center)), 60f, 28f);

            TextMeshProUGUI sold = UiBuild.SingleLine(UiBuild.LocalizedLabel(name + "Sold", fill.transform, UiKeys.Map.ShopSold, 22f, UiPalette.TextDim, TextAlignmentOptions.Center));
            UiBuild.Stretch(sold.rectTransform);
            sold.gameObject.SetActive(false);

            var view = rim.gameObject.AddComponent<ShopTileView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_rim", rim);
            UiBuild.SetReference(view, "_group", group);
            UiBuild.SetReference(view, "_cell", cell);
            UiBuild.SetReference(view, "_potionPocket", pocket.gameObject);
            UiBuild.SetReference(view, "_potionIcon", potionIcon);
            UiBuild.SetReference(view, "_name", tileName);
            UiBuild.SetReference(view, "_sub", sub);
            UiBuild.SetReference(view, "_priceRow", priceRow.gameObject);
            UiBuild.SetReference(view, "_price", price);
            UiBuild.SetReference(view, "_sold", sold);
            return view;
        }

        /// <summary>A shape with rounded corners of the given radius (Unity's built-in rounded sprite, sliced), in the color.</summary>
        static Image Rounded(string name, Transform parent, Color color, float radius)
        {
            Image image = UiBuild.Image(name, parent, color);
            image.sprite = UiBuild.BuiltinSprite("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = RoundedSpriteRadius / radius;
            return image;
        }

        /// <summary>The corner of the built-in rounded sprite at its own size.</summary>
        const float RoundedSpriteRadius = 10f;
    }
}
