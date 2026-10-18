using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// One row of LevelData.csv (Slice C stage 27, Docs/Design/05_Mercenary_Growth.md §2): a mercenary's level and the experience it takes to reach
    /// the next level (0 at the last level). StaticData keeps the rows in level order and checks that they run from 1 without a gap; what the levels
    /// do is LevelRules. Slice D stage 34 took the maximum HP bonus out: a level grows the stats (JobData.Growth), and constitution sets the HP.
    /// </summary>
    public sealed class LevelData
    {
        public const string DefinitionName = "Level";

        [JsonConstructor]
        public LevelData(int level, int expToNext)
        {
            if (level < 1)
            {
                throw new DataException($"{DefinitionName}: Level {level} must be at least 1.");
            }

            if (expToNext < 0)
            {
                throw new DataException($"{DefinitionName} {level}: ExpToNext cannot be negative.");
            }

            Level = level;
            ExpToNext = expToNext;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public int Level { get; }

        /// <summary>The experience that takes a mercenary from this level to the next; 0 at the last level.</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public int ExpToNext { get; }

        /// <summary>True at the last level: no experience takes the mercenary further.</summary>
        public bool IsLast => ExpToNext == 0;
    }
}
