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

        /// <summary>Stage 48: virtues that reached the maximum and ended there instead of the exhaustion.</summary>
        public int VirtuesSpent;

        /// <summary>Critical hits (Slice B stage 22) the party dealt and took.</summary>
        public int CritsDealt;
        public int CritsTaken;
        public int MissesDealt;
        public int MissesTaken;

        /// <summary>The effect vocabulary (Slice D stage 44), the party's side: stacks of bleed and regen it put and took, thorns damage it dealt back and took, cleanses, staggers, charges.</summary>
        public int BleedDealt;
        public int BleedTaken;
        public int RegenGiven;
        public long ThornsDealt;
        public long ThornsTaken;
        public int Cleanses;
        public int Staggers;
        public int Charges;

        /// <summary>The seven states of stage 45: stacks the party put on anyone, and stacks put on the party, by state.</summary>
        public readonly Dictionary<EffectKind, int> StatesPut = new Dictionary<EffectKind, int>();
        public readonly Dictionary<EffectKind, int> StatesTaken = new Dictionary<EffectKind, int>();
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
                    case BattleEventKind.Missed:
                        if (e.Source.Side == BattleSide.Party)
                        {
                            MissesDealt++;
                        }
                        else
                        {
                            MissesTaken++;
                        }

                        break;
                    case BattleEventKind.CriticalHit:
                        if (e.Source.Side == BattleSide.Party)
                        {
                            CritsDealt++;
                        }
                        else
                        {
                            CritsTaken++;
                        }

                        break;
                    case BattleEventKind.BleedApplied:
                        if (e.Target.Side == BattleSide.Party)
                        {
                            BleedTaken += e.A;
                        }
                        else
                        {
                            BleedDealt += e.A;
                        }

                        break;
                    case BattleEventKind.RegenApplied:
                        if (e.Target.Side == BattleSide.Party)
                        {
                            RegenGiven += e.A;
                        }

                        break;
                    case BattleEventKind.Damaged:
                        if (e.Id == BattleEvent.CauseThorns)
                        {
                            if (e.Target.Side == BattleSide.Party)
                            {
                                ThornsTaken += e.A;
                            }
                            else
                            {
                                ThornsDealt += e.A;
                            }
                        }

                        break;
                    case BattleEventKind.Cleansed:
                        if (e.A + e.B > 0)
                        {
                            Cleanses++;
                        }

                        break;
                    case BattleEventKind.Staggered:
                        Staggers++;
                        break;
                    case BattleEventKind.Charged:
                        Charges++;
                        break;
                    case BattleEventKind.StateApplied:
                    {
                        Dictionary<EffectKind, int> by = e.Source.Side == BattleSide.Party ? StatesPut : StatesTaken;
                        var state = (EffectKind)e.B;
                        by.TryGetValue(state, out int stacks);
                        by[state] = stacks + e.A;
                        break;
                    }
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
                    case BattleEventKind.FatigueChanged:
                        VirtuesSpent += e.Id == BattleEvent.FatigueVirtueEnd ? 1 : 0;
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
            Console.WriteLine($"  per battle: critical hits dealt {Ratio(CritsDealt, Battles)}, taken {Ratio(CritsTaken, Battles)}; misses (Slice D stage 33) by the party {Ratio(MissesDealt, Battles)}, by the enemies {Ratio(MissesTaken, Battles)}");
            if (Afflictions + Virtues + Collapses > 0)
            {
                Console.WriteLine($"  fatigue: afflictions {Afflictions}, virtues {Virtues}, collapses {Collapses} (deaths {CollapseDeaths}), virtues spent at the maximum {VirtuesSpent}");
            }
            if (BleedDealt + BleedTaken + RegenGiven + Cleanses + Staggers + Charges > 0 || ThornsDealt + ThornsTaken > 0)
            {
                // The effect vocabulary (Slice D stage 44), per battle: printed only when a battle used any of it, so older reports read as before.
                Console.WriteLine($"  effects per battle: bleed stacks put {Ratio(BleedDealt, Battles)}, taken {Ratio(BleedTaken, Battles)}; regen stacks {Ratio(RegenGiven, Battles)}; "
                    + $"thorns dealt back {Centi.ToText((int)(ThornsDealt / Battles))}, taken {Centi.ToText((int)(ThornsTaken / Battles))}; cleanses {Ratio(Cleanses, Battles)}, staggers {Ratio(Staggers, Battles)}, charges {Ratio(Charges, Battles)}");
            }
            if (StatesPut.Count + StatesTaken.Count > 0)
            {
                // The seven states of stage 45, stacks per battle: what the party put on anyone / what was put on the party.
                string put = string.Join(", ", StatesPut.OrderBy(p => p.Key).Select(p => $"{p.Key} {Ratio(p.Value, Battles)}"));
                string taken = string.Join(", ", StatesTaken.OrderBy(p => p.Key).Select(p => $"{p.Key} {Ratio(p.Value, Battles)}"));
                Console.WriteLine($"  states per battle (stage 45): put by the party [{put}]; put on the party [{taken}]");
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
        /// Resolves "rowan" (mercenary id) or "warrior" (job id). A job takes the first starting mercenary of that job not already in the party,
        /// or else (Slice D stage 36: the archer — the ranger then — and the stage 47 jobs come only from the stagecoach) the first name of the recruit pool not in it, on that job.
        /// With a row (":1", ":2", ...) on every member the rows are taken as written. Without them the party lines up by the recommended
        /// row of each job (ties keep the order given), one per row from the front.
        /// </summary>
        public static List<PartyMember> ParseParty(StaticData data, string spec)
        {
            var members = new List<(MercenaryData Mercenary, JobData Job, int Row)>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            int explicitRows = 0;
            foreach (string token in spec.Split(','))
            {
                string[] parts = token.Trim().Split(':');
                string name = parts[0];
                MercenaryData mercenary;
                JobData job;
                if (data.Mercenaries.Contains(name))
                {
                    mercenary = data.Mercenaries.Get(name);
                    job = mercenary.JobId == null ? null : data.Jobs.Get(mercenary.JobId);
                }
                else
                {
                    job = data.Jobs.Contains(name) ? data.Jobs.Get(name) : null;
                    mercenary = job == null
                        ? null
                        : data.Mercenaries.Ordered.FirstOrDefault(m => m.JobId == name && !used.Contains(m.Id))
                            ?? data.Mercenaries.Ordered.FirstOrDefault(m => !m.Starting && !used.Contains(m.Id));
                }

                if (mercenary == null || job == null)
                {
                    throw new ArgumentException($"'{name}' is neither a mercenary id with a job nor a job.");
                }

                used.Add(mercenary.Id);

                int row = 0;
                if (parts.Length > 1)
                {
                    if (!int.TryParse(parts[1], out row) || !BattleRows.IsValid(row))
                    {
                        throw new ArgumentException($"'{parts[1]}' is not a row ({BattleRows.Front}..{BattleRows.Count}).");
                    }

                    explicitRows++;
                }

                members.Add((mercenary, job, row));
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

            // A starting mercenary brings the traits the data gives it (Slice D stage 37); a name of the pool has none.
            return members.Select(m => new PartyMember(m.Mercenary.Id, m.Job.Id, m.Row, traits: m.Mercenary.Traits)).ToList();
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

            // HP, damage, healing, shields and the storm are hundredths (Slice B stage 22): printed as the numbers they stand for.
            string C(int hundredths)
            {
                return Centi.ToText(hundredths);
            }

            foreach (BattleEvent e in battle.Events)
            {
                string detail;
                switch (e.Kind)
                {
                    case BattleEventKind.ItemActivated: detail = $"{Name(e.Source)} uses {e.Id}"; break;
                    case BattleEventKind.Damaged: detail = $"{Name(e.Target)} takes {C(e.A)} from {e.Id} (shield absorbed {C(e.B)}) -> hp {C(e.C)}"; break;
                    case BattleEventKind.CriticalHit: detail = $"{Name(e.Target)} critical hit by {e.Id}: {C(e.A)} (roll {e.B} vs {e.C}%)"; break;
                    case BattleEventKind.Healed: detail = $"{Name(e.Target)} healed {C(e.B)} by {e.Id} -> hp {C(e.C)}"; break;
                    case BattleEventKind.ShieldGained: detail = $"{Name(e.Target)} shield +{C(e.A)} by {e.Id} -> {C(e.C)}"; break;
                    case BattleEventKind.BurnApplied: detail = $"{Name(e.Target)} burn +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.BleedApplied: detail = $"{Name(e.Target)} bleed +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.RegenApplied: detail = $"{Name(e.Target)} regen +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.ThornsGained: detail = $"{Name(e.Target)} thorns +{C(e.A)} by {e.Id} -> {C(e.C)}"; break;
                    case BattleEventKind.Cleansed: detail = $"{Name(e.Target)} cleansed by {e.Id}: burn -{e.A}, bleed -{e.B}"; break;
                    case BattleEventKind.Staggered: detail = $"{Name(e.Target)} staggered {e.A} ms by {e.Id} ({e.B} items)"; break;
                    case BattleEventKind.Charged: detail = $"{Name(e.Target)} charged {e.A} ms by {e.Id} ({e.B} items)"; break;
                    case BattleEventKind.StateApplied: detail = $"{Name(e.Target)} {(EffectKind)e.B} +{e.A} by {e.Id} -> {e.C}"; break;
                    case BattleEventKind.DogEntered: detail = $"{Name(e.Target)} at death's door, grace until {e.A}"; break;
                    case BattleEventKind.DogExited: detail = $"{Name(e.Target)} back from death's door"; break;
                    case BattleEventKind.GraceBroken: detail = $"{Name(e.Target)} grace broken after {e.A} hits"; break;
                    case BattleEventKind.DeathRolled: detail = $"{Name(e.Target)} death roll {e.B} vs {e.A}% -> {(e.C == 1 ? "DIED" : "survived")}"; break;
                    case BattleEventKind.Died: detail = $"{Name(e.Target)} died"; break;
                    case BattleEventKind.RowsAdvanced: detail = $"{(BattleSide)e.A} row {e.B} is empty: those behind advance"; break;
                    case BattleEventKind.PotionUsed: detail = $"potion {e.Id} on {Name(e.Target)}"; break;
                    case BattleEventKind.RetreatAttempted: detail = $"retreat roll {e.B} vs {e.A}% -> {(e.C == 1 ? "success" : "failed")}"; break;
                    case BattleEventKind.StormTicked: detail = $"storm {C(e.A)}"; break;
                    case BattleEventKind.BrokeDown: detail = $"{Name(e.Target)} broke down: {e.Id} (roll {e.A} vs virtue {e.B}%)"; break;
                    case BattleEventKind.Collapsed: detail = $"{Name(e.Target)} collapsed -> {(e.C == 1 ? "DIED" : "death's door")}"; break;
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

            /// <summary>The bag squares and the bags added to the start bag, summed over every member at the end (Slice B stage 19).</summary>
            public long BagSquares;
            public int BagsAdded;
            public int MembersAtEnd;

            /// <summary>Star items on the boards at the end, the melee weapons their stars light, and the star damage on the weapons, summed (Slice B stage 20).</summary>
            public int StarItems;
            public int StarsLit;
            public long StarDamage;

            /// <summary>Items that activate when a battle starts (the armour, Slice D stage 39) on the boards at the end, and those on the member in row 1.</summary>
            public int StartItems;
            public int StartItemsFront;

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

            /// <summary>The shops (Slice B stage 17): shops entered, things bought and refreshes made there, and the region coins won, spent and left at the end.</summary>
            public int Shops;
            public int Bought;
            public int Refreshes;
            public long CoinsWon;
            public long CoinsSpent;
            public long CoinsLeft;

            /// <summary>2026-10-17: the party's own base weapons on the shops' grids, and the general items among the drops.</summary>
            public int BaseWeaponsOffered;
            public int GeneralDrops;

            /// <summary>The loot (Slice B stage 18): drops the won battles left, and how many of them were picked up.</summary>
            public int Drops;
            public int DropsTaken;

            /// <summary>Drops a member had room for but nobody could wield (Slice D stage 42: the stat the item asks for).</summary>
            public int DropsKeptOffByRequirement;

            /// <summary>Items merged a tier up, items mended at camps, and the items on the boards at the end by tier.</summary>
            public int Merges;
            public int Mends;
            public int Forges;
            public int UniqueAtEnd;
            public readonly int[] TiersAtEnd = new int[4];
            public readonly BattleStats All = new BattleStats();
            public readonly SortedDictionary<int, BattleStats> ByFloor = new SortedDictionary<int, BattleStats>();
        }

        /// <summary>Whole expeditions: node choices, battles, loot, camps and shops, until cleared, wiped or retreated.</summary>
        public static void Expedition(StaticData data, string dungeonId, List<PartyMember> party, SimPolicy policy, int runs, ulong baseSeed)
        {
            ExpeditionStats stats = RunExpeditions(data, dungeonId, party, policy, runs, baseSeed);

            Console.WriteLine($"dungeon {dungeonId}, party {Describe(party)}, policy {policy.Name}, seed {baseSeed}");
            Console.WriteLine($"expeditions {runs}: cleared {BattleStats.Percent(stats.Cleared, runs)}, wiped {BattleStats.Percent(stats.Wiped, runs)}, retreated {BattleStats.Percent(stats.Retreated, runs)}");
            Console.WriteLine("  stats (Slice D stage 32, str/dex/con/int/wis/end): " + string.Join(", ", party.Select(m => $"{m.MercenaryId} {m.Stats ?? StatRules.BaseOf(data, m.JobId)}")));
            Console.WriteLine($"  deaths per expedition {BattleStats.Ratio(stats.Deaths, runs)}, expeditions with a death {BattleStats.Percent(stats.ExpeditionsWithDeath, runs)}, battles won per expedition {BattleStats.Ratio(stats.BattlesWon, runs)}");
            Console.WriteLine($"  at the end: items on boards {BattleStats.Ratio(stats.ItemsOnBoards, runs)}, in the inventory {BattleStats.Ratio(stats.ItemsInInventory, runs)}");
            Console.WriteLine($"  bags per member at the end: added {BattleStats.Ratio(stats.BagsAdded, stats.MembersAtEnd)}, bag squares {Average(stats.BagSquares, stats.MembersAtEnd)}");
            Console.WriteLine($"  stars per expedition at the end: star items {BattleStats.Ratio(stats.StarItems, runs)}, weapons lit {BattleStats.Ratio(stats.StarsLit, runs)}, star damage on weapons {AverageCenti(stats.StarDamage, runs)}");
            Console.WriteLine($"  armour (Slice D stage 39) per expedition at the end: on the boards {BattleStats.Ratio(stats.StartItems, runs)}, on row 1 {BattleStats.Ratio(stats.StartItemsFront, runs)}");
            Console.WriteLine($"  fatigue of the survivors at the end: avg {Average(stats.SurvivorFatigue, stats.Survivors)}, highest {stats.HighestFatigue}; equipment fatigue per member per battle {Average(stats.EquipmentFatigue, stats.MemberBattles)}");
            Console.WriteLine($"  fatigue per expedition: afflictions {BattleStats.Ratio(stats.All.Afflictions, runs)}, virtues {BattleStats.Ratio(stats.All.Virtues, runs)}, collapses {BattleStats.Ratio(stats.All.Collapses, runs)}; "
                + $"expeditions with a breakdown {BattleStats.Percent(stats.ExpeditionsWithBreakdown, runs)}, survivors at or over the breakdown {BattleStats.Percent(stats.SurvivorsAtBreakdown, stats.Survivors)}, afflicted at the end {BattleStats.Percent(stats.SurvivorsAfflicted, stats.Survivors)}");
            Console.WriteLine($"  per expedition: elites {BattleStats.Ratio(stats.Elites, runs)}, camps {BattleStats.Ratio(stats.Camps, runs)}, battle time {stats.BattleTimeMs / 1000.0 / runs:F1}s at x1");
            Console.WriteLine($"  per expedition (2026-10-17): general drops {BattleStats.Ratio(stats.GeneralDrops, runs)} of the drops, the party's base weapons on offer {BattleStats.Ratio(stats.BaseWeaponsOffered, runs)} (per shop {(stats.Shops > 0 ? (double)stats.BaseWeaponsOffered / stats.Shops : 0):F2})");
            Console.WriteLine($"  per expedition: drops {BattleStats.Ratio(stats.Drops, runs)}, picked up {BattleStats.Ratio(stats.DropsTaken, runs)}, kept off a board by a stat requirement (Slice D stage 42) {BattleStats.Ratio(stats.DropsKeptOffByRequirement, runs)}");
            Console.WriteLine($"  per expedition: uniques made after won battles (stage 50) {BattleStats.Ratio(stats.Forges, runs)}, expeditions ending with a unique item {BattleStats.Percent(stats.UniqueAtEnd, runs)}");
            Console.WriteLine($"  per expedition: merges {BattleStats.Ratio(stats.Merges, runs)}, mends at camps {BattleStats.Ratio(stats.Mends, runs)}; on the boards at the end: "
                + $"common {BattleStats.Ratio(stats.TiersAtEnd[0], runs)}, 1-star {BattleStats.Ratio(stats.TiersAtEnd[1], runs)}, 2-star {BattleStats.Ratio(stats.TiersAtEnd[2], runs)}, 3-star {BattleStats.Ratio(stats.TiersAtEnd[3], runs)}");
            Console.WriteLine($"  per expedition: shops {BattleStats.Ratio(stats.Shops, runs)}, bought {BattleStats.Ratio(stats.Bought, runs)}, refreshes {BattleStats.Ratio(stats.Refreshes, runs)}; "
                + $"coins won {Average(stats.CoinsWon, runs)}, spent {Average(stats.CoinsSpent, runs)}, left at the end {Average(stats.CoinsLeft, runs)}");
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
                    lined.Add(new PartyMember(order[i].MercenaryId, order[i].JobId, BattleRows.Front + i, traits: order[i].Traits));
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

        /// <summary>Plays one expedition to its end as the policy would, adding to the stats as it goes. True when someone broke down.</summary>
        static bool PlayExpedition(StaticData data, ExpeditionState state, SimPolicy policy, ExpeditionStats stats)
        {
            BattleStats all = stats.All;
            SortedDictionary<int, BattleStats> byFloor = stats.ByFloor;
            bool brokeDown = false;
            while (state.Phase != ExpeditionPhase.Finished)
            {
                if (state.Phase == ExpeditionPhase.PickingLoot)
                {
                    stats.Drops += state.Loot.Count;
                    var carried = new HashSet<string>(StringComparer.Ordinal);
                    foreach (string enemyId in data.EnemyGroups.Get(state.Map.Get(state.CurrentNodeId).EnemyGroupId).Enemies)
                    {
                        foreach (ItemGrant grant in data.Enemies.Get(enemyId).Items)
                        {
                            carried.Add(grant.ItemId);
                        }
                    }

                    stats.GeneralDrops += state.Loot.Count(d => d != null && d.Kind == OfferKind.Item && !carried.Contains(d.Id));
                    stats.Merges += policy.PickLoot(data, state, out int taken, out int keptOff);
                    stats.DropsTaken += taken;
                    stats.DropsKeptOffByRequirement += keptOff;
                    continue;
                }

                if (state.Phase == ExpeditionPhase.AtCamp)
                {
                    bool didAtCamp = policy.ChooseAtCamp(data, state);
                    if (didAtCamp) stats.Mends++;
                    continue;
                }

                if (state.Phase == ExpeditionPhase.AtShop)
                {
                    var partyWeapons = new HashSet<string>(state.Members.Where(m => m.Alive).Select(m => data.Jobs.Get(m.JobId).WeaponItemId), StringComparer.Ordinal);
                    stats.BaseWeaponsOffered += state.Shop.Stock.Count(o => o != null && o.Kind == OfferKind.Item && partyWeapons.Contains(o.Id));
                    int coinsBefore = state.Coins;
                    stats.Bought += policy.ShopAt(data, state, out int refreshes, out int shopMerges);
                    stats.Refreshes += refreshes;
                    stats.Merges += shopMerges;
                    stats.CoinsSpent += coinsBefore - state.Coins;
                    continue;
                }

                MapNode node = SimPolicy.ChooseNode(ExpeditionRules.AvailableNodes(state));
                if (node.Kind == MapNodeKind.Camp)
                {
                    ExpeditionRules.EnterCamp(state, node.Id);
                    stats.Camps++;
                    continue;
                }

                if (node.Kind == MapNodeKind.Shop)
                {
                    ExpeditionRules.EnterShop(data, state, node.Id);
                    stats.Shops++;
                    continue;
                }

                stats.Elites += node.Kind == MapNodeKind.Elite ? 1 : 0;
                foreach (ExpeditionMember member in state.Members.Where(m => m.Alive))
                {
                    stats.EquipmentFatigue += FatigueRules.EquipmentCost(data.Balance, member.Board.InReadingOrder());
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
                int coinsBeforeBattle = state.Coins;
                ExpeditionRules.CompleteBattle(data, state, battle);
                stats.Forges += state.LastForged.Count;
                stats.CoinsWon += state.Coins - coinsBeforeBattle;
            }

            return brokeDown;
        }

        /// <summary>
        /// Whole runs (Slice C stage 25, Docs/Design/04_Lobby_100Day_Economy.md): a lobby policy hires while the roster is under PartySize + bench and
        /// the gold covers the price (stage 26; the first candidate, never dismissing anyone), picks the party (the least tired), treats a party member at or
        /// over treatAbove at the inn while the gold lasts, rests when the party is still tired, goes to the final dungeon once it is open, and fights
        /// the final stand past the last day. Expeditions play as in <see cref="Expedition"/>. Reports how runs end and when, and the gold. The policy's
        /// numbers are simulator tools, not game rules.
        /// </summary>
        /// <param name="build">Whether the lobby policy upgrades the town's buildings (stage 30): the cheapest next level whose price leaves the hire price in hand, the stagecoach first on a tie, then the barracks, the inn (the gate has no level since 2026-10-18).</param>
        public static void Run(StaticData data, SimPolicy policy, int runs, ulong baseSeed, int restAbove, int bench, int treatAbove, bool build = true)
        {
            string[] buildOrder = { BuildingId.Coach, BuildingId.Barracks, BuildingId.Inn };
            long upgrades = 0, goldBuilt = 0, finished = 0;
            var levelsAtEndBy = new Dictionary<string, long>(StringComparer.Ordinal);
            foreach (string id in BuildingId.All)
            {
                levelsAtEndBy[id] = 0;
            }

            int victories = 0, wipeouts = 0, deathLimits = 0, finalStandsLost = 0, finalStands = 0, finalStandsWon = 0, unlocked = 0, earlyWins = 0;
            long days = 0, expeditions = 0, deaths = 0, rests = 0, unlockDays = 0;
            long goldEarned = 0, goldHired = 0, goldTreated = 0, goldLeft = 0, hires = 0, treatments = 0, rosterAtEnd = 0, hiredDeaths = 0;
            long levelsAtEnd = 0, levelUps = 0, highestLevel = 0, runsAtMaxLevel = 0, runsWithLiving = 0;
            var statsAtEnd = new long[StatBlock.All.Length];
            long hiredStrengths = 0, hiredWeaknesses = 0, strengthsAtEnd = 0, weaknessesAtEnd = 0;
            // Slice D stage 38: how often a survivor met each source of acquired traits at a settlement, the traits each left, and those the living hold at the end.
            TraitSource[] sources = (TraitSource[])Enum.GetValues(typeof(TraitSource));
            var sourcesMet = new long[sources.Length];
            var traitsGained = new long[sources.Length];
            long acquiredStrengthsAtEnd = 0, acquiredWeaknessesAtEnd = 0;
            long finalExpeditions = 0, finalCleared = 0, finalWiped = 0, finalRetreated = 0, finalDeaths = 0, runsThatClearedFinal = 0;
            var scratch = new ExpeditionStats();
            for (int run = 0; run < runs; run++)
            {
                RunState state = RunRules.NewRun(data, SeedDeriver.Derive(baseSeed, "run", run));
                var hired = new HashSet<string>(StringComparer.Ordinal);
                bool noted = false;
                int guard = 0;
                while (!state.IsOver && guard++ < 10000)
                {
                    // The stagecoach first: a fresh recruit may be the least tired of all. The candidate with the fewest weaknesses goes first,
                    // then the one with the most strengths (Slice D stage 37), then the first.
                    while (state.Roster.Count < data.Balance.PartySize + bench && RunRules.CanHire(data, state, 0) == HireCheck.Ok)
                    {
                        MercenaryState recruit = RunRules.Hire(data, state, BestCandidate(data, state));
                        hired.Add(recruit.Id);
                        hires++;
                        goldHired += data.Balance.HirePrice;
                        hiredStrengths += Count(data, recruit.Traits, TraitPolarity.Positive);
                        hiredWeaknesses += Count(data, recruit.Traits, TraitPolarity.Negative);
                    }

                    List<PartySlot> party = ChooseParty(data, state);
                    foreach (PartySlot slot in party)
                    {
                        MercenaryState member = RunRules.FindMercenary(state, slot.MercenaryId);
                        // Stage 48: the treatment ends an affliction whatever the fatigue, so the policy treats a member in one too.
                        while ((member.Fatigue >= treatAbove || member.AfflictionId != null) && RunRules.CanTreat(data, state, member.Id) == TreatCheck.Ok)
                        {
                            RunRules.Treat(data, state, member.Id);
                            treatments++;
                            goldTreated += data.Balance.TreatmentPrice;
                        }
                    }

                    // The buildings (stage 30): the cheapest next level the gold covers with a hire's price to spare, until none does.
                    while (build)
                    {
                        string pick = null;
                        int pickPrice = int.MaxValue;
                        foreach (string id in buildOrder)
                        {
                            BuildingData next = BuildingRules.NextLevel(data, state, id);
                            if (next != null && BuildingRules.CanUpgrade(data, state, id) == UpgradeCheck.Ok && state.Gold - next.Price >= data.Balance.HirePrice && next.Price < pickPrice)
                            {
                                pick = id;
                                pickPrice = next.Price;
                            }
                        }

                        if (pick == null)
                        {
                            break;
                        }

                        BuildingRules.Upgrade(data, state, pick);
                        upgrades++;
                        goldBuilt += pickPrice;
                    }

                    party = ChooseParty(data, state);
                    RunRules.SetParty(data, state, party);
                    if (RunRules.CanRest(data, state) && AverageFatigue(state, party) >= restAbove)
                    {
                        finished += RunRules.Rest(data, state).Count;
                        rests++;
                        continue;
                    }

                    DungeonData dungeon = ChooseDungeon(data, state);
                    if (dungeon == null)
                    {
                        break;
                    }

                    if (dungeon.IsFinal && !noted && !RunRules.IsFinalStandDue(data, state))
                    {
                        unlocked++;
                        unlockDays += state.Day;
                        noted = true;
                    }

                    ExpeditionState expedition = RunRules.BeginExpedition(data, state, dungeon.Id);
                    finalStands += expedition.FinalStand ? 1 : 0;
                    PlayExpedition(data, expedition, policy, scratch);
                    if (dungeon.IsFinal)
                    {
                        // The final dungeon on its own (stage 28): how the boss and its court fare against the roster a run brings.
                        finalExpeditions++;
                        finalCleared += expedition.Result == ExpeditionResult.Cleared ? 1 : 0;
                        finalWiped += expedition.Result == ExpeditionResult.Wiped ? 1 : 0;
                        finalRetreated += expedition.Result == ExpeditionResult.Retreated ? 1 : 0;
                        finalDeaths += expedition.Members.Count(m => !m.Alive);
                        runsThatClearedFinal += expedition.Result == ExpeditionResult.Cleared ? 1 : 0;
                    }

                    deaths += expedition.Members.Count(m => !m.Alive);
                    hiredDeaths += expedition.Members.Count(m => !m.Alive && hired.Contains(m.MercenaryId));
                    expeditions++;
                    foreach (ExpeditionMember member in expedition.Members)
                    {
                        foreach (TraitSource source in TraitRules.SourcesOf(data, expedition.Result, member))
                        {
                            sourcesMet[(int)source]++;
                        }
                    }

                    SettlementReport report = RunRules.Settle(data, state, expedition);
                    foreach (AcquiredTraits gained in report.NewTraits)
                    {
                        foreach (string id in gained.TraitIds)
                        {
                            traitsGained[(int)data.Traits.Get(id).Source.Value]++;
                        }
                    }

                    goldEarned += report.GoldEarned;
                    finished += report.BuildingsCompleted.Count;
                    // Slice D stage 35: the barracks levels up by hand; every policy levels everyone up right after the settlement, the roster's order.
                    foreach (MercenaryState mercenary in state.Roster)
                    {
                        while (RunRules.CanLevelUp(data, state, mercenary.Id))
                        {
                            RunRules.LevelUp(data, state, mercenary.Id);
                            levelUps++;
                        }
                    }
                    if (expedition.FinalStand && state.Outcome == RunOutcome.Victory)
                    {
                        finalStandsWon++;
                    }
                    else if (state.Outcome == RunOutcome.Victory)
                    {
                        earlyWins++;
                    }
                }

                days += state.Day;
                goldLeft += state.Gold;
                foreach (string id in BuildingId.All)
                {
                    levelsAtEndBy[id] += BuildingRules.LevelOf(state, id);
                }

                rosterAtEnd += state.Roster.Count;
                int highest = state.Roster.Count == 0 ? 0 : state.Roster.Max(m => m.Level);
                levelsAtEnd += state.Roster.Sum(m => m.Level);
                foreach (MercenaryState living in state.Roster)
                {
                    strengthsAtEnd += Count(data, living.Traits, TraitPolarity.Positive);
                    weaknessesAtEnd += Count(data, living.Traits, TraitPolarity.Negative);
                    acquiredStrengthsAtEnd += living.Traits.Count(id => data.Traits.Get(id).Kind == TraitKind.Acquired && data.Traits.Get(id).Polarity == TraitPolarity.Positive);
                    acquiredWeaknessesAtEnd += living.Traits.Count(id => data.Traits.Get(id).Kind == TraitKind.Acquired && data.Traits.Get(id).Polarity == TraitPolarity.Negative);
                    StatBlock stats = living.Stats ?? StatRules.BaseOf(data, living.JobId);
                    for (int i = 0; i < StatBlock.All.Length; i++)
                    {
                        statsAtEnd[i] += stats[StatBlock.All[i]];
                    }
                }
                highestLevel += highest;
                runsWithLiving += state.Roster.Count > 0 ? 1 : 0;
                runsAtMaxLevel += highest >= data.MaxLevel ? 1 : 0;
                if (state.Outcome == RunOutcome.Victory)
                {
                    victories++;
                }
                else
                {
                    switch (state.DefeatCause)
                    {
                        case DefeatCause.Wipeout: wipeouts++; break;
                        case DefeatCause.DeathLimit: deathLimits++; break;
                        case DefeatCause.FinalStandLost: finalStandsLost++; break;
                    }
                }
            }

            Console.WriteLine($"runs {runs}, policy {policy.Name}, rest when the party's fatigue averages {restAbove} or more, hire up to {data.Balance.PartySize + bench} in the roster, treat a member at {treatAbove} or more or in an affliction, seed {baseSeed}");
            Console.WriteLine($"  victories {BattleStats.Percent(victories, runs)} (before the last day {BattleStats.Percent(earlyWins, runs)}, at the final stand {BattleStats.Percent(finalStandsWon, runs)}); "
                + $"defeats: final stand lost {BattleStats.Percent(finalStandsLost, runs)}, death limit {BattleStats.Percent(deathLimits, runs)}, wipeout {BattleStats.Percent(wipeouts, runs)}");
            Console.WriteLine($"  per run: days at the end {Average(days, runs)}, expeditions {Average(expeditions, runs)}, rests {Average(rests, runs)}, deaths {Average(deaths, runs)}; "
                + $"final stands fought {BattleStats.Percent(finalStands, runs)}; the final dungeon opened in {BattleStats.Percent(unlocked, runs)} of runs, on day {Average(unlockDays, unlocked)} on average");
            Console.WriteLine($"  gold per run: earned {Average(goldEarned, runs)}, spent on hiring {Average(goldHired, runs)} ({Average(hires, runs)} hires, of whom {Average(hiredDeaths, runs)} died), "
                + $"on treatment {Average(goldTreated, runs)} ({Average(treatments, runs)} treatments), held at the end {Average(goldLeft, runs)}; roster at the end {Average(rosterAtEnd, runs)}");
            Console.WriteLine($"  final dungeon (stage 28): expeditions {Average(finalExpeditions, runs)} per run, of which cleared {BattleStats.Percent((int)finalCleared, (int)finalExpeditions)}, "
                + $"wiped {BattleStats.Percent((int)finalWiped, (int)finalExpeditions)}, retreated {BattleStats.Percent((int)finalRetreated, (int)finalExpeditions)}, deaths per expedition {Average(finalDeaths, (int)finalExpeditions)}; "
                + $"runs that cleared it {BattleStats.Percent((int)runsThatClearedFinal, runs)}");
            Console.WriteLine($"  buildings (stage 30): {(build ? "built" : "not built")}; upgrades per run {Average(upgrades, runs)} (finished {Average(finished, runs)}), gold spent {Average(goldBuilt, runs)}; "
                + "levels at the end " + string.Join(", ", BuildingId.All.Select(id => $"{id} {Average(levelsAtEndBy[id], runs)}")));
            Console.WriteLine($"  levels (stage 27): level-ups per run {Average(levelUps, runs)}, the living at the end average level {Average(levelsAtEnd, (int)rosterAtEnd)}, "
                + $"the highest {Average(highestLevel, (int)runsWithLiving)} on average (runs with someone living), at the last level ({data.MaxLevel}) in {BattleStats.Percent((int)runsAtMaxLevel, runs)} of runs");
            Console.WriteLine("  stats (Slice D stage 32): the living at the end average "
                + string.Join(", ", StatBlock.All.Select((stat, i) => $"{stat} {Average(statsAtEnd[i], (int)rosterAtEnd)}")));
            Console.WriteLine($"  traits (Slice D stage 37): a hire brings {Average(hiredStrengths, (int)hires)} strengths and {Average(hiredWeaknesses, (int)hires)} weaknesses; "
                + $"the living at the end have {Average(strengthsAtEnd, (int)rosterAtEnd)} and {Average(weaknessesAtEnd, (int)rosterAtEnd)}");
            Console.WriteLine("  acquired traits (Slice D stage 38), per run: a survivor met / a trait was left — "
                + string.Join(", ", sources.Select(source => $"{source} {Average(sourcesMet[(int)source], runs)} / {Average(traitsGained[(int)source], runs)}"))
                + $"; the living at the end hold {Average(acquiredStrengthsAtEnd, (int)rosterAtEnd)} acquired strengths and {Average(acquiredWeaknessesAtEnd, (int)rosterAtEnd)} weaknesses");
        }

        /// <summary>The run policy's pick at the stagecoach (stage 37): the fewest weaknesses, then the most strengths, then the first.</summary>
        static int BestCandidate(StaticData data, RunState state)
        {
            int best = 0;
            for (int i = 1; i < state.Recruits.Count; i++)
            {
                List<string> traits = state.Recruits[i].Traits, bestTraits = state.Recruits[best].Traits;
                int weaker = Count(data, traits, TraitPolarity.Negative) - Count(data, bestTraits, TraitPolarity.Negative);
                if (weaker < 0 || (weaker == 0 && Count(data, traits, TraitPolarity.Positive) > Count(data, bestTraits, TraitPolarity.Positive)))
                {
                    best = i;
                }
            }

            return best;
        }

        static int Count(StaticData data, IReadOnlyList<string> traitIds, TraitPolarity polarity)
        {
            return traitIds.Count(id => data.Traits.Get(id).Polarity == polarity);
        }

        /// <summary>The final dungeon once it is open (or once the final stand is due), else the first dungeon that is not final and is open.</summary>
        static DungeonData ChooseDungeon(StaticData data, RunState state)
        {
            DungeonData final = data.FinalDungeon;
            if (RunRules.IsFinalStandDue(data, state))
            {
                return final;
            }

            if (final != null && RunRules.IsUnlocked(state, final))
            {
                return final;
            }

            return data.Dungeons.Ordered.FirstOrDefault(d => !d.IsFinal && RunRules.IsUnlocked(state, d));
        }

        /// <summary>
        /// Up to PartySize of the living, the least tired first, lined up front to back by each job's recommended row.
        /// Slice D stage 47 (eight jobs, three of them for the front): a party without a healer or without anyone for the front does not get through the mine
        /// (Design/20 §5), and a player keeps one of each — so does the policy: the least tired healer (a job whose base weapon heals) and the least tired
        /// front-row job (recommended row 1 or 2) go first when the roster has them, the rest by fatigue as before.
        /// </summary>
        static List<PartySlot> ChooseParty(StaticData data, RunState state)
        {
            List<MercenaryState> byFatigue = state.Roster.OrderBy(m => m.Fatigue).ThenBy(m => m.Id, StringComparer.Ordinal).ToList();
            var picked = new List<MercenaryState>();
            MercenaryState healer = byFatigue.FirstOrDefault(m => Heals(data, m.JobId));
            if (healer != null)
            {
                picked.Add(healer);
            }

            MercenaryState front = byFatigue.FirstOrDefault(m => !picked.Contains(m) && data.Jobs.Get(m.JobId).RecommendedRow <= 2);
            if (front != null)
            {
                picked.Add(front);
            }

            foreach (MercenaryState member in byFatigue)
            {
                if (picked.Count >= data.Balance.PartySize)
                {
                    break;
                }

                if (!picked.Contains(member))
                {
                    picked.Add(member);
                }
            }

            List<MercenaryState> chosen = picked
                .OrderBy(m => data.Jobs.Get(m.JobId).RecommendedRow).ThenBy(m => m.Id, StringComparer.Ordinal)
                .ToList();
            var party = new List<PartySlot>();
            for (int i = 0; i < chosen.Count; i++)
            {
                party.Add(new PartySlot { MercenaryId = chosen[i].Id, Row = i + 1 });
            }

            return party;
        }

        /// <summary>Whether a job's base weapon heals (the priest's staff): the party's healer for <see cref="ChooseParty"/>.</summary>
        static bool Heals(StaticData data, string jobId)
        {
            return data.Items.Get(data.Jobs.Get(jobId).WeaponItemId).Effects.Any(e => e.Kind == EffectKind.Heal);
        }

        static int AverageFatigue(RunState state, List<PartySlot> party)
        {
            return party.Count == 0 ? 0 : (int)party.Average(slot => RunRules.FindMercenary(state, slot.MercenaryId).Fatigue);
        }

        /// <summary>Whether a unique item is on a living member's board or in the inventory at the end (stage 50: the boards may hold more than one).</summary>
        static bool HoldsAUnique(ExpeditionState state)
        {
            return state.Members.Any(m => m.Alive && m.Board.Items.Any(i => i.Item.Item.Unique)) || state.Inventory.Items.Any(i => i.Item.Item.Unique);
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
                bool brokeDown = PlayExpedition(data, state, policy, stats);

                stats.CoinsLeft += state.Coins;

                stats.UniqueAtEnd += HoldsAUnique(state) ? 1 : 0;
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
                stats.ItemsOnBoards += state.Members.Sum(m => m.Board.Items.Count);
                foreach (BoardItem placed in state.Members.SelectMany(m => m.Board.Items))
                {
                    stats.TiersAtEnd[(int)placed.Item.Tier]++;
                }

                foreach (ExpeditionMember member in state.Members)
                {
                    stats.BagSquares += member.Board.Bags.Sum(bag => bag.Bag.Area);
                    stats.BagsAdded += member.Board.Bags.Count(bag => !bag.Bag.Start);
                    stats.MembersAtEnd++;
                    foreach (BoardItem placed in member.Board.Items)
                    {
                        if (placed.Item.Item.Stars.Count > 0)
                        {
                            stats.StarItems++;
                            stats.StarsLit += StarRules.Lit(member.Board, placed).Count;
                        }

                        stats.StarDamage += StarRules.DamageOn(member.Board, placed);
                        if (placed.Item.Item.Trigger == ItemTrigger.BattleStart)
                        {
                            stats.StartItems++;
                            stats.StartItemsFront += member.Row == 1 ? 1 : 0;
                        }
                    }
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

        /// <summary>The average of values kept in hundredths (Slice B stage 22), as the number it stands for.</summary>
        static string AverageCenti(long hundredths, int count)
        {
            return count == 0 ? "-" : $"{(double)hundredths / count / Centi.One:F2}";
        }

        static string Describe(List<PartyMember> party)
        {
            return string.Join(",", party.Select(m => $"{m.MercenaryId}({m.JobId}):{m.Row}"));
        }
    }
}
