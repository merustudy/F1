using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The armour (Slice D stage 39, Docs/Design/02_Combat_System.md §3·§4 "방어구"): an item that activates once when a battle starts and never
    /// cycles, the points a helm takes off the critical chance of the weapons that hit its owner, and the fatigue it costs as defensive gear.
    /// </summary>
    public sealed class ArmorTests
    {
        static ItemData Armour(string id = "armour", int shield = 545, int critTaken = 0, RowSpan rows = null, int width = 2, int height = 2)
        {
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Shield, TargetMode.Self, shield) };
            return new ItemData(id, TestData.Text(id), ItemCategory.Armor, width, height, 0, rows ?? RowSpan.All, effects, 10, price: 20, dropWeight: 10,
                trigger: ItemTrigger.BattleStart, critTakenPercent: critTaken);
        }

        /// <summary>A weapon with a critical chance: <paramref name="damage"/> to the front enemy every second.</summary>
        static EquippedItem Weapon(int critChance, int damage = 100, int cooldownMs = 1000)
        {
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, damage) };
            return new EquippedItem(new ItemData("crit_blade", TestData.Text("crit_blade"), ItemCategory.Weapon, 1, 1, cooldownMs, RowSpan.All, effects, 0,
                critChancePercent: critChance));
        }

        static BattleEngine Battle(BattleUnitSetup[] party, BattleUnitSetup[] enemies, int untilMs)
        {
            var battle = new BattleEngine(TestData.Setup(TestData.Balance(), party, enemies));
            battle.AdvanceTo(untilMs);
            return battle;
        }

        [Test]
        public void AnItemThatActivatesWhenABattleStarts_ShieldsItsOwnerOnce_AtZero_AfterThePassives()
        {
            BattleUnitSetup knight = TestData.Mercenary("knight", 1, 4000, new EquippedItem(Armour()))
                .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self, 545);
            var battle = new BattleEngine(TestData.Setup(TestData.Balance(), new[] { knight }, new[] { TestData.Enemy("e", 1, 100000) }));

            List<BattleEvent> shields = battle.Events.Where(e => e.Kind == BattleEventKind.ShieldGained).ToList();
            Assert.AreEqual(2, shields.Count, "The passive's and the armour's, before any time passes.");
            Assert.AreEqual(BattleEvent.CausePassive, shields[0].Id, "The job's passive comes first.");
            Assert.AreEqual("armour", shields[1].Id);
            Assert.AreEqual(1090, battle.Party[0].Shield, "5.45 + 5.45.");
            Assert.IsTrue(battle.Events.Single(e => e.Kind == BattleEventKind.ItemActivated).TimeMs == 0, "Its activation is logged at 0 ms, for the screen's pulse.");
            Assert.IsTrue(battle.Party[0].Items[0].Active, "It works where its owner stands.");
            Assert.IsFalse(battle.Party[0].Items[0].Cycles, "But it never charges.");
        }

        [Test]
        public void AnItemThatActivatesWhenABattleStarts_NeverActivatesAgain()
        {
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, new EquippedItem(Armour()), TestData.Attack(1000, 1)) },
                new[] { TestData.Enemy("e", 1, 100000) },
                30000);
            Assert.AreEqual(1, battle.Events.Count(e => e.Kind == BattleEventKind.ItemActivated && e.Id == "armour"));
            Assert.AreEqual(30, battle.Events.Count(e => e.Kind == BattleEventKind.ItemActivated && e.Id == "attack"), "The other item cycles as before.");
        }

        [Test]
        public void AnItemThatActivatesWhenABattleStarts_DoesNothing_OutsideItsRows()
        {
            ItemData frontOnly = Armour("front_plate", rows: RowSpan.Front(1));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("a", 1, 4000), TestData.Mercenary("b", 2, 4000, new EquippedItem(frontOnly)) },
                new[] { TestData.Enemy("e", 1, 100000) },
                10000);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.ItemActivated && e.Id == "front_plate"));
            Assert.AreEqual(0, battle.Party[1].Shield);
        }

        [Test]
        public void TheTierMultipliesTheArmoursShield()
        {
            var battle = new BattleEngine(TestData.Setup(TestData.Balance(),
                new[] { TestData.Mercenary("m", 1, 4000, new EquippedItem(Armour(), tier: ItemTier.Tier1)) },
                new[] { TestData.Enemy("e", 1, 100000) }));
            Assert.AreEqual(1090, battle.Party[0].Shield, "1-star: Tier1Percent 200.");
        }

        [Test]
        public void TheHelmTakesItsPointsOffTheCriticalChance_NeverUnderZero()
        {
            BalanceData balance = TestData.Balance();
            ItemData weapon = Weapon(5).Item;
            Assert.AreEqual(200, StatRules.CritChance(balance, null, weapon, takenPercent: 3), "5% less 3 points: 2%.");
            Assert.AreEqual(0, StatRules.CritChance(balance, null, weapon, takenPercent: 10), "Never under 0.");
            Assert.AreEqual(500, StatRules.CritChance(balance, null, weapon), "A card has no target: the weapon's.");

            // In battle: a certain critical hit on a mercenary in a helm of 40 points is a 60% one, and the log says so.
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, new EquippedItem(Armour("helm", 218, 40, width: 2, height: 1))) },
                new[] { TestData.Enemy("e", 1, 100000, Weapon(100)) },
                20000);
            List<BattleEvent> crits = battle.Events.Where(e => e.Kind == BattleEventKind.CriticalHit).ToList();
            Assert.IsTrue(crits.Count > 0 && crits.Count < 20, "Some of twenty attacks, not all.");
            Assert.IsTrue(crits.All(e => e.C == 6000));
        }

        [Test]
        public void TheHelm_ChangesWhichHitsAreCritical_NotTheRollsDrawn()
        {
            // The weapon is certain to strike a critical hit; a helm of 100 points takes it to 0. The roll is drawn all the same, so every attack
            // rolls the same damage in both battles: the bare target takes 150% of what the helmed one takes, hit for hit.
            ItemData helm = new ItemData("helm", TestData.Text("helm"), ItemCategory.Armor, 2, 1, 600000, RowSpan.All,
                new List<ItemEffect> { TestData.Effect(EffectKind.Shield, TargetMode.Self, 1) }, 0, critTakenPercent: 100);
            var damage = new List<ItemEffect> { new ItemEffect(EffectKind.Damage, TargetMode.EnemyFront, 1, 100, 400) };
            var weapon = new EquippedItem(new ItemData("ranged", TestData.Text("ranged"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, damage, 0, critChancePercent: 100));

            BattleEngine bare = Battle(new[] { TestData.Mercenary("m", 1, 1000000) }, new[] { TestData.Enemy("e", 1, 100000, weapon) }, 20000);
            BattleEngine helmed = Battle(new[] { TestData.Mercenary("m", 1, 1000000, new EquippedItem(helm)) }, new[] { TestData.Enemy("e", 1, 100000, weapon) }, 20000);

            List<int> bareHits = bare.Events.Where(e => e.Kind == BattleEventKind.Damaged).Select(e => e.A).ToList();
            List<int> helmedHits = helmed.Events.Where(e => e.Kind == BattleEventKind.Damaged).Select(e => e.A).ToList();
            Assert.AreEqual(20, bareHits.Count);
            Assert.AreEqual(20, bare.Events.Count(e => e.Kind == BattleEventKind.CriticalHit));
            Assert.AreEqual(0, helmed.Events.Count(e => e.Kind == BattleEventKind.CriticalHit));
            Assert.AreEqual(helmedHits.Select(hit => hit * 150 / 100), bareHits, "The same rolls, one and a half times.");
        }

        [Test]
        public void ArmourIsEquipment_ItCostsTheEquipmentFatigue()
        {
            BalanceData balance = TestData.Balance(("FatigueEquipment", 1));
            Assert.IsTrue(FatigueRules.CostsFatigue(new EquippedItem(Armour())));
            Assert.AreEqual(1, FatigueRules.ItemCost(balance, new EquippedItem(Armour())), "One for each piece, whatever its size.");
            Assert.AreEqual(2, FatigueRules.EquipmentCost(balance, new[] { new EquippedItem(Armour()), new EquippedItem(Armour("helm", 218, 3, width: 2, height: 1)) }));
        }

        [Test]
        public void TheDefinition_ChecksTheTriggerAndTheCritTaken()
        {
            var shield = new List<ItemEffect> { TestData.Effect(EffectKind.Shield, TargetMode.Self, 100) };
            Assert.Throws<DataException>(() => new ItemData("a", TestData.Text("a"), ItemCategory.Armor, 1, 1, 5000, RowSpan.All, shield, 0, trigger: ItemTrigger.BattleStart),
                "An item that activates when a battle starts has no cooldown.");
            Assert.Throws<DataException>(() => new ItemData("a", TestData.Text("a"), ItemCategory.Armor, 1, 1, 0, RowSpan.All, new List<ItemEffect>(), 0, trigger: ItemTrigger.BattleStart),
                "It needs an effect.");
            Assert.Throws<DataException>(() => new ItemData("a", TestData.Text("a"), ItemCategory.Support, 1, 1, 5000, RowSpan.All, shield, 0, critTakenPercent: 3),
                "Only defensive gear takes points off the critical chance.");
            Assert.Throws<DataException>(() => new ItemData("a", TestData.Text("a"), ItemCategory.Armor, 1, 1, 5000, RowSpan.All, shield, 0, critTakenPercent: 101));
            Assert.DoesNotThrow(() => Armour("ok", critTaken: 3));
            Assert.IsTrue(TestData.Item("cycling", 1000, EffectKind.Damage, TargetMode.EnemyFront).Cycles);
            Assert.IsFalse(Armour().Cycles);
        }
    }
}
