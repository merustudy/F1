using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>The reward screen: the party on the left as in battle, the rewards stacked on the right with the buttons under them.</summary>
        static UIScreen BuildReward(Transform holder)
        {
            RewardScreen screen = Screen<RewardScreen>("RewardScreen", holder, out RectTransform frame);

            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 80f);
            UiBuild.Box(UiBuild.LocalizedLabel("RewardTitle", header.transform, UiKeys.Reward.Title, 36f, UiPalette.Text), 40f, 14f, 900f, 52f);

            // The rewards, one under the other, as wide as the right half.
            RectTransform options = UiBuild.Box(UiBuild.Rect("Options", frame), 980f, 110f, 920f, 636f);
            VerticalLayoutGroup stack = UiBuild.Vertical(options, 12f);
            stack.childControlWidth = true;
            stack.childForceExpandWidth = true;
            RewardOptionView optionTemplate = BuildRewardOption(options);

            UiBuild.Box(UiBuild.LocalizedLabel("RewardHint", frame, UiKeys.Reward.Hint, 22f, UiPalette.TextDim), 980f, 770f, 920f, 60f);

            ButtonParts toInventory = UiBuild.LocalizedButton("RewardToInventory", frame, UiKeys.Reward.ToInventory, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(toInventory.Rect, 980f, 940f, 290f, 84f);
            ButtonParts inventoryToggle = UiBuild.Button("InventoryToggle", frame, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(inventoryToggle.Rect, 1290f, 940f, 290f, 84f);
            ButtonParts skip = UiBuild.LocalizedButton("Skip", frame, UiKeys.Reward.Skip, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(skip.Rect, 1600f, 940f, 300f, 84f);

            PartySideView party = BuildPartySide(frame);

            UiBuild.SetReference(screen, "_optionTemplate", optionTemplate);
            UiBuild.SetReference(screen, "_optionParent", options);
            UiBuild.SetReference(screen, "_skip", skip.Button);
            UiBuild.SetReference(screen, "_toInventory", toInventory.Button);
            UiBuild.SetReference(screen, "_inventoryToggle", inventoryToggle.Button);
            UiBuild.SetReference(screen, "_inventoryToggleLabel", inventoryToggle.Label);
            UiBuild.SetReference(screen, "_party", party);
            return screen;
        }

        /// <summary>One reward card. The whole card is the button; the action text sits at its bottom right.</summary>
        static RewardOptionView BuildRewardOption(Transform parent)
        {
            Image frame = UiBuild.Image("OptionTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(frame, 920f, 200f);
            Button button = UiBuild.MakeButton(frame);
            Transform option = frame.transform;

            TextMeshProUGUI kind = UiBuild.Box(UiBuild.Label("OptionKind", option, 22f, UiPalette.TextDim), 20f, 12f, 300f, 30f);
            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("OptionTitle", option, 32f, UiPalette.Text), 20f, 44f, 880f, 44f));
            TextMeshProUGUI body = UiBuild.Label("OptionBody", option, 22f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(body, 20f, 92f, 880f, 100f);
            TextMeshProUGUI action = UiBuild.Label("OptionAction", option, 26f, UiPalette.Text, TextAlignmentOptions.Right);
            UiBuild.Box(action, 20f, 158f, 880f, 34f);

            var view = frame.gameObject.AddComponent<RewardOptionView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_kind", kind);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_body", body);
            UiBuild.SetReference(view, "_action", action);

            frame.gameObject.SetActive(false);
            return view;
        }
    }
}
