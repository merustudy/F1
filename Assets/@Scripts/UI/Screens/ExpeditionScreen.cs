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
    /// the chosen dungeon's details, the party as the lobby formed it (read only here), and the departure. Past the last day only the
    /// final dungeon can be chosen: the final stand (Docs/Design/04 §2). The lobby opens this screen; it goes back to the lobby or on
    /// to the node map.
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
        [SerializeField] Button _toLobby;
        [SerializeField] Image _map;
        [SerializeField] DungeonMarkerView _markerTemplate;
        [SerializeField] TMP_Text _name;
        [SerializeField] TMP_Text _affinity;
        [SerializeField] TMP_Text _floors;
        [SerializeField] TMP_Text _days;
        [SerializeField] TMP_Text _potions;
        [SerializeField] TMP_Text _cleared;
        [SerializeField] TMP_Text _hint;
        [SerializeField] ExpeditionPartyCardView[] _cards;
        [SerializeField] TMP_Text _status;
        [SerializeField] Button _depart;

        readonly List<DungeonMarkerView> _markers = new List<DungeonMarkerView>();
        Sprite _mapArt;
        string _selectedId;

        public string SelectedDungeonId => _selectedId;

        /// <summary>The map is loaded before the screen opens, into the lobby's scope: the lobby and this screen share it.</summary>
        public override async Task PrepareAsync()
        {
            _mapArt = await Managers.Resource.LoadAsync<Sprite>(ArtAddress.ExpeditionMap, ResourceScope.Lobby);
        }

        protected override void OnOpen()
        {
            _map.sprite = _mapArt;
            _toLobby.onClick.AddListener(() => GoTo(ScreenId.Lobby));
            _depart.onClick.AddListener(OnDepart);

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
            _depart.interactable = chosen == DepartCheck.Ok;
            _status.text = finalStand ? UiStrings.Get(UiKeys.Lobby.FinalStandDue) : StatusText(chosen, balance);
            _status.color = chosen != DepartCheck.Ok ? UiPalette.Burn : finalStand ? UiPalette.Virtue : UiPalette.Good;
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

        /// <summary>One card per row the party can stand in, as the lobby formed it: the name, the job with the member's state, the fatigue pips. An empty row says so.</summary>
        void RefreshParty(StaticData data, RunState run)
        {
            BalanceData balance = data.Balance;
            for (int i = 0; i < _cards.Length; i++)
            {
                int row = i + 1;
                _cards[i].gameObject.SetActive(i < balance.PartySize);
                PartySlot slot = null;
                foreach (PartySlot candidate in run.Party)
                {
                    if (candidate.Row == row)
                    {
                        slot = candidate;
                    }
                }

                if (slot == null)
                {
                    _cards[i].ShowEmpty(UiStrings.Get(UiKeys.Lobby.EmptyRow));
                    continue;
                }

                MercenaryState mercenary = RunRules.FindMercenary(run, slot.MercenaryId);
                FatigueStateData affliction = mercenary.AfflictionId == null ? null : data.FatigueStates.Get(mercenary.AfflictionId);
                _cards[i].Show(
                    UiText.Mercenary(slot.MercenaryId),
                    UiText.JobLine(mercenary.JobId, affliction),
                    mercenary.Fatigue,
                    balance.MaxFatigue,
                    balance.FatigueBreakdown);
            }
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
