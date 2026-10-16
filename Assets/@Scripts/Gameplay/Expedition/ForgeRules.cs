using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>A forge the party could do now: whose board, which base item, which catalyst lit on it, by which recipe.</summary>
    public sealed class ForgeOption
    {
        public ForgeOption(int memberIndex, BoardItem baseItem, BoardItem catalyst, RecipeData recipe)
        {
            MemberIndex = memberIndex;
            BaseItem = baseItem;
            Catalyst = catalyst;
            Recipe = recipe;
        }

        public int MemberIndex { get; }
        public BoardItem BaseItem { get; }
        public BoardItem Catalyst { get; }
        public RecipeData Recipe { get; }
    }

    /// <summary>
    /// The camp's forge (Slice B stage 23, Docs/Design/02_Combat_System.md §4 "담금질", 03 §1): a recipe's base item at its tier, with the
    /// recipe's catalyst at its tier lit on it (the catalyst's star square on the base: StarRules), is forged into the recipe's result — a
    /// unique item, of which a party holds one. The result takes the base's place, placement, tier and base-weapon standing; the catalyst
    /// is gone; the camp is spent (no rest, no mend). Pure rules over the expedition state.
    /// </summary>
    public static class ForgeRules
    {
        /// <summary>Whether a unique item is on any living member's board or in the inventory: the party holds one at most.</summary>
        public static bool PartyHasUnique(ExpeditionState state)
        {
            foreach (ExpeditionMember member in state.Members)
            {
                if (!member.Alive)
                {
                    continue;
                }

                foreach (BoardItem placed in member.Board.Items)
                {
                    if (placed.Item.Item.Unique)
                    {
                        return true;
                    }
                }
            }

            foreach (BoardItem kept in state.Inventory.Items)
            {
                if (kept.Item.Item.Unique)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Every forge the boards allow now, in member order and the boards' reading order: for each recipe, a base item at the recipe's tier
        /// with a catalyst at the recipe's tier among the star items lit on it, where the result would fit in the base's place once the
        /// catalyst is gone. None while the party holds a unique item.
        /// </summary>
        public static List<ForgeOption> Options(StaticData data, ExpeditionState state)
        {
            var options = new List<ForgeOption>();
            if (PartyHasUnique(state))
            {
                return options;
            }

            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (!member.Alive)
                {
                    continue;
                }

                foreach (BoardItem baseItem in member.Board.PlacedInReadingOrder())
                {
                    foreach (RecipeData recipe in data.Recipes.Ordered)
                    {
                        if (baseItem.Item.Item.Id != recipe.BaseItem || baseItem.Item.Tier != recipe.BaseTier)
                        {
                            continue;
                        }

                        BoardItem catalyst = CatalystOn(member.Board, baseItem, recipe);
                        if (catalyst != null && ResultFits(data, member.Board, baseItem, catalyst, recipe))
                        {
                            options.Add(new ForgeOption(m, baseItem, catalyst, recipe));
                        }
                    }
                }
            }

            return options;
        }

        /// <summary>The first star item of the recipe's catalyst at its tier whose star is lit on the base, or null.</summary>
        static BoardItem CatalystOn(ItemBoard board, BoardItem baseItem, RecipeData recipe)
        {
            foreach (BoardItem source in StarRules.Sources(board, baseItem))
            {
                if (source.Item.Item.Id == recipe.Catalyst && source.Item.Tier == recipe.CatalystTier)
                {
                    return source;
                }
            }

            return null;
        }

        /// <summary>Whether the result, laid where the base lies (its placement), covers only the base's and the catalyst's squares, on bags.</summary>
        static bool ResultFits(StaticData data, ItemBoard board, BoardItem baseItem, BoardItem catalyst, RecipeData recipe)
        {
            ItemData result = data.Items.Get(recipe.Result);
            Placement at = baseItem.At;
            int w = at.WidthOf(result.Width, result.Height);
            int h = at.HeightOf(result.Width, result.Height);
            if (!board.OnBags(at.X, at.Y, w, h))
            {
                return false;
            }

            foreach (BoardItem under in board.ItemsUnder(at.X, at.Y, w, h, baseItem))
            {
                if (under != catalyst)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>The forge the base item covering a square of a member's board allows at the camp, or null.</summary>
        public static ForgeOption OptionAt(StaticData data, ExpeditionState state, int memberIndex, int x, int y)
        {
            if (state.Phase != ExpeditionPhase.AtCamp)
            {
                return null;
            }

            foreach (ForgeOption option in Options(data, state))
            {
                if (option.MemberIndex == memberIndex && option.BaseItem.Covers(x, y))
                {
                    return option;
                }
            }

            return null;
        }

        public static bool CanForgeAtCamp(StaticData data, ExpeditionState state, int memberIndex, int x, int y)
        {
            return OptionAt(data, state, memberIndex, x, y) != null;
        }

        /// <summary>
        /// What forging an option makes: the recipe's unique in the base's standing (a base weapon stays one, its power too) and at no tier.
        /// A unique item has no tier (round 70 follow-up 3): it never merges, there being one, and is never mended; its numbers are the data's.
        /// </summary>
        public static EquippedItem Result(StaticData data, ForgeOption option)
        {
            EquippedItem before = option.BaseItem.Item;
            return new EquippedItem(data.Items.Get(option.Recipe.Result), before.IsBase, ItemTier.Common, before.PowerPercent);
        }

        /// <summary>
        /// The camp's forge: the catalyst leaves the board, the base item becomes the recipe's result in its place (its placement and
        /// base-weapon standing kept, at no tier: <see cref="Result"/>), and the party goes on to the next floor without resting.
        /// </summary>
        public static void ForgeAtCamp(StaticData data, ExpeditionState state, int memberIndex, int x, int y)
        {
            ForgeOption option = OptionAt(data, state, memberIndex, x, y)
                ?? throw new InvalidOperationException($"Nothing at ({x},{y}) of member {memberIndex} can be forged now.");
            ItemBoard board = state.Members[memberIndex].Board;
            board.Items.Remove(option.Catalyst);
            option.BaseItem.Item = Result(data, option);
            state.Phase = ExpeditionPhase.ChoosingNode;
        }
    }
}
