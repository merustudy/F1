using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// An item as an enemy carries it (Slice B stage 22, Docs/Design/02_Combat_System.md §4): the item's own values times the enemy's
    /// power percent. Enemies carry the same items harder than they drop.
    /// </summary>
    public sealed class ItemGrant
    {
        [JsonConstructor]
        public ItemGrant(string itemId, int powerPercent)
        {
            ItemId = DataId.Require(itemId, "ItemGrant ItemId");
            if (powerPercent < 1)
            {
                throw new DataException($"Item '{itemId}': an enemy's power percent must be at least 1.");
            }

            PowerPercent = powerPercent;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string ItemId { get; }

        /// <summary>How hard the enemy carries the item, in percent of the item's values.</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public int PowerPercent { get; }
    }

    /// <summary>
    /// A phase of a boss (Slice C stage 28, Docs/Design/02_Combat_System.md §9): when the boss's HP falls to this percent of its maximum, the
    /// items it carries are replaced by these (their cooldowns start over). Phases are listed from the highest percent down.
    /// </summary>
    public sealed class EnemyPhase
    {
        [JsonConstructor]
        public EnemyPhase(int hpPercent, IReadOnlyList<ItemGrant> items)
        {
            if (hpPercent < 1 || hpPercent > 99)
            {
                throw new DataException($"Phase at {hpPercent}%: HpPercent must be 1..99.");
            }

            if (items == null || items.Count < 1 || items.Count > BoardFrame.Height)
            {
                throw new DataException($"Phase at {hpPercent}%: a phase carries 1..{BoardFrame.Height} items.");
            }

            HpPercent = hpPercent;
            Items = items;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public int HpPercent { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public IReadOnlyList<ItemGrant> Items { get; }
    }

    public sealed class EnemyData
    {
        public const string DefinitionName = "Enemy";

        /// <summary>FigureScale of an enemy drawn at the common size of a figure place, in percent.</summary>
        public const int DefaultFigureScale = 100;
        public const int MinFigureScale = 50;
        public const int MaxFigureScale = 300;

        [JsonConstructor]
        public EnemyData(string id, LocalizedText name, int level, int maxHp, IReadOnlyList<ItemGrant> items, string figure = null, int figureScale = DefaultFigureScale, IReadOnlyList<EnemyPhase> phases = null)
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

            if (items == null || items.Count < 1 || items.Count > BoardFrame.Height)
            {
                throw new DataException($"{DefinitionName} '{id}': an enemy carries 1..{BoardFrame.Height} items.");
            }

            if (figureScale < MinFigureScale || figureScale > MaxFigureScale)
            {
                throw new DataException($"{DefinitionName} '{id}': FigureScale must be {MinFigureScale}..{MaxFigureScale} (percent).");
            }

            // The phases (stage 28) come from the highest HP percent down, each lower than the one before.
            var ladder = new List<EnemyPhase>(phases ?? new List<EnemyPhase>());
            for (int i = 0; i < ladder.Count; i++)
            {
                if (ladder[i] == null)
                {
                    throw new DataException($"{DefinitionName} '{id}': a phase is missing.");
                }

                if (i > 0 && ladder[i].HpPercent >= ladder[i - 1].HpPercent)
                {
                    throw new DataException($"{DefinitionName} '{id}': the phases must come at falling HP percents ({ladder[i - 1].HpPercent}% then {ladder[i].HpPercent}%).");
                }
            }

            Level = level;
            MaxHp = maxHp;
            Items = items;
            Figure = ArtAddress.Optional(figure, $"{DefinitionName} '{id}'", nameof(Figure));
            FigureScale = figureScale;
            Phases = ladder;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Level { get; }

        /// <summary>In hundredths (stage 22).</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int MaxHp { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public IReadOnlyList<ItemGrant> Items { get; }

        /// <summary>The logical address of the enemy's full-body art. Null when it has no art yet.</summary>
        [JsonProperty(Order = 6, Required = Required.AllowNull)]
        public string Figure { get; }

        /// <summary>
        /// How large the enemy's figure is drawn, in percent of the common figure place (100 for most enemies; a boss
        /// is drawn larger and may overlap its neighbours). Presentation only: it changes no rule and no position.
        /// </summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public int FigureScale { get; }

        /// <summary>A boss's phases (stage 28): none for most enemies.</summary>
        [JsonProperty(Order = 8, Required = Required.Always)]
        public IReadOnlyList<EnemyPhase> Phases { get; }

        /// <summary>The address of the attack pose drawn after the figure (<see cref="ArtAddress.PoseOf"/>). Null when the enemy has no figure.</summary>
        [JsonIgnore]
        public string AttackPose => ArtAddress.PoseOf(Figure, ArtAddress.Attack);

        /// <summary>The address of the hit pose drawn after the figure. Null when the enemy has no figure.</summary>
        [JsonIgnore]
        public string HitPose => ArtAddress.PoseOf(Figure, ArtAddress.Hit);
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
            IReadOnlyList<string> enemies,
            bool isElite = false)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            DungeonId = DataId.Require(dungeonId, $"{DefinitionName} '{id}' DungeonId");

            if (isBoss && isElite)
            {
                throw new DataException($"{DefinitionName} '{id}': a group is a boss group or an elite group, not both.");
            }

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
            IsElite = isElite;
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

        /// <summary>An elite group waits on the map's elite nodes, never on a battle node (Docs/Design/03_Dungeon_Structure.md §1).</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public bool IsElite { get; }
    }
}
