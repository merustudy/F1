using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>"When, under which condition, what, to whom, how much" of a job passive.</summary>
    public sealed class PassiveSpec
    {
        [JsonConstructor]
        public PassiveSpec(
            PassiveTrigger trigger,
            PassiveCondition condition,
            PassiveEffect effect,
            PassiveTarget target,
            int magnitude)
        {
            if (magnitude < 1)
            {
                throw new DataException("Passive magnitude must be at least 1.");
            }

            bool valid;
            switch (trigger)
            {
                case PassiveTrigger.BattleStart:
                    valid = effect == PassiveEffect.Shield
                        && (target == PassiveTarget.Self || target == PassiveTarget.AllyAll)
                        && condition != PassiveCondition.SelfInDog;
                    break;
                case PassiveTrigger.WeaponHit:
                    valid = effect == PassiveEffect.Burn && target == PassiveTarget.EventTarget;
                    break;
                case PassiveTrigger.Heal:
                    valid = effect == PassiveEffect.Shield && target == PassiveTarget.EventTarget;
                    break;
                case PassiveTrigger.Always:
                    valid = effect == PassiveEffect.WeaponPowerPercent && target == PassiveTarget.Self;
                    break;
                default:
                    valid = false;
                    break;
            }

            if (!valid)
            {
                throw new DataException($"Passive combination {trigger}/{condition}/{effect}/{target} is not supported.");
            }

            Trigger = trigger;
            Condition = condition;
            Effect = effect;
            Target = target;
            Magnitude = magnitude;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public PassiveTrigger Trigger { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public PassiveCondition Condition { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public PassiveEffect Effect { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public PassiveTarget Target { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Magnitude { get; }
    }

    /// <summary>A mercenary job. Immutable; the constructor enforces its own rules.</summary>
    public sealed class JobData
    {
        public const string DefinitionName = "Job";
        public const int MaxItemSlots = 6;

        [JsonConstructor]
        public JobData(
            string id,
            LocalizedText name,
            int maxHp,
            int itemSlots,
            string weaponItemId,
            int weaponGrade,
            BattleRow recommendedRow,
            PassiveSpec passive,
            LocalizedText passiveText = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (maxHp < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': MaxHp must be at least 1.");
            }

            if (itemSlots < 1 || itemSlots > MaxItemSlots)
            {
                throw new DataException($"{DefinitionName} '{id}': ItemSlots must be 1..{MaxItemSlots}.");
            }

            if (weaponGrade < 1)
            {
                throw new DataException($"{DefinitionName} '{id}': WeaponGrade must be at least 1.");
            }

            MaxHp = maxHp;
            ItemSlots = itemSlots;
            WeaponItemId = DataId.Require(weaponItemId, $"{DefinitionName} '{id}' WeaponItemId");
            WeaponGrade = weaponGrade;
            if ((passive == null) != (passiveText == null))
            {
                throw new DataException($"{DefinitionName} '{id}': a passive and its PassiveText come together.");
            }

            RecommendedRow = recommendedRow;
            Passive = passive;
            PassiveText = passiveText;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int MaxHp { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public int ItemSlots { get; }

        /// <summary>The item every mercenary of this job starts an expedition with.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public string WeaponItemId { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int WeaponGrade { get; }

        /// <summary>A default for initial placement only. Rules always use the actual row.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public BattleRow RecommendedRow { get; }

        /// <summary>Null when the job has no passive.</summary>
        [JsonProperty(Order = 8, Required = Required.AllowNull)]
        public PassiveSpec Passive { get; }

        /// <summary>
        /// What the passive does, for the player. "{0}" stands for the passive's magnitude, so the
        /// number is written in one place only. Null when the job has no passive.
        /// </summary>
        [JsonProperty(Order = 9, Required = Required.AllowNull)]
        public LocalizedText PassiveText { get; }
    }
}
