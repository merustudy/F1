using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;
using F1.Save;

namespace F1.Flow
{
    /// <summary>
    /// Owns the run (the 100 days) and its save file: creating and loading the run, the lobby
    /// commands and applying the return settlement. Rules are computed by RunRules; this class
    /// checks that a command is allowed now, calls the rule and saves the result.
    ///
    /// run.json is written here and nowhere else. ExpeditionManager hands over its part of the
    /// snapshot, so a change that touches both is confirmed by one atomic write.
    /// </summary>
    public sealed class RunManager
    {
        public const string FileName = "run.json";

        readonly DataManager _data;
        readonly SaveManager _save;
        readonly Func<ulong> _newSeed;
        RunState _run;
        ExpeditionState _loadedExpedition;
        BattleRecord _loadedBattle;
        byte[] _unsavedSnapshot;

        /// <param name="newSeed">Gives the seed of a new run. AppRoot passes an OS random source; tests pass a fixed value.</param>
        public RunManager(DataManager data, SaveManager save, Func<ulong> newSeed)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _save = save ?? throw new ArgumentNullException(nameof(save));
            _newSeed = newSeed ?? throw new ArgumentNullException(nameof(newSeed));
        }

        /// <summary>Raised after a new run replaced whatever was in progress.</summary>
        public event Action RunStarted;

        /// <summary>Raised when saving starts or stops being blocked. The argument is <see cref="IsSaveBlocked"/>.</summary>
        public event Action<bool> SaveBlockedChanged;

        public bool HasRun => _run != null;

        /// <summary>The run state. Read it; only rules change it.</summary>
        public RunState Run => _run ?? throw new InvalidOperationException("There is no run.");

        /// <summary>True from departure until the return settlement.</summary>
        public bool IsAway { get; private set; }

        /// <summary>What <see cref="Load"/> found on disk.</summary>
        public SaveLoadStatus LoadStatus { get; private set; } = SaveLoadStatus.Missing;

        /// <summary>
        /// True while the last change could not be written. Nothing may change until
        /// <see cref="RetrySave"/> succeeds.
        /// </summary>
        public bool IsSaveBlocked => _unsavedSnapshot != null;

        /// <summary>
        /// Reads run.json. Called once at boot, after the static data is loaded. A file that cannot be
        /// read or does not describe a legal state leaves no run; a new one can still be started.
        /// </summary>
        public void Load()
        {
            StaticData data = _data.Data;
            RunState run = null;
            ExpeditionState expedition = null;
            BattleRecord battle = null;

            SaveLoadResult<RunSaveData> loaded = _save.Load<RunSaveData>(FileName, save =>
            {
                try
                {
                    RunSaveMigrator.Migrate(save, data);
                    RunSaveMapper.Read(save, data, out run, out expedition);
                    battle = save.Expedition?.Battle;
                    return true;
                }
                catch (RunSaveException)
                {
                    return false;
                }
            });

            LoadStatus = loaded.Status;
            if (!loaded.HasValue)
            {
                return;
            }

            _run = run;
            IsAway = expedition != null;
            _loadedExpedition = expedition;
            _loadedBattle = battle;
        }

        /// <summary>Starts a run from day one. Anything in progress is dropped.</summary>
        public void StartNewRun()
        {
            RequireWritable();
            _run = RunRules.NewRun(_data.Data, _newSeed());
            IsAway = false;
            _loadedExpedition = null;
            _loadedBattle = null;
            Save(null);
            RunStarted?.Invoke();
        }

        /// <summary>Why a party cannot be set, or null when it is valid.</summary>
        public string PartyProblem(IReadOnlyList<PartySlot> party)
        {
            return RunRules.PartyProblem(_data.Data, Run, party);
        }

        public void SetParty(IReadOnlyList<PartySlot> party)
        {
            RequireAtHome();
            RunRules.SetParty(_data.Data, _run, party);
            Save(null);
        }

        /// <summary>True when the mercenary can take that row of the party now (join it, or trade places inside it).</summary>
        public bool CanPlaceInParty(string mercenaryId, int row)
        {
            return HasRun && !IsAway && RunRules.CanPlaceInParty(_data.Data, _run, mercenaryId, row);
        }

        public void PlaceInParty(string mercenaryId, int row)
        {
            RequireAtHome();
            RunRules.PlaceInParty(_data.Data, _run, mercenaryId, row);
            Save(null);
        }

        public void RemoveFromParty(string mercenaryId)
        {
            RequireAtHome();
            RunRules.RemoveFromParty(_run, mercenaryId);
            Save(null);
        }

        public void Rest()
        {
            RequireAtHome();
            RunRules.Rest(_data.Data, _run);
            Save(null);
        }

        public DepartCheck CanDepart(string dungeonId)
        {
            return RunRules.CanDepart(_data.Data, Run, dungeonId);
        }

        /// <summary>
        /// Writes again the snapshot that could not be saved: the same bytes, nothing newer.
        /// Returns true when saving is no longer blocked.
        /// </summary>
        public bool RetrySave()
        {
            if (_unsavedSnapshot == null)
            {
                return true;
            }

            try
            {
                _save.SaveBytes(FileName, _unsavedSnapshot);
            }
            catch (SaveWriteException)
            {
                return false;
            }

            _unsavedSnapshot = null;
            SaveBlockedChanged?.Invoke(false);
            return true;
        }

        /// <summary>Called by ExpeditionManager. The party leaves; the lobby is closed until the settlement.</summary>
        internal ExpeditionState BeginExpedition(string dungeonId)
        {
            RequireAtHome();
            ExpeditionState expedition = RunRules.BeginExpedition(_data.Data, _run, dungeonId);
            IsAway = true;
            return expedition;
        }

        /// <summary>Called by ExpeditionManager when the expedition ended. Applies the return settlement.</summary>
        internal SettlementReport Settle(ExpeditionState expedition)
        {
            if (!IsAway)
            {
                throw new InvalidOperationException("No expedition is in progress.");
            }

            SettlementReport report = RunRules.Settle(_data.Data, Run, expedition);
            IsAway = false;
            return report;
        }

        /// <summary>
        /// The expedition that was in progress when the file was saved, handed over once to
        /// ExpeditionManager. Null when the party was at home.
        /// </summary>
        internal ExpeditionState TakeLoadedExpedition(out BattleRecord battle)
        {
            ExpeditionState expedition = _loadedExpedition;
            battle = _loadedBattle;
            _loadedExpedition = null;
            _loadedBattle = null;
            return expedition;
        }

        /// <summary>
        /// Confirms the current state by writing run.json. ExpeditionManager passes its part; at home
        /// it is null. A failed write blocks every further change until it is retried successfully.
        /// </summary>
        internal void Save(ExpeditionRecord expedition)
        {
            byte[] snapshot = SaveManager.Serialize(RunSaveMapper.ToSave(Run, expedition));
            try
            {
                _save.SaveBytes(FileName, snapshot);
            }
            catch (SaveWriteException)
            {
                _unsavedSnapshot = snapshot;
                SaveBlockedChanged?.Invoke(true);
            }
        }

        /// <summary>Every command that changes state calls this first.</summary>
        internal void RequireWritable()
        {
            if (IsSaveBlocked)
            {
                throw new InvalidOperationException("The last change is not saved yet. Retry the save first.");
            }
        }

        /// <summary>Lobby commands need a run whose party is not on an expedition.</summary>
        void RequireAtHome()
        {
            RequireWritable();
            if (_run == null)
            {
                throw new InvalidOperationException("There is no run.");
            }

            if (IsAway)
            {
                throw new InvalidOperationException("The party is on an expedition.");
            }
        }
    }
}
