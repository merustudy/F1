using F1.Data;
using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildLobby(Transform holder)
        {
            LobbyScreen screen = Screen<LobbyScreen>("LobbyScreen", holder, out RectTransform frame);

            // Header
            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 100f);
            UiBuild.Box(UiBuild.LocalizedLabel("HeaderTitle", header.transform, UiKeys.Lobby.Title, 44f, UiPalette.Text), 40f, 20f, 600f, 60f);
            TextMeshProUGUI day = UiBuild.Label("Day", header.transform, 44f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(day, 660f, 20f, 600f, 60f);
            ButtonParts toTitle = UiBuild.LocalizedButton("ToTitle", header.transform, UiKeys.Lobby.ToTitle, UiPalette.ButtonQuiet, 28f);
            UiBuild.Box(toTitle.Rect, 1640f, 22f, 240f, 56f);

            // Roster
            Image roster = UiBuild.Panel("Roster", frame, UiPalette.Panel);
            UiBuild.Box(roster, 40f, 120f, 1160f, 920f);
            UiBuild.Box(UiBuild.LocalizedLabel("RosterHeader", roster.transform, UiKeys.Lobby.Roster, 32f, UiPalette.TextDim), 24f, 16f, 400f, 44f);
            RectTransform entries = UiBuild.Box(UiBuild.Rect("Entries", roster.transform), 24f, 72f, 1112f, 760f);
            UiBuild.Vertical(entries, 10f);
            RosterEntryView entryTemplate = BuildRosterEntry(entries);
            TextMeshProUGUI fallen = UiBuild.Label("Fallen", roster.transform, 24f, UiPalette.Danger, TextAlignmentOptions.TopLeft);
            UiBuild.Box(fallen, 24f, 848f, 1112f, 60f);

            // Expedition
            Image expedition = UiBuild.Panel("Expedition", frame, UiPalette.Panel);
            UiBuild.Box(expedition, 1220f, 120f, 660f, 920f);
            Transform panel = expedition.transform;
            UiBuild.Box(UiBuild.LocalizedLabel("ExpeditionHeader", panel, UiKeys.Lobby.Expedition, 32f, UiPalette.TextDim), 24f, 16f, 400f, 44f);
            TextMeshProUGUI dungeonName = UiBuild.Box(UiBuild.Label("DungeonName", panel, 40f, UiPalette.Text), 24f, 70f, 612f, 52f);
            TextMeshProUGUI dungeonAffinity = UiBuild.Box(UiBuild.Label("DungeonAffinity", panel, 26f, UiPalette.TextDim), 24f, 126f, 612f, 36f);
            TextMeshProUGUI dungeonCost = UiBuild.Box(UiBuild.Label("DungeonCost", panel, 26f, UiPalette.Text), 24f, 164f, 612f, 36f);
            TextMeshProUGUI dungeonCleared = UiBuild.Box(UiBuild.Label("DungeonCleared", panel, 26f, UiPalette.TextDim), 24f, 202f, 612f, 36f);

            // The party, one line per row, row 1 (facing the enemy) first. The lobby shows as many
            // lines as the party has members at most.
            UiBuild.Box(UiBuild.LocalizedLabel("PartyHeader", panel, UiKeys.Lobby.Party, 30f, UiPalette.TextDim), 24f, 280f, 300f, 40f);
            var partyRows = new GameObject[BattleRows.Count];
            var rowNames = new TextMeshProUGUI[BattleRows.Count];
            for (int i = 0; i < rowNames.Length; i++)
            {
                int row = i + 1;
                RectTransform line = UiBuild.Box(UiBuild.Rect("PartyRow" + row, panel), 24f, 332f + 50f * i, 612f, 40f);
                UiBuild.Box(UiBuild.LocalizedLabel("PartyRowLabel" + row, line, UiText.RowKey(row), 26f, UiPalette.TextDim), 0f, 0f, 110f, 40f);
                rowNames[i] = UiBuild.Box(UiBuild.Label("PartyRowNames" + row, line, 28f, UiPalette.Text), 116f, 0f, 496f, 40f);
                partyRows[i] = line.gameObject;
            }

            TextMeshProUGUI departStatus = UiBuild.Label("DepartStatus", panel, 26f, UiPalette.Text, TextAlignmentOptions.BottomLeft);
            UiBuild.Box(departStatus, 24f, 600f, 612f, 76f);
            ButtonParts rest = UiBuild.Button("Rest", panel, UiPalette.ButtonQuiet, 30f);
            UiBuild.Box(rest.Rect, 24f, 690f, 612f, 80f);
            ButtonParts depart = UiBuild.LocalizedButton("Depart", panel, UiKeys.Lobby.Depart, UiPalette.Button, 40f);
            UiBuild.Silence(depart.Button);
            UiBuild.Box(depart.Rect, 24f, 790f, 612f, 100f);

            // Shown when no mercenary is left.
            Image overlay = UiBuild.Image("RunOverPanel", frame, UiPalette.Overlay, raycastTarget: true);
            UiBuild.Stretch(overlay.rectTransform);
            Image overPanel = UiBuild.Panel("RunOverBox", overlay.transform, UiPalette.Panel);
            UiBuild.Box(overPanel, 510f, 330f, 900f, 420f);
            UiBuild.Box(
                UiBuild.LocalizedLabel("RunOverTitle", overPanel.transform, UiKeys.Lobby.RunOver, 56f, UiPalette.Danger, TextAlignmentOptions.Center),
                0f, 50f, 900f, 80f);
            TextMeshProUGUI runOverDay = UiBuild.Label("RunOverDay", overPanel.transform, 32f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Box(runOverDay, 0f, 150f, 900f, 50f);
            ButtonParts newRun = UiBuild.LocalizedButton("RunOverNewRun", overPanel.transform, UiKeys.Title.NewRun, UiPalette.Button, 36f);
            UiBuild.Box(newRun.Rect, 250f, 270f, 400f, 90f);

            UiBuild.SetReference(screen, "_day", day);
            UiBuild.SetReference(screen, "_toTitle", toTitle.Button);
            UiBuild.SetReference(screen, "_entryTemplate", entryTemplate);
            UiBuild.SetReference(screen, "_entryParent", entries);
            UiBuild.SetReference(screen, "_fallen", fallen);
            UiBuild.SetReference(screen, "_dungeonName", dungeonName);
            UiBuild.SetReference(screen, "_dungeonAffinity", dungeonAffinity);
            UiBuild.SetReference(screen, "_dungeonCost", dungeonCost);
            UiBuild.SetReference(screen, "_dungeonCleared", dungeonCleared);
            UiBuild.SetReferences(screen, "_partyRows", partyRows);
            UiBuild.SetReferences(screen, "_rowNames", rowNames);
            UiBuild.SetReference(screen, "_departStatus", departStatus);
            UiBuild.SetReference(screen, "_rest", rest.Button);
            UiBuild.SetReference(screen, "_restLabel", rest.Label);
            UiBuild.SetReference(screen, "_depart", depart.Button);
            UiBuild.SetReference(screen, "_runOverPanel", overlay.gameObject);
            UiBuild.SetReference(screen, "_runOverDay", runOverDay);
            UiBuild.SetReference(screen, "_runOverNewRun", newRun.Button);
            return screen;
        }

        /// <summary>The template of one roster line. The lobby clones it per mercenary.</summary>
        static RosterEntryView BuildRosterEntry(Transform parent)
        {
            Image background = UiBuild.Panel("EntryTemplate", parent, UiPalette.PanelLight);
            UiBuild.Size(background, 1112f, 116f);
            Transform entry = background.transform;

            TextMeshProUGUI name = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryName", entry, 32f, UiPalette.Text), 20f, 8f, 220f, 40f));
            TextMeshProUGUI job = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryJob", entry, 24f, UiPalette.TextDim), 250f, 12f, 190f, 34f));
            TextMeshProUGUI fatigue = UiBuild.Box(UiBuild.Label("EntryFatigue", entry, 22f, UiPalette.Text, TextAlignmentOptions.Right), 410f, 12f, 200f, 34f);
            UiBar fatigueBar = UiBuild.Bar("EntryFatigueBar", entry, UiPalette.Good);
            UiBuild.Box(fatigueBar, 20f, 54f, 590f, 14f);
            TextMeshProUGUI passive = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("EntryPassive", entry, 20f, UiPalette.TextDim), 20f, 76f, 590f, 32f));

            // One button per row, then the button that takes the mercenary out of the party. The row
            // buttons are lined up against the right edge of their box, so the ones the lobby hides
            // (rows the party cannot reach) leave no hole next to "remove".
            RectTransform rowBox = UiBuild.Box(UiBuild.Rect("EntryRows", entry), 628f, 28f, 352f, 60f);
            UiBuild.Horizontal(rowBox, 8f, 0, TextAnchor.MiddleRight);
            var rowButtons = new Button[BattleRows.Count];
            var rowFrames = new Image[BattleRows.Count];
            for (int i = 0; i < rowButtons.Length; i++)
            {
                int row = i + 1;
                ButtonParts button = UiBuild.LocalizedButton("EntryRow" + row, rowBox, UiText.RowKey(row), UiPalette.ButtonQuiet, 22f);
                UiBuild.Size(button.Rect, 80f, 60f);
                rowButtons[i] = button.Button;
                rowFrames[i] = button.Frame;
            }

            ButtonParts remove = UiBuild.LocalizedButton("EntryRemove", entry, UiKeys.Lobby.Remove, UiPalette.ButtonQuiet, 22f);
            UiBuild.Box(remove.Rect, 988f, 28f, 112f, 60f);

            var view = background.gameObject.AddComponent<RosterEntryView>();
            UiBuild.SetReference(view, "_name", name);
            UiBuild.SetReference(view, "_job", job);
            UiBuild.SetReference(view, "_passive", passive);
            UiBuild.SetReference(view, "_fatigue", fatigue);
            UiBuild.SetReference(view, "_fatigueBar", fatigueBar);
            UiBuild.SetReferences(view, "_rows", rowButtons);
            UiBuild.SetReferences(view, "_rowFrames", rowFrames);
            UiBuild.SetReference(view, "_remove", remove.Button);

            background.gameObject.SetActive(false);
            return view;
        }
    }
}
