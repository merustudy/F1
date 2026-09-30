using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>One effect of an item. Its size is grade x PowerPercent / 100 (at least 1).</summary>
    public sealed class ItemEffect
    {
        [JsonConstructor]
        public ItemEffect(EffectKind kind, TargetMode target, int powerPercent)
        {
            if (powerPercent < 1)
            {
                throw new DataException("Effect PowerPercent must be at least 1.");
            }

            bool targetsEnemy = target == TargetMode.EnemyFront || target == TargetMode.EnemyRear || target == TargetMode.EnemyAll;
            bool harmful = kind == EffectKind.Damage || kind == EffectKind.Burn;
            if (targetsEnemy != harmful)
            {
                throw new DataException($"Effect {kind} cannot target {target}.");
            }

            Kind = kind;
            Target = target;
            PowerPercent = powerPercent;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public EffectKind Kind { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public TargetMode Target { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int PowerPercent { get; }

        /// <summary>Effect size at a grade: grade x PowerPercent / 100, rounded down, at least 1.</summary>
        public int MagnitudeAt(int grade)
        {
            int magnitude = grade * PowerPercent / 100;
            return magnitude < 1 ? 1 : magnitude;
        }
    }

    /// <summary>
    /// A battle card. Items exist only inside a dungeon and activate on their own cooldown.
    /// Items are not relics: relics are a different definition, file and type.
    /// </summary>
    public sealed class ItemData
    {
        public const string DefinitionName = "Item";
        public const int MaxEffects = 2;
        public const int MinCooldownMs = 100;

        [JsonConstructor]
        public ItemData(
            string id,
            LocalizedText name,
            ItemCategory category,
            int cooldownMs,
            RowRequirement row,
            IReadOnlyList<ItemEffect> effects,
            int rewardWeight)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (cooldownMs < MinCooldownMs)
            {
                throw new DataException($"{DefinitionName} '{id}': CooldownMs must be at least {MinCooldownMs}.");
            }

            if (effects == null || effects.Count < 1 || effects.Count > MaxEffects)
            {
                throw new DataException($"{DefinitionName} '{id}': an item has 1..{MaxEffects} effects.");
            }

            foreach (ItemEffect effect in effects)
            {
                if (effect == null)
                {
                    throw new DataException($"{DefinitionName} '{id}': an effect is missing.");
                }
            }

            if (rewardWeight < 0)
            {
                throw new DataException($"{DefinitionName} '{id}': RewardWeight cannot be negative.");
            }

            Category = category;
            CooldownMs = cooldownMs;
            Row = row;
            Effects = effects;
            RewardWeight = rewardWeight;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public ItemCategory Category { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int CooldownMs { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public RowRequirement Row { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public IReadOnlyList<ItemEffect> Effects { get; }

        /// <summary>Relative chance to be offered as a battle reward. 0 means it is never offered.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public int RewardWeight { get; }
    }
}
