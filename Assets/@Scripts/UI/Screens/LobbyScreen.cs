using System.Collections.Generic;
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
    /// The lobby: the day, the roster with fatigue, choosing the party and its rows, resting and
    /// departing. When no mercenary is left it shows that the run is over.
    /// </summary>
    public sealed class LobbyScreen : UIScreen
    {
        [SerializeField] TMP_Text _day;
        [SerializeField] Button _toTitle;
        [SerializeField] RosterEntryView _entryTemplate;
        [SerializeField] Transform _entryParent;
        [SerializeField] TMP_Text _fallen;
        [SerializeField] TMP_Text _dungeonName;
        [SerializeField] TMP_Text _dungeonAffinity;
        [SerializeField] TMP_Text _dungeonCost;
        [SerializeField] TMP_Text _dungeonCleared;
        [SerializeField] GameObject[] _partyRows;
        [SerializeField] TMP_Text[] _rowNames;
        [SerializeField] TMP_Text _departStatus;
        [SerializeField] Button _rest;
        [SerializeField] TMP_Text _restLabel;
        [SerializeField] Button _depart;
        [SerializeField] GameObject _runOverPanel;
        [SerializeField] TMP_Text _runOverDay;
        [SerializeField] Button _runOverNewRun;

        readonly Dictionary<string, RosterEntryView> _entries = new Dictionary<string, RosterEntryView>();

        /// <summary>Slice A has one dungeon. The lobby offers the first one in the data.</summary>
        static DungeonData Dungeon => Managers.Data.Data.Dungeons.Ordered[0];

        protected override void OnOpen()
        {
            _toTitle.onClick.AddListener(() => GoTo(ScreenId.Title));
            _rest.onClick.AddListener(OnRest);
            _depart.onClick.AddListener(OnDepart);
            _runOverNewRun.onClick.AddListener(OnNewRun);
            Managers.Sound.PlayMusic(MusicTrack.Lobby);
        }

        public override void Refresh()
        {
            StaticData data = Managers.Data.Data;
            BalanceData balance = data.Balance;
            RunState run = Managers.Run.Run;
            DungeonData dungeon = Dungeon;

            _day.text = UiStrings.Get(UiKeys.Lobby.Day, run.Day, balance.TotalDays);
            RefreshRoster(data, run);
            _fallen.text = run.Fallen.Count > 0 ? UiStrings.Get(UiKeys.Lobby.Fallen, UiText.MercenaryList(run.Fallen)) : string.Empty;

            _dungeonName.text = UiText.Name(dungeon.Name);
            _dungeonAffinity.text = UiStrings.Get(UiKeys.Lobby.Affinity, UiText.Name(data.Affinities.Get(dungeon.AffinityId).Name));
            _dungeonCost.text = UiStrings.Get(UiKeys.Lobby.Cost, dungeon.FatigueCost, dungeon.DurationDays);
            run.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
            _dungeonCleared.text = UiStrings.Get(UiKeys.Lobby.Cleared, cleared);

            // One line per row the party can stand in: a side has more rows than the party has members.
            for (int i = 0; i < _rowNames.Length; i++)
            {
                _partyRows[i].SetActive(i < balance.PartySize);
                _rowNames[i].text = RowNames(run, i + 1);
            }

            DepartCheck check = Managers.Run.CanDepart(dungeon.Id);
            _depart.interactable = check == DepartCheck.Ok;
            _departStatus.text = DepartText(check, balance);
            _departStatus.color = check == DepartCheck.Ok ? UiPalette.Good : UiPalette.Burn;

            _rest.interactable = !run.IsOver;
            _restLabel.text = UiStrings.Get(UiKeys.Lobby.Rest, balance.FatigueRecoveryPerDay * balance.RestDays, balance.RestDays);

            _runOverPanel.SetActive(run.IsOver);
            _runOverDay.text = UiStrings.Get(UiKeys.Lobby.RunOverDay, run.Day);
        }

        void RefreshRoster(StaticData data, RunState run)
        {
            // The roster only shrinks, so entries of the dead are removed and the rest are reused.
            var alive = new HashSet<string>();
            foreach (MercenaryState mercenary in run.Roster)
            {
                alive.Add(mercenary.Id);
                if (!_entries.TryGetValue(mercenary.Id, out RosterEntryView entry))
                {
                    entry = Instantiate(_entryTemplate, _entryParent);
                    entry.gameObject.SetActive(true);
                    _entries.Add(mercenary.Id, entry);

                    string id = mercenary.Id;
                    for (int i = 0; i < entry.Rows.Count; i++)
                    {
                        int row = i + 1;
                        entry.Rows[i].onClick.AddListener(() => PutInRow(id, row));
                    }

                    entry.Remove.onClick.AddListener(() => RemoveFromParty(id));
                }

                // Which rows the mercenary can take is a rule; the lobby only asks.
                var canTake = new bool[Mathf.Min(entry.Rows.Count, data.Balance.PartySize)];
                for (int i = 0; i < canTake.Length; i++)
                {
                    canTake[i] = Managers.Run.CanPlaceInParty(mercenary.Id, i + 1);
                }

                JobData job = data.Jobs.Get(mercenary.JobId);
                entry.Show(
                    UiText.Mercenary(mercenary.Id),
                    UiText.Name(job.Name),
                    UiText.Passive(job),
                    mercenary.Fatigue,
                    data.Balance.MaxFatigue,
                    RowOf(run, mercenary.Id),
                    canTake);
            }

            var gone = new List<string>();
            foreach (string id in _entries.Keys)
            {
                if (!alive.Contains(id))
                {
                    gone.Add(id);
                }
            }

            foreach (string id in gone)
            {
                Destroy(_entries[id].gameObject);
                _entries.Remove(id);
            }
        }

        /// <summary>The row the mercenary holds in the party, or 0 when it is not in the party.</summary>
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

        static string RowNames(RunState run, int row)
        {
            var ids = new List<string>();
            foreach (PartySlot slot in run.Party)
            {
                if (slot.Row == row)
                {
                    ids.Add(slot.MercenaryId);
                }
            }

            return ids.Count == 0 ? UiStrings.Get(UiKeys.Lobby.EmptyRow) : UiText.MercenaryList(ids);
        }

        static string DepartText(DepartCheck check, BalanceData balance)
        {
            switch (check)
            {
                case DepartCheck.Ok: return UiStrings.Get(UiKeys.Lobby.DepartOk);
                case DepartCheck.PartyTooSmall: return UiStrings.Get(UiKeys.Lobby.PartyTooSmall, balance.MinPartySize);
                case DepartCheck.NotEnoughFatigue: return UiStrings.Get(UiKeys.Lobby.NotEnoughFatigue);
                default: return UiStrings.Get(UiKeys.Lobby.RunOver);
            }
        }

        /// <summary>Joins the party in that row, or trades places with the mercenary standing there.</summary>
        void PutInRow(string mercenaryId, int row)
        {
            if (Managers.Run.CanPlaceInParty(mercenaryId, row))
            {
                Managers.Run.PlaceInParty(mercenaryId, row);
                Refresh();
            }
        }

        void RemoveFromParty(string mercenaryId)
        {
            Managers.Run.RemoveFromParty(mercenaryId);
            Refresh();
        }

        void OnRest()
        {
            Managers.Run.Rest();
            Refresh();
        }

        void OnDepart()
        {
            if (Managers.Run.CanDepart(Dungeon.Id) != DepartCheck.Ok)
            {
                return;
            }

            Managers.Expedition.Depart(Dungeon.Id);
            Managers.Sound.PlayEffect(SoundEffect.Depart);
            GoToCurrentPhase();
        }

        void OnNewRun()
        {
            Managers.Run.StartNewRun();

            // The roster of the old run is gone: start the list over.
            foreach (RosterEntryView entry in _entries.Values)
            {
                Destroy(entry.gameObject);
            }

            _entries.Clear();
            Refresh();
        }
    }
}
