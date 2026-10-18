using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildSettlement(Transform holder)
        {
            SettlementScreen screen = Screen<SettlementScreen>("SettlementScreen", holder, out RectTransform frame);

            Image panel = UiBuild.Panel("Panel", frame, UiPalette.Panel);
            UiBuild.Box(panel, 410f, 160f, 1100f, 760f);
            Transform p = panel.transform;

            TextMeshProUGUI title = UiBuild.Label("Title", p, 64f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(title, 0f, 40f, 1100f, 90f);
            TextMeshProUGUI dungeon = UiBuild.Label("Dungeon", p, 32f, UiPalette.TextDim, TextAlignmentOptions.Center);
            UiBuild.Box(dungeon, 0f, 134f, 1100f, 44f);

            TextMeshProUGUI survivors = UiBuild.Box(UiBuild.Label("Survivors", p, 30f, UiPalette.Text), 80f, 220f, 940f, 44f);
            TextMeshProUGUI fallen = UiBuild.Box(UiBuild.Label("Fallen", p, 30f, UiPalette.Text), 80f, 276f, 940f, 44f);
            TextMeshProUGUI fatigue = UiBuild.Box(UiBuild.Label("Fatigue", p, 28f, UiPalette.Text), 80f, 350f, 940f, 40f);
            TextMeshProUGUI days = UiBuild.Box(UiBuild.Label("Days", p, 28f, UiPalette.Text), 80f, 398f, 940f, 40f);
            TextMeshProUGUI gold = UiBuild.Box(UiBuild.Label("Gold", p, 28f, UiPalette.DiabloGold), 80f, 446f, 940f, 40f);
            TextMeshProUGUI exp = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("Exp", p, 28f, UiPalette.Bone)), 80f, 494f, 940f, 40f);
            // Who can level up in the barracks (Slice D stage 35, Round 82): one gold line under the experience; the screen grows the panel by it.
            TextMeshProUGUI ready = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("Ready", p, 24f, UiPalette.StarLit, TextAlignmentOptions.TopLeft)), 110f, 544f, 910f, 0f);
            TextMeshProUGUI itemsLost = UiBuild.Box(UiBuild.LocalizedLabel("ItemsLost", p, UiKeys.Settle.ItemsLost, 24f, UiPalette.TextDim), 80f, 542f, 940f, 36f);
            TextMeshProUGUI runOver = UiBuild.Box(UiBuild.Label("RunOver", p, 32f, UiPalette.Danger), 80f, 586f, 940f, 48f);

            ButtonParts confirm = UiBuild.LocalizedButton("Confirm", p, UiKeys.Common.Confirm, UiPalette.Button, 36f);
            UiBuild.Box(confirm.Rect, 350f, 650f, 400f, 90f);

            UiBuild.SetReference(screen, "_title", title);
            UiBuild.SetReference(screen, "_dungeon", dungeon);
            UiBuild.SetReference(screen, "_survivors", survivors);
            UiBuild.SetReference(screen, "_fallen", fallen);
            UiBuild.SetReference(screen, "_fatigue", fatigue);
            UiBuild.SetReference(screen, "_days", days);
            UiBuild.SetReference(screen, "_gold", gold);
            UiBuild.SetReference(screen, "_exp", exp);
            UiBuild.SetReference(screen, "_runOver", runOver);
            UiBuild.SetReference(screen, "_confirm", confirm.Button);
            UiBuild.SetReference(screen, "_ready", ready);
            UiBuild.SetReference(screen, "_itemsLost", itemsLost);
            UiBuild.SetReference(screen, "_panel", (RectTransform)panel.transform);
            return screen;
        }
    }
}
