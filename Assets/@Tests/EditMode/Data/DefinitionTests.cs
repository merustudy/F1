using System.Collections.Generic;
using System.Linq;
using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Each definition type enforces its own rules in its constructor.</summary>
    public sealed class DefinitionTests
    {
        /// <summary>The rows an InRows condition needs; none for the other conditions.</summary>
        static int[] RowsFor(PassiveCondition condition)
        {
            return condition == PassiveCondition.InRows ? new[] { 1 } : new int[0];
        }

        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.InRows, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.AllyAll)]
        [TestCase(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.SelfInDog, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.InRows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self)]
        public void PassiveSpec_AcceptsImplementedCombinations(PassiveTrigger trigger, PassiveCondition condition, PassiveEffect effect, PassiveTarget target)
        {
            Assert.DoesNotThrow(() => new PassiveSpec(trigger, condition, RowsFor(condition), effect, target, 5));
        }

        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.SelfInDog, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.InRows, PassiveEffect.WeaponPowerPercent, PassiveTarget.AllyAll)]
        public void PassiveSpec_RejectsCombinationsTheEngineDoesNotImplement(PassiveTrigger trigger, PassiveCondition condition, PassiveEffect effect, PassiveTarget target)
        {
            Assert.Throws<DataException>(() => new PassiveSpec(trigger, condition, RowsFor(condition), effect, target, 5));
        }

        [Test]
        public void PassiveSpec_RejectsNonPositiveMagnitude()
        {
            Assert.Throws<DataException>(() => new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, null, PassiveEffect.Shield, PassiveTarget.EventTarget, 0));
        }

        [Test]
        public void PassiveSpec_InRowsNeedsAValidSetOfRows_OtherConditionsTakeNone()
        {
            PassiveSpec InRows(params int[] rows)
            {
                return new PassiveSpec(PassiveTrigger.Always, PassiveCondition.InRows, rows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 5);
            }

            CollectionAssert.AreEqual(new[] { 2, 3 }, InRows(2, 3).Rows);
            Assert.Throws<DataException>(() => InRows(), "No rows.");
            Assert.Throws<DataException>(() => InRows(0), "Below row 1.");
            Assert.Throws<DataException>(() => InRows(BattleRows.Count + 1), "Beyond the last row.");
            Assert.Throws<DataException>(() => InRows(2, 1), "Not ascending.");
            Assert.Throws<DataException>(() => InRows(1, 1), "Repeated.");

            Assert.Throws<DataException>(
                () => new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, new[] { 1 }, PassiveEffect.Shield, PassiveTarget.EventTarget, 5),
                "Rows without the InRows condition.");
            CollectionAssert.IsEmpty(new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, null, PassiveEffect.Shield, PassiveTarget.EventTarget, 5).Rows);
        }

        [TestCase(EffectKind.Damage, TargetMode.EnemyFront, true)]
        [TestCase(EffectKind.Damage, TargetMode.EnemyBack, true)]
        [TestCase(EffectKind.Burn, TargetMode.EnemyBack, true)]
        [TestCase(EffectKind.Shield, TargetMode.EnemyBack, false)]
        [TestCase(EffectKind.Burn, TargetMode.EnemyAll, true)]
        [TestCase(EffectKind.Heal, TargetMode.AllyLowestHp, true)]
        [TestCase(EffectKind.Shield, TargetMode.Self, true)]
        [TestCase(EffectKind.Damage, TargetMode.Self, false)]
        [TestCase(EffectKind.Burn, TargetMode.AllyAll, false)]
        [TestCase(EffectKind.Heal, TargetMode.EnemyFront, false)]
        [TestCase(EffectKind.Shield, TargetMode.EnemyAll, false)]
        public void ItemEffect_HarmfulEffectsTargetEnemies_HelpfulEffectsTargetAllies(EffectKind kind, TargetMode target, bool valid)
        {
            if (valid)
            {
                Assert.DoesNotThrow(() => TestData.Effect(kind, target));
            }
            else
            {
                Assert.Throws<DataException>(() => TestData.Effect(kind, target));
            }
        }

        [Test]
        public void ItemEffect_TargetsCountedFromAnEndNeedAReach_OthersTakeNone()
        {
            foreach (TargetMode target in new[] { TargetMode.EnemyFront, TargetMode.EnemyBack })
            {
                Assert.AreEqual(1, new ItemEffect(EffectKind.Damage, target, 1, 100).Reach);
                Assert.AreEqual(BattleRows.Count, new ItemEffect(EffectKind.Damage, target, BattleRows.Count, 100).Reach);
                Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Damage, target, 0, 100), "No reach.");
                Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Damage, target, BattleRows.Count + 1, 100), "Deeper than a side has rows.");
            }

            Assert.AreEqual(0, new ItemEffect(EffectKind.Damage, TargetMode.EnemyAll, 0, 100).Reach);
            Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Damage, TargetMode.EnemyAll, 2, 100));
            Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Heal, TargetMode.AllyLowestHp, 1, 100));
            Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Shield, TargetMode.Self, 1, 100));
        }

        [Test]
        public void ItemData_RejectsBadCooldownEffectCountAndWeight()
        {
            ItemEffect effect = TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront);
            LocalizedText name = TestData.Text("x");

            int[] rows = TestData.AllRows;

            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 99, rows, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, rows, new ItemEffect[0], 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, rows, new[] { effect, effect, effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, rows, new[] { effect }, -1));
            Assert.Throws<DataException>(() => new ItemData("x", null, ItemCategory.Weapon, 1000, rows, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("Bad Id", name, ItemCategory.Weapon, 1000, rows, new[] { effect }, 0));
        }

        [Test]
        public void ItemData_RowsAreAnAscendingSetOfValidRows_AndSayWhereTheItemWorks()
        {
            ItemEffect effect = TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront);
            LocalizedText name = TestData.Text("x");
            ItemData Item(params int[] rows)
            {
                return new ItemData("x", name, ItemCategory.Weapon, 1000, rows, new[] { effect }, 0);
            }

            ItemData melee = Item(1, 2);
            Assert.IsTrue(melee.UsableIn(1));
            Assert.IsTrue(melee.UsableIn(2));
            Assert.IsFalse(melee.UsableIn(3));

            Assert.Throws<DataException>(() => Item(), "No rows.");
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, null, new[] { effect }, 0), "Rows missing.");
            Assert.Throws<DataException>(() => Item(0));
            Assert.Throws<DataException>(() => Item(BattleRows.Count + 1));
            Assert.Throws<DataException>(() => Item(3, 2), "Not ascending.");
            Assert.Throws<DataException>(() => Item(2, 2), "Repeated.");
        }

        [Test]
        public void JobData_RejectsBadStats()
        {
            LocalizedText name = TestData.Text("x");

            Assert.Throws<DataException>(() => new JobData("x", name, 0, 3, "sword", 10, 1, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 0, "sword", 10, 1, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, JobData.MaxItemSlots + 1, "sword", 10, 1, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 0, 1, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "Sword", 10, 1, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 0, null), "Recommended row below 1.");
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, BattleRows.Count + 1, null), "Recommended row beyond the last row.");
        }

        [Test]
        public void JobData_APassiveAndItsTextComeTogether()
        {
            LocalizedText name = TestData.Text("x");
            var passive = new PassiveSpec(PassiveTrigger.BattleStart, PassiveCondition.InRows, new[] { 1 }, PassiveEffect.Shield, PassiveTarget.Self, 20);

            Assert.DoesNotThrow(() => new JobData("x", name, 100, 3, "sword", 10, 1, null));
            Assert.DoesNotThrow(() => new JobData("x", name, 100, 3, "sword", 10, 1, passive, TestData.Text("Shield {0}")));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, passive));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, null, TestData.Text("Shield {0}")));
        }

        [Test]
        public void EnemyGroupData_BossGroupsHaveFloorZero_OthersHaveARange()
        {
            string[] one = { "rat" };

            Assert.DoesNotThrow(() => new EnemyGroupData("g", "d", 0, 0, true, one));
            Assert.DoesNotThrow(() => new EnemyGroupData("g", "d", 1, 3, false, one));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 1, 1, true, one));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 0, 0, false, one));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 3, 2, false, one));
        }

        [Test]
        public void EnemyGroupData_ListsItsEnemiesFromTheFront_OnePerRow()
        {
            EnemyGroupData Group(params string[] enemies)
            {
                return new EnemyGroupData("g", "d", 1, 1, false, enemies);
            }

            CollectionAssert.AreEqual(new[] { "rat", "bat", "rat" }, Group("rat", "bat", "rat").Enemies);
            Assert.AreEqual(BattleRows.Count, Group(Enumerable.Repeat("rat", BattleRows.Count).ToArray()).Enemies.Count, "A full line.");

            Assert.Throws<DataException>(() => Group(), "Nobody.");
            Assert.Throws<DataException>(() => Group(Enumerable.Repeat("rat", BattleRows.Count + 1).ToArray()), "More enemies than rows.");
            Assert.Throws<DataException>(() => Group("Bad Id"));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 1, 1, false, null));
        }

        [Test]
        public void DungeonData_RejectsBadShapeAndCosts()
        {
            LocalizedText name = TestData.Text("x");
            var potions = new List<string>();

            Assert.DoesNotThrow(() => new DungeonData("d", name, "swift", 3, 2, 3, 30, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 0, 2, 3, 30, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 3, 2, 30, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, DungeonData.MaxMapWidth + 1, 30, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, -1, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 30, 0, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 30, 2, 0, 2, potions));
        }

        [Test]
        public void DungeonData_RewardGradeGrowsByFloor()
        {
            var dungeon = new DungeonData("d", TestData.Text("d"), "swift", 3, 2, 3, 30, 2, 8, 2, new List<string>());

            Assert.AreEqual(8, dungeon.RewardGradeAt(1));
            Assert.AreEqual(12, dungeon.RewardGradeAt(3));
        }

        [Test]
        public void BalanceData_RejectsInconsistentPartySizes()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("MinPartySize", 4), ("PartySize", 3)));
            Assert.DoesNotThrow(() => TestData.Balance(("PartySize", BattleRows.Count), ("MinPartySize", BattleRows.Count)));
            Assert.Throws<DataException>(() => TestData.Balance(("PartySize", BattleRows.Count + 1)), "One mercenary per row: no more than there are rows.");
        }

        [Test]
        public void ItemEffect_MagnitudeAt_IsNeverBelowOne()
        {
            ItemEffect effect = TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 25);

            Assert.AreEqual(1, effect.MagnitudeAt(1));
            Assert.AreEqual(1, effect.MagnitudeAt(4));
            Assert.AreEqual(2, effect.MagnitudeAt(8));
        }
    }
}
