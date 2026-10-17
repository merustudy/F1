using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// One row of BuildingData.csv (Slice C stage 30, Docs/Design/04_Lobby_100Day_Economy.md §7): a level of one building of the
    /// town. The building is one of <see cref="BuildingId"/>, its levels run from 1 without a gap (StaticData checks the ladder), the
    /// first level is the start (no price, no days) and every level after it is built for a price in gold over a number of days. The
    /// bonus is what the level adds to the building's base value of BalanceData: the roster limit for the barracks, the recruit count
    /// for the stagecoach, the treatment's relief for the inn, the settlement's gold in percent for the gate (BuildingRules). Level 1
    /// adds nothing.
    /// </summary>
    public sealed class BuildingData
    {
        public const string DefinitionName = "Building";

        [JsonConstructor]
        public BuildingData(string id, int level, int price, int days, int bonus)
        {
            if (!BuildingId.IsKnown(id))
            {
                throw new DataException($"{DefinitionName}: Id '{id}' is not a building of the town ({string.Join(", ", BuildingId.All)}).");
            }

            if (level < 1)
            {
                throw new DataException($"{DefinitionName} {id}: Level {level} must be at least 1.");
            }

            if (price < 0 || days < 0 || bonus < 0)
            {
                throw new DataException($"{DefinitionName} {id} {level}: Price, Days and Bonus cannot be negative.");
            }

            if (level == 1 && (price != 0 || days != 0 || bonus != 0))
            {
                throw new DataException($"{DefinitionName} {id} 1: the first level is the start: Price, Days and Bonus are 0.");
            }

            if (level > 1 && (price <= 0 || days <= 0))
            {
                throw new DataException($"{DefinitionName} {id} {level}: a level after the first takes a Price and Days above 0.");
            }

            Id = id;
            Level = level;
            Price = price;
            Days = days;
            Bonus = bonus;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Level { get; }

        /// <summary>The gold the upgrade to this level costs; 0 at level 1.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Price { get; }

        /// <summary>The days the construction of this level takes; 0 at level 1.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Days { get; }

        /// <summary>What this level adds to the building's base value (see the class summary); 0 at level 1.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Bonus { get; }
    }

    /// <summary>The buildings of the town (stage 30): the ids BuildingData.csv may use, each with what its levels raise.</summary>
    public static class BuildingId
    {
        /// <summary>The barracks: its levels raise the roster limit.</summary>
        public const string Barracks = "barracks";

        /// <summary>The inn: its levels raise the treatment's relief.</summary>
        public const string Inn = "inn";

        /// <summary>The stagecoach: its levels raise the number of candidates.</summary>
        public const string Coach = "coach";

        /// <summary>
        /// The three buildings with levels. The gate is not one of them (2026-10-18, the user: "성문은 업그레이드 없음(원정)"): it is the
        /// fixed way to the expedition screen, a picture of the town with nothing to raise.
        /// </summary>
        public static readonly string[] All = { Barracks, Inn, Coach };

        public static bool IsKnown(string id)
        {
            return id == Barracks || id == Inn || id == Coach;
        }
    }
}
