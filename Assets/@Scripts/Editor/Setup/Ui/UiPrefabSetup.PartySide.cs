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

        /// <summary>
        /// The inventory window over the mercenaries' stage (round 52, "B안"): the stage from under the header to the board panel, its left
        /// half, a little darkened; the window at its top, as high as its grid of board-sized squares needs, so the marks under the figures show.
        /// </summary>
        const float InventoryCoverTop = PartyRightTop;
        const float InventoryCoverWidth = 960f;
        const float InventoryCoverHeight = BoardPanelTop - PartyRightTop;
        const float InventoryWindowX = 8f;
        const float InventoryWindowWidth = 944f;
        const float InventoryWindowHeight = 350f;

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

            InventoryWindowView inventory = BuildInventoryPopup(frame);
            ItemTooltipView tooltip = BuildItemTooltip(frame, "ItemTooltip");

            var view = frame.gameObject.AddComponent<PartySideView>();
            UiBuild.SetReference(view, "_field", field);
            UiBuild.SetReferences(view, "_columns", columns);
            UiBuild.SetReference(view, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(view, "_potionParent", potions);
            UiBuild.SetReference(view, "_detail", detail);
            UiBuild.SetReference(view, "_toInventory", toInventory.Button);
            UiBuild.SetReference(view, "_inventory", inventory);
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
        /// A member's board in a column of the board panel, as in battle (PartyBoardView; round 47): the head with the row and the name,
        /// and under it the board as a grid (Slice B stage 19, BuildGridBoard), on a rect of their own over the column so that the battle
        /// screen can show and hide the board apart from its own. Every object is named after the prefix.
        /// </summary>
        static PartyBoardView BuildPartyBoard(RectTransform column, string p, int row)
        {
            RectTransform board = UiBuild.Rect(p + "Board", column);
            UiBuild.Stretch(board);
            GridBoardView grid = BuildGridBoard(board, p);

            // The head: the column is its row, so the stud never changes; the view writes whoever stands in it.
            Image head = BuildBoardHead(board, p + "Board", out TextMeshProUGUI rowText, out TextMeshProUGUI name);
            rowText.text = row.ToString(CultureInfo.InvariantCulture);

            // At the head's right end, the fatigue the board's equipment costs when a battle starts (round 32, B1).
            TextMeshProUGUI fatigueText = BuildFatigueTag(head.transform, p + "BoardFatigue", new Vector2(1f, 0.5f), new Vector2(-FatigueTagInset, -FatigueHeadDrop), out GameObject fatigue);

            var view = board.gameObject.AddComponent<PartyBoardView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_fatigueTotal", fatigue);
            UiBuild.SetReference(view, "_fatigueTotalText", fatigueText);
            UiBuild.SetReference(view, "_grid", grid);
            return view;
        }

        /// <summary>The label of a move button: closer to the button's edges than usual, and smaller when the word is long.</summary>
        static void FitMoveLabel(TextMeshProUGUI label)
        {
            UiBuild.Stretch(label.rectTransform, 6f, 4f, 6f, 6f);
            UiBuild.ShrinkToFit(UiBuild.SingleLine(label), 14f);
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

        // ---- The inventory window (round 49: Diablo II's inventory window; round 52: over the stage) ---------------------------------------------------------

        const float InventoryPlateWidth = 300f;
        const float InventoryPlateTop = 14f;
        const float InventoryPlateHeight = 40f;
        const float InventoryGridLeft = 24f;
        const float InventoryGridTop = 68f;
        const float InventoryRimWidth = InventoryGridView.Rim;
        const float InventoryCoinsGap = 14f;
        const float InventoryCoinsWidth = 150f;
        const float InventoryCoinsHeight = 32f;
        const float InventoryInfoGap = 18f;
        const float InventoryInfoTop = 62f;
        const float InventoryInfoFontSize = 15f;
        const float InventoryHintBottom = 14f;

        /// <summary>
        /// The inventory window (InventoryWindowView; round 49's Diablo II window, round 52's place): a light shade over the mercenaries'
        /// stage that takes its clicks, and at its top the stone box: the heading on a dark plate and the squares used at the right, the
        /// grid of board-sized squares sunk in the stone at the left with the coins on a plate below its corner, the held item's tooltip
        /// in a black box beside the grid, and how to use the grid at the bottom. The screen's toggle button and the I key open and close
        /// it; the view fills it. The node map's party side and the battle screen after a win each build one.
        /// </summary>
        static InventoryWindowView BuildInventoryPopup(Transform frame)
        {
            Image cover = UiBuild.Image("InventoryPanel", frame, UiPalette.InventoryShade, raycastTarget: true);
            UiBuild.Box(cover, 0f, InventoryCoverTop, InventoryCoverWidth, InventoryCoverHeight);
            Image box = KitFrame("InventoryBox", cover.transform, UiArt.Table);
            UiBuild.Box(box, InventoryWindowX, 0f, InventoryWindowWidth, InventoryWindowHeight);

            Image plate = UiBuild.Image("InventoryHeadingPlate", box.transform, UiPalette.StonePlate);
            UiBuild.Place(plate.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -InventoryPlateTop), new Vector2(InventoryPlateWidth, InventoryPlateHeight));
            AddSunkBevel(plate.rectTransform, "InventoryHeadingPlate");
            TextMeshProUGUI heading = UiBuild.SingleLine(UiBuild.LocalizedLabel("InventoryHeading", plate.transform, UiKeys.Board.InventoryHeading, 24f, UiPalette.DiabloGold, TextAlignmentOptions.Center));
            UiBuild.Stretch(heading.rectTransform);

            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Label("InventoryTitle", box.transform, 17f, UiPalette.Bone, TextAlignmentOptions.Right));
            UiBuild.Place(title.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -InventoryPlateTop), new Vector2(240f, InventoryPlateHeight));

            InventoryGridView grid = BuildInventoryGrid(box.transform, out RectTransform rim);

            Image coins = UiBuild.Image("InventoryCoins", rim, UiPalette.StonePlate);
            coins.rectTransform.anchorMin = Vector2.zero;
            coins.rectTransform.anchorMax = Vector2.zero;
            coins.rectTransform.pivot = new Vector2(0f, 1f);
            coins.rectTransform.anchoredPosition = new Vector2(0f, -InventoryCoinsGap);
            coins.rectTransform.sizeDelta = new Vector2(InventoryCoinsWidth, InventoryCoinsHeight);
            AddSunkBevel(coins.rectTransform, "InventoryCoins");
            UiBuild.Place(KitIcon("InventoryCoinIcon", coins.transform, UiArt.Coin).rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(22f, 22f));
            TextMeshProUGUI coinCount = UiBuild.SingleLine(UiBuild.Label("InventoryCoinCount", coins.transform, 20f, UiPalette.DiabloGold, TextAlignmentOptions.Right));
            UiBuild.Stretch(coinCount.rectTransform, 36f, 0f, 10f, 0f);

            // The held item's tooltip: Diablo's black box with a grey line, the lines centred, beside the grid; it grows down from its top.
            float infoLeft = InventoryGridLeft + rim.sizeDelta.x + InventoryInfoGap;
            Image info = UiBuild.Image("InventoryInfo", box.transform, UiPalette.TooltipFill);
            AddLine(info, UiPalette.TooltipLine, 1f);
            info.rectTransform.anchorMin = new Vector2(0f, 1f);
            info.rectTransform.anchorMax = new Vector2(0f, 1f);
            info.rectTransform.pivot = new Vector2(0f, 1f);
            info.rectTransform.anchoredPosition = new Vector2(infoLeft, -InventoryInfoTop);
            info.rectTransform.sizeDelta = new Vector2(InventoryWindowWidth - infoLeft - InventoryGridLeft, 100f);
            VerticalLayoutGroup infoLines = UiBuild.Vertical(info.rectTransform, 0f);
            infoLines.padding = new RectOffset(12, 12, 10, 10);
            infoLines.childControlWidth = true;
            infoLines.childControlHeight = true;
            infoLines.childForceExpandWidth = true;
            info.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI infoText = UiBuild.Label("InventoryInfoText", info.transform, InventoryInfoFontSize, UiPalette.Text, TextAlignmentOptions.Center);
            infoText.lineSpacing = 4f;
            info.gameObject.SetActive(false);

            TextMeshProUGUI hint = UiBuild.SingleLine(UiBuild.LocalizedLabel("InventoryHint", box.transform, UiKeys.Board.InventoryHint, 15f, UiPalette.TextDim, TextAlignmentOptions.Center));
            hint.rectTransform.anchorMin = new Vector2(0.5f, 0f);
            hint.rectTransform.anchorMax = new Vector2(0.5f, 0f);
            hint.rectTransform.pivot = new Vector2(0.5f, 0f);
            hint.rectTransform.anchoredPosition = new Vector2(0f, InventoryHintBottom);
            hint.rectTransform.sizeDelta = new Vector2(InventoryWindowWidth - 2f * InventoryGridLeft, 22f);
            UiBuild.ShrinkToFit(hint, 12f);

            var window = cover.gameObject.AddComponent<InventoryWindowView>();
            UiBuild.SetReference(window, "_title", title);
            UiBuild.SetReference(window, "_grid", grid);
            UiBuild.SetReference(window, "_coins", coinCount);
            UiBuild.SetReference(window, "_infoBox", info.gameObject);
            UiBuild.SetReference(window, "_info", infoText);
            cover.gameObject.SetActive(false);
            return window;
        }

        /// <summary>
        /// The inventory's grid (InventoryGridView, round 49): its rim of stone sunk in the box at the top centre (the view sizes it to the
        /// inventory), the grey that the gaps between the squares show, and over that, in the order they are drawn, the squares (which
        /// take the pointer), the pieces and the ghost. The view makes the squares and the pieces from the templates at runtime.
        /// </summary>
        static InventoryGridView BuildInventoryGrid(Transform box, out RectTransform rim)
        {
            Image stone = UiBuild.Image("InventoryGrid", box, UiPalette.InventoryRim);
            rim = stone.rectTransform;
            Vector2 size = InventoryGridView.Size(10, 3) + 2f * InventoryRimWidth * Vector2.one;
            UiBuild.Place(rim, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(InventoryGridLeft, -InventoryGridTop), size);
            AddSunkBevel(rim, "InventoryGrid");
            Image well = UiBuild.Image("InventoryWell", rim, UiPalette.GridSquareLine);
            UiBuild.Stretch(well.rectTransform, InventoryRimWidth, InventoryRimWidth, InventoryRimWidth, InventoryRimWidth);

            RectTransform squares = Layer(well.rectTransform, "InventorySquares");
            RectTransform pieces = Layer(well.rectTransform, "InventoryPieces");
            RectTransform ghosts = Layer(well.rectTransform, "InventoryGhost");
            GridSquareView squareTemplate = BuildGridSquare(squares, "InventorySquareTemplate");
            ItemSlotView pieceTemplate = BuildItemSlot(pieces, "InventoryPieceTemplate");
            Image ghostFill = UiBuild.Image("InventoryGhostFill", ghosts, Color.white);
            ghostFill.gameObject.SetActive(false);
            Image ghostFloor = UiBuild.Image("InventoryGhostFloor", squares, UiPalette.GridSquare);
            ghostFloor.gameObject.SetActive(false);
            RectTransform ghostArt = UiBuild.Rect("InventoryGhostArt", ghosts);
            Image ghostIcon = UiBuild.Image("InventoryGhostIcon", ghostArt, Color.white);
            ghostIcon.preserveAspect = true;
            UiBuild.Stretch(ghostIcon.rectTransform);
            ghostArt.gameObject.SetActive(false);

            var view = stone.gameObject.AddComponent<InventoryGridView>();
            UiBuild.SetReference(view, "_squareLayer", squares);
            UiBuild.SetReference(view, "_pieceLayer", pieces);
            UiBuild.SetReference(view, "_ghostLayer", ghosts);
            UiBuild.SetReference(view, "_squareTemplate", squareTemplate);
            UiBuild.SetReference(view, "_pieceTemplate", pieceTemplate);
            UiBuild.SetReference(view, "_ghostFill", ghostFill);
            UiBuild.SetReference(view, "_ghostFloor", ghostFloor);
            UiBuild.SetReference(view, "_ghostArt", ghostArt);
            UiBuild.SetReference(view, "_ghostIcon", ghostIcon);
            return view;
        }
    }
}
