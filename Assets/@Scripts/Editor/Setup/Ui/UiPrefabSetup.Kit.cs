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
        /// them, the stage shows through. From the top of their box, MarksGap under the feet: the HP bar
        /// with its numbers on it (MarksBarInset in from each side of the column: 160 in a column of
        /// 180), the row of fatigue pips under it (2026-10-06 round 36, C), then the state line. The badge
        /// at the bar's left end reaches out of the box.
        /// </summary>
        const float MarksHeight = 50f;
        const float MarksBarTop = 2f;
        const float MarksBarHeight = 22f;
        const float MarksBarInset = 10f;

        /// <summary>The fatigue pips under the bar, as wide as the bar: FatiguePips.Count of them with a gap between two (round 36, C); violet under, red over (Round 95).</summary>
        const float MarksPipsTop = 26f;
        const float MarksPipsHeight = 4f;
        const float MarksPipGap = 2f;

        /// <summary>How far the rim of a state shows around the bar.</summary>
        const float MarksRim = 2f;

        /// <summary>The badge at the bar's left end (a shield and its number, the skull at death's door): its size, and how far it reaches out of the bar's end and above its top.</summary>
        const float MarksBadgeSize = 36f;
        const float MarksBadgeOut = 20f;
        const float MarksBadgeRise = 7f;

        /// <summary>The state line under the pips, starting a little in from the bar's left end.</summary>
        const float MarksStateTop = 32f;
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

        /// <summary>The gloom of the Diablo kit: a soft darkening towards the edges of the whole screen.</summary>
        const float ScreenVignetteAlpha = 0.35f;

        /// <summary>
        /// The fatigue tag of the party side (2026-10-06 round 32, B1): on a cell, at its top-right corner as far in as the tier
        /// tag is from the bottom-left one, saying "+1"; on the head of a board, at its right end as far in, saying the total.
        /// The head's tag is as wide as its words and the pad on either side (the view sizes it), and sits a pixel low, with
        /// the name's glyphs (the strip's art has more rim above).
        /// </summary>
        const float FatigueTagHeight = 20f;
        const float FatigueTagCellWidth = 30f;
        const float FatigueTagInset = 5f;
        const float FatigueTagFontSize = 13f;
        const float FatigueHeadDrop = 1f;

        /// <summary>
        /// The marks of a tier above Common on an item cell (2026-10-07 round 41): the outline around the icon (TierStyle), and the
        /// tier tag at the cell's bottom-left corner, as far in as the fatigue tag is from the top-right one. A cell the chosen item
        /// would merge into shows the tier the merge makes on a veil of this alpha inside the cell's line, in words of this size,
        /// lifted a little off the tag.
        /// </summary>
        const float TierTagInset = 5f;
        const float MergeVeilInset = 3f;
        const float MergeVeilAlpha = 0.6f;

        /// <summary>The space between a figure and its marks, and between the marks and what stands under them.</summary>
        const float MarksGap = 6f;

        /// <summary>A unit's name shrinks down to this size when it does not fit the head of its board.</summary>
        const float UnitNameMinSize = 10f;

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
            public RectTransform PipsRow;
            public UiBar[] Pips;
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

        // ---- The item card (round 42) --------------------------------------------------------------------------------------------

        const float TooltipWidth = 400f;
        const float TooltipPadding = 16f;
        /// <summary>Room at the card's left that round 41's tier stripe took; the lines keep its margin (R12 took the stripe's parts out).</summary>
        const float TooltipStripeRoom = 8f;
        const float TooltipRadius = 4f;
        const float TooltipPlateHeight = 56f;
        const float TooltipNotch = 16f;

        /// <summary>
        /// An item's card (round 42, Docs/Architecture/12_UI.md "툴팁"; Diablo II's tooltip since round 49): a black see-through card
        /// with a grey hairline and the item's lines stacked inside and centred, sized to them (the name in its rarity colour, the effects
        /// in the magic blue); notches (two diamonds clipped to what lies outside the edge) under its bottom edge for the party side and
        /// on its side edges for the battle. Hidden until a click opens it.
        /// </summary>
        const float TooltipSellHeight = 38f;

        static ItemTooltipView BuildItemTooltip(Transform frame, string name)
        {
            RectTransform card = UiBuild.Rect(name, frame);
            UiBuild.Box(card, 0f, 0f, TooltipWidth, 100f);
            VerticalLayoutGroup lines = UiBuild.Vertical(card, 6f);
            lines.padding = new RectOffset((int)(TooltipPadding + TooltipStripeRoom), (int)TooltipPadding, (int)TooltipPadding, (int)TooltipPadding);
            lines.childControlWidth = true;
            lines.childControlHeight = true;
            lines.childForceExpandWidth = true;
            card.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image line = Rounded(name + "Line", card, UiPalette.TooltipLine, TooltipRadius);
            UiBuild.Stretch(OutOfLayout(line.rectTransform));
            Image fill = Rounded(name + "Fill", card, UiPalette.TooltipFill, TooltipRadius - 1f);
            UiBuild.Stretch(OutOfLayout(fill.rectTransform), 1f, 1f, 1f, 1f);

            // The notches (one shows at a time): under the bottom edge for the party side, on a side edge for the battle (mockup 6).
            RectTransform notch = NotchClip(name + "Notch", card, Vector2.zero, new Vector2(0.5f, 1f), new Vector2(TooltipNotch * 2f, TooltipNotch), new Vector2(0.5f, 1f));
            RectTransform notchLeft = NotchClip(name + "NotchLeft", card, new Vector2(0f, 1f), new Vector2(1f, 0.5f), new Vector2(TooltipNotch, TooltipNotch * 2f), new Vector2(1f, 0.5f));
            RectTransform notchRight = NotchClip(name + "NotchRight", card, new Vector2(1f, 1f), new Vector2(0f, 0.5f), new Vector2(TooltipNotch, TooltipNotch * 2f), new Vector2(0f, 0.5f));

            // A unique item's name sits on a plate head (stage 23; round 42's mockup 7), with "유니크 · 단계" under it; the title line is off then.
            Image plate = KitFrame(name + "Plate", card, UiArt.PlateLabel);
            LayoutElement plateElement = plate.gameObject.AddComponent<LayoutElement>();
            plateElement.minHeight = TooltipPlateHeight;
            plateElement.preferredHeight = TooltipPlateHeight;
            TextMeshProUGUI plateTitle = UiBuild.SingleLine(UiBuild.Label(name + "PlateTitle", plate.transform, 24f, UiPalette.Unique, TextAlignmentOptions.Center));
            UiBuild.Stretch(plateTitle.rectTransform, 24f, 4f, 24f, 4f);
            plate.gameObject.SetActive(false);
            TextMeshProUGUI title = UiBuild.Label(name + "Title", card, 24f, UiPalette.Text, TextAlignmentOptions.Center);
            TextMeshProUGUI uniqueLine = UiBuild.Label(name + "Unique", card, 17f, UiPalette.Unique, TextAlignmentOptions.Center);
            uniqueLine.gameObject.SetActive(false);
            Image rule = UiBuild.Image(name + "Rule", card, UiPalette.TooltipLine);
            LayoutElement ruleElement = rule.gameObject.AddComponent<LayoutElement>();
            ruleElement.minHeight = 1f;
            ruleElement.preferredHeight = 1f;
            // 2026-10-20 Round 88 2차 (the user: "피해는 항목 중 가장 위쪽으로 배치"): the effects stand above the facts.
            TextMeshProUGUI effects = UiBuild.Label(name + "Effects", card, 20f, UiPalette.TooltipEffect, TextAlignmentOptions.Center);
            TextMeshProUGUI facts = UiBuild.Label(name + "Facts", card, 19f, UiPalette.Text, TextAlignmentOptions.Center);
            TextMeshProUGUI fatigue = UiBuild.Label(name + "Fatigue", card, 19f, UiPalette.Text, TextAlignmentOptions.Center);
            TextMeshProUGUI merge = UiBuild.Label(name + "Merge", card, 19f, UiPalette.Text, TextAlignmentOptions.Center);

            // Round 74 (2026-10-17, the user's B): a sell button at the card's foot while at a shop (what the shop pays, or that it cannot be
            // sold, off); off everywhere else. The one part of the card that takes the pointer.
            Image sellBack = Rounded(name + "Sell", card, UiPalette.Button, 6f);
            LayoutElement sellElement = sellBack.gameObject.AddComponent<LayoutElement>();
            sellElement.minHeight = TooltipSellHeight;
            sellElement.preferredHeight = TooltipSellHeight;
            Button sell = UiBuild.MakeButton(sellBack);
            TextMeshProUGUI sellLabel = UiBuild.SingleLine(UiBuild.Label(name + "SellLabel", sellBack.transform, 19f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(sellLabel.rectTransform);
            sellBack.gameObject.SetActive(false);

            var view = card.gameObject.AddComponent<ItemTooltipView>();
            UiBuild.SetReference(view, "_sell", sell);
            UiBuild.SetReference(view, "_sellLabel", sellLabel);
            UiBuild.SetReference(view, "_notchBottom", notch);
            UiBuild.SetReference(view, "_notchLeft", notchLeft);
            UiBuild.SetReference(view, "_notchRight", notchRight);
            UiBuild.SetReference(view, "_title", title);
            UiBuild.SetReference(view, "_plate", plate);
            UiBuild.SetReference(view, "_plateTitle", plateTitle);
            UiBuild.SetReference(view, "_uniqueLine", uniqueLine);
            UiBuild.SetReference(view, "_facts", facts);
            UiBuild.SetReference(view, "_effects", effects);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_merge", merge);
            card.gameObject.SetActive(false);
            return view;
        }

        /// <summary>A part of the card the layout leaves where it is (the frame, the notch).</summary>
        static RectTransform OutOfLayout(RectTransform rect)
        {
            rect.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
            return rect;
        }

        /// <summary>
        /// A notch of the card: a clipping rect anchored to a point of the card, holding two diamonds (brass under ink) centred on
        /// the card's edge, so that only the point outside the card shows. The view moves it along the edge and turns it on. Off here.
        /// </summary>
        static RectTransform NotchClip(string name, RectTransform card, Vector2 anchor, Vector2 pivot, Vector2 size, Vector2 diamondAnchor)
        {
            RectTransform clip = OutOfLayout(UiBuild.Rect(name, card));
            clip.gameObject.AddComponent<RectMask2D>();
            clip.anchorMin = anchor;
            clip.anchorMax = anchor;
            clip.pivot = pivot;
            clip.sizeDelta = size;
            clip.anchoredPosition = Vector2.zero;
            Diamond(name + "Line", clip, UiPalette.TooltipLine, TooltipNotch + 2f, diamondAnchor);
            Diamond(name + "Fill", clip, UiPalette.TooltipFill, TooltipNotch, diamondAnchor);
            clip.gameObject.SetActive(false);
            return clip;
        }

        /// <summary>A square turned 45°, centred on a point of its parent.</summary>
        static void Diamond(string name, RectTransform parent, Color color, float size, Vector2 anchor)
        {
            RectTransform rect = UiBuild.Image(name, parent, color).rectTransform;
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = new Vector2(size, size);
            rect.localRotation = Quaternion.Euler(0f, 0f, 45f);
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
            // Every bar has a second fill, left empty here (Round 95: only the fatigue pips use it).
            Image over = UiBuild.Image(name + "Over", area, fillColor);
            UiBar.Fill(over.rectTransform, 0f);

            var bar = track.gameObject.AddComponent<UiBar>();
            UiBuild.SetReference(bar, "_fill", fill.rectTransform);
            UiBuild.SetReference(bar, "_fillImage", fill);
            UiBuild.SetReference(bar, "_overFill", over.rectTransform);
            UiBuild.SetReference(bar, "_overFillImage", over);
            if (ghost)
            {
                var trail = track.gameObject.AddComponent<UiBarGhost>();
                UiBuild.SetReference(trail, "_bar", bar);
                UiBuild.SetReference(trail, "_ghost", ghostFill.rectTransform);
            }

            return bar;
        }

        /// <summary>
        /// The tier tag at a cell's bottom-left corner (round 41): the pill, which the view tints and widens to the tier's
        /// pips, and every pip it could carry, from the left (round 69: diamonds the size of round 41's stars). Off until the view shows a
        /// tier. Takes no clicks.
        /// </summary>
        static Image BuildTierTag(Transform parent, string name, out Image[] pips)
        {
            Image pill = KitFrame(name, parent, UiArt.TierTag);
            UiBuild.Place(pill.rectTransform, Vector2.zero, Vector2.zero, new Vector2(TierTagInset, TierTagInset), new Vector2(TierStyle.TagWidth(TierStyle.MostPips), TierStyle.TagHeight));
            pips = new Image[TierStyle.MostPips];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = KitIcon(name + "Pip" + (i + 1), pill.transform, UiArt.TierPip);
                // Round 49: the pill is see-through in Diablo's look, so each pip has a dark edge to stand on any ground.
                var edge = pips[i].gameObject.AddComponent<Outline>();
                edge.effectColor = new Color(0f, 0f, 0f, 0.9f);
                edge.effectDistance = new Vector2(1f, -1f);
                UiBuild.Place(pips[i].rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(TierStyle.TagPad + i * TierStyle.PipPitch, 0f), new Vector2(TierStyle.PipSize, TierStyle.PipSize));
            }

            pill.gameObject.SetActive(false);
            return pill;
        }

        /// <summary>
        /// The marks under a unit's feet: its HP as a bar with the numbers on it, the badge at the bar's
        /// left end (the view shows it with a shield or at death's door), behind the bar the rim the
        /// view colours with the unit's state, and under the bar the row of fatigue pips (round 36, C; the
        /// view fills them, and hides the row for an enemy). They stretch across the column; the caller
        /// places them and adds the state line under the pips. The words are outlined: they stand on the
        /// stage. Every object is named after the prefix, because a prefab must not repeat a name.
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

            // Under the bar, the fatigue as pips (round 36, C): a line as wide as the bar, so that it follows the column's width,
            // with the pips laid along it by shares and the gap taken off between two of them.
            RectTransform pipsRow = UiBuild.Line(UiBuild.Rect(prefix + "Pips", marks), MarksPipsTop, MarksPipsHeight, MarksBarInset, MarksBarInset);
            var pips = new UiBar[FatiguePips.Count];
            for (int i = 0; i < pips.Length; i++)
            {
                pips[i] = UiBuild.PipBar(prefix + "Pip" + i, pipsRow);
                var pip = (RectTransform)pips[i].transform;
                pip.anchorMin = new Vector2((float)i / pips.Length, 0f);
                pip.anchorMax = new Vector2((float)(i + 1) / pips.Length, 1f);
                pip.offsetMin = new Vector2(i == 0 ? 0f : MarksPipGap / 2f, 0f);
                pip.offsetMax = new Vector2(i == pips.Length - 1 ? 0f : -MarksPipGap / 2f, 0f);
            }

            return new MarksParts { Marks = marks, Rim = rim, HpBar = hpBar, HpTrack = track, Hp = hp, Badge = badge, BadgeNumber = number, PipsRow = pipsRow, Pips = pips };
        }

        /// <summary>Where the state line of a unit's marks is: under the fatigue pips, from a little in from the bar's left end.</summary>
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
        /// whatever is behind them. The battle screen and the party side have it at the same place, top left (round 46).
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

            // Stage 46: a potion without a bottle yet shows its name in the pocket (the item piece's rule for a missing picture).
            TextMeshProUGUI label = UiBuild.Outlined(UiBuild.Label("PotionName", frame.transform, 13f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Stretch(label.rectTransform, 4f, 4f, 4f, 4f);
            label.raycastTarget = false;
            label.enabled = false;

            var view = frame.gameObject.AddComponent<PotionSlotView>();
            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_frame", frame);
            UiBuild.SetReference(view, "_icon", icon);
            UiBuild.SetReference(view, "_label", label);
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
