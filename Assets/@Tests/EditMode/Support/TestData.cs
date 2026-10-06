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
                { "InventoryCells", 10 },
                { "CampHealPercent", 30 },
                { "CampFatigueRelief", 20 },
                { "TierSilverPercent", 200 },
                { "TierGoldPercent", 300 },
                { "TierDiamondPercent", 400 },
                { "MapBranchChancePercent", 50 },
                { "FinalBossLevel", 14 },
            };
            foreach ((string key, int value) in overrides)
            {
                values[key] = value;
            }

            return values.Select(pair => new BalanceEntry(pair.Key, pair.Value)).ToList();
        }

        /// <param name="rows">Where the owner must stand for the item to work. Everywhere when omitted.</param>
        /// <param name="size">Cells the item takes on a board. One when omitted.</param>
        public static ItemData Item(
            string id,
            int cooldownMs,
            EffectKind kind,
            TargetMode target,
            int powerPercent = 100,
            ItemCategory category = ItemCategory.Weapon,
            RowSpan rows = null,
            int rewardWeight = 0,
            ItemEffect second = null,
            int reach = DefaultReach,
            int size = 1)
        {
            var effects = new List<ItemEffect> { Effect(kind, target, powerPercent, reach) };
            if (second != null)
            {
                effects.Add(second);
            }

            return new ItemData(id, Text(id), category, size, cooldownMs, rows ?? RowSpan.All, effects, rewardWeight);
        }

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

        /// <summary>A mercenary whose board is exactly its items: no empty cells.</summary>
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
                ItemSlots = Cells(items),
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
                ItemSlots = Cells(items),
                Passive = null,
                HasDog = false,
            };
        }

        /// <summary>The cells the items take. A null entry (which only a test of the setup validation passes) takes none.</summary>
        static int Cells(EquippedItem[] items)
        {
            return items.Where(item => item != null).Sum(item => item.Item.Size);
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
        /// four mercenaries and a few reward items. The big items ("pike", "ballista") are never
        /// offered as rewards; board tests put them on boards directly.
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
                    Item("bow", 3000, EffectKind.Damage, TargetMode.EnemyBack, rows: RowSpan.Back(2), rewardWeight: 5),
                    Item("pike", 3000, EffectKind.Damage, TargetMode.EnemyFront, 120, size: 2),
                    Item("ballista", 5000, EffectKind.Damage, TargetMode.EnemyAll, 150, size: 3),
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
                    new DungeonData("cave", Text("cave"), "swift", 2, 2, 3, 2, 8, 2, new List<string> { "tonic" }),
                },
                Mercenaries = new List<MercenaryData>
                {
                    new MercenaryData("anna", Text("anna"), "tank"),
                    new MercenaryData("ben", Text("ben"), "healer"),
                    new MercenaryData("cora", Text("cora"), "striker"),
                    new MercenaryData("dan", Text("dan"), "tank"),
                },
                FatigueStates = FatigueStates(),
            };
        }

        public static StaticData Data(params (string Key, int Value)[] balanceOverrides)
        {
            return new StaticData(Parts(balanceOverrides));
        }
    }
}
