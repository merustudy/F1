using F1.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.Editor.Setup
{
    public static partial class UiPrefabSetup
    {
        static UIScreen BuildNodeMap(Transform holder)
        {
            NodeMapScreen screen = Screen<NodeMapScreen>("NodeMapScreen", holder, out RectTransform frame);

            // Header
            Image header = UiBuild.Panel("Header", frame, UiPalette.Panel);
            UiBuild.Box(header, 0f, 0f, 1920f, 90f);
            TextMeshProUGUI dungeon = UiBuild.Box(UiBuild.Label("Dungeon", header.transform, 40f, UiPalette.Text), 40f, 18f, 900f, 54f);
            TextMeshProUGUI progress = UiBuild.Label("Progress", header.transform, 32f, UiPalette.TextDim, TextAlignmentOptions.Right);
            UiBuild.Box(progress, 1280f, 24f, 600f, 44f);

            // Map: nodes and paths are created at runtime inside the area, measured from its bottom-left corner.
            Image map = UiBuild.Panel("Map", frame, UiPalette.Panel);
            UiBuild.Box(map, 40f, 110f, 760f, 930f);
            RectTransform mapArea = UiBuild.Box(UiBuild.Rect("MapArea", map.transform), 30f, 30f, 700f, 870f);

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

            // Who waits at the selected node.
            Image info = UiBuild.Panel("NodeInfo", frame, UiPalette.Panel);
            UiBuild.Box(info, 820f, 110f, 1060f, 290f);
            TextMeshProUGUI nodeTitle = UiBuild.Box(UiBuild.Label("NodeTitle", info.transform, 34f, UiPalette.Text), 24f, 16f, 760f, 48f);
            UiBuild.Box(UiBuild.LocalizedLabel("FrontHeader", info.transform, UiKeys.Map.EnemyFront, 22f, UiPalette.TextDim), 24f, 76f, 370f, 30f);
            TextMeshProUGUI enemyFront = UiBuild.Label("EnemyFront", info.transform, 26f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(enemyFront, 24f, 110f, 370f, 160f);
            UiBuild.Box(UiBuild.LocalizedLabel("RearHeader", info.transform, UiKeys.Map.EnemyRear, 22f, UiPalette.TextDim), 410f, 76f, 370f, 30f);
            TextMeshProUGUI enemyRear = UiBuild.Label("EnemyRear", info.transform, 26f, UiPalette.Text, TextAlignmentOptions.TopLeft);
            UiBuild.Box(enemyRear, 410f, 110f, 370f, 160f);
            ButtonParts enter = UiBuild.LocalizedButton("Enter", info.transform, UiKeys.Map.Enter, UiPalette.Button, 36f);
            UiBuild.Box(enter.Rect, 800f, 170f, 236f, 96f);

            PartyBoardView board = BuildBoard(frame, 820f, 420f);

            UiBuild.SetReference(screen, "_dungeon", dungeon);
            UiBuild.SetReference(screen, "_progress", progress);
            UiBuild.SetReference(screen, "_mapArea", mapArea);
            UiBuild.SetReference(screen, "_nodeTemplate", nodeTemplate);
            UiBuild.SetReference(screen, "_edgeTemplate", edgeTemplate);
            UiBuild.SetReference(screen, "_nodeTitle", nodeTitle);
            UiBuild.SetReference(screen, "_enemyFront", enemyFront);
            UiBuild.SetReference(screen, "_enemyRear", enemyRear);
            UiBuild.SetReference(screen, "_enter", enter.Button);
            UiBuild.SetReference(screen, "_board", board);
            return screen;
        }
    }
}
