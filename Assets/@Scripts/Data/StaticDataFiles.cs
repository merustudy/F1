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

        public static readonly Entry Balance = new Entry(BalanceData.DefinitionName, "balance");
        public static readonly Entry Job = new Entry(JobData.DefinitionName, "job");
        public static readonly Entry Item = new Entry(ItemData.DefinitionName, "item");
        public static readonly Entry Bag = new Entry(BagData.DefinitionName, "bag");
        public static readonly Entry Potion = new Entry(PotionData.DefinitionName, "potion");
        public static readonly Entry Enemy = new Entry(EnemyData.DefinitionName, "enemy");
        public static readonly Entry EnemyGroup = new Entry(EnemyGroupData.DefinitionName, "enemy-group");
        public static readonly Entry Affinity = new Entry(AffinityData.DefinitionName, "affinity");
        public static readonly Entry Dungeon = new Entry(DungeonData.DefinitionName, "dungeon");
        public static readonly Entry Mercenary = new Entry(MercenaryData.DefinitionName, "mercenary");
        public static readonly Entry FatigueState = new Entry(FatigueStateData.DefinitionName, "fatigue-state");

        public static readonly IReadOnlyList<Entry> All = new[]
        {
            Balance,
            Job,
            Item,
            Bag,
            Potion,
            Enemy,
            EnemyGroup,
            Affinity,
            Dungeon,
            Mercenary,
            FatigueState,
        };
    }
}
