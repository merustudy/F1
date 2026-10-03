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
        /// The battle screen, top to bottom: the header (retreat, the dungeon and the floor, the
        /// captions, pause and speed), the potions under it on the left, headroom, the stage, then
        /// the board panel. Everything above the panel is in front of the dungeon's background.
        /// Each column of the stage holds one unit: a full-body figure standing on the background's
        /// floor with its plate under its feet. The stage's box leaves room at both ends of the
        /// frame; the screen sets the columns' places and widths when it opens (FieldLayout). The
        /// board panel has one column per row on each side, right under the stage's column of that
        /// row: the unit's item cells stacked on a bag (2026-10-03 mockup V, after the lines of
        /// mockups A, E and G). The storm clock stands between the two sides.
        /// </summary>
        const float BattleFieldLeft = 120f;
        /// <summary>The stage stands high enough for the panel of eight stacked cells under it (mockup V: 56 higher than before).</summary>
        const float BattleFieldTop = 206f;
        const float BattleFieldWidth = 1680f;

        /// <summary>A figure's place is as tall as this and the art in it as wide as its 3:4 canvas, whatever the column's width is.</summary>
        const float BattleFigureHeight = 300f;
        const float BattleFigureWidth = BattleFigureHeight * 3f / 4f;

        /// <summary>Where the figures' feet stand, from the field's top: the bottom of the figures' places.</summary>
        const float BattleFloorY = BattleFigureHeight;

        /// <summary>
        /// A dungeon's background. Every background is drawn on one canvas (3:2) with the units'
        /// floor at one height of it (ArtPipeline/tools/gen_image.py: SCENE_CANVAS, SCENE_FLOOR),
        /// so its place is fixed: as wide as the frame, its floor on the floor of the field.
        /// </summary>
        const float BattleBackgroundWidth = 1920f;
        const float BattleBackgroundHeight = BattleBackgroundWidth * 1536f / 2304f;
        const float BattleBackgroundFloor = 0.57f;

        const float BattlePlateTop = BattleFigureHeight + PlateGap;

        /// <summary>A unit on the stage: the figure and the plate under its feet.</summary>
        const float BattleUnitHeight = BattlePlateTop + PlateHeight;

        /// <summary>
        /// The board panel under the stage: its box, and the room above and below its columns. A
        /// column holds the most cells a board can have, stacked (JobData.MaxItemSlots), so the
        /// panel is as high as they are plus the room.
        /// </summary>
        const float BoardPanelTop = 620f;
        const float BoardPanelHeight = 460f;
        const float BoardColumnsTop = 16f;
        const float BoardColumnsHeight = BoardPanelHeight - 2f * BoardColumnsTop;

        /// <summary>
        /// The middle of the board panel, between the two sides' row 1 columns (FieldLayout.SideGap
        /// wide): the storm clock (a dial with the ring that fills as the storm comes and the
        /// battle time on its face) and the storm's words under it. The clock is as wide as the
        /// gap allows.
        /// </summary>
        const float ClockSize = 160f;
        const float ClockTop = 16f;
        const float ClockX = 960f - ClockSize / 2f;

        /// <summary>The dial's sprite holds the disc at 84% of its canvas (ui_piece fit), so its box is larger than the disc.</summary>
        const float DialBox = ClockSize / 0.84f;
        const float StormLineTop = ClockTop + ClockSize + 10f;
        const float StormLineWidth = FieldLayout.SideGap;

        /// <summary>The last few events as captions in the middle of the header, the newest at the bottom.</summary>
        const float CaptionTop = 12f;
        const float CaptionHeight = 20f;
        const int CaptionLines = 3;
        const float CaptionWidth = 560f;

        /// <summary>The stage's box the storm's dusk, the red of death's door and the lightning cover: from under the header to the panel.</summary>
        const float StageFxTop = 84f;
        const float StageFxHeight = BoardPanelTop - StageFxTop;

        /// <summary>The shadow under a figure's feet: a dark ellipse.</summary>
        const float ShadowWidth = 150f;
        const float ShadowHeight = 26f;
        const float ShadowAlpha = 0.35f;

        static UIScreen BuildBattle(Transform holder)
        {
            BattleScreen screen = Screen<BattleScreen>("BattleScreen", holder, out RectTransform frame);

            // The dungeon's background, behind everything else. The screen shows it when the dungeon has one.
            Image background = UiBuild.Image("Background", frame, Color.white);
            UiBuild.Box(background, 0f, BattleFieldTop + BattleFloorY - BattleBackgroundFloor * BattleBackgroundHeight, BattleBackgroundWidth, BattleBackgroundHeight);
            background.enabled = false;

            // Header: retreat and the dungeon with its floor on the left, the captions in the middle, pause and
            // speed on the right. The battle time and the storm are on the clock in the middle of the board panel.
            Image header = KitFrame("Header", frame, UiArt.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 84f);
            ButtonParts retreat = KitButton("Retreat", header.transform, UiPalette.Danger, 28f);
            UiBuild.Box(retreat.Rect, 40f, 16f, 300f, 52f);
            TextMeshProUGUI title = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label("Title", header.transform, 26f, UiPalette.Text, TextAlignmentOptions.Left)), 18f);
            UiBuild.Box(title, 372f, 16f, 300f, 52f);

            // The captions: the last few events, the newest at the bottom and brightest (BattleScreen.RenderCaptions).
            var captions = new TextMeshProUGUI[CaptionLines];
            for (int i = 0; i < CaptionLines; i++)
            {
                captions[i] = UiBuild.SingleLine(UiBuild.Label("Caption" + i, header.transform, 16f, UiPalette.TextDim, TextAlignmentOptions.Center));
                UiBuild.Box(captions[i], 960f - CaptionWidth / 2f, CaptionTop + i * CaptionHeight, CaptionWidth, CaptionHeight);
            }

            ButtonParts pause = KitLocalizedButton("Pause", header.transform, UiKeys.Battle.Pause, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(pause.Rect, 1530f, 16f, 110f, 52f);
            var speedButtons = new Button[3];
            var speedFrames = new Image[3];
            var speedLabels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                ButtonParts speed = KitButton("Speed" + i, header.transform, UiPalette.ButtonQuiet, 24f);
                UiBuild.Box(speed.Rect, 1650f + 90f * i, 16f, 80f, 52f);
                speedButtons[i] = speed.Button;
                speedFrames[i] = speed.Frame;
                speedLabels[i] = speed.Label;
            }

            // Potions under the retreat button. Their hint is written on a small plate so that it
            // reads over the background; the plate is as wide as the text. The rest of the space
            // above the field stays empty: the background shows there.
            PotionSlotView potionTemplate = BuildPotionStrip(frame, 30f, out RectTransform potions);
            Image hintPlate = KitFrame("PotionHintPlate", frame, UiArt.PlateLabel, 0.6f);
            UiBuild.Box(hintPlate, 656f, 110f, 300f, 48f);
            HorizontalLayoutGroup hintLayout = UiBuild.Horizontal(hintPlate.rectTransform, 0f, 0, TextAnchor.MiddleLeft);
            hintLayout.padding = new RectOffset(22, 22, 0, 0);
            hintLayout.childControlWidth = true;
            hintLayout.childControlHeight = true;
            var hintFit = hintPlate.gameObject.AddComponent<ContentSizeFitter>();
            hintFit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            TextMeshProUGUI potionHint = UiBuild.Label("PotionHint", hintPlate.transform, 20f, UiPalette.TextDim, TextAlignmentOptions.Left);
            potionHint.textWrappingMode = TextWrappingModes.NoWrap;

            // Field: one column per row on each side. The screen puts them in place when it opens,
            // because how many the party uses depends on the party size. The rearmost row is built
            // first and row 1 last: a figure is wider than its column, and where two figures of a
            // side touch, the one in front is drawn over the one behind.
            RectTransform field = UiBuild.Box(UiBuild.Rect("Field", frame), BattleFieldLeft, BattleFieldTop, BattleFieldWidth, BattleUnitHeight);
            float columnWidth = BattleFieldWidth / (BattleRows.Count * 2);
            var partyRows = new RectTransform[BattleRows.Count];
            var enemyRows = new RectTransform[BattleRows.Count];
            for (int i = BattleRows.Count - 1; i >= 0; i--)
            {
                int row = i + 1;
                partyRows[i] = BuildBattleColumn(field, "PartyRow" + row, columnWidth * (BattleRows.Count - row), columnWidth);
                enemyRows[i] = BuildBattleColumn(field, "EnemyRow" + row, columnWidth * (BattleRows.Count + i), columnWidth);
            }

            BattleUnitView unitTemplate = BuildBattleUnit(field);

            // The board panel: one column per row on each side, under the stage's column of that row.
            // A unit's board (its cells stacked) is put in the column of the row it stands in, as its
            // figure is put in that row's stage column; the screen places both when it opens. The
            // storm clock stands in the middle, where the two sides stand apart as on the stage.
            Image panel = KitFrame("BoardPanel", frame, UiArt.Panel);
            UiBuild.Box(panel, 0f, BoardPanelTop, 1920f, BoardPanelHeight);
            var partyBoards = new RectTransform[BattleRows.Count];
            var enemyBoards = new RectTransform[BattleRows.Count];
            for (int i = 0; i < BattleRows.Count; i++)
            {
                int row = i + 1;
                partyBoards[i] = BuildBoardColumn(panel.transform, "PartyBoard" + row, BattleFieldLeft + columnWidth * (BattleRows.Count - row), columnWidth);
                enemyBoards[i] = BuildBoardColumn(panel.transform, "EnemyBoard" + row, BattleFieldLeft + columnWidth * (BattleRows.Count + i), columnWidth);
            }

            BattleBoardView boardTemplate = BuildBattleBoard(panel.transform);

            // The storm clock in the middle of the panel: the dial, the ring that fills on it, the time on its face,
            // and the storm's words under it with the storm icon.
            Image dial = KitIcon("Dial", panel.transform, UiArt.Dial);
            UiBuild.Box(dial, 960f - DialBox / 2f, ClockTop - (DialBox - ClockSize) / 2f, DialBox, DialBox);
            Image ring = KitIcon("StormRing", panel.transform, UiArt.Ring);
            UiBuild.Box(ring, ClockX, ClockTop, ClockSize, ClockSize);
            ring.type = Image.Type.Filled;
            ring.fillMethod = Image.FillMethod.Radial360;
            ring.fillOrigin = (int)Image.Origin360.Top;
            ring.fillClockwise = true;
            ring.fillAmount = 0f;
            ring.color = UiPalette.Text;
            TextMeshProUGUI clockTime = UiBuild.SingleLine(UiBuild.Label("ClockTime", panel.transform, 32f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Box(clockTime, ClockX, ClockTop + ClockSize / 2f - 30f, ClockSize, 60f);

            RectTransform stormLine = UiBuild.Box(UiBuild.Rect("StormLine", panel.transform), 960f - StormLineWidth / 2f, StormLineTop, StormLineWidth, 30f);
            HorizontalLayoutGroup stormLayout = UiBuild.Horizontal(stormLine, 8f, 0, TextAnchor.MiddleCenter);
            stormLayout.childControlWidth = true;
            stormLayout.childControlHeight = true;
            Image stormIcon = KitIcon("StormIcon", stormLine, UiArt.Storm);
            var stormIconSize = stormIcon.gameObject.AddComponent<LayoutElement>();
            stormIconSize.preferredWidth = 26f;
            stormIconSize.preferredHeight = 26f;
            TextMeshProUGUI clockLabel = UiBuild.Label("ClockLabel", stormLine, 18f, UiPalette.TextDim);
            clockLabel.textWrappingMode = TextWrappingModes.NoWrap;

            // The fx layer over the stage and the panel: the storm's dusk, the red of death's door and the
            // lightning over the stage's box, and the numbers and ghosts anywhere. Built before the result so
            // that the result is drawn over it. Nothing in it takes a click.
            RectTransform fx = UiBuild.Rect("Fx", frame);
            UiBuild.Stretch(fx);
            Image stormVignette = UiBuild.Image("StormVignette", fx, new Color(0f, 0f, 0f, 0f));
            stormVignette.sprite = UiArt.Load(UiArt.Vignette);
            UiBuild.Box(stormVignette, 0f, StageFxTop, 1920f, StageFxHeight);
            stormVignette.enabled = false;
            Image dangerVignette = UiBuild.Image("DangerVignette", fx, new Color(UiPalette.Danger.r, UiPalette.Danger.g, UiPalette.Danger.b, 0f));
            dangerVignette.sprite = UiArt.Load(UiArt.Vignette);
            UiBuild.Box(dangerVignette, 0f, StageFxTop, 1920f, StageFxHeight);
            dangerVignette.enabled = false;
            Image flash = UiBuild.Image("Flash", fx, new Color(1f, 1f, 1f, 0f));
            UiBuild.Box(flash, 0f, StageFxTop, 1920f, StageFxHeight);
            flash.enabled = false;
            Image ghostTemplate = UiBuild.Image("GhostTemplate", fx, Color.white);
            ghostTemplate.preserveAspect = true;
            ghostTemplate.gameObject.SetActive(false);
            FloatingTextView floatingTemplate = BuildFloatingText(fx);
            var fxLayer = fx.gameObject.AddComponent<BattleFxLayer>();
            UiBuild.SetReference(fxLayer, "_floatingTemplate", floatingTemplate);
            UiBuild.SetReference(fxLayer, "_ghostTemplate", ghostTemplate);
            UiBuild.SetReference(fxLayer, "_flash", flash);
            UiBuild.SetReference(fxLayer, "_stormVignette", stormVignette);
            UiBuild.SetReference(fxLayer, "_dangerVignette", dangerVignette);
            UiBuild.SetReference(fxLayer, "_shaken", field);

            // Result: covers the field when the battle has ended.
            Image overlay = UiBuild.Image("ResultPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(overlay.rectTransform);
            Image result = KitFrame("ResultBox", overlay.transform, UiArt.Panel);
            UiBuild.Box(result, 310f, 170f, 1300f, 540f);
            TextMeshProUGUI resultTitle = UiBuild.Label("ResultTitle", result.transform, 72f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(resultTitle, 0f, 30f, 1300f, 100f);
            TextMeshProUGUI resultDetail = UiBuild.Label("ResultDetail", result.transform, 24f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(resultDetail, 50f, 150f, 1200f, 250f);
            ButtonParts showLog = KitLocalizedButton("ShowLog", result.transform, UiKeys.Battle.ShowLog, UiPalette.ButtonQuiet, 32f);
            UiBuild.Box(showLog.Rect, 250f, 420f, 380f, 84f);
            ButtonParts resume = KitLocalizedButton("Continue", result.transform, UiKeys.Common.Continue, UiPalette.Button, 36f);
            UiBuild.Box(resume.Rect, 670f, 420f, 380f, 84f);

            // The whole log of the finished battle, over the result. Shown by "show log"; the lines
            // are created at runtime. Built after the result so that it is drawn over it.
            Image logOverlay = UiBuild.Image("LogPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(logOverlay.rectTransform);
            Image logBox = KitFrame("LogBox", logOverlay.transform, UiArt.Panel);
            UiBuild.Box(logBox, 200f, 80f, 1520f, 920f);
            UiBuild.Box(UiBuild.LocalizedLabel("LogTitle", logBox.transform, UiKeys.Battle.LogTitle, 40f, UiPalette.Text), 40f, 24f, 800f, 52f);
            TextMeshProUGUI logPartyFallen = UiBuild.SingleLine(UiBuild.Label("LogPartyFallen", logBox.transform, 22f, UiPalette.Danger));
            UiBuild.Box(logPartyFallen, 40f, 84f, 700f, 30f);
            TextMeshProUGUI logEnemyFallen = UiBuild.SingleLine(UiBuild.Label("LogEnemyFallen", logBox.transform, 22f, UiPalette.TextDim));
            UiBuild.Box(logEnemyFallen, 760f, 84f, 720f, 30f);
            ScrollRect logScroll = UiBuild.VerticalScroll("LogScroll", logBox.transform, 16f, out RectTransform logContent);
            UiBuild.Box((RectTransform)logScroll.transform, 40f, 124f, 1440f, 700f);
            TextMeshProUGUI logChunk = UiBuild.Label("LogChunkTemplate", logContent, 20f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            logChunk.gameObject.SetActive(false);
            ButtonParts logClose = KitLocalizedButton("LogClose", logBox.transform, UiKeys.Common.Close, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(logClose.Rect, 560f, 840f, 400f, 64f);
            logOverlay.gameObject.SetActive(false);

            UiBuild.SetReference(screen, "_background", background);
            UiBuild.SetReference(screen, "_title", title);
            UiBuild.SetReference(screen, "_clockTime", clockTime);
            UiBuild.SetReference(screen, "_clockLabel", clockLabel);
            UiBuild.SetReference(screen, "_clockRing", ring);
            UiBuild.SetReferences(screen, "_captions", captions);
            UiBuild.SetReference(screen, "_fx", fxLayer);
            UiBuild.SetReference(screen, "_pause", pause.Button);
            UiBuild.SetReference(screen, "_pauseFrame", pause.Frame);
            UiBuild.SetReferences(screen, "_speedButtons", speedButtons);
            UiBuild.SetReferences(screen, "_speedFrames", speedFrames);
            UiBuild.SetReferences(screen, "_speedLabels", speedLabels);
            UiBuild.SetReference(screen, "_unitTemplate", unitTemplate);
            UiBuild.SetReference(screen, "_field", field);
            UiBuild.SetReferences(screen, "_partyRows", partyRows);
            UiBuild.SetReferences(screen, "_enemyRows", enemyRows);
            UiBuild.SetReference(screen, "_boardTemplate", boardTemplate);
            UiBuild.SetReferences(screen, "_partyBoardColumns", partyBoards);
            UiBuild.SetReferences(screen, "_enemyBoardColumns", enemyBoards);
            UiBuild.SetReference(screen, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(screen, "_potionParent", potions);
            UiBuild.SetReference(screen, "_potionHint", potionHint);
            UiBuild.SetReference(screen, "_retreat", retreat.Button);
            UiBuild.SetReference(screen, "_retreatLabel", retreat.Label);
            UiBuild.SetReference(screen, "_resultPanel", overlay.gameObject);
            UiBuild.SetReference(screen, "_resultTitle", resultTitle);
            UiBuild.SetReference(screen, "_resultDetail", resultDetail);
            UiBuild.SetReference(screen, "_showLog", showLog.Button);
            UiBuild.SetReference(screen, "_continue", resume.Button);
            UiBuild.SetReference(screen, "_logPanel", logOverlay.gameObject);
            UiBuild.SetReference(screen, "_logScroll", logScroll);
            UiBuild.SetReference(screen, "_logContent", logContent);
            UiBuild.SetReference(screen, "_logChunkTemplate", logChunk);
            UiBuild.SetReference(screen, "_logPartyFallen", logPartyFallen);
            UiBuild.SetReference(screen, "_logEnemyFallen", logEnemyFallen);
            UiBuild.SetReference(screen, "_logClose", logClose.Button);
            return screen;
        }

        /// <summary>One row of a side: a column that holds the row's unit. The unit takes the width of the column.</summary>
        static RectTransform BuildBattleColumn(Transform field, string name, float x, float width)
        {
            RectTransform column = UiBuild.Box(UiBuild.Rect(name, field), x, 0f, width, BattleUnitHeight);
            VerticalLayoutGroup units = UiBuild.Vertical(column, 10f);
            units.childControlWidth = true;
            units.childForceExpandWidth = true;
            return column;
        }

        /// <summary>
        /// One row's column of the board panel, under the stage's column of that row: it holds the
        /// board of the unit standing in the row. The party side of the node map and the reward
        /// screen builds the same columns.
        /// </summary>
        static RectTransform BuildBoardColumn(Transform panel, string name, float x, float width)
        {
            return UiBuild.Box(UiBuild.Rect(name, panel), x, BoardColumnsTop, width, BoardColumnsHeight);
        }

        /// <summary>
        /// One unit on the stage: the full-body figure (its art, or a placeholder) and its plate
        /// under its feet. The whole unit is the button a potion is aimed at. Its width follows the
        /// column it is put in, so everything inside is laid out as lines that stretch across it.
        /// </summary>
        static BattleUnitView BuildBattleUnit(Transform parent)
        {
            RectTransform root = UiBuild.Rect("UnitTemplate", parent);
            UiBuild.Size(root, BattleFieldWidth / (BattleRows.Count * 2), BattleUnitHeight);

            // The figure's place, as wide as the column. A click on it counts for the unit.
            RectTransform figure = BuildFigure(root, "UnitFigure", takesClicks: true, out FigureView figureView);
            UiBuild.Line(figure, 0f, BattleFigureHeight);

            // The plate: the row, the name, HP, then the line of states.
            PlateParts plate = BuildPlate(root, "Unit", UiArt.PlateParty, raycastTarget: true);
            UiBuild.Line(plate.Plate, BattlePlateTop, PlateHeight);

            // The button lives on the root so that a click on the figure or the plate counts.
            Button button = PotionTargetButton(root, plate.Plate);

            // The line of states: the death's door state, or the shield and the burn the unit
            // carries, each an icon with its number. Only what applies is shown, from the left.
            RectTransform states = PlateStateLine(UiBuild.Rect("UnitStates", plate.Plate.transform));
            HorizontalLayoutGroup line = UiBuild.Horizontal(states, 10f, 0, TextAnchor.MiddleLeft);
            line.childControlWidth = true;
            line.childControlHeight = true;
            GameObject statusChip = BuildStateChip(states, "UnitStatus", UiArt.DeathsDoor, UiPalette.Text, out TextMeshProUGUI status);
            GameObject shieldChip = BuildStateChip(states, "UnitShield", UiArt.Shield, UiPalette.Shield, out TextMeshProUGUI shield);
            GameObject burnChip = BuildStateChip(states, "UnitBurn", UiArt.Burn, UiPalette.Burn, out TextMeshProUGUI burn);

            var view = root.gameObject.AddComponent<BattleUnitView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_figureView", figureView);
            UiBuild.SetReference(view, "_plate", plate.Plate);
            UiBuild.SetReference(view, "_plateParty", UiArt.Load(UiArt.PlateParty));
            UiBuild.SetReference(view, "_plateEnemy", UiArt.Load(UiArt.PlateEnemy));
            UiBuild.SetReference(view, "_plateDanger", UiArt.Load(UiArt.PlateDanger));
            UiBuild.SetReference(view, "_plateTarget", UiArt.Load(UiArt.PlateTarget));
            UiBuild.SetReference(view, "_row", plate.Row);
            UiBuild.SetReference(view, "_name", plate.Name);
            UiBuild.SetReference(view, "_hp", plate.Hp);
            UiBuild.SetReference(view, "_hpBar", plate.HpBar);
            UiBuild.SetReference(view, "_shieldChip", shieldChip);
            UiBuild.SetReference(view, "_shield", shield);
            UiBuild.SetReference(view, "_burnChip", burnChip);
            UiBuild.SetReference(view, "_burn", burn);
            UiBuild.SetReference(view, "_statusChip", statusChip);
            UiBuild.SetReference(view, "_status", status);

            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// One unit's board of the board panel: its cells stacked top to bottom on their bag, in the
        /// middle of whichever column of the panel the board is put in. The view sizes the board to
        /// the unit's cells (the bag follows) and makes the cells in it. The whole board is the
        /// button a potion is aimed at; the bag is the graphic the button tints.
        /// </summary>
        static BattleBoardView BuildBattleBoard(Transform parent)
        {
            RectTransform root = UiBuild.Rect("BoardTemplate", parent);
            UiBuild.Stretch(root);

            RectTransform cells = UiBuild.Rect("BoardCells", root);
            UiBuild.Place(cells, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(BattleItemView.CellWidth, BattleItemView.BoardHeight(JobData.MaxItemSlots)));
            UiBuild.Vertical(cells, BattleItemView.CellGapY, 0, TextAnchor.UpperCenter);
            Image bag = BuildBoardBag(cells, "BoardBag");
            bag.raycastTarget = true;
            BattleItemView itemTemplate = BuildBattleItem(cells);
            GameObject emptyCell = BuildEmptyCell(cells);

            Button button = PotionTargetButton(root, bag);

            var view = root.gameObject.AddComponent<BattleBoardView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_cells", cells);
            UiBuild.SetReference(view, "_itemTemplate", itemTemplate);
            UiBuild.SetReference(view, "_emptyCellTemplate", emptyCell);

            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The button of a unit's view on the stage or in the panel: a click anywhere on the view is
        /// the click a potion is aimed with. A disabled one must not look dimmed, because it is
        /// disabled whenever no potion waits for a target.
        /// </summary>
        static Button PotionTargetButton(RectTransform root, Image targetGraphic)
        {
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = targetGraphic;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };
            return button;
        }

        /// <summary>One state on a plate's state line: an icon with its number or words next to it. An icon never stands alone.</summary>
        static GameObject BuildStateChip(Transform line, string name, string art, Color textColor, out TextMeshProUGUI text)
        {
            RectTransform chip = UiBuild.Rect(name + "Chip", line);
            HorizontalLayoutGroup layout = UiBuild.Horizontal(chip, 3f, 0, TextAnchor.MiddleLeft);
            layout.childControlWidth = true;
            layout.childControlHeight = true;

            Image icon = KitIcon(name + "Icon", chip, art);
            var size = icon.gameObject.AddComponent<LayoutElement>();
            size.preferredWidth = 18f;
            size.preferredHeight = 18f;

            text = UiBuild.Label(name, chip, 15f, textColor);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            return chip.gameObject;
        }

        /// <summary>
        /// The place of a unit's figure: the art, or the placeholder (a silhouette and a label)
        /// for a unit without art. Nothing is drawn behind it. The caller places it across its
        /// column. The art keeps its own size whatever the column's width is: it stands at the
        /// bottom centre and may reach into the columns next to it. With
        /// <paramref name="takesClicks"/> the place, as wide as the column, takes clicks without
        /// drawing anything. The battle screen and the party side both use it.
        /// </summary>
        static RectTransform BuildFigure(Transform parent, string name, bool takesClicks, out FigureView view)
        {
            RectTransform figure = UiBuild.Rect(name, parent);
            if (takesClicks)
            {
                var clicks = figure.gameObject.AddComponent<Image>();
                clicks.color = Color.clear;
                clicks.raycastTarget = true;
            }

            // The shadow under the feet, behind whatever stands there, so that the unit stands on the floor instead of floating.
            Image shadow = UiBuild.Image(name + "Shadow", figure, new Color(0f, 0f, 0f, ShadowAlpha));
            shadow.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Place(shadow.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(ShadowWidth, ShadowHeight));

            // The stand-in for a unit without art. The silhouette stands on the place's bottom.
            RectTransform placeholder = UiBuild.Rect(name + "Placeholder", figure);
            UiBuild.Stretch(placeholder);
            BuildSilhouette(placeholder, name);
            UiBuild.Line(
                UiBuild.LocalizedLabel(name + "Label", placeholder, UiKeys.Battle.FigurePlaceholder, 16f, UiPalette.TextDim, TextAlignmentOptions.Center),
                BattleFigureHeight - 32f, 26f);

            // The art. Every figure shares one canvas and one floor line, so each stands in the same place at the same size.
            Image art = UiBuild.Image(name + "Art", figure, Color.white);
            UiBuild.Place(art.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(BattleFigureWidth, BattleFigureHeight));
            art.preserveAspect = true;
            art.enabled = false;

            view = figure.gameObject.AddComponent<FigureView>();
            UiBuild.SetReference(view, "_art", art);
            UiBuild.SetReference(view, "_placeholder", placeholder.gameObject);
            return figure;
        }

        /// <summary>A standing figure made of simple shapes: head, body and two legs, feet at the bottom of the place.</summary>
        static void BuildSilhouette(Transform figure, string prefix)
        {
            Image head = UiBuild.Image(prefix + "Head", figure, UiPalette.ButtonQuiet);
            head.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(56f, 56f));
            Image body = UiBuild.Image(prefix + "Body", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(90f, 112f));
            Image leftLeg = UiBuild.Image(prefix + "LegLeft", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(leftLeg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, -200f), new Vector2(34f, 84f));
            Image rightLeg = UiBuild.Image(prefix + "LegRight", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(rightLeg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -200f), new Vector2(34f, 84f));
        }

        /// <summary>
        /// One item in battle: a cell of the kit that is its own cooldown gauge, with the item's
        /// icon over the charge in the icon's place (the cell less the margin the icons are drawn
        /// for), and the name for an item without an icon. A big item is made taller at runtime:
        /// its cells are stacked.
        /// </summary>
        static BattleItemView BuildBattleItem(Transform parent)
        {
            // The charge covers the cell from the left, inside its rim.
            var charge = new Color(UiPalette.Gauge.r, UiPalette.Gauge.g, UiPalette.Gauge.b, CooldownAlpha);
            UiBar cooldown = KitBar("ItemTemplate", parent, UiArt.Slot, 1f, 7f, charge);
            cooldown.GetComponent<Image>().raycastTarget = true;
            UiBuild.Size(cooldown, BattleItemView.CellWidth, BattleItemView.CellHeight);

            TextMeshProUGUI name = UiBuild.ShrinkToFit(
                UiBuild.SingleLine(UiBuild.Label("ItemName", cooldown.transform, 18f, UiPalette.Text, TextAlignmentOptions.Center)), 13f);
            UiBuild.Stretch(name.rectTransform, 10f, 0f, 10f, 0f);

            Image icon = UiBuild.Image("ItemIcon", cooldown.transform, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform, ItemIconMarginX, ItemIconMarginY, ItemIconMarginX, ItemIconMarginY);

            // The flash over the cell when the item fires, inside the rim. Off until then.
            Image flash = UiBuild.Image("ItemFlash", cooldown.transform, new Color(1f, 1f, 1f, 0f));
            UiBuild.Stretch(flash.rectTransform, 6f, 6f, 6f, 6f);
            flash.enabled = false;

            var view = cooldown.gameObject.AddComponent<BattleItemView>();
            UiBuild.SetReference(view, "_cooldown", cooldown);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_flash", flash);

            cooldown.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// A text that rises from a unit: the colored text in front and a dark copy a little behind
        /// it. The fx layer makes one per text in flight from this template.
        /// </summary>
        static FloatingTextView BuildFloatingText(RectTransform fx)
        {
            RectTransform root = UiBuild.Rect("FloatingTemplate", fx);
            UiBuild.Size(root, 320f, 60f);
            TextMeshProUGUI shadow = UiBuild.Label("FloatingShadow", root, 34f, UiPalette.Ink, TextAlignmentOptions.Center);
            shadow.textWrappingMode = TextWrappingModes.NoWrap;
            UiBuild.Stretch(shadow.rectTransform, 2f, 2f, -2f, -2f);
            TextMeshProUGUI text = UiBuild.Label("FloatingText", root, 34f, UiPalette.Text, TextAlignmentOptions.Center);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            UiBuild.Stretch(text.rectTransform);

            var view = root.gameObject.AddComponent<FloatingTextView>();
            UiBuild.SetReference(view, "_text", text);
            UiBuild.SetReference(view, "_shadow", shadow);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>An empty item slot: a faint cell, so that the player sees how many slots the unit has.</summary>
        static GameObject BuildEmptyCell(Transform parent)
        {
            Image cell = KitFrame("EmptyCellTemplate", parent, UiArt.Slot, raycastTarget: true);
            cell.color = new Color(1f, 1f, 1f, EmptyCellAlpha);
            UiBuild.Size(cell, BattleItemView.CellWidth, BattleItemView.CellHeight);
            cell.gameObject.SetActive(false);
            return cell.gameObject;
        }
    }
}
