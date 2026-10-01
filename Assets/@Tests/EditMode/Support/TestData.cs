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
                { "MaxFatigue", 100 },
                { "FatigueRecoveryPerDay", 10 },
                { "RestDays", 1 },
                { "DogGraceMs", 3000 },
                { "DogGraceBreakHits", 3 },
                { "DogDeathChancePercent", 100 },
                { "BurnTickMs", 1000 },
                { "StormStartMs", 600000 },
                { "StormTickMs", 1000 },
                { "StormBaseDamage", 5 },
                { "StormGrowth", 5 },
                { "PotionSlots", 3 },
                { "PotionCooldownMs", 1500 },
                { "RetreatChancePercent", 100 },
                { "RetreatCooldownMs", 5000 },
                { "PostBattleHealPercent", 10 },
                { "MinCooldownMs", 200 },
                { "RewardChoices", 3 },
                { "MapBranchChancePercent", 50 },
                { "FinalBossLevel", 14 },
            };
            foreach ((string key, int value) in overrides)
            {
                values[key] = value;
            }

            return values.Select(pair => new BalanceEntry(pair.Key, pair.Value)).ToList();
        }

        public static ItemData Item(
            string id,
            int cooldownMs,
            EffectKind kind,
            TargetMode target,
            int powerPercent = 100,
            ItemCategory category = ItemCategory.Weapon,
            int[] rows = null,
            int rewardWeight = 0,
            ItemEffect second = null,
            int reach = DefaultReach)
        {
            var effects = new List<ItemEffect> { Effect(kind, target, powerPercent, reach) };
            if (second != null)
            {
                effects.Add(second);
            }

            return new ItemData(id, Text(id), category, cooldownMs, rows ?? AllRows, effects, rewardWeight);
        }

        /// <summary>Every battle row: what an item without a row limit lists.</summary>
        public static readonly int[] AllRows = Enumerable.Range(BattleRows.Front, BattleRows.Count).ToArray();

        /// <summary>Stands for "one enemy" on targets that are counted from an end of the enemy line, and "none" on the others.</summary>
        public const int DefaultReach = -1;

        /// <summary>An item effect. Without a reach it hits one enemy (the front or the rearmost one).</summary>
        public static ItemEffect Effect(EffectKind kind, TargetMode target, int powerPercent = 100, int reach = DefaultReach)
        {
            if (reach == DefaultReach)
            {
                reach = ItemEffect.TakesReach(target) ? 1 : 0;
            }

            return new ItemEffect(kind, target, reach, powerPercent);
        }

        /// <summary>A plain attack: grade x 100% damage to the front enemy.</summary>
        public static EquippedItem Attack(int cooldownMs, int damage, string id = "attack")
        {
            return new EquippedItem(Item(id, cooldownMs, EffectKind.Damage, TargetMode.EnemyFront), damage);
        }

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

        public static BattleUnitSetup WithPassive(
            this BattleUnitSetup unit,
            PassiveTrigger trigger,
            PassiveCondition condition,
            PassiveEffect effect,
            PassiveTarget target,
            int magnitude,
            params int[] rows)
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
            };
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
        /// three mercenaries and a few reward items.
        /// </summary>
        public static StaticDataParts Parts(params (string Key, int Value)[] balanceOverrides)
        {
            return new StaticDataParts
            {
                Balance = BalanceEntries(balanceOverrides),
                Jobs = new List<JobData>
                {
                    new JobData("tank", Text("tank"), 100, 3, "blade", 10, 1, null),
                    new JobData("healer", Text("healer"), 60, 3, "staff", 10, 2, null),
                    new JobData("striker", Text("striker"), 80, 2, "blade", 12, 3, null),
                },
                Items = new List<ItemData>
                {
                    Item("blade", 2000, EffectKind.Damage, TargetMode.EnemyFront),
                    Item("staff", 4000, EffectKind.Heal, TargetMode.AllyLowestHp, category: ItemCategory.Support),
                    Item("claw", 3000, EffectKind.Damage, TargetMode.EnemyFront),
                    Item("charm", 5000, EffectKind.Shield, TargetMode.Self, category: ItemCategory.Support, rewardWeight: 5),
                    Item("knife", 1500, EffectKind.Damage, TargetMode.EnemyFront, 50, rewardWeight: 5),
                    Item("bow", 3000, EffectKind.Damage, TargetMode.EnemyBack, rows: new[] { 2, 3 }, rewardWeight: 5),
                },
                Potions = new List<PotionData>
                {
                    new PotionData("tonic", Text("tonic"), PotionEffect.Heal, 40, 5),
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
                },
                Affinities = new List<AffinityData>
                {
                    new AffinityData("swift", Text("swift"), -80),
                },
                Dungeons = new List<DungeonData>
                {
                    new DungeonData("cave", Text("cave"), "swift", 2, 2, 3, 30, 2, 8, 2, new List<string> { "tonic" }),
                },
                Mercenaries = new List<MercenaryData>
                {
                    new MercenaryData("anna", Text("anna"), "tank"),
                    new MercenaryData("ben", Text("ben"), "healer"),
                    new MercenaryData("cora", Text("cora"), "striker"),
                    new MercenaryData("dan", Text("dan"), "tank"),
                },
            };
        }

        public static StaticData Data(params (string Key, int Value)[] balanceOverrides)
        {
            return new StaticData(Parts(balanceOverrides));
        }
    }
}
