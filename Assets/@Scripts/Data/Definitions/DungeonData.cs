using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>A region affinity: a difficulty modifier applied to the enemies of a dungeon.</summary>
    public sealed class AffinityData
    {
        public const string DefinitionName = "Affinity";

        [JsonConstructor]
        public AffinityData(string id, LocalizedText name, int enemyCooldownPermille)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (enemyCooldownPermille <= -1000 || enemyCooldownPermille > 1000)
            {
                throw new DataException($"{DefinitionName} '{id}': EnemyCooldownPermille must be within -999..1000.");
            }

            EnemyCooldownPermille = enemyCooldownPermille;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        /// <summary>Change to enemy item cooldowns in thousandths. -80 means 8% faster.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int EnemyCooldownPermille { get; }
    }

    public sealed class DungeonData
    {
        public const string DefinitionName = "Dungeon";
        public const int MaxMapWidth = 4;

        [JsonConstructor]
        public DungeonData(
            string id,
            LocalizedText name,
            string affinityId,
            int floors,
            int mapMinWidth,
            int mapMaxWidth,
            int fatigueCost,
            int durationDays,
            int itemGradeBase,
            int itemGradePerFloor,
            IReadOnlyList<string> startingPotions,
            string background = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            AffinityId = DataId.Require(affinityId, $"{DefinitionName} '{id}' AffinityId");

            if (floors < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': Floors must be at least 1.");
            }

            if (mapMinWidth < 1 || mapMaxWidth < mapMinWidth || mapMaxWidth > MaxMapWidth)
            {
                throw new DataException($"{DefinitionName} '{id}': map width must satisfy 1 <= Min <= Max <= {MaxMapWidth}.");
            }

            if (fatigueCost < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': FatigueCost cannot be negative.");
            }

            if (durationDays < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': DurationDays must be at least 1.");
            }

            if (itemGradeBase < 1 || itemGradePerFloor < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': item grades must be positive.");
            }

            if (startingPotions == null)
            {
                throw new DataException($"{DefinitionName} '{id}': StartingPotions is missing.");
            }

            foreach (string potionId in startingPotions)
            {
                DataId.Require(potionId, $"{DefinitionName} '{id}' StartingPotions");
            }

            Floors = floors;
            MapMinWidth = mapMinWidth;
            MapMaxWidth = mapMaxWidth;
            FatigueCost = fatigueCost;
            DurationDays = durationDays;
            ItemGradeBase = itemGradeBase;
            ItemGradePerFloor = itemGradePerFloor;
            StartingPotions = startingPotions;
            Background = ArtAddress.Optional(background, $"{DefinitionName} '{id}'", nameof(Background));
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public string AffinityId { get; }

        /// <summary>Number of battle floors. The boss floor comes after them.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Floors { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int MapMinWidth { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int MapMaxWidth { get; }

        [JsonProperty(Order = 7, Required = Required.Always)]
        public int FatigueCost { get; }

        [JsonProperty(Order = 8, Required = Required.Always)]
        public int DurationDays { get; }

        [JsonProperty(Order = 9, Required = Required.Always)]
        public int ItemGradeBase { get; }

        [JsonProperty(Order = 10, Required = Required.Always)]
        public int ItemGradePerFloor { get; }

        [JsonProperty(Order = 11, Required = Required.Always)]
        public IReadOnlyList<string> StartingPotions { get; }

        /// <summary>The logical address of the background its battles are fought in front of. Null when it has no art yet.</summary>
        [JsonProperty(Order = 12, Required = Required.AllowNull)]
        public string Background { get; }

        /// <summary>Grade of reward items offered after winning on a battle floor (1-based).</summary>
        public int RewardGradeAt(int floor)
        {
            return ItemGradeBase + (floor - 1) * ItemGradePerFloor;
        }
    }

    /// <summary>A mercenary of the starting roster.</summary>
    public sealed class MercenaryData
    {
        public const string DefinitionName = "Mercenary";

        [JsonConstructor]
        public MercenaryData(string id, LocalizedText name, string jobId)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            JobId = DataId.Require(jobId, $"{DefinitionName} '{id}' JobId");
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public string JobId { get; }
    }
}
