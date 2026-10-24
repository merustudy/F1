using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Round 104 (판정 2): the battle's opening as the screen plays it — one step per activation of an item that activates when a battle starts,
    /// in the engine's order with its effects, a maximum HP that is no step (told at once), and the times the steps play while the clock holds.
    /// </summary>
    public sealed class BattleOpeningTests
    {
        static EquippedItem StartItem(string id, params ItemEffect[] effects)
        {
            return new EquippedItem(new ItemData(id, TestData.Text(id), ItemCategory.Armor, 1, 1, 0, RowSpan.All, effects.ToList(), 10, price: 10,
                dropWeight: 10, trigger: ItemTrigger.BattleStart));
        }

        static BattleEngine Opening()
        {
            BattleUnitSetup knight = TestData.Mercenary("knight", 1, 4000,
                    StartItem("plate", TestData.Effect(EffectKind.Shield, TargetMode.Self, 818)),
                    StartItem("belt", TestData.Effect(EffectKind.MaxHp, TargetMode.Self, 2400)),
                    TestData.Attack(1000, 1))
                .WithPassive(PassiveTrigger.BattleStart, PassiveCondition.None, PassiveEffect.Shield, PassiveTarget.Self, 545);
            BattleUnitSetup mage = TestData.Mercenary("mage", 2, 3000,
                StartItem("robe", TestData.Effect(EffectKind.Shield, TargetMode.Self, 546), TestData.Effect(EffectKind.Charge, TargetMode.Self, 1500)),
                TestData.Attack(2000, 1));
            BattleUnitSetup rat = TestData.Enemy("rat", 1, 1100, StartItem("hide", TestData.Effect(EffectKind.Thorns, TargetMode.Self, 60)), TestData.Attack(1500, 1));
            return new BattleEngine(TestData.Setup(TestData.Balance(), new[] { knight, mage }, new[] { rat }));
        }

        static bool StartsTheBattle(string id)
        {
            return id == "plate" || id == "belt" || id == "robe" || id == "hide";
        }

        [Test]
        public void EveryActivation_IsAStep_InTheEnginesOrder_WithItsEffects_ButAMaxHpAloneIsToldAtOnce()
        {
            BattleEngine battle = Opening();
            BattleOpening opening = BattleOpening.Of(battle.Events, StartsTheBattle);
            IReadOnlyList<BattleEvent> events = battle.Events;

            CollectionAssert.AreEqual(new[] { "plate", "robe", "hide" }, opening.Steps.Select(s => events[s.Events[0]].Id).ToArray(),
                "The party's first, unit and slot order; the belt raised the maximum HP only.");
            foreach (BattleOpening.Step step in opening.Steps)
            {
                Assert.AreEqual(BattleEventKind.ItemActivated, events[step.Events[0]].Kind, "A step starts with its activation.");
                Assert.IsTrue(step.Events.Skip(1).All(i => events[i].Id == events[step.Events[0]].Id && events[i].Kind != BattleEventKind.ItemActivated));
            }

            CollectionAssert.AreEqual(new[] { BattleEventKind.ShieldGained, BattleEventKind.Charged },
                opening.Steps[1].Events.Skip(1).Select(i => events[i].Kind).ToArray(), "The robe's two effects play with it.");
            Assert.AreEqual(BattleEventKind.ThornsGained, events[opening.Steps[2].Events[1]].Kind);
            Assert.AreEqual(1, opening.AtOnce.Count);
            Assert.AreEqual(BattleEventKind.MaxHpRaised, events[opening.AtOnce[0]].Kind, "The belt's maximum HP is there from the first frame.");
            Assert.IsFalse(opening.Steps.SelectMany(s => s.Events).Any(i => events[i].Id == BattleEvent.CausePassive), "A job's passive shield is no step.");
        }

        [Test]
        public void TheStepsPlayFromAThirdOfASecond_0_35Apart_AndTheClockHoldsHalfASecondMore()
        {
            BattleOpening opening = BattleOpening.Of(Opening().Events, StartsTheBattle);

            CollectionAssert.AreEqual(new[] { 0.3f, 0.65f, 1.0f }, opening.Steps.Select(s => s.At).ToArray(), new FloatComparer());
            Assert.AreEqual(1.5f, opening.Length, 1e-4f);
        }

        [Test]
        public void ManySteps_StayWithinTwoSeconds()
        {
            float[] times = BattleOpening.Times(8);

            Assert.AreEqual(BattleOpening.First, times[0], 1e-4f);
            Assert.AreEqual(BattleOpening.Longest - BattleOpening.Tail, times[7], 1e-4f, "The gap shrinks: the last step plays half a second before two.");
            Assert.Less(times[1] - times[0], BattleOpening.Gap);
            CollectionAssert.AreEqual(new[] { 0.3f }, BattleOpening.Times(1), new FloatComparer());
            Assert.IsEmpty(BattleOpening.Times(0));
        }

        [Test]
        public void ABattleWithoutSuchItems_HasNoOpening_AndRunsAtOnce()
        {
            var battle = new BattleEngine(TestData.Setup(TestData.Balance(), new[] { TestData.Mercenary("m", 1, 4000, TestData.Attack(1000, 1)) },
                new[] { TestData.Enemy("e", 1, 4000) }));
            BattleOpening opening = BattleOpening.Of(battle.Events, _ => false);

            Assert.IsEmpty(opening.Steps);
            Assert.AreEqual(0f, opening.Length);
        }

        sealed class FloatComparer : System.Collections.IComparer
        {
            public int Compare(object x, object y)
            {
                return System.Math.Abs((float)x - (float)y) < 1e-4f ? 0 : ((float)x).CompareTo((float)y);
            }
        }
    }
}
