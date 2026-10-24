using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;

namespace F1.Tests
{
    /// <summary>
    /// Small hand-built data for rule tests. Tests use this instead of the shipped CSV so that
    /// balance changes never break them.
    /// </summary>
    internal static class TestData
    {
        public static LocalizedText Text(string text)
        {
            return new LocalizedText(new Dictionary<string, string> { { "ko-KR", text }, { "en-US", text } });
        }

        /// <summary>Default constants chosen to make arithmetic in tests easy. Override what a test cares about.</summary>
        public static BalanceData Balance(params (string Key, int Value)[] overrides)
        {
            return new BalanceData(BalanceEntries(overrides));
        }

        public static List<BalanceEntry> BalanceEntries(params (string Key, int Value)[] overrides)
        {
            var values = new Dictionary<string, int>
            {
                { "PartySize", 3 },
                { "MinPartySize", 1 },
                { "TotalDays", 100 },
                { "DeathLimit", 15 },
                { "MaxFatigue", 200 },
                { "FatigueRecoveryPerDay", 10 },
                { "FatigueBreakdown", 100 },
                { "FatigueBattleEntry", 5 },
                { "FatigueEquipment", 1 },
                { "FatigueOnHit", 0 },
                { "FatigueOnDog", 0 },
                { "FatigueOnAllyDog", 0 },
                { "FatigueOnAllyDeath", 0 },
                { "FatigueOnKill", 0 },
                { "VirtueChancePercent", 25 },
                { "VirtueFatigue", 40 },
                // Stage 48, at the shipped recovery (a mental breakdown ends at 0) and no overwork, so the cooldowns of the older tests stay as they were; the overwork tests set 25.
                { "AfflictionRecovery", 0 },
                { "OverworkCooldownPercent", 0 },
                { "RestDays", 1 },
                { "FreeRetreatPercent", 0 },
                { "DogGraceMs", 3000 },
                { "DogGraceBreakHits", 3 },
                { "DogDeathChancePercent", 100 },
                { "BurnTickMs", 1000 },
                { "StormStartMs", 600000 },
                { "StormTickMs", 1000 },
                { "StormBaseDamage", 5 },
                { "StormGrowth", 5 },
                // Slice B stage 22, at the values that keep the arithmetic of these tests as before: a burn stack deals 1, every damage that
                // gets through is a hit, and no item has a crit chance unless a test gives it one.
                { "BurnDamagePerStack", 1 },
                // Slice D stage 44, at the same easy values: a bleed stack deals 1 and a regen stack heals 1 at a tick.
                { "BleedDamagePerStack", 1 },
                { "RegenHealPerStack", 1 },
                // Stage 45, at the shipped values: a stack of freeze or haste moves cooldowns 10%, of blind or aim the hit 5 points, of keen the critical
                // chance 5 points, of fury the multiplier 25 points, of vulnerable the damage taken 10%.
                { "FreezePercentPerStack", 10 },
                { "BlindPercentPerStack", 5 },
                { "AimPercentPerStack", 5 },
                { "KeenPercentPerStack", 5 },
                { "FuryPercentPerStack", 25 },
                { "HastePercentPerStack", 10 },
                { "VulnerablePercentPerStack", 10 },
                { "HitMinDamage", 0 },
                { "PhysicalCritDamagePercent", 150 },
                { "MagicCritDamagePercent", 150 },
                { "CritFatigueTaken", 0 },
                { "CritFatigueTakenAllies", 0 },
                { "CritFatigueDealt", 0 },
                { "CritFatigueDealtAllies", 0 },
                { "PotionSlots", 3 },
                { "PotionCooldownMs", 1500 },
                { "RetreatChancePercent", 100 },
                { "RetreatCooldownMs", 5000 },
                { "MinCooldownMs", 200 },
                { "DropCount", 2 },
                { "EliteDropCount", 3 },
                { "InventoryWidth", 10 },
                { "InventoryHeight", 3 },
                { "EliteBagPercent", 0 },
                // Drops keep the loot's tier (the rule before 2026-10-19), so the tests' drops stay as they were.
                { "DropTierKeepPercent", 100 },
                // Stage 49: no drop copies a board's item (the rule before it), so the tests' drops stay as they were.
                { "DropOwnedPercent", 0 },
                { "CampHealPercent", 30 },
                { "CampFatigueRelief", 20 },
                { "Tier1Percent", 200 },
                { "Tier2Percent", 300 },
                { "Tier3Percent", 400 },
                { "MapBranchChancePercent", 50 },
                { "FinalBossLevel", 14 },
                { "ShopSlots", 4 },
                { "ShopRefreshBase", 3 },
                { "ShopRefreshStep", 2 },
                { "SellPercent", 50 },
                { "CoinsPerGold", 20 },
                { "CoinsPerEnemy", 2 },
                { "CoinsPerFloor", 1 },
                // An expedition starts without coins (the rule before 2026-10-19), so that the tests' coins stay as they were.
                { "ExpeditionStartCoins", 0 },
                { "GoldPerFloor", 2 },
                { "GoldClear", 20 },
                { "HirePrice", 30 },
                { "RecruitCount", 3 },
                { "RosterLimit", 6 },
                { "TreatmentPrice", 20 },
                { "TreatmentRelief", 40 },
                { "ExpPerFloor", 1 },
                { "ExpClear", 5 },
                { "EliteCoinPercent", 200 },
                { "ShopPotionChancePercent", 50 },
                // Slice D stage 32: the shipped values. The test jobs name no stats (all at the baseline), so the arithmetic of the tests stays.
                { "StatBaseline", 10 },
                { "StatMin", 1 },
                { "StatMax", 30 },
                { "StatPowerPercentPerPoint", 10 },
                // 2026-10-20 스탯 개정: in the tests the power and fatigue baselines stay 10 and nothing quickens an item, so the arithmetic stays; a point of
                // constitution gives 0.10 HP in every band, so the test jobs' HP are their constitution's (the tank 1.00 at 10, the healer 0.60 at 6, the striker 0.80 at 8).
                { "PowerBaseline", 10 },
                { "FatigueBaseline", 10 },
                { "CooldownPercentPerPoint", 0 },
                { "HpPerConPointLow", 10 },
                { "HpPerConPointMid", 10 },
                { "HpPerConPointHigh", 10 },
                { "HpBandLow", 10 },
                { "HpBandMid", 20 },
                // Slice D stage 33: every hit lands (a certain hit rolls nothing, so the tests' damage streams stay as they were); the rest as shipped.
                { "HitBasePercent", 100 },
                { "HitMinPercent", 5 },
                { "HitMaxPercent", 100 },
                { "HitPerDexPoint", 100 },
                { "DodgePerDexPoint", 50 },
                { "CritPerDexPoint", 50 },
                { "CritPerWisPoint", 50 },
                { "DogDeathPercentPerEndPoint", 1 },
                { "VirtuePerEndPoint", 1 },
                { "FatiguePercentPerEndPoint", 3 },
                // Slice D stage 34: no spread (a candidate draws nothing more, so the tests' candidates stay as they were); growth tests set 2.
                { "RecruitStatSpread", 0 },
                // No innate traits by default (Slice D stage 37): a candidate draws none and rolls nothing, so the stagecoach draws as before.
                { "RecruitTraitWeight0", 1 },
                { "RecruitTraitWeight1", 0 },
                { "RecruitTraitWeight2", 0 },
                // No build by default (stage 57): the data's builds, if any, are never drawn.
                { "BuildWeightNone", 1 },
                // No acquired traits by default (Slice D stage 38): the settlement rolls nothing, so it settles as before.
                { "TraitChanceClearPercent", 0 },
                { "TraitChanceRetreatPercent", 0 },
                { "TraitChanceDeathsDoorPercent", 0 },
                { "TraitChanceAfflictionPercent", 0 },
                { "TraitChanceVirtuePercent", 0 },
                { "TraitChanceAllyDiedPercent", 0 },
            };
            foreach ((string key, int value) in overrides)
            {
                values[key] = value;
            }

            return values.Select(pair => new BalanceEntry(pair.Key, pair.Value)).ToList();
        }

        /// <param name="rows">Where the owner must stand for the item to work. Everywhere when omitted.</param>
        /// <param name="width">Squares the item takes across on a board, unturned (Slice B stage 19). One when omitted.</param>
        /// <param name="height">Squares it takes down. One when omitted.</param>
        /// <param name="price">What a shop sells it for at Common; 0 (the default) keeps it out of every shop.</param>
        /// <param name="value">The effect's one value (stage 22: no range unless a test builds one); an item carried at power 10 then deals value x 10%.</param>
        public static ItemData Item(
            string id,
            int cooldownMs,
            EffectKind kind,
            TargetMode target,
            int value = 100,
            ItemCategory category = ItemCategory.Weapon,
            RowSpan rows = null,
            int shopWeight = 0,
            ItemEffect second = null,
            int reach = DefaultReach,
            int width = 1,
            int height = 1,
            int price = 0,
            bool melee = false,
            bool unique = false,
            IReadOnlyList<StarSquare> stars = null,
            int starDamage = 0,
            int dropWeight = 0,
            ItemSchool? school = null,
            int critDamagePercent = 0,
            Stat? reqStat = null,
            int reqValue = 0)
        {
            var effects = new List<ItemEffect> { Effect(kind, target, value, reach) };
            if (second != null)
            {
                effects.Add(second);
            }

            // 2026-10-20 스탯 개정: a weapon or an attack item is physical unless a test says Magic; the rest has no school. Stage 42: an item asks for a stat only when a test says.
            return new ItemData(id, Text(id), category, width, height, cooldownMs, rows ?? RowSpan.All, effects, shopWeight, null, price, melee, stars, starDamage, unique: unique, dropWeight: dropWeight,
                school: school ?? DefaultSchool(category), critDamagePercent: critDamagePercent, reqStat: reqStat, reqValue: reqValue);
        }

        /// <summary>The school a test item has unless the test says: physical for a weapon or an attack item, none for the rest.</summary>
        public static ItemSchool? DefaultSchool(ItemCategory category)
        {
            return category == ItemCategory.Weapon || category == ItemCategory.Attack ? ItemSchool.Physical : (ItemSchool?)null;
        }

        /// <summary>Stands for "one enemy" on targets that are counted from an end of the enemy line, and "none" on the others.</summary>
        public const int DefaultReach = -1;

        /// <summary>An item effect with one value (no range). Without a reach it hits one enemy (the front or the rearmost one).</summary>
        public static ItemEffect Effect(EffectKind kind, TargetMode target, int value = 100, int reach = DefaultReach)
        {
            if (reach == DefaultReach)
            {
                reach = ItemEffect.TakesReach(target) ? 1 : 0;
            }

            return new ItemEffect(kind, target, reach, value, value);
        }

        /// <summary>A plain attack: <paramref name="damage"/> to the front enemy, every time (value 100 carried at that power).</summary>
        public static EquippedItem Attack(int cooldownMs, int damage, string id = "attack")
        {
            return new EquippedItem(Item(id, cooldownMs, EffectKind.Damage, TargetMode.EnemyFront), powerPercent: damage);
        }

        /// <summary>A mercenary carrying exactly these items, in this order (no layout: a screen would stack them).</summary>
        public static BattleUnitSetup Mercenary(string id, int row, int hp, params EquippedItem[] items)
        {
            return new BattleUnitSetup
            {
                SourceId = id,
                Name = Text(id),
                Row = row,
                MaxHp = hp,
                Hp = hp,
                Items = items,
                Passive = null,
                HasDog = true,
            };
        }

        public static BattleUnitSetup Enemy(string id, int row, int hp, params EquippedItem[] items)
        {
            return new BattleUnitSetup
            {
                SourceId = id,
                Name = Text(id),
                Row = row,
                MaxHp = hp,
                Hp = hp,
                Items = items,
                Passive = null,
                HasDog = false,
            };
        }

        /// <param name="rows">Where the unit must stand for an InRows condition; null for the other conditions.</param>
        public static BattleUnitSetup WithPassive(
            this BattleUnitSetup unit,
            PassiveTrigger trigger,
            PassiveCondition condition,
            PassiveEffect effect,
            PassiveTarget target,
            int magnitude,
            RowSpan rows = null)
        {
            unit.Passive = new PassiveSpec(trigger, condition, rows, effect, target, magnitude);
            return unit;
        }

        /// <summary>Starts the unit below full HP: current HP stays, max HP becomes <paramref name="maxHp"/>.</summary>
        public static BattleUnitSetup WithMaxHp(this BattleUnitSetup unit, int maxHp)
        {
            unit.MaxHp = maxHp;
            return unit;
        }

        /// <summary>The fatigue states of the test data: an affliction and a virtue of each kind of change.</summary>
        /// <summary>A trait of the tests (Slice D stage 37): innate unless a source makes it acquired (stage 38), weight 10.</summary>
        public static TraitData Trait(string id, TraitPolarity polarity, int hit = 0, int dodge = 0, int crit = 0, int dogDeath = 0, int virtue = 0, int fatigue = 0,
            int maxHp = 0, string excludes = null, int weight = 10, TraitSource? source = null)
        {
            TraitKind kind = source == null ? TraitKind.Innate : TraitKind.Acquired;
            return new TraitData(id, Text(id), kind, polarity, source, weight, excludes, hit, dodge, crit, dogDeath, virtue, fatigue, maxHp);
        }

        /// <summary>
        /// Four innate traits — keen and dull clash — and the acquired ones a candidate never draws (stage 38): one of each source, two from death's
        /// door (scar_t and lucky_t), and grit_t, which clashes with dull. Parts() has none: a test adds them.
        /// </summary>
        public static List<TraitData> Traits()
        {
            return new List<TraitData>
            {
                Trait("keen", TraitPolarity.Positive, hit: 5, excludes: "dull"),
                Trait("dull", TraitPolarity.Negative, hit: -5),
                Trait("sturdy", TraitPolarity.Positive, dogDeath: -5),
                Trait("frail_t", TraitPolarity.Negative, maxHp: -10),
                Trait("scar_t", TraitPolarity.Negative, maxHp: -5, source: TraitSource.DeathsDoor),
                Trait("lucky_t", TraitPolarity.Positive, dogDeath: -10, source: TraitSource.DeathsDoor),
                Trait("vet_t", TraitPolarity.Positive, crit: 2, source: TraitSource.Clear),
                Trait("grit_t", TraitPolarity.Negative, virtue: -10, excludes: "dull", source: TraitSource.Retreat),
                Trait("dread_t", TraitPolarity.Negative, fatigue: 10, source: TraitSource.Affliction),
                Trait("steel_t", TraitPolarity.Positive, virtue: 10, source: TraitSource.Virtue),
            };
        }

        /// <summary>The test data with <see cref="Traits"/>.</summary>
        public static StaticData DataWithTraits(params (string Key, int Value)[] balanceOverrides)
        {
            StaticDataParts parts = Parts(balanceOverrides);
            parts.Traits = Traits();
            return new StaticData(parts);
        }

        public static List<FatigueStateData> FatigueStates()
        {
            return new List<FatigueStateData>
            {
                new FatigueStateData("fearful", Text("fearful"), FatigueStateKind.Affliction, 25, 0, 0, Text("slower")),
                new FatigueStateData("hopeless", Text("hopeless"), FatigueStateKind.Affliction, 0, -50, 0, Text("half healing")),
                new FatigueStateData("reckless", Text("reckless"), FatigueStateKind.Affliction, 0, 0, 25, Text("deadlier door")),
                new FatigueStateData("focused", Text("focused"), FatigueStateKind.Virtue, -20, 0, 0, Text("faster")),
                new FatigueStateData("stalwart", Text("stalwart"), FatigueStateKind.Virtue, 0, 50, -15, Text("hardier")),
            };
        }

        /// <summary>A battle with seed 1, no affinity and no potions. Tests set the fields they need.</summary>
        public static BattleSetup Setup(BalanceData balance, BattleUnitSetup[] party, BattleUnitSetup[] enemies)
        {
            return new BattleSetup
            {
                Seed = 1,
                Balance = balance,
                Party = party,
                Enemies = enemies,
                EnemyCooldownPermille = 0,
                Potions = new PotionData[0],
                FatigueStates = FatigueStates(),
            };
        }

        /// <summary>Starts the unit with this fatigue and, when given, in this state.</summary>
        public static BattleUnitSetup WithFatigue(this BattleUnitSetup unit, int fatigue, FatigueStateData state = null)
        {
            unit.Fatigue = fatigue;
            unit.FatigueState = state;
            return unit;
        }

        public static BattleUnitSetup[] Units(params BattleUnitSetup[] units)
        {
            return units;
        }

        public static PotionData HealPotion(int amount, string id = "heal_potion")
        {
            return new PotionData(id, Text(id), PotionEffect.Heal, amount, 1);
        }

        public static PotionData ShieldPotion(int amount, string id = "shield_potion")
        {
            return new PotionData(id, Text(id), PotionEffect.Shield, amount, 1);
        }

        /// <summary>
        /// A complete, valid data set: one dungeon of two battle floors and a boss, three jobs,
        /// four mercenaries and a few shop items (each priced and weighted; the tonic too). The grunts carry claws, which is what drops. The big items ("pike",
        /// "ballista") are never dropped or stocked; board tests put them on boards directly. Shapes (Slice B stage 19): blade and bow 2x1,
        /// staff 3x1, claw, charm and knife 1x1, pike 3x2, ballista 3x3. The bags: the start "pack" 3x2, a "pouch" 3x1 a shop sells (and an
        /// elite may drop), a "strap" 2x1 only an elite drops.
        /// </summary>
        public static StaticDataParts Parts(params (string Key, int Value)[] balanceOverrides)
        {
            return new StaticDataParts
            {
                Balance = BalanceEntries(balanceOverrides),
                Jobs = new List<JobData>
                {
                    // 2026-10-20 스탯 개정: a job has no HP of its own; the healer's and the striker's constitution give the 0.60 and 0.80 the tests always had.
                    new JobData("tank", Text("tank"), "blade", 1, null),
                    new JobData("healer", Text("healer"), "staff", 2, null, stats: StatBlock.Uniform(10).With(Stat.Con, 6)),
                    new JobData("striker", Text("striker"), "blade", 3, null, stats: StatBlock.Uniform(10).With(Stat.Con, 8)),
                },
                Items = new List<ItemData>
                {
                    // The base weapons deal what the jobs' weapon grade 10 made them deal before stage 22 took the grade out.
                    Item("blade", 2000, EffectKind.Damage, TargetMode.EnemyFront, 10, width: 2),
                    Item("staff", 4000, EffectKind.Heal, TargetMode.AllyLowestHp, 10, category: ItemCategory.Support, width: 3),
                    Item("claw", 3000, EffectKind.Damage, TargetMode.EnemyFront),
                    Item("charm", 5000, EffectKind.Shield, TargetMode.Self, category: ItemCategory.Support, shopWeight: 5, price: 10),
                    Item("knife", 1500, EffectKind.Damage, TargetMode.EnemyFront, 50, shopWeight: 5, price: 10),
                    Item("bow", 3000, EffectKind.Damage, TargetMode.EnemyBack, rows: RowSpan.Back(2), shopWeight: 5, width: 2, price: 10),
                    Item("pike", 3000, EffectKind.Damage, TargetMode.EnemyFront, 120, width: 3, height: 2),
                    Item("ballista", 5000, EffectKind.Damage, TargetMode.EnemyAll, 150, width: 3, height: 3),
                    // A unique item (stage 23): never sold or dropped (no weight, no price), a melee weapon with a star above and below its first square.
                    Item("gladius_t", 2000, EffectKind.Damage, TargetMode.EnemyFront, 30, width: 2, melee: true, unique: true, stars: new[] { new StarSquare(0, -1), new StarSquare(0, 1) }, starDamage: 100),
                },
                Bags = new List<BagData>
                {
                    new BagData("pack", Text("pack"), 3, 2, true, 0, 0, 0),
                    new BagData("pouch", Text("pouch"), 3, 1, false, 6, 5, 1),
                    new BagData("strap", Text("strap"), 2, 1, false, 0, 0, 1),
                },
                Potions = new List<PotionData>
                {
                    new PotionData("tonic", Text("tonic"), PotionEffect.Heal, 40, 5, null, 5),
                },
                Enemies = new List<EnemyData>
                {
                    new EnemyData("grunt", Text("grunt"), 2, 30, new List<ItemGrant> { new ItemGrant("claw", 5) }),
                    new EnemyData("chief", Text("chief"), 9, 120, new List<ItemGrant> { new ItemGrant("claw", 12) }),
                },
                EnemyGroups = new List<EnemyGroupData>
                {
                    new EnemyGroupData("pair", "cave", 1, 2, false, new[] { "grunt", "grunt" }),
                    new EnemyGroupData("trio", "cave", 2, 2, false, new[] { "grunt", "grunt", "grunt" }),
                    new EnemyGroupData("lair", "cave", 0, 0, true, new[] { "chief", "grunt" }),
                    new EnemyGroupData("pit_pair", "pit", 1, 1, false, new[] { "grunt", "grunt" }),
                    new EnemyGroupData("pit_lord", "pit", 0, 0, true, new[] { "chief", "chief" }),
                },
                Affinities = new List<AffinityData>
                {
                    new AffinityData("swift", Text("swift"), -80),
                },
                Dungeons = new List<DungeonData>
                {
                    new DungeonData("cave", Text("cave"), "swift", 2, 2, 3, 2, new List<string> { "tonic" }, mapX: 40, mapY: 30),
                    Pit(),
                },
                // The starting roster, then the recruit pool (Slice C stage 26): names the stagecoach draws candidates from, with a job rolled.
                Mercenaries = new List<MercenaryData>
                {
                    new MercenaryData("anna", Text("anna"), "tank"),
                    new MercenaryData("ben", Text("ben"), "healer"),
                    new MercenaryData("cora", Text("cora"), "striker"),
                    new MercenaryData("dan", Text("dan"), "tank"),
                    new MercenaryData("eve", Text("eve"), null, starting: false),
                    new MercenaryData("finn", Text("finn"), null, starting: false),
                    new MercenaryData("gil", Text("gil"), null, starting: false),
                    new MercenaryData("hana", Text("hana"), null, starting: false),
                    new MercenaryData("ivo", Text("ivo"), null, starting: false),
                },
                FatigueStates = FatigueStates(),
                Recipes = new List<RecipeData>(),
                // Three levels (stage 27): 10 experience to leave the first, 20 the second; +10% and +25% HP.
                Levels = Levels(),
                // The town's buildings (stage 30): two levels each; the second costs 40 gold and 3 days (the inn 2 days) and adds 2 to the roster limit,
                // 1 candidate, 20 relief, 10% gold.
                Buildings = Buildings(),
            };
        }

        public static List<BuildingData> Buildings()
        {
            return new List<BuildingData>
            {
                new BuildingData(BuildingId.Barracks, 1, 0, 0, 0),
                new BuildingData(BuildingId.Barracks, 2, 40, 3, 2),
                new BuildingData(BuildingId.Inn, 1, 0, 0, 0),
                new BuildingData(BuildingId.Inn, 2, 40, 2, 20),
                new BuildingData(BuildingId.Coach, 1, 0, 0, 0),
                new BuildingData(BuildingId.Coach, 2, 40, 3, 1),
            };
        }

        public static List<LevelData> Levels()
        {
            return new List<LevelData>
            {
                new LevelData(1, 10),
                new LevelData(2, 20),
                new LevelData(3, 0),
            };
        }

        /// <summary>
        /// The blade at a value that wins every battle of the test dungeon (the flow kits' strong party: before stage 22, a weapon grade of
        /// 200 did it).
        /// </summary>
        public static void StrongBlade(StaticDataParts parts, int value = 200)
        {
            parts.Items = parts.Items.Select(item => item.Id == "blade" ? Item("blade", 2000, EffectKind.Damage, TargetMode.EnemyFront, value, width: 2) : item).ToList();
        }

        /// <summary>
        /// The final dungeon of the test data (Slice C stage 25): one floor and its boss, open after one clear of the cave. Its groups
        /// (pit_pair, pit_lord) are in Parts(), so a test that replaces the dungeon list keeps this one or the groups point nowhere.
        /// </summary>
        public static DungeonData Pit()
        {
            return new DungeonData("pit", Text("pit"), "swift", 1, 1, 1, 1, new List<string> { "tonic" }, isFinal: true, unlockDungeonId: "cave", unlockClears: 1, mapX: 55, mapY: 10);
        }

        /// <summary>
        /// The story (Slice D stage 55) added to the parts: a sequence "tale" of a scene with two lines and the lobby's banner, a speaker "clerk"
        /// with two faces, a dialogue at the run's start (two lines) and one when the pit opens (one line).
        /// </summary>
        public static StaticDataParts WithStory(StaticDataParts parts)
        {
            parts.StoryScenes = new List<StorySceneData>
            {
                new StorySceneData("tale_one", "tale", 10, "story/tale/one", 4000, 2000, new List<int> { 2 }, new StoryCamera(500, 500, 100), new StoryCamera(400, 400, 120),
                    StoryEffect.None, StoryTransition.Cross, false, new List<LocalizedText> { Text("one"), Text("“two”") }, null),
                new StorySceneData("tale_two", "tale", 20, "story/tale/two", 3000, 0, new List<int>(), new StoryCamera(500, 500, 100), new StoryCamera(500, 500, 100),
                    StoryEffect.Heartbeat, StoryTransition.Black, false, new List<LocalizedText> { Text("three") }, null),
                new StorySceneData("tale_end", "tale", 30, null, 5000, 4000, new List<int> { 1 }, null, null,
                    StoryEffect.None, StoryTransition.Cross, true, new List<LocalizedText> { Text("end") }, null),
            };
            parts.Npcs = new List<NpcData>
            {
                new NpcData("clerk", Text("clerk"), "unit/npc/clerk", new List<NpcPortrait> { new NpcPortrait("calm", "portrait/npc/clerk-calm"), new NpcPortrait("smile", "portrait/npc/clerk-smile") }),
            };
            parts.Dialogues = new List<DialogueData>
            {
                new DialogueData("pit_open", 20, DialogueTrigger.DungeonUnlocked, "pit"),
                new DialogueData("hello", 10, DialogueTrigger.RunStart, null),
            };
            parts.DialogueLines = new List<DialogueLineData>
            {
                new DialogueLineData("hello_2", "hello", 20, "clerk", "smile", Text("hello two")),
                new DialogueLineData("hello_1", "hello", 10, "clerk", "calm", Text("hello one")),
                new DialogueLineData("pit_open_1", "pit_open", 10, "clerk", "calm", Text("the pit")),
            };
            return parts;
        }

        public static StaticData Data(params (string Key, int Value)[] balanceOverrides)
        {
            return new StaticData(Parts(balanceOverrides));
        }
    }
}
