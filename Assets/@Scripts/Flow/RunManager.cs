using System;
using System.Collections.Generic;
using F1.Core;
using F1.Data;
using F1.Gameplay;

namespace F1.Flow
{
    /// <summary>
    /// Owns the run (the 100 days): creating it, the lobby commands and applying the return
    /// settlement. Rules are computed by RunRules; this class checks that a command is allowed now,
    /// calls the rule and confirms the result.
    /// </summary>
    public sealed class RunManager
    {
        readonly DataManager _data;
        readonly Func<ulong> _newSeed;
        RunState _run;

        /// <param name="newSeed">Gives the seed of a new run. AppRoot passes an OS random source; tests pass a fixed value.</param>
        public RunManager(DataManager data, Func<ulong> newSeed)
        {
            _data = data ?? throw new ArgumentNullException(nameof(data));
            _newSeed = newSeed ?? throw new ArgumentNullException(nameof(newSeed));
        }

        /// <summary>Raised after a new run replaced whatever was in progress.</summary>
        public event Action RunStarted;

        public bool HasRun => _run != null;

        /// <summary>The run state. Read it; only rules change it.</summary>
        public RunState Run => _run ?? throw new InvalidOperationException("There is no run.");

        /// <summary>True from departure until the return settlement.</summary>
        public bool IsAway { get; private set; }

        /// <summary>Starts a run from day one. Anything in progress is dropped.</summary>
        public void StartNewRun()
        {
            _run = RunRules.NewRun(_data.Data, _newSeed());
            IsAway = false;
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
        }

        public void Rest()
        {
            RequireAtHome();
            RunRules.Rest(_data.Data, _run);
        }

        public DepartCheck CanDepart(string dungeonId)
        {
            return RunRules.CanDepart(_data.Data, Run, dungeonId);
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

        /// <summary>Lobby commands need a run whose party is not on an expedition.</summary>
        void RequireAtHome()
        {
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
