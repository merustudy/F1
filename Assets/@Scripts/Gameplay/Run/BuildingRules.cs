using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The town's buildings and their construction (Slice C stage 30, Docs/Design/04_Lobby_100Day_Economy.md §7): every building starts
    /// at level 1, an upgrade pays the next level's price and puts the building under construction for its days, the days pass only as the
    /// run's days do (an expedition's settlement, a day of rest) and the level arrives when the finish day is reached. While a building is
    /// under construction it is locked: the stagecoach does not hire, the inn does not treat (resting goes on), the barracks does not dismiss
    /// and the gate does not let the party leave. Past the last day no lock holds and nothing can be built: the days do not pass any more.
    /// What a level gives is the BuildingData row's Bonus over the BalanceData base: the roster limit (barracks), the candidates (stagecoach),
    /// the treatment's relief (inn) and the settlement's gold in percent (gate). Pure functions over <see cref="RunState"/>.
    /// </summary>
    public static class BuildingRules
    {
        public const int FirstLevel = 1;

        /// <summary>The building's level; a building the state does not list is at the first level.</summary>
        public static int LevelOf(RunState state, string buildingId)
        {
            return state.BuildingLevels.TryGetValue(buildingId, out int level) ? level : FirstLevel;
        }

        /// <summary>The construction going on at the building, or null.</summary>
        public static ConstructionState ConstructionOf(RunState state, string buildingId)
        {
            foreach (ConstructionState construction in state.Constructions)
            {
                if (construction.BuildingId == buildingId)
                {
                    return construction;
                }
            }

            return null;
        }

        /// <summary>The days a construction still takes: at least 1 while it goes on (it ends the moment the finish day is reached).</summary>
        public static int DaysLeft(RunState state, ConstructionState construction)
        {
            return Math.Max(1, construction.FinishDay - state.Day);
        }

        /// <summary>A building under construction is locked, unless the last day has passed (the days do not pass any more: the lock would never lift).</summary>
        public static bool IsLocked(StaticData data, RunState state, string buildingId)
        {
            return ConstructionOf(state, buildingId) != null && !RunRules.IsFinalStandDue(data, state);
        }

        /// <summary>What the building's current level adds to its base value.</summary>
        public static int BonusOf(StaticData data, RunState state, string buildingId)
        {
            return data.BuildingAt(buildingId, LevelOf(state, buildingId)).Bonus;
        }

        /// <summary>The next level's row, or null at the last level.</summary>
        public static BuildingData NextLevel(StaticData data, RunState state, string buildingId)
        {
            int next = LevelOf(state, buildingId) + 1;
            return next > data.MaxBuildingLevel(buildingId) ? null : data.BuildingAt(buildingId, next);
        }

        // ---- What the levels give ----------------------------------------------------------

        /// <summary>How many mercenaries the roster holds: RosterLimit plus the barracks' bonus.</summary>
        public static int RosterLimit(StaticData data, RunState state)
        {
            return data.Balance.RosterLimit + BonusOf(data, state, BuildingId.Barracks);
        }

        /// <summary>How many candidates the stagecoach draws: RecruitCount plus the stagecoach's bonus.</summary>
        public static int RecruitCount(StaticData data, RunState state)
        {
            return data.Balance.RecruitCount + BonusOf(data, state, BuildingId.Coach);
        }

        /// <summary>How much fatigue a treatment takes off: TreatmentRelief plus the inn's bonus.</summary>
        public static int TreatmentRelief(StaticData data, RunState state)
        {
            return data.Balance.TreatmentRelief + BonusOf(data, state, BuildingId.Inn);
        }

        /// <summary>The gate's bonus on the settlement's gold, in percent (0 at the first level).</summary>
        public static int GoldBonusPercent(StaticData data, RunState state)
        {
            return BonusOf(data, state, BuildingId.Gate);
        }

        /// <summary>Gold with the gate's bonus on it, rounded down.</summary>
        public static int WithGateBonus(StaticData data, RunState state, int gold)
        {
            return gold * (100 + GoldBonusPercent(data, state)) / 100;
        }

        // ---- Upgrading ---------------------------------------------------------------------

        /// <summary>
        /// Whether the building can be upgraded now: the run goes on and the last day has not passed, the building has a next level and is not
        /// under construction, and the gold covers the next level's price. Any number of buildings may be under construction at once.
        /// </summary>
        public static UpgradeCheck CanUpgrade(StaticData data, RunState state, string buildingId)
        {
            if (state.IsOver)
            {
                return UpgradeCheck.RunIsOver;
            }

            if (RunRules.IsFinalStandDue(data, state))
            {
                return UpgradeCheck.FinalStandDue;
            }

            BuildingData next = NextLevel(data, state, buildingId);
            if (next == null)
            {
                return UpgradeCheck.MaxLevel;
            }

            if (ConstructionOf(state, buildingId) != null)
            {
                return UpgradeCheck.UnderConstruction;
            }

            return state.Gold >= next.Price ? UpgradeCheck.Ok : UpgradeCheck.NotEnoughGold;
        }

        /// <summary>Pays the next level's price and starts its construction: it finishes on the day Days from now.</summary>
        public static ConstructionState Upgrade(StaticData data, RunState state, string buildingId)
        {
            UpgradeCheck check = CanUpgrade(data, state, buildingId);
            if (check != UpgradeCheck.Ok)
            {
                throw new InvalidOperationException($"Cannot upgrade '{buildingId}': {check}.");
            }

            BuildingData next = NextLevel(data, state, buildingId);
            state.Gold -= next.Price;
            var construction = new ConstructionState { BuildingId = buildingId, ToLevel = next.Level, FinishDay = state.Day + next.Days };
            state.Constructions.Add(construction);
            return construction;
        }

        /// <summary>
        /// Ends every construction whose finish day has come: the building takes its new level. Called where the days pass (RunRules). Returns the
        /// buildings finished, in the order of <see cref="BuildingId.All"/>.
        /// </summary>
        public static List<string> Complete(RunState state)
        {
            var finished = new List<string>();
            foreach (string buildingId in BuildingId.All)
            {
                ConstructionState construction = ConstructionOf(state, buildingId);
                if (construction != null && construction.FinishDay <= state.Day)
                {
                    state.BuildingLevels[buildingId] = construction.ToLevel;
                    state.Constructions.Remove(construction);
                    finished.Add(buildingId);
                }
            }

            return finished;
        }
    }

    /// <summary>Whether a building can be upgraded now (Slice C stage 30).</summary>
    public enum UpgradeCheck
    {
        Ok,
        RunIsOver,
        FinalStandDue,
        MaxLevel,
        UnderConstruction,
        NotEnoughGold,
    }
}
