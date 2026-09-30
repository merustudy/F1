using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// The lobby and run rules (Docs/Design/04_Lobby_100Day_Economy.md): roster, party, fatigue,
    /// days and the return settlement. Pure functions over <see cref="RunState"/>.
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
                    Fatigue = data.Balance.MaxFatigue,
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
            int front = 0;
            int rear = 0;
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

                if (slot.Row == BattleRow.Front)
                {
                    front++;
                }
                else
                {
                    rear++;
                }
            }

            if (front > data.Balance.RowCapacity || rear > data.Balance.RowCapacity)
            {
                return $"A row holds at most {data.Balance.RowCapacity} mercenaries.";
            }

            return null;
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

            DungeonData dungeon = data.Dungeons.Get(dungeonId);
            foreach (PartySlot slot in state.Party)
            {
                if (FindMercenary(state, slot.MercenaryId).Fatigue < dungeon.FatigueCost)
                {
                    return DepartCheck.NotEnoughFatigue;
                }
            }

            return DepartCheck.Ok;
        }

        /// <summary>Passes RestDays. Everyone in the roster recovers fatigue.</summary>
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
                party.Add(new PartyMember(slot.MercenaryId, FindMercenary(state, slot.MercenaryId).JobId, slot.Row));
            }

            ulong seed = SeedDeriver.Derive(state.Seed, "expedition", state.ExpeditionCount);
            ExpeditionState expedition = ExpeditionRules.Create(data, dungeonId, seed, party);
            state.ExpeditionCount++;
            return expedition;
        }

        /// <summary>
        /// The return settlement, in this order: items and potions vanish (with the expedition state),
        /// the dead leave the roster for good, survivors pay fatigue, days pass and those who stayed
        /// home recover, and a clear is recorded.
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
                FatigueCost = dungeon.FatigueCost,
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
                    mercenary.Fatigue = Math.Max(0, mercenary.Fatigue - dungeon.FatigueCost);
                    report.SurvivorIds.Add(member.MercenaryId);
                }
                else
                {
                    state.Roster.Remove(mercenary);
                    state.Fallen.Add(member.MercenaryId);
                    state.Party.RemoveAll(slot => slot.MercenaryId == member.MercenaryId);
                    report.FallenIds.Add(member.MercenaryId);
                }
            }

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

        /// <summary>Advances the day counter. Mercenaries not in <paramref name="away"/> recover fatigue for each day.</summary>
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

                mercenary.Fatigue = Math.Min(data.Balance.MaxFatigue, mercenary.Fatigue + recovery);
            }
        }
    }
}
