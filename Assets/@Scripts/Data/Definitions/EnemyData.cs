using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>An item at a grade, as carried by an enemy.</summary>
    public sealed class ItemGrant
    {
        [JsonConstructor]
        public ItemGrant(string itemId, int grade)
        {
            ItemId = DataId.Require(itemId, "ItemGrant ItemId");
            if (grade < 1)
            {
                throw new DataException($"Item '{itemId}': grade must be at least 1.");
            }

            Grade = grade;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string ItemId { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Grade { get; }
    }

    public sealed class EnemyData
    {
        public const string DefinitionName = "Enemy";

        [JsonConstructor]
        public EnemyData(string id, LocalizedText name, int level, int maxHp, IReadOnlyList<ItemGrant> items, string figure = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (level < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': Level must be at least 1.");
            }

            if (maxHp < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': MaxHp must be at least 1.");
            }

            if (items == null || items.Count < 1 || items.Count > JobData.MaxItemSlots)
            {
                throw new DataException($"{DefinitionName} '{id}': an enemy carries 1..{JobData.MaxItemSlots} items.");
            }

            Level = level;
            MaxHp = maxHp;
            Items = items;
            Figure = ArtAddress.Optional(figure, $"{DefinitionName} '{id}'", nameof(Figure));
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Level { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int MaxHp { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public IReadOnlyList<ItemGrant> Items { get; }

        /// <summary>The logical address of the enemy's full-body art. Null when it has no art yet.</summary>
        [JsonProperty(Order = 6, Required = Required.AllowNull)]
        public string Figure { get; }

        /// <summary>The address of the face cut out of the figure (<see cref="ArtAddress.FaceOf"/>). Null when the enemy has no figure.</summary>
        [JsonIgnore]
        public string Face => ArtAddress.FaceOf(Figure);
    }

    /// <summary>A set of enemies that fights together, and where in a dungeon it appears.</summary>
    public sealed class EnemyGroupData
    {
        public const string DefinitionName = "EnemyGroup";

        [JsonConstructor]
        public EnemyGroupData(
            string id,
            string dungeonId,
            int minFloor,
            int maxFloor,
            bool isBoss,
            IReadOnlyList<string> enemies)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            DungeonId = DataId.Require(dungeonId, $"{DefinitionName} '{id}' DungeonId");

            if (isBoss)
            {
                if (minFloor != 0 || maxFloor != 0)
                {
                    throw new DataException($"{DefinitionName} '{id}': a boss group has MinFloor and MaxFloor 0.");
                }
            }
            else if (minFloor < 1 || maxFloor < minFloor)
            {
                throw new DataException($"{DefinitionName} '{id}': floors must satisfy 1 <= MinFloor <= MaxFloor.");
            }

            if (enemies == null || enemies.Count < 1 || enemies.Count > BattleRows.Count)
            {
                throw new DataException($"{DefinitionName} '{id}': a group has 1..{BattleRows.Count} enemies, one per row.");
            }

            foreach (string enemyId in enemies)
            {
                DataId.Require(enemyId, $"{DefinitionName} '{id}' Enemies");
            }

            MinFloor = minFloor;
            MaxFloor = maxFloor;
            IsBoss = isBoss;
            Enemies = enemies;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public string DungeonId { get; }

        /// <summary>First battle floor (1-based) this group can appear on. 0 for a boss group.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int MinFloor { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int MaxFloor { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public bool IsBoss { get; }

        /// <summary>Enemy ids from the front: the first stands in row 1, the next in row 2 and so on.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public IReadOnlyList<string> Enemies { get; }
    }
}
