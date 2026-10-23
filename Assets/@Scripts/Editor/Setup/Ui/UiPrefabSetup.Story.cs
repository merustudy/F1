using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>The shade of a dialogue over the town (Round 101 안 A: 62%).</summary>
        static readonly Color DialogueShade = new Color(6f / 255f, 6f / 255f, 8f / 255f, 0.62f);

        /// <summary>The dialogue box's and the banner's dark (the mockups' 236/255 and 190/255).</summary>
        static readonly Color DialogueBox = new Color(16f / 255f, 15f / 255f, 18f / 255f, 236f / 255f);
        static readonly Color BannerShade = new Color(UiPalette.StoryShade.r, UiPalette.StoryShade.g, UiPalette.StoryShade.b, 190f / 255f);

        /// <summary>
        /// The intro (Slice D stage 55, the Round 98 motion mockup): two pictures (a scene and the one crossing over it; the screen sets their
        /// textures and views), the heartbeat's vignette, the subtitles' band, three line rows (a shadow under each line), the faint hint at the
        /// bottom right, the black over everything, and a transparent catcher for the click that goes on.
        /// </summary>
        static UIScreen BuildIntro(Transform holder)
        {
            IntroScreen screen = Screen<IntroScreen>("IntroScreen", holder, out RectTransform frame);

            RawImage pictureBase = Raw("PictureBase", frame, Color.white);
            RawImage pictureOver = Raw("PictureOver", frame, Color.white);
            RawImage vignette = Raw("Vignette", frame, Color.clear);
            RawImage band = Raw("Band", frame, Color.white);
            UiBuild.Box(band, 0f, 1080f - 345f, 1920f, 345f);

            var rows = new RectTransform[3];
            var groups = new CanvasGroup[3];
            var texts = new TMP_Text[3];
            var shadows = new TMP_Text[3];
            for (int i = 0; i < 3; i++)
            {
                RectTransform row = UiBuild.Box(UiBuild.Rect("Line" + (i + 1), frame), 0f, 800f + i * 63f, 1920f, IntroScreen.LineSize + 9f);
                groups[i] = row.gameObject.AddComponent<CanvasGroup>();
                groups[i].blocksRaycasts = false;
                TextMeshProUGUI shadow = UiBuild.SingleLine(UiBuild.Label("Line" + (i + 1) + "Shadow", row, IntroScreen.LineSize, new Color(0f, 0f, 0f, 200f / 255f), TextAlignmentOptions.Top));
                UiBuild.Box(shadow, 3f, 3f, 1920f, IntroScreen.LineSize + 9f);
                TextMeshProUGUI text = UiBuild.SingleLine(UiBuild.Label("Line" + (i + 1) + "Text", row, IntroScreen.LineSize, UiPalette.StoryWhite, TextAlignmentOptions.Top));
                UiBuild.Box(text, 0f, 0f, 1920f, IntroScreen.LineSize + 9f);
                text.richText = true;
                rows[i] = row;
                texts[i] = text;
                shadows[i] = shadow;
            }

            TextMeshProUGUI hint = UiBuild.SingleLine(UiBuild.Label("Hint", frame, 27f, UiPalette.StoryHint, TextAlignmentOptions.TopRight));
            UiBuild.Box(hint, 1920f - 36f - 900f, 1030f, 900f, 40f);

            Image black = UiBuild.Image("Black", frame, Color.black);
            UiBuild.Stretch(black.rectTransform);

            Image catcher = UiBuild.Image("Catcher", frame, Color.clear, raycastTarget: true);
            UiBuild.Stretch(catcher.rectTransform);
            Button catcherButton = UiBuild.MakeButton(catcher);

            UiBuild.SetReference(screen, "_pictureBase", pictureBase);
            UiBuild.SetReference(screen, "_pictureOver", pictureOver);
            UiBuild.SetReference(screen, "_vignette", vignette);
            UiBuild.SetReference(screen, "_band", band);
            UiBuild.SetReferences(screen, "_lineRows", rows);
            UiBuild.SetReferences(screen, "_lineGroups", groups);
            UiBuild.SetReferences(screen, "_lineTexts", texts);
            UiBuild.SetReferences(screen, "_lineShadows", shadows);
            UiBuild.SetReference(screen, "_hint", hint);
            UiBuild.SetReference(screen, "_black", black);
            UiBuild.SetReference(screen, "_catcher", catcherButton);
            return screen;
        }

        static RawImage Raw(string name, Transform parent, Color color)
        {
            RectTransform rect = UiBuild.Rect(name, parent);
            var image = rect.gameObject.AddComponent<RawImage>();
            image.color = color;
            image.raycastTarget = false;
            UiBuild.Stretch(rect);
            return image;
        }

        /// <summary>
        /// A dialogue over the lobby (Round 101 안 A): the shade over everything, the speaker's picture at the left (height 880, its foot under the
        /// screen's bottom; a second image fades the face before out), the box along the bottom with its brass rim, the name plate, the line and the
        /// hint, and a catcher over it all for the click that goes on.
        /// </summary>
        static DialogueView BuildDialogue(Transform frame)
        {
            Image root = UiBuild.Image("Dialogue", frame, Color.clear);
            UiBuild.Stretch(root.rectTransform);
            var group = root.gameObject.AddComponent<CanvasGroup>();
            var view = root.gameObject.AddComponent<DialogueView>();

            Image shade = UiBuild.Image("DialogueShade", root.transform, DialogueShade, raycastTarget: true);
            UiBuild.Stretch(shade.rectTransform);

            const float portraitHeight = 880f;
            const float portraitWidth = portraitHeight * 1024f / 1536f;
            Image portrait = UiBuild.Image("DialoguePortrait", root.transform, Color.white);
            portrait.preserveAspect = true;
            UiBuild.Box(portrait, 40f, 1080f - portraitHeight + 10f, portraitWidth, portraitHeight);
            Image portraitBefore = UiBuild.Image("DialoguePortraitBefore", root.transform, Color.white);
            portraitBefore.preserveAspect = true;
            UiBuild.Box(portraitBefore, 40f, 1080f - portraitHeight + 10f, portraitWidth, portraitHeight);
            portraitBefore.gameObject.SetActive(false);

            Image box = UiBuild.Panel("DialogueBox", root.transform, DialogueBox);
            UiBuild.Box(box, 640f, 820f, 1220f, 220f);
            Rim("DialogueBoxRim", box.transform, 3f, UiPalette.Brass);
            TextMeshProUGUI text = UiBuild.Label("DialogueText", box.transform, 36f, UiPalette.StoryWhite, TextAlignmentOptions.TopLeft);
            UiBuild.Box(text, 50f, 46f, 1140f, 120f);
            TextMeshProUGUI hint = UiBuild.SingleLine(UiBuild.Label("DialogueHint", box.transform, 22f, UiPalette.TextDim, TextAlignmentOptions.Right));
            UiBuild.Box(hint, 220f, 176f, 970f, 30f);

            Image plate = UiBuild.Panel("DialoguePlate", root.transform, new Color(28f / 255f, 24f / 255f, 22f / 255f, 1f));
            UiBuild.Box(plate, 670f, 790f, 200f, 56f);
            Rim("DialoguePlateRim", plate.transform, 3f, UiPalette.Brass);
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Label("DialogueName", plate.transform, 32f, UiPalette.DiabloGold, TextAlignmentOptions.Center));
            UiBuild.Stretch(name.rectTransform, 4f, 4f, 4f, 4f);

            Image catcher = UiBuild.Image("DialogueCatcher", root.transform, Color.clear, raycastTarget: true);
            UiBuild.Stretch(catcher.rectTransform);
            Button catcherButton = UiBuild.MakeButton(catcher);

            UiBuild.SetReference(view, "_shade", shade);
            UiBuild.SetReference(view, "_portrait", portrait);
            UiBuild.SetReference(view, "_portraitBefore", portraitBefore);
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_text", text);
            UiBuild.SetReference(view, "_hint", hint);
            UiBuild.SetReference(view, "_catcher", catcherButton);
            UiBuild.SetReference(view, "_group", group);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>
        /// The intro's last scene over the lobby (the Round 98 mockup's lobby frame): a black cover it comes up out of, a band over the header with
        /// the countdown (gold, 45), a box at the bottom with the guide line (45 px of white on the dark; the screen fits its width), and a catcher.
        /// </summary>
        static StoryBannerView BuildStoryBanner(Transform frame)
        {
            Image root = UiBuild.Image("StoryBanner", frame, Color.clear);
            UiBuild.Stretch(root.rectTransform);
            var view = root.gameObject.AddComponent<StoryBannerView>();

            // Over the lobby the band covers the header (100): opaque, or the day's words would show through the countdown (Round 98 had no header).
            RectTransform countdownRoot = UiBuild.Box(UiBuild.Rect("BannerCountdown", root.transform), 0f, 0f, 1920f, 100f);
            var countdownGroup = countdownRoot.gameObject.AddComponent<CanvasGroup>();
            Image band = UiBuild.Image("BannerBand", countdownRoot, UiPalette.StoryShade);
            UiBuild.Stretch(band.rectTransform);
            TextMeshProUGUI countdown = UiBuild.SingleLine(UiBuild.Label("BannerCountdownText", countdownRoot, 45f, UiPalette.StoryGold, TextAlignmentOptions.Center));
            UiBuild.Box(countdown, 0f, 20f, 1920f, 60f);

            RectTransform guideRoot = UiBuild.Box(UiBuild.Rect("BannerGuide", root.transform), 0f, 0f, 1920f, 1080f);
            var guideGroup = guideRoot.gameObject.AddComponent<CanvasGroup>();
            Image guideBox = UiBuild.Image("BannerGuideBox", guideRoot, BannerShade);
            UiBuild.Place(guideBox.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(1080f - 138f)), new Vector2(760f, 78f));
            TextMeshProUGUI guide = UiBuild.SingleLine(UiBuild.Label("BannerGuideText", guideBox.transform, 39f, UiPalette.StoryWhite, TextAlignmentOptions.Center));
            UiBuild.Stretch(guide.rectTransform);

            Image cover = UiBuild.Image("BannerCover", root.transform, Color.black);
            UiBuild.Stretch(cover.rectTransform);

            Image catcher = UiBuild.Image("BannerCatcher", root.transform, Color.clear, raycastTarget: true);
            UiBuild.Stretch(catcher.rectTransform);
            Button catcherButton = UiBuild.MakeButton(catcher);

            UiBuild.SetReference(view, "_cover", cover);
            UiBuild.SetReference(view, "_countdownGroup", countdownGroup);
            UiBuild.SetReference(view, "_countdown", countdown);
            UiBuild.SetReference(view, "_guideGroup", guideGroup);
            UiBuild.SetReference(view, "_guideBox", guideBox.rectTransform);
            UiBuild.SetReference(view, "_guide", guide);
            UiBuild.SetReference(view, "_catcher", catcherButton);
            root.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A speaker standing in the town (Round 101 안 B): the figure (a button; the screen places it), and a stone plate with the name under its foot.</summary>
        static TownNpcView BuildTownNpc(Transform frame)
        {
            Image figure = UiBuild.Image("TownNpc", frame, Color.white, raycastTarget: true);
            figure.preserveAspect = true;
            UiBuild.Box(figure, TownNpcView.FootX - 105f, TownNpcView.FootY - TownNpcView.Height, 210f, TownNpcView.Height);
            Button button = UiBuild.MakeButton(figure);
            var view = figure.gameObject.AddComponent<TownNpcView>();

            Image plate = UiBuild.Panel("TownNpcPlate", figure.transform, UiPalette.StonePlate);
            UiBuild.Place(plate.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(140f, 42f));
            Rim("TownNpcPlateRim", plate.transform, 2f, UiPalette.Brass);
            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Label("TownNpcName", plate.transform, 26f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(name.rectTransform);

            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_figure", figure);
            UiBuild.SetReference(view, "_name", name);
            figure.gameObject.SetActive(false);
            return view;
        }
    }
}
