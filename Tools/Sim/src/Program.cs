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
                    case "trace": return Trace(store, options);
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

        const string DefaultParty = "knight,bishop,spellblade";

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
            Console.WriteLine("  trace        Event log of one battle");
            Console.WriteLine("               --dungeon id  --group id  --party a,b,c  --policy none|balanced|safe  --seed n");
            Console.WriteLine("  A party member is a mercenary id or a job id, optionally with :Front or :Rear.");
            Console.WriteLine("  Common: --project-root <path>");
        }
    }
}
