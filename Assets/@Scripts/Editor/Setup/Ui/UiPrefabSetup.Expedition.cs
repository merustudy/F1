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
        /// runtime) and the home marker, the header, the details of the chosen dungeon over the painting's dark right third with the
        /// status and the way into the party popup under them (Round 77: no strip), and the party popup (Rounds 75 B and 76 D). The
        /// map's sprite is loaded at runtime (ArtAddress.ExpeditionMap).
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
            UiBuild.Box(details, 1320f, 120f, 560f, 920f);
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

            // Round 77 (2026-10-17, the user: the strip is not needed): the party is seen and formed in the popup. Under the details a
            // line, the status with the party's size first, and the two buttons that open the popup — [용병 목록] to look and form,
            // [출발] to set out (the popup's own [출발] departs). Both are clicks like any other.
            UiBuild.Box(UiBuild.Image("DetailsLine", d, UiPalette.Line), 26f, 670f, 508f, 1f);
            TextMeshProUGUI status = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("Status", d, 22f, UiPalette.Text)), 26f, 692f, 508f, 34f);
            ButtonParts roster = UiBuild.LocalizedButton("Roster", d, UiKeys.Expedition.Roster, UiPalette.ButtonQuiet, 28f);
            UiBuild.Box(roster.Rect, 26f, 740f, 254f, 140f);
            ButtonParts depart = UiBuild.LocalizedButton("Depart", d, UiKeys.Expedition.Depart, UiPalette.Button, 34f);
            UiBuild.Box(depart.Rect, 300f, 740f, 234f, 140f);

            // The shade over the map (the header stays clear) while the party popup is open, and the popup over it (Round 75 B, Round 76 D,
            // ArtPipeline/Archive/76-party-figures/mock-figures-D-stage-list.png): the title with the chosen dungeon's line, the stage at
            // the left with the party standing as the node map's party side stands it, the roster's list at the right, the status and the
            // departure under the stage.
            Image shade = UiBuild.Image("Shade", frame, TownShade, raycastTarget: true);
            UiBuild.Box(shade, 0f, 100f, 1920f, 980f);
            shade.gameObject.SetActive(false);
            Image partyWindow = Window(frame, "PartyWindow", new Rect(PartyWindowX, PartyWindowY, PartyWindowWidth, PartyWindowHeight), UiKeys.Expedition.PartyTitle, "PartyClose", out ButtonParts partyClose);
            Transform w = partyWindow.transform;
            TextMeshProUGUI partyDungeon = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PartyDungeon", w, 22f, UiPalette.Text)), 170f, 24f, 900f, 34f);

            Image stage = UiBuild.Panel("Stage", w, UiPalette.Slot);
            UiBuild.Box(stage, PartyStageX, PartyStageY, PartyStageWidth, PartyStageHeight);
            UiBuild.Box(UiBuild.Image("StageFloorLine", stage.transform, UiPalette.Line), 20f, PartyStageFoot + 1f, PartyStageWidth - 40f, 2f);
            var slots = new PartyRowSlotView[BattleRows.Count];
            for (int i = 0; i < slots.Length; i++)
            {
                int row = i + 1;
                slots[i] = BuildPartyStageColumn(stage.transform, row, PartyStageColumnWidth * (BattleRows.Count - row));
            }

            TextMeshProUGUI partyStatus = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PartyStatus", w, 24f, UiPalette.Text)), PartyStageX + 24f, PartyStageY + PartyStageHeight - 68f, 600f, 40f);
            ButtonParts partyDepart = UiBuild.LocalizedButton("PartyDepart", w, UiKeys.Expedition.Depart, UiPalette.Button, 30f);
            UiBuild.Silence(partyDepart.Button);
            UiBuild.Box(partyDepart.Rect, PartyStageX + PartyStageWidth - 24f - 294f, PartyStageY + PartyStageHeight - 80f - 24f, 294f, 64f);

            TextMeshProUGUI rosterHeader = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RosterHeader", w, 20f, UiPalette.TextDim)), PartyListX, 86f, PartyListWidth, 30f);
            RectTransform list = UiBuild.Box(UiBuild.Rect("List", w), PartyListX, 120f, PartyListWidth, PartyWindowHeight - 120f - 24f);
            UiBuild.Vertical(list, 8f);
            PartyListRowView rowTemplate = BuildPartyListRow(list);
            GameObject emptyTemplate = BuildEmptySlot(list);

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
            UiBuild.SetReference(screen, "_status", status);
            UiBuild.SetReference(screen, "_roster", roster.Button);
            UiBuild.SetReference(screen, "_depart", depart.Button);
            UiBuild.SetReference(screen, "_shade", shade.gameObject);
            UiBuild.SetReference(screen, "_partyWindow", partyWindow.gameObject);
            UiBuild.SetReference(screen, "_partyDungeon", partyDungeon);
            UiBuild.SetReferences(screen, "_slots", slots);
            UiBuild.SetReference(screen, "_rosterHeader", rosterHeader);
            UiBuild.SetReference(screen, "_rowTemplate", rowTemplate);
            UiBuild.SetReference(screen, "_emptyTemplate", emptyTemplate);
            UiBuild.SetReference(screen, "_partyStatus", partyStatus);
            UiBuild.SetReference(screen, "_partyDepart", partyDepart.Button);
            UiBuild.SetReference(screen, "_partyClose", partyClose.Button);
            return screen;
        }

        // The party popup (Round 76 D): the window, its stage at the left and its list at the right.
        const float PartyWindowX = 40f;
        const float PartyWindowY = 110f;
        const float PartyWindowWidth = 1840f;
        const float PartyWindowHeight = 900f;
        const float PartyStageX = 24f;
        const float PartyStageY = 84f;
        const float PartyStageWidth = 1080f;
        const float PartyStageHeight = PartyWindowHeight - PartyStageY - 24f;
        const float PartyStageColumnWidth = PartyStageWidth / BattleRows.Count;
        /// <summary>The floor line of the stage, where the figures' feet stand (the mock: 440 under the window's top).</summary>
        const float PartyStageFoot = 356f;
        const float PartyListX = 1128f;
        const float PartyListWidth = PartyWindowWidth - PartyListX - 24f;
        const float PartyListRowHeight = 86f;

        /// <summary>
        /// A column of the popup's stage (the mock): the figure place of the battle and the node map (225x300, the shadow, the
        /// placeholder) standing on the floor line, or the dim empty place with its words; under the feet the row label, the name and
        /// the level, the job line (the empty row's hint in its place), the fatigue pips, and the moves — forward, back.
        /// </summary>
        static PartyRowSlotView BuildPartyStageColumn(Transform parent, int row, float x)
        {
            float w = PartyStageColumnWidth;
            RectTransform column = UiBuild.Box(UiBuild.Rect("PartySlot" + row, parent), x, 0f, w, PartyStageHeight);
            float cx = w / 2f;
            RectTransform figure = BuildFigure(column, "SlotFigure" + row, takesClicks: false, out FigureView figureView);
            UiBuild.Box(figure, cx - BattleFigureWidth / 2f, PartyStageFoot - BattleFigureHeight, BattleFigureWidth, BattleFigureHeight);
            Image empty = UiBuild.Panel("SlotEmpty" + row, column, Tinted(UiPalette.Slot, 0.5f));
            UiBuild.Box(empty, cx - 90f, PartyStageFoot - BattleFigureHeight, 180f, BattleFigureHeight);
            UiBuild.Box(UiBuild.LocalizedLabel("SlotEmptyLabel" + row, empty.transform, UiKeys.Lobby.EmptyRow, 24f, UiPalette.TextDim, TextAlignmentOptions.Center), 0f, 136f, 180f, 32f);

            float top = PartyStageFoot + 16f;
            UiBuild.Box(UiBuild.LocalizedLabel("SlotRowLabel" + row, column, UiText.RowKey(row), 20f, UiPalette.TextDim), cx - 110f, top, 44f, 28f);
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("SlotName" + row, column, 26f, UiPalette.Text)), cx - 66f, top - 3f, 126f, 34f);
            TextMeshProUGUI level = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("SlotLevel" + row, column, 18f, UiPalette.Bone, TextAlignmentOptions.Right)), cx + 60f, top + 2f, 50f, 26f);
            TextMeshProUGUI detail = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("SlotDetail" + row, column, 20f, UiPalette.TextDim)), cx - 110f, top + 34f, 220f, 28f);
            RectTransform pipsRow = UiBuild.Box(UiBuild.Rect("SlotPips" + row, column), cx - 110f, top + 66f, 220f, 12f);
            UiBar[] pips = FatiguePipRow("Slot" + row + "Pip", pipsRow, 220f, 12f);
            ButtonParts forward = UiBuild.LocalizedButton("SlotForward" + row, column, UiKeys.Expedition.Forward, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(forward.Rect, cx - 110f, top + 88f, 106f, 40f);
            ButtonParts back = UiBuild.LocalizedButton("SlotBack" + row, column, UiKeys.Expedition.Back, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(back.Rect, cx + 4f, top + 88f, 106f, 40f);

            var view = column.gameObject.AddComponent<PartyRowSlotView>();
            UiBuild.SetReference(view, "_figure", figureView);
            UiBuild.SetReference(view, "_empty", empty.gameObject);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_level", level);
            UiBuild.SetReference(view, "_detail", detail);
            UiBuild.SetReferences(view, "_pips", pips);
            UiBuild.SetReference(view, "_forward", forward.Button);
            UiBuild.SetReference(view, "_back", back.Button);
            return view;
        }

        /// <summary>
        /// The template of a line of the popup's list (the mock, as the barracks lists the roster): a small figure, the name, the job,
        /// the level, the passive, the pips, the row tag and the party button. The screen clones it per mercenary.
        /// </summary>
        static PartyListRowView BuildPartyListRow(Transform parent)
        {
            Image background = UiBuild.Panel("RowTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, PartyListWidth, PartyListRowHeight);
            Transform line = background.transform;
            UiBuild.Box(UiBuild.Panel("RowPortraitBox", line, UiPalette.Slot), 10f, 7f, 56f, 72f);
            Image portrait = UiBuild.Image("RowPortrait", line, Color.white);
            UiBuild.Box(portrait, 10f, 7f, 56f, 72f);
            portrait.preserveAspect = true;
            portrait.enabled = false;
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RowName", line, 24f, UiPalette.Text)), 80f, 6f, 126f, 32f);
            TextMeshProUGUI job = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RowJob", line, 18f, UiPalette.TextDim)), 210f, 11f, 96f, 26f);
            TextMeshProUGUI level = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RowLevel", line, 17f, UiPalette.Bone)), 310f, 11f, 70f, 26f);
            TextMeshProUGUI passive = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RowPassive", line, 15f, UiPalette.TextDim)), 80f, 38f, 440f, 24f);
            RectTransform pipsRow = UiBuild.Box(UiBuild.Rect("RowPips", line), 80f, 66f, 320f, 9f);
            UiBar[] pips = FatiguePipRow("RowPip", pipsRow, 320f, 9f);
            TextMeshProUGUI row = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("RowTag", line, 20f, UiPalette.Brass, TextAlignmentOptions.Right)), PartyListWidth - 200f, 30f, 60f, 28f);
            ButtonParts button = UiBuild.Button("RowButton", line, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(button.Rect, PartyListWidth - 124f, 23f, 112f, 40f);

            var view = background.gameObject.AddComponent<PartyListRowView>();
            UiBuild.SetReference(view, "_frame", background);
            UiBuild.SetReference(view, "_portrait", portrait);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_level", level);
            UiBuild.SetReference(view, "_passive", passive);
            UiBuild.SetReference(view, "_row", row);
            UiBuild.SetReferences(view, "_pips", pips);
            UiBuild.SetReference(view, "_button", button.Button);
            UiBuild.SetReference(view, "_buttonLabel", button.Label);
            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The template of an empty line of the list: a thin rim and the words. The screen clones one per place left in the roster.</summary>
        static GameObject BuildEmptySlot(Transform parent)
        {
            RectTransform slot = UiBuild.Rect("EmptyTemplate", parent);
            UiBuild.Size(slot, PartyListWidth, PartyListRowHeight);
            Rim("EmptyRim", slot, 1f, UiPalette.Line);
            UiBuild.Box(UiBuild.LocalizedLabel("EmptyLabel", slot, UiKeys.Expedition.EmptySlot, 18f, UiPalette.TextDim, TextAlignmentOptions.Center), 0f, 0f, PartyListWidth, PartyListRowHeight);
            slot.gameObject.SetActive(false);
            return slot.gameObject;
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
