using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The lobby and run rules (Docs/Design/04_Lobby_100Day_Economy.md): roster, party, fatigue,
    /// days and the return settlement. Pure functions over <see cref="RunState"/>. Fatigue builds up
    /// on an expedition (<see cref="FatigueRules"/>) and comes down on the days a mercenary stays home.
    /// </summary>
    public static class RunRules
    {
        public static RunState NewRun(StaticData data, ulong seed)
        {
            var state = new RunState
            {
                Seed = seed,
                Day = 1,
            };

            foreach (MercenaryData mercenary in data.Mercenaries.Ordered)
            {
                state.Roster.Add(new MercenaryState
                {
                    Id = mercenary.Id,
                    JobId = mercenary.JobId,
                    Fatigue = 0,
                });
            }

            state.IsOver = state.Roster.Count == 0;
            return state;
        }

        public static MercenaryState FindMercenary(RunState state, string mercenaryId)
        {
            foreach (MercenaryState mercenary in state.Roster)
            {
                if (mercenary.Id == mercenaryId)
                {
                    return mercenary;
                }
            }

            return null;
        }

        /// <summary>Why a party cannot be set, or null when it is valid.</summary>
        public static string PartyProblem(StaticData data, RunState state, IReadOnlyList<PartySlot> party)
        {
            if (party == null)
            {
                return "Party is missing.";
            }

            if (party.Count > data.Balance.PartySize)
            {
                return $"A party has at most {data.Balance.PartySize} mercenaries.";
            }

            var seen = new HashSet<string>();
            var rows = new List<int>();
            foreach (PartySlot slot in party)
            {
                if (slot == null || FindMercenary(state, slot.MercenaryId) == null)
                {
                    return $"Mercenary '{slot?.MercenaryId}' is not in the roster.";
                }

                if (!seen.Add(slot.MercenaryId))
                {
                    return $"Mercenary '{slot.MercenaryId}' is in the party twice.";
                }

                rows.Add(slot.Row);
            }

            return Formation.Problem(rows);
        }

        /// <summary>Sets the lobby party. An empty party is allowed; it just cannot depart.</summary>
        public static void SetParty(StaticData data, RunState state, IReadOnlyList<PartySlot> party)
        {
            string problem = PartyProblem(data, state, party);
            if (problem != null)
            {
                throw new InvalidOperationException(problem);
            }

            var copy = new List<PartySlot>();
            foreach (PartySlot slot in party)
            {
                copy.Add(new PartySlot { MercenaryId = slot.MercenaryId, Row = slot.Row });
            }

            state.Party = copy;
        }

        /// <summary>
        /// True when the mercenary can take that row now. One who is not in the party joins the
        /// first empty row. One who is in the party moves to a row where someone else stands.
        /// </summary>
        public static bool CanPlaceInParty(StaticData data, RunState state, string mercenaryId, int row)
        {
            if (FindMercenary(state, mercenaryId) == null)
            {
                return false;
            }

            int[] rows = PartyRows(state);
            int index = state.Party.FindIndex(slot => slot.MercenaryId == mercenaryId);
            if (index >= 0)
            {
                return Formation.CanMove(rows, index, row);
            }

            return state.Party.Count < data.Balance.PartySize && Formation.CanJoin(rows, row);
        }

        /// <summary>
        /// Puts a mercenary in a row of the party. A member who moves trades places with the
        /// mercenary standing there.
        /// </summary>
        public static void PlaceInParty(StaticData data, RunState state, string mercenaryId, int row)
        {
            if (!CanPlaceInParty(data, state, mercenaryId, row))
            {
                throw new InvalidOperationException($"Mercenary '{mercenaryId}' cannot take row {row} now.");
            }

            int index = state.Party.FindIndex(slot => slot.MercenaryId == mercenaryId);
            if (index < 0)
            {
                state.Party.Add(new PartySlot { MercenaryId = mercenaryId, Row = row });
                return;
            }

            int[] rows = PartyRows(state);
            Formation.Move(rows, index, row);
            SetPartyRows(state, rows);
        }

        /// <summary>Takes a mercenary out of the party. Those who stood behind advance.</summary>
        public static void RemoveFromParty(RunState state, string mercenaryId)
        {
            if (state.Party.RemoveAll(slot => slot.MercenaryId == mercenaryId) == 0)
            {
                throw new InvalidOperationException($"Mercenary '{mercenaryId}' is not in the party.");
            }

            CloseGaps(state);
        }

        public static DepartCheck CanDepart(StaticData data, RunState state, string dungeonId)
        {
            if (state.IsOver)
            {
                return DepartCheck.RunIsOver;
            }

            if (state.Party.Count < data.Balance.MinPartySize)
            {
                return DepartCheck.PartyTooSmall;
            }

            // Only a dungeon that exists can be chosen; fatigue never keeps anyone home.
            data.Dungeons.Get(dungeonId);
            return DepartCheck.Ok;
        }

        /// <summary>Passes RestDays. Everyone in the roster recovers: their fatigue comes down.</summary>
        public static void Rest(StaticData data, RunState state)
        {
            if (state.IsOver)
            {
                throw new InvalidOperationException("The run is over.");
            }

            PassDays(data, state, data.Balance.RestDays, null);
        }

        /// <summary>Starts an expedition with the lobby party. Its seed is derived from the run seed.</summary>
        public static ExpeditionState BeginExpedition(StaticData data, RunState state, string dungeonId)
        {
            DepartCheck check = CanDepart(data, state, dungeonId);
            if (check != DepartCheck.Ok)
            {
                throw new InvalidOperationException($"Cannot depart: {check}.");
            }

            var party = new List<PartyMember>();
            foreach (PartySlot slot in state.Party)
            {
                MercenaryState mercenary = FindMercenary(state, slot.MercenaryId);
                party.Add(new PartyMember(slot.MercenaryId, mercenary.JobId, slot.Row, mercenary.Fatigue, mercenary.AfflictionId));
            }

            ulong seed = SeedDeriver.Derive(state.Seed, "expedition", state.ExpeditionCount);
            ExpeditionState expedition = ExpeditionRules.Create(data, dungeonId, seed, party);
            state.ExpeditionCount++;
            return expedition;
        }

        /// <summary>
        /// The return settlement, in this order: items and potions vanish (with the expedition state),
        /// the dead leave the roster for good, survivors keep the fatigue they came back with, days pass
        /// and those who stayed home recover, and a clear is recorded.
        /// </summary>
        public static SettlementReport Settle(StaticData data, RunState state, ExpeditionState expedition)
        {
            if (expedition.Phase != ExpeditionPhase.Finished)
            {
                throw new InvalidOperationException("The expedition has not finished.");
            }

            DungeonData dungeon = data.Dungeons.Get(expedition.DungeonId);
            var report = new SettlementReport
            {
                DungeonId = dungeon.Id,
                Result = expedition.Result,
                DaysPassed = dungeon.DurationDays,
            };

            var participants = new HashSet<string>();
            foreach (ExpeditionMember member in expedition.Members)
            {
                participants.Add(member.MercenaryId);
                MercenaryState mercenary = FindMercenary(state, member.MercenaryId);
                if (mercenary == null)
                {
                    throw new InvalidOperationException($"Mercenary '{member.MercenaryId}' is not in the roster.");
                }

                if (member.Alive)
                {
                    // The fatigue comes home, and an affliction with it; a virtue ends with the expedition.
                    mercenary.Fatigue = member.Fatigue;
                    mercenary.AfflictionId = member.StateId != null && data.FatigueStates.Get(member.StateId).Kind == FatigueStateKind.Affliction ? member.StateId : null;
                    report.SurvivorIds.Add(member.MercenaryId);
                    report.SurvivorFatigue.Add(member.Fatigue);
                    report.SurvivorStates.Add(mercenary.AfflictionId);
                }
                else
                {
                    state.Roster.Remove(mercenary);
                    state.Fallen.Add(member.MercenaryId);
                    state.Party.RemoveAll(slot => slot.MercenaryId == member.MercenaryId);
                    report.FallenIds.Add(member.MercenaryId);
                }
            }

            // The lobby party keeps the formation chosen before leaving; those behind the dead advance.
            CloseGaps(state);
            PassDays(data, state, dungeon.DurationDays, participants);

            if (expedition.Result == ExpeditionResult.Cleared)
            {
                state.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
                state.ClearedDungeons[dungeon.Id] = cleared + 1;
            }

            state.IsOver = state.Roster.Count == 0;
            report.DayAfter = state.Day;
            report.RunIsOver = state.IsOver;
            return report;
        }

        static int[] PartyRows(RunState state)
        {
            var rows = new int[state.Party.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                rows[i] = state.Party[i].Row;
            }

            return rows;
        }

        static void SetPartyRows(RunState state, int[] rows)
        {
            for (int i = 0; i < rows.Length; i++)
            {
                state.Party[i].Row = rows[i];
            }
        }

        static void CloseGaps(RunState state)
        {
            int[] rows = PartyRows(state);
            Formation.CloseGaps(rows);
            SetPartyRows(state, rows);
        }

        /// <summary>Advances the day counter. Mercenaries not in <paramref name="away"/> recover: their fatigue comes down for each day.</summary>
        static void PassDays(StaticData data, RunState state, int days, HashSet<string> away)
        {
            state.Day += days;
            int recovery = data.Balance.FatigueRecoveryPerDay * days;
            foreach (MercenaryState mercenary in state.Roster)
            {
                if (away != null && away.Contains(mercenary.Id))
                {
                    continue;
                }

                mercenary.Fatigue = FatigueRules.Add(data.Balance, mercenary.Fatigue, -recovery);
                mercenary.AfflictionId = FatigueRules.StateAfter(data, mercenary.AfflictionId, mercenary.Fatigue);
            }
        }
    }
}
