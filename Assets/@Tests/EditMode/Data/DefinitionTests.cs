using System.Collections.Generic;
using System.Linq;
using F1.Data;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Each definition type enforces its own rules in its constructor.</summary>
    public sealed class DefinitionTests
    {
        /// <summary>The span an InRows condition needs; none for the other conditions.</summary>
        static RowSpan RowsFor(PassiveCondition condition)
        {
            return condition == PassiveCondition.InRows ? RowSpan.Front(1) : null;
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
        public void PassiveSpec_InRowsNeedsASpan_OtherConditionsTakeNone()
        {
            PassiveSpec InRows(RowSpan rows)
            {
                return new PassiveSpec(PassiveTrigger.Always, PassiveCondition.InRows, rows, PassiveEffect.WeaponPowerPercent, PassiveTarget.Self, 5);
            }

            Assert.AreEqual(RowEnd.Back, InRows(RowSpan.Back(2)).Rows.From);
            Assert.AreEqual(2, InRows(RowSpan.Back(2)).Rows.Reach);
            Assert.Throws<DataException>(() => InRows(null), "No span.");

            Assert.Throws<DataException>(
                () => new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, RowSpan.Front(1), PassiveEffect.Shield, PassiveTarget.EventTarget, 5),
                "A span without the InRows condition.");
            Assert.IsNull(new PassiveSpec(PassiveTrigger.Heal, PassiveCondition.None, null, PassiveEffect.Shield, PassiveTarget.EventTarget, 5).Rows);
        }

        [Test]
        public void RowSpan_CountsFromAnEnd_OverTheLivingLine()
        {
            RowSpan front2 = RowSpan.Front(2);
            Assert.IsTrue(front2.Contains(1, 4) && front2.Contains(2, 4));
            Assert.IsFalse(front2.Contains(3, 4) || front2.Contains(4, 4));
            Assert.IsTrue(front2.Contains(2, 2), "The front N does not depend on the line length.");

            RowSpan back3 = RowSpan.Back(3);
            Assert.IsFalse(back3.Contains(1, 4));
            Assert.IsTrue(back3.Contains(2, 4) && back3.Contains(3, 4) && back3.Contains(4, 4), "Rows 2..4 of four.");
            Assert.IsTrue(back3.Contains(1, 3), "Rows 1..3 of three.");
            Assert.IsTrue(back3.Contains(1, 1), "A line shorter than the reach is covered whole.");

            Assert.IsTrue(RowSpan.All.IsEveryRow);
            Assert.IsTrue(RowSpan.Back(BattleRows.Count).IsEveryRow);
            Assert.IsFalse(RowSpan.Back(BattleRows.Count - 1).IsEveryRow);
            Assert.IsTrue(RowSpan.All.Contains(BattleRows.Count, BattleRows.Count));
        }

        [Test]
        public void RowSpan_ReachIsWithinTheRowsOfASide()
        {
            Assert.DoesNotThrow(() => RowSpan.Front(BattleRows.Count));
            Assert.Throws<DataException>(() => RowSpan.Front(0));
            Assert.Throws<DataException>(() => RowSpan.Back(BattleRows.Count + 1));
        }

        [TestCase("front:1", RowEnd.Front, 1)]
        [TestCase("front:2", RowEnd.Front, 2)]
        [TestCase("back:3", RowEnd.Back, 3)]
        [TestCase("all", RowEnd.Front, BattleRows.Count)]
        public void RowSpan_ReadsFrontBackAndAll(string text, RowEnd from, int reach)
        {
            Assert.IsTrue(RowSpan.TryParse(text, out RowSpan span));
            Assert.AreEqual(from, span.From);
            Assert.AreEqual(reach, span.Reach);
            Assert.AreEqual(text, span.ToString());
        }

        [TestCase("")]
        [TestCase("1")]
        [TestCase("1+2")]
        [TestCase("front")]
        [TestCase("front:")]
        [TestCase("front:0")]
        [TestCase("back:5")]
        [TestCase("middle:2")]
        [TestCase("Front:2")]
        [TestCase("front:+2")]
        [TestCase("front: 2")]
        [TestCase("front:2:1")]
        public void RowSpan_RejectsAnythingElse(string text)
        {
            Assert.IsFalse(RowSpan.TryParse(text, out RowSpan span), text);
            Assert.IsNull(span);
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
        public void ItemData_RejectsBadSizeCooldownEffectCountAndWeight()
        {
            ItemEffect effect = TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront);
            LocalizedText name = TestData.Text("x");
            RowSpan rows = RowSpan.All;

            Assert.DoesNotThrow(() => new ItemData("x", name, ItemCategory.Weapon, ItemData.MaxSize, 1000, rows, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 0, 1000, rows, new[] { effect }, 0), "No size.");
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, ItemData.MaxSize + 1, 1000, rows, new[] { effect }, 0), "Too big.");
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 99, rows, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, null, new[] { effect }, 0), "Rows missing.");
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new ItemEffect[0], 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect, effect, effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, -1));
            Assert.Throws<DataException>(() => new ItemData("x", null, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0));
            Assert.Throws<DataException>(() => new ItemData("Bad Id", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0));
        }

        [Test]
        public void ItemData_Rows_SayWhereTheItemWorks_OnTheLivingLine()
        {
            ItemData melee = TestData.Item("x", 1000, EffectKind.Damage, TargetMode.EnemyFront, rows: RowSpan.Front(2));
            Assert.IsTrue(melee.UsableIn(1, 4));
            Assert.IsTrue(melee.UsableIn(2, 4));
            Assert.IsFalse(melee.UsableIn(3, 4));

            ItemData staff = TestData.Item("y", 1000, EffectKind.Damage, TargetMode.EnemyFront, rows: RowSpan.Back(3));
            Assert.IsFalse(staff.UsableIn(1, 4));
            Assert.IsTrue(staff.UsableIn(2, 4));
            Assert.IsTrue(staff.UsableIn(1, 2), "First of two is within the rear three.");
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
            var passive = new PassiveSpec(PassiveTrigger.BattleStart, PassiveCondition.InRows, RowSpan.Front(1), PassiveEffect.Shield, PassiveTarget.Self, 20);

            Assert.DoesNotThrow(() => new JobData("x", name, 100, 3, "sword", 10, 1, null));
            Assert.DoesNotThrow(() => new JobData("x", name, 100, 3, "sword", 10, 1, passive, TestData.Text("Shield {0}")));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, passive));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, null, TestData.Text("Shield {0}")));
        }

        [Test]
        public void Figure_IsAnAddressWithoutSpaces_OrLeftOut()
        {
            LocalizedText name = TestData.Text("x");
            var items = new List<ItemGrant> { new ItemGrant("claw", 1) };

            Assert.IsNull(new JobData("x", name, 100, 3, "sword", 10, 1, null).Figure, "No figure by default.");
            Assert.AreEqual("unit/job/x", new JobData("x", name, 100, 3, "sword", 10, 1, null, null, "unit/job/x").Figure);
            Assert.AreEqual("unit/enemy/x", new EnemyData("x", name, 1, 10, items, "unit/enemy/x").Figure);
            Assert.IsNull(new EnemyData("x", name, 1, 10, items).Figure);

            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, null, null, ""));
            Assert.Throws<DataException>(() => new JobData("x", name, 100, 3, "sword", 10, 1, null, null, "unit/job/x "));
            Assert.Throws<DataException>(() => new EnemyData("x", name, 1, 10, items, "unit/ enemy/x"));
        }

        [Test]
        public void Poses_AreTheFigureAddressUnderPose_WithThePose_OrNullWithoutAFigure()
        {
            LocalizedText name = TestData.Text("x");
            var job = new JobData("x", name, 100, 3, "sword", 10, 1, null, null, "unit/job/x");

            Assert.AreEqual("pose/job/x-attack", job.AttackPose);
            Assert.AreEqual("pose/job/x-hit", job.HitPose);
            Assert.IsNull(new JobData("x", name, 100, 3, "sword", 10, 1, null).AttackPose, "No pose without a figure.");
            Assert.IsNull(new JobData("x", name, 100, 3, "sword", 10, 1, null).HitPose);
            var items = new List<ItemGrant> { new ItemGrant("claw", 1) };
            var enemy = new EnemyData("goblin_raider", name, 1, 10, items, "unit/enemy/goblin-raider");
            Assert.AreEqual("pose/enemy/goblin-raider-attack", enemy.AttackPose, "A monster has its poses too (Docs/Design/10 §5).");
            Assert.AreEqual("pose/enemy/goblin-raider-hit", enemy.HitPose);
            Assert.IsNull(new EnemyData("x", name, 1, 10, items).AttackPose, "No pose without a figure.");
            Assert.IsNull(new EnemyData("x", name, 1, 10, items).HitPose);
            Assert.AreEqual("pose/job/goblin-raider-hit", ArtAddress.PoseOf("unit/job/goblin-raider", ArtAddress.Hit));
            Assert.IsNull(ArtAddress.PoseOf(null, ArtAddress.Attack));
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

            Assert.DoesNotThrow(() => new DungeonData("d", name, "swift", 3, 2, 3, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 0, 2, 3, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 3, 2, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, DungeonData.MaxMapWidth + 1, 2, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 0, 8, 2, potions));
            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 2, 0, 2, potions));
        }

        [Test]
        public void Background_IsAnAddressWithoutSpaces_OrLeftOut()
        {
            LocalizedText name = TestData.Text("x");
            var potions = new List<string>();

            Assert.IsNull(new DungeonData("d", name, "swift", 3, 2, 3, 2, 8, 2, potions).Background, "No background by default.");
            Assert.AreEqual("background/dungeon/d", new DungeonData("d", name, "swift", 3, 2, 3, 2, 8, 2, potions, "background/dungeon/d").Background);

            Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 2, 8, 2, potions, ""));
            DataException error = Assert.Throws<DataException>(() => new DungeonData("d", name, "swift", 3, 2, 3, 2, 8, 2, potions, "background/ dungeon/d"));
            StringAssert.Contains("Background", error.Message, "The error names the column.");
        }

        [Test]
        public void Icon_IsAnAddressWithoutSpaces_OrLeftOut()
        {
            ItemEffect effect = TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront);
            LocalizedText name = TestData.Text("x");
            RowSpan rows = RowSpan.All;

            Assert.IsNull(new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0).Icon, "No icon by default.");
            Assert.AreEqual("item/x", new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0, "item/x").Icon);

            Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0, ""));
            DataException error = Assert.Throws<DataException>(() => new ItemData("x", name, ItemCategory.Weapon, 1, 1000, rows, new[] { effect }, 0, "item/ x"));
            StringAssert.Contains("Icon", error.Message, "The error names the column.");
        }

        [Test]
        public void DungeonData_ItemGradeGrowsByFloor()
        {
            var dungeon = new DungeonData("d", TestData.Text("d"), "swift", 3, 2, 3, 2, 8, 2, new List<string>());

            Assert.AreEqual(8, dungeon.ItemGradeAt(1));
            Assert.AreEqual(12, dungeon.ItemGradeAt(3));
        }

        [Test]
        public void BalanceData_RejectsInconsistentPartySizes()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("MinPartySize", 4), ("PartySize", 3)));
            Assert.DoesNotThrow(() => TestData.Balance(("PartySize", BattleRows.Count), ("MinPartySize", BattleRows.Count)));
            Assert.Throws<DataException>(() => TestData.Balance(("PartySize", BattleRows.Count + 1)), "One mercenary per row: no more than there are rows.");
        }

        [Test]
        public void BalanceData_InventoryCells_HoldAtLeastTheBiggestItem()
        {
            Assert.Throws<DataException>(() => TestData.Balance(("InventoryCells", ItemData.MaxSize - 1)));
            Assert.DoesNotThrow(() => TestData.Balance(("InventoryCells", ItemData.MaxSize)));
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
