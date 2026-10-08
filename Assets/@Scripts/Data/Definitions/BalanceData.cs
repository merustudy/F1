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
            new KeySpec(nameof(FatigueBreakdown), 1, Big),
            new KeySpec(nameof(FatigueBattleEntry), 0, Big),
            new KeySpec(nameof(FatigueEquipment), 0, Big),
            new KeySpec(nameof(FatigueOnHit), 0, Big),
            new KeySpec(nameof(FatigueOnDog), 0, Big),
            new KeySpec(nameof(FatigueOnAllyDog), 0, Big),
            new KeySpec(nameof(FatigueOnAllyDeath), 0, Big),
            new KeySpec(nameof(FatigueOnKill), 0, Big),
            new KeySpec(nameof(VirtueChancePercent), 0, 100),
            new KeySpec(nameof(VirtueFatigue), 0, Big),
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
            new KeySpec(nameof(DropCount), 1, MaxLootCards),
            new KeySpec(nameof(EliteDropCount), 1, MaxLootCards),
            new KeySpec(nameof(InventoryCells), ItemData.MaxSize, 100),
            new KeySpec(nameof(CampHealPercent), 0, 100),
            new KeySpec(nameof(CampFatigueRelief), 0, Big),
            new KeySpec(nameof(TierBronzePercent), 100, Big),
            new KeySpec(nameof(TierSilverPercent), 100, Big),
            new KeySpec(nameof(TierGoldPercent), 100, Big),
            new KeySpec(nameof(MapBranchChancePercent), 0, 100),
            new KeySpec(nameof(FinalBossLevel), 2, Big),
            new KeySpec(nameof(ShopSlots), 1, MaxShopSlots),
            new KeySpec(nameof(ShopRefreshBase), 0, Big),
            new KeySpec(nameof(ShopRefreshStep), 0, Big),
            new KeySpec(nameof(CoinsPerEnemy), 0, Big),
            new KeySpec(nameof(CoinsPerFloor), 0, Big),
            new KeySpec(nameof(EliteCoinPercent), 100, Big),
        };

        /// <summary>The most things a shop can offer at once: what its window has room for (Docs/Architecture/12_UI.md "상점").</summary>
        public const int MaxShopSlots = 4;

        /// <summary>The most drops a battle can leave: what the loot screen has room for (Docs/Architecture/12_UI.md "전리품 화면").</summary>
        public const int MaxLootCards = 3;

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

            if (TierBronzePercent <= 100 || TierSilverPercent <= TierBronzePercent || TierGoldPercent <= TierSilverPercent)
            {
                throw new DataException($"{DefinitionName}: every tier must be stronger than the one under it (Common is 100).");
            }

            if (FatigueBreakdown > MaxFatigue)
            {
                throw new DataException($"{DefinitionName}: FatigueBreakdown cannot exceed MaxFatigue.");
            }

            if (VirtueFatigue >= FatigueBreakdown)
            {
                throw new DataException($"{DefinitionName}: VirtueFatigue must be under FatigueBreakdown, or a virtue would break down again at once.");
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
        /// <summary>The most fatigue a mercenary can carry (it builds up from 0). Reaching it is collapsing (Docs/Design/04_Lobby_100Day_Economy.md §3).</summary>
        public int MaxFatigue => _values[nameof(MaxFatigue)];
        /// <summary>How much fatigue comes down for each day a mercenary stays home.</summary>
        public int FatigueRecoveryPerDay => _values[nameof(FatigueRecoveryPerDay)];
        /// <summary>Where breaking down begins: the breakdown check (Docs/Design/04_Lobby_100Day_Economy.md §3). The lobby shows the fatigue past it in red.</summary>
        public int FatigueBreakdown => _values[nameof(FatigueBreakdown)];

        /// <summary>
        /// Fatigue a mercenary gains in battle (Docs/Design/04_Lobby_100Day_Economy.md §3): from a hit the shield did not stop, from
        /// reaching death's door, from an ally reaching it, from an ally dying; and what killing an enemy takes off.
        /// </summary>
        public int FatigueOnHit => _values[nameof(FatigueOnHit)];
        public int FatigueOnDog => _values[nameof(FatigueOnDog)];
        public int FatigueOnAllyDog => _values[nameof(FatigueOnAllyDog)];
        public int FatigueOnAllyDeath => _values[nameof(FatigueOnAllyDeath)];
        public int FatigueOnKill => _values[nameof(FatigueOnKill)];

        /// <summary>The breakdown at the threshold: the chance it is a virtue rather than an affliction, and the fatigue a virtue brings the unit down to.</summary>
        public int VirtueChancePercent => _values[nameof(VirtueChancePercent)];
        public int VirtueFatigue => _values[nameof(VirtueFatigue)];
        /// <summary>The fatigue every living member pays when a battle starts.</summary>
        public int FatigueBattleEntry => _values[nameof(FatigueBattleEntry)];
        /// <summary>The fatigue each piece of equipment on a board (not a base weapon) adds when a battle starts.</summary>
        public int FatigueEquipment => _values[nameof(FatigueEquipment)];
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
        /// <summary>How many of the items the enemies of a won battle carried drop as loot (Slice B stage 18); an elite group drops <see cref="EliteDropCount"/>.</summary>
        public int DropCount => _values[nameof(DropCount)];
        public int EliteDropCount => _values[nameof(EliteDropCount)];
        /// <summary>Cells of the expedition inventory. An item takes its size there as on a board, so it holds at least the biggest item.</summary>
        public int InventoryCells => _values[nameof(InventoryCells)];
        /// <summary>Resting at a camp: the share of their maximum HP every living member gets back, and how much their fatigue comes down.</summary>
        public int CampHealPercent => _values[nameof(CampHealPercent)];
        public int CampFatigueRelief => _values[nameof(CampFatigueRelief)];
        /// <summary>How big an item's effects are at a tier, in percent of Common's.</summary>
        public int TierBronzePercent => _values[nameof(TierBronzePercent)];
        public int TierSilverPercent => _values[nameof(TierSilverPercent)];
        public int TierGoldPercent => _values[nameof(TierGoldPercent)];

        /// <summary>The percent of Common's effects an item has at a tier: 100 at Common.</summary>
        public int TierPercent(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Bronze: return TierBronzePercent;
                case ItemTier.Silver: return TierSilverPercent;
                case ItemTier.Gold: return TierGoldPercent;
                default: return 100;
            }
        }
        /// <summary>Chance that a map node also leads to the neighbour of its nearest node on the next floor.</summary>
        public int MapBranchChancePercent => _values[nameof(MapBranchChancePercent)];
        /// <summary>The enemy level reserved for the final boss. No other enemy may use it or a higher one.</summary>
        public int FinalBossLevel => _values[nameof(FinalBossLevel)];

        /// <summary>
        /// The shop (Slice B stage 17, Docs/Design/03_Dungeon_Structure.md §5): how many things it offers at once, and what a refresh
        /// costs the first time and how much more each time after, within one shop.
        /// </summary>
        public int ShopSlots => _values[nameof(ShopSlots)];
        public int ShopRefreshBase => _values[nameof(ShopRefreshBase)];
        public int ShopRefreshStep => _values[nameof(ShopRefreshStep)];

        /// <summary>
        /// The region coins a won battle brings: for each enemy of the group, and for each floor below the first; an elite's are
        /// this percent of that (Docs/Design/03_Dungeon_Structure.md §5).
        /// </summary>
        public int CoinsPerEnemy => _values[nameof(CoinsPerEnemy)];
        public int CoinsPerFloor => _values[nameof(CoinsPerFloor)];
        public int EliteCoinPercent => _values[nameof(EliteCoinPercent)];
    }
}
