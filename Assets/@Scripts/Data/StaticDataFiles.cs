using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>
    /// The list of static data definitions. The transformer, the loader and the Addressables entry
    /// list all read this, so a definition is declared in exactly one place.
    /// </summary>
    public static class StaticDataFiles
    {
        public const int SchemaVersion = 1;
        public const string SourceDirectory = "Assets/@Data/Source";
        public const string GeneratedDirectory = "Assets/@Data/Generated";

        public sealed class Entry
        {
            public Entry(string name, string addressName)
            {
                Name = name;
                SourceFileName = name + "Data.csv";
                GeneratedFileName = name + "Data.json";
                Address = "data/app/" + addressName;
            }

            /// <summary>Definition name, for example "Job".</summary>
            public string Name { get; }
            public string SourceFileName { get; }
            public string GeneratedFileName { get; }
            /// <summary>Logical address of the generated JSON.</summary>
            public string Address { get; }
        }

        public static readonly Entry Job = new Entry(JobData.DefinitionName, "job");

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            Job,
        };
    }
}
