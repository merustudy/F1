using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The overlay shown while the last change could not be saved. It lies over whatever screen
        /// is shown and takes every click, so only "retry" and "quit" remain.
        /// </summary>
        static SaveErrorOverlay BuildSaveErrorOverlay(Transform holder)
        {
            RectTransform root = UiBuild.Rect("SaveErrorOverlay", holder);
            UiBuild.Stretch(root);

            Image blocker = UiBuild.Image("Blocker", root, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(blocker.rectTransform);

            Image box = UiBuild.Panel("Box", blocker.transform, UiPalette.Panel);
            UiBuild.Place(box.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100f, 420f));

            TextMeshProUGUI message = UiBuild.LocalizedLabel("Message", box.transform, UiKeys.Save.Failed, 30f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(message, 50f, 40f, 1000f, 200f);

            ButtonParts retry = UiBuild.LocalizedButton("Retry", box.transform, UiKeys.Save.Retry, UiPalette.Button, 34f);
            UiBuild.Box(retry.Rect, 190f, 290f, 340f, 84f);
            ButtonParts quit = UiBuild.LocalizedButton("Quit", box.transform, UiKeys.Title.Quit, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(quit.Rect, 570f, 290f, 340f, 84f);

            var overlay = root.gameObject.AddComponent<SaveErrorOverlay>();
            UiBuild.SetReference(overlay, "_panel", blocker.gameObject);
            UiBuild.SetReference(overlay, "_retry", retry.Button);
            UiBuild.SetReference(overlay, "_quit", quit.Button);

            // Hidden until a save fails.
            blocker.gameObject.SetActive(false);
            return overlay;
        }
    }
}
