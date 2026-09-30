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
                    case BattleEventKind.PotionUsed:
                        PotionsUsed++;
                        break;
                    case BattleEventKind.RetreatAttempted:
                        RetreatAttempts++;
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
            Console.WriteLine($"  per battle: potions {Ratio(PotionsUsed, Battles)}, retreat attempts {Ratio(RetreatAttempts, Battles)}");
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
        /// <summary>Resolves "rowan" (mercenary id) or "knight" (job id), with an optional ":Front"/":Rear".</summary>
        public static List<PartyMember> ParseParty(StaticData data, string spec)
        {
            var party = new List<PartyMember>();
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

                JobData job = data.Jobs.Get(mercenary.JobId);
                BattleRow row = job.RecommendedRow;
                if (parts.Length > 1 && !Enum.TryParse(parts[1], false, out row))
                {
                    throw new ArgumentException($"'{parts[1]}' is not Front or Rear.");
                }

                party.Add(new PartyMember(mercenary.Id, job.Id, row));
            }

            return party;
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
                    case BattleEventKind.PotionUsed: detail = $"potion {e.Id} on {Name(e.Target)}"; break;
                    case BattleEventKind.RetreatAttempted: detail = $"retreat roll {e.B} vs {e.A}% -> {(e.C == 1 ? "success" : "failed")}"; break;
                    case BattleEventKind.StormTicked: detail = $"storm {e.A}"; break;
                    case BattleEventKind.BattleEnded: detail = $"ended: {(BattleResult)e.A}"; break;
                    default: detail = e.Kind.ToString(); break;
                }

                Console.WriteLine($"{e.TimeMs,6} {detail}");
            }
        }

        /// <summary>Whole expeditions: node choices, battles, rewards, until cleared, wiped or retreated.</summary>
        public static void Expedition(StaticData data, string dungeonId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            int cleared = 0;
            int wiped = 0;
            int retreated = 0;
            int deaths = 0;
            int expeditionsWithDeath = 0;
            int battlesWon = 0;
            var all = new BattleStats();
            var byFloor = new SortedDictionary<int, BattleStats>();

            for (int run = 0; run < runs; run++)
            {
                ulong seed = SeedDeriver.Derive(baseSeed, "sim", run);
                ExpeditionState state = ExpeditionRules.Create(data, dungeonId, seed, party);
                while (state.Phase != ExpeditionPhase.Finished)
                {
                    if (state.Phase == ExpeditionPhase.ChoosingReward)
                    {
                        policy.ChooseReward(data, state);
                        continue;
                    }

                    MapNode node = SimPolicy.ChooseNode(ExpeditionRules.AvailableNodes(state));
                    var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, node.Id));
                    policy.RunBattle(battle);
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
                    case ExpeditionResult.Cleared: cleared++; break;
                    case ExpeditionResult.Wiped: wiped++; break;
                    case ExpeditionResult.Retreated: retreated++; break;
                }

                int dead = state.Members.Count(m => !m.Alive);
                deaths += dead;
                if (dead > 0)
                {
                    expeditionsWithDeath++;
                }

                battlesWon += state.BattlesWon;
            }

            Console.WriteLine($"dungeon {dungeonId}, party {Describe(party)}, policy {policy.Name}, seed {baseSeed}");
            Console.WriteLine($"expeditions {runs}: cleared {BattleStats.Percent(cleared, runs)}, wiped {BattleStats.Percent(wiped, runs)}, retreated {BattleStats.Percent(retreated, runs)}");
            Console.WriteLine($"  deaths per expedition {BattleStats.Ratio(deaths, runs)}, expeditions with a death {BattleStats.Percent(expeditionsWithDeath, runs)}, battles won per expedition {BattleStats.Ratio(battlesWon, runs)}");
            all.Print("all battles");
            foreach (KeyValuePair<int, BattleStats> floor in byFloor)
            {
                floor.Value.Print($"floor {floor.Key}");
            }
        }

        static string Describe(List<PartyMember> party)
        {
            return string.Join(",", party.Select(m => $"{m.MercenaryId}({m.JobId}):{m.Row}"));
        }
    }
}
