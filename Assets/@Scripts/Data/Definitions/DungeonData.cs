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
            int durationDays,
            int itemGradeBase,
            int itemGradePerFloor,
            IReadOnlyList<string> startingPotions,
            string background = null,
            int eliteMinFloor = 0,
            int eliteChancePercent = 0,
            int campMinFloor = 0,
            int campChancePercent = 0,
            int campFloor = 0,
            int enemyHpPerFloorPercent = 0,
            int enemyGradePerFloor = 0,
            int silverFloor = 0,
            int goldFloor = 0,
            int diamondFloor = 0)
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

            if (eliteMinFloor < 0 || campMinFloor < 0 || eliteChancePercent < 0 || eliteChancePercent > 100 || campChancePercent < 0 || campChancePercent > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': the elite and camp floors cannot be negative and their chances are 0..100.");
            }

            if (campFloor != 0 && (campFloor < 2 || campFloor > floors))
            {
                throw new DataException($"{DefinitionName} '{id}': CampFloor is 0 or a map floor from 2 (the first floor is battles only).");
            }

            if (enemyHpPerFloorPercent < 0 || enemyGradePerFloor < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': the deeper enemies cannot get weaker.");
            }

            int lastTierFloor = 0;
            foreach (int tierFloor in new[] { silverFloor, goldFloor, diamondFloor })
            {
                if (tierFloor == 0)
                {
                    continue;
                }

                if (tierFloor < 1 || tierFloor > floors || tierFloor <= lastTierFloor)
                {
                    throw new DataException($"{DefinitionName} '{id}': the floors where Silver, Gold and Diamond rewards start are 0 (never) or map floors, each deeper than the one before.");
                }

                lastTierFloor = tierFloor;
            }

            if ((goldFloor != 0 && silverFloor == 0) || (diamondFloor != 0 && goldFloor == 0))
            {
                throw new DataException($"{DefinitionName} '{id}': a tier's rewards cannot start where the tier under it never does.");
            }

            Floors = floors;
            MapMinWidth = mapMinWidth;
            MapMaxWidth = mapMaxWidth;
            DurationDays = durationDays;
            ItemGradeBase = itemGradeBase;
            ItemGradePerFloor = itemGradePerFloor;
            StartingPotions = startingPotions;
            Background = ArtAddress.Optional(background, $"{DefinitionName} '{id}'", nameof(Background));
            EliteMinFloor = eliteMinFloor;
            EliteChancePercent = eliteChancePercent;
            CampMinFloor = campMinFloor;
            CampChancePercent = campChancePercent;
            CampFloor = campFloor;
            EnemyHpPerFloorPercent = enemyHpPerFloorPercent;
            EnemyGradePerFloor = enemyGradePerFloor;
            SilverFloor = silverFloor;
            GoldFloor = goldFloor;
            DiamondFloor = diamondFloor;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public string AffinityId { get; }

        /// <summary>Number of map floors. The boss floor comes after them.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Floors { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int MapMinWidth { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int MapMaxWidth { get; }

        [JsonProperty(Order = 7, Required = Required.Always)]
        public int DurationDays { get; }

        [JsonProperty(Order = 8, Required = Required.Always)]
        public int ItemGradeBase { get; }

        [JsonProperty(Order = 9, Required = Required.Always)]
        public int ItemGradePerFloor { get; }

        [JsonProperty(Order = 10, Required = Required.Always)]
        public IReadOnlyList<string> StartingPotions { get; }

        /// <summary>The logical address of the background its battles are fought in front of. Null when it has no art yet.</summary>
        [JsonProperty(Order = 11, Required = Required.AllowNull)]
        public string Background { get; }

        /// <summary>
        /// The special nodes of the map (Docs/Design/03_Dungeon_Structure.md §1): from which floor an elite or a camp may stand
        /// on a node and the chance of each, and the floor whose every node is a camp (0: none). The first floor is battles only.
        /// </summary>
        [JsonProperty(Order = 12, Required = Required.Always)]
        public int EliteMinFloor { get; }

        [JsonProperty(Order = 13, Required = Required.Always)]
        public int EliteChancePercent { get; }

        [JsonProperty(Order = 14, Required = Required.Always)]
        public int CampMinFloor { get; }

        [JsonProperty(Order = 15, Required = Required.Always)]
        public int CampChancePercent { get; }

        [JsonProperty(Order = 16, Required = Required.Always)]
        public int CampFloor { get; }

        /// <summary>How much stronger the enemies of a floor are than those of the first: HP percent and item grades per floor deeper. The boss is as its data says.</summary>
        [JsonProperty(Order = 17, Required = Required.Always)]
        public int EnemyHpPerFloorPercent { get; }

        [JsonProperty(Order = 18, Required = Required.Always)]
        public int EnemyGradePerFloor { get; }

        /// <summary>The first floor whose battles reward Silver, Gold and Diamond items; 0 for never. An elite rewards one tier up.</summary>
        [JsonProperty(Order = 19, Required = Required.Always)]
        public int SilverFloor { get; }

        [JsonProperty(Order = 20, Required = Required.Always)]
        public int GoldFloor { get; }

        [JsonProperty(Order = 21, Required = Required.Always)]
        public int DiamondFloor { get; }

        /// <summary>Whether a node of this floor may be an elite: never on the first floor or the camp floor.</summary>
        public bool EliteCanStandOn(int floor)
        {
            return EliteChancePercent > 0 && floor >= EliteMinFloor && floor > 1 && floor != CampFloor && floor <= Floors;
        }

        /// <summary>
        /// Whether a node of this floor may be a camp by chance: never on the first floor, and never on the camp floor or the one
        /// before it, so a path does not rest twice in a row.
        /// </summary>
        public bool CampCanStandOn(int floor)
        {
            return CampChancePercent > 0 && floor >= CampMinFloor && floor > 1 && floor <= Floors && (CampFloor == 0 || floor < CampFloor - 1);
        }

        /// <summary>Grade of reward items offered after winning on a battle floor (1-based).</summary>
        public int RewardGradeAt(int floor)
        {
            return ItemGradeBase + (floor - 1) * ItemGradePerFloor;
        }

        /// <summary>Tier of reward items offered after winning on a floor: the deepest tier started by then, one more for an elite (Diamond at most).</summary>
        public ItemTier RewardTierAt(int floor, bool elite)
        {
            ItemTier tier = ItemTier.Bronze;
            if (SilverFloor != 0 && floor >= SilverFloor)
            {
                tier = ItemTier.Silver;
            }

            if (GoldFloor != 0 && floor >= GoldFloor)
            {
                tier = ItemTier.Gold;
            }

            if (DiamondFloor != 0 && floor >= DiamondFloor)
            {
                tier = ItemTier.Diamond;
            }

            return elite && tier < ItemTier.Diamond ? tier + 1 : tier;
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
