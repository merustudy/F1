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
        /// The party side of the node map and the loot screen, in the battle screen's shape
        /// (2026-10-03 mockups A and V): on the stage, the battle's party columns, each with the
        /// figure, the marks under its feet and the two buttons that move the member a row; under
        /// the stage, the battle's board panel, with the board of each row (its head with the row and
        /// the name, its cells) stacked in the panel column under its figure on a table that holds only the boards, and the
        /// screen's own words and buttons on a plate of their own under the right half (2026-10-08 round 46, A); the
        /// potions in the battle's strip at the battle's place, top left; the right half (the map, the loot, the inventory
        /// popup) from under the header down to that plate. The columns stand in a field shaped like the battle's, and the view puts them
        /// and the panel columns in the battle screen's places when it opens (FieldLayout), so
        /// everything inside a column is laid out as lines that stretch across it.
        /// Built after the right half of the screen so the popup draws over it.
        /// </summary>
        const float PartyMoveHeight = 40f;

        /// <summary>Where the potion strip stands: where the battle has it, top left under the header (round 46; the figures stand clear of it).</summary>
        const float PartyPotionsX = BattlePotionsX;

        /// <summary>
        /// What the right half holds (the map, the loot, the inventory popup; round 46): from under the header, the potion strip's
        /// top in battle, down to the right half's table under it.
        /// </summary>
        const float PartyRightTop = 92f;
        const float PartyRightBottom = BoardPanelTop + PanelRightTop - PanelRightGap;
        const float PartyRightHeight = PartyRightBottom - PartyRightTop;

        /// <summary>The popup over the right half: the right half's whole height.</summary>
        const float InventoryPopupTop = PartyRightTop;
        const float InventoryPopupHeight = PartyRightHeight;

        /// <summary>The party's field stands as high as it must for the move buttons to clear the panel by this gap.</summary>
        const float PartyPanelGap = 12f;
        const float PartyFieldTop = BoardPanelTop - PartyPanelGap - PartyColumnHeight;

        /// <summary>
        /// From the top of a column: the figure, the marks under its feet (as in battle, the state line naming the job and the
        /// member's state), then the move buttons, each a gap under the one above. The marks are lower than the plates they
        /// replaced (2026-10-05 round 29), and the column stands on the panel, so its figures came 46 down with them (and 4 more
        /// with the fatigue pips, 2026-10-06 round 36).
        /// </summary>
        const float PartyMarksTop = BattleFigureHeight + MarksGap;
        const float PartyMoveTop = PartyMarksTop + MarksHeight + MarksGap;
        const float PartyColumnHeight = PartyMoveTop + PartyMoveHeight;

        /// <summary>
        /// The board panel's tables (round 46, A): the boards' table as wide as the boards' half, and the right half's own table
        /// under the map, its top PanelRightTop under the panel's (half the plate of before: 218 of 436).
        /// </summary>
        const float PartyTableWidth = 960f;
        const float PanelRightTableX = 968f;
        const float PanelRightTop = 218f;
        const float PanelRightGap = 12f;
        const float PanelPlateInset = 12f;
        const float PanelRightTableHeight = BoardPanelHeight - PanelRightTop;
        const float PanelPlateHeight = PanelRightTableHeight - 2f * PanelPlateInset;

        /// <summary>The heap of skulls in the boards' table's bottom-right corner.</summary>
        const float SkullsWidth = 104f;

        /// <summary>
        /// The right half's plate, measured from the board panel's top-left corner: the heading (a title and, beside it on its
        /// baseline, a hint; the heading's middle is the baseline), two lines of the selected item's facts or of how to use the
        /// boards, then the buttons.
        /// </summary>
        const float PanelRightX = 1000f;
        const float PanelRightWidth = 880f;
        const float PanelTitleTop = PanelRightTop + 34f;
        const float PanelTitleHeight = 48f;
        const float PanelHeadingGap = 24f;
        const float PanelDetailTop = PanelRightTop + 76f;
        const float PanelDetailHeight = 62f;
        const float PanelDetailMinSize = 14f;
        const float PanelButtonsTop = PanelRightTop + 150f;
        const float PanelButtonHeight = 64f;

        /// <param name="toInventoryX">Where the "to inventory" button stands in the panel's button line; the screen puts its own buttons next to it.</param>
        /// <param name="panel">The board panel, for the screen's own words and buttons on its right half's plate.</param>
        static PartySideView BuildPartySide(RectTransform frame, float toInventoryX, float toInventoryWidth, out RectTransform panel)
        {
            // The battle screen's potion strip, above the right half.
            PotionSlotView potionTemplate = BuildPotionStrip(frame, PartyPotionsX, out RectTransform potions);

            // The field is the battle field's box, so that the view can place the columns with the
            // battle's formula. As in battle, the rearmost row is built first so that the figure in
            // front is drawn over the one behind.
            RectTransform field = UiBuild.Box(UiBuild.Rect("PartyField", frame), BattleFieldLeft, PartyFieldTop, BattleFieldWidth, PartyColumnHeight);
            var columns = new PartyColumnView[BattleRows.Count];
            for (int row = BattleRows.Count; row >= BattleRows.Front; row--)
            {
                columns[row - 1] = BuildPartyColumn(field, row);
            }

            // The board panel, in the battle's place: the party's boards in its columns, under the stage's columns, on a table of
            // their own; the right half's words and buttons on a dark plate laid on a table of its own under the map (round 46, A).
            // A chain hangs at the boards' left end and a heap of skulls lies in their table's corner.
            panel = UiBuild.Box(UiBuild.Rect("BoardPanel", frame), 0f, BoardPanelTop, 1920f, BoardPanelHeight);
            UiBuild.Box(KitFrame("BoardTable", panel, UiArt.Table), 0f, 0f, PartyTableWidth, BoardPanelHeight);
            UiBuild.Box(KitFrame("PanelRightTable", panel, UiArt.Table), PanelRightTableX, PanelRightTop, 1920f - PanelRightTableX, PanelRightTableHeight);
            Image rightPlate = KitFrame("PanelRightPlate", panel, UiArt.PlateLabel);
            UiBuild.Box(rightPlate, PanelRightX - 20f, PanelRightTop + PanelPlateInset, PanelRightWidth + 40f, PanelPlateHeight);
            UiBuild.Box(KitIcon("ChainLeft", panel, UiArt.Chain), 48f, 20f, 24f, 140f);
            UiBuild.Box(KitIcon("Skulls", panel, UiArt.Skulls), PartyTableWidth - SkullsWidth - 24f, 386f, SkullsWidth, 60f);
            float w = BattleFieldWidth / (BattleRows.Count * 2);
            for (int row = BattleRows.Front; row <= BattleRows.Count; row++)
            {
                RectTransform board = BuildBoardColumn(panel, "PartyBoard" + row, BattleFieldLeft + w * (BattleRows.Count - row), w);
                PartyBoardView boardView = BuildPartyBoard(board, "Party" + row, row);
                UiBuild.SetReference(columns[row - 1], "_board", board.gameObject);
                UiBuild.SetReference(columns[row - 1], "_boardView", boardView);
            }

            // The right half: the selected item's facts (or how to use the boards), and the button that takes it off its board.
            TextMeshProUGUI detail = UiBuild.ShrinkToFit(UiBuild.Label("PartyDetail", panel, 19f, UiPalette.Text, TextAlignmentOptions.TopLeft), PanelDetailMinSize);
            UiBuild.Box(detail, PanelRightX, PanelDetailTop, PanelRightWidth, PanelDetailHeight);
            ButtonParts toInventory = KitLocalizedButton("ToInventory", panel, UiKeys.Board.ToInventory, UiPalette.ButtonQuiet, 24f);
            UiBuild.Silence(toInventory.Button);
            UiBuild.Box(toInventory.Rect, toInventoryX, PanelButtonsTop, toInventoryWidth, PanelButtonHeight);

            // The gloom over the whole screen, under the popup.
            BuildScreenVignette(frame);

            GameObject popup = BuildInventoryPopup(frame, out TMP_Text inventoryTitle, out GameObject inventoryEmpty, out InventoryEntryView entryTemplate, out RectTransform entryParent);
            ItemTooltipView tooltip = BuildItemTooltip(frame, "ItemTooltip");

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
            UiBuild.SetReference(view, "_tooltip", tooltip);
            UiBuild.SetReference(view, "_boardPanel", panel);
            return view;
        }

        /// <summary>
        /// One row's column on the stage: the figure, then what is known of whoever stands there:
        /// the marks (HP, the fatigue pips, and the job with the member's state on the state line) and
        /// the two buttons that move the member a row. The state line takes a click (round 36, the state's
        /// words then read on the detail line). The row and the name are on the head of the row's board.
        /// The view sets the column's place and width when the screen opens, so every part is
        /// a line that stretches across the column. Every object is named after the row, because a
        /// prefab must not repeat a name. The row's board is in its column of the panel
        /// (<see cref="BuildPartyBoard"/>).
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
            UiBuild.Stretch(info, 0f, PartyMarksTop, 0f, 0f);

            // The marks of the battle screen; the state line names the job and, after it, the member's state. A clear button over
            // the line takes the click on the state's name (live only while there is one; the view decides).
            MarksParts marks = BuildMarks(info, p, raycastTarget: false);
            UiBuild.Line(marks.Marks, 0f, MarksHeight);
            TextMeshProUGUI job = MarksStateLine(UiBuild.Outlined(UiBuild.SingleLine(UiBuild.Label(p + "Job", marks.Marks, 15f, UiPalette.TextDim))));
            Image stateHit = MarksStateLine(UiBuild.Image(p + "State", marks.Marks, Color.clear, raycastTarget: true));
            Button state = UiBuild.MakeButton(stateHit);

            // Forward goes towards row 1 (to the right), back the other way: the left and the right half of one line.
            // Half a column is narrow, so the labels may shrink.
            float moveTop = PartyMoveTop - PartyMarksTop;
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
            UiBuild.SetReference(view, "_info", info.gameObject);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_state", state);
            UiBuild.SetReference(view, "_hp", marks.Hp);
            UiBuild.SetReference(view, "_hpBar", marks.HpBar);
            UiBuild.SetReferences(view, "_fatiguePips", marks.Pips);
            UiBuild.SetReference(view, "_forward", forward.Button);
            UiBuild.SetReference(view, "_back", back.Button);
            return view;
        }

        /// <summary>
        /// A member's board in a column of the board panel, as in battle (PartyBoardView; round 47): the head with the row and
        /// the name, and under it the cells stacked on their bag in the middle of the column, on a rect of their own over the
        /// column so that the battle screen can show and hide the board apart from its own. The view sizes the board to the
        /// member's cells at runtime (the bag follows) and makes the cells. Every object is named after the prefix.
        /// </summary>
        static PartyBoardView BuildPartyBoard(RectTransform column, string p, int row)
        {
            RectTransform board = UiBuild.Rect(p + "Board", column);
            UiBuild.Stretch(board);
            RectTransform cells = UiBuild.Rect(p + "Cells", board);
            UiBuild.Place(cells, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -BoardCellsTop), new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(JobData.MaxItemSlots)));
            UiBuild.Vertical(cells, BattleItemView.CellGapY, 0, TextAnchor.UpperCenter);
            BuildBoardBag(cells, p + "Bag");
            ItemSlotView slotTemplate = BuildItemSlot(cells, p + "CellTemplate");

            // The head: the column is its row, so the stud never changes; the view writes whoever stands in it.
            Image head = BuildBoardHead(board, p + "Board", out TextMeshProUGUI rowText, out TextMeshProUGUI name);
            rowText.text = row.ToString(CultureInfo.InvariantCulture);

            // At the head's right end, the fatigue the board's equipment costs when a battle starts (round 32, B1).
            TextMeshProUGUI fatigueText = BuildFatigueTag(head.transform, p + "BoardFatigue", new Vector2(1f, 0.5f), new Vector2(-FatigueTagInset, -FatigueHeadDrop), out GameObject fatigue);

            var view = board.gameObject.AddComponent<PartyBoardView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_fatigueTotal", fatigue);
            UiBuild.SetReference(view, "_fatigueTotalText", fatigueText);
            UiBuild.SetReference(view, "_slotTemplate", slotTemplate);
            UiBuild.SetReference(view, "_slotParent", cells);
            return view;
        }

        /// <summary>The label of a move button: closer to the button's edges than usual, and smaller when the word is long.</summary>
        static void FitMoveLabel(TextMeshProUGUI label)
        {
            UiBuild.Stretch(label.rectTransform, 6f, 4f, 6f, 6f);
            UiBuild.ShrinkToFit(UiBuild.SingleLine(label), 14f);
        }

        /// <summary>
        /// One cell of a board, in a slot of the kit: the icon over the silhouette of its tier's outline, the name with the grade
        /// under it in one text for an item without an icon (the view writes the two lines; they never wrap, a long name shrinks),
        /// the fatigue tag, the merge mark and the tier tag. It is as wide as a cell; its height is set at runtime, the cells its item takes, stacked.
        /// </summary>
        static ItemSlotView BuildItemSlot(Transform parent, string name)
        {
            Image frame = KitFrame(name, parent, UiArt.Slot);
            UiBuild.Size(frame, BattleItemView.CellWidth, BattleItemView.CellHeight);
            Button button = UiBuild.MakeButton(frame);
            var rightClick = frame.gameObject.AddComponent<RightClick>();

            // Silent: the side sounds a click as what it did, an item put in or a click (PartySideView).
            UiBuild.Silence(button);
            ColorBlock colors = button.colors;
            colors.disabledColor = new Color(QuietCellTint, QuietCellTint, QuietCellTint, 1f);
            button.colors = colors;

            // Words: the empty cell, or the name and grade of an item without an icon.
            TextMeshProUGUI text = UiBuild.Label(name + "Text", frame.transform, 19f, UiPalette.InkText);
            UiBuild.Stretch(text.rectTransform, 14f, 4f, 12f, 4f);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiBuild.ShrinkToFit(text, 13f);

            // Behind the icon, the silhouette that becomes the tier's outline (round 41); then the icon in the place the battle's cell gives it.
            Image outline = BuildOutline(frame.transform, name + "Outline", out SilhouetteOutline outlineEffect);
            Image icon = UiBuild.Image(name + "Icon", frame.transform, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform, ItemIconMarginX, ItemIconMarginY, ItemIconMarginX, ItemIconMarginY);

            // The fatigue tag at the top-right corner: "+1" on equipment that costs fatigue when a battle starts (round 32, B1).
            TextMeshProUGUI fatigueText = BuildFatigueTag(frame.transform, name + "Fatigue", Vector2.one, new Vector2(-FatigueTagInset, -FatigueTagInset), out GameObject fatigue);

            // The mark of a cell the chosen item would merge into (round 35): a veil over the cell with the words of the tier the
            // merge makes, lifted off the tier tag; the view shows that tier's outline and stars with it.
            RectTransform merge = UiBuild.Rect(name + "Merge", frame.transform);
            UiBuild.Stretch(merge);
            Image veil = UiBuild.Image(name + "MergeVeil", merge, new Color(0.08f, 0.09f, 0.12f, MergeVeilAlpha));
            UiBuild.Stretch(veil.rectTransform, MergeVeilInset, MergeVeilInset, MergeVeilInset, MergeVeilInset);
            TextMeshProUGUI mergeText = UiBuild.SingleLine(UiBuild.Label(name + "MergeText", merge, MergeMarkFontSize, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(mergeText.rectTransform, 0f, 0f, 0f, MergeMarkLift);
            merge.gameObject.SetActive(false);

            // Over everything, the tier tag with the stars (round 41): the view shows it for a tier above Common or a merge.
            Image tierTag = BuildTierTag(frame.transform, name + "Tier", out Image[] stars);

            var view = frame.gameObject.AddComponent<ItemSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_rightClick", rightClick);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_plain", UiArt.Load(UiArt.Slot));
            UiBuild.SetReference(view, "_selected", UiArt.Load(UiArt.SlotSelected));
            UiBuild.SetReference(view, "_outline", outline);
            UiBuild.SetReference(view, "_outlineEffect", outlineEffect);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_text", text);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_fatigueText", fatigueText);
            UiBuild.SetReference(view, "_tierTag", tierTag);
            UiBuild.SetReferences(view, "_stars", stars);
            UiBuild.SetReference(view, "_merge", merge.gameObject);
            UiBuild.SetReference(view, "_mergeText", mergeText);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// A fatigue tag (round 32, B1): the violet pill with its words, anchored by its corner or end to the same point of its
        /// parent and moved in by <paramref name="position"/>. Hidden until a view shows a cost; a head's view also sets its width.
        /// </summary>
        static TextMeshProUGUI BuildFatigueTag(Transform parent, string name, Vector2 anchor, Vector2 position, out GameObject tag)
        {
            Image pill = KitFrame(name, parent, UiArt.FatigueTag);
            UiBuild.Place(pill.rectTransform, anchor, anchor, position, new Vector2(FatigueTagCellWidth, FatigueTagHeight));
            TextMeshProUGUI words = UiBuild.SingleLine(UiBuild.Label(name + "Text", pill.transform, FatigueTagFontSize, UiPalette.Fatigue, TextAlignmentOptions.Center));
            words.overflowMode = TextOverflowModes.Overflow;
            UiBuild.Stretch(words.rectTransform);
            tag = pill.gameObject;
            tag.SetActive(false);
            return words;
        }

        /// <summary>
        /// The inventory popup: a dimmed cover over the right half of the screen under the potions
        /// and above the board panel, with a box that lists the items top to bottom. The screen's toggle button opens
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
            UiBuild.SetReference(view, "_rightClick", frame.gameObject.AddComponent<RightClick>());
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_facts", facts);

            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
