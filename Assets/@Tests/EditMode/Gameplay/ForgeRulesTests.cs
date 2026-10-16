using System;
using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The camp's forge (Slice B stage 23): a recipe's base at its tier with its catalyst lit on it makes the unique item in the base's
    /// place; the catalyst is used up, the camp is spent, and a party holds one unique.
    /// </summary>
    public sealed class ForgeRulesTests
    {
        const string Sabre = "sabre_t";
        const string Hone = "hone_t";
        const string Unique = "gladius_t";

        /// <summary>The test data with a melee sabre (2x1), a hone (a star item with a star above and below) and the recipe sabre + hone -> gladius.</summary>
        static StaticData Data()
        {
            StaticDataParts parts = TestData.Parts();
            var items = new List<ItemData>(parts.Items)
            {
                new ItemData(Sabre, TestData.Text(Sabre), ItemCategory.Weapon, 2, 1, 2000, RowSpan.All,
                    new[] { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 10, 1) }, 0, melee: true),
                new ItemData(Hone, TestData.Text(Hone), ItemCategory.Other, 1, 1, 0, RowSpan.All, new ItemEffect[0], 0,
                    stars: new[] { new StarSquare(0, -1), new StarSquare(0, 1) }, starDamage: 100),
            };
            parts.Items = items;
            parts.Recipes = new List<RecipeData> { new RecipeData(Unique, Unique, Sabre, ItemTier.Tier3, Hone, ItemTier.Tier3) };
            return new StaticData(parts);
        }

        static ExpeditionState AtTheCamp(StaticData data, ItemTier sabreTier = ItemTier.Tier3, int honeX = 0, ItemTier honeTier = ItemTier.Tier3)
        {
            List<PartyMember> party = data.Mercenaries.Ordered.Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index)).ToList();
            ExpeditionState state = ExpeditionRules.Create(data, data.Dungeons.Ordered[0].Id, 7, party);
            ExpeditionMember anna = state.Members[0];
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Sabre), isBase: true, tier: sabreTier), 0, 1);
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Hone), tier: honeTier), honeX, 2);
            state.Phase = ExpeditionPhase.AtCamp;
            return state;
        }

        [Test]
        public void Options_FindTheRecipe_OnABaseAtItsTier_WithTheCatalystAtItsTierLitOnIt()
        {
            StaticData data = Data();
            ExpeditionState state = AtTheCamp(data);

            List<ForgeOption> options = ForgeRules.Options(data, state);

            Assert.AreEqual(1, options.Count);
            Assert.AreEqual(0, options[0].MemberIndex);
            Assert.AreEqual(Sabre, options[0].BaseItem.Item.Item.Id);
            Assert.AreEqual(Hone, options[0].Catalyst.Item.Item.Id);
            Assert.AreEqual(Unique, options[0].Recipe.Result);
            Assert.IsTrue(ForgeRules.CanForgeAtCamp(data, state, 0, 1, 1), "Any square of the base.");
            Assert.IsFalse(ForgeRules.CanForgeAtCamp(data, state, 0, 0, 0), "The job weapon is no recipe's base.");
        }

        [Test]
        public void Options_AreNone_WhenATierIsShort_WhenTheStarIsNotLit_OrWhenThePartyHoldsAUnique()
        {
            StaticData data = Data();
            Assert.IsEmpty(ForgeRules.Options(data, AtTheCamp(data, sabreTier: ItemTier.Tier2)), "The base below its tier.");
            Assert.IsEmpty(ForgeRules.Options(data, AtTheCamp(data, honeTier: ItemTier.Tier2)), "The catalyst below its tier.");
            Assert.IsEmpty(ForgeRules.Options(data, AtTheCamp(data, honeX: 2)), "The hone's star over an empty square: not lit on the sabre.");

            ExpeditionState holding = AtTheCamp(data);
            holding.Inventory.Add(new EquippedItem(data.Items.Get(Unique), tier: ItemTier.Tier3), new Placement(0, 0));
            Assert.IsTrue(ForgeRules.PartyHasUnique(holding));
            Assert.IsEmpty(ForgeRules.Options(data, holding), "One unique per party.");
        }

        [Test]
        public void ForgeAtCamp_UsesTheCatalyst_MakesTheUniqueInTheBasesPlace_KeepsItsStanding_AndLeavesTheCamp()
        {
            StaticData data = Data();
            ExpeditionState state = AtTheCamp(data);
            ExpeditionMember anna = state.Members[0];

            ForgeRules.ForgeAtCamp(data, state, 0, 0, 1);

            EquippedItem made = TestBoards.ItemAt(anna, 0, 1);
            Assert.AreEqual(Unique, made.Item.Id);
            Assert.AreEqual(ItemTier.Common, made.Tier, "A unique has no tier: it never merges (there is one) and is never mended.");
            Assert.IsTrue(made.IsBase, "The base's standing: a base weapon stays one (no fatigue).");
            Assert.AreSame(made, TestBoards.ItemAt(anna, 1, 1), "In the base's place: both squares.");
            Assert.IsNull(anna.Board.ItemAt(0, 2), "The catalyst is gone.");
            Assert.AreEqual(ExpeditionPhase.ChoosingNode, state.Phase, "The camp is spent: no rest.");
            Assert.IsTrue(ForgeRules.PartyHasUnique(state));
            Assert.IsFalse(ForgeRules.CanForgeAtCamp(data, state, 0, 0, 1), "Not at a camp, and the party holds its unique.");

            state.Phase = ExpeditionPhase.AtCamp;
            Assert.IsFalse(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0, 1), "A unique is never mended: it has no tier.");
            Assert.IsTrue(ExpeditionRules.CanUpgradeAtCamp(state, 0, 0, 0), "The job weapon still can be.");
        }

        [Test]
        public void ForgeAtCamp_Refuses_OffTheCamp_AndWhereNoRecipeIsReady()
        {
            StaticData data = Data();
            ExpeditionState state = AtTheCamp(data);
            Assert.Throws<InvalidOperationException>(() => ForgeRules.ForgeAtCamp(data, state, 0, 0, 0), "The job weapon.");
            state.Phase = ExpeditionPhase.ChoosingNode;
            Assert.IsFalse(ForgeRules.CanForgeAtCamp(data, state, 0, 0, 1));
            Assert.Throws<InvalidOperationException>(() => ForgeRules.ForgeAtCamp(data, state, 0, 0, 1));
        }

        [Test]
        public void TheData_RefusesARecipe_WhoseResultIsNotUnique_OrWhoseCatalystHasNoStars_AndAUniqueForSale()
        {
            Assert.Throws<DataException>(() => new RecipeData("r", Sabre, Sabre, ItemTier.Tier3, Hone, ItemTier.Tier3), "The result is an ingredient.");
            Assert.Throws<DataException>(() => new ItemData("relic", TestData.Text("relic"), ItemCategory.Other, 1, 1, 1000, RowSpan.All,
                new[] { TestData.Effect(EffectKind.Shield, TargetMode.Self, 10, TestData.DefaultReach) }, 5, price: 10, unique: true), "A unique item is never sold.");

            StaticDataParts notUnique = TestData.Parts();
            notUnique.Recipes = new List<RecipeData> { new RecipeData("r", "knife", "blade", ItemTier.Tier3, "charm", ItemTier.Tier3) };
            DataValidationException problems = Assert.Throws<DataValidationException>(() => new StaticData(notUnique), "The knife is not unique; the charm has no stars.");
            StringAssert.Contains("not a unique item", problems.Message);
            StringAssert.Contains("has no stars", problems.Message);
        }
    }
}
