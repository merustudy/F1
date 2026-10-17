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
    /// Where to go (Round 71 E2, Docs/Design/04_Lobby_100Day_Economy.md §7): the painted map of the lands with a marker per dungeon,
    /// the chosen dungeon's details, the party on the strip, and the departure. [출발] opens the party popup over the map (Round 75 B,
    /// 2026-10-17): the party is formed there — on the stage at the left the members stand as the node map's party side stands them,
    /// with the moves that trade rows under their feet; the roster's list at the right puts mercenaries in and takes them out (Round 76
    /// D) — and the popup's own [출발] sets out. Past the last day only the final dungeon can be chosen: the final stand
    /// (Docs/Design/04 §2). The lobby opens this screen; it goes back to the lobby or on to the node map.
    /// </summary>
    public sealed class ExpeditionScreen : UIScreen
    {
        /// <summary>The painting (2304x1536) scaled to the screen's height sits at the left; its dark right third lies under the details.</summary>
        public const float MapWidth = 1620f;
        public const float MapHeight = 1080f;

        /// <summary>The company's town on the painting, as percents of its width and height (the home marker is not a dungeon's).</summary>
        public const float HomeXPercent = 12.5f;
        public const float HomeYPercent = 69.6f;

        [SerializeField] TMP_Text _day;
        [SerializeField] TMP_Text _gold;
        [SerializeField] Button _toLobby;
        [SerializeField] Image _map;
        [SerializeField] DungeonMarkerView _markerTemplate;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _affinity;
        [SerializeField] TMP_Text _floors;
        [SerializeField] TMP_Text _days;
        [SerializeField] TMP_Text _potions;
        [SerializeField] TMP_Text _cleared;
        [SerializeField] TMP_Text _reward;
        [SerializeField] TMP_Text _hint;
        [SerializeField] ExpeditionPartyCardView[] _cards;
        [SerializeField] TMP_Text _status;
        [SerializeField] Button _depart;

        // The party popup (Round 75 B; the stage and the list, Round 76 D).
        [SerializeField] GameObject _shade;
        [SerializeField] GameObject _partyWindow;
        [SerializeField] TMP_Text _partyDungeon;
        [SerializeField] PartyRowSlotView[] _slots;
        [SerializeField] TMP_Text _rosterHeader;
        [SerializeField] PartyListRowView _rowTemplate;
        [SerializeField] GameObject _emptyTemplate;
        [SerializeField] TMP_Text _partyStatus;
        [SerializeField] Button _partyDepart;
        [SerializeField] Button _partyClose;

        readonly List<DungeonMarkerView> _markers = new List<DungeonMarkerView>();
        readonly Dictionary<string, PartyListRowView> _rows = new Dictionary<string, PartyListRowView>();
        readonly List<GameObject> _emptySlots = new List<GameObject>();
        readonly Dictionary<string, Sprite> _figures = new Dictionary<string, Sprite>();
        Sprite _mapArt;
        string _selectedId;
        bool _partyOpen;

        public string SelectedDungeonId => _selectedId;

        /// <summary>True while the party popup is over the map.</summary>
        public bool IsPartyOpen => _partyOpen;

        /// <summary>The empty lines of the popup's list: the roster's room up to its limit.</summary>
        public int EmptySlotCount => _emptySlots.Count;

        /// <summary>
        /// The map and the jobs' figures (for the party popup's stage and list, Round 76 D) are loaded before the screen opens, into
        /// the lobby's scope: the lobby and this screen share it. The figures are the unit art of the expedition's scope, loaded here
        /// by address as well (Docs/Architecture/04 "Scope").
        /// </summary>
        public override async Task PrepareAsync()
        {
            _mapArt = await Managers.Resource.LoadAsync<Sprite>(ArtAddress.ExpeditionMap, ResourceScope.Lobby);
            foreach (JobData job in Managers.Data.Data.Jobs.Ordered)
            {
                if (job.Figure != null)
                {
                    _figures[job.Id] = await Managers.Resource.LoadAsync<Sprite>(job.Figure, ResourceScope.Lobby);
                }
            }
        }

        protected override void OnOpen()
        {
            _map.sprite = _mapArt;
            _toLobby.onClick.AddListener(() => GoTo(ScreenId.Lobby));
            _depart.onClick.AddListener(OpenParty);
            _partyClose.onClick.AddListener(CloseParty);
            _partyDepart.onClick.AddListener(OnDepart);
            for (int i = 0; i < _slots.Length; i++)
            {
                int row = i + 1;
                PartyRowSlotView slot = _slots[i];
                slot.Forward.onClick.AddListener(() => MoveInParty(slot.MercenaryId, row - 1));
                slot.Back.onClick.AddListener(() => MoveInParty(slot.MercenaryId, row + 1));
            }

            StaticData data = Managers.Data.Data;
            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                DungeonMarkerView marker = Instantiate(_markerTemplate, _markerTemplate.transform.parent);
                marker.gameObject.SetActive(true);
                marker.name = "Marker_" + dungeon.Id;
                marker.DungeonId = dungeon.Id;
                marker.PlaceAt(new Vector2(MapWidth * dungeon.MapX / 100f, MapHeight * dungeon.MapY / 100f));
                string id = dungeon.Id;
                marker.Button.onClick.AddListener(() => OnMarker(id));
                _markers.Add(marker);
            }

            _selectedId = DefaultDungeon(data).Id;
            _partyOpen = false;
            Managers.Sound.PlayMusic(MusicTrack.Lobby);
        }

        /// <summary>The final dungeon once the final stand is due, else the first dungeon that is not final.</summary>
        static DungeonData DefaultDungeon(StaticData data)
        {
            if (Managers.Run.IsFinalStandDue && data.FinalDungeon != null)
            {
                return data.FinalDungeon;
            }

            foreach (DungeonData dungeon in data.Dungeons.Ordered)
            {
                if (!dungeon.IsFinal)
                {
                    return dungeon;
                }
            }

            return data.Dungeons.Ordered[0];
        }

        public override void Refresh()
        {
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            RunState run = Managers.Run.Run;
            _day.text = UiStrings.Get(UiKeys.Lobby.Day, run.Day, balance.TotalDays);
            _gold.text = UiStrings.Get(UiKeys.Lobby.Gold, run.Gold);

            foreach (DungeonMarkerView marker in _markers)
            {
                DungeonData dungeon = data.Dungeons.Get(marker.DungeonId);
                DepartCheck check = Managers.Run.CanDepart(dungeon.Id);
                bool closed = check == DepartCheck.Locked || check == DepartCheck.FinalStandDue;
                run.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
                string name = dungeon.IsFinal
                    ? UiStrings.Get(UiKeys.Expedition.FinalName, UiText.Name(dungeon.Name))
                    : UiText.Name(dungeon.Name);
                string detail = check == DepartCheck.Locked
                    ? UiStrings.Get(UiKeys.Expedition.MarkerLocked, UiText.Name(data.Dungeons.Get(dungeon.UnlockDungeonId).Name), dungeon.UnlockClears)
                    : UiStrings.Get(UiKeys.Expedition.MarkerOpen, cleared, dungeon.DurationDays);
                marker.Show(name, detail, closed, dungeon.Id == _selectedId);
            }

            RefreshDetails(data, run);
            RefreshParty(data, run);

            DepartCheck chosen = Managers.Run.CanDepart(_selectedId);
            bool finalStand = chosen == DepartCheck.Ok && Managers.Run.IsFinalStandDue;
            // [출발] opens the party popup (Round 75 B), so a party still short does not keep it off: the popup fills the party.
            _depart.interactable = chosen == DepartCheck.Ok || chosen == DepartCheck.PartyTooSmall;
            string status = finalStand ? UiStrings.Get(UiKeys.Lobby.FinalStandDue) : StatusText(chosen, balance);
            Color statusColor = chosen != DepartCheck.Ok ? UiPalette.Burn : finalStand ? UiPalette.Virtue : UiPalette.Good;
            _status.text = status;
            _status.color = statusColor;

            RefreshPartyWindow(data, run, chosen, status, statusColor);
            _shade.SetActive(_partyOpen);
            _partyWindow.SetActive(_partyOpen);
        }

        /// <summary>
        /// The chosen dungeon: its rows, and the hints under them. An open dungeon says which dungeons its clears open and that the
        /// final dungeon's boss waits past the last day; the final dungeon says what it is.
        /// </summary>
        void RefreshDetails(StaticData data, RunState run)
        {
            DungeonData dungeon = data.Dungeons.Get(_selectedId);
            _name.text = UiText.Name(dungeon.Name);
            _affinity.text = UiText.Name(data.Affinities.Get(dungeon.AffinityId).Name);
            _floors.text = UiStrings.Get(UiKeys.Expedition.Floors, dungeon.Floors);
            _days.text = UiStrings.Get(UiKeys.Expedition.Days, dungeon.DurationDays, run.Day + dungeon.DurationDays);
            _potions.text = PotionsText(data, dungeon);
            run.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
            _cleared.text = UiStrings.Get(UiKeys.Expedition.ClearedCount, cleared);
            // What the settlement pays (stage 26): the same for every dungeon until a dungeon has rewards of its own.
            _reward.text = UiStrings.Get(UiKeys.Expedition.Reward, data.Balance.GoldPerFloor, data.Balance.GoldClear);

            var hints = new List<string>();
            if (dungeon.IsFinal)
            {
                hints.Add(UiStrings.Get(UiKeys.Expedition.FinalHint));
            }
            else
            {
                foreach (DungeonData other in data.Dungeons.Ordered)
                {
                    if (other.UnlockDungeonId == dungeon.Id)
                    {
                        hints.Add(UiStrings.Get(UiKeys.Expedition.UnlockHint, UiText.Name(other.Name), other.UnlockClears));
                    }
                }

                if (data.FinalDungeon != null)
                {
                    hints.Add(UiStrings.Get(UiKeys.Expedition.FinalStandHint, data.Balance.TotalDays, UiText.Name(data.FinalDungeon.Name)));
                }
            }

            _hint.text = string.Join("\n", hints);
        }

        /// <summary>The starting potions by kind, each with its count: "치유 포션 ×2".</summary>
        static string PotionsText(StaticData data, DungeonData dungeon)
        {
            var kinds = new List<string>();
            var counts = new Dictionary<string, int>();
            foreach (string potionId in dungeon.StartingPotions)
            {
                if (!counts.ContainsKey(potionId))
                {
                    kinds.Add(potionId);
                    counts[potionId] = 0;
                }

                counts[potionId]++;
            }

            var parts = new List<string>();
            foreach (string potionId in kinds)
            {
                parts.Add(UiStrings.Get(UiKeys.Expedition.Potions, UiText.Name(data.Potions.Get(potionId).Name), counts[potionId]));
            }

            return string.Join(", ", parts);
        }

        /// <summary>One card per row the party can stand in, as the party is formed: the name and the level (stage 27), the job with the member's state, the fatigue pips. An empty row says so.</summary>
        void RefreshParty(StaticData data, RunState run)
        {
            BalanceData balance = data.Balance;
            for (int i = 0; i < _cards.Length; i++)
            {
                int row = i + 1;
                _cards[i].gameObject.SetActive(i < balance.PartySize);
                PartySlot slot = SlotInRow(run, row);
                if (slot == null)
                {
                    _cards[i].ShowEmpty(UiStrings.Get(UiKeys.Lobby.EmptyRow));
                    continue;
                }

                MercenaryState mercenary = RunRules.FindMercenary(run, slot.MercenaryId);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                _cards[i].Show(
                    UiText.Mercenary(slot.MercenaryId),
                    UiStrings.Get(UiKeys.Lobby.Level, mercenary.Level),
                    UiText.JobLine(mercenary.JobId, affliction),
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown);
            }
        }

        // ---- The party popup (Round 75 B; the stage and the list, Round 76 D) ---------------

        /// <summary>
        /// The popup: the chosen dungeon's line; on the stage a slot per row (the member's figure with its moves under the feet, or
        /// the empty place and its hint); in the list a line per mercenary of the roster (lit and tagged with its row while in the
        /// party, its button putting it in or taking it out) and an empty line per place left up to the roster's limit; the status and
        /// the departure, which only a party that can set out enables.
        /// </summary>
        void RefreshPartyWindow(StaticData data, RunState run, DepartCheck chosen, string status, Color statusColor)
        {
            BalanceData balance = data.Balance;
            DungeonData dungeon = data.Dungeons.Get(_selectedId);
            string dungeonName = dungeon.IsFinal ? UiStrings.Get(UiKeys.Expedition.FinalName, UiText.Name(dungeon.Name)) : UiText.Name(dungeon.Name);
            _partyDungeon.text = UiStrings.Get(UiKeys.Expedition.PartyDungeon, dungeonName, dungeon.DurationDays, run.Day + dungeon.DurationDays);

            for (int i = 0; i < _slots.Length; i++)
            {
                int row = i + 1;
                _slots[i].gameObject.SetActive(i < balance.PartySize);
                PartySlot slot = SlotInRow(run, row);
                if (slot == null)
                {
                    _slots[i].ShowEmpty(UiStrings.Get(UiKeys.Expedition.EmptyRowHint));
                    continue;
                }

                MercenaryState mercenary = RunRules.FindMercenary(run, slot.MercenaryId);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                _slots[i].Show(
                    slot.MercenaryId,
                    FigureOf(mercenary.JobId),
                    UiText.Mercenary(slot.MercenaryId),
                    UiStrings.Get(UiKeys.Lobby.Level, mercenary.Level),
                    UiText.JobLine(mercenary.JobId, affliction),
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown,
                    canForward: Managers.Run.CanPlaceInParty(slot.MercenaryId, row - 1),
                    canBack: Managers.Run.CanPlaceInParty(slot.MercenaryId, row + 1));
            }

            var alive = new HashSet<string>();
            int next = run.Party.Count + 1;
            foreach (MercenaryState mercenary in run.Roster)
            {
                alive.Add(mercenary.Id);
                if (!_rows.TryGetValue(mercenary.Id, out PartyListRowView line))
                {
                    line = Instantiate(_rowTemplate, _rowTemplate.transform.parent);
                    line.gameObject.SetActive(true);
                    line.name = "Row_" + mercenary.Id;
                    line.MercenaryId = mercenary.Id;
                    string id = mercenary.Id;
                    line.Button.onClick.AddListener(() => OnRowButton(id));
                    _rows.Add(mercenary.Id, line);
                }

                int row = RowOf(run, mercenary.Id);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                JobData job = data.Jobs.Get(mercenary.JobId);
                line.Show(
                    FigureOf(mercenary.JobId),
                    UiText.Mercenary(mercenary.Id),
                    UiText.JobLine(mercenary.JobId, affliction),
                    affliction != null,
                    UiStrings.Get(UiKeys.Lobby.Level, mercenary.Level),
                    UiText.Passive(job),
                    row == 0 ? null : UiStrings.Get(UiText.RowKey(row)),
                    row != 0,
                    row == 0 && Managers.Run.CanPlaceInParty(mercenary.Id, next),
                    UiStrings.Get(row != 0 ? UiKeys.Expedition.Leave : UiKeys.Expedition.Join),
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown);
                line.transform.SetAsLastSibling();
            }

            var gone = new List<string>();
            foreach (KeyValuePair<string, PartyListRowView> pair in _rows)
            {
                if (!alive.Contains(pair.Key))
                {
                    gone.Add(pair.Key);
                }
            }

            foreach (string id in gone)
            {
                Destroy(_rows[id].gameObject);
                _rows.Remove(id);
            }

            // The room left in the roster, as empty lines after the mercenaries' (the coach fills them).
            int empties = Mathf.Max(0, balance.RosterLimit - run.Roster.Count);
            while (_emptySlots.Count < empties)
            {
                GameObject slot = Instantiate(_emptyTemplate, _emptyTemplate.transform.parent);
                slot.name = "EmptySlot" + (_emptySlots.Count + 1);
                _emptySlots.Add(slot);
            }

            while (_emptySlots.Count > empties)
            {
                Destroy(_emptySlots[_emptySlots.Count - 1]);
                _emptySlots.RemoveAt(_emptySlots.Count - 1);
            }

            foreach (GameObject slot in _emptySlots)
            {
                slot.SetActive(true);
                slot.transform.SetAsLastSibling();
            }

            _rosterHeader.text = UiStrings.Get(UiKeys.Expedition.RosterHeader, run.Roster.Count, balance.RosterLimit);
            _partyStatus.text = status;
            _partyStatus.color = statusColor;
            _partyDepart.interactable = chosen == DepartCheck.Ok;
        }

        /// <summary>A job's figure, loaded before the screen opened; null for a job without one.</summary>
        Sprite FigureOf(string jobId)
        {
            return _figures.TryGetValue(jobId, out Sprite art) ? art : null;
        }

        static PartySlot SlotInRow(RunState run, int row)
        {
            foreach (PartySlot candidate in run.Party)
            {
                if (candidate.Row == row)
                {
                    return candidate;
                }
            }

            return null;
        }

        /// <summary>The row a mercenary holds in the party, or 0 outside it.</summary>
        static int RowOf(RunState run, string mercenaryId)
        {
            foreach (PartySlot slot in run.Party)
            {
                if (slot.MercenaryId == mercenaryId)
                {
                    return slot.Row;
                }
            }

            return 0;
        }

        static string StatusText(DepartCheck check, BalanceData balance)
        {
            switch (check)
            {
                case DepartCheck.Ok: return UiStrings.Get(UiKeys.Lobby.DepartOk);
                case DepartCheck.PartyTooSmall: return UiStrings.Get(UiKeys.Lobby.PartyTooSmall, balance.MinPartySize);
                case DepartCheck.Locked: return UiStrings.Get(UiKeys.Expedition.Locked);
                case DepartCheck.FinalStandDue: return UiStrings.Get(UiKeys.Lobby.FinalStandDue);
                default: return UiStrings.Get(UiKeys.Lobby.RunOver);
            }
        }

        void OnMarker(string dungeonId)
        {
            DepartCheck check = Managers.Run.CanDepart(dungeonId);
            if (check == DepartCheck.Locked || check == DepartCheck.FinalStandDue)
            {
                return;
            }

            _selectedId = dungeonId;
            Refresh();
        }

        void OpenParty()
        {
            DepartCheck check = Managers.Run.CanDepart(_selectedId);
            if (check != DepartCheck.Ok && check != DepartCheck.PartyTooSmall)
            {
                return;
            }

            _partyOpen = true;
            Refresh();
        }

        void CloseParty()
        {
            _partyOpen = false;
            Refresh();
        }

        /// <summary>A line's button: in the party, its mercenary comes out; outside it, the mercenary takes the next row from the front (the button is off once the party is full).</summary>
        void OnRowButton(string mercenaryId)
        {
            RunState run = Managers.Run.Run;
            if (RowOf(run, mercenaryId) != 0)
            {
                Managers.Run.RemoveFromParty(mercenaryId);
            }
            else
            {
                int next = run.Party.Count + 1;
                if (!Managers.Run.CanPlaceInParty(mercenaryId, next))
                {
                    return;
                }

                Managers.Run.PlaceInParty(mercenaryId, next);
            }

            Refresh();
        }

        /// <summary>A move: the member trades places with whoever stands in that row (the buttons are off when nobody does).</summary>
        void MoveInParty(string mercenaryId, int row)
        {
            if (mercenaryId == null || !Managers.Run.CanPlaceInParty(mercenaryId, row))
            {
                return;
            }

            Managers.Run.PlaceInParty(mercenaryId, row);
            Refresh();
        }

        /// <summary>The popup's [출발]: the party as formed sets out for the chosen dungeon.</summary>
        void OnDepart()
        {
            if (Managers.Run.CanDepart(_selectedId) != DepartCheck.Ok)
            {
                return;
            }

            Managers.Expedition.Depart(_selectedId);
            Managers.Sound.PlayEffect(SoundEffect.Depart);
            GoToCurrentPhase();
        }
    }
}
