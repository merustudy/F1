using System.Collections.Generic;
using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Each definition type enforces its own rules in its constructor.</summary>
    public sealed class DefinitionTests
    {
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.Front, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.AllyAll)]
        [TestCase(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.SelfInDog, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.Rear, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self)]
        public void PassiveSpec_AcceptsImplementedCombinations(PassiveTrigger trigger, PassiveCondition condition, PassiveEffect effect, PassiveTarget target)
        {
            Assert.DoesNotThrow(() => new PassiveSpec(trigger, condition, effect, target, 5));
        }

        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.BattleStart, PassiveCondition.SelfInDog, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self)]
        [TestCase(PassiveTrigger.Always, PassiveCondition.Rear, PassiveEffect.WeaponPowerPercent, PassiveTarget.AllyAll)]
        public void PassiveSpec_RejectsCombinationsTheEngineDoesNotImplement(PassiveTrigger trigger, PassiveCondition condition, PassiveEffect effect, PassiveTarget target)
        {
            Assert.Throws<DataException>(() => new PassiveSpec(trigger, condition, effect, target, 5));
        }

        [Test]
        public void PassiveSpec_RejectsNonPositiveMagnitude()
        {
            Assert.Throws<DataException>(() => new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget, 0));
        }

        [TestCase(EffectKind.Damage, TargetMode.EnemyFront, true)]
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
                Assert.DoesNotThrow(() => new ItemEffect(kind, target, 100));
            }
            else
            {
                Assert.Throws<DataException>(() => new ItemEffect(kind, target, 100));
            }
        }

        [Test]
        public void ItemData_RejectsBadCooldownEffectCountAndWeight()
        {
            var effect = new ItemEffect(EffectKind.Damage, TargetMode.EnemyFront, 100);
            LocalizedText name = TestData.Text("x");

            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 99, RowRequirement.Any, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, RowRequirement.Any, new ItemEffect[0], 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, RowRequirement.Any, new[] { effect, effect, effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1000, RowRequirement.Any, new[] { effect }, -1));
            Assert.Throws<DataException>(() => new ItemData("x", null, ItemCategory.Weapon, 1000, RowRequirement.Any, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("Bad Id", name, ItemCategory.Weapon, 1000, RowRequirement.Any, new[] { effect }, 0));
        }

        [Test]
        public void JobData_RejectsBadStats()
        {
            LocalizedText name = TestData.Text("x");

            Assert.Throws<DataException>(() => new JobData("x", name, 0, 3, "sword", 10, BattleRow.Front, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 0, "sword", 10, BattleRow.Front, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, JobData.MaxItemSlots + 1, "sword", 10, BattleRow.Front, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 0, BattleRow.Front, null));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "Sword", 10, BattleRow.Front, null));
        }

        [Test]
        public void EnemyGroupData_BossGroupsHaveFloorZero_OthersHaveARange()
        {
            var one = new List<string> { "rat" };
            var none = new List<string>();

            Assert.DoesNotThrow(() => new EnemyGroupData("g", "d", 0, 0, true, one, none));
            Assert.DoesNotThrow(() => new EnemyGroupData("g", "d", 1, 3, false, none, one));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 1, 1, true, one, none));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 0, 0, false, one, none));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 3, 2, false, one, none));
            Assert.Throws<DataException>(() => new EnemyGroupData("g", "d", 1, 1, false, none, none));
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
            Assert.Throws<DataException>(() => TestData.Balance(("RowCapacity", 1), ("PartySize", 3)));
            Assert.DoesNotThrow(() => TestData.Balance(("RowCapacity", 2), ("PartySize", 4), ("MinPartySize", 4)));
        }

        [Test]
        public void ItemEffect_MagnitudeAt_IsNeverBelowOne()
        {
            var effect = new ItemEffect(EffectKind.Damage, TargetMode.EnemyFront, 25);

            Assert.AreEqual(1, effect.MagnitudeAt(1));
            Assert.AreEqual(1, effect.MagnitudeAt(4));
            Assert.AreEqual(2, effect.MagnitudeAt(8));
        }
    }
}
