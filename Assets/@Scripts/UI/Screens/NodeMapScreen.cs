using System.Collections.Generic;
using System.Threading.Tasks;
using F1.Core;
using F1.Data;
using F1.Flow;
using F1.Gameplay;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace F1.UI
{
    /// <summary>
    /// The expedition map. The player arranges the party on the left (the battle screen's shape)
    /// and picks the next node on the right. Who waits at a node is not shown: that is found out
    /// in the battle (Docs/Design/03_Dungeon_Structure.md §1).
    /// </summary>
    public sealed class NodeMapScreen : UIScreen
    {
        [SerializeField] TMP_Text _dungeon;
        [SerializeField] TMP_Text _progress;
        [SerializeField] RectTransform _mapArea;
        [SerializeField] MapNodeView _nodeTemplate;
        [SerializeField] Image _edgeTemplate;
        [SerializeField] TMP_Text _nodeTitle;
        [SerializeField] Button _enter;
        [SerializeField] Button _inventoryToggle;
        [SerializeField] TMP_Text _inventoryToggleLabel;
        [SerializeField] PartySideView _party;

        readonly Dictionary<int, MapNodeView> _nodes = new Dictionary<int, MapNodeView>();
        int _selectedNodeId = -1;

        ExpeditionArt _art;

        /// <summary>The art of the units is loaded before the screen opens, so nothing waits for it afterwards.</summary>
        public override async Task PrepareAsync()
        {
            _art = await ExpeditionArt.LoadAsync(Managers.Resource, Managers.Data.Data);
        }

        protected override void OnOpen()
        {
            BuildMap(Managers.Expedition.Expedition.Map);
            _enter.onClick.AddListener(OnEnter);
            _inventoryToggle.onClick.AddListener(OnInventoryToggle);
            _party.Open(_art);

            // With a single way forward there is nothing to choose: select it.
            IReadOnlyList<MapNode> available = Managers.Expedition.AvailableNodes();
            if (available.Count == 1)
            {
                _selectedNodeId = available[0].Id;
            }
        }

        public override void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;

            _dungeon.text = UiText.Name(data.Dungeons.Get(expedition.DungeonId).Name);
            int currentFloor = expedition.CurrentNodeId < 0 ? 0 : expedition.Map.Get(expedition.CurrentNodeId).Floor;
            _progress.text = UiStrings.Get(UiKeys.Map.Progress, currentFloor, expedition.Map.FloorCount);

            var available = new HashSet<int>();
            foreach (MapNode node in manager.AvailableNodes())
            {
                available.Add(node.Id);
            }

            foreach (MapNode node in expedition.Map.Nodes)
            {
                MapNodeState state;
                if (available.Contains(node.Id))
                {
                    state = MapNodeState.Available;
                }
                else if (node.Id == expedition.CurrentNodeId)
                {
                    state = MapNodeState.Current;
                }
                else
                {
                    state = node.Floor <= currentFloor ? MapNodeState.Passed : MapNodeState.Ahead;
                }

                _nodes[node.Id].Show(KindText(node), state, node.Id == _selectedNodeId);
            }

            bool hasSelection = _selectedNodeId >= 0;
            _enter.interactable = hasSelection;
            _nodeTitle.text = hasSelection
                ? UiStrings.Get(UiKeys.Map.NodeTitle, expedition.Map.Get(_selectedNodeId).Floor, KindText(expedition.Map.Get(_selectedNodeId)))
                : UiStrings.Get(UiKeys.Map.SelectNode);
            _inventoryToggleLabel.text = UiStrings.Get(_party.InventoryOpen ? UiKeys.Board.InventoryHide : UiKeys.Board.InventoryShow);
            _party.Refresh();
        }

        static string KindText(MapNode node)
        {
            return UiStrings.Get(node.Kind == MapNodeKind.Boss ? UiKeys.Map.Boss : UiKeys.Map.Battle);
        }

        /// <summary>Lays the nodes out floor by floor, first floor at the bottom, and draws the paths between them.</summary>
        void BuildMap(NodeMap map)
        {
            Vector2 size = _mapArea.rect.size;
            var positions = new Dictionary<int, Vector2>();
            for (int floor = 1; floor <= map.FloorCount; floor++)
            {
                List<MapNode> nodes = map.OnFloor(floor);
                foreach (MapNode node in nodes)
                {
                    float x = (node.Column + 0.5f) / nodes.Count * size.x;
                    float y = (floor - 0.5f) / map.FloorCount * size.y;
                    positions.Add(node.Id, new Vector2(x, y));
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                foreach (int nextId in node.NextNodeIds)
                {
                    DrawEdge(positions[node.Id], positions[nextId]);
                }
            }

            foreach (MapNode node in map.Nodes)
            {
                MapNodeView view = Instantiate(_nodeTemplate, _mapArea);
                view.gameObject.SetActive(true);
                view.Rect.anchorMin = Vector2.zero;
                view.Rect.anchorMax = Vector2.zero;
                view.Rect.anchoredPosition = positions[node.Id];
                _nodes.Add(node.Id, view);

                int id = node.Id;
                view.Button.onClick.AddListener(() => OnNodeClicked(id));
            }
        }

        void DrawEdge(Vector2 from, Vector2 to)
        {
            Image edge = Instantiate(_edgeTemplate, _mapArea);
            edge.gameObject.SetActive(true);
            RectTransform rect = edge.rectTransform;
            Vector2 delta = to - from;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.zero;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = (from + to) * 0.5f;
            rect.sizeDelta = new Vector2(delta.magnitude, rect.sizeDelta.y);
            rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        }

        void OnNodeClicked(int nodeId)
        {
            _selectedNodeId = nodeId;
            Refresh();
        }

        void OnInventoryToggle()
        {
            _party.ToggleInventory();
            Refresh();
        }

        void OnEnter()
        {
            if (_selectedNodeId < 0)
            {
                return;
            }

            Managers.Expedition.EnterNode(_selectedNodeId);
            GoToCurrentPhase();
        }
    }
}
