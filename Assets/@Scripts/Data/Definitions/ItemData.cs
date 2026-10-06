using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>One effect of an item. Its size is grade x PowerPercent / 100 (at least 1), times the percent of the item's tier.</summary>
    public sealed class ItemEffect
    {
        /// <param name="reach">
        /// How many enemies are hit, counted from the front (EnemyFront) or from the back (EnemyBack).
        /// 0 for every other target.
        /// </param>
        [JsonConstructor]
        public ItemEffect(EffectKind kind, TargetMode target, int reach, int powerPercent)
        {
            if (powerPercent < 1)
            {
                throw new DataException("Effect PowerPercent must be at least 1.");
            }

            bool harmful = kind == EffectKind.Damage || kind == EffectKind.Burn;
            if (TargetsEnemy(target) != harmful)
            {
                throw new DataException($"Effect {kind} cannot target {target}.");
            }

            if (TakesReach(target))
            {
                if (reach < 1 || reach > BattleRows.Count)
                {
                    throw new DataException($"Effect target {target} needs a Reach of 1..{BattleRows.Count}.");
                }
            }
            else if (reach != 0)
            {
                throw new DataException($"Effect target {target} does not take a Reach.");
            }

            Kind = kind;
            Target = target;
            Reach = reach;
            PowerPercent = powerPercent;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public EffectKind Kind { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public TargetMode Target { get; }

        /// <summary>How many enemies are hit from the end <see cref="Target"/> counts from. 0 when the target takes no reach.</summary>
        [JsonProperty(Order = 3, Required = Required.Always)]
        public int Reach { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int PowerPercent { get; }

        public static bool TargetsEnemy(TargetMode target)
        {
            return TakesReach(target) || target == TargetMode.EnemyAll;
        }

        /// <summary>True for the targets that are counted from one end of the enemy line.</summary>
        public static bool TakesReach(TargetMode target)
        {
            return target == TargetMode.EnemyFront || target == TargetMode.EnemyBack;
        }

        /// <summary>Effect size at a grade, at Common: grade x PowerPercent / 100, rounded down, at least 1.</summary>
        public int MagnitudeAt(int grade)
        {
            return MagnitudeAt(grade, 100);
        }

        /// <summary>Effect size at a grade and a tier's percent (<see cref="BalanceData.TierPercent"/>): grade x PowerPercent x tierPercent / 10000, rounded down, at least 1.</summary>
        public int MagnitudeAt(int grade, int tierPercent)
        {
            long magnitude = (long)grade * PowerPercent * tierPercent / 10000;
            return magnitude < 1 ? 1 : (int)magnitude;
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

        /// <summary>The biggest item: small, medium and large take 1, 2 and 3 cells of a board.</summary>
        public const int MaxSize = 3;

        [JsonConstructor]
        public ItemData(
            string id,
            LocalizedText name,
            ItemCategory category,
            int size,
            int cooldownMs,
            RowSpan rows,
            IReadOnlyList<ItemEffect> effects,
            int rewardWeight,
            string icon = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (size < 1 || size > MaxSize)
            {
                throw new DataException($"{DefinitionName} '{id}': Size must be 1..{MaxSize}.");
            }

            if (cooldownMs < MinCooldownMs)
            {
                throw new DataException($"{DefinitionName} '{id}': CooldownMs must be at least {MinCooldownMs}.");
            }

            Rows = rows ?? throw new DataException($"{DefinitionName} '{id}': Rows is missing.");
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
            Size = size;
            CooldownMs = cooldownMs;
            Effects = effects;
            RewardWeight = rewardWeight;
            Icon = ArtAddress.Optional(icon, $"{DefinitionName} '{id}'", nameof(Icon));
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public ItemCategory Category { get; }

        /// <summary>How many cells of a board the item takes.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int Size { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int CooldownMs { get; }

        /// <summary>Where in its line the owner must stand for the item to work, counted from the front or the back.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public RowSpan Rows { get; }

        [JsonProperty(Order = 7, Required = Required.Always)]
        public IReadOnlyList<ItemEffect> Effects { get; }

        /// <summary>Relative chance to be offered as a battle reward. 0 means it is never offered.</summary>
        [JsonProperty(Order = 8, Required = Required.Always)]
        public int RewardWeight { get; }

        /// <summary>The logical address of the icon its cell shows. Null when it has no art yet: the cell shows the name.</summary>
        [JsonProperty(Order = 9, Required = Required.AllowNull)]
        public string Icon { get; }

        /// <param name="row">The row the owner stands in.</param>
        /// <param name="lineLength">How many units of the owner's side are alive.</param>
        public bool UsableIn(int row, int lineLength)
        {
            return Rows.Contains(row, lineLength);
        }
    }
}
