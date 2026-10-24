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
                    // Slice D stage 36: the critical chance holds everywhere (no condition), so a card can count it without knowing the row.
                    // Stage 47: the rogue's dodge the same — it is read where the hit is rolled, with no row to check.
                    valid = target == PassiveTarget.Self
                        && (effect == PassiveEffect.WeaponPowerPercent
                            || ((effect == PassiveEffect.CritChancePercent || effect == PassiveEffect.DodgePercent) && condition == PassiveCondition.None));
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

        /// <summary>A shield in hundredths (Slice B stage 22), burn stacks, or a weapon power percent.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public int Magnitude { get; }
    }

    /// <summary>A mercenary job. Immutable; the constructor enforces its own rules.</summary>
    public sealed class JobData
    {
        public const string DefinitionName = "Job";

        [JsonConstructor]
        public JobData(
            string id,
            LocalizedText name,
            string weaponItemId,
            int recommendedRow,
            PassiveSpec passive,
            LocalizedText passiveText = null,
            string figure = null,
            StatBlock stats = null,
            StatBlock growth = null,
            LocalizedText passiveName = null,
            IReadOnlyList<string> figuresMale = null,
            IReadOnlyList<string> figuresFemale = null)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (!BattleRows.IsValid(recommendedRow))
            {
                throw new DataException($"{DefinitionName} '{id}': RecommendedRow must be {BattleRows.Front}..{BattleRows.Count}.");
            }

            WeaponItemId = DataId.Require(weaponItemId, $"{DefinitionName} '{id}' WeaponItemId");
            if ((passive == null) != (passiveText == null))
            {
                throw new DataException($"{DefinitionName} '{id}': a passive and its PassiveText come together.");
            }

            // The passive is the job's trait on the screens (Slice D stage 37, Docs/Design/05 §6): its short name comes with it.
            if (passive == null && passiveName != null)
            {
                throw new DataException($"{DefinitionName} '{id}': a PassiveName needs a passive.");
            }

            RecommendedRow = recommendedRow;
            Passive = passive;
            PassiveText = passiveText;
            PassiveName = passiveName;
            Figure = ArtAddress.Optional(figure, $"{DefinitionName} '{id}'", nameof(Figure));
            // Stage 57 (Docs/Design/10 §2, the user's "이름의 성별로"): the figures a candidate of each gender is drawn among; none means the job's own.
            FiguresMale = Addresses(figuresMale, $"{DefinitionName} '{id}'", nameof(FiguresMale));
            FiguresFemale = Addresses(figuresFemale, $"{DefinitionName} '{id}'", nameof(FiguresFemale));
            if ((FiguresMale.Count > 0 || FiguresFemale.Count > 0) && Figure == null)
            {
                throw new DataException($"{DefinitionName} '{id}': a job with figures by gender has its own Figure as well.");
            }
            Stats = stats;
            // The growth grades (Slice D stage 34): the percent chance of a point per level-up, stat by stat; 100 is a sure point.
            Stat? outside = growth?.FirstOutside(0, MaxGrowthPercent);
            if (outside != null)
            {
                throw new DataException($"{DefinitionName} '{id}': Growth{outside} must be 0..{MaxGrowthPercent}.");
            }

            Growth = growth;
        }

        /// <summary>The highest growth grade (Docs/Design/05_Mercenary_Growth.md §2: 0~200%): two sure points a level.</summary>
        public const int MaxGrowthPercent = 200;

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        /// <summary>The item every mercenary of this job starts an expedition with.</summary>
        [JsonProperty(Order = 4, Required = Required.Always)]
        public string WeaponItemId { get; }

        /// <summary>Where the job usually stands. The simulator lines a party up by it; rules always use the actual row.</summary>
        [JsonProperty(Order = 6, Required = Required.Always)]
        public int RecommendedRow { get; }

        /// <summary>Null when the job has no passive.</summary>
        [JsonProperty(Order = 7, Required = Required.AllowNull)]
        public PassiveSpec Passive { get; }

        /// <summary>
        /// What the passive does, for the player. "{0}" stands for the passive's magnitude, so the
        /// number is written in one place only. Null when the job has no passive.
        /// </summary>
        [JsonProperty(Order = 8, Required = Required.AllowNull)]
        public LocalizedText PassiveText { get; }

        /// <summary>
        /// The logical address of the full-body art of the job's mercenaries. Null when the job
        /// has no art yet: the screen then shows a placeholder.
        /// </summary>
        [JsonProperty(Order = 9, Required = Required.AllowNull)]
        public string Figure { get; }

        /// <summary>
        /// The base stats of the job's mercenaries (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2): a new run's roster and the stagecoach's
        /// candidates start from them. Null for a job that names none (test data): every stat is then the baseline. The shipped CSV requires them.
        /// </summary>
        [JsonProperty(Order = 10, Required = Required.AllowNull)]
        public StatBlock Stats { get; }

        /// <summary>
        /// The growth grades of the job's mercenaries (Slice D stage 34, Docs/Design/05_Mercenary_Growth.md §2), stat by stat in percent: at each
        /// level-up a stat gains grade ÷ 100 points and one more by the remainder's chance. Null for a job that names none (test data): nothing grows.
        /// </summary>
        [JsonProperty(Order = 11, Required = Required.AllowNull)]
        public StatBlock Growth { get; }

        /// <summary>
        /// The passive's short name as the job's trait (Slice D stage 37, Round 84 판정 4: 선봉·광기·급소·후방 영창·축복); <see cref="PassiveText"/> is
        /// what it does. Null without a passive, or for test data that names none (the screens then use the job's name).
        /// </summary>
        [JsonProperty(Order = 12, Required = Required.AllowNull)]
        public LocalizedText PassiveName { get; }

        /// <summary>The figures a male candidate of the job is drawn among (stage 57), in the data's order; empty for the job's own <see cref="Figure"/>.</summary>
        [JsonProperty(Order = 13, Required = Required.Always)]
        public IReadOnlyList<string> FiguresMale { get; }

        /// <summary>The figures a female candidate of the job is drawn among (stage 57); empty for the job's own <see cref="Figure"/>.</summary>
        [JsonProperty(Order = 14, Required = Required.Always)]
        public IReadOnlyList<string> FiguresFemale { get; }

        /// <summary>The figures of a gender (stage 57).</summary>
        public IReadOnlyList<string> FiguresFor(Gender gender)
        {
            return gender == Gender.Female ? FiguresFemale : FiguresMale;
        }

        /// <summary>Whether an address is one of the job's figures: its own or one of a gender's (stage 57).</summary>
        public bool HasFigure(string address)
        {
            if (address == null)
            {
                return false;
            }

            foreach (string figure in FiguresMale)
            {
                if (figure == address)
                {
                    return true;
                }
            }

            foreach (string figure in FiguresFemale)
            {
                if (figure == address)
                {
                    return true;
                }
            }

            return address == Figure;
        }

        static IReadOnlyList<string> Addresses(IReadOnlyList<string> values, string owner, string column)
        {
            var list = new List<string>();
            foreach (string value in values ?? new List<string>())
            {
                if (list.Contains(value))
                {
                    throw new DataException($"{owner}: {column} names '{value}' twice.");
                }

                list.Add(ArtAddress.Optional(value, owner, column) ?? throw new DataException($"{owner}: {column} has an empty address."));
            }

            return list;
        }

        /// <summary>The address of the attack pose drawn after the figure (<see cref="ArtAddress.PoseOf"/>). Null when the job has no figure.</summary>
        [JsonIgnore]
        public string AttackPose => ArtAddress.PoseOf(Figure, ArtAddress.Attack);

        /// <summary>The address of the hit pose drawn after the figure. Null when the job has no figure.</summary>
        [JsonIgnore]
        public string HitPose => ArtAddress.PoseOf(Figure, ArtAddress.Hit);

        /// <summary>
        /// The address of the pose held through the moment of a breakdown (an affliction, or the collapse), drawn after the figure
        /// (round 38). Null when the job has no figure. The file is drawn job by job: where it is not yet, the figure stands in.
        /// </summary>
        [JsonIgnore]
        public string BrokenPose => ArtAddress.PoseOf(Figure, ArtAddress.Broken);

        /// <summary>The address of the pose held through the moment of a virtue, as <see cref="BrokenPose"/>.</summary>
        [JsonIgnore]
        public string ResolutePose => ArtAddress.PoseOf(Figure, ArtAddress.Resolute);
    }
}
