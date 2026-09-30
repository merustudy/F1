using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildTitle(Transform holder)
        {
            TitleScreen screen = Screen<TitleScreen>("TitleScreen", holder, out RectTransform frame);

            TextMeshProUGUI gameName = UiBuild.LocalizedLabel("GameName", frame, UiKeys.Title.GameName, 140f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(gameName, 0f, 180f, 1920f, 180f);

            // A column: when Continue is hidden the buttons below move up.
            const float width = 400f;
            const float height = 84f;
            RectTransform buttons = UiBuild.Box(UiBuild.Rect("Buttons", frame), 760f, 460f, width, 400f);
            UiBuild.Vertical(buttons, 20f);

            ButtonParts newRun = UiBuild.LocalizedButton("NewRun", buttons, UiKeys.Title.NewRun, UiPalette.Button, 36f);
            UiBuild.Size(newRun.Rect, width, height);

            ButtonParts resume = UiBuild.LocalizedButton("Continue", buttons, UiKeys.Title.Continue, UiPalette.Button, 36f);
            UiBuild.Size(resume.Rect, width, height);

            ButtonParts language = UiBuild.Button("Language", buttons, UiPalette.ButtonQuiet, 32f);
            UiBuild.Size(language.Rect, width, height);

            ButtonParts quit = UiBuild.LocalizedButton("Quit", buttons, UiKeys.Title.Quit, UiPalette.ButtonQuiet, 32f);
            UiBuild.Size(quit.Rect, width, height);

            TextMeshProUGUI notice = UiBuild.Label("Notice", frame, 26f, UiPalette.Burn, TextAlignmentOptions.Center);
            UiBuild.Box(notice, 260f, 900f, 1400f, 80f);

            // Asked before a new run replaces the one in progress.
            Image overlay = UiBuild.Image("ConfirmPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(overlay.rectTransform);
            Image panel = UiBuild.Panel("ConfirmBox", overlay.transform, UiPalette.Panel);
            UiBuild.Box(panel, 460f, 360f, 1000f, 340f);
            TextMeshProUGUI warning = UiBuild.LocalizedLabel("Warning", panel.transform, UiKeys.Title.OverwriteWarning, 34f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(warning, 40f, 40f, 920f, 120f);
            ButtonParts yes = UiBuild.LocalizedButton("Yes", panel.transform, UiKeys.Common.Confirm, UiPalette.Danger, 32f);
            UiBuild.Box(yes.Rect, 180f, 210f, 300f, 84f);
            ButtonParts no = UiBuild.LocalizedButton("No", panel.transform, UiKeys.Common.Cancel, UiPalette.ButtonQuiet, 32f);
            UiBuild.Box(no.Rect, 520f, 210f, 300f, 84f);

            UiBuild.SetReference(screen, "_newRun", newRun.Button);
            UiBuild.SetReference(screen, "_continue", resume.Button);
            UiBuild.SetReference(screen, "_language", language.Button);
            UiBuild.SetReference(screen, "_languageLabel", language.Label);
            UiBuild.SetReference(screen, "_quit", quit.Button);
            UiBuild.SetReference(screen, "_notice", notice);
            UiBuild.SetReference(screen, "_confirmPanel", overlay.gameObject);
            UiBuild.SetReference(screen, "_confirmYes", yes.Button);
            UiBuild.SetReference(screen, "_confirmNo", no.Button);
            return screen;
        }
    }
}
