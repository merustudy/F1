using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The loot screen (Slice B stage 18; the reward screen before it): the party as in battle (the stage on the left, the boards in
        /// the panel under it), the potions and the drops stacked on the right above the panel, and the hint and the buttons in the
        /// panel's right half.
        /// </summary>
        static UIScreen BuildLoot(Transform holder)
        {
            LootScreen screen = Screen<LootScreen>("LootScreen", holder, out RectTransform frame);

            Image header = KitFrame("Header", frame, UiArt.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 84f);
            Image titlePlate = KitFrame("TitlePlate", header.transform, UiArt.PlateLabel);
            UiBuild.Box(titlePlate, 960f - TitlePlateWidth / 2f, 4f, TitlePlateWidth, TitlePlateHeight);
            TextMeshProUGUI lootTitle = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.LocalizedLabel("LootTitle", titlePlate.transform, UiKeys.Loot.Title, 26f, UiPalette.Brass, TextAlignmentOptions.Center)), 18f);
            UiBuild.Stretch(lootTitle.rectTransform, 20f, 8f, 20f, 8f);
            TextMeshProUGUI coins = BuildHeaderCoins(header.transform);

            // The drops, one under the other, as wide as the right half, under the potions and above the panel. Three cards fit the room (BalanceData.MaxLootCards).
            RectTransform cards = UiBuild.Box(UiBuild.Rect("Cards", frame), 980f, PartyRightTop, 920f, BoardPanelTop - 16f - PartyRightTop);
            VerticalLayoutGroup stack = UiBuild.Vertical(cards, LootCardGap);
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            LootCardView cardTemplate = BuildLootCard(cards);

            PartySideView party = BuildPartySide(frame, 1230f, 170f, out Image panel);

            // The panel's right half: how to pick up, the picked drop's facts (the party side writes them), then the buttons.
            UiBuild.Box(UiBuild.LocalizedLabel("LootHint", panel.transform, UiKeys.Loot.Hint, 22f, UiPalette.TextDim), PanelRightX, PanelTitleTop, PanelRightWidth, 48f);
            ButtonParts toInventory = KitLocalizedButton("LootToInventory", panel.transform, UiKeys.Loot.ToInventory, UiPalette.ButtonQuiet, 24f);
            UiBuild.Silence(toInventory.Button);
            UiBuild.Box(toInventory.Rect, PanelRightX, PanelButtonsTop, 210f, PanelButtonHeight);
            ButtonParts inventoryToggle = KitButton("InventoryToggle", panel.transform, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(inventoryToggle.Rect, 1420f, PanelButtonsTop, 210f, PanelButtonHeight);
            ButtonParts leave = KitLocalizedButton("Leave", panel.transform, UiKeys.Loot.Leave, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(leave.Rect, 1650f, PanelButtonsTop, 230f, PanelButtonHeight);

            UiBuild.SetReference(screen, "_cardTemplate", cardTemplate);
            UiBuild.SetReference(screen, "_cardParent", cards);
            UiBuild.SetReference(screen, "_leave", leave.Button);
            UiBuild.SetReference(screen, "_toInventory", toInventory.Button);
            UiBuild.SetReference(screen, "_inventoryToggle", inventoryToggle.Button);
            UiBuild.SetReference(screen, "_inventoryToggleLabel", inventoryToggle.Label);
            UiBuild.SetReference(screen, "_party", party);
            UiBuild.SetReference(screen, "_coins", coins);
            return screen;
        }

        /// <summary>A loot card's height and the gap between two: three cards fill the room between the potions and the panel (2026-10-03 mockup V).</summary>
        const float LootCardHeight = 132f;
        const float LootCardGap = 8f;

        /// <summary>The stripe of an item's tier at the card's left edge (2026-10-06 round 35).</summary>
        const float LootStripeWidth = 6f;

        /// <summary>One loot card. The whole card is the button; the kind, the title and the action share the first line, the facts fill the rest.</summary>
        static LootCardView BuildLootCard(Transform parent)
        {
            Image frame = UiBuild.Image("CardTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(frame, 920f, LootCardHeight);
            Button button = UiBuild.MakeButton(frame);

            // Silent: a drop taken sounds as put in, a card picked as a click (LootScreen).
            UiBuild.Silence(button);
            Transform card = frame.transform;

            // The tier stripe down the left edge; the view colours and shows it for a tier above Common.
            Image stripe = UiBuild.Image("CardStripe", card, UiPalette.TierBronze);
            stripe.rectTransform.anchorMin = Vector2.zero;
            stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
            stripe.rectTransform.pivot = new Vector2(0f, 0.5f);
            stripe.rectTransform.offsetMin = Vector2.zero;
            stripe.rectTransform.offsetMax = new Vector2(LootStripeWidth, 0f);
            stripe.enabled = false;

            TextMeshProUGUI kind = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CardKind", card, 19f, UiPalette.TextDim), 20f, 12f, 110f, 28f));
            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CardTitle", card, 26f, UiPalette.Text), 130f, 6f, 500f, 38f));
            TextMeshProUGUI action = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CardAction", card, 22f, UiPalette.Text, TextAlignmentOptions.Right), 640f, 8f, 260f, 34f));
            TextMeshProUGUI body = UiBuild.Label("CardBody", card, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(body, 20f, 46f, 880f, LootCardHeight - 52f);

            var view = frame.gameObject.AddComponent<LootCardView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_stripe", stripe);
            UiBuild.SetReference(view, "_kind", kind);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_body", body);
            UiBuild.SetReference(view, "_action", action);

            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
