using System;
using F1.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>What an item's owner does on the battle screen when the item fires, by what the item is (Docs/Design/10 §5).</summary>
    public sealed class ItemMotionTests
    {
        [TestCase(ItemCategory.Weapon, BattlePresenter.Motion.Strike)]
        [TestCase(ItemCategory.Armor, BattlePresenter.Motion.Lunge)]
        [TestCase(ItemCategory.Attack, BattlePresenter.Motion.Lunge)]
        [TestCase(ItemCategory.Other, BattlePresenter.Motion.Lunge)]
        [TestCase(ItemCategory.Support, BattlePresenter.Motion.Pulse)]
        public void AnItemMovesItsOwner_ByItsCategory(ItemCategory category, BattlePresenter.Motion motion)
        {
            Assert.AreEqual(motion, BattlePresenter.MotionOf(category));
        }

        [Test]
        public void EveryCategory_HasItsOwnWords()
        {
            var keys = new System.Collections.Generic.HashSet<string>();
            foreach (ItemCategory category in Enum.GetValues(typeof(ItemCategory)))
            {
                Assert.IsTrue(keys.Add(UiText.CategoryKey(category)), category.ToString());
            }
        }
    }
}
