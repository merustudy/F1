using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A mercenary's trait (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): a strength or a weakness it was born with or that an
    /// expedition left on it. What it does is a term added to the stats' formulas — the hit chance of its attacks, its dodge, the critical
    /// chance of its items that have one and its chance of a virtue in percent points, its death chance at death's door in percent points, the
    /// fatigue it gains and its maximum HP in percent (TraitBonus sums them). The job's trait is the job's passive (JobData), not a row here.
    /// </summary>
    public sealed class TraitData
    {
        public const string DefinitionName = "Trait";

        /// <summary>The most any one term may move its number, either way.</summary>
        public const int MaxShift = 100;

        /// <param name="weight">How often the trait is drawn among those it competes with (a candidate's innate traits); 0 never.</param>
        /// <param name="excludes">The trait this one cannot stand beside (its opposite), or null. The two never come together, whichever names the other.</param>
        [JsonConstructor]
        public TraitData(
            string id,
            LocalizedText name,
            TraitKind kind,
            TraitPolarity polarity,
            int weight,
            string excludes,
            int hitPercent,
            int dodgePercent,
            int critPercent,
            int dogDeathPercent,
            int virtuePercent,
            int fatiguePercent,
            int maxHpPercent)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (weight < 0 || weight > 1000)
            {
                throw new DataException($"{DefinitionName} '{id}': Weight must be within 0..1000.");
            }

            if (excludes != null)
            {
                DataId.Require(excludes, $"{DefinitionName} '{id}' Excludes");
                if (excludes == id)
                {
                    throw new DataException($"{DefinitionName} '{id}': a trait cannot exclude itself.");
                }
            }

            foreach ((string what, int value) in new[]
            {
                (nameof(HitPercent), hitPercent), (nameof(DodgePercent), dodgePercent), (nameof(CritPercent), critPercent),
                (nameof(DogDeathPercent), dogDeathPercent), (nameof(VirtuePercent), virtuePercent), (nameof(FatiguePercent), fatiguePercent),
                (nameof(MaxHpPercent), maxHpPercent),
            })
            {
                if (value < -MaxShift || value > MaxShift)
                {
                    throw new DataException($"{DefinitionName} '{id}': {what} must be within -{MaxShift}..{MaxShift}.");
                }
            }

            if (hitPercent == 0 && dodgePercent == 0 && critPercent == 0 && dogDeathPercent == 0 && virtuePercent == 0 && fatiguePercent == 0 && maxHpPercent == 0)
            {
                throw new DataException($"{DefinitionName} '{id}': a trait must change something.");
            }

            Kind = kind;
            Polarity = polarity;
            Weight = weight;
            Excludes = excludes;
            HitPercent = hitPercent;
            DodgePercent = dodgePercent;
            CritPercent = critPercent;
            DogDeathPercent = dogDeathPercent;
            VirtuePercent = virtuePercent;
            FatiguePercent = fatiguePercent;
            MaxHpPercent = maxHpPercent;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public TraitKind Kind { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public TraitPolarity Polarity { get; }

        [JsonProperty(Order = 5, Required = Required.Always)]
        public int Weight { get; }

        [JsonProperty(Order = 6, Required = Required.AllowNull)]
        public string Excludes { get; }

        /// <summary>Added to the hit chance of the mercenary's attacks, in percent points.</summary>
        [JsonProperty(Order = 7, Required = Required.Always)]
        public int HitPercent { get; }

        /// <summary>Added to the mercenary's dodge, in percent points.</summary>
        [JsonProperty(Order = 8, Required = Required.Always)]
        public int DodgePercent { get; }

        /// <summary>Added to the critical chance of the mercenary's items that have one, in percent points.</summary>
        [JsonProperty(Order = 9, Required = Required.Always)]
        public int CritPercent { get; }

        /// <summary>Added to the mercenary's death chance on a hit at death's door, in percent points.</summary>
        [JsonProperty(Order = 10, Required = Required.Always)]
        public int DogDeathPercent { get; }

        /// <summary>Added to the mercenary's chance of a virtue at the breakdown, in percent points.</summary>
        [JsonProperty(Order = 11, Required = Required.Always)]
        public int VirtuePercent { get; }

        /// <summary>Added to the percent of a fatigue gain the mercenary takes (endurance's percent counts with it).</summary>
        [JsonProperty(Order = 12, Required = Required.Always)]
        public int FatiguePercent { get; }

        /// <summary>Added to the percent of the job's maximum HP the mercenary has (constitution's percent counts with it).</summary>
        [JsonProperty(Order = 13, Required = Required.Always)]
        public int MaxHpPercent { get; }

        /// <summary>Whether two traits cannot stand together: one names the other as its opposite.</summary>
        public bool Clashes(TraitData other)
        {
            return other != null && (Excludes == other.Id || other.Excludes == Id);
        }
    }
}
