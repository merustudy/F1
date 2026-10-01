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
        /// under it on the left, empty headroom for the background art, then the field. Each
        /// column of the field holds one unit: a full-body figure standing on the floor line and
        /// the info card under it. The screen sets the columns' places and widths when it opens.
        /// </summary>
        const float BattleFieldTop = 236f;
        const float BattleFieldWidth = 1880f;
        const float BattleColumnLabelHeight = 28f;
        const float BattleFigureHeight = 300f;
        const float BattleFigureGap = 12f;
        const float BattleCardMargin = 12f;
        const float BattleCardItemsTop = 164f;
        const float BattleFloorY = BattleColumnLabelHeight - 2f + BattleFigureHeight + 2f;

        static float BattleCardHeight => BattleCardItemsTop + BattleItemView.BoardHeight(JobData.MaxItemSlots) + BattleCardMargin;
        static float BattleUnitHeight => BattleFigureHeight + BattleFigureGap + BattleCardHeight;
        static float BattleFieldHeight => BattleColumnLabelHeight - 2f + BattleUnitHeight;

        static UIScreen BuildBattle(Transform holder)
        {
            BattleScreen screen = Screen<BattleScreen>("BattleScreen", holder, out RectTransform frame);

            // Header: retreat on the left, battle time, storm, pause and speed.
            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 80f);
            ButtonParts retreat = UiBuild.Button("Retreat", header.transform, UiPalette.Danger, 28f);
            UiBuild.Box(retreat.Rect, 40f, 14f, 300f, 52f);
            TextMeshProUGUI time = UiBuild.Label("Time", header.transform, 40f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(time, 760f, 14f, 400f, 52f);
            TextMeshProUGUI storm = UiBuild.Box(UiBuild.Label("Storm", header.transform, 26f, UiPalette.TextDim), 1180f, 22f, 330f, 36f);

            ButtonParts pause = UiBuild.LocalizedButton("Pause", header.transform, UiKeys.Battle.Pause, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(pause.Rect, 1530f, 14f, 110f, 52f);
            var speedButtons = new Button[3];
            var speedFrames = new Image[3];
            var speedLabels = new TextMeshProUGUI[3];
            for (int i = 0; i < 3; i++)
            {
                ButtonParts speed = UiBuild.Button("Speed" + i, header.transform, UiPalette.ButtonQuiet, 24f);
                UiBuild.Box(speed.Rect, 1650f + 90f * i, 14f, 80f, 52f);
                speedButtons[i] = speed.Button;
                speedFrames[i] = speed.Frame;
                speedLabels[i] = speed.Label;
            }

            // Potions under the retreat button, on a strip so that the slots show against the
            // background, with the hint next to them. The rest of the space above the field stays
            // empty: the background art goes there.
            Image potionsPanel = UiBuild.Panel("PotionsPanel", frame, UiPalette.Panel);
            UiBuild.Box(potionsPanel, 30f, 90f, 610f, 76f);
            RectTransform potions = UiBuild.Box(UiBuild.Rect("Potions", frame), 40f, 96f, 590f, 64f);
            UiBuild.Horizontal(potions, 10f);
            PotionSlotView potionTemplate = BuildPotionSlot(potions, 190f);
            TextMeshProUGUI potionHint = UiBuild.Label("PotionHint", frame, 20f, UiPalette.TextDim, TextAlignmentOptions.Left);
            UiBuild.Box(potionHint, 660f, 96f, 700f, 64f);

            // Field: one column per row on each side. The party's columns come first in the prefab
            // and the enemy's after them; the screen puts them in place when it opens, because how
            // many the party uses depends on the party size. The floor line runs under the figures.
            RectTransform field = UiBuild.Box(UiBuild.Rect("Field", frame), 20f, BattleFieldTop, BattleFieldWidth, BattleFieldHeight);
            Image floor = UiBuild.Image("Floor", field, UiPalette.Line);
            UiBuild.Line(floor, BattleFloorY, 2f);
            float columnWidth = BattleFieldWidth / (BattleRows.Count * 2);
            var partyRows = new RectTransform[BattleRows.Count];
            var partyRowLabels = new RectTransform[BattleRows.Count];
            var enemyRows = new RectTransform[BattleRows.Count];
            var enemyRowLabels = new RectTransform[BattleRows.Count];
            for (int i = 0; i < BattleRows.Count; i++)
            {
                int row = i + 1;
                partyRows[i] = BuildBattleColumn(field, "PartyRow" + row, UiText.RowKey(row), columnWidth * (BattleRows.Count - row), columnWidth, out partyRowLabels[i]);
                enemyRows[i] = BuildBattleColumn(field, "EnemyRow" + row, UiText.RowKey(row), columnWidth * (BattleRows.Count + i), columnWidth, out enemyRowLabels[i]);
            }

            BattleUnitView unitTemplate = BuildBattleUnit(field);

            // Result: covers the field when the battle has ended.
            Image overlay = UiBuild.Image("ResultPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(overlay.rectTransform);
            Image result = UiBuild.Panel("ResultBox", overlay.transform, UiPalette.Panel);
            UiBuild.Box(result, 310f, 170f, 1300f, 540f);
            TextMeshProUGUI resultTitle = UiBuild.Label("ResultTitle", result.transform, 72f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(resultTitle, 0f, 30f, 1300f, 100f);
            TextMeshProUGUI resultDetail = UiBuild.Label("ResultDetail", result.transform, 24f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(resultDetail, 50f, 150f, 1200f, 250f);
            ButtonParts showLog = UiBuild.LocalizedButton("ShowLog", result.transform, UiKeys.Battle.ShowLog, UiPalette.ButtonQuiet, 32f);
            UiBuild.Box(showLog.Rect, 250f, 420f, 380f, 84f);
            ButtonParts resume = UiBuild.LocalizedButton("Continue", result.transform, UiKeys.Common.Continue, UiPalette.Button, 36f);
            UiBuild.Box(resume.Rect, 670f, 420f, 380f, 84f);

            // The whole log of the finished battle, over the result. Shown by "show log"; the lines
            // are created at runtime. Built after the result so that it is drawn over it.
            Image logOverlay = UiBuild.Image("LogPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(logOverlay.rectTransform);
            Image logBox = UiBuild.Panel("LogBox", logOverlay.transform, UiPalette.Panel);
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
            ButtonParts logClose = UiBuild.LocalizedButton("LogClose", logBox.transform, UiKeys.Common.Close, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(logClose.Rect, 560f, 840f, 400f, 64f);
            logOverlay.gameObject.SetActive(false);

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
            UiBuild.SetReferences(screen, "_partyRowLabels", partyRowLabels);
            UiBuild.SetReferences(screen, "_enemyRows", enemyRows);
            UiBuild.SetReferences(screen, "_enemyRowLabels", enemyRowLabels);
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

        /// <summary>
        /// One row of a side: a small label over a column that holds the row's unit. The unit takes
        /// the width of the column.
        /// </summary>
        static RectTransform BuildBattleColumn(Transform field, string name, string labelKey, float x, float width, out RectTransform labelRect)
        {
            TextMeshProUGUI label = UiBuild.LocalizedLabel(name + "Label", field, labelKey, 20f, UiPalette.TextDim, TextAlignmentOptions.Center);
            labelRect = UiBuild.Box(label.rectTransform, x, 0f, width, BattleColumnLabelHeight);

            float top = BattleColumnLabelHeight - 2f;
            RectTransform column = UiBuild.Box(UiBuild.Rect(name, field), x, top, width, BattleFieldHeight - top);
            VerticalLayoutGroup units = UiBuild.Vertical(column, 10f);
            units.childControlWidth = true;
            units.childForceExpandWidth = true;
            return column;
        }

        /// <summary>
        /// One unit: the full-body figure (a placeholder until there is art) over the info card.
        /// The whole unit is the button a potion is aimed at. Its width follows the column it is
        /// put in, so everything inside is laid out as lines that stretch across it.
        /// </summary>
        static BattleUnitView BuildBattleUnit(Transform parent)
        {
            RectTransform root = UiBuild.Rect("UnitTemplate", parent);
            UiBuild.Size(root, BattleFieldWidth / (BattleRows.Count * 2), BattleUnitHeight);

            // Figure placeholder: a faint rounded frame in the side's color with a silhouette in it.
            Image figure = UiBuild.Image("UnitFigure", root, BattleUnitView.FigureTint(UiPalette.Party), raycastTarget: true);
            figure.sprite = UiBuild.BuiltinSprite("UI/Skin/UISprite.psd");
            figure.type = Image.Type.Sliced;
            UiBuild.Line(figure, 0f, BattleFigureHeight);
            BuildSilhouette(figure.transform);
            UiBuild.Line(
                UiBuild.LocalizedLabel("FigureLabel", figure.transform, UiKeys.Battle.FigurePlaceholder, 16f, UiPalette.TextDim, TextAlignmentOptions.Center),
                BattleFigureHeight - 32f, 26f);

            // Info card: name, status, HP, shield and burn, then the item board.
            Image frame = UiBuild.Image("UnitCard", root, UiPalette.Party, raycastTarget: true);
            UiBuild.Line(frame, BattleFigureHeight + BattleFigureGap, BattleCardHeight);

            // The button lives on the root so that a click on the figure or on the card both count.
            // A disabled unit must not look dimmed: it is disabled whenever no potion waits for a target.
            var button = root.gameObject.AddComponent<Button>();
            button.targetGraphic = frame;
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.88f, 0.88f, 0.88f, 1f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            button.colors = colors;
            button.navigation = new Navigation { mode = Navigation.Mode.None };

            const float m = BattleCardMargin;
            Transform card = frame.transform;
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Line(UiBuild.Label("UnitName", card, 28f, UiPalette.Text), 10f, 38f, m, m));
            TextMeshProUGUI status = UiBuild.Line(UiBuild.Label("UnitStatus", card, 17f, UiPalette.Text, TextAlignmentOptions.TopLeft), 50f, 46f, m, m);

            UiBar hpBar = UiBuild.Bar("UnitHpBar", card, UiPalette.Good);
            UiBuild.Line(hpBar, 100f, 26f, m, m);
            TextMeshProUGUI hp = UiBuild.Label("UnitHp", card, 18f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Line(hp, 100f, 26f, m, m);

            // Shield on the left half of the line, burn on the right half.
            TextMeshProUGUI shield = UiBuild.SingleLine(UiBuild.Line(UiBuild.Label("UnitShield", card, 18f, UiPalette.Shield), 130f, 26f, m, m));
            shield.rectTransform.anchorMax = new Vector2(0.5f, 1f);
            shield.rectTransform.offsetMax = new Vector2(0f, shield.rectTransform.offsetMax.y);
            TextMeshProUGUI burn = UiBuild.SingleLine(UiBuild.Line(UiBuild.Label("UnitBurn", card, 18f, UiPalette.Burn, TextAlignmentOptions.Right), 130f, 26f, m, m));
            burn.rectTransform.anchorMin = new Vector2(0.5f, 1f);
            burn.rectTransform.offsetMin = new Vector2(0f, burn.rectTransform.offsetMin.y);

            // The item board: one cell per item slot, top to bottom, as many as a unit can have at most.
            RectTransform items = UiBuild.Line(UiBuild.Rect("UnitItems", card), BattleCardItemsTop, BattleItemView.BoardHeight(JobData.MaxItemSlots), m, m);
            VerticalLayoutGroup cells = UiBuild.Vertical(items, BattleItemView.CellGap);
            cells.childControlWidth = true;
            cells.childForceExpandWidth = true;
            BattleItemView itemTemplate = BuildBattleItem(items);
            GameObject emptyCell = BuildEmptyCell(items);

            var view = root.gameObject.AddComponent<BattleUnitView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_figure", figure);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_status", status);
            UiBuild.SetReference(view, "_hp", hp);
            UiBuild.SetReference(view, "_hpBar", hpBar);
            UiBuild.SetReference(view, "_shield", shield);
            UiBuild.SetReference(view, "_burn", burn);
            UiBuild.SetReference(view, "_itemTemplate", itemTemplate);
            UiBuild.SetReference(view, "_emptyCellTemplate", emptyCell);
            UiBuild.SetReference(view, "_itemParent", items);

            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A standing figure made of simple shapes: head, body and two legs, feet at the bottom of the frame.</summary>
        static void BuildSilhouette(Transform figure)
        {
            Image head = UiBuild.Image("FigureHead", figure, UiPalette.ButtonQuiet);
            head.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Place(head.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(56f, 56f));
            Image body = UiBuild.Image("FigureBody", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -88f), new Vector2(90f, 112f));
            Image leftLeg = UiBuild.Image("FigureLegLeft", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(leftLeg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-24f, -200f), new Vector2(34f, 84f));
            Image rightLeg = UiBuild.Image("FigureLegRight", figure, UiPalette.ButtonQuiet);
            UiBuild.Place(rightLeg.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(24f, -200f), new Vector2(34f, 84f));
        }

        /// <summary>
        /// One item in battle: a cell with a geometric placeholder icon behind the cooldown fill
        /// and the item's name over it. The icon stands for the item's first effect.
        /// </summary>
        static BattleItemView BuildBattleItem(Transform parent)
        {
            UiBar cooldown = UiBuild.Bar("ItemTemplate", parent, UiPalette.Gauge);
            UiBuild.Size(cooldown, 100f, BattleItemView.CellHeight);
            Transform cell = cooldown.transform;

            // The fill is translucent so that the icon under it stays visible.
            Image fill = cell.GetChild(0).GetComponent<Image>();
            fill.color = new Color(UiPalette.Gauge.r, UiPalette.Gauge.g, UiPalette.Gauge.b, 0.82f);

            GameObject damage = BuildIconShape(cell, "ItemIconDamage", IconShape.Diamond);
            GameObject heal = BuildIconShape(cell, "ItemIconHeal", IconShape.Plus);
            GameObject shield = BuildIconShape(cell, "ItemIconShield", IconShape.Square);
            GameObject burn = BuildIconShape(cell, "ItemIconBurn", IconShape.Circle);
            foreach (GameObject icon in new[] { burn, shield, heal, damage })
            {
                icon.transform.SetSiblingIndex(0);
            }

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Label("ItemName", cell, 17f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(name.rectTransform, 4f, 0f, 4f, 0f);

            var view = cooldown.gameObject.AddComponent<BattleItemView>();
            UiBuild.SetReference(view, "_cooldown", cooldown);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_iconDamage", damage);
            UiBuild.SetReference(view, "_iconHeal", heal);
            UiBuild.SetReference(view, "_iconShield", shield);
            UiBuild.SetReference(view, "_iconBurn", burn);

            cooldown.gameObject.SetActive(false);
            return view;
        }

        enum IconShape { Diamond, Plus, Square, Circle }

        /// <summary>A geometric placeholder icon centered in a cell, made of plain images. It is translucent so that the name over it stays readable.</summary>
        static GameObject BuildIconShape(Transform cell, string name, IconShape shape)
        {
            RectTransform icon = UiBuild.Rect(name, cell);
            UiBuild.Place(icon, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
            var group = icon.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0.6f;
            group.blocksRaycasts = false;
            switch (shape)
            {
                case IconShape.Diamond:
                    Image diamond = UiBuild.Image(name + "Shape", icon, UiPalette.Icon);
                    UiBuild.Place(diamond.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(24f, 24f));
                    diamond.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    break;
                case IconShape.Plus:
                    Image vertical = UiBuild.Image(name + "Vertical", icon, UiPalette.Icon);
                    UiBuild.Place(vertical.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(9f, 32f));
                    Image horizontal = UiBuild.Image(name + "Horizontal", icon, UiPalette.Icon);
                    UiBuild.Place(horizontal.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 9f));
                    break;
                case IconShape.Square:
                    Image square = UiBuild.Image(name + "Shape", icon, UiPalette.Icon);
                    UiBuild.Place(square.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26f, 26f));
                    break;
                default:
                    Image circle = UiBuild.Image(name + "Shape", icon, UiPalette.Icon);
                    circle.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
                    UiBuild.Place(circle.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(32f, 32f));
                    break;
            }

            icon.gameObject.SetActive(false);
            return icon.gameObject;
        }

        /// <summary>An empty item slot: a faint cell, so that the player sees how many slots the unit has.</summary>
        static GameObject BuildEmptyCell(Transform parent)
        {
            Image cell = UiBuild.Image("EmptyCellTemplate", parent, new Color(UiPalette.Line.r, UiPalette.Line.g, UiPalette.Line.b, 0.3f));
            cell.sprite = UiBuild.BuiltinSprite("UI/Skin/UISprite.psd");
            cell.type = Image.Type.Sliced;
            UiBuild.Size(cell, 100f, BattleItemView.CellHeight);
            cell.gameObject.SetActive(false);
            return cell.gameObject;
        }
    }
}
