using System.Globalization;
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
        /// The party side of the node map and the reward screen, in the battle screen's shape
        /// (2026-10-03 mockup A): on the stage, the battle's party columns, each with the figure,
        /// the plate under its feet and the two buttons that move the member a row; under the stage,
        /// the battle's board panel, whose left half holds one line per row (the face, then the
        /// board side by side) and whose right half holds the screen's own words and buttons; the
        /// potions in the strip under the header; the inventory as a popup over the right half of
        /// the screen above the panel. The columns stand in a field shaped like the battle's, and
        /// the view puts them in the battle screen's places when it opens (FieldLayout), so
        /// everything inside a column is laid out as lines that stretch across it.
        /// Built after the right half of the screen so the popup draws over it.
        /// </summary>
        const float PartyMoveHeight = 40f;

        /// <summary>The popup over the right half: it stays above the board panel.</summary>
        const float InventoryPopupTop = 100f;
        const float InventoryPopupHeight = BoardPanelTop - InventoryPopupTop - 16f;

        /// <summary>From the top of a column: the plate, then the move buttons, each a gap under the one above.</summary>
        const float PartyPlateTop = BattleFigureHeight + PlateGap;
        const float PartyMoveTop = PartyPlateTop + PlateHeight + PlateGap;
        const float PartyColumnHeight = PartyMoveTop + PartyMoveHeight;

        /// <summary>
        /// The right half of the board panel, measured from the panel's top-left corner: a title
        /// line, a hint line, the selected item's facts, then the buttons.
        /// </summary>
        const float PanelRightX = 1000f;
        const float PanelRightWidth = 880f;
        const float PanelTitleTop = 24f;
        const float PanelHintTop = 72f;
        const float PanelDetailTop = 122f;
        const float PanelButtonsTop = 224f;
        const float PanelButtonHeight = 76f;

        /// <param name="toInventoryX">Where the "to inventory" button stands in the panel's button line; the screen puts its own buttons next to it.</param>
        /// <param name="panel">The board panel, for the screen's own words and buttons in its right half.</param>
        static PartySideView BuildPartySide(RectTransform frame, float toInventoryX, float toInventoryWidth, out Image panel)
        {
            // Potions where the battle screen has them.
            PotionSlotView potionTemplate = BuildPotionStrip(frame, out RectTransform potions);

            // The field is the battle field's box, so that the view can place the columns with the
            // battle's formula. As in battle, the rearmost row is built first so that the figure in
            // front is drawn over the one behind.
            RectTransform field = UiBuild.Box(UiBuild.Rect("PartyField", frame), BattleFieldLeft, BattleFieldTop, BattleFieldWidth, PartyColumnHeight);
            var columns = new PartyColumnView[BattleRows.Count];
            for (int row = BattleRows.Count; row >= BattleRows.Front; row--)
            {
                columns[row - 1] = BuildPartyColumn(field, row);
            }

            // The board panel of the battle screen: the party's lines in its left half.
            panel = KitFrame("BoardPanel", frame, UiArt.Panel);
            UiBuild.Box(panel, 0f, BoardPanelTop, 1920f, BoardPanelHeight);
            Image seam = UiBuild.Image("BoardSeam", panel.transform, new Color(UiPalette.Line.r, UiPalette.Line.g, UiPalette.Line.b, 0.47f));
            UiBuild.Box(seam, 959f, BoardSeamInset, 2f, BoardPanelHeight - 2f * BoardSeamInset);
            float linesTop = (BoardPanelHeight - (BattleRows.Count * BattleItemView.CellHeight + (BattleRows.Count - 1) * BoardLineGap)) / 2f;
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                float y = linesTop + (row - 1) * (BattleItemView.CellHeight + BoardLineGap);
                RectTransform line = UiBuild.Box(UiBuild.Rect("PartyLine" + row, panel.transform), BoardLineMargin, y, 1920f / 2f - BoardLineMargin, BattleItemView.CellHeight);
                BuildPartyLine(line, row, columns[row - 1]);
            }

            // The right half: the selected item's facts (or how to use the boards), and the button that takes it off its board.
            TextMeshProUGUI detail = UiBuild.Label("PartyDetail", panel.transform, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(detail, PanelRightX, PanelDetailTop, PanelRightWidth, 54f);
            ButtonParts toInventory = KitLocalizedButton("ToInventory", panel.transform, UiKeys.Board.ToInventory, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(toInventory.Rect, toInventoryX, PanelButtonsTop, toInventoryWidth, PanelButtonHeight);

            GameObject popup = BuildInventoryPopup(frame, out TMP_Text inventoryTitle, out GameObject inventoryEmpty, out InventoryEntryView entryTemplate, out RectTransform entryParent);

            var view = frame.gameObject.AddComponent<PartySideView>();
            UiBuild.SetReference(view, "_field", field);
            UiBuild.SetReferences(view, "_columns", columns);
            UiBuild.SetReference(view, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(view, "_potionParent", potions);
            UiBuild.SetReference(view, "_detail", detail);
            UiBuild.SetReference(view, "_toInventory", toInventory.Button);
            UiBuild.SetReference(view, "_inventoryPanel", popup);
            UiBuild.SetReference(view, "_inventoryTitle", inventoryTitle);
            UiBuild.SetReference(view, "_inventoryEmpty", inventoryEmpty);
            UiBuild.SetReference(view, "_entryTemplate", entryTemplate);
            UiBuild.SetReference(view, "_entryParent", entryParent);
            return view;
        }

        /// <summary>
        /// One row's column on the stage: the figure, then what is known of whoever stands there:
        /// the plate (the row, the name, HP and the job) and the two buttons that move the member a
        /// row. The view sets the column's place and width when the screen opens, so every part is
        /// a line that stretches across the column. Every object is named after the row, because a
        /// prefab must not repeat a name. The row's board is in its line of the panel
        /// (<see cref="BuildPartyLine"/>).
        /// </summary>
        static PartyColumnView BuildPartyColumn(RectTransform field, int row)
        {
            string p = "Party" + row;
            float w = BattleFieldWidth / (BattleRows.Count * 2);
            RectTransform column = UiBuild.Box(UiBuild.Rect(p + "Column", field), w * (BattleRows.Count - row), 0f, w, PartyColumnHeight);

            RectTransform figure = BuildFigure(column, p + "Figure", takesClicks: false, out FigureView figureView);
            UiBuild.Line(figure, 0f, BattleFigureHeight);

            // Everything under the figure is shown and hidden together: nobody stands in an empty row.
            RectTransform info = UiBuild.Rect(p + "Info", column);
            UiBuild.Stretch(info, 0f, PartyPlateTop, 0f, 0f);

            // The plate of the battle screen. The column is its row, so the badge never changes; the state line names the job.
            PlateParts plate = BuildPlate(info, p, UiArt.PlateParty, raycastTarget: false);
            UiBuild.Line(plate.Plate, 0f, PlateHeight);
            plate.Row.text = row.ToString(CultureInfo.InvariantCulture);
            TextMeshProUGUI job = PlateStateLine(UiBuild.SingleLine(UiBuild.Label(p + "Job", plate.Plate.transform, 15f, UiPalette.TextDim)));

            // Forward goes towards row 1 (to the right), back the other way: the left and the right half of one line.
            // Half a column is narrow, so the labels may shrink.
            float moveTop = PartyMoveTop - PartyPlateTop;
            ButtonParts forward = KitLocalizedButton(p + "Forward", info, UiKeys.Board.Forward, UiPalette.ButtonQuiet, 20f);
            UiBuild.Line(forward.Rect, moveTop, PartyMoveHeight);
            forward.Rect.anchorMax = new Vector2(0.5f, 1f);
            forward.Rect.offsetMax = new Vector2(-2f, forward.Rect.offsetMax.y);
            FitMoveLabel(forward.Label);
            ButtonParts back = KitLocalizedButton(p + "Back", info, UiKeys.Board.Back, UiPalette.ButtonQuiet, 20f);
            UiBuild.Line(back.Rect, moveTop, PartyMoveHeight);
            back.Rect.anchorMin = new Vector2(0.5f, 1f);
            back.Rect.offsetMin = new Vector2(2f, back.Rect.offsetMin.y);
            FitMoveLabel(back.Label);

            var view = column.gameObject.AddComponent<PartyColumnView>();
            UiBuild.SetReference(view, "_figure", figure.gameObject);
            UiBuild.SetReference(view, "_figureView", figureView);
            UiBuild.SetReference(view, "_card", info.gameObject);
            UiBuild.SetReference(view, "_name", plate.Name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_hp", plate.Hp);
            UiBuild.SetReference(view, "_hpBar", plate.HpBar);
            UiBuild.SetReference(view, "_forward", forward.Button);
            UiBuild.SetReference(view, "_back", back.Button);
            return view;
        }

        /// <summary>
        /// One row's line of the board panel, as in battle: the face in a plate frame with the row
        /// badge at its corner, then the board's cells side by side. The row's column view shows
        /// and fills it. Every object is named after the row.
        /// </summary>
        static void BuildPartyLine(RectTransform line, int row, PartyColumnView view)
        {
            string p = "Party" + row;
            UiBuild.Horizontal(line, BoardFaceGap, 0, TextAnchor.MiddleLeft);

            Image frame = KitFrame(p + "Face", line, UiArt.PlateParty, 0.5f);
            UiBuild.Size(frame, BoardFaceSize, BoardFaceSize);
            Image placeholder = UiBuild.Image(p + "FacePlaceholder", frame.transform, UiPalette.ButtonQuiet);
            placeholder.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Stretch(placeholder.rectTransform, 12f, 12f, 12f, 12f);
            Image face = UiBuild.Image(p + "FaceArt", frame.transform, Color.white);
            face.preserveAspect = true;
            UiBuild.Stretch(face.rectTransform, BoardFaceInset, BoardFaceInset, BoardFaceInset, BoardFaceInset);
            face.enabled = false;
            // Named apart from the plate's badge in the column of the same row.
            TextMeshProUGUI number = KitBadge(frame.transform, p + "FaceBadge", p + "FaceRow", 14f, out Image badge);
            UiBuild.Place(badge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(2f, 2f), new Vector2(22f, 22f));
            number.text = row.ToString(CultureInfo.InvariantCulture);

            // The board: the battle's cells side by side, as many as a board can have at most. The view sizes them at runtime.
            RectTransform cells = UiBuild.Rect(p + "Cells", line);
            UiBuild.Size(cells, BattleItemView.BoardWidth(JobData.MaxItemSlots), BattleItemView.CellHeight);
            UiBuild.Horizontal(cells, BattleItemView.CellGapX, 0, TextAnchor.MiddleLeft);
            ItemSlotView slotTemplate = BuildItemSlot(cells, p + "CellTemplate");

            UiBuild.SetReference(view, "_line", line.gameObject);
            UiBuild.SetReference(view, "_face", face);
            UiBuild.SetReference(view, "_facePlaceholder", placeholder.gameObject);
            UiBuild.SetReference(view, "_slotTemplate", slotTemplate);
            UiBuild.SetReference(view, "_slotParent", cells);
        }

        /// <summary>The label of a move button: closer to the button's edges than usual, and smaller when the word is long.</summary>
        static void FitMoveLabel(TextMeshProUGUI label)
        {
            UiBuild.Stretch(label.rectTransform, 6f, 4f, 6f, 6f);
            UiBuild.ShrinkToFit(UiBuild.SingleLine(label), 14f);
        }

        /// <summary>
        /// One cell of a board, in a slot of the kit: the name with the grade under it, in one text
        /// (the view writes the two lines). The lines never wrap; a name too long for the cell
        /// shrinks. It is as high as a cell; its width is set at runtime, the cells its item takes.
        /// </summary>
        static ItemSlotView BuildItemSlot(Transform parent, string name)
        {
            Image frame = KitFrame(name, parent, UiArt.Slot);
            UiBuild.Size(frame, BattleItemView.CellWidth, BattleItemView.CellHeight);
            Button button = UiBuild.MakeButton(frame);

            // Words: the empty cell, or the name and grade of an item without an icon.
            TextMeshProUGUI text = UiBuild.Label(name + "Text", frame.transform, 19f, UiPalette.Text);
            UiBuild.Stretch(text.rectTransform, 14f, 4f, 12f, 4f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiBuild.ShrinkToFit(text, 13f);

            // The icon in the place the battle's cell gives it, and the grade badge at the bottom-left corner over it.
            Image icon = UiBuild.Image(name + "Icon", frame.transform, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform, ItemIconMarginX, ItemIconMarginY, ItemIconMarginX, ItemIconMarginY);
            TextMeshProUGUI grade = KitBadge(frame.transform, name + "Badge", name + "Grade", 14f, out Image badge);
            UiBuild.Place(badge.rectTransform, Vector2.zero, Vector2.zero, new Vector2(GradeBadgeInset, GradeBadgeInset), new Vector2(GradeBadgeSize, GradeBadgeSize));

            var view = frame.gameObject.AddComponent<ItemSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_plain", UiArt.Load(UiArt.Slot));
            UiBuild.SetReference(view, "_selected", UiArt.Load(UiArt.SlotSelected));
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_badge", badge.gameObject);
            UiBuild.SetReference(view, "_grade", grade);
            UiBuild.SetReference(view, "_text", text);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The inventory popup: a dimmed cover over the right half of the screen above the board
        /// panel, with a box that lists the items top to bottom. The screen's toggle button opens
        /// and closes it; the view fills it.
        /// </summary>
        static GameObject BuildInventoryPopup(Transform frame, out TMP_Text title, out GameObject empty, out InventoryEntryView entryTemplate, out RectTransform entryParent)
        {
            Image cover = UiBuild.Image("InventoryPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Box(cover, 970f, InventoryPopupTop, 940f, InventoryPopupHeight);
            Image box = UiBuild.Panel("InventoryBox", cover.transform, UiPalette.Panel);
            UiBuild.Box(box, 10f, 10f, 920f, InventoryPopupHeight - 20f);
            title = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("InventoryTitle", box.transform, 32f, UiPalette.Text), 24f, 20f, 600f, 44f));
            UiBuild.Box(UiBuild.LocalizedLabel("InventoryHint", box.transform, UiKeys.Board.InventoryHint, 20f, UiPalette.TextDim), 24f, 70f, 872f, 30f);
            ScrollRect scroll = UiBuild.VerticalScroll("InventoryList", box.transform, 16f, out entryParent);
            UiBuild.Box((RectTransform)scroll.transform, 24f, 110f, 872f, InventoryPopupHeight - 20f - 130f);
            entryTemplate = BuildInventoryEntry(entryParent);
            empty = UiBuild.Box(UiBuild.LocalizedLabel("InventoryEmpty", box.transform, UiKeys.Board.InventoryEmpty, 22f, UiPalette.TextDim), 40f, 122f, 400f, 32f).gameObject;

            cover.gameObject.SetActive(false);
            return cover.gameObject;
        }

        /// <summary>One line of the inventory popup: the item's title over a summary of its facts.</summary>
        static InventoryEntryView BuildInventoryEntry(Transform parent)
        {
            Image frame = UiBuild.Image("InventoryEntryTemplate", parent, UiPalette.Slot);
            UiBuild.Size(frame, 800f, 76f);
            var element = frame.gameObject.AddComponent<LayoutElement>();
            element.minHeight = 76f;
            element.preferredHeight = 76f;
            Button button = UiBuild.MakeButton(frame);

            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Label("InventoryEntryTitle", frame.transform, 24f, UiPalette.Text));
            UiBuild.Stretch(title.rectTransform, 16f, 6f, 16f, 36f);
            TextMeshProUGUI facts = UiBuild.SingleLine(UiBuild.Label("InventoryEntryFacts", frame.transform, 18f, UiPalette.TextDim));
            UiBuild.Stretch(facts.rectTransform, 16f, 40f, 16f, 8f);

            var view = frame.gameObject.AddComponent<InventoryEntryView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_facts", facts);

            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
