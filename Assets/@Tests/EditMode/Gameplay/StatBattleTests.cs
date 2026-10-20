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
            var blade = new EquippedItem(TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));
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
            var sure = new ItemData("sure", TestData.Text("sure"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 100, school: ItemSchool.Physical);
            BattleUnitSetup owner = Owner(With(Stat.Str, 12), 1000, new EquippedItem(sure));
            owner.Passive = new PassiveSpec(PassiveTrigger.Always, PassiveCondition.None, null, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 50);
            var battle = new BattleEngine(TestData.Setup(Balance, new[] { owner }, new[] { TestData.Enemy("dummy", 1, 100000) }));
            battle.AdvanceTo(1000);
            Assert.AreEqual(270, battle.Events.Single(e => e.Kind == BattleEventKind.Damaged).A);
            Assert.AreEqual(270, battle.Events.Single(e => e.Kind == BattleEventKind.CriticalHit).A, "The critical hit's line says what was dealt.");
        }

        [Test]
        public void HealAndShield_OfSupportItems_GrowWithWisdom()
        {
            // 2026-10-20 스탯 개정: an attack or a support item grows with wisdom (spirit before); a magic weapon's heal with intellect.
            var herbs = new EquippedItem(TestData.Item("herbs", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Support));
            BattleUnitSetup owner = Owner(With(Stat.Wis, 14), 1000, herbs);
            owner.Hp = 500;
            List<BattleEvent> heals = Run(new[] { owner }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.Healed);
            Assert.AreEqual(140, heals.Single().A, "1.00 x 1.4 (wisdom 14).");

            var ward = new EquippedItem(TestData.Item("ward", 1000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Support));
            List<BattleEvent> shields = Run(new[] { Owner(With(Stat.Wis, 8), 1000, ward) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.ShieldGained);
            Assert.AreEqual(80, shields.Single().A, "Wisdom 8: 0.8.");

            var staff = new EquippedItem(TestData.Item("staff", 1000, EffectKind.Heal, TargetMode.Self, 100, ItemCategory.Weapon, school: ItemSchool.Magic));
            BattleUnitSetup healer = Owner(With(Stat.Int, 13), 1000, staff);
            healer.Hp = 500;
            Assert.AreEqual(130, Run(new[] { healer }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.Healed).Single().A, "A magic weapon's heal: intellect 13.");
        }

        [Test]
        public void Burn_OfAnAttackItem_GrowsWithWisdom_RoundedHalfUp()
        {
            var flask = new EquippedItem(TestData.Item("flask", 1000, EffectKind.Burn, TargetMode.EnemyFront, 2, ItemCategory.Attack, school: ItemSchool.Magic));
            List<BattleEvent> burns = Run(new[] { Owner(With(Stat.Wis, 13), 1000, flask) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.BurnApplied);
            Assert.AreEqual(3, burns.Single().A, "2 stacks x 1.3 = 2.6 -> 3.");
            burns = Run(new[] { Owner(With(Stat.Wis, 12), 1000, flask) }, new[] { TestData.Enemy("dummy", 1, 100000) }, 1000, BattleEventKind.BurnApplied);
            Assert.AreEqual(2, burns.Single().A, "2.4 -> 2.");
        }

        [Test]
        public void Cooldown_OfEquipment_ShortensWithTheQuickeningStat_InBattle()
        {
            // 2026-10-20 스탯 개정: dexterity 14 and 1% a point: a 1000ms blade cycles in 960ms (two blows by 1920ms, where the baseline lands its second at 2000ms).
            BalanceData quick = TestData.Balance(("CooldownPercentPerPoint", 1));
            var blade = new EquippedItem(TestData.Item("blade", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));
            var battle = new BattleEngine(TestData.Setup(quick, new[] { Owner(With(Stat.Dex, 14), 1000, blade) }, new[] { TestData.Enemy("dummy", 1, 100000) }));
            battle.AdvanceTo(1999);
            Assert.AreEqual(new[] { 960, 1920 }, battle.Events.Where(e => e.Kind == BattleEventKind.Damaged).Select(e => e.TimeMs).ToArray());

            var slow = new BattleEngine(TestData.Setup(quick, new[] { Owner(With(Stat.Dex, 7), 1000, blade) }, new[] { TestData.Enemy("dummy", 1, 100000) }));
            slow.AdvanceTo(2060);
            Assert.AreEqual(new[] { 1030, 2060 }, slow.Events.Where(e => e.Kind == BattleEventKind.Damaged).Select(e => e.TimeMs).ToArray(), "Under the baseline: 3% slower.");
        }

        [Test]
        public void Enemies_AndItemsWithoutAnAxis_StayAsTheyAre()
        {
            // An enemy carries a strength item: it has no stats. A strong mercenary carries an item that grows with none (defensive gear).
            var claw = new EquippedItem(TestData.Item("claw", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));
            var plain = new EquippedItem(TestData.Item("plain", 5000, EffectKind.Shield, TargetMode.Self, 100, ItemCategory.Armor));
            var battle = new BattleEngine(TestData.Setup(
                Balance,
                // Dexterity at the baseline: the brute's claw cannot be dodged (stage 33), so both blows land.
                new[] { Owner(StatBlock.Uniform(20).With(Stat.Dex, 10), 100000, plain) },
                new[] { TestData.Enemy("brute", 1, 100000, claw) }));
            battle.AdvanceTo(5000);
            Assert.IsTrue(battle.Events.Where(e => e.Kind == BattleEventKind.Damaged).All(e => e.A == 100), "The brute's claw: the data's 1.00.");
            Assert.AreEqual(100, battle.Events.Single(e => e.Kind == BattleEventKind.ShieldGained).A, "The shield of a mercenary with every stat at 20: the data's.");
        }

        [Test]
        public void SameSetupWithStats_GivesTheSameLog()
        {
            BattleSetup Setup()
            {
                var blade = new EquippedItem(TestData.Item("blade", 1300, EffectKind.Damage, TargetMode.EnemyFront, 100));
                var herbs = new EquippedItem(TestData.Item("herbs", 1700, EffectKind.Heal, TargetMode.Self, 50, ItemCategory.Support));
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
