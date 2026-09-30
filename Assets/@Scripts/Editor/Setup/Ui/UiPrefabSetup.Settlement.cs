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
            UiBuild.Box(UiBuild.LocalizedLabel("ItemsLost", p, UiKeys.Settle.ItemsLost, 24f, UiPalette.TextDim), 80f, 456f, 940f, 36f);
            TextMeshProUGUI runOver = UiBuild.Box(UiBuild.Label("RunOver", p, 32f, UiPalette.Danger), 80f, 520f, 940f, 48f);

            ButtonParts confirm = UiBuild.LocalizedButton("Confirm", p, UiKeys.Common.Confirm, UiPalette.Button, 36f);
            UiBuild.Box(confirm.Rect, 350f, 630f, 400f, 90f);

            UiBuild.SetReference(screen, "_title", title);
            UiBuild.SetReference(screen, "_dungeon", dungeon);
            UiBuild.SetReference(screen, "_survivors", survivors);
            UiBuild.SetReference(screen, "_fallen", fallen);
            UiBuild.SetReference(screen, "_fatigue", fatigue);
            UiBuild.SetReference(screen, "_days", days);
            UiBuild.SetReference(screen, "_runOver", runOver);
            UiBuild.SetReference(screen, "_confirm", confirm.Button);
            return screen;
        }
    }
}
