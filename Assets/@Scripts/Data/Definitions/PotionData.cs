using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>A consumable the player uses on a mercenary during battle.</summary>
    public sealed class PotionData
    {
        public const string DefinitionName = "Potion";

        [JsonConstructor]
        public PotionData(string id, LocalizedText name, PotionEffect effect, int magnitude, int rewardWeight, string icon = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (magnitude < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': Magnitude must be at least 1.");
            }

            if (rewardWeight < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': RewardWeight cannot be negative.");
            }

            Effect = effect;
            Magnitude = magnitude;
            RewardWeight = rewardWeight;
            Icon = ArtAddress.Optional(icon, $"{DefinitionName} '{id}'", nameof(Icon));
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public PotionEffect Effect { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Magnitude { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int RewardWeight { get; }

        /// <summary>The logical address of the potion's bottle icon. Null when it has no art yet.</summary>
        [JsonProperty(Order = 6, Required = Required.AllowNull)]
        public string Icon { get; }
    }
}
