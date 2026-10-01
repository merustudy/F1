using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace F1.Data
{
    /// <summary>One row of BalanceData.csv.</summary>
    public sealed class BalanceEntry
    {
        [JsonConstructor]
        public BalanceEntry(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
            {
                throw new DataException("Balance key is empty.");
            }

            Key = key;
            Value = value;
        }

        [JsonProperty(Order = 1, Required = Required.Always)]
        public string Key { get; }

        [JsonProperty(Order = 2, Required = Required.Always)]
        public int Value { get; }
    }

    /// <summary>
    /// The constants of the rules. Every key is required, every key is known, and every value is
    /// range-checked, so code never falls back to a hidden default.
    /// </summary>
    public sealed class BalanceData
    {
        public const string DefinitionName = "Balance";

        readonly struct KeySpec
        {
            public KeySpec(string key, int min, int max)
            {
                Key = key;
                Min = min;
                Max = max;
            }

            public string Key { get; }
            public int Min { get; }
            public int Max { get; }
        }

        const int Big = 1000000;

        static readonly KeySpec[] Specs =
        {
            new KeySpec(nameof(PartySize), 1, 6),
            new KeySpec(nameof(MinPartySize), 1, 6),
            new KeySpec(nameof(TotalDays), 1, Big),
            new KeySpec(nameof(MaxFatigue), 1, Big),
            new KeySpec(nameof(FatigueRecoveryPerDay), 0, Big),
            new KeySpec(nameof(RestDays), 1, Big),
            new KeySpec(nameof(DogGraceMs), 0, Big),
            new KeySpec(nameof(DogGraceBreakHits), 1, 100),
            new KeySpec(nameof(DogDeathChancePercent), 0, 100),
            new KeySpec(nameof(BurnTickMs), 100, Big),
            new KeySpec(nameof(StormStartMs), 1000, Big),
            new KeySpec(nameof(StormTickMs), 100, Big),
            new KeySpec(nameof(StormBaseDamage), 1, Big),
            new KeySpec(nameof(StormGrowth), 0, Big),
            new KeySpec(nameof(PotionSlots), 0, 6),
            new KeySpec(nameof(PotionCooldownMs), 0, Big),
            new KeySpec(nameof(RetreatChancePercent), 0, 100),
            new KeySpec(nameof(RetreatCooldownMs), 0, Big),
            new KeySpec(nameof(PostBattleHealPercent), 0, 100),
            new KeySpec(nameof(MinCooldownMs), 50, Big),
            new KeySpec(nameof(RewardChoices), 1, 5),
            new KeySpec(nameof(MapBranchChancePercent), 0, 100),
            new KeySpec(nameof(FinalBossLevel), 2, Big),
        };

        readonly Dictionary<string, int> _values = new Dictionary<string, int>(StringComparer.Ordinal);

        public BalanceData(IEnumerable<BalanceEntry> entries)
        {
            if (entries == null)
            {
                throw new DataException($"{DefinitionName}: no data.");
            }

            foreach (BalanceEntry entry in entries)
            {
                if (_values.ContainsKey(entry.Key))
                {
                    throw new DataException($"{DefinitionName}: duplicate key '{entry.Key}'.");
                }

                _values.Add(entry.Key, entry.Value);
            }

            foreach (KeySpec spec in Specs)
            {
                if (!_values.TryGetValue(spec.Key, out int value))
                {
                    throw new DataException($"{DefinitionName}: key '{spec.Key}' is missing.");
                }

                if (value < spec.Min || value > spec.Max)
                {
                    throw new DataException($"{DefinitionName}: '{spec.Key}' = {value} is outside {spec.Min}..{spec.Max}.");
                }
            }

            if (_values.Count != Specs.Length)
            {
                string unknown = _values.Keys.First(key => Specs.All(spec => spec.Key != key));
                throw new DataException($"{DefinitionName}: unknown key '{unknown}'.");
            }

            if (MinPartySize > PartySize)
            {
                throw new DataException($"{DefinitionName}: MinPartySize cannot exceed PartySize.");
            }

            // One mercenary stands in each row.
            if (PartySize > BattleRows.Count)
            {
                throw new DataException($"{DefinitionName}: PartySize cannot exceed the {BattleRows.Count} rows of a side.");
            }
        }

        /// <summary>The rows in key order, for writing the generated file.</summary>
        public List<BalanceEntry> ToEntries()
        {
            return _values
                .OrderBy(pair => pair.Key, StringComparer.Ordinal)
                .Select(pair => new BalanceEntry(pair.Key, pair.Value))
                .ToList();
        }

        public int PartySize => _values[nameof(PartySize)];
        public int MinPartySize => _values[nameof(MinPartySize)];
        public int TotalDays => _values[nameof(TotalDays)];
        public int MaxFatigue => _values[nameof(MaxFatigue)];
        public int FatigueRecoveryPerDay => _values[nameof(FatigueRecoveryPerDay)];
        public int RestDays => _values[nameof(RestDays)];
        public int DogGraceMs => _values[nameof(DogGraceMs)];
        public int DogGraceBreakHits => _values[nameof(DogGraceBreakHits)];
        public int DogDeathChancePercent => _values[nameof(DogDeathChancePercent)];
        public int BurnTickMs => _values[nameof(BurnTickMs)];
        public int StormStartMs => _values[nameof(StormStartMs)];
        public int StormTickMs => _values[nameof(StormTickMs)];
        public int StormBaseDamage => _values[nameof(StormBaseDamage)];
        public int StormGrowth => _values[nameof(StormGrowth)];
        public int PotionSlots => _values[nameof(PotionSlots)];
        public int PotionCooldownMs => _values[nameof(PotionCooldownMs)];
        public int RetreatChancePercent => _values[nameof(RetreatChancePercent)];
        public int RetreatCooldownMs => _values[nameof(RetreatCooldownMs)];
        public int PostBattleHealPercent => _values[nameof(PostBattleHealPercent)];
        public int MinCooldownMs => _values[nameof(MinCooldownMs)];
        public int RewardChoices => _values[nameof(RewardChoices)];
        /// <summary>Chance that a map node also leads to the neighbour of its nearest node on the next floor.</summary>
        public int MapBranchChancePercent => _values[nameof(MapBranchChancePercent)];
        /// <summary>The enemy level reserved for the final boss. No other enemy may use it or a higher one.</summary>
        public int FinalBossLevel => _values[nameof(FinalBossLevel)];
    }
}
