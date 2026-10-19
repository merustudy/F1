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

            // The traits the expedition left (Slice D stage 38, Round 84 판정 7): one line a survivor under the "ready" line — a bone diamond, the
            // words and the traits' chips (icon 22, name 24) — and the card their right click opens. The screen places the lines it shows.
            TraitTooltipView traitTooltip = BuildTraitTooltip(p, "Settle");
            var newTraitRows = new RectTransform[NewTraitRows];
            var newTraitLabels = new TextMeshProUGUI[NewTraitRows];
            var newTraitLines = new TraitLineView[NewTraitRows];
            for (int i = 0; i < NewTraitRows; i++)
            {
                RectTransform row = UiBuild.Box(UiBuild.Rect("NewTraits" + i, p), 110f, 544f, 910f, 34f);
                RectTransform mark = UiBuild.Box(UiBuild.Rect("NewTraits" + i + "Mark", row), 4f, 6f, 22f, 22f);
                Diamond("NewTraits" + i + "MarkFill", mark, UiPalette.Bone, 15f, new Vector2(0.5f, 0.5f));
                newTraitLabels[i] = UiBuild.SingleLine(UiBuild.Label("NewTraits" + i + "Label", row, 24f, UiPalette.Bone, TextAlignmentOptions.Left));
                UiBuild.Box(newTraitLabels[i].rectTransform, 36f, 0f, 400f, 34f);
                newTraitLines[i] = BuildTraitLine("NewTraits" + i + "Line", row, 460f, 0f, 34f, 22f, 24f, NewTraitChips, true, traitTooltip);
                row.gameObject.SetActive(false);
                newTraitRows[i] = row;
            }

            traitTooltip.transform.SetAsLastSibling();

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
            UiBuild.SetReferences(screen, "_newTraitRows", newTraitRows);
            UiBuild.SetReferences(screen, "_newTraitLabels", newTraitLabels);
            UiBuild.SetReferences(screen, "_newTraitLines", newTraitLines);
            return screen;
        }

        /// <summary>The "new traits" lines of the settlement (stage 38): one for each member a party can have, and the most traits one expedition leaves (a source each).</summary>
        const int NewTraitRows = 4;
        const int NewTraitChips = 3;
    }
}
