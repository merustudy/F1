using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The stars of Docs/Design/02_Combat_System.md §4 (Slice B stage 20, Backpack Battles' stars): squares round an item that turn with it,
    /// lit by a melee weapon lying on them, which then deals more; an item fills one star of another item at most.
    /// </summary>
    public sealed class StarRulesTests
    {
        static readonly BagData Pack = new BagData("pack", TestData.Text("pack"), 3, 3, true, 0, 0, 0);

        /// <summary>The whetstone of the data: one square, a star above and one below, no effects.</summary>
        static ItemData Whetstone(int starDamage = 1)
        {
            return new ItemData("whetstone", TestData.Text("whetstone"), ItemCategory.Other, 1, 1, 0, RowSpan.All, new ItemEffect[0], 8, null, 8,
                false, new[] { new StarSquare(0, -1), new StarSquare(0, 1) }, starDamage);
        }

        /// <summary>A two-square star item with a star above each square: a weapon across both fills one of them only.</summary>
        static ItemData Hone()
        {
            return new ItemData("hone", TestData.Text("hone"), ItemCategory.Other, 2, 1, 0, RowSpan.All, new ItemEffect[0], 0, null, 0,
                false, new[] { new StarSquare(0, -1), new StarSquare(1, -1) }, 1);
        }

        static ItemData Weapon(string id, int width, bool melee = true)
        {
            return new ItemData(id, TestData.Text(id), ItemCategory.Weapon, width, 1, 1000, RowSpan.All,
                new[] { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront) }, 0, null, 0, melee);
        }

        static ItemBoard PackBoard()
        {
            var board = new ItemBoard();
            board.Bags.Add(new BoardBag(Pack, new Placement(0, 0)));
            return board;
        }

        static BoardItem Lay(ItemBoard board, ItemData item, int x, int y, int turns = 0, ItemTier tier = ItemTier.Common)
        {
            var placed = new BoardItem(new EquippedItem(item, 8, tier: tier), new Placement(x, y, turns));
            board.Items.Add(placed);
            return placed;
        }

        [Test]
        public void StarSquares_TurnWithTheItem_AQuarterClockwiseAtATime()
        {
            ItemData whetstone = Whetstone();
            CollectionAssert.AreEqual(new[] { (1, 0), (1, 2) }, StarRules.StarSquares(whetstone, new Placement(1, 1, 0)));
            CollectionAssert.AreEqual(new[] { (2, 1), (0, 1) }, StarRules.StarSquares(whetstone, new Placement(1, 1, 1)), "Turned: above is right.");
            CollectionAssert.AreEqual(new[] { (1, 2), (1, 0) }, StarRules.StarSquares(whetstone, new Placement(1, 1, 2)));
            CollectionAssert.AreEqual(new[] { (0, 1), (2, 1) }, StarRules.StarSquares(whetstone, new Placement(1, 1, 3)));

            // A 2x1 shape with a star above its left square stands up 1x2 when turned: the left square on top, the star to its right.
            CollectionAssert.AreEqual(new[] { (0, 0), (1, 0) }, StarRules.StarSquares(Hone(), new Placement(0, 1, 0)));
            CollectionAssert.AreEqual(new[] { (1, 1), (1, 2) }, StarRules.StarSquares(Hone(), new Placement(0, 1, 1)));
        }

        [Test]
        public void Marks_ALitStarOnAMeleeWeapon_AHollowOneElsewhere_NoneOffTheBags()
        {
            ItemBoard board = PackBoard();
            Lay(board, Weapon("sword", 3), 0, 0);
            Lay(board, Weapon("staff", 2, melee: false), 0, 2);

            List<StarMark> marks = StarRules.Marks(board, Whetstone(), new Placement(0, 1), null);
            Assert.AreEqual(2, marks.Count);
            Assert.IsTrue(marks.Single(m => m.Y == 0).Lit, "The sword above lights its star.");
            Assert.IsFalse(marks.Single(m => m.Y == 2).Lit, "A weapon that is not melee lights nothing.");

            List<StarMark> low = StarRules.Marks(board, Whetstone(), new Placement(2, 2), null);
            Assert.AreEqual(1, low.Count, "The star under the pack's last row is off the bags: not drawn.");
            Assert.IsFalse(low[0].Lit, "An empty square: a hollow star.");

            BoardItem sword = board.ItemAt(0, 0);
            Assert.IsFalse(StarRules.Marks(board, Whetstone(), new Placement(0, 1), sword).Single(m => m.Y == 0).Lit, "The sword held from this board lies nowhere.");
        }

        [Test]
        public void DamageOn_EachStarItemOnce_StarsOfDifferentItemsAdd()
        {
            ItemBoard board = PackBoard();
            BoardItem sword = Lay(board, Weapon("sword", 3), 0, 1);
            Lay(board, Whetstone(), 0, 0);
            Assert.AreEqual(1, StarRules.DamageOn(board, sword));

            Lay(board, Whetstone(), 2, 2);
            Assert.AreEqual(2, StarRules.DamageOn(board, sword), "A second whetstone touching it adds its own.");

            Lay(board, Hone(), 0, 2);
            Assert.AreEqual(3, StarRules.DamageOn(board, sword), "The sword lies on both of the hone's stars: it fills one of them, so the hone adds once.");
            CollectionAssert.AreEqual(new[] { "whetstone", "whetstone", "hone" }, StarRules.Sources(board, sword).Select(s => s.Item.Item.Id));
        }

        [Test]
        public void DamageOn_OnlyAMeleeWeaponIsStrengthened()
        {
            ItemBoard board = PackBoard();
            BoardItem staff = Lay(board, Weapon("staff", 3, melee: false), 0, 1);
            Lay(board, Whetstone(), 0, 0);
            Assert.AreEqual(0, StarRules.DamageOn(board, staff));
            Assert.IsEmpty(StarRules.Sources(board, staff));
        }

        [Test]
        public void StarDamage_GrowsByItsOwnPerTierStep()
        {
            ItemData whetstone = Whetstone(starDamage: 2);
            Assert.AreEqual(2, new EquippedItem(whetstone, 8).StarDamage);
            Assert.AreEqual(4, new EquippedItem(whetstone, 8, tier: ItemTier.Bronze).StarDamage);
            Assert.AreEqual(6, new EquippedItem(whetstone, 8, tier: ItemTier.Silver).StarDamage);
            Assert.AreEqual(8, new EquippedItem(whetstone, 8, tier: ItemTier.Gold).StarDamage);
            Assert.AreEqual(0, new EquippedItem(Weapon("sword", 3), 8, tier: ItemTier.Gold).StarDamage, "No stars, nothing.");

            ItemBoard board = PackBoard();
            BoardItem sword = Lay(board, Weapon("sword", 3), 0, 1);
            Lay(board, Whetstone(), 1, 2, tier: ItemTier.Silver);
            Assert.AreEqual(3, StarRules.DamageOn(board, sword));
        }

        [Test]
        public void Lit_TheMeleeWeaponsOnAStarItemsStars_InTheOrderOfItsStars()
        {
            ItemBoard board = PackBoard();
            Lay(board, Weapon("sword", 3), 0, 0);
            Lay(board, Weapon("axe", 3), 0, 2);
            BoardItem whetstone = Lay(board, Whetstone(), 1, 1);
            CollectionAssert.AreEqual(new[] { "sword", "axe" }, StarRules.Lit(board, whetstone).Select(w => w.Item.Item.Id));

            BoardItem turned = board.Items.Last();
            turned.At = new Placement(1, 1, 1);
            Assert.IsEmpty(StarRules.Lit(board, turned), "Turned, its stars are left and right, on empty squares.");
        }

        [Test]
        public void DamageInReadingOrder_IsTheBattlesOrder()
        {
            ItemBoard board = PackBoard();
            Lay(board, Whetstone(), 0, 0);
            Lay(board, Weapon("sword", 3), 0, 1);
            Lay(board, Weapon("staff", 2, melee: false), 0, 2);

            CollectionAssert.AreEqual(new[] { 0, 1, 0 }, StarRules.DamageInReadingOrder(board));
            CollectionAssert.AreEqual(new[] { "whetstone", "sword", "staff" }, board.InReadingOrder().Select(i => i.Item.Id));
        }
    }
}
