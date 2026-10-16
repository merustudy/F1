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
            IReadOnlyList<string> startingPotions,
            string background = null,
            int eliteMinFloor = 0,
            int eliteChancePercent = 0,
            int campMinFloor = 0,
            int campChancePercent = 0,
            int campFloor = 0,
            int enemyHpPerFloorPercent = 0,
            int enemyPowerPerFloorPercent = 0,
            int tier1Floor = 0,
            int tier2Floor = 0,
            int tier3Floor = 0,
            int shopMinFloor = 0,
            int shopChancePercent = 0,
            bool isFinal = false,
            string unlockDungeonId = null,
            int unlockClears = 0,
            int mapX = 0,
            int mapY = 0)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            AffinityId = DataId.Require(affinityId, $"{DefinitionName} '{id}' AffinityId");
            IsFinal = isFinal;
            UnlockDungeonId = string.IsNullOrEmpty(unlockDungeonId) ? null : DataId.Require(unlockDungeonId, $"{DefinitionName} '{id}' UnlockDungeonId");
            UnlockClears = unlockClears;

            if (unlockClears < 0 || (UnlockDungeonId == null && unlockClears != 0))
            {
                throw new DataException($"{DefinitionName} '{id}': UnlockClears counts the clears of UnlockDungeonId and cannot be negative.");
            }

            if (UnlockDungeonId == Id)
            {
                throw new DataException($"{DefinitionName} '{id}': a dungeon cannot unlock itself.");
            }

            if (mapX < 0 || mapX > 100 || mapY < 0 || mapY > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': MapX and MapY are percents of the expedition map, 0..100.");
            }

            MapX = mapX;
            MapY = mapY;

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

            if (shopMinFloor < 0 || shopChancePercent < 0 || shopChancePercent > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': the shop floor cannot be negative and its chance is 0..100.");
            }

            if (campFloor != 0 && (campFloor < 2 || campFloor > floors))
            {
                throw new DataException($"{DefinitionName} '{id}': CampFloor is 0 or a map floor from 2 (the first floor is battles only).");
            }

            if (enemyHpPerFloorPercent < 0 || enemyPowerPerFloorPercent < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': the deeper enemies cannot get weaker.");
            }

            int lastTierFloor = 0;
            foreach (int tierFloor in new[] { tier1Floor, tier2Floor, tier3Floor })
            {
                if (tierFloor == 0)
                {
                    continue;
                }

                if (tierFloor < 1 || tierFloor > floors || tierFloor <= lastTierFloor)
                {
                    throw new DataException($"{DefinitionName} '{id}': the floors where 1-, 2- and 3-star items start are 0 (never) or map floors, each deeper than the one before.");
                }

                lastTierFloor = tierFloor;
            }

            if ((tier2Floor != 0 && tier1Floor == 0) || (tier3Floor != 0 && tier2Floor == 0))
            {
                throw new DataException($"{DefinitionName} '{id}': a tier cannot start where the tier under it never does.");
            }

            Floors = floors;
            MapMinWidth = mapMinWidth;
            MapMaxWidth = mapMaxWidth;
            DurationDays = durationDays;
            StartingPotions = startingPotions;
            Background = ArtAddress.Optional(background, $"{DefinitionName} '{id}'", nameof(Background));
            EliteMinFloor = eliteMinFloor;
            EliteChancePercent = eliteChancePercent;
            CampMinFloor = campMinFloor;
            CampChancePercent = campChancePercent;
            CampFloor = campFloor;
            EnemyHpPerFloorPercent = enemyHpPerFloorPercent;
            EnemyPowerPerFloorPercent = enemyPowerPerFloorPercent;
            Tier1Floor = tier1Floor;
            Tier2Floor = tier2Floor;
            Tier3Floor = tier3Floor;
            ShopMinFloor = shopMinFloor;
            ShopChancePercent = shopChancePercent;
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

        /// <summary>
        /// How much stronger the enemies of a floor are than those of the first: HP percent, and power percent of their items (stage 22: was item
        /// grades), per floor deeper. The boss is as its data says.
        /// </summary>
        [JsonProperty(Order = 17, Required = Required.Always)]
        public int EnemyHpPerFloorPercent { get; }

        [JsonProperty(Order = 18, Required = Required.Always)]
        public int EnemyPowerPerFloorPercent { get; }

        /// <summary>The first floor from which the loot of battles and the stock of shops are 1-, 2- and 3-star items; 0 for never. An elite's loot is a tier up.</summary>
        [JsonProperty(Order = 19, Required = Required.Always)]
        public int Tier1Floor { get; }

        [JsonProperty(Order = 20, Required = Required.Always)]
        public int Tier2Floor { get; }

        [JsonProperty(Order = 21, Required = Required.Always)]
        public int Tier3Floor { get; }

        /// <summary>The shop nodes (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §1): from which floor a shop may stand on a node, and its chance.</summary>
        [JsonProperty(Order = 22, Required = Required.Always)]
        public int ShopMinFloor { get; }

        [JsonProperty(Order = 23, Required = Required.Always)]
        public int ShopChancePercent { get; }

        /// <summary>
        /// The final dungeon (Slice C stage 25, Docs/Design/03_Dungeon_Structure.md §1): clearing it wins the run, and the forced final stand
        /// after the last day fights its boss alone.
        /// </summary>
        public bool IsFinal { get; }

        /// <summary>The dungeon whose clears open this one, or null when it is open from the start.</summary>
        public string UnlockDungeonId { get; }

        /// <summary>How many clears of <see cref="UnlockDungeonId"/> open this dungeon.</summary>
        public int UnlockClears { get; }

        /// <summary>Where the dungeon's marker stands on the expedition map (Round 71 E2): percents of the painting's width and height from its top-left.</summary>
        public int MapX { get; }
        public int MapY { get; }

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

        /// <summary>Whether a node of this floor may be a shop by chance: never on the first floor or the camp floor. It may stand just before the camp floor.</summary>
        public bool ShopCanStandOn(int floor)
        {
            return ShopChancePercent > 0 && floor >= ShopMinFloor && floor > 1 && floor <= Floors && floor != CampFloor;
        }

        /// <summary>Tier of the items found on a floor: the deepest tier started by then, one more for an elite's loot (3-star at most).</summary>
        public ItemTier ItemTierAt(int floor, bool elite)
        {
            ItemTier tier = ItemTier.Common;
            if (Tier1Floor != 0 && floor >= Tier1Floor)
            {
                tier = ItemTier.Tier1;
            }

            if (Tier2Floor != 0 && floor >= Tier2Floor)
            {
                tier = ItemTier.Tier2;
            }

            if (Tier3Floor != 0 && floor >= Tier3Floor)
            {
                tier = ItemTier.Tier3;
            }

            return elite && tier < ItemTier.Tier3 ? tier + 1 : tier;
        }
    }

    /// <summary>A mercenary of the starting roster.</summary>
    /// <summary>
    /// A row of MercenaryData.csv: a mercenary of the starting roster (Starting, with its job), or a name of the recruit pool (not Starting,
    /// no job: a candidate at the stagecoach gets this name and a job rolled when it is drawn; Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §6).
    /// </summary>
    public sealed class MercenaryData
    {
        public const string DefinitionName = "Mercenary";

        [JsonConstructor]
        public MercenaryData(string id, LocalizedText name, string jobId, bool starting = true)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            Starting = starting;
            if (starting)
            {
                JobId = DataId.Require(jobId, $"{DefinitionName} '{id}' JobId");
            }
            else if (jobId != null)
            {
                throw new DataException($"{DefinitionName} '{id}': a name of the recruit pool has no JobId; the job is rolled when the candidate is drawn.");
            }
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        /// <summary>The job of a starting mercenary; null for a name of the recruit pool.</summary>
        [JsonProperty(Order = 3)]
        public string JobId { get; }

        /// <summary>True for the starting roster; false for a name the stagecoach draws candidates from.</summary>
        [JsonProperty(Order = 4)]
        public bool Starting { get; }
    }
}
