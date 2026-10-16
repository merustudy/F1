using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;

namespace F1.Sim
{
    /// <summary>
    /// Unity-free entry point for static data and simulation. It compiles the same data, transform
    /// and gameplay code as the game and reads the same generated JSON.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            try
            {
                Dictionary<string, string> options = ParseOptions(args);
                string projectRoot = FindProjectRoot(options);
                var store = new StaticDataFileStore(projectRoot);

                switch (args[0])
                {
                    case "transform": return Transform(store);
                    case "validate": return Validate(store);
                    case "battle": return Battle(store, options);
                    case "expedition": return Expedition(store, options);
                    case "run": return Run(store, options);
                    case "formations": return Formations(store, options);
                    case "trace": return Trace(store, options);
                    case "map": return Map(store, options);
                    case "items": return Items(store);
                    default:
                        PrintUsage();
                        return 2;
                }
            }
            catch (DataTransformException exception)
            {
                foreach (string error in exception.Errors)
                {
                    Console.Error.WriteLine(error);
                }

                Console.Error.WriteLine($"FAILED: {exception.Errors.Count} error(s). Nothing was written.");
                return 1;
            }
            catch (Exception exception) when (
                exception is DataException || exception is DataValidationException
                || exception is ArgumentException || exception is KeyNotFoundException)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine("FAILED");
                return 1;
            }
        }

        static int Transform(StaticDataFileStore store)
        {
            TransformResult result = StaticDataTransformer.Transform(store.ReadSource);
            List<string> changed = store.WriteGenerated(result);
            Console.WriteLine(changed.Count == 0
                ? "OK: generated data is already up to date."
                : "OK: regenerated " + string.Join(", ", changed));
            return 0;
        }

        static int Validate(StaticDataFileStore store)
        {
            TransformResult result = StaticDataTransformer.Transform(store.ReadSource);
            List<string> stale = store.FindStale(result);
            if (stale.Count > 0)
            {
                Console.Error.WriteLine("Stale generated files (run transform): " + string.Join(", ", stale));
                return 1;
            }

            StaticData data = LoadGenerated(store);
            Console.WriteLine($"OK: generated data matches its sources and loads ({data.Jobs.Count} jobs, {data.Items.Count} items, {data.Enemies.Count} enemies).");
            return 0;
        }

        static int Battle(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            string dungeonId = Option(options, "dungeon", data.Dungeons.Ordered[0].Id);
            string groupId = Option(options, "group", data.BossGroupOf(dungeonId).Id);
            Simulations.Battle(
                data,
                dungeonId,
                groupId,
                Simulations.ParseParty(data, Option(options, "party", DefaultParty)),
                SimPolicy.Parse(Option(options, "policy", "balanced")),
                int.Parse(Option(options, "runs", "1000"), CultureInfo.InvariantCulture),
                ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture));
            return 0;
        }

        static int Expedition(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            Simulations.Expedition(
                data,
                Option(options, "dungeon", data.Dungeons.Ordered[0].Id),
                Simulations.ParseParty(data, Option(options, "party", DefaultParty)),
                SimPolicy.Parse(Option(options, "policy", "balanced")),
                int.Parse(Option(options, "runs", "1000"), CultureInfo.InvariantCulture),
                ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture));
            return 0;
        }

        static int Run(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            Simulations.Run(
                data,
                SimPolicy.Parse(Option(options, "policy", "balanced")),
                int.Parse(Option(options, "runs", "300"), CultureInfo.InvariantCulture),
                ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture),
                int.Parse(Option(options, "rest-above", "60"), CultureInfo.InvariantCulture),
                int.Parse(Option(options, "bench", "2"), CultureInfo.InvariantCulture),
                int.Parse(Option(options, "treat-above", "100"), CultureInfo.InvariantCulture));
            return 0;
        }

        static int Formations(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            Simulations.Formations(
                data,
                Option(options, "dungeon", data.Dungeons.Ordered[0].Id),
                Simulations.ParseParty(data, Option(options, "party", DefaultParty)),
                SimPolicy.Parse(Option(options, "policy", "balanced")),
                int.Parse(Option(options, "runs", "1000"), CultureInfo.InvariantCulture),
                ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture));
            return 0;
        }

        static int Trace(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            string dungeonId = Option(options, "dungeon", data.Dungeons.Ordered[0].Id);
            Simulations.Trace(
                data,
                dungeonId,
                Option(options, "group", data.BossGroupOf(dungeonId).Id),
                Simulations.ParseParty(data, Option(options, "party", DefaultParty)),
                SimPolicy.Parse(Option(options, "policy", "balanced")),
                ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture));
            return 0;
        }

        /// <summary>
        /// Every item's damage at Common as the size rule reads it (Slice B stage 22, Docs/Design/02_Combat_System.md §4): the range, the
        /// average on one unit, how many it hits, and the damage per square per second over all it hits. The dungeon's weapons follow
        /// "squares x 1/3 x cooldown / units hit" (1/3 a square a second); the rest are out of the rule.
        /// </summary>
        static int Items(StaticDataFileStore store)
        {
            StaticData data = LoadGenerated(store);
            Console.WriteLine("item            shape cooldown  range        avg/unit units  per square per second  (rule: 0.33)");
            foreach (ItemData item in data.Items.Ordered)
            {
                foreach (ItemEffect effect in item.Effects)
                {
                    if (effect.Kind != EffectKind.Damage)
                    {
                        continue;
                    }

                    int units = effect.Target == TargetMode.EnemyAll ? BattleRows.Count : effect.Reach;
                    double average = (effect.Min + effect.Max) / 2.0 / Centi.One;
                    double perSquare = average * units / (item.CooldownMs / 1000.0) / item.Area;
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture, "{0,-15} {1}x{2}  {3,5:F1}s   {4,-12} {5,6:F2} {6,5}  {7,8:F2}",
                        item.Id, item.Width, item.Height, item.CooldownMs / 1000.0, Centi.ToText(effect.Min) + "-" + Centi.ToText(effect.Max), average, units, perSquare));
                }
            }

            return 0;
        }

        /// <summary>Prints the map of one expedition: one node per line, "id floor column kind group next,ids".</summary>
        static int Map(StaticDataFileStore store, Dictionary<string, string> options)
        {
            StaticData data = LoadGenerated(store);
            DungeonData dungeon = data.Dungeons.Get(Option(options, "dungeon", data.Dungeons.Ordered[0].Id));
            NodeMap map = MapGenerator.Generate(data, dungeon, ulong.Parse(Option(options, "seed", "1"), CultureInfo.InvariantCulture));
            foreach (MapNode node in map.Nodes)
            {
                Console.WriteLine($"{node.Id} {node.Floor} {node.Column} {node.Kind} {node.EnemyGroupId ?? "-"} {string.Join(",", node.NextNodeIds)}");
            }

            return 0;
        }

        const string DefaultParty = "knight,spellblade,bishop,archmage";

        /// <summary>Loads static data exactly as the game does: generated JSON through StaticDataLoader.</summary>
        internal static StaticData LoadGenerated(StaticDataFileStore store)
        {
            return StaticDataLoader.Load(file =>
                store.ReadGenerated(file.GeneratedFileName)
                ?? throw new DataException($"{file.GeneratedFileName}: generated file is missing. Run transform."));
        }

        static Dictionary<string, string> ParseOptions(string[] args)
        {
            var options = new Dictionary<string, string>(StringComparer.Ordinal);
            for (int i = 1; i < args.Length; i++)
            {
                if (!args[i].StartsWith("--", StringComparison.Ordinal) || i + 1 >= args.Length)
                {
                    throw new ArgumentException($"Expected '--name value' but got '{args[i]}'.");
                }

                options[args[i].Substring(2)] = args[i + 1];
                i++;
            }

            return options;
        }

        static string Option(Dictionary<string, string> options, string name, string fallback)
        {
            return options.TryGetValue(name, out string value) ? value : fallback;
        }

        static string FindProjectRoot(Dictionary<string, string> options)
        {
            if (options.TryGetValue("project-root", out string root))
            {
                return Path.GetFullPath(root);
            }

            var directory = new DirectoryInfo(Directory.GetCurrentDirectory());
            while (directory != null)
            {
                if (Directory.Exists(Path.Combine(directory.FullName, "Assets"))
                    && Directory.Exists(Path.Combine(directory.FullName, "ProjectSettings")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new DataException("Unity project root was not found. Run from inside the repository or pass --project-root <path>.");
        }

        static void PrintUsage()
        {
            Console.WriteLine("Usage: dotnet run --project Tools/Sim -- <command> [--name value ...]");
            Console.WriteLine("  transform    Convert Assets/@Data/Source/*.csv to Assets/@Data/Generated/*.json");
            Console.WriteLine("  validate     Check that generated data is up to date and loads");
            Console.WriteLine("  battle       Many battles of a fresh party against one enemy group");
            Console.WriteLine("               --dungeon id  --group id  --party a,b,c  --policy none|balanced|safe  --runs n  --seed n");
            Console.WriteLine("  expedition   Many whole expeditions");
            Console.WriteLine("               --dungeon id  --party a,b,c  --policy none|balanced|safe  --runs n  --seed n");
            Console.WriteLine("  run          Whole runs to their end: a lobby policy hires, treats, picks the party, rests and the dungeon (Slice C stages 25-26)");
            Console.WriteLine("               --policy none|balanced|safe  --runs n  --seed n  --rest-above fatigue  --bench n (hire up to PartySize + n)  --treat-above fatigue");
            Console.WriteLine("  formations   Whole expeditions for every order of the party, front to back");
            Console.WriteLine("               --dungeon id  --party a,b,c  --policy none|balanced|safe  --runs n  --seed n");
            Console.WriteLine("  items        Every item's damage per square per second (the size rule of stage 22)");
            Console.WriteLine("  map          The map of one expedition, one node per line");
            Console.WriteLine("               --dungeon id  --seed n");
            Console.WriteLine("  trace        Event log of one battle");
            Console.WriteLine("               --dungeon id  --group id  --party a,b,c  --policy none|balanced|safe  --seed n");
            Console.WriteLine("  A party member is a mercenary id or a job id. Add a row (:1, :2, ...) to every member to set the rows;");
            Console.WriteLine("  without them the party lines up by each job's recommended row.");
            Console.WriteLine("  Common: --project-root <path>");
        }
    }
}
