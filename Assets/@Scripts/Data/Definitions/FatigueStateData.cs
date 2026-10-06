using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A state the breakdown at the fatigue threshold brings (Docs/Design/04_Lobby_100Day_Economy.md §3): an affliction or a
    /// virtue, and what it does to the unit's battle numbers. Darkest Dungeon's afflictions refuse orders; an auto battle has
    /// no orders, so a state moves the numbers instead: how fast the unit's items cycle, how much healing it takes, and how
    /// likely a hit at death's door kills it.
    /// </summary>
    public sealed class FatigueStateData
    {
        public const string DefinitionName = "FatigueState";

        /// <param name="cooldownPercent">Change to the unit's item cooldowns, in percent: 25 is a quarter slower, -20 a fifth faster.</param>
        /// <param name="healTakenPercent">Change to the healing the unit receives, in percent.</param>
        /// <param name="deathChanceDelta">Added to the death chance of a hit at death's door, in percentage points.</param>
        [JsonConstructor]
        public FatigueStateData(string id, LocalizedText name, FatigueStateKind kind, int cooldownPercent, int healTakenPercent, int deathChanceDelta, LocalizedText description)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            Description = description ?? throw new DataException($"{DefinitionName} '{id}': Description is missing.");
            if (cooldownPercent < -50 || cooldownPercent > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': CooldownPercent must be within -50..100.");
            }

            if (healTakenPercent < -100 || healTakenPercent > 200)
            {
                throw new DataException($"{DefinitionName} '{id}': HealTakenPercent must be within -100..200.");
            }

            if (deathChanceDelta < -100 || deathChanceDelta > 100)
            {
                throw new DataException($"{DefinitionName} '{id}': DeathChanceDelta must be within -100..100.");
            }

            if (cooldownPercent == 0 && healTakenPercent == 0 && deathChanceDelta == 0)
            {
                throw new DataException($"{DefinitionName} '{id}': a state must change something.");
            }

            Kind = kind;
            CooldownPercent = cooldownPercent;
            HealTakenPercent = healTakenPercent;
            DeathChanceDelta = deathChanceDelta;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public FatigueStateKind Kind { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int CooldownPercent { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int HealTakenPercent { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int DeathChanceDelta { get; }

        /// <summary>What the state does, in words for the screens.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public LocalizedText Description { get; }
    }
}
