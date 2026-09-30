using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildBattle(Transform holder)
        {
            BattleScreen screen = Screen<BattleScreen>("BattleScreen", holder, out RectTransform frame);

            // Header: where, battle time, storm, speed.
            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 80f);
            TextMeshProUGUI node = UiBuild.Box(UiBuild.Label("Node", header.transform, 32f, UiPalette.Text), 40f, 18f, 500f, 44f);
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

            // Field: party on the left (rear, then front), enemies on the right (front, then rear).
            RectTransform partyRear = BuildBattleColumn(frame, "PartyRear", UiKeys.Common.Rear, 30f);
            RectTransform partyFront = BuildBattleColumn(frame, "PartyFront", UiKeys.Common.Front, 490f);
            RectTransform enemyFront = BuildBattleColumn(frame, "EnemyFront", UiKeys.Common.Front, 990f);
            RectTransform enemyRear = BuildBattleColumn(frame, "EnemyRear", UiKeys.Common.Rear, 1450f);
            BattleUnitView unitTemplate = BuildBattleUnit(frame);

            // Potions and retreat.
            Image controls = UiBuild.Panel("Controls", frame, UiPalette.Panel);
            UiBuild.Box(controls, 30f, 760f, 560f, 300f);
            UiBuild.Box(UiBuild.LocalizedLabel("PotionsHeader", controls.transform, UiKeys.Board.Potions, 24f, UiPalette.TextDim), 16f, 10f, 300f, 32f);
            RectTransform potions = UiBuild.Box(UiBuild.Rect("Potions", controls.transform), 16f, 48f, 528f, 64f);
            UiBuild.Horizontal(potions, 8f);
            PotionSlotView potionTemplate = BuildPotionSlot(potions, 170f);
            TextMeshProUGUI potionHint = UiBuild.Label("PotionHint", controls.transform, 22f, UiPalette.TextDim, TextAlignmentOptions.TopLeft);
            UiBuild.Box(potionHint, 16f, 122f, 528f, 64f);
            ButtonParts retreat = UiBuild.Button("Retreat", controls.transform, UiPalette.Danger, 32f);
            UiBuild.Box(retreat.Rect, 16f, 200f, 528f, 84f);

            // Log: newest line at the bottom.
            Image logPanel = UiBuild.Panel("Log", frame, UiPalette.Panel);
            UiBuild.Box(logPanel, 610f, 760f, 1280f, 300f);
            TextMeshProUGUI log = UiBuild.Label("LogText", logPanel.transform, 19f, UiPalette.Text, TextAlignmentOptions.BottomLeft);
            log.textWrappingMode = TextWrappingModes.NoWrap;
            log.overflowMode = TextOverflowModes.Truncate;
            UiBuild.Box(log, 16f, 10f, 1248f, 280f);

            // Result: covers the field when the battle has ended.
            Image overlay = UiBuild.Image("ResultPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(overlay.rectTransform);
            Image result = UiBuild.Panel("ResultBox", overlay.transform, UiPalette.Panel);
            UiBuild.Box(result, 310f, 170f, 1300f, 540f);
            TextMeshProUGUI resultTitle = UiBuild.Label("ResultTitle", result.transform, 72f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(resultTitle, 0f, 30f, 1300f, 100f);
            TextMeshProUGUI resultDetail = UiBuild.Label("ResultDetail", result.transform, 24f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(resultDetail, 50f, 150f, 1200f, 250f);
            ButtonParts resume = UiBuild.LocalizedButton("Continue", result.transform, UiKeys.Common.Continue, UiPalette.Button, 36f);
            UiBuild.Box(resume.Rect, 450f, 420f, 400f, 84f);

            UiBuild.SetReference(screen, "_node", node);
            UiBuild.SetReference(screen, "_time", time);
            UiBuild.SetReference(screen, "_storm", storm);
            UiBuild.SetReference(screen, "_pause", pause.Button);
            UiBuild.SetReference(screen, "_pauseFrame", pause.Frame);
            UiBuild.SetReferences(screen, "_speedButtons", speedButtons);
            UiBuild.SetReferences(screen, "_speedFrames", speedFrames);
            UiBuild.SetReferences(screen, "_speedLabels", speedLabels);
            UiBuild.SetReference(screen, "_unitTemplate", unitTemplate);
            UiBuild.SetReference(screen, "_partyFront", partyFront);
            UiBuild.SetReference(screen, "_partyRear", partyRear);
            UiBuild.SetReference(screen, "_enemyFront", enemyFront);
            UiBuild.SetReference(screen, "_enemyRear", enemyRear);
            UiBuild.SetReference(screen, "_potionTemplate", potionTemplate);
            UiBuild.SetReference(screen, "_potionParent", potions);
            UiBuild.SetReference(screen, "_potionHint", potionHint);
            UiBuild.SetReference(screen, "_retreat", retreat.Button);
            UiBuild.SetReference(screen, "_retreatLabel", retreat.Label);
            UiBuild.SetReference(screen, "_log", log);
            UiBuild.SetReference(screen, "_resultPanel", overlay.gameObject);
            UiBuild.SetReference(screen, "_resultTitle", resultTitle);
            UiBuild.SetReference(screen, "_resultDetail", resultDetail);
            UiBuild.SetReference(screen, "_continue", resume.Button);
            return screen;
        }

        /// <summary>One row of a side: a small label and a column that stacks up to three unit cards.</summary>
        static RectTransform BuildBattleColumn(Transform frame, string name, string labelKey, float x)
        {
            TextMeshProUGUI label = UiBuild.LocalizedLabel(name + "Label", frame, labelKey, 20f, UiPalette.TextDim, TextAlignmentOptions.Center);
            UiBuild.Box(label, x, 88f, 440f, 28f);

            RectTransform column = UiBuild.Box(UiBuild.Rect(name, frame), x, 120f, 440f, 630f);
            UiBuild.Vertical(column, 10f);
            return column;
        }

        static BattleUnitView BuildBattleUnit(Transform parent)
        {
            Image frame = UiBuild.Image("UnitTemplate", parent, UiPalette.Party);
            UiBuild.Size(frame, 440f, 203f);
            Button button = UiBuild.MakeButton(frame);

            // A disabled unit card must not look dimmed: it is disabled whenever no potion is waiting for a target.
            ColorBlock colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;

            Transform unit = frame.transform;
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("UnitName", unit, 28f, UiPalette.Text), 14f, 6f, 412f, 36f));
            TextMeshProUGUI status = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("UnitStatus", unit, 19f, UiPalette.Text), 14f, 42f, 412f, 26f));

            UiBar hpBar = UiBuild.Bar("UnitHpBar", unit, UiPalette.Good);
            UiBuild.Box(hpBar, 14f, 72f, 412f, 24f);
            TextMeshProUGUI hp = UiBuild.Label("UnitHp", unit, 18f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(hp, 14f, 72f, 412f, 24f);

            TextMeshProUGUI shield = UiBuild.Box(UiBuild.Label("UnitShield", unit, 20f, UiPalette.Shield), 14f, 100f, 200f, 28f);
            TextMeshProUGUI burn = UiBuild.Box(UiBuild.Label("UnitBurn", unit, 20f, UiPalette.Burn, TextAlignmentOptions.Right), 226f, 100f, 200f, 28f);

            RectTransform items = UiBuild.Box(UiBuild.Rect("UnitItems", unit), 14f, 132f, 412f, 63f);
            UiBuild.Grid(items, new Vector2(134f, 29f), 5f, 3);
            BattleItemView itemTemplate = BuildBattleItem(items);

            var view = frame.gameObject.AddComponent<BattleUnitView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_status", status);
            UiBuild.SetReference(view, "_hp", hp);
            UiBuild.SetReference(view, "_hpBar", hpBar);
            UiBuild.SetReference(view, "_shield", shield);
            UiBuild.SetReference(view, "_burn", burn);
            UiBuild.SetReference(view, "_itemTemplate", itemTemplate);
            UiBuild.SetReference(view, "_itemParent", items);

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>One item in battle: its name over a bar that fills as the cooldown runs.</summary>
        static BattleItemView BuildBattleItem(Transform parent)
        {
            UiBar cooldown = UiBuild.Bar("ItemTemplate", parent, UiPalette.Gauge);
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Label("ItemName", cooldown.transform, 16f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(name.rectTransform, 4f, 0f, 4f, 0f);

            var view = cooldown.gameObject.AddComponent<BattleItemView>();
            UiBuild.SetReference(view, "_cooldown", cooldown);
            UiBuild.SetReference(view, "_name", name);

            cooldown.gameObject.SetActive(false);
            return view;
        }
    }
}
