using System;
using System.Collections.Generic;
using F1.Data;

namespace F1.Gameplay
{
    /// <summary>
    /// A link between a recipe's pieces on one board (Slice D stage 50, Docs/Design/02_Combat_System.md §4 "유니크의 조합"): whose board, the base, the
    /// catalyst touching it, by which recipe, and whether the next won battle combines them (ready) or a tier is short (the board's grey line).
    /// </summary>
    public sealed class ForgeLink
    {
        public ForgeLink(int memberIndex, BoardItem baseItem, BoardItem catalyst, RecipeData recipe, bool ready)
        {
            MemberIndex = memberIndex;
            BaseItem = baseItem;
            Catalyst = catalyst;
            Recipe = recipe;
            Ready = ready;
        }

        public int MemberIndex { get; }
        public BoardItem BaseItem { get; }
        public BoardItem Catalyst { get; }
        public RecipeData Recipe { get; }
        public bool Ready { get; }
    }

    /// <summary>
    /// A unique item a won battle made (stage 50): whose board, the base and the catalyst as they were and where they lay, and the unique.
    /// The battle screen plays the merge from it; it is not saved (a resumed expedition shows the unique without the effect).
    /// </summary>
    public sealed class UniqueForged
    {
        public UniqueForged(int memberIndex, EquippedItem baseBefore, Placement baseAt, EquippedItem catalyst, Placement catalystAt, string resultId)
        {
            MemberIndex = memberIndex;
            BaseBefore = baseBefore;
            BaseAt = baseAt;
            Catalyst = catalyst;
            CatalystAt = catalystAt;
            ResultId = resultId;
        }

        public int MemberIndex { get; }
        public EquippedItem BaseBefore { get; }
        public Placement BaseAt { get; }
        public EquippedItem Catalyst { get; }
        public Placement CatalystAt { get; }
        public string ResultId { get; }
    }

    /// <summary>
    /// The combination of unique items (Slice D stage 50, Docs/Design/02_Combat_System.md §4 "유니크의 조합", Design/29 — the Backpack Battles way; it
    /// replaced the camp's forge of stage 23): a recipe's base at its tier and the recipe's catalyst at its tier, side by side on one board (they share
    /// a side of a square), become the recipe's result when a battle that is not the boss's is won. The result takes the base's place, placement and
    /// base-weapon standing at no tier; the catalyst is gone. A piece is in one link at most (the line the board shows), and the party holds each
    /// unique once (other uniques beside it). Pure rules over the expedition state.
    /// </summary>
    public static class ForgeRules
    {
        /// <summary>Whether the unique item is on a living member's board or in the inventory: the party holds each unique once (stage 50).</summary>
        public static bool PartyHas(ExpeditionState state, string itemId)
        {
            foreach (ExpeditionMember member in state.Members)
            {
                if (!member.Alive)
                {
                    continue;
                }

                foreach (BoardItem placed in member.Board.Items)
                {
                    if (placed.Item.Item.Id == itemId)
                    {
                        return true;
                    }
                }
            }

            foreach (BoardItem kept in state.Inventory.Items)
            {
                if (kept.Item.Item.Id == itemId)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Whether two pieces of a board share a side of a square: above, below, left or right, not across a corner.</summary>
        public static bool Touches(BoardItem a, BoardItem b)
        {
            for (int ax = a.At.X; ax < a.At.X + a.Width; ax++)
            {
                for (int ay = a.At.Y; ay < a.At.Y + a.Height; ay++)
                {
                    for (int bx = b.At.X; bx < b.At.X + b.Width; bx++)
                    {
                        for (int by = b.At.Y; by < b.At.Y + b.Height; by++)
                        {
                            if (Math.Abs(ax - bx) + Math.Abs(ay - by) == 1)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// The links the boards show and the next won battle combines (Design/29 판정 3·6), in member order and the boards' reading order. First the
        /// ready ones: each base, in reading order, takes the first touching catalyst no link has taken yet whose tiers are the recipe's, where the result
        /// fits and the party does not hold that unique and no earlier ready link makes it. Then the grey ones: each base not linked yet, with the first
        /// touching catalyst not linked yet, where a tier is short and the party does not hold the unique. Anything else draws no line.
        /// </summary>
        public static List<ForgeLink> Links(StaticData data, ExpeditionState state)
        {
            var links = new List<ForgeLink>();
            var linked = new HashSet<BoardItem>();
            var making = new HashSet<string>(StringComparer.Ordinal);
            for (int pass = 0; pass < 2; pass++)
            {
                bool ready = pass == 0;
                for (int m = 0; m < state.Members.Count; m++)
                {
                    ExpeditionMember member = state.Members[m];
                    if (!member.Alive)
                    {
                        continue;
                    }

                    List<BoardItem> pieces = member.Board.PlacedInReadingOrder();
                    foreach (BoardItem baseItem in pieces)
                    {
                        if (linked.Contains(baseItem))
                        {
                            continue;
                        }

                        ForgeLink link = LinkOf(data, state, member.Board, pieces, m, baseItem, ready, linked, making);
                        if (link == null)
                        {
                            continue;
                        }

                        links.Add(link);
                        linked.Add(link.BaseItem);
                        linked.Add(link.Catalyst);
                        if (ready)
                        {
                            making.Add(link.Recipe.Result);
                        }
                    }
                }
            }

            return links;
        }

        static ForgeLink LinkOf(StaticData data, ExpeditionState state, ItemBoard board, List<BoardItem> pieces, int memberIndex, BoardItem baseItem, bool ready,
            HashSet<BoardItem> linked, HashSet<string> making)
        {
            foreach (RecipeData recipe in data.Recipes.Ordered)
            {
                if (baseItem.Item.Item.Id != recipe.BaseItem || PartyHas(state, recipe.Result) || making.Contains(recipe.Result))
                {
                    continue;
                }

                if (ready && baseItem.Item.Tier != recipe.BaseTier)
                {
                    continue;
                }

                foreach (BoardItem catalyst in pieces)
                {
                    if (catalyst == baseItem || linked.Contains(catalyst) || catalyst.Item.Item.Id != recipe.Catalyst || !Touches(baseItem, catalyst))
                    {
                        continue;
                    }

                    bool tiersMet = baseItem.Item.Tier == recipe.BaseTier && catalyst.Item.Tier == recipe.CatalystTier;
                    if (ready ? tiersMet && ResultFits(data, board, baseItem, catalyst, recipe) : !tiersMet)
                    {
                        return new ForgeLink(memberIndex, baseItem, catalyst, recipe, ready);
                    }
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

        /// <summary>
        /// What a ready link makes: the recipe's unique in the base's standing (a base weapon stays one, its power too) and at no tier. A unique item
        /// has no tier (round 70 follow-up 3): it never merges and is never mended; its numbers are the data's.
        /// </summary>
        public static EquippedItem Result(StaticData data, ForgeLink link)
        {
            EquippedItem before = link.BaseItem.Item;
            return new EquippedItem(data.Items.Get(link.Recipe.Result), before.IsBase, ItemTier.Common, before.PowerPercent);
        }

        /// <summary>
        /// The combination after a won battle (stage 50, <see cref="ExpeditionRules.CompleteBattle"/>): every ready link at once — the catalyst leaves the
        /// board, the base becomes the unique in its place (<see cref="Result"/>). Returns what was made, for the battle screen's merge.
        /// </summary>
        public static List<UniqueForged> ForgeAfterBattle(StaticData data, ExpeditionState state)
        {
            var made = new List<UniqueForged>();
            foreach (ForgeLink link in Links(data, state))
            {
                if (!link.Ready)
                {
                    continue;
                }

                made.Add(new UniqueForged(link.MemberIndex, link.BaseItem.Item, link.BaseItem.At, link.Catalyst.Item, link.Catalyst.At, link.Recipe.Result));
                state.Members[link.MemberIndex].Board.Items.Remove(link.Catalyst);
                link.BaseItem.Item = Result(data, link);
            }

            return made;
        }
    }
}
