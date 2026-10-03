using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The battle rules of Docs/Design/02_Combat_System.md, one rule per test.</summary>
    public sealed class BattleEngineTests
    {
        static BattleEngine Battle(BalanceData balance, BattleUnitSetup[] party, BattleUnitSetup[] enemies, Action<BattleSetup> configure = null)
        {
            BattleSetup setup = TestData.Setup(balance, party, enemies);
            configure?.Invoke(setup);
            return new BattleEngine(setup);
        }

        static List<BattleEvent> Of(BattleEngine battle, BattleEventKind kind)
        {
            return battle.Events.Where(e => e.Kind == kind).ToList();
        }

        /// <param name="rows">Where the owner must stand for the item to work. Everywhere when omitted.</param>
        /// <param name="reach">How many enemies it hits from the end its target counts from. One when omitted.</param>
        static EquippedItem Item(
            string id,
            int cooldownMs,
            EffectKind kind,
            TargetMode target,
            int grade,
            ItemCategory category = ItemCategory.Weapon,
            RowSpan rows = null,
            int reach = TestData.DefaultReach)
        {
            return new EquippedItem(TestData.Item(id, cooldownMs, kind, target, 100, category, rows, reach: reach), grade);
        }

        /// <summary>Enemies that do nothing, one per row from row 1, with the given HP.</summary>
        static BattleUnitSetup[] IdleLine(params int[] hp)
        {
            return hp.Select((value, index) => TestData.Enemy("e" + index, index + 1, value)).ToArray();
        }

        static BattleUnitSetup IdleEnemy(int hp, int row = 1)
        {
            return TestData.Enemy("dummy", row, hp);
        }

        /// <summary>A mercenary who does nothing, to stand in a row.</summary>
        static BattleUnitSetup Idle(string id, int row, int hp = 100)
        {
            return TestData.Mercenary(id, row, hp);
        }

        // ---- Time and cooldowns -------------------------------------------------------------

        [Test]
        public void Item_FiresFirstAtItsCooldown_ThenEveryCooldown()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 5))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(999);
            Assert.AreEqual(0, Of(battle, BattleEventKind.ItemActivated).Count);
            Assert.AreEqual(999, battle.TimeMs);

            battle.AdvanceTo(1000);
            Assert.AreEqual(1, Of(battle, BattleEventKind.ItemActivated).Count);
            Assert.AreEqual(95, battle.Enemies[0].Hp);

            battle.AdvanceTo(3500);
            CollectionAssert.AreEqual(new[] { 1000, 2000, 3000 }, Of(battle, BattleEventKind.ItemActivated).Select(e => e.TimeMs));
            Assert.AreEqual(85, battle.Enemies[0].Hp);
        }

        [Test]
        public void SameTimestamp_PartyActsBeforeEnemies_InUnitThenSlotOrder()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("front", 1, 100, TestData.Attack(1000, 1, "f0"), TestData.Attack(1000, 1, "f1")),
                    TestData.Mercenary("rear", 2, 100, TestData.Attack(1000, 1, "r0"))),
                TestData.Units(TestData.Enemy("enemy", 1, 100, TestData.Attack(1000, 1, "e0"))));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(new[] { "f0", "f1", "r0", "e0" }, Of(battle, BattleEventKind.ItemActivated).Select(e => e.Id));
        }

        [Test]
        public void BurnTick_IsProcessedBeforeItemsOfTheSameTimestamp()
        {
            // Burn item: grade 3 -> 3 stacks, fires at 1500 and 3000. Ticks are at 1000, 2000, 3000...
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("torch", 1500, EffectKind.Burn, TargetMode.EnemyFront, 3))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(2999);
            Assert.AreEqual(97, battle.Enemies[0].Hp, "The 2000 tick deals 3.");
            Assert.AreEqual(2, battle.Enemies[0].Burn, "A tick lowers burn by 1.");

            battle.AdvanceTo(3000);
            Assert.AreEqual(95, battle.Enemies[0].Hp, "The 3000 tick deals the 2 stacks left, before the item adds more.");
            Assert.AreEqual(4, battle.Enemies[0].Burn, "1 left after the tick, plus 3 new.");
        }

        [Test]
        public void EnemyCooldown_IsScaledByAffinityAndRounded_PartyIsNot()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(2800, 1, "sword"))),
                TestData.Units(TestData.Enemy("e", 1, 100, TestData.Attack(2800, 1, "blade"))),
                setup => setup.EnemyCooldownPermille = -80);

            battle.AdvanceTo(3000);

            List<BattleEvent> fired = Of(battle, BattleEventKind.ItemActivated);
            Assert.AreEqual(2576, fired.Single(e => e.Id == "blade").TimeMs, "2800 x 0.92 = 2576");
            Assert.AreEqual(2800, fired.Single(e => e.Id == "sword").TimeMs);
        }

        [Test]
        public void EnemyCooldown_NeverDropsBelowMinCooldown()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("MinCooldownMs", 200)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(TestData.Enemy("e", 1, 100, TestData.Attack(300, 1))),
                setup => setup.EnemyCooldownPermille = -900);

            Assert.AreEqual(200, battle.Enemies[0].Items[0].CooldownMs);
        }

        [Test]
        public void AdvanceTo_WhenTimeGoesBackwards_Throws()
        {
            BattleEngine battle = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100)), TestData.Units(IdleEnemy(100)));
            battle.AdvanceTo(500);

            Assert.Throws<ArgumentOutOfRangeException>(() => battle.AdvanceTo(499));
        }

        // ---- Items ---------------------------------------------------------------------------

        [TestCase(8, 45, 3)]
        [TestCase(12, 100, 12)]
        [TestCase(1, 45, 1)]
        [TestCase(10, 110, 11)]
        public void EffectSize_IsGradeTimesPowerPercent_RoundedDown_AtLeastOne(int grade, int powerPercent, int expected)
        {
            var item = new EquippedItem(TestData.Item("x", 1000, EffectKind.Damage, TargetMode.EnemyFront, powerPercent), grade);
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, item)),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            Assert.AreEqual(100 - expected, battle.Enemies[0].Hp);
        }

        [Test]
        public void Item_WhoseOwnerStandsOutsideItsSpan_NeverFires()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    Idle("front", 1),
                    TestData.Mercenary("a", 2, 100, Item("axe", 1000, EffectKind.Damage, TargetMode.EnemyFront, 9, rows: RowSpan.Front(1)))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(10000);

            Assert.IsFalse(battle.Party[1].Items[0].Active);
            Assert.AreEqual(0, Of(battle, BattleEventKind.ItemActivated).Count);
            Assert.AreEqual(100, battle.Enemies[0].Hp);
        }

        [Test]
        public void Item_CountedFromTheFront_WorksInRows1ToN_WhateverTheLineLength()
        {
            EquippedItem Spear()
            {
                return Item("spear", 1000, EffectKind.Damage, TargetMode.EnemyFront, 1, rows: RowSpan.Front(2));
            }

            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("one", 1, 100, Spear()),
                    TestData.Mercenary("two", 2, 100, Spear()),
                    TestData.Mercenary("three", 3, 100, Spear()),
                    TestData.Mercenary("four", 4, 100, Spear())),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(new[] { true, true, false, false }, battle.Party.Select(u => u.Items[0].Active));
            Assert.AreEqual(98, battle.Enemies[0].Hp, "Only the front two fired.");
        }

        [TestCase(4, new[] { false, true, true, true })]
        [TestCase(3, new[] { true, true, true })]
        [TestCase(2, new[] { true, true })]
        public void Item_CountedFromTheBack_WorksInTheRearNOfTheLivingLine(int partySize, bool[] expectedActive)
        {
            // "back:3" is rows 2..4 of four, and everywhere once three or fewer stand.
            BattleUnitSetup[] party = Enumerable.Range(1, partySize)
                .Select(row => TestData.Mercenary("m" + row, row, 100, Item("staff", 1000, EffectKind.Damage, TargetMode.EnemyFront, 1, rows: RowSpan.Back(3))))
                .ToArray();

            BattleEngine battle = Battle(TestData.Balance(), party, TestData.Units(IdleEnemy(100)));

            CollectionAssert.AreEqual(expectedActive, battle.Party.Select(u => u.Items[0].Active));
        }

        [Test]
        public void Item_WithTwoEffects_AppliesBothInOrder()
        {
            ItemData mace = TestData.Item("mace", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, second: TestData.Effect(EffectKind.Shield, TargetMode.Self, 50));
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, new EquippedItem(mace, 10))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            Assert.AreEqual(90, battle.Enemies[0].Hp);
            Assert.AreEqual(5, battle.Party[0].Shield);
        }

        // ---- Targeting -----------------------------------------------------------------------

        [Test]
        public void EnemyFront_WithReach1_HitsOnlyTheFrontEnemy_ThenWhoeverAdvancesIntoRow1()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 40))),
                IdleLine(80, 100, 100));

            battle.AdvanceTo(1000);
            CollectionAssert.AreEqual(new[] { 40, 100, 100 }, battle.Enemies.Select(e => e.Hp));

            battle.AdvanceTo(2000);
            Assert.IsFalse(battle.Enemies[0].Alive);

            battle.AdvanceTo(3000);
            CollectionAssert.AreEqual(new[] { 0, 60, 100 }, battle.Enemies.Select(e => e.Hp), "The one who advanced into row 1.");
        }

        [TestCase(2, new[] { 93, 93, 100, 100 })]
        [TestCase(3, new[] { 93, 93, 93, 100 })]
        [TestCase(4, new[] { 93, 93, 93, 93 })]
        public void EnemyFront_HitsAsManyAsItsReach_CountedFromTheFront(int reach, int[] expectedHp)
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("sweep", 1000, EffectKind.Damage, TargetMode.EnemyFront, 7, reach: reach))),
                IdleLine(100, 100, 100, 100));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(expectedHp, battle.Enemies.Select(e => e.Hp));
            CollectionAssert.AreEqual(
                Enumerable.Range(0, reach),
                Of(battle, BattleEventKind.Damaged).Select(e => e.Target.Index),
                "Applied from the front back.");
        }

        [Test]
        public void EnemyBack_WithReach1_HitsTheRearmostEnemy_WhoeverThatIsNow()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyBack, 30))),
                IdleLine(100, 100, 30));

            battle.AdvanceTo(1000);
            CollectionAssert.AreEqual(new[] { 100, 100, 0 }, battle.Enemies.Select(e => e.Hp));
            Assert.AreEqual(0, Of(battle, BattleEventKind.RowsAdvanced).Count, "Nobody stood behind the rearmost.");

            battle.AdvanceTo(2000);
            CollectionAssert.AreEqual(new[] { 100, 70, 0 }, battle.Enemies.Select(e => e.Hp), "Row 2 is the rearmost now.");
        }

        [TestCase(2, new[] { 100, 100, 93, 93 })]
        [TestCase(3, new[] { 100, 93, 93, 93 })]
        public void EnemyBack_HitsAsManyAsItsReach_CountedFromTheBack(int reach, int[] expectedHp)
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("volley", 1000, EffectKind.Damage, TargetMode.EnemyBack, 7, reach: reach))),
                IdleLine(100, 100, 100, 100));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(expectedHp, battle.Enemies.Select(e => e.Hp));
            CollectionAssert.AreEqual(
                Enumerable.Range(0, reach).Select(i => 3 - i),
                Of(battle, BattleEventKind.Damaged).Select(e => e.Target.Index),
                "Applied from the rearmost forward.");
        }

        [TestCase(TargetMode.EnemyFront)]
        [TestCase(TargetMode.EnemyBack)]
        public void AReachDeeperThanTheLivingLine_HitsEveryoneAliveOnce_AndNeverAnEmptyRow(TargetMode target)
        {
            // Three enemies, the middle one killed at 700: two are left, in rows 1 and 2. Reach 3 finds both and nothing else.
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("opener", 1, 100, Item("opener", 700, EffectKind.Damage, TargetMode.EnemyFront, 50, reach: 2)),
                    TestData.Mercenary("a", 2, 100, Item("wide", 1200, EffectKind.Damage, target, 7, reach: 3))),
                IdleLine(100, 50, 100));

            battle.AdvanceTo(700);
            Assert.IsFalse(battle.Enemies[1].Alive);
            CollectionAssert.AreEqual(new[] { 1, 2, 2 }, battle.Enemies.Select(e => e.Row), "The dead one keeps the row it died in; the one behind advanced into it.");

            battle.AdvanceTo(1200);

            List<BattleEvent> hits = Of(battle, BattleEventKind.Damaged).Where(e => e.Id == "wide").ToList();
            CollectionAssert.AreEquivalent(new[] { 0, 2 }, hits.Select(e => e.Target.Index), "Each living enemy once; the dead one is not a target.");
        }

        [Test]
        public void EnemyAll_HitsEveryLivingEnemy()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("fire", 1000, EffectKind.Damage, TargetMode.EnemyAll, 7))),
                IdleLine(100, 100, 100, 100));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(new[] { 93, 93, 93, 93 }, battle.Enemies.Select(e => e.Hp));
        }

        [Test]
        public void AllyLowestHp_PicksLowestRatio_AndEarlierUnitOnTie()
        {
            EquippedItem heal = Item("heal", 1000, EffectKind.Heal, TargetMode.AllyLowestHp, 10, ItemCategory.Support);
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("half", 1, 50).WithMaxHp(100),
                    TestData.Mercenary("sixty", 2, 30).WithMaxHp(50),
                    TestData.Mercenary("healer", 3, 80, heal)),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);
            Assert.AreEqual(60, battle.Party[0].Hp, "50% is lower than 60%.");

            battle.AdvanceTo(2000);
            Assert.AreEqual(70, battle.Party[0].Hp, "60% ties with 60%; the earlier unit wins.");
            Assert.AreEqual(30, battle.Party[1].Hp);
        }

        [Test]
        public void EnemyItems_TargetThePartyWithTheSameRules()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("front", 1, 100), TestData.Mercenary("rear", 2, 100)),
                TestData.Units(TestData.Enemy("archer", 1, 100, Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyBack, 9))));

            battle.AdvanceTo(1000);

            Assert.AreEqual(100, battle.Party[0].Hp);
            Assert.AreEqual(91, battle.Party[1].Hp);
        }

        // ---- Advancing -----------------------------------------------------------------------

        [Test]
        public void WhenAUnitDies_EveryoneBehindItMovesOneRowForward()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 50))),
                TestData.Units(TestData.Enemy("front", 1, 50), TestData.Enemy("middle", 2, 100), TestData.Enemy("back", 3, 100)));

            battle.AdvanceTo(1000);

            Assert.IsFalse(battle.Enemies[0].Alive);
            Assert.AreEqual(1, battle.Enemies[0].Row, "The dead stay where they fell.");
            Assert.AreEqual(1, battle.Enemies[1].Row);
            Assert.AreEqual(2, battle.Enemies[2].Row);
            CollectionAssert.AreEqual(new[] { 0, 1, 2 }, battle.Enemies.Select(u => u.Index), "Unit order does not change.");

            BattleEvent advanced = Of(battle, BattleEventKind.RowsAdvanced).Single();
            Assert.AreEqual(1000, advanced.TimeMs);
            Assert.AreEqual((int)BattleSide.Enemy, advanced.A);
            Assert.AreEqual(1, advanced.B, "The row that emptied.");
            Assert.Less(battle.Events.ToList().FindIndex(e => e.Kind == BattleEventKind.Died), battle.Events.ToList().IndexOf(advanced), "The advance follows the death.");
        }

        [Test]
        public void WhenAUnitInTheMiddleDies_OnlyThoseBehindItMove()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("spear", 1000, EffectKind.Damage, TargetMode.EnemyFront, 50, reach: 2))),
                TestData.Units(TestData.Enemy("front", 1, 100), TestData.Enemy("middle", 2, 50), TestData.Enemy("back", 3, 100), TestData.Enemy("last", 4, 100)));

            battle.AdvanceTo(1000);

            Assert.IsFalse(battle.Enemies[1].Alive);
            CollectionAssert.AreEqual(new[] { 1, 2, 2, 3 }, battle.Enemies.Select(e => e.Row), "The front stays; the two behind the dead one move up.");
            Assert.AreEqual(2, Of(battle, BattleEventKind.RowsAdvanced).Single().B);
        }

        [Test]
        public void WhenTheRearmostUnitDies_NobodyMoves()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyBack, 50))),
                TestData.Units(TestData.Enemy("front", 1, 100), TestData.Enemy("back", 2, 50)));

            battle.AdvanceTo(1000);

            Assert.IsFalse(battle.Enemies[1].Alive);
            Assert.AreEqual(1, battle.Enemies[0].Row);
            Assert.AreEqual(0, Of(battle, BattleEventKind.RowsAdvanced).Count);
        }

        [Test]
        public void AMercenaryAtDeathsDoor_KeepsTheRow_UntilDeath()
        {
            // Hits at 500 (to 0 HP), then 1000, 1500, 2000 during a long grace: the third counted hit breaks it and kills.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 100000), ("DogGraceBreakHits", 3), ("DogDeathChancePercent", 100)),
                TestData.Units(Idle("front", 1, 1), Idle("behind", 2)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(1999);
            Assert.IsTrue(battle.Party[0].InDog);
            Assert.AreEqual(2, battle.Party[1].Row, "At death's door is not dead.");
            Assert.AreEqual(100, battle.Party[1].Hp, "Row 1 still shields the one behind.");

            battle.AdvanceTo(2000);
            Assert.IsFalse(battle.Party[0].Alive);
            Assert.AreEqual(1, battle.Party[1].Row);
            Assert.AreEqual((int)BattleSide.Party, Of(battle, BattleEventKind.RowsAdvanced).Single().A);

            battle.AdvanceTo(2500);
            Assert.AreEqual(99, battle.Party[1].Hp, "Now in row 1, the next hit lands on them.");
        }

        [Test]
        public void TheAdvance_HappensBeforeTheNextEffectChoosesItsTarget()
        {
            // Two effects of one item, both aimed at the front enemy: the first kills row 1, the second hits whoever advanced.
            ItemData doubleStrike = TestData.Item("double", 1000, EffectKind.Damage, TargetMode.EnemyFront, 100, second: TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 50));
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, new EquippedItem(doubleStrike, 20))),
                TestData.Units(TestData.Enemy("front", 1, 20), TestData.Enemy("behind", 2, 100)));

            battle.AdvanceTo(1000);

            Assert.IsFalse(battle.Enemies[0].Alive);
            Assert.AreEqual(90, battle.Enemies[1].Hp);
        }

        [Test]
        public void TargetsOfOneEffect_AreChosenBeforeItIsApplied()
        {
            // Reach 2 from the front picks rows 1 and 2. Row 1 dies and row 2 advances, but each is hit exactly once.
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("spear", 1000, EffectKind.Damage, TargetMode.EnemyFront, 20, reach: 2))),
                TestData.Units(TestData.Enemy("front", 1, 20), TestData.Enemy("middle", 2, 100), TestData.Enemy("back", 3, 100)));

            battle.AdvanceTo(1000);

            CollectionAssert.AreEqual(new[] { 0, 80, 100 }, battle.Enemies.Select(e => e.Hp));
            CollectionAssert.AreEqual(new[] { 1, 1, 2 }, battle.Enemies.Select(e => e.Row));
        }

        [Test]
        public void AnItem_StartsAFreshCooldown_WhenItsOwnerAdvancesIntoItsSpan()
        {
            // The front mercenary falls at 500 and dies at 1000. The axe (front row only, cooldown 700) was idle in row 2.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(
                    Idle("front", 1, 1),
                    TestData.Mercenary("reserve", 2, 100, Item("axe", 700, EffectKind.Damage, TargetMode.EnemyFront, 9, rows: RowSpan.Front(1)))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(999);
            Assert.IsFalse(battle.Party[1].Items[0].Active);

            battle.AdvanceTo(1000);
            Assert.IsTrue(battle.Party[1].Items[0].Active);
            Assert.AreEqual(1700, battle.Party[1].Items[0].NextFireMs, "A whole cooldown from the moment of the advance.");

            battle.AdvanceTo(1699);
            Assert.AreEqual(0, Of(battle, BattleEventKind.ItemActivated).Count(e => e.Id == "axe"));

            battle.AdvanceTo(2400);
            CollectionAssert.AreEqual(new[] { 1700, 2400 }, Of(battle, BattleEventKind.ItemActivated).Where(e => e.Id == "axe").Select(e => e.TimeMs));
        }

        [Test]
        public void AnItemCountedFromTheBack_KeepsWorking_WhenItsOwnerIsPulledToTheFront()
        {
            // The staff works in the rear two. Its owner stands second of two, and alone in row 1 after the
            // front dies at 1000: still within the rear two of a line of one. The cooldown is not touched.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(
                    Idle("front", 1, 1),
                    TestData.Mercenary("healer", 2, 100, Item("staff", 400, EffectKind.Shield, TargetMode.Self, 5, ItemCategory.Support, RowSpan.Back(2)))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(2000);

            Assert.AreEqual(1, battle.Party[1].Row);
            Assert.IsTrue(battle.Party[1].Items[0].Active);
            CollectionAssert.AreEqual(new[] { 400, 800, 1200, 1600, 2000 }, Of(battle, BattleEventKind.ItemActivated).Where(e => e.Id == "staff").Select(e => e.TimeMs));
        }

        [Test]
        public void WhenTheRearmostUnitDies_AnItemCountedFromTheBack_CanTurnOnForTheFrontUnit()
        {
            // The front enemy's bow works in the rear two: idle while three stand, on from the moment the
            // party's shot kills the rearmost enemy and the line is two long. Nobody moved; the line got shorter.
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("shot", 1000, EffectKind.Damage, TargetMode.EnemyBack, 9))),
                TestData.Units(
                    TestData.Enemy("front", 1, 100, Item("bow", 600, EffectKind.Damage, TargetMode.EnemyFront, 9, rows: RowSpan.Back(2))),
                    TestData.Enemy("middle", 2, 100),
                    TestData.Enemy("rear", 3, 5)));

            battle.AdvanceTo(999);
            Assert.IsFalse(battle.Enemies[0].Items[0].Active);
            Assert.AreEqual(100, battle.Party[0].Hp);

            battle.AdvanceTo(1000);
            Assert.IsFalse(battle.Enemies[2].Alive);
            Assert.AreEqual(0, Of(battle, BattleEventKind.RowsAdvanced).Count, "Nobody stood behind the dead.");
            Assert.IsTrue(battle.Enemies[0].Items[0].Active);
            Assert.AreEqual(1600, battle.Enemies[0].Items[0].NextFireMs, "A whole cooldown from the moment the line shortened.");

            battle.AdvanceTo(1600);
            Assert.AreEqual(91, battle.Party[0].Hp);
        }

        [Test]
        public void AnItem_KeepsItsCooldown_WhenItsOwnerAdvancesWithinItsSpan()
        {
            // The owner moves from row 3 to row 2 at 1000; the bow (rear two) works in both, so it still fires at 1500.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(
                    Idle("front", 1, 1),
                    Idle("middle", 2),
                    TestData.Mercenary("archer", 3, 100, Item("bow", 1500, EffectKind.Damage, TargetMode.EnemyFront, 9, rows: RowSpan.Back(2)))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(1500);

            Assert.AreEqual(2, battle.Party[2].Row);
            Assert.AreEqual(1500, Of(battle, BattleEventKind.ItemActivated).Single(e => e.Id == "bow").TimeMs);
        }

        [Test]
        public void AnItemThatTurnsOnAtTheMomentOfADeath_DoesNotFireThatMoment_ButAWholeCooldownLater()
        {
            // The archer's bow works in the rearmost row only, and the archer stands in front of a guard: idle.
            // The party's shot kills the guard at 1000; the archer is the rearmost of a line of one from then on.
            // Its cooldown starts at 1000, so it does not fire at 1000 even though a cooldown had passed.
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("shot", 1000, EffectKind.Damage, TargetMode.EnemyBack, 50))),
                TestData.Units(
                    TestData.Enemy("archer", 1, 100, Item("bow", 1000, EffectKind.Damage, TargetMode.EnemyFront, 9, rows: RowSpan.Back(1))),
                    TestData.Enemy("guard", 2, 50)));

            battle.AdvanceTo(1000);
            Assert.IsFalse(battle.Enemies[1].Alive);
            Assert.IsTrue(battle.Enemies[0].Items[0].Active);
            Assert.AreEqual(100, battle.Party[0].Hp, "Nothing fired the moment the bow turned on.");

            battle.AdvanceTo(2000);
            Assert.AreEqual(91, battle.Party[0].Hp);
            Assert.AreEqual(2000, Of(battle, BattleEventKind.ItemActivated).Single(e => e.Id == "bow").TimeMs);
        }

        [Test]
        public void ABattleWithAdvances_ReplaysToTheSameLog()
        {
            BattleSetup Setup()
            {
                return TestData.Setup(
                    TestData.Balance(("DogGraceMs", 1000), ("DogDeathChancePercent", 50)),
                    TestData.Units(
                        TestData.Mercenary("front", 1, 30, TestData.Attack(900, 20, "sword")),
                        TestData.Mercenary("middle", 2, 30, Item("spear", 1100, EffectKind.Damage, TargetMode.EnemyFront, 12, rows: RowSpan.Front(2), reach: 2)),
                        TestData.Mercenary("back", 3, 30, Item("bow", 1300, EffectKind.Damage, TargetMode.EnemyBack, 15, rows: RowSpan.Back(2)))),
                    TestData.Units(
                        TestData.Enemy("brute", 1, 120, TestData.Attack(700, 12, "club")),
                        TestData.Enemy("archer", 2, 60, Item("shot", 1000, EffectKind.Damage, TargetMode.EnemyBack, 9, rows: RowSpan.Back(2))),
                        TestData.Enemy("shaman", 3, 60, Item("hex", 1500, EffectKind.Burn, TargetMode.EnemyFront, 3, ItemCategory.Support, RowSpan.Back(2)))));
            }

            var whole = new BattleEngine(Setup());
            whole.RunToEnd();

            var stepped = new BattleEngine(Setup());
            for (int time = 37; stepped.Result == BattleResult.Ongoing; time += 37)
            {
                stepped.AdvanceTo(time);
            }

            BattleEngine replayed = BattleEngine.Replay(Setup(), whole.Inputs, whole.TimeMs);

            Assert.Greater(Of(whole, BattleEventKind.RowsAdvanced).Count, 0, "The battle is meant to have advances.");
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(stepped.Events));
            Assert.AreEqual(BattleLog.Hash(whole.Events), BattleLog.Hash(replayed.Events));
        }

        // ---- Damage, heal, shield, burn ------------------------------------------------------

        [Test]
        public void Damage_IsAbsorbedByShieldBeforeHp()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100)
                    .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self, 5)),
                TestData.Units(TestData.Enemy("e", 1, 100, TestData.Attack(1000, 8))));

            battle.AdvanceTo(1000);

            BattleEvent damaged = Of(battle, BattleEventKind.Damaged).Single();
            Assert.AreEqual(8, damaged.A);
            Assert.AreEqual(5, damaged.B, "Absorbed by shield.");
            Assert.AreEqual(97, damaged.C);
            Assert.AreEqual(0, battle.Party[0].Shield);
            Assert.AreEqual(97, battle.Party[0].Hp);
        }

        [Test]
        public void Heal_StopsAtMaxHp()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 95, Item("herb", 1000, EffectKind.Heal, TargetMode.Self, 10, ItemCategory.Support)).WithMaxHp(100)),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            Assert.AreEqual(100, battle.Party[0].Hp);
            Assert.AreEqual(5, Of(battle, BattleEventKind.Healed).Single().B, "Only the HP actually restored.");
        }

        [Test]
        public void Shield_StacksAndDoesNotDecay()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("ward", 1000, EffectKind.Shield, TargetMode.Self, 6, ItemCategory.Support))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(3999);

            Assert.AreEqual(18, battle.Party[0].Shield);
        }

        // ---- Death-or-Glory ------------------------------------------------------------------

        [Test]
        public void Enemy_DiesAtZeroHp_AndVictoryEndsTheBattle()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 50))),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(5000);

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.AreEqual(2000, battle.TimeMs, "Time stops when the battle ends.");
            Assert.IsFalse(battle.Enemies[0].Alive);
            Assert.AreEqual(1, Of(battle, BattleEventKind.Died).Count);
            Assert.AreEqual((int)BattleResult.Victory, Of(battle, BattleEventKind.BattleEnded).Single().A);
        }

        [Test]
        public void Mercenary_AtZeroHp_EntersDogAndKeepsFighting()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 3000)),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(900, 1))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 60))));

            battle.AdvanceTo(2000);

            BattleUnit mercenary = battle.Party[0];
            Assert.AreEqual(0, mercenary.Hp);
            Assert.IsTrue(mercenary.Alive);
            Assert.IsTrue(mercenary.InDog);
            Assert.AreEqual(5000, Of(battle, BattleEventKind.DogEntered).Single().A, "Grace ends DogGraceMs after entering.");
            Assert.AreEqual(0, Of(battle, BattleEventKind.DeathRolled).Count, "The hit that causes death's door is not rolled.");

            battle.AdvanceTo(2700);
            Assert.AreEqual(3, Of(battle, BattleEventKind.ItemActivated).Count(e => e.Source.Side == BattleSide.Party), "A mercenary at death's door still acts.");
        }

        [Test]
        public void Dog_HitsDuringGraceAreCounted_TheBreakingHitRolls()
        {
            // Hits at 500 (enters), 1000, 1500, 2000. With 3 hits to break, the 2000 hit breaks and rolls.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 10000), ("DogGraceBreakHits", 3), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 100))));

            battle.AdvanceTo(1999);
            Assert.AreEqual(2, battle.Party[0].GraceHits);
            Assert.AreEqual(0, Of(battle, BattleEventKind.DeathRolled).Count);
            Assert.IsTrue(battle.Party[0].Alive);

            battle.AdvanceTo(2000);
            BattleEvent broken = Of(battle, BattleEventKind.GraceBroken).Single();
            Assert.AreEqual(2000, broken.TimeMs);
            Assert.AreEqual(3, broken.A);
            Assert.AreEqual(2000, Of(battle, BattleEventKind.DeathRolled).Single().TimeMs);
            Assert.IsFalse(battle.Party[0].Alive);
            Assert.AreEqual(BattleResult.Defeat, battle.Result);
        }

        [Test]
        public void Dog_AfterGraceExpires_EveryHitRolls()
        {
            // Enters at 1000, grace until 2500. Hits at 2000 (counted), 3000 and 4000 (rolled).
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 1500), ("DogGraceBreakHits", 10), ("DogDeathChancePercent", 0)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 100))));

            battle.AdvanceTo(4000);

            CollectionAssert.AreEqual(new[] { 3000, 4000 }, Of(battle, BattleEventKind.DeathRolled).Select(e => e.TimeMs));
            Assert.AreEqual(0, Of(battle, BattleEventKind.GraceBroken).Count);
        }

        [Test]
        public void Dog_WithZeroDeathChance_NeverKills_WithFullChance_KillsOnFirstRoll()
        {
            BattleUnitSetup[] party = TestData.Units(TestData.Mercenary("a", 1, 10));
            BattleUnitSetup[] enemies = TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 10)));

            BattleEngine safe = Battle(TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 0)), party, enemies);
            safe.AdvanceTo(30000);
            Assert.IsTrue(safe.Party[0].Alive);
            Assert.IsTrue(Of(safe, BattleEventKind.DeathRolled).All(e => e.C == 0));
            Assert.Greater(Of(safe, BattleEventKind.DeathRolled).Count, 10);

            BattleEngine deadly = Battle(TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)), party, enemies);
            deadly.AdvanceTo(30000);
            Assert.AreEqual(1000, deadly.TimeMs, "Enters at 500, first roll at 1000 kills.");
            Assert.AreEqual(BattleResult.Defeat, deadly.Result);
        }

        [Test]
        public void Dog_RollUsesChanceFromBalance_AndLogsTheRoll()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 35)),
                TestData.Units(TestData.Mercenary("a", 1, 10)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 10))));

            battle.RunToEnd();

            foreach (BattleEvent roll in Of(battle, BattleEventKind.DeathRolled))
            {
                Assert.AreEqual(35, roll.A);
                Assert.That(roll.B, Is.InRange(0, 99));
                Assert.AreEqual(roll.B < 35 ? 1 : 0, roll.C);
            }

            Assert.AreEqual(1, Of(battle, BattleEventKind.DeathRolled).Count(e => e.C == 1));
        }

        [Test]
        public void Dog_HealAboveZeroExitsDog_AndReentryStartsANewGrace()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 3000), ("DogGraceBreakHits", 10), ("DogDeathChancePercent", 0)),
                TestData.Units(TestData.Mercenary("a", 1, 100, Item("herb", 1500, EffectKind.Heal, TargetMode.Self, 10, ItemCategory.Support))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 100))));

            battle.AdvanceTo(1500);
            Assert.AreEqual(10, battle.Party[0].Hp);
            Assert.IsFalse(battle.Party[0].InDog);
            Assert.AreEqual(1, Of(battle, BattleEventKind.DogExited).Count);

            battle.AdvanceTo(2000);
            Assert.IsTrue(battle.Party[0].InDog);
            CollectionAssert.AreEqual(new[] { 4000, 5000 }, Of(battle, BattleEventKind.DogEntered).Select(e => e.A), "Each entry starts its own grace.");
            Assert.AreEqual(0, battle.Party[0].GraceHits);
        }

        [Test]
        public void Dog_DamageFullyAbsorbedByShield_IsNotAHit()
        {
            // Would die on the first counted hit (break at 1, chance 100) if absorbed damage counted.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 100000), ("DogGraceBreakHits", 1), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 5, Item("ward", 1200, EffectKind.Shield, TargetMode.Self, 50, ItemCategory.Support))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 5))));

            battle.AdvanceTo(8000);

            Assert.IsTrue(battle.Party[0].Alive);
            Assert.IsTrue(battle.Party[0].InDog);
            Assert.AreEqual(0, battle.Party[0].GraceHits);
            Assert.AreEqual(0, Of(battle, BattleEventKind.DeathRolled).Count);
        }

        [Test]
        public void Dog_BurnTickCountsAsAHit()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 3)),
                TestData.Units(TestData.Enemy("e", 1, 1000, Item("hex", 500, EffectKind.Burn, TargetMode.EnemyFront, 3, ItemCategory.Support))));

            battle.RunToEnd();

            // 500: burn 3. 1000: tick 3 -> HP 0, death's door (hex adds 3 -> 5). 2000: tick is a hit -> roll -> dies.
            Assert.AreEqual(2000, battle.TimeMs);
            Assert.AreEqual(BattleEvent.CauseBurn, Of(battle, BattleEventKind.Damaged).Last().Id);
            Assert.AreEqual(BattleResult.Defeat, battle.Result);
        }

        [Test]
        public void MercenaryStartingAtZeroHp_StartsAtDeathsDoor()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 2000)),
                TestData.Units(TestData.Mercenary("a", 1, 0).WithMaxHp(100)),
                TestData.Units(IdleEnemy(100)));

            Assert.IsTrue(battle.Party[0].InDog);
            Assert.AreEqual(2000, battle.Party[0].GraceEndMs);
        }

        // ---- Passives ------------------------------------------------------------------------

        [TestCase(1, 20)]
        [TestCase(2, 0)]
        public void Passive_BattleStartShield_AppliesOnlyInItsSpan(int knightRow, int expectedShield)
        {
            BattleUnitSetup knight = TestData.Mercenary("knight", knightRow, 100)
                .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.InRows, PassiveEffect.Shield, PassiveTarget.Self, 20, RowSpan.Front(1));
            BattleUnitSetup other = Idle("other", knightRow == 1 ? 2 : 1);

            BattleEngine battle = Battle(
                TestData.Balance(),
                knightRow == 1 ? TestData.Units(knight, other) : TestData.Units(other, knight),
                TestData.Units(IdleEnemy(100)));

            Assert.AreEqual(expectedShield, battle.Party.Single(u => u.Setup.SourceId == "knight").Shield);
        }

        [Test]
        public void Passive_BattleStartShieldForAllAllies_ShieldsTheWholeParty()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("paladin", 1, 100)
                        .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.InRows, PassiveEffect.Shield, PassiveTarget.AllyAll, 8, RowSpan.Front(1)),
                    TestData.Mercenary("other", 2, 100)),
                TestData.Units(IdleEnemy(100)));

            CollectionAssert.AreEqual(new[] { 8, 8 }, battle.Party.Select(u => u.Shield));
            Assert.AreEqual(0, battle.Enemies[0].Shield);
        }

        [Test]
        public void Passive_OnHeal_ShieldsTheHealedTarget()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(
                    TestData.Mercenary("hurt", 1, 50).WithMaxHp(100),
                    TestData.Mercenary("bishop", 2, 90, Item("staff", 1000, EffectKind.Heal, TargetMode.AllyLowestHp, 10, ItemCategory.Support))
                        .WithPassive(PassiveTrigger.Heal, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.EventTarget, 10)),
                TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            Assert.AreEqual(60, battle.Party[0].Hp);
            Assert.AreEqual(10, battle.Party[0].Shield);
            Assert.AreEqual(0, battle.Party[1].Shield);
            Assert.AreEqual(BattleEvent.CausePassive, Of(battle, BattleEventKind.ShieldGained).Single().Id);
        }

        [Test]
        public void Passive_OnWeaponHit_BurnsTheTarget_ButNotForSupportItems()
        {
            BattleUnitSetup Spellblade(ItemCategory category)
            {
                return TestData.Mercenary("spellblade", 1, 100, Item("x", 1000, EffectKind.Damage, TargetMode.EnemyFront, 12, category))
                    .WithPassive(PassiveTrigger.WeaponHit, PassiveCondition.None, PassiveEffect.Burn, PassiveTarget.EventTarget, 3);
            }

            BattleEngine weapon = Battle(TestData.Balance(), TestData.Units(Spellblade(ItemCategory.Weapon)), TestData.Units(IdleEnemy(100)));
            weapon.AdvanceTo(1000);
            Assert.AreEqual(3, weapon.Enemies[0].Burn);

            BattleEngine support = Battle(TestData.Balance(), TestData.Units(Spellblade(ItemCategory.Support)), TestData.Units(IdleEnemy(100)));
            support.AdvanceTo(1000);
            Assert.AreEqual(0, support.Enemies[0].Burn);
        }

        [Test]
        public void Passive_WeaponPowerWhileInDog_AppliesOnlyAtDeathsDoor()
        {
            BattleUnitSetup Valkyrie(int hp)
            {
                return TestData.Mercenary("valkyrie", 1, hp, TestData.Attack(1000, 10)).WithMaxHp(100)
                    .WithPassive(PassiveTrigger.Always, PassiveCondition.SelfInDog, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 100);
            }

            BattleEngine healthy = Battle(TestData.Balance(), TestData.Units(Valkyrie(100)), TestData.Units(IdleEnemy(100)));
            healthy.AdvanceTo(1000);
            Assert.AreEqual(90, healthy.Enemies[0].Hp);

            BattleEngine atDeathsDoor = Battle(TestData.Balance(), TestData.Units(Valkyrie(0)), TestData.Units(IdleEnemy(100)));
            atDeathsDoor.AdvanceTo(1000);
            Assert.AreEqual(80, atDeathsDoor.Enemies[0].Hp);
        }

        [TestCase(1, 8)]
        [TestCase(2, 10)]
        [TestCase(3, 10)]
        public void Passive_WeaponPowerInASpan_UsesTheActualRowAndTheLivingLine(int archmageRow, int expectedDamage)
        {
            // The rear two of a line of three are rows 2 and 3.
            var party = new List<BattleUnitSetup>();
            for (int row = 1; row <= 3; row++)
            {
                party.Add(row == archmageRow
                    ? TestData.Mercenary("archmage", row, 100, TestData.Attack(1000, 8))
                        .WithPassive(PassiveTrigger.Always, PassiveCondition.InRows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 25, RowSpan.Back(2))
                    : Idle("other" + row, row));
            }

            BattleEngine battle = Battle(TestData.Balance(), party.ToArray(), TestData.Units(IdleEnemy(100)));

            battle.AdvanceTo(1000);

            Assert.AreEqual(100 - expectedDamage, battle.Enemies[0].Hp);
        }

        [Test]
        public void Passive_WeaponPowerInASpanCountedFromTheBack_IsKeptWhenTheOwnerIsPulledForward()
        {
            // The front mercenary (1 HP) falls at 500 and dies at 1000; the archmage moves from row 2 to row 1,
            // which is still within the rear two of a line of one.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(
                    Idle("front", 1, 1),
                    TestData.Mercenary("archmage", 2, 100, TestData.Attack(900, 8))
                        .WithPassive(PassiveTrigger.Always, PassiveCondition.InRows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 25, RowSpan.Back(2))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(900);
            Assert.AreEqual(990, battle.Enemies[0].Hp, "In row 2 of two: 8 x 1.25.");

            battle.AdvanceTo(1800);
            Assert.AreEqual(1, battle.Party[1].Row);
            Assert.AreEqual(980, battle.Enemies[0].Hp, "Alone in row 1: still 8 x 1.25.");
        }

        [Test]
        public void Passive_WeaponPowerInASpanCountedFromTheFront_IsLostWhenTheLineGrowsNoLonger_ButGainedWhenPulledIn()
        {
            // The front mercenary dies at 1000; the one in row 2 with a front-row passive moves into row 1 and gains it.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(
                    Idle("front", 1, 1),
                    TestData.Mercenary("duelist", 2, 100, TestData.Attack(900, 8))
                        .WithPassive(PassiveTrigger.Always, PassiveCondition.InRows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 25, RowSpan.Front(1))),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))));

            battle.AdvanceTo(900);
            Assert.AreEqual(992, battle.Enemies[0].Hp, "In row 2: 8.");

            battle.AdvanceTo(1800);
            Assert.AreEqual(1, battle.Party[1].Row);
            Assert.AreEqual(982, battle.Enemies[0].Hp, "In row 1: 8 x 1.25.");
        }

        // ---- Storm and battle end ------------------------------------------------------------

        [Test]
        public void Storm_GrowsEachTick_AndEndsABattleNobodyCanWin()
        {
            // Storm damage 5, 10, 15... from 1000. Enemy (100 HP) dies on the 6th tick; the mercenary
            // reaches 0 HP on that same tick but mercenaries are processed first and do not die from it.
            BattleEngine battle = Battle(
                TestData.Balance(("StormStartMs", 1000), ("StormTickMs", 1000), ("StormBaseDamage", 5), ("StormGrowth", 5)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(IdleEnemy(100)));

            battle.RunToEnd();

            CollectionAssert.AreEqual(new[] { 5, 10, 15, 20, 25, 30 }, Of(battle, BattleEventKind.StormTicked).Select(e => e.A));
            Assert.AreEqual(6000, battle.TimeMs);
            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.IsTrue(battle.Party[0].InDog);
        }

        [Test]
        public void NextStormDamage_IsWhatTheNextTickDeals()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("StormStartMs", 1000), ("StormTickMs", 1000), ("StormBaseDamage", 5), ("StormGrowth", 5)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(IdleEnemy(100)));

            Assert.AreEqual(5, battle.NextStormDamage);

            battle.AdvanceTo(1000);
            Assert.AreEqual(10, battle.NextStormDamage);

            battle.AdvanceTo(2000);
            Assert.AreEqual(15, battle.NextStormDamage);
        }

        // ---- Reading deaths from the log -------------------------------------------------------

        [Test]
        public void PartyDeaths_WhenTheGraceRanOut_NameTheLastHitAndTheRoll()
        {
            // 10 HP against 10 damage every second: at death's door at 1000, grace until 2500,
            // the hit at 2000 only counts, the hit at 3000 rolls and kills.
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 1500), ("DogGraceBreakHits", 3), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 10)),
                TestData.Units(TestData.Enemy("e", 1, 100, TestData.Attack(1000, 10, "claw"))));

            battle.RunToEnd();
            DeathCause death = BattleLog.PartyDeaths(battle.Events).Single();

            Assert.AreEqual(new UnitRef(BattleSide.Party, 0), death.Unit);
            Assert.AreEqual(1000, death.DogEnteredMs);
            Assert.AreEqual(3000, death.DiedMs);
            Assert.AreEqual(new UnitRef(BattleSide.Enemy, 0), death.LastHitSource);
            Assert.AreEqual("claw", death.LastHitCause);
            Assert.AreEqual(100, death.DeathChancePercent);
            Assert.AreEqual(Of(battle, BattleEventKind.DeathRolled).Single().B, death.DeathRoll);
            Assert.IsFalse(death.GraceWasBroken);
            Assert.AreEqual(0, death.GraceHits);
        }

        [Test]
        public void PartyDeaths_WhenHitsBrokeTheGrace_SayHowMany()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 60000), ("DogGraceBreakHits", 2), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 10)),
                TestData.Units(TestData.Enemy("e", 1, 100, TestData.Attack(1000, 10, "claw"))));

            battle.RunToEnd();
            DeathCause death = BattleLog.PartyDeaths(battle.Events).Single();

            Assert.IsTrue(death.GraceWasBroken);
            Assert.AreEqual(2, death.GraceHits);
            Assert.AreEqual(1000, death.DogEnteredMs);
            Assert.AreEqual(3000, death.DiedMs);
        }

        [Test]
        public void PartyDeaths_WhenBurnDealtTheLastHit_NameBurnWithNoSource()
        {
            // The enemy applies 5 burn at 500. Burn ticks at 1000 (5, to 0 HP), 2000 (4, counted), 3000 (3, grace over: roll).
            var ember = new EquippedItem(TestData.Item("ember", 500, EffectKind.Burn, TargetMode.EnemyFront, category: ItemCategory.Support), 5);
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 1500), ("DogGraceBreakHits", 3), ("DogDeathChancePercent", 100), ("BurnTickMs", 1000)),
                TestData.Units(TestData.Mercenary("a", 1, 5)),
                TestData.Units(TestData.Enemy("e", 1, 100, ember)));

            battle.AdvanceTo(600);
            Assert.AreEqual(5, battle.Party[0].Burn);
            battle.RunToEnd();
            DeathCause death = BattleLog.PartyDeaths(battle.Events).Single();

            Assert.IsTrue(death.LastHitSource.IsNone);
            Assert.AreEqual(BattleEvent.CauseBurn, death.LastHitCause);
        }

        [Test]
        public void PartyDeaths_ListOnlyMercenariesWhoDied()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 50))),
                TestData.Units(IdleEnemy(100)));

            battle.RunToEnd();

            Assert.AreEqual(BattleResult.Victory, battle.Result);
            Assert.IsEmpty(BattleLog.PartyDeaths(battle.Events), "The enemy died; nobody in the party did.");
        }

        [Test]
        public void WhenBothSidesFallAtTheSameMoment_ItIsADefeat()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("StormStartMs", 1000), ("StormBaseDamage", 100), ("StormGrowth", 0), ("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 10)),
                TestData.Units(IdleEnemy(150)));

            battle.RunToEnd();

            Assert.AreEqual(2000, battle.TimeMs);
            Assert.AreEqual(BattleResult.Defeat, battle.Result);
        }

        [Test]
        public void Defeat_WhenEveryMercenaryIsDead()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 10), TestData.Mercenary("b", 2, 10)),
                TestData.Units(TestData.Enemy("e", 1, 1000, Item("cleave", 1000, EffectKind.Damage, TargetMode.EnemyAll, 10))));

            battle.RunToEnd();

            Assert.AreEqual(BattleResult.Defeat, battle.Result);
            Assert.IsTrue(battle.Party.All(u => !u.Alive));
        }

        // ---- Player inputs -------------------------------------------------------------------

        [Test]
        public void Potion_HealsTheTarget_EmptiesTheSlot_AndStartsTheCooldown()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("PotionCooldownMs", 1500)),
                TestData.Units(TestData.Mercenary("a", 1, 50).WithMaxHp(100)),
                TestData.Units(IdleEnemy(100)),
                setup => setup.Potions = new[] { TestData.HealPotion(30), TestData.HealPotion(30) });

            battle.AdvanceTo(400);
            Assert.IsTrue(battle.TryUsePotion(0, 0));

            Assert.AreEqual(80, battle.Party[0].Hp);
            Assert.IsNull(battle.Potions[0]);
            Assert.IsNotNull(battle.Potions[1]);
            Assert.AreEqual(1900, battle.PotionReadyMs);
            Assert.IsFalse(battle.TryUsePotion(1, 0), "Still on cooldown.");

            battle.AdvanceTo(1900);
            Assert.IsTrue(battle.TryUsePotion(1, 0));
            Assert.AreEqual(100, battle.Party[0].Hp);
        }

        [Test]
        public void Potion_OnAMercenaryAtDeathsDoor_BringsThemBack()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 5000)),
                TestData.Units(TestData.Mercenary("a", 1, 10)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 10))),
                setup => setup.Potions = new[] { TestData.HealPotion(5) });

            battle.AdvanceTo(1000);
            Assert.IsTrue(battle.Party[0].InDog);

            Assert.IsTrue(battle.TryUsePotion(0, 0));

            Assert.IsFalse(battle.Party[0].InDog);
            Assert.AreEqual(5, battle.Party[0].Hp);
        }

        [Test]
        public void ShieldPotion_AddsShield()
        {
            BattleEngine battle = Battle(
                TestData.Balance(),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(IdleEnemy(100)),
                setup => setup.Potions = new[] { TestData.ShieldPotion(40) });

            Assert.IsTrue(battle.TryUsePotion(0, 0));

            Assert.AreEqual(40, battle.Party[0].Shield);
        }

        [Test]
        public void InvalidInputs_AreRejectedAndNotRecorded()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("DogGraceMs", 0), ("DogDeathChancePercent", 100)),
                TestData.Units(TestData.Mercenary("dies", 1, 1), TestData.Mercenary("lives", 2, 100)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(500, 1))),
                setup => setup.Potions = new[] { TestData.HealPotion(10) });
            battle.AdvanceTo(1000);
            Assert.IsFalse(battle.Party[0].Alive);

            Assert.IsFalse(battle.TryUsePotion(1, 1), "Empty slot.");
            Assert.IsFalse(battle.TryUsePotion(5, 1), "Slot out of range.");
            Assert.IsFalse(battle.TryUsePotion(0, 0), "Dead target.");
            Assert.IsFalse(battle.TryUsePotion(0, 7), "Target out of range.");

            Assert.AreEqual(0, battle.Inputs.Count);
            Assert.IsNotNull(battle.Potions[0]);
        }

        [Test]
        public void Retreat_WhenItSucceeds_EndsTheBattleAsRetreated()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("RetreatChancePercent", 100)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(IdleEnemy(100)));
            battle.AdvanceTo(700);

            Assert.IsTrue(battle.TryRetreat());

            Assert.AreEqual(BattleResult.Retreated, battle.Result);
            Assert.AreEqual(1, Of(battle, BattleEventKind.RetreatAttempted).Single().C);
            Assert.IsFalse(battle.TryRetreat(), "No input after the battle ended.");
            Assert.AreEqual(1, battle.Inputs.Count);
        }

        [Test]
        public void Retreat_WhenItFails_CannotBeTriedAgainUntilTheCooldownPasses()
        {
            BattleEngine battle = Battle(
                TestData.Balance(("RetreatChancePercent", 0), ("RetreatCooldownMs", 5000)),
                TestData.Units(TestData.Mercenary("a", 1, 100)),
                TestData.Units(IdleEnemy(100)));
            battle.AdvanceTo(1000);

            Assert.IsTrue(battle.TryRetreat(), "An attempt was made.");
            Assert.AreEqual(BattleResult.Ongoing, battle.Result);
            Assert.AreEqual(6000, battle.RetreatReadyMs);
            Assert.IsFalse(battle.CanRetreat);
            Assert.IsFalse(battle.TryRetreat());

            battle.AdvanceTo(6000);
            Assert.IsTrue(battle.TryRetreat());
            Assert.AreEqual(2, battle.Inputs.Count);
        }

        [Test]
        public void RetreatAttempts_DoNotChangeDeathRolls()
        {
            BattleEngine Build()
            {
                return Battle(
                    TestData.Balance(("RetreatChancePercent", 0), ("RetreatCooldownMs", 0), ("DogGraceMs", 0), ("DogDeathChancePercent", 5)),
                    TestData.Units(TestData.Mercenary("a", 1, 10)),
                    TestData.Units(TestData.Enemy("e", 1, 100000, TestData.Attack(500, 10))),
                    setup => setup.Seed = 77);
            }

            BattleEngine quiet = Build();
            quiet.AdvanceTo(20000);

            BattleEngine noisy = Build();
            for (int time = 250; time <= 20000 && noisy.Result == BattleResult.Ongoing; time += 250)
            {
                noisy.AdvanceTo(time);
                noisy.TryRetreat();
            }

            CollectionAssert.AreEqual(
                Of(quiet, BattleEventKind.DeathRolled).Select(e => (e.TimeMs, e.B)),
                Of(noisy, BattleEventKind.DeathRolled).Select(e => (e.TimeMs, e.B)));
            Assert.Greater(Of(noisy, BattleEventKind.RetreatAttempted).Count, 5);
        }

        [Test]
        public void Input_AtATimestamp_AppliesAfterTheAutomaticEventsOfThatTimestamp()
        {
            BattleSetup Setup()
            {
                BattleSetup setup = TestData.Setup(
                    TestData.Balance(),
                    TestData.Units(TestData.Mercenary("a", 1, 100).WithMaxHp(200)),
                    TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 30))));
                setup.Potions = new[] { TestData.HealPotion(50) };
                return setup;
            }

            var live = new BattleEngine(Setup());
            live.AdvanceTo(1000);
            Assert.IsTrue(live.TryUsePotion(0, 0));
            Assert.AreEqual(120, live.Party[0].Hp, "Hit for 30 first, then healed 50.");
            Assert.AreEqual(1000, live.Inputs[0].TimeMs);
            live.AdvanceTo(2500);

            BattleEngine replayed = BattleEngine.Replay(Setup(), live.Inputs, 2500);

            Assert.AreEqual(BattleLog.Hash(live.Events), BattleLog.Hash(replayed.Events));
            Assert.AreEqual(live.Party[0].Hp, replayed.Party[0].Hp);
        }

        [Test]
        public void ApplyRecordedInput_WhenNoLongerValid_Throws()
        {
            BattleEngine battle = Battle(TestData.Balance(), TestData.Units(TestData.Mercenary("a", 1, 100)), TestData.Units(IdleEnemy(100)));

            Assert.Throws<InvalidOperationException>(() => battle.ApplyRecordedInput(new BattleInput(500, BattleInputKind.UsePotion, 0, 0)));
        }

        // ---- Setup validation ----------------------------------------------------------------

        [Test]
        public void Constructor_RejectsInvalidSetups()
        {
            BalanceData balance = TestData.Balance(("PotionSlots", 1));
            BattleUnitSetup[] party = TestData.Units(TestData.Mercenary("a", 1, 100));
            BattleUnitSetup[] enemies = TestData.Units(IdleEnemy(100));

            Assert.Throws<ArgumentNullException>(() => new BattleEngine(null));
            Assert.Throws<ArgumentException>(() => Battle(balance, TestData.Units(), enemies), "Empty party.");
            Assert.Throws<ArgumentException>(() => Battle(balance, party, TestData.Units()), "No enemies.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, TestData.Units(TestData.Mercenary("r", 2, 100), TestData.Mercenary("f", 1, 100)), enemies),
                "Row 2 listed before row 1.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, TestData.Units(TestData.Mercenary("alone", 2, 100)), enemies),
                "Nobody in row 1.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, party, TestData.Units(IdleEnemy(100, 1), IdleEnemy(100, 3))),
                "Row 3 behind an empty row 2.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, TestData.Units(TestData.Mercenary("a", 0, 100)), enemies),
                "Row 0.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, party, TestData.Units(IdleEnemy(100, 1), IdleEnemy(100, 1))),
                "Two in one row.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, party, IdleLine(Enumerable.Repeat(100, BattleRows.Count + 1).ToArray())),
                "More units than a side has rows.");
            Assert.DoesNotThrow(() => Battle(balance, party, IdleLine(Enumerable.Repeat(100, BattleRows.Count).ToArray())), "A full line.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, party, enemies, setup => setup.Potions = new[] { TestData.HealPotion(1), TestData.HealPotion(1) }),
                "More potions than slots.");
            Assert.Throws<ArgumentException>(
                () => Battle(balance, TestData.Units(TestData.Mercenary("a", 1, 150).WithMaxHp(100)), enemies),
                "HP above max.");

            BattleUnitSetup overloaded = TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 1), TestData.Attack(1000, 1, "b"));
            overloaded.ItemSlots = 1;
            Assert.Throws<ArgumentException>(() => Battle(balance, TestData.Units(overloaded), enemies), "More items than the board holds.");

            BattleUnitSetup gap = TestData.Mercenary("a", 1, 100, TestData.Attack(1000, 1), null);
            gap.ItemSlots = 2;
            Assert.Throws<ArgumentException>(() => Battle(balance, TestData.Units(gap), enemies), "An empty entry on the board.");
        }
    }
}
