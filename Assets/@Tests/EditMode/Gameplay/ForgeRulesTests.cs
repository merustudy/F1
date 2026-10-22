using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The combination of unique items (Slice D stage 50, Design/29, Docs/Design/02_Combat_System.md §4 "유니크의 조합"): a recipe's base and catalyst
    /// at their tiers side by side on one board become the unique when a battle is won; a piece is in one link at most (the first in reading order),
    /// a grey link has a tier short, and the party holds each unique once. It replaced the camp's forge of stage 23.
    /// </summary>
    public sealed class ForgeRulesTests
    {
        const string Sabre = "sabre_t";
        const string Hone = "hone_t";
        const string Unique = "gladius_t";
        const string Axe = "axe_t";
        const string Edge = "edge_t";

        /// <summary>The test data with a sabre and an axe (2x1 melee weapons), a hone, and the recipes sabre + hone -> gladius, axe + hone -> edge.</summary>
        static StaticData Data()
        {
            StaticDataParts parts = TestData.Parts();
            var items = new List<ItemData>(parts.Items)
            {
                new ItemData(Sabre, TestData.Text(Sabre), ItemCategory.Weapon, 2, 1, 2000, RowSpan.All,
                    new[] { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront, 10, 1) }, 0, melee: true),
                new ItemData(Hone, TestData.Text(Hone), ItemCategory.Other, 1, 1, 0, RowSpan.All, new ItemEffect[0], 0,
                    stars: new[] { new StarSquare(0, -1), new StarSquare(0, 1) }, starDamage: 100),
                TestData.Item(Axe, 2000, EffectKind.Damage, TargetMode.EnemyFront, 10, width: 2, melee: true),
                TestData.Item(Edge, 2000, EffectKind.Damage, TargetMode.EnemyFront, 30, width: 2, melee: true, unique: true),
            };
            parts.Items = items;
            parts.Recipes = new List<RecipeData>
            {
                new RecipeData(Unique, Unique, Sabre, ItemTier.Tier3, Hone, ItemTier.Tier3),
                new RecipeData(Edge, Edge, Axe, ItemTier.Tier3, Hone, ItemTier.Tier3),
            };
            return new StaticData(parts);
        }

        static ExpeditionState Create(StaticData data)
        {
            List<PartyMember> party = data.StartingMercenaries.Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index)).ToList();
            return ExpeditionRules.Create(data, data.Dungeons.Ordered[0].Id, 7, party);
        }

        /// <summary>The first member's board: the sabre at (1, 1) — its squares (1, 1) and (2, 1) — and a hone where asked.</summary>
        static ExpeditionState WithSabre(StaticData data, int honeX, int honeY, ItemTier sabreTier = ItemTier.Tier3, ItemTier honeTier = ItemTier.Tier3)
        {
            ExpeditionState state = Create(data);
            ExpeditionMember anna = state.Members[0];
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Sabre), isBase: true, tier: sabreTier), 1, 1);
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Hone), tier: honeTier), honeX, honeY);
            return state;
        }

        [TestCase(0, 1, TestName = "Links_AHoneOnTheLeft_IsReady")]
        [TestCase(2, 0, TestName = "Links_AHoneAbove_IsReady")]
        [TestCase(1, 2, TestName = "Links_AHoneBelow_IsReady")]
        public void Links_AHoneSideBySide_IsAReadyLink(int honeX, int honeY)
        {
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, honeX, honeY);

            List<ForgeLink> links = ForgeRules.Links(data, state);

            Assert.AreEqual(1, links.Count);
            Assert.IsTrue(links[0].Ready);
            Assert.AreEqual((0, Sabre, Hone, Unique), (links[0].MemberIndex, links[0].BaseItem.Item.Item.Id, links[0].Catalyst.Item.Item.Id, links[0].Recipe.Result));
        }

        [Test]
        public void Links_AHoneAcrossACorner_IsNoLink()
        {
            StaticData data = Data();
            Assert.IsEmpty(ForgeRules.Links(data, WithSabre(data, 0, 2)), "(0, 2) meets the sabre's (1, 1) only at a corner.");
        }

        [Test]
        public void Links_ATierShort_IsAGreyLink_AndTheWinMakesNothing()
        {
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1, honeTier: ItemTier.Tier2);

            List<ForgeLink> links = ForgeRules.Links(data, state);
            Assert.AreEqual(1, links.Count);
            Assert.IsFalse(links[0].Ready, "The hone is a tier short: the grey line.");
            Assert.IsEmpty(ForgeRules.ForgeAfterBattle(data, state));
            Assert.AreEqual(Sabre, TestBoards.ItemAt(state.Members[0], 1, 1).Item.Id);
        }

        [Test]
        public void Links_OneHoneBetweenTwoBases_LinksTheFirstInReadingOrder_Only()
        {
            // The user's Design/29 판정 3: a piece in two ready recipes takes part in the one its line shows — the first base in reading order.
            StaticData data = Data();
            ExpeditionState state = Create(data);
            ExpeditionMember anna = state.Members[0];
            anna.Board.Items.Clear();
            // The sabre (0, 0)-(1, 0), the hone at (2, 0) beside it, the axe (1, 1)-(2, 1) under the hone: both bases touch the one hone.
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Sabre), tier: ItemTier.Tier3), 0, 0);
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Hone), tier: ItemTier.Tier3), 2, 0);
            TestBoards.Put(anna, new EquippedItem(data.Items.Get(Axe), tier: ItemTier.Tier3), 1, 1);

            List<ForgeLink> links = ForgeRules.Links(data, state);

            Assert.AreEqual(1, links.Count);
            Assert.AreEqual(Sabre, links[0].BaseItem.Item.Item.Id, "The upper base is read first.");
            List<UniqueForged> made = ForgeRules.ForgeAfterBattle(data, state);
            Assert.AreEqual(new[] { Unique }, made.Select(f => f.ResultId).ToArray());
            Assert.AreEqual(Axe, TestBoards.ItemAt(anna, 1, 1).Item.Id, "The other base waits for another hone.");
        }

        [Test]
        public void Links_AReadyLinkComesFirst_EvenWhenAGreyOneIsReadEarlier()
        {
            // A base touching a 2-star hone and a 3-star one links the 3-star one: the ready links are found before the grey ones.
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1, honeTier: ItemTier.Tier2);
            TestBoards.Put(state.Members[0], new EquippedItem(data.Items.Get(Hone), tier: ItemTier.Tier3), 2, 0);

            List<ForgeLink> links = ForgeRules.Links(data, state);

            Assert.AreEqual(1, links.Count);
            Assert.IsTrue(links[0].Ready);
            Assert.AreEqual(ItemTier.Tier3, links[0].Catalyst.Item.Tier);
        }

        [Test]
        public void Links_AUniqueThePartyHolds_IsNotMadeAgain_AnotherUniqueIs()
        {
            // The user's Design/29 판정 6: each unique once in the party, other uniques beside it.
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1);
            state.Inventory.Items.Add(new BoardItem(new EquippedItem(data.Items.Get(Unique)), new Placement(0, 0)));
            Assert.IsTrue(ForgeRules.PartyHas(state, Unique));
            Assert.IsEmpty(ForgeRules.Links(data, state), "The gladius is held already: no line, nothing made.");

            ExpeditionMember bob = state.Members[1];
            TestBoards.Put(bob, new EquippedItem(data.Items.Get(Axe), tier: ItemTier.Tier3), 0, 1);
            TestBoards.Put(bob, new EquippedItem(data.Items.Get(Hone), tier: ItemTier.Tier3), 2, 1);
            List<UniqueForged> made = ForgeRules.ForgeAfterBattle(data, state);
            Assert.AreEqual(new[] { Edge }, made.Select(f => f.ResultId).ToArray(), "Another unique is made beside the gladius.");
        }

        [Test]
        public void Links_TheSameUniqueReadyTwice_MakesTheFirstOnly()
        {
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1);
            ExpeditionMember bob = state.Members[1];
            TestBoards.Put(bob, new EquippedItem(data.Items.Get(Sabre), tier: ItemTier.Tier3), 0, 1);
            TestBoards.Put(bob, new EquippedItem(data.Items.Get(Hone), tier: ItemTier.Tier3), 2, 1);

            List<UniqueForged> made = ForgeRules.ForgeAfterBattle(data, state);

            Assert.AreEqual(1, made.Count);
            Assert.AreEqual(0, made[0].MemberIndex, "The first member's, the party's order.");
            Assert.AreEqual(Sabre, TestBoards.ItemAt(bob, 0, 1).Item.Id);
        }

        [Test]
        public void ForgeAfterBattle_MakesTheUniqueInTheBasesPlace_TheCatalystGone_AndSaysWhatItWas()
        {
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1);
            ExpeditionMember anna = state.Members[0];

            List<UniqueForged> made = ForgeRules.ForgeAfterBattle(data, state);

            Assert.AreEqual(1, made.Count);
            Assert.AreEqual((Sabre, ItemTier.Tier3, 1, 1), (made[0].BaseBefore.Item.Id, made[0].BaseBefore.Tier, made[0].BaseAt.X, made[0].BaseAt.Y));
            Assert.AreEqual((Hone, 0, 1), (made[0].Catalyst.Item.Id, made[0].CatalystAt.X, made[0].CatalystAt.Y));
            EquippedItem result = TestBoards.ItemAt(anna, 1, 1);
            Assert.AreEqual(Unique, result.Item.Id);
            Assert.IsTrue(result.IsBase, "The base weapon's standing is kept.");
            Assert.AreEqual(ItemTier.Common, result.Tier, "A unique has no tier.");
            Assert.IsNull(anna.Board.ItemAt(0, 1), "The hone is used up.");
            Assert.IsEmpty(ForgeRules.Links(data, state), "Nothing left to link.");
        }

        [Test]
        public void CompleteBattle_AWonBattle_MakesTheUniqueBeforeTheLoot()
        {
            StaticData data = Data();
            ExpeditionState state = WithSabre(data, 0, 1);
            MapNode node = ExpeditionRules.AvailableNodes(state)[0];
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, node.Id));
            battle.RunToEnd();
            Assume.That(battle.Result, Is.EqualTo(BattleResult.Victory), "The first floor is won.");

            ExpeditionRules.CompleteBattle(data, state, battle);

            Assert.AreEqual(1, state.LastForged.Count);
            Assert.AreEqual(Unique, TestBoards.ItemAt(state.Members[0], 1, 1).Item.Id);
        }
    }
}
