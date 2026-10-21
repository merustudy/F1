using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The second round of the effect vocabulary (Slice D stage 45, Docs/Design/02_Combat_System.md §3·§4·§5, 18_Effect_Vocabulary_Proposal.md §10):
    /// seven states that count stacks and lose one at every tick — freeze and haste on the cooldowns, blind and aim on the hit, keen and fury on the
    /// critical hit, vulnerable on the damage items deal — and the rule that any effect may go to either side.
    /// The test balance: a stack of freeze or haste moves cooldowns 10%, of blind or aim the hit 5 points, of keen the critical chance 5 points, of fury
    /// the multiplier 25 points, of vulnerable the damage taken 10%; hits are certain (HitBasePercent 100) unless a test lowers the base.
    /// </summary>
    public sealed class SecondVocabularyTests
    {
        /// <summary>An item that fires once when the battle starts, putting stacks of a state on its target.</summary>
        static EquippedItem Opening(string id, EffectKind kind, TargetMode target, int stacks, ItemCategory category = ItemCategory.Support, ItemSchool? school = null)
        {
            var effects = new List<ItemEffect> { TestData.Effect(kind, target, stacks) };
            return new EquippedItem(new ItemData(id, TestData.Text(id), category, 1, 1, 0, RowSpan.All, effects, 0, trigger: ItemTrigger.BattleStart,
                school: school ?? TestData.DefaultSchool(category)));
        }

        /// <summary>A magic attack item on a cooldown: it puts stacks on the front enemy and never misses.</summary>
        static EquippedItem Thrower(string id, int cooldownMs, EffectKind kind, int stacks)
        {
            return new EquippedItem(TestData.Item(id, cooldownMs, kind, TargetMode.EnemyFront, stacks, category: ItemCategory.Attack, school: ItemSchool.Magic));
        }

        static BattleEngine Battle(BattleUnitSetup[] party, BattleUnitSetup[] enemies, int untilMs, BalanceData balance = null)
        {
            var battle = new BattleEngine(TestData.Setup(balance ?? TestData.Balance(), party, enemies));
            battle.AdvanceTo(untilMs);
            return battle;
        }

        static List<BattleEvent> Of(BattleEngine battle, BattleEventKind kind, string cause = null)
        {
            return battle.Events.Where(e => e.Kind == kind && (cause == null || e.Id == cause)).ToList();
        }

        static IEnumerable<int> Activations(BattleEngine battle, string item)
        {
            return Of(battle, BattleEventKind.ItemActivated, item).Select(e => e.TimeMs);
        }

        // ---- Freeze and haste ----

        [Test]
        public void Freeze_SlowsTheCooldownsScheduledWhileItLasts_AndDecaysAtTheTick()
        {
            // The enemy's attack cycles every second. Three stacks of freeze land at 3.5 s, in the middle of a cooldown, which they do not touch: the
            // attack fires at 4 s as scheduled. The tick at 4 s takes one first (2 left), then the attack fires and schedules 1000 x 120% = 1200 ms; at
            // 5 s one more goes (1 left); the attack at 5.2 s schedules 1100 ms; at 6 s the last goes; the attack at 6.3 s schedules 1000 ms again.
            // (The thrower fires again at 7 s, after the battle is read.)
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, Thrower("frost", 3500, EffectKind.Freeze, 3)) },
                new[] { TestData.Enemy("e", 1, 100000, TestData.Attack(1000, 1)) },
                6400);

            BattleEvent applied = Of(battle, BattleEventKind.StateApplied).Single();
            Assert.AreEqual(3500, applied.TimeMs);
            Assert.AreEqual((int)EffectKind.Freeze, applied.B);
            Assert.AreEqual(3, applied.A);
            Assert.AreEqual(3, applied.C);
            CollectionAssert.AreEqual(new[] { 1000, 2000, 3000, 4000, 5200, 6300 }, Activations(battle, "attack").ToList());
            Assert.AreEqual(0, battle.Enemies[0].Freeze, "Gone after the tick at 6 s.");
        }

        [Test]
        public void Haste_QuickensTheCooldowns_NeverUnderTheLeastCooldown_AndNotTheItemDoingIt()
        {
            // Three stacks of haste at 0 ms on both mercenaries. The tick at 1 s takes one; the attack fires at 1 s and schedules 800 ms; at 1.8 s (2 stacks
            // still) 800 ms again; the tick at 2 s leaves one; at 2.6 s it schedules 900 ms; at 3 s the last goes; at 3.5 s 1000 ms.
            EquippedItem drum = Opening("drum", EffectKind.Haste, TargetMode.AllyAll, 3);
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("a", 1, 100000, drum), TestData.Mercenary("b", 2, 100000, TestData.Attack(1000, 1)) },
                new[] { TestData.Enemy("e", 1, 100000) },
                4600);
            CollectionAssert.AreEqual(new[] { 1000, 1800, 2600, 3500, 4500 }, Activations(battle, "attack"));

            // Twenty stacks would take the cooldown under 0: it stops at MinCooldownMs.
            BalanceData balance = TestData.Balance();
            BattleEngine floored = Battle(
                new[] { TestData.Mercenary("a", 1, 100000, Opening("drum", EffectKind.Haste, TargetMode.AllyAll, 20)), TestData.Mercenary("b", 2, 100000, TestData.Attack(1000, 1)) },
                new[] { TestData.Enemy("e", 1, 100000) },
                1000 + balance.MinCooldownMs);
            CollectionAssert.AreEqual(new[] { 1000, 1000 + balance.MinCooldownMs }, Activations(floored, "attack"));
        }

        // ---- Blind and aim ----

        [Test]
        public void Blind_TakesItsPointsOffTheHit_Aim_AddsThem_OnThePhysicalBlowsOfTheOneWhoCarriesThem()
        {
            // At a base of 50%: thirty stacks of aim make every one of ten swings certain; thirty stacks of blind take the chance to the least allowed.
            BalanceData balance = TestData.Balance(("HitBasePercent", 50));
            EquippedItem sword = new EquippedItem(TestData.Item("sword", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));

            BattleEngine aimed = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, Opening("sight", EffectKind.Aim, TargetMode.Self, 30), sword) },
                new[] { TestData.Enemy("e", 1, 100000) },
                10000,
                balance);
            Assert.AreEqual(10, Of(aimed, BattleEventKind.Damaged, "sword").Count);
            Assert.AreEqual(0, Of(aimed, BattleEventKind.Missed).Count, "+150 points over 50%: certain, no roll drawn.");

            BattleEngine blinded = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, sword) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("soot", EffectKind.Blind, TargetMode.EnemyFront, 30, ItemCategory.Attack, ItemSchool.Magic)) },
                10000,
                balance);
            List<BattleEvent> misses = Of(blinded, BattleEventKind.Missed, "sword");
            Assert.IsTrue(misses.Count >= 5, $"At the least chance ({balance.HitMinPercent}%) most of ten swings miss: {misses.Count} did.");
            Assert.IsTrue(misses.All(e => e.B == balance.HitMinPercent * 100), "The chance is the least allowed, in ten-thousandths.");

            // A magic blow never rolls: blind means nothing to it.
            EquippedItem bolt = new EquippedItem(TestData.Item("bolt", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, school: ItemSchool.Magic));
            BattleEngine magic = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, bolt) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("soot", EffectKind.Blind, TargetMode.EnemyFront, 30, ItemCategory.Attack, ItemSchool.Magic)) },
                5000,
                balance);
            Assert.AreEqual(5, Of(magic, BattleEventKind.Damaged, "bolt").Count);
            Assert.AreEqual(0, Of(magic, BattleEventKind.Missed).Count);
        }

        // ---- Keen and fury ----

        [Test]
        public void Keen_AddsItsPointsToTheCriticalChance_OfAWeaponThatHasOne()
        {
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            var blade = new EquippedItem(new ItemData("blade", TestData.Text("blade"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 1));
            BattleEngine keen = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, Opening("edge", EffectKind.Keen, TargetMode.Self, 30), blade) },
                new[] { TestData.Enemy("e", 1, 100000) },
                10000);
            Assert.AreEqual(10, Of(keen, BattleEventKind.CriticalHit).Count, "1% + 30 stacks x 5 points: every blow of ten.");
            Assert.AreEqual(150, Of(keen, BattleEventKind.Damaged, "blade").First().A, "150% of 100 at the first blow.");

            // A weapon without a critical chance has none, keen or not.
            var blunt = new EquippedItem(TestData.Item("blunt", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100));
            BattleEngine none = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, Opening("edge", EffectKind.Keen, TargetMode.Self, 30), blunt) },
                new[] { TestData.Enemy("e", 1, 100000) },
                3000);
            Assert.AreEqual(0, Of(none, BattleEventKind.CriticalHit).Count);
        }

        [Test]
        public void Fury_AddsItsPointsToTheCriticalMultiplier_AndDecaysAtTheTick()
        {
            // A certain critical hit of 100: three stacks of fury at 0 ms. The tick at 1 s takes one (2 x 25 = +50 points: 200), at 2 s one (175), at 3 s none (150).
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 100) };
            var blade = new EquippedItem(new ItemData("blade", TestData.Text("blade"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, critChancePercent: 100));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, Opening("rage", EffectKind.Fury, TargetMode.Self, 3), blade) },
                new[] { TestData.Enemy("e", 1, 100000) },
                3000);
            CollectionAssert.AreEqual(new[] { 200, 175, 150 }, Of(battle, BattleEventKind.Damaged, "blade").Select(e => e.A));
        }

        // ---- Vulnerable ----

        [Test]
        public void Vulnerable_RaisesTheDamageItemsDeal_NotTheTicks()
        {
            // The party's opener puts two stacks of vulnerable on the enemy at 0 ms. The tick at 1 s takes one: the blow of 100 at 1 s deals 110, the one
            // at 2 s 100. A burn tick on the same enemy deals its stack as ever.
            BattleEngine battle = Battle(
                new[]
                {
                    TestData.Mercenary("m", 1, 100000, Opening("mark", EffectKind.Vulnerable, TargetMode.EnemyFront, 2, ItemCategory.Attack, ItemSchool.Magic),
                        Opening("ember", EffectKind.Burn, TargetMode.EnemyFront, 1, ItemCategory.Attack, ItemSchool.Magic), TestData.Attack(1000, 100)),
                },
                new[] { TestData.Enemy("e", 1, 100000) },
                2000);
            CollectionAssert.AreEqual(new[] { 110, 100 }, Of(battle, BattleEventKind.Damaged, "attack").Select(e => e.A).ToList());
            Assert.AreEqual(1, Of(battle, BattleEventKind.Damaged, BattleEvent.CauseBurn).Single().A, "The burn's tick is not an item's blow.");
        }

        // ---- The tick, the cleanse, the sides ----

        [Test]
        public void TheStates_LoseAStackAtTheTick_AfterTheRegen_AndACleanseTakesOffWhatHurts()
        {
            // Every state at 2 stacks on the front mercenary (its own openers and the enemy's). At 1 s each has 1. A cleanse of 5 at 1.5 s takes the
            // freeze, blind and vulnerable (and the burn), not the aim, keen, fury or haste.
            EquippedItem water = new EquippedItem(TestData.Item("water", 1500, EffectKind.Cleanse, TargetMode.Self, 5, category: ItemCategory.Support));
            BattleEngine battle = Battle(
                new[]
                {
                    TestData.Mercenary("m", 1, 100000, Opening("a", EffectKind.Aim, TargetMode.Self, 2), Opening("k", EffectKind.Keen, TargetMode.Self, 2),
                        Opening("f", EffectKind.Fury, TargetMode.Self, 2), Opening("h", EffectKind.Haste, TargetMode.Self, 2), water),
                },
                new[]
                {
                    TestData.Enemy("e", 1, 100000, Opening("fr", EffectKind.Freeze, TargetMode.EnemyFront, 2, ItemCategory.Attack, ItemSchool.Magic),
                        Opening("bl", EffectKind.Blind, TargetMode.EnemyFront, 2, ItemCategory.Attack, ItemSchool.Magic),
                        Opening("vu", EffectKind.Vulnerable, TargetMode.EnemyFront, 2, ItemCategory.Attack, ItemSchool.Magic),
                        Opening("em", EffectKind.Burn, TargetMode.EnemyFront, 3, ItemCategory.Attack, ItemSchool.Magic)),
                },
                1000);
            BattleUnit m = battle.Party[0];
            Assert.AreEqual(1, m.Freeze);
            Assert.AreEqual(1, m.Blind);
            Assert.AreEqual(1, m.Aim);
            Assert.AreEqual(1, m.Keen);
            Assert.AreEqual(1, m.Fury);
            Assert.AreEqual(1, m.Haste);
            Assert.AreEqual(1, m.Vulnerable);
            Assert.AreEqual(2, m.Burn);

            battle.AdvanceTo(1500);
            BattleEvent cleansed = Of(battle, BattleEventKind.Cleansed).Single();
            Assert.AreEqual(2, cleansed.A, "The burn's two stacks.");
            Assert.AreEqual(0, cleansed.B, "No bleed.");
            Assert.AreEqual(3, cleansed.C, "One each of freeze, blind and vulnerable.");
            Assert.AreEqual(0, m.Freeze + m.Blind + m.Vulnerable + m.Burn);
            Assert.AreEqual(4, m.Aim + m.Keen + m.Fury + m.Haste, "What helps stays.");

            battle.AdvanceTo(2000);
            Assert.AreEqual(0, m.Aim + m.Keen + m.Fury + m.Haste, "The last stacks went at the tick.");
        }

        [Test]
        public void AnyEffect_MayGoToEitherSide_AndABlowOnOnesOwnSide_NeverMissesCritsOrMeetsThorns()
        {
            // A physical weapon that cuts its owner (a toll blade), a regen on the enemy, a freeze on oneself: all land as the data says. The two stacks of
            // chill slow the blade itself: the tick at 1 s leaves one, so the cut at 1 s schedules 1100 ms; the tick at 2 s takes it, so the cut at 2.1 s
            // schedules 1000 ms again.
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.Self, 100) };
            var toll = new EquippedItem(new ItemData("toll", TestData.Text("toll"), ItemCategory.Weapon, 1, 1, 1000, RowSpan.All, effects, 0, melee: true, critChancePercent: 100));
            BattleUnitSetup self = TestData.Mercenary("m", 1, 100000, toll, Opening("briar", EffectKind.Thorns, TargetMode.Self, 300, ItemCategory.Armor),
                    Opening("chill", EffectKind.Freeze, TargetMode.Self, 2), Opening("gift", EffectKind.Regen, TargetMode.EnemyFront, 2))
                .WithPassive(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget, 3);
            BattleUnitSetup dodgy = TestData.Enemy("e", 1, 100000);
            dodgy.Dodge = 100;
            BattleEngine battle = Battle(new[] { self }, new[] { dodgy }, 3100, TestData.Balance(("HitBasePercent", 50)));

            List<BattleEvent> cuts = Of(battle, BattleEventKind.Damaged, "toll");
            CollectionAssert.AreEqual(new[] { 1000, 2100, 3100 }, cuts.Select(e => e.TimeMs).ToList(), "Every cut lands (no hit roll on one's own side), on cooldowns the chill slowed.");
            Assert.IsTrue(cuts.All(e => e.Target.Side == BattleSide.Party && e.A == 100), "And none crits, certain as the blade is.");
            Assert.AreEqual(0, Of(battle, BattleEventKind.Missed).Count + Of(battle, BattleEventKind.CriticalHit).Count);
            Assert.AreEqual(0, Of(battle, BattleEventKind.Damaged, BattleEvent.CauseThorns).Count, "One's own thorns do not answer one's own blade.");
            Assert.AreEqual(0, battle.Party[0].Burn, "The weapon-hit passive stays quiet on one's own side.");
            Assert.AreEqual(2, Of(battle, BattleEventKind.Healed, BattleEvent.CauseRegen).Count, "The enemy regenerates on the party's gift (2 stacks: ticks at 1 and 2 s).");
            BattleEvent chill = Of(battle, BattleEventKind.StateApplied).Single(e => e.B == (int)EffectKind.Freeze && e.Target.Side == BattleSide.Party);
            Assert.AreEqual(2, chill.A, "The chill on oneself: two stacks, logged once by its one opener.");
        }

        [Test]
        public void ABattleWithTheSevenStates_ReplaysToTheSameLog()
        {
            BattleSetup Setup()
            {
                var blade = new EquippedItem(new ItemData("blade", TestData.Text("blade"), ItemCategory.Weapon, 1, 1, 900, RowSpan.All,
                    new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 300) }, 0, melee: true, critChancePercent: 20));
                return TestData.Setup(TestData.Balance(("HitBasePercent", 70)),
                    new[]
                    {
                        TestData.Mercenary("a", 1, 6000, blade, Thrower("frost", 1300, EffectKind.Freeze, 2), Opening("sight", EffectKind.Aim, TargetMode.AllyAll, 4)),
                        TestData.Mercenary("b", 2, 5000, Thrower("mark", 1700, EffectKind.Vulnerable, 2), Opening("rage", EffectKind.Fury, TargetMode.AllyAll, 3),
                            new EquippedItem(TestData.Item("drum", 2100, EffectKind.Haste, TargetMode.AllyAll, 2, category: ItemCategory.Support)),
                            new EquippedItem(TestData.Item("edge", 1900, EffectKind.Keen, TargetMode.AllyAll, 2, category: ItemCategory.Support))),
                    },
                    new[]
                    {
                        TestData.Enemy("e1", 1, 8000, new EquippedItem(TestData.Item("club", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, melee: true), powerPercent: 250),
                            Opening("soot", EffectKind.Blind, TargetMode.EnemyAll, 3, ItemCategory.Attack, ItemSchool.Magic)),
                        TestData.Enemy("e2", 2, 8000, new EquippedItem(TestData.Item("water", 2300, EffectKind.Cleanse, TargetMode.AllyAll, 1, category: ItemCategory.Support))),
                    });
            }

            var whole = new BattleEngine(Setup());
            whole.AdvanceTo(20000);
            var stepped = new BattleEngine(Setup());
            for (int t = 100; t <= 20000; t += 100)
            {
                stepped.AdvanceTo(t);
            }

            BattleEngine replayed = BattleEngine.Replay(Setup(), whole.Inputs, whole.TimeMs);
            var seen = new HashSet<EffectKind>(whole.Events.Where(e => e.Kind == BattleEventKind.StateApplied).Select(e => (EffectKind)e.B));
            Assert.AreEqual(7, seen.Count, "Every state was stacked at least once.");
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(stepped.Events));
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(replayed.Events));
        }

        // ---- The data ----

        [Test]
        public void TheDefinition_CountsTheStatesAsStacks_AndKnowsWhichHurt()
        {
            foreach (EffectKind state in new[] { EffectKind.Freeze, EffectKind.Blind, EffectKind.Aim, EffectKind.Keen, EffectKind.Fury, EffectKind.Haste, EffectKind.Vulnerable })
            {
                Assert.IsTrue(ItemEffect.IsState(state) && ItemEffect.CountsStacks(state) && ItemEffect.CountsWhole(state), state.ToString());
                Assert.DoesNotThrow(() => TestData.Effect(state, TargetMode.Self));
                Assert.DoesNotThrow(() => TestData.Effect(state, TargetMode.EnemyAll));
                Assert.Throws<DataException>(() => new ItemEffect(state, TargetMode.Self, 0, 1, 2), "One value, not a range.");
            }

            Assert.IsTrue(ItemEffect.Hurts(EffectKind.Freeze) && ItemEffect.Hurts(EffectKind.Blind) && ItemEffect.Hurts(EffectKind.Vulnerable));
            Assert.IsFalse(ItemEffect.Hurts(EffectKind.Aim) || ItemEffect.Hurts(EffectKind.Keen) || ItemEffect.Hurts(EffectKind.Fury) || ItemEffect.Hurts(EffectKind.Haste));
            Assert.IsFalse(ItemEffect.IsState(EffectKind.Burn) || ItemEffect.IsState(EffectKind.Bleed) || ItemEffect.IsState(EffectKind.Stagger));

            // A physical attack item's freeze rolls its hit on the other side; a magic one's never; aim never.
            ItemData physical = TestData.Item("ice", 1000, EffectKind.Freeze, TargetMode.EnemyFront, 2, category: ItemCategory.Attack);
            ItemData magic = TestData.Item("ice", 1000, EffectKind.Freeze, TargetMode.EnemyFront, 2, category: ItemCategory.Attack, school: ItemSchool.Magic);
            Assert.IsTrue(StatRules.CanMiss(physical, physical.Effects[0]));
            Assert.IsFalse(StatRules.CanMiss(magic, magic.Effects[0]));
            Assert.IsFalse(StatRules.CanMiss(TestData.Item("sight", 1000, EffectKind.Aim, TargetMode.Self, 2, category: ItemCategory.Attack), TestData.Effect(EffectKind.Aim, TargetMode.Self, 2)));
        }

        [Test]
        public void Transform_ReadsTheStatesWhole_AndTheBalanceHasTheirPoints()
        {
            string items = TestCsv.Items
                + "ice,얼음,Ice,Attack,2,1,4000,all,Freeze,EnemyFront,1,3,Blind,EnemyFront,1,2,0,,0,false,,,,Magic\n"
                + "banner,깃발,Banner,Support,2,2,6000,back:3,Aim,AllyAll,,2,Haste,AllyAll,,1,0,,0,false,,,,\n"
                + "horn,뿔피리,Horn,Support,2,1,5000,all,Keen,Self,,2,Fury,Self,,1,0,,0,false,,,,\n"
                + "brand,낙인,Brand,Attack,2,1,3000,all,Vulnerable,EnemyFront,1,2,,,,,0,,0,false,,,,Magic\n";
            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, items)).Data;
            Assert.AreEqual(3, data.Items.Get("ice").Effects[0].Min);
            Assert.AreEqual(EffectKind.Blind, data.Items.Get("ice").Effects[1].Kind);
            Assert.AreEqual(2, data.Items.Get("banner").Effects[0].Min);
            Assert.AreEqual(EffectKind.Haste, data.Items.Get("banner").Effects[1].Kind);
            Assert.AreEqual(EffectKind.Fury, data.Items.Get("horn").Effects[1].Kind);
            Assert.AreEqual(2, data.Items.Get("brand").Effects[0].Min);

            BalanceData balance = data.Balance;
            Assert.AreEqual(10, balance.FreezePercentPerStack);
            Assert.AreEqual(5, balance.BlindPercentPerStack);
            Assert.AreEqual(5, balance.AimPercentPerStack);
            Assert.AreEqual(5, balance.KeenPercentPerStack);
            Assert.AreEqual(25, balance.FuryPercentPerStack);
            Assert.AreEqual(10, balance.HastePercentPerStack);
            Assert.AreEqual(10, balance.VulnerablePercentPerStack);
        }
    }
}
