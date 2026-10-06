using System;
using System.Collections.Generic;
using System.Globalization;
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
    /// in the battle (Docs/Design/03_Dungeon_Structure.md §1). The map scrolls up from the first floor
    /// to the boss; at a camp, its window lies over the map (2026-10-06 round 34).
    /// </summary>
    public sealed class NodeMapScreen : UIScreen
    {
        /// <summary>How far apart the floors are: as on the first, short maps (four floors in 354), however many floors a map has.</summary>
        public const float FloorSpacing = 88.5f;

        /// <summary>The room the names of a floor's nodes take under them.</summary>
        const float NameRoom = 26f;

        [SerializeField] TMP_Text _dungeon;
        [SerializeField] TMP_Text _progress;
        [SerializeField] ScrollRect _mapScroll;
        [SerializeField] RectTransform _mapArea;
        [SerializeField] MapNodeView _nodeTemplate;
        [SerializeField] Image _edgeTemplate;
        [SerializeField] TMP_Text _nodeTitle;
        [SerializeField] TMP_Text _nodeHint;
        [SerializeField] Button _enter;
        [SerializeField] TMP_Text _enterLabel;
        [SerializeField] GameObject _camp;
        [SerializeField] TMP_Text _campTitle;
        [SerializeField] TMP_Text _campHint;
        [SerializeField] GameObject _campChoices;
        [SerializeField] Button _rest;
        [SerializeField] TMP_Text _restBody;
        [SerializeField] Button _mend;
        [SerializeField] GameObject _campMend;
        [SerializeField] GameObject _mendPlate;
        [SerializeField] Image _mendIcon;
        [SerializeField] TMP_Text _mendChange;
        [SerializeField] TMP_Text _mendEffects;
        [SerializeField] Button _mendBack;
        [SerializeField] Button _mendConfirm;
        [SerializeField] Button _inventoryToggle;
        [SerializeField] TMP_Text _inventoryToggleLabel;
        [SerializeField] PartySideView _party;

        readonly Dictionary<int, MapNodeView> _nodes = new Dictionary<int, MapNodeView>();
        int _selectedNodeId = -1;
        float _floorSpacing;

        // The camp's mend step (round 35): choosing on the boards the item to raise a tier, then confirming it in the window.
        bool _mending;
        int _mendMember = -1;
        int _mendCell = -1;

        /// <summary>The scroll of the map, for tests: where it shows and how far it goes.</summary>
        public ScrollRect MapScroll => _mapScroll;

        /// <summary>The view of a node of the map.</summary>
        public MapNodeView NodeView(int nodeId)
        {
            return _nodes[nodeId];
        }

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
            _rest.onClick.AddListener(OnRest);
            _mend.onClick.AddListener(OnMend);
            _mendBack.onClick.AddListener(OnMendBack);
            _mendConfirm.onClick.AddListener(OnMendConfirm);
            _inventoryToggle.onClick.AddListener(OnInventoryToggle);
            _party.Open(_art);
            Managers.Sound.PlayMusic(MusicTrack.Dungeon);
            SelectTheOnlyWay();
            ScrollToCurrentFloor();
        }

        /// <summary>With a single way forward there is nothing to choose: select it.</summary>
        void SelectTheOnlyWay()
        {
            IReadOnlyList<MapNode> available = Managers.Expedition.AvailableNodes();
            _selectedNodeId = available.Count == 1 ? available[0].Id : -1;
        }

        public override void Refresh()
        {
            ExpeditionManager manager = Managers.Expedition;
            ExpeditionState expedition = manager.Expedition;
            StaticData data = Managers.Data.Data;

            _dungeon.text = UiText.Name(data.Dungeons.Get(expedition.DungeonId).Name);
            int currentFloor = CurrentFloor(expedition);
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

                _nodes[node.Id].Show(KindText(node.Kind), node.Kind, state, node.Id == _selectedNodeId);
            }

            // At a camp, the panel only points at the camp's window; the window says the rest. While an item is being chosen to
            // mend, the boards serve that choice: the cells that can be mended take the click, the chosen one is the brass cell,
            // and the inventory stays out of it.
            bool atCamp = manager.Phase == GamePhase.Camp;
            _mending &= atCamp;
            _camp.SetActive(atCamp);
            _enter.gameObject.SetActive(!atCamp);
            _inventoryToggle.gameObject.SetActive(!_mending);
            _party.ToInventory.gameObject.SetActive(!_mending);
            _party.ExternalCanPlace = _mending ? (member, cell) => manager.CanUpgradeAtCamp(member, cell) : (Func<int, int, bool>)null;
            _party.CellClickOverride = _mending ? ChooseMendItem : (Func<int, int, bool>)null;
            _party.ExternalMerges = null;
            _party.ExternalSelectedMember = _mending ? _mendMember : -1;
            _party.ExternalSelectedCell = _mending ? _mendCell : -1;
            _party.DetailOverride = _mending ? UiStrings.Get(UiKeys.Map.MendHint) : null;
            if (atCamp)
            {
                MapNode camp = expedition.Map.Get(expedition.CurrentNodeId);
                _nodeTitle.text = UiStrings.Get(UiKeys.Map.NodeTitle, camp.Floor, KindText(camp.Kind));
                _nodeHint.text = UiStrings.Get(UiKeys.Map.AtCamp);
                _campTitle.text = _mending ? UiStrings.Get(UiKeys.Map.MendTitle, camp.Floor) : _nodeTitle.text;
                _campHint.text = UiStrings.Get(_mending ? UiKeys.Map.MendHint : UiKeys.Map.CampChoose);
                _campChoices.SetActive(!_mending);
                _campMend.SetActive(_mending);
                _restBody.text = UiStrings.Get(UiKeys.Map.RestHeal, data.Balance.CampHealPercent) + "\n"
                    + UiStrings.Get(UiKeys.Map.RestFatigue, data.Balance.CampFatigueRelief);
                RefreshMendPlate(manager, data.Balance);
            }
            else
            {
                MapNode selected = _selectedNodeId >= 0 ? expedition.Map.Get(_selectedNodeId) : null;
                _enter.interactable = selected != null;
                _enterLabel.text = UiStrings.Get(selected != null && selected.Kind == MapNodeKind.Camp ? UiKeys.Map.EnterCamp : UiKeys.Map.Enter);
                _nodeTitle.text = selected != null
                    ? UiStrings.Get(UiKeys.Map.NodeTitle, selected.Floor, KindText(selected.Kind))
                    : UiStrings.Get(UiKeys.Map.SelectNode);
                _nodeHint.text = UiStrings.Get(HintKey(selected));
            }

            _inventoryToggleLabel.text = UiStrings.Get(_party.InventoryOpen ? UiKeys.Board.InventoryHide : UiKeys.Board.InventoryShow);
            _party.Refresh();
        }

        /// <summary>The chosen item's change on the mend plate: its tier before and after, and each effect's size before and after.</summary>
        void RefreshMendPlate(ExpeditionManager manager, BalanceData balance)
        {
            bool chosen = _mending && _mendMember >= 0 && manager.CanUpgradeAtCamp(_mendMember, _mendCell);
            _mendPlate.SetActive(chosen);
            _mendConfirm.interactable = chosen;
            if (!chosen)
            {
                return;
            }

            List<EquippedItem> board = manager.Expedition.Members[_mendMember].Items;
            EquippedItem item = board[ItemBoard.IndexAtCell(board, _mendCell)];
            EquippedItem mended = item.TierUp();
            _mendIcon.sprite = _art.OfItem(item.Item.Id);
            _mendIcon.enabled = _mendIcon.sprite != null;
            _mendChange.text = UiText.Name(item.Item.Name) + "  " + UiText.Change(UiText.TierWord(item.Tier), UiText.TierWord(mended.Tier));
            var lines = new List<string>();
            foreach (ItemEffect effect in item.Item.Effects)
            {
                lines.Add(UiText.Change(UiText.Effect(effect, item.Magnitude(balance, effect)), mended.Magnitude(balance, effect).ToString(CultureInfo.InvariantCulture)));
            }

            _mendEffects.text = string.Join("\n", lines);
        }

        static int CurrentFloor(ExpeditionState expedition)
        {
            return expedition.CurrentNodeId < 0 ? 0 : expedition.Map.Get(expedition.CurrentNodeId).Floor;
        }

        static string KindText(MapNodeKind kind)
        {
            switch (kind)
            {
                case MapNodeKind.Boss: return UiStrings.Get(UiKeys.Map.Boss);
                case MapNodeKind.Elite: return UiStrings.Get(UiKeys.Map.Elite);
                case MapNodeKind.Camp: return UiStrings.Get(UiKeys.Map.Camp);
                default: return UiStrings.Get(UiKeys.Map.Battle);
            }
        }

        /// <summary>The panel's hint for the chosen node: who waits stays unknown, but an elite is known to be strong and a camp to be safe.</summary>
        static string HintKey(MapNode selected)
        {
            if (selected == null)
            {
                return UiKeys.Map.Unknown;
            }

            switch (selected.Kind)
            {
                case MapNodeKind.Elite: return UiKeys.Map.EliteHint;
                case MapNodeKind.Camp: return UiKeys.Map.CampHint;
                default: return UiKeys.Map.Unknown;
            }
        }

        /// <summary>
        /// Lays the nodes out floor by floor, first floor at the bottom over <see cref="BottomMargin"/>, <see cref="FloorSpacing"/>
        /// apart (a map short enough to fit is spread over the whole view), and draws the paths between them.
        /// </summary>
        void BuildMap(NodeMap map)
        {
            float viewHeight = _mapScroll.viewport.rect.height;
            float margin = BottomMargin(viewHeight);
            _floorSpacing = Mathf.Max(FloorSpacing, (viewHeight - margin) / map.FloorCount);
            _mapArea.sizeDelta = new Vector2(_mapArea.sizeDelta.x, margin + map.FloorCount * _floorSpacing);
            float width = _mapArea.rect.width;

            var positions = new Dictionary<int, Vector2>();
            for (int floor = 1; floor <= map.FloorCount; floor++)
            {
                List<MapNode> nodes = map.OnFloor(floor);
                foreach (MapNode node in nodes)
                {
                    float x = (node.Column + 0.5f) / nodes.Count * width;
                    float y = margin + (floor - 0.5f) * _floorSpacing;
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

        /// <summary>
        /// The room under the first floor: the names of its nodes, and as much more as puts the top of the view through the
        /// middle of a floor's nodes while the map is at its bottom, so that no names stand at the top without their nodes.
        /// </summary>
        static float BottomMargin(float viewHeight)
        {
            float margin = viewHeight % FloorSpacing - FloorSpacing / 2f;
            while (margin < NameRoom)
            {
                margin += FloorSpacing;
            }

            return margin;
        }

        /// <summary>
        /// Scrolls the map so that the floor the party stands on is where the first floor is at the start: a part of the floor
        /// it came from shows under it, and the top of the view passes through the middle of a floor's nodes further up.
        /// </summary>
        void ScrollToCurrentFloor()
        {
            float range = _mapArea.rect.height - _mapScroll.viewport.rect.height;
            int floor = Mathf.Max(1, CurrentFloor(Managers.Expedition.Expedition));
            _mapScroll.StopMovement();
            _mapScroll.verticalNormalizedPosition = range <= 0f ? 0f : Mathf.Clamp01((floor - 1) * _floorSpacing / range);
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
            Managers.Sound.PlayEffect(SoundEffect.NodeMove);
            if (Managers.Expedition.Phase != GamePhase.Camp)
            {
                GoToCurrentPhase();
                return;
            }

            // A camp is on this screen: its window opens over the map, which follows the party up a floor.
            _selectedNodeId = -1;
            ScrollToCurrentFloor();
            Refresh();
        }

        void OnRest()
        {
            Managers.Expedition.RestAtCamp();
            SelectTheOnlyWay();
            Refresh();
        }

        /// <summary>The mend card: the window turns to the mend step and the boards wait for the item to raise. The inventory popup closes.</summary>
        void OnMend()
        {
            _mending = true;
            _mendMember = -1;
            _mendCell = -1;
            if (_party.InventoryOpen)
            {
                _party.ToggleInventory();
            }

            _party.ClearSelection();
            Refresh();
        }

        void OnMendBack()
        {
            _mending = false;
            _mendMember = -1;
            _mendCell = -1;
            Refresh();
        }

        /// <summary>A click on a board cell while mending chooses the item there, if it can be mended; the click sounds here.</summary>
        bool ChooseMendItem(int member, int cell)
        {
            if (Managers.Expedition.CanUpgradeAtCamp(member, cell))
            {
                _mendMember = member;
                _mendCell = cell;
            }

            Managers.Sound.PlayEffect(SoundEffect.Button);
            Refresh();
            return true;
        }

        /// <summary>Mends the chosen item (a tier up) and sounds it as put in; the party goes on to the next floor.</summary>
        void OnMendConfirm()
        {
            ExpeditionManager manager = Managers.Expedition;
            if (!_mending || !manager.CanUpgradeAtCamp(_mendMember, _mendCell))
            {
                return;
            }

            manager.UpgradeAtCamp(_mendMember, _mendCell);
            Managers.Sound.PlayEffect(SoundEffect.ItemPlace);
            _mending = false;
            _mendMember = -1;
            _mendCell = -1;
            SelectTheOnlyWay();
            Refresh();
        }
    }
}
