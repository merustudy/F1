using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The owner's stat in battle (Slice D stage 32, Docs/Design/02_Combat_System.md §4): an item that grows with a stat deals, heals, shields and
    /// burns by its owner's value of it — the damage after the critical hit and before the weapon-power passive, a burn's stacks rounded. Enemies
    /// and items without an axis stay as they are. One rule per test.
    /// </summary>
    public sealed class StatBattleTests
    {
        static readonly BalanceData Balance = TestData.Balance();

        static BattleUnitSetup Owner(StatBlock stats, int hp, params EquippedItem[] items)
        {
            BattleUnitSetup unit = TestData.Mercenary("owner", 1, hp, items);
            unit.Stats = stats;
            return unit;
        }

        static StatBlock With(Stat stat, int value)
        {
            return StatBlock.Uniform(10).With(stat, value);
        }

        static List<BattleEvent> Run(BattleUnitSetup[] party, BattleUnitSetup[] enemies, int untilMs, BattleEventKind kind)
        {
            var battle = new BattleEngine(TestData.Setup(Balance, party, enemies));
            battle.AdvanceTo(untilMs);
            return battle.Events.Where(e => e.Kind == kind).ToList();
        }

        [Test]
        public void Damage_GrowsWithTheOwnersStat()
        {
            var blade = new EquippedItem(TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, scalesWith: Stat.Str));
            List<BattleEvent> hits = Run(new[] { Owner(With(Stat.Str, 12), 1000, blade) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.Damaged);
            Assert.AreEqual(120, hits.Single().A, "1.00 x 1.2 (strength 12).");

            hits = Run(new[] { Owner(With(Stat.Str, 7), 1000, blade) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.Damaged);
            Assert.AreEqual(70, hits.Single().A, "Strength 7: 0.7.");
        }

        [Test]
        public void Damage_TakesTheStat_WithTheCriticalHitAndTheWeaponPowerPassive()
        {
            // 1.00 rolled, a sure critical hit (x1.5), strength 12 (x1.2), then a weapon-power passive of +50%: 2.70.
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            var sure = new ItemData("sure", TestData.Text("sure"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 100, scalesWith: Stat.Str);
            BattleUnitSetup owner = Owner(With(Stat.Str, 12), 1000, new EquippedItem(sure));
            owner.Passive = new PassiveSpec(PassiveTrigger.Always, PassiveCondition.None, null, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 50);
            var battle = new BattleEngine(TestData.Setup(Balance, new[] { owner }, new[] { TestData.Enemy("dummy", 1, 100000) }));
            battle.AdvanceTo(1000);
            Assert.AreEqual(270, battle.Events.Single(e => e.Kind == BattleEventKind.Damaged).A);
            Assert.AreEqual(270, battle.Events.Single(e => e.Kind == BattleEventKind.CriticalHit).A, "The critical hit's line says what was dealt.");
        }

        [Test]
        public void HealAndShield_GrowWithSpirit()
        {
            var herbs = new EquippedItem(TestData.Item("herbs", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Support, scalesWith: Stat.Spi));
            BattleUnitSetup owner = Owner(With(Stat.Spi, 14), 1000, herbs);
            owner.Hp = 500;
            List<BattleEvent> heals = Run(new[] { owner }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.Healed);
            Assert.AreEqual(140, heals.Single().A, "1.00 x 1.4 (spirit 14).");

            var ward = new EquippedItem(TestData.Item("ward", 1000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Support, scalesWith: Stat.Spi));
            List<BattleEvent> shields = Run(new[] { Owner(With(Stat.Spi, 8), 1000, ward) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.ShieldGained);
            Assert.AreEqual(80, shields.Single().A, "Spirit 8: 0.8.");
        }

        [Test]
        public void Burn_GrowsWithIntellect_RoundedHalfUp()
        {
            var flask = new EquippedItem(TestData.Item("flask", 1000, EffectKind.Burn, TargetMode.EnemyFront, 2, ItemCategory.Attack, scalesWith: Stat.Int));
            List<BattleEvent> burns = Run(new[] { Owner(With(Stat.Int, 13), 1000, flask) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.BurnApplied);
            Assert.AreEqual(3, burns.Single().A, "2 stacks x 1.3 = 2.6 -> 3.");
            burns = Run(new[] { Owner(With(Stat.Int, 12), 1000, flask) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.BurnApplied);
            Assert.AreEqual(2, burns.Single().A, "2.4 -> 2.");
        }

        [Test]
        public void Enemies_AndItemsWithoutAnAxis_StayAsTheyAre()
        {
            // An enemy carries a strength item: it has no stats. A strong mercenary carries an item that grows with none.
            var claw = new EquippedItem(TestData.Item("claw", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, scalesWith: Stat.Str));
            var plain = new EquippedItem(TestData.Item("plain", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));
            var battle = new BattleEngine(TestData.Setup(
                Balance,
                // Dexterity at the baseline: the brute's claw cannot be dodged (stage 33), so both blows land.
                new[] { Owner(StatBlock.Uniform(20).With(Stat.Dex, 10), 100000, plain) },
                new[] { TestData.Enemy("brute", 1, 100000, claw) }));
            battle.AdvanceTo(1000);
            Assert.IsTrue(battle.Events.Where(e => e.Kind == BattleEventKind.Damaged).All(e => e.A == 100));
            Assert.AreEqual(2, battle.Events.Count(e => e.Kind == BattleEventKind.Damaged));
        }

        [Test]
        public void SameSetupWithStats_GivesTheSameLog()
        {
            BattleSetup Setup()
            {
                var blade = new EquippedItem(TestData.Item("blade", 1300, EffectKind.Damage, TargetMode.EnemyFront, 100, scalesWith: Stat.Str));
                var herbs = new EquippedItem(TestData.Item("herbs", 1700, EffectKind.Heal, TargetMode.Self, 50, ItemCategory.Support, scalesWith: Stat.Spi));
                var claw = new EquippedItem(TestData.Item("claw", 900, EffectKind.Damage, TargetMode.EnemyFront, 100));
                return TestData.Setup(Balance, new[] { Owner(new StatBlock(13, 9, 12, 6, 14, 10), 2000, blade, herbs) }, new[] { TestData.Enemy("brute", 1, 3000, claw) });
            }

            var first = new BattleEngine(Setup());
            var second = new BattleEngine(Setup());
            first.AdvanceTo(20000);
            for (int t = 250; t <= 20000; t += 250)
            {
                second.AdvanceTo(t);
            }

            Assert.AreEqual(BattleLog.Hash(first.Events), BattleLog.Hash(second.Events));
        }
    }
}
