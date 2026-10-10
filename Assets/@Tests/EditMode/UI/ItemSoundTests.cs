using System.Linq;
using F1.Core;
using F1.Data;
using F1.Editor.Data;
using F1.UI;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>The sound of an item that fires, by what the item is (Docs/Design/12_Sound_Direction.md §2).</summary>
    public sealed class ItemSoundTests
    {
        [TestCase(ItemCategory.Weapon, SoundEffect.ItemWeapon)]
        [TestCase(ItemCategory.Armor, SoundEffect.ItemArmor)]
        [TestCase(ItemCategory.Attack, SoundEffect.ItemAttack)]
        [TestCase(ItemCategory.Support, SoundEffect.ItemSupport)]
        public void AnItemSounds_ByItsCategory(ItemCategory category, SoundEffect effect)
        {
            Assert.AreEqual(effect, BattlePresenter.EffectOf(category));
        }

        [Test]
        public void TheOtherCategory_HasNoSound_AndOnlyItemsThatNeverFireAreOfIt()
        {
            Assert.IsNull(BattlePresenter.EffectOf(ItemCategory.Other));
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            StaticData data = StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
            Assert.IsTrue(data.Items.Ordered.Where(item => !item.IsPassive).All(item => BattlePresenter.EffectOf(item.Category).HasValue),
                "Every shipped item that fires makes a sound when it does.");
            Assert.IsTrue(data.Items.Ordered.Where(item => item.Category == ItemCategory.Other).All(item => item.IsPassive),
                "An Other item (the whetstone, Slice B stage 20) never fires, so it needs no sound.");
        }
    }
}
