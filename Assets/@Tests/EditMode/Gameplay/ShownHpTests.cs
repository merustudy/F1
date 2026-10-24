using System.Linq;
using F1.Data;
using F1.Editor.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// Slice D stage 56 (Design/38 판정 1, the user's "상점 등에서 최대 체력 아이템 장착/해제시 바로 ui에 적용되게, 현재 체력도 비율적으로 증가/감소 …
    /// 장착/해제 반복에 의해 현재 체력이 채워지거나 감소하는 일이 없도록"): the expedition's screens show a member's HP with what its board's maximum HP
    /// items raise, the HP in proportion — the very values the battle starts with — while the member's own HP stays without the items.
    /// </summary>
    public sealed class ShownHpTests
    {
        static StaticData LoadShipped()
        {
            StaticDataFileStore store = DataTransformMenu.CreateStore();
            return StaticDataLoader.Load(file => store.ReadGenerated(file.GeneratedFileName));
        }

        /// <summary>The starting mercenaries on the mine, the warrior (constitution 6: both items' requirements) in front.</summary>
        static ExpeditionState Create(StaticData data)
        {
            var party = data.StartingMercenaries.OrderBy(m => m.JobId == "warrior" ? 0 : 1).Take(data.Balance.PartySize)
                .Select((m, index) => new PartyMember(m.Id, m.JobId, BattleRows.Front + index)).ToList();
            return ExpeditionRules.Create(data, "abandoned_mine", 7, party);
        }

        [Test]
        public void ShownHp_IsWhatTheBattleStartsWith_TheMemberKeepingItsOwn()
        {
            StaticData data = LoadShipped();
            ExpeditionState state = Create(data);
            ExpeditionMember front = state.Members[0];
            Assume.That(front.JobId, Is.EqualTo("warrior"));
            int ownMax = front.MaxHp;
            front.Hp = ownMax * 2 / 3 + 7;
            int ownHp = front.Hp;

            MemberHp bare = ExpeditionRules.ShownHp(data, state, 0);
            Assert.AreEqual((ownHp, ownMax), (bare.Hp, bare.MaxHp), "Without the items: its own.");

            // A bronze belt (8 x 2) and a silver amulet (4 x 3), with the weapon between them.
            EquippedItem weapon = front.Board.InReadingOrder().First();
            TestBoards.Lay(data, front, new EquippedItem(data.Items.Get("leather_belt"), tier: ItemTier.Tier1), weapon,
                new EquippedItem(data.Items.Get("guardian_amulet"), tier: ItemTier.Tier2));
            MemberHp shown = ExpeditionRules.ShownHp(data, state, 0);
            Assert.AreEqual(ownMax + 1600 + 1200, shown.MaxHp);
            int afterBelt = MaxHpRules.Scale(ownHp, ownMax, ownMax + 1600);
            Assert.AreEqual(MaxHpRules.Scale(afterBelt, ownMax + 1600, ownMax + 2800), shown.Hp, "In proportion, item by item, rounded down each time.");
            Assert.AreEqual((ownHp, ownMax), (front.Hp, front.MaxHp), "Showing changes nothing.");

            var battle = new BattleEngine(ExpeditionRules.BuildBattleSetup(data, state, data.EnemyGroups.Ordered.First(g => !g.IsBoss).Id, 1));
            BattleUnit unit = battle.Party.Single(u => u.Setup.SourceId == front.MercenaryId);
            Assert.AreEqual((shown.Hp, shown.MaxHp), (unit.Hp, unit.MaxHp), "The battle starts with what the screen showed.");
        }

        [Test]
        public void ShownHp_PuttingABeltOnAndTakingItOffAgainAndAgain_NeverChangesTheHp()
        {
            StaticData data = LoadShipped();
            ExpeditionState state = Create(data);
            ExpeditionMember front = state.Members[0];
            front.Hp = front.MaxHp / 3 + 1;
            int ownHp = front.Hp;
            int ownMax = front.MaxHp;
            state.Inventory.Add(new EquippedItem(data.Items.Get("leather_belt")));
            int index = state.Inventory.Count - 1;
            Placement at = Enumerable.Range(0, BoardFrame.Width * BoardFrame.Height)
                .Select(i => TestBoards.At(i % BoardFrame.Width, i / BoardFrame.Width))
                .First(p => ExpeditionRules.CanPlaceFromInventory(data, state, index, 0, p));

            MemberHp worn = default;
            for (int round = 0; round < 30; round++)
            {
                ExpeditionRules.PlaceFromInventory(data, state, index, 0, at);
                MemberHp now = ExpeditionRules.ShownHp(data, state, 0);
                if (round == 0)
                {
                    worn = now;
                    Assert.AreEqual(ownMax + 800, worn.MaxHp);
                    Assert.Greater(worn.Hp, ownHp, "It shows raised at once.");
                }

                Assert.AreEqual((worn.Hp, worn.MaxHp), (now.Hp, now.MaxHp), "Round " + round + ": worn, the same every time.");
                ExpeditionRules.MoveToInventory(data, state, 0, at.X, at.Y);
                index = state.Inventory.Count - 1;
                MemberHp off = ExpeditionRules.ShownHp(data, state, 0);
                Assert.AreEqual((ownHp, ownMax), (off.Hp, off.MaxHp), "Round " + round + ": taken off, the same HP back.");
            }

            Assert.AreEqual((ownHp, ownMax), (front.Hp, front.MaxHp));
        }

        [Test]
        public void ShownHp_OfADeadMember_IsItsOwn()
        {
            StaticData data = LoadShipped();
            ExpeditionState state = Create(data);
            ExpeditionMember front = state.Members[0];
            TestBoards.Lay(data, front, new EquippedItem(data.Items.Get("leather_belt")));
            front.Alive = false;
            front.Hp = 0;

            MemberHp shown = ExpeditionRules.ShownHp(data, state, 0);

            Assert.AreEqual((0, front.MaxHp), (shown.Hp, shown.MaxHp));
        }
    }
}
