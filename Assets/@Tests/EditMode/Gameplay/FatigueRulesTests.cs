using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>Fatigue of Docs/Design/04_Lobby_100Day_Economy.md §3: what a battle costs, and the equipment on a board.</summary>
    public sealed class FatigueRulesTests
    {
        static readonly PartyMember[] Party =
        {
            new PartyMember("anna", "tank", 1, 10),
            new PartyMember("ben", "healer", 2, 0),
            new PartyMember("cora", "striker", 3, 195),
        };

        static EquippedItem Found(StaticData data, string id, ItemCategory? category = null)
        {
            ItemData item = data.Items.Get(id);
            if (category.HasValue)
            {
                item = TestData.Item(id, item.CooldownMs, item.Effects[0].Kind, item.Effects[0].Target, category: category.Value, width: item.Width, height: item.Height);
            }

            return new EquippedItem(item, 8);
        }

        [Test]
        public void Equipment_CostsOnePerItem_WeaponsAndArmorOnly_NotABaseWeapon()
        {
            StaticData data = TestData.Data(("FatigueEquipment", 2));
            BalanceData balance = data.Balance;

            Assert.AreEqual(2, FatigueRules.ItemCost(balance, Found(data, "knife")), "A weapon that was found.");
            Assert.AreEqual(2, FatigueRules.ItemCost(balance, Found(data, "knife", ItemCategory.Armor)), "Armor.");
            Assert.AreEqual(0, FatigueRules.ItemCost(balance, Found(data, "charm")), "A support item.");
            Assert.AreEqual(0, FatigueRules.ItemCost(balance, Found(data, "knife", ItemCategory.Attack)), "An attack item.");
            Assert.AreEqual(0, FatigueRules.ItemCost(balance, Found(data, "knife", ItemCategory.Other)), "Anything else.");
            Assert.AreEqual(0, FatigueRules.ItemCost(balance, new EquippedItem(data.Items.Get("blade"), 10, isBase: true)), "A base weapon.");
            Assert.AreEqual(2, FatigueRules.ItemCost(balance, Found(data, "ballista")), "By count: three cells cost as much as one.");
        }

        [Test]
        public void ABattle_CostsTheEntry_AndEveryPieceOfEquipmentOnTheBoard()
        {
            StaticData data = TestData.Data(("FatigueBattleEntry", 5), ("FatigueEquipment", 1));
            var board = new List<EquippedItem>
            {
                new EquippedItem(data.Items.Get("blade"), 10, isBase: true),
                Found(data, "knife"),
                Found(data, "bow"),
                Found(data, "charm"),
            };

            Assert.AreEqual(2, FatigueRules.EquipmentCost(data.Balance, board));
            Assert.AreEqual(7, FatigueRules.BattleEntryCost(data.Balance, board));
        }

        [Test]
        public void Fatigue_StaysWithinZeroAndTheMaximum()
        {
            BalanceData balance = TestData.Balance(("MaxFatigue", 200));

            Assert.AreEqual(150, FatigueRules.Add(balance, 100, 50));
            Assert.AreEqual(200, FatigueRules.Add(balance, 190, 50));
            Assert.AreEqual(0, FatigueRules.Add(balance, 10, -50));
        }

        [Test]
        public void TheExpedition_LeavesWithEachMembersFatigue_AndMarksTheJobWeaponsAsBase()
        {
            StaticData data = TestData.Data();

            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);

            CollectionAssert.AreEqual(new[] { 10, 0, 195 }, state.Members.Select(m => m.Fatigue));
            Assert.IsTrue(state.Members.All(m => TestBoards.Items(m).Single().IsBase));
            Assert.Throws<System.ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[] { new PartyMember("anna", "tank", 1, 201) }));
            Assert.Throws<System.ArgumentException>(() => ExpeditionRules.Create(data, "cave", 1, new[] { new PartyMember("anna", "tank", 1, -1) }));
        }

        [Test]
        public void EnteringABattle_EveryLivingMemberPays_UpToTheMaximum_AndTheDeadPayNothing()
        {
            StaticData data = TestData.Data(("FatigueBattleEntry", 5), ("FatigueEquipment", 1), ("MaxFatigue", 200));
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);
            TestBoards.Put(state.Members[0], Found(data, "knife"), 2, 0);
            state.Members[1].Alive = false;
            state.Members[1].Hp = 0;
            state.Members[2].Row = 2;

            ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id);

            Assert.AreEqual(16, state.Members[0].Fatigue, "10, the entry 5 and the knife 1.");
            Assert.AreEqual(0, state.Members[1].Fatigue, "The dead do not go into battle.");
            Assert.AreEqual(200, state.Members[2].Fatigue, "195 and 5, never above the maximum.");
        }

        [Test]
        public void ABaseWeapon_StaysOne_OnAnotherBoard()
        {
            StaticData data = TestData.Data(("FatigueEquipment", 1));
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);
            TestBoards.Put(state.Members[0], Found(data, "knife"), 2, 0);

            ExpeditionRules.MoveItem(data, state, 0, 0, 0, 2, TestBoards.At(0, 1));

            EquippedItem moved = TestBoards.ItemAt(state.Members[2], 0, 1);
            Assert.AreEqual("blade", moved.Item.Id);
            Assert.IsTrue(moved.IsBase, "Anna's base weapon on Cora's board.");
            Assert.AreEqual(0, FatigueRules.EquipmentCost(data.Balance, TestBoards.Items(state.Members[2])), "Two base weapons cost nothing.");
            Assert.AreEqual(1, FatigueRules.EquipmentCost(data.Balance, TestBoards.Items(state.Members[0])), "The knife Anna found.");
        }

        [Test]
        public void ADrop_IsNeverABaseWeapon()
        {
            StaticData data = FlowTestKit.StrongParty();
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, Party);
            var battle = new BattleEngine(ExpeditionRules.BeginBattle(data, state, ExpeditionRules.AvailableNodes(state)[0].Id));
            battle.RunToEnd();
            ExpeditionRules.CompleteBattle(data, state, battle);
            Assert.AreEqual(ExpeditionPhase.PickingLoot, state.Phase, "The strong party wins.");
            int option = 0;

            ExpeditionRules.TakeLootToInventory(data, state, option);

            Assert.IsFalse(state.Inventory.Single().IsBase);
        }
    }
}
