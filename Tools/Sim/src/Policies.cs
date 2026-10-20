using System;
using System.Collections.Generic;
using F1.Data;
using F1.Gameplay;

namespace F1.Sim
{
    /// <summary>
    /// Stands in for the player. Policies are simulator tools, not game rules: they only make the
    /// inputs a player could make, through the same engine API the game uses.
    /// </summary>
    public sealed class SimPolicy
    {
        /// <summary>No player input at all.</summary>
        public static readonly SimPolicy None = new SimPolicy("none", 0, 0, 0, false, false);

        /// <summary>
        /// Drinks when someone is at death's door or badly hurt. Leaves after a death, or when two are
        /// at death's door with no potion left.
        /// </summary>
        public static readonly SimPolicy Balanced = new SimPolicy("balanced", 30, 2, 1, false, true);

        /// <summary>
        /// Drinks earlier. Leaves after a death, or as soon as one mercenary is past their grace at
        /// death's door with no potion left.
        /// </summary>
        public static readonly SimPolicy Safe = new SimPolicy("safe", 50, 1, 1, true, true);

        SimPolicy(string name, int potionHpPercent, int retreatDogCount, int retreatDeathCount, bool countOnlyPastGrace, bool active)
        {
            Name = name;
            PotionHpPercent = potionHpPercent;
            RetreatDogCount = retreatDogCount;
            RetreatDeathCount = retreatDeathCount;
            CountOnlyPastGrace = countOnlyPastGrace;
            Active = active;
        }

        public string Name { get; }

        /// <summary>Use a heal potion on a mercenary at or below this HP percent.</summary>
        public int PotionHpPercent { get; }

        /// <summary>Retreat when this many mercenaries are at death's door and no potion can help.</summary>
        public int RetreatDogCount { get; }

        /// <summary>Retreat when this many mercenaries have died in this battle.</summary>
        public int RetreatDeathCount { get; }

        /// <summary>Count a mercenary at death's door only once their grace is over, when hits start to roll.</summary>
        public bool CountOnlyPastGrace { get; }

        public bool Active { get; }

        public static SimPolicy Parse(string name)
        {
            switch (name)
            {
                case "none": return None;
                case "balanced": return Balanced;
                case "safe": return Safe;
                default: throw new ArgumentException($"Unknown policy '{name}'. Use none, balanced or safe.");
            }
        }

        /// <summary>Makes the inputs this policy wants at the current battle time.</summary>
        public void Act(BattleEngine battle)
        {
            if (!Active || battle.Result != BattleResult.Ongoing)
            {
                return;
            }

            UsePotionIfNeeded(battle);
            if (battle.Result == BattleResult.Ongoing && ShouldRetreat(battle))
            {
                battle.TryRetreat();
            }
        }

        void UsePotionIfNeeded(BattleEngine battle)
        {
            BattleUnit target = null;
            foreach (BattleUnit unit in battle.Party)
            {
                if (unit.Alive && unit.InDog)
                {
                    target = unit;
                    break;
                }
            }

            bool targetInDog = target != null;
            if (target == null)
            {
                foreach (BattleUnit unit in battle.Party)
                {
                    if (unit.Alive && unit.Hp * 100 <= unit.MaxHp * PotionHpPercent && (target == null || unit.Hp * target.MaxHp < target.Hp * unit.MaxHp))
                    {
                        target = unit;
                    }
                }
            }

            if (target == null)
            {
                return;
            }

            // A heal answers low HP and death's door; a shield is only worth it at death's door.
            int slot = FindPotion(battle, PotionEffect.Heal);
            if (slot < 0 && targetInDog)
            {
                slot = FindPotion(battle, PotionEffect.Shield);
            }

            if (slot >= 0)
            {
                battle.TryUsePotion(slot, target.Index);
            }
        }

        bool ShouldRetreat(BattleEngine battle)
        {
            if (!battle.CanRetreat)
            {
                return false;
            }

            int dead = 0;
            int inDog = 0;
            foreach (BattleUnit unit in battle.Party)
            {
                if (!unit.Alive)
                {
                    dead++;
                }
                else if (unit.InDog && (!CountOnlyPastGrace || battle.TimeMs >= unit.GraceEndMs))
                {
                    inDog++;
                }
            }

            if (dead >= RetreatDeathCount)
            {
                return true;
            }

            return inDog >= RetreatDogCount && !HasAnyPotion(battle);
        }

        static int FindPotion(BattleEngine battle, PotionEffect effect)
        {
            for (int i = 0; i < battle.Potions.Count; i++)
            {
                if (battle.Potions[i] != null && battle.Potions[i].Effect == effect)
                {
                    return i;
                }
            }

            return -1;
        }

        static bool HasAnyPotion(BattleEngine battle)
        {
            for (int i = 0; i < battle.Potions.Count; i++)
            {
                if (battle.Potions[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>Runs a battle to its end, letting the policy act after every automatic event.</summary>
        public void RunBattle(BattleEngine battle)
        {
            Act(battle);
            while (battle.Result == BattleResult.Ongoing)
            {
                battle.AdvanceTo(battle.NextAutomaticEventMs());
                Act(battle);
            }
        }

        /// <summary>A living mercenary under this share of their maximum HP makes the party rest at a camp rather than mend an item.</summary>
        const int RestBelowHpPercent = 50;

        /// <summary>
        /// At a camp: rests when someone is badly hurt or at or over the fatigue threshold (or without player input); otherwise
        /// mends (a tier up) the first item below Tier3 that works where its owner stands, from the front member back. Returns
        /// true for mending.
        /// </summary>
        /// <summary>Whether the last camp the policy decided at was spent on the forge (stage 23).</summary>
        public bool LastCampForged { get; private set; }

        public bool ChooseAtCamp(StaticData data, ExpeditionState state)
        {
            LastCampForged = false;
            bool hurt = false;
            foreach (ExpeditionMember member in state.Members)
            {
                hurt |= member.Alive && (member.Hp * 100 < member.MaxHp * RestBelowHpPercent || member.Fatigue >= data.Balance.FatigueBreakdown);
            }

            // The forge first (stage 23): a recipe ready on a board makes the unique item, at the price of the rest and the mend.
            if (Active && !hurt)
            {
                List<ForgeOption> forges = ForgeRules.Options(data, state);
                if (forges.Count > 0)
                {
                    ForgeOption forge = forges[0];
                    ForgeRules.ForgeAtCamp(data, state, forge.MemberIndex, forge.BaseItem.At.X, forge.BaseItem.At.Y);
                    LastCampForged = true;
                    return true;
                }
            }

            if (Active && !hurt)
            {
                int living = ExpeditionRules.LivingCount(state);
                var order = new List<int>();
                for (int m = 0; m < state.Members.Count; m++)
                {
                    order.Add(m);
                }

                order.Sort((a, b) => state.Members[a].Row.CompareTo(state.Members[b].Row));
                foreach (int m in order)
                {
                    ExpeditionMember member = state.Members[m];
                    foreach (BoardItem placed in member.Board.PlacedInReadingOrder())
                    {
                        if (placed.Item.Item.UsableIn(member.Row, living) && ExpeditionRules.CanUpgradeAtCamp(state, m, placed.At.X, placed.At.Y))
                        {
                            ExpeditionRules.UpgradeAtCamp(state, m, placed.At.X, placed.At.Y);
                            return true;
                        }
                    }
                }
            }

            ExpeditionRules.RestAtCamp(data, state);
            return false;
        }

        /// <summary>How many times the policy refreshes a shop's stock at most. A simulator setting, not a rule of the game.</summary>
        const int MaxRefreshes = 2;

        /// <summary>
        /// At a shop (Slice B stage 17): without player input, leaves at once. Otherwise buys what is useful while the coins last:
        /// a potion while fewer than two are held, an item that merges into one on a board (a tier up), an item that has room on the
        /// bags of a member standing where it works, a bag that has room in a member's frame (Slice B stage 19); refreshes the stock, at most <see cref="MaxRefreshes"/> times, while nothing
        /// on offer is useful and the coins cover the refresh and the cheapest offer; then leaves. Returns how many things it bought;
        /// <paramref name="merges"/> is how many of them merged into an item on a board.
        /// </summary>
        public int ShopAt(StaticData data, ExpeditionState state, out int refreshes, out int merges)
        {
            refreshes = 0;
            merges = 0;
            int bought = 0;
            if (Active)
            {
                for (int guard = 0; guard < 50; guard++)
                {
                    if (BuyOne(data, state, out bool merged))
                    {
                        bought++;
                        merges += merged ? 1 : 0;
                        continue;
                    }

                    if (refreshes >= MaxRefreshes || !ExpeditionRules.CanRefreshShop(data, state)
                        || state.Coins - ExpeditionRules.RefreshCost(data, state) < CheapestOffer(data, state))
                    {
                        break;
                    }

                    ExpeditionRules.RefreshShop(data, state);
                    refreshes++;
                }
            }

            ExpeditionRules.LeaveShop(state);
            return bought;
        }

        /// <summary>
        /// Buys the first useful offer the coins cover: a potion, a merge, an item for a member standing where it works, then a bag.
        /// <paramref name="merged"/> is true when what it bought merged into an item on a board.
        /// </summary>
        static bool BuyOne(StaticData data, ExpeditionState state, out bool merged)
        {
            merged = false;
            int potionsHeld = 0;
            foreach (string potion in state.Potions)
            {
                if (potion != null)
                {
                    potionsHeld++;
                }
            }

            IReadOnlyList<ItemOffer> stock = state.Shop.Stock;

            // A recipe's catalyst first (stage 23): onto a member whose board holds the recipe's base without a catalyst lit on it yet, where
            // its stars light the most (MemberWithStarRoomFor); a player after the unique buys the whetstone before anything else.
            for (int slot = 0; slot < stock.Count; slot++)
            {
                ItemOffer offer = stock[slot];
                if (offer == null || offer.Kind != OfferKind.Item || !ExpeditionRules.CanAfford(data, state, slot))
                {
                    continue;
                }

                foreach (RecipeData recipe in data.Recipes.Ordered)
                {
                    if (offer.Id != recipe.Catalyst)
                    {
                        continue;
                    }

                    ItemData catalyst = data.Items.Get(offer.Id);
                    int who = MemberWithStarRoomFor(data, state, catalyst, out Placement at);
                    if (who < 0 || !state.Members[who].Board.Items.Exists(i => i.Item.Item.Id == recipe.BaseItem && !StarRules.Sources(state.Members[who].Board, i).Exists(s => s.Item.Item.Id == recipe.Catalyst)))
                    {
                        continue;
                    }

                    if (ExpeditionRules.CanBuyToBoard(data, state, slot, who, at))
                    {
                        ExpeditionRules.BuyToBoard(data, state, slot, who, at);
                        return true;
                    }
                }
            }

            for (int slot = 0; slot < stock.Count; slot++)
            {
                if (stock[slot] != null && stock[slot].Kind == OfferKind.Potion && potionsHeld < 2 && ExpeditionRules.CanBuyPotion(data, state, slot))
                {
                    ExpeditionRules.BuyPotion(data, state, slot);
                    return true;
                }
            }

            for (int slot = 0; slot < stock.Count; slot++)
            {
                ItemOffer offer = stock[slot];
                if (offer == null || offer.Kind != OfferKind.Item || !ExpeditionRules.CanAfford(data, state, slot))
                {
                    continue;
                }

                var item = new EquippedItem(data.Items.Get(offer.Id), tier: offer.Tier);
                if (FindMergeTarget(data, state, item, out int member, out Placement at) && ExpeditionRules.CanBuyToBoard(data, state, slot, member, at))
                {
                    ExpeditionRules.BuyToBoard(data, state, slot, member, at);
                    merged = true;
                    return true;
                }
            }

            for (int slot = 0; slot < stock.Count; slot++)
            {
                ItemOffer offer = stock[slot];
                if (offer == null || offer.Kind != OfferKind.Item || !ExpeditionRules.CanAfford(data, state, slot))
                {
                    continue;
                }

                int member = MemberWithRoomFor(data, state, data.Items.Get(offer.Id), out Placement at);
                if (member >= 0 && ExpeditionRules.CanBuyToBoard(data, state, slot, member, at))
                {
                    ExpeditionRules.BuyToBoard(data, state, slot, member, at);
                    return true;
                }
            }

            for (int slot = 0; slot < stock.Count; slot++)
            {
                ItemOffer offer = stock[slot];
                if (offer == null || offer.Kind != OfferKind.Bag || !ExpeditionRules.CanAfford(data, state, slot))
                {
                    continue;
                }

                int member = MemberWithBagRoomFor(state, data.Bags.Get(offer.Id), out Placement at);
                if (member >= 0 && ExpeditionRules.CanBuyToBoard(data, state, slot, member, at))
                {
                    ExpeditionRules.BuyToBoard(data, state, slot, member, at);
                    return true;
                }
            }

            return false;
        }

        /// <summary>The lowest price among the offers still on sale, or 0 when nothing is.</summary>
        static int CheapestOffer(StaticData data, ExpeditionState state)
        {
            int cheapest = int.MaxValue;
            foreach (ItemOffer offer in state.Shop.Stock)
            {
                if (offer != null)
                {
                    cheapest = Math.Min(cheapest, ExpeditionRules.PriceOf(data, offer));
                }
            }

            return cheapest == int.MaxValue ? 0 : cheapest;
        }

        /// <summary>Takes the first node offered. The map is random, so this is an unbiased path.</summary>
        public static MapNode ChooseNode(List<MapNode> available)
        {
            return available[0];
        }

        /// <summary>
        /// Picks up the loot a won battle dropped (Slice B stage 18), a drop at a time: one that merges into an item on a board (a tier
        /// up), else one that has room on the bags of a mercenary standing where it works, else one that fits the inventory; a bag goes
        /// where a frame has room for it (stage 19); the rest is left. Without player input there is no merging, only what has room on a
        /// board or fits the inventory. Then merges what can merge and equips from the inventory whatever has room. Returns how many
        /// merges it made, a drop merged as it was picked up included; <paramref name="taken"/> is how many drops it took;
        /// <paramref name="keptOffByRequirement"/> is how many drops some living member had room for but no member who could wield them (Slice D stage 42).
        /// </summary>
        public int PickLoot(StaticData data, ExpeditionState state, out int taken, out int keptOffByRequirement)
        {
            taken = 0;
            keptOffByRequirement = 0;
            int merges = 0;
            for (int slot = 0; slot < state.Loot.Count && state.Phase == ExpeditionPhase.PickingLoot; slot++)
            {
                ItemOffer drop = state.Loot[slot];
                if (drop == null)
                {
                    continue;
                }

                if (drop.Kind == OfferKind.Bag)
                {
                    int holder = MemberWithBagRoomFor(state, data.Bags.Get(drop.Id), out Placement spot);
                    if (holder >= 0)
                    {
                        ExpeditionRules.TakeLoot(data, state, slot, holder, spot);
                        taken++;
                    }

                    continue;
                }

                var item = new EquippedItem(data.Items.Get(drop.Id), tier: drop.Tier);
                if (Active && FindMergeTarget(data, state, item, out int member, out Placement at) && ExpeditionRules.CanTakeLoot(data, state, slot, member, at))
                {
                    ExpeditionRules.TakeLoot(data, state, slot, member, at);
                    taken++;
                    merges++;
                    continue;
                }

                int room = MemberWithRoomFor(data, state, item.Item, out Placement free);
                if (room >= 0)
                {
                    ExpeditionRules.TakeLoot(data, state, slot, room, free);
                    taken++;
                    continue;
                }

                if (item.Item.HasRequirement && MemberWithRoomFor(data, state, item.Item, out _, heedRequirement: false) >= 0)
                {
                    keptOffByRequirement++;
                }

                if (ExpeditionRules.CanTakeLootToInventory(data, state, slot))
                {
                    ExpeditionRules.TakeLootToInventory(data, state, slot);
                    taken++;
                }
            }

            if (state.Phase == ExpeditionPhase.PickingLoot)
            {
                ExpeditionRules.LeaveLoot(state);
            }

            return merges + (taken > 0 ? Tidy(data, state) : 0);
        }

        /// <summary>
        /// After the loot: merges what can merge (an inventory item into the same one on a board, then an item on a board into
        /// the next same one found on the boards), then equips from the inventory. Without player input only the equipping.
        /// Returns how many merges it made.
        /// </summary>
        int Tidy(StaticData data, ExpeditionState state)
        {
            int merges = 0;
            bool merged = Active;
            while (merged)
            {
                merged = false;
                for (int i = 0; i < state.Inventory.Count && !merged; i++)
                {
                    if (FindMergeTarget(data, state, state.Inventory[i], out int member, out Placement at))
                    {
                        ExpeditionRules.PlaceFromInventory(data, state, i, member, at);
                        merged = true;
                    }
                }

                for (int m = 0; m < state.Members.Count && !merged; m++)
                {
                    if (!state.Members[m].Alive)
                    {
                        continue;
                    }

                    foreach (BoardItem placed in state.Members[m].Board.PlacedInReadingOrder())
                    {
                        // A base weapon is merged into, never moved away to merge (2026-10-19): the policy keeps every mercenary armed.
                        if (!placed.Item.IsBase && FindMergeTarget(data, state, placed.Item, out int member, out Placement at) && (member != m || !at.Equals(placed.At)))
                        {
                            ExpeditionRules.MoveItem(data, state, m, placed.At.X, placed.At.Y, member, at);
                            merged = true;
                            break;
                        }
                    }
                }

                merges += merged ? 1 : 0;
            }

            EquipFromInventory(data, state);
            return merges;
        }

        /// <summary>
        /// The first item on a living member's board (from the first member, in reading order) that the given item merges into, and where it lies. The member
        /// must wield the item (Slice D stage 42): what lies on its board already says it does, but the rule is asked, not assumed.
        /// </summary>
        static bool FindMergeTarget(StaticData data, ExpeditionState state, EquippedItem item, out int member, out Placement at)
        {
            for (member = 0; member < state.Members.Count; member++)
            {
                if (!state.Members[member].Alive || !ExpeditionRules.MeetsRequirement(state,member, item.Item))
                {
                    continue;
                }

                foreach (BoardItem there in state.Members[member].Board.PlacedInReadingOrder())
                {
                    if (ExpeditionRules.CanMerge(item, there.Item))
                    {
                        at = there.At;
                        return true;
                    }
                }
            }

            at = default;
            return false;
        }

        /// <summary>
        /// The first living member standing where the item works, who may wield it (Slice D stage 42: the stat it asks for), whose bags have room for it,
        /// and the first such room; or -1. A star item (Slice B stage 20) goes where its stars light the most melee weapons instead, and nowhere when they
        /// would light none.
        /// </summary>
        /// <param name="heedRequirement">False to count a member who cannot wield the item (the report's count of drops a requirement kept off a board).</param>
        static int MemberWithRoomFor(StaticData data, ExpeditionState state, ItemData item, out Placement at, bool heedRequirement = true)
        {
            if (item.Stars.Count > 0)
            {
                return MemberWithStarRoomFor(data, state, item, out at);
            }

            int living = ExpeditionRules.LivingCount(state);
            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (member.Alive && (!heedRequirement || ExpeditionRules.MeetsRequirement(state,m, item)) && item.UsableIn(member.Row, living)
                    && member.Board.FindRoom(item.Width, item.Height, out at))
                {
                    return m;
                }
            }

            at = default;
            return -1;
        }

        /// <summary>
        /// For a star item: the living member and the free placement (every turn) whose stars light the most melee weapons, the first such in
        /// member order and then reading order; -1 when no placement lights any.
        /// </summary>
        static int MemberWithStarRoomFor(StaticData data, ExpeditionState state, ItemData item, out Placement at)
        {
            int best = -1;
            int most = 0;
            at = default;
            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (!member.Alive || !ExpeditionRules.MeetsRequirement(state,m, item))
                {
                    continue;
                }

                for (int y = 0; y < BoardFrame.Height; y++)
                {
                    for (int x = 0; x < BoardFrame.Width; x++)
                    {
                        for (int turns = 0; turns < 4; turns++)
                        {
                            var spot = new Placement(x, y, turns);
                            int w = spot.WidthOf(item.Width, item.Height);
                            int h = spot.HeightOf(item.Width, item.Height);
                            if (!member.Board.OnBags(x, y, w, h) || member.Board.ItemsUnder(x, y, w, h, null).Count > 0)
                            {
                                continue;
                            }

                            int lit = 0;
                            foreach (StarMark mark in StarRules.Marks(member.Board, item, spot, null))
                            {
                                lit += mark.Lit ? 1 : 0;
                            }

                            if (lit > most)
                            {
                                most = lit;
                                best = m;
                                at = spot;
                            }
                        }
                    }
                }
            }

            return best;
        }

        /// <summary>The living member with the fewest bag squares whose frame has room for a bag, and the first such room; or -1.</summary>
        static int MemberWithBagRoomFor(ExpeditionState state, BagData bag, out Placement at)
        {
            int best = -1;
            int fewest = int.MaxValue;
            at = default;
            for (int m = 0; m < state.Members.Count; m++)
            {
                ExpeditionMember member = state.Members[m];
                if (!member.Alive || !member.Board.FindBagRoom(bag.Width, bag.Height, out Placement spot))
                {
                    continue;
                }

                int squares = 0;
                foreach (BoardBag held in member.Board.Bags)
                {
                    squares += held.Bag.Area;
                }

                if (squares < fewest)
                {
                    fewest = squares;
                    best = m;
                    at = spot;
                }
            }

            return best;
        }

        /// <summary>Puts every inventory item that has room on someone's bags on that board, in inventory order.</summary>
        static void EquipFromInventory(StaticData data, ExpeditionState state)
        {
            for (int i = 0; i < state.Inventory.Count;)
            {
                int member = MemberWithRoomFor(data, state, state.Inventory[i].Item, out Placement at);
                if (member < 0)
                {
                    i++;
                    continue;
                }

                ExpeditionRules.PlaceFromInventory(data, state, i, member, at);
            }
        }
    }
}
