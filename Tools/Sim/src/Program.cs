using System;
using System.Collections.Generic;
using System.IO;
using F1.Data;
using F1.Editor.Data;

namespace F1.Sim
{
    /// <summary>
    /// Unity-free entry point for static data and simulation.
    ///   dotnet run --project Tools/Sim -- transform    CSV -> generated JSON
    ///   dotnet run --project Tools/Sim -- validate     generated JSON is up to date and loads
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
                string projectRoot = FindProjectRoot(args);
                switch (args[0])
                {
                    case "transform": return Transform(projectRoot);
                    case "validate": return Validate(projectRoot);
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
            catch (Exception exception) when (exception is DataException || exception is DataValidationException)
            {
                Console.Error.WriteLine(exception.Message);
                Console.Error.WriteLine("FAILED");
                return 1;
            }
        }

        static int Transform(string projectRoot)
        {
            var store = new StaticDataFileStore(projectRoot);
            TransformResult result = StaticDataTransformer.Transform(store.ReadSource);
            List<string> changed = store.WriteGenerated(result);
            Console.WriteLine(changed.Count == 0
                ? "OK: generated data is already up to date."
                : "OK: regenerated " + string.Join(", ", changed));
            return 0;
        }

        static int Validate(string projectRoot)
        {
            var store = new StaticDataFileStore(projectRoot);
            TransformResult result = StaticDataTransformer.Transform(store.ReadSource);
            List<string> stale = store.FindStale(result);
            if (stale.Count > 0)
            {
                Console.Error.WriteLine("Stale generated files (run transform): " + string.Join(", ", stale));
                return 1;
            }

            StaticData data = LoadGenerated(store);
            Console.WriteLine($"OK: generated data matches its sources and loads ({data.Jobs.Count} jobs).");
            return 0;
        }

        /// <summary>Loads static data exactly as the game does: generated JSON through StaticDataLoader.</summary>
        internal static StaticData LoadGenerated(StaticDataFileStore store)
        {
            return StaticDataLoader.Load(file =>
                store.ReadGenerated(file.GeneratedFileName)
                ?? throw new DataException($"{file.GeneratedFileName}: generated file is missing. Run transform."));
        }

        static string FindProjectRoot(string[] args)
        {
            for (int i = 1; i < args.Length - 1; i++)
            {
                if (args[i] == "--project-root")
                {
                    return Path.GetFullPath(args[i + 1]);
                }
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
            Console.WriteLine("Usage: dotnet run --project Tools/Sim -- <command> [--project-root <path>]");
            Console.WriteLine("  transform   Convert Assets/@Data/Source/*.csv to Assets/@Data/Generated/*.json");
            Console.WriteLine("  validate    Check that generated data is up to date and loads");
        }
    }
}
