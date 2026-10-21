using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The effect vocabulary (Slice D stage 44, Docs/Design/02_Combat_System.md §3·§4·§5, 18_Effect_Vocabulary_Proposal.md): bleed, regen, thorns,
    /// cleanse, stagger, charge and a weapon's lifesteal. Each rule, their order at a tick, what the data checks, and that the log replays.
    /// The test balance deals 1 per bleed stack and heals 1 per regen stack at a tick, and lets every sliver through as a hit (HitMinDamage 0).
    /// </summary>
    public sealed class EffectVocabularyTests
    {
        /// <summary>An item that fires once when the battle starts: the way a test puts a state on a unit at 0 ms.</summary>
        static ItemData StartItem(string id, EffectKind kind, TargetMode target, int value, ItemCategory category = ItemCategory.Support, ItemSchool? school = null)
        {
            var effects = new List<ItemEffect> { TestData.Effect(kind, target, value) };
            return new ItemData(id, TestData.Text(id), category, 1, 1, 0, RowSpan.All, effects, 0, trigger: ItemTrigger.BattleStart,
                school: school ?? TestData.DefaultSchool(category));
        }

        /// <summary>The enemy opens the fight with this many stacks of bleed or burn on the front mercenary: a magic attack item, so it never misses.</summary>
        static EquippedItem Opening(string id, EffectKind kind, int stacks)
        {
            return new EquippedItem(StartItem(id, kind, TargetMode.EnemyFront, stacks, ItemCategory.Attack, ItemSchool.Magic));
        }

        static BattleEngine Battle(BattleUnitSetup[] party, BattleUnitSetup[] enemies, int untilMs, BalanceData balance = null, params PotionData[] potions)
        {
            BattleSetup setup = TestData.Setup(balance ?? TestData.Balance(), party, enemies);
            setup.Potions = potions;
            var battle = new BattleEngine(setup);
            battle.AdvanceTo(untilMs);
            return battle;
        }

        static List<BattleEvent> Of(BattleEngine battle, BattleEventKind kind, string cause = null)
        {
            return battle.Events.Where(e => e.Kind == kind && (cause == null || e.Id == cause)).ToList();
        }

        // ---- Bleed ----

        [Test]
        public void Bleed_TicksThroughTheShield_TiresNobody_AndDecaysToNothing()
        {
            // 3 stacks at 0 ms; the mercenary shields itself every half second. The ticks at 1, 2 and 3 seconds deal 3, 2 and 1 straight to HP.
            EquippedItem buckler = new EquippedItem(TestData.Item("buckler", 500, EffectKind.Shield, TargetMode.Self, 1000, category: ItemCategory.Armor));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 4000, buckler) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("knife", EffectKind.Bleed, 3)) },
                4000,
                TestData.Balance(("FatigueOnHit", 5)));

            Assert.AreEqual(3, Of(battle, BattleEventKind.BleedApplied).Single().A);
            List<BattleEvent> ticks = Of(battle, BattleEventKind.Damaged, BattleEvent.CauseBleed);
            CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, ticks.Select(e => e.TimeMs));
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, ticks.Select(e => e.A), "Stacks x BleedDamagePerStack, one stack less after each tick.");
            Assert.IsTrue(ticks.All(e => e.B == 0 && e.Source.IsNone), "The shield absorbs none of it; the wound is inside.");
            Assert.AreEqual(4000 - 6, battle.Party[0].Hp);
            Assert.IsTrue(battle.Party[0].Shield > 0, "The shield stands untouched.");
            Assert.AreEqual(0, battle.Party[0].Bleed);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.FatigueChanged), "A bleed tick is a hit that tires nobody, as a burn tick is.");
        }

        [Test]
        public void Bleed_StopsWhenTheUnitIsHealed_ByAnyHeal()
        {
            PotionData potion = TestData.HealPotion(100);
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 4000) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("knife", EffectKind.Bleed, 5)) },
                1500,
                null,
                potion);
            Assert.AreEqual(4, battle.Party[0].Bleed, "One tick took one stack.");

            Assert.IsTrue(battle.TryUsePotion(0, 0));
            Assert.AreEqual(0, battle.Party[0].Bleed);
            BattleEvent bound = Of(battle, BattleEventKind.Cleansed).Single();
            Assert.AreEqual(potion.Id, bound.Id, "Logged as a cleanse by the heal's cause.");
            Assert.AreEqual(0, bound.A);
            Assert.AreEqual(4, bound.B);

            battle.AdvanceTo(6000);
            Assert.AreEqual(1, Of(battle, BattleEventKind.Damaged, BattleEvent.CauseBleed).Count, "Only the tick before the heal.");
        }

        [Test]
        public void APhysicalBleed_RollsItsHit_AMagicOneNeverMisses()
        {
            BattleUnitSetup dodgy = TestData.Enemy("e", 1, 100000);
            dodgy.Dodge = 100;
            EquippedItem saw = new EquippedItem(TestData.Item("saw", 1000, EffectKind.Bleed, TargetMode.EnemyFront, 1));
            BattleEngine physical = Battle(new[] { TestData.Mercenary("m", 1, 100000, saw) }, new[] { dodgy }, 20000);
            int swings = Of(physical, BattleEventKind.ItemActivated, "saw").Count;
            int misses = Of(physical, BattleEventKind.Missed, "saw").Count;
            Assert.AreEqual(20, swings);
            Assert.AreEqual(swings, misses + Of(physical, BattleEventKind.BleedApplied).Count, "Every swing misses or bleeds.");
            Assert.IsTrue(misses > 0, "Against a dodge of 100 the chance is the least allowed.");

            dodgy = TestData.Enemy("e", 1, 100000);
            dodgy.Dodge = 100;
            EquippedItem spell = new EquippedItem(TestData.Item("spell", 1000, EffectKind.Bleed, TargetMode.EnemyFront, 1, school: ItemSchool.Magic));
            BattleEngine magic = Battle(new[] { TestData.Mercenary("m", 1, 100000, spell) }, new[] { dodgy }, 20000);
            Assert.AreEqual(20, Of(magic, BattleEventKind.BleedApplied).Count);
            Assert.AreEqual(0, Of(magic, BattleEventKind.Missed).Count);

            Assert.IsTrue(StatRules.CanMiss(saw.Item, saw.Item.Effects[0]));
            Assert.IsFalse(StatRules.CanMiss(spell.Item, spell.Item.Effects[0]));
            Assert.IsTrue(StatRules.CanMiss(TestData.Item("frost", 1000, EffectKind.Stagger, TargetMode.EnemyFront, 500, category: ItemCategory.Attack), TestData.Effect(EffectKind.Stagger, TargetMode.EnemyFront, 500)));
            Assert.IsFalse(StatRules.CanMiss(TestData.Item("herb", 1000, EffectKind.Regen, TargetMode.Self, 1, category: ItemCategory.Support), TestData.Effect(EffectKind.Regen, TargetMode.Self, 1)));
        }

        // ---- Regen ----

        [Test]
        public void Regen_HealsAtTheTick_Decays_AndTriggersNoHealPassive()
        {
            BattleUnitSetup priest = TestData.Mercenary("p", 1, 4000, new EquippedItem(StartItem("herb", EffectKind.Regen, TargetMode.Self, 3)))
                .WithPassive(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget, 100);
            priest.Hp = 1000;
            BattleEngine battle = Battle(new[] { priest }, new[] { TestData.Enemy("e", 1, 100000) }, 4000);

            Assert.AreEqual(3, Of(battle, BattleEventKind.RegenApplied).Single().A);
            List<BattleEvent> heals = Of(battle, BattleEventKind.Healed, BattleEvent.CauseRegen);
            CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, heals.Select(e => e.TimeMs));
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, heals.Select(e => e.A), "Stacks x RegenHealPerStack, one stack less after each tick.");
            Assert.AreEqual(1006, battle.Party[0].Hp);
            Assert.AreEqual(0, battle.Party[0].Regen);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.ShieldGained), "A tick is not the owner's heal effect: the priest's blessing stays quiet.");
        }

        [Test]
        public void AtOneTick_BurnThenBleedThenRegen_AndTheRegenBindsTheBleed()
        {
            // The enemy opens with 2 bleed and 1 burn on the mercenary; the mercenary brings 1 regen. At 1 s: the burn deals 1, the bleed deals 2 and
            // falls to 1, then the regen heals 1 and binds what is left of the bleed.
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 4000, new EquippedItem(StartItem("herb", EffectKind.Regen, TargetMode.Self, 1))) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("knife", EffectKind.Bleed, 2), Opening("ember", EffectKind.Burn, 1)) },
                1000);

            List<BattleEvent> atTheTick = battle.Events.Where(e => e.TimeMs == 1000 && e.Kind != BattleEventKind.ItemActivated).ToList();
            CollectionAssert.AreEqual(
                new[] { BattleEvent.CauseBurn, BattleEvent.CauseBleed, BattleEvent.CauseRegen, BattleEvent.CauseRegen },
                atTheTick.Select(e => e.Id),
                "Damaged by the burn, Damaged by the bleed, Healed by the regen, the bleed Cleansed by it.");
            Assert.AreEqual(BattleEventKind.Cleansed, atTheTick[3].Kind);
            Assert.AreEqual(1, atTheTick[3].B, "One stack was left to bind.");
            Assert.AreEqual(0, battle.Party[0].Bleed);
            Assert.AreEqual(0, battle.Party[0].Burn);
            Assert.AreEqual(0, battle.Party[0].Regen);
            Assert.AreEqual(4000 - 1 - 2 + 1, battle.Party[0].Hp);
        }

        // ---- Thorns ----

        [Test]
        public void Thorns_DealBackAMeleeBlow_ThroughTheAttackersShield_AndNothingForARangedOne()
        {
            EquippedItem briar = new EquippedItem(StartItem("briar", EffectKind.Thorns, TargetMode.Self, 300, ItemCategory.Armor));
            EquippedItem club = new EquippedItem(TestData.Item("club", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, melee: true), powerPercent: 200);
            EquippedItem bow = new EquippedItem(TestData.Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100), powerPercent: 200);
            EquippedItem plate = new EquippedItem(StartItem("plate", EffectKind.Shield, TargetMode.Self, 500, ItemCategory.Armor));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, briar) },
                new[] { TestData.Enemy("e", 1, 100000, plate, club, bow) },
                3000);

            Assert.AreEqual(300, battle.Party[0].Thorns);
            List<BattleEvent> back = Of(battle, BattleEventKind.Damaged, BattleEvent.CauseThorns);
            Assert.AreEqual(3, back.Count, "Once for each club blow at 1, 2 and 3 s; the bow's blows return nothing.");
            Assert.IsTrue(back.All(e => e.A == 300 && e.Target.Side == BattleSide.Enemy && e.Source.IsNone));
            CollectionAssert.AreEqual(new[] { 300, 200, 0 }, back.Select(e => e.B), "The attacker's shield of 5.00 takes the first two.");
            Assert.AreEqual(100000 - 400, battle.Enemies[0].Hp);
            Assert.IsFalse(battle.Events.Any(e => e.Kind == BattleEventKind.FatigueChanged), "Thorns tire nobody.");
        }

        [Test]
        public void Thorns_AreGoneWithAFallenUnit_AndAnAttackerTheyFellStopsItsBlow()
        {
            // A blow that fells the thorny unit gets nothing back.
            EquippedItem club = new EquippedItem(TestData.Item("club", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, melee: true), powerPercent: 200);
            EquippedItem thorns = new EquippedItem(StartItem("briar", EffectKind.Thorns, TargetMode.Self, 300, ItemCategory.Armor));
            BattleEngine felled = Battle(new[] { TestData.Mercenary("m", 1, 100000, club) }, new[] { TestData.Enemy("e", 1, 100, thorns) }, 1000);
            Assert.AreEqual(BattleResult.Victory, felled.Result);
            Assert.AreEqual(0, Of(felled, BattleEventKind.Damaged, BattleEvent.CauseThorns).Count);

            // A sweeping attacker felled by the first unit's thorns never reaches the second.
            EquippedItem scythe = new EquippedItem(TestData.Item("scythe", 1000, EffectKind.Damage, TargetMode.EnemyAll, 100, melee: true), powerPercent: 100);
            BattleEngine reaper = Battle(
                new[] { TestData.Mercenary("a", 1, 100000, thorns), TestData.Mercenary("b", 2, 100000, thorns) },
                new[] { TestData.Enemy("e", 1, 100, scythe) },
                1000);
            Assert.AreEqual(BattleResult.Victory, reaper.Result);
            List<BattleEvent> blows = Of(reaper, BattleEventKind.Damaged, "scythe");
            Assert.AreEqual(1, blows.Count, "The first blow landed; its thorns killed the attacker before the second.");
            Assert.AreEqual(100000, reaper.Party[1].Hp);
        }

        // ---- Cleanse ----

        [Test]
        public void Cleanse_TakesThatManyStacksOfBurnAndOfBleedOff_ByAnItemAndByAPotion()
        {
            EquippedItem water = new EquippedItem(TestData.Item("water", 1500, EffectKind.Cleanse, TargetMode.Self, 2, category: ItemCategory.Support));
            var potion = new PotionData("purge", TestData.Text("purge"), PotionEffect.Cleanse, 10, 1);
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 4000, water) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("ember", EffectKind.Burn, 5), Opening("knife", EffectKind.Bleed, 4)) },
                1500,
                null,
                potion);

            // After the tick at 1 s: burn 4, bleed 3. The water at 1.5 s takes two of each.
            BattleEvent byItem = Of(battle, BattleEventKind.Cleansed).Single();
            Assert.AreEqual("water", byItem.Id);
            Assert.AreEqual(2, byItem.A);
            Assert.AreEqual(2, byItem.B);
            Assert.AreEqual(2, battle.Party[0].Burn);
            Assert.AreEqual(1, battle.Party[0].Bleed);

            Assert.IsTrue(battle.TryUsePotion(0, 0));
            BattleEvent byPotion = Of(battle, BattleEventKind.Cleansed).Last();
            Assert.AreEqual("purge", byPotion.Id);
            Assert.AreEqual(2, byPotion.A, "What was left of the burn.");
            Assert.AreEqual(1, byPotion.B, "What was left of the bleed.");
            Assert.AreEqual(0, battle.Party[0].Burn);
            Assert.AreEqual(0, battle.Party[0].Bleed);
        }

        // ---- Stagger and charge ----

        [Test]
        public void Stagger_PushesEveryCyclingItemOfTheTargetBack_Once()
        {
            // The enemy's attack cycles every second. A magic stagger of 500 ms at 1.5 s moves its next activation from 2 s to 2.5 s, and the frost's
            // second activation at 3 s moves the one due at 3.5 s to 4 s: each push is once, by the milliseconds, nothing more.
            EquippedItem frost = new EquippedItem(TestData.Item("frost", 1500, EffectKind.Stagger, TargetMode.EnemyFront, 500, category: ItemCategory.Attack, school: ItemSchool.Magic));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 100000, frost) },
                new[] { TestData.Enemy("e", 1, 100000, TestData.Attack(1000, 1)) },
                4000);

            CollectionAssert.AreEqual(new[] { 1000, 2500, 4000 }, Of(battle, BattleEventKind.ItemActivated, "attack").Select(e => e.TimeMs));
            BattleEvent staggered = Of(battle, BattleEventKind.Staggered).First();
            Assert.AreEqual(1500, staggered.TimeMs);
            Assert.AreEqual(500, staggered.A);
            Assert.AreEqual(1, staggered.B, "One item was pushed.");
            Assert.AreEqual(BattleSide.Enemy, staggered.Target.Side);
        }

        [Test]
        public void Charge_PullsAnAllysItemsForward_NeverBeforeTheNextMillisecond_AndNeverItself()
        {
            // The drum charges everyone by 5 s at 1.2 s: the ally's attack due at 2 s fires at 1.201 s and cycles from there. The drum does not pull itself.
            EquippedItem drum = new EquippedItem(TestData.Item("drum", 1200, EffectKind.Charge, TargetMode.AllyAll, 5000, category: ItemCategory.Support));
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("a", 1, 100000, drum), TestData.Mercenary("b", 2, 100000, TestData.Attack(1000, 1)) },
                new[] { TestData.Enemy("e", 1, 100000) },
                3500);

            // 1 s, then 1.201 s (pulled at 1.2 s), 2.201 s, 2.401 s (pulled again when the drum fires at 2.4 s), 3.401 s.
            CollectionAssert.AreEqual(new[] { 1000, 1201, 2201, 2401, 3401 }, Of(battle, BattleEventKind.ItemActivated, "attack").Select(e => e.TimeMs));
            CollectionAssert.AreEqual(new[] { 1200, 2400 }, Of(battle, BattleEventKind.ItemActivated, "drum").Select(e => e.TimeMs), "Its own cooldown is untouched.");
            List<BattleEvent> charged = Of(battle, BattleEventKind.Charged).Where(e => e.TimeMs == 1200).ToList();
            Assert.AreEqual(2, charged.Count, "One event per ally, the drummer included.");
            Assert.AreEqual(0, charged.Single(e => e.Target.Index == 0).B, "The drummer's only cycling item is the drum: nothing pulled.");
            Assert.AreEqual(1, charged.Single(e => e.Target.Index == 1).B);
        }

        // ---- Determinism ----

        [Test]
        public void ABattleWithTheWholeVocabulary_ReplaysToTheSameLog()
        {
            BattleSetup Setup()
            {
                var fang = new EquippedItem(TestData.Item("fang", 900, EffectKind.Damage, TargetMode.EnemyFront, 300, melee: true));
                var saw = new EquippedItem(TestData.Item("saw", 1300, EffectKind.Bleed, TargetMode.EnemyFront, 2));
                var drum = new EquippedItem(TestData.Item("drum", 2100, EffectKind.Charge, TargetMode.AllyAll, 400, category: ItemCategory.Support));
                var water = new EquippedItem(TestData.Item("water", 1700, EffectKind.Cleanse, TargetMode.AllyLowestHp, 1, category: ItemCategory.Support));
                var herb = new EquippedItem(TestData.Item("herb", 1100, EffectKind.Regen, TargetMode.AllyLowestHp, 2, category: ItemCategory.Support));
                var briar = new EquippedItem(StartItem("briar", EffectKind.Thorns, TargetMode.Self, 120, ItemCategory.Armor));
                var frost = new EquippedItem(TestData.Item("frost", 1900, EffectKind.Stagger, TargetMode.EnemyFront, 300, category: ItemCategory.Attack, school: ItemSchool.Magic));
                var ember = new EquippedItem(TestData.Item("ember", 1500, EffectKind.Burn, TargetMode.EnemyFront, 2, category: ItemCategory.Attack, school: ItemSchool.Magic));
                var club = new EquippedItem(TestData.Item("club", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, melee: true), powerPercent: 250);
                BattleSetup setup = TestData.Setup(TestData.Balance(),
                    new[] { TestData.Mercenary("a", 1, 6000, fang, briar, drum), TestData.Mercenary("b", 2, 5000, saw, water, herb, frost) },
                    new[] { TestData.Enemy("e1", 1, 8000, club, ember), TestData.Enemy("e2", 2, 8000, Opening("knife", EffectKind.Bleed, 2), club) });
                setup.Potions = new[] { TestData.HealPotion(300), new PotionData("purge", TestData.Text("purge"), PotionEffect.Cleanse, 3, 1) };
                return setup;
            }

            var whole = new BattleEngine(Setup());
            whole.AdvanceTo(2200);
            Assert.IsTrue(whole.TryUsePotion(1, 1));
            whole.AdvanceTo(4100);
            Assert.IsTrue(whole.TryUsePotion(0, 0));
            whole.AdvanceTo(20000);

            var stepped = new BattleEngine(Setup());
            for (int t = 100; t <= 20000; t += 100)
            {
                if (t == 2200)
                {
                    stepped.AdvanceTo(2200);
                    Assert.IsTrue(stepped.TryUsePotion(1, 1));
                }
                else if (t == 4100)
                {
                    stepped.AdvanceTo(4100);
                    Assert.IsTrue(stepped.TryUsePotion(0, 0));
                }
                else
                {
                    stepped.AdvanceTo(t);
                }
            }

            BattleEngine replayed = BattleEngine.Replay(Setup(), whole.Inputs, whole.TimeMs);
            Assert.IsTrue(whole.Events.Any(e => e.Kind == BattleEventKind.BleedApplied) && whole.Events.Any(e => e.Kind == BattleEventKind.RegenApplied)
                && whole.Events.Any(e => e.Kind == BattleEventKind.Staggered) && whole.Events.Any(e => e.Kind == BattleEventKind.Charged)
                && whole.Events.Any(e => e.Kind == BattleEventKind.Cleansed) && whole.Events.Any(e => e.Kind == BattleEventKind.Damaged && e.Id == BattleEvent.CauseThorns),
                "The battle used the whole vocabulary.");
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(stepped.Events));
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(replayed.Events));
        }

        // ---- The data ----

        [Test]
        public void TheDefinition_LetsAnyKindGoToEitherSide_AndCountsStacksAndMilliseconds()
        {
            // 2026-10-21 (the user, stage 45): the data names the target, whichever side. What hurts is still known (the hit roll, the cleanse).
            foreach (EffectKind kind in new[] { EffectKind.Bleed, EffectKind.Stagger, EffectKind.Regen, EffectKind.Thorns, EffectKind.Cleanse, EffectKind.Charge })
            {
                foreach (TargetMode target in new[] { TargetMode.EnemyFront, TargetMode.EnemyAll, TargetMode.Self, TargetMode.AllyLowestHp, TargetMode.AllyAll })
                {
                    Assert.DoesNotThrow(() => TestData.Effect(kind, target), $"{kind} to {target}");
                }
            }

            Assert.IsTrue(ItemEffect.Hurts(EffectKind.Bleed) && ItemEffect.Hurts(EffectKind.Stagger) && ItemEffect.Hurts(EffectKind.Damage) && ItemEffect.Hurts(EffectKind.Burn));
            Assert.IsFalse(ItemEffect.Hurts(EffectKind.Regen) || ItemEffect.Hurts(EffectKind.Thorns) || ItemEffect.Hurts(EffectKind.Cleanse) || ItemEffect.Hurts(EffectKind.Charge));

            Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Bleed, TargetMode.EnemyFront, 1, 1, 2), "A bleed has one value, not a range.");
            Assert.Throws<DataException>(() => new ItemEffect(EffectKind.Stagger, TargetMode.EnemyFront, 1, 0, 0), "At least one millisecond.");

            foreach (EffectKind stacks in new[] { EffectKind.Burn, EffectKind.Bleed, EffectKind.Regen, EffectKind.Cleanse })
            {
                Assert.IsTrue(ItemEffect.CountsStacks(stacks) && ItemEffect.CountsWhole(stacks), stacks.ToString());
            }

            foreach (EffectKind milliseconds in new[] { EffectKind.Stagger, EffectKind.Charge })
            {
                Assert.IsTrue(!ItemEffect.CountsStacks(milliseconds) && ItemEffect.CountsWhole(milliseconds), milliseconds.ToString());
            }

            foreach (EffectKind hundredths in new[] { EffectKind.Damage, EffectKind.Heal, EffectKind.Shield, EffectKind.Thorns })
            {
                Assert.IsFalse(ItemEffect.CountsWhole(hundredths), hundredths.ToString());
            }
        }

        [Test]
        public void Transform_ReadsTheVocabularysValuesWhole_AndTheThornsInHundredths()
        {
            // Stacks and milliseconds are whole numbers in the data; thorns are hundredths, as a shield. A bleed aimed at an ally is refused.
            string items = TestCsv.Items
                + "saw,톱,Saw,Weapon,2,1,2000,front:2,Damage,EnemyFront,1,1-2,Bleed,EnemyFront,1,2,0,,0,true,,,5,Physical\n"
                + "frost,서리,Frost,Attack,2,1,4000,all,Stagger,EnemyFront,1,800,,,,,0,,0,false,,,,Magic\n"
                + "drum,북,Drum,Support,2,2,6000,back:3,Charge,AllyAll,,500,Regen,AllyAll,,2,0,,0,false,,,,\n"
                + "briar,가시,Briar,Armor,1,1,5000,all,Thorns,Self,,1.5,,,,,0,,0,false,,,,\n";
            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, items)).Data;

            ItemData saw = data.Items.Get("saw");
            Assert.AreEqual(EffectKind.Bleed, saw.Effects[1].Kind);
            Assert.AreEqual(2, saw.Effects[1].Min, "Stacks, whole.");
            Assert.AreEqual(800, data.Items.Get("frost").Effects[0].Min, "Milliseconds, whole.");
            Assert.AreEqual(500, data.Items.Get("drum").Effects[0].Min);
            Assert.AreEqual(2, data.Items.Get("drum").Effects[1].Min);
            Assert.AreEqual(150, data.Items.Get("briar").Effects[0].Min, "Thorns in hundredths, as a shield.");

            // A bleed on one's own side is allowed (stage 45); thorns of nothing are not.
            Assert.DoesNotThrow(() => StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item, items.Replace("Bleed,EnemyFront,1,2,0", "Bleed,Self,,2,0"))));
            var exception = Assert.Throws<DataTransformException>(() => StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Item,
                items.Replace("Thorns,Self,,1.5", "Thorns,Self,,0"))));
            StringAssert.Contains("must be at least 1", exception.Errors[0]);
        }

        [Test]
        public void Transform_ReadsACleansePotionsStacksWhole()
        {
            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Potion, TestCsv.Potions + "purge,정화제,Purge,Cleanse,3,2,,8\n")).Data;
            PotionData purge = data.Potions.Get("purge");
            Assert.AreEqual(PotionEffect.Cleanse, purge.Effect);
            Assert.AreEqual(3, purge.Magnitude, "Stacks, not hundredths.");
            Assert.AreEqual(1400, data.Potions.Get("tonic").Magnitude, "A heal stays in hundredths.");
        }

        // ---- The regen potion (stage 46) ----

        [Test]
        public void ARegenPotion_PutsItsStacksOnTheMercenary_WhichHealTickByTick_AndStopABleed()
        {
            // Stage 46 (Docs/Design/02 §10): the potion's stacks are an item's regen — logged by the potion, healing stacks x RegenHealPerStack (1 in
            // the test balance) at each tick, one stack fewer each tick; and its first tick, a heal, stops a bleed like any heal.
            var potion = new PotionData("salve", TestData.Text("salve"), PotionEffect.Regen, 3, 1);
            BattleEngine battle = Battle(
                new[] { TestData.Mercenary("m", 1, 4000, TestData.Attack(100000, 1)) },
                new[] { TestData.Enemy("e", 1, 100000, Opening("ember", EffectKind.Burn, 5), Opening("knife", EffectKind.Bleed, 4)) },
                500,
                null,
                potion);

            Assert.IsTrue(battle.TryUsePotion(0, 0));
            BattleEvent applied = Of(battle, BattleEventKind.RegenApplied).Single();
            Assert.AreEqual("salve", applied.Id, "Logged by the potion.");
            Assert.IsTrue(applied.Source.IsNone);
            Assert.AreEqual(3, applied.A);
            Assert.AreEqual(3, battle.Party[0].Regen);

            battle.AdvanceTo(3500);
            CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, Of(battle, BattleEventKind.Healed, BattleEvent.CauseRegen).Select(e => e.TimeMs), "Three ticks: 3, 2, 1 stacks.");
            CollectionAssert.AreEqual(new[] { 3, 2, 1 }, Of(battle, BattleEventKind.Healed, BattleEvent.CauseRegen).Select(e => e.A));
            Assert.AreEqual(0, battle.Party[0].Regen);
            Assert.AreEqual(0, battle.Party[0].Bleed, "The first regen tick, a heal, stopped the bleed (02 §5).");
            // Ticks at 1, 2, 3 s in the order burn -> bleed -> regen: burn 5 + 4 + 3, one bleed tick of 4 before the regen stopped it, regen 3 + 2 + 1.
            Assert.AreEqual(4000 - 12 - 4 + 6, battle.Party[0].Hp);
        }

        [Test]
        public void Transform_ReadsARegenPotionsStacksWhole()
        {
            StaticData data = StaticDataTransformer.Transform(TestCsv.With(StaticDataFiles.Potion, TestCsv.Potions + "salve,연고,Salve,Regen,8,2,,8\n")).Data;
            PotionData salve = data.Potions.Get("salve");
            Assert.AreEqual(PotionEffect.Regen, salve.Effect);
            Assert.AreEqual(8, salve.Magnitude, "Stacks, not hundredths.");
        }

        [Test]
        public void TheBalance_HasTheTickValues_InHundredths()
        {
            BalanceData balance = TestData.Balance();
            Assert.AreEqual(1, balance.BleedDamagePerStack);
            Assert.AreEqual(1, balance.RegenHealPerStack);
            Assert.IsTrue(BalanceData.IsCenti("BleedDamagePerStack") && BalanceData.IsCenti("RegenHealPerStack"), "Written as hundredths in the data, like the burn's.");
        }
    }
}
