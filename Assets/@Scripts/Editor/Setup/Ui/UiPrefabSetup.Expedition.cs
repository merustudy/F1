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
        /// The expedition screen (Round 71 E2): the painted map at the left with a marker per dungeon (cloned from a template at
        /// runtime) and the home marker, the header, the details of the chosen dungeon over the painting's dark right third, and the
        /// party strip with the departure at the bottom. The map's sprite is loaded at runtime (ArtAddress.ExpeditionMap).
        /// </summary>
        static UIScreen BuildExpedition(Transform holder)
        {
            ExpeditionScreen screen = Screen<ExpeditionScreen>("ExpeditionScreen", holder, out RectTransform frame);

            // The map first, so that everything else is drawn over it.
            Image map = UiBuild.Image("Map", frame, Color.white);
            map.preserveAspect = false;
            UiBuild.Box(map, 0f, 0f, ExpeditionScreen.MapWidth, ExpeditionScreen.MapHeight);

            // The home marker: a small disc and its plate, where the painting put the town.
            Vector2 home = new Vector2(ExpeditionScreen.MapWidth * ExpeditionScreen.HomeXPercent / 100f, ExpeditionScreen.MapHeight * ExpeditionScreen.HomeYPercent / 100f);
            Image homeRing = Disc("HomeRing", map.transform, UiPalette.Bone, 34f);
            UiBuild.Box(homeRing, home.x - 17f, home.y - 17f, 34f, 34f);
            Image homeDisc = Disc("HomeDisc", map.transform, UiPalette.ButtonQuiet, 28f);
            UiBuild.Box(homeDisc, home.x - 14f, home.y - 14f, 28f, 28f);
            Image homePlate = UiBuild.Panel("HomePlate", map.transform, UiPalette.LabelBox);
            UiBuild.Box(homePlate, home.x + 26f, home.y - 20f, 90f, 40f);
            UiBuild.Box(UiBuild.LocalizedLabel("HomeName", homePlate.transform, UiKeys.Expedition.Home, 20f, UiPalette.TextDim, TextAlignmentOptions.Center), 0f, 0f, 90f, 40f);

            DungeonMarkerView markerTemplate = BuildDungeonMarker(map.transform);

            // Header
            Image header = UiBuild.Panel("Header", frame, UiPalette.Background);
            UiBuild.Box(header, 0f, 0f, 1920f, 100f);
            UiBuild.Box(UiBuild.LocalizedLabel("HeaderTitle", header.transform, UiKeys.Expedition.Title, 44f, UiPalette.Text), 40f, 20f, 600f, 60f);
            TextMeshProUGUI day = UiBuild.Label("Day", header.transform, 44f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(day, 660f, 20f, 600f, 60f);
            TextMeshProUGUI gold = UiBuild.Label("Gold", header.transform, 32f, UiPalette.DiabloGold, TextAlignmentOptions.Right);
            UiBuild.Box(gold, 1340f, 26f, 280f, 48f);
            ButtonParts toLobby = UiBuild.LocalizedButton("ToLobby", header.transform, UiKeys.Expedition.ToLobby, UiPalette.ButtonQuiet, 28f);
            UiBuild.Box(toLobby.Rect, 1640f, 22f, 240f, 56f);

            // Details of the chosen dungeon: the name, then rows of a dim label at the left and the value at the right (the mock),
            // and the hints under them.
            Image details = UiBuild.Panel("Details", frame, Tinted(UiPalette.Panel, 0.9f));
            UiBuild.Box(details, 1320f, 120f, 560f, 640f);
            Transform d = details.transform;
            UiBuild.Box(UiBuild.LocalizedLabel("DetailsHeader", d, UiKeys.Lobby.Expedition, 30f, UiPalette.TextDim), 26f, 24f, 300f, 40f);
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("Name", d, 40f, UiPalette.Text)), 26f, 86f, 508f, 52f);
            TextMeshProUGUI affinity = DetailRow(d, "Affinity", UiKeys.Expedition.AffinityLabel, 170f);
            TextMeshProUGUI floors = DetailRow(d, "Floors", UiKeys.Expedition.FloorsLabel, 222f);
            TextMeshProUGUI days = DetailRow(d, "Days", UiKeys.Expedition.DaysLabel, 274f);
            TextMeshProUGUI potions = DetailRow(d, "Potions", UiKeys.Expedition.PotionsLabel, 326f);
            TextMeshProUGUI cleared = DetailRow(d, "Cleared", UiKeys.Expedition.ClearedLabel, 378f);
            TextMeshProUGUI reward = DetailRow(d, "Reward", UiKeys.Expedition.RewardLabel, 430f);
            TextMeshProUGUI hint = UiBuild.Box(UiBuild.Label("Hint", d, 20f, UiPalette.TextDim, TextAlignmentOptions.TopLeft), 26f, 520f, 508f, 100f);

            // The party as the lobby formed it, and the departure
            Image bottom = UiBuild.Panel("Bottom", frame, Tinted(UiPalette.Panel, 0.9f));
            UiBuild.Box(bottom, 40f, 780f, 1840f, 260f);
            Transform b = bottom.transform;
            UiBuild.Box(UiBuild.LocalizedLabel("PartyHeader", b, UiKeys.Expedition.Party, 24f, UiPalette.TextDim), 24f, 22f, 900f, 36f);
            var cards = new ExpeditionPartyCardView[BattleRows.Count];
            for (int i = 0; i < cards.Length; i++)
            {
                int row = i + 1;
                Image slot = UiBuild.Panel("PartyRow" + row, b, UiPalette.PanelLight);
                UiBuild.Box(slot, 24f + 300f * i, 70f, 280f, 160f);
                var card = slot.gameObject.AddComponent<ExpeditionPartyCardView>();
                UiBuild.Box(UiBuild.LocalizedLabel("PartyRowLabel" + row, slot.transform, UiText.RowKey(row), 20f, UiPalette.TextDim), 20f, 14f, 60f, 30f);
                TextMeshProUGUI cardName = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PartyRowName" + row, slot.transform, 26f, UiPalette.Text)), 80f, 10f, 128f, 36f);
                TextMeshProUGUI cardLevel = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PartyRowLevel" + row, slot.transform, 18f, UiPalette.Bone, TextAlignmentOptions.Right)), 208f, 14f, 52f, 28f);
                TextMeshProUGUI cardDetail = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PartyRowDetail" + row, slot.transform, 20f, UiPalette.TextDim)), 20f, 56f, 240f, 32f);
                // Fatigue as the pips every screen shows (12_UI "피로도와 붕괴의 상태"), under the job.
                RectTransform pipsRow = UiBuild.Box(UiBuild.Rect("PartyRowPips" + row, slot.transform), 20f, 100f, 240f, 14f);
                UiBar[] pips = FatiguePipRow("PartyRow" + row + "Pip", pipsRow, 240f, 14f);
                UiBuild.SetReference(card, "_name", cardName);
                UiBuild.SetReference(card, "_level", cardLevel);
                UiBuild.SetReference(card, "_detail", cardDetail);
                UiBuild.SetReferences(card, "_pips", pips);
                cards[i] = card;
            }

            TextMeshProUGUI status = UiBuild.Label("Status", b, 22f, UiPalette.Text);
            UiBuild.Box(status, 1520f, 52f, 300f, 36f);
            ButtonParts depart = UiBuild.LocalizedButton("Depart", b, UiKeys.Expedition.Depart, UiPalette.Button, 34f);
            UiBuild.Silence(depart.Button);
            UiBuild.Box(depart.Rect, 1520f, 100f, 294f, 120f);

            UiBuild.SetReference(screen, "_day", day);
            UiBuild.SetReference(screen, "_gold", gold);
            UiBuild.SetReference(screen, "_toLobby", toLobby.Button);
            UiBuild.SetReference(screen, "_map", map);
            UiBuild.SetReference(screen, "_markerTemplate", markerTemplate);
            UiBuild.SetReference(screen, "_name", name);
            UiBuild.SetReference(screen, "_affinity", affinity);
            UiBuild.SetReference(screen, "_floors", floors);
            UiBuild.SetReference(screen, "_days", days);
            UiBuild.SetReference(screen, "_potions", potions);
            UiBuild.SetReference(screen, "_cleared", cleared);
            UiBuild.SetReference(screen, "_reward", reward);
            UiBuild.SetReference(screen, "_hint", hint);
            UiBuild.SetReferences(screen, "_cards", cards);
            UiBuild.SetReference(screen, "_status", status);
            UiBuild.SetReference(screen, "_depart", depart.Button);
            return screen;
        }

        /// <summary>A row of the details: the dim label at the left, and the value code fills in, right-aligned at the panel's inset.</summary>
        static TextMeshProUGUI DetailRow(Transform panel, string name, string labelKey, float y)
        {
            UiBuild.Box(UiBuild.LocalizedLabel(name + "Label", panel, labelKey, 24f, UiPalette.TextDim), 26f, y, 130f, 40f);
            return UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name, panel, 24f, UiPalette.Text, TextAlignmentOptions.Right)), 156f, y, 378f, 40f);
        }

        /// <summary>The template of a dungeon's marker: the selection ring, the rim, the disc (the button) and the plate under it.</summary>
        static DungeonMarkerView BuildDungeonMarker(Transform parent)
        {
            RectTransform rect = UiBuild.Box(UiBuild.Rect("MarkerTemplate", parent), 0f, 0f, DungeonMarkerView.Width, DungeonMarkerView.Height);
            var view = rect.gameObject.AddComponent<DungeonMarkerView>();
            Vector2 c = DungeonMarkerView.DiscCenter;

            Image selection = Disc("MarkerSelection", rect, UiPalette.Selected, 80f);
            UiBuild.Box(selection, c.x - 40f, c.y - 40f, 80f, 80f);
            Image ring = Disc("MarkerRing", rect, UiPalette.Brass, 60f);
            UiBuild.Box(ring, c.x - 30f, c.y - 30f, 60f, 60f);
            Image disc = Disc("MarkerDisc", rect, UiPalette.PanelLight, 52f, raycastTarget: true);
            UiBuild.Box(disc, c.x - 26f, c.y - 26f, 52f, 52f);
            Button button = UiBuild.MakeButton(disc);

            // The plate under the disc, as wide as the marker less a margin: a locked dungeon's second line is the longest.
            Image plate = UiBuild.Panel("MarkerPlate", rect, UiPalette.LabelBox);
            UiBuild.Box(plate, 10f, 66f, 300f, 70f);
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("MarkerName", plate.transform, 24f, UiPalette.Text, TextAlignmentOptions.Center)), 0f, 6f, 300f, 32f);
            TextMeshProUGUI detail = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("MarkerDetail", plate.transform, 18f, UiPalette.TextDim, TextAlignmentOptions.Center)), 0f, 38f, 300f, 28f);

            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_selection", selection);
            UiBuild.SetReference(view, "_ring", ring);
            UiBuild.SetReference(view, "_disc", disc);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_detail", detail);
            rect.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A round image of Unity's built-in knob sprite, as the node map draws its nodes.</summary>
        static Image Disc(string name, Transform parent, Color color, float size, bool raycastTarget = false)
        {
            Image image = UiBuild.Image(name, parent, color, raycastTarget);
            image.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Size(image, size, size);
            return image;
        }
    }
}
