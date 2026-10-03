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
        /// The battle screen, top to bottom: the header (retreat, time, storm, speed), the potions
        /// under it on the left, empty headroom, then the field, all in front of the dungeon's
        /// background. Each column of the field holds one unit: a full-body figure standing on the
        /// background's floor, its plate under its feet and its item cells under the plate. The
        /// field's box leaves room at both ends of the frame; the screen sets the columns' places
        /// and widths when it opens (FieldLayout).
        /// </summary>
        const float BattleFieldLeft = 120f;
        const float BattleFieldTop = 262f;
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
        const float BattleItemsTop = BattlePlateTop + PlateHeight + PlateGap;

        static float BattleUnitHeight => BattleItemsTop + BattleItemView.BoardHeight(JobData.MaxItemSlots);

        static UIScreen BuildBattle(Transform holder)
        {
            BattleScreen screen = Screen<BattleScreen>("BattleScreen", holder, out RectTransform frame);

            // The dungeon's background, behind everything else. The screen shows it when the dungeon has one.
            Image background = UiBuild.Image("Background", frame, Color.white);
            UiBuild.Box(background, 0f, BattleFieldTop + BattleFloorY - BattleBackgroundFloor * BattleBackgroundHeight, BattleBackgroundWidth, BattleBackgroundHeight);
            background.enabled = false;

            // Header: retreat on the left, battle time, storm, pause and speed.
            Image header = KitFrame("Header", frame, UiArt.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 84f);
            ButtonParts retreat = KitButton("Retreat", header.transform, UiPalette.Danger, 28f);
            UiBuild.Box(retreat.Rect, 40f, 16f, 300f, 52f);
            TextMeshProUGUI time = UiBuild.Label("Time", header.transform, 40f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(time, 760f, 16f, 400f, 52f);
            UiBuild.Box(KitIcon("StormIcon", header.transform, UiArt.Storm), 1168f, 20f, 44f, 44f);
            TextMeshProUGUI storm = UiBuild.Box(UiBuild.Label("Storm", header.transform, 26f, UiPalette.TextDim), 1220f, 24f, 300f, 36f);

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
            PotionSlotView potionTemplate = BuildPotionStrip(frame, out RectTransform potions);
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
            UiBuild.SetReference(screen, "_time", time);
            UiBuild.SetReference(screen, "_storm", storm);
            UiBuild.SetReference(screen, "_pause", pause.Button);
            UiBuild.SetReference(screen, "_pauseFrame", pause.Frame);
            UiBuild.SetReferences(screen, "_speedButtons", speedButtons);
            UiBuild.SetReferences(screen, "_speedFrames", speedFrames);
            UiBuild.SetReferences(screen, "_speedLabels", speedLabels);
            UiBuild.SetReference(screen, "_unitTemplate", unitTemplate);
            UiBuild.SetReference(screen, "_field", field);
            UiBuild.SetReferences(screen, "_partyRows", partyRows);
            UiBuild.SetReferences(screen, "_enemyRows", enemyRows);
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
        /// One unit: the full-body figure (its art, or a placeholder), its plate under its feet and
        /// its item cells under the plate. The whole unit is the button a potion is aimed at. Its
        /// width follows the column it is put in, so everything inside is laid out as lines that
        /// stretch across it.
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

            // The button lives on the root so that a click on the figure, the plate or a cell counts.
            // A disabled unit must not look dimmed: it is disabled whenever no potion waits for a target.
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = plate.Plate;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            // The line of states: the death's door state, or the shield and the burn the unit
            // carries, each an icon with its number. Only what applies is shown, from the left.
            RectTransform states = PlateStateLine(UiBuild.Rect("UnitStates", plate.Plate.transform));
            HorizontalLayoutGroup line = UiBuild.Horizontal(states, 10f, 0, TextAnchor.MiddleLeft);
            line.childControlWidth = true;
            line.childControlHeight = true;
            GameObject statusChip = BuildStateChip(states, "UnitStatus", UiArt.DeathsDoor, UiPalette.Text, out TextMeshProUGUI status);
            GameObject shieldChip = BuildStateChip(states, "UnitShield", UiArt.Shield, UiPalette.Shield, out TextMeshProUGUI shield);
            GameObject burnChip = BuildStateChip(states, "UnitBurn", UiArt.Burn, UiPalette.Burn, out TextMeshProUGUI burn);

            // The item board: one cell per item slot, top to bottom, as many as a unit can have at most.
            // A click on a cell counts for the unit, as one on its figure or its plate does.
            RectTransform items = UiBuild.Line(UiBuild.Rect("UnitItems", root), BattleItemsTop, BattleItemView.BoardHeight(JobData.MaxItemSlots));
            VerticalLayoutGroup cells = UiBuild.Vertical(items, BattleItemView.CellGap);
            cells.childControlWidth = true;
            cells.childForceExpandWidth = true;
            BattleItemView itemTemplate = BuildBattleItem(items);
            GameObject emptyCell = BuildEmptyCell(items);

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
            UiBuild.SetReference(view, "_itemTemplate", itemTemplate);
            UiBuild.SetReference(view, "_emptyCellTemplate", emptyCell);
            UiBuild.SetReference(view, "_itemParent", items);

            root.gameObject.SetActive(false);
            return view;
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
        /// for), and the name for an item without an icon. Its width follows the board; a big
        /// item is made taller at runtime.
        /// </summary>
        static BattleItemView BuildBattleItem(Transform parent)
        {
            // The charge covers the cell from the left, inside its rim.
            var charge = new Color(UiPalette.Gauge.r, UiPalette.Gauge.g, UiPalette.Gauge.b, CooldownAlpha);
            UiBar cooldown = KitBar("ItemTemplate", parent, UiArt.Slot, 1f, 7f, charge);
            cooldown.GetComponent<Image>().raycastTarget = true;
            UiBuild.Size(cooldown, 100f, BattleItemView.CellHeight);

            TextMeshProUGUI name = UiBuild.ShrinkToFit(
                UiBuild.SingleLine(UiBuild.Label("ItemName", cooldown.transform, 18f, UiPalette.Text, TextAlignmentOptions.Center)), 13f);
            UiBuild.Stretch(name.rectTransform, 10f, 0f, 10f, 0f);

            Image icon = UiBuild.Image("ItemIcon", cooldown.transform, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform, ItemIconMarginX, ItemIconMarginY, ItemIconMarginX, ItemIconMarginY);

            var view = cooldown.gameObject.AddComponent<BattleItemView>();
            UiBuild.SetReference(view, "_cooldown", cooldown);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_name", name);

            cooldown.gameObject.SetActive(false);
            return view;
        }

        /// <summary>An empty item slot: a faint cell, so that the player sees how many slots the unit has.</summary>
        static GameObject BuildEmptyCell(Transform parent)
        {
            Image cell = KitFrame("EmptyCellTemplate", parent, UiArt.Slot, raycastTarget: true);
            cell.color = new Color(1f, 1f, 1f, EmptyCellAlpha);
            UiBuild.Size(cell, 100f, BattleItemView.CellHeight);
            cell.gameObject.SetActive(false);
            return cell.gameObject;
        }
    }
}
