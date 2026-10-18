using F1.Data;
using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>The wash over a building under the pointer: a warm light, and a brass rim round it (the mock's 28/255 fill and 3 px line).</summary>
        static readonly Color BuildingWash = new Color(1f, 220f / 255f, 150f / 255f, 0.11f);
        const float BuildingRim = 3f;

        /// <summary>The shade over the town while a window is open: the mock's 150/255 over the town, the header left clear.</summary>
        static readonly Color TownShade = new Color(0f, 0f, 0f, 0.67f);

        /// <summary>
        /// The lobby as the town (Round 71, the three-quarter view): the painting under everything, a building per quarter (its hotspot a
        /// button, its wash, its plate), the header, the bubble, the roster strip, and over the town the shade and the windows of the quarters
        /// and of the run's end (Round 72). The painting's sprite is loaded at runtime (ArtAddress.LobbyTown).
        /// </summary>
        static UIScreen BuildLobby(Transform holder)
        {
            LobbyScreen screen = Screen<LobbyScreen>("LobbyScreen", holder, out RectTransform frame);

            // The town first, so that everything else is drawn over it: 1920x1280 from the top, the strip over its lower part.
            Image town = UiBuild.Image("Town", frame, Color.white);
            town.preserveAspect = false;
            UiBuild.Box(town, 0f, 0f, LobbyScreen.TownWidth, LobbyScreen.TownHeight);

            // The buildings (Round 78, stage 30): pictures of their own that the screen stands on the empty plots of the far town at their level's
            // size (LobbyScreen.Plots; the gate's one picture at LobbyScreen.GatePlot), the farther ones first so that the nearer ones draw over them: the inn at the back, the gate, the barracks, the stagecoach.
            BuildingView inn = BuildBuilding(frame, "Inn");
            BuildingView gate = BuildBuilding(frame, "Gate");
            BuildingView barracks = BuildBuilding(frame, "Barracks");
            BuildingView coach = BuildBuilding(frame, "Coach");

            // Header: the title, the day (past the last day: "— 최종 결전", the screen sets it), the gold, the way to the title.
            Image header = UiBuild.Panel("Header", frame, UiPalette.Background);
            UiBuild.Box(header, 0f, 0f, 1920f, 100f);
            UiBuild.Box(UiBuild.LocalizedLabel("HeaderTitle", header.transform, UiKeys.Lobby.Title, 44f, UiPalette.Text), 40f, 20f, 500f, 60f);
            TextMeshProUGUI day = UiBuild.Label("Day", header.transform, 40f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(day, 540f, 22f, 840f, 56f);
            TextMeshProUGUI gold = UiBuild.Label("Gold", header.transform, 32f, UiPalette.DiabloGold, TextAlignmentOptions.Right);
            UiBuild.Box(gold, 1340f, 26f, 280f, 48f);
            ButtonParts toTitle = UiBuild.LocalizedButton("ToTitle", header.transform, UiKeys.Lobby.ToTitle, UiPalette.ButtonQuiet, 28f);
            UiBuild.Box(toTitle.Rect, 1640f, 22f, 240f, 56f);

            // The bubble over a building under the pointer: its name and what it holds now. The screen places and shows it.
            Image bubble = UiBuild.Panel("Bubble", frame, UiPalette.StonePlate);
            UiBuild.Box(bubble, 0f, 0f, 460f, 80f);
            AddLine(bubble, UiPalette.Brass, 2f);
            TextMeshProUGUI bubbleName = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("BubbleName", bubble.transform, 24f, UiPalette.Text, TextAlignmentOptions.Center)), 0f, 8f, 460f, 32f);
            TextMeshProUGUI bubbleDetail = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("BubbleDetail", bubble.transform, 20f, UiPalette.TextDim, TextAlignmentOptions.Center)), 0f, 42f, 460f, 30f);
            bubble.gameObject.SetActive(false);

            // The roster strip (안 1): a chip per mercenary, the roster's count and the deaths, and the way to the expedition screen.
            Image strip = UiBuild.Panel("Strip", frame, UiPalette.Panel);
            UiBuild.Box(strip, 0f, 960f, 1920f, 120f);
            UiBuild.Box(UiBuild.Image("StripLine", strip.transform, UiPalette.Line), 0f, 0f, 1920f, 2f);
            RectTransform chips = UiBuild.Box(UiBuild.Rect("Chips", strip.transform), 40f, 10f, 1480f, 102f);
            UiBuild.Horizontal(chips, 8f);
            RosterChipView chipTemplate = BuildRosterChip(chips);
            TextMeshProUGUI stripStatus = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("StripStatus", strip.transform, 20f, UiPalette.TextDim)), 1560f, 10f, 320f, 28f);
            // Opens the expedition screen (round 71 E2): a button like any other. The departure, with its own sound, is there.
            ButtonParts depart = UiBuild.Button("Depart", strip.transform, UiPalette.Button, 24f);
            UiBuild.Box(depart.Rect, 1560f, 44f, 320f, 62f);

            // The shade over the town (the header stays clear) while a window is open, and the windows over it.
            Image shade = UiBuild.Image("Shade", frame, TownShade, raycastTarget: true);
            UiBuild.Box(shade, 0f, 100f, 1920f, 980f);
            shade.gameObject.SetActive(false);

            // The barracks: the building line (stage 30), the roster with the rows, the party commands and dismissing (a scrolling list: the barracks'
            // levels let the roster outgrow the window), a summary line under it.
            Image barracksWindow = Window(frame, "BarracksWindow", new Rect(40f, 130f, 1840f, 910f), UiKeys.Lobby.BarracksTitle, "BarracksClose", out ButtonParts barracksClose);
            // Slice D stage 35: the barracks under construction only stops dismissing; its note says the level-up still works.
            BuildingLineView barracksLine = BuildBuildingLine(barracksWindow.transform, "Barracks", 1792f, 832f, UiKeys.Lobby.BarracksLockedNote);
            RectTransform entries = ListScroll("Entries", barracksWindow.transform, 24f, 136f, 1792f, 688f, 8f);
            // The trait card (Slice D stage 37) stands in the window, outside the scrolling list's mask; the lines' chips open it.
            TraitTooltipView barracksTraitCard = BuildTraitTooltip(barracksWindow.transform, "Barracks");
            RosterEntryView entryTemplate = BuildRosterEntry(entries, barracksTraitCard);
            TextMeshProUGUI barracksSummary = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("BarracksSummary", barracksWindow.transform, 22f, UiPalette.TextDim)), 24f, 868f, 1792f, 32f);

            // The inn: the building line, resting (a day for everyone, even while the inn is built), and the treatment of one mercenary for gold.
            Image innWindow = Window(frame, "InnWindow", new Rect(360f, 150f, 1200f, 860f), UiKeys.Lobby.InnTitle, "InnClose", out ButtonParts innClose);
            BuildingLineView innLine = BuildBuildingLine(innWindow.transform, "Inn", 1152f, 780f);
            ButtonParts rest = UiBuild.Button("Rest", innWindow.transform, UiPalette.ButtonQuiet, 26f);
            UiBuild.Box(rest.Rect, 24f, 140f, 1152f, 64f);
            RectTransform patients = ListScroll("Patients", innWindow.transform, 24f, 216f, 1152f, 556f, 6f);
            PatientView patientTemplate = BuildPatient(patients);
            TextMeshProUGUI innNote = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("InnNote", innWindow.transform, 20f, UiPalette.TextDim)), 24f, 814f, 1152f, 30f);

            // The stagecoach: the building line, the candidates with their hire buttons (a scrolling list: the stagecoach's levels bring more).
            Image coachWindow = Window(frame, "CoachWindow", new Rect(470f, 160f, 980f, 760f), UiKeys.Lobby.CoachTitle, "CoachClose", out ButtonParts coachClose);
            BuildingLineView coachLine = BuildBuildingLine(coachWindow.transform, "Coach", 932f, 672f);
            TextMeshProUGUI coachSummary = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("CoachSummary", coachWindow.transform, 22f, UiPalette.TextDim)), 24f, 138f, 932f, 32f);
            RectTransform candidates = ListScroll("Candidates", coachWindow.transform, 24f, 178f, 932f, 486f, 8f);
            TraitTooltipView coachTraitCard = BuildTraitTooltip(coachWindow.transform, "Coach");
            CandidateView candidateTemplate = BuildCandidate(candidates, coachTraitCard);
            TextMeshProUGUI noCandidates = UiBuild.Box(UiBuild.LocalizedLabel("NoCandidates", coachWindow.transform, UiKeys.Lobby.NoCandidates, 24f, UiPalette.TextDim, TextAlignmentOptions.Center), 24f, 400f, 932f, 40f);
            UiBuild.Box(UiBuild.LocalizedLabel("CoachNote", coachWindow.transform, UiKeys.Lobby.CoachNote, 20f, UiPalette.TextDim), 24f, 706f, 932f, 30f);

            // The result window (Round 72): the title says how the run ended, four rows, and the new run.
            Image runOverWindow = UiBuild.Panel("RunOverWindow", frame, UiPalette.Panel);
            UiBuild.Box(runOverWindow, 510f, 290f, 900f, 500f);
            AddLine(runOverWindow, UiPalette.Brass, 2f);
            Transform o = runOverWindow.transform;
            TextMeshProUGUI runOverTitle = UiBuild.Box(UiBuild.Label("RunOverTitle", o, 44f, UiPalette.Danger, TextAlignmentOptions.Center), 20f, 30f, 860f, 64f);
            TextMeshProUGUI runOverDay = RunOverRow(o, "RunOverDay", UiKeys.Lobby.EndedOn, 140f);
            TextMeshProUGUI runOverExpeditions = RunOverRow(o, "RunOverExpeditions", UiKeys.Lobby.Expeditions, 188f);
            TextMeshProUGUI runOverFallen = RunOverRow(o, "RunOverFallen", UiKeys.Lobby.FallenLabel, 236f);
            TextMeshProUGUI runOverSurvivors = RunOverRow(o, "RunOverSurvivors", UiKeys.Lobby.Survivors, 284f);
            ButtonParts newRun = UiBuild.LocalizedButton("RunOverNewRun", o, UiKeys.Title.NewRun, UiPalette.Button, 30f);
            UiBuild.Box(newRun.Rect, 250f, 400f, 400f, 70f);
            runOverWindow.gameObject.SetActive(false);

            UiBuild.SetReference(screen, "_town", town);
            UiBuild.SetReference(screen, "_day", day);
            UiBuild.SetReference(screen, "_gold", gold);
            UiBuild.SetReference(screen, "_toTitle", toTitle.Button);
            UiBuild.SetReference(screen, "_barracks", barracks);
            UiBuild.SetReference(screen, "_inn", inn);
            UiBuild.SetReference(screen, "_coach", coach);
            UiBuild.SetReference(screen, "_gate", gate);
            UiBuild.SetReference(screen, "_bubble", bubble.rectTransform);
            UiBuild.SetReference(screen, "_bubbleName", bubbleName);
            UiBuild.SetReference(screen, "_bubbleDetail", bubbleDetail);
            UiBuild.SetReference(screen, "_chipTemplate", chipTemplate);
            UiBuild.SetReference(screen, "_chipParent", chips);
            UiBuild.SetReference(screen, "_stripStatus", stripStatus);
            UiBuild.SetReference(screen, "_depart", depart.Button);
            UiBuild.SetReference(screen, "_departLabel", depart.Label);
            UiBuild.SetReference(screen, "_shade", shade.gameObject);
            UiBuild.SetReference(screen, "_barracksLine", barracksLine);
            UiBuild.SetReference(screen, "_innLine", innLine);
            UiBuild.SetReference(screen, "_coachLine", coachLine);
            UiBuild.SetReference(screen, "_barracksWindow", barracksWindow.gameObject);
            UiBuild.SetReference(screen, "_barracksClose", barracksClose.Button);
            UiBuild.SetReference(screen, "_entryTemplate", entryTemplate);
            UiBuild.SetReference(screen, "_entryParent", entries);
            UiBuild.SetReference(screen, "_barracksSummary", barracksSummary);
            UiBuild.SetReference(screen, "_innWindow", innWindow.gameObject);
            UiBuild.SetReference(screen, "_innClose", innClose.Button);
            UiBuild.SetReference(screen, "_rest", rest.Button);
            UiBuild.SetReference(screen, "_restLabel", rest.Label);
            UiBuild.SetReference(screen, "_patientTemplate", patientTemplate);
            UiBuild.SetReference(screen, "_patientParent", patients);
            UiBuild.SetReference(screen, "_innNote", innNote);
            UiBuild.SetReference(screen, "_coachWindow", coachWindow.gameObject);
            UiBuild.SetReference(screen, "_coachClose", coachClose.Button);
            UiBuild.SetReference(screen, "_coachSummary", coachSummary);
            UiBuild.SetReference(screen, "_candidateTemplate", candidateTemplate);
            UiBuild.SetReference(screen, "_candidateParent", candidates);
            UiBuild.SetReference(screen, "_noCandidates", noCandidates);
            UiBuild.SetReference(screen, "_runOverWindow", runOverWindow.gameObject);
            UiBuild.SetReference(screen, "_runOverTitle", runOverTitle);
            UiBuild.SetReference(screen, "_runOverDay", runOverDay);
            UiBuild.SetReference(screen, "_runOverExpeditions", runOverExpeditions);
            UiBuild.SetReference(screen, "_runOverFallen", runOverFallen);
            UiBuild.SetReference(screen, "_runOverSurvivors", runOverSurvivors);
            UiBuild.SetReference(screen, "_runOverNewRun", newRun.Button);
            return screen;
        }

        /// <summary>
        /// A building of the town (Round 78): the picture of the building at its level (its box is the hotspot, a button; the screen places and
        /// sizes it), the scaffold over it while it is built, the wash with its brass rim shown under the pointer, the plate (a stone box with a
        /// line, the name with the level and a second line the screen fills) under the picture's foot, and under the plate the badge of a
        /// construction (the days left over a bar).
        /// </summary>
        static BuildingView BuildBuilding(Transform frame, string name)
        {
            Image picture = UiBuild.Image(name, frame, Color.white, raycastTarget: true);
            UiBuild.Box(picture, 0f, 0f, 400f, 280f);
            Button button = UiBuild.MakeButton(picture);
            var view = picture.gameObject.AddComponent<BuildingView>();

            Image scaffold = UiBuild.Image(name + "Scaffold", picture.transform, Color.white);
            UiBuild.Box(scaffold, 0f, 0f, 400f, 280f);
            scaffold.gameObject.SetActive(false);

            // The wash cannot carry an Outline: the effect's offset copies of a translucent rect fill it. The rim is four thin images inside its edges.
            Image wash = UiBuild.Image(name + "Wash", picture.transform, BuildingWash);
            UiBuild.Stretch(wash.rectTransform);
            Rim(name + "Rim", wash.transform, BuildingRim, UiPalette.Brass);
            wash.gameObject.SetActive(false);

            Image plate = UiBuild.Panel(name + "Plate", picture.transform, UiPalette.StonePlate);
            UiBuild.Box(plate, 0f, 0f, BuildingView.PlateWidth, BuildingView.PlateHeight);
            Outline plateLine = AddLine(plate, UiPalette.Line, 2f);
            TextMeshProUGUI plateName = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name + "Name", plate.transform, 26f, UiPalette.Text, TextAlignmentOptions.Center)), 0f, 6f, BuildingView.PlateWidth, 38f);
            TextMeshProUGUI plateSub = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name + "Sub", plate.transform, 20f, UiPalette.TextDim, TextAlignmentOptions.Center)), 0f, 44f, BuildingView.PlateWidth, 30f);
            plateSub.gameObject.SetActive(false);

            // The badge of a construction (the mock: "건설 중 · 2일 남음" in brass over a thin brass bar), a brass line round it.
            Image badge = UiBuild.Panel(name + "Badge", picture.transform, UiPalette.StonePlate);
            UiBuild.Box(badge, 0f, 0f, BuildingView.BadgeWidth, BuildingView.BadgeHeight);
            AddLine(badge, UiPalette.Brass, 2f);
            TextMeshProUGUI badgeWords = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name + "BadgeWords", badge.transform, 22f, UiPalette.Brass, TextAlignmentOptions.Center)), 0f, 6f, BuildingView.BadgeWidth, 30f);
            UiBar badgeBar = UiBuild.Bar(name + "BadgeBar", badge.transform, UiPalette.Brass);
            UiBuild.Box(badgeBar, 16f, 42f, BuildingView.BadgeWidth - 32f, 8f);
            badge.gameObject.SetActive(false);

            // Slice D stage 35: a small gold count at the plate's upper right corner (the barracks' mercenaries who can level up), hidden at zero.
            Image count = UiBuild.Panel(name + "Count", plate.transform, UiPalette.LevelUp);
            UiBuild.Box(count, BuildingView.PlateWidth - 40f, -14f, 70f, 34f);
            TextMeshProUGUI countWords = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name + "CountWords", count.transform, 20f, UiPalette.LevelUpText, TextAlignmentOptions.Center)), 0f, 2f, 70f, 30f);
            count.gameObject.SetActive(false);

            UiBuild.SetReference(view, "_button", button);
            UiBuild.SetReference(view, "_picture", picture);
            UiBuild.SetReference(view, "_scaffold", scaffold);
            UiBuild.SetReference(view, "_wash", wash.gameObject);
            UiBuild.SetReference(view, "_plate", plate.rectTransform);
            UiBuild.SetReference(view, "_plateLine", plateLine);
            UiBuild.SetReference(view, "_name", plateName);
            UiBuild.SetReference(view, "_sub", plateSub);
            UiBuild.SetReference(view, "_badge", badge.rectTransform);
            UiBuild.SetReference(view, "_badgeWords", badgeWords);
            UiBuild.SetReference(view, "_badgeBar", badgeBar);
            UiBuild.SetReference(view, "_count", count.rectTransform);
            UiBuild.SetReference(view, "_countWords", countWords);
            return view;
        }

        /// <summary>
        /// The building line of a window (stage 30): a light panel under the title with the level (bone), what the next level gives (dim) and the
        /// [업그레이드] button at the right; and, at <paramref name="lockedY"/>, the note that the building is closed while under construction.
        /// </summary>
        static BuildingLineView BuildBuildingLine(Transform window, string prefix, float width, float lockedY, string lockedKey = UiKeys.Lobby.LockedNote)
        {
            Image line = UiBuild.Panel(prefix + "Line", window, UiPalette.PanelLight);
            UiBuild.Box(line, 24f, 72f, width, 56f);
            TextMeshProUGUI level = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(prefix + "Level", line.transform, 26f, UiPalette.Bone)), 20f, 11f, 150f, 34f);
            TextMeshProUGUI effect = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(prefix + "Effect", line.transform, 22f, UiPalette.TextDim)), 170f, 13f, width - 480f, 32f);
            ButtonParts upgrade = UiBuild.Button(prefix + "Upgrade", line.transform, UiPalette.Button, 22f);
            UiBuild.Box(upgrade.Rect, width - 296f, 7f, 276f, 42f);
            TextMeshProUGUI locked = UiBuild.Box(UiBuild.LocalizedLabel(prefix + "Locked", window, lockedKey, 22f, UiPalette.Burn, TextAlignmentOptions.Center), 24f, lockedY, width, 32f);
            locked.gameObject.SetActive(false);

            var view = line.gameObject.AddComponent<BuildingLineView>();
            UiBuild.SetReference(view, "_level", level);
            UiBuild.SetReference(view, "_effect", effect);
            UiBuild.SetReference(view, "_upgrade", upgrade.Button);
            UiBuild.SetReference(view, "_upgradeLabel", upgrade.Label);
            UiBuild.SetReference(view, "_locked", locked);
            return view;
        }

        /// <summary>
        /// A scrolling list of a window (stage 30: the lists outgrow their windows as the buildings level): the box is the viewport (a mask that
        /// takes the wheel), the content stacks the rows the screen clones from a template with the given spacing and grows to fit them.
        /// </summary>
        static RectTransform ListScroll(string name, Transform window, float x, float y, float width, float height, float spacing)
        {
            RectTransform root = UiBuild.Box(UiBuild.Rect(name + "Scroll", window), x, y, width, height);
            var scroll = root.gameObject.AddComponent<ScrollRect>();
            Image viewport = UiBuild.Image(name + "Viewport", root, Color.clear, raycastTarget: true);
            UiBuild.Stretch(viewport.rectTransform);
            viewport.gameObject.AddComponent<RectMask2D>();
            RectTransform content = UiBuild.Rect(name, viewport.transform);
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.offsetMin = Vector2.zero;
            content.offsetMax = Vector2.zero;
            UiBuild.Vertical(content, spacing);
            var fitter = content.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.viewport = viewport.rectTransform;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 40f;
            return content;
        }

        /// <summary>A rim of four thin images along the inside of the parent's edges.</summary>
        static void Rim(string name, Transform parent, float thickness, Color color)
        {
            Edge(UiBuild.Image(name + "Top", parent, color), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -thickness), Vector2.zero);
            Edge(UiBuild.Image(name + "Bottom", parent, color), Vector2.zero, new Vector2(1f, 0f), Vector2.zero, new Vector2(0f, thickness));
            Edge(UiBuild.Image(name + "Left", parent, color), Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(thickness, 0f));
            Edge(UiBuild.Image(name + "Right", parent, color), new Vector2(1f, 0f), Vector2.one, new Vector2(-thickness, 0f), Vector2.zero);
        }

        static void Edge(Image image, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            RectTransform rect = image.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
        }

        /// <summary>A quarter's window over the shaded town: a panel with a brass line, its title and its close button. Closed until opened.</summary>
        static Image Window(Transform frame, string name, Rect box, string titleKey, string closeName, out ButtonParts close)
        {
            Image window = UiBuild.Panel(name, frame, UiPalette.Panel);
            UiBuild.Box(window, box.x, box.y, box.width, box.height);
            AddLine(window, UiPalette.Brass, 2f);
            UiBuild.Box(UiBuild.LocalizedLabel(name + "Title", window.transform, titleKey, 30f, UiPalette.TextDim), 24f, 16f, 700f, 44f);
            close = UiBuild.LocalizedButton(closeName, window.transform, UiKeys.Common.Close, UiPalette.ButtonQuiet, 22f);
            UiBuild.Box(close.Rect, box.width - 180f, 16f, 156f, 50f);
            window.gameObject.SetActive(false);
            return window;
        }

        /// <summary>A row of the result window: the dim label at the left, the value the screen fills at the right.</summary>
        static TextMeshProUGUI RunOverRow(Transform window, string name, string labelKey, float y)
        {
            UiBuild.Box(UiBuild.LocalizedLabel(name + "Label", window, labelKey, 26f, UiPalette.TextDim), 50f, y, 300f, 40f);
            return UiBuild.Box(UiBuild.SingleLine(UiBuild.Label(name, window, 26f, UiPalette.Text, TextAlignmentOptions.Right)), 350f, y, 500f, 40f);
        }

        /// <summary>The template of a chip of the roster strip: the name, the row tag, the job line and the fatigue pips. The lobby clones it per mercenary.</summary>
        static RosterChipView BuildRosterChip(Transform parent)
        {
            Image background = UiBuild.Panel("ChipTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, 176f, 102f);
            Transform chip = background.transform;
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("ChipName", chip, 20f, UiPalette.Text)), 10f, 6f, 108f, 28f);
            // The level at the right of the name (the mock's "Lv 2", stage 27), the row the mercenary holds at the right of the job.
            // Slice D stage 35: a ▲ in gold goes before the level when the barracks can level the mercenary up — the box reaches left for it.
            TextMeshProUGUI level = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("ChipLevel", chip, 16f, UiPalette.Bone, TextAlignmentOptions.Right)), 100f, 8f, 66f, 24f);
            TextMeshProUGUI job = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("ChipJob", chip, 16f, UiPalette.TextDim)), 10f, 36f, 104f, 24f);
            TextMeshProUGUI row = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("ChipRow", chip, 16f, UiPalette.Brass, TextAlignmentOptions.Right)), 114f, 36f, 52f, 24f);
            RectTransform pips = UiBuild.Box(UiBuild.Rect("ChipPips", chip), 10f, 72f, 156f, 10f);
            UiBar[] pipRow = FatiguePipRow("ChipPip", pips, 156f, 10f);

            var view = background.gameObject.AddComponent<RosterChipView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_level", level);
            UiBuild.SetReference(view, "_row", row);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReferences(view, "_pips", pipRow);
            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The template of a line of the inn: the name, the job line, the pips and the words, and the treat button. The lobby clones it per mercenary.</summary>
        static PatientView BuildPatient(Transform parent)
        {
            Image background = UiBuild.Panel("PatientTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, 1152f, 72f);
            Transform line = background.transform;
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PatientName", line, 26f, UiPalette.Text)), 16f, 6f, 200f, 34f);
            TextMeshProUGUI job = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PatientJob", line, 20f, UiPalette.TextDim)), 224f, 10f, 240f, 30f);
            RectTransform pips = UiBuild.Box(UiBuild.Rect("PatientPips", line), 16f, 50f, 440f, 10f);
            UiBar[] pipRow = FatiguePipRow("PatientPip", pips, 440f, 10f);
            TextMeshProUGUI fatigue = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("PatientFatigue", line, 20f, UiPalette.Fatigue, TextAlignmentOptions.Right)), 470f, 10f, 380f, 30f);
            ButtonParts treat = UiBuild.Button("PatientTreat", line, UiPalette.Button, 20f);
            UiBuild.Box(treat.Rect, 876f, 10f, 260f, 52f);

            var view = background.gameObject.AddComponent<PatientView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReferences(view, "_pips", pipRow);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_treat", treat.Button);
            UiBuild.SetReference(view, "_treatLabel", treat.Label);
            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The template of a candidate of the stagecoach: the name, the job, its traits and the hire button. The lobby clones it per candidate.</summary>
        static CandidateView BuildCandidate(Transform parent, TraitTooltipView traitCard)
        {
            Image background = UiBuild.Panel("CandidateTemplate", parent, UiPalette.PanelLight);
            // 150 high from Round 81 (Slice D stage 32): the stat line stands under the passive, past a hairline; the hire button sits in the middle.
            UiBuild.Size(background, 932f, 150f);
            Transform line = background.transform;
            TextMeshProUGUI name = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("CandidateName", line, 30f, UiPalette.Text)), 20f, 10f, 260f, 40f);
            TextMeshProUGUI job = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("CandidateJob", line, 22f, UiPalette.TextDim)), 290f, 16f, 180f, 32f);
            TextMeshProUGUI detail = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("CandidateDetail", line, 18f, UiPalette.Bone)), 480f, 19f, 210f, 28f);
            // The traits where the passive stood (Slice D stage 37, Round 84): the job's trait and the 0~2 the candidate was born with.
            TraitLineView traits = BuildTraitLine("CandidateTraits", line, 20f, 62f, 28f, 24f, 17f, 6, true, traitCard);
            UiBuild.Box(UiBuild.Image("CandidateStatLine", line, UiPalette.ButtonQuiet), 20f, 102f, 660f, 1f);
            TextMeshProUGUI stats = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("CandidateStats", line, 20f, UiPalette.Text), 20f, 106f, 680f, 32f));
            ButtonParts hire = UiBuild.Button("CandidateHire", line, UiPalette.Button, 22f);
            UiBuild.Box(hire.Rect, 700f, 47f, 212f, 56f);

            var view = background.gameObject.AddComponent<CandidateView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_detail", detail);
            UiBuild.SetReference(view, "_traits", traits);
            UiBuild.SetReference(view, "_stats", stats);
            UiBuild.SetReference(view, "_hire", hire.Button);
            UiBuild.SetReference(view, "_hireLabel", hire.Label);
            background.gameObject.SetActive(false);
            return view;
        }

        /// <summary>The fatigue pips of a roster line (FatiguePips.Count of them): the gap between two, and how wide they all are.</summary>
        const float FatiguePipGap = 4f;
        const float FatiguePipsWidth = 590f;

        /// <summary>
        /// FatiguePips.Count bars side by side across a width, FatiguePipGap apart: the roster line's, the strip's chips', the inn's lines' and the party
        /// cards' of the expedition screen (round 71 E2). <see cref="UI.FatiguePips.Show"/> fills them.
        /// </summary>
        static UiBar[] FatiguePipRow(string namePrefix, RectTransform parent, float width, float height)
        {
            var pips = new UiBar[FatiguePips.Count];
            float pipWidth = (width - FatiguePipGap * (pips.Length - 1)) / pips.Length;
            for (int i = 0; i < pips.Length; i++)
            {
                float left = Mathf.Round(i * (pipWidth + FatiguePipGap));
                float right = Mathf.Round(i * (pipWidth + FatiguePipGap) + pipWidth);
                pips[i] = UiBuild.Bar(namePrefix + i, parent, UiPalette.FatigueBar);
                UiBuild.Box(pips[i], left, 0f, right - left, height);
            }

            return pips;
        }

        /// <summary>The barracks line's stat sheet (Round 81 안 B): where it starts and how wide a cell is (six of them fill 640..1108).</summary>
        const float StatSheetLeft = 640f;
        const float StatCellWidth = 78f;

        /// <summary>The template of one roster line of the barracks (1792 wide, 92 high: eight lines fit the window). The lobby clones it per mercenary.</summary>
        static RosterEntryView BuildRosterEntry(Transform parent, TraitTooltipView traitCard)
        {
            Image background = UiBuild.Panel("EntryTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, 1792f, 92f);
            Transform entry = background.transform;

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryName", entry, 30f, UiPalette.Text), 20f, 6f, 220f, 38f));
            TextMeshProUGUI job = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryJob", entry, 22f, UiPalette.TextDim), 250f, 10f, 190f, 32f));
            // The fatigue words grow to the left when an affliction's name follows them (round 36): their box reaches back over the
            // job's, whose words are short and left-aligned, and they stay on one line.
            TextMeshProUGUI fatigue = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryFatigue", entry, 20f, UiPalette.Fatigue, TextAlignmentOptions.Right), 330f, 10f, 280f, 32f));

            // Fatigue as pips (2026-10-06 round 33, C, Darkest Dungeon's way): FatiguePips.Count bars side by side in the bar's place,
            // each a tenth of the most fatigue; the view fills them and colours the ones past the breakdown.
            RectTransform pips = UiBuild.Box(UiBuild.Rect("EntryFatiguePips", entry), 20f, 48f, FatiguePipsWidth, 12f);
            UiBar[] fatiguePips = FatiguePipRow("EntryFatiguePip", pips, FatiguePipsWidth, 12f);

            // The traits on the left of the line under the pips (Slice D stage 37, Round 84 안 A: where the passive stood — the job's trait first, an icon
            // and a name each, "+N" past what fits) and, at its right end, what the mercenary's affliction does (round 36; empty without one).
            TraitLineView traits = BuildTraitLine("EntryTraits", entry, 20f, 62f, 26f, 24f, 17f, 8, true, traitCard);
            TextMeshProUGUI state = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryState", entry, 16f, UiPalette.FatigueDanger, TextAlignmentOptions.Right), 410f, 62f, 200f, 26f));

            // The six stats as a sheet (Slice D stage 32, Round 81 안 B): where the row buttons and "remove" stood (the party is formed in the
            // expedition screen's popup only), a cell each, the name over the value, a hairline between two cells.
            var statNames = new TextMeshProUGUI[StatBlock.All.Length];
            var statValues = new TextMeshProUGUI[StatBlock.All.Length];
            for (int i = 0; i < statNames.Length; i++)
            {
                float left = StatSheetLeft + i * StatCellWidth;
                if (i > 0)
                {
                    UiBuild.Box(UiBuild.Image("EntryStatLine" + i, entry, UiPalette.ButtonQuiet), left, 16f, 1f, 60f);
                }

                statNames[i] = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryStatName" + i, entry, 18f, UiPalette.TextDim, TextAlignmentOptions.Center), left, 12f, StatCellWidth, 26f));
                statValues[i] = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryStatValue" + i, entry, 30f, UiPalette.Text, TextAlignmentOptions.Center), left, 38f, StatCellWidth, 44f));
            }

            // The level and the experience (stage 27, the mock's block: "Lv 2", "경험치 4/20" and the bar), then dismissing (stage 26) at the far right.
            TextMeshProUGUI level = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("EntryLevel", entry, 28f, UiPalette.Bone)), 1140f, 8f, 110f, 40f);
            TextMeshProUGUI exp = UiBuild.Box(UiBuild.SingleLine(UiBuild.Label("EntryExp", entry, 18f, UiPalette.TextDim)), 1250f, 16f, 230f, 30f);
            UiBar expBar = UiBuild.Box(UiBuild.Bar("EntryExpBar", entry, UiPalette.Bone), 1140f, 56f, 340f, 8f);
            ButtonParts dismiss = UiBuild.LocalizedButton("EntryDismiss", entry, UiKeys.Lobby.Dismiss, UiPalette.ButtonQuiet, 20f);
            UiBuild.Box(dismiss.Rect, 1620f, 16f, 152f, 60f);

            // Slice D stage 35 (Round 82 안 A): the level-up button at the end of the shortened gold bar (RosterEntryView.ReadyBarWidth), shown only when
            // the experience meets the level, and the gold rim round a line whose roll the window just showed.
            ButtonParts levelUp = UiBuild.LocalizedButton("EntryLevelUp", entry, UiKeys.Lobby.LevelUp, UiPalette.LevelUp, 22f);
            UiBuild.Box(levelUp.Rect, 1348f, 20f, 148f, 52f);
            levelUp.Label.color = UiPalette.LevelUpText;
            // Its own sound (the heal's small bell, LobbyScreen.LevelUp) instead of the click.
            UiBuild.Silence(levelUp.Button);
            levelUp.Button.gameObject.SetActive(false);
            // The rim is four thin images inside the line's edges (an Outline's copies outside them would be cut by the scrolling list's mask).
            RectTransform rim = UiBuild.Rect("EntryRim", entry);
            UiBuild.Stretch(rim);
            Rim("EntryRim", rim, 2f, UiPalette.Brass);
            rim.gameObject.SetActive(false);

            var view = background.gameObject.AddComponent<RosterEntryView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_traits", traits);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_state", state);
            UiBuild.SetReferences(view, "_fatiguePips", fatiguePips);
            UiBuild.SetReferences(view, "_statNames", statNames);
            UiBuild.SetReferences(view, "_statValues", statValues);
            UiBuild.SetReference(view, "_dismiss", dismiss.Button);
            UiBuild.SetReference(view, "_level", level);
            UiBuild.SetReference(view, "_exp", exp);
            UiBuild.SetReference(view, "_expBar", expBar);
            UiBuild.SetReference(view, "_levelUp", levelUp.Button);
            UiBuild.SetReference(view, "_rim", rim.gameObject);

            background.gameObject.SetActive(false);
            return view;
        }
    }
}
