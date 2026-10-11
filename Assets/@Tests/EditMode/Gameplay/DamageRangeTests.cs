using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Slice B stage 22 (Docs/Design/02_Combat_System.md §4·§5, 04 §3): damage is a range rolled once an attack, a weapon's critical hit is
    /// rolled for each unit hit and tires or relieves, damage under the hit minimum is no hit, a burn stack deals the data's share.
    /// All numbers are in hundredths.
    /// </summary>
    public sealed class DamageRangeTests
    {
        static EquippedItem Weapon(int min, int max, int critPercent = 0, TargetMode target = TargetMode.EnemyFront, int cooldownMs = 1000)
        {
            int reach = ItemEffect.TakesReach(target) ? 1 : 0;
            var effect = new ItemEffect(EffectKind.Damage, target, reach, min, max);
            return new EquippedItem(new ItemData("w", TestData.Text("w"), ItemCategory.Weapon, 1, 1, cooldownMs, RowSpan.All, new[] { effect }, 0, critChancePercent: critPercent));
        }

        static BattleEngine Battle(BalanceData balance, BattleUnitSetup[] party, BattleUnitSetup[] enemies, ulong seed = 1)
        {
            BattleSetup setup = TestData.Setup(balance, party, enemies);
            setup.Seed = seed;
            return new BattleEngine(setup);
        }

        static List<int> DamageTaken(BattleEngine battle, BattleSide side)
        {
            return battle.Events.Where(e => e.Kind == BattleEventKind.Damaged && e.Target.Side == side).Select(e => e.A).ToList();
        }

        [Test]
        public void Damage_IsRolledFromTheLeastToTheMost_TheSameForTheSameSeed()
        {
            BattleEngine first = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(200, 400))), TestData.Units(TestData.Enemy("e", 1, 100000)));
            first.AdvanceTo(30000);
            List<int> rolls = DamageTaken(first, BattleSide.Enemy);

            Assert.AreEqual(30, rolls.Count);
            Assert.IsTrue(rolls.All(r => r >= 200 && r <= 400), "2.00 to 4.00.");
            Assert.Greater(rolls.Distinct().Count(), 10, "Rolled in hundredths, not one value.");

            BattleEngine again = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(200, 400))), TestData.Units(TestData.Enemy("e", 1, 100000)));
            again.AdvanceTo(30000);
            CollectionAssert.AreEqual(rolls, DamageTaken(again, BattleSide.Enemy), "The same seed rolls the same.");

            BattleEngine other = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(200, 400))), TestData.Units(TestData.Enemy("e", 1, 100000)), seed: 2);
            other.AdvanceTo(30000);
            CollectionAssert.AreNotEqual(rolls, DamageTaken(other, BattleSide.Enemy));
        }

        [Test]
        public void AWeaponHittingSeveral_RollsOnceAnAttack_ButACriticalHitForEachUnit()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(200, 400, critPercent: 50, target: TargetMode.EnemyAll))),
                TestData.Units(TestData.Enemy("e1", 1, 100000), TestData.Enemy("e2", 2, 100000), TestData.Enemy("e3", 3, 100000)));
            battle.AdvanceTo(40000);

            bool mixed = false;
            foreach (IGrouping<int, BattleEvent> attack in battle.Events.Where(e => e.Kind == BattleEventKind.Damaged).GroupBy(e => e.TimeMs))
            {
                List<int> amounts = attack.Select(e => e.A).ToList();
                int roll = amounts.Min();
                Assert.IsTrue(amounts.All(a => a == roll || a == roll * 150 / 100), "One roll for the attack; a critical hit is 150% of it.");
                mixed |= amounts.Contains(roll) && amounts.Any(a => a != roll);
            }

            Assert.IsTrue(mixed, "In one attack some units took a critical hit and others did not.");
        }

        [Test]
        public void ACriticalHit_IsTheRollWithItsStars_TimesCritDamagePercent_ThenThePassive()
        {
            BattleUnitSetup merc = TestData.Mercenary("a", 1, 100000, Weapon(200, 200, critPercent: 100))
                .WithPassive(PassiveTrigger.Always, PassiveCondition.None, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 100);
            merc.StarDamage = new[] { 100 };
            BattleEngine battle = Battle(TestData.Balance(), TestData.Units(merc), TestData.Units(TestData.Enemy("e", 1, 100000)));
            battle.AdvanceTo(1000);

            BattleEvent critical = battle.Events.Single(e => e.Kind == BattleEventKind.CriticalHit);
            BattleEvent damaged = battle.Events.Single(e => e.Kind == BattleEventKind.Damaged);
            Assert.AreEqual(900, damaged.A, "(2.00 + the star's 1.00) x 150% = 4.50, doubled by the passive.");
            Assert.AreEqual(900, critical.A);
            List<BattleEvent> events = battle.Events.ToList();
            Assert.AreEqual(events.IndexOf(critical) + 1, events.IndexOf(damaged), "The critical hit is told just before its damage.");
            Assert.AreEqual(100, critical.C, "The weapon's chance.");
        }

        [Test]
        public void ACriticalHit_TiresTheMercenaryHitAndItsAllies_AndRelievesTheOneWhoDealtOneAndItsAllies()
        {
            BalanceData balance = TestData.Balance(("CritFatigueTaken", 9), ("CritFatigueTakenAllies", 2), ("CritFatigueDealt", 3), ("CritFatigueDealtAllies", 1));

            // An enemy's critical hit on the front mercenary.
            BattleEngine taken = Battle(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100000).WithFatigue(50), TestData.Mercenary("b", 2, 100000).WithFatigue(50)),
                TestData.Units(TestData.Enemy("e", 1, 100000, Weapon(100, 100, critPercent: 100))));
            taken.AdvanceTo(1000);
            Assert.AreEqual(59, taken.Party[0].Fatigue, "The one hit: +9.");
            Assert.AreEqual(52, taken.Party[1].Fatigue, "An ally: +2.");
            Assert.IsTrue(taken.Events.Any(e => e.Kind == BattleEventKind.FatigueChanged && e.Id == BattleEvent.FatigueCritTaken && e.A == 9));

            // A mercenary's critical hit on an enemy.
            BattleEngine dealt = Battle(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(100, 100, critPercent: 100)).WithFatigue(50), TestData.Mercenary("b", 2, 100000).WithFatigue(50)),
                TestData.Units(TestData.Enemy("e", 1, 100000)));
            dealt.AdvanceTo(1000);
            Assert.AreEqual(47, dealt.Party[0].Fatigue, "The one who dealt it: -3.");
            Assert.AreEqual(49, dealt.Party[1].Fatigue, "An ally: -1.");
        }

        [Test]
        public void AWeaponWithoutACritChance_RollsNoCriticalHit()
        {
            BattleEngine battle = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100000, Weapon(200, 400))), TestData.Units(TestData.Enemy("e", 1, 100000)));
            battle.AdvanceTo(20000);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.CriticalHit));
        }

        [Test]
        public void DamageThroughAShield_UnderHitMinDamage_IsNoHit_UnlessItTakesTheHpToZero()
        {
            BalanceData balance = TestData.Balance(("HitMinDamage", 50), ("FatigueOnHit", 2));
            BattleUnitSetup merc = TestData.Mercenary("a", 1, 1000)
                .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self, 100);

            // 1.20 on a shield of 1.00: 0.20 gets through, under 0.50. The HP goes down; it tires nobody.
            BattleEngine sliver = Battle(balance, TestData.Units(merc), TestData.Units(TestData.Enemy("e", 1, 100000, Weapon(120, 120))));
            sliver.AdvanceTo(1000);
            Assert.AreEqual(980, sliver.Party[0].Hp);
            Assert.AreEqual(0, sliver.Party[0].Fatigue, "No hit.");

            // The next 1.20 meets no shield: a hit.
            sliver.AdvanceTo(2000);
            Assert.AreEqual(860, sliver.Party[0].Hp);
            Assert.AreEqual(2, sliver.Party[0].Fatigue, "A hit tires.");

            // 0.30 that takes the last 0.30 of HP is a hit: death's door.
            BattleEngine last = Battle(balance, TestData.Units(TestData.Mercenary("b", 1, 30)), TestData.Units(TestData.Enemy("e", 1, 100000, Weapon(30, 30))));
            last.AdvanceTo(1000);
            Assert.IsTrue(last.Party[0].InDog, "At 0 HP: death's door.");
        }

        [Test]
        public void ABurnTick_DealsItsStacksTimesBurnDamagePerStack()
        {
            BalanceData balance = TestData.Balance(("BurnDamagePerStack", 27));
            var burn = new EquippedItem(TestData.Item("flask", 100000, EffectKind.Burn, TargetMode.EnemyFront, 3, category: ItemCategory.Attack));
            BattleUnitSetup merc = TestData.Mercenary("a", 1, 1000, burn);
            BattleEngine battle = Battle(balance, TestData.Units(merc), TestData.Units(TestData.Enemy("e", 1, 1000)));
            battle.AdvanceTo(100000);
            battle.AdvanceTo(104000);

            List<int> ticks = battle.Events.Where(e => e.Kind == BattleEventKind.Damaged && e.Id == BattleEvent.CauseBurn).Select(e => e.A).ToList();
            CollectionAssert.AreEqual(new[] { 81, 54, 27 }, ticks, "Three stacks: 0.81, 0.54, 0.27.");
        }
    }
}
