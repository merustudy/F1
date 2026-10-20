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
            public KeySpec(string key, int min, int max, bool centi = false)
            {
                Key = key;
                Min = min;
                Max = max;
                Centi = centi;
            }

            public string Key { get; }
            public int Min { get; }
            public int Max { get; }

            /// <summary>A value in hundredths (Slice B stage 22, <see cref="Data.Centi"/>): the data writes "0.55" and the rules read 55.</summary>
            public bool Centi { get; }
        }

        const int Big = 1000000;

        static readonly KeySpec[] Specs =
        {
            new KeySpec(nameof(PartySize), 1, 6),
            new KeySpec(nameof(MinPartySize), 1, 6),
            new KeySpec(nameof(TotalDays), 1, Big),
            new KeySpec(nameof(DeathLimit), 1, Big),
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
            new KeySpec(nameof(StormBaseDamage), 1, Big, centi: true),
            new KeySpec(nameof(StormGrowth), 0, Big, centi: true),
            new KeySpec(nameof(BurnDamagePerStack), 1, Big, centi: true),
            new KeySpec(nameof(HitMinDamage), 0, Big, centi: true),
            new KeySpec(nameof(PhysicalCritDamagePercent), 100, 1000),
            new KeySpec(nameof(MagicCritDamagePercent), 100, 1000),
            new KeySpec(nameof(CritFatigueTaken), 0, Big),
            new KeySpec(nameof(CritFatigueTakenAllies), 0, Big),
            new KeySpec(nameof(CritFatigueDealt), 0, Big),
            new KeySpec(nameof(CritFatigueDealtAllies), 0, Big),
            new KeySpec(nameof(PotionSlots), 0, 6),
            new KeySpec(nameof(PotionCooldownMs), 0, Big),
            new KeySpec(nameof(RetreatChancePercent), 0, 100),
            new KeySpec(nameof(RetreatCooldownMs), 0, Big),
            new KeySpec(nameof(PostBattleHealPercent), 0, 100),
            new KeySpec(nameof(MinCooldownMs), 50, Big),
            new KeySpec(nameof(DropCount), 1, MaxLootCards),
            new KeySpec(nameof(EliteDropCount), 1, MaxLootCards),
            new KeySpec(nameof(InventoryWidth), BoardFrame.MaxSide, 30),
            new KeySpec(nameof(InventoryHeight), BoardFrame.MaxSide, 30),
            new KeySpec(nameof(EliteBagPercent), 0, 100),
            new KeySpec(nameof(DropTierKeepPercent), 0, 100),
            new KeySpec(nameof(CampHealPercent), 0, 100),
            new KeySpec(nameof(CampFatigueRelief), 0, Big),
            new KeySpec(nameof(Tier1Percent), 100, Big),
            new KeySpec(nameof(Tier2Percent), 100, Big),
            new KeySpec(nameof(Tier3Percent), 100, Big),
            new KeySpec(nameof(MapBranchChancePercent), 0, 100),
            new KeySpec(nameof(FinalBossLevel), 2, Big),
            new KeySpec(nameof(ShopSlots), 1, MaxShopSlots),
            new KeySpec(nameof(ShopRefreshBase), 0, Big),
            new KeySpec(nameof(ShopRefreshStep), 0, Big),
            new KeySpec(nameof(ShopPotionChancePercent), 0, 100),
            new KeySpec(nameof(CoinsPerEnemy), 0, Big),
            new KeySpec(nameof(CoinsPerFloor), 0, Big),
            new KeySpec(nameof(ExpeditionStartCoins), 0, Big),
            new KeySpec(nameof(EliteCoinPercent), 100, Big),
            new KeySpec(nameof(SellPercent), 0, 100),
            new KeySpec(nameof(CoinsPerGold), 1, Big),
            new KeySpec(nameof(GoldPerFloor), 0, Big),
            new KeySpec(nameof(GoldClear), 0, Big),
            new KeySpec(nameof(HirePrice), 0, Big),
            new KeySpec(nameof(RecruitCount), 0, 10),
            new KeySpec(nameof(RosterLimit), 1, Big),
            new KeySpec(nameof(TreatmentPrice), 0, Big),
            new KeySpec(nameof(TreatmentRelief), 1, Big),
            new KeySpec(nameof(ExpPerFloor), 0, Big),
            new KeySpec(nameof(ExpClear), 0, Big),
            new KeySpec(nameof(StatBaseline), 1, 100),
            new KeySpec(nameof(StatMin), 1, 100),
            new KeySpec(nameof(StatMax), 1, 100),
            new KeySpec(nameof(StatPowerPercentPerPoint), 0, 100),
            new KeySpec(nameof(PowerBaseline), 1, 100),
            new KeySpec(nameof(FatigueBaseline), 1, 100),
            new KeySpec(nameof(CooldownPercentPerPoint), 0, 100),
            new KeySpec(nameof(HpPerConPointLow), 1, Big, centi: true),
            new KeySpec(nameof(HpPerConPointMid), 1, Big, centi: true),
            new KeySpec(nameof(HpPerConPointHigh), 1, Big, centi: true),
            new KeySpec(nameof(HpBandLow), 1, 100),
            new KeySpec(nameof(HpBandMid), 1, 100),
            new KeySpec(nameof(HitBasePercent), 0, 100),
            new KeySpec(nameof(HitMinPercent), 0, 100),
            new KeySpec(nameof(HitMaxPercent), 0, 100),
            new KeySpec(nameof(HitPerDexPoint), 0, 10000, centi: true),
            new KeySpec(nameof(DodgePerDexPoint), 0, 10000, centi: true),
            new KeySpec(nameof(CritPerDexPoint), 0, 10000, centi: true),
            new KeySpec(nameof(CritPerWisPoint), 0, 10000, centi: true),
            new KeySpec(nameof(DogDeathPercentPerEndPoint), 0, 100),
            new KeySpec(nameof(VirtuePerEndPoint), 0, 100),
            new KeySpec(nameof(FatiguePercentPerEndPoint), 0, 100),
            new KeySpec(nameof(RecruitStatSpread), 0, 10),
            new KeySpec(nameof(RecruitTraitWeight0), 0, 1000),
            new KeySpec(nameof(RecruitTraitWeight1), 0, 1000),
            new KeySpec(nameof(RecruitTraitWeight2), 0, 1000),
            new KeySpec(nameof(TraitChanceClearPercent), 0, 100),
            new KeySpec(nameof(TraitChanceRetreatPercent), 0, 100),
            new KeySpec(nameof(TraitChanceDeathsDoorPercent), 0, 100),
            new KeySpec(nameof(TraitChanceAfflictionPercent), 0, 100),
            new KeySpec(nameof(TraitChanceVirtuePercent), 0, 100),
        };

        /// <summary>The most things a shop can offer at once (Slice B stage 21: the merchant's grid holds any eight of them; Docs/Architecture/12_UI.md "상인").</summary>
        public const int MaxShopSlots = 8;

        /// <summary>The most drops a battle can leave: one on the floor before each of the enemy's rows 1 to 3 (Docs/Architecture/12_UI.md "이긴 뒤: 전리품").</summary>
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

            if (Tier1Percent <= 100 || Tier2Percent <= Tier1Percent || Tier3Percent <= Tier2Percent)
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

            // The stats (Slice D stage 32): the baseline lies in the range, and even the lowest stat leaves an item some power and a mercenary some HP.
            if (StatMin > StatBaseline || StatBaseline > StatMax)
            {
                throw new DataException($"{DefinitionName}: StatBaseline must lie within StatMin..StatMax.");
            }

            // 2026-10-20 스탯 개정: the power and fatigue baselines lie in the range too; the lowest stat keeps some power, the highest some cooldown; the HP bands rise.
            if (StatMin > PowerBaseline || PowerBaseline > StatMax || StatMin > FatigueBaseline || FatigueBaseline > StatMax)
            {
                throw new DataException($"{DefinitionName}: PowerBaseline and FatigueBaseline must lie within StatMin..StatMax.");
            }

            if (100 - StatPowerPercentPerPoint * (PowerBaseline - StatMin) < 1)
            {
                throw new DataException($"{DefinitionName}: StatPowerPercentPerPoint leaves the lowest stat no power (under 1%).");
            }

            if (100 - CooldownPercentPerPoint * (StatMax - PowerBaseline) < 1)
            {
                throw new DataException($"{DefinitionName}: CooldownPercentPerPoint leaves the highest stat no cooldown (under 1%).");
            }

            if (HpBandLow >= HpBandMid)
            {
                throw new DataException($"{DefinitionName}: HpBandLow must be under HpBandMid.");
            }

            if (HitMinPercent > HitBasePercent || HitBasePercent > HitMaxPercent)
            {
                throw new DataException($"{DefinitionName}: HitBasePercent must lie within HitMinPercent..HitMaxPercent.");
            }

            // How many innate traits a candidate draws (Slice D stage 37): some count must be possible.
            if (RecruitTraitWeight0 + RecruitTraitWeight1 + RecruitTraitWeight2 <= 0)
            {
                throw new DataException($"{DefinitionName}: RecruitTraitWeight0..2 cannot all be 0.");
            }

            if (RosterLimit < PartySize)
            {
                throw new DataException($"{DefinitionName}: RosterLimit cannot be under PartySize, or a full party could never be formed.");
            }
        }

        /// <summary>True for a key whose value is in hundredths: the data writes it with up to two decimals (stage 22).</summary>
        public static bool IsCenti(string key)
        {
            return Specs.Any(spec => spec.Key == key && spec.Centi);
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

        /// <summary>Deaths that lose a run (Slice C stage 25, Docs/Design/04_Lobby_100Day_Economy.md §4).</summary>
        public int DeathLimit => _values[nameof(DeathLimit)];
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

        /// <summary>
        /// Slice B stage 22 (Docs/Design/02_Combat_System.md §4·§5, 04 §3): a burn's tick deals this per stack (hundredths); damage that gets
        /// through a shield is a hit only from <see cref="HitMinDamage"/> (hundredths) or when it takes the HP to 0; a critical hit is the
        /// rolled damage times <see cref="PhysicalCritDamagePercent"/>% (a magic weapon's <see cref="MagicCritDamagePercent"/>%; 2026-10-20 스탯 개정), and tires the one it hit and its allies, and relieves the one who dealt it and
        /// its allies.
        /// </summary>
        public int BurnDamagePerStack => _values[nameof(BurnDamagePerStack)];
        public int HitMinDamage => _values[nameof(HitMinDamage)];
        public int PhysicalCritDamagePercent => _values[nameof(PhysicalCritDamagePercent)];
        public int MagicCritDamagePercent => _values[nameof(MagicCritDamagePercent)];
        public int CritFatigueTaken => _values[nameof(CritFatigueTaken)];
        public int CritFatigueTakenAllies => _values[nameof(CritFatigueTakenAllies)];
        public int CritFatigueDealt => _values[nameof(CritFatigueDealt)];
        public int CritFatigueDealtAllies => _values[nameof(CritFatigueDealtAllies)];
        public int PotionSlots => _values[nameof(PotionSlots)];
        public int PotionCooldownMs => _values[nameof(PotionCooldownMs)];
        public int RetreatChancePercent => _values[nameof(RetreatChancePercent)];
        public int RetreatCooldownMs => _values[nameof(RetreatCooldownMs)];
        public int PostBattleHealPercent => _values[nameof(PostBattleHealPercent)];
        public int MinCooldownMs => _values[nameof(MinCooldownMs)];
        /// <summary>How many of the items the enemies of a won battle carried drop as loot (Slice B stage 18); an elite group drops <see cref="EliteDropCount"/>.</summary>
        public int DropCount => _values[nameof(DropCount)];
        public int EliteDropCount => _values[nameof(EliteDropCount)];
        /// <summary>
        /// The expedition inventory's grid (round 49, Docs/Design/03_Dungeon_Structure.md §5: Diablo II's inventory): its width and height in
        /// squares. Each is at least the longest side an item can have, so any item fits an empty inventory.
        /// </summary>
        public int InventoryWidth => _values[nameof(InventoryWidth)];
        public int InventoryHeight => _values[nameof(InventoryHeight)];

        /// <summary>
        /// The chance, in percent, that an elite's loot holds a bag (Slice B stage 19, Docs/Design/03_Dungeon_Structure.md §5): its last drop is
        /// a bag drawn by <see cref="BagData.LootWeight"/> instead of an item. The number of drops stays.
        /// </summary>
        public int EliteBagPercent => _values[nameof(EliteBagPercent)];

        /// <summary>
        /// The chance, in percent, that a dropped item keeps the loot's tier (2026-10-19 play feedback, Docs/Design/03_Dungeon_Structure.md §5):
        /// else it drops a tier lower. Common rolls nothing; 100 is the rule before it.
        /// </summary>
        public int DropTierKeepPercent => _values[nameof(DropTierKeepPercent)];
        /// <summary>Resting at a camp: the share of their maximum HP every living member gets back, and how much their fatigue comes down.</summary>
        public int CampHealPercent => _values[nameof(CampHealPercent)];
        public int CampFatigueRelief => _values[nameof(CampFatigueRelief)];
        /// <summary>How big an item's effects are at a tier, in percent of Common's.</summary>
        public int Tier1Percent => _values[nameof(Tier1Percent)];
        public int Tier2Percent => _values[nameof(Tier2Percent)];
        public int Tier3Percent => _values[nameof(Tier3Percent)];

        /// <summary>The percent of Common's effects an item has at a tier: 100 at Common.</summary>
        public int TierPercent(ItemTier tier)
        {
            switch (tier)
            {
                case ItemTier.Tier1: return Tier1Percent;
                case ItemTier.Tier2: return Tier2Percent;
                case ItemTier.Tier3: return Tier3Percent;
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

        /// <summary>The chance of each potion a shop stocks to be on offer (round 56); when none is, one is, by its weight.</summary>
        public int ShopPotionChancePercent => _values[nameof(ShopPotionChancePercent)];
        public int ShopRefreshBase => _values[nameof(ShopRefreshBase)];
        public int ShopRefreshStep => _values[nameof(ShopRefreshStep)];

        /// <summary>What a shop pays for an item of the party's, as a percent of what it would sell it for at its tier (2026-10-17, Round 74).</summary>
        public int SellPercent => _values[nameof(SellPercent)];

        /// <summary>
        /// How many region coins make one gold at the settlement of a cleared expedition (2026-10-17): the coins left plus what the
        /// shop would pay for the items the party brings home are turned into gold, the remainder lost with the expedition.
        /// </summary>
        public int CoinsPerGold => _values[nameof(CoinsPerGold)];

        /// <summary>
        /// The region coins a won battle brings: for each enemy of the group, and for each floor below the first; an elite's are
        /// this percent of that (Docs/Design/03_Dungeon_Structure.md §5).
        /// </summary>
        public int CoinsPerEnemy => _values[nameof(CoinsPerEnemy)];
        public int CoinsPerFloor => _values[nameof(CoinsPerFloor)];

        /// <summary>
        /// The region coins an expedition starts with (2026-10-19, with the shop floor: the first shop can sell a couple of things, so that the
        /// build can be thought of early). A clear brings the coins left home as gold (CoinsPerGold).
        /// </summary>
        public int ExpeditionStartCoins => _values[nameof(ExpeditionStartCoins)];
        public int EliteCoinPercent => _values[nameof(EliteCoinPercent)];

        /// <summary>
        /// The lobby's gold (Slice C stage 26, Docs/Design/04_Lobby_100Day_Economy.md §5·§9): what the return settlement pays for each map floor
        /// the party reached, and on top of that for clearing the dungeon (its boss). It comes in nowhere else; a run starts with none.
        /// </summary>
        public int GoldPerFloor => _values[nameof(GoldPerFloor)];
        public int GoldClear => _values[nameof(GoldClear)];

        /// <summary>
        /// The stagecoach (Docs/Design/04 §6): what hiring a candidate costs (one price, whatever the job), how many candidates are drawn at
        /// each settlement, and the most mercenaries the roster holds.
        /// </summary>
        public int HirePrice => _values[nameof(HirePrice)];
        public int RecruitCount => _values[nameof(RecruitCount)];
        public int RosterLimit => _values[nameof(RosterLimit)];

        /// <summary>The inn's treatment (Docs/Design/04 §3): what it costs, and how much fatigue it takes off one mercenary without a day passing.</summary>
        public int TreatmentPrice => _values[nameof(TreatmentPrice)];
        public int TreatmentRelief => _values[nameof(TreatmentRelief)];

        /// <summary>
        /// The experience the return settlement gives every surviving party member (Slice C stage 27, Docs/Design/05_Mercenary_Growth.md §2):
        /// for each map floor the party reached, and on top of that for clearing the dungeon. The levels it takes them to are LevelData.
        /// </summary>
        public int ExpPerFloor => _values[nameof(ExpPerFloor)];
        public int ExpClear => _values[nameof(ExpClear)];

        /// <summary>
        /// The stats (Slice D stage 32, Docs/Design/05_Mercenary_Growth.md §2): the baseline every effect is counted from, the range a stat stays in,
        /// how much each point above (or below) the baseline grows an item's power (Docs/Design/02 §4) and a mercenary's maximum HP (05 §2), in percent.
        /// </summary>
        public int StatBaseline => _values[nameof(StatBaseline)];
        public int StatMin => _values[nameof(StatMin)];
        public int StatMax => _values[nameof(StatMax)];
        public int StatPowerPercentPerPoint => _values[nameof(StatPowerPercentPerPoint)];

        /// <summary>
        /// 2026-10-20 스탯 개정 (Docs/Design/05 §2, 02 §4, 04 §3): the baseline an item's power and cooldown count from (strength, intellect, wisdom; dexterity
        /// and wisdom for the cooldown), what each point above it takes off the cooldown of the owner's equipment (percent), the baseline the fatigue a
        /// mercenary gains counts from, and the HP each point of constitution gives (hundredths): the low band up to HpBandLow points, the middle band up to
        /// HpBandMid, the high band above. The rolls (hit, dodge, critical hits, the virtue, the death at death's door) count from StatBaseline.
        /// </summary>
        public int PowerBaseline => _values[nameof(PowerBaseline)];
        public int CooldownPercentPerPoint => _values[nameof(CooldownPercentPerPoint)];
        public int FatigueBaseline => _values[nameof(FatigueBaseline)];
        public int HpPerConPointLow => _values[nameof(HpPerConPointLow)];
        public int HpPerConPointMid => _values[nameof(HpPerConPointMid)];
        public int HpPerConPointHigh => _values[nameof(HpPerConPointHigh)];
        public int HpBandLow => _values[nameof(HpBandLow)];
        public int HpBandMid => _values[nameof(HpBandMid)];

        /// <summary>
        /// The stats' rolls (Slice D stage 33, Docs/Design/02_Combat_System.md §4·§5·§6, 04 §3): the hit chance against a target at the baseline and
        /// the bounds it stays in (whole percents); what each point of the attacking item's stat adds to it, each point of the target's dexterity takes
        /// off it and each point of dexterity adds to a weapon's critical chance (hundredths of a percent point: "0.5" is 50, which the rolls count
        /// in ten-thousandths); what each point of constitution takes off the death chance at death's door and each point of spirit adds to a
        /// virtue (percent points); and what each point of endurance takes off the fatigue a mercenary gains (percent).
        /// </summary>
        public int HitBasePercent => _values[nameof(HitBasePercent)];
        public int HitMinPercent => _values[nameof(HitMinPercent)];
        public int HitMaxPercent => _values[nameof(HitMaxPercent)];
        public int HitPerDexPoint => _values[nameof(HitPerDexPoint)];
        public int DodgePerDexPoint => _values[nameof(DodgePerDexPoint)];
        public int CritPerDexPoint => _values[nameof(CritPerDexPoint)];
        public int CritPerWisPoint => _values[nameof(CritPerWisPoint)];
        public int DogDeathPercentPerEndPoint => _values[nameof(DogDeathPercentPerEndPoint)];
        public int VirtuePerEndPoint => _values[nameof(VirtuePerEndPoint)];
        public int FatiguePercentPerEndPoint => _values[nameof(FatiguePercentPerEndPoint)];

        /// <summary>How far a candidate's stat may lie from its job's base value, either way (Slice D stage 34, Docs/Design/04 §6): drawn evenly in -n..n.</summary>
        public int RecruitStatSpread => _values[nameof(RecruitStatSpread)];

        /// <summary>
        /// The weights of how many innate traits a candidate is born with (Slice D stage 37, Docs/Design/05 §6): none, one or two. The draw happens
        /// only when one or two can come (a weight above 0 for them) and there is a trait to draw.
        /// </summary>
        public int RecruitTraitWeight0 => _values[nameof(RecruitTraitWeight0)];
        public int RecruitTraitWeight1 => _values[nameof(RecruitTraitWeight1)];
        public int RecruitTraitWeight2 => _values[nameof(RecruitTraitWeight2)];

        /// <summary>
        /// The chance, in percent, that a survivor acquires a trait of each source at the return settlement (Slice D stage 38, Docs/Design/05 §6):
        /// a cleared expedition, a retreat, a return from death's door, coming home in an affliction, a virtue. Rolled only when a trait of the
        /// source can still be taken.
        /// </summary>
        public int TraitChanceClearPercent => _values[nameof(TraitChanceClearPercent)];
        public int TraitChanceRetreatPercent => _values[nameof(TraitChanceRetreatPercent)];
        public int TraitChanceDeathsDoorPercent => _values[nameof(TraitChanceDeathsDoorPercent)];
        public int TraitChanceAfflictionPercent => _values[nameof(TraitChanceAfflictionPercent)];
        public int TraitChanceVirtuePercent => _values[nameof(TraitChanceVirtuePercent)];
    }
}
