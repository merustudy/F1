using System.Collections.Generic;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>"When, under which condition, what, to whom, how much" of a job passive.</summary>
    public sealed class PassiveSpec
    {
        /// <param name="rows">Where the unit must stand for an InRows condition. Null for every other condition.</param>
        [JsonConstructor]
        public PassiveSpec(
            PassiveTrigger trigger,
            PassiveCondition condition,
            RowSpan rows,
            PassiveEffect effect,
            PassiveTarget target,
            int magnitude)
        {
            if (magnitude < 1)
            {
                throw new DataException("Passive magnitude must be at least 1.");
            }

            if (condition == PassiveCondition.InRows)
            {
                if (rows == null)
                {
                    throw new DataException("Passive condition InRows needs Rows.");
                }
            }
            else if (rows != null)
            {
                throw new DataException($"Passive condition {condition} does not take rows.");
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
            Rows = rows;
            Effect = effect;
            Target = target;
            Magnitude = magnitude;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public PassiveTrigger Trigger { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public PassiveCondition Condition { get; }

        /// <summary>Where the unit must stand for an InRows condition, counted from the front or the back; null otherwise.</summary>
        [JsonProperty(Order = 3, Required = Required.AllowNull)]
        public RowSpan Rows { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public PassiveEffect Effect { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public PassiveTarget Target { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int Magnitude { get; }
    }

    /// <summary>A mercenary job. Immutable; the constructor enforces its own rules.</summary>
    public sealed class JobData
    {
        public const string DefinitionName = "Job";

        /// <summary>The most cells any board has. A unit's column of the board panel is built to hold this many cells stacked (8 until the 2026-10-04 mockup B made it 7).</summary>
        public const int MaxItemSlots = 7;

        [JsonConstructor]
        public JobData(
            string id,
            LocalizedText name,
            int maxHp,
            int itemSlots,
            string weaponItemId,
            int weaponGrade,
            int recommendedRow,
            PassiveSpec passive,
            LocalizedText passiveText = null,
            string figure = null)
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

            if (!BattleRows.IsValid(recommendedRow))
            {
                throw new DataException($"{DefinitionName} '{id}': RecommendedRow must be {BattleRows.Front}..{BattleRows.Count}.");
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
            Figure = ArtAddress.Optional(figure, $"{DefinitionName} '{id}'", nameof(Figure));
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public int MaxHp { get; }

        /// <summary>Cells of the item board. Items take their size in cells.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public int ItemSlots { get; }

        /// <summary>The item every mercenary of this job starts an expedition with.</summary>
        [JsonProperty(Order = 5, Required = Required.Always)]
        public string WeaponItemId { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int WeaponGrade { get; }

        /// <summary>Where the job usually stands. The simulator lines a party up by it; rules always use the actual row.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public int RecommendedRow { get; }

        /// <summary>Null when the job has no passive.</summary>
        [JsonProperty(Order = 8, Required = Required.AllowNull)]
        public PassiveSpec Passive { get; }

        /// <summary>
        /// What the passive does, for the player. "{0}" stands for the passive's magnitude, so the
        /// number is written in one place only. Null when the job has no passive.
        /// </summary>
        [JsonProperty(Order = 9, Required = Required.AllowNull)]
        public LocalizedText PassiveText { get; }

        /// <summary>
        /// The logical address of the full-body art of the job's mercenaries. Null when the job
        /// has no art yet: the screen then shows a placeholder.
        /// </summary>
        [JsonProperty(Order = 10, Required = Required.AllowNull)]
        public string Figure { get; }

        /// <summary>The address of the face cut out of the figure (<see cref="ArtAddress.FaceOf"/>). Null when the job has no figure.</summary>
        [JsonIgnore]
        public string Face => ArtAddress.FaceOf(Figure);

        /// <summary>The address of the attack pose drawn after the figure (<see cref="ArtAddress.PoseOf"/>). Null when the job has no figure.</summary>
        [JsonIgnore]
        public string AttackPose => ArtAddress.PoseOf(Figure, ArtAddress.Attack);

        /// <summary>The address of the hit pose drawn after the figure. Null when the job has no figure.</summary>
        [JsonIgnore]
        public string HitPose => ArtAddress.PoseOf(Figure, ArtAddress.Hit);
    }
}
