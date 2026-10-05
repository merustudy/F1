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
        /// (UiArt): frames stretched as nine-slices, tinted buttons, bars in a frame, the marks under
        /// a unit and the head of its board. A sprite is drawn at twice its size on screen; a thin
        /// piece shrinks the frame's border further (borderScale below 1).
        ///
        /// The marks under a unit's feet (2026-10-05 round 29, Slay the Spire's way): no plate behind
        /// them, the stage shows through. From the top of their box, PlateGap under the feet: the HP bar
        /// with its numbers on it (MarksBarInset in from each side of the column: 160 in a column of
        /// 180), then the state line. The badge at the bar's left end reaches out of the box.
        /// </summary>
        const float MarksHeight = 46f;
        const float MarksBarTop = 2f;
        const float MarksBarHeight = 22f;
        const float MarksBarInset = 10f;

        /// <summary>How far the rim of a state shows around the bar.</summary>
        const float MarksRim = 2f;

        /// <summary>The badge at the bar's left end (a shield and its number, the skull at death's door): its size, and how far it reaches out of the bar's end and above its top.</summary>
        const float MarksBadgeSize = 36f;
        const float MarksBadgeOut = 20f;
        const float MarksBadgeRise = 7f;

        /// <summary>The state line under the bar, starting a little in from the bar's left end.</summary>
        const float MarksStateTop = 28f;
        const float MarksStateHeight = 18f;
        const float MarksStateIndent = 6f;

        /// <summary>
        /// The head of a unit's board (2026-10-05 round 29): the strip with the row on a gold stud and the
        /// name, at the top of the board's column, as wide as an item cell's ink rim (which shows 3 past
        /// the cell on either side). The cells start under it.
        /// </summary>
        const float BoardHeadHeight = 22f;
        const float BoardHeadOut = 3f;
        const float BoardHeadStud = 18f;
        const float BoardHeadStudLeft = 4f;
        const float BoardHeadNameGap = 6f;
        const float BoardCellsTop = 24f;

        /// <summary>The name tag is drawn for a strip 30 high: a thinner one shrinks its border in step (UiArt.NameTag).</summary>
        const float NameTagArtHeight = 30f;

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

        /// <summary>The space between a figure and its marks, and between the marks and what stands under them.</summary>
        const float PlateGap = 6f;

        /// <summary>A unit's name shrinks down to this size when it does not fit the head of its board.</summary>
        const float UnitNameMinSize = 10f;

        /// <summary>How much of an empty item cell shows: it is there, but nothing is in it.</summary>
        const float EmptyCellAlpha = 0.8f;

        /// <summary>
        /// How much of its color a board cell keeps while it takes no click: dimmer than a live cell,
        /// but opaque, so that bone stays bone over the iron panel instead of sinking into it.
        /// </summary>
        const float QuietCellTint = 0.8f;

        /// <summary>
        /// The cooldown's light (2026-10-04 round 18): how strongly the candle's gold lies on the charged
        /// part of a cell (under the icon), and the glow and the line at the front of the charge. How dark
        /// the part not charged yet is belongs to the view, which brings it back after a flash (BattleItemView).
        /// </summary>
        const float ChargeTint = 0.22f;
        const float FrontGlowAlpha = 0.45f;
        const float FrontLineAlpha = 0.9f;

        /// <summary>The parts of a unit's marks that a view fills in. The state line is left to the caller.</summary>
        struct MarksParts
        {
            public RectTransform Marks;
            public Image Rim;
            public UiBar HpBar;
            public Image HpTrack;
            public TextMeshProUGUI Hp;
            public Image Badge;
            public TextMeshProUGUI BadgeNumber;
        }

        /// <summary>A colour of the palette at this alpha.</summary>
        static Color Tinted(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
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
        /// A badge with a number on it: a brass disc with a dark rim. The grade of an item on its cell
        /// on the party side. The caller places the rim. Takes no clicks.
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
        /// The marks under a unit's feet: its HP as a bar with the numbers on it, the badge at the bar's
        /// left end (the view shows it with a shield or at death's door), and behind the bar the rim the
        /// view colours with the unit's state. They stretch across the column; the caller places them and
        /// adds the state line under the bar. The words are outlined: they stand on the stage. Every
        /// object is named after the prefix, because a prefab must not repeat a name.
        /// </summary>
        static MarksParts BuildMarks(Transform parent, string prefix, bool raycastTarget)
        {
            RectTransform marks = UiBuild.Rect(prefix + "Marks", parent);

            // The rim: a rounded shape a little larger than the bar, behind it, so that its colour shows around the bar.
            Image rim = UiBuild.Image(prefix + "Rim", marks, Color.white);
            rim.sprite = UiBuild.BuiltinSprite("UI/Skin/UISprite.psd");
            rim.type = Image.Type.Sliced;
            rim.pixelsPerUnitMultiplier = 2f;
            UiBuild.Line(rim, MarksBarTop - MarksRim, MarksBarHeight + 2f * MarksRim, MarksBarInset - MarksRim, MarksBarInset - MarksRim);
            rim.enabled = false;

            UiBar hpBar = KitBar(prefix + "HpBar", marks, UiArt.Trough, 0.4f, 4f, UiPalette.Blood, ghost: true);
            UiBuild.Line(hpBar, MarksBarTop, MarksBarHeight, MarksBarInset, MarksBarInset);
            var track = hpBar.GetComponent<Image>();
            track.raycastTarget = raycastTarget;
            TextMeshProUGUI hp = UiBuild.Outlined(UiBuild.SingleLine(UiBuild.Label(prefix + "Hp", marks, 15f, UiPalette.Text, TextAlignmentOptions.Center)));
            UiBuild.Line(hp, MarksBarTop, MarksBarHeight, MarksBarInset, MarksBarInset);

            // The badge over the bar's left end, reaching out of it; its number in the middle.
            Image badge = KitIcon(prefix + "Badge", marks, UiArt.Shield);
            UiBuild.Box(badge, MarksBarInset - MarksBadgeOut, MarksBarTop - MarksBadgeRise, MarksBadgeSize, MarksBadgeSize);
            TextMeshProUGUI number = UiBuild.Outlined(UiBuild.SingleLine(UiBuild.Label(prefix + "BadgeNumber", badge.transform, 15f, UiPalette.Text, TextAlignmentOptions.Center)));
            UiBuild.Stretch(number.rectTransform);
            badge.gameObject.SetActive(false);

            return new MarksParts { Marks = marks, Rim = rim, HpBar = hpBar, HpTrack = track, Hp = hp, Badge = badge, BadgeNumber = number };
        }

        /// <summary>Where the state line of a unit's marks is: under the HP bar, from a little in from its left end.</summary>
        static T MarksStateLine<T>(T component)
            where T : Component
        {
            return UiBuild.Line(component, MarksStateTop, MarksStateHeight, MarksBarInset + MarksStateIndent, MarksBarInset);
        }

        /// <summary>
        /// The head of a unit's board, at the top of the board's column: the iron strip, the row written
        /// on the gold stud at its left end, and the name next to it (outlined; the view colours it with
        /// the unit's side). Built after the cells, so that it lies over the top of their bag. The battle
        /// screen and the party side both use it.
        /// </summary>
        static Image BuildBoardHead(Transform board, string prefix, out TextMeshProUGUI row, out TextMeshProUGUI name)
        {
            Image head = KitFrame(prefix + "Head", board, UiArt.NameTag, BoardHeadHeight / NameTagArtHeight);
            UiBuild.Line(head, 0f, BoardHeadHeight, -BoardHeadOut, -BoardHeadOut);

            Image stud = KitIcon(prefix + "Stud", head.transform, UiArt.GoldStud);
            UiBuild.Box(stud, BoardHeadStudLeft, (BoardHeadHeight - BoardHeadStud) / 2f, BoardHeadStud, BoardHeadStud);
            row = UiBuild.SingleLine(UiBuild.Label(prefix + "Row", stud.transform, 11f, UiPalette.Ink, TextAlignmentOptions.Center));
            UiBuild.Stretch(row.rectTransform);

            name = UiBuild.Outlined(UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label(prefix + "Name", head.transform, 14f, UiPalette.Text)), UnitNameMinSize));
            UiBuild.Line(name, 0f, BoardHeadHeight, BoardHeadStudLeft + BoardHeadStud + BoardHeadNameGap, BoardHeadNameGap);
            return head;
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
