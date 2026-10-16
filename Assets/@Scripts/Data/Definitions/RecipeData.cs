using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A forge recipe (Slice B stage 23, Docs/Design/02_Combat_System.md §4 "담금질"): a base item at a tier, with a catalyst item at a tier
    /// whose star is lit on it, forged at a camp into the result — a unique item (one per party). The first: the longsword and a whetstone
    /// into the gladius. What the forge does with the pieces is ForgeRules; this is the data.
    /// </summary>
    public sealed class RecipeData
    {
        public const string DefinitionName = "Recipe";

        [JsonConstructor]
        public RecipeData(string id, string result, string baseItem, ItemTier baseTier, string catalyst, ItemTier catalystTier)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Result = DataId.Require(result, DefinitionName + " Result");
            BaseItem = DataId.Require(baseItem, DefinitionName + " BaseItem");
            Catalyst = DataId.Require(catalyst, DefinitionName + " Catalyst");
            if (result == baseItem || result == catalyst)
            {
                throw new DataException($"{DefinitionName} '{id}': the result is not one of the ingredients.");
            }

            BaseTier = baseTier;
            CatalystTier = catalystTier;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        /// <summary>The unique item the forge makes.</summary>
        [JsonProperty(Order = 2, Required = Required.Always)]
        public string Result { get; }

        /// <summary>The item the result takes the place of (it keeps the base's placement, tier and base-weapon standing).</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public string BaseItem { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public ItemTier BaseTier { get; }

        /// <summary>The star item whose star must be lit on the base; the forge consumes it.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public string Catalyst { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public ItemTier CatalystTier { get; }
    }
}
