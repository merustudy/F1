using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;

namespace F1.Sim
{
    /// <summary>Counters gathered from the event log of one or many battles.</summary>
    public sealed class BattleStats
    {
        public int Battles;
        public int Victories;
        public int Defeats;
        public int Retreats;
        public long DurationMs;
        public int LongestMs;
        public int ReachedStorm;
        public int DogEntries;
        public int GraceBreaks;
        public int DeathRolls;
        public int Deaths;
        public int PotionsUsed;
        public int RetreatAttempts;
        public int PartyAdvances;

        /// <summary>The fatigue of the party in battle: breakdowns into an affliction or a virtue, collapses at the maximum (and how many killed).</summary>
        public int Afflictions;
        public int Virtues;
        public int Collapses;
        public int CollapseDeaths;
        public readonly Dictionary<string, int> DeathsByMercenary = new Dictionary<string, int>();

        public void Add(BattleEngine battle)
        {
            Battles++;
            switch (battle.Result)
            {
                case BattleResult.Victory: Victories++; break;
                case BattleResult.Defeat: Defeats++; break;
                case BattleResult.Retreated: Retreats++; break;
            }

            DurationMs += battle.TimeMs;
            LongestMs = Math.Max(LongestMs, battle.TimeMs);
            bool storm = false;
            foreach (BattleEvent e in battle.Events)
            {
                switch (e.Kind)
                {
                    case BattleEventKind.StormTicked:
                        storm = true;
                        break;
                    case BattleEventKind.DogEntered:
                        DogEntries++;
                        break;
                    case BattleEventKind.GraceBroken:
                        GraceBreaks++;
                        break;
                    case BattleEventKind.DeathRolled:
                        DeathRolls++;
                        break;
                    case BattleEventKind.Died:
                        if (e.Target.Side == BattleSide.Party)
                        {
                            Deaths++;
                            string id = battle.Unit(e.Target).Setup.SourceId;
                            DeathsByMercenary.TryGetValue(id, out int count);
                            DeathsByMercenary[id] = count + 1;
                        }

                        break;
                    case BattleEventKind.RowsAdvanced:
                        if (e.A == (int)BattleSide.Party)
                        {
                            PartyAdvances++;
                        }

                        break;
                    case BattleEventKind.PotionUsed:
                        PotionsUsed++;
                        break;
                    case BattleEventKind.RetreatAttempted:
                        RetreatAttempts++;
                        break;
                    case BattleEventKind.BrokeDown:
                        if (e.C == 1)
                        {
                            Virtues++;
                        }
                        else
                        {
                            Afflictions++;
                        }

                        break;
                    case BattleEventKind.Collapsed:
                        Collapses++;
                        CollapseDeaths += e.C;
                        break;
                }
            }

            if (storm)
            {
                ReachedStorm++;
            }
        }

        public void Print(string title)
        {
            Console.WriteLine(title);
            if (Battles == 0)
            {
                Console.WriteLine("  (no battles)");
                return;
            }

            Console.WriteLine($"  battles {Battles}: victory {Percent(Victories, Battles)}, defeat {Percent(Defeats, Battles)}, retreat {Percent(Retreats, Battles)}");
            Console.WriteLine($"  duration avg {DurationMs / Battles / 1000.0:F1}s, longest {LongestMs / 1000.0:F1}s, reached storm {Percent(ReachedStorm, Battles)}");
            Console.WriteLine($"  per battle: deaths {Ratio(Deaths, Battles)}, DoG entries {Ratio(DogEntries, Battles)}, grace breaks {Ratio(GraceBreaks, Battles)}, death rolls {Ratio(DeathRolls, Battles)}");
            Console.WriteLine($"  per battle: potions {Ratio(PotionsUsed, Battles)}, retreat attempts {Ratio(RetreatAttempts, Battles)}, party advances {Ratio(PartyAdvances, Battles)}");
            if (Afflictions + Virtues + Collapses > 0)
            {
                Console.WriteLine($"  fatigue: afflictions {Afflictions}, virtues {Virtues}, collapses {Collapses} (deaths {CollapseDeaths})");
            }
            if (DeathsByMercenary.Count > 0)
            {
                string byMercenary = string.Join(", ", DeathsByMercenary.OrderBy(p => p.Key, StringComparer.Ordinal).Select(p => $"{p.Key} {p.Value}"));
                Console.WriteLine($"  deaths by mercenary: {byMercenary}");
            }
        }

        public static string Percent(int part, int whole)
        {
            return whole == 0 ? "-" : $"{part * 100.0 / whole:F1}%";
        }

        public static string Ratio(int part, int whole)
        {
            return whole == 0 ? "-" : $"{(double)part / whole:F2}";
        }
    }

    public static class Simulations
    {
        /// <summary>
        /// Resolves "rowan" (mercenary id) or "knight" (job id). With a row (":1", ":2", ...) on every
        /// member the rows are taken as written. Without them the party lines up by the recommended
        /// row of each job (ties keep the order given), one per row from the front.
        /// </summary>
        public static List<PartyMember> ParseParty(StaticData data, string spec)
        {
            var members = new List<(MercenaryData Mercenary, JobData Job, int Row)>();
            int explicitRows = 0;
            foreach (string token in spec.Split(','))
            {
                string[] parts = token.Trim().Split(':');
                string name = parts[0];
                MercenaryData mercenary = data.Mercenaries.Contains(name)
                    ? data.Mercenaries.Get(name)
                    : data.Mercenaries.Ordered.FirstOrDefault(m => m.JobId == name);
                if (mercenary == null)
                {
                    throw new ArgumentException($"'{name}' is neither a mercenary id nor a job with a mercenary.");
                }

                int row = 0;
                if (parts.Length > 1)
                {
                    if (!int.TryParse(parts[1], out row) || !BattleRows.IsValid(row))
                    {
                        throw new ArgumentException($"'{parts[1]}' is not a row ({BattleRows.Front}..{BattleRows.Count}).");
                    }

                    explicitRows++;
                }

                members.Add((mercenary, data.Jobs.Get(mercenary.JobId), row));
            }

            if (explicitRows != 0 && explicitRows != members.Count)
            {
                throw new ArgumentException("Give a row to every party member or to none.");
            }

            if (explicitRows == 0)
            {
                // OrderBy is stable: jobs with the same recommended row keep the order given.
                members = members.OrderBy(m => m.Job.RecommendedRow).ToList();
                for (int i = 0; i < members.Count; i++)
                {
                    members[i] = (members[i].Mercenary, members[i].Job, BattleRows.Front + i);
                }
            }

            return members.Select(m => new PartyMember(m.Mercenary.Id, m.Job.Id, m.Row)).ToList();
        }

        /// <summary>One fresh party against one enemy group, many seeds.</summary>
        public static void Battle(StaticData data, string dungeonId, string groupId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            EnemyGroupData group = data.EnemyGroups.Get(groupId);
            if (group.DungeonId != dungeonId)
            {
                throw new ArgumentException($"Group '{groupId}' belongs to dungeon '{group.DungeonId}'.");
            }

            var stats = new BattleStats();
            for (int run = 0; run < runs; run++)
            {
                ulong seed = SeedDeriver.Derive(baseSeed, "sim", run);
                ExpeditionState state = ExpeditionRules.Create(data, dungeonId, seed, party);

                var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, groupId, SeedDeriver.Derive(seed, "battle", 0)));
                policy.RunBattle(battle);
                stats.Add(battle);
            }

            Console.WriteLine($"dungeon {dungeonId}, group {groupId}, party {Describe(party)}, policy {policy.Name}, seed {baseSeed}");
            stats.Print("battle (fresh party, default weapons)");
        }

        /// <summary>Prints the event log of one battle, for reading what happened and why.</summary>
        public static void Trace(StaticData data, string dungeonId, string groupId, List<PartyMember> party, SimPolicy policy, ulong seed)
        {
            ExpeditionState state = ExpeditionRules.Create(data, dungeonId, seed, party);
            var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, groupId, SeedDeriver.Derive(seed, "battle", 0)));
            policy.RunBattle(battle);

            string Name(UnitRef unit)
            {
                return unit.IsNone ? "-" : $"{(unit.Side == BattleSide.Party ? "P" : "E")}{unit.Index}:{battle.Unit(unit).Setup.SourceId}";
            }

            foreach (BattleEvent e in battle.Events)
            {
                string detail;
                switch (e.Kind)
                {
                    case BattleEventKind.ItemActivated: detail = $"{Name(e.Source)} uses {e.Id}"; break;
                    case BattleEventKind.Damaged: detail = $"{Name(e.Target)} takes {e.A} from {e.Id} (shield absorbed {e.B}) -> hp {e.C}"; break;
                    case BattleEventKind.Healed: detail = $"{Name(e.Target)} healed {e.B} by {e.Id} -> hp {e.C}"; break;
                    case BattleEventKind.ShieldGained: detail = $"{Name(e.Target)} shield +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.BurnApplied: detail = $"{Name(e.Target)} burn +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.DogEntered: detail = $"{Name(e.Target)} at death's door, grace until {e.A}"; break;
                    case BattleEventKind.DogExited: detail = $"{Name(e.Target)} back from death's door"; break;
                    case BattleEventKind.GraceBroken: detail = $"{Name(e.Target)} grace broken after {e.A} hits"; break;
                    case BattleEventKind.DeathRolled: detail = $"{Name(e.Target)} death roll {e.B} vs {e.A}% -> {(e.C == 1 ? "DIED" : "survived")}"; break;
                    case BattleEventKind.Died: detail = $"{Name(e.Target)} died"; break;
                    case BattleEventKind.RowsAdvanced: detail = $"{(BattleSide)e.A} row {e.B} is empty: those behind advance"; break;
                    case BattleEventKind.PotionUsed: detail = $"potion {e.Id} on {Name(e.Target)}"; break;
                    case BattleEventKind.RetreatAttempted: detail = $"retreat roll {e.B} vs {e.A}% -> {(e.C == 1 ? "success" : "failed")}"; break;
                    case BattleEventKind.StormTicked: detail = $"storm {e.A}"; break;
                    case BattleEventKind.BattleEnded: detail = $"ended: {(BattleResult)e.A}"; break;
                    default: detail = e.Kind.ToString(); break;
                }

                Console.WriteLine($"{e.TimeMs,6} {detail}");
            }
        }

        /// <summary>What many expeditions of one party came to.</summary>
        sealed class ExpeditionStats
        {
            public int Runs;
            public int Cleared;
            public int Wiped;
            public int Retreated;
            public int Deaths;
            public int ExpeditionsWithDeath;
            public int BattlesWon;
            public int ItemsOnBoards;
            public int ItemsInInventory;

            /// <summary>Fatigue the survivors came back with, and how many came back.</summary>
            public long SurvivorFatigue;
            public int Survivors;
            public int HighestFatigue;

            /// <summary>Fatigue paid for equipment when battles started, and for how many members in all those battles.</summary>
            public long EquipmentFatigue;
            public int MemberBattles;

            /// <summary>Nodes entered by kind, and the battle time of whole expeditions.</summary>
            public int Elites;
            public int Camps;
            public long BattleTimeMs;

            /// <summary>Expeditions in which someone broke down, and the survivors who came home at or over the threshold, or afflicted.</summary>
            public int ExpeditionsWithBreakdown;
            public int SurvivorsAtBreakdown;
            public int SurvivorsAfflicted;

            /// <summary>Items merged a tier up, items mended at camps, and the items on the boards at the end by tier.</summary>
            public int Merges;
            public int Mends;
            public readonly int[] TiersAtEnd = new int[4];
            public readonly BattleStats All = new BattleStats();
            public readonly SortedDictionary<int, BattleStats> ByFloor = new SortedDictionary<int, BattleStats>();
        }

        /// <summary>Whole expeditions: node choices, battles, rewards, until cleared, wiped or retreated.</summary>
        public static void Expedition(StaticData data, string dungeonId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            ExpeditionStats stats = RunExpeditions(data, dungeonId, party, policy, runs, baseSeed);

            Console.WriteLine($"dungeon {dungeonId}, party {Describe(party)}, policy {policy.Name}, seed {baseSeed}");
            Console.WriteLine($"expeditions {runs}: cleared {BattleStats.Percent(stats.Cleared, runs)}, wiped {BattleStats.Percent(stats.Wiped, runs)}, retreated {BattleStats.Percent(stats.Retreated, runs)}");
            Console.WriteLine($"  deaths per expedition {BattleStats.Ratio(stats.Deaths, runs)}, expeditions with a death {BattleStats.Percent(stats.ExpeditionsWithDeath, runs)}, battles won per expedition {BattleStats.Ratio(stats.BattlesWon, runs)}");
            Console.WriteLine($"  at the end: items on boards {BattleStats.Ratio(stats.ItemsOnBoards, runs)}, in the inventory {BattleStats.Ratio(stats.ItemsInInventory, runs)}");
            Console.WriteLine($"  fatigue of the survivors at the end: avg {Average(stats.SurvivorFatigue, stats.Survivors)}, highest {stats.HighestFatigue}; equipment fatigue per member per battle {Average(stats.EquipmentFatigue, stats.MemberBattles)}");
            Console.WriteLine($"  fatigue per expedition: afflictions {BattleStats.Ratio(stats.All.Afflictions, runs)}, virtues {BattleStats.Ratio(stats.All.Virtues, runs)}, collapses {BattleStats.Ratio(stats.All.Collapses, runs)}; "
                + $"expeditions with a breakdown {BattleStats.Percent(stats.ExpeditionsWithBreakdown, runs)}, survivors at or over the breakdown {BattleStats.Percent(stats.SurvivorsAtBreakdown, stats.Survivors)}, afflicted at the end {BattleStats.Percent(stats.SurvivorsAfflicted, stats.Survivors)}");
            Console.WriteLine($"  per expedition: elites {BattleStats.Ratio(stats.Elites, runs)}, camps {BattleStats.Ratio(stats.Camps, runs)}, battle time {stats.BattleTimeMs / 1000.0 / runs:F1}s at x1");
            Console.WriteLine($"  per expedition: merges {BattleStats.Ratio(stats.Merges, runs)}, mends at camps {BattleStats.Ratio(stats.Mends, runs)}; on the boards at the end: "
                + $"bronze {BattleStats.Ratio(stats.TiersAtEnd[0], runs)}, silver {BattleStats.Ratio(stats.TiersAtEnd[1], runs)}, gold {BattleStats.Ratio(stats.TiersAtEnd[2], runs)}, diamond {BattleStats.Ratio(stats.TiersAtEnd[3], runs)}");
            stats.All.Print("all battles");
            foreach (KeyValuePair<int, BattleStats> floor in stats.ByFloor)
            {
                floor.Value.Print($"floor {floor.Key}");
            }
        }

        /// <summary>
        /// The same party in every order, front to back, with the same seeds: shows how much the
        /// rows matter. Best formation first.
        /// </summary>
        public static void Formations(StaticData data, string dungeonId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            var results = new List<(List<PartyMember> Party, ExpeditionStats Stats)>();
            foreach (List<PartyMember> order in Orders(party))
            {
                var lined = new List<PartyMember>();
                for (int i = 0; i < order.Count; i++)
                {
                    lined.Add(new PartyMember(order[i].MercenaryId, order[i].JobId, BattleRows.Front + i));
                }

                results.Add((lined, RunExpeditions(data, dungeonId, lined, policy, runs, baseSeed)));
            }

            Console.WriteLine($"dungeon {dungeonId}, policy {policy.Name}, seed {baseSeed}, expeditions {runs} per formation");
            foreach ((List<PartyMember> lined, ExpeditionStats stats) in results.OrderByDescending(r => r.Stats.Cleared).ThenBy(r => r.Stats.Deaths))
            {
                Console.WriteLine(
                    $"  {Describe(lined),-46} cleared {BattleStats.Percent(stats.Cleared, runs),6}, wiped {BattleStats.Percent(stats.Wiped, runs),6}, retreated {BattleStats.Percent(stats.Retreated, runs),6}, deaths per expedition {BattleStats.Ratio(stats.Deaths, runs)}");
            }
        }

        /// <summary>Every ordering of the members, in a fixed order.</summary>
        static IEnumerable<List<PartyMember>> Orders(List<PartyMember> members)
        {
            if (members.Count <= 1)
            {
                yield return new List<PartyMember>(members);
                yield break;
            }

            for (int i = 0; i < members.Count; i++)
            {
                var rest = new List<PartyMember>(members);
                rest.RemoveAt(i);
                foreach (List<PartyMember> tail in Orders(rest))
                {
                    tail.Insert(0, members[i]);
                    yield return tail;
                }
            }
        }

        static ExpeditionStats RunExpeditions(StaticData data, string dungeonId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            var stats = new ExpeditionStats { Runs = runs };
            BattleStats all = stats.All;
            SortedDictionary<int, BattleStats> byFloor = stats.ByFloor;

            for (int run = 0; run < runs; run++)
            {
                ulong seed = SeedDeriver.Derive(baseSeed, "sim", run);
                ExpeditionState state = ExpeditionRules.Create(data, dungeonId, seed, party);
                bool brokeDown = false;
                while (state.Phase != ExpeditionPhase.Finished)
                {
                    if (state.Phase == ExpeditionPhase.ChoosingReward)
                    {
                        stats.Merges += policy.ChooseReward(data, state);
                        continue;
                    }

                    if (state.Phase == ExpeditionPhase.AtCamp)
                    {
                        stats.Mends += policy.ChooseAtCamp(data, state) ? 1 : 0;
                        continue;
                    }

                    MapNode node = SimPolicy.ChooseNode(ExpeditionRules.AvailableNodes(state));
                    if (node.Kind == MapNodeKind.Camp)
                    {
                        ExpeditionRules.EnterCamp(state, node.Id);
                        stats.Camps++;
                        continue;
                    }

                    stats.Elites += node.Kind == MapNodeKind.Elite ? 1 : 0;
                    foreach (ExpeditionMember member in state.Members.Where(m => m.Alive))
                    {
                        stats.EquipmentFatigue += FatigueRules.EquipmentCost(data.Balance, member.Items);
                        stats.MemberBattles++;
                    }

                    var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, node.Id));
                    policy.RunBattle(battle);
                    stats.BattleTimeMs += battle.TimeMs;
                    brokeDown |= battle.Events.Any(e => e.Kind == BattleEventKind.BrokeDown);
                    all.Add(battle);
                    if (!byFloor.TryGetValue(node.Floor, out BattleStats floorStats))
                    {
                        floorStats = new BattleStats();
                        byFloor.Add(node.Floor, floorStats);
                    }

                    floorStats.Add(battle);
                    ExpeditionRules.CompleteBattle(data, state, battle);
                }

                switch (state.Result)
                {
                    case ExpeditionResult.Cleared: stats.Cleared++; break;
                    case ExpeditionResult.Wiped: stats.Wiped++; break;
                    case ExpeditionResult.Retreated: stats.Retreated++; break;
                }

                int dead = state.Members.Count(m => !m.Alive);
                stats.Deaths += dead;
                if (dead > 0)
                {
                    stats.ExpeditionsWithDeath++;
                }

                stats.BattlesWon += state.BattlesWon;
                stats.ItemsOnBoards += state.Members.Sum(m => m.Items.Count);
                foreach (EquippedItem item in state.Members.SelectMany(m => m.Items))
                {
                    stats.TiersAtEnd[(int)item.Tier]++;
                }
                stats.ItemsInInventory += state.Inventory.Count;
                stats.ExpeditionsWithBreakdown += brokeDown ? 1 : 0;
                foreach (ExpeditionMember member in state.Members.Where(m => m.Alive))
                {
                    stats.SurvivorFatigue += member.Fatigue;
                    stats.Survivors++;
                    stats.HighestFatigue = Math.Max(stats.HighestFatigue, member.Fatigue);
                    stats.SurvivorsAtBreakdown += member.Fatigue >= data.Balance.FatigueBreakdown ? 1 : 0;
                    stats.SurvivorsAfflicted += member.StateId != null && data.FatigueStates.Get(member.StateId).Kind == FatigueStateKind.Affliction ? 1 : 0;
                }
            }

            return stats;
        }

        static string Average(long total, int count)
        {
            return count == 0 ? "-" : $"{(double)total / count:F2}";
        }

        static string Describe(List<PartyMember> party)
        {
            return string.Join(",", party.Select(m => $"{m.MercenaryId}({m.JobId}):{m.Row}"));
        }
    }
}
