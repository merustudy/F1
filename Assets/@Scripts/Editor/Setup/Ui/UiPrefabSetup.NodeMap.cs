using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        /// <summary>
        /// The node map: the party as in battle (the stage on the left, the boards in the panel
        /// under it), the potions and the map on the right above the panel, and the chosen node's words and the
        /// buttons in the panel's right half. The panel names no enemies: who waits at a node is
        /// not shown (Docs/Design/03_Dungeon_Structure.md §1).
        /// </summary>
        static UIScreen BuildNodeMap(Transform holder)
        {
            NodeMapScreen screen = Screen<NodeMapScreen>("NodeMapScreen", holder, out RectTransform frame);

            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 80f);
            TextMeshProUGUI dungeon = UiBuild.Box(UiBuild.Label("Dungeon", header.transform, 36f, UiPalette.Text), 40f, 14f, 900f, 52f);
            TextMeshProUGUI progress = UiBuild.Label("Progress", header.transform, 30f, UiPalette.TextDim, TextAlignmentOptions.Right);
            UiBuild.Box(progress, 1280f, 18f, 600f, 44f);

            // Map: nodes and paths are created at runtime inside the area, measured from its bottom-left corner.
            // It starts under the potions and ends above the panel.
            Image map = UiBuild.Panel("Map", frame, UiPalette.Panel);
            UiBuild.Box(map, 980f, PartyRightTop, 920f, BoardPanelTop - 16f - PartyRightTop);
            RectTransform mapArea = UiBuild.Box(UiBuild.Rect("MapArea", map.transform), 30f, 30f, 860f, BoardPanelTop - 16f - PartyRightTop - 60f);

            Image edgeTemplate = UiBuild.Image("EdgeTemplate", mapArea, UiPalette.Line);
            UiBuild.Place(edgeTemplate.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 6f));
            edgeTemplate.gameObject.SetActive(false);

            Image nodeFrame = UiBuild.Image("NodeTemplate", mapArea, UiPalette.ButtonQuiet);
            UiBuild.Place(nodeFrame.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150f, 84f));
            Button nodeButton = UiBuild.MakeButton(nodeFrame);

            // Only the nodes that can be chosen are clickable. The others must keep their state color
            // instead of looking dimmed.
            ColorBlock nodeColors = nodeButton.colors;
            nodeColors.disabledColor = Color.white;
            nodeButton.colors = nodeColors;
            TextMeshProUGUI nodeLabel = UiBuild.Label("NodeLabel", nodeFrame.transform, 28f, UiPalette.Text, TextAlignmentOptions.Center);
            UiBuild.Stretch(nodeLabel.rectTransform);
            var nodeTemplate = nodeFrame.gameObject.AddComponent<MapNodeView>();
            UiBuild.SetReference(nodeTemplate, "_button", nodeButton);
            UiBuild.SetReference(nodeTemplate, "_frame", nodeFrame);
            UiBuild.SetReference(nodeTemplate, "_label", nodeLabel);
            nodeFrame.gameObject.SetActive(false);

            PartySideView party = BuildPartySide(frame, PanelRightX, 200f, out Image panel);

            // The chosen node in the panel's right half: its floor and kind, the note that the enemies stay unknown, then the inventory and the fight.
            TextMeshProUGUI nodeTitle = UiBuild.SingleLine(UiBuild.Box(UiBuild.Label("NodeTitle", panel.transform, 34f, UiPalette.Text), PanelRightX, PanelTitleTop, PanelRightWidth, 48f));
            UiBuild.Box(UiBuild.LocalizedLabel("NodeHint", panel.transform, UiKeys.Map.Unknown, 20f, UiPalette.TextDim), PanelRightX, PanelHintTop, PanelRightWidth, 30f);
            ButtonParts inventoryToggle = KitButton("InventoryToggle", panel.transform, UiPalette.ButtonQuiet, 24f);
            UiBuild.Box(inventoryToggle.Rect, 1220f, PanelButtonsTop, 236f, PanelButtonHeight);
            ButtonParts enter = KitLocalizedButton("Enter", panel.transform, UiKeys.Map.Enter, UiPalette.Button, 30f);
            UiBuild.Box(enter.Rect, 1644f, PanelButtonsTop, 236f, PanelButtonHeight);

            UiBuild.SetReference(screen, "_dungeon", dungeon);
            UiBuild.SetReference(screen, "_progress", progress);
            UiBuild.SetReference(screen, "_mapArea", mapArea);
            UiBuild.SetReference(screen, "_nodeTemplate", nodeTemplate);
            UiBuild.SetReference(screen, "_edgeTemplate", edgeTemplate);
            UiBuild.SetReference(screen, "_nodeTitle", nodeTitle);
            UiBuild.SetReference(screen, "_enter", enter.Button);
            UiBuild.SetReference(screen, "_inventoryToggle", inventoryToggle.Button);
            UiBuild.SetReference(screen, "_inventoryToggleLabel", inventoryToggle.Label);
            UiBuild.SetReference(screen, "_party", party);
            return screen;
        }
    }
}
