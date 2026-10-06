using System;
using System.Collections.Generic;

namespace F1.Data
{
    /// <summary>
    /// Builds <see cref="StaticData"/> from generated JSON, and writes it back. The game (through
    /// DataManager) and the simulator (from files on disk) both go through <see cref="Load"/>.
    /// </summary>
    public static class StaticDataLoader
    {
        /// <param name="readJson">Returns the generated JSON text of a definition.</param>
        public static StaticData Load(Func<StaticDataFiles.Entry, string> readJson)
        {
            if (readJson == null)
            {
                throw new ArgumentNullException(nameof(readJson));
            }

            return new StaticData(new StaticDataParts
            {
                Balance = Read<BalanceEntry>(StaticDataFiles.Balance, readJson),
                Jobs = Read<JobData>(StaticDataFiles.Job, readJson),
                Items = Read<ItemData>(StaticDataFiles.Item, readJson),
                Potions = Read<PotionData>(StaticDataFiles.Potion, readJson),
                Enemies = Read<EnemyData>(StaticDataFiles.Enemy, readJson),
                EnemyGroups = Read<EnemyGroupData>(StaticDataFiles.EnemyGroup, readJson),
                Affinities = Read<AffinityData>(StaticDataFiles.Affinity, readJson),
                Dungeons = Read<DungeonData>(StaticDataFiles.Dungeon, readJson),
                Mercenaries = Read<MercenaryData>(StaticDataFiles.Mercenary, readJson),
                FatigueStates = Read<FatigueStateData>(StaticDataFiles.FatigueState, readJson),
            });
        }

        /// <summary>Generated file name -> JSON text for every definition, items in id order.</summary>
        public static Dictionary<string, string> Serialize(StaticData data)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal)
            {
                { StaticDataFiles.Balance.GeneratedFileName, StaticDataJson.Serialize(data.Balance.ToEntries()) },
                { StaticDataFiles.Job.GeneratedFileName, StaticDataJson.Serialize(new List<JobData>(data.Jobs.Ordered)) },
                { StaticDataFiles.Item.GeneratedFileName, StaticDataJson.Serialize(new List<ItemData>(data.Items.Ordered)) },
                { StaticDataFiles.Potion.GeneratedFileName, StaticDataJson.Serialize(new List<PotionData>(data.Potions.Ordered)) },
                { StaticDataFiles.Enemy.GeneratedFileName, StaticDataJson.Serialize(new List<EnemyData>(data.Enemies.Ordered)) },
                { StaticDataFiles.EnemyGroup.GeneratedFileName, StaticDataJson.Serialize(new List<EnemyGroupData>(data.EnemyGroups.Ordered)) },
                { StaticDataFiles.Affinity.GeneratedFileName, StaticDataJson.Serialize(new List<AffinityData>(data.Affinities.Ordered)) },
                { StaticDataFiles.Dungeon.GeneratedFileName, StaticDataJson.Serialize(new List<DungeonData>(data.Dungeons.Ordered)) },
                { StaticDataFiles.Mercenary.GeneratedFileName, StaticDataJson.Serialize(new List<MercenaryData>(data.Mercenaries.Ordered)) },
                { StaticDataFiles.FatigueState.GeneratedFileName, StaticDataJson.Serialize(new List<FatigueStateData>(data.FatigueStates.Ordered)) },
            };
        }

        static List<T> Read<T>(StaticDataFiles.Entry file, Func<StaticDataFiles.Entry, string> readJson)
        {
            return StaticDataJson.Deserialize<T>(readJson(file), file.GeneratedFileName);
        }
    }
}
