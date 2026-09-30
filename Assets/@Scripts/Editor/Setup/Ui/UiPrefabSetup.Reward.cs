using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildReward(Transform holder)
        {
            RewardScreen screen = Screen<RewardScreen>("RewardScreen", holder, out RectTransform frame);

            UiBuild.Box(UiBuild.LocalizedLabel("RewardTitle", frame, UiKeys.Reward.Title, 44f, UiPalette.Text), 40f, 20f, 900f, 60f);
            UiBuild.Box(UiBuild.LocalizedLabel("RewardHint", frame, UiKeys.Reward.Hint, 22f, UiPalette.TextDim), 40f, 84f, 1840f, 34f);

            RectTransform options = UiBuild.Box(UiBuild.Rect("Options", frame), 40f, 130f, 1840f, 290f);
            UiBuild.Horizontal(options, 20f);
            RewardOptionView optionTemplate = BuildRewardOption(options);

            PartyBoardView board = BuildBoard(frame, 40f, 440f);

            ButtonParts skip = UiBuild.LocalizedButton("Skip", frame, UiKeys.Reward.Skip, UiPalette.ButtonQuiet, 32f);
            UiBuild.Box(skip.Rect, 1560f, 960f, 320f, 84f);

            UiBuild.SetReference(screen, "_optionTemplate", optionTemplate);
            UiBuild.SetReference(screen, "_optionParent", options);
            UiBuild.SetReference(screen, "_skip", skip.Button);
            UiBuild.SetReference(screen, "_board", board);
            return screen;
        }

        /// <summary>One reward card. The whole card is the button.</summary>
        static RewardOptionView BuildRewardOption(Transform parent)
        {
            Image frame = UiBuild.Image("OptionTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(frame, 600f, 290f);
            Button button = UiBuild.MakeButton(frame);
            Transform option = frame.transform;

            TextMeshProUGUI kind = UiBuild.Box(UiBuild.Label("OptionKind", option, 22f, UiPalette.TextDim), 20f, 12f, 300f, 30f);
            TextMeshProUGUI title = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("OptionTitle", option, 32f, UiPalette.Text), 20f, 44f, 560f, 44f));
            TextMeshProUGUI body = UiBuild.Label("OptionBody", option, 22f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(body, 20f, 94f, 560f, 140f);
            TextMeshProUGUI action = UiBuild.Label("OptionAction", option, 26f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(action, 20f, 238f, 560f, 40f);

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
