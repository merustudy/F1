using System.Collections.Generic;
using System.Linq;
using F1.Data;
using F1.Gameplay;
using NUnit.Framework;

namespace F1.Tests
{
    /// <summary>
    /// The fatigue revision of Slice D stage 48 (Docs/Design/04_Lobby_100Day_Economy.md §3, Design/21 §8): a mental breakdown (an affliction) lasts until the
    /// fatigue comes down to AfflictionRecovery, and a camp's rest or the inn's treatment ends it at once; overwork slows an afflicted unit at the threshold
    /// or over; a virtue that reaches the maximum ends instead of the exhaustion; and the states' new numbers (hit, dodge, critical chance, damage dealt,
    /// fatigue gained, the allies' fatigue as a battle starts).
    /// </summary>
    public sealed class FatigueRevisionTests
    {
        static List<BattleEvent> Of(BattleEngine battle, BattleEventKind kind)
        {
            return battle.Events.Where(e => e.Kind == kind).ToList();
        }

        static FatigueStateData State(string id)
        {
            return TestData.FatigueStates().Single(s => s.Id == id);
        }

        static FatigueStateData Custom(
            string id, FatigueStateKind kind, int hit = 0, int dodge = 0, int crit = 0, int damage = 0, int fatigueGain = 0, int allyFatigue = 0, int heal = 0)
        {
            LocalizedText t = TestData.Text(id);
            return new FatigueStateData(id, t, kind, 0, heal, 0, t, hit, dodge, crit, damage, fatigueGain, allyFatigue);
        }

        static int FirstActivationMs(BattleEngine battle, int partyIndex)
        {
            return Of(battle, BattleEventKind.ItemActivated).First(e => e.Source.Side == BattleSide.Party && e.Source.Index == partyIndex).TimeMs;
        }

        // ---- When a mental breakdown ends --------------------------------------------------------

        [Test]
        public void AnAffliction_LastsUnderTheThreshold_AndEndsWhenTheFatigueComesDownToTheRecovery()
        {
            BalanceData balance = TestData.Balance(("FatigueOnKill", 5), ("AfflictionRecovery", 0));
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 50)).WithFatigue(102, State("fearful")),
                    TestData.Mercenary("b", 2, 1000, TestData.Attack(1000, 50)).WithFatigue(4, State("hopeless"))),
                TestData.Units(TestData.Enemy("e", 1, 10), TestData.Enemy("f", 2, 10), TestData.Enemy("g", 3, 1000))));

            battle.AdvanceTo(1250);

            Assert.AreEqual(97, battle.Party[0].Fatigue, "102 less a kill's 5.");
            Assert.AreEqual("fearful", battle.Party[0].State?.Id, "Under the threshold the breakdown lasts now.");
            Assert.AreEqual(0, battle.Party[1].Fatigue, "4 less 5, not below 0.");
            Assert.IsNull(battle.Party[1].State, "At the recovery (0) it ends.");
            Assert.AreEqual("hopeless", Of(battle, BattleEventKind.FatigueStateEnded).Single().Id);
        }

        [Test]
        public void FatigueRules_EndAnAfflictionAtTheRecovery_AndTellOverwork_AndTheCureLeavesAVirtue()
        {
            StaticData data = TestData.Data(("AfflictionRecovery", 0));
            BalanceData balance = data.Balance;
            FatigueStateData fearful = data.FatigueStates.Get("fearful");
            FatigueStateData focused = data.FatigueStates.Get("focused");

            Assert.IsFalse(FatigueRules.AfflictionEnds(balance, fearful, 1));
            Assert.IsTrue(FatigueRules.AfflictionEnds(balance, fearful, 0));
            Assert.IsFalse(FatigueRules.AfflictionEnds(balance, focused, 0), "A virtue lasts until the expedition ends.");

            Assert.IsTrue(FatigueRules.IsOverworked(balance, fearful, 100));
            Assert.IsTrue(FatigueRules.IsOverworked(balance, fearful, 180));
            Assert.IsFalse(FatigueRules.IsOverworked(balance, fearful, 99));
            Assert.IsFalse(FatigueRules.IsOverworked(balance, focused, 150), "Only a mental breakdown overworks.");
            Assert.IsFalse(FatigueRules.IsOverworked(balance, null, 150));

            Assert.IsNull(FatigueRules.Cure(data, "fearful"));
            Assert.AreEqual("focused", FatigueRules.Cure(data, "focused"));
            Assert.IsNull(FatigueRules.Cure(data, null));

            Assert.Throws<DataException>(() => TestData.Balance(("AfflictionRecovery", 100)), "A breakdown would end the moment it began.");
        }

        [Test]
        public void TheCampsRest_EndsEveryAffliction_WhateverTheFatigue_AndLeavesAVirtue()
        {
            StaticDataParts parts = TestData.Parts();
            parts.Dungeons = new List<DungeonData> { TestData.Pit(), new DungeonData("cave", TestData.Text("cave"), "swift", 2, 2, 3, 2, new List<string> { "tonic" }, campFloor: 2) };
            var data = new StaticData(parts);
            var party = new[] { new PartyMember("anna", "tank", 1, 110, "fearful"), new PartyMember("ben", "healer", 2, 150, "hopeless"), new PartyMember("cora", "striker", 3, 60) };
            ExpeditionState state = ExpeditionRules.Create(data, "cave", 1, party);
            state.Members[2].StateId = "focused";
            state.CurrentNodeId = state.Map.OnFloor(1)[0].Id;
            ExpeditionRules.EnterCamp(state, ExpeditionRules.AvailableNodes(state)[0].Id);

            ExpeditionRules.RestAtCamp(data, state);

            Assert.AreEqual(90, state.Members[0].Fatigue);
            Assert.IsNull(state.Members[0].StateId);
            Assert.AreEqual(130, state.Members[1].Fatigue);
            Assert.IsNull(state.Members[1].StateId, "Still over the threshold, and ended all the same.");
            Assert.AreEqual("focused", state.Members[2].StateId, "A virtue stays.");
        }

        [Test]
        public void DaysOff_EndAnAfflictionOnlyAtTheRecovery()
        {
            StaticData data = TestData.Data(("AfflictionRecovery", 0));
            RunState run = RunRules.NewRun(data, 1);
            MercenaryState anna = run.Roster.First(m => m.Id == "anna");
            anna.Fatigue = 25;
            anna.AfflictionId = "fearful";

            RunRules.Rest(data, run);
            RunRules.Rest(data, run);
            Assert.AreEqual(5, anna.Fatigue);
            Assert.AreEqual("fearful", anna.AfflictionId, "Under the threshold, above the recovery: it lasts.");

            RunRules.Rest(data, run);
            Assert.AreEqual(0, anna.Fatigue);
            Assert.IsNull(anna.AfflictionId);
        }

        // ---- Overwork ----------------------------------------------------------------------------

        [Test]
        public void Overwork_SlowsAnAfflictedUnitsItems_AtTheThresholdOrOver_NotUnderIt_NorAVirtue()
        {
            BalanceData balance = TestData.Balance(("OverworkCooldownPercent", 25));
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 1)).WithFatigue(120, State("hopeless")),
                    TestData.Mercenary("b", 2, 1000, TestData.Attack(1000, 1)).WithFatigue(60, State("hopeless")),
                    TestData.Mercenary("c", 3, 1000, TestData.Attack(1000, 1)).WithFatigue(120, State("stalwart"))),
                TestData.Units(TestData.Enemy("e", 1, 100000))));

            battle.AdvanceTo(3000);

            Assert.AreEqual(1250, FirstActivationMs(battle, 0), "Overworked: a quarter slower from the first cooldown.");
            Assert.AreEqual(1000, FirstActivationMs(battle, 1), "A breakdown under the threshold is not overwork.");
            Assert.AreEqual(1000, FirstActivationMs(battle, 2), "A virtue is never overwork.");
        }

        // ---- A virtue at the maximum ------------------------------------------------------------

        [Test]
        public void AVirtueAtTheMaximum_EndsInsteadOfTheExhaustion_AndTheFatigueComesDownToTheVirtuesValue()
        {
            BalanceData balance = TestData.Balance(("FatigueOnHit", 10), ("DogDeathChancePercent", 0));
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 1000).WithFatigue(195, State("focused")), TestData.Mercenary("b", 2, 1000)),
                TestData.Units(TestData.Enemy("e", 1, 1000, TestData.Attack(1000, 1)))));

            battle.AdvanceTo(1000);

            BattleUnit a = battle.Party[0];
            Assert.IsNull(a.State);
            Assert.AreEqual(balance.VirtueFatigue, a.Fatigue);
            Assert.IsFalse(a.InDog);
            Assert.AreEqual(0, Of(battle, BattleEventKind.Collapsed).Count, "No exhaustion.");
            Assert.AreEqual("focused", Of(battle, BattleEventKind.FatigueStateEnded).Single().Id);
            BattleEvent spent = Of(battle, BattleEventKind.FatigueChanged).Single(e => e.Id == BattleEvent.FatigueVirtueEnd);
            Assert.AreEqual(balance.VirtueFatigue - balance.MaxFatigue, spent.A);
        }

        // ---- The states' new numbers ------------------------------------------------------------

        [Test]
        public void AStatesDamagePercent_ChangesWhatItsItemsDealToTheOtherSide()
        {
            BalanceData balance = TestData.Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    TestData.Mercenary("a", 1, 1000, TestData.Attack(1000, 100)).WithFatigue(120, Custom("weak", FatigueStateKind.Affliction, damage: -25)),
                    TestData.Mercenary("b", 2, 1000, TestData.Attack(1000, 100)).WithFatigue(40, Custom("strong", FatigueStateKind.Virtue, damage: 25)),
                    TestData.Mercenary("c", 3, 1000, TestData.Attack(1000, 100))),
                TestData.Units(TestData.Enemy("e", 1, 100000))));

            battle.AdvanceTo(1000);

            List<BattleEvent> hits = Of(battle, BattleEventKind.Damaged).Where(e => e.Source.Side == BattleSide.Party).ToList();
            int plain = hits.Single(e => e.Source.Index == 2).A;
            Assert.AreEqual(plain * 75 / 100, hits.Single(e => e.Source.Index == 0).A);
            Assert.AreEqual(plain * 125 / 100, hits.Single(e => e.Source.Index == 1).A);
        }

        [Test]
        public void AStatesHitAndDodge_MoveTheHitChance()
        {
            // Every hit is certain in the test balance; a state's -15 points makes the attacker roll at 85%, and the target's +10 dodge makes the enemy roll at 90%.
            BalanceData balance = TestData.Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 100000, TestData.Attack(100, 1)).WithFatigue(120, Custom("shaky", FatigueStateKind.Affliction, hit: -15, dodge: 10))),
                TestData.Units(TestData.Enemy("e", 1, 100000, TestData.Attack(100, 1)))));

            battle.AdvanceTo(30000);

            List<BattleEvent> misses = Of(battle, BattleEventKind.Missed);
            Assert.IsTrue(misses.Any(e => e.Source.Side == BattleSide.Party), "The shaky one misses now and then.");
            Assert.IsTrue(misses.Where(e => e.Source.Side == BattleSide.Party).All(e => e.B == 8500));
            Assert.IsTrue(misses.Any(e => e.Source.Side == BattleSide.Enemy), "The enemy misses the dodging one now and then.");
            Assert.IsTrue(misses.Where(e => e.Source.Side == BattleSide.Enemy).All(e => e.B == 9000));
        }

        [Test]
        public void AStatesCritPercent_AddsToTheWeaponsCriticalChance()
        {
            BalanceData balance = TestData.Balance();
            var effects = new List<ItemEffect> { TestData.Effect(EffectKind.Damage, TargetMode.EnemyFront) };
            ItemData five = new ItemData("five", TestData.Text("five"), ItemCategory.Weapon, 1, 1, 100, RowSpan.All, effects, 0, critChancePercent: 5, school: ItemSchool.Physical);
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(TestData.Mercenary("a", 1, 1000, new EquippedItem(five)).WithFatigue(40, Custom("sharp", FatigueStateKind.Virtue, crit: 8))),
                TestData.Units(TestData.Enemy("e", 1, 1000000))));

            battle.AdvanceTo(60000);

            List<BattleEvent> crits = Of(battle, BattleEventKind.CriticalHit);
            Assert.IsTrue(crits.Count > 0);
            Assert.IsTrue(crits.All(e => e.C == 1300), "5% and the state's 8 points.");
        }

        [Test]
        public void AStatesFatigueGain_AddsToTheEndurancesPercent_ForGainsOnly()
        {
            BalanceData balance = TestData.Balance(("FatigueOnHit", 3), ("FatigueOnKill", 0));
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    TestData.Mercenary("a", 1, 1000).WithFatigue(40, Custom("calm", FatigueStateKind.Virtue, fatigueGain: -33)),
                    TestData.Mercenary("b", 2, 1000).WithFatigue(40)),
                TestData.Units(TestData.Enemy("e", 1, 1000, new EquippedItem(TestData.Item("sweep", 1000, EffectKind.Damage, TargetMode.EnemyAll), powerPercent: 1)))));

            battle.AdvanceTo(1000);

            Assert.AreEqual(42, battle.Party[0].Fatigue, "3 x 67% = 2.01, rounded to 2.");
            Assert.AreEqual(43, battle.Party[1].Fatigue);
        }

        [Test]
        public void AStatesAllyFatigue_MovesEveryAlly_AsTheBattleStarts_NotTheUnitItself()
        {
            BalanceData balance = TestData.Balance();
            BattleEngine battle = new BattleEngine(TestData.Setup(
                balance,
                TestData.Units(
                    TestData.Mercenary("a", 1, 1000).WithFatigue(120, State("hopeless")),
                    TestData.Mercenary("b", 2, 1000).WithFatigue(130, Custom("rant", FatigueStateKind.Affliction, allyFatigue: 3)),
                    TestData.Mercenary("c", 3, 1000).WithFatigue(2, Custom("cheer", FatigueStateKind.Virtue, allyFatigue: -3))),
                TestData.Units(TestData.Enemy("e", 1, 1000))));

            Assert.AreEqual(120, battle.Party[0].Fatigue, "+3 from b, -3 from c.");
            Assert.AreEqual(130 - 3, battle.Party[1].Fatigue, "Only c's -3.");
            Assert.AreEqual(5, battle.Party[2].Fatigue, "2 + 3 from b; c's own -3 is not for c.");
            List<BattleEvent> moved = Of(battle, BattleEventKind.FatigueChanged);
            Assert.IsTrue(moved.Any(e => e.Id == "rant" && e.Source.Side == BattleSide.Party && e.Source.Index == 1 && e.Target.Index == 0 && e.A == 3));
            Assert.IsTrue(moved.Any(e => e.Id == "cheer" && e.Source.Index == 2 && e.Target.Index == 1 && e.A == -3));
        }

        [Test]
        public void FatigueStateData_CountsTheNewNumbers_AndBoundsThem()
        {
            LocalizedText t = TestData.Text("x");
            Assert.DoesNotThrow(() => new FatigueStateData("hit", t, FatigueStateKind.Affliction, 0, 0, 0, t, hitPercent: -15));
            Assert.DoesNotThrow(() => new FatigueStateData("rant", t, FatigueStateKind.Affliction, 0, 0, 0, t, allyFatigueAtStart: 3));
            Assert.Throws<DataException>(() => new FatigueStateData("hit", t, FatigueStateKind.Affliction, 0, 0, 0, t, hitPercent: 101));
            Assert.Throws<DataException>(() => new FatigueStateData("dmg", t, FatigueStateKind.Virtue, 0, 0, 0, t, damagePercent: 201));
            Assert.Throws<DataException>(() => new FatigueStateData("gain", t, FatigueStateKind.Virtue, 0, 0, 0, t, fatigueGainPercent: -101));
            Assert.Throws<DataException>(() => new FatigueStateData("ally", t, FatigueStateKind.Virtue, 0, 0, 0, t, allyFatigueAtStart: 101));
        }
    }
}
