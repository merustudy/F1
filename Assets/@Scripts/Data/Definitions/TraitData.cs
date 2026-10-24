using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>
    /// A mercenary's trait (Slice D stage 37, Docs/Design/05_Mercenary_Growth.md §6): a strength or a weakness it was born with or that an
    /// expedition left on it. What it does is a term added to the stats' formulas — the hit chance of its attacks, its dodge, the critical
    /// chance of its items that have one and its chance of a virtue in percent points, its death chance at death's door in percent points, the
    /// fatigue it gains and its maximum HP in percent (TraitBonus sums them). The job's trait is the job's passive (JobData), not a row here.
    /// An acquired trait says what leaves it at the settlement (Source, stage 38); an innate one has none.
    /// Stage 57 (Docs/Design/39_Trait_Expansion_Proposal.md): a trait may also shift the six stats, the growth grades, the damage the
    /// mercenary deals, the healing it takes and its allies' fatigue at the start of each battle. A build (Build) is an innate trait of the
    /// one build slot a candidate draws (a mercenary has one build at most), and may name its figure's variant (Figure). An acquired trait of
    /// a breakdown or a virtue may name the state it marks (SourceState). Rare is shown on the card.
    /// </summary>
    public sealed class TraitData
    {
        public const string DefinitionName = "Trait";

        /// <summary>The most any one term may move its number, either way.</summary>
        public const int MaxShift = 100;

        /// <summary>The most a trait may shift one stat, either way (stage 57).</summary>
        public const int MaxStatShift = 10;

        /// <summary>The most a trait may shift the allies' fatigue at the start of a battle, either way (stage 57).</summary>
        public const int MaxAllyFatigue = 10;

        /// <summary>The range of a build figure's scale on the stage, in percent (stage 57; an enemy's FigureScale has the same).</summary>
        public const int MinFigureScale = 50;
        public const int MaxFigureScale = 300;

        /// <param name="source">What leaves the trait at the settlement (stage 38): required for an acquired trait, null for an innate one.</param>
        /// <param name="weight">How often the trait is drawn among those it competes with (a candidate's innate traits, or the acquired traits of one source); 0 never.</param>
        /// <param name="excludes">The trait this one cannot stand beside (its opposite), or null. The two never come together, whichever names the other.</param>
        /// <param name="build">Whether the trait is a build (stage 57): an innate trait of the one build slot.</param>
        /// <param name="figure">The figure variant a build is drawn in (e.g. "giant": unit/job/knight_giant), or null.</param>
        /// <param name="sourceState">The breakdown or virtue an acquired trait of that source marks (a FatigueStateData id), or null for any.</param>
        [JsonConstructor]
        public TraitData(
            string id,
            LocalizedText name,
            TraitKind kind,
            TraitPolarity polarity,
            TraitSource? source,
            int weight,
            string excludes,
            int hitPercent,
            int dodgePercent,
            int critPercent,
            int dogDeathPercent,
            int virtuePercent,
            int fatiguePercent,
            int maxHpPercent,
            int critDamagePercent = 0,
            bool build = false,
            bool rare = false,
            string figure = null,
            string sourceState = null,
            int str = 0,
            int dex = 0,
            int con = 0,
            int @int = 0,
            int wis = 0,
            int end = 0,
            int growthPercent = 0,
            int damagePercent = 0,
            int healTakenPercent = 0,
            int allyFatigueAtStart = 0,
            int figureScale = 100)
        {
            Id = DataId.Require(id, DefinitionName + " Id");
            Name = name ?? throw new DataException($"{DefinitionName} '{id}': Name is missing.");
            if (weight < 0 || weight > 1000)
            {
                throw new DataException($"{DefinitionName} '{id}': Weight must be within 0..1000.");
            }

            if (kind == TraitKind.Acquired && source == null)
            {
                throw new DataException($"{DefinitionName} '{id}': an acquired trait needs a Source.");
            }

            if (kind == TraitKind.Innate && source != null)
            {
                throw new DataException($"{DefinitionName} '{id}': an innate trait has no Source.");
            }

            if (build && kind != TraitKind.Innate)
            {
                throw new DataException($"{DefinitionName} '{id}': a build is an innate trait.");
            }

            if (figure != null)
            {
                DataId.Require(figure, $"{DefinitionName} '{id}' Figure");
                if (!build)
                {
                    throw new DataException($"{DefinitionName} '{id}': only a build names a figure variant.");
                }
            }

            if (figureScale < MinFigureScale || figureScale > MaxFigureScale)
            {
                throw new DataException($"{DefinitionName} '{id}': FigureScale must be within {MinFigureScale}..{MaxFigureScale}.");
            }

            if (figureScale != 100 && figure == null)
            {
                throw new DataException($"{DefinitionName} '{id}': only a build with a figure variant scales it.");
            }

            if (sourceState != null)
            {
                DataId.Require(sourceState, $"{DefinitionName} '{id}' SourceState");
                if (source != TraitSource.Affliction && source != TraitSource.Virtue)
                {
                    throw new DataException($"{DefinitionName} '{id}': only a trait of an affliction or a virtue names a SourceState.");
                }
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
                (nameof(MaxHpPercent), maxHpPercent), (nameof(GrowthPercent), growthPercent), (nameof(DamagePercent), damagePercent),
                (nameof(HealTakenPercent), healTakenPercent),
            })
            {
                if (value < -MaxShift || value > MaxShift)
                {
                    throw new DataException($"{DefinitionName} '{id}': {what} must be within -{MaxShift}..{MaxShift}.");
                }
            }

            // What the trait adds to the physical critical multiplier (2026-10-20 스탯 개정), in percent points; never under 0.
            if (critDamagePercent < 0 || critDamagePercent > ItemData.MaxCritDamageBonus)
            {
                throw new DataException($"{DefinitionName} '{id}': CritDamagePercent must be within 0..{ItemData.MaxCritDamageBonus}.");
            }

            foreach ((string what, int value) in new[] { (nameof(Str), str), (nameof(Dex), dex), (nameof(Con), con), (nameof(Int), @int), (nameof(Wis), wis), (nameof(End), end) })
            {
                if (value < -MaxStatShift || value > MaxStatShift)
                {
                    throw new DataException($"{DefinitionName} '{id}': {what} must be within -{MaxStatShift}..{MaxStatShift}.");
                }
            }

            if (allyFatigueAtStart < -MaxAllyFatigue || allyFatigueAtStart > MaxAllyFatigue)
            {
                throw new DataException($"{DefinitionName} '{id}': AllyFatigueAtStart must be within -{MaxAllyFatigue}..{MaxAllyFatigue}.");
            }

            bool shiftsStats = str != 0 || dex != 0 || con != 0 || @int != 0 || wis != 0 || end != 0;
            if (hitPercent == 0 && dodgePercent == 0 && critPercent == 0 && dogDeathPercent == 0 && virtuePercent == 0 && fatiguePercent == 0 && maxHpPercent == 0 && critDamagePercent == 0
                && !shiftsStats && growthPercent == 0 && damagePercent == 0 && healTakenPercent == 0 && allyFatigueAtStart == 0)
            {
                throw new DataException($"{DefinitionName} '{id}': a trait must change something.");
            }

            Kind = kind;
            Polarity = polarity;
            Source = source;
            Weight = weight;
            Excludes = excludes;
            HitPercent = hitPercent;
            DodgePercent = dodgePercent;
            CritPercent = critPercent;
            DogDeathPercent = dogDeathPercent;
            VirtuePercent = virtuePercent;
            FatiguePercent = fatiguePercent;
            MaxHpPercent = maxHpPercent;
            CritDamagePercent = critDamagePercent;
            Build = build;
            Rare = rare;
            Figure = figure;
            SourceState = sourceState;
            Str = str;
            Dex = dex;
            Con = con;
            Int = @int;
            Wis = wis;
            End = end;
            GrowthPercent = growthPercent;
            DamagePercent = damagePercent;
            HealTakenPercent = healTakenPercent;
            AllyFatigueAtStart = allyFatigueAtStart;
            FigureScale = figureScale;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Id { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public LocalizedText Name { get; }

        [JsonProperty(Order = 3, Required = Required.Always)]
        public TraitKind Kind { get; }

        [JsonProperty(Order = 4, Required = Required.Always)]
        public TraitPolarity Polarity { get; }

        /// <summary>What leaves an acquired trait at the settlement (stage 38); null for an innate trait.</summary>
        [JsonProperty(Order = 5, Required = Required.AllowNull)]
        public TraitSource? Source { get; }

        [JsonProperty(Order = 6, Required = Required.Always)]
        public int Weight { get; }

        [JsonProperty(Order = 7, Required = Required.AllowNull)]
        public string Excludes { get; }

        /// <summary>Added to the hit chance of the mercenary's attacks, in percent points.</summary>
        [JsonProperty(Order = 8, Required = Required.Always)]
        public int HitPercent { get; }

        /// <summary>Added to the mercenary's dodge, in percent points.</summary>
        [JsonProperty(Order = 9, Required = Required.Always)]
        public int DodgePercent { get; }

        /// <summary>Added to the critical chance of the mercenary's items that have one, in percent points.</summary>
        [JsonProperty(Order = 10, Required = Required.Always)]
        public int CritPercent { get; }

        /// <summary>Added to the mercenary's death chance on a hit at death's door, in percent points.</summary>
        [JsonProperty(Order = 11, Required = Required.Always)]
        public int DogDeathPercent { get; }

        /// <summary>Added to the mercenary's chance of a virtue at the breakdown, in percent points.</summary>
        [JsonProperty(Order = 12, Required = Required.Always)]
        public int VirtuePercent { get; }

        /// <summary>Added to the percent of a fatigue gain the mercenary takes (endurance's percent counts with it).</summary>
        [JsonProperty(Order = 13, Required = Required.Always)]
        public int FatiguePercent { get; }

        /// <summary>Added to the percent of the job's maximum HP the mercenary has (constitution's percent counts with it).</summary>
        [JsonProperty(Order = 14, Required = Required.Always)]
        public int MaxHpPercent { get; }

        /// <summary>Percent points added to the physical critical multiplier of the owner's weapons (2026-10-20 스탯 개정, Docs/Design/02 §4). 0 for most.</summary>
        [JsonProperty(Order = 15, Required = Required.Always)]
        public int CritDamagePercent { get; }

        /// <summary>Whether the trait is a build (stage 57): the one build slot's — two builds never stand together.</summary>
        [JsonProperty(Order = 16, Required = Required.Always)]
        public bool Build { get; }

        /// <summary>Whether the card calls the trait rare (stage 57). Only shown; how often it comes is its Weight.</summary>
        [JsonProperty(Order = 17, Required = Required.Always)]
        public bool Rare { get; }

        /// <summary>The figure variant a build is drawn in (stage 57: the job's figure address + "_" + this), or null for the job's own.</summary>
        [JsonProperty(Order = 18, Required = Required.AllowNull)]
        public string Figure { get; }

        /// <summary>The breakdown or virtue an acquired trait of that source marks (stage 57), or null for any of its kind.</summary>
        [JsonProperty(Order = 19, Required = Required.AllowNull)]
        public string SourceState { get; }

        /// <summary>Added to the mercenary's strength (stage 57). The stats stay within StatMin..StatMax.</summary>
        [JsonProperty(Order = 20, Required = Required.Always)]
        public int Str { get; }

        [JsonProperty(Order = 21, Required = Required.Always)]
        public int Dex { get; }

        [JsonProperty(Order = 22, Required = Required.Always)]
        public int Con { get; }

        [JsonProperty(Order = 23, Required = Required.Always)]
        public int Int { get; }

        [JsonProperty(Order = 24, Required = Required.Always)]
        public int Wis { get; }

        [JsonProperty(Order = 25, Required = Required.Always)]
        public int End { get; }

        /// <summary>Added to every growth grade of the mercenary's job at a level-up (stage 57), in percent; a grade never goes under 0.</summary>
        [JsonProperty(Order = 26, Required = Required.Always)]
        public int GrowthPercent { get; }

        /// <summary>Added to the percent of the damage the mercenary deals (stage 57): where a breakdown's or a virtue's damage counts.</summary>
        [JsonProperty(Order = 27, Required = Required.Always)]
        public int DamagePercent { get; }

        /// <summary>Added to the percent of the healing the mercenary takes (stage 57): where a breakdown's or a virtue's counts.</summary>
        [JsonProperty(Order = 28, Required = Required.Always)]
        public int HealTakenPercent { get; }

        /// <summary>The fatigue each of the mercenary's allies takes (or sheds) at the start of each battle (stage 57), as a breakdown's.</summary>
        [JsonProperty(Order = 29, Required = Required.Always)]
        public int AllyFatigueAtStart { get; }

        /// <summary>
        /// How large a build's figure variant stands on the stage (stage 57), in percent of the common figure place: a giant's variant is drawn on the common
        /// canvas and shown larger, as an enemy's FigureScale. Used only where the variant is drawn for the job; 100 otherwise.
        /// </summary>
        [JsonProperty(Order = 30, Required = Required.Always)]
        public int FigureScale { get; }

        /// <summary>Whether the trait shifts a stat (stage 57).</summary>
        public bool ShiftsStats => Str != 0 || Dex != 0 || Con != 0 || Int != 0 || Wis != 0 || End != 0;

        /// <summary>Whether two traits cannot stand together: one names the other as its opposite, or both are builds.</summary>
        public bool Clashes(TraitData other)
        {
            return other != null && (Excludes == other.Id || other.Excludes == Id || (Build && other.Build && other.Id != Id));
        }
    }
}
