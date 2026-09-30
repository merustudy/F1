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
        [SerializeField] TMP_Text _frontNames;
        [SerializeField] TMP_Text _rearNames;
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

            _frontNames.text = RowNames(run, BattleRow.Front);
            _rearNames.text = RowNames(run, BattleRow.Rear);

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
                    entry.Front.onClick.AddListener(() => PutInParty(id, BattleRow.Front));
                    entry.Rear.onClick.AddListener(() => PutInParty(id, BattleRow.Rear));
                    entry.Remove.onClick.AddListener(() => RemoveFromParty(id));
                }

                JobData job = data.Jobs.Get(mercenary.JobId);
                BattleRow? row = RowOf(run, mercenary.Id);
                entry.Show(
                    UiText.Mercenary(mercenary.Id),
                    UiText.Name(job.Name),
                    UiText.Passive(job),
                    mercenary.Fatigue,
                    data.Balance.MaxFatigue,
                    row,
                    Managers.Run.PartyProblem(PartyWith(run, mercenary.Id, BattleRow.Front)) == null,
                    Managers.Run.PartyProblem(PartyWith(run, mercenary.Id, BattleRow.Rear)) == null);
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

        static BattleRow? RowOf(RunState run, string mercenaryId)
        {
            foreach (PartySlot slot in run.Party)
            {
                if (slot.MercenaryId == mercenaryId)
                {
                    return slot.Row;
                }
            }

            return null;
        }

        /// <summary>The current party with one mercenary put in a row: moved if already in, added at the end otherwise.</summary>
        static List<PartySlot> PartyWith(RunState run, string mercenaryId, BattleRow row)
        {
            var party = new List<PartySlot>();
            bool found = false;
            foreach (PartySlot slot in run.Party)
            {
                bool isTarget = slot.MercenaryId == mercenaryId;
                found |= isTarget;
                party.Add(new PartySlot { MercenaryId = slot.MercenaryId, Row = isTarget ? row : slot.Row });
            }

            if (!found)
            {
                party.Add(new PartySlot { MercenaryId = mercenaryId, Row = row });
            }

            return party;
        }

        static string RowNames(RunState run, BattleRow row)
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

        void PutInParty(string mercenaryId, BattleRow row)
        {
            List<PartySlot> party = PartyWith(Managers.Run.Run, mercenaryId, row);
            if (Managers.Run.PartyProblem(party) == null)
            {
                Managers.Run.SetParty(party);
                Refresh();
            }
        }

        void RemoveFromParty(string mercenaryId)
        {
            var party = new List<PartySlot>();
            foreach (PartySlot slot in Managers.Run.Run.Party)
            {
                if (slot.MercenaryId != mercenaryId)
                {
                    party.Add(new PartySlot { MercenaryId = slot.MercenaryId, Row = slot.Row });
                }
            }

            Managers.Run.SetParty(party);
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
