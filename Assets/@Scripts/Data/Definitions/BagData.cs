using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A bag (Slice B stage 19, Docs/Design/02_Combat_System.md §4): it lies in a mercenary's board frame and makes the squares items
    /// lie on. A bag has no effect of its own. Bags are not items: a different definition, file and type.
    /// </summary>
    public sealed class BagData
    {
        public const string DefinitionName = "Bag";

        [JsonConstructor]
        public BagData(string id, LocalizedText name, int width, int height, bool start, int price, int shopWeight, int lootWeight)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (width < 1 || width > BoardFrame.MaxSide || height < 1 || height > BoardFrame.MaxSide)
            {
                throw new DataException($"{DefinitionName} '{id}': Width and Height must be 1..{BoardFrame.MaxSide}.");
            }

            if (price < 0 || shopWeight < 0 || lootWeight < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': Price, ShopWeight and LootWeight cannot be negative.");
            }

            if (start && (shopWeight > 0 || lootWeight > 0))
            {
                throw new DataException($"{DefinitionName} '{id}': the start bag is not sold or dropped.");
            }

            Width = width;
            Height = height;
            Start = start;
            Price = price;
            ShopWeight = shopWeight;
            LootWeight = lootWeight;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        /// <summary>The squares across, unturned. A bag turns like an item.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Width { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Height { get; }

        /// <summary>
        /// True for the bag every mercenary leaves on an expedition with, at the top of the frame. It stays there: it is never moved,
        /// turned or taken off. Exactly one bag is the start bag.
        /// </summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public bool Start { get; }

        /// <summary>What a shop sells it for, in region coins. 0 means no shop stocks it.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public int Price { get; }

        /// <summary>Relative chance to be stocked by a shop. 0 means no shop stocks it.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public int ShopWeight { get; }

        /// <summary>Relative chance to be the bag an elite's loot holds (<c>BalanceData.EliteBagPercent</c>). 0 means it never drops.</summary>
        [JsonProperty(Order = 8, Required = Required.Always)]
        public int LootWeight { get; }

        [JsonIgnore]
        public int Area => Width * Height;
    }
}
