using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The pieces the expedition screens are built from, drawn with the art of the interface
        /// (UiArt): frames stretched as nine-slices, tinted buttons, bars in a frame, and the plate
        /// under a unit. A sprite is drawn at twice its size on screen; a thin piece shrinks the
        /// frame's border further (borderScale below 1).
        /// </summary>
        const float PlateHeight = 92f;

        /// <summary>
        /// The margin between an item cell and its icon. The icon is scaled into the place inside
        /// it, in proportion. The icons were drawn for cells of this width stacked like these
        /// (Docs/Architecture/13_ART_PIPELINE.md "후처리 (`cell`)"), so they fill the place.
        /// </summary>
        const float ItemIconMarginX = 8f;
        const float ItemIconMarginY = 5f;

        /// <summary>How far the inventory panel behind a board reaches out past its cells, sideways (the panels of two columns, FieldLayout.ColumnGap apart, must not touch) and up and down.</summary>
        const float BoardBagPadX = 4f;
        const float BoardBagPadY = 10f;

        /// <summary>The gloom of the Diablo kit: a soft darkening towards the edges of the whole screen.</summary>
        const float ScreenVignetteAlpha = 0.35f;

        /// <summary>The grade badge of an item on the party side: its size, and its distance from the cell's bottom-left corner.</summary>
        const float GradeBadgeSize = 24f;
        const float GradeBadgeInset = 5f;

        /// <summary>The space between a figure and its plate, and between the plate and what stands under it.</summary>
        const float PlateGap = 6f;

        /// <summary>A unit's name shrinks down to this size when it does not fit its plate.</summary>
        const float UnitNameMinSize = 14f;

        /// <summary>How much of an empty item cell shows: it is there, but nothing is in it.</summary>
        const float EmptyCellAlpha = 0.8f;

        /// <summary>
        /// How much of its color a board cell keeps while it takes no click: dimmer than a live cell,
        /// but opaque, so that bone stays bone over the iron panel instead of sinking into it.
        /// </summary>
        const float QuietCellTint = 0.8f;

        /// <summary>How strongly the charge of an item covers its cell.</summary>
        const float CooldownAlpha = 0.84f;

        /// <summary>The parts of a plate that a view fills in. The state line is left to the caller.</summary>
        struct PlateParts
        {
            public Image Plate;
            public TextMeshProUGUI Row;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Hp;
            public UiBar HpBar;
        }

        /// <summary>A frame of the kit, stretched to wherever the caller puts it.</summary>
        static Image KitFrame(string name, Transform parent, string art, float borderScale = 1f, bool raycastTarget = false)
        {
            Image image = UiBuild.Image(name, parent, Color.white, raycastTarget);
            Skin(image, art, borderScale);
            return image;
        }

        /// <summary>
        /// Draws an image with a frame of the kit. The image's color tints the frame. A frame
        /// whose edges carry rivets or stitches is tiled, so that they keep their shape however
        /// far the frame reaches; the others are stretched.
        /// </summary>
        static void Skin(Image image, string art, float borderScale = 1f)
        {
            image.sprite = UiArt.Load(art);
            image.type = UiArt.IsTiled(art) ? Image.Type.Tiled : Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 1f / borderScale;
        }

        /// <summary>
        /// The bag behind a board: a leather slab stretched over the cells and a little past them,
        /// so it is as long as the board. It is the first child of the cells' rect and the layout
        /// skips it, so the cells are laid out over it.
        /// </summary>
        static Image BuildBoardBag(RectTransform cells, string name)
        {
            Image bag = KitFrame(name, cells, UiArt.Bag);
            bag.transform.SetAsFirstSibling();
            UiBuild.Stretch(bag.rectTransform, -BoardBagPadX, -BoardBagPadY, -BoardBagPadX, -BoardBagPadY);
            bag.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return bag;
        }

        /// <summary>An icon of the kit, in proportion inside wherever the caller puts it.</summary>
        static Image KitIcon(string name, Transform parent, string art)
        {
            Image image = UiBuild.Image(name, parent, Color.white);
            image.sprite = UiArt.Load(art);
            image.preserveAspect = true;
            return image;
        }

        /// <summary>A button of the kit whose label code fills in at runtime. The white button art takes the color.</summary>
        static ButtonParts KitButton(string name, Transform parent, Color color, float fontSize)
        {
            ButtonParts parts = UiBuild.Button(name, parent, color, fontSize);
            Skin(parts.Frame, UiArt.Button);
            return parts;
        }

        /// <summary>A button of the kit with a fixed, localized label.</summary>
        static ButtonParts KitLocalizedButton(string name, Transform parent, string key, Color color, float fontSize)
        {
            ButtonParts parts = UiBuild.LocalizedButton(name, parent, key, color, fontSize);
            Skin(parts.Frame, UiArt.Button);
            return parts;
        }

        /// <summary>
        /// A bar in a frame of the kit: the fill grows from the left inside the frame's rim.
        /// The caller places it. With a ghost (<see cref="UiBarGhost"/>), a trailing fill behind the
        /// fill lingers where the value just was (the HP bars: a hit reads as a red tail that is eaten away).
        /// </summary>
        static UiBar KitBar(string name, Transform parent, string art, float borderScale, float inset, Color fillColor, bool ghost = false)
        {
            Image track = KitFrame(name, parent, art, borderScale);
            RectTransform area = UiBuild.Rect(name + "Area", track.transform);
            UiBuild.Stretch(area, inset, inset, inset, inset);
            Image ghostFill = null;
            if (ghost)
            {
                ghostFill = UiBuild.Image(name + "Ghost", area, UiPalette.BloodLight);
                UiBuild.Stretch(ghostFill.rectTransform);
            }

            Image fill = UiBuild.Image(name + "Fill", area, fillColor);
            UiBuild.Stretch(fill.rectTransform);

            var bar = track.gameObject.AddComponent<UiBar>();
            UiBuild.SetReference(bar, "_fill", fill.rectTransform);
            UiBuild.SetReference(bar, "_fillImage", fill);
            if (ghost)
            {
                var trail = track.gameObject.AddComponent<UiBarGhost>();
                UiBuild.SetReference(trail, "_bar", bar);
                UiBuild.SetReference(trail, "_ghost", ghostFill.rectTransform);
            }

            return bar;
        }

        /// <summary>
        /// A badge with a number on it: a brass disc with a dark rim. The row of a unit on its plate,
        /// the grade of an item on its cell. The caller places the rim. Takes no clicks.
        /// </summary>
        static TextMeshProUGUI KitBadge(Transform parent, string name, string textName, float fontSize, out Image rim)
        {
            rim = UiBuild.Image(name, parent, UiPalette.Ink);
            rim.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            Image face = UiBuild.Image(name + "Face", rim.transform, UiPalette.Brass);
            face.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Stretch(face.rectTransform, 3f, 3f, 3f, 3f);
            TextMeshProUGUI number = UiBuild.Label(textName, rim.transform, fontSize, UiPalette.Ink, TextAlignmentOptions.Center);
            UiBuild.Stretch(number.rectTransform);
            return number;
        }

        /// <summary>
        /// The plate under a unit: the badge with its row, its name, and its HP as a bar with the
        /// numbers on it. The plate stretches across its column; the caller places it and adds the
        /// state line under the bar. Every object is named after the prefix, because a prefab
        /// must not repeat a name.
        /// </summary>
        static PlateParts BuildPlate(Transform parent, string prefix, string art, bool raycastTarget)
        {
            Image plate = KitFrame(prefix + "Plate", parent, art, raycastTarget: raycastTarget);
            Transform t = plate.transform;

            // The row badge, at the top-left corner.
            TextMeshProUGUI row = KitBadge(t, prefix + "Badge", prefix + "Row", 17f, out Image badge);
            UiBuild.Box(badge, 11f, 10f, 26f, 26f);

            TextMeshProUGUI name = UiBuild.ShrinkToFit(
                UiBuild.SingleLine(UiBuild.Line(UiBuild.Label(prefix + "Name", t, 20f, UiPalette.Text), 10f, 26f, 45f, 11f)),
                UnitNameMinSize);

            UiBar hpBar = KitBar(prefix + "HpBar", t, UiArt.Trough, 0.4f, 4f, UiPalette.Blood, ghost: true);
            UiBuild.Line(hpBar, 39f, 23f, 11f, 11f);
            TextMeshProUGUI hp = UiBuild.SingleLine(UiBuild.Label(prefix + "Hp", t, 15f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Line(hp, 39f, 23f, 11f, 11f);

            return new PlateParts { Plate = plate, Row = row, Name = name, Hp = hp, HpBar = hpBar };
        }

        /// <summary>Where the state line of a plate is: under the HP bar.</summary>
        static T PlateStateLine<T>(T component)
            where T : Component
        {
            return UiBuild.Line(component, 65f, 18f, 13f, 11f);
        }

        /// <summary>
        /// The strip of potion slots under the header, on a panel so that the slots show against
        /// whatever is behind them. The battle screen has it on the left; the party side, whose
        /// party stands higher, above the right half (2026-10-03 mockup V).
        /// </summary>
        static PotionSlotView BuildPotionStrip(Transform frame, float x, out RectTransform potions)
        {
            Image panel = KitFrame("PotionsPanel", frame, UiArt.Belt, 0.75f);
            UiBuild.Box(panel, x, 92f, PotionStripWidth, 84f);
            potions = UiBuild.Box(UiBuild.Rect("Potions", frame), x + 12f, 102f, PotionStripWidth - 24f, PotionSlotSize);
            UiBuild.Horizontal(potions, PotionSlotGap);
            return BuildPotionSlot(potions);
        }

        /// <summary>A potion slot is a square iron pocket that shows only the bottle (2026-10-04); the belt holds three with a gap between them.</summary>
        const float PotionSlotSize = 64f;
        const float PotionSlotGap = 8f;
        const float PotionStripWidth = 24f + 3f * PotionSlotSize + 2f * PotionSlotGap;

        /// <summary>One potion slot: the bottle alone, in a square pocket of the kit. Its words are shown elsewhere while it is chosen (the battle's hint plate, the party side's detail line).</summary>
        static PotionSlotView BuildPotionSlot(Transform parent)
        {
            Image frame = KitFrame("PotionTemplate", parent, UiArt.PotionSlot);
            UiBuild.Size(frame, PotionSlotSize, PotionSlotSize);
            Button button = UiBuild.MakeButton(frame);

            Image icon = UiBuild.Image("PotionIcon", frame.transform, Color.white);
            icon.preserveAspect = true;
            UiBuild.Stretch(icon.rectTransform, 7f, 7f, 7f, 7f);
            icon.enabled = false;

            var view = frame.gameObject.AddComponent<PotionSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_plain", UiArt.Load(UiArt.PotionSlot));
            UiBuild.SetReference(view, "_selected", UiArt.Load(UiArt.PotionSlotSelected));

            frame.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The gloom of the Diablo kit over a whole screen: a soft darkening towards the edges. Takes no click; the caller builds it under what must stay bright (the result, a popup).</summary>
        static void BuildScreenVignette(Transform frame)
        {
            Image vignette = UiBuild.Image("ScreenVignette", frame, new Color(0f, 0f, 0f, ScreenVignetteAlpha));
            vignette.sprite = UiArt.Load(UiArt.Vignette);
            UiBuild.Stretch(vignette.rectTransform);
        }
    }
}
