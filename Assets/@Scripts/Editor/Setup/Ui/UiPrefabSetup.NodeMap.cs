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

            Image header = KitFrame("Header", frame, UiArt.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 84f);
            Image titlePlate = KitFrame("TitlePlate", header.transform, UiArt.PlateLabel);
            UiBuild.Box(titlePlate, 960f - TitlePlateWidth / 2f, 4f, TitlePlateWidth, TitlePlateHeight);
            TextMeshProUGUI dungeon = UiBuild.ShrinkToFit(UiBuild.SingleLine(UiBuild.Label("Dungeon", titlePlate.transform, 26f, UiPalette.Brass, TextAlignmentOptions.Center)), 18f);
            UiBuild.Stretch(dungeon.rectTransform, 20f, 8f, 20f, 8f);
            TextMeshProUGUI progress = UiBuild.Label("Progress", header.transform, 30f, UiPalette.TextDim, TextAlignmentOptions.Right);
            UiBuild.Box(progress, 1280f, 18f, 600f, 44f);

            // Map: nodes and paths are created at runtime inside the area, measured from its bottom-left corner.
            // It starts under the potions and ends above the panel.
            Image map = KitFrame("Map", frame, UiArt.Tablet);
            UiBuild.Box(map, 980f, PartyRightTop, 920f, BoardPanelTop - 16f - PartyRightTop);
            RectTransform mapArea = UiBuild.Box(UiBuild.Rect("MapArea", map.transform), 30f, 30f, 860f, BoardPanelTop - 16f - PartyRightTop - 60f);

            // The paths are lines of gold carved in the stone tablet; a node is a disc in the color of its state with the marker of its kind on it and its name under it.
            Image edgeTemplate = UiBuild.Image("EdgeTemplate", mapArea, new Color(UiPalette.Brass.r, UiPalette.Brass.g, UiPalette.Brass.b, 0.85f));
            UiBuild.Place(edgeTemplate.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(100f, 4f));
            edgeTemplate.gameObject.SetActive(false);

            Image nodeFrame = UiBuild.Image("NodeTemplate", mapArea, UiPalette.ButtonQuiet);
            nodeFrame.sprite = UiBuild.BuiltinSprite("UI/Skin/Knob.psd");
            UiBuild.Place(nodeFrame.rectTransform, Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(74f, 74f));
            Button nodeButton = UiBuild.MakeButton(nodeFrame);
            Image nodeIcon = KitIcon("NodeIcon", nodeFrame.transform, UiArt.NodeBattle);
            UiBuild.Stretch(nodeIcon.rectTransform, 13f, 13f, 13f, 13f);

            // Only the nodes that can be chosen are clickable. The others must keep their state color
            // instead of looking dimmed.
            ColorBlock nodeColors = nodeButton.colors;
            nodeColors.disabledColor = Color.white;
            nodeButton.colors = nodeColors;
            TextMeshProUGUI nodeLabel = UiBuild.SingleLine(UiBuild.Label("NodeLabel", nodeFrame.transform, 18f, UiPalette.Text, TextAlignmentOptions.Center));
            UiBuild.Place(nodeLabel.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(140f, 24f));
            var nodeTemplate = nodeFrame.gameObject.AddComponent<MapNodeView>();
            UiBuild.SetReference(nodeTemplate, "_button", nodeButton);
            UiBuild.SetReference(nodeTemplate, "_frame", nodeFrame);
            UiBuild.SetReference(nodeTemplate, "_icon", nodeIcon);
            UiBuild.SetReference(nodeTemplate, "_battleIcon", UiArt.Load(UiArt.NodeBattle));
            UiBuild.SetReference(nodeTemplate, "_bossIcon", UiArt.Load(UiArt.NodeBoss));
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
