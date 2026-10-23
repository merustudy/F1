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

            foreach (MercenaryData mercenary in data.StartingMercenaries)
            {
                state.Roster.Add(new MercenaryState
                {
                    Id = mercenary.Id,
                    JobId = mercenary.JobId,
                    Fatigue = 0,
                    Level = LevelRules.FirstLevel,
                    Stats = StatRules.BaseOf(data, mercenary.JobId),
                    Traits = new List<string>(mercenary.Traits),
                });
            }

            if (state.Roster.Count == 0)
            {
                state.Outcome = RunOutcome.Defeat;
                state.DefeatCause = DefeatCause.Wipeout;
            }

            // The stagecoach has candidates from day one (nothing to pay them with yet: a run starts with no gold).
            DrawRecruits(data, state);
            return state;
        }

        // ---- The stagecoach, the inn and the gold (Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §3·§5·§6) ----

        /// <summary>
        /// Draws the candidates of the stagecoach anew: RecruitCount names of the recruit pool that are not in the roster, dead or dismissed
        /// (the candidates being replaced are free again), each with a job rolled among every job, level 1, fatigue 0. The stream is derived
        /// from the run seed and the number of expeditions so far, so a draw is the same on the same run.
        /// </summary>
        public static void DrawRecruits(StaticData data, RunState state)
        {
            var taken = new HashSet<string>(StringComparer.Ordinal);
            foreach (MercenaryState mercenary in state.Roster)
            {
                taken.Add(mercenary.Id);
            }

            taken.UnionWith(state.Fallen);
            taken.UnionWith(state.Dismissed);

            var pool = new List<MercenaryData>();
            foreach (MercenaryData name in data.RecruitPool)
            {
                if (!taken.Contains(name.Id))
                {
                    pool.Add(name);
                }
            }

            var rng = new Pcg32(SeedDeriver.Derive(state.Seed, "recruit", state.ExpeditionCount), RngStream.Recruit);
            var drawn = new List<RecruitState>();
            IReadOnlyList<JobData> jobs = data.Jobs.Ordered;
            while (drawn.Count < BuildingRules.RecruitCount(data, state) && pool.Count > 0)
            {
                MercenaryData name = pool[rng.NextInt(pool.Count)];
                pool.Remove(name);
                JobData job = jobs[rng.NextInt(jobs.Count)];
                // The spread, then (Slice D stage 42, Docs/Design/04 §6) the candidate meets its job's base weapon: the stat the weapon asks for is raised to what
                // it asks when the spread took it under, no draw spent (the traits below draw the same numbers either way).
                StatBlock stats = StatRules.MeetingBaseWeapon(data, job.Id, Spread(data, StatRules.BaseOf(data, job.Id), rng));
                // Then the innate traits it was born with (Slice D stage 37), on the same stream.
                drawn.Add(new RecruitState { Id = name.Id, JobId = job.Id, Stats = stats, Traits = TraitRules.DrawInnate(data, rng) });
            }

            state.Recruits = drawn;
        }

        /// <summary>
        /// A candidate's stats (Slice D stage 34, Docs/Design/04 §6, Battle Brothers' backgrounds): each stat of the job's base values moved by an even
        /// draw in -RecruitStatSpread..RecruitStatSpread on the stagecoach's stream, strength first, within StatMin..StatMax. No draw with no spread.
        /// </summary>
        static StatBlock Spread(StaticData data, StatBlock stats, Pcg32 rng)
        {
            int spread = data.Balance.RecruitStatSpread;
            if (spread == 0)
            {
                return stats;
            }

            foreach (Stat stat in StatBlock.All)
            {
                int value = stats[stat] + rng.NextInt(2 * spread + 1) - spread;
                stats = stats.With(stat, Math.Max(data.Balance.StatMin, Math.Min(data.Balance.StatMax, value)));
            }

            return stats;
        }

        /// <summary>Whether the candidate at that index of <see cref="RunState.Recruits"/> can be hired now: the run goes on, the roster has room, the gold covers the price.</summary>
        public static HireCheck CanHire(StaticData data, RunState state, int index)
        {
            if (state.IsOver)
            {
                return HireCheck.RunIsOver;
            }

            if (BuildingRules.IsLocked(data, state, BuildingId.Coach))
            {
                return HireCheck.CoachLocked;
            }

            if (index < 0 || index >= state.Recruits.Count)
            {
                return HireCheck.NoCandidate;
            }

            if (state.Roster.Count >= BuildingRules.RosterLimit(data, state))
            {
                return HireCheck.RosterFull;
            }

            return state.Gold >= data.Balance.HirePrice ? HireCheck.Ok : HireCheck.NotEnoughGold;
        }

        /// <summary>Hires a candidate: the price leaves the gold, the candidate joins the roster fresh (fatigue 0) and leaves the stagecoach.</summary>
        public static MercenaryState Hire(StaticData data, RunState state, int index)
        {
            HireCheck check = CanHire(data, state, index);
            if (check != HireCheck.Ok)
            {
                throw new InvalidOperationException($"Cannot hire: {check}.");
            }

            RecruitState candidate = state.Recruits[index];
            state.Recruits.RemoveAt(index);
            state.Gold -= data.Balance.HirePrice;
            var hired = new MercenaryState
            {
                Id = candidate.Id,
                JobId = candidate.JobId,
                Fatigue = 0,
                Level = LevelRules.FirstLevel,
                Stats = candidate.Stats ?? StatRules.BaseOf(data, candidate.JobId),
                Traits = new List<string>(candidate.Traits ?? new List<string>()),
            };
            state.Roster.Add(hired);
            return hired;
        }

        /// <summary>A mercenary can be dismissed while the run goes on, the barracks is not under construction (stage 30) and it is not the last one left (the run ends only by its outcomes).</summary>
        public static bool CanDismiss(StaticData data, RunState state, string mercenaryId)
        {
            return !state.IsOver && !BuildingRules.IsLocked(data, state, BuildingId.Barracks) && FindMercenary(state, mercenaryId) != null && state.Roster.Count > 1;
        }

        /// <summary>
        /// Whether a mercenary of the roster can be levelled up now (Slice D stage 35): the run goes on and its experience meets the level's
        /// ExpToNext. The barracks being built does not stop it (the level is the mercenary's; the barracks is only where the button stands).
        /// </summary>
        public static bool CanLevelUp(StaticData data, RunState state, string mercenaryId)
        {
            MercenaryState mercenary = FindMercenary(state, mercenaryId);
            return !state.IsOver && mercenary != null && LevelRules.CanLevelUp(data, mercenary);
        }

        /// <summary>Levels a mercenary of the roster up by one level and rolls what it grows (<see cref="LevelRules.LevelUp"/>). Returns the gains.</summary>
        public static StatBlock LevelUp(StaticData data, RunState state, string mercenaryId)
        {
            if (!CanLevelUp(data, state, mercenaryId))
            {
                throw new InvalidOperationException($"Mercenary '{mercenaryId}' cannot level up now.");
            }

            return LevelRules.LevelUp(data, FindMercenary(state, mercenaryId), state.Seed);
        }

        /// <summary>Dismisses a mercenary, for free: out of the party (those behind advance) and the roster, never to return.</summary>
        public static void Dismiss(StaticData data, RunState state, string mercenaryId)
        {
            if (!CanDismiss(data, state, mercenaryId))
            {
                throw new InvalidOperationException($"Mercenary '{mercenaryId}' cannot be dismissed now.");
            }

            if (state.Party.RemoveAll(slot => slot.MercenaryId == mercenaryId) > 0)
            {
                CloseGaps(state);
            }

            state.Roster.Remove(FindMercenary(state, mercenaryId));
            state.Dismissed.Add(mercenaryId);
        }

        /// <summary>Whether the inn can treat a mercenary now: the run goes on, the mercenary carries fatigue, the gold covers the price. No day passes for it.</summary>
        public static TreatCheck CanTreat(StaticData data, RunState state, string mercenaryId)
        {
            if (state.IsOver)
            {
                return TreatCheck.RunIsOver;
            }

            if (BuildingRules.IsLocked(data, state, BuildingId.Inn))
            {
                return TreatCheck.InnLocked;
            }

            MercenaryState mercenary = FindMercenary(state, mercenaryId);
            if (mercenary == null)
            {
                return TreatCheck.NotInRoster;
            }

            if (mercenary.Fatigue <= 0)
            {
                return TreatCheck.NotTired;
            }

            return state.Gold >= data.Balance.TreatmentPrice ? TreatCheck.Ok : TreatCheck.NotEnoughGold;
        }

        /// <summary>
        /// Adds gold to a run that goes on. Only the development builds' test button calls it (Round 97, 2026-10-23: "로비 화면에서 금화 테스트 버튼");
        /// no rule of the game grants gold this way.
        /// </summary>
        public static void GrantGold(RunState state, int amount)
        {
            if (amount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amount), amount, "Grant a positive amount of gold.");
            }

            if (state.IsOver)
            {
                throw new InvalidOperationException("The run is over.");
            }

            state.Gold += amount;
        }

        /// <summary>Treats a mercenary at the inn: the price leaves the gold, TreatmentRelief leaves its fatigue and (stage 48) its affliction ends whatever the fatigue.</summary>
        public static void Treat(StaticData data, RunState state, string mercenaryId)
        {
            TreatCheck check = CanTreat(data, state, mercenaryId);
            if (check != TreatCheck.Ok)
            {
                throw new InvalidOperationException($"Cannot treat '{mercenaryId}': {check}.");
            }

            MercenaryState mercenary = FindMercenary(state, mercenaryId);
            state.Gold -= data.Balance.TreatmentPrice;
            mercenary.Fatigue = FatigueRules.Add(data.Balance, mercenary.Fatigue, -BuildingRules.TreatmentRelief(data, state));
            // Stage 48 (Docs/Design/04_Lobby_100Day_Economy.md §3): the treatment ends an affliction whatever the fatigue.
            mercenary.AfflictionId = FatigueRules.Cure(data, mercenary.AfflictionId);
        }

        /// <summary>
        /// The map floors the party reached on an ended expedition: the floor of the node it stood on last, the boss floor counting as the last
        /// map floor (the boss pays by the clear). The final stand has no map floors.
        /// </summary>
        public static int FloorsReached(StaticData data, ExpeditionState expedition)
        {
            if (expedition.FinalStand || expedition.CurrentNodeId < 0)
            {
                return 0;
            }

            return Math.Min(expedition.Map.Nodes[expedition.CurrentNodeId].Floor, data.Dungeons.Get(expedition.DungeonId).Floors);
        }

        /// <summary>What the return settlement pays for an ended expedition: GoldPerFloor for each floor reached, and GoldClear for a clear.</summary>
        /// <summary>
        /// What the shop would pay for the items the party brings home (2026-10-17): every item on the living members' boards and in the
        /// inventory that could be sold (not a base weapon, not a unique; bags and potions are not items), at <see cref="ExpeditionRules.SellPriceOf"/>.
        /// </summary>
        public static int ItemCoinsFor(StaticData data, ExpeditionState expedition)
        {
            int coins = 0;
            foreach (ExpeditionMember member in expedition.Members)
            {
                if (!member.Alive)
                {
                    continue;
                }

                foreach (BoardItem item in member.Board.Items)
                {
                    if (!item.Item.IsBase && !item.Item.Item.Unique)
                    {
                        coins += ExpeditionRules.SellPriceOf(data, item.Item);
                    }
                }
            }

            foreach (BoardItem item in expedition.Inventory.Items)
            {
                if (!item.Item.IsBase && !item.Item.Item.Unique)
                {
                    coins += ExpeditionRules.SellPriceOf(data, item.Item);
                }
            }

            return coins;
        }

        public static int GoldFor(StaticData data, ExpeditionState expedition)
        {
            return data.Balance.GoldPerFloor * FloorsReached(data, expedition) + (expedition.Result == ExpeditionResult.Cleared ? data.Balance.GoldClear : 0);
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
            DungeonData dungeon = data.Dungeons.Get(dungeonId);
            if (IsFinalStandDue(data, state))
            {
                // Past the last day only the final stand is left, and the final dungeon is open for it whether or not it was unlocked.
                return dungeon.IsFinal ? DepartCheck.Ok : DepartCheck.FinalStandDue;
            }

            return IsUnlocked(state, dungeon) ? DepartCheck.Ok : DepartCheck.Locked;
        }

        /// <summary>
        /// True from the day after the last one (Docs/Design/04_Lobby_100Day_Economy.md §2): the only expedition left is the final stand,
        /// the final dungeon's boss alone. Passing the last day is not itself a defeat.
        /// </summary>
        public static bool IsFinalStandDue(StaticData data, RunState state)
        {
            return !state.IsOver && state.Day > data.Balance.TotalDays;
        }

        /// <summary>A dungeon is open from the start, or once the dungeon it names was cleared often enough (Docs/Design/03_Dungeon_Structure.md §1).</summary>
        public static bool IsUnlocked(RunState state, DungeonData dungeon)
        {
            if (dungeon.UnlockDungeonId == null)
            {
                return true;
            }

            state.ClearedDungeons.TryGetValue(dungeon.UnlockDungeonId, out int cleared);
            return cleared >= dungeon.UnlockClears;
        }

        /// <summary>Resting takes a day; past the last day there is none to spend, and an ended run does nothing.</summary>
        public static bool CanRest(StaticData data, RunState state)
        {
            return !state.IsOver && !IsFinalStandDue(data, state);
        }

        /// <summary>Passes RestDays. Everyone in the roster recovers: their fatigue comes down. Returns the buildings whose construction the days finished (stage 30).</summary>
        public static List<string> Rest(StaticData data, RunState state)
        {
            if (!CanRest(data, state))
            {
                throw new InvalidOperationException(state.IsOver ? "The run is over." : "The last day has passed: only the final stand is left.");
            }

            return PassDays(data, state, data.Balance.RestDays, null);
        }

        /// <summary>How much fatigue comes down over a number of days at home. The lobby shows it on the rest button.</summary>
        public static int FatigueRecovery(BalanceData balance, int days)
        {
            return balance.FatigueRecoveryPerDay * days;
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
                party.Add(new PartyMember(slot.MercenaryId, mercenary.JobId, slot.Row, mercenary.Fatigue, mercenary.AfflictionId, mercenary.Level, mercenary.Stats, mercenary.Traits));
            }

            ulong seed = SeedDeriver.Derive(state.Seed, "expedition", state.ExpeditionCount);
            // Past the last day the departure is the final stand: the final dungeon's boss alone (CanDepart allowed no other dungeon).
            ExpeditionState expedition = ExpeditionRules.Create(data, dungeonId, seed, party, finalStand: IsFinalStandDue(data, state));
            state.ExpeditionCount++;
            state.ExpeditionsByDungeon.TryGetValue(dungeonId, out int visits);
            state.ExpeditionsByDungeon[dungeonId] = visits + 1;
            return expedition;
        }

        /// <summary>
        /// The return settlement, in this order: items and potions vanish (with the expedition state),
        /// the dead leave the roster for good, survivors keep the fatigue they came back with, gain the expedition's
        /// experience (stage 27) and roll the traits it may leave on them (Slice D stage 38), days pass and those who stayed home recover, a clear
        /// is recorded, the gold is paid (Slice C stage 26) and, if the run goes on, the stagecoach draws new candidates. A settlement that ends
        /// the run rolls the traits all the same.
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
            report.ExpEarned = LevelRules.ExpFor(data, expedition);
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
                    // The fatigue comes home, and an affliction with it; a virtue ends with the expedition. Every survivor gets the same experience (stage 27).
                    mercenary.Fatigue = member.Fatigue;
                    mercenary.AfflictionId = member.StateId != null && data.FatigueStates.Get(member.StateId).Kind == FatigueStateKind.Affliction ? member.StateId : null;
                    report.SurvivorIds.Add(member.MercenaryId);
                    report.SurvivorFatigue.Add(member.Fatigue);
                    report.SurvivorStates.Add(mercenary.AfflictionId);
                    // Slice D stage 35: the experience only builds up; the barracks levels the mercenary up (LevelUp below).
                    if (LevelRules.Gain(data, mercenary, report.ExpEarned))
                    {
                        report.ReadyToLevelUp.Add(member.MercenaryId);
                    }

                    // Slice D stage 38: what the expedition leaves on the survivor (05 §6), on the survivor's own stream for this expedition.
                    List<string> acquired = TraitRules.DrawAcquired(data, mercenary.Traits, TraitRules.SourcesOf(data, expedition.Result, member),
                        TraitRules.AcquiredStream(state.Seed, member.MercenaryId, state.ExpeditionCount));
                    if (acquired.Count > 0)
                    {
                        mercenary.Traits.AddRange(acquired);
                        report.NewTraits.Add(new AcquiredTraits { MercenaryId = member.MercenaryId, TraitIds = acquired });
                    }
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
            report.BuildingsCompleted = PassDays(data, state, dungeon.DurationDays, participants);

            if (expedition.Result == ExpeditionResult.Cleared)
            {
                state.ClearedDungeons.TryGetValue(dungeon.Id, out int cleared);
                state.ClearedDungeons[dungeon.Id] = cleared + 1;
            }

            report.FloorsReached = FloorsReached(data, expedition);
            report.GoldEarned = GoldFor(data, expedition);
            if (expedition.Result == ExpeditionResult.Cleared)
            {
                // 2026-10-17 (the user's request): a cleared expedition's region coins come home as gold — the coins left plus what the shop
                // would pay for the items the party brings back, CoinsPerGold to one, the remainder lost with the expedition.
                report.CoinsFromItems = ItemCoinsFor(data, expedition);
                report.CoinsConverted = expedition.Coins + report.CoinsFromItems;
                report.GoldFromCoins = report.CoinsConverted / data.Balance.CoinsPerGold;
                report.GoldEarned += report.GoldFromCoins;
            }

            state.Gold += report.GoldEarned;
            report.GoldAfter = state.Gold;

            CloseTheRun(data, state, dungeon, expedition);
            if (!state.IsOver)
            {
                DrawRecruits(data, state);
            }

            report.DayAfter = state.Day;
            report.Outcome = state.Outcome;
            report.DefeatCause = state.DefeatCause;
            report.RunIsOver = state.IsOver;
            return report;
        }

        /// <summary>
        /// Whether this settlement ends the run (Docs/Design/04_Lobby_100Day_Economy.md §8): clearing the final dungeon wins; losing the final
        /// stand, the last mercenary, or reaching the death limit loses. Passing the last day alone ends nothing: the final stand follows.
        /// </summary>
        static void CloseTheRun(StaticData data, RunState state, DungeonData dungeon, ExpeditionState expedition)
        {
            if (expedition.Result == ExpeditionResult.Cleared && dungeon.IsFinal)
            {
                state.Outcome = RunOutcome.Victory;
                state.DefeatCause = DefeatCause.None;
                return;
            }

            DefeatCause cause = DefeatCause.None;
            if (expedition.FinalStand)
            {
                cause = DefeatCause.FinalStandLost;
            }
            else if (state.Roster.Count == 0)
            {
                cause = DefeatCause.Wipeout;
            }
            else if (state.Fallen.Count >= data.Balance.DeathLimit)
            {
                cause = DefeatCause.DeathLimit;
            }

            if (cause != DefeatCause.None)
            {
                state.Outcome = RunOutcome.Defeat;
                state.DefeatCause = cause;
            }
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

        /// <summary>
        /// Advances the day counter. Mercenaries not in <paramref name="away"/> recover: their fatigue comes down for each day. The constructions
        /// whose finish day is reached end (stage 30): the buildings finished are returned.
        /// </summary>
        static List<string> PassDays(StaticData data, RunState state, int days, HashSet<string> away)
        {
            state.Day += days;
            int recovery = FatigueRecovery(data.Balance, days);
            foreach (MercenaryState mercenary in state.Roster)
            {
                if (away != null && away.Contains(mercenary.Id))
                {
                    continue;
                }

                mercenary.Fatigue = FatigueRules.Add(data.Balance, mercenary.Fatigue, -recovery);
                mercenary.AfflictionId = FatigueRules.StateAfter(data, mercenary.AfflictionId, mercenary.Fatigue);
            }

            return BuildingRules.Complete(state);
        }
    }
}
